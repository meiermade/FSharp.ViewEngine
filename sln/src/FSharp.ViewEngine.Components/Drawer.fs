namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>drawer</category>
[<RequireQualifiedAccess>]
type DrawerSide =
    | Start
    | End
    | Top
    | Bottom

/// <category>drawer</category>
[<RequireQualifiedAccess>]
type DrawerSize =
    | Standard
    | Large

/// <category>drawer</category>
[<NoEquality; NoComparison>]
type DrawerConfig =
    private
        { id:string
          title:string
          body:HtmlElement
          description:string option
          footer:HtmlElement option
          initialFocusId:string
          side:DrawerSide
          size:DrawerSize
          container:string option
          isOpen:bool
          attributes:HtmlAttribute list }

/// <category>drawer</category>
[<RequireQualifiedAccess>]
module Drawer =
    let create id title body =
        NativeOverlay.requireText (nameof id) "A stable drawer ID is required." id
        NativeOverlay.requireText (nameof title) "A drawer title is required." title
        { id = id
          title = title
          body = body
          description = None
          footer = None
          initialFocusId = $"{id}-close"
          side = DrawerSide.End
          size = DrawerSize.Standard
          container = None
          isOpen = false
          attributes = [] }

    let withDescription description (config:DrawerConfig) =
        NativeOverlay.requireText (nameof description) "A drawer description cannot be empty." description
        { config with description = Some description }

    let withFooter footer (config:DrawerConfig) = { config with footer = Some footer }

    let withInitialFocus initialFocusId (config:DrawerConfig) =
        NativeOverlay.requireText (nameof initialFocusId) "A drawer initial-focus ID cannot be empty." initialFocusId
        { config with initialFocusId = initialFocusId }

    let withSide side (config:DrawerConfig) = { config with side = side }

    let withSize size (config:DrawerConfig) = { config with size = size }

    /// Consumer-owned dimensions and route lifecycle; structural overlay attributes remain protected.
    let withAttributes attributes (config:DrawerConfig) = { config with attributes = config.attributes @ attributes }

    /// Opt into an app-contained dialog. Render beside this background inside a positioned viewport.
    let withContainer backgroundId (config:DrawerConfig) =
        NativeOverlay.requireText (nameof backgroundId) "A drawer background ID is required." backgroundId
        { config with container=Some backgroundId }

    /// Server-owned initial visibility for a contained drawer (also available without JavaScript).
    let withOpen isOpen (config:DrawerConfig) = { config with isOpen=isOpen }

    let trigger label (config:DrawerConfig) =
        match config.container with
        | Some _ -> NativeOverlay.containedTrigger config.id config.initialFocusId label
        | None -> NativeOverlay.trigger config.id (Some config.initialFocusId) label

    let private closeButton label (config:DrawerConfig) =
        NativeOverlay.closeButton config.id $"{config.id}-close" label

    let render (config:DrawerConfig) =
        if config.isOpen && config.container.IsNone then invalidArg (nameof config) "withOpen requires withContainer; use the native trigger for a document-modal drawer."
        let titleId = $"{config.id}-title"
        let descriptionId = $"{config.id}-description"
        let sideClasses =
            match config.side with
            | DrawerSide.Start -> "inset-y-0 left-0 ml-0 mr-auto h-dvh border-r"
            | DrawerSide.End -> "inset-y-0 right-0 ml-auto mr-0 h-dvh border-l"
            | DrawerSide.Top -> "inset-x-0 top-0 mb-auto mt-0 w-full max-w-none border-b"
            | DrawerSide.Bottom -> "inset-x-0 bottom-0 mb-0 mt-auto w-full max-w-none border-t"
        let sizeClasses =
            match config.side, config.size with
            | (DrawerSide.Start | DrawerSide.End), DrawerSize.Standard -> "w-full max-w-full sm:w-96"
            | (DrawerSide.Start | DrawerSide.End), DrawerSize.Large -> "w-full max-w-full sm:w-[42rem]"
            | (DrawerSide.Top | DrawerSide.Bottom), DrawerSize.Standard -> "h-[min(20rem,calc(100%-3rem))]"
            | (DrawerSide.Top | DrawerSide.Bottom), DrawerSize.Large -> "h-[min(36rem,calc(100%-3rem))]"
        let content = dialog {
            _id config.id
            _ariaLabelledby titleId
            _ariaModal config.container.IsNone
            if config.container.IsSome then _open config.isOpen
            if config.description.IsSome then _ariaDescribedby descriptionId
            for attribute in ComponentHtml.safeAttributes [ "id"; "class"; "open"; "aria-labelledby"; "aria-describedby"; "aria-modal"; "data-on:close"; "data-on:click" ] config.attributes do attribute
            _dataOn ("click", NativeOverlay.dismissOnBackdropExpression config.id)
            _dataOn ("close", NativeOverlay.restoreFocusExpression config.id)
            _class (ComponentHtml.classes [
                (if config.container.IsSome then "absolute max-h-full" else "fixed max-h-none")
                " rounded-none border-[var(--fve-border)] bg-[var(--fve-surface)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-[var(--fve-overlay-backdrop)]"
                sizeClasses
                (if config.container.IsSome then sideClasses.Replace("h-dvh", "h-full") else sideClasses) ])
            div {
                _class "flex h-full flex-col"
                div {
                    _class "flex shrink-0 items-start justify-between gap-4 border-b border-[var(--fve-border)] p-5"
                    div {
                        _class "min-w-0 flex-1"
                        h2 { _id titleId; _class "text-lg font-semibold [overflow-wrap:anywhere]"; config.title }
                        match config.description with
                        | Some description -> p { _id descriptionId; _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                        | None -> ()
                    }
                    closeButton "Close" config
                }
                div { _class "min-h-0 flex-1 overflow-y-auto p-5"; config.body }
                match config.footer with
                | Some footer -> div { _class "flex shrink-0 flex-wrap justify-end gap-3 border-t border-[var(--fve-border)] p-5"; footer }
                | None -> ()
            }
        }
        match config.container with
        | Some background -> NativeOverlay.contained config.id background true content
        | None -> content
