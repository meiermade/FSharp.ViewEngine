namespace FSharp.ViewEngine.Components

open System
open System.Text.Json
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>toggle-group</category>
[<RequireQualifiedAccess>]
type ToggleGroupVariant = Connected | Spaced

/// <category>toggle-group</category>
[<AutoOpen>]
module ToggleGroupAttributes =
    type Html with
        static member _variant(value:ToggleGroupVariant) = Html._attr("variant", if value = ToggleGroupVariant.Connected then "connected" else "spaced")
        static member _allowEmpty(value:bool) = Html._attr("allow-empty", if value then "true" else "false")

type internal ToggleGroupItem(attributes:HtmlAttribute list, children:HtmlElement list, choice:SelectedChoice) =
    inherit HtmlElement()
    member _.Attributes = attributes
    member _.Children = children
    member _.Choice = choice
    override _.Render(_) = invalidOp "Render fveToggleGroupItem inside fveToggleGroup or fveMultiToggleGroup."

module internal ToggleGroup =
    let private flag name attributes =
        ComponentElement.attribute name attributes |> Option.exists (fun attribute -> attribute.Value <> ValueSome "false")

    let item (attributes:HtmlAttribute list) (children:HtmlElement list) =
        let value = ComponentElement.value "value" attributes |> Option.defaultWith (fun () -> invalidArg "attributes" "A toggle item requires a string value.")
        if isNull value then invalidArg "attributes" "A toggle value cannot be null."
        let label =
            match ComponentElement.value "label" attributes with
            | Some label when not (String.IsNullOrWhiteSpace label) -> label
            | _ ->
                if children |> List.exists (function :? TextElement | :? NoopElement -> false | _ -> true) then
                    invalidArg "attributes" "Rich toggle items require a plain label attribute."
                System.Net.WebUtility.HtmlDecode(Render.toString (ComponentContent.fragment children))
        if String.IsNullOrWhiteSpace label then invalidArg "children" "A toggle item requires text or a label."
        ToggleGroupItem(attributes, children, { value = value; label = label }) :> HtmlElement

    let root multiple attributes (children:HtmlElement list) =
        let attributes = ComponentElement.attributes attributes
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "attributes" "A stable toggle-group ID without whitespace is required."
        let name = ComponentElement.required "name" attributes
        if ComponentElement.value "aria-label" attributes |> Option.orElseWith (fun () -> ComponentElement.value "aria-labelledby" attributes) |> Option.forall String.IsNullOrWhiteSpace then
            invalidArg "attributes" "A toggle group requires aria-label or aria-labelledby."
        let items = children |> List.choose (function :? ToggleGroupItem as item -> Some item | _ -> None)
        if List.isEmpty items then invalidArg "children" "A toggle group requires at least one item."
        ChoiceSelection.validateKeys (fun (item:ToggleGroupItem) -> item.Choice.value) items
        if (ComponentElement.attribute (if multiple then "value" else "values") attributes).IsSome then
            invalidArg "attributes" "Use value for ToggleGroup and values for MultiToggleGroup."
        let values =
            if multiple then
                match ComponentElement.value "values" attributes with
                | None -> []
                | Some json ->
                    try
                        let values = JsonSerializer.Deserialize<string array>(json)
                        if isNull values || Array.exists isNull values then invalidArg "attributes" "Selected values cannot be null."
                        values |> Array.toList |> List.distinct
                    with :? JsonException -> invalidArg "attributes" "MultiToggleGroup values must be a JSON array of strings."
            else ComponentElement.value "value" attributes |> Option.toList
        let selected = values |> List.map (fun value -> items |> List.tryFind (fun item -> item.Choice.value = value) |> Option.map _.Choice |> Option.defaultWith (fun () -> invalidArg "attributes" "Every selected value must match a toggle item."))
        let disabled = flag "disabled" attributes
        let readOnly = flag "readonly" attributes
        let unavailable = disabled || readOnly
        let vertical = ComponentElement.choice "orientation" "horizontal" ["horizontal"; "vertical"] attributes = "vertical"
        let spaced = ComponentElement.choice "variant" "connected" ["connected"; "spaced"] attributes = "spaced"
        let size = ComponentElement.size None attributes
        let signal = "_" + ComponentHtml.signalToken id + "_selected"
        let initial = ChoiceSelection.json selected
        let available = items |> List.filter (fun item -> not (unavailable || flag "disabled" item.Attributes))
        let buttonId (item:ToggleGroupItem) = ComponentElement.value "id" item.Attributes |> Option.defaultValue (id + "-toggle-" + ComponentHtml.optionToken item.Choice.value)
        let ids = available |> List.map buttonId
        let focus target = "document.getElementById(" + ComponentHtml.javascriptString target + ")?.focus()"
        let renderItem (item:ToggleGroupItem) =
            let choice = item.Choice
            let pressed = ChoiceSelection.selected signal choice.value
            let click =
                if multiple then ChoiceSelection.toggle signal choice.value choice.label
                elif flag "allow-empty" attributes then $"${signal} = {pressed} ? [] : {ChoiceSelection.json [choice]}"
                else $"${signal} = {ChoiceSelection.json [choice]}"
            let position = available |> List.tryFindIndex (fun candidate -> candidate.Choice.value = choice.value) |> Option.defaultValue 0
            let keys =
                if List.isEmpty ids then ""
                else
                    let previous = ids[(position - 1 + ids.Length) % ids.Length]
                    let next = ids[(position + 1) % ids.Length]
                    let previousKey, nextKey = if vertical then "ArrowUp", "ArrowDown" else "ArrowLeft", "ArrowRight"
                    $"evt.key === '{previousKey}' && (evt.preventDefault(), {focus previous}); evt.key === '{nextKey}' && (evt.preventDefault(), {focus next}); evt.key === 'Home' && (evt.preventDefault(), {focus ids.Head}); evt.key === 'End' && (evt.preventDefault(), {focus (List.last ids)})"
            let defaults = [
                _id (buttonId item); _type "button"
                _data ("fve-group-control", "outline")
                _disabled (unavailable || flag "disabled" item.Attributes)
                _ariaPressed (List.contains choice.value values)
                _dataAttr ("aria-pressed", $"{pressed} ? 'true' : 'false'")
                if not unavailable && not (flag "disabled" item.Attributes) then
                    _dataOn ("click", click)
                    _dataOn ("keydown", keys)
                _class ("inline-flex items-center justify-center gap-2 rounded-[var(--fve-radius-control)] [--fve-control-border-width:1px] border border-[var(--fve-border)] bg-[var(--fve-surface)] font-medium text-[var(--fve-text)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:z-10 focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)] aria-pressed:bg-[var(--fve-brand-subtle)] aria-pressed:text-[var(--fve-brand-text)] disabled:pointer-events-none disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0 " + ComponentHtml.sizeClasses size) ]
            ComponentElement.render "button" defaults (ComponentHtml.safeAttributes ["id"; "type"; "disabled"; "value"; "label"; "aria-pressed"] item.Attributes)
                (if List.isEmpty item.Children then [text choice.label] else item.Children)
        let classes =
            "inline-flex min-w-0 max-w-full" + (if not vertical && not spaced then " " + ComponentHtml.horizontalScrollClasses else "")
        let trackClasses =
            if not spaced then ControlGroup.classes (if vertical then "vertical" else "horizontal") + " shrink-0"
            elif vertical then "inline-flex flex-col items-stretch gap-2"
            else "inline-flex min-w-0 flex-wrap items-center gap-2"
        let defaults = [
            yield _role "group"
            yield _ariaDisabled unavailable
            yield _class classes
            yield _dataSignals $"{{{signal}: {initial}}}"
            yield _dataOn ("reset__window", $"if (evt.target === el.closest('form')) queueMicrotask(() => {{ ${signal} = {initial} }})")
            if not vertical && not spaced then yield! ComponentHtml.horizontalScrollAttributes ]
        let body = [
            yield ChoiceSelection.render name signal selected disabled
            // Successful form values stay outside the visual track and never count as segments.
            yield div {
                _class trackClasses
                for child in children do
                    match child with
                    | :? ToggleGroupItem as item -> renderItem item
                    | _ -> child
            } ]
        ComponentElement.render "div" defaults (ComponentHtml.safeAttributes ["name"; "value"; "values"; "disabled"; "readonly"; "variant"; "orientation"; "size"; "allow-empty"] attributes) body

/// <category>toggle-group</category>
[<AutoOpen>]
module ToggleGroupDsl =
    let fveToggleGroup = ComponentElementBuilder(ToggleGroup.root false)
    let fveMultiToggleGroup = ComponentElementBuilder(ToggleGroup.root true)
    let fveToggleGroupItem = ComponentElementBuilder(ToggleGroup.item)
