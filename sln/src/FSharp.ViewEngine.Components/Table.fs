namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<RequireQualifiedAccess>]
type TableMobileLayout =
    | Scroll
    | Records

[<RequireQualifiedAccess>]
type private MobileCell =
    | Field
    | Primary
    | Summary
    | Actions

[<RequireQualifiedAccess>]
type TableSortDirection =
    | Ascending
    | Descending

[<NoEquality; NoComparison>]
type TableSort =
    private
        { destination:string
          direction:TableSortDirection option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module TableSort =
    let by destination =
        if String.IsNullOrWhiteSpace destination then invalidArg (nameof destination) "A sort destination is required."
        { destination = destination; direction = None; attributes = [] }
    let ascending destination = { by destination with direction = Some TableSortDirection.Ascending }
    let descending destination = { by destination with direction = Some TableSortDirection.Descending }
    /// Adds opt-in interaction attributes while Table retains the destination and accessibility contract.
    let withAttributes (attributes:HtmlAttribute list) (sort:TableSort) = { sort with attributes = attributes }

[<NoEquality; NoComparison>]
type TableColumn<'row> =
    private
        { heading:string
          cell:'row -> HtmlElement
          rowHeader:bool
          headingVisible:bool
          stickyEnd:bool
          alignEnd:bool
          mobile:MobileCell
          sort:TableSort option }

[<RequireQualifiedAccess>]
type TableSurface =
    | Panel
    | Plain

/// Selection is scoped to eligible rendered rows. Keys must be stable and unique.
[<NoEquality; NoComparison>]
type TableSelectionConfig<'row> =
    private
        { id:string
          keyFor:'row -> string
          labelFor:'row -> string
          disabledFor:'row -> bool
          selectedKeys:Set<string>
          formName:string }

[<RequireQualifiedAccess>]
module TableSelection =
    let create id keyFor labelFor =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable selection ID is required."
        { id = id; keyFor = keyFor; labelFor = labelFor; disabledFor = (fun _ -> false); selectedKeys = Set.empty; formName = id }
    let withSelectedKeys keys config = { config with selectedKeys = Set.ofList keys }
    let withDisabledRows predicate config = { config with disabledFor = predicate }
    let withFormName name config =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A selection form name is required."
        { config with formName = name }

/// Hierarchy is consumer-authored: keys, ancestors, levels, aggregates, and eligibility remain application policy.
[<NoEquality; NoComparison>]
type TableHierarchyConfig<'row> =
    private
        { id:string
          keyFor:'row -> string
          labelFor:'row -> string
          ancestorsFor:'row -> string list
          levelFor:'row -> int
          hasChildrenFor:'row -> bool
          expandedKeys:Set<string> }

[<RequireQualifiedAccess>]
module TableHierarchy =
    let create id keyFor labelFor ancestorsFor levelFor hasChildrenFor =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable hierarchy ID is required."
        { id = id; keyFor = keyFor; labelFor = labelFor; ancestorsFor = ancestorsFor; levelFor = levelFor; hasChildrenFor = hasChildrenFor; expandedKeys = Set.empty }
    let withExpandedKeys keys config = { config with expandedKeys = Set.ofList keys }

[<NoEquality; NoComparison>]
type TableConfig<'row> =
    private
        { caption:string
          columns:TableColumn<'row> list
          rows:'row list
          emptyState:HtmlElement
          captionVisible:bool
          density:Density
          surface:TableSurface
          mobileLayout:TableMobileLayout
          selection:TableSelectionConfig<'row> option
          hierarchy:TableHierarchyConfig<'row> option
          rowAttributes:'row -> HtmlAttribute list
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Table =
    let private layoutClasses =
        String.concat " " [
            "@container/fve-table w-full min-w-0 max-w-full [--fve-table-background:var(--fve-page)] data-[surface=panel]:[--fve-table-background:var(--fve-surface)]"
            "[&_.fve-table-grid]:w-full [&_.fve-table-grid]:border-collapse [&_.fve-table-grid_thead]:bg-[var(--fve-table-background)] [&_.fve-table-grid_tr]:border-b [&_.fve-table-grid_tr]:border-[var(--fve-border)]"
            "[&_.fve-table-cell]:whitespace-nowrap [&_.fve-table-cell]:px-[var(--fve-table-padding-inline,0.75rem)] [&_.fve-table-cell]:py-[var(--fve-table-padding-block-compact,0.25rem)] data-[density=comfortable]:[&_.fve-table-cell]:py-[var(--fve-table-padding-block-comfortable,0.75rem)]"
            "[&_.fve-table-sort-control]:inline-flex [&_.fve-table-sort-control]:items-center [&_.fve-table-sort-control]:gap-1 [&_.fve-table-sort-control]:rounded-[var(--fve-radius-control)] [&_.fve-table-sort-control]:text-inherit [&_.fve-table-sort-control]:no-underline [&_.fve-table-sort-control:hover]:text-[var(--fve-text)] [&_.fve-table-sort-control:hover]:underline [&_.fve-table-sort-control:hover]:underline-offset-[0.2em] [&_.fve-table-sort-control:focus-visible]:outline-2 [&_.fve-table-sort-control:focus-visible]:outline-offset-2 [&_.fve-table-sort-control:focus-visible]:outline-[var(--fve-brand-ring)]"
            "[&_.fve-table-row]:[--fve-row-background:var(--fve-table-background)] [&_.fve-table-row]:bg-[var(--fve-row-background)] [&_.fve-table-row:hover]:[--fve-row-background:var(--fve-surface-hover)] [&_.fve-table-row:focus-within]:[--fve-row-background:var(--fve-surface-hover)] [&_.fve-table-row[data-selected=true]]:[--fve-row-background:var(--fve-brand-subtle)] [&_.fve-table-row[data-selected=true]_.fve-table-mobile-label]:text-[var(--fve-text)]"
            "[&_.fve-table-actions]:bg-[var(--fve-row-background,var(--fve-table-background))] [&_.fve-table-actions_button[aria-haspopup=menu]]:size-[var(--fve-table-control-size,1.75rem)] [&_.fve-table-actions_button[aria-haspopup=menu]]:rounded-md [&_.fve-table-actions_button[aria-haspopup=menu]]:border [&_.fve-table-actions_button[aria-haspopup=menu]]:border-transparent [&_.fve-table-row:hover_.fve-table-actions_button[aria-haspopup=menu]]:border-[var(--fve-border)] [&_.fve-table-row:focus-within_.fve-table-actions_button[aria-haspopup=menu]]:border-[var(--fve-border)] [&_.fve-table-actions_button[aria-haspopup=menu]:hover]:border-[var(--fve-muted-text)]"
            "forced-colors:[&_.fve-table-row:focus-within]:outline forced-colors:[&_.fve-table-row:focus-within]:-outline-offset-1 forced-colors:[&_.fve-table-row:focus-within]:outline-[Highlight] forced-colors:[&_.fve-table-actions_[role=menuitem]:focus]:outline-2 forced-colors:[&_.fve-table-actions_[role=menuitem]:focus]:outline-[Highlight]"
            "@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-scroll]:overflow-x-visible"
            "@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:flex @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:flex-wrap @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:items-center @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:gap-x-4 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:gap-y-2 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:border-b @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-sort]:p-3"
            "@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-grid]:block @max-[40rem]/fve-table:[&.fve-table-records_tbody]:block @max-[40rem]/fve-table:[&.fve-table-records_thead]:block @max-[40rem]/fve-table:[&.fve-table-records_thead_tr]:block"
            "@max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:absolute @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:top-0 @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:left-0 @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:size-px @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:m-0 @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:overflow-hidden @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:p-0 @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:[clip-path:inset(50%)] @max-[40rem]/fve-table:[&.fve-table-records_thead_th:not(.fve-table-selection)]:whitespace-nowrap"
            "@max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:flex @max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:w-auto @max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:items-center @max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:gap-2 @max-[40rem]/fve-table:[&.fve-table-records_thead_.fve-table-selection]:px-3"
            "@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-selection-target]:shrink-0 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-selection-target]:mx-[calc((1rem-var(--fve-table-control-size,1.75rem))/2)]"
            "@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row]:grid @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row]:grid-cols-[auto_minmax(0,1fr)_auto] @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row]:gap-x-2 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row]:gap-y-1 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row]:p-3"
            "@max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:min-w-0 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:p-0 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:text-left @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:whitespace-normal @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-cell]:[overflow-wrap:anywhere]"
            "@max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:col-start-1 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:row-start-1 @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:w-auto @max-[40rem]/fve-table:[&.fve-table-records_tbody_.fve-table-selection]:self-center"
            "@max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=primary]]:col-start-2 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=primary]]:row-start-1 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=primary]]:self-center @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=summary]]:col-start-2 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=summary]]:row-start-2"
            "@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row:not(:has(.fve-table-selection))_[data-mobile-cell=primary]]:col-span-2 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row:not(:has(.fve-table-selection))_[data-mobile-cell=primary]]:col-start-1 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row:not(:has(.fve-table-selection))_[data-mobile-cell=summary]]:col-span-2 @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-row:not(:has(.fve-table-selection))_[data-mobile-cell=summary]]:col-start-1"
            "@max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=actions]]:static @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=actions]]:col-start-3 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=actions]]:row-start-1 @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=actions]]:w-auto @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=actions]]:bg-transparent"
            "@max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:col-span-full @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:grid @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:grid-cols-[minmax(6rem,35%)_minmax(0,1fr)] @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:place-items-start @max-[40rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:gap-2"
            "@max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-label]:inline @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-label]:font-normal @max-[40rem]/fve-table:[&.fve-table-records_.fve-table-mobile-label]:text-[var(--fve-muted-text)]"
            "@max-[16rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:grid-cols-1 @max-[16rem]/fve-table:[&.fve-table-records_[data-mobile-cell=field]]:gap-0.5" ]

    let column heading cell =
        if String.IsNullOrWhiteSpace heading then invalidArg (nameof heading) "A column heading is required."
        { heading = heading; cell = cell; rowHeader = false; headingVisible = true; stickyEnd = false; alignEnd = false; mobile = MobileCell.Field; sort = None }

    let rowActionsColumn cell =
        { heading = "Actions"; cell = cell; rowHeader = false; headingVisible = false; stickyEnd = true; alignEnd = true; mobile = MobileCell.Actions; sort = None }

    let alignEnd (column:TableColumn<'row>) = { column with alignEnd = true }
    let asRowHeader (column:TableColumn<'row>) = { column with rowHeader = true }
    let asMobilePrimary (column:TableColumn<'row>) = { column with mobile = MobileCell.Primary }
    let asMobileSummary (column:TableColumn<'row>) = { column with mobile = MobileCell.Summary }
    /// The consumer owns the destination and has already ordered the supplied rows.
    let withSort sort (column:TableColumn<'row>) = { column with sort = Some sort }

    let create caption columns rows =
        if String.IsNullOrWhiteSpace caption then invalidArg (nameof caption) "A table caption is required."
        if List.isEmpty columns then invalidArg (nameof columns) "At least one table column is required."
        { caption = caption; columns = columns; rows = rows
          emptyState = div { _class "p-6 text-center text-sm text-[var(--fve-muted-text)]"; "No records" }
          captionVisible = false; density = Density.Compact; surface = TableSurface.Plain
          mobileLayout = TableMobileLayout.Scroll; selection = None; hierarchy = None; rowAttributes = (fun _ -> []); attributes = [] }

    let withEmptyState emptyState config = { config with emptyState = emptyState }
    let withVisibleCaption config = { config with captionVisible = true }
    let withDensity density config = { config with density = density }
    let withSurface surface config = { config with surface = surface }
    let withMobileLayout layout config = { config with mobileLayout = layout }
    let withSelection selection config = { config with selection = Some selection }
    let withHierarchy hierarchy config = { config with hierarchy = Some hierarchy }
    /// Adds consumer-owned presentation or Datastar attributes to each rendered row without replacing table semantics.
    let withRowAttributes rowAttributes (config:TableConfig<'row>) = { config with rowAttributes = rowAttributes }
    let withAttributes attributes (config:TableConfig<'row>) = { config with attributes = attributes }

    let render (config:TableConfig<'row>) =
        if config.mobileLayout = TableMobileLayout.Records then
            let count role = config.columns |> List.filter (fun column -> column.mobile = role) |> List.length
            if count MobileCell.Primary <> 1 then invalidArg (nameof config) "Mobile records require exactly one primary column."
            if count MobileCell.Summary > 1 || count MobileCell.Actions > 1 then
                invalidArg (nameof config) "Mobile records support at most one summary and one actions column."

        let selectionRows =
            match config.selection with
            | None -> []
            | Some selection ->
                let rows = config.rows |> List.map (fun row -> selection.keyFor row, selection.labelFor row, selection.disabledFor row)
                if rows |> List.exists (fun (key, label, _) -> String.IsNullOrWhiteSpace key || String.IsNullOrWhiteSpace label) then
                    invalidArg (nameof config) "Every selectable row requires a non-empty key and accessible label."
                let keys = rows |> List.map (fun (key, _, _) -> key)
                if keys.Length <> (keys |> Set.ofList |> Set.count) then invalidArg (nameof config) "Selection keys must be unique."
                rows
        let hierarchyRows =
            match config.hierarchy with
            | None -> []
            | Some hierarchy ->
                let rows = config.rows |> List.map (fun row -> hierarchy.keyFor row, hierarchy.labelFor row, hierarchy.ancestorsFor row, hierarchy.levelFor row, hierarchy.hasChildrenFor row)
                let keys = rows |> List.map (fun (key, _, _, _, _) -> key)
                if rows |> List.exists (fun (key, label, _, level, _) -> String.IsNullOrWhiteSpace key || String.IsNullOrWhiteSpace label || level < 0) then
                    invalidArg (nameof config) "Every hierarchical row requires a key, label, and non-negative level."
                if keys.Length <> (keys |> Set.ofList |> Set.count) then invalidArg (nameof config) "Hierarchy keys must be unique."
                if rows |> List.collect (fun (_, _, ancestors, _, _) -> ancestors) |> List.exists (fun ancestor -> not (List.contains ancestor keys)) then
                    invalidArg (nameof config) "Every hierarchy ancestor must reference a rendered row."
                rows
        let hierarchySignal = config.hierarchy |> Option.map (fun hierarchy -> $"_table_{ComponentHtml.optionToken hierarchy.id}_expanded") |> Option.defaultValue ""
        let expanded = "$" + hierarchySignal
        let eligible = selectionRows |> List.choose (fun (key, _, disabled) -> if disabled then None else Some key)
        let eligibleJson = ComponentHtml.javascriptString eligible
        let signal = config.selection |> Option.map (fun selection -> $"_table_{ComponentHtml.optionToken selection.id}_selected") |> Option.defaultValue ""
        let selected = $"${signal}"
        let initial = config.selection |> Option.map (fun selection -> eligible |> List.filter selection.selectedKeys.Contains) |> Option.defaultValue []
        let allSelected = $"({eligible.Length} > 0 && {selected}.length == {eligible.Length})"
        let notify = $"el.closest('[data-fve-table]').dispatchEvent(new CustomEvent('fve-table-selection-change', {{ bubbles: true, detail: {{ keys: Array.from({selected}) }} }}))"
        let selectionControl id accessibleLabel name value checkedValue disabled effect change =
            label {
                _class "fve-table-selection-target inline-flex size-[var(--fve-table-control-size,1.75rem)] cursor-pointer items-center justify-center align-middle"
                input {
                    _id id
                    _type "checkbox"
                    _tabindex 0
                    _ariaLabel accessibleLabel
                    if not (String.IsNullOrEmpty name) then _name name
                    _value value
                    _checked checkedValue
                    _disabled disabled
                    _class "fve-table-checkbox m-0 size-4 cursor-[inherit] appearance-none rounded border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] bg-center bg-[length:100%] checked:border-[var(--fve-brand-solid)] checked:bg-[var(--fve-brand-solid)] checked:bg-[url('data:image/svg+xml,%3Csvg%20xmlns=%27http://www.w3.org/2000/svg%27%20viewBox=%270%200%2016%2016%27%3E%3Cpath%20d=%27m3.5%208%203%203%206-6%27%20fill=%27none%27%20stroke=%27white%27%20stroke-width=%272%27%20stroke-linecap=%27round%27%20stroke-linejoin=%27round%27/%3E%3C/svg%3E')] indeterminate:border-[var(--fve-brand-solid)] indeterminate:bg-[var(--fve-brand-solid)] indeterminate:bg-[url('data:image/svg+xml,%3Csvg%20xmlns=%27http://www.w3.org/2000/svg%27%20viewBox=%270%200%2016%2016%27%3E%3Cpath%20d=%27M4%208h8%27%20stroke=%27white%27%20stroke-width=%272%27%20stroke-linecap=%27round%27/%3E%3C/svg%3E')] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50 forced-colors:appearance-auto forced-colors:bg-none"
                    _dataEffect effect
                    _dataOn ("change", $"{change}; {notify}")
                }
            }
        let cellClass column =
            ComponentHtml.classes [
                "fve-table-cell"
                if column.alignEnd then "text-right"
                if column.rowHeader then "font-medium"
                if column.stickyEnd then "sticky right-0 z-20 w-px fve-table-actions" ]
        let mobileRole = function
            | MobileCell.Primary -> "primary"
            | MobileCell.Summary -> "summary"
            | MobileCell.Actions -> "actions"
            | MobileCell.Field -> "field"
        let sortLabel = function
            | Some TableSortDirection.Ascending -> "ascending"
            | Some TableSortDirection.Descending -> "descending"
            | None -> "unsorted"
        let sortGlyph = function
            | Some TableSortDirection.Ascending -> "↑"
            | Some TableSortDirection.Descending -> "↓"
            | None -> "↕"
        let sortControl (className:string) (column:TableColumn<'row>) =
            match column.sort with
            | None -> text column.heading
            | Some sort ->
                a {
                    for attribute in sort.attributes do
                        if attribute.Name.StartsWith("data-on:", StringComparison.OrdinalIgnoreCase) then attribute
                    _href sort.destination
                    _ariaLabel $"Sort by {column.heading}; currently {sortLabel sort.direction}"
                    _class className
                    span { column.heading }
                    span { _ariaHidden true; _class "text-xs"; sortGlyph sort.direction }
                }
        let sortableColumns = config.columns |> List.filter (fun column -> column.sort.IsSome)
        let sortByLabel = "Sort by"
        div {
            _attr ("data-fve-table", "true")
            _attr ("data-surface", if config.surface = TableSurface.Panel then "panel" else "plain")
            _class (ComponentHtml.classes [ "fve-table"; layoutClasses; if config.mobileLayout = TableMobileLayout.Records then "fve-table-records" ])
            _attr ("data-density", if config.density = Density.Compact then "compact" else "comfortable")
            match config.selection with
            | Some selection ->
                _id selection.id
                _attr ("data-signals__ifmissing", $"{{ {signal}: {ComponentHtml.javascriptString initial} }}")
                // Retain eligible selections across morphs; never select off-page or disabled records.
                _dataEffect $"if ({selected}.some(key => !{eligibleJson}.includes(key))) {{ {selected} = {selected}.filter(key => {eligibleJson}.includes(key)); {notify} }}"
                _dataOn ("fve-selection-clear", $"{selected} = []; {notify}")
            | None -> ()
            match config.hierarchy with
            | Some hierarchy ->
                _dataSignals ("{" + hierarchySignal + ": " + ComponentHtml.javascriptString (Set.toList hierarchy.expandedKeys) + "}")
            | None -> ()
            div {
                _role "region"
                _ariaLabel config.caption
                _tabindex 0
                _class (ComponentHtml.classes [ "fve-table-scroll relative overflow-x-auto bg-[var(--fve-table-background)]"; if config.surface = TableSurface.Panel then "rounded-[var(--fve-radius-panel)] ring-1 ring-[var(--fve-border)]" ])
                if config.mobileLayout = TableMobileLayout.Records && sortableColumns.Length > 0 then
                    div {
                        _role "group"
                        _ariaLabel ($"Sort {config.caption}")
                        _class "fve-table-mobile-sort hidden"
                        span { _class "text-sm font-medium text-[var(--fve-muted-text)]"; sortByLabel }
                        for column in sortableColumns do
                            sortControl "fve-table-sort-control" column
                    }
                if List.isEmpty config.rows then config.emptyState
                else
                    table {
                        _role "table"
                        _ariaLabel config.caption
                        _class "fve-table-grid min-w-full border-collapse text-left text-sm"
                        for attribute in ComponentHtml.safeAttributes [ "class"; "role"; "aria-label"; "aria-labelledby"; "aria-describedby" ] config.attributes do attribute
                        caption {
                            _class (if config.captionVisible then "px-3 py-2 text-left text-sm font-semibold text-[var(--fve-text)]" else "sr-only")
                            config.caption
                        }
                        thead {
                            _role "rowgroup"
                            _class "text-xs font-semibold text-[var(--fve-muted-text)]"
                            tr {
                                _role "row"
                                match config.selection with
                                | Some selection ->
                                    th {
                                        _role "columnheader"
                                        _scope "col"
                                        _class "fve-table-cell fve-table-selection w-px"
                                        selectionControl $"{selection.id}-all" "Select all rows on this page" "" "all" (initial.Length = eligible.Length && eligible.Length > 0) eligible.IsEmpty
                                            $"el.checked = {allSelected}; el.indeterminate = {selected}.length > 0 && !{allSelected}"
                                            $"{selected} = el.checked ? {eligibleJson} : []"
                                        span {
                                            _ariaHidden true
                                            _class "fve-table-mobile-label hidden"
                                            "Select all"
                                        }
                                    }
                                | None -> ()
                                for column in config.columns do
                                    th {
                                        _role "columnheader"
                                        _scope "col"
                                        if column.stickyEnd then _attr ("data-fve-sticky-cell", "true")
                                        match column.sort with
                                        | Some sort when sort.direction.IsSome -> _ariaSort (sortLabel sort.direction)
                                        | _ -> ()
                                        _class (cellClass column)
                                        if column.headingVisible then sortControl "fve-table-sort-control" column else span { _class "sr-only"; column.heading }
                                    }
                            }
                        }
                        tbody {
                            _role "rowgroup"
                            _class "text-[var(--fve-text)]"
                            for index, row in List.indexed config.rows do
                                tr {
                                    _role "row"
                                    _class "fve-table-row"
                                    let protectedRowAttributes =
                                        [ "role"; "class"; "id"; "data-selected"
                                          if Option.isSome config.hierarchy then "data-show" ]
                                    for attribute in ComponentHtml.safeAttributes protectedRowAttributes (config.rowAttributes row) do attribute
                                    match config.hierarchy with
                                    | Some _ ->
                                        let _, _, ancestors, _, _ = hierarchyRows[index]
                                        if not (List.isEmpty ancestors) then
                                            let visible = ancestors |> List.map (fun ancestor -> expanded + ".includes(" + ComponentHtml.javascriptString ancestor + ")") |> String.concat " && "
                                            _dataShow visible
                                    | None -> ()
                                    match config.selection with
                                    | Some selection ->
                                        let key, _, _ = selectionRows[index]
                                        _id $"{selection.id}-row-{ComponentHtml.optionToken key}"
                                        _dataAttr ("data-selected", $"{selected}.includes({ComponentHtml.javascriptString key}) ? 'true' : 'false'")
                                    | None -> ()
                                    match config.selection with
                                    | Some selection ->
                                        let key, label, disabled = selectionRows[index]
                                        let keyJson = ComponentHtml.javascriptString key
                                        td {
                                            _role "cell"
                                            _class "fve-table-cell fve-table-selection w-px"
                                            selectionControl $"{selection.id}-select-{ComponentHtml.optionToken key}" $"Select {label}" selection.formName key (List.contains key initial) disabled
                                                $"el.checked = {selected}.includes({keyJson})"
                                                $"{selected} = el.checked ? [...{selected}.filter(key => key != {keyJson}), {keyJson}] : {selected}.filter(key => key != {keyJson})"
                                        }
                                    | None -> ()
                                    for column in config.columns do
                                        let attributes = [
                                            _role (if column.rowHeader then "rowheader" else "cell")
                                            _class (cellClass column)
                                            _attr ("data-mobile-cell", mobileRole column.mobile)
                                            if column.stickyEnd then _attr ("data-fve-sticky-cell", "true") ]
                                        let content = fragment {
                                            if column.mobile = MobileCell.Field then
                                                span { _ariaHidden true; _class "fve-table-mobile-label hidden"; column.heading }
                                            match config.hierarchy with
                                            | Some _ when column.rowHeader ->
                                                let key, label, _, level, hasChildren = hierarchyRows[index]
                                                span {
                                                    _class "inline-flex items-center gap-2"
                                                    _style ("padding-inline-start:" + string (float level * 1.25) + "rem")
                                                    if hasChildren then
                                                        button {
                                                            _type "button"
                                                            _ariaLabel ("Toggle " + label)
                                                            _dataAttr ("aria-expanded", expanded + ".includes(" + ComponentHtml.javascriptString key + ") ? 'true' : 'false'")
                                                            _dataOn ("click", expanded + " = " + expanded + ".includes(" + ComponentHtml.javascriptString key + ") ? " + expanded + ".filter(key => key != " + ComponentHtml.javascriptString key + ") : [..." + expanded + ", " + ComponentHtml.javascriptString key + "]")
                                                            _class "inline-flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                                                            span { _ariaHidden true; _dataText (expanded + ".includes(" + ComponentHtml.javascriptString key + ") ? '−' : '+'"); "+" }
                                                        }
                                                    else
                                                        span { _ariaHidden true; _class "inline-block size-8 shrink-0" }
                                                    column.cell row
                                                }
                                            | _ -> column.cell row }
                                        if column.rowHeader then
                                            th {
                                                _scope "row"
                                                for attribute in attributes do attribute
                                                content
                                            }
                                        else
                                            td {
                                                for attribute in attributes do attribute
                                                content
                                            }
                                }
                        }
                    }
            }
            if config.selection.IsSome then
                output {
                    _role "status"
                    _ariaLive "polite"
                    _class (ComponentHtml.classes [ "block py-2 text-xs text-[var(--fve-muted-text)]"; if config.surface = TableSurface.Panel then "px-3" ])
                    _dataText $"{selected}.length + ' selected on this page'"
                    $"{initial.Length} selected on this page"
                }
        }
