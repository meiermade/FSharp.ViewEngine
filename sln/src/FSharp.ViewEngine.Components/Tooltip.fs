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
[<AutoOpen>]
module TooltipAttributes =
    type Html with
        static member _side(value:TooltipSide) = Html._attr("side", match value with TooltipSide.Top -> "top" | TooltipSide.Bottom -> "bottom" | TooltipSide.Start -> "start" | TooltipSide.End -> "end")
        static member _delay(value:int) = Html._attr("delay", string value)

/// <summary>
/// Supplementary, non-interactive description shown on pointer hover and keyboard focus.
/// </summary>
/// <remarks>
/// Apply the tooltip's ID plus "-content" to the focusable child's aria-describedby.
/// Append existing description IDs and retain the control's accessible name.
/// Tooltip content must not contain interactive controls or information unavailable elsewhere.
/// </remarks>
/// <category>tooltip</category>
module internal Tooltip =
    let internal render attributes children =
        let id = ComponentElement.required "id" attributes |> TextField.stableId
        let description = ComponentElement.required "description" attributes
        if List.isEmpty children then invalidArg (nameof children) "A tooltip requires a focusable child."
        let delay =
            match ComponentElement.value "delay" attributes with
            | None -> 400
            | Some value -> match Int32.TryParse value with true, delay when delay >= 0 && delay <= 2000 -> delay | _ -> invalidArg "delay" "Tooltip delay must be between zero and 2000 milliseconds."
        let contentId = id + "-content"
        let anchor = "--fve-tooltip-" + ComponentHtml.signalToken id
        let position =
            match ComponentElement.choice "side" "top" ["top"; "bottom"; "start"; "end"] attributes with
            | "start" -> "left"
            | "end" -> "right"
            | side -> side
        ComponentElement.render "fve-tooltip"
            [_id id; _class "inline-flex"; _style $"anchor-name:{anchor}"; _attr("delay", string delay)]
            (ComponentHtml.safeAttributes ["description"; "side"; "delay"] attributes)
            (children @ [span {
                _id contentId
                _role "tooltip"
                _attr ("popover", "manual")
                _style $"position-anchor:{anchor};position-area:{position};position-try-fallbacks:flip-block,flip-inline"
                _class "m-2 max-w-64 before:absolute before:-inset-2 rounded-md bg-[var(--fve-text)] px-2.5 py-1.5 text-xs leading-5 text-[var(--fve-surface)] shadow-lg [@media(prefers-reduced-motion:no-preference)]:transition-[display,opacity] [transition-behavior:allow-discrete] starting:open:opacity-0"
                description
            }])

/// <category>tooltip</category>
[<AutoOpen>]
module TooltipDsl =
    let fveTooltip = ComponentElementBuilder(Tooltip.render)
