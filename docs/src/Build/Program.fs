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
let docsDir = srcDir </> "Docs"

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
let tailwindcss args = exec docsDir "tailwindcss" args

Target.create "Watch" (fun _ ->
    let watchApp = dotnet docsDir ["watch"; "run"; "--no-restore"]
    let watchCss = tailwindcss ["--input"; "input.css"; "--output"; "wwwroot/css/output.css"; "--watch"]
    Async.Parallel [| watchApp; watchCss |]
    |> Async.RunSynchronously
    |> ignore
)

Target.create "BuildCss" <| fun _ ->
    tailwindcss [ "--input"; "input.css"; "--output"; "wwwroot/css/output.css"; "--minify" ]
    |> Async.RunSynchronously

Target.create "Publish" <| fun _ ->
    dotnet docsDir [
        "publish"
        "--output"; "./out"
        "--self-contained"; "false"
    ]
    |> Async.RunSynchronously

Target.create "Default" (fun _ -> Target.listAvailable())

"BuildCss" ==>! "Publish"
        
Target.runOrDefaultWithArguments "Default"
