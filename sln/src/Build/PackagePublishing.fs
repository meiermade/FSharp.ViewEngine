module PackagePublishing

open System
open System.Diagnostics
open System.IO
open System.IO.Compression
open System.Net
open System.Net.Http
open System.Security.Cryptography
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
type Package =
    | ViewEngine
    | Components

    member this.Id =
        match this with
        | Package.ViewEngine -> "FSharp.ViewEngine"
        | Package.Components -> "FSharp.ViewEngine.Components"

    member this.TagPrefix =
        match this with
        | Package.ViewEngine -> "v"
        | Package.Components -> "components/v"

type PackageDependency =
    { package:Package
      minimumVersion:string }

type Inputs =
    { package:Package
      version:string
      minimumDependency:PackageDependency option
      markLatest:bool }

type SelectionInputs =
    { name:string
      coreVersion:string
      componentsVersion:string
      core:Inputs option
      components:Inputs option }

let private stableVersionPattern = Regex("^[0-9]{4}\\.[0-9]{1,2}\\.[0-9]+$")

let private requireStableVersion (description:string) (value:string) =
    if not (stableVersionPattern.IsMatch value) then
        invalidArg description $"{description} must use YYYY.M.MINOR form, found: {value}"
    value

let validateInputs packageId version minimumDependencyVersion markLatest =
    let version = requireStableVersion "Package version" version

    match packageId, minimumDependencyVersion with
    | "FSharp.ViewEngine", None when markLatest ->
        { package = Package.ViewEngine
          version = version
          minimumDependency = None
          markLatest = true }
    | "FSharp.ViewEngine", Some _ ->
        invalidArg (nameof minimumDependencyVersion) "Core releases must not specify a minimum dependency version."
    | "FSharp.ViewEngine", None ->
        invalidArg (nameof markLatest) "Core releases must be the repository-wide Latest release."
    | "FSharp.ViewEngine.Components", Some coreVersion when not markLatest ->
        { package = Package.Components
          version = version
          minimumDependency =
            Some {
                package = Package.ViewEngine
                minimumVersion = requireStableVersion "Minimum Core version" coreVersion
            }
          markLatest = false }
    | "FSharp.ViewEngine.Components", None ->
        invalidArg (nameof minimumDependencyVersion) "Components releases require a minimum Core version."
    | "FSharp.ViewEngine.Components", Some _ ->
        invalidArg (nameof markLatest) "Components releases must not become the repository-wide Latest release."
    | "FSharp.ViewEngine.Docs", _ ->
        invalidArg (nameof packageId) "The Docs release train is retired. Publish FSharp.ViewEngine.Components instead."
    | package, _ -> invalidArg (nameof packageId) $"Unsupported package: {package}"

let validateSelection selection coreVersion componentsVersion =
    let coreVersion = requireStableVersion "Core version" coreVersion
    let componentsVersion = requireStableVersion "Components version" componentsVersion
    let create core components =
        { name = selection
          coreVersion = coreVersion
          componentsVersion = componentsVersion
          core = core
          components = components }

    match selection with
    | "core" -> create (Some(validateInputs "FSharp.ViewEngine" coreVersion None true)) None
    | "components" -> create None (Some(validateInputs "FSharp.ViewEngine.Components" componentsVersion (Some coreVersion) false))
    | "both" ->
        create
            (Some(validateInputs "FSharp.ViewEngine" coreVersion None true))
            (Some(validateInputs "FSharp.ViewEngine.Components" componentsVersion (Some coreVersion) false))
    | "docs" -> create None None
    | value -> invalidArg (nameof selection) $"Unsupported package selection: {value}"

let validateCoherence (selection:SelectionInputs) coreChanged componentsChanged =
    let validate package changed selected =
        match changed, selected with
        | true, false -> invalidOp $"{package} changed since its latest package tag and must be selected."
        | false, true -> invalidOp $"{package} has not changed since its latest package tag and must not be republished."
        | _ -> ()

    validate "FSharp.ViewEngine" coreChanged selection.core.IsSome
    validate "FSharp.ViewEngine.Components" componentsChanged selection.components.IsSome

    if selection.name = "docs" && (coreChanged || componentsChanged) then
        invalidOp "A Docs-only release cannot advertise unpublished package contract changes."

let validateLocalPackage (package:Package) version (packagePath:string) =
    let version = requireStableVersion "Minimum dependency version" version
    let expectedName = $"{package.Id}.{version}.nupkg"
    if not (File.Exists packagePath) || Path.GetFileName(packagePath) <> expectedName then
        invalidOp $"Expected selected {package.Id} package {expectedName}, found: {packagePath}"

