# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
FSharp.ViewEngine is a view engine for F# web applications. It provides a functional approach to generating HTML with type-safe F# code, including integrated support for HTMX, Alpine.js, Tailwind CSS, and SVG elements.

## Repository Structure
- `fsharp-view-engine/` — F# library, app, tests, and build system
- `pulumi/` — Pulumi infrastructure (TypeScript) for deploying the demo app
- `.github/` — CI workflows (at repo root, with `working-directory: fsharp-view-engine`)
- `.claude/` — Claude Code settings

## Common Development Commands

**Important:** When running in a bash shell (including Claude Code), always use `./fake.sh` instead of `fake.cmd`.

```bash
# All dotnet/fake commands run from fsharp-view-engine/
cd fsharp-view-engine

# Restore tools and packages
dotnet tool restore
dotnet paket install

# Run tests (uses Expecto, so dotnet run, not dotnet test)
./fake.sh Test
dotnet run --project src/Tests/Tests.fsproj   # Direct

# Run a single test by name
dotnet run --project src/Tests/Tests.fsproj -- --filter "Should render html document"

# Run demo app with Tailwind watch
./fake.sh Watch

# Create NuGet package (needs GITHUB_REF_NAME env var)
./fake.sh Pack

# Pulumi deployment
cd pulumi
npm install
pulumi up -s prod
```

## Architecture

### Core Type System (`fsharp-view-engine/src/FSharp.ViewEngine/Core.fs`)
Two discriminated unions form the foundation:
- **Element**: `Text | Tag | Void | Fragment | Raw | Noop` — represents DOM nodes
- **Attribute**: `KeyValue | Boolean | Children | Noop` — represents HTML attributes

Rendering uses `StringBuilder` with recursive pattern matching. `Text` is HTML-encoded; `Raw` is not. `Void` elements (e.g., `br`, `img`) are self-closing and reject children.

### Module Organization
- `Html.fs` — Standard HTML elements and attributes as static members on `Html` type
- `Htmx.fs` — HTMX attributes (`_hxGet`, `_hxPost`, etc.) on `Htmx` type
- `Alpine.fs` — Alpine.js directives (`_xData`, `_xShow`, etc.) on `Alpine` type
- `Tailwind.fs` — Tailwind UI custom elements on `Tailwind` type
- `Svg.fs` — SVG elements and attributes on `Svg` type

### Usage Pattern
```fsharp
open FSharp.ViewEngine
open type Html
open type Htmx

div [ _class "container"; _hxGet "/api"; _children [ h1 "Hello" ] ]
|> Element.render
```

### Project Structure
- `fsharp-view-engine/src/FSharp.ViewEngine/` — Core library (NuGet package)
- `fsharp-view-engine/src/Tests/` — xUnit tests
- `fsharp-view-engine/src/Build/` — FAKE build system (`Program.fs` defines targets)
- `fsharp-view-engine/src/App/` — Demo Giraffe web app with Markdown docs
- `pulumi/` — Infrastructure as code (Pulumi + TypeScript)

## Development Patterns

- **New HTML elements** in `Html.fs`: use `Tag` for normal elements, `Void` for self-closing. Add convenience overloads (e.g., `p (text:string)`).
- **Framework attributes**: HTMX → `Htmx.fs` with `_hx` prefix; Alpine → `Alpine.fs` with `_x` prefix.
- **Tests** compare rendered HTML strings using `String.clean` for whitespace normalization. Use `// language=HTML` comment for IDE syntax highlighting in expected strings.

## Build System
- FAKE build scripts invoked via `fake.cmd`/`fake.sh`
- Paket for package management (`paket.dependencies` at root of `fsharp-view-engine/`)
- .NET 10.0 SDK (`global.json`)
- CI: GitHub Actions — tests on PRs, NuGet publish on release tags (`v*.*.*`)

## Infrastructure (Pulumi)
- TypeScript Pulumi project in `pulumi/`
- Deploys demo app to Kubernetes via AWS ECR + Cloudflare Tunnel
- Domain: `fsharpviewengine.meiermade.com`
- Stack references: `identity`, `infrastructure`, `fsharp-view-engine-identity`
