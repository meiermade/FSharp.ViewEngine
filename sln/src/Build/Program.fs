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
    let tag = Environment.environVarOrFail "GITHUB_REF_NAME"
    let m = Regex.Match(tag, @"^v(\d+\.\d+\.\d+)$")
    if m.Success then m.Groups[1].Value else failwith $"invalid tag: {tag}"

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
    let project = srcDir </> "FSharp.ViewEngine" </> "FSharp.ViewEngine.fsproj"
    Trace.trace $"Packing {project}"
    let version = getVersion()
    dotnet rootDir ["pack"; project; "--configuration"; "Release"; "--output"; nugetsDir; $"/p:PackageVersion={version}"]
    |> Async.RunSynchronously
)

Target.create "VerifyPackage" (fun _ ->
    let nugets = !! $"{nugetsDir}/*.nupkg" |> Seq.toList
    let package =
        match nugets with
        | [ package ] -> package
        | _ -> failwith $"Expected exactly one package, found {nugets.Length}"

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
