namespace FSharp.ViewEngine.Components.Primitives

open System
open System.Text.Json
open FSharp.ViewEngine
open type Html
open type Datastar

/// Type markers keep single-value and multiple-value configuration pipelines distinct.
type SingleSelection = internal | SingleSelection
type MultipleSelection = internal | MultipleSelection

[<NoEquality; NoComparison>]
type internal SelectedChoice = { value:string; label:string }

module internal ChoiceSelection =
    let json (choices:SelectedChoice list) =
        choices
        |> List.map (fun item -> dict [ "value", item.value; "label", item.label ])
        |> JsonSerializer.Serialize

    let validateKeys encode options =
        let keys = options |> List.map encode
        if keys |> List.exists isNull then invalidArg (nameof options) "Encoded choice values cannot be null."
        if (keys |> List.distinct |> List.length) <> keys.Length then
            invalidArg (nameof options) "Encoded choice values must be unique."

    let summary signal placeholder =
        $"(${signal}.length ? ${signal}.slice(0, 2).map(item => item.label).join(', ') + (${signal}.length > 2 ? ' + ' + (${signal}.length - 2) + ' more' : '') : {ComponentHtml.javascriptString placeholder})"

    let toggle signal value label =
        let key = ComponentHtml.javascriptString value
        let item = json [ { value = value; label = label } ]
        $"${signal} = ${signal}.some(item => item.value === {key}) ? ${signal}.filter(item => item.value !== {key}) : ${signal}.concat({item})"

    let selected signal value = $"${signal}.some(item => item.value === {ComponentHtml.javascriptString value})"

    // Dynamic remote results cannot pre-render all successful controls. Reconcile only
    // selected items, with encoded identity and textContent (never HTML from labels).
    // Datastar owns the selected array; these nodes are its native form presentation.
    let render name signal initial unavailable =
        let disabled = if unavailable then "true" else "false"
        let reconcile =
            $"""const choices = ${signal};
const host = el.firstElementChild;
const existing = new Map(Array.from(host.children).map(node => [node.dataset.fveChoiceValue, node]));
for (const [index, item] of choices.entries()) {{
    let node = existing.get(item.value);
    if (!node) {{
        node = document.createElement('span');
        node.dataset.fveChoiceValue = item.value;
        const field = document.createElement('input');
        field.type = 'hidden';
        node.append(field);
    }}
    const field = node.querySelector('input');
    field.name = {ComponentHtml.javascriptString name};
    field.value = item.value;
    field.disabled = {disabled};
    if (host.children[index] !== node) host.insertBefore(node, host.children[index] || null);
    existing.delete(item.value);
}}
for (const node of existing.values()) node.remove();"""
        div {
            _class "hidden"
            _dataEffect reconcile
            div {
                _class "contents"
                _dataIgnoreMorph
                for item:SelectedChoice in initial do
                    span {
                        _attr ("data-fve-choice-value", item.value)
                        input { _type "hidden"; _name name; _value item.value; _disabled unavailable }
                    }
            }
        }

    let selectAll actionLabel label signal unavailable eligibleChoices focusId =
        let disabled = if unavailable then "true" else "false"
        button {
            _type "button"
            _tabindex 0
            _ariaLabel ($"{actionLabel} {label}")
            _disabled unavailable
            _dataAttr ("disabled", $"{disabled} || !({eligibleChoices}).some(option => !${signal}.some(choice => choice.value === option.value))")
            _dataOn ("click", $"const additions = ({eligibleChoices}).filter(option => !${signal}.some(choice => choice.value === option.value)); ${signal} = ${signal}.concat(additions); document.getElementById({ComponentHtml.javascriptString focusId})?.focus()")
            _class "fve-popup-control inline-flex min-h-8 items-center rounded-[var(--fve-radius-control)] px-2 text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-muted-text)] hover:text-[var(--fve-text)] disabled:cursor-not-allowed disabled:opacity-50"
            text actionLabel
        }

    let clear label signal initial unavailable focusId =
        let disabled = if unavailable then "true" else "false"
        button {
            _type "button"
            _tabindex 0
            _ariaLabel ("Clear " + label)
            _disabled (unavailable || List.isEmpty initial)
            _dataAttr ("disabled", $"{disabled} || !${signal}.length")
            _dataOn ("click", $"${signal} = []; document.getElementById({ComponentHtml.javascriptString focusId})?.focus()")
            _class "fve-popup-control ml-auto inline-flex min-h-8 items-center rounded-[var(--fve-radius-control)] px-2 text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-muted-text)] hover:text-[var(--fve-text)] disabled:cursor-not-allowed disabled:opacity-50"
            "Clear selection"
        }

    let announcement signal initial =
        output {
            _role "status"
            _class "sr-only"
            _dataText $"${signal}.length + ' selected'"
            $"{List.length initial} selected"
        }
