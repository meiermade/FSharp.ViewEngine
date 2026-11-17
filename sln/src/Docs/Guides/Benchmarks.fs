namespace Docs.Pages

open Docs.Common

module Benchmarks =
    let page =
        { id = "benchmarks"
          path = "/benchmarks"
          aliases = []
          navLabel = "Benchmarks"
          category = "Project"
          title = "Benchmarks"
          browserTitle = "Benchmarks - FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "Latest render times and memory allocations for FSharp.ViewEngine." ]
            Heading { id = "framework-comparison"; title = "Rendering performance"; level = 2 }
            Paragraph [ Text "FSharp.ViewEngine builds and renders the representative HTML document in 1.726 μs, allocating 11.41 KB per operation. Rendering a prebuilt document takes 899.9 ns." ]
            BarChart {
                label = "Build and render comparison"
                title = "Build and render"
                description = "Mean build-and-render time per operation."
                bars = [
                    { label = "FSharp.ViewEngine"; duration = "1.726 μs"; widthPercent = 43; highlighted = true }
                    { label = "Oxpecker.ViewEngine"; duration = "2.348 μs"; widthPercent = 58; highlighted = false }
                    { label = "Giraffe.ViewEngine"; duration = "2.993 μs"; widthPercent = 74; highlighted = false }
                    { label = "Feliz.ViewEngine"; duration = "4.033 μs"; widthPercent = 100; highlighted = false }
                ]
            }
            DataTable(
                [ "Engine"; "Build + render"; "Render only"; "Allocated" ],
                [ [ "FSharp.ViewEngine"; "1.726 μs"; "899.9 ns"; "11.41 KB" ]
                  [ "Oxpecker.ViewEngine 2.1.0"; "2.348 μs"; "967.8 ns"; "12.89 KB" ]
                  [ "Giraffe.ViewEngine 1.4.0"; "2.993 μs"; "1,164.0 ns"; "23.95 KB" ]
                  [ "Feliz.ViewEngine 1.0.3"; "4.033 μs"; "2,137.5 ns"; "25.85 KB" ] ])
            Paragraph [ Text "Allocations are per build-and-render operation; render-only uses a prebuilt document. These measure rendering, not HTTP throughput." ]
            Heading { id = "environment"; title = "Environment"; level = 2 }
            Paragraph [ Text "Measured November 15, 2026 · .NET 11.0.0 · SDK 11.0.100 · Apple M5 Max Arm64, 18 cores · macOS 26.4.1." ]
            Paragraph [ Text "BenchmarkDotNet 0.16.0-preview.2, isolated Release processes. MediumRun: two launches, ten warmups, fifteen measured iterations, 100 ms target." ]
            Heading { id = "running-benchmarks"; title = "Reproduce"; level = 2 }
            CodeBlock("shell", """cd sln
./fake.sh Benchmark --filter '*' --exporters json""")
            Paragraph [ Download("Download the raw benchmark results and environment metadata", "/benchmarks/results.json"); Text " for all 45 cases in the latest run." ]
          ] }
