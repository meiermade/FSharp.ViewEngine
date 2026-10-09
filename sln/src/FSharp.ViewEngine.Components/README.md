# FSharp.ViewEngine Components

Reusable, server-rendered Tailwind components with Datastar interactions. Install them as source your application owns; compose your own layouts, routes, and domain behavior.

## Install

```sh
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init src/Acme.Components/Acme.Components.fsproj --namespace Acme.Components
dotnet fve add button input --config src/Acme.Components/fve.json
```

```fsharp
open FSharp.ViewEngine
open Acme.Components
open type Html

let accountForm =
    form {
        Input.create "name" "Account name" |> Input.required |> Input.render
        Button.create (ButtonContent.Text "Create account") |> Button.asSubmit |> Button.render
    }
```

The CLI installs dependencies in F# compile order under your chosen namespace. Commit the generated project, `fve.json`, source, and tool manifest. Core remains a normal package dependency; this source project is not a published compiled package.

## Styling

The CLI copies no CSS. Use Tailwind CSS v4 with the Typography plugin, scanning both component and application source:

```css
@import "tailwindcss";
@plugin "@tailwindcss/typography";
@source "./src/Acme.Components/Components/**/*.fs";
@source "./src/Acme.Web/**/*.fs";
```

Provide your own fonts and compiled stylesheet. Components use semantic `--fve-*` variables; choose a theme, density, and control size on the owning region:

```fsharp
let theme =
    ComponentsTheme.sky
    |> ComponentsTheme.withDensity Density.Compact
    |> ComponentsTheme.withControlSize ControlSize.Small

let workspace =
    div {
        for attribute in ComponentsTheme.attributes theme do attribute
        p { "Consumer-owned layout" }
    }
```

Use component options and authored utilities rather than globally restyling descendants.

## Browser and host requirements

Load Datastar and any assets required by the selected controls. The [nonce CSP guide](https://fve.meiermade.com/extensions/datastar#content-security-policy) explains the supported integration, including `ThemeSwitcher.assetsWithNonce`, `CodeBlock.assetsWithNonce`, and `Mermaid.assetsWithNonce`. Full documents get fresh host-issued nonces; enhanced patches retain the active document nonce. Nonces do not authorize inline event handlers.

Components currently require an explicit inline-style allowance for CSS variables and positioning. Keep expressions and raw markup developer-controlled; serialize user values as data. Signals are public and client-controlled. Validation, authentication, authorization, CSRF, caching, and the complete response policy belong to the host.

## Documentation and source updates

Each [component page](https://fve.meiermade.com/components) owns its API, variants, states, interactions, and copyable examples. The [three full-page examples](https://fve.meiermade.com/examples) demonstrate consumer-owned Application, Specification, and API documentation compositions; their download includes an independently buildable host and README.

Before updating the pinned CLI, run `dotnet fve diff --config src/Acme.Components/fve.json`. Local edits are never silently replaced; `fve add --overwrite` is explicit. See the [migration guide](MIGRATION.md) for retired selectors and changed APIs.
