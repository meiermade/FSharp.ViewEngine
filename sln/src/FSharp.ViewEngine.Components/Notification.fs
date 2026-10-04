namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>notification</category>
[<RequireQualifiedAccess>]
type NotificationColor =
    | Primary
    | Secondary
    | Success
    | Warning
    | Error
    | Info
    | Neutral
    | Custom of ColorPalette

/// <category>notification</category>
[<NoEquality; NoComparison>]
type NotificationConfig =
    private
        { id:string
          title:string
          content:HtmlElement
          color:NotificationColor
          announcement:LiveAnnouncement
          actions:HtmlElement option
          ttl:int option }

/// <remarks>
/// The default lifetime is 5000ms. persistent disables expiry, not manual dismissal or replacement.
/// </remarks>
/// <category>notification</category>
[<RequireQualifiedAccess>]
module Notification =
    /// Creates an automatically expiring notification with a neutral color, polite announcement, no actions, and a 5000ms lifetime.
    let create id title content =
        { id = TextField.stableId id
          title = TextField.requiredText (nameof title) title
          content = content
          color = NotificationColor.Neutral
          announcement = LiveAnnouncement.Polite
          actions = None
          ttl = Some 5000 }

    let withColor color (config:NotificationConfig) = { config with color = color }
    let withAnnouncement announcement (config:NotificationConfig) = { config with announcement = announcement }
    let withActions actions (config:NotificationConfig) = { config with actions = Some actions }

    /// Sets the automatic-removal lifetime in milliseconds.
    let withTtl milliseconds (config:NotificationConfig) =
        if milliseconds <= 0 then invalidArg (nameof milliseconds) "Notification lifetime must be greater than zero."
        { config with ttl = Some milliseconds }

    /// Keeps the notification present until it is dismissed explicitly.
    let persistent (config:NotificationConfig) = { config with ttl = None }

    let private palette = function
        | NotificationColor.Primary -> ComponentColors.primary
        | NotificationColor.Secondary -> ComponentColors.secondary
        | NotificationColor.Success -> ComponentColors.success
        | NotificationColor.Warning -> ComponentColors.warning
        | NotificationColor.Error -> ComponentColors.error
        | NotificationColor.Info -> ComponentColors.info
        | NotificationColor.Neutral -> ComponentColors.neutral
        | NotificationColor.Custom colors -> colors

    let private colorIcon = function
        | NotificationColor.Success -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M16.7 5.3a1 1 0 0 1 0 1.4l-8 8a1 1 0 0 1-1.4 0l-4-4a1 1 0 0 1 1.4-1.4L8 12.6l7.3-7.3a1 1 0 0 1 1.4 0Z" clip-rule="evenodd"/></svg>"""
        | NotificationColor.Warning -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M8.5 3.1a1.75 1.75 0 0 1 3 0l6.1 10.6a1.75 1.75 0 0 1-1.5 2.6H3.9a1.75 1.75 0 0 1-1.5-2.6L8.5 3.1ZM10 7a.75.75 0 0 1 .75.75v3a.75.75 0 0 1-1.5 0v-3A.75.75 0 0 1 10 7Zm0 6a.88.88 0 1 0 0 1.75A.88.88 0 0 0 10 13Z" clip-rule="evenodd"/></svg>"""
        | NotificationColor.Error -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16ZM7.2 7.2a.75.75 0 0 1 1.1 0L10 8.9l1.7-1.7a.75.75 0 1 1 1.1 1.1L11.1 10l1.7 1.7a.75.75 0 1 1-1.1 1.1L10 11.1l-1.7 1.7a.75.75 0 1 1-1.1-1.1L8.9 10 7.2 8.3a.75.75 0 0 1 0-1.1Z" clip-rule="evenodd"/></svg>"""
        | NotificationColor.Info
        | NotificationColor.Primary -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm.75-8.75a.75.75 0 0 0-1.5 0v4a.75.75 0 0 0 1.5 0v-4ZM10 5.5a1 1 0 1 0 0 2 1 1 0 0 0 0-2Z" clip-rule="evenodd"/></svg>"""
        | NotificationColor.Secondary
        | NotificationColor.Neutral
        | NotificationColor.Custom _ -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path d="M10 2a5 5 0 0 0-5 5v2.3c0 .7-.2 1.3-.6 1.9l-1 1.5A1.5 1.5 0 0 0 4.7 15h10.6a1.5 1.5 0 0 0 1.3-2.3l-1-1.5a3.5 3.5 0 0 1-.6-1.9V7a5 5 0 0 0-5-5ZM8 16a2 2 0 0 0 4 0H8Z"/></svg>"""

    let private closeIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 1 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""

    let private removalExpression root reason (config:NotificationConfig) =
        $"const notification = {root}; const region = notification?.closest('[data-fve-notification-region]'); if (notification?.contains(document.activeElement)) document.activeElement?.blur(); notification?.remove(); region?.dispatchEvent(new CustomEvent('fve-notification-dismiss', {{ bubbles: true, detail: {{ id: {ComponentHtml.javascriptString config.id}, reason: '{reason}' }} }}))"

    let render (config:NotificationConfig) =
        let elapsedSignal = ComponentHtml.signalToken (config.id + "-elapsed")
        let elapsed = "$" + elapsedSignal
        let timeoutRemoval = removalExpression "el" "timeout" config
        article {
            _id config.id
            _attr ("data-fve-notification", "true")
            match config.ttl with
            | Some milliseconds ->
                _dataSignals (elapsedSignal, [ "ifmissing" ], "0")
                _attr ("data-fve-notification-ttl", string milliseconds)
                _dataOnInterval (
                    [ "duration.100ms" ],
                    $"if (!document.hidden && !el.matches(':hover') && !el.matches(':focus-within')) {{ {elapsed} += 100; if ({elapsed} >= {milliseconds}) {{ {timeoutRemoval} }} }}")
            | None -> _attr ("data-fve-notification-persistent", "true")
            _style (ComponentColors.style (palette config.color) [])
            _class "pointer-events-auto relative grid min-w-0 grid-cols-[auto_minmax(0,1fr)_auto] items-start gap-x-3 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 text-[var(--fve-text)] shadow-lg [overflow-wrap:anywhere] transition-[transform,opacity] duration-200 motion-reduce:transition-none @max-[20rem]/fve-notification:grid-cols-1 @max-[20rem]/fve-notification:gap-y-2 @max-[20rem]/fve-notification:p-3"
            span {
                _class "mt-0.5 inline-flex size-7 items-center justify-center rounded-full bg-[var(--fve-color-soft)] text-[var(--fve-color-text)] @max-[20rem]/fve-notification:hidden"
                colorIcon config.color
            }
            div {
                _class "min-w-0 @max-[20rem]/fve-notification:col-start-1 @max-[20rem]/fve-notification:row-start-2"
                match config.announcement with
                | LiveAnnouncement.Static -> ()
                | LiveAnnouncement.Polite -> _role "status"; _ariaAtomic true
                | LiveAnnouncement.Assertive -> _role "alert"; _ariaAtomic true
                p { _class "text-sm font-semibold leading-5"; config.title }
                div { _class "mt-1 text-sm leading-5 text-[var(--fve-muted-text)]"; config.content }
            }
            button {
                _type "button"
                _ariaLabel ("Dismiss " + config.title)
                _attr ("data-fve-notification-dismiss", "true")
                _class "inline-flex size-8 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-color-focus)] @max-[20rem]/fve-notification:col-start-1 @max-[20rem]/fve-notification:row-start-1 @max-[20rem]/fve-notification:justify-self-end"
                _dataOn ("click", removalExpression "evt.currentTarget.closest('[data-fve-notification]')" "dismiss" config)
                closeIcon
            }
            match config.actions with
            | Some actions -> div { _class "col-start-2 col-end-4 mt-3 flex flex-wrap items-center gap-2 @max-[20rem]/fve-notification:col-start-1 @max-[20rem]/fve-notification:col-end-2 @max-[20rem]/fve-notification:row-start-3"; actions }
            | None -> ()
        }

/// <category>notification</category>
[<NoEquality; NoComparison>]
type NotificationRegionConfig =
    private
        { id:string
          label:string
          notification:NotificationConfig option
          withinContainer:bool }

/// <summary>A consumer-supplied bottom-end region that renders zero or one latest notification.</summary>
/// <remarks>
/// Supply zero or one latest confirmed notification. A newer notification replaces the current one.
/// </remarks>
/// <category>notification</category>
[<RequireQualifiedAccess>]
module NotificationRegion =
    /// Creates a notification region with an optional current notification.
    let create id label notification =
        { id = TextField.stableId id
          label = TextField.requiredText (nameof label) label
          notification = notification
          withinContainer = false }

    let withinContainer (config:NotificationRegionConfig) = { config with withinContainer = true }

    let render (config:NotificationRegionConfig) =
        let positionClasses =
            if config.withinContainer then "absolute max-h-[calc(100%-24px)]"
            else "fixed max-h-[calc(100dvh-24px)]"
        section {
            _id config.id
            _ariaLabel config.label
            _attr ("data-fve-notification-region", "true")
            _class (positionClasses + " pointer-events-none inset-x-[12px] bottom-[12px] z-50 ml-auto grid w-auto max-w-sm overflow-y-auto overscroll-contain @container/fve-notification sm:inset-x-auto sm:bottom-4 sm:end-4 sm:w-full")
            match config.notification with
            | Some notification -> Notification.render notification
            | None -> ()
        }
