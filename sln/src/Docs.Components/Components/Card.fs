namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal Card =
    let part name attributes (children:HtmlElement list) =
        let children = children |> List.filter (fun child -> not (child :? NoopElement))
        if List.isEmpty children then empty
        else
            let classes =
                match name with
                | "media" -> "min-w-0"
                | "footer" -> "flex min-w-0 flex-wrap items-center gap-2 border-t border-[var(--fve-border)] p-[var(--fve-card-inset,1.25rem)]"
                | _ -> "p-[var(--fve-card-inset,1.25rem)]"
            ComponentPart("card", name, ComponentElement.render "div" [_class classes; _attr("data-fve-card-" + name, "true")] attributes children) :> HtmlElement

    let render attributes children =
        let size = ComponentElement.size (Some Size.Medium) attributes |> Option.get
        let inset =
            match size with
            | Size.ExtraSmall -> "0.5rem" | Size.Small -> "0.75rem" | Size.Medium -> "1.25rem"
            | Size.Large -> "1.75rem" | Size.ExtraLarge -> "2.25rem"
        let parts, body = ComponentParts.split "card" children
        let region name = Map.tryFind name parts |> Option.defaultValue empty
        ComponentElement.render "div"
            [ _attr("data-fve-card", "true")
              _class "min-w-0 overflow-hidden [overflow-wrap:anywhere] rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)]"
              _style ("--fve-card-inset:" + inset) ] (ComponentHtml.safeAttributes ["size"] attributes)
            [ region "media"
              region "header"
              if not (List.isEmpty body) then div { _class "p-[var(--fve-card-inset,1.25rem)]"; body }
              region "footer" ]

/// <summary>Native card regions, ordinary typed attributes and body content.</summary>
/// <category>card</category>
type CardBuilder internal () =
    inherit ComponentElementBuilder(Card.render)

/// <category>card</category>
[<AutoOpen>]
module CardDsl =
    let fveCard = CardBuilder()
    let fveCardHeader = ComponentElementBuilder(Card.part "header")
    let fveCardFooter = ComponentElementBuilder(Card.part "footer")
    let fveCardMedia = ComponentElementBuilder(Card.part "media")
