namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar

[<RequireQualifiedAccess>]
type Tone =
    | Neutral
    | Brand
    | Positive
    | Warning
    | Critical
    | Informative

[<RequireQualifiedAccess>]
type ControlSize =
    | Small
    | Medium
    | Large

[<RequireQualifiedAccess>]
type Radius =
    | None
    | Medium
    | Large
    | Full

[<RequireQualifiedAccess>]
type Density =
    | Compact
    | Comfortable

[<NoEquality; NoComparison>]
type ComponentsTheme =
    private
        { paletteClass:string
          radiusClass:string
          densityClass:string }

[<RequireQualifiedAccess>]
module ComponentsTheme =
    let sky =
        { paletteClass = "fve-theme-sky"
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable" }

    let emerald =
        { paletteClass = "fve-theme-emerald"
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable" }

    let withRadius radius theme =
        let radiusClass =
            match radius with
            | Radius.None -> "fve-radius-none"
            | Radius.Medium -> "fve-radius-medium"
            | Radius.Large -> "fve-radius-large"
            | Radius.Full -> "fve-radius-full"
        { theme with radiusClass = radiusClass }

    let withDensity density theme =
        let densityClass =
            match density with
            | Density.Compact -> "fve-density-compact"
            | Density.Comfortable -> "fve-density-comfortable"
        { theme with densityClass = densityClass }

    let className theme =
        [ "fve-components"; theme.paletteClass; theme.radiusClass; theme.densityClass ]
        |> String.concat " "

    let attributes theme =
        [ _class (className theme) ]

module private ContractHtml =
    let classes values = values |> List.filter (String.IsNullOrWhiteSpace >> not) |> String.concat " "

    let safeAttributes reservedNames attributes =
        attributes
        |> List.filter (fun attribute ->
            reservedNames
            |> List.exists (fun reservedName ->
                String.Equals(attribute.Name, reservedName, StringComparison.OrdinalIgnoreCase)
                || (reservedName.EndsWith(':') && attribute.Name.StartsWith(reservedName, StringComparison.OrdinalIgnoreCase)))
            |> not)

    let javascriptString value = System.Text.Json.JsonSerializer.Serialize(value)

    let signalToken value =
        let token = Regex.Replace(value, "[^A-Za-z0-9]+", "_").Trim('_')
        if String.IsNullOrEmpty token then "component" else token.ToLowerInvariant()

    let optionToken (value:string) =
        value
        |> Encoding.UTF8.GetBytes
        |> Array.map (fun character -> character.ToString("x2"))
        |> String.concat ""
        |> (+) "v"

    let toneClasses = function
        | Tone.Neutral -> "bg-[var(--fve-neutral-subtle)] text-[var(--fve-neutral-text)] ring-[var(--fve-border)]"
        | Tone.Brand -> "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)] ring-[var(--fve-brand-ring)]"
        | Tone.Positive -> "bg-[var(--fve-positive-subtle)] text-[var(--fve-positive-text)] ring-[var(--fve-positive-ring)]"
        | Tone.Warning -> "bg-[var(--fve-warning-subtle)] text-[var(--fve-warning-text)] ring-[var(--fve-warning-ring)]"
        | Tone.Critical -> "bg-[var(--fve-critical-subtle)] text-[var(--fve-critical-text)] ring-[var(--fve-critical-ring)]"
        | Tone.Informative -> "bg-[var(--fve-info-subtle)] text-[var(--fve-info-text)] ring-[var(--fve-info-ring)]"

    let sizeClasses = function
        | ControlSize.Small -> "min-h-[calc(var(--fve-control-min-height)-0.25rem)] px-2.5 py-[var(--fve-control-padding-block)] text-xs"
        | ControlSize.Medium -> "min-h-[var(--fve-control-min-height)] px-3 py-[var(--fve-control-padding-block)] text-sm"
        | ControlSize.Large -> "min-h-[calc(var(--fve-control-min-height)+0.5rem)] px-4 py-[var(--fve-control-padding-block)] text-base"

[<RequireQualifiedAccess>]
type ButtonVariant =
    | Primary
    | Secondary
    | Ghost
    | Destructive

[<RequireQualifiedAccess>]
type ButtonType =
    | Button
    | Submit
    | Reset

[<NoEquality; NoComparison>]
type ButtonConfig =
    private
        { label:string
          variant:ButtonVariant
          size:ControlSize
          buttonType:ButtonType
          leading:HtmlElement option
          trailing:HtmlElement option
          disabled:bool
          className:string option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Button =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A button label is required."
        { label = label
          variant = ButtonVariant.Secondary
          size = ControlSize.Medium
          buttonType = ButtonType.Button
          leading = None
          trailing = None
          disabled = false
          className = None
          attributes = [] }

    let withVariant variant config = { config with variant = variant }
    let withSize size config = { config with size = size }
    let asSubmit config = { config with buttonType = ButtonType.Submit }
    let withLeading leading config = { config with leading = Some leading }
    let withTrailing trailing config = { config with trailing = Some trailing }
    let disabled config = { config with disabled = true }
    let withClass className config = { config with className = Some className }
    let withAttributes attributes config = { config with attributes = attributes }

    let render config =
        let variantClasses =
            match config.variant with
            | ButtonVariant.Primary -> "bg-[var(--fve-brand-solid)] text-white hover:bg-[var(--fve-brand-hover)] focus-visible:ring-[var(--fve-brand-ring)]"
            | ButtonVariant.Secondary -> "bg-[var(--fve-surface)] text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:ring-[var(--fve-brand-ring)]"
            | ButtonVariant.Ghost -> "bg-transparent text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:ring-[var(--fve-brand-ring)]"
            | ButtonVariant.Destructive -> "bg-[var(--fve-critical-solid)] text-white hover:bg-[var(--fve-critical-hover)] focus-visible:ring-[var(--fve-critical-ring)]"
        let buttonType =
            match config.buttonType with
            | ButtonType.Button -> "button"
            | ButtonType.Submit -> "submit"
            | ButtonType.Reset -> "reset"
        button {
            _type buttonType
            _disabled config.disabled
            _class (
                ContractHtml.classes [
                    "inline-flex items-center justify-center gap-2 rounded-[var(--fve-radius-control)] font-semibold shadow-sm outline-none transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50"
                    ContractHtml.sizeClasses config.size
                    variantClasses
                    config.className |> Option.defaultValue "" ])
            for attribute in ContractHtml.safeAttributes [ "type"; "disabled"; "class" ] config.attributes do attribute
            config.leading |> Option.defaultValue empty
            config.label
            config.trailing |> Option.defaultValue empty
        }

    let primary label = create label |> withVariant ButtonVariant.Primary |> render
    let secondary label = create label |> withVariant ButtonVariant.Secondary |> render

[<NoEquality; NoComparison>]
type StatusConfig =
    private
        { label:string
          tone:Tone
          leading:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Status =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A status label is required."
        { label = label; tone = Tone.Neutral; leading = None; attributes = [] }

    let withTone tone (config:StatusConfig) = { config with tone = tone }
    let withLeading leading (config:StatusConfig) = { config with leading = Some leading }
    let withAttributes attributes (config:StatusConfig) = { config with attributes = attributes }

    let render config =
        span {
            _class (ContractHtml.classes [ "inline-flex items-center gap-1.5 rounded-full px-2 py-1 text-xs font-medium ring-1 ring-inset"; ContractHtml.toneClasses config.tone ])
            for attribute in ContractHtml.safeAttributes [ "class" ] config.attributes do attribute
            config.leading |> Option.defaultValue empty
            config.label
        }

    let positive label = create label |> withTone Tone.Positive |> render
    let warning label = create label |> withTone Tone.Warning |> render

[<NoEquality; NoComparison>]
type TableColumn<'row> =
    private
        { heading:string
          cell:'row -> HtmlElement
          headerClass:string option
          cellClass:string option }

[<NoEquality; NoComparison>]
type TableConfig<'row> =
    private
        { caption:string
          columns:TableColumn<'row> list
          rows:'row list
          emptyState:HtmlElement
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Table =
    let column heading cell =
        if String.IsNullOrWhiteSpace heading then invalidArg (nameof heading) "A column heading is required."
        { heading = heading; cell = cell; headerClass = None; cellClass = None }

    let alignEnd (column:TableColumn<'row>) =
        { column with headerClass = Some "text-right"; cellClass = Some "text-right" }

    let create caption columns rows =
        if String.IsNullOrWhiteSpace caption then invalidArg (nameof caption) "A table caption is required."
        if List.isEmpty columns then invalidArg (nameof columns) "At least one table column is required."
        { caption = caption
          columns = columns
          rows = rows
          emptyState = div { _class "p-6 text-center text-sm text-[var(--fve-muted-text)]"; "No records" }
          attributes = [] }

    let withEmptyState emptyState (config:TableConfig<'row>) = { config with emptyState = emptyState }
    let withAttributes attributes (config:TableConfig<'row>) = { config with attributes = attributes }

    let render config =
        div {
            _class "overflow-hidden rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] ring-1 ring-[var(--fve-border)]"
            if List.isEmpty config.rows then
                config.emptyState
            else
                table {
                    _class "min-w-full divide-y divide-[var(--fve-border)] text-left text-sm"
                    for attribute in ContractHtml.safeAttributes [ "class" ] config.attributes do attribute
                    caption { _class "sr-only"; config.caption }
                    thead {
                        _class "bg-[var(--fve-surface-subtle)] text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"
                        tr {
                            for column in config.columns do
                                th {
                                    _scope "col"
                                    _class (ContractHtml.classes [ "px-4 py-3"; column.headerClass |> Option.defaultValue "" ])
                                    column.heading
                                }
                        }
                    }
                    tbody {
                        _class "divide-y divide-[var(--fve-border)] text-[var(--fve-text)]"
                        for row in config.rows do
                            tr {
                                _class "hover:bg-[var(--fve-surface-hover)]"
                                for column in config.columns do
                                    td {
                                        _class (ContractHtml.classes [ "px-4 py-3"; column.cellClass |> Option.defaultValue "" ])
                                        column.cell row
                                    }
                            }
                    }
                }
        }

[<NoEquality; NoComparison>]
type SelectOption<'value> =
    private
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
          attributes = [] }

    let withSelected selected (config:SelectConfig<'value>) = { config with selected = Some selected }
    let withId id (config:SelectConfig<'value>) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable component ID is required."
        { config with id = Some id }
    let withVisuallyHiddenLabel (config:SelectConfig<'value>) = { config with labelVisuallyHidden = true }
    let withDescription description (config:SelectConfig<'value>) = { config with description = Some description }
    let withPlaceholder placeholder (config:SelectConfig<'value>) = { config with placeholder = Some placeholder }
    let withValidation message (config:SelectConfig<'value>) = { config with validation = Some message }
    let withAttributes attributes (config:SelectConfig<'value>) = { config with attributes = attributes }

    let render config =
        let instanceId = config.id |> Option.defaultValue config.name |> ContractHtml.signalToken
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
        let optionId (choice:SelectOption<_>) =
            $"{fieldId}-option-{choice.value |> config.encode |> ContractHtml.optionToken}"
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
        let moveActive offset =
            let missingIndex = if offset > 0 then -1 else 0
            $"evt.preventDefault(), ${openSignal} = true, {enabledOptions}.length && (${activeSignal} = {enabledOptions}.at((({currentIndex} < 0 ? {missingIndex} : {currentIndex}) + {offset} + {enabledOptions}.length) %% {enabledOptions}.length)?.id || '', {scrollToActive})"
        let searchText = $"(Array.from(${typeaheadSignal}).every(character => character == ${typeaheadSignal}[0]) ? ${typeaheadSignal}[0] : ${typeaheadSignal})"
        let startIndex = $"Math.max(0, {currentIndex} + 1)"
        let orderedOptions = $"{enabledOptions}.slice({startIndex}).concat({enabledOptions}.slice(0, {startIndex}))"
        let typeaheadMatch = $"{orderedOptions}.find(item => item.dataset.fveOptionLabel.startsWith({searchText}))"
        let typeahead =
            $"evt.key.length == 1 && evt.key != ' ' && (evt.preventDefault(), ${typeaheadSignal} = Date.now() - ${typeaheadTimeSignal} > 700 ? evt.key.toLowerCase() : ${typeaheadSignal} + evt.key.toLowerCase(), ${typeaheadTimeSignal} = Date.now(), ${openSignal} = true, ${activeSignal} = ({typeaheadMatch})?.id || ${activeSignal}, {scrollToActive})"
        let selectedOrFirst = $"{selectedOption} || {firstOption}"
        let keydown =
            String.concat "; " [
                $"evt.key == 'ArrowDown' && ({moveActive 1})"
                $"evt.key == 'ArrowUp' && ({moveActive -1})"
                $"evt.key == 'Home' && (evt.preventDefault(), ${openSignal} = true, {activate firstOption})"
                $"evt.key == 'End' && (evt.preventDefault(), ${openSignal} = true, {activate lastOption})"
                $"(evt.key == 'Enter' || evt.key == ' ') && (evt.preventDefault(), ${openSignal} && ${activeSignal} ? document.getElementById(${activeSignal})?.click() : (${openSignal} = true, {activate selectedOrFirst}))"
                $"evt.key == 'Escape' && (evt.preventDefault(), ${openSignal} = false, ${activeSignal} = ({selectedOrFirst})?.id || '', ${typeaheadSignal} = '')"
                $"evt.key == 'Tab' && (${openSignal} = false, ${activeSignal} = ({selectedOrFirst})?.id || '', ${typeaheadSignal} = '')"
                typeahead ]
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        div {
            _class "relative grid gap-1.5"
            _dataSignals $"{{{openSignal}: false, {valueSignal}: {ContractHtml.javascriptString selectedValue}, {labelSignal}: {ContractHtml.javascriptString selectedLabel}, {activeSignal}: {ContractHtml.javascriptString initialActiveId}, {typeaheadSignal}: '', {typeaheadTimeSignal}: 0}}"
            label {
                _id labelId
                _for triggerId
                _class (if config.labelVisuallyHidden then "sr-only" else "text-sm font-medium text-[var(--fve-text)]")
                config.label
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            input {
                _type "hidden"
                _name config.name
                _value selectedValue
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
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                _dataAttr ("aria-activedescendant", $"${openSignal} && document.getElementById(${activeSignal}) ? ${activeSignal} : null")
                if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                _ariaInvalid config.validation.IsSome
                _dataOn ("click", [ "stop" ], $"document.getElementById('{triggerId}').focus(); ${openSignal} = !${openSignal}; ${openSignal} && queueMicrotask(() => (!document.getElementById(${activeSignal}) && (${activeSignal} = ({selectedOrFirst})?.id || ''), document.getElementById(${activeSignal})?.scrollIntoView({{block: 'nearest'}})))")
                _dataOn ("keydown", keydown)
                _class "group flex min-h-[var(--fve-control-min-height)] w-full items-center justify-between gap-3 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-left text-sm text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                for attribute in ContractHtml.safeAttributes [ "id"; "type"; "name"; "value"; "role"; "aria-haspopup"; "aria-controls"; "aria-labelledby"; "aria-expanded"; "aria-activedescendant"; "aria-describedby"; "aria-invalid"; "data-bind:"; "data-attr:"; "data-on:"; "class" ] config.attributes do attribute
                span {
                    _class "min-w-0 truncate"
                    _dataText $"${labelSignal}"
                    selectedLabel
                }
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
                    button {
                        _id choiceId
                        _type "button"
                        _role "option"
                        _tabindex -1
                        _disabled choice.disabled
                        _ariaDisabled choice.disabled
                        _ariaSelected (config.selected = Some choice.value)
                        _attr ("data-fve-option-label", choice.label.ToLowerInvariant())
                        _dataAttr ("aria-selected", $"${valueSignal} == {ContractHtml.javascriptString encodedValue} ? 'true' : 'false'")
                        _dataAttr ("data-active", $"${activeSignal} == {ContractHtml.javascriptString choiceId} ? 'true' : null")
                        if choice.disabled |> not then
                            _dataOn ("mousedown", "evt.preventDefault()")
                            _dataOn ("click", $"${activeSignal} = {ContractHtml.javascriptString choiceId}; ${valueSignal} = {ContractHtml.javascriptString encodedValue}; ${labelSignal} = {ContractHtml.javascriptString choice.label}; ${typeaheadSignal} = ''; ${openSignal} = false; document.getElementById('{triggerId}').focus()")
                        _class "flex w-full items-center justify-between gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-left text-sm text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] focus:bg-[var(--fve-surface-hover)] data-[active=true]:bg-[var(--fve-surface-hover)] data-[active=true]:ring-2 data-[active=true]:ring-inset data-[active=true]:ring-[var(--fve-brand-ring)] aria-selected:bg-[var(--fve-brand-subtle)] aria-selected:text-[var(--fve-brand-text)] disabled:cursor-not-allowed disabled:opacity-50"
                        span { _class "min-w-0 truncate"; choice.label }
                        span {
                            _ariaHidden "true"
                            _dataShow $"${valueSignal} == {ContractHtml.javascriptString encodedValue}"
                            _style "display:none"
                            _class "shrink-0 font-semibold text-[var(--fve-brand-text)]"
                            "✓"
                        }
                    }
            }
            match config.validation with
            | Some message -> p { _id validationId; _class "text-sm text-[var(--fve-critical-text)]"; message }
            | None -> ()
        }

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
        let instanceId = config.id |> Option.defaultValue config.name |> ContractHtml.signalToken
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
            $"{fieldId}-option-{choice.value |> config.encode |> ContractHtml.optionToken}"
        let visibleOptions = $"Array.from(document.querySelectorAll('#{listboxId} [role=option]:not(:disabled)')).filter(item => item.style.display != 'none')"
        let firstVisibleOption = $"{visibleOptions}.at(0)"
        let optionSignature =
            config.options
            |> List.map (fun choice -> config.encode choice.value)
            |> String.concat "\u001f"
            |> ContractHtml.javascriptString
        let synchronizeActive = $"(!document.getElementById(${activeSignal}) || document.getElementById(${activeSignal}).style.display == 'none') && (${activeSignal} = ({firstVisibleOption})?.id || '')"
        let optionMatches (choice:SelectOption<_>) =
            $"!${querySignal}.trim() || {ContractHtml.javascriptString (choice.label.ToLowerInvariant())}.includes(${querySignal}.trim().toLowerCase())"
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
                    _dataAttr ("aria-selected", $"${valueSignal} == {ContractHtml.javascriptString encodedValue} ? 'true' : 'false'")
                    _dataAttr ("data-active", $"${activeSignal} == {ContractHtml.javascriptString choiceId} ? 'true' : null")
                    match config.search with
                    | ComboboxSearch.Static -> _dataShow (optionMatches choice)
                    | ComboboxSearch.Remote _ -> ()
                    if choice.disabled |> not then
                        _dataOn ("mousedown", "evt.preventDefault()")
                        _dataOn ("click", $"${activeSignal} = {ContractHtml.javascriptString choiceId}; ${valueSignal} = {ContractHtml.javascriptString encodedValue}; ${querySignal} = {ContractHtml.javascriptString choice.label}; ${openSignal} = false; document.getElementById('{fieldId}').focus()")
                    _class "flex w-full items-center justify-between gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-left text-sm text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] focus:bg-[var(--fve-surface-hover)] data-[active=true]:bg-[var(--fve-surface-hover)] data-[active=true]:ring-2 data-[active=true]:ring-inset data-[active=true]:ring-[var(--fve-brand-ring)] aria-selected:bg-[var(--fve-brand-subtle)] aria-selected:text-[var(--fve-brand-text)] disabled:cursor-not-allowed disabled:opacity-50"
                    span { _class "min-w-0 truncate"; choice.label }
                    span {
                        _ariaHidden "true"
                        _dataShow $"${valueSignal} == {ContractHtml.javascriptString encodedValue}"
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
        let instanceId = config.id |> Option.defaultValue config.name |> ContractHtml.signalToken
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
            $"{fieldId}-option-{choice.value |> config.encode |> ContractHtml.optionToken}"
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
            _dataSignals $"{{{openSignal}: false, {querySignal}: {ContractHtml.javascriptString selectedLabel}, {valueSignal}: {ContractHtml.javascriptString selectedValue}, {activeSignal}: {ContractHtml.javascriptString initialActiveId}}}"
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
                | ComboboxSearch.Remote endpoint -> _dataOn ("input", [ "debounce.250ms" ], $"${openSignal} = true; ${valueSignal} = ''; ${activeSignal} = ''; @get({ContractHtml.javascriptString endpoint})")
                _class "min-h-[var(--fve-control-min-height)] w-full rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-sm text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                for attribute in ContractHtml.safeAttributes [ "id"; "type"; "name"; "value"; "role"; "aria-controls"; "aria-expanded"; "aria-activedescendant"; "aria-autocomplete"; "aria-describedby"; "aria-invalid"; "autocomplete"; "placeholder"; "data-bind:"; "data-attr:"; "data-on:"; "class" ] config.attributes do attribute
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
        let token = ContractHtml.signalToken config.name
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
        let token = ContractHtml.signalToken config.name
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

[<NoEquality; NoComparison>]
type ToggleButtonConfig =
    private
        { id:string
          label:string
          isPressed:bool
          isDisabled:bool }

[<RequireQualifiedAccess>]
module ToggleButton =
    let create id label =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable toggle button ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A toggle button label is required."
        { id = id; label = label; isPressed = false; isDisabled = false }

    let pressed (config:ToggleButtonConfig) = { config with isPressed = true }
    let disabled (config:ToggleButtonConfig) = { config with isDisabled = true }

    let render (config:ToggleButtonConfig) =
        let signal = $"_{ContractHtml.signalToken config.id}_pressed"
        let initialValue = if config.isPressed then "true" else "false"
        button {
            _id config.id
            _type "button"
            _disabled config.isDisabled
            _ariaPressed config.isPressed
            _dataSignals $"{{{signal}: {initialValue}}}"
            _dataAttr ("aria-pressed", $"${signal} ? 'true' : 'false'")
            _dataOn ("click", $"${signal} = !${signal}")
            _class "inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)] aria-pressed:bg-[var(--fve-brand-subtle)] aria-pressed:text-[var(--fve-brand-text)] disabled:pointer-events-none disabled:opacity-50"
            config.label
        }

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
        let token = ContractHtml.signalToken config.name
        let groupId = $"fve-radio-{token}"
        let descriptionId = $"{groupId}-description"
        let valueSignal = $"{token}_value"
        let selectedValue = config.selected |> Option.map config.encode |> Option.defaultValue ""
        fieldset {
            _dataSignals $"{{{valueSignal}: {ContractHtml.javascriptString selectedValue}}}"
            legend { _class "text-sm font-medium text-[var(--fve-text)]"; config.label }
            match config.description with
            | Some description -> p { _id descriptionId; _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            div {
                _class "mt-2 grid gap-2"
                for choice in config.options do
                    let encodedValue = config.encode choice.value
                    let optionId = $"{groupId}-option-{ContractHtml.optionToken encodedValue}"
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
                                _dataShow $"${valueSignal} == {ContractHtml.javascriptString encodedValue}"
                                _style "display:none"
                                _class "size-2.5 rounded-full bg-[var(--fve-brand-solid)]"
                            }
                        }
                        span { _class "font-medium"; choice.label }
                    }
            }
        }

[<RequireQualifiedAccess>]
type MenuTone =
    | Default
    | Destructive

[<NoEquality; NoComparison>]
type MenuItem<'destination> =
    private
        | Link of label:string * destination:'destination
        | Action of label:string * datastarExpression:string * tone:MenuTone
        | Separator

[<NoEquality; NoComparison>]
type DropdownMenuConfig<'destination> =
    private
        { id:string
          label:string
          items:MenuItem<'destination> list }

[<RequireQualifiedAccess>]
module MenuItem =
    let link destination label = Link(label, destination)
    let action datastarExpression label = Action(label, datastarExpression, MenuTone.Default)
    let destructiveAction datastarExpression label = Action(label, datastarExpression, MenuTone.Destructive)
    let separator<'destination> : MenuItem<'destination> = Separator

[<RequireQualifiedAccess>]
module DropdownMenu =
    let create id label items =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable menu ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu label is required."
        { id = id; label = label; items = items }

    let render resolve config =
        let openSignal = $"_{ContractHtml.signalToken config.id}_open"
        let triggerId = $"{config.id}-trigger"
        let menuId = $"{config.id}-menu"
        let firstItem = $"document.querySelector('#{menuId} [role=menuitem]:not(:disabled)')"
        let lastItem = $"Array.from(document.querySelectorAll('#{menuId} [role=menuitem]:not(:disabled)')).at(-1)"
        let enabledItems = "Array.from(el.querySelectorAll('[role=menuitem]:not(:disabled)'))"
        let currentIndex = $"{enabledItems}.indexOf(document.activeElement)"
        let moveNext = $"evt.preventDefault(), {enabledItems}.at(({currentIndex} + 1) %% {enabledItems}.length)?.focus()"
        let movePrevious = $"evt.preventDefault(), {enabledItems}.at(({currentIndex} - 1 + {enabledItems}.length) %% {enabledItems}.length)?.focus()"
        div {
            _class "relative inline-flex"
            _dataSignals $"{{{openSignal}: false}}"
            button {
                _id triggerId
                _type "button"
                _ariaHaspopup "menu"
                _ariaExpanded false
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                _ariaControls menuId
                _dataOn ("click", [ "stop" ], $"${openSignal} = !${openSignal}; ${openSignal} && queueMicrotask(() => {firstItem}?.focus())")
                _dataOn ("keydown", $"evt.key == 'ArrowDown' && (evt.preventDefault(), ${openSignal} = true, queueMicrotask(() => {firstItem}?.focus())); evt.key == 'ArrowUp' && (evt.preventDefault(), ${openSignal} = true, queueMicrotask(() => {lastItem}?.focus()))")
                _class "inline-flex min-h-[var(--fve-control-min-height)] items-center rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                config.label
            }
            div {
                _id menuId
                _role "menu"
                _ariaLabel config.label
                _dataShow $"${openSignal}"
                _dataOn ("click", [ "outside" ], $"${openSignal} = false")
                _dataOn ("keydown", $"evt.key == 'Escape' && (evt.preventDefault(), ${openSignal} = false, document.getElementById('{triggerId}').focus()); evt.key == 'ArrowDown' && ({moveNext}); evt.key == 'ArrowUp' && ({movePrevious}); evt.key == 'Home' && (evt.preventDefault(), {firstItem}?.focus()); evt.key == 'End' && (evt.preventDefault(), {lastItem}?.focus()); evt.key == 'Tab' && (${openSignal} = false)")
                _style "display:none"
                _class "absolute right-0 top-full z-30 mt-2 min-w-48 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] p-1 shadow-lg ring-1 ring-[var(--fve-border)]"
                for item in config.items do
                    match item with
                    | Link(label, destination) ->
                        a {
                            _href (resolve destination)
                            _role "menuitem"
                            _tabindex -1
                            _dataOn ("click", $"${openSignal} = false")
                            _class "block rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-sm text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] focus:bg-[var(--fve-surface-hover)]"
                            label
                        }
                    | Action(label, expression, tone) ->
                        button {
                            _type "button"
                            _role "menuitem"
                            _tabindex -1
                            _dataOn ("click", $"${openSignal} = false; {expression}")
                            _class (
                                match tone with
                                | MenuTone.Default -> "block w-full rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-left text-sm text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] focus:bg-[var(--fve-surface-hover)]"
                                | MenuTone.Destructive -> "block w-full rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-left text-sm text-[var(--fve-critical-text)] outline-none hover:bg-[var(--fve-critical-subtle)] focus:bg-[var(--fve-critical-subtle)]")
                            label
                        }
                    | Separator -> div { _role "separator"; _class "my-1 h-px bg-[var(--fve-border)]" }
            }
        }

[<NoEquality; NoComparison>]
type DialogConfig =
    private
        { id:string
          title:string
          body:HtmlElement
          description:string option
          footer:HtmlElement option
          initialFocusId:string option }

[<RequireQualifiedAccess>]
module Dialog =
    let create id title body =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable dialog ID is required."
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A dialog title is required."
        { id = id; title = title; body = body; description = None; footer = None; initialFocusId = None }

    let withDescription description (config:DialogConfig) = { config with description = Some description }
    let withFooter footer (config:DialogConfig) = { config with footer = Some footer }
    let withInitialFocus initialFocusId (config:DialogConfig) = { config with initialFocusId = Some initialFocusId }

    let trigger label (config:DialogConfig) =
        let dialogId = ContractHtml.javascriptString config.id
        let openExpression =
            match config.initialFocusId with
            | Some initialFocusId ->
                let focusId = ContractHtml.javascriptString initialFocusId
                $"document.getElementById({dialogId}).showModal(); queueMicrotask(() => document.getElementById({focusId}).focus())"
            | None -> $"document.getElementById({dialogId}).showModal()"
        Button.create label
        |> Button.withAttributes [ _id $"{config.id}-trigger"; _ariaHaspopup "dialog"; _ariaControls config.id; _dataOn ("click", openExpression) ]
        |> Button.render

    let closeButton label (config:DialogConfig) =
        let dialogId = ContractHtml.javascriptString config.id
        Button.create label
        |> Button.withAttributes [ _id $"{config.id}-close"; _dataOn ("click", $"document.getElementById({dialogId}).close()") ]
        |> Button.render

    let render config =
        let titleId = $"{config.id}-title"
        let descriptionId = $"{config.id}-description"
        let triggerId = ContractHtml.javascriptString $"{config.id}-trigger"
        dialog {
            _id config.id
            _ariaLabelledby titleId
            if config.description.IsSome then _ariaDescribedby descriptionId
            _dataOn ("close", $"document.getElementById({triggerId}).focus()")
            _class "m-auto w-[min(32rem,calc(100%-2rem))] rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-slate-950/50"
            div {
                _class "p-6"
                h2 { _id titleId; _class "text-lg font-semibold"; config.title }
                match config.description with
                | Some description -> p { _id descriptionId; _class "mt-2 text-sm text-[var(--fve-muted-text)]"; description }
                | None -> ()
                div { _class "mt-4"; config.body }
                match config.footer with
                | Some footer -> div { _class "mt-6 flex justify-end gap-3"; footer }
                | None -> ()
            }
        }

[<NoEquality; NoComparison>]
type CollectionConfig =
    private
        { title:string
          description:string option
          actions:HtmlElement option
          toolbar:HtmlElement option
          content:HtmlElement }

[<RequireQualifiedAccess>]
module Collection =
    let create title content =
        { title = title; description = None; actions = None; toolbar = None; content = content }

    let withDescription description (config:CollectionConfig) = { config with description = Some description }
    let withActions actions (config:CollectionConfig) = { config with actions = Some actions }
    let withToolbar toolbar (config:CollectionConfig) = { config with toolbar = Some toolbar }

    let render config =
        section {
            _class "grid gap-6"
            header {
                _class "flex flex-wrap items-start justify-between gap-4"
                div {
                    h1 { _class "text-2xl font-semibold tracking-tight text-[var(--fve-text)]"; config.title }
                    match config.description with
                    | Some description -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                    | None -> ()
                }
                config.actions |> Option.defaultValue empty
            }
            config.toolbar |> Option.defaultValue empty
            config.content
        }

[<NoEquality; NoComparison>]
type DetailConfig =
    private
        { title:string
          metadata:HtmlElement option
          actions:HtmlElement option
          sections:HtmlElement list }

[<RequireQualifiedAccess>]
module Detail =
    let create title sections = { title = title; metadata = None; actions = None; sections = sections }
    let withMetadata metadata (config:DetailConfig) = { config with metadata = Some metadata }
    let withActions actions (config:DetailConfig) = { config with actions = Some actions }

    let render config =
        article {
            _class "grid gap-6"
            header {
                _class "flex flex-wrap items-start justify-between gap-4 border-b border-[var(--fve-border)] pb-5"
                div {
                    h1 { _class "text-2xl font-semibold tracking-tight text-[var(--fve-text)]"; config.title }
                    config.metadata |> Option.defaultValue empty
                }
                config.actions |> Option.defaultValue empty
            }
            for detailSection in config.sections do
                section { _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-5 ring-1 ring-[var(--fve-border)]"; detailSection }
        }

[<NoEquality; NoComparison>]
type NavigationItem<'destination> =
    private
        { label:string
          destination:'destination }

[<NoEquality; NoComparison>]
type AppShellConfig<'destination when 'destination:equality> =
    private
        { productName:string
          current:'destination
          navigation:NavigationItem<'destination> list
          content:HtmlElement
          breadcrumbs:NavigationItem<'destination> list
          accountMenu:HtmlElement option
          theme:ComponentsTheme }

[<RequireQualifiedAccess>]
module NavigationItem =
    let create destination label = { label = label; destination = destination }

[<RequireQualifiedAccess>]
module AppShell =
    let create productName current navigation content =
        if String.IsNullOrWhiteSpace productName then invalidArg (nameof productName) "A product name is required."
        { productName = productName
          current = current
          navigation = navigation
          content = content
          breadcrumbs = []
          accountMenu = None
          theme = ComponentsTheme.sky }

    let withBreadcrumbs breadcrumbs (config:AppShellConfig<'destination>) = { config with breadcrumbs = breadcrumbs }
    let withAccountMenu accountMenu (config:AppShellConfig<'destination>) = { config with accountMenu = Some accountMenu }
    let withTheme theme (config:AppShellConfig<'destination>) = { config with theme = theme }

    let render resolve config =
        div {
            _class (ContractHtml.classes [ ComponentsTheme.className config.theme; "grid min-h-[36rem] bg-[var(--fve-page)] text-[var(--fve-text)] lg:grid-cols-[16rem_1fr]" ])
            aside {
                _class "hidden border-r border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 lg:block"
                strong { _class "block px-3 py-[var(--fve-control-padding-block)] text-base"; config.productName }
                nav {
                    _ariaLabel "Primary"
                    _class "mt-5 grid gap-1"
                    for item in config.navigation do
                        a {
                            _href (resolve item.destination)
                            if item.destination = config.current then _ariaCurrent "page"
                            _class (
                                if item.destination = config.current then
                                    "rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-subtle)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-brand-text)]"
                                else
                                    "rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-sm text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)]")
                            item.label
                        }
                }
            }
            div {
                _class "min-w-0"
                header {
                    _class "flex min-h-16 items-center justify-between border-b border-[var(--fve-border)] bg-[var(--fve-surface)] px-4 sm:px-6"
                    nav {
                        _ariaLabel "Breadcrumb"
                        _class "flex items-center gap-2 text-sm text-[var(--fve-muted-text)]"
                        for breadcrumb in config.breadcrumbs do
                            a { _href (resolve breadcrumb.destination); _class "hover:text-[var(--fve-text)]"; breadcrumb.label }
                    }
                    config.accountMenu |> Option.defaultValue empty
                }
                main { _class "mx-auto max-w-7xl p-4 sm:p-6 lg:p-8"; config.content }
            }
        }
