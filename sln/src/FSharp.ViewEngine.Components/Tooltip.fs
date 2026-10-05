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
          trigger:string -> HtmlElement
          side:TooltipSide
          delay:int
          attributes:HtmlAttribute list }

/// <summary>
/// Supplementary, non-interactive description shown on pointer hover and keyboard focus.
/// </summary>
/// <remarks>
/// Apply the trigger renderer's description ID to the focusable control's aria-describedby.
/// Append existing description IDs and retain the control's accessible name.
/// Tooltip content must not contain interactive controls or information unavailable elsewhere.
/// </remarks>
/// <category>tooltip</category>
[<RequireQualifiedAccess>]
module Tooltip =
    /// The trigger renderer receives the tooltip description ID. Apply it as aria-describedby
    /// on the focusable control, appending any existing description IDs without changing its name.
    let create id description trigger =
        { id = TextField.stableId id
          description = TextField.requiredText (nameof description) description
          trigger = trigger
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
        let show = $"clearTimeout(el._fveTooltipTimer); const tip = el.querySelector('[role=tooltip]'); el._fveTooltipTimer = setTimeout(() => {{ if (el.isConnected && tip.isConnected && !tip.matches(':popover-open')) tip.showPopover() }}, {config.delay})"
        // The tooltip's hit area covers its margin; a short grace period also tolerates pointer transitions.
        let hide = "clearTimeout(el._fveTooltipTimer); el._fveTooltipTimer = setTimeout(() => { if (!el.matches(':hover') && !el.contains(document.activeElement)) el.querySelector('[role=tooltip]')?.hidePopover() }, 150)"
        let dismiss = "if (evt.key == 'Escape') { clearTimeout(el._fveTooltipTimer); const tip = el.querySelector('[role=tooltip]'); if (tip.matches(':popover-open')) { tip.hidePopover(); evt.preventDefault(); evt.stopPropagation() } }"
        span {
            _id config.id
            _class "inline-flex"
            _style $"anchor-name:{anchor}"
            for attribute in ComponentHtml.safeAttributes [ "id"; "class"; "style"; "aria-describedby" ] config.attributes do attribute
            _dataOn ("mouseenter", show)
            _dataOn ("mouseleave", hide)
            _dataOn ("focusin", show)
            _dataOn ("focusout", hide)
            _dataOn ("keydown__window__capture", dismiss)
            config.trigger contentId
            span {
                _id contentId
                _role "tooltip"
                _attr ("popover", "manual")
                _style $"position-anchor:{anchor};position-area:{position};position-try-fallbacks:flip-block,flip-inline"
                _class "m-2 max-w-64 before:absolute before:-inset-2 rounded-md bg-[var(--fve-text)] px-2.5 py-1.5 text-xs leading-5 text-[var(--fve-surface)] shadow-lg [@media(prefers-reduced-motion:no-preference)]:transition-[display,opacity] [transition-behavior:allow-discrete] starting:open:opacity-0"
                config.description
            }
        }
