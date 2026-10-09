[![Publish Packages](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/publish.yml/badge.svg)](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/publish.yml)
[![Deploy staging](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/deploy.yml/badge.svg)](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/deploy.yml)
[![NuGet Core](https://img.shields.io/nuget/v/FSharp.ViewEngine)](https://www.nuget.org/packages/FSharp.ViewEngine)
[![NuGet Components](https://img.shields.io/nuget/v/FSharp.ViewEngine.Components)](https://www.nuget.org/packages/FSharp.ViewEngine.Components)

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

Documentation site built using FSharp.ViewEngine available at [https://fve.meiermade.com](https://fve.meiermade.com).
> See [sln/src/Docs](./sln/src/Docs) for the source code.

## Installation
Add the core view engine package with your preferred CLI.

```shell
dotnet add package FSharp.ViewEngine
```

```shell
dotnet paket add FSharp.ViewEngine
```

For accessible, server-rendered Tailwind components with Datastar interactions, pin the `fve` tool and add consumer-owned source:

```shell
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init src/Acme.Components/Acme.Components.fsproj --namespace Acme.Components
dotnet fve add button input --config src/Acme.Components/fve.json
```

`fve` copies independently installable Components and genuine helpers in deterministic F# compile order. Controls, feedback, tables, calendars, menus, overlays, headers, Card, and documentation controls share one consumer-selected namespace. Consumers own and commit the source; `fve diff` never silently replaces edits. See the [Components source documentation](./sln/src/FSharp.ViewEngine.Components/README.md).

Page and site assembly is ordinary copyable F# source. Exactly three full-page templates share a small financial Account/Transaction model: Specification, Application, and API documentation. The Examples gallery opens each template in a new tab outside the library shell, without an extra viewer bar. A download icon in each example's top bar supplies a source ZIP containing genuine Domain/Application libraries, the Server host, layout, pages, projects, setup, Tailwind input, and pinned browser assets/fonts. Download links are catalog-owned top-bar actions and are omitted by the standalone host. The Specification groups named resource workflows and provides clickable System context → Solution → project contracts.

## Local catalog development

From the preserved candidate checkout, use the .NET 10 SDK and Tailwind CSS CLI v4.2.2 on `PATH`. After restoring the repository's tools/packages, run one watcher:

```sh
cd sln
./fake.sh WatchDocs --single-target
```

The watcher serves F# changes and compiles CSS from the common `sln/src` source root, including Components and the authored templates, at the stable review URL `http://127.0.0.1:5054`. Starting it replaces only the previous FSharp.ViewEngine Docs watcher, including across worktrees; it never takes an unrelated listener. `WatchDocs` pins both the listener and public origin to this URL, ignoring inherited URL overrides and launch profiles. Package publication and sibling application changes are not needed. After adding/removing project references or compile items, restart this candidate's watcher so it reloads the project graph; ordinary edits stay in the same loop.

- `/components` — independently installable controls, display, navigation, overlays, layout blocks, code, and diagrams, with shared guides.
- `/components/card`, `/components/page-header`, `/components/section-header` — generic surfaces and optional reusable headings.
- `/components/input`, `/components/textarea`, `/components/error-summary`, `/components/field-group` — focused form components, not a form-layout framework.
- `/components/code-block`, `/components/callout`, `/components/example`, `/components/mermaid`, `/components/fsharp-api-reference` — reusable documentation building blocks.
- `/examples` — exactly three template cards.
- `/examples/application` — home, accounts, matching details, create/edit, transactions, settings, and profile.
- `/examples/specification` — wide workflow canvases with shared Application HTML states, copyable App-mode composition, sequence diagrams, bullet rules and architecture.
- `/examples/api-documentation` — account CRUD and transaction operations, requests, responses, payloads, and validation.

Every reusable component has its own route, navigation entry, installation selector, and compiling source. Templates are consumer-authored pages rather than framework APIs. Canonical compositions use official Datastar 1.0.4 with host-issued nonce CSP: no unsafe-eval or unrestricted inline scripts, same-origin backend actions, and an explicit separate allowance for component inline styling. The complete source ZIP demonstrates the policy and authorized asset initialization. See the [Datastar security integration](https://fve.meiermade.com/extensions/datastar#content-security-policy).

Demo forms validate finite server-rendered states without cookies, persistence, private-value retention, or financial effects. API operations are illustrative contracts, not live endpoints. Existing bookmarked framework-index URLs redirect to their owning component or template destination; compatibility fixtures are not additional top-level gallery entries.

In another terminal at the checkout root, run focused catalog checks:

```sh
cd e2e
E2E_START_LOCAL=0 E2E_CROSS_BROWSER_MODE=full DOCS_E2E_BASE_URL=http://127.0.0.1:5054 \
  npx playwright test tests/catalog-areas.spec.ts \
  --project=chromium --project=firefox --project=webkit --workers=1
```

For focused control interactions, replace the spec with `tests/multiple-choice.spec.ts` or `tests/popup-focus.spec.ts`.

From `e2e`, verify browser selection or run the local suite in CI's pinned browser container:

```sh
npm run test:selection
E2E_SERVER_PORT=6054 bash scripts/test-ci.sh
```

PR CI runs the full Chromium suite and focused Firefox/WebKit regressions. Releases run a short protected-staging smoke suite; package verification and production safeguards remain required.

## Releases

Two NuGet packages have independent release trains managed through the **Publish packages** workflow:

- `FSharp.ViewEngine` uses tags such as `v2026.8.1`.
- `FSharp.ViewEngine.Cli` uses tags such as `cli/v2026.9.0`.
- Release dispatches publish Core, CLI, both, or Docs-only snapshots. A Docs-only release is accepted only when package-contract coherence checks prove the selected public Core and CLI versions already contain every contract change.

CLI releases package the exact canonical component source registry compiled by this repository. A combined release publishes Core before the CLI when both changed. Engine releases become the repository-wide GitHub “Latest” release; CLI releases do not. Direct packing requires explicit package-version properties.

When a published package is permanently replaced, deprecate it only after its replacement and canonical production documentation are verified. In NuGet.org **Manage Packages → Deprecation**, select every version, choose **Legacy**, name the replacement package, and leave the versions listed/downloadable. Verify the resulting public metadata and pinned downloads with `node e2e/scripts/verify-package-retirement.mjs`; the check covers every Components and Docs version returned by NuGet.org rather than a hard-coded list.

Versioned changelog entries are added in a follow-up pull request after the package is published and verified and its GitHub release has been reconciled. Feature pull requests and pre-publication workflow steps must not claim a package version or release date that does not yet exist.

Every successful push to `main` deploys one immutable candidate to the Cloudflare Access-protected staging site at `https://fve.meiermade.net` and runs a bounded, credential-scoped smoke test. The public documentation site at `https://fve.meiermade.com` remains on its last released package-coherent image until an explicit release promotes that exact staging-accepted digest; ordinary merges do not update production. The legacy `https://fsharpviewengine.meiermade.com` hostname permanently redirects to the canonical origin while preserving paths and query strings.

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
