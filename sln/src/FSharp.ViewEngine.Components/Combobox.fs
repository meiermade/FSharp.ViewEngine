namespace FSharp.ViewEngine.Components

open System
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
          loadingMessage:string
          error:string option
          isClearable:bool
          isLoading:bool
          isDisabled:bool
          isPending:bool
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
          loadingMessage = "Loading options"
          error = None
          isClearable = false
          isLoading = false
          isDisabled = false
          isPending = false
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
    let withLoadingMessage message (config:ComboboxConfig<'value>) = { config with loadingMessage = message }
    let withError message (config:ComboboxConfig<'value>) = { config with error = Some message }
    let withSearch search (config:ComboboxConfig<'value>) = { config with search = search }
    let withOptions options (config:ComboboxConfig<'value>) = { config with options = options }
    let clearable (config:ComboboxConfig<'value>) = { config with isClearable = true }
    let loading (config:ComboboxConfig<'value>) = { config with isLoading = true }
    let disabled (config:ComboboxConfig<'value>) = { config with isDisabled = true }
    let pending (config:ComboboxConfig<'value>) = { config with isPending = true }
    let withAttributes attributes (config:ComboboxConfig<'value>) = { config with attributes = attributes }

    let private request endpoint =
        $"@get({ComponentHtml.javascriptString endpoint}, {{requestCancellation: 'auto'}})"

    let renderOptions (config:ComboboxConfig<'value>) =
        let instanceId = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let fieldId = $"fve-combobox-{instanceId}"
        let labelId = $"{fieldId}-label"
        let popupId = $"{fieldId}-popup"
        let listboxId = $"{fieldId}-options"
        let openSignal = $"_{instanceId}_open"
        let querySignal =
            match config.search with
            | ComboboxSearch.Static -> $"_{instanceId}_query"
            | ComboboxSearch.Remote _ -> $"{instanceId}_query"
        let valueSignal = $"{instanceId}_value"
        let activeSignal = $"_{instanceId}_active"
        let requestPendingSignal = $"_{instanceId}_request_pending"
        let unavailable = config.isDisabled || config.isPending
        let busy = if config.isLoading then "true" else $"${requestPendingSignal}"
        let ready = if config.isLoading then "false" else $"!${requestPendingSignal}"
        let optionId (choice:SelectOption<_>) =
            $"{fieldId}-option-{choice.value |> config.encode |> ComponentHtml.optionToken}"
        let visibleOptions = $"Array.from(document.querySelectorAll('#{listboxId} [role=option]:not(:disabled)')).filter(item => item.style.display != 'none')"
        let firstVisibleOption = $"{visibleOptions}.at(0)"
        let synchronizeActive = $"(!document.getElementById(${activeSignal}) || document.getElementById(${activeSignal}).style.display == 'none') && (${activeSignal} = ({firstVisibleOption})?.id || '')"
        let optionMatches (choice:SelectOption<_>) =
            $"!${querySignal}.trim() || {ComponentHtml.javascriptString (choice.label.ToLowerInvariant())}.includes(${querySignal}.trim().toLowerCase())"
        let anyOptionMatches =
            match config.options with
            | [] -> "false"
            | options -> options |> List.map optionMatches |> String.concat " || "
        div {
            _id popupId
            _dataShow $"${openSignal}"
            _dataOn ("click", [ "outside" ], $"${openSignal} = false")
            _style "display:none"
            _class "absolute left-0 top-full z-30 mt-1 max-h-60 w-full overflow-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] p-1 shadow-lg ring-1 ring-[var(--fve-border)]"
            div {
                _id listboxId
                _role "listbox"
                _ariaLabelledby labelId
                _ariaBusy config.isLoading
                _dataAttr ("aria-busy", $"{busy} ? 'true' : null")
                _dataEffect $"{ready} && queueMicrotask(() => ({synchronizeActive}))"
                if config.error.IsNone then
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
                            match config.search with
                            | ComboboxSearch.Static -> _dataShow $"{ready} && ({optionMatches choice})"
                            | ComboboxSearch.Remote _ -> _dataShow ready
                            if choiceUnavailable |> not then
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
            }
            p {
                _role "status"
                _dataShow busy
                if config.isLoading |> not then _style "display:none"
                _class "flex items-center justify-center gap-2 px-3 py-4 text-center text-sm text-[var(--fve-muted-text)]"
                ComponentHtml.loadingGlyph ControlSize.Small
                config.loadingMessage
            }
            match config.error with
            | Some message ->
                div {
                    _dataShow ready
                    _class "grid justify-items-center gap-2 px-3 py-4 text-center"
                    p {
                        _role "alert"
                        _class "text-sm text-[var(--fve-critical-text)]"
                        message
                    }
                    match config.search with
                    | ComboboxSearch.Remote endpoint when unavailable |> not ->
                        button {
                            _type "button"
                            _dataIndicator requestPendingSignal
                            _dataOn ("click", $"{request endpoint}.then(() => document.getElementById('{fieldId}').focus())")
                            _class "inline-flex min-h-8 items-center justify-center rounded-[var(--fve-radius-control)] px-3 py-1.5 text-sm font-semibold text-[var(--fve-brand-text)] ring-1 ring-inset ring-[var(--fve-brand-ring)] hover:bg-[var(--fve-brand-subtle)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                            "Retry"
                        }
                    | _ -> ()
                }
            | None ->
                p {
                    _role "status"
                    _dataShow $"{ready} && !({anyOptionMatches})"
                    if config.options.IsEmpty |> not then _style "display:none"
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
        let requestPendingSignal = $"_{instanceId}_request_pending"
        let unavailable = config.isDisabled || config.isPending
        let busy = if config.isLoading || config.isPending then "true" else $"${requestPendingSignal}"
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
                $"evt.key == 'Escape' && ${openSignal} && (evt.preventDefault(), ${openSignal} = false)"
                $"evt.key == 'Tab' && (${openSignal} = false)" ]
        let synchronizeVisibleActive = $"${activeSignal} = ({firstVisibleOption})?.id || '', {scrollToActive}"
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        let inputClasses =
            ComponentHtml.classes [
                "min-h-[var(--fve-control-min-height)] w-full rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-sm text-[var(--fve-text)] ring-1 ring-inset outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-50"
                if config.isClearable || config.isPending || config.isLoading || (match config.search with ComboboxSearch.Remote _ -> true | _ -> false) then "pr-12"
                if config.validation.IsSome then "ring-[var(--fve-critical-ring)] focus-visible:ring-[var(--fve-critical-ring)]" else "ring-[var(--fve-border)] focus-visible:ring-[var(--fve-brand-ring)]" ]
        div {
            _class "relative grid gap-1.5"
            _dataSignals $"{{{openSignal}: false, {querySignal}: {ComponentHtml.javascriptString selectedLabel}, {valueSignal}: {ComponentHtml.javascriptString selectedValue}, {activeSignal}: {ComponentHtml.javascriptString initialActiveId}, {requestPendingSignal}: false}}"
            label {
                _id labelId
                _for fieldId
                _class (if config.labelVisuallyHidden then "sr-only" else "text-sm font-medium text-[var(--fve-text)]")
                config.label
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            div {
                _class "relative"
                input {
                    _id fieldId
                    _type "search"
                    _role "combobox"
                    _ariaHaspopup "listbox"
                    _ariaControls listboxId
                    _ariaExpanded false
                    _ariaAutocomplete "list"
                    _ariaDisabled unavailable
                    _ariaInvalid config.validation.IsSome
                    _disabled unavailable
                    if config.isLoading || config.isPending then _ariaBusy true
                    _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                    _dataAttr ("aria-busy", $"{busy} ? 'true' : null")
                    _dataAttr ("aria-activedescendant", $"${openSignal} && !({busy}) && document.getElementById(${activeSignal}) ? ${activeSignal} : null")
                    if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                    _autocomplete "off"
                    _placeholder (config.placeholder |> Option.defaultValue "Search options")
                    _dataBind querySignal
                    match config.search with
                    | ComboboxSearch.Remote _ -> _dataIndicator requestPendingSignal
                    | ComboboxSearch.Static -> ()
                    if unavailable |> not then
                        _dataOn ("click", [ "stop" ], $"${openSignal} = true; queueMicrotask(() => ((!document.getElementById(${activeSignal}) || document.getElementById(${activeSignal}).style.display == 'none') && ({synchronizeVisibleActive})))")
                        _dataOn ("keydown", keydown)
                        match config.search with
                        | ComboboxSearch.Static -> _dataOn ("input", $"${openSignal} = true; ${valueSignal} = ''; queueMicrotask(() => ({synchronizeVisibleActive}))")
                        | ComboboxSearch.Remote endpoint -> _dataOn ("input", [ "debounce.250ms" ], $"${openSignal} = true; ${valueSignal} = ''; ${activeSignal} = ''; {request endpoint}")
                    _class inputClasses
                    for attribute in ComponentHtml.safeAttributes [ "id"; "type"; "name"; "value"; "disabled"; "role"; "aria-haspopup"; "aria-controls"; "aria-expanded"; "aria-activedescendant"; "aria-autocomplete"; "aria-describedby"; "aria-disabled"; "aria-invalid"; "aria-busy"; "autocomplete"; "placeholder"; "data-bind:"; "data-indicator:"; "data-attr:"; "data-on:"; "class" ] config.attributes do attribute
                }
                if config.isClearable then
                    button {
                        _type "button"
                        _ariaLabel $"Clear {config.label}"
                        _disabled unavailable
                        _dataShow $"!({busy}) && (${querySignal}.length > 0 || ${valueSignal}.length > 0)"
                        if String.IsNullOrEmpty selectedLabel then _style "display:none"
                        match config.search with
                        | ComboboxSearch.Remote _ -> _dataIndicator requestPendingSignal
                        | ComboboxSearch.Static -> ()
                        if unavailable |> not then
                            let refresh =
                                match config.search with
                                | ComboboxSearch.Static -> ""
                                | ComboboxSearch.Remote endpoint -> $"; {request endpoint}"
                            _dataOn ("click", $"${querySignal} = ''; ${valueSignal} = ''; ${activeSignal} = ''; ${openSignal} = true{refresh}; document.getElementById('{fieldId}').focus()")
                        _class "absolute inset-y-0 right-0 inline-flex items-center justify-center px-3 text-[var(--fve-muted-text)] hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50"
                        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path d="M5.47 5.47a.75.75 0 0 1 1.06 0L10 8.94l3.47-3.47a.75.75 0 1 1 1.06 1.06L11.06 10l3.47 3.47a.75.75 0 1 1-1.06 1.06L10 11.06l-3.47 3.47a.75.75 0 0 1-1.06-1.06L8.94 10 5.47 6.53a.75.75 0 0 1 0-1.06Z"/></svg>"""
                    }
                span {
                    _dataShow busy
                    if config.isLoading |> not && config.isPending |> not then _style "display:none"
                    _class "pointer-events-none absolute inset-y-0 right-0 inline-flex items-center px-3"
                    ComponentHtml.loadingGlyph ControlSize.Small
                }
            }
            input {
                _type "hidden"
                _name config.name
                _value selectedValue
                _disabled unavailable
                _dataBind valueSignal
            }
            renderOptions config
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
