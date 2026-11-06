# Migrating installed source

Run `dotnet fve diff --config ...` before updating. Merge consumer edits deliberately, then use `dotnet fve add ... --overwrite` to refresh selected source and dependencies. The CLI does not silently regenerate installed files or retain retired configuration APIs.

## One authoring model

Every public component uses an argument-free `fveComponent { ... }` expression. Replace component `create`/`with...`/`render` pipelines and styling custom operations with ordinary typed HTML attributes and semantic child parts. Supporting data constructors, such as CalendarEvent, remain on their owning component page.

```fsharp
fveCard {
    _size Size.Small
    _class "max-w-lg"
    fveCardHeader { h2 { "Review" } }
    fveBadge { _color Color.Success; "Ready" }
    fveCardFooter { fveLink { _href "/components/card"; "View details" } }
}
```

Attributes, strings, native elements, conditions, loops and `yield!` compose normally. Scalar attributes are last-wins; classes and styles compose. A builder does not imply a custom element: native layouts and controls remain native.

Use qualified, component-specific variants: `ButtonVariant`, `LinkVariant`, `BadgeVariant`, `TabsVariant`, `ToggleButtonVariant`, and `ToggleGroupVariant`. There is no shared `Variant`. Shared `Color`, `Size`, `Density` and `Orientation` retain their native HTML wire values. Native overloads such as `_size 20` remain available. `ControlSize` becomes `Size`.

## Editable runtime delivery

The application owns its entry, bundling and script inclusion. Import the editable JavaScript modules listed on each component's Installation section, including its transitive dependencies. Behavior-owning components now use Rocket setup/cleanup with private instance state, including small native enhancements and shared scrolling. Presentation-only components remain ordinary HTML. Light-DOM owners preserve native controls/content and caller-owned Datastar bindings; a caller binding does not become private widget state. Morphing an owner does not reinitialize unchanged caller bindings or cancel their active requests. All owners share one pinned Rocket/Datastar runtime; do not load another ordinary Datastar bundle. Scan installed F# and component JavaScript with Tailwind; exclude the vendored ECharts bundle from class discovery.

