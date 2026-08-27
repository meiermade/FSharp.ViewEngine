# FSharp.ViewEngine.Components

Accessible, server-rendered Tailwind components for [FSharp.ViewEngine](https://www.nuget.org/packages/FSharp.ViewEngine), with Datastar as the interaction model.

## Install

```shell
dotnet add package FSharp.ViewEngine.Components
```

The package declares its minimum compatible `FSharp.ViewEngine` version. Components and Core version independently.

## Render a component

```fsharp
open FSharp.ViewEngine
open FSharp.ViewEngine.Components

let createButton =
    Button.create "Create account"
    |> Button.withVariant ButtonVariant.Primary
    |> Button.render

let html = Render.toString createButton
```

Wrap component compositions in a semantic theme class:

```fsharp
open type Html

div {
    for attribute in ComponentsTheme.attributes ComponentsTheme.sky do
        attribute

    createButton
}
```

## Tailwind CSS 4

The NuGet package includes `FSharp.ViewEngine.Components.tailwind.css` under `contentFiles/any/any`. Copy that manifest into the application’s CSS source tree and import it after Tailwind:

```css
@import "tailwindcss";
@import "./FSharp.ViewEngine.Components.tailwind.css";
```

The manifest contains the renderer-owned utility inventory and semantic CSS variables. Applications may override semantic variables in their own theme class without replacing component markup:

```css
.acme-theme {
  --fve-brand-solid: oklch(58% 0.18 264);
  --fve-brand-hover: oklch(51% 0.2 264);
  --fve-brand-ring: oklch(68% 0.16 264);
}
```

## Foundations

Button, IconButton, Badge, Status, LoadingIndicator, and EmptyState share the semantic theme, tone, size, radius, density, light-mode, and dark-mode contracts where applicable. IconButton and LoadingIndicator require accessible labels. Pending buttons retain their action name, expose busy state, and prevent duplicate activation.

## Interaction and state

Components use Datastar signals for ephemeral open, query, focus, and selection presentation. Applications remain responsible for durable state, authorization, validation, routing, and server actions.

Select, Combobox, DropdownMenu, Dialog, Checkbox, Switch, ToggleButton, and RadioGroup preserve their distinct form and accessibility semantics. Required accessible labels are constructor inputs.

## Documentation

The complete component gallery, typed examples, theming guidance, and application-boundary guidance are published at:

https://fsharpviewengine.meiermade.com/components
