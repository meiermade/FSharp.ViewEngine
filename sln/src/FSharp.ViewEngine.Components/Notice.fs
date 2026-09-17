namespace FSharp.ViewEngine.Components.Primitives

open FSharp.ViewEngine
open type Html

[<RequireQualifiedAccess>]
type NoticeAnnouncement =
    | Static
    | Polite
    | Assertive

[<NoEquality; NoComparison>]
type NoticeConfig =
    private
        { id:string
          title:string
          content:HtmlElement
          tone:Tone
          announcement:NoticeAnnouncement
          actions:HtmlElement option }

[<RequireQualifiedAccess>]
module Notice =
    let create id title content =
        { id = TextField.stableId id
          title = TextField.requiredText (nameof title) title
          content = content
          tone = Tone.Informative
          announcement = NoticeAnnouncement.Static
          actions = None }
    let withTone tone (config:NoticeConfig) = { config with tone = tone }
    /// Announcement urgency is independent of visual tone. Static notices do not create live regions.
    let withAnnouncement announcement (config:NoticeConfig) = { config with announcement = announcement }
    let withActions actions (config:NoticeConfig) = { config with actions = Some actions }
    let render (config:NoticeConfig) =
        div {
            _id config.id
            _attr ("data-fve-notice", "true")
            _class (ComponentHtml.classes [
                "grid min-w-0 gap-3 [overflow-wrap:anywhere] rounded-[var(--fve-radius-control)] p-4 text-sm ring-1 ring-inset"
                ComponentHtml.toneClasses config.tone ])
            div {
                match config.announcement with
                | NoticeAnnouncement.Static -> ()
                | NoticeAnnouncement.Polite -> _role "status"; _ariaAtomic true
                | NoticeAnnouncement.Assertive -> _role "alert"; _ariaAtomic true
                p { _class "font-semibold"; config.title }
                div { _class "mt-1 min-w-0"; config.content }
            }
            match config.actions with
            | Some actions -> div { _class "flex flex-wrap items-center gap-2"; actions }
            | None -> ()
        }
