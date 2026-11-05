namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>floating-panel</category>
[<RequireQualifiedAccess>]
type FloatingPanelState =
    | Open
    | Minimized
    | Dismissed

/// <category>floating-panel</category>
[<RequireQualifiedAccess>]
type FloatingPanelBoundary =
    | Viewport
    | Container

/// <category>floating-panel</category>
[<NoEquality; NoComparison>]
type internal FloatingPanelConfig =
    private
        { id:string
          title:string
          body:HtmlElement
          description:string option
          footer:HtmlElement option
          state:FloatingPanelState
          boundary:FloatingPanelBoundary
          attributes:HtmlAttribute list }

/// <category>floating-panel</category>
[<AutoOpen>]
module FloatingPanelAttributes =
    type Html with
        static member _state(value:FloatingPanelState) = Html._attr("state", match value with FloatingPanelState.Open -> "open" | FloatingPanelState.Minimized -> "minimized" | FloatingPanelState.Dismissed -> "dismissed")
        static member _boundary(value:FloatingPanelBoundary) = Html._attr("boundary", if value = FloatingPanelBoundary.Container then "container" else "viewport")

module internal FloatingPanel =
    let footer attributes children =
        ComponentPart("FloatingPanel", "footer", ComponentElement.render "div" [_class "shrink-0 border-t border-[var(--fve-border)] p-4"] attributes children) :> HtmlElement

    let private configuration attributes children =
        let parts, body = ComponentParts.split "FloatingPanel" children
        { id = ComponentElement.required "id" attributes |> TextField.stableId
          title = ComponentElement.required "label" attributes
          body = ComponentContent.fragment body
          description = ComponentElement.value "description" attributes |> Option.map (TextField.requiredText "description")
          footer = Map.tryFind "footer" parts
          state = match ComponentElement.choice "state" "open" ["open"; "minimized"; "dismissed"] attributes with "minimized" -> FloatingPanelState.Minimized | "dismissed" -> FloatingPanelState.Dismissed | _ -> FloatingPanelState.Open
          boundary = if ComponentElement.choice "boundary" "viewport" ["viewport"; "container"] attributes = "container" then FloatingPanelBoundary.Container else FloatingPanelBoundary.Viewport
          attributes = ComponentHtml.safeAttributes ["id"; "label"; "description"; "state"; "boundary"; "aria-labelledby"; "data-show"] attributes }

    let private icon path =
        raw $"""<svg viewBox="0 0 20 20" fill="currentColor" class="size-5" aria-hidden="true"><path d="{path}"/></svg>"""

    let render attributes children =
        let config = configuration attributes children
        let token = ComponentHtml.signalToken config.id
        let signal = $"_{token}FloatingPanelState"
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
                let defaults = [
                    yield _id config.id
                    yield _ariaLabelledby titleId
                    yield _class ((if config.boundary = FloatingPanelBoundary.Container then "absolute" else "fixed") + " fve-floating-panel right-[max(1rem,env(safe-area-inset-right))] bottom-[max(1rem,env(safe-area-inset-bottom))] z-40 flex max-h-[min(36rem,calc(100dvh-5rem))] w-[min(24rem,calc(100%-2rem))] flex-col overflow-hidden [body:has([data-fve-bottom-navigation=true])_&]:bottom-[max(4rem,calc(env(safe-area-inset-bottom)+3.5rem))] max-[30rem]:right-[max(8px,env(safe-area-inset-right))] max-[30rem]:w-[min(24rem,calc(100%-16px))] rounded-[var(--fve-radius-panel)] bg-[var(--fve-background)] text-[var(--fve-text)] shadow-xl ring-1 ring-[var(--fve-border)]")
                    yield _dataShow (value + " == 'open'")
                    if config.state <> FloatingPanelState.Open then yield _style "display:none" ]
                for attribute in ComponentElement.attributes (defaults @ config.attributes) do attribute
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
                | Some footer -> footer
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

/// <category>floating-panel</category>
[<AutoOpen>]
module FloatingPanelDsl =
    let fveFloatingPanel = ComponentElementBuilder(FloatingPanel.render)
    let fveFloatingPanelFooter = ComponentElementBuilder(FloatingPanel.footer)