let belongsToPackage (packageId:string) (path:string) =
    let fileName = Path.GetFileName path
    let versionStart = packageId.Length + 1
    fileName.StartsWith($"{packageId}.", StringComparison.Ordinal)
    && fileName.Length > versionStart
    && Char.IsDigit fileName[versionStart]

let expectedAssetNames packageId version =
    [ $"{packageId}.{version}.nupkg"
      $"{packageId}.{version}.snupkg"
      "SHA256SUMS" ]

let validateReleaseAssets expected actual =
    let expected = List.sort expected
    let actual = List.sort actual
    if actual <> expected then
        let expectedList = String.concat ", " expected
        let actualList = String.concat ", " actual
        invalidOp $"GitHub Release assets differ. Expected: {expectedList}. Actual: {actualList}."

let private archiveContents path =
    use archive = ZipFile.OpenRead path
    archive.Entries
    |> Seq.filter (fun entry ->
        not (String.IsNullOrEmpty entry.Name)
        && not (String.Equals(entry.FullName, ".signature.p7s", StringComparison.OrdinalIgnoreCase)))
    |> Seq.map (fun entry ->
        use stream = entry.Open()
        use content = new MemoryStream()
        stream.CopyTo content
        entry.FullName, content.ToArray())
    |> Map.ofSeq

let verifyPublishedPackage expectedPath publishedPath =
    let expected = archiveContents expectedPath
    let published = archiveContents publishedPath
    let expectedNames = expected |> Map.keys |> Set.ofSeq
    let publishedNames = published |> Map.keys |> Set.ofSeq

    if expectedNames <> publishedNames then
        let expectedList = String.concat ", " expectedNames
        let publishedList = String.concat ", " publishedNames
        invalidOp $"Published package entries differ. Expected: {expectedList}. Actual: {publishedList}."

    for KeyValue(name, expectedContent) in expected do
        if expectedContent <> published[name] then
            invalidOp $"Published package entry differs: {name}"

let private sha256 path =
    use stream = File.OpenRead path
    SHA256.HashData stream |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

let writeChecksums outputPath packagePaths =
    let lines =
        packagePaths
        |> List.map (fun path -> $"{sha256 path}  {Path.GetFileName path}")
    File.WriteAllLines(outputPath, lines)

let verifyChecksums checksumPath packageDirectory =
    for line in File.ReadAllLines checksumPath do
        let parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        if parts.Length <> 2 then invalidOp $"Invalid checksum line: {line}"
        let path = Path.Combine(packageDirectory, parts[1])
        if not (File.Exists path) then invalidOp $"Missing checksummed package asset: {path}"
        let actual = sha256 path
        if not (String.Equals(actual, parts[0], StringComparison.OrdinalIgnoreCase)) then
            invalidOp $"Checksum mismatch for {parts[1]}. Expected {parts[0]}, found {actual}."

let runProcess captureOutput command arguments =
    let startInfo = ProcessStartInfo(command)
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- captureOutput
    startInfo.RedirectStandardError <- captureOutput
    for argument in arguments do startInfo.ArgumentList.Add argument

    use childProcess = Process.Start startInfo
    let output = if captureOutput then childProcess.StandardOutput.ReadToEnd() else ""
    let error = if captureOutput then childProcess.StandardError.ReadToEnd() else ""
    childProcess.WaitForExit()

    if childProcess.ExitCode <> 0 then
        invalidOp $"{command} failed with exit code {childProcess.ExitCode}. {error.Trim()}"
    output.Trim()

let private processExitCode command arguments =
    let startInfo = ProcessStartInfo(command)
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    for argument in arguments do startInfo.ArgumentList.Add argument
    use childProcess = Process.Start startInfo
    childProcess.WaitForExit()
    childProcess.ExitCode

let private packageUrl (packageId:string) (version:string) =
    let slug = packageId.ToLowerInvariant()
    $"https://api.nuget.org/v3-flatcontainer/{slug}/{version}/{slug}.{version}.nupkg"

let private symbolsUrl (packageId:string) (version:string) =
    let fileName = $"{packageId.ToLowerInvariant()}.{version}.snupkg"
    $"https://globalcdn.nuget.org/symbol-packages/{fileName}"

let hasChangesSinceLatestTag repository tagPattern paths =
    let tags =
        runProcess true "git" [ "-C"; repository; "tag"; "--list"; tagPattern; "--sort=-version:refname" ]
        |> fun value -> value.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)

    match tags |> Array.tryHead with
    | None -> true
    | Some tag ->
        match processExitCode "git" ([ "-C"; repository; "diff"; "--quiet"; tag; "--" ] @ paths) with
        | 0 -> false
        | 1 -> true
        | code -> invalidOp $"git diff for {tagPattern} failed with exit code {code}."

