namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<RequireQualifiedAccess>]
type FloatingPanelState =
    | Open
    | Minimized
    | Dismissed

[<RequireQualifiedAccess>]
type FloatingPanelBoundary =
    | Viewport
    | Container

[<NoEquality; NoComparison>]
type FloatingPanelConfig =
    private
        { id:string
          title:string
          body:HtmlElement
          description:string option
          footer:HtmlElement option
          state:FloatingPanelState
          boundary:FloatingPanelBoundary }

/// A persistent, non-modal product surface. It does not own completion or persistence policy.
[<RequireQualifiedAccess>]
module FloatingPanel =
    let create id title body =
        { id = TextField.stableId id
          title = TextField.requiredText (nameof title) title
          body = body
          description = None
          footer = None
          state = FloatingPanelState.Open
          boundary = FloatingPanelBoundary.Viewport }

    let withDescription description (config:FloatingPanelConfig) =
        { config with description = Some (TextField.requiredText (nameof description) description) }

    let withFooter footer (config:FloatingPanelConfig) = { config with footer = Some footer }
    let withinContainer (config:FloatingPanelConfig) = { config with boundary = FloatingPanelBoundary.Container }
    let minimized (config:FloatingPanelConfig) = { config with state = FloatingPanelState.Minimized }
    let dismissed (config:FloatingPanelConfig) = { config with state = FloatingPanelState.Dismissed }

    let private icon path =
        raw $"""<svg viewBox="0 0 20 20" fill="currentColor" class="size-5" aria-hidden="true"><path d="{path}"/></svg>"""

    let render (config:FloatingPanelConfig) =
        let token = ComponentHtml.signalToken config.id
        let signal = $"{token}FloatingPanelState"
        let value = "$" + signal
        let titleId = config.id + "-title"
        let titleExpression = ComponentHtml.javascriptString titleId
        let restoreFocus = $"queueMicrotask(() => document.getElementById({titleExpression})?.focus())"
        let focusRestore id = $"queueMicrotask(() => document.getElementById({ComponentHtml.javascriptString id})?.focus())"
        let minimizedFocus = focusRestore (config.id + "-open")
        let dismissedFocus = focusRestore (config.id + "-restore")
        let stateEvent state =
            $"evt.currentTarget.closest('[data-fve-floating-panel-root]')?.dispatchEvent(new CustomEvent('fve-floating-panel-state', {{ bubbles: true, detail: {{ id: {ComponentHtml.javascriptString config.id}, state: {ComponentHtml.javascriptString state} }} }}))"
        let minimizedEvent = stateEvent "minimized"
        let dismissedEvent = stateEvent "dismissed"
        let openEvent = stateEvent "open"
        let initial =
            match config.state with
            | FloatingPanelState.Open -> "open"
            | FloatingPanelState.Minimized -> "minimized"
            | FloatingPanelState.Dismissed -> "dismissed"
        div {
            _attr ("data-fve-floating-panel-root", "true")
            _dataSignals $"{{{signal}: {ComponentHtml.javascriptString initial}}}"
            section {
                _id config.id
                _ariaLabelledby titleId
                _class ((if config.boundary = FloatingPanelBoundary.Container then "absolute" else "fixed") + " fve-floating-panel right-[max(1rem,env(safe-area-inset-right))] bottom-[max(1rem,env(safe-area-inset-bottom))] z-40 flex max-h-[min(36rem,calc(100dvh-5rem))] w-[min(24rem,calc(100%-2rem))] flex-col overflow-hidden [body:has([data-fve-bottom-navigation=true])_&]:bottom-[max(4rem,calc(env(safe-area-inset-bottom)+3.5rem))] max-[30rem]:right-[max(8px,env(safe-area-inset-right))] max-[30rem]:w-[min(24rem,calc(100%-16px))] rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] text-[var(--fve-text)] shadow-xl ring-1 ring-[var(--fve-border)]")
                _dataShow (value + " == 'open'")
                if config.state <> FloatingPanelState.Open then _style "display:none"
                header {
                    _class "fve-floating-panel-header flex shrink-0 flex-wrap items-start justify-between gap-3 border-b border-[var(--fve-border)] p-4 max-[30rem]:px-3"
                    div {
                        _class "fve-floating-panel-heading min-w-0 flex-1 max-[20rem]:basis-full"
                        h2 {
                            _id titleId
                            _tabindex -1
                            _class "text-base font-semibold outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                            config.title
                        }
                        match config.description with
                        | Some description -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                        | None -> ()
                    }
                    div {
                        _class "ml-auto flex shrink-0 items-center gap-1"
                        button {
                            _type "button"
                            _ariaLabel ("Minimize " + config.title)
                            _ariaControls config.id
                            _class "inline-flex size-8 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                            _dataOn ("click", $"{value} = 'minimized'; {minimizedEvent}; {minimizedFocus}")
                            icon "M4 9.25a.75.75 0 0 0 0 1.5h12a.75.75 0 0 0 0-1.5H4Z"
                        }
                        button {
                            _type "button"
                            _ariaLabel ("Dismiss " + config.title)
                            _class "inline-flex size-8 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                            _dataOn ("click", $"{value} = 'dismissed'; {dismissedEvent}; {dismissedFocus}")
                            icon "M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 1 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"
                        }
                    }
                }
                div {
                    _class "fve-floating-panel-body min-h-0 overflow-y-auto p-4 max-[30rem]:px-3"
                    config.body
                }
                match config.footer with
                | Some footer -> div { _class "shrink-0 border-t border-[var(--fve-border)] p-4"; footer }
                | None -> ()
            }
            button {
                _id (config.id + "-open")
                _type "button"
                _ariaControls config.id
                _ariaExpanded false
                _class ((if config.boundary = FloatingPanelBoundary.Container then "absolute" else "fixed") + " fve-floating-panel-trigger right-[max(1rem,env(safe-area-inset-right))] bottom-[max(1rem,env(safe-area-inset-bottom))] z-40 inline-flex min-h-[var(--fve-control-min-height)] max-w-[min(24rem,calc(100%-2rem))] items-center rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] [body:has([data-fve-bottom-navigation=true])_&]:bottom-[max(4rem,calc(env(safe-area-inset-bottom)+3.5rem))] max-[30rem]:right-[max(8px,env(safe-area-inset-right))] max-[30rem]:max-w-[min(24rem,calc(100%-16px))] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] shadow-lg ring-1 ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]")
                _dataShow (value + " == 'minimized'")
                if config.state <> FloatingPanelState.Minimized then _style "display:none"
                _dataOn ("click", $"{value} = 'open'; {openEvent}; {restoreFocus}")
                "Open " + config.title
            }
            button {
                _id (config.id + "-restore")
                _type "button"
                _ariaControls config.id
                _class ((if config.boundary = FloatingPanelBoundary.Container then "absolute" else "fixed") + " fve-floating-panel-trigger right-[max(1rem,env(safe-area-inset-right))] bottom-[max(1rem,env(safe-area-inset-bottom))] z-40 inline-flex min-h-[var(--fve-control-min-height)] max-w-[min(24rem,calc(100%-2rem))] items-center rounded-[var(--fve-radius-control)] px-3 [body:has([data-fve-bottom-navigation=true])_&]:bottom-[max(4rem,calc(env(safe-area-inset-bottom)+3.5rem))] max-[30rem]:right-[max(8px,env(safe-area-inset-right))] max-[30rem]:max-w-[min(24rem,calc(100%-16px))] py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-brand-text)] underline-offset-2 hover:bg-[var(--fve-surface-hover)] hover:underline focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]")
                _dataShow (value + " == 'dismissed'")
                if config.state <> FloatingPanelState.Dismissed then _style "display:none"
                _dataOn ("click", $"{value} = 'open'; {openEvent}; {restoreFocus}")
                "Restore " + config.title
            }
        }
