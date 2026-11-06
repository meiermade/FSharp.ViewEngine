namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal LoadingIndicator =
    let render attributes (children:HtmlElement list) =
        let label =
            if List.isEmpty children then ComponentElement.required "aria-label" attributes
            else ComponentElement.value "aria-label" attributes |> Option.defaultValue ""
        let size = ComponentElement.size (Some Size.Medium) attributes |> Option.get
        ComponentElement.render "span"
            [_role "status"; _ariaLive "polite"; _class "inline-flex items-center gap-2 text-sm text-[var(--fve-muted-text)]"]
            (ComponentHtml.safeAttributes ["role"; "aria-live"; "size"] attributes)
            [ ComponentHtml.loadingGlyph size
              if List.isEmpty children then span { _class "sr-only"; label }
              else span { children } ]

/// <category>loading-indicator</category>
[<AutoOpen>]
module LoadingIndicatorDsl =
    /// A polite loading status. Optional ordinary content supplies a visible label.
    let fveLoadingIndicator = ComponentElementBuilder(LoadingIndicator.render)
