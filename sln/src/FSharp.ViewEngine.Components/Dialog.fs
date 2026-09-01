namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

module internal NativeOverlay =
    let requireText argumentName message value =
        if String.IsNullOrWhiteSpace value then invalidArg argumentName message

    let trigger dialogId initialFocusId label =
        let dialogIdExpression = ComponentHtml.javascriptString dialogId
        let focusExpression =
            initialFocusId
            |> Option.map (fun id ->
                let focusIdExpression = ComponentHtml.javascriptString id
                $"; queueMicrotask(() => document.getElementById({focusIdExpression})?.focus())")
            |> Option.defaultValue ""
        Button.create label
        |> Button.withAttributes [
            _id $"{dialogId}-trigger"
            _ariaHaspopup "dialog"
            _ariaControls dialogId
            _dataOn ("click", $"document.getElementById({dialogIdExpression}).showModal(){focusExpression}") ]
        |> Button.render

    let closeExpression dialogId =
        let dialogIdExpression = ComponentHtml.javascriptString dialogId
        $"document.getElementById({dialogIdExpression}).close()"

    let closeButton dialogId buttonId label disabled attributes =
        Button.create label
        |> (if disabled then Button.disabled else id)
        |> Button.withAttributes (_id buttonId :: _dataOn ("click", closeExpression dialogId) :: attributes)
        |> Button.render

    let restoreFocusExpression dialogId =
        let triggerIdExpression = ComponentHtml.javascriptString $"{dialogId}-trigger"
        $"document.getElementById({triggerIdExpression})?.focus()"

    let dismissOnBackdropExpression dialogId =
        $"evt.target == evt.currentTarget && {closeExpression dialogId}"

