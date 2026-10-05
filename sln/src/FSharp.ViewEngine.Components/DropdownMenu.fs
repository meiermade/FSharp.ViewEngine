namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>dropdown-menu</category>
[<RequireQualifiedAccess>]
type DropdownMenuItemColor =
    | Primary
    | Secondary
    | Success
    | Warning
    | Error
    | Info
    | Neutral
    | Custom of ColorPalette

/// <category>dropdown-menu</category>
[<RequireQualifiedAccess>]
type DropdownMenuAlignment =
    | Start
    | End

/// <category>dropdown-menu</category>
[<NoEquality; NoComparison>]
type DropdownMenuTrigger =
    private
        { body:HtmlElement
          iconOnly:bool
          fullRow:bool
          attributes:HtmlAttribute list
          groupClass:string option }

/// <category>dropdown-menu</category>
[<RequireQualifiedAccess>]
module DropdownMenuTrigger =
    let text label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A visible menu-trigger label is required."
        { body = Html.text label; iconOnly = false; fullRow = false; attributes = []; groupClass = None }

    let content content : DropdownMenuTrigger = { body = content; iconOnly = false; fullRow = false; attributes = []; groupClass = None }
    let icon icon : DropdownMenuTrigger = { body = icon; iconOnly = true; fullRow = false; attributes = []; groupClass = None }
    /// A square-edged, full-width trigger for an owning navigation/context row.
    let asFullRow (trigger:DropdownMenuTrigger) = { trigger with fullRow = true; iconOnly = false }
    let withAttributes attributes (trigger:DropdownMenuTrigger) = { trigger with attributes = attributes }

[<NoEquality; NoComparison>]
type private MenuItemContent =
    { label:string
      leading:HtmlElement option
      trailing:HtmlElement option
      description:string option
      shortcut:string option
      disabled:bool
      pending:bool
      color:DropdownMenuItemColor }

/// <category>dropdown-menu</category>
[<NoEquality; NoComparison>]
type DropdownMenuItem<'destination> =
    private
        | Link of content:MenuItemContent * destination:'destination
        | Action of content:MenuItemContent * datastarExpression:string
        | Radio of content:MenuItemContent * datastarExpression:string * isChecked:bool * checkedExpression:string option
        | Checkbox of content:MenuItemContent * datastarExpression:string * isChecked:bool * checkedExpression:string option
        | Separator
        | Group of label:string * items:DropdownMenuItem<'destination> list

/// <category>dropdown-menu</category>
[<NoEquality; NoComparison>]
type DropdownMenuConfig<'destination> =
    private
        { id:string
          label:string
          items:DropdownMenuItem<'destination> list
          alignment:DropdownMenuAlignment
          trigger:DropdownMenuTrigger option
          containerClass:string option }

