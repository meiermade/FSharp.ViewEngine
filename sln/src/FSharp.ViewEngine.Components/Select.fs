namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type SelectOption<'value> =
    internal
        { value:'value
          label:string
          disabled:bool }

[<NoEquality; NoComparison>]
type SelectConfig<'value when 'value:equality> =
    private
        { name:string
          id:string option
          encode:'value -> string
          options:SelectOption<'value> list
          selected:'value option
          label:string
          labelVisuallyHidden:bool
          description:string option
          placeholder:string option
          validation:string option
          isRequired:bool
          isDisabled:bool
          isPending:bool
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Select =
    let option value label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An option label is required."
        { value = value; label = label; disabled = false }

    let disable (option:SelectOption<'value>) = { option with disabled = true }

    let create name label encode options =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible label is required."
        { name = name
          id = None
          encode = encode
          options = options
          selected = None
          label = label
          labelVisuallyHidden = false
          description = None
          placeholder = None
          validation = None
          isRequired = false
          isDisabled = false
          isPending = false
          attributes = [] }

    let withSelected selected (config:SelectConfig<'value>) = { config with selected = Some selected }
    let withId id (config:SelectConfig<'value>) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable component ID is required."
        { config with id = Some id }
    let withVisuallyHiddenLabel (config:SelectConfig<'value>) = { config with labelVisuallyHidden = true }
    let withDescription description (config:SelectConfig<'value>) = { config with description = Some description }
    let withPlaceholder placeholder (config:SelectConfig<'value>) = { config with placeholder = Some placeholder }
    let withValidation message (config:SelectConfig<'value>) = { config with validation = Some message }
    let required (config:SelectConfig<'value>) = { config with isRequired = true }
    let disabled (config:SelectConfig<'value>) = { config with isDisabled = true }
    let pending (config:SelectConfig<'value>) = { config with isPending = true }
    let withAttributes attributes (config:SelectConfig<'value>) = { config with attributes = attributes }

    let render config =
        let instanceId = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let fieldId = $"fve-select-{instanceId}"
        let labelId = $"{fieldId}-label"
        let descriptionId = $"{fieldId}-description"
        let validationId = $"{fieldId}-validation"
        let triggerId = $"{fieldId}-trigger"
        let listboxId = $"{fieldId}-options"
        let openSignal = $"_{instanceId}_open"
        let valueSignal = $"{instanceId}_value"
        let labelSignal = $"_{instanceId}_label"
        let activeSignal = $"_{instanceId}_active"
        let typeaheadSignal = $"_{instanceId}_typeahead"
        let typeaheadTimeSignal = $"_{instanceId}_typeahead_time"
        let unavailable = config.isDisabled || config.isPending
        let optionId (choice:SelectOption<_>) =
            $"{fieldId}-option-{choice.value |> config.encode |> ComponentHtml.optionToken}"
        let selectedChoice = config.options |> List.tryFind (fun option -> Some option.value = config.selected)
        let selectedValue = selectedChoice |> Option.map (fun option -> config.encode option.value) |> Option.defaultValue ""
        let selectedLabel =
            selectedChoice
            |> Option.map _.label
            |> Option.orElse config.placeholder
            |> Option.defaultValue "Select an option"
        let initialActiveId =
            selectedChoice
            |> Option.filter (fun choice -> choice.disabled |> not)
            |> Option.orElseWith (fun () -> config.options |> List.tryFind (fun choice -> choice.disabled |> not))
            |> Option.map optionId
            |> Option.defaultValue ""
        let enabledOptions = $"Array.from(document.querySelectorAll('#{listboxId} [role=option]:not(:disabled)'))"
        let firstOption = $"{enabledOptions}.at(0)"
        let lastOption = $"{enabledOptions}.at(-1)"
        let selectedOption = $"document.querySelector('#{listboxId} [aria-selected=true]:not(:disabled)')"
        let currentIndex = $"{enabledOptions}.findIndex(item => item.id == ${activeSignal})"
        let scrollToActive = $"queueMicrotask(() => document.getElementById(${activeSignal})?.scrollIntoView({{block: 'nearest'}}))"
        let activate optionExpression = $"${activeSignal} = ({optionExpression})?.id || '', {scrollToActive}"
        let selectedOrFirst = $"{selectedOption} || {firstOption}"
        let openAt optionExpression = $"${openSignal} = true, {activate optionExpression}"
        let moveActive offset =
            let nextIndex =
                if offset > 0 then
                    $"Math.min(({currentIndex} < 0 ? 0 : {currentIndex} + {offset}), {enabledOptions}.length - 1)"
                else
                    $"Math.max(({currentIndex} < 0 ? 0 : {currentIndex} + {offset}), 0)"
            $"evt.preventDefault(), ${openSignal} ? ({enabledOptions}.length && (${activeSignal} = {enabledOptions}.at({nextIndex})?.id || '', {scrollToActive})) : ({openAt (if offset > 0 then selectedOrFirst else firstOption)})"
        let jumpActive offset =
            let nextIndex =
                if offset > 0 then
                    $"Math.min(({currentIndex} < 0 ? 0 : {currentIndex} + {offset}), {enabledOptions}.length - 1)"
                else
                    $"Math.max(({currentIndex} < 0 ? 0 : {currentIndex} + {offset}), 0)"
            $"evt.preventDefault(), ${openSignal} = true, {enabledOptions}.length && (${activeSignal} = {enabledOptions}.at({nextIndex})?.id || '', {scrollToActive})"
        let commitActive = $"${activeSignal} && document.getElementById(${activeSignal})?.click()"
        let searchText = $"(Array.from(${typeaheadSignal}).every(character => character == ${typeaheadSignal}[0]) ? ${typeaheadSignal}[0] : ${typeaheadSignal})"
        let startIndex = $"Math.max(0, {currentIndex} + 1)"
        let orderedOptions = $"{enabledOptions}.slice({startIndex}).concat({enabledOptions}.slice(0, {startIndex}))"
        let typeaheadMatch = $"{orderedOptions}.find(item => item.dataset.fveOptionLabel.startsWith({searchText}))"
        let typeahead =
            $"evt.key.length == 1 && evt.key != ' ' && !evt.altKey && !evt.ctrlKey && !evt.metaKey && (evt.preventDefault(), ${typeaheadSignal} = Date.now() - ${typeaheadTimeSignal} > 700 ? evt.key.toLowerCase() : ${typeaheadSignal} + evt.key.toLowerCase(), ${typeaheadTimeSignal} = Date.now(), ${openSignal} = true, ${activeSignal} = ({typeaheadMatch})?.id || ${activeSignal}, {scrollToActive})"
        let keydown =
            String.concat "; " [
                $"evt.altKey && evt.key == 'ArrowDown' && (evt.preventDefault(), ${openSignal} || ({openAt selectedOrFirst}))"
                $"evt.altKey && evt.key == 'ArrowUp' && ${openSignal} && (evt.preventDefault(), {commitActive})"
                $"!evt.altKey && evt.key == 'ArrowDown' && ({moveActive 1})"
                $"!evt.altKey && evt.key == 'ArrowUp' && ({moveActive -1})"
                $"evt.key == 'Home' && (evt.preventDefault(), {openAt firstOption})"
                $"evt.key == 'End' && (evt.preventDefault(), {openAt lastOption})"
                $"evt.key == 'PageUp' && ({jumpActive -10})"
                $"evt.key == 'PageDown' && ({jumpActive 10})"
                $"(evt.key == 'Enter' || evt.key == ' ') && (evt.preventDefault(), ${openSignal} && ${activeSignal} ? {commitActive} : ({openAt selectedOrFirst}))"
                $"evt.key == 'Escape' && ${openSignal} && (evt.preventDefault(), ${openSignal} = false, ${activeSignal} = ({selectedOrFirst})?.id || '', ${typeaheadSignal} = '')"
                $"evt.key == 'Tab' && ${openSignal} && ({commitActive}, ${openSignal} = false, ${typeaheadSignal} = '')"
                typeahead ]
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        let triggerClasses =
            ComponentHtml.classes [
                "group flex min-h-[var(--fve-control-min-height)] w-full items-center justify-between gap-3 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-left text-sm text-[var(--fve-text)] ring-1 ring-inset outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-50"
                if config.validation.IsSome then "ring-[var(--fve-critical-ring)] focus-visible:ring-[var(--fve-critical-ring)]" else "ring-[var(--fve-border)] focus-visible:ring-[var(--fve-brand-ring)]" ]
        div {
            _class "relative grid gap-1.5"
            _dataSignals $"{{{openSignal}: false, {valueSignal}: {ComponentHtml.javascriptString selectedValue}, {labelSignal}: {ComponentHtml.javascriptString selectedLabel}, {activeSignal}: {ComponentHtml.javascriptString initialActiveId}, {typeaheadSignal}: '', {typeaheadTimeSignal}: 0}}"
            label {
                _id labelId
                _for triggerId
                _class (if config.labelVisuallyHidden then "sr-only" else "text-sm font-medium text-[var(--fve-text)]")
                config.label
                if config.isRequired then
                    span {
                        _ariaHidden "true"
                        _class "text-[var(--fve-critical-text)]"
                        " *"
                    }
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            input {
                _type "hidden"
                _name config.name
                _value selectedValue
                _disabled unavailable
                _dataBind valueSignal
            }
            button {
                _id triggerId
                _type "button"
                _role "combobox"
                _ariaHaspopup "listbox"
                _ariaControls listboxId
                _ariaLabelledby labelId
                _ariaExpanded false
                _ariaRequired config.isRequired
                _ariaDisabled unavailable
                _ariaInvalid config.validation.IsSome
                _disabled unavailable
                if config.isPending then _ariaBusy true
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                _dataAttr ("aria-activedescendant", $"${openSignal} && document.getElementById(${activeSignal}) ? ${activeSignal} : null")
                if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                _dataOn ("click", [ "stop" ], $"document.getElementById('{triggerId}').focus(); ${openSignal} = !${openSignal}; ${openSignal} && queueMicrotask(() => (!document.getElementById(${activeSignal}) && (${activeSignal} = ({selectedOrFirst})?.id || ''), document.getElementById(${activeSignal})?.scrollIntoView({{block: 'nearest'}})))")
                _dataOn ("keydown", keydown)
                _class triggerClasses
                for attribute in ComponentHtml.safeAttributes [ "id"; "type"; "name"; "value"; "disabled"; "role"; "aria-haspopup"; "aria-controls"; "aria-labelledby"; "aria-expanded"; "aria-activedescendant"; "aria-describedby"; "aria-required"; "aria-disabled"; "aria-invalid"; "aria-busy"; "data-bind:"; "data-attr:"; "data-on:"; "class" ] config.attributes do attribute
                span {
                    _class "min-w-0 truncate"
                    _dataText $"${labelSignal}"
                    selectedLabel
                }
                if config.isPending then
                    ComponentHtml.loadingGlyph ControlSize.Small
                else
                    raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4 shrink-0 text-[var(--fve-muted-text)] transition-transform group-aria-expanded:rotate-180" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
            }
            div {
                _id listboxId
                _role "listbox"
                _ariaLabelledby labelId
                _dataShow $"${openSignal}"
                _dataOn ("click", [ "outside" ], $"${openSignal} = false; ${activeSignal} = ({selectedOrFirst})?.id || ''; ${typeaheadSignal} = ''")
                _style "display:none"
                _class "absolute left-0 top-full z-30 mt-1 max-h-60 w-full overflow-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] p-1 shadow-lg ring-1 ring-[var(--fve-border)]"
                for choice in config.options do
                    let encodedValue = config.encode choice.value
                    let choiceId = optionId choice
                    let choiceUnavailable = choice.disabled || unavailable
                    button {
                        _id choiceId
                        _type "button"
                        _role "option"
                        _tabindex -1
                        _disabled choiceUnavailable
                        _ariaDisabled choiceUnavailable
                        _ariaSelected (config.selected = Some choice.value)
                        _attr ("data-fve-option-label", choice.label.ToLowerInvariant())
                        _dataAttr ("aria-selected", $"${valueSignal} == {ComponentHtml.javascriptString encodedValue} ? 'true' : 'false'")
                        _dataAttr ("data-active", $"${activeSignal} == {ComponentHtml.javascriptString choiceId} ? 'true' : null")
                        if choiceUnavailable |> not then
                            _dataOn ("mousedown", "evt.preventDefault()")
                            _dataOn ("click", $"${activeSignal} = {ComponentHtml.javascriptString choiceId}; ${valueSignal} = {ComponentHtml.javascriptString encodedValue}; ${labelSignal} = {ComponentHtml.javascriptString choice.label}; ${typeaheadSignal} = ''; ${openSignal} = false; document.getElementById('{triggerId}').focus()")
                        _class "flex w-full items-center justify-between gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-left text-sm text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] focus:bg-[var(--fve-surface-hover)] data-[active=true]:bg-[var(--fve-surface-hover)] data-[active=true]:ring-2 data-[active=true]:ring-inset data-[active=true]:ring-[var(--fve-brand-ring)] aria-selected:bg-[var(--fve-brand-subtle)] aria-selected:text-[var(--fve-brand-text)] disabled:cursor-not-allowed disabled:opacity-50"
                        span { _class "min-w-0 truncate"; choice.label }
                        span {
                            _ariaHidden "true"
                            _dataShow $"${valueSignal} == {ComponentHtml.javascriptString encodedValue}"
                            _style "display:none"
                            _class "shrink-0 font-semibold text-[var(--fve-brand-text)]"
                            "✓"
                        }
                    }
            }
            match config.validation with
            | Some message ->
                p {
                    _id validationId
                    _role "alert"
                    _class "text-sm text-[var(--fve-critical-text)]"
                    message
                }
            | None -> ()
        }
