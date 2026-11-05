namespace Example.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

module internal Dialog =
    let private optional name attributes =
        ComponentElement.value name attributes
        |> Option.map (fun value ->
            NativeOverlay.requireText name $"A supplied {name} cannot be empty." value
            value)

    let footer attributes children =
        ComponentPart(
            "Dialog",
            "footer",
            ComponentElement.render "div" [ _class "mt-6 flex justify-end gap-3" ] attributes children
        )
        :> HtmlElement

    let render attributes children =
        let id = ComponentElement.required "id" attributes

        if id |> Seq.exists Char.IsWhiteSpace then
            invalidArg "id" "A stable dialog ID without whitespace is required."

        let title = ComponentElement.required "label" attributes
        let description = optional "description" attributes
        let focus = optional "initial-focus" attributes
        let container = optional "container" attributes

        let isOpen =
            match ComponentElement.attribute "open" attributes with
            | Some { Value = ValueNone } -> true
            | Some { Value = ValueSome value } -> value <> "false"
            | None -> false

        if isOpen && container.IsNone then
            invalidArg
                "open"
                "An initially open dialog requires a container; use fveDialogTrigger for a document-modal dialog."

        let dismiss =
            ComponentElement.choice "dismiss-on-backdrop" "false" [ "true"; "false" ] attributes = "true"

        let parts, body = ComponentParts.split "Dialog" children
        let titleId = id + "-title"
        let descriptionId = id + "-description"

        let describedby =
            [ if description.IsSome then
                  yield descriptionId
              match ComponentElement.value "aria-describedby" attributes with
              | Some value -> yield value
              | None -> () ]
            |> String.concat " "

        let defaults =
            [ yield _id id
              yield _ariaLabelledby titleId
              yield _ariaModal container.IsNone
              if container.IsSome then
                  yield _open isOpen
                  yield _data ("fve-contained", "true")
              if describedby <> "" then
                  yield _ariaDescribedby describedby
              match focus with
              | Some focus -> yield _data ("fve-initial-focus", focus)
              | None -> ()
              yield NativeOverlay.eventAttribute "close" (NativeOverlay.restoreFocusExpression id) attributes
              if dismiss then
                  yield NativeOverlay.eventAttribute "click" (NativeOverlay.dismissOnBackdropExpression id) attributes
              yield
                  _class (
                      (if container.IsSome then
                           "absolute inset-0 max-h-[calc(100%-2rem)] overflow-auto "
                       else
                           "")
                      + "m-auto w-[min(32rem,calc(100%-2rem))] rounded-[var(--fve-radius-panel)] border-0 bg-[var(--fve-background)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-[var(--fve-overlay-backdrop)]"
                  ) ]

        let content =
            ComponentElement.render
                "dialog"
                defaults
                (ComponentHtml.safeAttributes
                    [ "id"
                      "label"
                      "description"
                      "initial-focus"
                      "container"
                      "dismiss-on-backdrop"
                      "open"
                      "aria-labelledby"
                      "aria-describedby"
                      "aria-modal"
                      "data-on:close"
                      if dismiss then
                          "data-on:click" ]
                    attributes)
                [ div {
                      _class "p-6"

                      h2 {
                          _id titleId
                          _class "text-lg font-semibold"
                          title
                      }

                      match description with
                      | Some description ->
                          p {
                              _id descriptionId
                              _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                              description
                          }
                      | None -> ()

                      div {
                          _class "mt-4"
                          body
                      }

                      match Map.tryFind "footer" parts with
                      | Some footer -> footer
                      | None -> ()
                  } ]

        match container with
        | Some background -> NativeOverlay.contained id background dismiss content
        | None -> content

/// <category>dialog</category>
[<AutoOpen>]
module DialogDsl =
    let fveDialog = ComponentElementBuilder(Dialog.render)
    let fveDialogFooter = ComponentElementBuilder(Dialog.footer)
    let fveDialogTrigger = ComponentTriggerBuilder(NativeOverlay.renderTrigger)
    let fveDialogClose = ComponentElementBuilder(NativeOverlay.renderClose)
