module PackageVerification

open System
open System.IO
open System.IO.Compression
open System.Diagnostics
open System.Reflection.Metadata
open System.Text.Json
open System.Text.RegularExpressions
open System.Xml.Linq

let private sourceLinkKind = Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A")

let private fail message = raise (InvalidOperationException message)

let private exactlyOne description values =
    match values |> Seq.toList with
    | [ value ] -> value
    | values -> fail $"Expected exactly one {description}, found {values.Length}"

let private entryFrameworks fileName (archive:ZipArchive) =
    let pattern = Regex($"^lib/(net[0-9]+\\.[0-9]+)/{Regex.Escape fileName}$")

    archive.Entries
    |> Seq.choose (fun entry ->
        let matched = pattern.Match entry.FullName
        if matched.Success then Some matched.Groups[1].Value else None)
    |> Seq.distinct
    |> Seq.sort
    |> String.concat " "

let private entryText (entry:ZipArchiveEntry) =
    use stream = entry.Open()
    use reader = new StreamReader(stream)
    reader.ReadToEnd()

let private verifyPackageContents assemblyName (archive:ZipArchive) =
    let frameworks = entryFrameworks $"{assemblyName}.dll" archive
    if frameworks <> "net8.0" then
        let found = if String.IsNullOrEmpty frameworks then "none" else frameworks
        fail $"Expected only lib/net8.0 for {assemblyName}, found: {found}"

    if archive.Entries |> Seq.exists (fun entry -> entry.FullName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) then
        fail "The main package must not contain PDB files"

