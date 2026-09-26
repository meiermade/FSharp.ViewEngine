namespace FSharp.ViewEngine.Components.Primitives

open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type PhoneConfig = private { content:HtmlElement }

[<RequireQualifiedAccess>]
module Phone =
    let create content = { content = content }

    let private hardware =
        div {
            _class "contents"
            _attr ("data-fve-phone-hardware", "true")
            _ariaHidden true
            span { _class "absolute -left-[0.6rem] top-28 -z-10 h-10 w-1.5 rounded bg-neutral-600"; _data("fve-phone-side-button", "true") }
            span { _class "absolute -left-[0.6rem] top-[10.25rem] -z-10 h-10 w-1.5 rounded bg-neutral-600"; _data("fve-phone-side-button", "true") }
            span { _class "absolute -right-[0.6rem] top-[8.5rem] -z-10 h-20 w-1.5 rounded bg-neutral-600"; _data("fve-phone-side-button", "true") }
        }

    let render (value:PhoneConfig) =
        div {
            _class "relative mx-auto h-[40rem] max-h-full w-[min(19.25rem,100%)] rounded-[3.25rem] border-[5px] border-neutral-600 bg-zinc-950 p-1.5 shadow-2xl"
            _attr ("data-fve-phone", "true")
            _attr ("data-fve-phone-frame", "iphone-inspired")
            hardware
            div {
                _class "flex h-full min-h-0 flex-col overflow-hidden rounded-[2.8rem] bg-[var(--fve-surface)]"
                _data("fve-phone-screen", "true")
                div {
                    _class "grid h-12 shrink-0 grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center bg-[var(--fve-surface)] px-5 pt-2 text-xs font-semibold text-[var(--fve-text)] [&>:last-child]:justify-self-end"
                    _data("fve-phone-status", "true")
                    span { "9:41" }
                    span { _class "h-[1.125rem] w-16 rounded-full bg-neutral-950"; _data("fve-phone-camera", "true") }
                    span { "5G · 100%" }
                }
                div { _class "min-h-0 flex-1 overflow-auto overscroll-contain"; _data("fve-phone-content", "true"); value.content }
            }
        }

    let internal fullscreenContent (value:PhoneConfig) = render value
