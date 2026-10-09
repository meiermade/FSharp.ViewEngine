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
- Accounts and Transactions compose Search/Add filter, joined applied-filter ButtonGroups with muted labels, Select values and native remove buttons, and a conditional selection toolbar above a bounded row-scrolling Table with sticky headings. Clear all preserves Search; native GET destinations own filtering and sorting. Selected counts, commands and review feedback never occupy a table footer. The Specification shares the rendering and documents added-filter, filtered, sorted, selected and review states. These finite examples do not implement infinite-scroll fetching or all-matching selection.
- Specification: the actual Docs shell conventions, with compact left-chevron navigation, hierarchical breadcrumbs, an icon theme menu, independently scrolling/resizable rails, readable articles with contents navigation, and wide workflow canvases with underlined state tabs, actual Application previews, connected App mode, sequence diagrams and bullet rules.
- API documentation: account list/create/update/delete and transaction operations, request fields, responses, errors, and payloads.

The catalog owns the Examples gallery, opens templates in new tabs, and supplies a native source-ZIP download link in each example’s top bar. Catalog controls are excluded from copied source. The ZIP includes all authored files, the standalone Giraffe host, project files, setup instructions, stylesheet input and browser assets—not snippets with missing helpers. Forms validate resettable states without durable submission retention or financial mutations. Create/edit use a single-column Drawer; validation errors return entered values only in a no-store response. Delete uses a centered Dialog, identifies the exact record and initially focuses Cancel. Opening an overlay preserves the underlying page title, breadcrumbs and public collection context. API examples describe a contract, not a deployed API.

## Ownership

- **Card** is a general content surface with optional header, media, footer, and regular/small sizing. Media is ordinary consumer-authored HTML; there is no media-library framework.
- **Side nav** owns four optional slots: `withHeader`, `withContext`, `withContent`, and `withFooter`. The header aligns with PageTopBar; context/footer accept ordered lists of rendered rows and stay outside the scrolling content. `SideNavContent.create` takes an ordered list of `SideNavNode` values: `SideNavItem.create` supplies links, `SideNavSection.create` supplies non-collapsible headings, and `SideNavGroup.create` supplies collapsible groups. Sections use muted 12px, medium-weight labels with authored casing and no child indentation or guide. Links never reserve empty chevron spacers; plain links use the same inset at every depth, aligned with the visible edge of sibling group chevrons rather than their labels. Guided children leave a 4px gutter between the line and row hover/current backgrounds. Collapsible groups retain nested indentation and vertical guides aligned beneath the chevron point. `SideNavRow.link` and `SideNavRow.menu` supply the same full-width 40px row presentation for workspace, settings, profile, or consumer-defined controls; menus reuse DropdownMenu. These tightly coupled helpers share the Side nav documentation page and installation selector.
- **Side nav**, **Breadcrumbs**, **Theme switcher**, and **Resizable** are independently installed controls used by both the actual Docs shell and its copyable documentation composition. ThemeSwitcher.assets (or assetsWithNonce) owns document preference initialization; nativeChoiceAttributes preserves native radio preferences before Datastar initializes; shared menu controls never supply their own competing theme runtime. Resizable.renderWithClasses lets consumers compose unframed responsive rails without private Templates helpers.
- **Page header**, **Section header**, and **Page top bar** are independent optional building blocks. Use them where needed; ordinary page HTML does not require a visible heading or chrome.
- **Input**, **Textarea**, **Error summary**, **Avatar**, **Copy and reveal**, **Dialog**, and **Drawer** have distinct selectors and source boundaries.
- **Button** renders native commands with `Button.render` and destination anchors with `Button.renderLink href`. Links retain shared colors, sizing, variants and safe attributes but do not support submit, disabled or pending states. Consumer-authored secondary page actions and Cancel use Neutral + Outline; primary actions remain Solid. `DropdownMenuTrigger.withVariant ButtonVariant.Outline` gives page-header overflow the same outline appearance without changing default Ghost row triggers or full utility-row styling.
- **Button group** joins related Button, Select, DropdownMenu, native link and muted label segments with shared sizing, corner treatment and independent interactive focus stops. `ButtonGroupItem.label` is noninteractive, not a disabled button; `ButtonGroupItem.select` retains Select’s accessible label, selected value, popup and keyboard behavior. `ButtonGroupItem.link destination content` preserves a resolved href and native navigation. Applied filters compose a label, Select and icon-only remove Button; URL filtering and native GET forms remain consumer-owned.
- **Select** emits a bubbling change event from its named value input after a committed single-value change. `Select.withNativeFallback` opts an ordinary single select into a styled native field before enhancement, with exactly one successful value control before and after Datastar initializes. Searchable/multiple controls retain their existing rendering.
- **Field group** owns related-field grouping. Constructors and tightly coupled helpers stay with their owning component rather than gaining empty catalog pages.
- **Code block**, **Callout**, **Example**, **Mermaid** (including sequence constructors), and **F# API reference** are reusable documentation controls. `CodeBlock.assets` and `Mermaid.assets` provide component-local browser lifecycles with host-provided asset URLs. Their `assetsWithNonce` variants authorize initialization and lazy scripts under the current document's CSP nonce.

