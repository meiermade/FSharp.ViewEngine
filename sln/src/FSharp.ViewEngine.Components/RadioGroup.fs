namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type RadioGroupConfig<'value when 'value:equality> =
    private
        { name:string
          id:string option
          label:string
          encode:'value -> string
          options:SelectOption<'value> list
          selected:'value option
          description:string option
          validation:string option
          isRequired:bool
          isDisabled:bool
          isPending:bool }

[<RequireQualifiedAccess>]
module RadioGroup =
    let option value label = Select.option value label
    let disable option = Select.disable option

    let create name label encode options =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A radio group label is required."
        { name = name
          id = None
          label = label
          encode = encode
          options = options
          selected = None
          description = None
          validation = None
          isRequired = false
          isDisabled = false
          isPending = false }

    let withId id (config:RadioGroupConfig<'value>) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable radio group ID is required."
        { config with id = Some id }
    let withSelected selected (config:RadioGroupConfig<'value>) = { config with selected = Some selected }
    let withDescription description (config:RadioGroupConfig<'value>) = { config with description = Some description }
    let withValidation message (config:RadioGroupConfig<'value>) = { config with validation = Some message }
    let required (config:RadioGroupConfig<'value>) = { config with isRequired = true }
    let disabled (config:RadioGroupConfig<'value>) = { config with isDisabled = true }
    let pending (config:RadioGroupConfig<'value>) = { config with isPending = true }

    let render (config:RadioGroupConfig<'value>) =
        let token = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let groupId = $"fve-radio-{token}"
        let legendId = $"{groupId}-legend"
        let descriptionId = $"{groupId}-description"
        let validationId = $"{groupId}-validation"
        let valueSignal = $"{token}_value"
        let selectedValue = config.selected |> Option.map config.encode |> Option.defaultValue ""
        let unavailable = config.isDisabled || config.isPending
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        fieldset {
            _id groupId
            _role "radiogroup"
            _ariaLabelledby legendId
            _ariaRequired config.isRequired
            _ariaInvalid config.validation.IsSome
            _ariaDisabled unavailable
            if config.isPending then _ariaBusy true
            if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
            _disabled unavailable
            _dataSignals $"{{{valueSignal}: {ComponentHtml.javascriptString selectedValue}}}"
            legend {
                _id legendId
                _class "text-sm font-medium text-[var(--fve-text)]"
                config.label
                if config.isRequired then
                    span {
                        _ariaHidden "true"
                        _class "text-[var(--fve-critical-text)]"
                        " *"
                    }
                if config.isPending then span { _class "ml-2 inline-flex"; ComponentHtml.loadingGlyph ControlSize.Small }
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            div {
                _class "mt-2 grid gap-2"
                for choice in config.options do
                    let encodedValue = config.encode choice.value
                    let optionId = $"{groupId}-option-{ComponentHtml.optionToken encodedValue}"
                    let choiceUnavailable = unavailable || choice.disabled
                    let controlClasses =
                        ComponentHtml.classes [
                            "flex size-5 shrink-0 items-center justify-center rounded-full bg-[var(--fve-surface)] ring-1 ring-inset transition-colors peer-checked:ring-2 peer-checked:ring-[var(--fve-brand-solid)] peer-focus-visible:ring-2 peer-focus-visible:ring-offset-2"
                            if config.validation.IsSome then "ring-[var(--fve-critical-ring)] peer-focus-visible:ring-[var(--fve-critical-ring)]" else "ring-[var(--fve-border)] peer-focus-visible:ring-[var(--fve-brand-ring)]" ]
                    label {
                        _for optionId
                        _class "flex cursor-pointer items-center gap-3 text-sm text-[var(--fve-text)] has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50"
                        input {
                            _id optionId
                            _type "radio"
                            _name config.name
                            _value encodedValue
                            _checked (config.selected = Some choice.value)
                            _disabled choiceUnavailable
                            if config.isRequired && not choiceUnavailable then _required true
                            _ariaInvalid config.validation.IsSome
                            _dataBind valueSignal
                            if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                            _class "peer sr-only"
                        }
                        span {
                            _ariaHidden "true"
                            _class controlClasses
                            span {
                                _dataShow $"${valueSignal} == {ComponentHtml.javascriptString encodedValue}"
                                _style "display:none"
                                _class "size-2.5 rounded-full bg-[var(--fve-brand-solid)]"
                            }
                        }
                        span { _class "font-medium"; choice.label }
                    }
            }
            match config.validation with
            | Some message ->
                p {
                    _id validationId
                    _role "alert"
                    _class "mt-2 text-sm text-[var(--fve-critical-text)]"
                    message
                }
            | None -> ()
        }
