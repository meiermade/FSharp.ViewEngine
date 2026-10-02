namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>tooltip</category>
[<RequireQualifiedAccess>]
type TooltipSide =
    | Top
    | Bottom
    | Start
    | End

/// <category>tooltip</category>
[<NoEquality; NoComparison>]
type TooltipConfig =
    private
        { id:string
          description:string
          content:HtmlElement
          side:TooltipSide
          delay:int
          attributes:HtmlAttribute list }

/// <summary>
/// Supplementary, non-interactive description shown on pointer hover and keyboard focus.
/// </summary>
/// <category>tooltip</category>
[<RequireQualifiedAccess>]
module Tooltip =
    let create id description content =
        { id = TextField.stableId id
          description = TextField.requiredText (nameof description) description
          content = content
          side = TooltipSide.Top
          delay = 400
          attributes = [] }

    let withSide side (config:TooltipConfig) = { config with side = side }

    let withDelay milliseconds (config:TooltipConfig) =
        if milliseconds < 0 || milliseconds > 2000 then invalidArg (nameof milliseconds) "Tooltip delay must be between zero and 2000 milliseconds."
        { config with delay = milliseconds }

    let withAttributes attributes (config:TooltipConfig) = { config with attributes = attributes }

    let render (config:TooltipConfig) =
        let contentId = config.id + "-content"
        let anchor = "--fve-tooltip-" + ComponentHtml.signalToken config.id
        let position =
            match config.side with
            | TooltipSide.Top -> "top"
            | TooltipSide.Bottom -> "bottom"
            | TooltipSide.Start -> "left"
            | TooltipSide.End -> "right"
        let show = $"clearTimeout(el._fveTooltipTimer); const tip = document.getElementById('{contentId}'); el._fveTooltipTimer = setTimeout(() => {{ if (!tip.matches(':popover-open')) tip.showPopover() }}, {config.delay})"
        let hide = $"clearTimeout(el._fveTooltipTimer); document.getElementById('{contentId}')?.hidePopover()"
        span {
            _id config.id
            _class "inline-flex"
            _style $"anchor-name:{anchor}"
            for attribute in ComponentHtml.safeAttributes [ "id"; "class"; "style"; "aria-describedby" ] config.attributes do attribute
            span {
                _ariaDescribedby contentId
                _class "inline-flex"
                _dataOn ("mouseenter", show)
                _dataOn ("mouseleave", hide)
                _dataOn ("focusin", show)
                _dataOn ("focusout", hide)
                _dataOn ("keydown", $"if (evt.key == 'Escape') {{ clearTimeout(el._fveTooltipTimer); document.getElementById('{contentId}')?.hidePopover(); evt.stopPropagation() }}")
                config.content
            }
            span {
                _id contentId
                _role "tooltip"
                _attr ("popover", "manual")
                _style $"position-anchor:{anchor};position-area:{position};position-try-fallbacks:flip-block,flip-inline"
                _class "m-2 max-w-64 rounded-md bg-[var(--fve-text)] px-2.5 py-1.5 text-xs leading-5 text-[var(--fve-surface)] shadow-lg [@media(prefers-reduced-motion:no-preference)]:transition-[display,opacity] [transition-behavior:allow-discrete] starting:open:opacity-0"
                config.description
            }
        }
