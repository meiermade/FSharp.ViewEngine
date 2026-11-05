namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal Callout =
    let render attributes (children:HtmlElement list) =
        let label = ComponentElement.required "label" attributes
        ComponentElement.render "div"
            [_class "border-l-2 border-[var(--fve-brand-ring)] bg-[var(--fve-brand-subtle)] px-4 py-3"; _attr("data-docs-callout", "true")]
            (ComponentHtml.safeAttributes ["label"] attributes)
            [ div { _class "text-xs font-semibold tracking-wide text-[var(--fve-brand-text)] uppercase"; label }
              div { _class "mt-1 text-sm leading-relaxed text-[var(--fve-text)]"; children } ]

/// <category>callout</category>
[<AutoOpen>]
module CalloutDsl =
    /// A labelled, non-announcing native callout with ordinary content.
    let fveCallout = ComponentElementBuilder(Callout.render)
