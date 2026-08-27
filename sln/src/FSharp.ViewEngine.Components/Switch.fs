namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<NoEquality; NoComparison>]
type SwitchConfig =
    private
        { name:string
          label:string
          description:string option
          isChecked:bool
          isDisabled:bool }

[<RequireQualifiedAccess>]
module Switch =
    let create name label =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A switch label is required."
        { name = name; label = label; description = None; isChecked = false; isDisabled = false }

    let withDescription description (config:SwitchConfig) = { config with description = Some description }
    let withChecked (config:SwitchConfig) = { config with isChecked = true }
    let disabled (config:SwitchConfig) = { config with isDisabled = true }

    let render (config:SwitchConfig) =
        let token = ComponentHtml.signalToken config.name
        let fieldId = $"fve-switch-{token}"
        let descriptionId = $"{fieldId}-description"
        let valueSignal = $"{token}_enabled"
        let initialValue = if config.isChecked then "true" else "false"
        div {
            _dataSignals $"{{{valueSignal}: {initialValue}}}"
            label {
                _for fieldId
                _class "flex cursor-pointer items-start justify-between gap-4 text-sm text-[var(--fve-text)] has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50"
                span { _class "font-medium"; config.label }
                span {
                    _class "relative mt-0.5 shrink-0"
                    input {
                        _id fieldId
                        _type "checkbox"
                        _name config.name
                        _value "true"
                        _role "switch"
                        _checked config.isChecked
                        _disabled config.isDisabled
                        _ariaChecked config.isChecked
                        _dataAttr ("aria-checked", $"${valueSignal} ? 'true' : 'false'")
                        _dataBind valueSignal
                        if config.description.IsSome then _ariaDescribedby descriptionId
                        _class "peer sr-only"
                    }
                    span {
                        _ariaHidden "true"
                        _class "block h-5 w-9 rounded-full bg-[var(--fve-neutral-subtle)] ring-1 ring-inset ring-[var(--fve-border)] transition-colors peer-checked:bg-[var(--fve-brand-solid)] peer-checked:ring-[var(--fve-brand-solid)] peer-focus-visible:ring-2 peer-focus-visible:ring-[var(--fve-brand-ring)] peer-focus-visible:ring-offset-2"
                    }
                    span {
                        _ariaHidden "true"
                        _dataClass ("translate-x-4", $"${valueSignal}")
                        _class "pointer-events-none absolute left-0.5 top-0.5 size-4 translate-x-0 rounded-full bg-white shadow-sm transition-transform"
                    }
                }
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "mt-1 pr-12 text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
        }
