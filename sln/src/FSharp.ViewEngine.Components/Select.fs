namespace FSharp.ViewEngine.Components.Primitives

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

[<RequireQualifiedAccess>]
type SelectSearch =
    | Static
    | Remote of endpoint:string

[<NoEquality; NoComparison>]
type SelectConfig<'value, 'mode when 'value:equality> =
    private
        { mode:'mode
          isMultiple:bool
          selectedMany:SelectOption<'value> list
          search:SelectSearch option
          query:string option
          emptyMessage:string
          loadingMessage:string
          error:string option
          isLoading:bool
          name:string
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
          size:ControlSize option
          attributes:HtmlAttribute list }

type SelectConfig<'value when 'value:equality> = SelectConfig<'value, SingleSelection>

[<RequireQualifiedAccess>]
module Select =
    let option value label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An option label is required."
        { value = value; label = label; disabled = false }

    let disable (option:SelectOption<'value>) = { option with disabled = true }

    let create name label encode options : SelectConfig<'value> =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible label is required."
        ChoiceSelection.validateKeys (fun choice -> encode choice.value) options
        { mode = SingleSelection
          isMultiple = false
          selectedMany = []
          search = None
          query = None
          emptyMessage = "No matching options"
          loadingMessage = "Loading options"
          error = None
          isLoading = false
          name = name
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
          size = None
          attributes = [] }

    let withSelected selected (config:SelectConfig<'value>) = { config with selected = Some selected }
    let multiple (config:SelectConfig<'value>) : SelectConfig<'value, MultipleSelection> =
        { mode = MultipleSelection
          isMultiple = true
          selectedMany = config.options |> List.filter (fun choice -> Some choice.value = config.selected)
          search = config.search
          query = config.query
          emptyMessage = config.emptyMessage
          loadingMessage = config.loadingMessage
          error = config.error
          isLoading = config.isLoading
          name = config.name
          id = config.id
          encode = config.encode
          options = config.options
          selected = None
          label = config.label
          labelVisuallyHidden = config.labelVisuallyHidden
          description = config.description
          placeholder = config.placeholder
          validation = config.validation
          isRequired = config.isRequired
          isDisabled = config.isDisabled
          isPending = config.isPending
          size = config.size
          attributes = config.attributes }
    let withSelectedMany selected (config:SelectConfig<'value, MultipleSelection>) =
        let choices =
            selected |> List.distinct |> List.map (fun value ->
                config.options |> List.tryFind (fun choice -> choice.value = value)
                |> Option.defaultWith (fun () -> invalidArg (nameof selected) "Every selected value must have a supplied option label."))
        { config with selectedMany = choices }
    let withId id (config:SelectConfig<'value, 'mode>) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable component ID is required."
        { config with id = Some id }
    let withSize size (config:SelectConfig<'value, 'mode>) = { config with size = Some size }
    let withVisuallyHiddenLabel (config:SelectConfig<'value, 'mode>) = { config with labelVisuallyHidden = true }
    let withDescription description (config:SelectConfig<'value, 'mode>) = { config with description = Some description }
    let withPlaceholder placeholder (config:SelectConfig<'value, 'mode>) = { config with placeholder = Some placeholder }
    let withValidation message (config:SelectConfig<'value, 'mode>) = { config with validation = Some message }
    let withSearch search (config:SelectConfig<'value, 'mode>) = { config with search = Some search }
    /// Echo the requested query when rendering remote results, so older matches cannot be selected.
    let withQuery query (config:SelectConfig<'value, 'mode>) = { config with query = Some query }
    let withOptions options (config:SelectConfig<'value, 'mode>) =
        ChoiceSelection.validateKeys (fun choice -> config.encode choice.value) options
        { config with options = options }
    let withEmptyMessage message (config:SelectConfig<'value, 'mode>) = { config with emptyMessage = message }
    let withLoadingMessage message (config:SelectConfig<'value, 'mode>) = { config with loadingMessage = message }
    let withError message (config:SelectConfig<'value, 'mode>) = { config with error = Some message }
    let loading (config:SelectConfig<'value, 'mode>) = { config with isLoading = true }
    let required (config:SelectConfig<'value, 'mode>) = { config with isRequired = true }
    let disabled (config:SelectConfig<'value, 'mode>) = { config with isDisabled = true }
    let pending (config:SelectConfig<'value, 'mode>) = { config with isPending = true }
    let withAttributes attributes (config:SelectConfig<'value, 'mode>) = { config with attributes = attributes }

    let private enabledChoices (config:SelectConfig<'value, 'mode>) =
        config.options
        |> List.filter (fun choice -> not choice.disabled)
        |> List.map (fun choice -> { SelectedChoice.value = config.encode choice.value; label = choice.label })
        |> ChoiceSelection.json

    // Ignore pointer events caused by keyboard scrolling under a stationary cursor.
    let private pointerMoved popupId =
        let popup = $"document.getElementById('{popupId}')"
        $"evt.pointerType != 'touch' && (evt.clientX != Number({popup}.dataset.fvePointerX) || evt.clientY != Number({popup}.dataset.fvePointerY))"

    let private pointerTracking =
        [ _attr ("data-fve-pointer-x", "NaN")
          _attr ("data-fve-pointer-y", "NaN")
          _dataPreserveAttr "data-fve-pointer-x data-fve-pointer-y"
          _dataOn ("pointermove", [ "window" ], "el.dataset.fvePointerX = evt.clientX; el.dataset.fvePointerY = evt.clientY") ]

    let private renderPlain config =
        let instanceId = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let fieldId = $"fve-select-{instanceId}"
        let labelId = $"{fieldId}-label"
        let descriptionId = $"{fieldId}-description"
        let validationId = $"{fieldId}-validation"
        let triggerId = $"{fieldId}-trigger"
        let popupId = $"{fieldId}-popup"
        let listboxId = $"{fieldId}-options"
        let openSignal = $"_{instanceId}_open"
        let popupElement = $"document.getElementById('{popupId}')"
        let popupIsOpen = $"{popupElement}.matches(':popover-open')"
        let listboxElement = $"document.getElementById('{listboxId}')"
        let openFocus = if config.isMultiple then $", {listboxElement}.focus()" else ""
        let synchronizePopup = $"${openSignal} ? ({popupIsOpen} || ({popupElement}.showPopover({{source: document.getElementById('{triggerId}')}}){openFocus})) : ({popupIsOpen} && {popupElement}.hidePopover())"
        let selectionSignal = $"{instanceId}_selected"
        let initialSelection = config.selectedMany |> List.map (fun choice -> { SelectedChoice.value = config.encode choice.value; label = choice.label })
        let valueSignal = $"{instanceId}_value"
        let labelSignal = $"_{instanceId}_label"
        let activeSignal = $"_{instanceId}_active"
        let typeaheadSignal = $"_{instanceId}_typeahead"
        let typeaheadTimeSignal = $"_{instanceId}_typeahead_time"
        let unavailable = config.isDisabled || config.isPending
        let optionId (choice:SelectOption<_>) =
            $"{fieldId}-option-{choice.value |> config.encode |> ComponentHtml.optionToken}"
        let selectedChoice =
            if config.isMultiple then config.selectedMany |> List.tryHead
            else config.options |> List.tryFind (fun option -> Some option.value = config.selected)
        let selectedValue = selectedChoice |> Option.map (fun option -> config.encode option.value) |> Option.defaultValue ""
        let selectedLabel =
            selectedChoice
            |> Option.map _.label
            |> Option.orElse config.placeholder
            |> Option.defaultValue "Select an option"
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
        let restoreFocus = if config.isMultiple then $", document.getElementById('{triggerId}').focus()" else ""
        let tabCommit = if config.isMultiple then $"document.getElementById('{triggerId}').focus()" else commitActive
        let keydown =
            String.concat "; " [
                $"evt.altKey && evt.key == 'ArrowDown' && (evt.preventDefault(), ${openSignal} || ({openAt selectedOrFirst}))"
                (if config.isMultiple then $"evt.altKey && evt.key == 'ArrowUp' && ${openSignal} && (evt.preventDefault(), ${openSignal} = false{restoreFocus})" else $"evt.altKey && evt.key == 'ArrowUp' && ${openSignal} && (evt.preventDefault(), {commitActive})")
                $"!evt.altKey && evt.key == 'ArrowDown' && ({moveActive 1})"
                $"!evt.altKey && evt.key == 'ArrowUp' && ({moveActive -1})"
                $"evt.key == 'Home' && (evt.preventDefault(), {openAt firstOption})"
                $"evt.key == 'End' && (evt.preventDefault(), {openAt lastOption})"
                $"evt.key == 'PageUp' && ({jumpActive -10})"
                $"evt.key == 'PageDown' && ({jumpActive 10})"
                $"(evt.key == 'Enter' || evt.key == ' ') && (evt.preventDefault(), ${openSignal} && ${activeSignal} ? {commitActive} : ({openAt selectedOrFirst}))"
                $"evt.key == 'Escape' && ${openSignal} && (evt.preventDefault(), ${openSignal} = false, ${activeSignal} = ({selectedOrFirst})?.id || '', ${typeaheadSignal} = ''{restoreFocus})"
                $"evt.key == 'Tab' && ${openSignal} && ({tabCommit}, ${openSignal} = false, ${typeaheadSignal} = '')"
                typeahead ]
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        let triggerClasses =
            ComponentHtml.classes [
                "fve-popup-field fve-popup-control group flex min-h-[var(--fve-control-min-height)] w-full items-center justify-between gap-3 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-left text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-normal text-[var(--fve-text)] ring-1 ring-inset outline-none hover:bg-[var(--fve-surface-hover)] disabled:cursor-not-allowed disabled:opacity-50"
                if config.validation.IsSome then "ring-[var(--fve-critical-ring)]" else "ring-[var(--fve-border)]" ]
        div {
            _class (ComponentHtml.classes [ ComponentHtml.controlSizeClass config.size; "relative grid min-w-0 grid-cols-1 content-start gap-1.5" ])
            let initialSignals = $"{{{openSignal}: false, {valueSignal}: {ComponentHtml.javascriptString selectedValue}, {labelSignal}: {ComponentHtml.javascriptString selectedLabel}, {activeSignal}: '', {typeaheadSignal}: '', {typeaheadTimeSignal}: 0}}"
            if not config.isMultiple then _dataSignals initialSignals
            if config.isMultiple then
                _attr ("data-signals__ifmissing", initialSignals.TrimEnd('}') + $", {selectionSignal}: {ChoiceSelection.json initialSelection}}}")
                let allowed = config.options |> List.map (fun choice -> config.encode choice.value) |> System.Text.Json.JsonSerializer.Serialize
                _dataEffect $"const allowed = {allowed}; const kept = ${selectionSignal}.filter(item => allowed.includes(item.value)); if (kept.length !== ${selectionSignal}.length) ${selectionSignal} = kept"
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
            if config.isMultiple then
                ChoiceSelection.render config.name selectionSignal initialSelection unavailable
            else
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
                _tabindex 0
                _popovertarget popupId
                if not config.isMultiple then _role "combobox"
                _ariaHaspopup "listbox"
                _ariaControls listboxId
                _ariaLabelledby labelId
                _ariaExpanded false
                if not config.isMultiple then _ariaRequired config.isRequired
                _ariaDisabled unavailable
                _ariaInvalid config.validation.IsSome
                _disabled unavailable
                if config.isPending then _ariaBusy true
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                if not config.isMultiple then
                    _dataAttr ("aria-activedescendant", $"${openSignal} && document.getElementById(${activeSignal}) ? ${activeSignal} : null")
                if config.isMultiple then _ariaDescribedby (String.concat " " ([ triggerId + "-selection" ] @ (if describedBy = "" then [] else [ describedBy ])))
                elif String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                _dataOn ("click", [ "prevent"; "stop" ], $"document.getElementById('{triggerId}').focus(); ${openSignal} = !${openSignal}")
                _dataOn ("keydown", keydown)
                _class triggerClasses
                for attribute in ComponentHtml.safeAttributes [ "id"; "type"; "name"; "value"; "disabled"; "role"; "aria-haspopup"; "aria-controls"; "aria-labelledby"; "aria-expanded"; "aria-activedescendant"; "aria-describedby"; "aria-required"; "aria-disabled"; "aria-invalid"; "aria-busy"; "data-bind:"; "data-attr:"; "data-on:"; "class" ] config.attributes do attribute
                span {
                    _class "min-w-0 truncate"
                    if config.isMultiple then
                        _id (triggerId + "-selection")
                        _dataText (ChoiceSelection.summary selectionSignal (config.placeholder |> Option.defaultValue "Select options"))
                        if initialSelection.IsEmpty then config.placeholder |> Option.defaultValue "Select options"
                        else initialSelection |> List.map _.label |> String.concat ", "
                    else
                        _dataText $"${labelSignal}"
                        selectedLabel
                }
                if config.isPending then
                    ComponentHtml.loadingGlyph ControlSize.Small
                else
                    raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4 shrink-0 text-[var(--fve-muted-text)] transition-transform group-aria-expanded:rotate-180" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
            }
            div {
                _id popupId
                _popover "auto"
                _role "group"
                _ariaLabel (config.label + " options")
                _dataEffect synchronizePopup
                _dataOn ("beforetoggle", $"${openSignal} = evt.newState == 'open'; evt.newState == 'closed' && (${activeSignal} = '', ${typeaheadSignal} = '')")
                for attribute in pointerTracking do attribute
                _style "inset: auto; margin: 0.25rem 0; position-area: block-end span-inline-end; position-try-fallbacks: flip-block, flip-inline, flip-block flip-inline; width: anchor-size(width)"
                _class "fve-popup fixed z-30 max-h-60 overflow-auto rounded-[var(--fve-radius-control)] border-0 bg-[var(--fve-surface)] p-0 shadow-lg"
                if config.isMultiple then
                    div {
                        _class "flex items-center justify-between p-1"
                        ChoiceSelection.selectAll "Select all" config.label selectionSignal unavailable (enabledChoices config) listboxId
                        ChoiceSelection.clear config.label selectionSignal initialSelection unavailable listboxId
                    }
                div {
                    _id listboxId
                    _role "listbox"
                    _ariaLabelledby labelId
                    if config.isMultiple then
                        _attr ("aria-multiselectable", "true")
                        _ariaRequired config.isRequired
                        if describedBy <> "" then _ariaDescribedby describedBy
                        _tabindex 0
                        _dataAttr ("aria-activedescendant", $"document.getElementById(${activeSignal}) ? ${activeSignal} : null")
                        _dataOn ("keydown", keydown)
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
                            let isSelected = if config.isMultiple then ChoiceSelection.selected selectionSignal encodedValue else $"${valueSignal} == {ComponentHtml.javascriptString encodedValue}"
                            _ariaSelected (if config.isMultiple then config.selectedMany |> List.exists (fun item -> item.value = choice.value) else config.selected = Some choice.value)
                            _attr ("data-fve-option-label", choice.label.ToLowerInvariant())
                            _attr ("data-fve-option-value", encodedValue)
                            _attr ("data-fve-option-text", choice.label)
                            _dataAttr ("aria-selected", $"{isSelected} ? 'true' : 'false'")
                            _dataAttr ("data-active", $"${activeSignal} == {ComponentHtml.javascriptString choiceId} ? 'true' : null")
                            if choiceUnavailable |> not then
                                _dataOn ("pointermove", $"({pointerMoved popupId}) && (${activeSignal} = {ComponentHtml.javascriptString choiceId})")
                                _dataOn ("pointerleave", $"({pointerMoved popupId}) && ${activeSignal} == {ComponentHtml.javascriptString choiceId} && (${activeSignal} = '')")
                                _dataOn ("mousedown", "evt.preventDefault()")
                                if config.isMultiple then
                                    _dataOn ("click", $"${activeSignal} = {ComponentHtml.javascriptString choiceId}; {ChoiceSelection.toggle selectionSignal encodedValue choice.label}; ${typeaheadSignal} = ''; {listboxElement}.focus()")
                                else
                                    _dataOn ("click", $"${activeSignal} = {ComponentHtml.javascriptString choiceId}; ${valueSignal} = {ComponentHtml.javascriptString encodedValue}; ${labelSignal} = {ComponentHtml.javascriptString choice.label}; ${typeaheadSignal} = ''; ${openSignal} = false; document.getElementById('{triggerId}').focus()")
                            _class "fve-popup-item flex w-full items-center justify-between gap-3 px-3 py-[var(--fve-control-padding-block)] text-left text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-normal text-[var(--fve-text)] disabled:cursor-not-allowed disabled:opacity-50"
                            span { _class "min-w-0 [overflow-wrap:anywhere]"; choice.label }
                            span {
                                _ariaHidden "true"
                                _dataShow isSelected
                                _style "display:none"
                                _class "shrink-0 font-semibold"
                                "✓"
                            }
                        }
                }
            }
            match config.description with
            | Some description -> p { _id descriptionId; _class "text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            if config.isMultiple then
                ChoiceSelection.announcement selectionSignal initialSelection
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

    let private searchableRequest endpoint instanceId multiple =
        if not multiple then $"@get({ComponentHtml.javascriptString endpoint}, {{requestCancellation: 'auto'}})"
        else
            $"""(() => {{
const field = document.getElementById('fve-select-{instanceId}-search');
if (!field || field.disabled) return Promise.resolve();
field._fveSelectRequest?.abort();
const controller = new AbortController();
field._fveSelectRequest = controller;
const observer = new MutationObserver(() => {{ if (!field.isConnected || field.disabled) controller.abort(); }});
const abortOnPageHide = () => controller.abort();
window.addEventListener('pagehide', abortOnPageHide, {{once: true}});
observer.observe(document, {{childList: true, subtree: true, attributes: true, attributeFilter: ['disabled']}});
$_{instanceId}_request_pending = true;
return @get({ComponentHtml.javascriptString endpoint}, {{requestCancellation: controller}}).finally(() => {{
    observer.disconnect();
    window.removeEventListener('pagehide', abortOnPageHide);
    if (field._fveSelectRequest === controller) {{
        delete field._fveSelectRequest;
        if (field.isConnected) $_{instanceId}_request_pending = false;
    }}
}});
}})()"""

    let renderOptions (config:SelectConfig<'value, 'mode>) =
        let instanceId = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let search = config.search |> Option.defaultValue SelectSearch.Static
        let fieldId = $"fve-select-{instanceId}"
        let searchId = $"{fieldId}-search"
        let labelId = $"{fieldId}-label"
        let popupId = $"{fieldId}-popup"
        let listboxId = $"{fieldId}-options"
        let openSignal = $"_{instanceId}_open"
        let popupElement = $"document.getElementById('{popupId}')"
        let popupIsOpen = $"{popupElement}.matches(':popover-open')"
        let synchronizePopup = $"${openSignal} ? ({popupIsOpen} || {popupElement}.showPopover({{source: document.getElementById('{fieldId}')}})) : ({popupIsOpen} && {popupElement}.hidePopover())"
        let querySignal =
            match search with
            | SelectSearch.Static -> $"_{instanceId}_query"
            | SelectSearch.Remote _ -> $"{instanceId}_query"
        let valueSignal = $"{instanceId}_value"
        let labelSignal = $"_{instanceId}_label"
        let activeSignal = $"_{instanceId}_active"
        let requestPendingSignal = $"_{instanceId}_request_pending"
        let unavailable = config.isDisabled || config.isPending
        let currentQuery =
            match search with
            | SelectSearch.Remote _ when config.isMultiple || config.query.IsSome ->
                let expectedQuery = config.query |> Option.defaultValue "" |> ComponentHtml.javascriptString
                $"${querySignal} === {expectedQuery}"
            | _ -> "true"
        let busy = if config.isLoading then "true" else $"(${requestPendingSignal} || !({currentQuery}))"
        let ready = if config.isLoading then "false" else $"(!${requestPendingSignal} && ({currentQuery}))"
        let selectionSignal = $"{instanceId}_selected"
        let initialSelection = config.selectedMany |> List.map (fun choice -> { SelectedChoice.value = config.encode choice.value; label = choice.label })
        let selectAllText = match search with SelectSearch.Static -> "Select all" | SelectSearch.Remote _ -> "Select all results"
        let optionId (choice:SelectOption<_>) =
            $"{fieldId}-option-{choice.value |> config.encode |> ComponentHtml.optionToken}"
        let visibleOptions = $"Array.from(document.querySelectorAll('#{listboxId} [role=option]:not(:disabled)')).filter(item => item.style.display != 'none')"
        let firstVisibleOption = $"{visibleOptions}.at(0)"
        let optionMatches (choice:SelectOption<_>) =
            $"!${querySignal}.trim() || {ComponentHtml.javascriptString (choice.label.ToLowerInvariant())}.includes(${querySignal}.trim().toLowerCase())"
        let eligibleChoices =
            let choices = enabledChoices config
            let matches =
                match search with
                | SelectSearch.Static -> $"{choices}.filter(option => !${querySignal}.trim() || option.label.toLowerCase().includes(${querySignal}.trim().toLowerCase()))"
                | SelectSearch.Remote _ -> choices
            if config.error.IsSome then "[]"
            else $"({ready}) ? {matches} : []"
        let anyOptionMatches =
            match search, config.options with
            | _, [] -> "false"
            | SelectSearch.Remote _, _ -> "true"
            | SelectSearch.Static, options -> options |> List.map optionMatches |> String.concat " || "
        let lastVisibleOption = $"{visibleOptions}.at(-1)"
        let currentIndex = $"{visibleOptions}.findIndex(item => item.id == ${activeSignal})"
        let scrollToActive = $"queueMicrotask(() => document.getElementById(${activeSignal})?.scrollIntoView({{block: 'nearest'}}))"
        let moveActive offset =
            let missingIndex = if offset > 0 then -1 else 0
            $"evt.preventDefault(), ${openSignal} = true, {visibleOptions}.length && (${activeSignal} = {visibleOptions}.at((({currentIndex} < 0 ? {missingIndex} : {currentIndex}) + {offset} + {visibleOptions}.length) %% {visibleOptions}.length)?.id || '', {scrollToActive})"
        let keydown =
            String.concat "; " [
                $"evt.key == 'ArrowDown' && ({moveActive 1})"
                $"evt.key == 'ArrowUp' && ({moveActive -1})"
                $"evt.key == 'Home' && ${openSignal} && (evt.preventDefault(), ${activeSignal} = ({firstVisibleOption})?.id || '', {scrollToActive})"
                $"evt.key == 'End' && ${openSignal} && (evt.preventDefault(), ${activeSignal} = ({lastVisibleOption})?.id || '', {scrollToActive})"
                $"evt.key == 'Enter' && ${openSignal} && ${activeSignal} && (evt.preventDefault(), document.getElementById(${activeSignal})?.click())"
                $"evt.key == 'Escape' && ${openSignal} && (evt.preventDefault(), ${openSignal} = false, document.getElementById('{fieldId}')?.focus())"
                (if config.isMultiple then "" else $"evt.key == 'Tab' && (${openSignal} = false)") ]
        let searchInputAttributes =
            [ yield _autocomplete "off"
              yield _placeholder (config.placeholder |> Option.defaultValue "Search options")
              yield _dataBind querySignal
              yield _dataAttr ("aria-busy", $"{busy} ? 'true' : null")
              if unavailable |> not then
                  yield _dataOn ("keydown", keydown)
                  match search with
                  | SelectSearch.Static ->
                      yield _dataOn ("input", $"${openSignal} = true; ${activeSignal} = ''")
                  | SelectSearch.Remote endpoint ->
                      if config.isMultiple then
                          yield _dataOn ("input", $"${openSignal} = true; ${activeSignal} = ''; el._fveSelectRequest?.abort(); delete el._fveSelectRequest; ${requestPendingSignal} = true")
                      yield _dataOn ("input", [ "debounce.250ms" ], $"${openSignal} = true; ${activeSignal} = ''; {searchableRequest endpoint instanceId config.isMultiple}") ]
        let searchInput =
            Input.create (fieldId + "-query") ($"Search {config.label}")
            |> Input.withId searchId
            |> Input.withType InputType.Search
            |> Input.withValue (config.query |> Option.defaultValue "")
            |> Input.withAttributes searchInputAttributes
            |> (if unavailable then Input.disabled else id)
            |> Input.renderEmbedded
        div {
            _id popupId
            _popover "auto"
            _tabindex 0
            _role "group"
            _ariaLabel (config.label + " options")
            _dataEffect synchronizePopup
            _dataOn ("beforetoggle", $"${openSignal} = evt.newState == 'open'; evt.newState == 'closed' && (${activeSignal} = '', document.getElementById('{searchId}')?._fveSelectRequest?.abort())")
            for attribute in pointerTracking do attribute
            _style "inset: auto; margin: 0.25rem 0; position-area: block-end span-inline-end; position-try-fallbacks: flip-block, flip-inline, flip-block flip-inline; width: anchor-size(width)"
            _class "fve-popup fixed z-30 max-h-60 overflow-auto rounded-[var(--fve-radius-control)] border-0 bg-[var(--fve-surface)] p-0 shadow-lg"
            div {
                _class "p-1"
                searchInput
            }
            if config.isMultiple then
                div {
                    _class "flex items-center justify-between px-1 pb-1"
                    ChoiceSelection.selectAll selectAllText config.label selectionSignal unavailable eligibleChoices searchId
                    ChoiceSelection.clear config.label selectionSignal initialSelection unavailable searchId
                }
            div {
                _id listboxId
                _role "listbox"
                if config.isMultiple then _attr ("aria-multiselectable", "true")
                _ariaRequired config.isRequired
                _ariaLabelledby labelId
                _ariaBusy config.isLoading
                _dataAttr ("aria-busy", $"{busy} ? 'true' : null")
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
                            let isSelected = if config.isMultiple then ChoiceSelection.selected selectionSignal encodedValue else $"${valueSignal} == {ComponentHtml.javascriptString encodedValue}"
                            _ariaSelected (if config.isMultiple then config.selectedMany |> List.exists (fun item -> item.value = choice.value) else config.selected = Some choice.value)
                            _attr ("data-fve-option-label", choice.label.ToLowerInvariant())
                            _attr ("data-fve-option-value", encodedValue)
                            _attr ("data-fve-option-text", choice.label)
                            _dataAttr ("aria-selected", $"{isSelected} ? 'true' : 'false'")
                            _dataAttr ("data-active", $"${activeSignal} == {ComponentHtml.javascriptString choiceId} ? 'true' : null")
                            match search with
                            | SelectSearch.Static -> _dataShow $"{ready} && ({optionMatches choice})"
                            | SelectSearch.Remote _ -> _dataShow ready
                            if choiceUnavailable |> not then
                                _dataOn ("pointermove", $"({pointerMoved popupId}) && ({ready}) && (${activeSignal} = {ComponentHtml.javascriptString choiceId})")
                                _dataOn ("pointerleave", $"({pointerMoved popupId}) && ${activeSignal} == {ComponentHtml.javascriptString choiceId} && (${activeSignal} = '')")
                                _dataOn ("mousedown", "evt.preventDefault()")
                                if config.isMultiple then
                                    _dataOn ("click", $"if ({ready}) {{ ${activeSignal} = {ComponentHtml.javascriptString choiceId}; {ChoiceSelection.toggle selectionSignal encodedValue choice.label}; document.getElementById('{searchId}').focus(); }}")
                                else
                                    _dataOn ("click", $"${activeSignal} = {ComponentHtml.javascriptString choiceId}; ${valueSignal} = {ComponentHtml.javascriptString encodedValue}; ${labelSignal} = {ComponentHtml.javascriptString choice.label}; ${querySignal} = ''; ${openSignal} = false; document.getElementById('{searchId}').focus()")
                            _class "fve-popup-item flex w-full items-center justify-between gap-3 px-3 py-[var(--fve-control-padding-block)] text-left text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-normal text-[var(--fve-text)] disabled:cursor-not-allowed disabled:opacity-50"
                            span { _class "min-w-0 [overflow-wrap:anywhere]"; choice.label }
                            span {
                                _ariaHidden "true"
                                _dataShow isSelected
                                _style "display:none"
                                _class "shrink-0 font-semibold"
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
                    match search with
                    | SelectSearch.Remote endpoint when unavailable |> not ->
                        button {
                            _type "button"
                            if not config.isMultiple then _dataIndicator requestPendingSignal
                            _dataOn ("click", $"{searchableRequest endpoint instanceId config.isMultiple}.then(() => document.getElementById('{searchId}')?.focus())")
                            _class "fve-popup-control inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-brand-text)] ring-1 ring-inset ring-[var(--fve-brand-ring)] hover:bg-[var(--fve-brand-subtle)]"
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

    let private renderSearchable config =
        let instanceId = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let search = config.search |> Option.defaultValue SelectSearch.Static
        let fieldId = $"fve-select-{instanceId}"
        let labelId = $"{fieldId}-label"
        let descriptionId = $"{fieldId}-description"
        let validationId = $"{fieldId}-validation"
        let listboxId = $"{fieldId}-options"
        let openSignal = $"_{instanceId}_open"
        let querySignal =
            match search with
            | SelectSearch.Static -> $"_{instanceId}_query"
            | SelectSearch.Remote _ -> $"{instanceId}_query"
        let valueSignal = $"{instanceId}_value"
        let labelSignal = $"_{instanceId}_label"
        let activeSignal = $"_{instanceId}_active"
        let requestPendingSignal = $"_{instanceId}_request_pending"
        let unavailable = config.isDisabled || config.isPending
        let busy = if config.isLoading || config.isPending then "true" else $"${requestPendingSignal}"
        let optionId (choice:SelectOption<_>) =
            $"{fieldId}-option-{choice.value |> config.encode |> ComponentHtml.optionToken}"
        let selectionSignal = $"{instanceId}_selected"
        let initialSelection = config.selectedMany |> List.map (fun choice -> { SelectedChoice.value = config.encode choice.value; label = choice.label })
        let selectedChoice = config.options |> List.tryFind (fun option -> Some option.value = config.selected)
        let selectedValue = selectedChoice |> Option.map (fun option -> config.encode option.value) |> Option.defaultValue ""
        let selectedLabel = config.query |> Option.defaultWith (fun () -> if config.isMultiple then "" else selectedChoice |> Option.map _.label |> Option.defaultValue "")
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
        let triggerClasses =
            ComponentHtml.classes [
                "fve-popup-field fve-popup-control group flex min-h-[var(--fve-control-min-height)] w-full items-center justify-between gap-3 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-left text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-normal text-[var(--fve-text)] ring-1 ring-inset outline-none hover:bg-[var(--fve-surface-hover)] disabled:cursor-not-allowed disabled:opacity-50"
                if config.validation.IsSome then "ring-[var(--fve-critical-ring)]" else "ring-[var(--fve-border)]" ]
        div {
            _class (ComponentHtml.classes [ ComponentHtml.controlSizeClass config.size; "relative grid min-w-0 grid-cols-1 content-start gap-1.5" ])
            if config.isMultiple then _id (fieldId + "-field")
            let initialSignals = $"{{{openSignal}: false, {querySignal}: '', {valueSignal}: {ComponentHtml.javascriptString selectedValue}, {labelSignal}: {ComponentHtml.javascriptString selectedLabel}, {activeSignal}: '', {requestPendingSignal}: false}}"
            if config.isMultiple then
                _attr ("data-signals__ifmissing", initialSignals.TrimEnd('}') + $", {selectionSignal}: {ChoiceSelection.json initialSelection}}}")
            else _dataSignals initialSignals
            label {
                _id labelId
                _for fieldId
                _class (if config.labelVisuallyHidden then "sr-only" else "text-sm font-medium text-[var(--fve-text)]")
                config.label
                if config.isRequired then
                    span {
                        _ariaHidden true
                        _class "text-[var(--fve-critical-text)]"
                        " *"
                    }
            }
            button {
                _id fieldId
                _type "button"
                _ariaHaspopup "dialog"
                _ariaControls (fieldId + "-popup")
                _ariaExpanded false
                _ariaLabelledby labelId
                _ariaDisabled unavailable
                _ariaInvalid config.validation.IsSome
                _disabled unavailable
                if config.isPending then _ariaBusy true
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                if unavailable |> not then
                    _dataOn ("click", [ "prevent"; "stop" ], $"${openSignal} = !${openSignal}; ${openSignal} && queueMicrotask(() => document.getElementById('{fieldId}-search')?.focus())")
                    _dataOn ("keydown", $"(evt.key == 'Enter' || evt.key == ' ' || evt.key == 'ArrowDown') && (evt.preventDefault(), ${openSignal} = true, queueMicrotask(() => document.getElementById('{fieldId}-search')?.focus())); evt.key == 'Escape' && ${openSignal} && (evt.preventDefault(), ${openSignal} = false)")
                _class triggerClasses
                for attribute in ComponentHtml.safeAttributes [ "id"; "type"; "name"; "disabled"; "aria-haspopup"; "aria-controls"; "aria-expanded"; "aria-labelledby"; "aria-describedby"; "aria-activedescendant"; "aria-disabled"; "aria-invalid"; "aria-busy"; "data-bind:"; "data-attr:"; "data-on:"; "class" ] config.attributes do attribute
                span {
                    _class "min-w-0 truncate text-left"
                    if config.isMultiple then
                        _id (fieldId + "-selection")
                        _dataText (ChoiceSelection.summary selectionSignal (config.placeholder |> Option.defaultValue "Select options"))
                        if initialSelection.IsEmpty then config.placeholder |> Option.defaultValue "Select options"
                        else initialSelection |> List.map _.label |> String.concat ", "
                    else
                        _dataText $"${labelSignal}"
                        selectedLabel
                }
                raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4 shrink-0 text-[var(--fve-muted-text)] transition-transform group-aria-expanded:rotate-180" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
            }
            if config.isMultiple then
                ChoiceSelection.render config.name selectionSignal initialSelection unavailable
                ChoiceSelection.announcement selectionSignal initialSelection
            else
                input {
                    _type "hidden"
                    _name config.name
                    _value selectedValue
                    _disabled unavailable
                    _dataBind valueSignal
                }
            renderOptions config
            match config.description with
            | Some description -> p { _id descriptionId; _class "text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
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


    let render config =
        match config.search with
        | Some _ -> renderSearchable config
        | None -> renderPlain config
