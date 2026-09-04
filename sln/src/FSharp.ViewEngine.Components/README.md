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
  --fve-brand-active: oklch(44% 0.18 264);
  --fve-brand-ring: oklch(68% 0.16 264);
}
```

## Foundations

Button, IconButton, Badge, Status, LoadingIndicator, and EmptyState share the semantic theme, tone, size, radius, density, light-mode, and dark-mode contracts where applicable. Available Button and IconButton variants provide hover, active, and focus-visible feedback. IconButton and LoadingIndicator require accessible labels. Pending buttons retain their action name, expose busy state, and prevent duplicate activation.

## Data display

Table renders typed consumer-owned rows with native captions, column and optional row headers, compact or comfortable density, custom cells/actions, empty content, and a labelled keyboard-reachable narrow overflow region.

DescriptionList and DetailField render responsive native `dl`/`dt`/`dd` relationships. Metric highlights consumer-formatted value content with optional trend text, status content, and description. Pagination presents explicit current/link/gap items through typed consumer destinations without owning page state. Chart groups consumer-drawn SVG or HTML, title, units, legend, annotations, empty state, and a required accessible summary or data representation without adding a chart runtime.

## Interaction and state

Components use Datastar signals for ephemeral open, query, focus, and selection presentation. Applications remain responsible for durable state, authorization, validation, routing, and server actions.

Select, Combobox, DropdownMenu, Dialog, Checkbox, Switch, ToggleButton, Tabs, and RadioGroup preserve their distinct form and accessibility semantics. Required accessible labels are constructor inputs.

Select is a typed select-only combobox: applications provide values, explicit encoding, options, and server validation while the component owns branded listbox presentation, active-descendant focus, disabled options, bounded typeahead, and the canonical closed/open keyboard model. `Select.required`, `Select.disabled`, `Select.pending`, and `Select.withValidation` expose truthful state without introducing a native-select wrapper. Disabled or pending Select values are omitted from ordinary form submission.

Combobox is an editable single-choice control with distinct query and selected identity. Static mode filters consumer-supplied typed options locally. Remote mode submits the query signal to an application endpoint and morphs the stable region returned by `Combobox.renderOptions`; each request explicitly uses Datastar `requestCancellation: 'auto'`, preventing an older in-flight response to the same endpoint from visibly replacing newer results. `Combobox.clearable`, `Combobox.loading`, `Combobox.withError`, `Combobox.disabled`, `Combobox.pending`, and `Combobox.withValidation` expose clear, loading, retryable error, unavailable, busy, and form-validation states without moving DOM focus away from the editable input. Disabled and pending values are omitted from ordinary form submission.

```fsharp
let accountCombobox =
    Combobox.create "account" "Parent account" string accountOptions
    |> Combobox.withSearch (ComboboxSearch.Remote "/accounts/search")
    |> Combobox.withEmptyMessage "No matching accounts"
    |> Combobox.clearable
    |> Combobox.render
```

Applications return authoritative typed options, ordering, errors, and validation. Query and interaction signals remain ephemeral; the encoded hidden selection is intentional form state.

Checkbox and RadioGroup support required state where native form semantics apply. Checkbox, Switch, and RadioGroup support stable IDs, disabled and pending states, descriptions, validation relationships, and ordinary native form submission. ToggleButton remains a non-submit action button with distinct `aria-pressed` state and supports disabled or pending activation. Pending controls retain their visible label, expose busy state, and prevent interaction; disabled and pending native controls follow platform omission from FormData. Stable IDs allow repeated form names without sharing Datastar signals, and server-rendered patches can authoritatively replace selected, checked, and pressed state.

```fsharp
let requiredStatus =
    Select.create "status" "Status" statusValue statusOptions
    |> Select.withId "account-status"
    |> Select.withPlaceholder "Choose a status"
    |> Select.required
    |> Select.render

let requiredMode =
    RadioGroup.create "mode" "Posting mode" id modeOptions
    |> RadioGroup.withId "posting-mode"
    |> RadioGroup.required
    |> RadioGroup.render
```

`required` and `disabled` are not combined on native inputs because disabled controls do not participate in browser constraint validation. Applications must validate every received value, and client-disabled presentation is not authorization.

Tabs switches among same-page peer panels with typed `TabsVariant.Segmented` and `TabsVariant.Underlined` presentation. A stable Tabs ID and required accessible group label produce collision-safe instance-local Datastar state, linked `tablist`/`tab`/`tabpanel` relationships, one roving tab stop, wrapping Left/Right movement, Home/End boundaries, automatic activation for immediately available server-rendered panels, and hidden inactive panels.

```fsharp
let accountTabs =
    Tabs.create "account-tabs" "Account sections" [
        Tab.create "overview" "Overview" overviewPanel
        Tab.create "activity" "Activity" activityPanel ]
    |> Tabs.withSelected "overview"
    |> Tabs.withVariant TabsVariant.Underlined
    |> Tabs.render
