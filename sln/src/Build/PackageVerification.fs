module PackageVerification

open System
open System.IO
open System.IO.Compression
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

let private verifyPackageContents (archive:ZipArchive) =
    let frameworks = entryFrameworks "FSharp.ViewEngine.dll" archive
    if frameworks <> "net8.0" then
        let found = if String.IsNullOrEmpty frameworks then "none" else frameworks
        fail $"Expected only lib/net8.0, found: {found}"

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

let private verifySymbols repositoryCommit symbolsPackagePath =
    if not (File.Exists symbolsPackagePath) then
        fail $"Missing symbol package: {symbolsPackagePath}"

    use symbolsArchive = ZipFile.OpenRead symbolsPackagePath
    let frameworks = entryFrameworks "FSharp.ViewEngine.pdb" symbolsArchive
    if frameworks <> "net8.0" then
        let found = if String.IsNullOrEmpty frameworks then "none" else frameworks
        fail $"Expected only lib/net8.0 symbols, found: {found}"

    let pdbEntry =
        symbolsArchive.Entries
        |> Seq.filter (fun entry -> entry.FullName = "lib/net8.0/FSharp.ViewEngine.pdb")
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

let private verifySelectedAsset framework projectDirectory =
    let assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json")
    use document = JsonDocument.Parse(File.ReadAllText assetsPath)

    if not (containsProperty "lib/net8.0/FSharp.ViewEngine.dll" document.RootElement) then
        fail $"{framework} did not select the net8.0 compatibility asset"

let private packageVersion (packagePath:string) =
    let matched = Regex.Match(Path.GetFileName packagePath, "^FSharp\\.ViewEngine\\.(.+)\\.nupkg$")
    if matched.Success then matched.Groups[1].Value
    else fail $"Unexpected package name: {Path.GetFileName packagePath}"

let private testFrameworks () =
    let configured =
        match Environment.GetEnvironmentVariable "PACKAGE_TEST_FRAMEWORKS" with
        | value when String.IsNullOrWhiteSpace value -> "net8.0 net9.0 net10.0"
        | value -> value

    Regex.Split(configured.Trim(), "\\s+")
    |> Array.filter (String.IsNullOrWhiteSpace >> not)

let private consumerProgram =
    """open FSharp.ViewEngine
open type Html

let actual = div { _class "package-smoke"; "ok" } |> Render.toString
if actual <> "<div class=\"package-smoke\">ok</div>" then
    failwith $"unexpected render: {actual}"

printfn "FSharp.ViewEngine package works on %s" System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
"""

let verify runDotnet packagePath =
    let packagePath = Path.GetFullPath packagePath
    let packageDirectory = Path.GetDirectoryName packagePath
    let version = packageVersion packagePath
    let symbolsPackagePath = Path.ChangeExtension(packagePath, ".snupkg")

    use packageArchive = ZipFile.OpenRead packagePath
    verifyPackageContents packageArchive
    let repositoryCommit = repositoryMetadata packageArchive
    verifySymbols repositoryCommit symbolsPackagePath

    let workDirectory = Path.Combine(Path.GetTempPath(), $"fsharp-viewengine-package.{Guid.NewGuid():N}")
    Directory.CreateDirectory workDirectory |> ignore

    try
        for framework in testFrameworks () do
            let projectDirectory = Path.Combine(workDirectory, framework)
            runDotnet workDirectory [ "new"; "console"; "--language"; "F#"; "--framework"; framework; "--output"; projectDirectory; "--no-restore" ]
            File.WriteAllText(Path.Combine(projectDirectory, "Program.fs"), consumerProgram)

            runDotnet projectDirectory [ "add"; "package"; "FSharp.ViewEngine"; "--version"; version; "--source"; packageDirectory; "--no-restore" ]
            runDotnet projectDirectory [ "restore"; "--source"; packageDirectory; "--source"; "https://api.nuget.org/v3/index.json" ]
            verifySelectedAsset framework projectDirectory
            runDotnet projectDirectory [ "run"; "--framework"; framework; "--no-restore" ]
    finally
        if Directory.Exists workDirectory then Directory.Delete(workDirectory, true)
