namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type BrowserConfig =
    private
        { content:HtmlElement
          address:string option }

[<RequireQualifiedAccess>]
module Browser =
    let create content = { content = content; address = None }

    let withAddress address value =
        if String.IsNullOrWhiteSpace address then invalidArg (nameof address) "A browser address is required."
        { value with address = Some address }

    let private toolbar (address:string) =
        div {
            _class "flex items-center gap-3 border-b border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] px-3 py-2"
            _data("fve-browser-toolbar", "true")
            div {
                _class "flex shrink-0 gap-1.5"
                span { _class "size-2.5 rounded-full bg-red-400" }
                span { _class "size-2.5 rounded-full bg-amber-400" }
                span { _class "size-2.5 rounded-full bg-emerald-400" }
            }
            div {
                _class "flex min-w-0 flex-1 items-center gap-2 rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 py-1.5 text-xs font-medium text-[var(--fve-muted-text)] shadow-sm [&_span]:truncate [&_svg]:size-3 [&_svg]:shrink-0"
                _data("fve-browser-address", "true")
                if address.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
                    raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.75 8V6a4.25 4.25 0 0 1 8.5 0v2h.25A1.5 1.5 0 0 1 16 9.5v6a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 4 15.5v-6A1.5 1.5 0 0 1 5.5 8h.25Zm7 0V6a2.75 2.75 0 1 0-5.5 0v2h5.5Z" clip-rule="evenodd" /></svg>"""
                span { address }
            }
        }

    let render (value:BrowserConfig) =
        div {
            _class "w-full min-w-0 overflow-hidden rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface)] shadow-sm"
            _data("browser-frame", "true")
            match value.address with
            | Some address ->
                _data("browser-url", address)
                toolbar address
            | None -> ()
            value.content
        }

    let internal fullscreenContent (value:BrowserConfig) = value.content
