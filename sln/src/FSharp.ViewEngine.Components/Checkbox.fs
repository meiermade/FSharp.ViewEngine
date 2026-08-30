namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type CheckboxConfig =
    private
        { name:string
          id:string option
          label:string
          description:string option
          validation:string option
          isChecked:bool
          isRequired:bool
          isDisabled:bool
          isPending:bool }

[<RequireQualifiedAccess>]
module Checkbox =
    let create name label =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A checkbox label is required."
        { name = name
          id = None
          label = label
          description = None
          validation = None
          isChecked = false
          isRequired = false
          isDisabled = false
          isPending = false }

    let withId id (config:CheckboxConfig) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable checkbox ID is required."
        { config with id = Some id }
    let withDescription description (config:CheckboxConfig) = { config with description = Some description }
    let withValidation message (config:CheckboxConfig) = { config with validation = Some message }
    let withChecked (config:CheckboxConfig) = { config with isChecked = true }
    let required (config:CheckboxConfig) = { config with isRequired = true }
    let disabled (config:CheckboxConfig) = { config with isDisabled = true }
    let pending (config:CheckboxConfig) = { config with isPending = true }

    let render (config:CheckboxConfig) =
        let token = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let fieldId = $"fve-checkbox-{token}"
        let descriptionId = $"{fieldId}-description"
        let validationId = $"{fieldId}-validation"
        let valueSignal = $"{token}_checked"
        let initialValue = if config.isChecked then "true" else "false"
        let unavailable = config.isDisabled || config.isPending
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        let controlClasses =
            ComponentHtml.classes [
                "mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] text-xs font-bold text-white ring-1 ring-inset transition-colors peer-checked:bg-[var(--fve-brand-solid)] peer-checked:ring-[var(--fve-brand-solid)] peer-focus-visible:ring-2 peer-focus-visible:ring-offset-2"
                if config.validation.IsSome then "ring-[var(--fve-critical-ring)] peer-focus-visible:ring-[var(--fve-critical-ring)]" else "ring-[var(--fve-border)] peer-focus-visible:ring-[var(--fve-brand-ring)]" ]
        div {
            _dataSignals $"{{{valueSignal}: {initialValue}}}"
            label {
                _for fieldId
                _class "flex cursor-pointer items-start gap-3 text-sm text-[var(--fve-text)] has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50"
                input {
                    _id fieldId
                    _type "checkbox"
                    _name config.name
                    _value "true"
                    _checked config.isChecked
                    _disabled unavailable
                    if config.isRequired && not unavailable then _required true
                    _ariaRequired config.isRequired
                    _ariaInvalid config.validation.IsSome
                    if config.isPending then _ariaBusy true
                    _dataBind valueSignal
                    if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                    _class "peer sr-only"
                }
                span {
                    _ariaHidden "true"
                    _class controlClasses
                    span { _dataShow $"${valueSignal}"; _style "display:none"; "✓" }
                }
                span {
                    _class "font-medium"
                    config.label
                    if config.isRequired then
                        span {
                            _ariaHidden "true"
                            _class "text-[var(--fve-critical-text)]"
                            " *"
                        }
                }
                if config.isPending then ComponentHtml.loadingGlyph ControlSize.Small
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "ml-8 mt-1 text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            match config.validation with
            | Some message ->
                p {
                    _id validationId
                    _role "alert"
                    _class "ml-8 mt-1 text-sm text-[var(--fve-critical-text)]"
                    message
                }
            | None -> ()
        }
