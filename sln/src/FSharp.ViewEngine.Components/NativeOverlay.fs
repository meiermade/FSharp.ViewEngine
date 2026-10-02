namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

module internal NativeOverlay =
    let requireText argumentName message value =
        if String.IsNullOrWhiteSpace value then invalidArg argumentName message

    let triggerAttributes dialogId initialFocusId =
        let dialogIdExpression = ComponentHtml.javascriptString dialogId
        let focusExpression =
            initialFocusId
            |> Option.map (fun id ->
                let focusIdExpression = ComponentHtml.javascriptString id
                $"; queueMicrotask(() => document.getElementById({focusIdExpression})?.focus())")
            |> Option.defaultValue ""
        [ _id $"{dialogId}-trigger"
          _ariaHaspopup "dialog"
          _ariaControls dialogId
          _dataOn ("click", $"document.getElementById({dialogIdExpression}).showModal(){focusExpression}") ]

    let trigger dialogId initialFocusId label =
        Button.create (ButtonContent.Text label)
        |> Button.withAttributes (triggerAttributes dialogId initialFocusId)
        |> Button.render

    let closeExpression dialogId =
        let dialogIdExpression = ComponentHtml.javascriptString dialogId
        $"document.getElementById({dialogIdExpression}).close()"

    let closeButton dialogId buttonId label =
        Button.create (ButtonContent.Text label)
        |> Button.withAttributes [ _id buttonId; _dataOn ("click", closeExpression dialogId) ]
        |> Button.render

    let restoreFocusExpression dialogId =
        let triggerIdExpression = ComponentHtml.javascriptString $"{dialogId}-trigger"
        $"document.getElementById({triggerIdExpression})?.focus()"

    let dismissOnBackdropExpression dialogId =
        $"evt.target == evt.currentTarget && {closeExpression dialogId}"
