namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<RequireQualifiedAccess>]
type MenuTone =
    | Default
    | Destructive

[<RequireQualifiedAccess>]
type MenuAlignment =
    | Start
    | End

[<RequireQualifiedAccess>]
type MenuTriggerPresentation =
    | Button
    | Overflow
    | Icon

[<NoEquality; NoComparison>]
type private MenuItemContent =
    { label:string
      leading:HtmlElement option
      shortcut:string option
      disabled:bool
      pending:bool
      className:string option }

[<NoEquality; NoComparison>]
type MenuItem<'destination> =
    private
        | Link of content:MenuItemContent * destination:'destination * tone:MenuTone
        | Action of content:MenuItemContent * datastarExpression:string * tone:MenuTone
        | Radio of content:MenuItemContent * datastarExpression:string * isChecked:bool * checkedExpression:string option
        | Separator
        | Group of label:string * items:MenuItem<'destination> list

[<NoEquality; NoComparison>]
type DropdownMenuConfig<'destination> =
    private
        { id:string
          label:string
          items:MenuItem<'destination> list
          alignment:MenuAlignment
          triggerContent:HtmlElement option
          triggerPresentation:MenuTriggerPresentation }

[<RequireQualifiedAccess>]
module MenuItem =
    let private content label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu item label is required."
        { label = label
          leading = None
          shortcut = None
          disabled = false
          pending = false
          className = None }

    let private mapContent update item =
        match item with
        | Link(itemContent, destination, tone) -> Link(update itemContent, destination, tone)
        | Action(itemContent, expression, tone) -> Action(update itemContent, expression, tone)
        | Radio(itemContent, expression, isChecked, checkedExpression) -> Radio(update itemContent, expression, isChecked, checkedExpression)
        | Separator -> invalidArg (nameof item) "A separator cannot have item presentation."
        | Group _ -> invalidArg (nameof item) "A group cannot have item presentation."

    let link destination label = Link(content label, destination, MenuTone.Default)
    let destructiveLink destination label = Link(content label, destination, MenuTone.Destructive)
    let action datastarExpression label = Action(content label, datastarExpression, MenuTone.Default)
    /// A mutually exclusive menu choice. The caller owns its checked state and selection action.
    let radio datastarExpression label = Radio(content label, datastarExpression, false, None)
    let withChecked isChecked = function
        | Radio(content, expression, _, checkedExpression) -> Radio(content, expression, isChecked, checkedExpression)
        | _ -> invalidArg "item" "Only radio menu items have a checked state."
    /// A trusted Datastar expression for client-local choice state.
    let withCheckedExpression checkedExpression = function
        | Radio(content, expression, isChecked, _) -> Radio(content, expression, isChecked, Some checkedExpression)
        | _ -> invalidArg "item" "Only radio menu items have a checked state."
    let destructiveAction datastarExpression label = Action(content label, datastarExpression, MenuTone.Destructive)
    let separator<'destination> : MenuItem<'destination> = Separator

    let group label items =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu group label is required."
        if List.isEmpty items then invalidArg (nameof items) "A menu group requires at least one item."
        if items |> List.exists (function | Group _ -> true | _ -> false) then
            invalidArg (nameof items) "Menu groups cannot be nested."
        Group(label, items)

    let disabled item = item |> mapContent (fun content -> { content with disabled = true })
    let pending item = item |> mapContent (fun content -> { content with pending = true })
    let withLeading leading item = item |> mapContent (fun content -> { content with leading = Some leading })
    let internal withClass className item = item |> mapContent (fun content -> { content with className = Some className })

    let withShortcut shortcut item =
        if String.IsNullOrWhiteSpace shortcut then invalidArg (nameof shortcut) "A visible shortcut is required."
        item |> mapContent (fun content -> { content with shortcut = Some shortcut })

    let internal isSeparator = function
        | Separator -> true
        | _ -> false

    let rec internal isDestructive = function
        | Link(_, _, MenuTone.Destructive)
        | Action(_, _, MenuTone.Destructive) -> true
        | Group(_, items) -> items |> List.exists isDestructive
        | _ -> false