```

Use Tabs only when controls reveal associated panels in the same page. Use links for URL navigation, RadioGroup for a submitted mutually exclusive value, and ToggleButton for one independently pressed action. Patch the stable Tabs root with the same item identities so Datastar can preserve valid selected state and focus across server-rendered updates.

DropdownMenu keeps typed destinations and trusted Datastar actions application-owned while providing labelled groups, separators, leading content, shortcut hints, destructive tone, disabled or pending items, and typed Start/End popup alignment. Enabled items support pointer activation, wrapping Arrow/Home/End movement, Enter/Space activation, bounded character navigation, outside/Tab dismissal, Escape focus restoration, isolated stable-ID signals, and server-rendered morph continuity.

```fsharp
let accountActions =
    DropdownMenu.create "account-actions" "Actions" [
        MenuItem.group "Account" [
            MenuItem.link Settings "Account settings"
            MenuItem.action "@post('/accounts/101/archive')" "Archive account"
            |> MenuItem.withShortcut "A"
            MenuItem.action "@get('/accounts/101/statement')" "Export statement"
            |> MenuItem.pending ]
        MenuItem.separator
        MenuItem.destructiveAction "@delete('/accounts/101/draft')" "Delete draft" ]
    |> DropdownMenu.withAlignment MenuAlignment.Start
    |> DropdownMenu.render destinationUrl
```

Disabled and pending presentation is not authorization. Applications decide which commands exist and enforce every action on the server.

Dialog, ConfirmationDialog, and Drawer use native modal dialogs and their top-layer backdrop. They require stable IDs and accessible titles, contain focus while open, and restore focus to their connected triggers. Dialog retains consumer-authored body and footer content and can opt into safe backdrop dismissal. ConfirmationDialog focuses the least destructive cancel action first, renders a destructive submit action, exposes server validation and pending state, and uses a Datastar request indicator to prevent duplicate confirmation. Drawer renders consumer-owned landmarks in a responsive typed Start or End panel and dismisses through Escape, its close action, or the backdrop.

```fsharp
let deleteConfirmation =
    ConfirmationDialog.create
        "delete-account"
        "Delete account?"
        "Posted entries remain in the audit history."
        "Keep account"
        "Delete account"
        "@post('/accounts/101/delete')"

let accountDrawer =
    Drawer.create "account-panel" "Account settings" accountNavigation
    |> Drawer.withDescription "Manage account preferences."
    |> Drawer.withSide DrawerSide.End
```

Applications own authorization, durable workflow state, validation, and the trusted Datastar action. Patch `ConfirmationDialog.renderContent` or a stable consumer-owned region inside Drawer so an open native dialog and its focus relationship remain intact.

## Navigation and page composition

`Breadcrumbs`, `SideNavigation`, `PageHeader`, `Page`, and `AppShell` preserve a sidebar-oriented ownership boundary:

- `Breadcrumbs` renders a labelled path whose ancestors are typed links and whose final item is the non-linked current page. Deep paths move earlier ancestors into a `DropdownMenu` on narrow screens.
- `SideNavigation` owns product identity and optional mark, grouped or ungrouped destinations, current-page state, optional workspace context, and optional footer/account content.
- `PageHeader` owns the route title, breadcrumbs, and actions. It renders exactly one visually hidden `h1` without duplicating the visible current breadcrumb as another heading.
- `Page` owns local section navigation or Tabs, the scroll region, semantic `Reading`, `Wide`, or `Full` width, and `Padded` or `FullBleed` body layout.
- `AppShell` owns only the semantic theme, persistent desktop sidebar, accessible mobile navigation overlay, one `main` landmark, and the rendered Page slot.

```fsharp
type Destination = Home | Accounts | Account of int | Reports

let destinationUrl = function
    | Home -> "/"
    | Accounts -> "/accounts"
    | Account id -> $"/accounts/{id}"
    | Reports -> "/reports"

let breadcrumbs =
    Breadcrumbs.create "account-breadcrumbs" "Breadcrumb" [
        BreadcrumbItem.create Home "Home"
        BreadcrumbItem.create Accounts "Accounts"
        BreadcrumbItem.create (Account 42) "Account 42" ]

let sideNavigation =
    SideNavigation.create "product-navigation" "Primary navigation" "Ledger" Accounts [
        SideNavigationSection.group "Manage" [
            SideNavigationItem.create Home "Dashboard"
            SideNavigationItem.create Accounts "Accounts" ]
        SideNavigationSection.group "Analyze" [
            SideNavigationItem.create Reports "Reports" ] ]

let page =
    PageHeader.create "Account 42" breadcrumbs
    |> fun pageHeader -> Page.create pageHeader accountContent
    |> Page.withWidth PageWidth.Wide
    |> Page.render destinationUrl

let application =
    AppShell.create "ledger-shell" sideNavigation page
    |> AppShell.withTheme ComponentsTheme.sky
    |> AppShell.render destinationUrl
```

Desktop and mobile use one SideNavigation tree, so destination hierarchy, current state, account access, and component IDs cannot drift. On mobile, AppShell focuses the current destination when opened, contains Tab focus, dismisses through Escape or backdrop interaction, and restores the trigger when dismissal stays on the current route. Applications continue to own authorization, route state, URL/history policy, product identity, and durable account state.

Standalone Navbar, stacked/top-navigation shells, generic non-navigation sidebars, icon-only collapse, floating or inset variants, right-side navigation, and multi-column shells are intentionally outside the first sidebar-shell contract.

## Documentation

The complete component gallery, typed examples, theming guidance, and application-boundary guidance are published at:

https://fsharpviewengine.meiermade.com/components