[<NoEquality; NoComparison>]
type DialogConfig =
    private
        { id:string
          title:string
          body:HtmlElement
          description:string option
          footer:HtmlElement option
          initialFocusId:string option
          dismissOnBackdrop:bool }

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
          dismissOnBackdrop = false }

    let withDescription description (config:DialogConfig) =
        NativeOverlay.requireText (nameof description) "A dialog description cannot be empty." description
        { config with description = Some description }

    let withFooter footer (config:DialogConfig) = { config with footer = Some footer }

    let withInitialFocus initialFocusId (config:DialogConfig) =
        NativeOverlay.requireText (nameof initialFocusId) "A dialog initial-focus ID cannot be empty." initialFocusId
        { config with initialFocusId = Some initialFocusId }

    let dismissOnBackdrop (config:DialogConfig) = { config with dismissOnBackdrop = true }

    let trigger label (config:DialogConfig) =
        NativeOverlay.trigger config.id config.initialFocusId label

    let closeButton label (config:DialogConfig) =
        NativeOverlay.closeButton config.id $"{config.id}-close" label false []

    let render config =
        let titleId = $"{config.id}-title"
        let descriptionId = $"{config.id}-description"
        dialog {
            _id config.id
            _ariaLabelledby titleId
            _ariaModal true
            if config.description.IsSome then _ariaDescribedby descriptionId
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

[<NoEquality; NoComparison>]
type ConfirmationDialogConfig =
    private
        { id:string
          title:string
          message:string
          cancelLabel:string
          confirmLabel:string
          confirmExpression:string
          validation:string option
          isPending:bool }

[<RequireQualifiedAccess>]
module ConfirmationDialog =
    let create id title message cancelLabel confirmLabel confirmExpression =
        NativeOverlay.requireText (nameof id) "A stable confirmation-dialog ID is required." id
        NativeOverlay.requireText (nameof title) "A confirmation-dialog title is required." title
        NativeOverlay.requireText (nameof message) "A confirmation message is required." message
        NativeOverlay.requireText (nameof cancelLabel) "A cancellation label is required." cancelLabel
        NativeOverlay.requireText (nameof confirmLabel) "A confirmation label is required." confirmLabel
        NativeOverlay.requireText (nameof confirmExpression) "A trusted confirmation expression is required." confirmExpression
        { id = id
          title = title
          message = message
          cancelLabel = cancelLabel
          confirmLabel = confirmLabel
          confirmExpression = confirmExpression
          validation = None
          isPending = false }

    let withValidation validation (config:ConfirmationDialogConfig) =
        NativeOverlay.requireText (nameof validation) "A confirmation validation message cannot be empty." validation
        { config with validation = Some validation }

    let pending (config:ConfirmationDialogConfig) = { config with isPending = true }

    let trigger label (config:ConfirmationDialogConfig) =
        NativeOverlay.trigger config.id (Some $"{config.id}-cancel") label

    let renderContent (config:ConfirmationDialogConfig) =
        let messageId = $"{config.id}-message"
        let validationId = $"{config.id}-validation"
        let pendingSignal = $"_{ComponentHtml.signalToken config.id}_pending"
        let dynamicPending = $"${pendingSignal}"
        let pendingVisible = if config.isPending then "true" else dynamicPending
        let cancelAttributes = [ _dataAttr ("disabled", dynamicPending) ]
        let confirmAttributes = [
            _id $"{config.id}-confirm"
            _dataAttr ("disabled", dynamicPending)
            _dataAttr ("aria-busy", $"{dynamicPending} ? 'true' : null") ]
        form {
            _id $"{config.id}-content"
            _dataIndicator pendingSignal
            _dataOn ("submit", config.confirmExpression)
            _ariaBusy config.isPending
            _dataAttr ("aria-busy", $"{dynamicPending} ? 'true' : null")
            p { _id messageId; _class "text-sm text-[var(--fve-muted-text)]"; config.message }
            match config.validation with
            | Some validation ->
                p {
                    _id validationId
                    _role "alert"
                    _class "mt-4 rounded-[var(--fve-radius-control)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)] ring-1 ring-inset ring-[var(--fve-critical-ring)]"
                    validation
                }
            | None ->
                p { _id validationId; _ariaHidden true; _class "hidden" }
            p {
                _role "status"
                _dataShow pendingVisible
                if config.isPending |> not then _style "display:none"
                _class "mt-4 flex items-center gap-2 text-sm text-[var(--fve-muted-text)]"
                ComponentHtml.loadingGlyph ControlSize.Small
                "Confirmation in progress."
            }
            div {
                _class "mt-6 flex flex-wrap justify-end gap-3"
                NativeOverlay.closeButton config.id $"{config.id}-cancel" config.cancelLabel config.isPending cancelAttributes
                Button.create config.confirmLabel
                |> Button.withVariant ButtonVariant.Destructive
                |> Button.asSubmit
                |> (if config.isPending then Button.pending else id)
                |> Button.withAttributes confirmAttributes
                |> Button.render
            }
        }

    let render (config:ConfirmationDialogConfig) =
        let titleId = $"{config.id}-title"
        let pendingSignal = $"_{ComponentHtml.signalToken config.id}_pending"
        let dynamicPending = $"${pendingSignal}"
        let cancelPending = if config.isPending then "true" else dynamicPending
        dialog {
            _id config.id
            _role "alertdialog"
            _ariaLabelledby titleId
            _ariaDescribedby $"{config.id}-message {config.id}-validation"
            _ariaModal true
            _dataSignals $"{{{pendingSignal}: false}}"
            _dataOn ("cancel", $"({cancelPending}) && evt.preventDefault()")
            _dataOn ("close", NativeOverlay.restoreFocusExpression config.id)
            _class "m-auto w-[min(28rem,calc(100%-2rem))] rounded-[var(--fve-radius-panel)] border-0 bg-[var(--fve-surface)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-[var(--fve-overlay-backdrop)]"
            div {
                _class "p-6"
                h2 { _id titleId; _class "text-lg font-semibold"; config.title }
                div { _class "mt-3"; renderContent config }
            }
        }

[<RequireQualifiedAccess>]
type DrawerSide =
    | Start
    | End

[<NoEquality; NoComparison>]
type DrawerConfig =
    private
        { id:string
          title:string
          body:HtmlElement
          description:string option
          footer:HtmlElement option
          initialFocusId:string
          side:DrawerSide }

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
          side = DrawerSide.End }

    let withDescription description (config:DrawerConfig) =
        NativeOverlay.requireText (nameof description) "A drawer description cannot be empty." description
        { config with description = Some description }

    let withFooter footer (config:DrawerConfig) = { config with footer = Some footer }

    let withInitialFocus initialFocusId (config:DrawerConfig) =
        NativeOverlay.requireText (nameof initialFocusId) "A drawer initial-focus ID cannot be empty." initialFocusId
        { config with initialFocusId = initialFocusId }

    let withSide side (config:DrawerConfig) = { config with side = side }

    let trigger label (config:DrawerConfig) =
        NativeOverlay.trigger config.id (Some config.initialFocusId) label

    let private closeButton label (config:DrawerConfig) =
        NativeOverlay.closeButton config.id $"{config.id}-close" label false []

    let render (config:DrawerConfig) =
        let titleId = $"{config.id}-title"
        let descriptionId = $"{config.id}-description"
        let sideClasses =
            match config.side with
            | DrawerSide.Start -> "left-0 ml-0 mr-auto border-r"
            | DrawerSide.End -> "right-0 ml-auto mr-0 border-l"
        dialog {
            _id config.id
            _ariaLabelledby titleId
            _ariaModal true
            if config.description.IsSome then _ariaDescribedby descriptionId
            _dataOn ("click", NativeOverlay.dismissOnBackdropExpression config.id)
            _dataOn ("close", NativeOverlay.restoreFocusExpression config.id)
            _class (ComponentHtml.classes [
                "fixed inset-y-0 h-dvh max-h-none w-[min(24rem,calc(100%-3rem))] rounded-none border-y-0 border-[var(--fve-border)] bg-[var(--fve-surface)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-[var(--fve-overlay-backdrop)] sm:w-96"
                sideClasses ])
            div {
                _class "flex h-full flex-col"
                div {
                    _class "flex shrink-0 items-start justify-between gap-4 border-b border-[var(--fve-border)] p-5"
                    div {
                        h2 { _id titleId; _class "text-lg font-semibold"; config.title }
                        match config.description with
                        | Some description -> p { _id descriptionId; _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                        | None -> ()
                    }
                    closeButton "Close" config
                }
                div { _class "min-h-0 flex-1 overflow-y-auto p-5"; config.body }
                match config.footer with
                | Some footer -> div { _class "flex shrink-0 justify-end gap-3 border-t border-[var(--fve-border)] p-5"; footer }
                | None -> ()
            }
        }
