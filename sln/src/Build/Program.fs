open System.Net
open System.Net.Sockets
open System.Text.RegularExpressions
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

System.Environment.GetCommandLineArgs()
|> Array.tail
|> Array.toList
|> Context.FakeExecutionContext.Create false "build.fsx"
|> Context.RuntimeContext.Fake
|> Context.setExecutionContext

let inline (==>!) x y = x ==> y |> ignore

let srcDir = Path.getDirectory __SOURCE_DIRECTORY__
let slnDir = Path.getDirectory srcDir
let rootDir = Path.getDirectory slnDir
let nugetsDir = rootDir </> "nugets"
let testsDir = srcDir </> "Tests"
let docsDir = srcDir </> "Docs"
let docsTestsDir = srcDir </> "Docs.Tests"
let buildTestsDir = srcDir </> "Build.Tests"
let benchmarksDir = srcDir </> "Benchmarks"
let changelogPath = docsDir </> "src" </> "Pages" </> "Changelog.fs"
let releaseRepository = Environment.environVarOrDefault "RELEASE_REPOSITORY" rootDir
let releaseMetadataPath =
    Environment.environVarOrDefault
        "RELEASE_METADATA_PATH"
        (__SOURCE_DIRECTORY__ </> "obj" </> "release-metadata.json")

let exec workDir cmd args =
    CreateProcess.fromRawCommand cmd args
    |> CreateProcess.withWorkingDirectory workDir
    |> CreateProcess.ensureExitCode
    |> Proc.start
    |> Async.AwaitTask
    |> Async.Ignore

let execEnv key value workDir cmd args =
    CreateProcess.fromRawCommand cmd args
    |> CreateProcess.setEnvironmentVariable key value
    |> CreateProcess.withWorkingDirectory workDir
    |> CreateProcess.ensureExitCode
    |> Proc.start
    |> Async.AwaitTask
    |> Async.Ignore

let dotnet workdir args = exec workdir "dotnet" args
let tailwindcss args = exec docsDir "tailwindcss" args

let availableLocalPort () =
    use listener = new TcpListener(IPAddress.Loopback, 0)
    listener.Start()
    (listener.LocalEndpoint :?> IPEndPoint).Port

let packageProject (package:PackagePublishing.Package) =
    srcDir </> package.Id </> $"{package.Id}.fsproj"

let boolEnvironment name =
    match Environment.environVarOrFail name with
    | "true" -> true
    | "false" -> false
    | value -> failwith $"{name} must be true or false, found: {value}"

let releaseInputs () =
    let minimumCoreVersion = Environment.environVarOrNone "DOCS_MINIMUM_CORE_VERSION"
    PackagePublishing.validateInputs
        (Environment.environVarOrFail "PACKAGE_ID")
        (Environment.environVarOrFail "PACKAGE_VERSION")
        minimumCoreVersion
        (boolEnvironment "MARK_LATEST")

let optionalEnvironment name =
    Environment.environVarOrNone name
    |> Option.filter (System.String.IsNullOrWhiteSpace >> not)

let releaseSelection () =
    PackagePublishing.validateSelection
        (Environment.environVarOrFail "PACKAGE_SELECTION")
        (optionalEnvironment "CORE_PACKAGE_VERSION")
        (optionalEnvironment "DOCS_PACKAGE_VERSION")
        (optionalEnvironment "DOCS_MINIMUM_CORE_VERSION")

let selectedPackage () =
    match Environment.environVarOrFail "PACKAGE_ID" with
    | "FSharp.ViewEngine" -> PackagePublishing.Package.ViewEngine
    | "FSharp.ViewEngine.Docs" -> PackagePublishing.Package.Docs
    | packageId -> failwith $"Unsupported package: {packageId}"

let getVersion () =
    let value = Environment.environVarOrFail "PACKAGE_VERSION"
    let matched = Regex.Match(value, @"^(\d+\.\d+\.\d+(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?)$")
    if matched.Success then matched.Groups[1].Value else failwith $"invalid package version: {value}"

