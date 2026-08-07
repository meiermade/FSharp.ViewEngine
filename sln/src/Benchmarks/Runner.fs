module Runner

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Runtime.InteropServices
open System.Text.Json
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Running
open Perfolizer.Horology

let private sdkVersion () =
    let startInfo = ProcessStartInfo("dotnet", "--version")
    startInfo.RedirectStandardOutput <- true
    startInfo.UseShellExecute <- false
    use sdkProcess = Process.Start(startInfo)
    let output = sdkProcess.StandardOutput.ReadToEnd().Trim()
    sdkProcess.WaitForExit()
    if sdkProcess.ExitCode = 0 then output else "unknown"

let private dependencyVersions () =
    let tracked =
        set [ "BenchmarkDotNet"; "FSharp.ViewEngine"; "Oxpecker.ViewEngine"; "Giraffe.ViewEngine"; "Feliz.ViewEngine" ]

    let assemblyPath = Assembly.GetExecutingAssembly().Location
    let dependencyManifest = Path.ChangeExtension(assemblyPath, ".deps.json")
    use document = JsonDocument.Parse(File.ReadAllText dependencyManifest)

    document.RootElement.GetProperty("libraries").EnumerateObject()
    |> Seq.choose (fun dependency ->
        match dependency.Name.Split('/') with
        | [| name; version |] when tracked.Contains name ->
            let kind = dependency.Value.GetProperty("type").GetString()
            Some(name, (version, kind))
        | _ -> None)
    |> Map.ofSeq

let private printMetadata jobName =
    printfn "Benchmark environment"
    printfn "  SDK: %s" (sdkVersion ())
    printfn "  Runtime: %s" RuntimeInformation.FrameworkDescription
    printfn "  OS: %s" RuntimeInformation.OSDescription
    printfn "  Architecture: %A" RuntimeInformation.ProcessArchitecture
    printfn "  Job: %s; process-isolated default toolchain" jobName
    printfn "Dependency versions"
    for KeyValue(name, (version, kind)) in dependencyVersions () do
        printfn "  %s: %s (%s)" name version kind

let runBenchmarks args =
    Workloads.Validation.run ()

    let smoke = args |> Array.contains "--smoke"
    let suppliedArgs = args |> Array.filter ((<>) "--smoke")
    let benchmarkArgs = if Array.isEmpty suppliedArgs then [| "--filter"; "*" |] else suppliedArgs
    let measurementJob = Job.MediumRun.WithIterationTime(TimeInterval.FromMilliseconds 100.)
    let job, jobName =
        if smoke then Job.Dry, "Dry smoke"
        else measurementJob, "MediumRun (100 ms iteration target)"
    printMetadata jobName

    let config =
        ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(job)

    BenchmarkSwitcher
        .FromAssembly(typeof<Workloads.AttributeEncodingBenchmarks>.Assembly)
        .Run(benchmarkArgs, config)
    |> ignore
