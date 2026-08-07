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
            Paragraph [ Text "These benchmarks show typical render times, compare FSharp.ViewEngine with other F# view engines, and provide the commands needed to reproduce the results." ]
            Heading { id = "typical-render-times"; title = "Typical Render Times"; level = 2 }
            Paragraph [ Text "The representative page is a complete HTML document with metadata, navigation, content, lists, a form, a table, and a footer." ]
            UnorderedList [
                [ Strong [ Text "Build and render:" ]; Text " 1.585 μs for the representative page." ]
                [ Strong [ Text "Render only:" ]; Text " 833.5 ns when the representative page is already built." ]
                [ Strong [ Text "Large response:" ]; Text " 0.23 ms to build and render 1,000 repeated articles." ]
            ]
            Paragraph [ Text "These are renderer operations, not HTTP requests per second. Routing, application work, I/O, concurrency, and response transport are not included." ]
            Heading { id = "framework-comparison"; title = "How It Compares"; level = 2 }
            Paragraph [ Text "For the representative build-and-render workload, the other engines take 1.35× to 2.35× as long as FSharp.ViewEngine. All four remain fast in absolute terms." ]
            BarChart {
                label = "Build and render comparison"
                title = "Typical dynamic page: build and render"
                description = "Mean duration per operation. Shorter bars are faster; values are normalized against the slowest result."
                bars = [
                    { label = "FSharp.ViewEngine"; duration = "1.585 μs"; comparison = "baseline"; widthPercent = 43; highlighted = true }
                    { label = "Oxpecker.ViewEngine"; duration = "2.147 μs"; comparison = "1.35× as long"; widthPercent = 58; highlighted = false }
                    { label = "Giraffe.ViewEngine"; duration = "2.649 μs"; comparison = "1.67× as long"; widthPercent = 71; highlighted = false }
                    { label = "Feliz.ViewEngine"; duration = "3.723 μs"; comparison = "2.35× as long"; widthPercent = 100; highlighted = false }
                ]
            }
            Heading { id = "methodology"; title = "How the Benchmarks Were Run"; level = 2 }
            Paragraph [ Text "The current results were measured on August 6, 2026 with BenchmarkDotNet 0.15.8, .NET SDK 10.0.201, .NET runtime 10.0.5, macOS 26.4.1, and an Apple M5 Max Arm64 processor." ]
            UnorderedList [
                [ Strong [ Text "Process isolation" ]; Text " — every benchmark executes in a generated benchmark process rather than inside the runner." ]
                [ Strong [ Text "Repeated measurement" ]; Text " — MediumRun uses two launches, ten warmups, and fifteen measured iterations." ]
                [ Strong [ Text "Bounded iteration time" ]; Text " — a 100 ms target avoids multi-gigabyte allocation pressure in the fastest render-only cases." ]
                [ Strong [ Text "Render-only setup" ]; Text " — documents are constructed once before warmup and measurement, keeping construction work out of the render loop." ]
                [ Strong [ Text "Reported values" ]; Text " — tables show the arithmetic mean and managed allocation per operation; lower is better." ]
            ]
            Paragraph [ Text "The comparison suite uses the latest stable releases checked for this measurement: "; Link("Oxpecker.ViewEngine 2.0.1", "https://www.nuget.org/packages/Oxpecker.ViewEngine/2.0.1"); Text ", "; Link("Giraffe.ViewEngine 1.4.0", "https://www.nuget.org/packages/Giraffe.ViewEngine/1.4.0"); Text ", and "; Link("Feliz.ViewEngine 1.0.3", "https://www.nuget.org/packages/Feliz.ViewEngine/1.0.3"); Text ". The runner prints resolved package versions with every execution. Results are representative measurements, not CI regression thresholds." ]
            Heading { id = "running-benchmarks"; title = "How to Run the Benchmarks"; level = 2 }
            Paragraph [ Text "Run commands from the solution directory. The FAKE targets forward trailing BenchmarkDotNet options, including filters. "; InlineContent.Code "BenchmarkSmoke"; Text " executes selected cases once in isolated processes to validate the suite without producing stable measurements." ]
            CodeBlock("shell", """cd sln

# Run the complete measurement suite.
./fake.sh Benchmark

# List or target benchmark cases.
./fake.sh Benchmark --list flat
./fake.sh Benchmark --filter '*Benchmarks.RenderOnly.*'

# Validate every case, or a filtered subset, once.
./fake.sh BenchmarkSmoke
./fake.sh BenchmarkSmoke --filter '*AttributeEncodingBenchmarks*'""")
            Heading { id = "appendix"; title = "Appendix: Detailed Results"; level = 2 }
            Paragraph [ Text "The following tables preserve the raw mean and managed-allocation results behind the analysis above." ]
            Heading { id = "appendix-build-and-render"; title = "Comparison: Build and Render"; level = 3 }
            DataTable(
                [ "Method"; "Mean"; "Allocated" ],
                [ [ "FSharp.ViewEngine"; "1.585 μs"; "11.39 KB" ]
                  [ "Oxpecker.ViewEngine"; "2.147 μs"; "12.88 KB" ]
                  [ "Giraffe.ViewEngine"; "2.649 μs"; "23.94 KB" ]
                  [ "Feliz.ViewEngine"; "3.723 μs"; "25.87 KB" ] ])
            Heading { id = "appendix-render-only"; title = "Comparison: Render Only"; level = 3 }
            DataTable(
                [ "Method"; "Mean"; "Allocated" ],
                [ [ "FSharp.ViewEngine"; "833.5 ns"; "2.93 KB" ]
                  [ "Oxpecker.ViewEngine"; "911.4 ns"; "2.93 KB" ]
                  [ "Giraffe.ViewEngine"; "989.6 ns"; "12.77 KB" ]
                  [ "Feliz.ViewEngine"; "1,872.9 ns"; "14.2 KB" ] ])
            Heading { id = "appendix-build-only"; title = "Comparison: Build Only"; level = 3 }
            DataTable(
                [ "Method"; "Mean"; "Allocated" ],
                [ [ "FSharp.ViewEngine"; "670.1 ns"; "8.46 KB" ]
                  [ "Oxpecker.ViewEngine"; "1,181.0 ns"; "9.95 KB" ]
                  [ "Giraffe.ViewEngine"; "1,654.9 ns"; "11.17 KB" ]
                  [ "Feliz.ViewEngine"; "1,782.9 ns"; "11.66 KB" ] ])
            Heading { id = "appendix-attribute-encoding"; title = "Attribute Encoding"; level = 3 }
            DataTable(
                [ "Value"; "Mean"; "Allocated" ],
                [ [ "Plain"; "36.17 ns"; "280 B" ]
                  [ "Encoded"; "81.92 ns"; "496 B" ] ])
            Heading { id = "appendix-storage-boundaries"; title = "Inline and Overflow Storage"; level = 3 }
            DataTable(
                [ "Shape"; "Count"; "Mean"; "Allocated" ],
                [ [ "Attributes"; "0"; "26.43 ns"; "200 B" ]
                  [ "Attributes"; "1"; "33.16 ns"; "216 B" ]
                  [ "Attributes"; "2"; "41.23 ns"; "240 B" ]
                  [ "Attributes"; "8"; "108.42 ns"; "744 B" ]
                  [ "Children"; "0"; "18.47 ns"; "160 B" ]
                  [ "Children"; "1"; "35.08 ns"; "320 B" ]
                  [ "Children"; "2"; "52.22 ns"; "488 B" ]
                  [ "Children"; "8"; "187.57 ns"; "1,648 B" ] ])
            Heading { id = "appendix-collection-inputs"; title = "Equivalent Collection Inputs"; level = 3 }
            DataTable(
                [ "Collection"; "Mean"; "Allocated" ],
                [ [ "Array"; "451.7 ns"; "3.45 KB" ]
                  [ "List"; "437.7 ns"; "3.45 KB" ]
                  [ "Sequence"; "482.8 ns"; "3.53 KB" ] ])
            Heading { id = "appendix-document-workloads"; title = "Document Workloads"; level = 3 }
            DataTable(
                [ "Workload"; "Build + render"; "Allocation"; "Render only"; "Allocation" ],
                [ [ "Small fragment"; "72.92 ns"; "680 B"; "51.05 ns"; "296 B" ]
                  [ "Representative page"; "1,538.00 ns"; "11,664 B"; "813.40 ns"; "3,000 B" ]
                  [ "Deeply nested"; "2,288.68 ns"; "12,096 B"; "1,069.54 ns"; "3,256 B" ]
                  [ "Large response"; "228,746.00 ns"; "1,252,539 B"; "77,196.10 ns"; "283,768 B" ] ])
            Heading { id = "appendix-profiling"; title = "Profiling Findings"; level = 3 }
            UnorderedList [
                [ Text "Build-only CPU samples are dominated by "; InlineContent.Code "TagBuilder.Run"; Text " and generated computation-expression methods; sampled allocations are DOM nodes and overflow collections rather than closure objects." ]
                [ Text "Render-only allocations are almost entirely the required returned "; InlineContent.Code "System.String"; Text "." ]
                [ Text "Optimized ARM64 JIT output retains indirect child-render calls, but profiling does not identify dispatch as a dominant cost." ]
                [ Text "General sequence input adds about 80 bytes and modest runtime overhead, which does not justify collection-specific overloads." ]
                [ Text "Measurements continue to support inline storage for zero, one, and two attributes or children." ]
                [ Text "The renderer retains at most one thread-static string builder no larger than 256K characters, bounding retained memory without regressing the representative large response." ]
            ]
          ] }
