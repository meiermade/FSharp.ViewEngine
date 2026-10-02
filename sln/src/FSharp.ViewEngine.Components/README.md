# FSharp.ViewEngine Components

Install reusable building blocks as source. Assemble pages, applications, specifications, and API references in your own F# HTML; there is no installable Application or Documentation framework.

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

let form =
    form {
        Input.create "name" "Account name" |> Input.required |> Input.render
        Button.create (ButtonContent.Text "Create account") |> Button.asSubmit |> Button.render
    }
```

The CLI installs the selected source and genuine dependencies in deterministic F# compile order. The component namespace is the namespace selected at initialization, without Primitives/Application/Documentation suffixes. `FSharp.ViewEngine` remains the normal Core package dependency.

## Catalog and templates

Every reusable component has a dedicated page with a default preview, installation selector, focused compiling examples, variants/states, and its own declaration-owned F# API reference.

Exactly three full-page templates share one Account/Transaction model:

- Application: home, searchable/filterable/sortable accounts, selection, details, create/edit, transactions, settings, and profile.
- Specification: wide workflow canvases with underlined Fixture state tabs, actual Application previews, Fixture-owned App mode, sequence diagrams and bullet rules.
- API documentation: account list/create/update/delete and transaction operations, request fields, responses, errors, and payloads.

The catalog owns the Examples gallery and the compact template-name Preview/Code bar. Those controls are excluded from template source. The source viewer exposes all authored files, the standalone Giraffe host, project file, and setup instructions—not snippets with missing helpers. Forms validate resettable states without retaining submissions or performing financial operations. API examples describe a contract, not a deployed API.

## Ownership

- **Card** is a general content surface with optional header, media, footer, and regular/small sizing. Media is ordinary consumer-authored HTML; there is no media-library framework.
- **Page header**, **Section header**, and **Page top bar** are independent optional building blocks. Use them where needed; ordinary page HTML does not require a visible heading or chrome.
- **Input**, **Textarea**, **Error summary**, **Avatar**, **Copy and reveal**, **Dialog**, and **Drawer** have distinct selectors and source boundaries.
- **Field group** owns related-field grouping. Constructors and tightly coupled helpers stay with their owning component rather than gaining empty catalog pages.
- **Code block**, **Callout**, **Example**, **Mermaid** (including sequence constructors), and **F# API reference** are reusable documentation controls. `CodeBlock.assets` and `Mermaid.assets` provide component-local browser lifecycles with host-provided asset URLs.
- **Browser** and **Phone** provide static frames. The Specification example (`Specification.fs` and `AppMode.fs`) demonstrates previews, clickable product journeys and compact review controls.
- **Upload list** displays consumer-owned queue states; it is not an upload transport.
- **Item** remains a list-row concept rather than a second Card.

All registry source is compiled by this repository. `Templates/` contains repository-only support for the existing catalog and retained compatibility examples; it is excluded from the source registry and from copied templates. This project is not a published compiled package.

## Presentation

Use Tailwind CSS v4 and the Typography plugin. Scan both owned component and application source:

```css
@import "tailwindcss";
@plugin "@tailwindcss/typography";
@source "./src/Acme.Components/Components/**/*.fs";
@source "./src/Acme.Web/**/*.fs";
```

The CLI copies no stylesheet. Components consume semantic `--fve-*` variables for background, surfaces, text, borders, palette, focus, density, and sizing. Use `--fve-background`, not the retired `--fve-page`. Light/dark mode follows `color-scheme`; select a theme on the authored region or document.

```fsharp
let theme =
    ComponentsTheme.sky
    |> ComponentsTheme.withDensity Density.Compact
    |> ComponentsTheme.withControlSize ControlSize.Small

let workspace = div { ComponentsTheme.attributes theme; p { "Consumer-owned layout" } }
```

Control sizes are 32/40/48px at the standard 16px root. Small supplies compact 14px application UI; Medium is the 16px form default. Density controls navigation and bar geometry independently. Compact bars have a 48px minimum; ordinary text and layout can reflow.

Authored elements own their utilities. Do not globally restyle component descendants from a generic ancestor. Provide host-owned fonts and one compiled stylesheet.

## Interaction contracts

- Native controls retain names, values, validation, activation, and unavailable behavior. Pending Input/Textarea values remain successful controls; disabled controls do not. Applications validate every received value and own authorization, durable state, and actions.
- `Table.withSelection` supplies native row checkboxes and mixed select-all state. Stable keys are unique; selection survives same-instance morphs and resizing, prunes removed rows, and emits `fve-table-selection-change`. Compose selection actions with ordinary Buttons and a consumer-owned form. The Table page demonstrates selection only. The Application example’s Accounts page owns its batch-action form and server validation; Table does not own commands or their outcomes.
- `ButtonContent.Custom` is noninteractive presentation. Button colors and variants are independent; keyboard feedback preserves native activation and reduced motion.
- Notification is latest-wins, zero-or-one. TTL, persistence, pause, and manual dismissal belong to that notification; there are no Toast aliases, stacks, or global managers. Durable actions belong in Notice or Dialog.
- Notice presentation does not imply live-region urgency. Dialog/Drawer own focus containment, dismissal, and restoration; applications own their contents and outcomes.
- Day, Week, Month, and Year are separate calendars. Month owns display and native single/range selection; Date picker composes it. Mobile time grids remain locally scrollable, not automatic agendas. Applications own dates, time zones, queries, and period navigation.
- Prism/Mermaid initialization and refresh are component-local and morph-safe. Source CR/LF is preserved. Copy controls are compact and inset from code.

## Migration and source safety

Use `dotnet fve diff --config ...` before replacing source. `add` preserves local edits; `--overwrite` is explicit. Preserve edited retired files and migrate them manually before removing compile items or configuration entries.

`BulkAction`/`BulkActions` and the `bulk-actions` selector are retired without aliases. Keep `Table.withSelection`; use its native checkbox values in your own form and use ordinary Button/DropdownMenu controls. Your application owns authorization, confirmation, processing, and server-confirmed outcomes. Preserve edited installed source while migrating, then explicitly remove the retired selector, file, and compile item from your project.

Replace old bundled selectors with their owning controls: `text-field` → `input` / `textarea` / `error-summary`; `identity` → `avatar` / `copy-reveal`; `tags` → `tag-input`; `upload` → `upload-list`; split Drawer from Dialog. Page/Section/Collection/Detail/AppShell and Documentation assemblies become consumer-authored template layout. Do not use aliases that copy an unrelated framework bundle as an installation boundary.

Registry changes, component routes/navigation, copied source, generated-consumer compilation, and focused browser checks must move together.
