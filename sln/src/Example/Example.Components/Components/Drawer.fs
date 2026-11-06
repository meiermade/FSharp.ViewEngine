namespace Example.Components

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
[<AutoOpen>]
module DrawerAttributes =
    type Html with
        static member _side(value: DrawerSide) =
            Html._attr (
                "side",
                match value with
                | DrawerSide.Start -> "start"
                | DrawerSide.End -> "end"
                | DrawerSide.Top -> "top"
                | DrawerSide.Bottom -> "bottom"
            )

        static member _size(value: DrawerSize) =
            Html._attr ("size", if value = DrawerSize.Large then "large" else "standard")

module internal Drawer =
    let private optional name attributes =
        ComponentElement.value name attributes
        |> Option.map (fun value ->
            NativeOverlay.requireText name $"A supplied {name} cannot be empty." value
            value)

    let footer attributes children =
        ComponentPart(
            "Drawer",
            "footer",
            ComponentElement.render
                "div"
                [ _class "flex shrink-0 flex-wrap justify-end gap-3 border-t border-[var(--fve-border)] p-5" ]
                attributes
                children
        )
        :> HtmlElement

    let render attributes children =
        let id = ComponentElement.required "id" attributes

        if id |> Seq.exists Char.IsWhiteSpace then
            invalidArg "id" "A stable drawer ID without whitespace is required."

        let title = ComponentElement.required "label" attributes
        let description = optional "description" attributes

        let focus =
            optional "initial-focus" attributes |> Option.defaultValue (id + "-close")

        let container = optional "container" attributes

        let isOpen =
            match ComponentElement.attribute "open" attributes with
            | Some { Value = ValueNone } -> true
            | Some { Value = ValueSome value } -> value <> "false"
            | None -> false

        if isOpen && container.IsNone then
            invalidArg
                "open"
                "An initially open drawer requires a container; use fveDrawerTrigger for a document-modal drawer."

        let side =
            ComponentElement.choice "side" "end" [ "start"; "end"; "top"; "bottom" ] attributes

        let size =
            ComponentElement.choice "size" "standard" [ "standard"; "large" ] attributes

        let parts, body = ComponentParts.split "Drawer" children
        let titleId = id + "-title"
        let descriptionId = id + "-description"

        let describedby =
            [ if description.IsSome then
                  yield descriptionId
              match ComponentElement.value "aria-describedby" attributes with
              | Some value -> yield value
              | None -> () ]
            |> String.concat " "

        let sideClasses =
            match side with
            | "start" -> "inset-y-0 left-0 ml-0 mr-auto h-dvh border-r"
            | "end" -> "inset-y-0 right-0 ml-auto mr-0 h-dvh border-l"
            | "top" -> "inset-x-0 top-0 mb-auto mt-0 w-full max-w-none border-b"
            | _ -> "inset-x-0 bottom-0 mb-0 mt-auto w-full max-w-none border-t"

        let sizeClasses =
            match side, size with
            | ("start" | "end"), "standard" -> "w-full max-w-full sm:w-96"
            | ("start" | "end"), _ -> "w-full max-w-full sm:w-[42rem]"
            | _, "standard" -> "h-[min(20rem,calc(100%-3rem))]"
            | _ -> "h-[min(36rem,calc(100%-3rem))]"

        let defaults =
            [ yield _id id
              yield _ariaLabelledby titleId
              yield _ariaModal container.IsNone
              yield _data ("fve-initial-focus", focus)
              if container.IsSome then
                  yield _open isOpen
                  yield _data ("fve-contained", "true")
              if describedby <> "" then
                  yield _ariaDescribedby describedby
              yield
                  _class (
                      ComponentHtml.classes
                          [ (if container.IsSome then
                                 "absolute max-h-full"
                             else
                                 "fixed max-h-none")
                            "rounded-none border-[var(--fve-border)] bg-[var(--fve-background)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-[var(--fve-overlay-backdrop)]"
                            sizeClasses
                            (if container.IsSome then
                                 sideClasses.Replace("h-dvh", "h-full")
                             else
                                 sideClasses) ]
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
                      "size"
                      "side"
                      "open"
                      "aria-labelledby"
                      "aria-describedby"
                      "aria-modal"
 ]
                    attributes)
                [ div {
                      _class "flex h-full flex-col"

                      div {
                          _class
                              "flex shrink-0 items-start justify-between gap-4 border-b border-[var(--fve-border)] p-5"

                          div {
                              _class "min-w-0 flex-1"

                              h2 {
                                  _id titleId
                                  _class "text-lg font-semibold [overflow-wrap:anywhere]"
                                  title
                              }

                              match description with
                              | Some description ->
                                  p {
                                      _id descriptionId
                                      _class "mt-1 text-sm text-[var(--fve-muted-text)]"
                                      description
                                  }
                              | None -> ()
                          }

                          NativeOverlay.closeButton id (id + "-close") "Close"
                      }

                      div {
                          _class "min-h-0 flex-1 overflow-y-auto p-5"
                          body
                      }

                      match Map.tryFind "footer" parts with
                      | Some footer -> footer
                      | None -> ()
                  } ]

        let content = match container with Some background -> NativeOverlay.contained id background true content | None -> content
        NativeOverlay.enhance "fve-drawer" id true content

/// <category>drawer</category>
[<AutoOpen>]
module DrawerDsl =
    let fveDrawer = ComponentElementBuilder(Drawer.render)
    let fveDrawerFooter = ComponentElementBuilder(Drawer.footer)
    let fveDrawerTrigger = ComponentTriggerBuilder(NativeOverlay.renderTrigger)
    let fveDrawerClose = ComponentElementBuilder(NativeOverlay.renderClose)
