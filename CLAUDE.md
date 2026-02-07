# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
FSharp.ViewEngine is a view engine for F# web applications. It provides a functional approach to generating HTML with type-safe F# code, including integrated support for HTMX, Alpine.js, Datastar, Tailwind CSS, and SVG elements.

## Repository Structure
- `sln/` — Solution root: build scripts, paket, Dockerfile, and `src/` subdirectory with all projects
- `etc/` — Assets (logos)
- `pulumi/` — Pulumi infrastructure (TypeScript) for deploying the docs app
- `.github/` — CI workflows

## Common Development Commands

**Important:** When running in a bash shell (including Claude Code), always use `./fake.sh` instead of `fake.cmd`.

### All targets (`sln/`)

```bash
cd sln

# Restore tools and packages
dotnet tool restore
dotnet paket install

# Run tests (uses Expecto, so dotnet run, not dotnet test)
./fake.sh Test
dotnet run --project src/Tests/Tests.fsproj   # Direct

# Run a single test by name
dotnet run --project src/Tests/Tests.fsproj -- --filter "Should render html document"

# Create NuGet package (needs GITHUB_REF_NAME env var)
./fake.sh Pack

# Run docs app with Tailwind watch (hot reload)
./fake.sh WatchDocs

# Build CSS for production
./fake.sh BuildDocsCss

# Publish docs app
./fake.sh PublishDocs
```

### Pulumi deployment

```bash
cd pulumi
npm install
pulumi up -s prod
```

## Architecture

### Core Type System (`sln/src/FSharp.ViewEngine/Core.fs`)
Two discriminated unions form the foundation:
- **Element**: `Text | Tag | Void | Fragment | Raw | Noop` — represents DOM nodes
- **Attribute**: `KeyValue | Boolean | Children | Noop` — represents HTML attributes

Rendering uses `StringBuilder` with recursive pattern matching. `Text` is HTML-encoded; `Raw` is not. `Void` elements (e.g., `br`, `img`) are self-closing and reject children.

### Module Organization (`sln/src/FSharp.ViewEngine/`)
- `Html.fs` — Standard HTML elements and attributes as static members on `Html` type
- `Htmx.fs` — HTMX attributes (`_hxGet`, `_hxPost`, etc.) on `Htmx` type
- `Alpine.fs` — Alpine.js directives (`_xData`, `_xShow`, etc.) on `Alpine` type
- `Datastar.fs` — Datastar attributes (`_dsSignals`, `_dsOn`, etc.) on `Datastar` type
- `Tailwind.fs` — Tailwind UI custom elements on `Tailwind` type
- `Svg.fs` — SVG elements and attributes on `Svg` type

### Usage Pattern
```fsharp
open FSharp.ViewEngine
open type Html
open type Htmx

div {
    _class "container"
    _hxGet "/api"
    h1 { "Hello" }
}
|> Render.toHtmlDocString
```

### Project Structure
- `sln/src/FSharp.ViewEngine/` — Core library (NuGet package)
- `sln/src/Tests/` — Expecto tests
- `sln/src/Benchmarks/` — BenchmarkDotNet benchmarks
- `sln/src/Build/` — FAKE build system (Test, Pack, PushNugets, WatchDocs, BuildDocsCss, PublishDocs targets)
- `sln/src/Docs/` — Documentation Giraffe web app with Markdown pages
- `pulumi/` — Infrastructure as code (Pulumi + TypeScript)

## Development Patterns

- **New HTML elements** in `Html.fs`: use `Tag` for normal elements, `Void` for self-closing. Add convenience overloads (e.g., `p (text:string)`).
- **Framework attributes**: HTMX → `Htmx.fs` with `_hx` prefix; Alpine → `Alpine.fs` with `_x` prefix; Datastar → `Datastar.fs` with `_ds` prefix.
- **New doc pages**: Add markdown in `sln/src/Docs/docs/`, handler in `Handlers.fs`, route in `Program.fs`, nav link in `Views.fs`.
- **Tests** compare rendered HTML strings using `String.clean` for whitespace normalization. Use `// language=HTML` comment for IDE syntax highlighting in expected strings.

## Build System
- FAKE build script invoked via `fake.cmd`/`fake.sh` in `sln/`
- Paket for package management (`paket.dependencies` in `sln/`)
- .NET 10.0 SDK (`global.json`)
- CI: GitHub Actions — tests on PRs, NuGet publish on release tags (`v*.*.*`)

## Infrastructure (Pulumi)
- TypeScript Pulumi project in `pulumi/`
- Deploys docs app to Kubernetes via AWS ECR
- Domain: `fsharpviewengine.meiermade.com`
- Stack references: `identity`, `infrastructure`, `fsharp-view-engine-identity`
