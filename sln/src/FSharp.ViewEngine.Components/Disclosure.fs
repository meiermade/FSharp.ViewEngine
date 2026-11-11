namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal Disclosure =
    let render attributes children =
        let title = ComponentElement.required "label" attributes
        ComponentElement.render "details"
            [_data("fve-disclosure", ""); _class "group min-w-0 border-b border-[var(--fve-border)] py-5"]
            (ComponentHtml.safeAttributes ["label"] attributes)
            [ summary {
                  _class "flex min-h-8 cursor-pointer list-none items-start justify-between gap-4 rounded-sm text-base font-semibold text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[var(--fve-brand-ring)] [&::-webkit-details-marker]:hidden"
                  span { _class "min-w-0"; title }
                  raw """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true" class="mt-0.5 size-5 shrink-0 transition-transform group-open:rotate-180"><path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5"/></svg>"""
              }
              div {
                  _class "mt-3 text-base leading-7 text-[var(--fve-muted-text)]"
                  yield! children
              } ]

/// <summary>Native keyboard-accessible disclosure; name groups support mutually exclusive questions.</summary>
/// <category>disclosure</category>
[<AutoOpen>]
module DisclosureDsl =
    let fveDisclosure = ComponentElementBuilder(Disclosure.render)