let private tryDownload (client:HttpClient) (url:string) (outputPath:string) =
    use response = client.GetAsync(url).GetAwaiter().GetResult()
    if response.StatusCode = HttpStatusCode.NotFound then false
    else
        response.EnsureSuccessStatusCode() |> ignore
        use source = response.Content.ReadAsStream()
        use destination = File.Create outputPath
        source.CopyTo destination
        true

let confirmPublished packageId version =
    use client = new HttpClient()
    use response = client.GetAsync(packageUrl packageId version).GetAwaiter().GetResult()
    response.IsSuccessStatusCode

let downloadPublishedArtifacts packageId version outputDirectory attempts (delay:TimeSpan) =
    Directory.CreateDirectory outputDirectory |> ignore
    let packagePath = Path.Combine(outputDirectory, $"{packageId}.{version}.nupkg")
    let symbolsPath = Path.Combine(outputDirectory, $"{packageId}.{version}.snupkg")
    use client = new HttpClient()

    let rec loop remaining =
        let packageReady = tryDownload client (packageUrl packageId version) packagePath
        let symbolsReady = tryDownload client (symbolsUrl packageId version) symbolsPath
        if packageReady && symbolsReady then packagePath
        elif remaining <= 1 then invalidOp $"{packageId} {version} package and symbols were not available from NuGet in time."
        else
            Threading.Thread.Sleep delay
            loop (remaining - 1)

    loop attempts

let publishOrVerify packagePath packageId version apiKey temporaryDirectory =
    Directory.CreateDirectory temporaryDirectory |> ignore
    let publishedPath = Path.Combine(temporaryDirectory, "published.nupkg")
    use client = new HttpClient()

    if tryDownload client (packageUrl packageId version) publishedPath then
        verifyPublishedPackage packagePath publishedPath
        printfn "%s %s already exists on NuGet and matches the verified artifact." packageId version
    else
        runProcess false "dotnet" [
            "nuget"; "push"; packagePath
            "--source"; "https://api.nuget.org/v3/index.json"
            "--api-key"; apiKey
        ] |> ignore

let waitForPublished packageId version attempts (delay:TimeSpan) =
    let rec loop remaining =
        if confirmPublished packageId version then ()
        elif remaining <= 1 then invalidOp $"{packageId} {version} was not available from NuGet in time."
        else
            Threading.Thread.Sleep delay
            loop (remaining - 1)
    loop attempts

let ensureTag repository tag commit =
    let head = runProcess true "git" [ "-C"; repository; "rev-parse"; "HEAD" ]
    if head <> commit then invalidOp $"Release commit is {commit}, but HEAD is {head}."

    if processExitCode "git" [ "-C"; repository; "rev-parse"; "--verify"; $"refs/tags/{tag}" ] = 0 then
        let existingCommit = runProcess true "git" [ "-C"; repository; "rev-list"; "-n"; "1"; tag ]
        if existingCommit <> commit then invalidOp $"Release tag {tag} points to {existingCommit}, not {commit}."
    else
        runProcess false "git" [ "-C"; repository; "tag"; tag; commit ] |> ignore
        runProcess false "git" [ "-C"; repository; "push"; "origin"; $"refs/tags/{tag}" ] |> ignore

let reconcileGitHubRelease repository packageId version tag previousTag markLatest (assetPaths:string list) =
    let expectedAssets = assetPaths |> List.map Path.GetFileName |> List.sort
    let latestArgument = if markLatest then "--latest" else "--latest=false"
    let common = [ "--repo"; repository ]

    if processExitCode "gh" ([ "release"; "view"; tag ] @ common) = 0 then
        let existingAssets =
            runProcess true "gh" ([ "release"; "view"; tag ] @ common @ [ "--json"; "assets"; "--jq"; ".assets[].name" ])
            |> fun output -> output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries) |> Array.sort |> Array.toList

        try validateReleaseAssets expectedAssets existingAssets
        with :? InvalidOperationException as error -> invalidOp $"GitHub Release {tag}: {error.Message}"

        runProcess false "gh" ([ "release"; "edit"; tag ] @ common @ [ latestArgument ]) |> ignore
    else
        let notes =
            match previousTag with
            | Some previous -> [ "--generate-notes"; "--notes-start-tag"; previous ]
            | None -> [ "--notes"; $"Initial {packageId} package release. See https://fve.meiermade.com/changelog for release details." ]

        runProcess false "gh" (
            [ "release"; "create"; tag ]
            @ common
            @ [ "--verify-tag"; "--title"; $"{packageId} {version}"; latestArgument ]
            @ notes
            @ assetPaths)
        |> ignore
