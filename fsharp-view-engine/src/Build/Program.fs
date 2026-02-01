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
let sln = rootDir </> "FSharp.ViewEngine.sln"
let nugetsDir = rootDir </> "nugets"
let testsDir = srcDir </> "Tests"

let dotnet workdir args =
    CreateProcess.fromRawCommand "dotnet" args
    |> CreateProcess.withWorkingDirectory workdir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let getVersion () =
    let tag = Environment.environVarOrFail "GITHUB_REF_NAME"
    let m = Regex.Match(tag, @"^v(\d+\.\d+\.\d+)$")
    if m.Success then m.Groups[1].Value else failwith $"invalid tag: {tag}"
    
Target.create "Clean" (fun _ -> Shell.cleanDir nugetsDir)

Target.create "Test" (fun _ -> dotnet testsDir ["test"])

Target.create "Pack" <| fun _ ->
    Trace.trace $"Packing {sln}"
    let version = getVersion()
    dotnet rootDir ["pack"; sln; "--configuration"; "Release"; "--output"; nugetsDir; $"/p:PackageVersion={version}"]
        
Target.create "Publish" <| fun _ ->
    let nugets = !! $"{nugetsDir}/*.nupkg" |> String.concat ", "
    Trace.trace $"Publishing {nugets}"
    let apiKey = Environment.environVarOrFail "NUGET_API_KEY"
    dotnet rootDir ["nuget"; "push"; $"{nugetsDir}/*.nupkg"; "--source"; "https://api.nuget.org/v3/index.json"; "--api-key"; apiKey]
        
Target.create "Default" (fun _ -> Target.listAvailable())

"Test" ==>! "Pack"
"Clean" ==>! "Pack"
"Pack" ==>! "Publish"
        
Target.runOrDefaultWithArguments "Default"
