namespace Example.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>tabs</category>
[<RequireQualifiedAccess>]
type TabsVariant = Segmented | Underlined

/// <category>tabs</category>
[<AutoOpen>]
module TabsAttributes =
    type Html with
        static member _variant(value:TabsVariant) = Html._attr("variant", if value = TabsVariant.Segmented then "segmented" else "underlined")

type internal TabsTriggerElement(attributes:HtmlAttribute list, children:HtmlElement list) =
    inherit HtmlElement()
    member _.Attributes = attributes
    member _.Children = children
    override _.Render _ = invalidOp "fveTabsTrigger belongs inside fveTabsItem."

type internal TabsItemElement(value:string, attributes:HtmlAttribute list, trigger:TabsTriggerElement, children:HtmlElement list) =
    inherit HtmlElement()
    member _.Value = value
    member _.Attributes = attributes
    member _.Trigger = trigger
    member _.Children = children
    override _.Render _ = invalidOp "fveTabsItem belongs inside fveTabs."

module internal Tabs =
    let private flag name attributes =
        match ComponentElement.attribute name attributes with
        | Some {Value = ValueNone} -> true
        | Some {Value = ValueSome value} -> value <> "false"
        | None -> false

    let trigger attributes children = TabsTriggerElement(ComponentElement.attributes attributes, children) :> HtmlElement

    let item attributes (children:HtmlElement list) =
        let value = ComponentElement.required "value" attributes
        let triggers, contents =
            children |> List.fold (fun (triggers, contents) child -> match child with :? TabsTriggerElement as trigger -> trigger :: triggers, contents | _ -> triggers, child :: contents) ([], [])
        let trigger = match triggers with [trigger] -> trigger | _ -> invalidArg "children" "Each tab item requires exactly one fveTabsTrigger."
        if String.IsNullOrWhiteSpace(Render.toString (ComponentContent.fragment trigger.Children)) then invalidArg "children" "A tab trigger requires visible content or a named icon."
        TabsItemElement(value, ComponentHtml.safeAttributes ["value"] attributes, trigger, List.rev contents) :> HtmlElement

    let private eventAttribute event expression attributes =
        let name = "data-on:" + event
        match ComponentElement.attribute name attributes with
        | Some attribute -> Html._attr(attribute.Name, expression + "; " + (attribute.Value |> ValueOption.toOption |> Option.defaultValue "void 0"))
        | None -> Html._attr(name, expression)

    let render attributes (children:HtmlElement list) =
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "id" "A stable tabs ID without whitespace is required."
        let label = ComponentElement.value "aria-label" attributes
        let labelledby = ComponentElement.value "aria-labelledby" attributes
        if label |> Option.forall String.IsNullOrWhiteSpace then
            if labelledby |> Option.forall String.IsNullOrWhiteSpace then invalidArg "aria-label" "A tab list needs an accessible name."
        let items = children |> List.choose (function :? TabsItemElement as item -> Some item | :? NoopElement -> None | _ -> invalidArg "children" "Tabs accepts fveTabsItem children.")
        if items.IsEmpty then invalidArg "children" "At least one tab item is required."
        let values = items |> List.map _.Value
        if values.Length <> (values |> List.distinct |> List.length) then invalidArg "value" "Tab item values must be unique."
        let unavailable (item:TabsItemElement) = flag "disabled" item.Trigger.Attributes || flag "readonly" item.Trigger.Attributes || flag "aria-disabled" item.Trigger.Attributes
        let available = items |> List.filter (unavailable >> not)
        if available.IsEmpty then invalidArg "children" "At least one tab trigger must be enabled."
        let selected = ComponentElement.value "value" attributes |> Option.defaultValue available.Head.Value
        if not (available |> List.exists (fun item -> item.Value = selected)) then invalidArg "value" "The selected tab must exist and be enabled."
        let horizontal = ComponentElement.choice "orientation" "horizontal" ["horizontal"; "vertical"] attributes = "horizontal"
        let variant = ComponentElement.choice "variant" "segmented" ["segmented"; "underlined"] attributes
        let signal = $"_tabs_{ComponentHtml.optionToken id}_selected"
        let selectedExpression (item:TabsItemElement) = $"${signal} == {ComponentHtml.javascriptString item.Value}"
        let selectExpression (item:TabsItemElement) = $"${signal} = {ComponentHtml.javascriptString item.Value}"
        let nativeId fallback authored =
            let id = ComponentElement.value "id" authored |> Option.defaultValue fallback
            if String.IsNullOrWhiteSpace id || (id |> Seq.exists Char.IsWhiteSpace) then invalidArg "id" "Native tab and panel IDs must be stable and contain no whitespace."
            id
        let tabId (item:TabsItemElement) = nativeId ($"{id}-tab-{ComponentHtml.optionToken item.Value}") item.Trigger.Attributes
        let panelId (item:TabsItemElement) = nativeId ($"{id}-panel-{ComponentHtml.optionToken item.Value}") item.Attributes
        let availableIds = available |> List.map (fun item -> ComponentHtml.javascriptString item.Value) |> String.concat ", "
        let ensureValidSelection = $"[{availableIds}].includes(${signal}) || (${signal} = {ComponentHtml.javascriptString selected})"
        let listClasses, tabClasses =
            if variant = "segmented" then
                "inline-flex min-w-0 max-w-full items-center gap-1 overflow-x-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-1",
                "min-h-[var(--fve-control-min-height)] shrink-0 rounded-[var(--fve-radius-control)] border-0 bg-transparent px-3 py-[calc(var(--fve-control-padding-block)+0.125rem)] text-sm font-semibold text-[var(--fve-muted-text)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)] aria-selected:bg-[var(--fve-surface)] aria-selected:text-[var(--fve-brand-text)] aria-selected:shadow-sm"
            else
                "flex min-w-0 w-full max-w-full items-center gap-4 overflow-x-auto border-b border-[var(--fve-border)]",
                "min-h-[var(--fve-control-min-height)] shrink-0 border-0 border-b-2 border-transparent bg-transparent px-2 py-[calc(var(--fve-control-padding-block)+0.125rem)] text-sm font-semibold text-[var(--fve-muted-text)] outline-none transition-colors hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)] aria-selected:border-[var(--fve-brand-solid)] aria-selected:text-[var(--fve-brand-text)]"
        let tabList =
            div {
                _id (id + "-list")
                _role "tablist"
                match label with Some label -> _ariaLabel label | None -> ()
                match labelledby with Some labelledby -> _ariaLabelledby labelledby | None -> ()
                _ariaOrientation (if horizontal then "horizontal" else "vertical")
                _class (listClasses + if horizontal then "" else " flex-col !items-stretch")
                for item in items do
                    let initialSelected = item.Value = selected
                    let index = available |> List.tryFindIndex (fun candidate -> candidate.Value = item.Value) |> Option.defaultValue 0
                    let focusAndSelect index =
                        let target = available[index]
                        $"{selectExpression target}, document.getElementById({ComponentHtml.javascriptString (tabId target)})?.focus()"
                    let previous = focusAndSelect ((index - 1 + available.Length) % available.Length)
                    let next = focusAndSelect ((index + 1) % available.Length)
                    let first = focusAndSelect 0
                    let last = focusAndSelect (available.Length - 1)
                    let directionalKeys = if horizontal then $"evt.key == 'ArrowLeft' && (evt.preventDefault(), {previous}); evt.key == 'ArrowRight' && (evt.preventDefault(), {next})" else $"evt.key == 'ArrowUp' && (evt.preventDefault(), {previous}); evt.key == 'ArrowDown' && (evt.preventDefault(), {next})"
                    let defaults = [
                        yield _id (tabId item)
                        yield _type "button"
                        yield _role "tab"
                        yield _ariaControls (panelId item)
                        yield _ariaSelected initialSelected
                        yield _ariaDisabled (unavailable item)
                        yield _disabled (flag "disabled" item.Trigger.Attributes)
                        yield _tabindex (if initialSelected then 0 else -1)
                        yield _dataAttr("aria-selected", $"{selectedExpression item} ? 'true' : 'false'")
                        yield _dataAttr("tabindex", $"{selectedExpression item} ? 0 : -1")
                        if not (unavailable item) then
                            yield eventAttribute "click" (selectExpression item) item.Trigger.Attributes
                            yield eventAttribute "keydown" ($"{directionalKeys}; evt.key == 'Home' && (evt.preventDefault(), {first}); evt.key == 'End' && (evt.preventDefault(), {last})") item.Trigger.Attributes
                        yield _class (tabClasses + " disabled:pointer-events-none disabled:opacity-50" + if horizontal then " whitespace-nowrap" else "") ]
                    let nativeAttributes =
                        ComponentHtml.safeAttributes ["role"; "aria-controls"; "aria-selected"; "aria-disabled"; "disabled"; "readonly"; "tabindex"; "data-on:click"; "data-on:keydown"; "data-attr:aria-selected"; "data-attr:tabindex"] item.Trigger.Attributes
                        |> List.filter (fun attribute -> not (unavailable item) || not (attribute.Name.StartsWith("data-on:click", StringComparison.OrdinalIgnoreCase) || attribute.Name.StartsWith("data-on:keydown", StringComparison.OrdinalIgnoreCase)))
                    ComponentElement.render "button" defaults nativeAttributes item.Trigger.Children
            }
        let panels =
            items |> List.map (fun item ->
                let initialSelected = item.Value = selected
                ComponentElement.render "div"
                    [_id (panelId item); _role "tabpanel"; _ariaLabelledby (tabId item); _tabindex 0; _hidden (not initialSelected); _dataAttr("hidden", $"{selectedExpression item} ? null : true"); _class ((if horizontal then "mt-4 min-w-0 max-w-full" else "min-w-0") + " outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]")]
                    (ComponentHtml.safeAttributes ["role"; "aria-labelledby"; "hidden"; "data-attr:hidden"] item.Attributes) item.Children)
        ComponentElement.render "div"
            [_id id; _class (if horizontal then "min-w-0 w-full max-w-full" else "grid min-w-0 max-w-full gap-4 sm:grid-cols-[auto_minmax(0,1fr)]"); _dataSignals $"{{{signal}: {ComponentHtml.javascriptString selected}}}"; _dataInit ensureValidSelection]
            (ComponentHtml.safeAttributes ["value"; "variant"; "orientation"; "aria-label"; "aria-labelledby"] attributes) (tabList :: panels)

/// <category>tabs</category>
[<AutoOpen>]
module TabsDsl =
    let fveTabs = ComponentElementBuilder(Tabs.render)
    let fveTabsItem = ComponentElementBuilder(Tabs.item)
    let fveTabsTrigger = ComponentElementBuilder(Tabs.trigger)
