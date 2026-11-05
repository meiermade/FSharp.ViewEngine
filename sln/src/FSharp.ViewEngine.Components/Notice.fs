namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>notice</category>
[<RequireQualifiedAccess>]
type NoticeVariant = Solid | Soft | Outline | Ghost

/// <category>notice</category>
[<AutoOpen>]
module NoticeAttributes =
    type Html with
        static member _variant(value:NoticeVariant) = Html._attr("variant", match value with NoticeVariant.Solid -> "solid" | NoticeVariant.Soft -> "soft" | NoticeVariant.Outline -> "outline" | NoticeVariant.Ghost -> "ghost")

module internal Notice =
    let actions attributes children =
        if String.IsNullOrWhiteSpace (Render.toString (ComponentContent.fragment children)) then empty
        else ComponentPart("Notice", "actions", ComponentElement.render "div" [_class "flex flex-wrap items-center gap-2"] attributes children) :> HtmlElement

    let render attributes children =
        let title = ComponentElement.required "label" attributes
        let parts, body = ComponentParts.split "Notice" children
        let variant =
            match ComponentElement.choice "variant" "soft" ["solid"; "soft"; "outline"; "ghost"] attributes with
            | "solid" -> ComponentColors.solid | "outline" -> ComponentColors.outline | "ghost" -> ComponentColors.ghost | _ -> ComponentColors.soft
        let announcement = ComponentElement.choice "announcement" "static" ["static"; "polite"; "assertive"] attributes
        ComponentElement.render "div"
            [_attr("data-fve-notice", "true"); _style (ComponentColors.style (ComponentElement.palette (_color Color.Info :: attributes)) []); _class ("grid min-w-0 gap-3 [overflow-wrap:anywhere] rounded-[var(--fve-radius-control)] p-4 text-sm " + variant)]
            (ComponentHtml.safeAttributes ["label"; "variant"; "color"; "palette"; "announcement"] attributes) [
                yield div {
                    if announcement = "polite" then _role "status"; _ariaAtomic true
                    elif announcement = "assertive" then _role "alert"; _ariaAtomic true
                    p { _class "font-semibold"; title }
                    div { _class "mt-1 min-w-0"; body }
                }
                match Map.tryFind "actions" parts with Some actions -> yield actions | None -> () ]

/// <category>notice</category>
[<AutoOpen>]
module NoticeDsl =
    let fveNotice = ComponentElementBuilder(Notice.render)
    let fveNoticeActions = ComponentElementBuilder(Notice.actions)
