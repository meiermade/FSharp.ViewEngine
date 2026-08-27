namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<RequireQualifiedAccess>]
type ComboboxSearch =
    | Static
    | Remote of endpoint:string

[<NoEquality; NoComparison>]
type ComboboxConfig<'value when 'value:equality> =
    private
        { name:string
          id:string option
          encode:'value -> string
          options:SelectOption<'value> list
          selected:'value option
          label:string
          labelVisuallyHidden:bool
          search:ComboboxSearch
          placeholder:string option
          description:string option
          validation:string option
          emptyMessage:string
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Combobox =
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
          search = ComboboxSearch.Static
          placeholder = None
          description = None
          validation = None
          emptyMessage = "No matching options"
          attributes = [] }

    let withSelected selected (config:ComboboxConfig<'value>) = { config with selected = Some selected }
    let withId id (config:ComboboxConfig<'value>) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable component ID is required."
        { config with id = Some id }
    let withVisuallyHiddenLabel (config:ComboboxConfig<'value>) = { config with labelVisuallyHidden = true }
    let withPlaceholder placeholder (config:ComboboxConfig<'value>) = { config with placeholder = Some placeholder }
    let withDescription description (config:ComboboxConfig<'value>) = { config with description = Some description }
    let withValidation message (config:ComboboxConfig<'value>) = { config with validation = Some message }
    let withEmptyMessage message (config:ComboboxConfig<'value>) = { config with emptyMessage = message }
    let withSearch search (config:ComboboxConfig<'value>) = { config with search = search }
    let withOptions options (config:ComboboxConfig<'value>) = { config with options = options }
    let withAttributes attributes (config:ComboboxConfig<'value>) = { config with attributes = attributes }

    let renderOptions (config:ComboboxConfig<'value>) =
        let instanceId = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let fieldId = $"fve-combobox-{instanceId}"
        let labelId = $"{fieldId}-label"
        let listboxId = $"{fieldId}-options"
        let openSignal = $"_{instanceId}_open"
        let querySignal =
            match config.search with
            | ComboboxSearch.Static -> $"_{instanceId}_query"
            | ComboboxSearch.Remote _ -> $"{instanceId}_query"
        let valueSignal = $"{instanceId}_value"
        let activeSignal = $"_{instanceId}_active"
        let optionId (choice:SelectOption<_>) =
            $"{fieldId}-option-{choice.value |> config.encode |> ComponentHtml.optionToken}"
        let visibleOptions = $"Array.from(document.querySelectorAll('#{listboxId} [role=option]:not(:disabled)')).filter(item => item.style.display != 'none')"
        let firstVisibleOption = $"{visibleOptions}.at(0)"
        let optionSignature =
            config.options
            |> List.map (fun choice -> config.encode choice.value)
            |> String.concat "\u001f"
            |> ComponentHtml.javascriptString
        let synchronizeActive = $"(!document.getElementById(${activeSignal}) || document.getElementById(${activeSignal}).style.display == 'none') && (${activeSignal} = ({firstVisibleOption})?.id || '')"
        let optionMatches (choice:SelectOption<_>) =
            $"!${querySignal}.trim() || {ComponentHtml.javascriptString (choice.label.ToLowerInvariant())}.includes(${querySignal}.trim().toLowerCase())"
        let anyOptionMatches =
            match config.options with
            | [] -> "false"
            | options -> options |> List.map optionMatches |> String.concat " || "
        div {
            _id listboxId
            _role "listbox"
            _ariaLabelledby labelId
            _dataShow $"${openSignal}"
            _dataInit $"queueMicrotask(() => ({synchronizeActive})); {optionSignature}"
            _dataOn ("click", [ "outside" ], $"${openSignal} = false")
            _style "display:none"
            _class "absolute left-0 top-full z-30 mt-1 max-h-60 w-full overflow-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] p-1 shadow-lg ring-1 ring-[var(--fve-border)]"
            for choice in config.options do
                let encodedValue = config.encode choice.value
                let choiceId = optionId choice
                button {
                    _id choiceId
                    _type "button"
                    _role "option"
                    _tabindex -1
                    _disabled choice.disabled
                    _ariaDisabled choice.disabled
                    _ariaSelected (config.selected = Some choice.value)
                    _attr ("data-fve-option-label", choice.label.ToLowerInvariant())
                    _dataAttr ("aria-selected", $"${valueSignal} == {ComponentHtml.javascriptString encodedValue} ? 'true' : 'false'")
                    _dataAttr ("data-active", $"${activeSignal} == {ComponentHtml.javascriptString choiceId} ? 'true' : null")
                    match config.search with
                    | ComboboxSearch.Static -> _dataShow (optionMatches choice)
                    | ComboboxSearch.Remote _ -> ()
                    if choice.disabled |> not then
                        _dataOn ("mousedown", "evt.preventDefault()")
                        _dataOn ("click", $"${activeSignal} = {ComponentHtml.javascriptString choiceId}; ${valueSignal} = {ComponentHtml.javascriptString encodedValue}; ${querySignal} = {ComponentHtml.javascriptString choice.label}; ${openSignal} = false; document.getElementById('{fieldId}').focus()")
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
            p {
                _role "status"
                _dataShow $"!({anyOptionMatches})"
                _style "display:none"
                _class "px-3 py-4 text-center text-sm text-[var(--fve-muted-text)]"
                config.emptyMessage
            }
        }

    let render config =
        let instanceId = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let fieldId = $"fve-combobox-{instanceId}"
        let labelId = $"{fieldId}-label"
        let descriptionId = $"{fieldId}-description"
        let validationId = $"{fieldId}-validation"
        let listboxId = $"{fieldId}-options"
        let openSignal = $"_{instanceId}_open"
        let querySignal =
            match config.search with
            | ComboboxSearch.Static -> $"_{instanceId}_query"
            | ComboboxSearch.Remote _ -> $"{instanceId}_query"
        let valueSignal = $"{instanceId}_value"
        let activeSignal = $"_{instanceId}_active"
        let optionId (choice:SelectOption<_>) =
            $"{fieldId}-option-{choice.value |> config.encode |> ComponentHtml.optionToken}"
        let selectedChoice = config.options |> List.tryFind (fun option -> Some option.value = config.selected)
        let selectedValue = selectedChoice |> Option.map (fun option -> config.encode option.value) |> Option.defaultValue ""
        let selectedLabel = selectedChoice |> Option.map _.label |> Option.defaultValue ""
        let initialActiveId =
            selectedChoice
            |> Option.filter (fun choice -> choice.disabled |> not)
            |> Option.orElseWith (fun () -> config.options |> List.tryFind (fun choice -> choice.disabled |> not))
            |> Option.map optionId
            |> Option.defaultValue ""
        let visibleOptions = $"Array.from(document.querySelectorAll('#{listboxId} [role=option]:not(:disabled)')).filter(item => item.style.display != 'none')"
        let firstVisibleOption = $"{visibleOptions}.at(0)"
        let lastVisibleOption = $"{visibleOptions}.at(-1)"
        let currentIndex = $"{visibleOptions}.findIndex(item => item.id == ${activeSignal})"
        let scrollToActive = $"queueMicrotask(() => document.getElementById(${activeSignal})?.scrollIntoView({{block: 'nearest'}}))"
        let activate optionExpression = $"${activeSignal} = ({optionExpression})?.id || '', {scrollToActive}"
        let moveActive offset =
            let missingIndex = if offset > 0 then -1 else 0
            $"evt.preventDefault(), ${openSignal} = true, {visibleOptions}.length && (${activeSignal} = {visibleOptions}.at((({currentIndex} < 0 ? {missingIndex} : {currentIndex}) + {offset} + {visibleOptions}.length) %% {visibleOptions}.length)?.id || '', {scrollToActive})"
        let keydown =
            String.concat "; " [
                $"evt.key == 'ArrowDown' && ({moveActive 1})"
                $"evt.key == 'ArrowUp' && ({moveActive -1})"
                $"evt.key == 'Home' && ${openSignal} && (evt.preventDefault(), {activate firstVisibleOption})"
                $"evt.key == 'End' && ${openSignal} && (evt.preventDefault(), {activate lastVisibleOption})"
                $"evt.key == 'Enter' && ${openSignal} && ${activeSignal} && (evt.preventDefault(), document.getElementById(${activeSignal})?.click())"
                $"evt.key == 'Escape' && (${openSignal} = false)"
                $"evt.key == 'Tab' && (${openSignal} = false)" ]
        let synchronizeVisibleActive = $"${activeSignal} = ({firstVisibleOption})?.id || '', {scrollToActive}"
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        div {
            _class "relative grid gap-1.5"
            _dataSignals $"{{{openSignal}: false, {querySignal}: {ComponentHtml.javascriptString selectedLabel}, {valueSignal}: {ComponentHtml.javascriptString selectedValue}, {activeSignal}: {ComponentHtml.javascriptString initialActiveId}}}"
            label {
                _id labelId
                _for fieldId
                _class (if config.labelVisuallyHidden then "sr-only" else "text-sm font-medium text-[var(--fve-text)]")
                config.label
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            input {
                _id fieldId
                _type "search"
                _role "combobox"
                _ariaControls listboxId
                _ariaExpanded false
                _ariaAutocomplete "list"
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                _dataAttr ("aria-activedescendant", $"${openSignal} && document.getElementById(${activeSignal}) ? ${activeSignal} : null")
                if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                _ariaInvalid config.validation.IsSome
                _autocomplete "off"
                _placeholder (config.placeholder |> Option.defaultValue "Search options")
                _dataBind querySignal
                _dataOn ("click", [ "stop" ], $"${openSignal} = true; queueMicrotask(() => ((!document.getElementById(${activeSignal}) || document.getElementById(${activeSignal}).style.display == 'none') && ({synchronizeVisibleActive})))")
                _dataOn ("keydown", keydown)
                match config.search with
                | ComboboxSearch.Static -> _dataOn ("input", $"${openSignal} = true; ${valueSignal} = ''; queueMicrotask(() => ({synchronizeVisibleActive}))")
                | ComboboxSearch.Remote endpoint -> _dataOn ("input", [ "debounce.250ms" ], $"${openSignal} = true; ${valueSignal} = ''; ${activeSignal} = ''; @get({ComponentHtml.javascriptString endpoint})")
                _class "min-h-[var(--fve-control-min-height)] w-full rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-sm text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                for attribute in ComponentHtml.safeAttributes [ "id"; "type"; "name"; "value"; "role"; "aria-controls"; "aria-expanded"; "aria-activedescendant"; "aria-autocomplete"; "aria-describedby"; "aria-invalid"; "autocomplete"; "placeholder"; "data-bind:"; "data-attr:"; "data-on:"; "class" ] config.attributes do attribute
            }
            input {
                _type "hidden"
                _name config.name
                _value selectedValue
                _dataBind valueSignal
            }
            renderOptions config
            match config.validation with
            | Some message -> p { _id validationId; _class "text-sm text-[var(--fve-critical-text)]"; message }
            | None -> ()
        }
