[![Publish Core](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/publish.yml/badge.svg)](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/publish.yml)
[![Publish Docs](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/publish-docs.yml/badge.svg)](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/publish-docs.yml)
[![Deploy](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/deploy.yml/badge.svg)](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/deploy.yml)
[![NuGet Core](https://img.shields.io/nuget/v/FSharp.ViewEngine)](https://www.nuget.org/packages/FSharp.ViewEngine)
[![NuGet Docs](https://img.shields.io/nuget/v/FSharp.ViewEngine.Docs)](https://www.nuget.org/packages/FSharp.ViewEngine.Docs)

<p align="center">
  <img src="etc/logo.svg" alt="FSharp.ViewEngine" width="128">
</p>

# FSharp.ViewEngine
A minimal, fast view engine for F#. Inspired by [Giraffe.ViewEngine](https://github.com/giraffe-fsharp/Giraffe.ViewEngine),
[Feliz.ViewEngine](https://github.com/dbrattli/Feliz.ViewEngine),
[Oxpecker.ViewEngine](https://github.com/Lanayx/Oxpecker), and
[Bolero](https://github.com/fsbolero/Bolero).

FSharp.ViewEngine combines ideas from several F# view engines into a clean, unified DSL:

- **Computation expression syntax** (like Oxpecker.ViewEngine and Bolero) for building elements
- **Feliz-style single sequence** of attributes and child elements — no separate attribute and children lists
- **Attributes prefixed with underscore** by convention (like Giraffe.ViewEngine, e.g. `_class`, `_id`, `_dataOn`), giving clean syntax and nice syntax highlighting
- **Mixed yielding** in computation expressions — you can yield strings, elements, and attributes in any order without needing a special `_children` attribute

The result is a DSL that is as minimal and fast as possible while remaining expressive and type-safe.

Documentation site built using FSharp.ViewEngine available at [https://fsharpviewengine.meiermade.com](https://fsharpviewengine.meiermade.com).
> See [sln/src/Docs](./sln/src/Docs) for the source code.

## Installation
Add the core view engine package with your preferred CLI.

```shell
dotnet add package FSharp.ViewEngine
```

```shell
dotnet paket add FSharp.ViewEngine
```

For documentation sites, API references, and executable software specifications, install the separate add-on package:

```shell
dotnet add package FSharp.ViewEngine.Docs
```

`FSharp.ViewEngine.Docs` provides article, reference, and canvas layouts; configurable navigation; accessible code/preview examples; API documentation components; diagrams; product frames; typed destinations; and structural validation. See its [package documentation](./sln/src/FSharp.ViewEngine.Docs/README.md).

## Releases

The two NuGet packages have independent release trains:

- `FSharp.ViewEngine` uses tags such as `v2026.8.1` and the **Publish Core** workflow.
- `FSharp.ViewEngine.Docs` uses tags such as `docs/v2026.8.0` and the **Publish Docs** workflow.

A Docs release declares its minimum compatible published Core version. Matching package versions are not required. Publish Core first whenever Docs needs APIs that are not already available on NuGet. Both publish workflows require an explicit version and a matching released-package changelog entry, publish from the protected `release` environment with NuGet Trusted Publishing, and reconcile retries against the verified package artifacts. Core releases become the repository-wide GitHub “Latest” release; Docs releases do not. Directly packing `FSharp.ViewEngine.Docs` also requires explicit Docs and minimum Core MSBuild version properties so it cannot silently produce incorrect dependency metadata. Documentation-site deployment is separate from package publication and deploys the selected source revision through the **Deploy** workflow.

## Core rendering helpers

`Render.toString` serializes a fragment; `Render.toHtmlDocString` prepends the HTML5 doctype for a complete document. Additional targets support existing `StringBuilder` and `TextWriter` instances plus UTF-8 bytes:

```fsharp
let siblings =
    Html.fragment {
        span { "One" }
        Html.comment "Trusted build marker"
        span { "Two" }
    }

let bytes = Render.toUtf8Bytes siblings
```

`Html.comment` rejects invalid HTML comment values containing `--` or ending with `-`. `Html.raw`, `Html.js`, custom markup names, and executable expression attributes remain trusted developer-controlled boundaries.

## Runtime compatibility

The package ships a single `net8.0` compatibility asset and is tested on supported .NET 8, .NET 9, and .NET 10 runtimes. NuGet automatically selects the `net8.0` asset for compatible newer runtimes.

Portable symbols are published separately with Source Link metadata, so supported debuggers can retrieve the matching source from GitHub without increasing the main package size.

## Usage
```fsharp
open FSharp.ViewEngine
open type Html
open type Datastar
open type TailwindElements

html {
    _lang "en"
    head {
        title { "Test" }
        meta { _charset "utf-8" }
        link { _href "/css/compiled.css"; _rel "stylesheet" }
    }
    body {
        _dataSignals "{showContent: false}"
        _class "bg-gray-50"
        div {
            _id "page"
            _class [ "flex"; "flex-col" ]
            h1 { "Hello from FSharp.ViewEngine" }
            button {
                _dataOn ("click", "$showContent = !$showContent")
                "Toggle content"
            }
        }
        br
        div {
            _dataShow "$showContent"
            _style "display: none"
            h2 { "Content" }
            p { "Some content" }
            ul {
                li { "One" }
                li { "Two" }
            }
        }
    }
}
|> Render.toHtmlDocString
```
```html
<!DOCTYPE html>
<html lang="en">
    <head>
        <title>Test</title>
        <meta charset="utf-8">
        <link href="/css/compiled.css" rel="stylesheet">
    </head>
    <body data-signals="{showContent: false}" class="bg-gray-50">
        <div id="page" class="flex flex-col">
            <h1>Hello from FSharp.ViewEngine</h1>
            <button data-on:click="$showContent = !$showContent">Toggle content</button>
        </div>
        <br>
        <div data-show="$showContent" style="display: none">
            <h2>Content</h2>
            <p>Some content</p>
            <ul>
                <li>One</li>
                <li>Two</li>
            </ul>
        </div>
    </body>
</html>
```

## Benchmarks
Measured on August 6, 2026 with BenchmarkDotNet 0.15.8 on .NET SDK 10.0.201 / runtime 10.0.5, macOS 26.4.1, Apple M5 Max Arm64. The process-isolated `MediumRun` configuration uses two launches, ten warmups, fifteen measured iterations, and a 100 ms iteration target. The shorter target avoids multi-gigabyte per-iteration allocation pressure in the fastest render-only workloads while retaining repeated measurements.

The suite covers comparison-engine build/render behavior plus attribute encoding, 0/1/2/8 attribute and child shapes, array/list/sequence loops, and small, representative, deeply nested, and large workloads. Every run prints its environment, resolved dependency versions, and job configuration.

```shell
cd sln

# Run the complete measurement suite.
./fake.sh Benchmark

# List or target benchmark cases with standard BenchmarkDotNet filters.
./fake.sh Benchmark --list flat
./fake.sh Benchmark --filter '*AttributeEncodingBenchmarks*'

# Execute every case, or a filtered subset, once as a validation smoke run.
./fake.sh BenchmarkSmoke
./fake.sh BenchmarkSmoke --filter '*AttributeEncodingBenchmarks*'
```

Results are representative measurements, not CI regression thresholds. Means and managed allocations are shown below; lower is better.

### View-engine comparisons

Build and render:

| Method        | Mean     | Allocated |
|-------------- |---------:|----------:|
| ViewEngineApi | 1.585 μs |  11.39 KB |
| OxpeckerApi   | 2.147 μs |  12.88 KB |
| GiraffeApi    | 2.649 μs |  23.94 KB |
| FelizApi      | 3.723 μs |  25.87 KB |

Render only:

| Method        | Mean       | Allocated |
|-------------- |-----------:|----------:|
| ViewEngineApi |   833.5 ns |   2.93 KB |
| OxpeckerApi   |   911.4 ns |   2.93 KB |
| GiraffeApi    |   989.6 ns |  12.77 KB |
| FelizApi      | 1,872.9 ns |   14.2 KB |

Build only:

| Method        | Mean       | Allocated |
|-------------- |-----------:|----------:|
| ViewEngineApi |   670.1 ns |   8.46 KB |
| OxpeckerApi   | 1,181.0 ns |   9.95 KB |
| GiraffeApi    | 1,654.9 ns |  11.17 KB |
| FelizApi      | 1,782.9 ns |  11.66 KB |

### FSharp.ViewEngine workloads

Attribute encoding:

| Value   | Mean     | Allocated |
|-------- |---------:|----------:|
| Plain   | 36.17 ns |     280 B |
| Encoded | 81.92 ns |     496 B |

Inline and overflow storage boundaries:

| Shape      | Count | Mean      | Allocated |
|----------- |------:|----------:|----------:|
| Attributes |     0 |  26.43 ns |     200 B |
| Attributes |     1 |  33.16 ns |     216 B |
| Attributes |     2 |  41.23 ns |     240 B |
| Attributes |     8 | 108.42 ns |     744 B |
| Children   |     0 |  18.47 ns |     160 B |
| Children   |     1 |  35.08 ns |     320 B |
| Children   |     2 |  52.22 ns |     488 B |
| Children   |     8 | 187.57 ns |   1,648 B |

Equivalent collection inputs:

| Collection | Mean     | Allocated |
|----------- |---------:|----------:|
| Array      | 451.7 ns |   3.45 KB |
| List       | 437.7 ns |   3.45 KB |
| Sequence   | 482.8 ns |   3.53 KB |

Document workloads:

| Workload            | Build and render | Build/render allocation | Render only | Render allocation |
|-------------------- |-----------------:|------------------------:|------------:|------------------:|
| Small fragment      |          72.92 ns |                   680 B |    51.05 ns |             296 B |
| Representative page |       1,538.00 ns |               11,664 B |   813.40 ns |           3,000 B |
| Deeply nested       |       2,288.68 ns |               12,096 B | 1,069.54 ns |           3,256 B |
| Large response      |     228,746.00 ns |            1,252,539 B | 77,196.10 ns |         283,768 B |

### Profiling findings

- Build-only CPU samples are dominated by `TagBuilder.Run` and generated computation-expression `Invoke` methods, but allocation samples contain DOM nodes and overflow collections rather than F# closure objects.
- Render-only allocation samples are almost entirely the required returned `System.String`.
- Optimized ARM64 JIT output retains indirect virtual calls for child `HtmlElement.Render` dispatch, but profiling does not show dispatch as a dominant cost relative to string creation and GC work.
- General sequence input adds about 80 bytes and modest runtime overhead; current results do not justify array/list-specific `For` overloads.
- The 0/1/2 inline attribute and child storage optimization remains justified by the allocation results.
- The thread-static `StringBuilder` pool now retains at most one builder with capacity no greater than 256K characters. The bound prevents unbounded per-thread retention without adding allocation or timing regressions to the representative 142K-character large response.
