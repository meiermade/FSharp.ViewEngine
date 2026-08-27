namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<NoEquality; NoComparison>]
type RadioGroupConfig<'value when 'value:equality> =
    private
        { name:string
          label:string
          encode:'value -> string
          options:SelectOption<'value> list
          selected:'value option
          description:string option
          isDisabled:bool }

[<RequireQualifiedAccess>]
module RadioGroup =
    let option value label = Select.option value label
    let disable option = Select.disable option

    let create name label encode options =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A radio group label is required."
        { name = name; label = label; encode = encode; options = options; selected = None; description = None; isDisabled = false }

    let withSelected selected (config:RadioGroupConfig<'value>) = { config with selected = Some selected }
    let withDescription description (config:RadioGroupConfig<'value>) = { config with description = Some description }
    let disabled (config:RadioGroupConfig<'value>) = { config with isDisabled = true }

    let render (config:RadioGroupConfig<'value>) =
        let token = ComponentHtml.signalToken config.name
        let groupId = $"fve-radio-{token}"
        let descriptionId = $"{groupId}-description"
        let valueSignal = $"{token}_value"
        let selectedValue = config.selected |> Option.map config.encode |> Option.defaultValue ""
        fieldset {
            _dataSignals $"{{{valueSignal}: {ComponentHtml.javascriptString selectedValue}}}"
            legend { _class "text-sm font-medium text-[var(--fve-text)]"; config.label }
            match config.description with
            | Some description -> p { _id descriptionId; _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            div {
                _class "mt-2 grid gap-2"
                for choice in config.options do
                    let encodedValue = config.encode choice.value
                    let optionId = $"{groupId}-option-{ComponentHtml.optionToken encodedValue}"
                    label {
                        _for optionId
                        _class "flex cursor-pointer items-center gap-3 text-sm text-[var(--fve-text)] has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50"
                        input {
                            _id optionId
                            _type "radio"
                            _name config.name
                            _value encodedValue
                            _checked (config.selected = Some choice.value)
                            _disabled (config.isDisabled || choice.disabled)
                            _dataBind valueSignal
                            if config.description.IsSome then _ariaDescribedby descriptionId
                            _class "peer sr-only"
                        }
                        span {
                            _ariaHidden "true"
                            _class "flex size-5 shrink-0 items-center justify-center rounded-full bg-[var(--fve-surface)] ring-1 ring-inset ring-[var(--fve-border)] transition-colors peer-checked:ring-2 peer-checked:ring-[var(--fve-brand-solid)] peer-focus-visible:ring-2 peer-focus-visible:ring-[var(--fve-brand-ring)] peer-focus-visible:ring-offset-2"
                            span {
                                _dataShow $"${valueSignal} == {ComponentHtml.javascriptString encodedValue}"
                                _style "display:none"
                                _class "size-2.5 rounded-full bg-[var(--fve-brand-solid)]"
                            }
                        }
                        span { _class "font-medium"; choice.label }
                    }
            }
        }
