namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>browser</category>
[<NoEquality; NoComparison>]
type BrowserConfig =
    private
        { content:HtmlElement
          address:string option
          windowControls:HtmlElement option
          toolbarActions:HtmlElement option
          expanded:bool }

/// <category>browser</category>
[<RequireQualifiedAccess>]
module Browser =
    let create content = { content=content; address=None; windowControls=None; toolbarActions=None; expanded=false }

    let withAddress address value =
        if String.IsNullOrWhiteSpace address then invalidArg (nameof address) "A browser address is required."
        { value with address=Some address }

    /// Consumer-owned expand/restore links or other leading chrome; no navigation runtime is installed.
    let withWindowControls controls value = { value with windowControls=Some controls }
    /// Consumer-owned state, journey and appearance controls.
    let withToolbarActions actions value = { value with toolbarActions=Some actions }
    /// Fill the viewport while retaining the toolbar. The host owns the URL and documentation visibility.
    let withExpanded expanded (value:BrowserConfig) = { value with expanded=expanded }

    let private toolbar (value:BrowserConfig) =
        div {
            _class "flex shrink-0 flex-wrap items-center gap-2 border-b border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] px-3 py-2"
            _data("fve-browser-toolbar", "true")
            match value.windowControls with
            | Some controls -> div { _class "flex shrink-0 items-center"; controls }
            | None -> div {
                _ariaHidden true
                _class "flex shrink-0 gap-1.5"
                span { _class "size-2.5 rounded-full bg-red-400" }
                span { _class "size-2.5 rounded-full bg-amber-400" }
                span { _class "size-2.5 rounded-full bg-emerald-400" }
              }
            match value.address with
            | Some address ->
                div {
                    _class "flex min-w-0 flex-[1_1_8rem] items-center gap-2 rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 py-1.5 text-xs font-medium text-[var(--fve-muted-text)] shadow-sm [&_span]:truncate [&_svg]:size-3 [&_svg]:shrink-0"
                    _data("fve-browser-address", "true")
                    _title address
                    if address.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
                        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.75 8V6a4.25 4.25 0 0 1 8.5 0v2h.25A1.5 1.5 0 0 1 16 9.5v6a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 4 15.5v-6A1.5 1.5 0 0 1 5.5 8h.25Zm7 0V6a2.75 2.75 0 1 0-5.5 0v2h5.5Z" clip-rule="evenodd" /></svg>"""
                    span { address }
                }
            | None -> ()
            match value.toolbarActions with
            | Some actions -> div { _class "ml-auto min-w-0 max-w-full"; actions }
            | None -> ()
        }

    let render (value:BrowserConfig) =
        div {
            _class ("flex w-full min-w-0 flex-col overflow-hidden bg-[var(--fve-surface)] "+(if value.expanded then "h-dvh" else "rounded-xl border border-[var(--fve-border)] shadow-sm"))
            _data("browser-frame", "true")
            _data("fve-browser-expanded", if value.expanded then "true" else "false")
            match value.address with Some address -> _data("browser-url", address) | None -> ()
            if value.address.IsSome || value.windowControls.IsSome || value.toolbarActions.IsSome then toolbar value
            div {
                _data("fve-browser-content", "true")
                _class (if value.expanded then "min-h-0 min-w-0 flex-1 overflow-auto" else "min-w-0")
                value.content
            }
        }