Target.create "ValidateReleaseSelection" <| fun _ ->
    let selection = releaseSelection ()
    let selected =
        [ selection.core; selection.docs ]
        |> List.choose id
        |> List.map (fun inputs -> $"{inputs.package.Id} {inputs.version}")
        |> String.concat ", "
    Trace.trace $"Validated package selection: {selected}"

Target.create "PrepareRelease" <| fun _ ->
    let inputs = releaseInputs ()
    let expectedRef = Environment.environVarOrDefault "GITHUB_REF" "refs/heads/main"
    if expectedRef <> "refs/heads/main" then failwith $"Releases must run from main, not {expectedRef}."

    PackagePublishing.validateChangelog inputs.package.Id inputs.version (System.IO.File.ReadAllText changelogPath)

    match inputs.minimumCoreVersion with
    | Some coreVersion when not (PackagePublishing.confirmPublished "FSharp.ViewEngine" coreVersion) ->
        match Environment.environVarOrNone "LOCAL_CORE_PACKAGE_PATH" with
        | Some packagePath -> PackagePublishing.validateLocalCorePackage coreVersion packagePath
        | None -> failwith $"FSharp.ViewEngine {coreVersion} must be available from NuGet before publishing Docs."
    | _ -> ()

    let metadata = Release.prepare releaseRepository releaseMetadataPath inputs.package.TagPrefix inputs.version
    Trace.trace $"Prepared {metadata.tag} for {metadata.commit}"
    Trace.trace $"Release metadata: {releaseMetadataPath}"

Target.create "ReadReleaseMetadata" <| fun _ ->
    let metadata = Release.readMetadata releaseMetadataPath
    let outputPath = Environment.environVarOrFail "GITHUB_OUTPUT"
    let previousTag = metadata.previousTag |> Option.defaultValue ""
    System.IO.File.AppendAllLines(outputPath, [
        $"tag={metadata.tag}"
        $"version={metadata.version}"
        $"commit={metadata.commit}"
        $"previousTag={previousTag}" ])

Target.create "RecordPackageChecksums" <| fun _ ->
    let packagePaths =
        !! $"{nugetsDir}/*.nupkg"
        ++ $"{nugetsDir}/*.snupkg"
        |> Seq.sort
        |> Seq.toList
    PackagePublishing.writeChecksums (nugetsDir </> "SHA256SUMS") packagePaths

Target.create "PublishPackageRelease" <| fun _ ->
    let inputs = releaseInputs ()
    let packageDirectory = Environment.environVarOrFail "PACKAGE_DIRECTORY" |> Path.getFullName
    let metadata = Release.readMetadata releaseMetadataPath
    let assets =
        PackagePublishing.expectedAssetNames inputs.package.Id inputs.version
        |> List.map (fun name -> packageDirectory </> name)
    let packagePath = packageDirectory </> $"{inputs.package.Id}.{inputs.version}.nupkg"

    PackagePublishing.verifyChecksums (packageDirectory </> "SHA256SUMS") packageDirectory
    PackagePublishing.publishOrVerify
        packagePath
        inputs.package.Id
        inputs.version
        (Environment.environVarOrFail "NUGET_API_KEY")
        (Environment.environVarOrDefault "RUNNER_TEMP" (System.IO.Path.GetTempPath()) </> "fsharp-viewengine-publish")
    PackagePublishing.waitForPublished inputs.package.Id inputs.version 60 (System.TimeSpan.FromSeconds 10.)
    PackagePublishing.ensureTag releaseRepository metadata.tag metadata.commit
    PackagePublishing.reconcileGitHubRelease
        (Environment.environVarOrFail "GITHUB_REPOSITORY")
        inputs.package.Id
        inputs.version
        metadata.tag
        metadata.previousTag
        inputs.markLatest
        assets

Target.create "CleanNugets" <| fun _ -> Shell.cleanDir nugetsDir

Target.create "Test" <| fun _ ->
    ["net8.0"; "net9.0"; "net10.0"]
    |> List.iter (fun tfm ->
        dotnet testsDir ["run"; "--framework"; tfm]
        |> Async.RunSynchronously
    )

    dotnet docsTestsDir ["run"]
    |> Async.RunSynchronously

    dotnet buildTestsDir ["run"]
    |> Async.RunSynchronously

