namespace FSharp.ViewEngine.Components

open System
open System.Text.Json
open FSharp.ViewEngine
open type Html

module internal TagInput =
    let tagClasses = "inline-flex min-h-8 max-w-full items-center gap-1 rounded-full bg-[var(--fve-neutral-subtle)] py-1 pr-1 pl-3 text-sm leading-5 text-[var(--fve-text)]"
    let removeClasses = "inline-grid size-6 shrink-0 cursor-pointer place-items-center rounded-full border-0 bg-transparent p-0 text-base leading-none text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50"

    let render attributes children =
        ComponentContent.noChildren "fveTagInput" children
        let attributes = ComponentElement.attributes attributes
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "attributes" "A stable TagInput ID without whitespace is required."
        let name = ComponentElement.required "name" attributes
        ComponentElement.choice "type" "text" ["text"] attributes |> ignore
        if (ComponentElement.attribute "required" attributes).IsSome then invalidArg "attributes" "Use aria-required and server validation for tag cardinality, not a required empty entry field."
        let values =
            match ComponentElement.value "values" attributes with
            | None -> []
            | Some json ->
                try
                    let values = JsonSerializer.Deserialize<string array>(json)
                    if isNull values || Array.exists String.IsNullOrWhiteSpace values then invalidArg "attributes" "Tag values cannot be empty."
                    let values = Array.toList values
                    if List.distinct values <> values then invalidArg "attributes" "Initial tag values must be unique."
                    values
                with :? JsonException -> invalidArg "attributes" "Tag values must be a JSON array of strings."
        let flag name = ComponentElement.attribute name attributes |> Option.exists (fun attribute -> attribute.Value <> ValueSome "false")
        let disabled = flag "disabled"
        let readOnly = flag "readonly"
        let unavailable = disabled || readOnly
        (Html.el "fve-tag-input") {
            _id (id + "-field")
            _name name
            _attr("values", JsonSerializer.Serialize values)
            _disabled disabled
            _readonly readOnly
            _class "grid min-w-0 content-start gap-2"
            ul {
                _attr("data-fve-tag-list", "")
                _class "flex min-w-0 flex-wrap gap-2"
                for value in values do
                    li {
                        _attr("data-fve-tag-value", value)
                        _class tagClasses
                        span { _attr("data-fve-tag-label", ""); _class "min-w-0 [overflow-wrap:anywhere]"; value }
                        input { _type "hidden"; _name name; _value value; _disabled disabled }
                        button { _type "button"; _attr("data-fve-tag-remove", ""); _ariaLabel ("Remove " + value); _disabled unavailable; _class removeClasses; "×" }
                    }
            }
            div {
                _class "flex min-w-0 flex-wrap gap-2"
                input {
                    _type "text"
                    _id id
                    _attr("data-fve-tag-entry", "")
                    _autocomplete "off"
                    _class ("min-h-[var(--fve-control-min-height)] min-w-0 basis-40 flex-1 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] text-[var(--fve-text)] border border-[var(--fve-border)] aria-invalid:border-[var(--fve-critical-ring)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50 " + ComponentContent.classes attributes)
                    for attribute in ComponentHtml.safeAttributes ["type"; "id"; "name"; "values"; "class"] attributes |> ComponentHtml.outerSignals do attribute
                }
                button {
                    _type "button"
                    _attr("data-fve-tag-add", "")
                    _disabled unavailable
                    _class "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm font-semibold border border-[var(--fve-border)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                    "Add tag"
                }
            }
            output { _attr("data-fve-tag-status", ""); _role "status"; _ariaLive "polite"; _class "text-sm text-[var(--fve-muted-text)]" }
        }

/// <category>tag-input</category>
[<AutoOpen>]
module TagInputDsl =
    /// Editable string tags. Compose labels and feedback with Field; name belongs to successful tag values.
    let fveTagInput = ComponentElementBuilder(TagInput.render)
