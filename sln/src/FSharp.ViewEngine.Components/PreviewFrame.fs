namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type internal PreviewFrame =
    { id:string
      label:string
      surface:string
      content:HtmlElement }

module internal PreviewFrame =
    let create surface id label content =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "An App-mode frame ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An App-mode frame label is required."
        { id = id; label = label; surface = surface; content = content }

    let render frame =
        div {
            _class "fve-app-mode-frame"
            _attr ("data-fve-app-mode-frame", "true")
            _attr ("data-fve-app-mode-frame-id", frame.id)
            _attr ("data-fve-app-mode-label", frame.label)
            _attr ("data-fve-app-mode-surface", frame.surface)
            button {
                _type "button"
                _attr ("data-fve-app-mode-launch", "true")
                _ariaLabel $"Open {frame.label} in App mode"
                _title $"Open {frame.label} in App mode"
                _class "fve-app-mode-launch"
                raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M7.25 3.75h-3.5v3.5M12.75 3.75h3.5v3.5M7.25 16.25h-3.5v-3.5M12.75 16.25h3.5v-3.5"/></svg>"""
            }
            div {
                _attr ("data-fve-app-mode-content", "true")
                frame.content
            }
        }
