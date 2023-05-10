open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open System.Text.RegularExpressions

let inline (==>!) x y = x ==> y |> ignore

let srcDir = Path.getDirectory __SOURCE_DIRECTORY__
let testsDir = srcDir </> "Tests"

let [<Literal>] Author = "Andrew Meier"

let projects =
    [ "FSharp.ViewEngine"
      "FSharp.ViewEngine.Html"
      "FSharp.ViewEngine.Htmx"
      "FSharp.ViewEngine.Alpine" ]

let getVersion () =
    let tag = Environment.environVarOrFail "GITHUB_REF_NAME"
    let m = Regex.Match(tag, @"^v(\d+\.\d+\.\d+)$")
    if m.Success then m.Groups[1].Value else failwith $"invalid tag: {tag}"
    
let getProjectDir project = srcDir </> project
let getProjectNugetPath project =
    let version = getVersion ()
    let projectDir = getProjectDir project
    projectDir </> "bin" </> "Release" </> $"{project}.{version}.nupkg"

let registerTargets() =
    
    Target.create "Clean" <| fun _ ->
        !!"**/Release"
        |> Shell.cleanDirs
    
    Target.create "Test" <| fun _ ->
        DotNet.test
            (fun opts ->
                { opts with
                    MSBuildParams = { MSBuild.CliArguments.Create() with DisableInternalBinLog  = true } })
            testsDir
    
    let packProject project =
        Trace.trace $"Packing {project}"
        let projectDir = getProjectDir project
        let version = getVersion()
        let customParams = [ $"/p:PackageVersion={version}" ] |> String.concat " "
        DotNet.pack
            (fun opts ->
                { opts with
                    Configuration = DotNet.BuildConfiguration.Release
                    Common = opts.Common |> DotNet.Options.withCustomParams (Some customParams)
                    MSBuildParams = { MSBuild.CliArguments.Create() with DisableInternalBinLog  = true } })
            projectDir
            
    let publish project =
        Trace.trace $"Publishing {project} nuget"
        let apiKey = Environment.environVarOrFail "NUGET_API_KEY"
        let nugetPath = getProjectNugetPath project
        DotNet.nugetPush
            (fun opts ->
                let pushParams =
                    { opts.PushParams with
                        ApiKey = Some apiKey
                        Source = Some "https://api.nuget.org/v3/index.json" }
                { opts with PushParams = pushParams })
            nugetPath
            
    Target.create "Pack" (fun _ -> projects |> Seq.iter packProject)
    Target.create "Publish" (fun _ -> projects |> Seq.iter publish)
    Target.create "Default" (fun _ -> Target.listAvailable())
    
    "Test" ==>! "Pack"
    "Clean" ==>! "Pack"
    "Pack" ==>! "Publish"
        
[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext
    registerTargets()
    Target.runOrDefaultWithArguments "Default"
    0
