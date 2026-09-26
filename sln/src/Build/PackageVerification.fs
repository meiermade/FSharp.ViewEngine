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

let private verifyCliContents (archive:ZipArchive) =
    let entries = archive.Entries |> Seq.map _.FullName |> Set.ofSeq
    for required in
        [ "tools/net8.0/any/DotnetToolSettings.xml"
          "tools/net8.0/any/FSharp.ViewEngine.Cli.dll"
          "tools/net8.0/any/FSharp.ViewEngine.Cli.deps.json"
          "tools/net8.0/any/FSharp.ViewEngine.Cli.runtimeconfig.json" ] do
        if not (entries.Contains required) then fail $"CLI package is missing {required}"

let private runExecutable (workDirectory:string) (executable:string) (arguments:string list) =
    let startInfo = ProcessStartInfo(executable)
    startInfo.WorkingDirectory <- workDirectory
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    for argument in arguments do startInfo.ArgumentList.Add argument
    use childProcess = Process.Start startInfo
    let output = childProcess.StandardOutput.ReadToEndAsync()
    let error = childProcess.StandardError.ReadToEndAsync()
    childProcess.WaitForExit()
    if childProcess.ExitCode <> 0 then
        fail $"{executable} failed with exit code {childProcess.ExitCode}.{Environment.NewLine}{output.Result}{Environment.NewLine}{error.Result}"
    output.Result.Trim()

let private verifyCliInstallation (runDotnet:string -> string list -> unit) (packagePath:string) (version:string) repositoryCommit =
    let packageDirectory = Path.GetDirectoryName packagePath
    let workDirectory = Path.Combine(Path.GetTempPath(), $"fsharp-viewengine-cli-package.{Guid.NewGuid():N}")
    Directory.CreateDirectory workDirectory |> ignore

    try
        let manifestDirectory = Path.Combine(workDirectory, "manifest")
        Directory.CreateDirectory manifestDirectory |> ignore
        runDotnet manifestDirectory [ "new"; "tool-manifest" ]
        runDotnet manifestDirectory [ "tool"; "install"; "FSharp.ViewEngine.Cli"; "--version"; version; "--add-source"; packageDirectory; "--local" ]
        runDotnet manifestDirectory [ "fve"; "--version" ]

        let projectPath = Path.Combine(manifestDirectory, "Acme.Components.fsproj")
        let configPath = Path.Combine(manifestDirectory, "fve.json")
        runDotnet manifestDirectory [ "fve"; "init"; projectPath; "--namespace"; "Acme.Components"; "--framework"; "net8.0" ]
        runDotnet manifestDirectory [ "fve"; "add"; "button"; "text-field"; "--config"; configPath ]
        runDotnet manifestDirectory [ "restore"; projectPath; "--source"; packageDirectory; "--source"; "https://api.nuget.org/v3/index.json" ]
        runDotnet manifestDirectory [ "build"; projectPath; "--no-restore" ]

        let toolDirectory = Path.Combine(workDirectory, "tool-path")
        runDotnet workDirectory [ "tool"; "install"; "FSharp.ViewEngine.Cli"; "--version"; version; "--add-source"; packageDirectory; "--tool-path"; toolDirectory ]
        let executable = Path.Combine(toolDirectory, if OperatingSystem.IsWindows() then "fve.exe" else "fve")
        let actualVersion = runExecutable workDirectory executable [ "--version" ]
        let expectedVersion = $"{version}+{repositoryCommit}"
        if actualVersion <> expectedVersion then
            fail $"Expected CLI version {expectedVersion}, found {actualVersion}"
    finally
        if Directory.Exists workDirectory then Directory.Delete(workDirectory, true)

let private verifyCommonPackageMetadata packageId (archive:ZipArchive) =
    let entries = archive.Entries |> Seq.map _.FullName |> Set.ofSeq
    for required in [ "LICENSE"; "README.md" ] do
        if not (entries.Contains required) then fail $"{packageId} package is missing {required}"

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
    if metadataValue "id" <> packageId then fail $"{packageId} package ID is incorrect"
    if metadataValue "readme" <> "README.md" then fail $"{packageId} package README metadata is incorrect"
    let license =
        document.Descendants()
        |> Seq.filter (fun element -> element.Name.LocalName = "license")
        |> exactlyOne "license element"
    let licenseType = license.Attribute(XName.Get "type")
    if isNull licenseType || licenseType.Value <> "file" || license.Value <> "LICENSE" then
        fail $"{packageId} package license metadata is incorrect"

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

let verify runDotnet packagePath =
    let packagePath = Path.GetFullPath packagePath
    let cliPackageId = "FSharp.ViewEngine.Cli"
    let corePackageId = "FSharp.ViewEngine"

    if PackagePublishing.belongsToPackage cliPackageId packagePath then
        let version = packageVersion cliPackageId packagePath
        use packageArchive = ZipFile.OpenRead packagePath
        verifyCommonPackageMetadata cliPackageId packageArchive
        verifyCliContents packageArchive
        let repositoryCommit = repositoryMetadata packageArchive
        verifyCliInstallation runDotnet packagePath version repositoryCommit
    elif PackagePublishing.belongsToPackage corePackageId packagePath then
        let packageDirectory = Path.GetDirectoryName packagePath
        let version = packageVersion corePackageId packagePath
        let symbolsPackagePath = Path.ChangeExtension(packagePath, ".snupkg")

        use packageArchive = ZipFile.OpenRead packagePath
        verifyPackageContents corePackageId packageArchive
        verifyCommonPackageMetadata corePackageId packageArchive
        let repositoryCommit = repositoryMetadata packageArchive
        verifySymbols corePackageId repositoryCommit symbolsPackagePath

        let workDirectory = Path.Combine(Path.GetTempPath(), $"fsharp-viewengine-package.{Guid.NewGuid():N}")
        Directory.CreateDirectory workDirectory |> ignore

        try
            let packagesDirectory = Path.Combine(workDirectory, "packages")

            for framework in testFrameworks () do
                let projectDirectory = Path.Combine(workDirectory, framework)
                runDotnet workDirectory [ "new"; "console"; "--language"; "F#"; "--framework"; framework; "--output"; projectDirectory; "--no-restore" ]
                File.WriteAllText(Path.Combine(projectDirectory, "Program.fs"), viewEngineConsumerProgram)

                runDotnet projectDirectory [ "add"; "package"; corePackageId; "--version"; version; "--source"; packageDirectory; "--no-restore" ]
                runDotnet projectDirectory [ "restore"; "--packages"; packagesDirectory; "--source"; packageDirectory; "--source"; "https://api.nuget.org/v3/index.json" ]
                verifySelectedAsset corePackageId framework projectDirectory
                runDotnet projectDirectory [ "run"; "--framework"; framework; "--no-restore" ]

                File.WriteAllText(Path.Combine(projectDirectory, "Program.fs"), invalidFragmentConsumerProgram)
                verifyFragmentAttributeRejection projectDirectory framework
        finally
            if Directory.Exists workDirectory then Directory.Delete(workDirectory, true)
    else
        fail $"Unexpected package name: {Path.GetFileName packagePath}"
