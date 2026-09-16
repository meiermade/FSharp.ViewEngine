namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type ChoiceCardOption<'value> =
    private
        { value:'value
          label:string
          description:string option
          leading:HtmlElement option
          metadata:string option
          disabled:bool }

[<RequireQualifiedAccess>]
module ChoiceCardOption =
    let create value label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A choice-card label is required."
        { value = value; label = label; description = None; leading = None; metadata = None; disabled = false }
    let withDescription description (option:ChoiceCardOption<'value>) = { option with description = Some description }
    let withLeading leading (option:ChoiceCardOption<'value>) = { option with leading = Some leading }
    let withMetadata metadata (option:ChoiceCardOption<'value>) = { option with metadata = Some metadata }
    let disabled (option:ChoiceCardOption<'value>) = { option with disabled = true }

[<NoEquality; NoComparison>]
type ChoiceCardsConfig<'value when 'value:equality> =
    private
        { id:string
          name:string
          label:string
          encode:'value -> string
          options:ChoiceCardOption<'value> list
          selected:'value list
          multiple:bool
          required:bool
          validation:string option }

[<RequireQualifiedAccess>]
module ChoiceCards =
    let single id name label encode options =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable choice-card ID is required."
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A choice-card group label is required."
        ChoiceSelection.validateKeys encode (options |> List.map _.value)
        { id = id; name = name; label = label; encode = encode; options = options; selected = []; multiple = false; required = false; validation = None }
    let multiple id name label encode options = { single id name label encode options with multiple = true }
    let withSelected selected (config:ChoiceCardsConfig<'value>) = { config with selected = List.distinct selected }
    let required config = { config with required = true }
    let withValidation validation config = { config with validation = Some validation }
    let render config =
        let legendId = config.id + "-legend"
        let validationId = config.id + "-validation"
        fieldset {
            _id config.id
            _ariaLabelledby legendId
            _ariaInvalid config.validation.IsSome
            if config.validation.IsSome then _ariaDescribedby validationId
            _class "grid gap-3"
            legend {
                _id legendId
                _class "text-sm font-medium text-[var(--fve-text)]"
                config.label
            }
            div {
                _class "grid gap-3 sm:grid-cols-2"
                for option in config.options do
                    let encoded = config.encode option.value
                    let optionId = config.id + "-" + ComponentHtml.optionToken encoded
                    label {
                        _for optionId
                        _class "relative flex min-w-0 cursor-pointer gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-4 ring-1 ring-[var(--fve-border)] has-[:checked]:bg-[var(--fve-brand-subtle)] has-[:checked]:ring-2 has-[:checked]:ring-[var(--fve-brand-ring)] has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-[var(--fve-brand-ring)] has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50"
                        input {
                            _id optionId
                            _type (if config.multiple then "checkbox" else "radio")
                            _name config.name
                            _value encoded
                            _checked (List.contains option.value config.selected)
                            _disabled option.disabled
                            _required (config.required && not option.disabled)
                            _class "mt-1 size-4 shrink-0 accent-[var(--fve-brand-solid)]"
                        }
                        match option.leading with
                        | Some leading ->
                            span {
                                _ariaHidden true
                                _class "flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-[var(--fve-radius-control)] bg-[var(--fve-neutral-subtle)]"
                                leading
                            }
                        | None -> ()
                        span {
                            _class "min-w-0 flex-1"
                            strong {
                                _class "block text-sm text-[var(--fve-text)]"
                                option.label
                            }
                            match option.description with
                            | Some description ->
                                span {
                                    _class "mt-1 block text-sm text-[var(--fve-muted-text)]"
                                    description
                                }
                            | None -> ()
                            match option.metadata with
                            | Some metadata ->
                                span {
                                    _class "mt-2 block text-xs font-medium text-[var(--fve-muted-text)]"
                                    metadata
                                }
                            | None -> ()
                        }
                    }
            }
            match config.validation with
            | Some validation ->
                p {
                    _id validationId
                    _role "alert"
                    _class "text-sm text-[var(--fve-critical-text)]"
                    validation
                }
            | None -> ()
        }
