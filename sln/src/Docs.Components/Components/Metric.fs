namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal Metric =
    let status attributes (children:HtmlElement list) =
        if List.isEmpty children then empty
        else ComponentPart("metric", "status", ComponentElement.render "span" [] attributes children) :> HtmlElement

    let render attributes children =
        let label = ComponentElement.required "label" attributes
        let parts, body = ComponentParts.split "metric" children
        ComponentElement.render "div" [_class "min-w-0"] (ComponentHtml.safeAttributes ["label"; "trend"; "description"] attributes)
            [ div {
                  _class "flex flex-wrap items-center gap-2"
                  p { _class "text-xs font-medium text-[var(--fve-muted-text)]"; label }
                  Map.tryFind "status" parts |> Option.defaultValue empty
              }
              div { _class "mt-2 break-words text-2xl font-semibold tracking-tight text-[var(--fve-text)]"; body }
              match ComponentElement.value "trend" attributes with
              | Some trend -> p { _class "mt-2 text-xs font-medium text-[var(--fve-text)]"; span { _class "sr-only"; "Trend: " }; trend }
              | None -> ()
              match ComponentElement.value "description" attributes with
              | Some description -> p { _class "mt-1 text-xs text-[var(--fve-muted-text)]"; description }
              | None -> () ]

/// <category>metric</category>
[<AutoOpen>]
module MetricDsl =
    /// The value is ordinary rich content; label, description and trend are HTML attributes.
    let fveMetric = ComponentElementBuilder(Metric.render)
    let fveMetricStatus = ComponentElementBuilder(Metric.status)
