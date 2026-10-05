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

- Application: home, searchable/filterable/sortable accounts, selection, details, create/edit, transactions, settings, and profile. All three Examples share the docs' sky brand theme and put page actions in a visible PageHeader, not PageTopBar.
- Accounts and Transactions compose Search/Add filter, editable removable applied chips, and a conditional selection toolbar above a bounded row-scrolling Table with sticky headings. Clear all preserves Search; native GET destinations own filtering and sorting. Selected counts, commands and review feedback never occupy a table footer. The Specification shares the rendering and documents added-filter, filtered, sorted, selected and review states. These finite examples do not implement infinite-scroll fetching or all-matching selection.
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
- `Table.withSelection` supplies native row checkboxes and mixed select-all state. Stable keys are unique; selection survives same-instance morphs and resizing, prunes removed rows, and emits `fve-table-selection-change` on initialization, morph updates and changes. Table provides a screen-reader selection announcement, not a visible count/footer. Compose visible counts and selection actions with ordinary Buttons and a consumer-owned toolbar/form. `Table.withScrollableRows` fills a consumer-bounded flex height with an independently scrollable row viewport and sticky column headings; it does not load data. The Table page owns focused selection and scrolling examples. The Application owns its batch-action forms and server validation; Table does not own commands or their outcomes.
- `ButtonContent.Custom` is noninteractive presentation. Button colors and variants are independent; keyboard feedback preserves native activation and reduced motion.
- Notification is latest-wins, zero-or-one. TTL, persistence, pause, and manual dismissal belong to that notification; there are no Toast aliases, stacks, or global managers. Durable actions belong in Notice or Dialog.
- Notice presentation does not imply live-region urgency. Dialog/Drawer own focus containment, dismissal, and restoration; applications own their contents and outcomes.
- Day, Week, Month, and Year are separate calendars. Month owns display and native single/range selection; Date picker composes it. Full months use shorter equal-height days, three aligned event rows per day, and an anchored Popover with the complete day list for overflow. `CalendarEvent.withEndDate` adds an inclusive all-day end date; full months keep these bars visible across dates in a stable lane and split them at week boundaries. Day/Week show each covered day's event, and compact Month/Year mark every covered date. Timed events remain same-day; consumers split overnight intervals and resolve time zones. Use `MonthCalendar.withId` for distinct stable DOM scopes when event calendars share a label. Compact months and mobile time grids remain locally scrollable, not automatic agendas. Applications own dates, time zones, queries, and period navigation.
- Tooltip takes a trigger renderer: `Tooltip.create id description (fun descriptionId -> ...)`. Set `_ariaDescribedby descriptionId` on the actual focusable control; append existing description IDs in the same attribute and retain its accessible name. Hover includes the tooltip text and Escape dismisses it without moving focus.
- Item’s title supplies a stretched row link; media, description and metadata remain separate interaction regions outside its anchor. A row with an explicit action cluster remains a plain Item.
- Prism/Mermaid initialization and refresh are component-local and morph-safe. Source CR/LF is preserved. Copy controls are compact and inset from code.

## Migration and source safety

`UploadState`/`UploadItem`/`UploadList` and the `upload-list` selector are retired without aliases or an upload example. Compose consumer-owned upload UI from ordinary HTML and independently installed controls when needed. Preserve edited installed source before explicitly removing retired compile/config entries.

`FirstStep`/`FirstSteps` and the `first-steps` selector are retired without aliases. Compose your own checklist HTML inside `FloatingPanel`; consumers own its progress and actions. Preserve edited installed source while migrating, then explicitly remove the retired file, selector, and compile item.

Tooltip’s former arbitrary-HTML trigger argument becomes a renderer receiving its description ID. Apply that ID to the actual Button/link/control rather than a surrounding span; no legacy trigger overload is retained.

Use `dotnet fve diff --config ...` before replacing source. `add` preserves local edits; `--overwrite` is explicit. Preserve edited retired files and migrate them manually before removing compile items or configuration entries.

`BulkAction`/`BulkActions` and the `bulk-actions` selector are retired without aliases. Keep `Table.withSelection`; use its native checkbox values in your own form and use ordinary Button/DropdownMenu controls. Your application owns authorization, confirmation, processing, and server-confirmed outcomes. Preserve edited installed source while migrating, then explicitly remove the retired selector, file, and compile item from your project.

Replace old bundled selectors with their owning controls: `text-field` → `input` / `textarea` / `error-summary`; `identity` → `avatar` / `copy-reveal`; `tags` → `tag-input`; split Drawer from Dialog. Page/Section/Collection/Detail/AppShell and Documentation assemblies become consumer-authored template layout. Do not use aliases that copy an unrelated framework bundle as an installation boundary.

Registry changes, component routes/navigation, copied source, generated-consumer compilation, and focused browser checks must move together.
