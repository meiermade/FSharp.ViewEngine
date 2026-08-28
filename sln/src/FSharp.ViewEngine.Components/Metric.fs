namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type MetricConfig =
    private
        { label:string
          value:HtmlElement
          description:string option
          trend:string option
          status:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Metric =
    let create label value =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A metric label is required."
        { label = label
          value = value
          description = None
          trend = None
          status = None
          attributes = [] }

    let text (label:string) (value:string) = create label (span { value })
    let withDescription (description:string) (config:MetricConfig) = { config with description = Some description }
    let withTrend (trend:string) (config:MetricConfig) = { config with trend = Some trend }
    let withStatus (status:HtmlElement) (config:MetricConfig) = { config with status = Some status }
    let withAttributes attributes (config:MetricConfig) = { config with attributes = attributes }

    let render config =
        div {
            _class "min-w-0"
            for attribute in ComponentHtml.safeAttributes [ "class" ] config.attributes do attribute
            div {
                _class "flex flex-wrap items-center gap-2"
                p { _class "text-xs font-medium text-[var(--fve-muted-text)]"; config.label }
                config.status |> Option.defaultValue empty
            }
            div { _class "mt-2 break-words text-2xl font-semibold tracking-tight text-[var(--fve-text)]"; config.value }
            match config.trend with
            | Some trend ->
                p {
                    _class "mt-2 text-xs font-medium text-[var(--fve-text)]"
                    span {
                        _class "sr-only"
                        "Trend: "
                    }
                    trend
                }
            | None -> ()
            match config.description with
            | Some description -> p { _class "mt-1 text-xs text-[var(--fve-muted-text)]"; description }
            | None -> ()
        }