Use the shared [Installation guide](https://fve.meiermade.com/components/installation) for the Tailwind v4/Typography and esbuild build. Chart and ECharts are separate lazy imports, not part of the basic control bundle. Licenses, notices and pinned asset provenance are installed with their dependencies.

## Button and form state

`fveButton` emits only its thin host. Rocket owns the native button, presentation, spinner, form submitter and lifecycle. Use `_color`, `_variant`, `_size`, native attributes and ordinary content. `Size.ExtraSmall`, `Small`, `Medium`, `Large` and `ExtraLarge` use the shared 24/28/32/36/40px control scale; Medium is the 32px application default. Regions previously using Small for a 32px baseline should use Medium. Extra small uses 12px text; the other steps keep 14px text while padding changes. Use `_iconOnly true` and an accessible name for square icon buttons instead of `aspect-square px-0`; icon-only geometry omits text-button padding. Custom colors use `_color Color.Custom` with `_palette typedPalette`.

Keep Datastar text/content bindings inside Button content rather than on its host. `_ariaBusy` controls busy presentation independently of `_disabled`. A Button-owned `_dataIndicator` supplies default request busy/disabled bindings; forms can own their indicator and bind submitters explicitly. Use an ordinary native submit button for a no-JavaScript path.

Across controls, `_readonly` prevents edits while preserving successful values, `_disabled` omits values, and `_ariaBusy` alone does not disable editing. Native labels, descriptions, errors, names and domain-to-string conversion belong to the caller.

## Fields and choices

Field is layout, not a control callback or HTML-tree rewriter. Input, Textarea, InputGroup, Select, TagInput, CopyReveal and DatePicker do not add a Field label/help/error wrapper.

Input always renders one native input, including `InputType.Search`; it does not add an icon, frame or clear button. Replace `_prefix`, `_suffix` and `fveInputIcon` with ordinary `fveInputGroupAddon` content around `fveInput`. Connect meaningful addon text explicitly with `_ariaDescribedby`. `InputGroupPosition` uses `InlineStart`, `InlineEnd`, `BlockStart` and `BlockEnd`; replace `fveInputGroupFooter` with a `BlockEnd` addon. The control precedes addons in DOM order, while logical positions determine their visual placement. Reuse ordinary Input, Textarea and Button inside a group. Native constraints receive `:user-invalid` styling after interaction/submission; `_ariaInvalid` remains available for application/server errors. Read-only and busy are independent, not a combined “pending” Input variant.

```fsharp
fveField {
    fveLabel { _for "email"; "Email" }
    fveInput { _id "email"; _name "email"; _type InputType.Email; _ariaDescribedby "email-help" }
    fveFieldDescription { _id "email-help"; "Used for notifications." }
}
```

FileSelection combines a native file input with built-in contextual clearing, without an automatic filename list or permanent empty-state action. Remove FileSelection's `_clearable` option: Clear is always available after selection, including on required fields, and hidden while empty or unavailable for editing. It identifies the field/selected file accessibly, clears native files, emits input/change and restores chooser focus. The native chooser displays the filename or file count. Keep richer lists, per-file removal and upload queues in application composition. `accept` guides the picker rather than validating content; check selections on change and validate the actual upload on the server.

Use `fveFieldError` and `_ariaInvalid` for validation. FieldGroup is a native fieldset with `fveFieldLegend`; use ordinary `for`/`id` and ARIA relationships. Field supports `FieldLayout.Vertical`, `Horizontal` and `Responsive`.

Checkbox, Switch and Radio are native control-only builders. Supply `_name`, string `_value`, `_checked`, and external labels. `_indeterminate` initializes Checkbox's mixed property. RadioGroup owns the fieldset; the browser owns radio exclusivity and arrows. ChoiceCards is layout around caller-authored native Radio/Checkbox controls, not a generic option/configuration API.

ToggleButton uses `_ariaPressed`. ToggleGroup/MultiToggleGroup use string `_value`/`_values` and one `fveToggleGroupItem` source. TagInput uses `_values` and its editable JavaScript module; local edits, reset, authoritative server content and reconnect cleanup stay component-owned. CopyReveal reads the current native input value and derives action names from its actual label.

## Select and MultiSelect

Remove separate option lists, generic domain configuration and encoder operations. Use `fveSelectContent` with `fveSelectItem` children. Plain text supplies an item's label; rich markup supplies `_label` explicitly. Roots use string `_value` or `_values`. Supply `fveSelectSelectedItem` labels for selected values absent from remote results.

Remote content remains independently renderable, using `_query`, `_loading`, `_error`, `_retry`, `_emptyMessage` and `_loadingMessage`. Search, cancellation, successful native values, focus and server patch behavior remain. Selection/query state is private: submit native form values rather than reading generated page signals. Remote requests explicitly project the requesting instance's existing `<token>_query`, `<token>_value` and `<token>_selected` payload; they do not include unrelated page or component state. For an application-requested refresh, dispatch `fve-select-refresh` on the owning host with an optional `detail.endpoint`. `fve-select-change` exposes `detail.value`, `detail.values` and labelled `detail.selected` entries. Select installs its own module and shared runtime without requiring Button. Plain Input, Textarea and InputGroup need no runtime for their native behavior; applications opt into Datastar bindings and actions explicitly.

## Navigation and overlays

SideNav uses Header/Compact, Content, Context/MobileContext, Footer/CompactFooter, Item/Group/Section/Row and decoration parts. Items own native `_href`; `_value` is the current href. Use `_width SideNavWidth`, `_persistenceKey`, group `_expanded`, and optional `_expandedSignal`. If `open type Svg` shadows `_width`, qualify `Html._width`. BottomNavigation follows the same native href/current-value boundary. Catalog-only AppShell accepts rendered navigation; it no longer takes a destination resolver. Native disclosures, persistence and scroll restoration belong to SideNav's Rocket owner. `_expandedSignal` remains an explicit adapter to application-owned expansion state and does not opt that state into component persistence.

DropdownMenu uses Trigger/Item/Group/Separator and item content parts. Use native href, action, role and checked attributes. Visible triggers provide their accessible name; icon-only triggers require one. Breadcrumb items own hrefs. Tooltip uses `_id`, `_description`, `_side` and `_delay`; the caller explicitly associates its focusable control with `<id>-content`. Appearance switching is a guide pattern, not a ThemeSwitcher component. Install color-mode support and initialize ColorMode.assets once before the stylesheet with an explicit product-owned localStorage key. Compose ordinary menus, radios or segmented choices that call window.fveColorMode.set and synchronize through fve-color-mode; see the [Theming and appearance guide](https://fve.meiermade.com/components/theming).

DropdownMenu, Popover, Dialog and Drawer triggers compose one supplied control; they do not create another button around its content. Put Button presentation, naming and states on the child:

```fsharp
fveDropdownMenuTrigger {
    fveButton {
        _variant ButtonVariant.Outline
        "Open menu"
    }
}
```

For a native button or custom renderer, forward the supplied attributes explicitly. Put trigger identity, states and caller event bindings on the part; presentation can live on the part or inside the renderer. Forward the supplied ID, type, relationships and behavior unchanged. The actual native control owns focus and popup anchoring. Caller click handlers run before the trigger behavior and can cancel it with `evt.preventDefault()`.

```fsharp
fveDropdownMenuTrigger {
    _class "rounded-full border px-3 py-2"
    fun (attributes: HtmlAttribute list) ->
        button {
            for attribute in attributes do
                attribute
            span { "Custom content" }
        }
}
```

Replace trigger `_iconOnly`, `_fullRow` and `_unstyled` presets with caller-owned control classes. Use a native renderer when the trigger must work without JavaScript; `fveButton` remains a thin Rocket-owned host.

Tabs use string `_value` and Item/Trigger parts. Resizable uses native Panel/Handle parts and typed size bounds. Popover uses Trigger/Content parts; its actual trigger supplies the popup name. Dialog/Drawer use `_id`, `_label`, optional focus/container attributes, Footer and native Trigger/Close parts with `_ariaControls`. They restore the actual opener. FloatingPanel uses `_state`, `_boundary` and Footer. Command/CommandDialog use Group/Item parts, native actions/hrefs and explicit labels for rich searchable content; `Command.triggerAttributes "id"` remains a native-button helper.

## Calendars and DatePicker

Day/Week/Month/Year builders take `_label`, typed `_date`, and `CalendarEvent<string>` values. Resolve event URLs in application code. Previous/Next/Today and CalendarDate parts own native hrefs. `Calendar.dates` and `Calendar.compactMonthDates` expose dates for application URL mapping instead of a destination callback. Use `_selectedDate`, `_state CalendarState`, Month's `_layout`, and Empty/StateAction parts where needed.

`fveMonthCalendarSelection` owns form selection on the Month calendar page. DatePicker composes the same control with Popover. Use canonical ISO strings in `_value`/`_endValue`, `_mode CalendarSelectionMode.Range`, and `_rangeEndName` for ranges. Bounds use native `_min`/`_max`; `_displayMonth`, `_weekStart`, `_captionLayout`, `_lang`, `_unavailableDates`, and `_clearable` configure presentation and eligibility. DatePicker's public `_id` belongs to the labelable native button. Native labels and feedback remain outside the control. Busy, readonly and disabled are distinct; uncanceled form reset restores initial dates and month. Calendar selection emits `fve-calendar-change` with ISO `detail.start`/`detail.end`; DatePicker observes that event and native values instead of constructing the calendar's private signal names. Today is subtly highlighted by default with `aria-current="date"`, independently of selection and footer actions; selection/range fills retain precedence. `_currentDate (date: DateOnly)` overrides the local rendering date without adding a shortcut. `_today (date: DateOnly)` supplies the same current-day identity and independently opts into a Today shortcut; supply the application's current date in its own timezone. It selects today (today–today for ranges), moves to that month and follows the normal completed-selection closing policy. It respects bounds/unavailable dates and cannot edit readonly/disabled values. The optional footer action is labeled **Clear** for both single dates and ranges. Clear remains at the footer's start, with Today at its end. DatePicker removes the outer Popover inset so the calendar alone owns its 12px padding.

## Native attributes and local identity

Component-only presentation attributes are consumed by native renderers instead of appearing on their HTML elements; ordinary HTML, ARIA, data attributes and caller styles remain available. Readonly Checkbox, Switch, Radio and FileSelection controls keep their persistent capture guards independently of caller event bindings, including once-only callbacks. DatePicker uses border-inclusive shared sizing, and CopyReveal icon actions use the square Button geometry.

Rocket state is opaque and per instance; generated page-global signal names are not a component API. Native public IDs, labels, names and successful values remain unchanged. Remote Select transport keys retain readable conventional tokens and a disjoint lossless encoding for other IDs.

## Breadcrumbs

Breadcrumbs shows the full trail whenever it fits its actual container. `_maxVisibleItems` is retired: collapse older middle ancestors only when needed, then the root if necessary, and truncate the current label last. Hidden destinations remain in the existing Dropdown menu; restoring ancestors preserves focus. Resizing, fonts and server morphs trigger local remeasurement. Without JavaScript, all ancestor links remain available through local horizontal scrolling.

## Table and Chart

Table's record/column/selection/hierarchy configuration APIs are removed. Compose native Row/Head/Cell parts inside Header/Body/Footer/Caption/Empty. Use `_ariaLabel`, `_surface Surface`, `_mobileLayout MobileLayout`, `_mobile Mobile` and `_scrollRows`. Sorting is native `aria-sort` plus an application-owned link; selection, hierarchy, row keys, queries and loading policy belong to the application. Rows remain reusable native HTML for Datastar patches. Infinite scrolling replaces only the loading row through the Datastar .NET SDK. Applications can render an ordinary native `noscript` view alongside the enhanced host.

`fveChart` takes `_id`, `_ariaLabel` and `_options` with JSON-compatible ECharts options built from F# records or anonymous records. `ChartModel`, `ChartSeries`, `ChartPoint`, `ChartRange`, `_model` and `_range` are retired: the host no longer prescribes dated line charts. Choose `series.type` and axes, or supply Sankey nodes/links and other ECharts structures directly. `_events [ "click"; "brushend" ]` forwards original payloads through bubbling `chart-click` and `chart-brushend` events. `chart-render` supplies `evt.detail.chart` after each options/theme update for application-owned JavaScript formatters and actions; `chart-error` supplies rendering failures. `_empty true` explicitly selects the empty state. `--fve-chart-height` controls height. The financial example owns its UTC bounds, currency formatting, stepped balances, settled brush normalization and server filtering.

## Remaining component families

Skeleton is class-first: `fveSkeleton { _class "h-4 w-64 rounded-full" }`. Dimensions and shape belong to caller utilities, not shape presets or default inline sizes. Optional `_width`/`_height` CSS lengths become explicit inline overrides; an omitted axis remains class-controlled. `fveSkeletonRegion` owns the single accessible loading status around the placeholder composition.

Native layout parts replace configuration pipelines for PageTopBar, PageHeader, SectionHeader, DescriptionList, Card, Browser and Phone. Focused feedback builders cover Callout, EmptyState, Metric, Notice, Notification/NotificationRegion, Message, ErrorSummary, Progress, LoadingIndicator and FileSelection. Use `_announcement` for live-region policy and explicit fragment destinations for errors. Parts stay on their owning page rather than becoming selectors.

CodeBlock, Mermaid and ExampleCode accept literal text, not HTML children, preserving source whitespace and encoding once. Example uses `_name` for inner relationships and a separate optional native `_id`. FSharpApiReference yields `System.Type` values; `forCategory` returns the owning type list. Their asset helpers remain explicit application setup. CodeBlock and Mermaid own rendering, source updates, copy feedback, pending/error state and stale-result cleanup through their editable modules; Prism/Mermaid loading remains shared and lazy. Retired `window.renderCode`, `window.renderMermaid` and `window.fsharpDocsCopy` hooks are not compatibility shims. Update a diagram's source attribute for normal rendering, or dispatch `fve-mermaid-render` for an explicit retry. Example owns its tabs/preview resizing and emits `fve-example-preview` with `detail.panel` for application-owned preview initialization, such as the catalog's iframe loading.

## Retired selectors and aggregates

| Previous boundary | Replacement |
| --- | --- |
| `text-field` | `input`, `textarea`, `error-summary` |
| `identity` | `avatar`, `copy-reveal` |
| `tags` | `tag-input` |
| `item`, `fveItem` and its parts, `ItemVariant` | Application-owned list rows using native `ul`/`li`, links, content and actions |
| `steps`, `fveSteps`, `fveStep`, `StepState` | Application-owned `nav`/`ol` markup, destinations and workflow state, with `aria-current="step"` where appropriate |
| Drawer bundled with Dialog | Install `drawer` and `dialog` separately |
| `upload-list` | Application-owned queue/transport with native HTML and installed controls |
| `first-steps` | Application-owned checklist inside FloatingPanel |
| `bulk-actions` | Native checkbox values and application-owned selection commands |
| Page/Section/Collection/Detail/AppShell and Documentation aggregates | Application layouts composed from independently installed components |

The [Examples gallery](https://fve.meiermade.com/examples) contains exactly Specification, Application and API documentation templates. Each is an independent consumer project, not another installable framework. Repository-only catalog helpers are excluded from the CLI registry.