Target.create "Pack"  (fun _ ->
    let package = selectedPackage ()
    let version = getVersion ()
    let arguments =
        [ "pack"
          packageProject package
          "--configuration"
          "Release"
          "--output"
          nugetsDir ]

    let arguments =
        match package with
        | PackagePublishing.Package.ViewEngine -> arguments @ [ $"/p:FSharpViewEnginePackageVersion={version}" ]
        | PackagePublishing.Package.Docs ->
            let minimumCoreVersion = Environment.environVarOrFail "DOCS_MINIMUM_CORE_VERSION"
            arguments @
                [ $"/p:FSharpViewEngineDocsPackageVersion={version}"
                  $"/p:FSharpViewEnginePackageVersion={minimumCoreVersion}" ]

    Trace.trace $"Packing {package.Id} {version}"
    dotnet rootDir arguments |> Async.RunSynchronously
)

Target.create "VerifyPackage" (fun _ ->
    let packages =
        match Environment.environVarOrNone "PACKAGE_PATH" with
        | Some package -> [ Path.getFullName package ]
        | None ->
            let package = selectedPackage ()
            let packageDirectory = Environment.environVarOrDefault "PACKAGE_DIRECTORY" nugetsDir
            let packages =
                !! $"{packageDirectory}/{package.Id}.*.nupkg"
                |> Seq.filter (fun path -> not (path.EndsWith(".snupkg", System.StringComparison.OrdinalIgnoreCase)))
                |> Seq.sort
                |> Seq.toList

            match packages with
            | [ packagePath ] -> [ packagePath ]
            | _ ->
                let found = packages |> List.map System.IO.Path.GetFileName |> String.concat ", "
                failwith $"Expected exactly one {package.Id} package, found: {found}"

    for package in packages do
        if not (File.exists package) then failwith $"Package does not exist: {package}"

        PackageVerification.verify
            (fun workDir args -> dotnet workDir args |> Async.RunSynchronously)
            package
)

Target.create "WatchDocs" (fun _ ->
    let docsUrl =
        System.Environment.GetEnvironmentVariable("SERVER_URL")
        |> Option.ofObj
        |> Option.defaultWith (fun () -> $"http://127.0.0.1:{availableLocalPort ()}")

    Trace.trace $"Starting the FSharp.ViewEngine Docs at {docsUrl}"

    let watchApp =
        execEnv "SERVER_URL" docsUrl docsDir "dotnet" ["watch"; "run"; "--no-restore"]

    let watchCss =
        tailwindcss ["--input"; "input.css"; "--output"; "wwwroot/css/output.css"; "--watch"]

    Async.Parallel [| watchApp; watchCss |]
    |> Async.RunSynchronously
    |> ignore
)

Target.create "Benchmark" <| fun parameters ->
    dotnet benchmarksDir ([ "run"; "--configuration"; "Release"; "--" ] @ parameters.Context.Arguments)
    |> Async.RunSynchronously

Target.create "BenchmarkSmoke" <| fun parameters ->
    dotnet benchmarksDir ([ "run"; "--configuration"; "Release"; "--"; "--smoke" ] @ parameters.Context.Arguments)
    |> Async.RunSynchronously

Target.create "BuildDocsCss" <| fun _ ->
    tailwindcss [ "--input"; "input.css"; "--output"; "wwwroot/css/output.css"; "--minify" ]
    |> Async.RunSynchronously

Target.create "PublishDocs" <| fun _ ->
    dotnet docsDir [
        "publish"
        "--output"; "./out"
        "--self-contained"; "false"
    ]
    |> Async.RunSynchronously

Target.create "Default" (fun _ -> Target.listAvailable())

"Test" ==>! "Pack"
"CleanNugets" ==>! "Pack"
"Pack" ==>! "VerifyPackage"
"BuildDocsCss" ==>! "PublishDocs"

Target.runOrDefaultWithArguments "Default"