/// <category>dropdown-menu</category>
[<RequireQualifiedAccess>]
module DropdownMenuItem =
    let private content label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu item label is required."
        { label = label
          leading = None
          trailing = None
          description = None
          shortcut = None
          disabled = false
          pending = false
          color = DropdownMenuItemColor.Neutral }

    let private mapContent update item =
        match item with
        | Link(itemContent, destination) -> Link(update itemContent, destination)
        | Action(itemContent, expression) -> Action(update itemContent, expression)
        | Radio(itemContent, expression, isChecked, checkedExpression) -> Radio(update itemContent, expression, isChecked, checkedExpression)
        | Checkbox(itemContent, expression, isChecked, checkedExpression) -> Checkbox(update itemContent, expression, isChecked, checkedExpression)
        | Separator -> invalidArg (nameof item) "A separator cannot have item presentation."
        | Group _ -> invalidArg (nameof item) "A group cannot have item presentation."

    let link destination label = Link(content label, destination)
    let action datastarExpression label = Action(content label, datastarExpression)
    /// A mutually exclusive menu choice. The caller owns its checked state and selection action.
    let radio datastarExpression label = Radio(content label, datastarExpression, false, None)
    /// An independently toggled menu choice. The caller owns its checked state and toggle action.
    let checkbox datastarExpression label = Checkbox(content label, datastarExpression, false, None)
    let withChecked isChecked = function
        | Radio(content, expression, _, checkedExpression) -> Radio(content, expression, isChecked, checkedExpression)
        | Checkbox(content, expression, _, checkedExpression) -> Checkbox(content, expression, isChecked, checkedExpression)
        | _ -> invalidArg "item" "Only radio and checkbox menu items have a checked state."
    /// A trusted Datastar expression for client-local choice state.
    let withCheckedExpression checkedExpression = function
        | Radio(content, expression, isChecked, _) -> Radio(content, expression, isChecked, Some checkedExpression)
        | Checkbox(content, expression, isChecked, _) -> Checkbox(content, expression, isChecked, Some checkedExpression)
        | _ -> invalidArg "item" "Only radio and checkbox menu items have a checked state."
    let separator<'destination> : DropdownMenuItem<'destination> = Separator

    let group label items =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu group label is required."
        if List.isEmpty items then invalidArg (nameof items) "A menu group requires at least one item."
        if items |> List.exists (function | Group _ -> true | _ -> false) then
            invalidArg (nameof items) "Menu groups cannot be nested."
        Group(label, items)

    let disabled item = item |> mapContent (fun content -> { content with disabled = true })
    let pending item = item |> mapContent (fun content -> { content with pending = true })
    let withColor color item = item |> mapContent (fun content -> { content with color = color })
    let withLeading leading item = item |> mapContent (fun content -> { content with leading = Some leading })
    let withTrailing trailing item = item |> mapContent (fun content -> { content with trailing = Some trailing })
    /// Supporting context beneath the primary menu-item label.
    let withDescription description item =
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "A visible description is required."
        item |> mapContent (fun content -> { content with description = Some description })

    let withShortcut shortcut item =
        if String.IsNullOrWhiteSpace shortcut then invalidArg (nameof shortcut) "A visible shortcut is required."
        item |> mapContent (fun content -> { content with shortcut = Some shortcut })

