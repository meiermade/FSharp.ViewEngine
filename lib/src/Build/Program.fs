open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open System.Text.RegularExpressions

System.Environment.GetCommandLineArgs()
|> Array.tail
|> Array.toList
|> Context.FakeExecutionContext.Create false "build.fsx"
|> Context.RuntimeContext.Fake
|> Context.setExecutionContext

let inline (==>!) x y = x ==> y |> ignore

let srcDir = Path.getDirectory __SOURCE_DIRECTORY__
let rootDir = Path.getDirectory srcDir
let nugetsDir = rootDir </> "nugets"
let testsDir = srcDir </> "Tests"

let exec workDir cmd args =
    CreateProcess.fromRawCommand cmd args
    |> CreateProcess.withWorkingDirectory workDir
    |> CreateProcess.ensureExitCode
    |> Proc.start
    |> Async.AwaitTask
    |> Async.Ignore

let execEnv env workDir cmd args =
    CreateProcess.fromRawCommand cmd args
    |> CreateProcess.withEnvironmentMap env
    |> CreateProcess.withWorkingDirectory workDir
    |> CreateProcess.ensureExitCode
    |> Proc.start
    |> Async.AwaitTask
    |> Async.Ignore

let dotnet workdir args = exec workdir "dotnet" args

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

Target.create "Pack"  (fun _ ->
    let project = srcDir </> "FSharp.ViewEngine" </> "FSharp.ViewEngine.fsproj"
    Trace.trace $"Packing {project}"
    let version = getVersion()
    dotnet rootDir ["pack"; project; "--configuration"; "Release"; "--output"; nugetsDir; $"/p:PackageVersion={version}"]
    |> Async.RunSynchronously
)
        
Target.create "PushNugets" (fun _ ->
    let nugets = !! $"{nugetsDir}/*.nupkg" |> String.concat ", "
    Trace.trace $"Publishing {nugets}"
    let apiKey = Environment.environVarOrFail "NUGET_API_KEY"
    dotnet rootDir ["nuget"; "push"; $"{nugetsDir}/*.nupkg"; "--source"; "https://api.nuget.org/v3/index.json"; "--api-key"; apiKey]
    |> Async.RunSynchronously
)
        
Target.create "Default" (fun _ -> Target.listAvailable())

"Test" ==>! "Pack"
"CleanNugets" ==>! "Pack"
"Pack" ==>! "PushNugets"
        
Target.runOrDefaultWithArguments "Default"
