namespace FSharp.ViewEngine.Components

open System
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type ChartConfig =
    private
        { id:string
          title:string
          summary:HtmlElement
          visual:HtmlElement option
          emptyState:HtmlElement option
          units:string option
          legend:HtmlElement option
          annotations:HtmlElement option
          summaryVisible:bool
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Chart =
    let private createConfig id title summary visual emptyState =
        if String.IsNullOrWhiteSpace id || not (Regex.IsMatch(id, "^[A-Za-z][A-Za-z0-9_-]*$")) then
            invalidArg (nameof id) "A chart ID beginning with a letter and containing only letters, numbers, underscores, or hyphens is required."
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A chart title is required."
        { id = id
          title = title
          summary = summary
          visual = visual
          emptyState = emptyState
          units = None
          legend = None
          annotations = None
          summaryVisible = false
          attributes = [] }

    let create id title summary visual = createConfig id title summary (Some visual) None
    let empty id title summary emptyState = createConfig id title summary None (Some emptyState)
    let withUnits units (config:ChartConfig) = { config with units = Some units }
    let withLegend legend (config:ChartConfig) = { config with legend = Some legend }
    let withAnnotations annotations (config:ChartConfig) = { config with annotations = Some annotations }
    let withVisibleSummary (config:ChartConfig) = { config with summaryVisible = true }
    let withAttributes attributes (config:ChartConfig) = { config with attributes = attributes }

    let render config =
        let titleId = $"{config.id}-title"
        let summaryId = $"{config.id}-summary"
        figure {
            _ariaLabelledby titleId
            _ariaDescribedby summaryId
            _class "min-w-0"
            for attribute in ComponentHtml.safeAttributes [ "class"; "role"; "aria-labelledby"; "aria-describedby" ] config.attributes do attribute
            figcaption {
                _id titleId
                _class "flex flex-wrap items-baseline justify-between gap-2 text-sm font-semibold text-[var(--fve-text)]"
                span { config.title }
                match config.units with
                | Some units -> span { _class "text-xs font-normal text-[var(--fve-muted-text)]"; units }
                | None -> ()
            }
            match config.legend with
            | Some legend ->
                section {
                    _ariaLabel "Legend"
                    _class "mt-3 text-xs text-[var(--fve-muted-text)]"
                    legend
                }
            | None -> ()
            div {
                _class "mt-4 min-w-0 overflow-x-auto"
                match config.visual, config.emptyState with
                | Some visual, _ -> visual
                | None, Some emptyState -> emptyState
                | None, None -> ()
            }
            match config.annotations with
            | Some annotations ->
                section {
                    _ariaLabel "Annotations"
                    _class "mt-3 text-xs text-[var(--fve-muted-text)]"
                    annotations
                }
            | None -> ()
            div {
                _id summaryId
                _class (
                    if config.summaryVisible then
                        "mt-4 border-t border-[var(--fve-border)] pt-4 text-sm text-[var(--fve-text)]"
                    else
                        "sr-only")
                config.summary
            }
        }
