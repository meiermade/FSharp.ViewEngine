namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>resizable</category>
[<RequireQualifiedAccess>]
type ResizableOrientation =
    | Horizontal
    | Vertical

/// <category>resizable</category>
[<NoEquality; NoComparison>]
type ResizablePanel =
    private
        { content:HtmlElement
          initialSize:int
          minimumSize:int
          maximumSize:int
          collapsedSize:int option }

/// <category>resizable</category>
[<RequireQualifiedAccess>]
module ResizablePanel =
    /// Creates the leading panel with a 50% initial size and 20–80% bounds.
    let create content =
        { content = content
          initialSize = 50
          minimumSize = 20
          maximumSize = 80
          collapsedSize = None }

    let withInitialSize percent (panel:ResizablePanel) =
        if percent < panel.minimumSize || percent > panel.maximumSize then invalidArg (nameof percent) "Initial size must be within the panel bounds."
        { panel with initialSize = percent }

    let withBounds minimum maximum (panel:ResizablePanel) =
        if minimum < 0 || maximum > 100 || minimum >= maximum then invalidArg (nameof minimum) "Panel bounds must be ordered percentages between 0 and 100."
        { panel with minimumSize = minimum; maximumSize = maximum; initialSize = Math.Clamp(panel.initialSize, minimum, maximum) }

    let collapsible collapsedSize (panel:ResizablePanel) =
        if collapsedSize < 0 || collapsedSize > panel.minimumSize then invalidArg (nameof collapsedSize) "Collapsed size must be between zero and the minimum size."
        { panel with collapsedSize = Some collapsedSize }

/// <category>resizable</category>
[<NoEquality; NoComparison>]
type ResizableConfig =
    private
        { id:string
          label:string
          leading:ResizablePanel
          trailing:HtmlElement
          orientation:ResizableOrientation
          attributes:HtmlAttribute list }

/// <category>resizable</category>
[<RequireQualifiedAccess>]
module Resizable =
    let create id label leading trailing =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable resizable-layout ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible resizable-layout label is required."
        { id = id; label = label; leading = leading; trailing = trailing; orientation = ResizableOrientation.Horizontal; attributes = [] }

    let withOrientation orientation (config:ResizableConfig) = { config with orientation = orientation }
    let withAttributes attributes (config:ResizableConfig) = { config with attributes = attributes }

    let render (config:ResizableConfig) =
        let signal = $"_{ComponentHtml.signalToken config.id}_size"
        let isHorizontal = config.orientation = ResizableOrientation.Horizontal
        let coordinate = if isHorizontal then "clientX" else "clientY"
        let extent = if isHorizontal then "width" else "height"
        let previousKey, nextKey = if isHorizontal then "ArrowLeft", "ArrowRight" else "ArrowUp", "ArrowDown"
        let minimum = config.leading.minimumSize
        let collapsed = config.leading.collapsedSize |> Option.defaultValue minimum
        let toggleCollapse =
            match config.leading.collapsedSize with
            | Some _ -> $"${signal} = ${signal} <= {minimum} ? {config.leading.initialSize} : {collapsed}"
            | None -> ""
        let pointerMove =
            $"el.hasPointerCapture(evt.pointerId) && (${signal} = Math.min({config.leading.maximumSize}, Math.max({minimum}, Number(el.dataset.fveStartSize) + (evt.{coordinate} - Number(el.dataset.fveStartPoint)) / el.parentElement.getBoundingClientRect().{extent} * 100)))"
        let keyboard =
            String.concat "; " [
                $"evt.key == '{previousKey}' && (evt.preventDefault(), ${signal} = Math.max({minimum}, ${signal} - 2))"
                $"evt.key == '{nextKey}' && (evt.preventDefault(), ${signal} = Math.min({config.leading.maximumSize}, ${signal} + 2))"
                $"evt.key == 'Home' && (evt.preventDefault(), ${signal} = {minimum})"
                $"evt.key == 'End' && (evt.preventDefault(), ${signal} = {config.leading.maximumSize})"
                if not (String.IsNullOrEmpty toggleCollapse) then $"evt.key == 'Enter' && (evt.preventDefault(), {toggleCollapse})" ]
        section {
            _id config.id
            _ariaLabel config.label
            _dataSignals $"{{{signal}: {config.leading.initialSize}}}"
            _class (if isHorizontal then "flex min-h-64 min-w-0 overflow-hidden rounded-[var(--fve-radius-panel)] ring-1 ring-inset ring-[var(--fve-border)]" else "flex min-h-96 min-w-0 flex-col overflow-hidden rounded-[var(--fve-radius-panel)] ring-1 ring-inset ring-[var(--fve-border)]")
            for attribute in ComponentHtml.safeAttributes [ "class"; "aria-label" ] config.attributes do attribute
            div {
                _class "min-h-0 min-w-0 shrink-0 overflow-auto"
                _dataAttr ("style", $"'flex-basis:' + ${signal} + '%%' ")
                config.leading.content
            }
            div {
                _role "separator"
                _tabindex 0
                _ariaLabel "Resize panels"
                _ariaOrientation (if isHorizontal then "vertical" else "horizontal")
                _ariaValuemin (string collapsed)
                _ariaValuemax (string config.leading.maximumSize)
                _ariaValuenow (string config.leading.initialSize)
                _dataAttr ("aria-valuenow", $"Math.round(${signal})")
                _attr ("data-fve-resize-handle", "true")
                _dataOn ("pointerdown", $"el.setPointerCapture(evt.pointerId); el.dataset.fveStartPoint = evt.{coordinate}; el.dataset.fveStartSize = ${signal}")
                _dataOn ("pointermove", pointerMove)
                _dataOn ("keydown", keyboard)
                if not (String.IsNullOrEmpty toggleCollapse) then _dataOn ("dblclick", toggleCollapse)
                _class (if isHorizontal then "group relative z-10 flex w-2 shrink-0 touch-none cursor-col-resize items-center justify-center bg-[var(--fve-surface-subtle)] outline-none hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]" else "group relative z-10 flex h-2 shrink-0 touch-none cursor-row-resize items-center justify-center bg-[var(--fve-surface-subtle)] outline-none hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]")
                span { _ariaHidden true; _class (if isHorizontal then "h-8 w-1 rounded-full bg-[var(--fve-border)] group-hover:bg-[var(--fve-muted-text)]" else "h-1 w-8 rounded-full bg-[var(--fve-border)] group-hover:bg-[var(--fve-muted-text)]") }
            }
            div { _class "min-h-0 min-w-0 flex-1 overflow-auto"; config.trailing }
        }
