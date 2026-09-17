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

let private verifyDependency dependentPackageId expectedDependencyId expectedVersion (archive:ZipArchive) =
    let nuspecEntry =
        archive.Entries
        |> Seq.filter (fun entry -> entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
        |> exactlyOne "NuSpec entry"

    let document = nuspecEntry |> entryText |> XDocument.Parse
    let attributeValue name (element:XElement) =
        match element.Attribute(XName.Get name) with
        | null -> ""
        | attribute -> attribute.Value

    let dependencies =
        document.Descendants()
        |> Seq.filter (fun element ->
            element.Name.LocalName = "dependency"
            && attributeValue "id" element = expectedDependencyId)
        |> Seq.toList

    match dependencies with
    | [ dependency ] ->
        let actualVersion = attributeValue "version" dependency
        if actualVersion <> expectedVersion then
            fail $"Expected {dependentPackageId} {expectedDependencyId} dependency {expectedVersion}, found {actualVersion}"
    | dependencies -> fail $"Expected exactly one {dependentPackageId} {expectedDependencyId} dependency, found {dependencies.Length}"

let private verifyComponentsContents (archive:ZipArchive) =
    let entries = archive.Entries |> Seq.map _.FullName |> Set.ofSeq
    for required in [ "LICENSE"; "README.md"; "contentFiles/any/any/FSharp.ViewEngine.Components.tailwind.css"; "contentFiles/any/any/AppMode.tailwind.css"; "contentFiles/any/any/app-mode.js"; "contentFiles/any/any/Documentation/Documentation.tailwind.css" ] do
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
open FSharp.ViewEngine.Components.Primitives
open FSharp.ViewEngine.Components.Application
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

type PackageDestination = Home | Values | Value of int | Reports
let destinationUrl = function Home -> "/" | Values -> "/values" | Value id -> $"/values/{id}" | Reports -> "/reports"

let packageBreadcrumbs =
    Breadcrumbs.create "package-breadcrumbs" "Breadcrumb" [
        BreadcrumbItem.create Home "Home"
        BreadcrumbItem.create Values "Values"
        BreadcrumbItem.create (Value 42) "Value 42" ]

let packageNavigation =
    SideNav.create
        "package-navigation"
        "Package navigation"
        (SideNavHeader.create "Package smoke" |> SideNavHeader.withContent (strong { icon; " Package smoke" }))
        [ SideNavSection.group "Manage" [
              SideNavItem.create Home "Home"
              SideNavItem.create Values "Values"
              SideNavItem.unavailable "Unavailable" ]
          SideNavSection.group "Analyze" [ SideNavItem.create Reports "Reports" ] ]
    |> SideNav.withCurrent Values
    |> SideNav.withWidth SideNavWidth.Standard
    |> SideNav.withContext (p { "Default workspace" })
    |> SideNav.withMobileContext (p { "Default workspace" })
    |> SideNav.withFooter (a { _href "/account"; "Account" })

let packagePage =
    let actions =
        ActionCluster.create "package-page-actions" [
            ApplicationAction.command "$refreshes++" "Refresh"
            ApplicationAction.link Reports "Reports" ]
        |> ActionCluster.withOverflow [ MenuItem.link Home "Home" ]
    let topBar =
        PageTopBar.create ()
        |> PageTopBar.withContent (div { _class "flex min-h-[var(--fve-shell-bar-min-height)] items-center px-4"; Breadcrumbs.render destinationUrl packageBreadcrumbs })
    PageHeader.create "Value 42"
    |> PageHeader.withSubtitle "Current package value"
    |> PageHeader.withActions actions
    |> fun pageHeader -> Page.create pageHeader (p { "Value details" })
    |> Page.withTopBar topBar
    |> Page.withWidth PageWidth.Reading
    |> Page.render destinationUrl

let packageShell =
    AppShell.create "package-shell" packageNavigation packagePage
    |> AppShell.withTheme ComponentsTheme.emerald
    |> AppShell.withBreakpoint AppShellBreakpoint.Large
    |> AppShell.withBoundary AppShellBoundary.Container
    |> AppShell.render destinationUrl

let packageSection =
    SectionHeader.create "Activity"
    |> SectionHeader.withDivider
    |> SectionHeader.withDescription "Recent package activity."
    |> SectionHeader.withActions (ActionCluster.create "package-section-actions" [ ApplicationAction.link Reports "View reports" ])
    |> fun sectionHeader -> Section.create sectionHeader (p { "No recent activity." })
    |> Section.withSurface SectionSurface.Plain
    |> Section.render destinationUrl

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
        Table.create "Values" [
            Table.column "Value" text |> Table.asRowHeader |> Table.asMobilePrimary
            Table.rowActionsColumn (fun value ->
                RowActions.create $"{value}-actions" value [ MenuItem.link Values "View values" ]
                |> RowActions.render destinationUrl) ] [ "One" ]
        |> Table.withSelection (TableSelection.create "package-selection" id id |> TableSelection.withSelectedKeys [ "One" ])
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.withVisibleCaption
        |> Table.withSurface TableSurface.Plain
        |> Table.render
        DescriptionList.create [ DetailField.text "Type" "Asset" ]
        |> DescriptionList.withColumns DescriptionListColumns.Four
        |> DescriptionList.render
        Metric.text "Balance" "$42,800"
        |> Metric.withTrend "Up 8%"
        |> Metric.render
        Pagination.create "Value pages" [ PaginationItem.current 1; PaginationItem.link 2 2 ]
        |> Pagination.withNext 2
        |> Pagination.render (fun page -> $"/values?page={page}")
        Select.create "status" "Status" id [ Select.option "active" "Active"; Select.option "disabled" "Disabled" |> Select.disable ]
        |> Select.withId "package-status"
        |> Select.withPlaceholder "Choose status"
        |> Select.required
        |> Select.withValidation "Choose a status."
        |> Select.render
        Select.create "account" "Account" id [ Select.option "operating" "Operating" ]
        |> Select.withSearch (SelectSearch.Remote "/accounts/search")
        |> Select.withSelected "operating"
        |> Select.render
        Select.create "packageMemberIds" "Members" id [ Select.option "alex" "Alex"; Select.option "jamie" "Jamie" ]
        |> Select.multiple
        |> Select.withSelectedMany [ "alex"; "jamie" ]
        |> Select.render
        Select.create "packageSearchIds" "Search members" id [ Select.option "alex" "Alex"; Select.option "jamie" "Jamie" ]
        |> Select.multiple
        |> Select.withSelectedMany [ "alex"; "jamie" ]
        |> Select.required
        |> Select.withQuery ""
        |> Select.withSearch (SelectSearch.Remote "/members/search")
        |> Select.render
        Select.create "loading-account" "Loading account" id []
        |> Select.loading
        |> Select.withLoadingMessage "Loading accounts"
        |> Select.render
        Select.create "error-account" "Error account" id []
        |> Select.withSearch SelectSearch.Static
        |> Select.withError "Accounts could not be loaded."
        |> Select.render
        Input.create "contactEmail" "Contact email"
        |> Input.withId "package-email"
        |> Input.withLeadingIcon icon
        |> Input.withType InputType.Email
        |> Input.withValue "account@example.test"
        |> Input.withDescription "For correspondence."
        |> Input.required
        |> Input.withValidation "Use the account address."
        |> Input.render
        Input.create "price" "Price"
        |> Input.withId "package-price"
        |> Input.withSuffix "USD"
        |> Input.withValue "12.50"
        |> Input.render
        Input.create "website" "Website"
        |> Input.withPrefix "https://"
        |> Input.render
        Input.create "query" "Search values"
        |> Input.withType InputType.Search
        |> Input.render
        Textarea.create "notes" "Notes"
        |> Textarea.withRows 3
        |> Textarea.withValue "Preserved while checking."
        |> Textarea.pending
        |> Textarea.render
        ErrorSummary.create "package-errors" "Check details" [ FieldError.create "package-email" "Contact email" "Use the account address." ]
        |> ErrorSummary.render
        Notice.create "package-feedback" "Details checked" (p { "Continue with the reviewed values." })
        |> Notice.withTone Tone.Positive
        |> Notice.withAnnouncement NoticeAnnouncement.Polite
        |> Notice.render
        FileSelection.create "package-files" "files" "Files"
        |> FileSelection.withAccept ".csv"
        |> FileSelection.multiple
        |> FileSelection.pending
        |> FileSelection.render
        TagInput.create "package-tags" "tags" "Tags" [ "reviewed" ]
        |> TagInput.render
        Progress.create "Import progress" 3 4
        |> Progress.render
        Steps.create "Import steps" [ Step.create "Upload" StepState.Complete |> Step.withDestination "/upload"; Step.create "Review" StepState.Current ]
        |> Steps.render id
        Calendar.create "Package schedule" CalendarView.Week "September 21–27" [ CalendarEvent.create "package-event" "Review package" "September 24" "/events/1" ]
        |> Calendar.withViewDestinations [ CalendarView.Week, "/schedule?view=week" ]
        |> Calendar.withError "Package schedule failed."
        |> Calendar.withStateAction (a { _href "/schedule/retry"; "Retry schedule" })
        |> Calendar.render id
        MediaLibrary.create "package-media" "Package media" "assetIds" [ MediaAsset.create "package-image" "Package image" "/package.png" "Package preview" "/media/1" |> MediaAsset.primary ]
        |> MediaLibrary.withSelected [ "package-image" ]
        |> MediaLibrary.render id
        Checkbox.create "confirmed" "Confirmed"
        |> Checkbox.withId "package-confirmed"
        |> Checkbox.required
        |> Checkbox.withIndeterminate
        |> Checkbox.withVisuallyHiddenLabel
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
        packageSection
        Section.withoutHeader "Period note" (p { "Current period" }) |> Section.render destinationUrl
        Page.create (PageHeader.create "Posting flow") (p { "Graph workspace" })
        |> Page.withBodyLayout PageBodyLayout.Canvas
        |> Page.render destinationUrl
        packageDialog |> Dialog.render
        packageConfirmation |> ConfirmationDialog.trigger "Delete value"
        packageConfirmation |> ConfirmationDialog.render
        packageDrawer |> Drawer.trigger "Open settings"
        packageDrawer |> Drawer.render
        packageShell
    }

