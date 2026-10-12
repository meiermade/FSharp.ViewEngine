namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

/// <category>phone</category>
[<NoEquality; NoComparison>]
type PhoneConfig = private { content:HtmlElement; toolbar:HtmlElement option; expanded:bool }

/// <category>phone</category>
[<RequireQualifiedAccess>]
module Phone =
    let create content = { content=content; toolbar=None; expanded=false }
    /// Consumer-owned review controls above the device, never inside its simulated status bar.
    let withToolbar toolbar value = { value with toolbar=Some toolbar }
    /// Realistic desktop proportions; edge-to-edge screen content on narrow or short viewports.
    let withExpanded expanded (value:PhoneConfig) = { value with expanded=expanded }

    let private hardware =
        div {
            _class "contents"
            _attr ("data-fve-phone-hardware", "true")
            _ariaHidden true
            span { _class "absolute -left-[0.6rem] top-28 -z-10 h-10 w-1.5 rounded bg-neutral-600"; _data("fve-phone-side-button", "true") }
            span { _class "absolute -left-[0.6rem] top-[10.25rem] -z-10 h-10 w-1.5 rounded bg-neutral-600"; _data("fve-phone-side-button", "true") }
            span { _class "absolute -right-[0.6rem] top-[8.5rem] -z-10 h-20 w-1.5 rounded bg-neutral-600"; _data("fve-phone-side-button", "true") }
        }

    let private device (value:PhoneConfig) =
        div {
            _class ("relative mx-auto shrink-0 rounded-[3.25rem] border-[5px] border-neutral-600 bg-zinc-950 p-1.5 shadow-2xl " + (if value.expanded then "aspect-[77/160] h-full max-h-[40rem] w-auto [@media(max-width:639px),(max-height:639px)]:aspect-auto [@media(max-width:639px),(max-height:639px)]:max-h-none [@media(max-width:639px),(max-height:639px)]:w-full [@media(max-width:639px),(max-height:639px)]:rounded-none [@media(max-width:639px),(max-height:639px)]:border-0 [@media(max-width:639px),(max-height:639px)]:p-0 [@media(max-width:639px),(max-height:639px)]:shadow-none [@media(max-width:639px),(max-height:639px)]:[&_[data-fve-phone-hardware]]:hidden [@media(max-width:639px),(max-height:639px)]:[&_[data-fve-phone-screen]]:rounded-none [@media(max-width:639px),(max-height:639px)]:[&_[data-fve-phone-status]]:hidden" else "h-[40rem] max-h-full w-[min(19.25rem,100%)]"))
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

    let render (value:PhoneConfig) =
        if value.toolbar.IsNone && not value.expanded then device value
        else div {
            _data("fve-phone-fixture", "true")
            _data("fve-phone-expanded", if value.expanded then "true" else "false")
            _class ("flex min-w-0 flex-col bg-[var(--fve-surface-subtle)] " + (if value.expanded then "h-dvh overflow-hidden" else "gap-4 rounded-xl border border-[var(--fve-border)] pb-4"))
            match value.toolbar with
            | Some toolbar -> div { _data("fve-phone-toolbar", "true"); _class "shrink-0 border-b border-[var(--fve-border)] px-3 py-2"; toolbar }
            | None -> ()
            div {
                _data("fve-phone-stage", "true")
                _class (if value.expanded then "flex min-h-0 flex-1 items-center justify-center p-4 [@media(max-width:639px),(max-height:639px)]:p-0" else "px-4")
                device value
            }
        }