let private repositoryMetadata (archive:ZipArchive) =
    let nuspecEntry =
        archive.Entries
        |> Seq.filter (fun entry -> entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
        |> exactlyOne "NuSpec entry"

    let document = nuspecEntry |> entryText |> XDocument.Parse
    let repository =
        document.Descendants()
        |> Seq.filter (fun element -> element.Name.LocalName = "repository")
        |> exactlyOne "repository element"

    let attribute name =
        match repository.Attribute(XName.Get name) with
        | null -> ""
        | value -> value.Value

    let repositoryType = attribute "type"
    let repositoryUrl = attribute "url"
    let repositoryCommit = attribute "commit"

    if repositoryType <> "git"
       || repositoryUrl <> "https://github.com/meiermade/FSharp.ViewEngine"
       || not (Regex.IsMatch(repositoryCommit, "^[0-9a-f]{40}$")) then
        fail "Package repository metadata is missing its GitHub URL or commit"

    repositoryCommit

let private sourceLinkMappings (pdbEntry:ZipArchiveEntry) =
    use entryStream = pdbEntry.Open()
    use stream = new MemoryStream()
    entryStream.CopyTo stream
    stream.Position <- 0L
    use provider = MetadataReaderProvider.FromPortablePdbStream stream
    let reader = provider.GetMetadataReader()

    reader.CustomDebugInformation
    |> Seq.choose (fun handle ->
        let information = reader.GetCustomDebugInformation handle
        if reader.GetGuid(information.Kind) = sourceLinkKind then
            Some(reader.GetBlobBytes information.Value)
        else
            None)
    |> Seq.collect (fun json ->
        use document = JsonDocument.Parse json
        let mutable documents = Unchecked.defaultof<JsonElement>
        if document.RootElement.TryGetProperty("documents", &documents) then
            documents.EnumerateObject()
            |> Seq.choose (fun mapping ->
                if mapping.Value.ValueKind = JsonValueKind.String then mapping.Value.GetString() |> Option.ofObj
                else None)
            |> Seq.toArray
        else
            Array.empty)
    |> Seq.toList

let private verifySymbols assemblyName repositoryCommit symbolsPackagePath =
    if not (File.Exists symbolsPackagePath) then
        fail $"Missing symbol package: {symbolsPackagePath}"

    use symbolsArchive = ZipFile.OpenRead symbolsPackagePath
    let frameworks = entryFrameworks $"{assemblyName}.pdb" symbolsArchive
    if frameworks <> "net8.0" then
        let found = if String.IsNullOrEmpty frameworks then "none" else frameworks
        fail $"Expected only lib/net8.0 symbols for {assemblyName}, found: {found}"

    let pdbEntry =
        symbolsArchive.Entries
        |> Seq.filter (fun entry -> entry.FullName = $"lib/net8.0/{assemblyName}.pdb")
        |> exactlyOne "portable PDB entry"

    let sourceUrl = $"https://raw.githubusercontent.com/meiermade/FSharp.ViewEngine/{repositoryCommit}/"
    let hasExpectedMapping =
        pdbEntry
        |> sourceLinkMappings
        |> List.exists (fun mapping -> mapping.StartsWith(sourceUrl, StringComparison.Ordinal))

    if not hasExpectedMapping then
        fail "Portable PDB does not map Source Link to the packaged repository commit"

let rec private containsProperty expectedName (element:JsonElement) =
    match element.ValueKind with
    | JsonValueKind.Object ->
        element.EnumerateObject()
        |> Seq.exists (fun property -> property.Name = expectedName || containsProperty expectedName property.Value)
    | JsonValueKind.Array -> element.EnumerateArray() |> Seq.exists (containsProperty expectedName)
    | _ -> false

let private verifySelectedAsset assemblyName framework projectDirectory =
    let assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json")
    use document = JsonDocument.Parse(File.ReadAllText assetsPath)

    if not (containsProperty $"lib/net8.0/{assemblyName}.dll" document.RootElement) then
        fail $"{framework} did not select the net8.0 compatibility asset for {assemblyName}"

let private packageVersion packageId (packagePath:string) =
    let matched = Regex.Match(Path.GetFileName packagePath, $"^{Regex.Escape packageId}\\.(.+)\\.nupkg$")
    if matched.Success then matched.Groups[1].Value
    else fail $"Unexpected package name: {Path.GetFileName packagePath}"

let private verifyCoreDependency dependentPackageId expectedVersion (archive:ZipArchive) =
    let nuspecEntry =
        archive.Entries
        |> Seq.filter (fun entry -> entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
        |> exactlyOne "NuSpec entry"

    let document = nuspecEntry |> entryText |> XDocument.Parse
    let attributeValue name (element:XElement) =
        match element.Attribute(XName.Get name) with
        | null -> ""
        | attribute -> attribute.Value

    let coreDependencies =
        document.Descendants()
        |> Seq.filter (fun element ->
            element.Name.LocalName = "dependency"
            && attributeValue "id" element = "FSharp.ViewEngine")
        |> Seq.toList

    match coreDependencies with
    | [ dependency ] ->
        let actualVersion = attributeValue "version" dependency
        if actualVersion <> expectedVersion then
            fail $"Expected {dependentPackageId} FSharp.ViewEngine dependency {expectedVersion}, found {actualVersion}"
    | dependencies -> fail $"Expected exactly one {dependentPackageId} FSharp.ViewEngine dependency, found {dependencies.Length}"

let private verifyComponentsContents (archive:ZipArchive) =
    let entries = archive.Entries |> Seq.map _.FullName |> Set.ofSeq
    for required in [ "LICENSE"; "README.md"; "contentFiles/any/any/FSharp.ViewEngine.Components.tailwind.css" ] do
        if not (entries.Contains required) then fail $"Components package is missing {required}"

    let nuspecEntry =
        archive.Entries
        |> Seq.filter (fun entry -> entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
        |> exactlyOne "NuSpec entry"
    let document = nuspecEntry |> entryText |> XDocument.Parse
    let metadataValue name =
        document.Descendants()
        |> Seq.filter (fun element -> element.Name.LocalName = name)
        |> exactlyOne $"{name} element"
        |> _.Value
    if metadataValue "id" <> "FSharp.ViewEngine.Components" then fail "Components package ID is incorrect"
    if metadataValue "readme" <> "README.md" then fail "Components package README metadata is incorrect"
    let license =
        document.Descendants()
        |> Seq.filter (fun element -> element.Name.LocalName = "license")
        |> exactlyOne "license element"
    let licenseType = license.Attribute(XName.Get "type")
    if isNull licenseType || licenseType.Value <> "file" || license.Value <> "LICENSE" then
        fail "Components package license metadata is incorrect"

let private testFrameworks () =
    let configured =
        match Environment.GetEnvironmentVariable "PACKAGE_TEST_FRAMEWORKS" with
        | value when String.IsNullOrWhiteSpace value -> "net8.0 net9.0 net10.0"
        | value -> value

    Regex.Split(configured.Trim(), "\\s+")
    |> Array.filter (String.IsNullOrWhiteSpace >> not)

let private viewEngineConsumerProgram =
    """open System.Reflection
open FSharp.ViewEngine
open type Html

let children = [ span { "One" }; span { "Two" } ]
let sequence = children |> Seq.map id

let direct = div { children; sequence; Seq.empty<HtmlElement> } |> Render.toString
if direct <> "<div><span>One</span><span>Two</span><span>One</span><span>Two</span></div>" then
    failwith $"unexpected direct collection render: {direct}"

let yielded = div { yield! children; yield! sequence } |> Render.toString
if yielded <> direct then
    failwith $"unexpected yielded collection render: {yielded}"

let bareFragment = fragment { "Items: "; children } |> Render.toString
let qualifiedFragment = Html.fragment { yield! children } |> Render.toString
if bareFragment <> "Items: <span>One</span><span>Two</span>" || qualifiedFragment <> "<span>One</span><span>Two</span>" then
    failwith $"unexpected fragments: {bareFragment} / {qualifiedFragment}"

let bareTitle = title { "Package smoke" } |> Render.toString
let qualifiedTitle = Html.title { _lang "en"; "Package smoke" } |> Render.toString
if bareTitle <> "<title>Package smoke</title>" || qualifiedTitle <> "<title lang=\"en\">Package smoke</title>" then
    failwith $"unexpected titles: {bareTitle} / {qualifiedTitle}"

let publicStatic = BindingFlags.Public ||| BindingFlags.Static
let publicInstance = BindingFlags.Public ||| BindingFlags.Instance
let fragmentAcceptsAttribute =
    typeof<FragmentBuilder>.GetMethods(publicInstance)
    |> Array.filter (fun methodInfo -> methodInfo.Name = "Yield")
    |> Array.collect (fun methodInfo -> methodInfo.GetParameters())
    |> Array.exists (fun parameter -> parameter.ParameterType = typeof<HtmlAttribute>)

if isNull (typeof<Html>.GetProperty("fragment", publicStatic))
   || isNull (typeof<Html>.GetProperty("title", publicStatic))
   || not (isNull (typeof<Html>.GetProperty("titleBuilder", publicStatic)))
   || (typeof<Html>.GetMethods(publicStatic) |> Array.exists (fun methodInfo -> methodInfo.Name = "fragment" || methodInfo.Name = "title"))
   || fragmentAcceptsAttribute then
    failwith "unexpected fragment/title public API"

printfn "FSharp.ViewEngine package works on %s" System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
"""

let private invalidFragmentConsumerProgram =
    """open FSharp.ViewEngine
open type Html

let invalid = fragment { _class "not-allowed" }
printfn "%A" invalid
"""

let private verifyFragmentAttributeRejection projectDirectory framework =
    let startInfo = ProcessStartInfo("dotnet")
    startInfo.WorkingDirectory <- projectDirectory
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    for argument in [ "build"; "--framework"; framework; "--no-restore" ] do
        startInfo.ArgumentList.Add argument

    use childProcess = Process.Start startInfo
    let output = childProcess.StandardOutput.ReadToEndAsync()
    let error = childProcess.StandardError.ReadToEndAsync()
    childProcess.WaitForExit()
    let diagnostics = $"{output.Result}{Environment.NewLine}{error.Result}"

    if childProcess.ExitCode = 0 then
        fail $"Expected {framework} fragment attributes to fail compilation"

    for expected in [ "FS0041"; "HtmlAttribute"; "FragmentBuilder.Yield" ] do
        if not (diagnostics.Contains(expected, StringComparison.Ordinal)) then
            fail $"Expected {framework} fragment rejection to contain '{expected}'. Diagnostics: {diagnostics}"

    printfn "Verified expected FS0041 fragment-attribute rejection on %s" framework

let private componentsConsumerProgram =
    """open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html

let icon = span { "+" }

let packageDialog =
    Dialog.create "package-dialog" "Review value" (p { "Review the current value." })
    |> Dialog.withDescription "Confirm before continuing."
    |> Dialog.withInitialFocus "package-dialog-close"
    |> Dialog.dismissOnBackdrop

let packageConfirmation =
    ConfirmationDialog.create "package-confirmation" "Delete value?" "This cannot be undone." "Keep value" "Delete value" "@post('/values/delete')"
    |> ConfirmationDialog.withValidation "The value is still referenced."

let packageDrawer =
    Drawer.create "package-drawer" "Value settings" (nav { _ariaLabel "Value settings"; a { _href "/values"; "Values" } })
    |> Drawer.withSide DrawerSide.Start

let view =
    div {
        for attribute in ComponentsTheme.attributes ComponentsTheme.sky do
            attribute
        Button.primary "Create account"
        Button.create "Sync accounts" |> Button.pending |> Button.render
        IconButton.create "Add account" icon |> IconButton.render
        Badge.create "New" |> Badge.withTone Tone.Brand |> Badge.render
        LoadingIndicator.create "Loading accounts" |> LoadingIndicator.render
        EmptyState.create "No accounts" "Create an account to begin."
        |> EmptyState.withActions (Button.primary "Create account")
        |> EmptyState.render
        Table.create "Values" [ Table.column "Value" text |> Table.asRowHeader ] [ "One" ]
        |> Table.withVisibleCaption
        |> Table.render
        DescriptionList.create [ DetailField.text "Type" "Asset" ]
        |> DescriptionList.render
        Metric.text "Balance" "$42,800"
        |> Metric.withTrend "Up 8%"
        |> Metric.render
        Pagination.create "Value pages" [ PaginationItem.current 1; PaginationItem.link 2 2 ]
        |> Pagination.withNext 2
        |> Pagination.render (fun page -> $"/values?page={page}")
        Chart.create "value-chart" "Value history" (p { "Value increased." }) (raw "<svg aria-hidden=\"true\"></svg>")
        |> Chart.render
        Select.create "status" "Status" id [ Select.option "active" "Active"; Select.option "disabled" "Disabled" |> Select.disable ]
        |> Select.withId "package-status"
        |> Select.withPlaceholder "Choose status"
        |> Select.required
        |> Select.withValidation "Choose a status."
        |> Select.render
        Combobox.create "account" "Account" id [ Select.option "operating" "Operating" ]
        |> Combobox.withSearch (ComboboxSearch.Remote "/accounts/search")
        |> Combobox.withSelected "operating"
        |> Combobox.clearable
        |> Combobox.render
        Combobox.create "loading-account" "Loading account" id []
        |> Combobox.loading
        |> Combobox.withLoadingMessage "Loading accounts"
        |> Combobox.render
        Combobox.create "error-account" "Error account" id []
        |> Combobox.withError "Accounts could not be loaded."
        |> Combobox.pending
        |> Combobox.render
        Checkbox.create "confirmed" "Confirmed"
        |> Checkbox.withId "package-confirmed"
        |> Checkbox.required
        |> Checkbox.pending
        |> Checkbox.render
        Switch.create "notifications" "Notifications"
        |> Switch.withId "package-notifications"
        |> Switch.withChecked
        |> Switch.withValidation "Could not save."
        |> Switch.render
        ToggleButton.create "package-compact" "Compact rows"
        |> ToggleButton.pending
        |> ToggleButton.render
        Tabs.create "package-tabs" "Value sections" [
            Tab.create "overview" "Overview" (p { "Value summary" })
            Tab.create "activity" "Activity" (p { "Value activity" }) ]
        |> Tabs.withSelected "activity"
        |> Tabs.withVariant TabsVariant.Underlined
        |> Tabs.render
        RadioGroup.create "mode" "Mode" id [ RadioGroup.option "automatic" "Automatic"; RadioGroup.option "manual" "Manual" ]
        |> RadioGroup.withId "package-mode"
        |> RadioGroup.required
        |> RadioGroup.withSelected "automatic"
        |> RadioGroup.render
        DropdownMenu.create "value-actions" "Actions" [
            MenuItem.group "Values" [
                MenuItem.link 1 "View value"
                |> MenuItem.withLeading icon
                |> MenuItem.withShortcut "V"
                MenuItem.action "$refreshValues++" "Refresh values"
                MenuItem.action "$exportValues++" "Export values" |> MenuItem.disabled
                MenuItem.action "$syncValues++" "Syncing values" |> MenuItem.pending ]
            MenuItem.separator
            MenuItem.destructiveAction "$deleteValue++" "Delete value" ]
        |> DropdownMenu.withAlignment MenuAlignment.Start
        |> DropdownMenu.render (fun value -> $"/values/{value}")
        packageDialog |> Dialog.trigger "Review value"
        packageDialog |> Dialog.render
        packageConfirmation |> ConfirmationDialog.trigger "Delete value"
        packageConfirmation |> ConfirmationDialog.render
        packageDrawer |> Drawer.trigger "Open settings"
        packageDrawer |> Drawer.render
    }

let actual = view |> Render.toString
if not (actual.Contains "fve-components fve-theme-sky")
   || not (actual.Contains "type=\"button\"")
   || not (actual.Contains "bg-[var(--fve-brand-solid)]")
   || not (actual.Contains ">Create account</button>")
   || not (actual.Contains "aria-busy=\"true\"")
   || not (actual.Contains "aria-label=\"Add account\"")
   || not (actual.Contains "role=\"status\"")
   || not (actual.Contains "No accounts")
   || not (actual.Contains "<caption")
   || not (actual.Contains "<dl")
   || not (actual.Contains "Trend: ")
   || not (actual.Contains "aria-current=\"page\"")
   || not (actual.Contains "<figure")
   || not (actual.Contains "role=\"combobox\"")
   || not (actual.Contains "aria-label=\"Clear Account\"")
   || not (actual.Contains "requestCancellation: &#39;auto&#39;")
   || not (actual.Contains "Loading accounts")
   || not (actual.Contains "Accounts could not be loaded.")
   || not (actual.Contains "aria-required=\"true\"")
   || not (actual.Contains "name=\"confirmed\"")
   || not (actual.Contains "role=\"switch\"")
   || not (actual.Contains "aria-pressed=\"false\"")
   || not (actual.Contains "role=\"tablist\"")
   || not (actual.Contains "role=\"tabpanel\"")
   || not (actual.Contains "aria-selected:border-[var(--fve-brand-solid)]")
   || not (actual.Contains "role=\"radiogroup\"")
   || not (actual.Contains "role=\"group\"")
   || not (actual.Contains "left-0")
   || not (actual.Contains "data-fve-menu-label=\"view value\"")
   || not (actual.Contains "aria-busy=\"true\"")
   || not (actual.Contains ">V</kbd>")
   || not (actual.Contains "id=\"package-dialog-trigger\"")
   || not (actual.Contains "backdrop:bg-[var(--fve-overlay-backdrop)]")
   || not (actual.Contains "role=\"alertdialog\"")
   || not (actual.Contains "data-indicator:_package_confirmation_pending")
   || not (actual.Contains "Value settings")
   || not (actual.Contains "left-0 ml-0 mr-auto border-r") then
    failwith $"Components package rendered unexpected HTML: {actual}"

printfn "FSharp.ViewEngine.Components package works on %s" System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
"""

let private docsConsumerProgram =
    """open FSharp.ViewEngine
open FSharp.ViewEngine.Docs
open type Html

type Destination = Home | Guide

let person = SequenceDiagram.participant "Person" "Person"
let app = SequenceDiagram.participant "App" "Application"
let diagram = SequenceDiagram.sequence [ person; app ] [ SequenceDiagram.call person app "Use product" ]

let navigation =
    [ Nav.page "home" "Overview" "/" Home
      Nav.group "guides" "Guides" true [ Nav.page "guide" "Guide" "/guide" Guide ] ]

match Navigation.validate navigation with
| [] -> ()
| issues -> failwith $"unexpected navigation issues: {issues}"

let site: DocsSite<Destination> =
    { name = "Package smoke"
      baseUrl = None
      description = None
      repository = None
      brandMark = span { "S" }
      homeId = "home"
      navigation = navigation
      storageKey = "package-smoke"
      defaultColorMode = DocsColorMode.System
      theme = DocsTheme.amber
      assets = DocsAssets.defaults
      search = [] }

let page =
    docsArticle "guide" "Guide" "Package verification" [
        docsSection "example" "Example" [
            docsCustom (docsExample "smoke-example" "Smoke example" "fsharp" "div { \"ok\" }" (div { "ok" })) ]
        docsSection "diagram" "Diagram" [ docsSequence diagram ] ]
    |> docsWithPager (docsPager (Some(docsPageLink "Home" "/")) None)

let actual = docsDocument site page |> Render.toString
if not (actual.Contains "class=\"spec-shell\"") || not (actual.Contains "data-docs-example=\"true\"") || not (actual.Contains "aria-label=\"Page navigation\"") || not (actual.Contains "data-mermaid-source=\"sequenceDiagram") || not (actual.Contains "data-mermaid-state=\"pending\"") || actual.Contains ">sequenceDiagram" then
    failwith "documentation document did not render expected components"

let graph: DirectedGraph<Destination> =
    { nodes = [ Home; Guide ]
      roots = [ Home ]
      edges = [ Home, Guide ] }

match DirectedGraph.validate graph with
| [] -> ()
| issues -> failwith $"unexpected graph issues: {issues}"

printfn "FSharp.ViewEngine.Docs package works on %s" System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
"""

type private PackageDefinition =
    { packageId:string
      assemblyName:string
      consumerProgram:string }

let private packageDefinition (packagePath:string) =
    let fileName = Path.GetFileName packagePath

    if fileName.StartsWith("FSharp.ViewEngine.Components.", StringComparison.Ordinal) then
        { packageId = "FSharp.ViewEngine.Components"
          assemblyName = "FSharp.ViewEngine.Components"
          consumerProgram = componentsConsumerProgram }
    elif fileName.StartsWith("FSharp.ViewEngine.Docs.", StringComparison.Ordinal) then
        { packageId = "FSharp.ViewEngine.Docs"
          assemblyName = "FSharp.ViewEngine.Docs"
          consumerProgram = docsConsumerProgram }
    elif fileName.StartsWith("FSharp.ViewEngine.", StringComparison.Ordinal) then
        { packageId = "FSharp.ViewEngine"
          assemblyName = "FSharp.ViewEngine"
          consumerProgram = viewEngineConsumerProgram }
    else
        fail $"Unexpected package name: {fileName}"

let verify runDotnet packagePath =
    let packagePath = Path.GetFullPath packagePath
    let packageDirectory = Path.GetDirectoryName packagePath
    let definition = packageDefinition packagePath
    let version = packageVersion definition.packageId packagePath
    let symbolsPackagePath = Path.ChangeExtension(packagePath, ".snupkg")

    use packageArchive = ZipFile.OpenRead packagePath
    verifyPackageContents definition.assemblyName packageArchive

    match definition.packageId with
    | "FSharp.ViewEngine.Components" ->
        let expectedCoreVersion = Environment.GetEnvironmentVariable "COMPONENTS_MINIMUM_CORE_VERSION"
        if String.IsNullOrWhiteSpace expectedCoreVersion then
            fail "COMPONENTS_MINIMUM_CORE_VERSION is required when verifying FSharp.ViewEngine.Components"
        verifyCoreDependency definition.packageId expectedCoreVersion packageArchive
        verifyComponentsContents packageArchive
    | "FSharp.ViewEngine.Docs" ->
        let expectedCoreVersion = Environment.GetEnvironmentVariable "DOCS_MINIMUM_CORE_VERSION"
        if String.IsNullOrWhiteSpace expectedCoreVersion then
            fail "DOCS_MINIMUM_CORE_VERSION is required when verifying FSharp.ViewEngine.Docs"
        verifyCoreDependency definition.packageId expectedCoreVersion packageArchive
    | _ -> ()

    let repositoryCommit = repositoryMetadata packageArchive
    verifySymbols definition.assemblyName repositoryCommit symbolsPackagePath

    let workDirectory = Path.Combine(Path.GetTempPath(), $"fsharp-viewengine-package.{Guid.NewGuid():N}")
    Directory.CreateDirectory workDirectory |> ignore

    try
        let packagesDirectory = Path.Combine(workDirectory, "packages")

        for framework in testFrameworks () do
            let projectDirectory = Path.Combine(workDirectory, framework)
            runDotnet workDirectory [ "new"; "console"; "--language"; "F#"; "--framework"; framework; "--output"; projectDirectory; "--no-restore" ]
            File.WriteAllText(Path.Combine(projectDirectory, "Program.fs"), definition.consumerProgram)

            runDotnet projectDirectory [ "add"; "package"; definition.packageId; "--version"; version; "--source"; packageDirectory; "--no-restore" ]
            runDotnet projectDirectory [ "restore"; "--packages"; packagesDirectory; "--source"; packageDirectory; "--source"; "https://api.nuget.org/v3/index.json" ]
            verifySelectedAsset definition.assemblyName framework projectDirectory
            runDotnet projectDirectory [ "run"; "--framework"; framework; "--no-restore" ]

            if definition.packageId = "FSharp.ViewEngine" then
                File.WriteAllText(Path.Combine(projectDirectory, "Program.fs"), invalidFragmentConsumerProgram)
                verifyFragmentAttributeRejection projectDirectory framework
    finally
        if Directory.Exists workDirectory then Directory.Delete(workDirectory, true)