for removedType in [ "FSharp.ViewEngine.Components.Chart"; "FSharp.ViewEngine.Components.ChartConfig"; "FSharp.ViewEngine.Components.Primitives.Chart"; "FSharp.ViewEngine.Components.Primitives.ChartConfig" ] do
    if not (isNull (typeof<TableSurface>.Assembly.GetType removedType)) then
        failwith $"Removed public API remains in the package: {removedType}"

let actual = view |> Render.toString
if not (actual.Contains "fve-components fve-theme-sky")
   || not (actual.Contains "type=\"button\"")
   || not (actual.Contains "bg-[var(--fve-brand-solid)]")
   || not (actual.Contains ">Create account</button>")
   || not (actual.Contains "aria-busy=\"true\"")
   || not (actual.Contains "aria-label=\"Add account\"")
   || not (actual.Contains "role=\"status\"")
   || not (actual.Contains "No accounts")
   || not (System.Text.RegularExpressions.Regex.IsMatch(actual, "<input id=\"package-files\"[^>]*disabled[^>]*aria-busy=\"true\""))
   || not (actual.Contains "package_tags_values")
   || not (actual.Contains "<progress")
   || not (actual.Contains "aria-current=\"step\"")
   || not (actual.Contains "aria-label=\"Calendar view\"")
   || not (actual.Contains "Package schedule failed.")
   || not (actual.Contains "Retry schedule")
   || not (actual.Contains "data-fve-media-library=\"true\"")
   || not (actual.Contains "<caption")
   || not (actual.Contains "fve-table-records")
   || not (actual.Contains "fve-table-selection-change")
   || not (actual.Contains "package_confirmed_mixed: true")
   || not (actual.Contains "data-fve-page-canvas=\"true\"")
   || not (actual.Contains "<dl")
   || not (actual.Contains "Trend: ")
   || not (actual.Contains "aria-current=\"page\"")
   || not (actual.Contains "role=\"combobox\"")
   || not (actual.Contains "aria-label=\"Clear Search Account\"")
   || not (actual.Contains "requestCancellation: &#39;auto&#39;")
   || not (actual.Contains "Loading accounts")
   || not (actual.Contains "Accounts could not be loaded.")
   || not (actual.Contains "aria-required=\"true\"")
   || not (actual.Contains "aria-describedby=\"package-price-suffix\"")
   || not (actual.Contains "aria-multiselectable=\"true\"")
   || not (actual.Contains "id=\"fve-select-packagesearchids-selection\"")
   || (System.Text.RegularExpressions.Regex.Matches(actual, "name=\"packageMemberIds\"").Count <> 2)
   || (System.Text.RegularExpressions.Regex.Matches(actual, "name=\"packageSearchIds\"").Count <> 2)
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
   || not (actual.Contains "left-0 ml-0 mr-auto border-r")
   || not (actual.Contains "id=\"package-breadcrumbs\"")
   || not (actual.Contains "aria-label=\"Package navigation\"")
   || not (actual.Contains ">Value 42</h1>")
   || not (actual.Contains "data-fve-page-top-bar=\"true\"")
   || not (actual.Contains "data-fve-section-header=\"true\"")
   || not (actual.Contains "aria-label=\"More actions for One\"")
   || not (actual.Contains "data-fve-page-scroll=\"true\"")
   || not (actual.Contains "id=\"package-shell\"")
   || not (actual.Contains "aria-label=\"Open navigation\"")
   || not (actual.Contains "<main") then
    failwith $"Components package rendered unexpected HTML: {actual}"