- **Browser** and **Phone** provide static frames. The Specification example (`Specification.fs` and `AppMode.fs`) demonstrates previews, clickable product journeys and compact review controls.
- **Upload list** displays consumer-owned queue states; it is not an upload transport.
- **Item** remains a list-row concept rather than a second Card.

All registry source is compiled by this repository. `Templates/` contains repository-only support for the existing catalog and retained compatibility examples; it is excluded from the source registry and from copied templates. This project is not a published compiled package.

## Datastar and content security policy

Canonical compositions use the pinned official Datastar 1.0.4 standard bundle and its [nonce CSP mode](https://data-star.dev/reference/security), not a replacement interaction runtime. The host generates a fresh cryptographically random nonce for each full-page response, authorizes it in `script-src 'self' 'nonce-{value}'`, and emits the matching `<html data-nonce="…">`. Datastar consumes that attribute and uses the active document nonce for expressions and scripts in patches. Partial responses do not replace the nonce. Use `connect-src 'self'` for same-origin backend actions, and explicitly authorize any required external asset origins.

Use `ThemeSwitcher.assetsWithNonce`, `CodeBlock.assetsWithNonce` and `Mermaid.assetsWithNonce` for their document-head initialization. Nonces do not authorize `onclick` attributes: compose actions with Datastar instead. Existing `assets` helpers remain available for hosts that intentionally choose a different policy.

Components emit inline CSS variables and positioning. The example host deliberately allows `style-src 'self' 'unsafe-inline'`; this is separate from the nonce-only script policy and does not promise compatibility with a self-only style policy. Authentication, authorization, CSRF defenses, cache policy and the full CSP remain host-owned.

Keep expressions and raw content developer-controlled. Serialize untrusted values as data, not executable expressions. Signals are public/client-controlled, never credentials or authorization. Validate all values and permissions server-side, and sanitize any intentionally allowed user HTML. See the complete example ZIP's `Hosting.fs`, `Layout.fs` and `Setup.md` for a working, independently buildable integration.

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

Control sizes are 32/40/48px at the standard 16px root. Small supplies compact 14px application UI; Medium is the 16px form default. Density controls navigation and bar geometry independently. Compact bars have a 48px minimum and compact navigation items a 28px minimum. Context/footer SideNavRow controls use a separate 40px minimum; ordinary text and layout can reflow.

Authored elements own their utilities. Do not globally restyle component descendants from a generic ancestor. Provide host-owned fonts and one compiled stylesheet.

## Interaction contracts

- Native controls retain names, values, validation, activation, and unavailable behavior. Pending Input/Textarea values remain successful controls; disabled controls do not. Applications validate every received value and own authorization, durable state, and actions.
- `Table.withSelection` supplies native row checkboxes and mixed select-all state. Stable keys are unique; selection survives same-instance morphs and resizing, prunes removed rows, and emits `fve-table-selection-change` on initialization, morph updates and changes. Table provides a screen-reader selection announcement, not a visible count/footer. Compose visible counts and selection actions with ordinary Buttons and a consumer-owned toolbar/form. `Table.withScrollableRows` fills a consumer-bounded flex height with an independently scrollable row viewport and sticky column headings; it does not load data. The Table page owns focused selection and scrolling examples. The Application owns its batch-action forms and server validation; Table does not own commands or their outcomes.
- `ButtonContent.Custom` is noninteractive presentation. Button colors and variants are independent; keyboard feedback preserves native activation and reduced motion.
- Notification is latest-wins, zero-or-one. TTL, persistence, pause, and manual dismissal belong to that notification; there are no Toast aliases, stacks, or global managers. Durable actions belong in Notice or Dialog.
- Notice presentation does not imply live-region urgency. Dialog/Drawer own native modal focus containment, dismissal, and restoration; applications own their contents and outcomes. `Drawer.withAttributes` allows consumer-owned width and route/dirty-form lifecycle hooks while protecting structural overlay attributes. Side drawers fill small screens; their bodies scroll independently of the header and optional footer.
- Day, Week, Month, and Year are separate calendars. Month owns display and native single/range selection; Date picker composes it. Full months use shorter equal-height days, three aligned event rows per day, and an anchored Popover with the complete day list for overflow. `CalendarEvent.withEndDate` adds an inclusive all-day end date; full months keep these bars visible across dates in a stable lane and split them at week boundaries. Day/Week show each covered day's event, and compact Month/Year mark every covered date. Timed events remain same-day; consumers split overnight intervals and resolve time zones. Use `MonthCalendar.withId` for distinct stable DOM scopes when event calendars share a label. Compact months and mobile time grids remain locally scrollable, not automatic agendas. Applications own dates, time zones, queries, and period navigation.
- Tooltip takes a trigger renderer: `Tooltip.create id description (fun descriptionId -> ...)`. Set `_ariaDescribedby descriptionId` on the actual focusable control; append existing description IDs in the same attribute and retain its accessible name. Hover includes the tooltip text and Escape dismisses it without moving focus.
- Item’s title supplies a stretched row link; media, description and metadata remain separate interaction regions outside its anchor. A row with an explicit action cluster remains a plain Item.
- Prism/Mermaid initialization and refresh are component-local and morph-safe. Source CR/LF is preserved. Copy controls are compact and inset from code.

## Migration and source safety

SideNav's required header and sections arguments become optional slots. Replace `SideNav.create id label header sections` with `SideNav.create id label |> SideNav.withHeader header |> SideNav.withContent (SideNavContent.create groups)`. Omit `withHeader` instead of supplying a dummy header and calling `withoutHeader`. Replace static `SideNavSection.group` (or the interim `SideNavGroup.create`) with `SideNavSection.create`. Replace `SideNavItem.nested` with `SideNavGroup.create` and move expansion/identity options to `SideNavGroup`. Pass ungrouped items directly to `SideNavContent.create`; the `ungrouped` wrapper is no longer needed. `SideNavNode` is the shared typed composition value. Context/footer now accept lists of rendered elements with no automatic padding; use SideNavRow controls or add padding to custom content. Supply a bounded parent height to pin those regions while only the content scrolls.

`UploadState`/`UploadItem`/`UploadList` and the `upload-list` selector are retired without aliases or an upload example. Compose consumer-owned upload UI from ordinary HTML and independently installed controls when needed. Preserve edited installed source before explicitly removing retired compile/config entries.

`FirstStep`/`FirstSteps` and the `first-steps` selector are retired without aliases. Compose your own checklist HTML inside `FloatingPanel`; consumers own its progress and actions. Preserve edited installed source while migrating, then explicitly remove the retired file, selector, and compile item.

Tooltip’s former arbitrary-HTML trigger argument becomes a renderer receiving its description ID. Apply that ID to the actual Button/link/control rather than a surrounding span; no legacy trigger overload is retained.

Use `dotnet fve diff --config ...` before replacing source. `add` preserves local edits; `--overwrite` is explicit. Preserve edited retired files and migrate them manually before removing compile items or configuration entries.

`BulkAction`/`BulkActions` and the `bulk-actions` selector are retired without aliases. Keep `Table.withSelection`; use its native checkbox values in your own form and use ordinary Button/DropdownMenu controls. Your application owns authorization, confirmation, processing, and server-confirmed outcomes. Preserve edited installed source while migrating, then explicitly remove the retired selector, file, and compile item from your project.

Replace old bundled selectors with their owning controls: `text-field` → `input` / `textarea` / `error-summary`; `identity` → `avatar` / `copy-reveal`; `tags` → `tag-input`; split Drawer from Dialog. Page/Section/Collection/Detail/AppShell and Documentation assemblies become consumer-authored template layout. Do not use aliases that copy an unrelated framework bundle as an installation boundary.

Registry changes, component routes/navigation, copied source, generated-consumer compilation, and focused browser checks must move together.
