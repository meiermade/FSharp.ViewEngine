namespace Example.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>resizable</category>
[<AutoOpen>]
module ResizableAttributes =
    type Html with
        static member _initialSize(value:int) = Html._attr("initial-size", string value)
        static member _minSize(value:int) = Html._attr("min-size", string value)
        static member _maxSize(value:int) = Html._attr("max-size", string value)
        static member _collapsedSize(value:int) = Html._attr("collapsed-size", string value)
        static member _framed(value:bool) = Html._attr("framed", if value then "true" else "false")

type internal ResizablePartElement(kind:string, attributes:HtmlAttribute list, children:HtmlElement list) =
    inherit HtmlElement()
    member _.Kind = kind
    member _.Attributes = attributes
    member _.Children = children
    override _.Render _ = invalidOp "Resizable parts belong inside fveResizable."

module internal Resizable =
    let part kind attributes children = ResizablePartElement(kind, attributes, children) :> HtmlElement

    let render attributes (children:HtmlElement list) =
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "id" "A stable resizable-layout ID without whitespace is required."
        let label = ComponentElement.required "aria-label" attributes
        let integer name fallback =
            match ComponentElement.value name attributes with
            | None -> fallback
            | Some value -> match Int32.TryParse value with true, number -> number | _ -> invalidArg name "A whole percentage is required."
        let minimum = integer "min-size" 20
        let maximum = integer "max-size" 80
        if minimum < 0 || maximum > 100 || minimum >= maximum then invalidArg "min-size" "Panel bounds must be ordered percentages between zero and 100."
        let initial = integer "initial-size" (Math.Clamp(50, minimum, maximum))
        if initial < minimum || initial > maximum then invalidArg "initial-size" "Initial size must be within the panel bounds."
        let collapsible = ComponentElement.attribute "collapsed-size" attributes |> Option.isSome
        let collapsed = integer "collapsed-size" minimum
        if collapsed < 0 || collapsed > minimum then invalidArg "collapsed-size" "Collapsed size must be between zero and the minimum size."
        let horizontal = ComponentElement.choice "orientation" "horizontal" ["horizontal"; "vertical"] attributes = "horizontal"
        let framed = ComponentElement.choice "framed" "true" ["true"; "false"] attributes = "true"
        let panels, handles =
            children |> List.fold (fun (panels, handles) child ->
                match child with
                | :? ResizablePartElement as part when part.Kind = "panel" -> part :: panels, handles
                | :? ResizablePartElement as part when part.Kind = "handle" -> panels, part :: handles
                | :? NoopElement -> panels, handles
                | _ -> invalidArg "children" "Use exactly two fveResizablePanel children and an optional fveResizableHandle.") ([], [])
        let leading, trailing = match List.rev panels with [leading; trailing] -> leading, trailing | _ -> invalidArg "children" "Resizable requires exactly two panels."
        let handleAttributes, handleChildren = match handles with [] -> [], [] | [handle] -> handle.Attributes, handle.Children | _ -> invalidArg "children" "Resizable accepts one handle."
        let leadingId = ComponentElement.value "id" leading.Attributes |> Option.defaultValue (id + "-leading")
        let rootClasses = ComponentHtml.classes [
            "flex min-w-0 overflow-hidden"
            if not horizontal then "flex-col"
            if framed then (if horizontal then "min-h-64" else "min-h-96") + " rounded-[var(--fve-radius-panel)] ring-1 ring-inset ring-[var(--fve-border)]" else "min-h-0" ]
        let handleClasses = "group relative z-10 flex shrink-0 touch-none items-center justify-center outline-none hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)] " + (if horizontal then "cursor-col-resize " else "cursor-row-resize ") + (if framed then (if horizontal then "w-2 " else "h-2 ") + "bg-[var(--fve-surface-subtle)]" else (if horizontal then "w-1 " else "h-1 ") + "bg-[var(--fve-border)]")
        let grip = span { _ariaHidden true; _class (if horizontal then "h-8 w-1 rounded-full bg-[var(--fve-border)] group-hover:bg-[var(--fve-muted-text)]" else "h-1 w-8 rounded-full bg-[var(--fve-border)] group-hover:bg-[var(--fve-muted-text)]") }
        let handleDefaults = [
            yield _role "separator"
            yield _tabindex 0
            yield _ariaLabel ("Resize " + label)
            yield _ariaControls leadingId
            yield _ariaOrientation (if horizontal then "vertical" else "horizontal")
            yield _ariaValuemin (string collapsed)
            yield _ariaValuemax (string maximum)
            yield _ariaValuenow (string initial)
            yield _attr("data-fve-resize-handle", "true")
            yield _class handleClasses ]
        let hostAttributes = [
            _id id; _role "group"; _ariaLabel label; _class rootClasses
            _attr("initial-size", string initial); _attr("min-size", string minimum); _attr("max-size", string maximum)
            _attr("orientation", if horizontal then "horizontal" else "vertical")
            if collapsible then _attr("collapsed-size", string collapsed) ]
        ComponentElement.render "fve-resizable" hostAttributes
            (ComponentHtml.safeAttributes ["orientation"; "initial-size"; "min-size"; "max-size"; "collapsed-size"; "framed"; "role"] attributes) [
                ComponentElement.render "div" [_id leadingId; _class ("min-h-0 min-w-0 shrink-0" + if framed then " overflow-auto" else ""); _style ($"flex-basis:{initial}%%"); _attr("data-fve-resize-leading", "")] leading.Attributes leading.Children
                ComponentElement.render "div" handleDefaults (ComponentHtml.safeAttributes ["role"; "aria-orientation"; "aria-valuemin"; "aria-valuemax"; "aria-valuenow"; "aria-controls"] handleAttributes) (if handleChildren.IsEmpty then [grip] else handleChildren)
                ComponentElement.render "div" [_class ("min-h-0 min-w-0 flex-1" + if framed then " overflow-auto" else "")] trailing.Attributes trailing.Children ]

/// <category>resizable</category>
[<AutoOpen>]
module ResizableDsl =
    let fveResizable = ComponentElementBuilder(Resizable.render)
    let fveResizablePanel = ComponentElementBuilder(Resizable.part "panel")
    let fveResizableHandle = ComponentElementBuilder(Resizable.part "handle")
