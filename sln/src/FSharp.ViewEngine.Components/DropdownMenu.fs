namespace FSharp.ViewEngine.Components

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

[<NoEquality; NoComparison>]
type private MenuItemContent =
    { label:string
      leading:HtmlElement option
      shortcut:string option
      disabled:bool
      pending:bool }

[<NoEquality; NoComparison>]
type MenuItem<'destination> =
    private
        | Link of content:MenuItemContent * destination:'destination
        | Action of content:MenuItemContent * datastarExpression:string * tone:MenuTone
        | Separator
        | Group of label:string * items:MenuItem<'destination> list

[<NoEquality; NoComparison>]
type DropdownMenuConfig<'destination> =
    private
        { id:string
          label:string
          items:MenuItem<'destination> list
          alignment:MenuAlignment
          triggerContent:HtmlElement option }

[<RequireQualifiedAccess>]
module MenuItem =
    let private content label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu item label is required."
        { label = label
          leading = None
          shortcut = None
          disabled = false
          pending = false }

    let private mapContent update item =
        match item with
        | Link(itemContent, destination) -> Link(update itemContent, destination)
        | Action(itemContent, expression, tone) -> Action(update itemContent, expression, tone)
        | Separator -> invalidArg (nameof item) "A separator cannot have item presentation."
        | Group _ -> invalidArg (nameof item) "A group cannot have item presentation."

    let link destination label = Link(content label, destination)
    let action datastarExpression label = Action(content label, datastarExpression, MenuTone.Default)
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

    let withShortcut shortcut item =
        if String.IsNullOrWhiteSpace shortcut then invalidArg (nameof shortcut) "A visible shortcut is required."
        item |> mapContent (fun content -> { content with shortcut = Some shortcut })

