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

    let containedTrigger dialogId initialFocusId label =
        let id = ComponentHtml.javascriptString dialogId
        let focus = ComponentHtml.javascriptString initialFocusId
        Button.create (ButtonContent.Text label)
        |> Button.withAttributes [ _id $"{dialogId}-trigger"; _ariaHaspopup "dialog"; _ariaControls dialogId
                                   _dataOn("click", $"document.getElementById({id}).show(); document.getElementById({focus})?.focus()") ]
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

    // Non-document-modal overlays leave surrounding host chrome accessible. Only the
    // named sibling background is inert; the host supplies a positioned viewport.
    let contained dialogId backgroundId dismissOnBackdrop (content:HtmlElement) =
        let id = ComponentHtml.javascriptString dialogId
        let background = ComponentHtml.javascriptString backgroundId
        let cancel = $"const dialog = document.getElementById({id}); if (dialog?.open && dialog.dispatchEvent(new Event('cancel', {{cancelable:true}}))) dialog.close()"
        div {
            _data("fve-overlay-background", backgroundId)
            _class "absolute inset-0 z-40 [&:not(:has(>dialog[open]))]:hidden"
            _dataInit ($"el._fveOverlayObserver?.disconnect(); const sync = () => {{ const background = document.getElementById({background}); const active = [...document.querySelectorAll('[data-fve-overlay-background]')].some(layer => layer.dataset.fveOverlayBackground === {background} && layer.querySelector('dialog[open]')); if (background) background.inert = active; if (!el.isConnected) observer.disconnect() }}; const observer = new MutationObserver(sync); el._fveOverlayObserver = observer; observer.observe(document.body, {{childList:true, subtree:true, attributes:true, attributeFilter:['open']}}); sync()")
            _dataOn("keydown__window", $"if (evt.key === 'Escape' && !evt.defaultPrevented && !document.querySelector(':popover-open, dialog:modal') && el.querySelector('dialog[open]')) {{ evt.preventDefault(); {cancel} }}")
            div {
                _ariaHidden true; _data("fve-overlay-backdrop", "true")
                _class "absolute inset-0 bg-[var(--fve-overlay-backdrop)]"
                if dismissOnBackdrop then _dataOn("click", cancel)
            }
            content
        }