[<RequireQualifiedAccess>]
module DropdownMenu =
    let create id label items =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable menu ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu label is required."
        { id = id
          label = label
          items = items
          alignment = MenuAlignment.End
          triggerContent = None
          triggerPresentation = MenuTriggerPresentation.Button }

    let withAlignment alignment config = { config with alignment = alignment }
    let withTriggerContent content config = { config with triggerContent = Some content }
    let asOverflow config = { config with triggerPresentation = MenuTriggerPresentation.Overflow }
    /// An icon-only trigger retaining the menu's accessible label.
    let withIconTrigger icon config =
        { config with triggerPresentation = MenuTriggerPresentation.Icon; triggerContent = Some icon }

    let render resolve config =
        let instanceId = ComponentHtml.signalToken config.id
        let openSignal = $"_{instanceId}_open"
        let overflowIcon =
            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5" aria-hidden="true"><path d="M3.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM8.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM13.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0Z"/></svg>"""
        let typeaheadSignal = $"_{instanceId}_typeahead"
        let typeaheadTimeSignal = $"_{instanceId}_typeahead_time"
        let triggerId = $"{config.id}-trigger"
        let menuId = $"{config.id}-menu"
        let positionArea =
            match config.alignment with
            | MenuAlignment.Start -> "block-end span-inline-end"
            | MenuAlignment.End -> "block-end span-inline-start"
        let enabledItems = $"Array.from(document.querySelectorAll('#{menuId} :is([role=menuitem], [role=menuitemradio]):not([aria-disabled=true])')).filter(item => item.getClientRects().length)"
        let firstItem = $"{enabledItems}.at(0)"
        let lastItem = $"{enabledItems}.at(-1)"
        let currentIndex = $"{enabledItems}.indexOf(document.activeElement)"
        let focus item = $"({item})?.focus()"
        // WebKit can retarget a stationary pointer after focus scrolling; only intentional coordinate changes move focus.
        let menuElement = $"document.getElementById('{menuId}')"
        let pointerMoved = $"(evt.clientX != Number({menuElement}.dataset.fvePointerX) || evt.clientY != Number({menuElement}.dataset.fvePointerY))"
        let rememberPointer = "el.dataset.fvePointerX = evt.clientX; el.dataset.fvePointerY = evt.clientY"
        let move offset =
            let missingIndex = if offset > 0 then -1 else 0
            $"evt.preventDefault(), {enabledItems}.length && {enabledItems}.at((({currentIndex} < 0 ? {missingIndex} : {currentIndex}) + {offset} + {enabledItems}.length) %% {enabledItems}.length)?.focus()"
        let searchText = $"(Array.from(${typeaheadSignal}).every(character => character == ${typeaheadSignal}[0]) ? ${typeaheadSignal}[0] : ${typeaheadSignal})"
        let startIndex = $"Math.max(0, {currentIndex} + 1)"
        let orderedItems = $"{enabledItems}.slice({startIndex}).concat({enabledItems}.slice(0, {startIndex}))"
        let typeaheadMatch = $"{orderedItems}.find(item => item.dataset.fveMenuLabel.startsWith({searchText}))"
        let typeahead =
            $"!evt.ctrlKey && !evt.metaKey && !evt.altKey && evt.key.length == 1 && evt.key != ' ' && (evt.preventDefault(), ${typeaheadSignal} = Date.now() - ${typeaheadTimeSignal} > 700 ? evt.key.toLowerCase() : ${typeaheadSignal} + evt.key.toLowerCase(), ${typeaheadTimeSignal} = Date.now(), {focus typeaheadMatch})"
        let menuIsOpen = $"{menuElement}.matches(':popover-open')"
        let cleanupPosition =
            $"(() => {{ const menu = {menuElement}; if (menu._fvePosition) {{ window.removeEventListener('scroll', menu._fvePosition, true); window.removeEventListener('resize', menu._fvePosition); delete menu._fvePosition; menu.style.setProperty('position-area', menu.dataset.fvePositionArea); menu.style.removeProperty('left'); menu.style.removeProperty('top') }} }})()"
        let horizontalPosition =
            match config.alignment with
            | MenuAlignment.Start -> "trigger.left"
            | MenuAlignment.End -> "trigger.right - width"
        let preparePosition =
            $"el.closest('[data-fve-sticky-cell=true]') && (() => {{ const menu = {menuElement}; const trigger = el.getBoundingClientRect(); menu.style.removeProperty('position-area'); const position = () => {{ const trigger = el.getBoundingClientRect(); const width = menu.offsetWidth; const height = menu.offsetHeight; const padding = 16; const gap = parseFloat(getComputedStyle(menu).marginTop); const left = Math.min(Math.max(padding, {horizontalPosition}), window.innerWidth - width - padding); const below = trigger.bottom + gap; const preferredTop = below + height + padding <= window.innerHeight ? below : trigger.top - height - gap; const top = Math.min(Math.max(padding, preferredTop), window.innerHeight - height - padding); menu.style.left = `${{left}}px`; menu.style.top = `${{top - gap}}px` }}; menu._fvePosition = position; window.addEventListener('scroll', position, true); window.addEventListener('resize', position); position() }})()"
        let hideMenu = $"{menuIsOpen} && ({menuElement}.hidePopover(), {cleanupPosition})"
        let closeAndRestore = $"{hideMenu}, ${typeaheadSignal} = '', document.getElementById('{triggerId}')?.focus()"
        let closeWithoutRestore = $"{hideMenu}, ${typeaheadSignal} = ''"
        let showAndFocus item = $"{menuIsOpen} || ({menuElement}.showPopover({{source: el}}), {preparePosition}), {focus item}"
        let menuKeydown =
            String.concat "; " [
                $"evt.key == 'Escape' && (evt.preventDefault(), {closeAndRestore})"
                $"evt.key == 'ArrowDown' && ({move 1})"
                $"evt.key == 'ArrowUp' && ({move -1})"
                $"evt.key == 'Home' && (evt.preventDefault(), {focus firstItem})"
                $"evt.key == 'End' && (evt.preventDefault(), {focus lastItem})"
                $"(evt.key == 'Enter' || evt.key == ' ') && {enabledItems}.includes(document.activeElement) && (evt.preventDefault(), document.activeElement.click())"
                $"evt.key == 'Tab' && ({closeWithoutRestore})"
                typeahead ]
        let showAndFocusFirst = showAndFocus firstItem
        let showAndFocusLast = showAndFocus lastItem
        let toggleAndFocus = $"{menuIsOpen} ? ({menuElement}.hidePopover(), {cleanupPosition}) : ({menuElement}.showPopover({{source: el}}), {preparePosition}, evt.detail == 0 ? ({focus firstItem}) : {menuElement}.focus({{preventScroll: true}}))"
        let triggerKeydown =
            String.concat "; " [
                $"evt.key == 'ArrowDown' && (evt.preventDefault(), {showAndFocusFirst})"
                $"evt.key == 'ArrowUp' && (evt.preventDefault(), {showAndFocusLast})" ]
        let itemContent content =
            [ if content.pending then
                  ComponentHtml.loadingGlyph ControlSize.Small
              else
                  match content.leading with
                  | Some leading -> span { _ariaHidden true; _class "flex size-4 shrink-0 items-center justify-center"; leading }
                  | None -> ()
              span { _class "min-w-0 truncate"; content.label }
              match content.shortcut with
              | Some shortcut -> kbd { _ariaHidden true; _class "ml-auto shrink-0 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] px-2 py-1 text-xs font-semibold text-[var(--fve-muted-text)]"; shortcut }
              | None -> () ]
        let itemClasses tone unavailable =
            ComponentHtml.classes [
                ComponentHtml.popupItemClasses
                "fve-popup-item flex w-full items-center gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-left text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-normal"
                match tone with
                | MenuTone.Default -> "text-[var(--fve-text)]"
                | MenuTone.Destructive -> "text-[var(--fve-critical-text)]"
                if unavailable then
                    "cursor-not-allowed opacity-50" ]
        let rec renderEntry path item =
            let entryId = $"{menuId}-entry-{path}"
            match item with
            | Link(content, destination, tone) ->
                let unavailable = content.disabled || content.pending
                a {
                    _id entryId
                    if unavailable |> not then _href (resolve destination)
                    _role "menuitem"
                    _tabindex -1
                    _ariaDisabled unavailable
                    if content.pending then _ariaBusy true
                    _attr ("data-fve-menu-label", content.label.ToLowerInvariant())
                    if unavailable |> not then
                        _dataOn ("pointermove", $"evt.pointerType != 'touch' && {pointerMoved} && el.focus({{preventScroll: true}})")
                        _dataOn ("pointerleave", $"evt.pointerType != 'touch' && {pointerMoved} && document.activeElement == el && {menuElement}.focus({{preventScroll: true}})")
                        _dataOn ("click", closeAndRestore)
                    _class (ComponentHtml.classes [ itemClasses tone unavailable; content.className |> Option.defaultValue "" ])
                    itemContent content
                }
            | Action(content, expression, _)
            | Radio(content, expression, _, _) ->
                let tone = match item with Action(_, _, tone) -> tone | _ -> MenuTone.Default
                let unavailable = content.disabled || content.pending
                button {
                    _id entryId
                    _type "button"
                    match item with
                    | Radio(_, _, isChecked, checkedExpression) ->
                        _role "menuitemradio"
                        _ariaChecked isChecked
                        match checkedExpression with
                        | Some expression -> _dataAttr ("aria-checked", $"({expression}) ? 'true' : 'false'")
                        | None -> ()
                    | _ -> _role "menuitem"
                    _tabindex -1
                    _disabled unavailable
                    _ariaDisabled unavailable
                    if content.pending then _ariaBusy true
                    _attr ("data-fve-menu-label", content.label.ToLowerInvariant())
                    if unavailable |> not then
                        _dataOn ("pointermove", $"evt.pointerType != 'touch' && {pointerMoved} && el.focus({{preventScroll: true}})")
                        _dataOn ("pointerleave", $"evt.pointerType != 'touch' && {pointerMoved} && document.activeElement == el && {menuElement}.focus({{preventScroll: true}})")
                        _dataOn ("click", $"{closeAndRestore}; {expression}")
                    _class (ComponentHtml.classes [ itemClasses tone unavailable; content.className |> Option.defaultValue "" ])
                    itemContent content
                    match item with
                    | Radio(_, _, isChecked, checkedExpression) ->
                        span {
                            _ariaHidden true
                            _class "ml-auto size-4 shrink-0"
                            match checkedExpression with
                            | Some expression -> _dataShow expression
                            | None -> ()
                            if not isChecked then _style "display:none"
                            raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 0 1 .143 1.051l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.479-9.816a.75.75 0 0 1 1.051-.143Z" clip-rule="evenodd"/></svg>"""
                        }
                    | _ -> ()
                }
            | Separator ->
                div { _id entryId; _role "separator"; _class "my-1 h-px bg-[var(--fve-border)]" }
            | Group(label, items) ->
                let labelId = $"{entryId}-label"
                div {
                    _id entryId
                    _role "group"
                    _ariaLabelledby labelId
                    div { _id labelId; _class "px-3 py-1 text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"; label }
                    for index, child in items |> List.indexed do
                        renderEntry $"{path}-{index}" child
                }
        div {
            _class "relative inline-flex"
            _dataSignals $"{{{openSignal}: false, {typeaheadSignal}: '', {typeaheadTimeSignal}: 0}}"
            button {
                _id triggerId
                _type "button"
                _popovertarget menuId
                _ariaHaspopup "menu"
                _ariaExpanded false
                match config.triggerPresentation, config.triggerContent with
                | MenuTriggerPresentation.Overflow, _
                | MenuTriggerPresentation.Icon, _
                | _, Some _ -> _ariaLabel config.label
                | _ -> ()
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                _ariaControls menuId
                _dataOn ("click", [ "prevent" ], $"${typeaheadSignal} = ''; {toggleAndFocus}")
                _dataOn ("keydown", triggerKeydown)
                _class (
                    match config.triggerPresentation with
                    | MenuTriggerPresentation.Button -> ComponentHtml.classes [ ComponentHtml.popupControlClasses; "fve-popup-control inline-flex min-h-[var(--fve-control-min-height)] items-center rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] active:bg-[var(--fve-surface-active)]" ]
                    | MenuTriggerPresentation.Overflow
                    | MenuTriggerPresentation.Icon -> ComponentHtml.classes [ ComponentHtml.popupControlClasses; "fve-popup-control inline-flex size-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] p-0 text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] active:bg-[var(--fve-surface-active)]" ])
                match config.triggerPresentation with
                | MenuTriggerPresentation.Overflow -> overflowIcon
                | MenuTriggerPresentation.Icon ->
                    span { _ariaHidden true; _class "inline-flex size-4 items-center justify-center"; config.triggerContent |> Option.defaultValue (text "") }
                | MenuTriggerPresentation.Button -> config.triggerContent |> Option.defaultValue (text config.label)
            }
            div {
                _id menuId
                _popover "auto"
                _role "menu"
                _tabindex -1
                _ariaLabel config.label
                _dataOn ("beforetoggle", $"${openSignal} = evt.newState == 'open'; evt.newState == 'closed' && (${typeaheadSignal} = '')")
                _dataOn ("toggle", $"evt.newState == 'closed' && ({cleanupPosition})")
                _dataOn ("keydown", menuKeydown)
                _dataOn ("pointermove", [ "window" ], rememberPointer)
                _attr ("data-fve-pointer-x", "NaN")
                _attr ("data-fve-pointer-y", "NaN")
                _attr ("data-fve-position-area", positionArea)
                _dataPreserveAttr "data-fve-pointer-x data-fve-pointer-y"
                _style $"inset: auto; margin: 0.5rem 0; position-area: {positionArea}; position-try-fallbacks: flip-block, flip-inline, flip-block flip-inline; width: min(16rem, calc(100vw - 2rem))"
                _class (ComponentHtml.classes [ ComponentHtml.popupClasses; "fve-popup fixed z-30 rounded-[var(--fve-radius-control)] border-0 bg-[var(--fve-surface)] p-1 shadow-lg" ])
                for index, item in config.items |> List.indexed do
                    renderEntry (string index) item
            }
        }