[<RequireQualifiedAccess>]
module DropdownMenu =
    let create id label items =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable menu ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A menu label is required."
        { id = id
          label = label
          items = items
          alignment = MenuAlignment.End
          triggerContent = None }

    let withAlignment alignment config = { config with alignment = alignment }
    let withTriggerContent content config = { config with triggerContent = Some content }

    let render resolve config =
        let instanceId = ComponentHtml.signalToken config.id
        let openSignal = $"_{instanceId}_open"
        let typeaheadSignal = $"_{instanceId}_typeahead"
        let typeaheadTimeSignal = $"_{instanceId}_typeahead_time"
        let triggerId = $"{config.id}-trigger"
        let menuId = $"{config.id}-menu"
        let enabledItems = $"Array.from(document.querySelectorAll('#{menuId} [role=menuitem]:not([aria-disabled=true])'))"
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
        let closeAndRestore = $"${openSignal} = false, ${typeaheadSignal} = '', document.getElementById('{triggerId}')?.focus()"
        let instanceIdentity = ComponentHtml.javascriptString config.id
        let announceOpen = $"window.dispatchEvent(new CustomEvent('fve-dropdown-open', {{detail: {instanceIdentity}}}))"
        let menuKeydown =
            String.concat "; " [
                $"evt.key == 'Escape' && (evt.preventDefault(), {closeAndRestore})"
                $"evt.key == 'ArrowDown' && ({move 1})"
                $"evt.key == 'ArrowUp' && ({move -1})"
                $"evt.key == 'Home' && (evt.preventDefault(), {focus firstItem})"
                $"evt.key == 'End' && (evt.preventDefault(), {focus lastItem})"
                $"(evt.key == 'Enter' || evt.key == ' ') && {enabledItems}.includes(document.activeElement) && (evt.preventDefault(), document.activeElement.click())"
                $"evt.key == 'Tab' && (${openSignal} = false, ${typeaheadSignal} = '')"
                typeahead ]
        let triggerKeydown =
            String.concat "; " [
                $"(evt.key == 'Enter' || evt.key == ' ' || evt.key == 'ArrowDown') && (evt.preventDefault(), {announceOpen}, ${openSignal} = true, queueMicrotask(() => {focus firstItem}))"
                $"evt.key == 'ArrowUp' && (evt.preventDefault(), {announceOpen}, ${openSignal} = true, queueMicrotask(() => {focus lastItem}))" ]
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
                "flex w-full items-center gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-left text-sm outline-none transition-colors"
                match tone with
                | MenuTone.Default -> "text-[var(--fve-text)]"
                | MenuTone.Destructive -> "text-[var(--fve-critical-text)]"
                if unavailable then
                    "cursor-not-allowed opacity-50"
                else
                    match tone with
                    | MenuTone.Default -> "hover:bg-[var(--fve-surface-hover)] active:bg-[var(--fve-surface-active)] focus:bg-[var(--fve-surface-hover)] focus:ring-2 focus:ring-inset focus:ring-[var(--fve-brand-ring)]"
                    | MenuTone.Destructive -> "hover:bg-[var(--fve-critical-subtle)] active:bg-[var(--fve-critical-subtle)] focus:bg-[var(--fve-critical-subtle)] focus:ring-2 focus:ring-inset focus:ring-[var(--fve-critical-ring)]" ]
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
                        _dataOn ("pointermove", $"{pointerMoved} && el.focus()")
                        _dataOn ("click", closeAndRestore)
                    _class (itemClasses MenuTone.Default unavailable)
                    itemContent content
                }
            | Action(content, expression, tone) ->
                let unavailable = content.disabled || content.pending
                button {
                    _id entryId
                    _type "button"
                    _role "menuitem"
                    _tabindex -1
                    _disabled unavailable
                    _ariaDisabled unavailable
                    if content.pending then _ariaBusy true
                    _attr ("data-fve-menu-label", content.label.ToLowerInvariant())
                    if unavailable |> not then
                        _dataOn ("pointermove", $"{pointerMoved} && el.focus()")
                        _dataOn ("click", $"{closeAndRestore}; {expression}")
                    _class (itemClasses tone unavailable)
                    itemContent content
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
            _dataOn ("fve-dropdown-open", [ "window" ], $"evt.detail != {instanceIdentity} && (${openSignal} = false, ${typeaheadSignal} = '')")
            button {
                _id triggerId
                _type "button"
                _ariaHaspopup "menu"
                _ariaExpanded false
                if config.triggerContent.IsSome then _ariaLabel config.label
                _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                _ariaControls menuId
                _dataOn ("click", [ "stop" ], $"{announceOpen}; ${openSignal} = !${openSignal}; ${typeaheadSignal} = ''; ${openSignal} && queueMicrotask(() => {focus firstItem})")
                _dataOn ("keydown", triggerKeydown)
                _class "inline-flex min-h-[var(--fve-control-min-height)] items-center rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none hover:bg-[var(--fve-surface-hover)] active:bg-[var(--fve-surface-active)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                config.triggerContent |> Option.defaultValue (text config.label)
            }
            div {
                _id menuId
                _role "menu"
                _ariaLabel config.label
                _dataShow $"${openSignal}"
                _dataOn ("click", [ "outside" ], $"${openSignal} = false; ${typeaheadSignal} = ''")
                _dataOn ("keydown", menuKeydown)
                _dataOn ("pointermove", [ "window" ], rememberPointer)
                _attr ("data-fve-pointer-x", "NaN")
                _attr ("data-fve-pointer-y", "NaN")
                _dataPreserveAttr "data-fve-pointer-x data-fve-pointer-y"
                _style "display:none"
                _class (ComponentHtml.classes [
                    "absolute top-full z-30 mt-2 w-64 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] p-1 shadow-lg ring-1 ring-[var(--fve-border)]"
                    match config.alignment with
                    | MenuAlignment.Start -> "left-0"
                    | MenuAlignment.End -> "right-0" ])
                for index, item in config.items |> List.indexed do
                    renderEntry (string index) item
            }
        }
