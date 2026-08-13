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
let benchmarksDir = srcDir </> "Benchmarks"
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

let getVersion () =
    let value =
        match Environment.environVarOrNone "PACKAGE_VERSION" with
        | Some version -> version
        | None -> Environment.environVarOrFail "GITHUB_REF_NAME"

    let matched = Regex.Match(value, @"^v?(\d+\.\d+\.\d+)$")
    if matched.Success then matched.Groups[1].Value else failwith $"invalid package version: {value}"

Target.create "PrepareRelease" <| fun _ ->
    let versionOverride = Environment.environVarOrNone "RELEASE_VERSION_OVERRIDE"
    let metadata = Release.prepare releaseRepository releaseMetadataPath versionOverride
    Trace.trace $"Prepared {metadata.tag} for {metadata.commit}"
    Trace.trace $"Release metadata: {releaseMetadataPath}"

Target.create "TagRelease" <| fun _ ->
    let metadata = Release.readMetadata releaseMetadataPath
    Release.tag releaseRepository metadata
    Trace.trace $"Release tag {metadata.tag} points to {metadata.commit}"

Target.create "CleanNugets" <| fun _ -> Shell.cleanDir nugetsDir

Target.create "Test" <| fun _ ->
    ["net8.0"; "net9.0"; "net10.0"]
    |> List.iter (fun tfm ->
        dotnet testsDir ["run"; "--framework"; tfm]
        |> Async.RunSynchronously
    )

    dotnet docsTestsDir ["run"]
    |> Async.RunSynchronously

Target.create "Pack"  (fun _ ->
    let version = getVersion()
    let projects =
        [ srcDir </> "FSharp.ViewEngine" </> "FSharp.ViewEngine.fsproj"
          srcDir </> "FSharp.ViewEngine.Docs" </> "FSharp.ViewEngine.Docs.fsproj" ]

    for project in projects do
        Trace.trace $"Packing {project}"
        dotnet rootDir ["pack"; project; "--configuration"; "Release"; "--output"; nugetsDir; $"/p:PackageVersion={version}"]
        |> Async.RunSynchronously
)

Target.create "VerifyPackage" (fun _ ->
    let packages =
        match Environment.environVarOrNone "PACKAGE_PATH" with
        | Some package -> [ Path.getFullName package ]
        | None ->
            let packageDirectory = Environment.environVarOrDefault "PACKAGE_DIRECTORY" nugetsDir
            let packages = !! $"{packageDirectory}/*.nupkg" |> Seq.sort |> Seq.toList
            let expectedPackageIds = Set [ "FSharp.ViewEngine"; "FSharp.ViewEngine.Docs" ]
            let packageIds =
                packages
                |> List.map (System.IO.Path.GetFileName >> fun fileName ->
                    if fileName.StartsWith("FSharp.ViewEngine.Docs.", System.StringComparison.Ordinal) then "FSharp.ViewEngine.Docs"
                    elif fileName.StartsWith("FSharp.ViewEngine.", System.StringComparison.Ordinal) then "FSharp.ViewEngine"
                    else fileName)
                |> Set.ofList

            if packages.Length <> expectedPackageIds.Count || packageIds <> expectedPackageIds then
                let found = packages |> List.map System.IO.Path.GetFileName |> String.concat ", "
                failwith $"Expected FSharp.ViewEngine and FSharp.ViewEngine.Docs packages, found: {found}"

            packages

    for package in packages do
        if not (File.exists package) then failwith $"Package does not exist: {package}"

        PackageVerification.verify
            (fun workDir args -> dotnet workDir args |> Async.RunSynchronously)
            package
)

Target.create "PushNugets" (fun _ ->
    let nugets = !! $"{nugetsDir}/*.nupkg" |> String.concat ", "
    Trace.trace $"Publishing {nugets} and its associated symbol package"
    let apiKey = Environment.environVarOrFail "NUGET_API_KEY"
    dotnet rootDir ["nuget"; "push"; $"{nugetsDir}/*.nupkg"; "--source"; "https://api.nuget.org/v3/index.json"; "--api-key"; apiKey]
    |> Async.RunSynchronously
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
"VerifyPackage" ==>! "PushNugets"
"BuildDocsCss" ==>! "PublishDocs"

Target.runOrDefaultWithArguments "Default"
