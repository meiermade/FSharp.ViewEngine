namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type LoadingIndicatorConfig =
    private
        { label:string
          size:ControlSize
          labelVisible:bool
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module LoadingIndicator =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible loading label is required."
        { label = label; size = ControlSize.Medium; labelVisible = false; attributes = [] }

    let withSize size (config:LoadingIndicatorConfig) = { config with size = size }
    let withVisibleLabel (config:LoadingIndicatorConfig) = { config with labelVisible = true }
    let withAttributes attributes (config:LoadingIndicatorConfig) = { config with attributes = attributes }

    let render config =
        span {
            _role "status"
            _ariaLive "polite"
            _class "inline-flex items-center gap-2 text-sm text-[var(--fve-muted-text)]"
            for attribute in ComponentHtml.safeAttributes [ "role"; "aria-live"; "class" ] config.attributes do attribute
            ComponentHtml.loadingGlyph config.size
            span {
                if config.labelVisible |> not then _class "sr-only"
                config.label
            }
        }
