namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type BrowserConfig =
    private
        { content:HtmlElement
          address:string option
          appMode:(string * string) option }

[<RequireQualifiedAccess>]
module Browser =
    let create content = { content = content; address = None; appMode = None }

    let withAddress address value =
        if String.IsNullOrWhiteSpace address then invalidArg (nameof address) "A browser address is required."
        { value with address = Some address }

    let withAppMode id label value = { value with appMode = Some (id, label) }

    /// Adds the package App-mode runtime. Hosts serve the packaged app-mode.js file themselves.
    let script source =
        if String.IsNullOrWhiteSpace source then invalidArg (nameof source) "An App-mode script URL is required."
        Html.script { _src source; _defer true }

    let private toolbar (address:string) =
        div {
            _class "spec-browser-toolbar"
            div {
                _class "spec-browser-dots"
                span { _class "spec-browser-dot spec-browser-dot-red" }
                span { _class "spec-browser-dot spec-browser-dot-amber" }
                span { _class "spec-browser-dot spec-browser-dot-green" }
            }
            div {
                _class "spec-browser-address"
                if address.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
                    raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.75 8V6a4.25 4.25 0 0 1 8.5 0v2h.25A1.5 1.5 0 0 1 16 9.5v6a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 4 15.5v-6A1.5 1.5 0 0 1 5.5 8h.25Zm7 0V6a2.75 2.75 0 1 0-5.5 0v2h5.5Z" clip-rule="evenodd" /></svg>"""
                span { address }
            }
        }

    let render (value:BrowserConfig) =
        let frame =
            match value.address with
            | Some address ->
                div {
                    _class "spec-browser-frame"
                    _data("browser-frame", "true")
                    _data("browser-url", address)
                    toolbar address
                    value.content
                }
            | None ->
                div {
                    _class "spec-browser-frame"
                    _data("browser-frame", "true")
                    value.content
                }
        match value.appMode with
        | Some (id, label) -> PreviewFrame.create "browser" id label frame |> PreviewFrame.render
        | None -> frame
