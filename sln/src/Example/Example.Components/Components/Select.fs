namespace Example.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>select</category>
[<RequireQualifiedAccess>]
type SelectSearch = Static | Remote of endpoint:string

/// <category>select</category>
[<RequireQualifiedAccess>]
type SelectPosition = SelectedItem | TriggerStart | TriggerEnd

/// <category>select</category>
[<AutoOpen>]
module SelectAttributes =
    type Html with
        static member _search(value:SelectSearch) = Html._attr("search", match value with SelectSearch.Static -> "static" | SelectSearch.Remote endpoint -> if String.IsNullOrWhiteSpace endpoint then invalidArg "endpoint" "A search endpoint is required." else endpoint)
        static member _position(value:SelectPosition) = Html._attr("position", match value with SelectPosition.SelectedItem -> "selected-item" | SelectPosition.TriggerStart -> "trigger-start" | SelectPosition.TriggerEnd -> "trigger-end")
        static member _nativeFallback(value:bool) = Html._attr("native-fallback", if value then "true" else "false")
        static member _query(value:string) = Html._attr("query", value)
        static member _loading(value:bool) = Html._attr("loading", if value then "true" else "false")
        static member _error(value:string) = Html._attr("error", value)
        static member _error(value:string option) = Html._attr("error", value |> Option.defaultValue "")
        static member _retry(value:string) = Html._attr("retry", value)
        static member _emptyMessage(value:string) = Html._attr("empty-message", value)
        static member _loadingMessage(value:string) = Html._attr("loading-message", value)

[<NoEquality; NoComparison>]
type internal SelectChoice = { value:string; label:string; disabled:bool }

type internal SelectPart(kind:string, choices:SelectChoice list, render:string option -> HtmlElement) =
    inherit HtmlElement()
    member _.Kind = kind
    member _.Choices = choices
    member _.RenderOwned(owner:string option) = render owner
    override _.Render(writer) = (render None).Render writer

[<NoEquality; NoComparison>]
type internal SelectConfig =
    { isMultiple:bool
      selectedMany:SelectChoice list
      search:SelectSearch option
      query:string option
      name:string
      id:string option
      options:SelectChoice list
      selected:string option
      label:string
      nativeFallback:bool
      placeholder:string option
      invalid:bool
      isRequired:bool
      isDisabled:bool
      isPending:bool
      isReadOnly:bool
      size:Size option
      position:SelectPosition
      controlClass:string
      attributes:HtmlAttribute list }

