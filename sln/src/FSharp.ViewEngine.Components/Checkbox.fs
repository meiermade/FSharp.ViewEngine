namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<NoEquality; NoComparison>]
type CheckboxConfig =
    private
        { name:string
          label:string
          description:string option
          isChecked:bool
          isDisabled:bool }

[<RequireQualifiedAccess>]
module Checkbox =
    let create name label =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A checkbox label is required."
        { name = name; label = label; description = None; isChecked = false; isDisabled = false }

    let withDescription description (config:CheckboxConfig) = { config with description = Some description }
    let withChecked (config:CheckboxConfig) = { config with isChecked = true }
    let disabled (config:CheckboxConfig) = { config with isDisabled = true }

    let render (config:CheckboxConfig) =
        let token = ComponentHtml.signalToken config.name
        let fieldId = $"fve-checkbox-{token}"
        let descriptionId = $"{fieldId}-description"
        let valueSignal = $"{token}_checked"
        let initialValue = if config.isChecked then "true" else "false"
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
                    _disabled config.isDisabled
                    _dataBind valueSignal
                    if config.description.IsSome then _ariaDescribedby descriptionId
                    _class "peer sr-only"
                }
                span {
                    _ariaHidden "true"
                    _class "mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] text-xs font-bold text-white ring-1 ring-inset ring-[var(--fve-border)] transition-colors peer-checked:bg-[var(--fve-brand-solid)] peer-checked:ring-[var(--fve-brand-solid)] peer-focus-visible:ring-2 peer-focus-visible:ring-[var(--fve-brand-ring)] peer-focus-visible:ring-offset-2"
                    span { _dataShow $"${valueSignal}"; _style "display:none"; "✓" }
                }
                span { _class "font-medium"; config.label }
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "ml-8 mt-1 text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
        }
