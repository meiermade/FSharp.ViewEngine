# FSharp.ViewEngine.Components

Accessible, server-rendered Tailwind components for [FSharp.ViewEngine](https://www.nuget.org/packages/FSharp.ViewEngine), with Datastar as the interaction model.

## Install

```shell
dotnet add package FSharp.ViewEngine.Components
```

The package declares its minimum compatible `FSharp.ViewEngine` version. Components and the engine are versioned independently. Documentation now lives in this assembly; no new `FSharp.ViewEngine.Docs` package is produced.

The unified library currently supplies **Primitives**, **Application**, and **Documentation**. Primitives supplies shared controls, themes, actions and sections; Application supplies pages, shells and collection/detail compositions; [Documentation](Documentation/README.md) supplies authoring, navigation and preview mechanics. Marketing and Ecommerce will be added when their reusable components and connected examples are implemented.

## Render a component

```fsharp
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open FSharp.ViewEngine.Components.Application

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

Documentation consumers additionally import `Documentation/Documentation.tailwind.css` from the same package. Hosts using `Browser.withAppMode` or `Phone.withAppMode` additionally import `AppMode.tailwind.css`; the base manifest retains static Browser and Phone styling but excludes the optional viewer. Product pages do not import optional Documentation or App-mode assets unless they use those capabilities. See [Documentation installation and migration](Documentation/README.md).

The manifest contains the renderer-owned utility inventory and semantic CSS variables. Applications may override semantic variables in their own theme class without replacing component markup:

```css
.acme-theme {
  --fve-brand-solid: oklch(58% 0.18 264);
  --fve-brand-hover: oklch(51% 0.2 264);
  --fve-brand-active: oklch(44% 0.18 264);
  --fve-brand-ring: oklch(68% 0.16 264);
}
```

## Control sizes and density

Single-line controls share three rem-based minimum heights: `ControlSize.Small` (32px), `Medium` (40px, the default), and `Large` (48px), at a 16px root font. Text can reflow rather than being clipped. Small is the compact application size with 14px text and a 20px line-height; Medium and Large use 16px text with a 24px line-height. Field values use regular (400) weight and action labels use medium (500); field labels stay at 14px. Navigation and tabs retain their separate UI typography. Application page headers, collection toolbars, section actions, and calendar navigation establish the compact size locally while data-entry forms inherit the surrounding theme size. The connected Page examples select Small for their complete application shells so page actions, filters, forms, and mode controls stay consistent; consumers can retain Medium for form-heavy products or override a local region explicitly.

```fsharp
let theme =
    ComponentsTheme.sky
    |> ComponentsTheme.withDensity Density.Compact
    |> ComponentsTheme.withControlSize ControlSize.Medium

// A local region can override the inherited size without resetting its theme.
div {
    _class (ControlSize.className ControlSize.Small)
    Input.create "query" "Search records"
    |> Input.withType InputType.Search
    |> Input.render
    Button.create "Search" |> Button.asSubmit |> Button.render
}
```

`Button.withSize`, `IconButton.withSize`, `Input.withSize`, and `Select.withSize` explicitly override the region. Controls otherwise inherit its size, including application actions and dropdown triggers. `Density` controls navigation/shell spacing; `Table.withDensity` controls record spacing. Neither changes the control-size selection. To retain dense 32px controls, select `ControlSize.Small` explicitly.

`InputType.Search` uses the shared Input renderer with a decorative magnifying glass and a keyboard-accessible, labelled clear button. The clear button follows the native value (including bound changes and form reset), disappears when empty or unavailable, restores input focus, and dispatches native `input` and `change` events. It clears only that field, not other filters, and does not submit the form automatically. `Input.withVisuallyHiddenLabel` supports compact search bars without losing their accessible name.

## Product frames

`Browser` and `Phone` are Primitives for rendering consumer-owned product HTML in browser and device treatments. `Browser.withAppMode` and `Phone.withAppMode` opt a named frame into the optional expanded viewer; they do not add Documentation dependencies or product behavior. Compose `Documentation.Fixture` only when a documentation or Spec host needs review workflow destinations and alternate states.

```fsharp
open FSharp.ViewEngine.Components.Primitives

let browser =
    Browser.create checkoutScreen
    |> Browser.withAddress "https://shop.example.test/checkout/shipping"
    |> Browser.withAppMode "checkout-shipping" "Shipping address"
    |> Browser.render
```

Hosts that opt into App mode import the packaged `AppMode.tailwind.css` after the base manifest and serve the packaged `app-mode.js` once with `Browser.script`; see the [Browser, Phone, and Fixture guide](Documentation/README.md#browser-phone-and-fixture-app-mode) for the optional CSS/runtime and Fixture composition contract.

## Foundations

Button, IconButton, Badge, Status, LoadingIndicator, and EmptyState share the semantic theme, tone, size, radius, density, light-mode, and dark-mode contracts where applicable. Available Button and IconButton variants provide hover, active, and focus-visible feedback. IconButton and LoadingIndicator require accessible labels. Pending buttons retain their action name, expose busy state, and prevent duplicate activation.

## Data display

Table renders typed consumer-owned rows with a required caption that is visually hidden by default, column and optional row headers, compact or comfortable density, plain or panel surfaces, empty content, and a labelled keyboard-reachable narrow overflow region. Compact density and the plain surface are the defaults; tables fill their page container. Plain table backgrounds, including headers and opaque sticky action cells, match `--fve-page`; hover and selection remain distinct. Explicit panel tables use `--fve-surface` instead. Panel presentation and a visible caption are explicit opt-ins. Compact row padding uses `--fve-table-padding-block-compact` (0.25rem), comfortable uses `--fve-table-padding-block-comfortable` (0.75rem), and horizontal padding uses `--fve-table-padding-inline` (0.75rem). Row controls use `--fve-table-control-size` (1.75rem). `Table.rowActionsColumn` and `RowActions` standardize record actions as a sticky horizontal-ellipsis menu column without a vertical divider. Sticky cells follow the row background. Trigger borders appear with row hover/focus and darken on button hover; keyboard focus remains visible.

Opt into `TableMobileLayout.Records` with `Table.withMobileLayout`, mark exactly one column `Table.asMobilePrimary`, and optionally mark one `Table.asMobileSummary`. Below a 40rem container width the same table tree becomes labelled records; no duplicate links, IDs, selection controls, or menus are created. All other fields remain visible. Names and summaries reclaim the checkbox space when selection is absent; below a 16rem container width, supporting labels stack above their values to accommodate narrow layouts and enlarged text. `Scroll` is the default for financial comparisons that need columns side by side.

`Table.withSelection (TableSelection.create id keyFor labelFor)` adds native row checkboxes and a mixed-state select-all checkbox. Keys must be non-empty and unique. Configure initial keys, disabled rows, and form names with `withSelectedKeys`, `withDisabledRows`, and `withFormName`. Select-all covers only eligible rendered rows. State survives resizing and same-instance morphs; off-page and disabled keys are pruned. The bubbling `fve-table-selection-change` event carries `detail.keys`; checked inputs also participate in native form submission. `BulkActions` consumes those stable keys for page-scoped commands and can clear the owning selection. Applications own commands, authorization, and validation of every submitted key. Cross-page selection is not implicit.

`Table.withRowAttributes` lets applications add bounded presentation behavior such as local filtering without transferring row semantics. `Table.withHierarchy` adds consumer-authored row levels, expanded state, disclosure actions, and aggregate descriptions to the same table tree; consumers remain responsible for computing hierarchy, totals, permissions, and server persistence.

`DescriptionList` and `DetailField` render full-width native `dl`/`dt`/`dd` relationships. Labels use muted uppercase 12px-equivalent text; values use normal-weight 14px-equivalent text. `DescriptionList.withColumns` accepts `One`, `Two` (default), `Three`, or `Four`: all stack on small screens, multi-column layouts use two columns from `sm`, and `Three`/`Four` reach their maximum at `lg`/`xl` respectively. Fields use consistent grid gaps, wrap long content, and add no cards, headings, dividers, or outer padding. The surrounding `Page`/`Section` owns those boundaries. `DetailField.withDescription` adds optional supporting text; status values remain content-sized.

Metric highlights consumer-formatted value content with optional trend text, status content, and description. Pagination presents explicit current/link/gap items through typed consumer destinations without owning page state. Charts are application-owned compositions: use `Section` and `SectionHeader` for layout, native `figure`/`figcaption` markup for the visualization, and an associated visible summary and data table. Compose `EmptyState` when no data exists. The package provides no `Chart` API or drawing runtime.

## Native fields and inline feedback

`Input.create name label` and `Textarea.create name label` render native controls with required labels, optional descriptions and validation, and normal form values. `withId` sets the exact DOM ID; `Input.id` / `Textarea.id` return the focus target for an error summary. Default IDs encode the form name without collapsing punctuation; repeated names require explicit distinct IDs.

Field wrappers top-align their contents in taller form rows: neighboring helper/error text or a textarea must not stretch a label or single-line control. Input, Textarea and Select present label → control → help/error text; help remains associated through `aria-describedby`. File and tag fields also retain their natural internal geometry. Compose columns with normal responsive grids—no spacer descriptions or fixed row heights are needed.

```fsharp
let email =
    Input.create "email" "Email address"
    |> Input.withId "contact-email"
    |> Input.withType InputType.Email
    |> Input.withDescription "For account correspondence."
    |> Input.withAttributes [ _autocomplete "email" ]
    |> Input.required

let notes =
    Textarea.create "notes" "Notes"
    |> Textarea.withRows 4
    |> Textarea.withAttributes [ _maxlength 400 ]
    |> Textarea.render
```

Input types are Text, Email, Telephone, Password, Number, Search, Url, Date, Time, and DateTimeLocal. Use `withAttributes` for native constraints, autocomplete/inputmode, and application-owned Datastar bindings; the component protects its structural and validation attributes. `FileSelection` renders a labelled native file input with accept/multiple/capture constraints and explicit description, validation, disabled, and pending states; applications still own upload transport and file validation. `TagInput` submits one hidden successful control per free-form value and provides named addition/removal, duplicate/empty feedback, and disabled/pending states. `InputType.Search` adds an accessible clear action that restores input focus and dispatches normal input/change events; it is query text, not a selected entity or popup combobox. Signal names used in HTML attribute keys must respect Datastar's casing conventions (lowercase names are simplest).

`Input.withLeadingIcon` accepts a decorative, non-interactive HTML icon; the visible label remains the accessible name. `Input.withPrefix` and `Input.withSuffix` add encoded, non-editable context such as `https://` or `USD`. Prefix/suffix text is associated through `aria-describedby`, independently of help/errors, and is **not** included in the input's submitted value. Adorned controls retain native input behavior and an outer focus-visible outline; consumers still own parsing and validation.

The catalog starts with focused [Input examples](https://fsharpviewengine.meiermade.com/components/input) and meaningful field states. Complete stacked, responsive two-column and sectioned forms are under **Application → Forms → Form layouts** (`/components/form-layouts`), rather than embedded in the Input primitive. The contact-validation endpoint and default field IDs remain supported there. Search with clear behavior stays in the Input gallery; result filtering is demonstrated in **Page examples → Account management**, not Form layouts. Choice controls retain focused submission/validation demonstrations after their basic examples.

`withValue` encodes input attributes or textarea content. `withValidation` associates corrective text through `aria-describedby`; it does not make every field an alert. `required` retains native constraints for editable controls. Disabled fields are omitted from FormData; **pending Input/Textarea values remain submitted**, unlike disabled choice controls. Pending fields prevent editing and are visibly busy. Input and Textarea have no standalone read-only state; use `DescriptionList` / `DetailField` for non-editable information. Applications own validation, submission, state, and whether to disable native constraint checking.

`ErrorSummary.create id title errors` requires at least one `FieldError.create controlId label message`. Links focus the exact control and retain native fragment navigation. `ErrorSummary.focusOnMount` optionally focuses a newly inserted summary after a server rejection; it does not continually steal focus. Render a new summary only when there are errors, and preserve submitted values in the returned controls.

`Notice.create id title content` presents persistent inline feedback, with `withTone` and optional consumer-owned `withActions`. Announcements are independent of tone: Static is the default, Polite adds a status region, and Assertive adds an alert. Actions sit outside the live region. Use record `Status` for field data and Notice for contextual feedback.

`Notification.create id title content` presents floating feedback inside a consumer-owned `NotificationRegion.create id label notifications` stack. It supports tone, announcement policy, meaningful actions, and explicit dismissal. Dismissal hides only that notification and dispatches `fve-notification-dismiss`; applications own the stack, timing, persistence, and server outcome. Notifications never auto-dismiss, so keyboard and assistive-technology users receive the same durable opportunity to act.

## Interaction and state

Components use Datastar signals for ephemeral open, query, focus, and selection presentation. Applications remain responsible for durable state, authorization, validation, routing, and server actions.

Select triggers and popup search inputs use a 2px inset brand outline only for keyboard-visible field focus; their normal background and text do not invert. Popup rows use solid brand fills for active focus, with white light-mode text and a bright brand-text/dark-surface pairing in dark mode. A selected inactive choice retains the subtle accent tint, checkmark and stronger weight; selected-plus-active remains clear through its checkmark/weight and active fill. DropdownMenu, clear/remove/retry actions and table overflow triggers retain their normal hover/pressed colors for pointer interaction and use a 2px inset brand outline for keyboard-visible focus; focus does not invert the trigger or its icon. Floating panels retain surface, radius and elevation shadow without an ordinary border or focus ring. Forced-colors mode restores system-color panel boundaries and focused/active-target outlines. This policy does not remove focus indicators from unrelated controls or borders from gallery frames. Custom themes must preserve text and focus contrast when overriding the existing surface, brand solid/text and ring tokens.

Select, DropdownMenu, Dialog, Checkbox, Switch, ToggleButton, Tabs, and RadioGroup preserve their distinct form and accessibility semantics. Required accessible labels are constructor inputs.

Single-mode Select is a typed select-only combobox: applications provide values, explicit encoding, options, and server validation while the component owns branded listbox presentation, active-descendant focus, disabled options, bounded typeahead, and the canonical closed/open keyboard model. Its listbox uses a native auto popover and CSS anchor positioning, so it enters the top layer, follows the trigger while scrolling, avoids ancestor clipping, and flips at viewport edges. `Select.required`, `Select.disabled`, `Select.pending`, and `Select.withValidation` expose truthful state without introducing a native-select wrapper. Disabled or pending Select values are omitted from ordinary form submission.

`Select.withSearch` adds a labelled `InputType.Search` field inside the Select popup while keeping the visible control a button-triggered Select. Search always includes its native clear action; it remains separate from selected identity. It is single-select by default, with distinct query and selected identity. Static mode filters consumer-supplied typed options locally. Remote mode submits the query signal to an application endpoint and morphs the stable region returned by `Select.renderOptions`; each request explicitly uses Datastar `requestCancellation: 'auto'`, preventing an older in-flight response to the same endpoint from visibly replacing newer results. Its native auto popover is anchored to the Select trigger, remains in the top layer across result morphs, follows scrolling, and flips at viewport edges. Multiple Select adds a Clear selection action at the top of the popup. `Select.loading`, `Select.withError`, `Select.disabled`, `Select.pending`, and `Select.withValidation` expose loading, retryable error, unavailable, busy, and form-validation states without changing the Select trigger's semantics. Disabled and pending values are omitted from ordinary form submission.

### Multiple selection

Use `Select.multiple` to enter an explicit multiple-selection pipeline, then `withSelectedMany` to supply ordered initial values. Single and multiple configs have distinct types: the single-value setter cannot be used on a multiple config, or vice versa. Shared configuration helpers and `render` work in either mode; existing single-mode calls and `SelectConfig<'value>` source annotations remain valid.

```fsharp
let members = [ Select.option "alex" "Alex Morgan"; Select.option "jamie" "Jamie Lee" ]

let selectMembers =
    Select.create "memberIds" "Members" id members
    |> Select.multiple
    |> Select.withSelectedMany [ "alex"; "jamie" ]
    |> Select.render

let searchMembers =
    Select.create "memberIds" "Members" id members
    |> Select.withSearch SelectSearch.Static
    |> Select.multiple
    |> Select.withSelectedMany [ "alex" ]
    |> Select.render
```

- Multiple Select uses a button and a focused multiple listbox. Arrow/Home/End/typeahead move the active option independently of selection; Space/Enter toggle without closing. Escape returns focus; Tab dismisses without selecting. The trigger summarizes selected labels and offers clear-all.
- Multiple searchable Select keeps its query separate from selected items. Enter toggles, Space edits text, and Backspace never removes a selection implicitly. Named remove buttons and clear-all return focus to the query; `clearable` adds a separate **clear search** action that does not clear selections.
- Each selected item contributes one native hidden input with the same form name. Order is selection order; duplicate initial values are deduplicated. Encoded option keys must be unique and every initial selected value needs a supplied option label. Disabled options cannot be newly selected; disabling/pending the entire control omits its values from FormData. Required/validation state is accessible; the host must validate submitted values, permissions and any selection-count policy.
- For remote results, use `Select.withQuery requestedQuery` with `Select.renderOptions`. Matching and empty/error decisions remain server-owned. Each multiple searchable Select uses its own abort controller, cancels obsolete requests immediately, and cancels in-flight work when its input is removed or disabled. Earlier requests cannot reset a later request's loading state. No query is included among the selected form values.
- Selection records and the query are initialized only when missing. Result and same-ID whole-field morphs retain edits, even when chosen labels are absent from the new result set. The small selected-token/hidden-input region is client-managed; the surrounding field, options, disabled state and validation remain morphable. An intentional authoritative reset should patch the instance's `<id>_selected` signal with validated `{value, label}` records (or `[]`); remount with a new stable ID for a different field instance. IDs use the same normalized instance token as the existing choice signals.
- When constructing a remote field on the server, seed `withSelectedMany` from options that include the selected labels **before** `withOptions` narrows the current results. This is initialization, not a label-lookup or form-state service.

Working local examples: `/components/select#components-select-multiple` and `/components/select#components-select-search-multiple`. The remote example includes empty/error/retry and whole-field refresh; validation examples submit ordinary repeated form values without saving data.

### Single-selection example

```fsharp
let accountSelect =
    Select.create "account" "Parent account" string accountOptions
    |> Select.withSearch (SelectSearch.Remote "/accounts/search")
    |> Select.withEmptyMessage "No matching accounts"
    |> Select.render
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

DropdownMenu keeps typed destinations and trusted Datastar actions application-owned while providing labelled groups, separators, leading content, shortcut hints, destructive tone, disabled or pending items, and typed Start/End popup alignment. Its native auto popover places the menu in the top layer. CSS anchor positioning keeps ordinary menus attached while scrolling and flips them at viewport edges; standardized sticky table cells use a fixed top-layer fallback that updates on nested scroll and resize events to avoid a narrow-viewport Chromium compositor defect. `DropdownMenu.asOverflow` supplies a ghost horizontal-ellipsis trigger using the inherited control size for page, section, and row action overflow. `DropdownMenu.withIconTrigger icon` uses the same compact treatment with a consumer-supplied decorative icon and the constructor's accessible label. `MenuItem.radio action label` creates a mutually exclusive choice; use `MenuItem.withChecked` for its initial state and optional `MenuItem.withCheckedExpression` for a trusted Datastar expression. The caller owns the choice state and action (and groups independent choice sets with `MenuItem.group`). Radio items expose `menuitemradio`/`aria-checked` and a checkmark, with the same hover/focus treatment as ordinary menu items. Enabled items support pointer activation, wrapping Arrow/Home/End movement, Enter/Space activation, bounded character navigation, outside/Tab dismissal, Escape focus restoration, isolated stable-ID signals, and server-rendered morph continuity.

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

Dialog, ConfirmationDialog, and Drawer use native modal dialogs and their top-layer backdrop. They require stable IDs and accessible titles, contain focus while open, and restore focus to their connected triggers. Dialog retains consumer-authored body and footer content and can opt into safe backdrop dismissal. ConfirmationDialog focuses the least destructive cancel action first, renders a destructive submit action, exposes server validation and pending state, and uses a Datastar request indicator to prevent duplicate confirmation. Drawer renders consumer-owned detail or form content in a responsive typed Start or End panel. `Drawer.withWidth DrawerWidth.Standard` is the default focused task width; `Wide` supports denser editing without becoming a full-page shell. Headers and optional footers remain fixed while the body scrolls. Drawers dismiss through Escape, their close action, or the backdrop.

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

`FloatingPanel.create id title content` renders persistent, non-modal assistance. Open, minimized, and dismissed states each retain an explicit recovery path; transitions dispatch `fve-floating-panel-state` with the panel ID and new state, then restore focus to the newly available control or heading. Viewport positioning is the default; `FloatingPanel.withinContainer` positions it inside a consumer-owned relative region. The panel has no backdrop and never traps focus. Consumers own persistence and decide whether dismissed guidance should be offered again.

## Operational application patterns

`ChoiceCards` retains native radio or checkbox semantics while adding descriptions, metadata, disabled choices, required validation, and responsive card presentation. `Progress` renders determinate native progress with active, complete, or failed context. `Steps` renders current, completed, available, and unavailable destinations from one typed sequence. `FirstSteps` composes `FloatingPanel` for optional setup guidance, with open, minimized, dismissed, and container-boundary modes that always preserve a recovery path.

`UploadList` presents consumer-owned queued, uploading, complete, failed, and cancelled files with determinate progress and explicit cancel/retry/remove actions. It is presentation only: applications own file bytes, transport, retry policy, validation, and durable state. `Avatar` and `CopyReveal` provide identity fallback and intentionally user-triggered credential reveal/copy behavior.

`Primitives.Calendar` renders four focused views: a Monday-first Month grid, minute-positioned Day and Week timelines with separate lanes for overlapping events, and a Year overview of twelve compact months. Day, Week, and Month reflow to a date-grouped agenda below a 48rem container width; Year stacks its compact months. `Calendar.create label view date events` takes a `DateOnly` anchor, not a display string; ranges are derived from that date. `CalendarEvent.create id title date destination` creates an all-day event; `CalendarEvent.withTime start finish` takes positive same-day `TimeOnly` intervals in whole minutes. Consumers split overnight/multi-day events and resolve time zones before rendering. Calendar does not infer today's date from the server clock: `withToday date destination`, `withSelectedDate` and `withDateDestination` provide explicit consumer-owned date navigation. Previous/next/view destinations remain real links. In Year, only dates containing events become date-destination links, keeping the overview’s keyboard focus order bounded; Day, Week, and Month retain direct event-detail links. Empty, loading, error/retry and unavailable presentation suppress stale event actions. Applications own time zones, recurrence, collision policy, fetching, and route state. Labels currently use invariant English and weeks start on Monday; this bounded renderer is not a localization or scheduling engine. `MediaLibrary` renders native repeated selection values, descriptive images, and stable selection events compatible with `BulkActions`; applications own storage, transformations, save operations, and media authorization.

Each consumer-facing component has a dedicated catalog page for its focused variants and copyable code. App shell teaches layout with minimal content. Reference-backed account management, dependency graph, execution detail, financial reporting, messaging, operations, scheduling and media workflows live under **Page examples**. They compose shared components with contained SVG and visible list/table alternatives rather than introducing universal graph, chart, or messaging engines. Docs-only handlers provide bounded, cookie-isolated temporary state; applications own their actual persistence, authorization, transport and image licensing.

## Navigation and page composition

`Breadcrumbs`, `SideNav`, `PageTopBar`, `PageHeader`, `Page`, `Section`, and `AppShell` preserve a sidebar-oriented ownership boundary:

- `Breadcrumbs` renders a labelled path whose ancestors are typed links and whose final item is the non-linked current page. Deep paths move all ancestors into a `DropdownMenu` on narrow screens.
- `SideNavHeader` accepts arbitrary consumer-owned product-header content. `SideNav` owns grouped or ungrouped typed destinations, optional current state, optional desktop/mobile context, width, and footer/account regions. Unavailable destinations remain truthful non-links.
- `PageTopBar` renders stable shell utility chrome outside the page scroll region. Its arbitrary content may contain breadcrumbs, search, utilities, or custom layout, but never the page `h1`.
- `PageHeader` renders exactly one visible required `h1`, optional subtitle, and typed workflow actions.
- `ActionCluster` permits at most two direct actions and one primary action; additional actions use a separate horizontal-ellipsis menu. Links remain links and commands remain buttons.
- `Page` owns local section navigation or Tabs, the scroll region, semantic `Reading`, `Wide`, or `Full` width, and `Padded` or `FullBleed` body layout. `Wide` is the constrained default; `Reading` and `Full` are explicit. `PageBodyLayout.Canvas` selects full width and fills the remaining height, with scrolling owned by the canvas rather than the document.
- `Collection` and `Detail` accept ordinary toolbar, result, metadata, and section content. They use whitespace between regions and inherit their parent's content boundary, without adding header-only padding or edge-to-edge dividers. A padded `Page` supplies the shared gutter once; standalone hosts should wrap the entire composition in responsive padding (for example `p-4 sm:p-6 lg:p-8`). Tables fill that inset content column, not the outer page. `withVisuallyHiddenTitle` keeps either region labelled beneath the page-owned visible heading without leaving a blank header gap. Detail stays single-column and does not invent cards or a sidebar around supplied sections. Keep statuses in `DetailField.status` values beneath a visible `SectionHeader` (for example, "Detail"), rather than adding status badges to the resource header.
- `SectionHeader` normally renders an `h2`, with optional description, actions, and an opt-in divider. `Section.withoutHeader` accepts an accessible label and content without a visible header. `Section.withLabel` overrides the region's accessible name without changing its visible heading, for example to distinguish a Transactions section from its nested table scroll region. `Section` defaults to plain presentation; panel surfaces are explicit.
- `BottomNavigation` renders a compact labelled list of typed destination links for small screens. Its current state uses `aria-current="page"`; it is navigation, never a tablist.
- `AppShell` owns only the semantic theme, persistent desktop sidebar, accessible mobile navigation overlay, an optional mobile BottomNavigation, one `main` landmark, and the rendered Page slot.

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

let sideNav =
    SideNav.create
        "product-navigation"
        "Primary navigation"
        (SideNavHeader.create "Ledger")
        [ SideNavSection.group "Manage" [
              SideNavItem.create Home "Dashboard"
              SideNavItem.create Accounts "Accounts" ]
          SideNavSection.group "Analyze" [
              SideNavItem.create Reports "Reports" ] ]
    |> SideNav.withCurrent Accounts

let topBar =
    PageTopBar.create ()
    |> PageTopBar.withContent (Breadcrumbs.render destinationUrl breadcrumbs)

let actions =
    ActionCluster.create "account-actions" [
        ApplicationAction.command "@post('/accounts/42/refresh')" "Refresh"
        |> ApplicationAction.withVariant ButtonVariant.Primary ]

let pageHeader =
    PageHeader.create "Account 42"
    |> PageHeader.withSubtitle "Operating account"
    |> PageHeader.withActions actions

let page =
    Page.create pageHeader accountContent
    |> Page.withTopBar topBar
    |> Page.withWidth PageWidth.Full
    |> Page.render destinationUrl

let application =
    AppShell.create "ledger-shell" sideNav page
    |> AppShell.withTheme (ComponentsTheme.sky |> ComponentsTheme.withDensity Density.Compact)
    |> AppShell.withMobileBottomNavigation "ledger-quick-navigation" "Ledger quick navigation" [
        BottomNavigationItem.create Home "Dashboard"
        BottomNavigationItem.create Accounts "Accounts"
        BottomNavigationItem.create Reports "Reports" ]
    |> AppShell.render destinationUrl
```

Desktop and mobile use one SideNav tree, so destination hierarchy, current state, account access, and component IDs cannot drift. `AppShell.withMobileBottomNavigation` accepts a compact, validated subset of those same destinations and derives its current link from SideNav; it is visible only below the configured sidebar breakpoint. On mobile, AppShell focuses the current destination when opened, contains Tab focus, dismisses through Escape or backdrop interaction, and restores the trigger when dismissal stays on the current route. `SideNavHeader` and `PageTopBar` share one shell-bar height token and matching border structure. Applications choose typed navigation width, Medium or Large breakpoint, and Viewport or Container shell boundary while retaining authorization, route state, URL/history policy, product identity, and durable account state.

Standalone Navbar, stacked/top-navigation shells, generic non-navigation sidebars, icon-only collapse, floating or inset variants, right-side navigation, and multi-column shells are intentionally outside the first sidebar-shell contract.

## Documentation

The complete component gallery, typed examples, theming guidance, and application-boundary guidance are published at:

https://fsharpviewengine.meiermade.com/components
