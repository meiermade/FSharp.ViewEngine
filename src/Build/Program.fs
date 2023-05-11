open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open System.Text.RegularExpressions

let inline (==>!) x y = x ==> y |> ignore

let srcDir = Path.getDirectory __SOURCE_DIRECTORY__
let rootDir = Path.getDirectory srcDir
let sln = rootDir </> "FSharp.ViewEngine.sln"
let nugetsDir = rootDir </> "nugets"
let testsDir = srcDir </> "Tests"

let getVersion () =
    let tag = Environment.environVarOrFail "GITHUB_REF_NAME"
    let m = Regex.Match(tag, @"^v(\d+\.\d+\.\d+)$")
    if m.Success then m.Groups[1].Value else failwith $"invalid tag: {tag}"
    
let registerTargets() =
    
    Target.create "Clean" (fun _ -> Shell.cleanDir nugetsDir)
    
    Target.create "Test" <| fun _ ->
        DotNet.test
            (fun opts ->
                { opts with
                    MSBuildParams = { MSBuild.CliArguments.Create() with DisableInternalBinLog  = true } })
            testsDir
    
    Target.create "Pack" <| fun _ ->
        Trace.trace $"Packing {sln}"
        let version = getVersion()
        let customParams = [ $"/p:PackageVersion={version}" ] |> String.concat " "
        DotNet.pack
            (fun opts ->
                { opts with
                    Configuration = DotNet.BuildConfiguration.Release
                    OutputPath = Some nugetsDir 
                    Common = opts.Common |> DotNet.Options.withCustomParams (Some customParams)
                    MSBuildParams = { MSBuild.CliArguments.Create() with DisableInternalBinLog  = true } })
            sln
            
    Target.create "Publish" <| fun _ ->
        let nugets = !! $"{nugetsDir}/*.nupkg" |> String.concat ", "
        Trace.trace $"Publishing {nugets}"
        let apiKey = Environment.environVarOrFail "NUGET_API_KEY"
        DotNet.nugetPush
            (fun opts ->
                let pushParams =
                    { opts.PushParams with
                        ApiKey = Some apiKey
                        Source = Some "https://api.nuget.org/v3/index.json" }
                { opts with PushParams = pushParams })
            $"{nugetsDir}/*.nupkg"
            
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
