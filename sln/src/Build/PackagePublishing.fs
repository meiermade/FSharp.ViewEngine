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
    | Docs

    member this.Id =
        match this with
        | Package.ViewEngine -> "FSharp.ViewEngine"
        | Package.Docs -> "FSharp.ViewEngine.Docs"

    member this.TagPrefix =
        match this with
        | Package.ViewEngine -> "v"
        | Package.Docs -> "docs/v"

type Inputs =
    { package:Package
      version:string
      minimumCoreVersion:string option
      markLatest:bool }

type SelectionInputs =
    { core:Inputs option
      docs:Inputs option }

let private stableVersionPattern = Regex("^[0-9]{4}\\.[0-9]{1,2}\\.[0-9]+$")

let private requireStableVersion (description:string) (value:string) =
    if not (stableVersionPattern.IsMatch value) then
        invalidArg description $"{description} must use YYYY.M.MINOR form, found: {value}"
    value

let validateInputs packageId version minimumCoreVersion markLatest =
    let version = requireStableVersion "Package version" version

    match packageId, minimumCoreVersion with
    | "FSharp.ViewEngine", None when markLatest ->
        { package = Package.ViewEngine
          version = version
          minimumCoreVersion = None
          markLatest = true }
    | "FSharp.ViewEngine", Some _ ->
        invalidArg (nameof minimumCoreVersion) "Core releases must not specify a minimum Core version."
    | "FSharp.ViewEngine", None ->
        invalidArg (nameof markLatest) "Core releases must be the repository-wide Latest release."
    | "FSharp.ViewEngine.Docs", Some coreVersion when not markLatest ->
        { package = Package.Docs
          version = version
          minimumCoreVersion = Some(requireStableVersion "Minimum Core version" coreVersion)
          markLatest = false }
    | "FSharp.ViewEngine.Docs", None ->
        invalidArg (nameof minimumCoreVersion) "Docs releases require a minimum Core version."
    | "FSharp.ViewEngine.Docs", Some _ ->
        invalidArg (nameof markLatest) "Docs releases must not become the repository-wide Latest release."
    | package, _ -> invalidArg (nameof packageId) $"Unsupported package: {package}"

let validateSelection selection coreVersion docsVersion minimumCoreVersion =
    let optionalValue = Option.filter (String.IsNullOrWhiteSpace >> not)
    let coreVersion = optionalValue coreVersion
    let docsVersion = optionalValue docsVersion
    let minimumCoreVersion = optionalValue minimumCoreVersion

    match selection, coreVersion, docsVersion, minimumCoreVersion with
    | "core", Some version, None, None ->
        { core = Some(validateInputs "FSharp.ViewEngine" version None true)
          docs = None }
    | "docs", None, Some version, Some coreVersion ->
        { core = None
          docs = Some(validateInputs "FSharp.ViewEngine.Docs" version (Some coreVersion) false) }
    | "both", Some coreVersion, Some docsVersion, Some minimumCoreVersion ->
        { core = Some(validateInputs "FSharp.ViewEngine" coreVersion None true)
          docs = Some(validateInputs "FSharp.ViewEngine.Docs" docsVersion (Some minimumCoreVersion) false) }
    | "core", _, _, _ ->
        invalidArg (nameof selection) "Core selection requires only a Core version."
    | "docs", _, _, _ ->
        invalidArg (nameof selection) "Docs selection requires only a Docs package version and minimum Core version."
    | "both", _, _, _ ->
        invalidArg (nameof selection) "Both selection requires Core, Docs package, and minimum Core versions."
    | value, _, _, _ -> invalidArg (nameof selection) $"Unsupported package selection: {value}"

let validateLocalCorePackage version (packagePath:string) =
    let version = requireStableVersion "Minimum Core version" version
    let expectedName = $"FSharp.ViewEngine.{version}.nupkg"
    if not (File.Exists packagePath) || Path.GetFileName(packagePath) <> expectedName then
        invalidOp $"Expected selected Core package {expectedName}, found: {packagePath}"

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
            | None -> [ "--notes"; $"Initial {packageId} package release. See https://fsharpviewengine.meiermade.com/changelog for release details." ]

        runProcess false "gh" (
            [ "release"; "create"; tag ]
            @ common
            @ [ "--verify-tag"; "--title"; $"{packageId} {version}"; latestArgument ]
            @ notes
            @ assetPaths)
        |> ignore
