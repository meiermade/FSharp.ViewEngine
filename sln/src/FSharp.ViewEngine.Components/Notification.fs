namespace FSharp.ViewEngine.Components.Primitives

open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type NotificationConfig =
    private
        { id:string
          title:string
          content:HtmlElement
          tone:Tone
          announcement:NoticeAnnouncement
          actions:HtmlElement option }

[<RequireQualifiedAccess>]
module Notification =
    let create id title content =
        { id = TextField.stableId id
          title = TextField.requiredText (nameof title) title
          content = content
          tone = Tone.Neutral
          announcement = NoticeAnnouncement.Polite
          actions = None }

    let withTone tone (config:NotificationConfig) = { config with tone = tone }
    let withAnnouncement announcement (config:NotificationConfig) = { config with announcement = announcement }
    let withActions actions (config:NotificationConfig) = { config with actions = Some actions }

    let private closeIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5" aria-hidden="true"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 1 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""

    let render (config:NotificationConfig) =
        let signal = ComponentHtml.signalToken (config.id + "-visible")
        let visible = "$" + signal
        article {
            _id config.id
            _attr ("data-fve-notification", "true")
            _dataSignals $"{{{signal}: true}}"
            _dataShow visible
            _class (ComponentHtml.classes [
                "grid min-w-0 grid-cols-[minmax(0,1fr)_auto] gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-4 text-[var(--fve-text)] shadow-lg ring-1 ring-[var(--fve-border)] [overflow-wrap:anywhere]"
                match config.tone with
                | Tone.Neutral -> ""
                | tone -> ComponentHtml.toneClasses tone ])
            div {
                _class "min-w-0"
                match config.announcement with
                | NoticeAnnouncement.Static -> ()
                | NoticeAnnouncement.Polite -> _role "status"; _ariaAtomic true
                | NoticeAnnouncement.Assertive -> _role "alert"; _ariaAtomic true
                p { _class "text-sm font-semibold"; config.title }
                div { _class "mt-1 text-sm"; config.content }
            }
            button {
                _type "button"
                _ariaLabel ("Dismiss " + config.title)
                _class "inline-flex size-8 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                _dataOn ("click", $"evt.currentTarget.blur(); {visible} = false; evt.currentTarget.closest('[data-fve-notification-region]')?.dispatchEvent(new CustomEvent('fve-notification-dismiss', {{ bubbles: true, detail: {{ id: {ComponentHtml.javascriptString config.id} }} }}))")
                closeIcon
            }
            match config.actions with
            | Some actions -> div { _class "col-span-2 flex flex-wrap items-center gap-2"; actions }
            | None -> ()
        }

[<NoEquality; NoComparison>]
type NotificationRegionConfig = private { id:string; label:string; notifications:NotificationConfig list; withinContainer:bool }

/// A consumer-supplied notification stack. It provides no queue, timer, or persistence service.
[<RequireQualifiedAccess>]
module NotificationRegion =
    let create id label notifications =
        if List.isEmpty notifications then invalidArg (nameof notifications) "At least one notification is required."
        { id = TextField.stableId id
          label = TextField.requiredText (nameof label) label
          notifications = notifications
          withinContainer = false }

    let withinContainer (config:NotificationRegionConfig) = { config with withinContainer = true }

    let render (config:NotificationRegionConfig) =
        section {
            _id config.id
            _ariaLabel config.label
            _attr ("data-fve-notification-region", "true")
            _class ((if config.withinContainer then "absolute" else "fixed") + " inset-x-4 top-4 z-50 ml-auto grid w-auto max-w-sm gap-3 sm:inset-x-auto sm:right-4 sm:w-full")
            for notification in config.notifications do Notification.render notification
        }
