namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
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
        let openSignal = $"_{ComponentHtml.signalToken config.id}_open"
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