/// <category>dropdown-menu</category>
[<RequireQualifiedAccess>]
module DropdownMenu =
    /// Defaults to End alignment. A trigger and non-empty content are required before render.
    let create id label =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable menu ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu label is required."
        { id = id
          label = label
          items = []
          alignment = DropdownMenuAlignment.End
          trigger = None
          containerClass = None }

    let withTrigger trigger (config:DropdownMenuConfig<'destination>) = { config with trigger = Some trigger }

    let withContent items (config:DropdownMenuConfig<'destination>) =
        if List.isEmpty items then invalidArg (nameof items) "Dropdown-menu content requires at least one item."
        { config with items = items }

    let withAlignment alignment config = { config with alignment = alignment }
    let internal withinGroup (containerClass:string) (triggerClass:string) (config:DropdownMenuConfig<'destination>) =
        { config with
            containerClass = Some containerClass
            trigger = config.trigger |> Option.map (fun trigger -> { trigger with groupClass = Some triggerClass }) }

    let render resolve config =
        let trigger = config.trigger |> Option.defaultWith (fun () -> invalidOp "DropdownMenu.withTrigger is required before rendering.")
        if List.isEmpty config.items then invalidOp "DropdownMenu.withContent is required before rendering."
        let instanceId = ComponentHtml.signalToken config.id
        let openSignal = $"_{instanceId}_open"
        let typeaheadSignal = $"_{instanceId}_typeahead"
        let typeaheadTimeSignal = $"_{instanceId}_typeahead_time"
        let triggerId = $"{config.id}-trigger"
        let menuId = $"{config.id}-menu"
        let positionArea =
            match config.alignment with
            | DropdownMenuAlignment.Start -> "block-end span-inline-end"
            | DropdownMenuAlignment.End -> "block-end span-inline-start"
        let enabledItems = $"Array.from(document.querySelectorAll('#{menuId} :is([role=menuitem], [role=menuitemradio], [role=menuitemcheckbox]):not([aria-disabled=true])')).filter(item => item.getClientRects().length)"
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
            | DropdownMenuAlignment.Start -> "trigger.left"
            | DropdownMenuAlignment.End -> "trigger.right - width"
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
              span {
                  _class "min-w-0 flex-1"
                  span { _class "block truncate"; content.label }
                  match content.description with
                  | Some description -> span { _class "block truncate text-xs text-[var(--fve-muted-text)]"; description }
                  | None -> ()
              }
              match content.trailing with
              | Some trailing -> span { _ariaHidden true; _class "ml-auto flex size-4 shrink-0 items-center justify-center"; trailing }
              | None -> ()
              match content.shortcut with
              | Some shortcut -> kbd { _ariaHidden true; _class "ml-auto shrink-0 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] px-2 py-1 text-xs font-semibold text-[var(--fve-muted-text)]"; shortcut }
              | None -> () ]
        let itemPalette = function
            | DropdownMenuItemColor.Primary -> ComponentColors.primary
            | DropdownMenuItemColor.Secondary -> ComponentColors.secondary
            | DropdownMenuItemColor.Success -> ComponentColors.success
            | DropdownMenuItemColor.Warning -> ComponentColors.warning
            | DropdownMenuItemColor.Error -> ComponentColors.error
            | DropdownMenuItemColor.Info -> ComponentColors.info
            | DropdownMenuItemColor.Neutral -> ComponentColors.neutral
            | DropdownMenuItemColor.Custom colors -> colors
        let itemClasses unavailable =
            ComponentHtml.classes [
                ComponentHtml.popupItemClasses
                "fve-popup-item flex w-full items-center gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-left text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-normal text-[var(--fve-color-text)]"
                if unavailable then
                    "cursor-not-allowed opacity-50" ]
        let rec renderEntry path item =
            let entryId = $"{menuId}-entry-{path}"
            match item with
            | Link(content, destination) ->
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
                    _style (ComponentColors.style (itemPalette content.color) [])
                    _class (itemClasses unavailable)
                    itemContent content
                }
            | Action(content, expression)
            | Radio(content, expression, _, _)
            | Checkbox(content, expression, _, _) ->
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
                    | Checkbox(_, _, isChecked, checkedExpression) ->
                        _role "menuitemcheckbox"
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
                        match item with
                        | Checkbox _ -> _dataOn ("click", expression)
                        | _ -> _dataOn ("click", $"{closeAndRestore}; {expression}")
                    _style (ComponentColors.style (itemPalette content.color) [])
                    _class (itemClasses unavailable)
                    itemContent content
                    match item with
                    | Radio(_, _, isChecked, checkedExpression)
                    | Checkbox(_, _, isChecked, checkedExpression) ->
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
            _class (ComponentHtml.classes [ (if trigger.fullRow then "relative flex w-full" else "relative inline-flex"); config.containerClass |> Option.defaultValue "" ])
            _dataSignals $"{{{openSignal}: false, {typeaheadSignal}: '', {typeaheadTimeSignal}: 0}}"
            button {
                _id triggerId
                _type "button"
                _popovertarget menuId
                _ariaHaspopup "menu"
                _ariaExpanded false
                _ariaLabel config.label
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                _ariaControls menuId
                _dataOn ("click", [ "prevent" ], $"${typeaheadSignal} = ''; {toggleAndFocus}")
                _dataOn ("keydown", triggerKeydown)
                _class (
                    if trigger.fullRow then
                        ComponentHtml.classes [ ComponentHtml.popupControlClasses; "fve-popup-control flex min-h-[var(--fve-shell-bar-min-height)] w-full items-center rounded-none px-4 py-3 text-left text-sm font-semibold text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] active:bg-[var(--fve-surface-active)]"; trigger.groupClass |> Option.defaultValue "" ]
                    elif trigger.iconOnly then
                        ComponentHtml.classes [ ComponentHtml.popupControlClasses; "fve-popup-control inline-flex size-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] p-0 text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] active:bg-[var(--fve-surface-active)]"; trigger.groupClass |> Option.defaultValue "" ]
                    else
                        ComponentHtml.classes [ ComponentHtml.popupControlClasses; "fve-popup-control inline-flex min-h-[var(--fve-control-min-height)] items-center rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] active:bg-[var(--fve-surface-active)]"; trigger.groupClass |> Option.defaultValue "" ])
                for attribute in ComponentHtml.safeAttributes [ "id"; "type"; "role"; "tabindex"; "popovertarget"; "aria-haspopup"; "aria-expanded"; "aria-label"; "aria-controls"; "class"; "data-on:"; "data-attr:"; "data-signals"; "data-init" ] trigger.attributes do attribute
                if trigger.iconOnly then
                    span { _ariaHidden true; _class "inline-flex size-4 items-center justify-center"; trigger.body }
                else
                    trigger.body
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
