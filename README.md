[![Publish Packages](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/publish.yml/badge.svg)](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/publish.yml)
[![Deploy staging](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/deploy.yml/badge.svg)](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/deploy.yml)
[![NuGet Core](https://img.shields.io/nuget/v/FSharp.ViewEngine)](https://www.nuget.org/packages/FSharp.ViewEngine)
[![NuGet CLI](https://img.shields.io/nuget/v/FSharp.ViewEngine.Cli)](https://www.nuget.org/packages/FSharp.ViewEngine.Cli)

<p align="center">
  <img src="etc/logo.svg" alt="FSharp.ViewEngine" width="128">
</p>

# FSharp.ViewEngine

A minimal, typed HTML DSL for F#. Build server-rendered HTML with computation expressions, mixing attributes, strings, and child elements in one sequence.

Inspired by [Giraffe.ViewEngine](https://github.com/giraffe-fsharp/Giraffe.ViewEngine), [Feliz.ViewEngine](https://github.com/dbrattli/Feliz.ViewEngine), [Oxpecker.ViewEngine](https://github.com/Lanayx/Oxpecker), and [Bolero](https://github.com/fsbolero/Bolero).

[Documentation](https://fve.meiermade.com) · [Components](https://fve.meiermade.com/components) · [Examples](https://fve.meiermade.com/examples)

## Install and render

```sh
dotnet add package FSharp.ViewEngine
```

```fsharp
open FSharp.ViewEngine
open type Html

let page =
    html {
        _lang "en"
        head { title { "Hello" } }
        body {
            h1 { _class "text-xl"; "Hello from FSharp.ViewEngine" }
            for name in ["Alice"; "Bob"] do
                p { name }
        }
    }

let document = Render.toHtmlDocString page
```

`Render.toString` renders fragments; `Render.toHtmlDocString` adds the HTML5 doctype. Additional targets support `StringBuilder`, `TextWriter`, and UTF-8 bytes. Ordinary text and attribute values are encoded; raw markup and executable expressions must remain developer-controlled.

Core targets `net8.0` and is tested on .NET 8, 9, and 10. Portable symbols include Source Link metadata.

## Components and examples

Install accessible Tailwind components with Datastar interactions as source your application owns:

```sh
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init src/Acme.Components/Acme.Components.fsproj --namespace Acme.Components
dotnet fve add button input --config src/Acme.Components/fve.json
```

Commit the tool manifest and generated source. See the [Components README](sln/src/FSharp.ViewEngine.Components/README.md) for styling and host requirements, and the [CLI README](sln/src/FSharp.ViewEngine.Cli/README.md) for safe source updates.

The [Examples gallery](https://fve.meiermade.com/examples) provides three complete, downloadable compositions: Application, Specification, and API documentation. They share immutable financial fixtures; forms demonstrate validation, not persistence, and API contracts are illustrative. Start with the [example README](sln/src/Docs/src/Examples/README.md).

## Local development

Use the .NET 10 SDK and Tailwind CSS CLI v4.2.2 on `PATH`:

```sh
cd sln
dotnet tool restore
dotnet paket restore
./fake.sh WatchDocs --single-target
```

The catalog runs at `http://127.0.0.1:5054`. Run one watcher; starting it replaces the previous Docs watcher across worktrees without taking unrelated listeners. Restart after changing project references or compile items.

To run a focused browser check against that watcher:

```sh
cd e2e
npm ci
E2E_START_LOCAL=0 DOCS_E2E_BASE_URL=http://127.0.0.1:5054 \
  npx playwright test tests/catalog-areas.spec.ts --project=chromium --workers=1
```

See [release procedures](.github/RELEASING.md) for package and Docs delivery. The [benchmark documentation](https://fve.meiermade.com/benchmarks) contains reproduction commands, methodology, detailed measurements, and profiling findings; results are not CI regression thresholds.
