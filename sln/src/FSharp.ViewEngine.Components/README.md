# FSharp.ViewEngine Components

Opinionated, [Tailwind CSS](https://tailwindcss.com/)-styled components for F#, with [Datastar](https://data-star.dev/)-powered interactivity. Install editable source and customize it in your application.

## Install

```sh
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init src/Acme.Components/Acme.Components.fsproj --namespace Acme.Components
dotnet fve add button field input --config src/Acme.Components/fve.json
```

```fsharp
open FSharp.ViewEngine
open Acme.Components
open type Html

let accountForm =
    form {
        fveField {
            fveLabel { _for "account-name"; "Account name" }
            fveInput { _id "account-name"; _name "name"; _required true }
        }
        fveButton { _color Color.Primary; _variant ButtonVariant.Solid; _type "submit"; "Create account" }
    }
```

The CLI installs dependencies in F# compile order under your chosen namespace. Commit the generated project, `fve.json`, source, and tool manifest. Core remains a normal package dependency; this source project is not a published compiled package.

## Styling

Install Tailwind CSS v4, its CLI and Typography plugin, and esbuild together in your web project:

```sh
cd src/Acme.Web
npm init -y
npm install --save-dev tailwindcss@4 @tailwindcss/cli@4 @tailwindcss/typography esbuild
```

Commit `package.json` and `package-lock.json`. Create `src/Acme.Web/input.css`:

```css
@import "tailwindcss" source(none);
@plugin "@tailwindcss/typography";
@source "../Acme.Components/Components/**/*.fs";
@source "../Acme.Components/Components/Assets/**/*.js";
@source not "../Acme.Components/Components/Assets/echarts.6.1.0.esm.min.js";
@source "./**/*.fs";
```

The CLI copies no CSS. The [Installation guide](https://fve.meiermade.com/components/installation) covers component JavaScript imports, npm build/watch scripts, and loading the compiled assets. Provide your own fonts. Components use semantic `--fve-*` variables; choose a theme, density, and control size on the owning region:

```fsharp
let theme =
    ComponentsTheme.sky
    |> ComponentsTheme.withDensity Density.Compact
    |> ComponentsTheme.withControlSize Size.Small

let workspace =
    div {
        for attribute in ComponentsTheme.attributes theme do attribute
        p { "Consumer-owned layout" }
    }
```

Use component options and authored utilities rather than globally restyling descendants.

CodeBlock, Example and API code samples include a subtle code surface and light/dark syntax colors in their scanned F# source; no Docs-host CSS or external Prism theme is required. For plain output, use `fveCodeBlock { _language "text"; source }` and `CodeBlock.assets None []`: the installed CodeBlock module and shared Rocket runtime are sufficient, with no Prism download. For optional highlighting, use the [Prism download builder](https://prismjs.com/download.html) to select your languages, save its JavaScript bundle under `wwwroot/scripts`, and pass its URL to `CodeBlock.assets None scripts` (or `assetsWithNonce` under nonce CSP). `None` selects the built-in presentation; download a theme's CSS only if you want to pass a custom stylesheet URL instead. The built-in colors follow the owning `ComponentsTheme` color scheme, including scoped themes. See the [Code block guide](https://fve.meiermade.com/components/code-block) for Giraffe setup and F#, Python, TypeScript, Bash and SQL examples.

## Browser and host requirements

Controls with browser behavior install editable JavaScript, a shared pinned Rocket runtime, and its license under `Components/Assets/`. Import the selected modules from your application entry and bundle them alongside Tailwind; see the [CLI browser-module setup](../FSharp.ViewEngine.Cli/README.md#browser-modules). Load that entry once, not an ordinary Datastar bundle alongside Rocket. Button emits a thin host and requires its module to create the native light-DOM control. Author ordinary attributes and content on that host; Rocket owns the generated button, styling, spinner and lifecycle. Presentation-only layouts remain server-rendered and need no module. Table owns presentation of server-authored native parts, not record/query policy. Chart installs a separate editable module and lazily loaded ECharts asset with license, notice and provenance; the application supplies ECharts options as F# records and opts into generic chart events. Dates, currency, series types and filtering remain consumer-owned. The [nonce CSP guide](https://fve.meiermade.com/extensions/datastar#content-security-policy) explains the supported integration, including `ColorMode.assetsWithNonce`, `CodeBlock.assetsWithNonce`, and `Mermaid.assetsWithNonce`. Full documents get fresh host-issued nonces; enhanced patches retain the active document nonce. Nonces do not authorize inline event handlers.

Components currently require an explicit inline-style allowance for CSS variables and positioning. Keep expressions and raw markup developer-controlled; serialize user values as data. Signals are public and client-controlled. Validation, authentication, authorization, CSRF, caching, and the complete response policy belong to the host.

## Documentation and source updates

Each [component page](https://fve.meiermade.com/components) owns its API, variants, states, interactions, and copyable examples. The [three full-page examples](https://fve.meiermade.com/examples) demonstrate consumer-owned Application, Specification, and API documentation compositions; their download includes an independently buildable host and README.

Before updating the pinned CLI, run `dotnet fve diff --config src/Acme.Components/fve.json`. Local edits are never silently replaced; `fve add --overwrite` is explicit. See the [migration guide](MIGRATION.md) for retired selectors and changed APIs.
