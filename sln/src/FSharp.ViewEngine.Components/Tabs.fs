namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>tabs</category>
[<RequireQualifiedAccess>]
type TabsVariant =
    | Segmented
    | Underlined

/// <category>tabs</category>
[<NoEquality; NoComparison>]
type TabItem =
    private
        { id:string
          label:string
          content:HtmlElement
          leading:HtmlElement option
          disabled:bool }

/// <category>tabs</category>
[<RequireQualifiedAccess>]
module TabItem =
    let create id label content =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable tab item ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A tab label is required."
        { id = id; label = label; content = content; leading = None; disabled = false }

    let withLeading leading (item:TabItem) = { item with leading = Some leading }
    let disabled (item:TabItem) = { item with disabled = true }

/// <category>tabs</category>
[<RequireQualifiedAccess>]
type TabsOrientation =
    | Horizontal
    | Vertical

/// <category>tabs</category>
[<NoEquality; NoComparison>]
type TabsConfig =
    private
        { id:string
          label:string
          items:TabItem list
          selectedId:string
          variant:TabsVariant
          orientation:TabsOrientation }

/// <category>tabs</category>
[<RequireQualifiedAccess>]
module Tabs =
    let create id label (items:TabItem list) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable tabs ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible tabs label is required."
        if List.isEmpty items then invalidArg (nameof items) "At least one tab item is required."

        let duplicateIds =
            items
            |> List.countBy _.id
            |> List.choose (fun (itemId, count) -> if count > 1 then Some itemId else None)

        if duplicateIds.IsEmpty |> not then
            let duplicateIdList = String.concat ", " duplicateIds
            invalidArg (nameof items) $"Tab item IDs must be unique: {duplicateIdList}."

        let available = items |> List.filter (fun item -> not item.disabled)
        if List.isEmpty available then invalidArg (nameof items) "At least one tab item must be enabled."

        { id = id
          label = label
          items = items
          selectedId = available.Head.id
          variant = TabsVariant.Segmented
          orientation = TabsOrientation.Horizontal }

    let withSelected selectedId (config:TabsConfig) =
        if String.IsNullOrWhiteSpace selectedId then invalidArg (nameof selectedId) "A selected tab item ID is required."
        if config.items |> List.exists (fun item -> item.id = selectedId && not item.disabled) |> not then
            invalidArg (nameof selectedId) $"The selected tab item '{selectedId}' does not exist or is disabled."
        { config with selectedId = selectedId }

    let withVariant variant (config:TabsConfig) = { config with variant = variant }
    let withOrientation orientation (config:TabsConfig) = { config with orientation = orientation }

    let private itemToken (item:TabItem) = ComponentHtml.optionToken item.id

    let render (config:TabsConfig) =
        let signal = $"_tabs_{ComponentHtml.optionToken config.id}_selected"
        let selectedExpression (item:TabItem) = $"${signal} == {ComponentHtml.javascriptString item.id}"
        let selectExpression (item:TabItem) = $"${signal} = {ComponentHtml.javascriptString item.id}"
        let tabId (item:TabItem) = $"{config.id}-tab-{itemToken item}"
        let panelId (item:TabItem) = $"{config.id}-panel-{itemToken item}"
        let availableItems = config.items |> List.filter (fun item -> not item.disabled)
        let availableTabIds = availableItems |> List.map tabId
        let availableIds = availableItems |> List.map (fun item -> ComponentHtml.javascriptString item.id) |> String.concat ", "
        let ensureValidSelection = $"[{availableIds}].includes(${signal}) || (${signal} = {ComponentHtml.javascriptString config.selectedId})"
        let listClasses, tabClasses =
            match config.variant with
            | TabsVariant.Segmented ->
                "inline-flex max-w-full items-center gap-1 overflow-x-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-1",
                "min-h-[var(--fve-control-min-height)] shrink-0 rounded-[var(--fve-radius-control)] border-0 bg-transparent px-3 py-[calc(var(--fve-control-padding-block)+0.125rem)] text-sm font-semibold text-[var(--fve-muted-text)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)] aria-selected:bg-[var(--fve-surface)] aria-selected:text-[var(--fve-brand-text)] aria-selected:shadow-sm"
            | TabsVariant.Underlined ->
                "flex max-w-full items-center gap-4 overflow-x-auto border-b border-[var(--fve-border)]",
                "min-h-[var(--fve-control-min-height)] shrink-0 border-0 border-b-2 border-transparent bg-transparent px-2 py-[calc(var(--fve-control-padding-block)+0.125rem)] text-sm font-semibold text-[var(--fve-muted-text)] outline-none transition-colors hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)] aria-selected:border-[var(--fve-brand-solid)] aria-selected:text-[var(--fve-brand-text)]"

        div {
            _id config.id
            _class (match config.orientation with TabsOrientation.Horizontal -> "min-w-0" | TabsOrientation.Vertical -> "grid min-w-0 gap-4 sm:grid-cols-[auto_1fr]")
            _dataSignals $"{{{signal}: {ComponentHtml.javascriptString config.selectedId}}}"
            _dataInit ensureValidSelection
            div {
                _role "tablist"
                _ariaLabel config.label
                _ariaOrientation (match config.orientation with TabsOrientation.Horizontal -> "horizontal" | TabsOrientation.Vertical -> "vertical")
                _class (listClasses + if config.orientation = TabsOrientation.Vertical then " flex-col !items-stretch" else "")
                for item in config.items do
                    let availableIndex = availableItems |> List.tryFindIndex (fun candidate -> candidate.id = item.id) |> Option.defaultValue 0
                    let previousIndex = (availableIndex - 1 + availableItems.Length) % availableItems.Length
                    let nextIndex = (availableIndex + 1) % availableItems.Length
                    let focusAndSelect targetIndex targetId =
                        let target = availableItems[targetIndex]
                        $"{selectExpression target}, document.getElementById({ComponentHtml.javascriptString targetId})?.focus()"
                    let previous = focusAndSelect previousIndex availableTabIds[previousIndex]
                    let next = focusAndSelect nextIndex availableTabIds[nextIndex]
                    let first = focusAndSelect 0 availableTabIds.Head
                    let last = focusAndSelect (availableItems.Length - 1) availableTabIds[availableTabIds.Length - 1]
                    let directionalKeys =
                        match config.orientation with
                        | TabsOrientation.Horizontal -> $"evt.key == 'ArrowLeft' && (evt.preventDefault(), {previous}); evt.key == 'ArrowRight' && (evt.preventDefault(), {next})"
                        | TabsOrientation.Vertical -> $"evt.key == 'ArrowUp' && (evt.preventDefault(), {previous}); evt.key == 'ArrowDown' && (evt.preventDefault(), {next})"
                    button {
                        _id (tabId item)
                        _type "button"
                        _role "tab"
                        _ariaControls (panelId item)
                        _ariaSelected (item.id = config.selectedId)
                        _ariaDisabled item.disabled
                        _disabled item.disabled
                        _tabindex (if item.id = config.selectedId && not item.disabled then 0 else -1)
                        _dataAttr ("aria-selected", $"{selectedExpression item} ? 'true' : 'false'")
                        _dataAttr ("tabindex", $"{selectedExpression item} ? 0 : -1")
                        if not item.disabled then
                            _dataOn ("click", selectExpression item)
                            _dataOn ("keydown", $"{directionalKeys}; evt.key == 'Home' && (evt.preventDefault(), {first}); evt.key == 'End' && (evt.preventDefault(), {last})")
                        _class (tabClasses + " disabled:pointer-events-none disabled:opacity-50")
                        match item.leading with
                        | Some leading -> span { _ariaHidden true; _class "mr-2 inline-flex size-4 items-center justify-center align-text-bottom"; leading }
                        | None -> ()
                        item.label
                    }
            }
            for item in config.items do
                let selected = item.id = config.selectedId
                div {
                    _id (panelId item)
                    _role "tabpanel"
                    _ariaLabelledby (tabId item)
                    _tabindex 0
                    _hidden (not selected)
                    _dataAttr ("hidden", $"{selectedExpression item} ? null : true")
                    _class (match config.orientation with TabsOrientation.Horizontal -> "mt-4 outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]" | TabsOrientation.Vertical -> "min-w-0 outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]")
                    item.content
                }
        }
