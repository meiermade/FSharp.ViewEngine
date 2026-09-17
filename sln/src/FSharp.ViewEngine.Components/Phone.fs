namespace FSharp.ViewEngine.Components.Primitives

open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type PhoneConfig =
    private
        { content:HtmlElement
          appMode:(string * string) option }

[<RequireQualifiedAccess>]
module Phone =
    let create content = { content = content; appMode = None }
    let withAppMode id label (value:PhoneConfig) = { value with appMode = Some (id, label) }

    let private hardware =
        div {
            _attr ("data-fve-phone-hardware", "true")
            _ariaHidden true
            span { _class "fve-phone-side-button fve-phone-side-button-volume-up" }
            span { _class "fve-phone-side-button fve-phone-side-button-volume-down" }
            span { _class "fve-phone-side-button fve-phone-side-button-power" }
        }

    let render (value:PhoneConfig) =
        let phone =
            div {
                _class "fve-phone"
                _attr ("data-fve-phone", "true")
                _attr ("data-fve-phone-frame", "iphone-inspired")
                hardware
                div {
                    _class "fve-phone-screen"
                    div {
                        _class "fve-phone-status"
                        span { "9:41" }
                        span { _class "fve-phone-camera" }
                        span { "5G · 100%" }
                    }
                    div { _class "fve-phone-content"; value.content }
                }
            }
        match value.appMode with
        | Some (id, label) -> PreviewFrame.create "phone" id label phone |> PreviewFrame.render
        | None -> phone
