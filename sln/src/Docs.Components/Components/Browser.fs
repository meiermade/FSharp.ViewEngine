namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>browser</category>
[<AutoOpen>]
module BrowserAttributes =
    type Html with
        static member _address(value:string) = Html._attr("address", value)

module internal Browser =
    let part name attributes children =
        if String.IsNullOrWhiteSpace (Render.toString (ComponentContent.fragment children)) then empty
        else ComponentPart("Browser", name, ComponentElement.render "div" [_class (if name = "controls" then "flex shrink-0 items-center" else "ml-auto min-w-0 max-w-full")] attributes children) :> HtmlElement

    let render attributes children =
        let parts, content = ComponentParts.split "Browser" children
        let address = ComponentElement.value "address" attributes
        if address |> Option.exists String.IsNullOrWhiteSpace then invalidArg "attributes" "A browser address cannot be empty."
        let expanded = ComponentElement.value "expanded" attributes = Some "true"
        let defaults = [
            _class ("flex w-full min-w-0 flex-col overflow-hidden bg-[var(--fve-surface)] " + if expanded then "h-dvh" else "rounded-xl border border-[var(--fve-border)] shadow-sm")
            _data("browser-frame", "true")
            _data("fve-browser-expanded", if expanded then "true" else "false")
            match address with Some address -> _data("browser-url", address) | None -> () ]
        ComponentElement.render "div" defaults (ComponentHtml.safeAttributes ["address"; "expanded"] attributes) [
            if address.IsSome || not parts.IsEmpty then
                yield div {
                    _class "flex shrink-0 flex-wrap items-center gap-2 border-b border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] px-3 py-2"
                    _data("fve-browser-toolbar", "true")
                    match Map.tryFind "controls" parts with
                    | Some controls -> controls
                    | None -> ()
                    match address with
                    | Some address ->
                        div {
                            _class "flex min-w-0 flex-[1_1_8rem] items-center gap-2 rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 py-1.5 text-xs font-medium text-[var(--fve-muted-text)] shadow-sm [&_span]:truncate [&_svg]:size-3 [&_svg]:shrink-0"
                            _data("fve-browser-address", "true")
                            _title address
                            if address.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
                                raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.75 8V6a4.25 4.25 0 0 1 8.5 0v2h.25A1.5 1.5 0 0 1 16 9.5v6a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 4 15.5v-6A1.5 1.5 0 0 1 5.5 8h.25Zm7 0V6a2.75 2.75 0 0 0-5.5 0v2h5.5Z" clip-rule="evenodd" /></svg>"""
                            span { address }
                        }
                    | None -> ()
                    match Map.tryFind "actions" parts with Some actions -> actions | None -> ()
                }
            yield div {
                _data("fve-browser-content", "true")
                _class (if expanded then "min-h-0 min-w-0 flex-1 overflow-auto" else "min-w-0")
                content
            } ]

/// <category>browser</category>
[<AutoOpen>]
module BrowserDsl =
    let fveBrowser = ComponentElementBuilder(Browser.render)
    let fveBrowserWindowControls = ComponentElementBuilder(Browser.part "controls")
    let fveBrowserToolbarActions = ComponentElementBuilder(Browser.part "actions")
