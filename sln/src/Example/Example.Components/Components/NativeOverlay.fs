namespace Example.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>dialog</category>
[<AutoOpen>]
module OverlayAttributes =
    type Html with
        static member _initialFocus(value: string) = Html._attr ("initial-focus", value)
        static member _container(value: string) = Html._attr ("container", value)

        static member _dismissOnBackdrop(value: bool) =
            Html._attr ("dismiss-on-backdrop", if value then "true" else "false")

module internal NativeOverlay =
    let requireText argumentName message value =
        if String.IsNullOrWhiteSpace value then
            invalidArg argumentName message

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

    let trigger dialogId initialFocusId (label: string) =
        fveButton {
            for attribute in triggerAttributes dialogId initialFocusId do
                attribute

            label
        }

    let containedTrigger dialogId initialFocusId (label: string) =
        let id = ComponentHtml.javascriptString dialogId
        let focus = ComponentHtml.javascriptString initialFocusId

        fveButton {
            _id $"{dialogId}-trigger"
            _ariaHaspopup "dialog"
            _ariaControls dialogId
            _dataOn ("click", $"document.getElementById({id}).show(); document.getElementById({focus})?.focus()")
            label
        }

    let closeExpression dialogId =
        let dialogIdExpression = ComponentHtml.javascriptString dialogId
        $"document.getElementById({dialogIdExpression}).close()"

    let closeButton dialogId buttonId (label: string) =
        fveButton {
            _id buttonId
            _dataOn ("click", closeExpression dialogId)
            label
        }

    let restoreFocusExpression dialogId =
        let id = ComponentHtml.javascriptString dialogId
        let triggerIdExpression = ComponentHtml.javascriptString $"{dialogId}-trigger"
        $"(() => {{ const dialog = document.getElementById({id}); const opener = dialog?._fveOpener; if (opener?.isConnected) opener.focus(); else document.getElementById({triggerIdExpression})?.focus(); if (dialog) delete dialog._fveOpener }})()"

    let dismissOnBackdropExpression dialogId =
        $"evt.target == evt.currentTarget && {closeExpression dialogId}"

    let eventAttribute event expression attributes =
        let name = "data-on:" + event

        match ComponentElement.attribute name attributes with
        | Some attribute ->
            Html._attr (
                name,
                expression
                + "; "
                + (attribute.Value |> ValueOption.toOption |> Option.defaultValue "void 0")
            )
        | None -> Html._attr (name, expression)

    let renderTrigger authored (children: HtmlElement list) =
        let control, attributes =
            ComponentTrigger.control "Overlay trigger" authored children

        let target = ComponentElement.required "aria-controls" attributes

        let id =
            ComponentElement.value "id" attributes
            |> Option.defaultValue (target + "-trigger")

        let opener = $"document.getElementById({ComponentHtml.javascriptString id})"
        let targetExpression = ComponentHtml.javascriptString target

        let show =
            $"(() => {{ const dialog = document.getElementById({targetExpression}); if (!dialog || dialog.open) return; dialog._fveOpener = {opener}; if (dialog.hasAttribute('data-fve-contained')) dialog.show(); else dialog.showModal(); const focus = dialog.dataset.fveInitialFocus; if (focus) queueMicrotask(() => document.getElementById(focus)?.focus()) }})()"

        ComponentTrigger.render
            control
            [ yield!
                  attributes
                  |> ComponentTrigger.withoutEvents [ "click" ]
                  |> ComponentHtml.safeAttributes [ "id"; "type"; "role"; "aria-haspopup"; "readonly" ]
              yield _id id
              yield _type "button"
              yield _ariaHaspopup "dialog"
              if ComponentTrigger.unavailable attributes then
                  yield _ariaDisabled true
              yield ComponentTrigger.event "click" (ComponentTrigger.guard id show) attributes ]

    let renderClose authored (children: HtmlElement list) =
        let attributes = ComponentElement.attributes authored
        let target = ComponentElement.required "aria-controls" attributes

        fveButton {
            yield eventAttribute "click" (closeExpression target) attributes
            yield! ComponentHtml.safeAttributes [ "data-on:click" ] attributes
            yield! children
        }

    // Non-document-modal overlays leave surrounding host chrome accessible. Only the
    // named sibling background is inert; the host supplies a positioned viewport.
    let contained dialogId backgroundId dismissOnBackdrop (content: HtmlElement) =
        let id = ComponentHtml.javascriptString dialogId
        let background = ComponentHtml.javascriptString backgroundId

        let cancel =
            $"const dialog = document.getElementById({id}); if (dialog?.open && dialog.dispatchEvent(new Event('cancel', {{cancelable:true}}))) dialog.close()"

        div {
            _data ("fve-overlay-background", backgroundId)
            _class "absolute inset-0 z-40 [&:not(:has(>dialog[open]))]:hidden"

            _dataInit (
                $"el._fveOverlayObserver?.disconnect(); const sync = () => {{ const background = document.getElementById({background}); const active = [...document.querySelectorAll('[data-fve-overlay-background]')].some(layer => layer.dataset.fveOverlayBackground === {background} && layer.querySelector('dialog[open]')); if (background) background.inert = active; if (!el.isConnected) observer.disconnect() }}; const observer = new MutationObserver(sync); el._fveOverlayObserver = observer; observer.observe(document.body, {{childList:true, subtree:true, attributes:true, attributeFilter:['open']}}); sync()"
            )

            _dataOn (
                "keydown__window",
                $"if (evt.key === 'Escape' && !evt.defaultPrevented && !document.querySelector(':popover-open, dialog:modal') && el.querySelector('dialog[open]')) {{ evt.preventDefault(); {cancel} }}"
            )

            div {
                _ariaHidden true
                _data ("fve-overlay-backdrop", "true")
                _class "absolute inset-0 bg-[var(--fve-overlay-backdrop)]"

                if dismissOnBackdrop then
                    _dataOn ("click", cancel)
            }

            content
        }
