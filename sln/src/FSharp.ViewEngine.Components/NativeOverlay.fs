namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>dialog</category>
[<AutoOpen>]
module OverlayAttributes =
    type Html with
        static member _initialFocus(value: string) = Html._attr ("initial-focus", value)
        static member _container(value: string) = Html._attr ("container", value)
        static member _dismissOnBackdrop(value: bool) = Html._attr ("dismiss-on-backdrop", if value then "true" else "false")

module internal NativeOverlay =
    let requireText argumentName message value =
        if String.IsNullOrWhiteSpace value then invalidArg argumentName message

    let triggerAttributes dialogId initialFocusId =
        [ _id (dialogId + "-trigger")
          _ariaHaspopup "dialog"
          _ariaControls dialogId
          _attr ("data-fve-overlay-target", dialogId)
          _attr ("data-fve-overlay-action", "open")
          match initialFocusId with Some id -> _attr ("data-fve-overlay-focus", id) | None -> () ]

    let trigger dialogId initialFocusId (label: string) =
        fveButton {
            for attribute in triggerAttributes dialogId initialFocusId do attribute
            label
        }

    let containedTrigger dialogId initialFocusId (label: string) =
        trigger dialogId (Some initialFocusId) label

    let closeButton dialogId buttonId (label: string) =
        fveButton {
            _id buttonId
            _attr ("data-fve-overlay-target", dialogId)
            _attr ("data-fve-overlay-action", "close")
            label
        }

    let renderTrigger authored (children: HtmlElement list) =
        let control, attributes = ComponentTrigger.control "Overlay trigger" authored children
        let target = ComponentElement.required "aria-controls" attributes
        let id = ComponentElement.value "id" attributes |> Option.defaultValue (target + "-trigger")
        ComponentTrigger.render control
            [ yield! ComponentHtml.safeAttributes [ "id"; "type"; "role"; "aria-haspopup"; "readonly" ] attributes
              yield _id id
              yield _type "button"
              yield _ariaHaspopup "dialog"
              yield _attr ("data-fve-overlay-target", target)
              yield _attr ("data-fve-overlay-action", "open")
              if ComponentTrigger.unavailable attributes then yield _ariaDisabled true ]

    let renderClose authored (children: HtmlElement list) =
        let attributes = ComponentElement.attributes authored
        let target = ComponentElement.required "aria-controls" attributes
        fveButton {
            _attr ("data-fve-overlay-target", target)
            _attr ("data-fve-overlay-action", "close")
            for attribute in attributes do attribute
            for child in children do child
        }

    // The host supplies a positioned viewport; only its named background is inert.
    let contained _dialogId backgroundId _dismissOnBackdrop (content: HtmlElement) =
        div {
            _attr ("data-fve-overlay-background", backgroundId)
            _class "absolute inset-0 z-40 [&:not(:has(>dialog[open]))]:hidden"
            div {
                _ariaHidden true
                _attr ("data-fve-overlay-backdrop", "true")
                _class "absolute inset-0 bg-[var(--fve-overlay-backdrop)]"
            }
            content
        }

    let enhance tag id dismiss content =
        ComponentElement.render tag
            [ _id (id + "-host"); _class "contents"
              if dismiss then _attr ("dismiss-on-backdrop", "") ] [] [content]
