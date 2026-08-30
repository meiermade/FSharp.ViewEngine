namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type SwitchConfig =
    private
        { name:string
          id:string option
          label:string
          description:string option
          validation:string option
          isChecked:bool
          isDisabled:bool
          isPending:bool }

[<RequireQualifiedAccess>]
module Switch =
    let create name label =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A switch label is required."
        { name = name
          id = None
          label = label
          description = None
          validation = None
          isChecked = false
          isDisabled = false
          isPending = false }

    let withId id (config:SwitchConfig) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable switch ID is required."
        { config with id = Some id }
    let withDescription description (config:SwitchConfig) = { config with description = Some description }
    let withValidation message (config:SwitchConfig) = { config with validation = Some message }
    let withChecked (config:SwitchConfig) = { config with isChecked = true }
    let disabled (config:SwitchConfig) = { config with isDisabled = true }
    let pending (config:SwitchConfig) = { config with isPending = true }

    let render (config:SwitchConfig) =
        let token = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let fieldId = $"fve-switch-{token}"
        let descriptionId = $"{fieldId}-description"
        let validationId = $"{fieldId}-validation"
        let valueSignal = $"{token}_enabled"
        let initialValue = if config.isChecked then "true" else "false"
        let unavailable = config.isDisabled || config.isPending
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        let trackClasses =
            ComponentHtml.classes [
                "block h-5 w-9 rounded-full bg-[var(--fve-neutral-subtle)] ring-1 ring-inset transition-colors peer-checked:bg-[var(--fve-brand-solid)] peer-checked:ring-[var(--fve-brand-solid)] peer-focus-visible:ring-2 peer-focus-visible:ring-offset-2"
                if config.validation.IsSome then "ring-[var(--fve-critical-ring)] peer-focus-visible:ring-[var(--fve-critical-ring)]" else "ring-[var(--fve-border)] peer-focus-visible:ring-[var(--fve-brand-ring)]" ]
        div {
            _dataSignals $"{{{valueSignal}: {initialValue}}}"
            label {
                _for fieldId
                _class "flex cursor-pointer items-start justify-between gap-4 text-sm text-[var(--fve-text)] has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50"
                span { _class "font-medium"; config.label }
                span {
                    _class "flex shrink-0 items-center gap-2"
                    if config.isPending then ComponentHtml.loadingGlyph ControlSize.Small
                    span {
                        _class "relative mt-0.5 shrink-0"
                        input {
                            _id fieldId
                            _type "checkbox"
                            _name config.name
                            _value "true"
                            _role "switch"
                            _checked config.isChecked
                            _disabled unavailable
                            _ariaChecked config.isChecked
                            _ariaDisabled unavailable
                            _ariaInvalid config.validation.IsSome
                            if config.isPending then _ariaBusy true
                            _dataAttr ("aria-checked", $"${valueSignal} ? 'true' : 'false'")
                            _dataBind valueSignal
                            if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                            _class "peer sr-only"
                        }
                        span {
                            _ariaHidden "true"
                            _class trackClasses
                        }
                        span {
                            _ariaHidden "true"
                            _dataClass ("translate-x-4", $"${valueSignal}")
                            _class "pointer-events-none absolute left-0.5 top-0.5 size-4 translate-x-0 rounded-full bg-white shadow-sm transition-transform"
                        }
                    }
                }
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "mt-1 pr-12 text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            match config.validation with
            | Some message ->
                p {
                    _id validationId
                    _role "alert"
                    _class "mt-1 pr-12 text-sm text-[var(--fve-critical-text)]"
                    message
                }
            | None -> ()
        }
