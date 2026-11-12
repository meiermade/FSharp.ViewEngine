namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type private NotificationData =
    { id:string; title:string; content:HtmlElement; colors:ColorPalette; kind:string
      announcement:LiveAnnouncement; actions:HtmlElement option; ttl:int option; attributes:HtmlAttribute list }

/// <category>notification</category>
[<AutoOpen>]
module NotificationAttributes =
    type Html with
        static member _ttl(value:int) = Html._attr("ttl", string value)
        static member _persistent(value:bool) = Html._attr("persistent", if value then "true" else "false")
        static member _withinContainer(value:bool) = Html._attr("within-container", if value then "true" else "false")

module internal Notification =
    let private colorIcon = function
        | "success" -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M16.7 5.3a1 1 0 0 1 0 1.4l-8 8a1 1 0 0 1-1.4 0l-4-4a1 1 0 0 1 1.4-1.4L8 12.6l7.3-7.3a1 1 0 0 1 1.4 0Z" clip-rule="evenodd"/></svg>"""
        | "warning" -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M8.5 3.1a1.75 1.75 0 0 1 3 0l6.1 10.6a1.75 1.75 0 0 1-1.5 2.6H3.9a1.75 1.75 0 0 1-1.5-2.6L8.5 3.1ZM10 7a.75.75 0 0 1 .75.75v3a.75.75 0 0 1-1.5 0v-3A.75.75 0 0 1 10 7Zm0 6a.88.88 0 1 0 0 1.75A.88.88 0 0 0 10 13Z" clip-rule="evenodd"/></svg>"""
        | "error" -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16ZM7.2 7.2a.75.75 0 0 1 1.1 0L10 8.9l1.7-1.7a.75.75 0 1 1 1.1 1.1L11.1 10l1.7 1.7a.75.75 0 1 1-1.1 1.1L10 11.1l-1.7 1.7a.75.75 0 1 1-1.1-1.1L8.9 10 7.2 8.3a.75.75 0 0 1 0-1.1Z" clip-rule="evenodd"/></svg>"""
        | "info"
        | "primary" -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm.75-8.75a.75.75 0 0 0-1.5 0v4a.75.75 0 0 0 1.5 0v-4ZM10 5.5a1 1 0 1 0 0 2 1 1 0 0 0 0-2Z" clip-rule="evenodd"/></svg>"""
        | _ -> raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path d="M10 2a5 5 0 0 0-5 5v2.3c0 .7-.2 1.3-.6 1.9l-1 1.5A1.5 1.5 0 0 0 4.7 15h10.6a1.5 1.5 0 0 0 1.3-2.3l-1-1.5a3.5 3.5 0 0 1-.6-1.9V7a5 5 0 0 0-5-5ZM8 16a2 2 0 0 0 4 0H8Z"/></svg>"""

    let private closeIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 1 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""

    let private render (config:NotificationData) =
        let content = article {
            _attr ("data-fve-notification", "true")
            match config.ttl with
            | Some milliseconds -> _attr ("data-fve-notification-ttl", string milliseconds)
            | None -> _attr ("data-fve-notification-persistent", "true")
            let classes = "pointer-events-auto relative grid min-w-0 grid-cols-[auto_minmax(0,1fr)_auto] items-start gap-x-3 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 text-[var(--fve-text)] shadow-lg [overflow-wrap:anywhere] transition-[transform,opacity] duration-200 motion-reduce:transition-none @max-[20rem]/fve-notification:grid-cols-1 @max-[20rem]/fve-notification:gap-y-2 @max-[20rem]/fve-notification:p-3"
            for attribute in ComponentElement.attributes ([_id config.id; _style (ComponentColors.style config.colors []); _class classes] @ config.attributes) do attribute
            span {
                _class "mt-0.5 inline-flex size-7 items-center justify-center rounded-full bg-[var(--fve-internal-color-soft)] text-[var(--fve-internal-color-text)] @max-[20rem]/fve-notification:hidden"
                colorIcon config.kind
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
                _class "inline-flex size-8 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-internal-color-focus)] @max-[20rem]/fve-notification:col-start-1 @max-[20rem]/fve-notification:row-start-1 @max-[20rem]/fve-notification:justify-self-end"
                closeIcon
            }
            match config.actions with
            | Some actions -> div { _class "col-start-2 col-end-4 mt-3 flex flex-wrap items-center gap-2 @max-[20rem]/fve-notification:col-start-1 @max-[20rem]/fve-notification:col-end-2 @max-[20rem]/fve-notification:row-start-3"; actions }
            | None -> ()
        }
        ComponentElement.render "fve-notification" [_id (config.id + "-host"); _class "contents"] [] [content]

    let actions attributes children =
        if String.IsNullOrWhiteSpace (Render.toString (ComponentContent.fragment children)) then empty
        else ComponentPart("Notification", "actions", ComponentElement.render "div" [_class "flex flex-wrap items-center gap-2"] attributes children) :> HtmlElement

    let root attributes children =
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "attributes" "A stable notification ID without whitespace is required."
        let persistent = ComponentElement.value "persistent" attributes = Some "true"
        let ttl =
            match ComponentElement.value "ttl" attributes with
            | Some _ when persistent -> invalidArg "attributes" "Use a lifetime or persistent mode, not both."
            | Some value -> match Int32.TryParse value with true, value when value > 0 -> Some value | _ -> invalidArg "attributes" "A notification lifetime must be positive."
            | None -> if persistent then None else Some 5000
        let parts, body = ComponentParts.split "Notification" children
        let announcement = match ComponentElement.choice "announcement" "polite" ["static"; "polite"; "assertive"] attributes with "static" -> LiveAnnouncement.Static | "assertive" -> LiveAnnouncement.Assertive | _ -> LiveAnnouncement.Polite
        render { id = id; title = ComponentElement.required "label" attributes; content = ComponentContent.fragment body
                 colors = ComponentElement.palette attributes; kind = ComponentElement.value "color" attributes |> Option.defaultValue "neutral"
                 announcement = announcement; actions = Map.tryFind "actions" parts; ttl = ttl
                 attributes = ComponentHtml.safeAttributes ["id"; "label"; "color"; "palette"; "announcement"; "ttl"; "persistent"] attributes }

    let region attributes (children:HtmlElement list) =
        ComponentElement.required "id" attributes |> ignore
        ComponentElement.required "aria-label" attributes |> ignore
        let children = children |> List.filter (function :? NoopElement -> false | _ -> true)
        if children.Length > 1 then invalidArg "children" "A notification region accepts zero or one current notification."
        let position = if ComponentElement.value "within-container" attributes = Some "true" then "absolute max-h-[calc(100%-24px)]" else "fixed max-h-[calc(100dvh-24px)]"
        ComponentElement.render "section"
            [_attr("data-fve-notification-region", "true"); _class (position + " pointer-events-none inset-x-[12px] bottom-[12px] z-50 ml-auto grid w-auto max-w-sm overflow-y-auto overscroll-contain @container/fve-notification sm:inset-x-auto sm:bottom-4 sm:end-4 sm:w-full")]
            (ComponentHtml.safeAttributes ["within-container"] attributes) children

/// <category>notification</category>
[<AutoOpen>]
module NotificationDsl =
    let fveNotification = ComponentElementBuilder(Notification.root)
    let fveNotificationActions = ComponentElementBuilder(Notification.actions)
    let fveNotificationRegion = ComponentElementBuilder(Notification.region)
