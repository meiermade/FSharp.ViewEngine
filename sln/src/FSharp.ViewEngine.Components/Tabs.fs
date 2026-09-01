namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<RequireQualifiedAccess>]
type TabsVariant =
    | Segmented
    | Underlined

[<NoEquality; NoComparison>]
type TabItem =
    private
        { id:string
          label:string
          content:HtmlElement }

[<RequireQualifiedAccess>]
module Tab =
    let create id label content =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable tab item ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A tab label is required."
        { id = id; label = label; content = content }

[<NoEquality; NoComparison>]
type TabsConfig =
    private
        { id:string
          label:string
          items:TabItem list
          selectedId:string
          variant:TabsVariant }

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

        { id = id
          label = label
          items = items
          selectedId = items.Head.id
          variant = TabsVariant.Segmented }

    let withSelected selectedId (config:TabsConfig) =
        if String.IsNullOrWhiteSpace selectedId then invalidArg (nameof selectedId) "A selected tab item ID is required."
        if config.items |> List.exists (fun item -> item.id = selectedId) |> not then
            invalidArg (nameof selectedId) $"The selected tab item '{selectedId}' does not exist."
        { config with selectedId = selectedId }

    let withVariant variant (config:TabsConfig) = { config with variant = variant }

    let private itemToken (item:TabItem) = ComponentHtml.optionToken item.id

    let render (config:TabsConfig) =
        let signal = $"_tabs_{ComponentHtml.optionToken config.id}_selected"
        let selectedExpression (item:TabItem) = $"${signal} == {ComponentHtml.javascriptString item.id}"
        let selectExpression (item:TabItem) = $"${signal} = {ComponentHtml.javascriptString item.id}"
        let tabId (item:TabItem) = $"{config.id}-tab-{itemToken item}"
        let panelId (item:TabItem) = $"{config.id}-panel-{itemToken item}"
        let tabIds = config.items |> List.map tabId
        let availableIds = config.items |> List.map (fun item -> ComponentHtml.javascriptString item.id) |> String.concat ", "
        let ensureValidSelection = $"[{availableIds}].includes(${signal}) || (${signal} = {ComponentHtml.javascriptString config.selectedId})"
        let listClasses, tabClasses =
            match config.variant with
            | TabsVariant.Segmented ->
                "inline-flex max-w-full items-center gap-1 overflow-x-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-1",
                "min-h-[var(--fve-control-min-height)] shrink-0 rounded-[var(--fve-radius-control)] border-0 bg-transparent px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-muted-text)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)] aria-selected:bg-[var(--fve-surface)] aria-selected:text-[var(--fve-brand-text)] aria-selected:shadow-sm"
            | TabsVariant.Underlined ->
                "flex max-w-full items-center gap-4 overflow-x-auto border-b border-[var(--fve-border)]",
                "min-h-[var(--fve-control-min-height)] shrink-0 border-0 border-b-2 border-transparent bg-transparent px-2 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-muted-text)] outline-none transition-colors hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)] aria-selected:border-[var(--fve-brand-solid)] aria-selected:text-[var(--fve-brand-text)]"

        div {
            _id config.id
            _class "min-w-0"
            _dataSignals $"{{{signal}: {ComponentHtml.javascriptString config.selectedId}}}"
            _dataInit ensureValidSelection
            div {
                _role "tablist"
                _ariaLabel config.label
                _ariaOrientation "horizontal"
                _class listClasses
                for index, item in config.items |> List.indexed do
                    let previousId = tabIds[(index - 1 + tabIds.Length) % tabIds.Length]
                    let nextId = tabIds[(index + 1) % tabIds.Length]
                    let focusAndSelect targetIndex targetId =
                        let target = config.items[targetIndex]
                        $"{selectExpression target}, document.getElementById({ComponentHtml.javascriptString targetId})?.focus()"
                    let previous = focusAndSelect ((index - 1 + config.items.Length) % config.items.Length) previousId
                    let next = focusAndSelect ((index + 1) % config.items.Length) nextId
                    let first = focusAndSelect 0 tabIds.Head
                    let last = focusAndSelect (config.items.Length - 1) tabIds[tabIds.Length - 1]
                    button {
                        _id (tabId item)
                        _type "button"
                        _role "tab"
                        _ariaControls (panelId item)
                        _ariaSelected (item.id = config.selectedId)
                        _tabindex (if item.id = config.selectedId then 0 else -1)
                        _dataAttr ("aria-selected", $"{selectedExpression item} ? 'true' : 'false'")
                        _dataAttr ("tabindex", $"{selectedExpression item} ? 0 : -1")
                        _dataOn ("click", selectExpression item)
                        _dataOn ("keydown", $"evt.key == 'ArrowLeft' && (evt.preventDefault(), {previous}); evt.key == 'ArrowRight' && (evt.preventDefault(), {next}); evt.key == 'Home' && (evt.preventDefault(), {first}); evt.key == 'End' && (evt.preventDefault(), {last})")
                        _class tabClasses
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
                    _class "mt-4 outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]"
                    item.content
                }
        }
