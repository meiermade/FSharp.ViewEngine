namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

/// <category>notice</category>
[<RequireQualifiedAccess>]
type NoticeColor =
    | Primary
    | Secondary
    | Success
    | Warning
    | Error
    | Info
    | Neutral
    | Custom of ColorPalette

/// <summary>Selects notice presentation independently of color and announcement urgency.</summary>
/// <category>notice</category>
[<RequireQualifiedAccess>]
type NoticeVariant =
    | Solid
    | Soft
    | Outline
    | Ghost

/// <category>notice</category>
[<NoEquality; NoComparison>]
type NoticeConfig =
    private
        { id:string
          title:string
          content:HtmlElement
          color:NoticeColor
          variant:NoticeVariant
          announcement:LiveAnnouncement
          actions:HtmlElement option }

/// <category>notice</category>
[<RequireQualifiedAccess>]
module Notice =
    /// Defaults to Info + borderless Soft, Static announcement policy, and no actions.
    let create id title content =
        { id = TextField.stableId id
          title = TextField.requiredText (nameof title) title
          content = content
          color = NoticeColor.Info
          variant = NoticeVariant.Soft
          announcement = LiveAnnouncement.Static
          actions = None }
    let withColor color (config:NoticeConfig) = { config with color = color }
    /// Selects presentation without adding interactive behavior or changing announcements.
    let withVariant variant (config:NoticeConfig) = { config with variant = variant }
    /// Announcement urgency is independent of visual color. Static notices do not create live regions.
    let withAnnouncement announcement (config:NoticeConfig) = { config with announcement = announcement }
    let withActions actions (config:NoticeConfig) = { config with actions = Some actions }
    let private palette = function
        | NoticeColor.Primary -> ComponentColors.primary
        | NoticeColor.Secondary -> ComponentColors.secondary
        | NoticeColor.Success -> ComponentColors.success
        | NoticeColor.Warning -> ComponentColors.warning
        | NoticeColor.Error -> ComponentColors.error
        | NoticeColor.Info -> ComponentColors.info
        | NoticeColor.Neutral -> ComponentColors.neutral
        | NoticeColor.Custom colors -> colors

    let render (config:NoticeConfig) =
        div {
            _id config.id
            _attr ("data-fve-notice", "true")
            _style (ComponentColors.style (palette config.color) [])
            _class (ComponentHtml.classes [
                "grid min-w-0 gap-3 [overflow-wrap:anywhere] rounded-[var(--fve-radius-control)] p-4 text-sm"
                match config.variant with
                | NoticeVariant.Solid -> ComponentColors.solid
                | NoticeVariant.Soft -> ComponentColors.soft
                | NoticeVariant.Outline -> ComponentColors.outline
                | NoticeVariant.Ghost -> ComponentColors.ghost ])
            div {
                match config.announcement with
                | LiveAnnouncement.Static -> ()
                | LiveAnnouncement.Polite -> _role "status"; _ariaAtomic true
                | LiveAnnouncement.Assertive -> _role "alert"; _ariaAtomic true
                p { _class "font-semibold"; config.title }
                div { _class "mt-1 min-w-0"; config.content }
            }
            match config.actions with
            | Some actions -> div { _class "flex flex-wrap items-center gap-2"; actions }
            | None -> ()
        }