printfn "FSharp.ViewEngine.Components package works on %s" System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
"""

let private docsConsumerProgram =
    """open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Documentation
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

let site =
    DocsSite.create "Package smoke" "home"
    |> DocsSite.withNavigation navigation
    |> DocsSite.withBrandMark (span { "S" })
    |> DocsSite.withStorageKey "package-smoke"
    |> DocsSite.withTheme DocsTheme.amber

let page =
    DocumentationPage.create "guide" "Guide" |> DocumentationPage.withDescription "Package verification" |> DocumentationPage.withSections [
        DocumentationSection.create "example" "Example" [
            Example.codeFirst "smoke-example" "Smoke example" "fsharp" "div { \"ok\" }" (div { "ok" }) ]
        DocumentationSection.create "diagram" "Diagram" [
            diagram |> SequenceDiagram.render |> Mermaid.create |> Mermaid.render ] ]
    |> DocumentationPage.withPager (DocsPager.create (Some(DocsPageLink.create "Home" "/")) None)

let actual =
    Document.create site page
    |> Document.render
    |> Render.toString
if not (actual.Contains "class=\"spec-shell\"")
   || not (actual.Contains "data-docs-example=\"true\"")
   || not (actual.Contains "aria-label=\"Page navigation\"")
   || not (actual.Contains "data-mermaid-source=\"sequenceDiagram")
   || not (actual.Contains "data-mermaid-state=\"pending\"")
   || not (actual.Contains "rel=\"stylesheet\" href=\"/css/compiled.css\"")
   || actual.Contains "<style"
   || actual.Contains ">sequenceDiagram" then
    failwith "documentation document did not render expected components and external stylesheet contract"

let graph: DirectedGraph<Destination> =
    { nodes = [ Home; Guide ]
      roots = [ Home ]
      edges = [ Home, Guide ] }

match DirectedGraph.validate graph with
| [] -> ()
| issues -> failwith $"unexpected graph issues: {issues}"

printfn "Components Documentation works on %s" System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
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
          consumerProgram =
            [ "Controls", componentsConsumerProgram; "Documentation", docsConsumerProgram ]
            |> List.map (fun (name, source) ->
                "module " + name + " =\n" + (source.Split('\n') |> Array.map (fun line -> "    " + line) |> String.concat "\n"))
            |> String.concat "\n\n" }
    elif PackagePublishing.belongsToPackage "FSharp.ViewEngine" packagePath then
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
        verifyDependency definition.packageId "FSharp.ViewEngine" expectedCoreVersion packageArchive
        verifyComponentsContents packageArchive
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