module internal Select =
    let private flag name attributes =
        match ComponentElement.attribute name attributes with
        | Some {Value = ValueNone} -> true
        | Some {Value = ValueSome value} -> value <> "false"
        | None -> false

    let private accessibleName triggerId (config:SelectConfig) =
        $"const control = document.getElementById({ComponentHtml.javascriptString triggerId}); const labelledBy = control?.getAttribute('aria-labelledby')?.split(/\\s+/).map(id => document.getElementById(id)?.textContent || '').join(' '); const label = labelledBy || control?.getAttribute('aria-label') || [...(control?.labels || [])].map(label => label.textContent.trim()).join(' ') || {ComponentHtml.javascriptString config.label}; "

    // A morph can replace server settings without rerunning unchanged data-init attributes.
    let private synchronizeName triggerId instanceId config =
        accessibleName triggerId config + $"const state = $_fveSelect.{instanceId}; state.open; if (state.settings.label !== label) state.settings.label = label; "

    // Ignore pointer events caused by keyboard scrolling under a stationary cursor.
    let private pointerTracking =
        [ _attr ("data-fve-pointer-x", "NaN")
          _attr ("data-fve-pointer-y", "NaN")
          _dataPreserveAttr "data-fve-pointer-x data-fve-pointer-y"
          _dataOn ("pointermove", [ "window" ], "el.dataset.fvePointerX = evt.clientX; el.dataset.fvePointerY = evt.clientY") ]

    let private edgePosition = function
        | SelectPosition.TriggerEnd -> "block-end span-inline-start"
        | SelectPosition.SelectedItem
        | SelectPosition.TriggerStart -> "block-end span-inline-end"

    let private initializePopupPosition = """
        el.fvePosition = () => {
            const p = el, root = p.closest('[data-fve-select]');
            if (!p.isConnected || !root || !p.matches(':popover-open')) return;
            const trigger = document.getElementById(root.dataset.fveSelectTrigger);
            if (!trigger) return;
            const settings = $_fveSelect[root.dataset.fveSelect].settings;
            const alignSelected = settings.aligned, edge = settings.position;
            const scroll = p.querySelector('[role=listbox]') || p;
            const r = trigger.getBoundingClientRect(), viewport = window.visualViewport;
            const top = (viewport?.offsetTop || 0) + 8;
            const bottom = (viewport?.offsetTop || 0) + (viewport?.height || innerHeight) - 8;
            p.style.positionArea = edge; p.style.margin = '0.25rem 0';
            p.style.top = 'auto'; p.style.bottom = 'auto'; p.style.left = 'auto'; p.style.right = 'auto';
            p.style.width = 'anchor-size(width)'; p.style.maxHeight = '';
            const limit = parseFloat(getComputedStyle(p).maxHeight);
            const padding = parseFloat(getComputedStyle(p).paddingTop) || 0;
            const inset = parseFloat(getComputedStyle(document.documentElement).fontSize) / 4;
            p.style.maxHeight = Math.max(0, Math.min(limit, bottom - top)) + 'px';
            if (alignSelected && !matchMedia('(pointer: coarse)').matches) {
                p.style.width = Math.max(0, r.width - inset * 2) + 'px';
                scroll.scrollTop = 0;
                const selected = p.querySelector('[role=option][aria-selected=true]') || p.querySelector('[role=option]:not(:disabled)');
                if (selected) {
                    const item = selected.getBoundingClientRect(), box = p.getBoundingClientRect();
                    const height = box.height, center = (r.top + r.bottom) / 2;
                    const itemCenter = item.top - box.top + item.height / 2;
                    const minOffset = Math.max(item.height / 2 + padding, itemCenter - (scroll.scrollHeight - scroll.clientHeight), center - (bottom - height));
                    const maxOffset = Math.min(height - item.height / 2 - padding, itemCenter, center - top);
                    if (minOffset <= maxOffset) {
                        const offset = Math.max(minOffset, Math.min(maxOffset, itemCenter));
                        const leftEdge = (viewport?.offsetLeft || 0) + 8;
                        const rightEdge = (viewport?.offsetLeft || 0) + (viewport?.width || innerWidth) - 8;
                        const width = Math.min(Math.max(0, r.width - inset * 2), Math.max(0, rightEdge - leftEdge));
                        p.style.positionArea = 'none'; p.style.margin = '0';
                        p.style.width = width + 'px';
                        p.style.top = (center - offset) + 'px';
                        p.style.left = Math.max(leftEdge, Math.min(r.left + inset, rightEdge - width)) + 'px';
                        scroll.scrollTop = itemCenter - offset;
                        return;
                    }
                }
            }
            p.style.width = 'anchor-size(width)';
            const below = Math.max(0, bottom - r.bottom), above = Math.max(0, r.top - top);
            const useBelow = below >= p.offsetHeight || below >= above;
            p.style.positionArea = useBelow ? edge : edge.replace('block-end', 'block-start');
            p.style.maxHeight = Math.max(0, Math.min(limit, (useBelow ? below : above) - 4)) + 'px';
            const selected = p.querySelector('[role=option][aria-selected=true]');
            if (selected) {
                const itemTop = selected.getBoundingClientRect().top - scroll.getBoundingClientRect().top + scroll.scrollTop;
                scroll.scrollTop = Math.max(0, itemTop + selected.offsetHeight / 2 - scroll.clientHeight / 2);
            }
        };
        """

    let private notifyChange fieldId valueSignal =
        $"if (previousValue !== ${valueSignal}) {{ const field = document.getElementById('{fieldId}-value'); field.value = ${valueSignal}; field.dispatchEvent(new Event('change', {{bubbles: true}})); }}"

    let private renderNative fieldId describedBy classes (config:SelectConfig) =
        div {
            _class "relative"
            _dataShow "false"
            select {
                _id (config.id |> Option.defaultValue config.name)
                _attr("data-fve-select-native", "")
                _data ("fve-group-control", "outline")
                _name config.name
                if describedBy <> "" then _ariaDescribedby describedBy
                _ariaInvalid config.invalid
                _required config.isRequired
                _disabled (config.isDisabled || config.isReadOnly)
                _dataAttr ("disabled", "true")
                _class (classes + " appearance-none pr-9")
                if config.selected.IsNone then
                    option { _value ""; _selected true; _disabled true; config.placeholder |> Option.defaultValue "Select an option" }
                for choice in config.options do
                    option { _value choice.value; _selected (config.selected = Some choice.value); _disabled choice.disabled; choice.label }
            }
            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 0 1 1.06 0l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
        }

    let private intent owner action detail =
        $"el.dispatchEvent(new CustomEvent('fve-select-{action}', {{bubbles: true, detail: {{owner: {ComponentHtml.javascriptString owner}{detail}}}}}))"

    let private toggleSelected signal =
        $"${signal} = ${signal}.some(item => item.value === evt.detail.value) ? ${signal}.filter(item => item.value !== evt.detail.value) : ${signal}.concat([{{value: evt.detail.value, label: evt.detail.label}}])"

    let private partAttributes protectedNames attributes =
        ComponentHtml.safeAttributes ("class" :: protectedNames) attributes

    // Root morphs update signals before detached descendants' subscriptions are cleaned up.
    let private partAttribute name expression = _dataAttr (name, $"el.isConnected ? ({expression}) : null")
    let private partShow expression = _dataShow $"el.isConnected && ({expression})"

    // Parts discover their owning state through ordinary DOM ancestry, including native popovers.
    let private owner = "el.closest('[data-fve-select]')"
    let private state = $"$_fveSelect[{owner}.dataset.fveSelect]"
    let private value = $"$[{owner}.dataset.fveSelectValue]"
    let private selection = $"$[{owner}.dataset.fveSelectSelected]"
    let private query = $"$[{owner}.dataset.fveSelectQuery]"
    let private popup = "el.closest('[data-fve-select-content]')"
    let private ready = $"(!{state}.request_pending && !{popup}.hasAttribute('data-fve-select-loading') && ({state}.settings.search !== 'remote' || {query} === {popup}.dataset.fveSelectQuery))"
    let private matches = $"({state}.settings.search !== 'static' || !{query}.trim() || el.dataset.fveOptionLabel.includes({query}.trim().toLowerCase()))"

    let private settings (config:SelectConfig) =
        System.Text.Json.JsonSerializer.Serialize {| multiple = config.isMultiple; search = (match config.search with None -> "none" | Some SelectSearch.Static -> "static" | Some (SelectSearch.Remote _) -> "remote"); unavailable = config.isDisabled || config.isReadOnly; required = config.isRequired; label = config.label; position = edgePosition config.position; aligned = config.position = SelectPosition.SelectedItem && not config.isMultiple && config.search.IsNone; blocked = config.options |> List.filter _.disabled |> List.map _.value |}

    let private contentElement instanceId =
        $"Array.from(document.querySelectorAll('[data-fve-select=\"{instanceId}\"] [data-fve-select-content]')).find(part => part.closest('[data-fve-select]').dataset.fveSelect === '{instanceId}')"

    let private rootAttributes instanceId fieldId triggerId querySignal =
        [ _attr ("data-fve-select", instanceId)
          _attr ("data-fve-select-field", fieldId)
          _attr ("data-fve-select-trigger", triggerId)
          _attr ("data-fve-select-value", instanceId + "_value")
          _attr ("data-fve-select-selected", instanceId + "_selected")
          _attr ("data-fve-select-query", querySignal) ]

    let internal renderItem (choice:SelectChoice) attributes (children:HtmlElement list) =
        let encoded = choice.value
        let selected = $"({state}.settings.multiple ? {selection}.some(item => item.value === el.closest('[data-fve-option-value]').dataset.fveOptionValue) : {value} === el.closest('[data-fve-option-value]').dataset.fveOptionValue)"
        let unavailable = if choice.disabled then "true" else "false"
        let disabled = $"{unavailable} || {state}.settings.unavailable || {state}.settings.blocked.includes(el.dataset.fveOptionValue)"
        button {
            _type "button"
            _role "option"
            _tabindex -1
            _disabled choice.disabled
            _ariaDisabled choice.disabled
            _ariaSelected false
            _dataPreserveAttr "id aria-selected aria-disabled disabled data-active"
            if choice.disabled then _attr ("data-fve-option-disabled", "")
            _attr ("data-fve-option-label", choice.label.ToLowerInvariant())
            _attr ("data-fve-option-value", encoded)
            _attr ("data-fve-option-text", choice.label)
            _dataInit $"el.id ||= {owner}.dataset.fveSelectField + '-option-' + {ComponentHtml.javascriptString (ComponentHtml.optionToken encoded)}"
            partAttribute "disabled" disabled
            partAttribute "aria-disabled" $"({disabled}) ? 'true' : 'false'"
            partAttribute "aria-selected" $"{selected} ? 'true' : 'false'"
            partAttribute "data-active" $"{state}.active === el.id ? 'true' : null"
            partShow $"{ready} && {matches}"
            let moved = $"evt.pointerType !== 'touch' && (evt.clientX !== Number({popup}.dataset.fvePointerX) || evt.clientY !== Number({popup}.dataset.fvePointerY))"
            _dataOn ("pointermove", $"if (!el.disabled && {ready} && ({moved})) {state}.active = el.id")
            _dataOn ("pointerleave", $"if (({moved}) && {state}.active === el.id) {state}.active = ''")
            _dataOn ("mousedown", "evt.preventDefault()")
            _dataOn ("click", $"if (!el.disabled && {ready}) el.dispatchEvent(new CustomEvent('fve-select-choice', {{bubbles:true, detail:{{owner:{owner}.dataset.fveSelectField, value:el.dataset.fveOptionValue, label:el.dataset.fveOptionText, id:el.id}}}}))")
            _class (ComponentHtml.classes [ ComponentHtml.popupItemClasses; "fve-popup-item flex w-full items-center justify-between gap-3 px-2 py-[var(--fve-control-padding-block)] text-left text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-normal text-[var(--fve-text)] disabled:cursor-not-allowed disabled:opacity-50"; ComponentContent.classes attributes ])
            for attribute in partAttributes [ "data-preserve-attr"; "type"; "role"; "tabindex"; "disabled"; "aria-disabled"; "aria-selected"; "data-on:"; "data-attr:"; "data-init"; "data-show"; "data-fve-option-disabled"; "data-fve-option-label"; "data-fve-option-value"; "data-fve-option-text" ] attributes do attribute
            span {
                _class "min-w-0 [overflow-wrap:anywhere]"
                if List.isEmpty children then choice.label
                else for child in children do child
            }
            span { _ariaHidden true; partShow selected; _style "display:none"; _class "shrink-0 font-semibold"; "✓" }
        }

    let internal renderContent ownerId attributes (children:HtmlElement list) (searchText:string) isLoading (failure:string option) (retryEndpoint:string option) (emptyMessage:string) (loadingMessage:string) =
        let fieldId =
            match ownerId with
            | Some _ -> ownerId
            | None ->
                ComponentElement.value "id" attributes
                |> Option.map (fun id ->
                    let stem = if id.EndsWith("-popup", StringComparison.Ordinal) then id[..id.Length - 7] elif id.EndsWith("-content", StringComparison.Ordinal) then id[..id.Length - 9] else id
                    if stem.StartsWith("fve-select-", StringComparison.Ordinal) then stem else "fve-select-" + ComponentHtml.signalToken stem)
        let visible = $"Array.from(el.closest('[data-fve-select-content]').querySelectorAll('[data-fve-option-value]')).filter(item => !item.disabled && ({state}.settings.search !== 'static' || item.dataset.fveOptionLabel.includes({query}.trim().toLowerCase())))"
        let eligible = $"({ready}) ? ({visible}).map(item => ({{value:item.dataset.fveOptionValue, label:item.dataset.fveOptionText}})) : []"
        let focus = $"{popup}.querySelector({state}.settings.search === 'none' ? '[data-fve-select-options]' : 'input[type=search]')?.focus()"
        let anyMatch = $"Array.from({popup}.querySelectorAll('[data-fve-option-value]')).some(item => {state}.settings.search !== 'static' || item.dataset.fveOptionLabel.includes({query}.trim().toLowerCase()))"
        let busy = $"!({ready})"
        div {
            _attr ("data-fve-select-content", "")
            _attr ("data-fve-select-query", searchText)
            if isLoading then _attr ("data-fve-select-loading", "")
            _popover "auto"
            _role "group"
            _tabindex -1
            partAttribute "aria-label" $"{state}.settings.label + ' options'"
            _dataInit ($"const field = {owner}.dataset.fveSelectField; el.id ||= field + '-popup'; const list = el.querySelector('[data-fve-select-options]'); list.id ||= field + '-options'; list.setAttribute('aria-label', {state}.settings.label); const input = el.querySelector('input[type=search]'); input.id ||= field + '-search'; input.setAttribute('aria-controls', list.id); " + initializePopupPosition)
            _dataEffect $"if (el.isConnected) {{ const root = {owner}; if ({state}.open && !el.matches(':popover-open')) {{ el.showPopover({{source:document.getElementById(root.dataset.fveSelectTrigger)}}); if ({state}.settings.multiple && {state}.settings.search === 'none') el.querySelector('[data-fve-select-options]')?.focus(); requestAnimationFrame(() => el.fvePosition?.()); }} else if (!{state}.open && el.matches(':popover-open')) el.hidePopover(); }}"
            _dataOn ("beforetoggle", $"if (el.isConnected) {{ {state}.open = evt.newState === 'open'; if (evt.newState === 'closed') {{ {state}.active = ''; {state}.typeahead = ''; {state}.request_pending = false; el.querySelector('input[type=search]')?._fveSelectRequest?.abort(); }} }}")
            _dataOn ("toggle", $"if (el.isConnected && evt.newState === 'open' && {state}.settings.search === 'remote' && {query} !== el.dataset.fveSelectQuery) el.querySelector('input[type=search]')?.dispatchEvent(new Event('input', {{bubbles:true}}))")
            _dataOn ("resize", [ "window" ], "if (el.matches(':popover-open')) requestAnimationFrame(() => el.fvePosition?.())")
            _dataOn ("scroll", [ "window"; "capture" ], "if (el.matches(':popover-open') && !(evt.target instanceof Node && el.contains(evt.target))) requestAnimationFrame(() => el.fvePosition?.())")
            for attribute in pointerTracking do attribute
            _style "inset:auto; margin:0.25rem 0; position-area:block-end span-inline-end; position-try-fallbacks:flip-block, flip-inline, flip-block flip-inline; width:anchor-size(width)"
            _class (ComponentHtml.classes [ ComponentHtml.popupClasses; "fve-popup fixed z-30 open:flex max-h-60 flex-col overflow-hidden rounded-[var(--fve-radius-control)] py-1"; ComponentContent.classes attributes ])
            for attribute in partAttributes [ "popover"; "role"; "tabindex"; "aria-label"; "style"; "data-on:"; "data-effect"; "data-init"; "data-fve-select-content"; "data-fve-select-query"; "data-fve-select-loading" ] attributes do attribute
            div {
                partShow $"{state}.settings.search !== 'none'"
                _style "display:none"
                _class "relative shrink-0 p-1"
                input {
                    _type "search"
                    match fieldId with Some id -> _id (id + "-search"); _ariaControls (id + "-options") | None -> ()
                    _autocomplete "off"
                    _placeholder "Search options"
                    _value searchText
                    _class (ComponentHtml.classes [ ComponentHtml.popupFieldClasses; "block min-h-[var(--fve-control-min-height)] w-full rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-10 py-[max(0px,calc(var(--fve-control-padding-block)-1px))] [&::-webkit-search-cancel-button]:appearance-none text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] text-[var(--fve-text)]" ])
                    partAttribute "aria-label" $"'Search ' + {state}.settings.label"
                    partAttribute "aria-busy" $"{busy} ? 'true' : null"
                    partAttribute "aria-activedescendant" $"document.getElementById({state}.active) ? {state}.active : null"
                    partAttribute "disabled" $"{state}.settings.unavailable"
                    _dataEffect $"if (el.isConnected && el.value !== {query}) el.value = {query}"
                    _dataOn ("input", $"{query} = el.value; {state}.open = true; {state}.active = ''; if ({state}.settings.search === 'remote') {{ el._fveSelectRequest?.abort(); delete el._fveSelectRequest; {state}.request_pending = true; }}")
                    _dataOn ("input", [ "debounce.250ms" ], $"if (el.isConnected && {state}.settings.search === 'remote') el.dispatchEvent(new CustomEvent('fve-select-search', {{bubbles:true, detail:{{}}}}))")
                }
                raw """<svg viewBox="0 0 20 20" fill="currentColor" class="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M8.5 3.5a5 5 0 1 0 0 10 5 5 0 0 0 0-10ZM2 8.5a6.5 6.5 0 1 1 11.596 4.047l3.928 3.929a.75.75 0 1 1-1.06 1.06l-3.929-3.928A6.5 6.5 0 0 1 2 8.5Z" clip-rule="evenodd"/></svg>"""
                button {
                    _type "button"
                    _tabindex 0
                    partAttribute "aria-label" $"'Clear Search ' + {state}.settings.label"
                    partAttribute "disabled" $"{state}.settings.unavailable"
                    partShow $"!!{query}"
                    _style "display:none"
                    _class "absolute right-2 top-1/2 grid size-8 -translate-y-1/2 place-items-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                    _dataOn ("click", "const input = el.parentElement.querySelector('input'); input.value = ''; input.dispatchEvent(new Event('input', {bubbles:true})); input.dispatchEvent(new Event('change', {bubbles:true})); input.focus()")
                    raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 1 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""
                }
            }
            div {
                partShow $"{state}.settings.multiple"
                _style "display:none"
                _class "flex shrink-0 items-center justify-between px-2 py-1"
                button {
                    _type "button"
                    _tabindex 0
                    partAttribute "aria-label" $"({state}.settings.search === 'remote' ? 'Select all results ' : 'Select all ') + {state}.settings.label"
                    partAttribute "disabled" $"{state}.settings.unavailable || !({eligible}).some(option => !{selection}.some(item => item.value === option.value))"
                    _dataOn ("click", $"const additions = ({eligible}).filter(option => !{selection}.some(item => item.value === option.value)); {selection} = {selection}.concat(additions); {focus}")
                    _class (ComponentHtml.classes [ ComponentHtml.popupControlClasses; "fve-popup-control inline-flex min-h-8 items-center rounded-[var(--fve-radius-control)] px-2 text-xs leading-4 font-medium text-[var(--fve-muted-text)] hover:text-[var(--fve-text)] disabled:cursor-not-allowed disabled:opacity-50" ])
                    _dataText $"el.isConnected ? ({state}.settings.search === 'remote' ? 'Select all results' : 'Select all') : ''"
                    "Select all"
                }
                button {
                    _type "button"
                    _tabindex 0
                    partAttribute "aria-label" $"'Clear ' + {state}.settings.label"
                    partAttribute "disabled" $"{state}.settings.unavailable || !{selection}.length"
                    _dataOn ("click", $"{selection} = []; {focus}")
                    _class (ComponentHtml.classes [ ComponentHtml.popupControlClasses; "fve-popup-control ml-auto inline-flex min-h-8 items-center rounded-[var(--fve-radius-control)] px-2 text-xs leading-4 font-medium text-[var(--fve-muted-text)] hover:text-[var(--fve-text)] disabled:cursor-not-allowed disabled:opacity-50" ])
                    "Clear selection"
                }
            }
            div {
                _attr ("data-fve-select-options", "")
                match fieldId with Some id -> _id (id + "-options") | None -> ()
                _role "listbox"
                _tabindex 0
                _dataPreserveAttr "id aria-label aria-multiselectable aria-required aria-busy aria-activedescendant"
                _class "min-h-0 overflow-auto outline-none"
                partAttribute "aria-label" $"{state}.settings.label"
                partAttribute "aria-multiselectable" $"{state}.settings.multiple ? 'true' : null"
                partAttribute "aria-required" $"{state}.settings.required ? 'true' : 'false'"
                partAttribute "aria-busy" $"{busy} ? 'true' : null"
                partAttribute "aria-activedescendant" $"document.getElementById({state}.active) ? {state}.active : null"
                if failure.IsNone then for child in children do child
            }
            p {
                _role "status"
                partShow busy
                _style "display:none"
                _class "flex shrink-0 items-center justify-center gap-2 px-3 py-4 text-center text-sm text-[var(--fve-muted-text)]"
                ComponentHtml.loadingGlyph Size.Small
                loadingMessage
            }
            match failure with
            | Some message ->
                div {
                    partShow ready
                    _class "grid shrink-0 justify-items-center gap-2 px-3 py-4 text-center"
                    p { _role "alert"; _class "text-sm text-[var(--fve-critical-text)]"; message }
                    match retryEndpoint with
                    | Some endpoint ->
                        button {
                            _type "button"
                            _tabindex 0
                            _dataOn ("click", $"el.dispatchEvent(new CustomEvent('fve-select-search', {{bubbles:true, detail:{{endpoint:{ComponentHtml.javascriptString endpoint}}}}})); {focus}")
                            _class (ComponentHtml.classes [ ComponentHtml.popupControlClasses; "fve-popup-control inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] border border-[var(--fve-brand-ring)] px-3 py-[max(0px,calc(var(--fve-control-padding-block)-1px))] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-brand-text)] hover:bg-[var(--fve-brand-subtle)]" ])
                            "Retry"
                        }
                    | None -> ()
                }
            | None ->
                p {
                    _role "status"
                    partShow $"{ready} && !({anyMatch})"
                    _style "display:none"
                    _class "shrink-0 px-3 py-4 text-center text-sm text-[var(--fve-muted-text)]"
                    emptyMessage
                }
        }

    let private renderPlain (children:HtmlElement list) config =
        let instanceId = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let fieldId = $"fve-select-{instanceId}"
        let triggerId = config.id |> Option.defaultValue config.name
        let popupId = $"{fieldId}-popup"
        let listboxId = $"{fieldId}-options"
        let openSignal = $"_fveSelect.{instanceId}.open"
        let popupElement = contentElement instanceId
        let listboxElement = $"({popupElement})?.querySelector('[data-fve-select-options]')"
        let selectionSignal = $"{instanceId}_selected"
        let initialSelection = config.selectedMany |> List.map (fun choice -> { SelectedChoice.value = choice.value; label = choice.label })
        let valueSignal = $"{instanceId}_value"
        let labelSignal = $"_fveSelect.{instanceId}.label"
        let activeSignal = $"_fveSelect.{instanceId}.active"
        let typeaheadSignal = $"_fveSelect.{instanceId}.typeahead"
        let typeaheadTimeSignal = $"_fveSelect.{instanceId}.typeahead_time"
        let unavailable = config.isDisabled || config.isReadOnly
        let selectedChoice =
            if config.isMultiple then config.selectedMany |> List.tryHead
            else config.options |> List.tryFind (fun option -> Some option.value = config.selected)
        let selectedValue = selectedChoice |> Option.map _.value |> Option.defaultValue ""
        let selectedLabel =
            selectedChoice
            |> Option.map _.label
            |> Option.orElse config.placeholder
            |> Option.defaultValue "Select an option"
        let enabledOptions = $"Array.from({listboxElement}?.querySelectorAll('[role=option]:not(:disabled)') || [])"
        let firstOption = $"{enabledOptions}.at(0)"
        let lastOption = $"{enabledOptions}.at(-1)"
        let selectedOption = $"{listboxElement}?.querySelector('[aria-selected=true]:not(:disabled)')"
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
        let restoreFocus = if config.isMultiple then $", document.getElementById({ComponentHtml.javascriptString triggerId}).focus()" else ""
        let tabCommit = if config.isMultiple then $"document.getElementById({ComponentHtml.javascriptString triggerId}).focus()" else commitActive
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
        let describedBy = ComponentElement.value "aria-describedby" config.attributes |> Option.defaultValue ""
        let triggerClasses =
            ComponentHtml.classes [
                ComponentHtml.popupFieldClasses
                "fve-popup-field fve-popup-control group flex min-h-[var(--fve-control-min-height)] w-full items-center justify-between gap-3 rounded-[var(--fve-radius-control)] border bg-[var(--fve-surface)] px-3 py-[max(0px,calc(var(--fve-control-padding-block)-1px))] text-left text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-normal text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] disabled:cursor-not-allowed disabled:opacity-50"
                (if config.invalid then "border-[var(--fve-critical-ring)]" else "border-[var(--fve-border)]") ]
        let renderValue attributes (children:HtmlElement list) = span {
            _class (ComponentHtml.classes [ "min-w-0 truncate"; ComponentContent.classes attributes ])
            for attribute in partAttributes [ "id"; "data-text" ] attributes do attribute
            if config.isMultiple then _id (triggerId + "-selection")
            if List.isEmpty children then
                if config.isMultiple then
                    _dataText (ChoiceSelection.summary selectionSignal (config.placeholder |> Option.defaultValue "Select options"))
                    if initialSelection.IsEmpty then config.placeholder |> Option.defaultValue "Select options"
                    else initialSelection |> List.map _.label |> String.concat ", "
                else
                    _dataText $"${labelSignal}"
                    selectedLabel
            else for child in children do child
        }
        let suffix () = if config.isMultiple then ChoiceSelection.announcement selectionSignal initialSelection else empty
        let root (body:HtmlElement) = div {
            for attribute in rootAttributes instanceId fieldId triggerId ($"_{instanceId}_query") do attribute
            _class (ComponentHtml.classes [ ComponentHtml.controlSizeClass config.size; "relative grid min-w-0 grid-cols-1 content-start gap-1.5" ])
            _id (fieldId + "-field")
            if not unavailable then
                _dataOn ("keydown", $"if (evt.target.closest('[data-fve-select]') === el && evt.target.matches('[data-fve-select-options]')) {{ {keydown}; }}")
                let toggle =
                    if config.isMultiple then $"${openSignal} = !${openSignal}"
                    else $"document.getElementById({ComponentHtml.javascriptString triggerId}).focus(); ${openSignal} ? (${openSignal} = false) : ({openAt selectedOrFirst})"
                _dataOn ("fve-select-toggle", [ "stop" ], $"if (evt.detail.owner === '{fieldId}') {{ {toggle}; }}")
                let choose =
                    if config.isMultiple then
                        $"{toggleSelected selectionSignal}; {listboxElement}.focus()"
                    else
                        $"const previousValue = ${valueSignal}; ${valueSignal} = evt.detail.value; ${labelSignal} = evt.detail.label; ${openSignal} = false; document.getElementById({ComponentHtml.javascriptString triggerId}).focus(); {notifyChange fieldId valueSignal}"
                _dataOn ("fve-select-choice", [ "stop" ], $"if (evt.detail.owner === '{fieldId}') {{ ${activeSignal} = evt.detail.id; ${typeaheadSignal} = ''; {choose}; }}")
            let initialValue = if config.nativeFallback then $"el.querySelector('[data-fve-select-native]')?.value ?? {ComponentHtml.javascriptString selectedValue}" else ComponentHtml.javascriptString selectedValue
            let initialLabel = if config.nativeFallback then $"el.querySelector('[data-fve-select-native]')?.selectedOptions[0]?.text || {ComponentHtml.javascriptString selectedLabel}" else ComponentHtml.javascriptString selectedLabel
            let initialSignals = $"{{_fveSelect:{{{ComponentHtml.javascriptString instanceId}:{{open:false,label:{initialLabel},active:'',typeahead:'',typeahead_time:0,request_pending:false,settings:{settings config}}}}}, {valueSignal}:{initialValue}, _{instanceId}_query:''}}"
            if not config.isMultiple then _dataSignals (initialSignals.TrimEnd('}') + $", {selectionSignal}: []}}")
            if config.isMultiple then
                _attr ("data-signals__ifmissing", initialSignals.TrimEnd('}') + $", {selectionSignal}: {ChoiceSelection.json initialSelection}}}")
            let reconcileSelection =
                if not config.isMultiple then ""
                else
                    let allowed = config.options |> List.map _.value |> System.Text.Json.JsonSerializer.Serialize
                    $"const allowed = {allowed}; const kept = ${selectionSignal}.filter(item => allowed.includes(item.value)); if (kept.length !== ${selectionSignal}.length) ${selectionSignal} = kept"
            _dataEffect (synchronizeName triggerId instanceId config + reconcileSelection)
            _dataInit ((if config.nativeFallback then $"const native = el.querySelector('[data-fve-select-native]'); const enhanced = el.querySelector('[data-fve-select-trigger-control]'); native.id = '{fieldId}-native'; enhanced.id = {ComponentHtml.javascriptString triggerId}; " else "") + accessibleName triggerId config + $"$_fveSelect.{instanceId}.settings = {{...{settings config}, label}}; " + (if config.nativeFallback then $"const previousValue = {ComponentHtml.javascriptString selectedValue}; const nativeFocused = document.activeElement?.id === '{fieldId}-native'; queueMicrotask(() => {{ if (!el.isConnected) return; if (nativeFocused) document.getElementById({ComponentHtml.javascriptString triggerId}).focus(); {notifyChange fieldId valueSignal} }});" else ""))
            if config.nativeFallback then renderNative fieldId describedBy triggerClasses config
            if config.isMultiple then
                ChoiceSelection.render config.name selectionSignal initialSelection config.isDisabled
            else
                input {
                    _id (fieldId + "-value")
                    _type "hidden"
                    _name config.name
                    _value selectedValue
                    _disabled (config.isDisabled || (config.nativeFallback && not config.isReadOnly))
                    if config.nativeFallback then _dataAttr ("disabled", if config.isDisabled then "true" else "false")
                    _dataBind valueSignal
                }
            body
            suffix ()
        }
        let renderTrigger attributes (children:HtmlElement list) = button {
                _id (if config.nativeFallback then fieldId + "-enhanced" else triggerId)
                _type "button"
                _attr("data-fve-select-trigger-control", "")
                _data ("fve-group-control", "outline")
                _tabindex 0
                _popovertarget popupId
                if not config.isMultiple then _role "combobox"
                _ariaHaspopup "listbox"
                _ariaControls listboxId
                _dataAttr ("aria-controls", $"(${openSignal}, {listboxElement}?.id || '{listboxId}')")
                _dataAttr ("popovertarget", $"(${openSignal}, ({popupElement})?.id || '{popupId}')")
                _ariaExpanded false
                if not config.isMultiple then _ariaRequired config.isRequired
                _ariaDisabled unavailable
                _ariaInvalid config.invalid
                _disabled unavailable
                if config.isPending then _ariaBusy true
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                if not config.isMultiple then
                    _dataAttr ("aria-activedescendant", $"${openSignal} && document.getElementById(${activeSignal}) ? ${activeSignal} : null")
                if config.isMultiple then _ariaDescribedby (String.concat " " ([ triggerId + "-selection" ] @ (if describedBy = "" then [] else [ describedBy ])))
                elif String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                _dataOn ("click", [ "prevent"; "stop" ], intent fieldId "toggle" "")
                _dataOn ("keydown", keydown)
                if config.nativeFallback then
                    _style "display:none"
                    _dataShow "true"
                _class (ComponentHtml.classes [ triggerClasses; config.controlClass; ComponentContent.classes attributes ])
                for attribute in partAttributes [ "id"; "type"; "name"; "value"; "disabled"; "required"; "role"; "aria-haspopup"; "aria-controls"; "aria-describedby"; "aria-invalid"; "aria-expanded"; "aria-activedescendant"; "aria-required"; "aria-disabled"; "aria-busy"; "data-bind:"; "data-attr:"; "data-on:" ] (config.attributes @ attributes) do attribute
                if List.isEmpty children then renderValue [] []
                else for child in children do child
                if config.isPending then
                    ComponentHtml.loadingGlyph Size.Small
                else
                    raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4 shrink-0 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
            }
        root (fragment { renderTrigger [] []; for child in children do child })

    let private searchableRequest endpoint instanceId =
        $"""(() => {{
const popup = {contentElement instanceId};
const field = popup?.querySelector('input[type=search]');
if (!field || field.disabled || !popup?.matches(':popover-open')) return Promise.resolve();
field._fveSelectRequest?.abort();
const controller = new AbortController();
field._fveSelectRequest = controller;
const observer = new MutationObserver(() => {{ if (!field.isConnected || field.disabled) controller.abort(); }});
const abortOnPageHide = () => controller.abort();
window.addEventListener('pagehide', abortOnPageHide, {{once: true}});
observer.observe(document, {{childList: true, subtree: true, attributes: true, attributeFilter: ['disabled']}});
$_fveSelect.{instanceId}.request_pending = true;
return @get(evt.detail.endpoint || {ComponentHtml.javascriptString endpoint}, {{requestCancellation: controller, filterSignals: {{include: /^{instanceId}_(query|value|selected)$/}}}}).finally(() => {{
    observer.disconnect();
    window.removeEventListener('pagehide', abortOnPageHide);
    if (field._fveSelectRequest === controller) {{
        delete field._fveSelectRequest;
        if (field.isConnected) $_fveSelect.{instanceId}.request_pending = false;
    }}
}});
}})()"""

    let private renderSearchable (children:HtmlElement list) (config:SelectConfig) =
        let instanceId = config.id |> Option.defaultValue config.name |> ComponentHtml.signalToken
        let search = config.search |> Option.defaultValue SelectSearch.Static
        let fieldId = $"fve-select-{instanceId}"
        let popupId = $"{fieldId}-popup"
        let triggerId = config.id |> Option.defaultValue config.name
        let openSignal = $"_fveSelect.{instanceId}.open"
        let popupElement = contentElement instanceId
        let searchElement = $"({popupElement})?.querySelector('input[type=search]')"
        let querySignal =
            match search with
            | SelectSearch.Static -> $"_{instanceId}_query"
            | SelectSearch.Remote _ -> $"{instanceId}_query"
        let valueSignal = $"{instanceId}_value"
        let labelSignal = $"_fveSelect.{instanceId}.label"
        let activeSignal = $"_fveSelect.{instanceId}.active"
        let requestPendingSignal = $"_fveSelect.{instanceId}.request_pending"
        let unavailable = config.isDisabled || config.isReadOnly
        let selectionSignal = $"{instanceId}_selected"
        let initialSelection = config.selectedMany |> List.map (fun choice -> { SelectedChoice.value = choice.value; label = choice.label })
        let visibleOptions = $"Array.from(({popupElement})?.querySelectorAll('[role=option]:not(:disabled)') || []).filter(item => item.style.display != 'none')"
        let firstVisibleOption = $"{visibleOptions}.at(0)"
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
                $"evt.key == 'Escape' && ${openSignal} && (evt.preventDefault(), ${openSignal} = false, document.getElementById({ComponentHtml.javascriptString triggerId})?.focus())"
                (if config.isMultiple then "" else $"evt.key == 'Tab' && (${openSignal} = false)") ]
        let selectedChoice = config.options |> List.tryFind (fun option -> Some option.value = config.selected)
        let selectedValue = selectedChoice |> Option.map _.value |> Option.defaultValue ""
        let selectedLabel = selectedChoice |> Option.map _.label |> Option.defaultValue ""
        let describedBy = ComponentElement.value "aria-describedby" config.attributes |> Option.defaultValue ""
        let triggerClasses = ComponentHtml.classes [
            ComponentHtml.popupFieldClasses
            "fve-popup-field fve-popup-control group flex min-h-[var(--fve-control-min-height)] w-full items-center justify-between gap-3 rounded-[var(--fve-radius-control)] border bg-[var(--fve-surface)] px-3 py-[max(0px,calc(var(--fve-control-padding-block)-1px))] text-left text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-normal text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] disabled:cursor-not-allowed disabled:opacity-50"
            if config.invalid then "border-[var(--fve-critical-ring)]" else "border-[var(--fve-border)]" ]
        let renderValue attributes (children:HtmlElement list) = span {
            _class (ComponentHtml.classes [ "min-w-0 truncate text-left"; ComponentContent.classes attributes ])
            for attribute in partAttributes [ "id"; "data-text" ] attributes do attribute
            if config.isMultiple then _id (fieldId + "-selection")
            if List.isEmpty children then
                if config.isMultiple then
                    _dataText (ChoiceSelection.summary selectionSignal (config.placeholder |> Option.defaultValue "Select options"))
                    if initialSelection.IsEmpty then config.placeholder |> Option.defaultValue "Select options"
                    else initialSelection |> List.map _.label |> String.concat ", "
                else
                    _dataText $"${labelSignal}"
                    selectedLabel
            else for child in children do child
        }
        let root (body:HtmlElement) = div {
            for attribute in rootAttributes instanceId fieldId triggerId querySignal do attribute
            _class (ComponentHtml.classes [ ComponentHtml.controlSizeClass config.size; "relative grid min-w-0 grid-cols-1 content-start gap-1.5" ])
            if not unavailable then
                _dataOn ("keydown", $"if (evt.target.closest('[data-fve-select]') === el && evt.target.matches('input[type=search]')) {{ {keydown}; }}")
                match search with
                | SelectSearch.Remote endpoint -> _dataOn ("fve-select-search", [ "stop" ], searchableRequest endpoint instanceId)
                | SelectSearch.Static -> ()
                _dataOn ("fve-select-toggle", [ "stop" ], $"if (evt.detail.owner === '{fieldId}') {{ ${openSignal} = !${openSignal}; ${openSignal} && queueMicrotask(() => {searchElement}?.focus()); }}")
                let choose =
                    if config.isMultiple then toggleSelected selectionSignal
                    else $"const previousValue = ${valueSignal}; ${valueSignal} = evt.detail.value; ${labelSignal} = evt.detail.label; ${querySignal} = ''; ${openSignal} = false; {notifyChange fieldId valueSignal}"
                _dataOn ("fve-select-choice", [ "stop" ], $"if (evt.detail.owner === '{fieldId}') {{ ${activeSignal} = evt.detail.id; {choose}; {searchElement}?.focus(); }}")
            _id (fieldId + "-field")
            let initialQuery = config.query |> Option.defaultValue "" |> ComponentHtml.javascriptString
            let initialSignals = $"{{_fveSelect:{{{ComponentHtml.javascriptString instanceId}:{{open:false,label:{ComponentHtml.javascriptString selectedLabel},active:'',typeahead:'',typeahead_time:0,request_pending:false,settings:{settings config}}}}}, {querySignal}:{initialQuery}, {valueSignal}:{ComponentHtml.javascriptString selectedValue}}}"
            if config.isMultiple then
                _attr ("data-signals__ifmissing", initialSignals.TrimEnd('}') + $", {selectionSignal}: {ChoiceSelection.json initialSelection}}}")
            else _attr ("data-signals__ifmissing", initialSignals.TrimEnd('}') + $", {selectionSignal}: []}}")
            _dataInit (accessibleName triggerId config + $"$_fveSelect.{instanceId}.settings = {{...{settings config}, label}}; if (!{searchElement}?._fveSelectRequest) ${requestPendingSignal} = false")
            _dataEffect (synchronizeName triggerId instanceId config)
            body
            if config.isMultiple then
                ChoiceSelection.render config.name selectionSignal initialSelection config.isDisabled
                ChoiceSelection.announcement selectionSignal initialSelection
            else
                input {
                    _id (fieldId + "-value")
                    _type "hidden"
                    _name config.name
                    _value selectedValue
                    _disabled config.isDisabled
                    _dataBind valueSignal
                }
        }
        let renderTrigger attributes (children:HtmlElement list) = button {
                _id triggerId
                _type "button"
                _attr("data-fve-select-trigger-control", "")
                _data ("fve-group-control", "outline")
                _ariaHaspopup "dialog"
                _ariaControls popupId
                _dataAttr ("aria-controls", $"(${openSignal}, ({popupElement})?.id || '{popupId}')")
                _ariaExpanded false
                _ariaDisabled unavailable
                _ariaInvalid config.invalid
                _disabled unavailable
                if config.isPending then _ariaBusy true
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                if String.IsNullOrEmpty describedBy |> not then _ariaDescribedby describedBy
                if unavailable |> not then
                    _dataOn ("click", [ "prevent"; "stop" ], intent fieldId "toggle" "")
                    _dataOn ("keydown", $"(evt.key == 'Enter' || evt.key == ' ' || evt.key == 'ArrowDown') && (evt.preventDefault(), ${openSignal} = true, queueMicrotask(() => {searchElement}?.focus())); evt.key == 'Escape' && ${openSignal} && (evt.preventDefault(), ${openSignal} = false)")
                _class (ComponentHtml.classes [ triggerClasses; config.controlClass; ComponentContent.classes attributes ])
                for attribute in partAttributes [ "id"; "type"; "name"; "disabled"; "required"; "aria-haspopup"; "aria-controls"; "aria-describedby"; "aria-invalid"; "aria-expanded"; "aria-activedescendant"; "aria-disabled"; "aria-busy"; "data-bind:"; "data-attr:"; "data-on:" ] (config.attributes @ attributes) do attribute
                if List.isEmpty children then renderValue [] []
                else for child in children do child
                raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4 shrink-0 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
            }
        root (fragment { renderTrigger [] []; for child in children do child })

    let internal renderRoot children config =
        match config.search with
        | Some _ -> renderSearchable children config
        | None -> renderPlain children config


    let private choices (children:HtmlElement list) =
        children |> List.collect (function :? SelectPart as part -> part.Choices | _ -> [])

    let private itemChoice attributes (children:HtmlElement list) =
        let value =
            match ComponentElement.value "value" attributes with
            | Some value when not (isNull value) -> value
            | _ -> invalidArg "attributes" "A Select item requires a string value."
        let label =
            match ComponentElement.value "label" attributes with
            | Some label when not (String.IsNullOrWhiteSpace label) -> label
            | _ ->
                if children |> List.exists (function :? TextElement | :? NoopElement -> false | _ -> true) then
                    invalidArg "attributes" "Rich Select items require a plain label attribute for search and selected text."
                let text = System.Net.WebUtility.HtmlDecode(Render.toString (ComponentContent.fragment children))
                if String.IsNullOrWhiteSpace text then invalidArg "children" "A Select item needs text or an explicit label."
                text
        { value = value; label = label; disabled = flag "disabled" attributes }

    let private ownedChildren owner (children:HtmlElement list) =
        children |> List.map (function :? SelectPart as part -> part.RenderOwned owner | child -> child)

    let item attributes children =
        let choice = itemChoice attributes children
        SelectPart("item", [choice], fun owner ->
            let attributes =
                match owner, ComponentElement.attribute "id" attributes with
                | Some owner, None -> _id (owner + "-option-" + ComponentHtml.optionToken choice.value) :: attributes
                | _ -> attributes
            renderItem choice (ComponentHtml.safeAttributes ["value"; "label"] attributes) children) :> HtmlElement

    let selectedItem attributes children =
        let choice = itemChoice attributes children
        SelectPart("selected", [choice], fun _ -> empty) :> HtmlElement

    let content attributes children =
        let items = choices children
        ChoiceSelection.validateKeys _.value items
        let optional name = ComponentElement.value name attributes |> Option.filter (String.IsNullOrWhiteSpace >> not)
        SelectPart("content", items, fun owner ->
            let attributes =
                match owner, ComponentElement.attribute "id" attributes with
                | Some owner, None -> _id (owner + "-popup") :: attributes
                | _ -> attributes
            renderContent owner (ComponentHtml.safeAttributes ["query"; "loading"; "error"; "retry"; "empty-message"; "loading-message"] attributes) (ownedChildren owner children)
                (optional "query" |> Option.defaultValue "") (flag "loading" attributes) (optional "error") (optional "retry")
                (optional "empty-message" |> Option.defaultValue "No matching options") (optional "loading-message" |> Option.defaultValue "Loading options")) :> HtmlElement

    let group attributes children =
        let label = ComponentElement.required "label" attributes
        SelectPart("group", choices children, fun owner ->
            ComponentElement.render "div" [_role "group"; _ariaLabel label; _class "mt-1 first:mt-0"] (ComponentHtml.safeAttributes ["label"] attributes)
                [ div { _ariaHidden true; _class "px-2 py-1 text-xs font-semibold text-[var(--fve-muted-text)]"; label }
                  yield! ownedChildren owner children ]) :> HtmlElement

    let root isMultiple attributes (children:HtmlElement list) =
        let attributes = ComponentElement.attributes attributes
        let name = ComponentElement.required "name" attributes
        let controlId = ComponentElement.value "id" attributes |> Option.defaultValue name
        if String.IsNullOrWhiteSpace controlId || controlId |> Seq.exists Char.IsWhiteSpace then invalidArg "attributes" "A stable Select ID without whitespace is required."
        let contentParts = children |> List.choose (function :? SelectPart as part when part.Kind = "content" -> Some part | _ -> None)
        if contentParts.Length <> 1 then invalidArg "children" "Select requires exactly one fveSelectContent."
        let items = contentParts.Head.Choices
        let selectedItems = children |> List.collect (function :? SelectPart as part when part.Kind = "selected" -> part.Choices | _ -> [])
        ChoiceSelection.validateKeys _.value items
        ChoiceSelection.validateKeys _.value selectedItems
        if (ComponentElement.attribute (if isMultiple then "value" else "values") attributes).IsSome then
            invalidArg "attributes" "Use value for Select and values for MultiSelect."
        let selection =
            if isMultiple then
                match ComponentElement.value "values" attributes with
                | None -> []
                | Some json ->
                    try
                        let values = System.Text.Json.JsonSerializer.Deserialize<string array>(json)
                        if isNull values || values |> Array.exists isNull then invalidArg "attributes" "Selected values cannot be null."
                        values |> Array.toList |> List.distinct
                    with :? System.Text.Json.JsonException -> invalidArg "attributes" "MultiSelect values must be a JSON array of strings."
            else ComponentElement.value "value" attributes |> Option.toList
        let allItems = items @ (selectedItems |> List.filter (fun selected -> items |> List.exists (fun item -> item.value = selected.value) |> not))
        let selected = selection |> List.map (fun value -> allItems |> List.tryFind (fun item -> item.value = value) |> Option.defaultWith (fun () -> invalidArg "attributes" "Every selected value needs an item or fveSelectSelectedItem label."))
        let search =
            ComponentElement.value "search" attributes |> Option.map (function "static" -> SelectSearch.Static | endpoint when not (String.IsNullOrWhiteSpace endpoint) -> SelectSearch.Remote endpoint | _ -> invalidArg "attributes" "A search mode or endpoint is required.")
        let nativeFallback = flag "native-fallback" attributes
        if nativeFallback && (isMultiple || search.IsSome) then invalidArg "attributes" "Native fallback is available only for ordinary single selects."
        let size = ComponentElement.size None attributes
        let config =
            { isMultiple = isMultiple; selectedMany = selected; search = search; query = ComponentElement.value "query" attributes
              name = name; id = Some controlId; options = allItems; selected = (if isMultiple then None else selection |> List.tryHead)
              label = ComponentElement.value "aria-label" attributes |> Option.defaultValue ""; nativeFallback = nativeFallback
              placeholder = ComponentElement.value "placeholder" attributes; invalid = flag "aria-invalid" attributes
              isRequired = flag "required" attributes; isDisabled = flag "disabled" attributes; isPending = flag "aria-busy" attributes; isReadOnly = flag "readonly" attributes
              size = size; position = (match ComponentElement.choice "position" "selected-item" ["selected-item"; "trigger-start"; "trigger-end"] attributes with "trigger-start" -> SelectPosition.TriggerStart | "trigger-end" -> SelectPosition.TriggerEnd | _ -> SelectPosition.SelectedItem)
              controlClass = ComponentContent.classes attributes
              attributes = ComponentHtml.safeAttributes ["class"; "size"; "search"; "position"; "native-fallback"; "query"; "values"; "value"; "readonly"; "placeholder"] attributes }
        let body = children |> List.filter (function :? SelectPart as part when part.Kind = "selected" -> false | _ -> true)
        renderRoot (ownedChildren (Some ("fve-select-" + ComponentHtml.signalToken controlId)) body) config

/// <category>select</category>
[<AutoOpen>]
module SelectDsl =
    /// Single string-valued form control. Domain values are serialized by the application.
    let fveSelect = ComponentElementBuilder(Select.root false)
    /// Repeated-name form values. Selected values use _values.
    let fveMultiSelect = ComponentElementBuilder(Select.root true)
    let fveSelectContent = ComponentElementBuilder(Select.content)
    let fveSelectItem = ComponentElementBuilder(Select.item)
    let fveSelectGroup = ComponentElementBuilder(Select.group)
    /// Selected labels omitted from a remote result page. Does not render an option.
    let fveSelectSelectedItem = ComponentElementBuilder(Select.selectedItem)
