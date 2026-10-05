namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>dialog</category>
[<NoEquality; NoComparison>]
type DialogConfig =
    private
        { id:string
          title:string
          body:HtmlElement
          description:string option
          footer:HtmlElement option
          initialFocusId:string option
          dismissOnBackdrop:bool
          attributes:HtmlAttribute list }

/// <category>dialog</category>
[<RequireQualifiedAccess>]
module Dialog =
    let create id title body =
        NativeOverlay.requireText (nameof id) "A stable dialog ID is required." id
        NativeOverlay.requireText (nameof title) "A dialog title is required." title
        { id = id
          title = title
          body = body
          description = None
          footer = None
          initialFocusId = None
          dismissOnBackdrop = false
          attributes = [] }

    let withDescription description (config:DialogConfig) =
        NativeOverlay.requireText (nameof description) "A dialog description cannot be empty." description
        { config with description = Some description }

    let withFooter footer (config:DialogConfig) = { config with footer = Some footer }

    let withAttributes attributes (config:DialogConfig) = { config with attributes = attributes }

    let withInitialFocus initialFocusId (config:DialogConfig) =
        NativeOverlay.requireText (nameof initialFocusId) "A dialog initial-focus ID cannot be empty." initialFocusId
        { config with initialFocusId = Some initialFocusId }

    let dismissOnBackdrop (config:DialogConfig) = { config with dismissOnBackdrop = true }

    let trigger label (config:DialogConfig) =
        NativeOverlay.trigger config.id config.initialFocusId label

    let closeButton label (config:DialogConfig) =
        NativeOverlay.closeButton config.id $"{config.id}-close" label

    let render config =
        let titleId = $"{config.id}-title"
        let descriptionId = $"{config.id}-description"
        dialog {
            _id config.id
            _ariaLabelledby titleId
            _ariaModal true
            if config.description.IsSome then _ariaDescribedby descriptionId
            for attribute in ComponentHtml.safeAttributes [ "id"; "class"; "open"; "aria-labelledby"; "aria-describedby"; "aria-modal"; "data-on:close"; "data-on:click" ] config.attributes do attribute
            _dataOn ("close", NativeOverlay.restoreFocusExpression config.id)
            if config.dismissOnBackdrop then
                _dataOn ("click", NativeOverlay.dismissOnBackdropExpression config.id)
            _class "m-auto w-[min(32rem,calc(100%-2rem))] rounded-[var(--fve-radius-panel)] border-0 bg-[var(--fve-surface)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-[var(--fve-overlay-backdrop)]"
            div {
                _class "p-6"
                h2 { _id titleId; _class "text-lg font-semibold"; config.title }
                match config.description with
                | Some description -> p { _id descriptionId; _class "mt-2 text-sm text-[var(--fve-muted-text)]"; description }
                | None -> ()
                div { _class "mt-4"; config.body }
                match config.footer with
                | Some footer -> div { _class "mt-6 flex justify-end gap-3"; footer }
                | None -> ()
            }
        }
