namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>popover</category>
[<RequireQualifiedAccess>]
type PopoverSide =
    | Top
    | Bottom
    | Start
    | End

/// <category>popover</category>
[<RequireQualifiedAccess>]
type PopoverAlignment =
    | Start
    | Center
    | End

/// <category>popover</category>
[<AutoOpen>]
module PopoverAttributes =
    type Html with
        static member _side(value: PopoverSide) =
            Html._attr (
                "side",
                match value with
                | PopoverSide.Top -> "top"
                | PopoverSide.Bottom -> "bottom"
                | PopoverSide.Start -> "start"
                | PopoverSide.End -> "end"
            )

        static member _alignment(value: PopoverAlignment) =
            Html._attr (
                "alignment",
                match value with
                | PopoverAlignment.Start -> "start"
                | PopoverAlignment.Center -> "center"
                | PopoverAlignment.End -> "end"
            )

        static member _focusOnOpen(value: bool) =
            Html._attr ("focus-on-open", if value then "true" else "false")

type internal PopoverPartElement(kind: string, attributes: HtmlAttribute list, children: HtmlElement list) =
    inherit HtmlElement()
    member _.Kind = kind
    member _.Attributes = attributes
    member _.Children = children

    override _.Render _ =
        invalidOp "Popover parts belong inside fvePopover."

module internal Popover =
    let part kind attributes children =
        PopoverPartElement(kind, ComponentElement.attributes attributes, children) :> HtmlElement

    let private eventAttribute event expression attributes =
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

    let render attributes (children: HtmlElement list) =
        let id = ComponentElement.required "id" attributes |> TextField.stableId

        let parts =
            children
            |> List.choose (function
                | :? PopoverPartElement as part -> Some part
                | :? NoopElement -> None
                | _ -> invalidArg "children" "Use fvePopoverTrigger and fvePopoverContent.")

        let owningPart kind =
            match parts |> List.filter (fun part -> part.Kind = kind) with
            | [ part ] -> part
            | _ -> invalidArg "children" $"Popover requires exactly one {kind} part."

        let trigger = owningPart "trigger"

        let control, controlAttributes =
            ComponentTrigger.control "fvePopoverTrigger" trigger.Attributes trigger.Children

        let content = owningPart "content"

        let panelId =
            ComponentElement.value "id" content.Attributes
            |> Option.defaultValue (id + "-content")
            |> TextField.stableId

        let triggerId =
            ComponentElement.value "id" controlAttributes
            |> Option.defaultValue (id + "-trigger")
            |> TextField.stableId

        let anchor = "--fve-popover-" + ComponentHtml.optionToken id

        let side =
            ComponentElement.choice "side" "bottom" [ "top"; "bottom"; "start"; "end" ] attributes

        let alignment =
            ComponentElement.choice "alignment" "start" [ "start"; "center"; "end" ] attributes

        let focusContent =
            ComponentElement.choice "focus-on-open" "false" [ "true"; "false" ] attributes = "true"

        let readOnly = ComponentTrigger.unavailable controlAttributes
        let openSignal = "_fvePopover" + ComponentHtml.signalToken id

        let position =
            match side, alignment with
            | "top", "start" -> "block-start span-inline-end"
            | "top", "center" -> "block-start"
            | "top", _ -> "block-start span-inline-start"
            | "bottom", "start" -> "block-end span-inline-end"
            | "bottom", "center" -> "block-end"
            | "bottom", _ -> "block-end span-inline-start"
            | "start", "start" -> "inline-start span-block-end"
            | "start", "center" -> "inline-start"
            | "start", _ -> "inline-start span-block-start"
            | _, "start" -> "inline-end span-block-end"
            | _, "center" -> "inline-end"
            | _ -> "inline-end span-block-start"

        let triggerDefaults =
            [ yield _id triggerId
              yield _type "button"
              yield _ariaHaspopup "dialog"
              yield _ariaControls panelId
              yield _attr ("popovertarget", panelId)
              yield _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
              if readOnly then
                  yield _ariaDisabled true
              yield
                  ComponentTrigger.event
                      "click"
                      (if readOnly then "evt.preventDefault()" else "void 0")
                      controlAttributes ]

        let triggerAttributes =
            controlAttributes
            |> ComponentTrigger.withoutEvents [ "click" ]
            |> ComponentHtml.safeAttributes
                [ "id"
                  "type"
                  "role"
                  "aria-controls"
                  "aria-haspopup"
                  "popovertarget"
                  "readonly"
                  "data-attr:aria-expanded" ]

        let contentAttributes =
            [ for name in [ "aria-label"; "aria-labelledby" ] do
                  match ComponentElement.attribute name attributes with
                  | Some attribute -> yield attribute
                  | None -> () ]
            @ content.Attributes
            |> ComponentElement.attributes

        let named =
            [ "aria-label"; "aria-labelledby" ]
            |> List.exists (fun name ->
                ComponentElement.value name contentAttributes
                |> Option.exists (String.IsNullOrWhiteSpace >> not))

        let restore =
            $"document.getElementById({ComponentHtml.javascriptString triggerId})?.focus()"

        let opened =
            if focusContent then
                "if (evt.newState == 'open') queueMicrotask(() => { if (el.isConnected && el.matches(':popover-open')) el.querySelector('input:not(:disabled),select:not(:disabled),textarea:not(:disabled),button:not(:disabled),a[href],[tabindex=\"0\"]')?.focus() }); "
            else
                ""

        let toggle =
            $"${openSignal} = evt.newState == 'open'; "
            + opened
            + $"if (evt.newState == 'closed' && el.dataset.fveEscapeDismissed == 'true') {{ delete el.dataset.fveEscapeDismissed; {restore} }}"

        let contentDefaults =
            [ yield _id panelId
              yield _role "dialog"
              if not named then
                  yield _ariaLabelledby triggerId
              yield _attr ("popover", "auto")
              yield
                  _style
                      $"inset:auto;margin:0.5rem 0;position-anchor:{anchor};position-area:{position};position-try-fallbacks:flip-block,flip-inline,flip-block flip-inline"
              yield
                  eventAttribute
                      "keydown"
                      "if (evt.key == 'Escape' && !evt.defaultPrevented) el.dataset.fveEscapeDismissed = 'true'"
                      contentAttributes
              yield eventAttribute "toggle" toggle contentAttributes
              // Closed visibility must beat caller display utilities without replacing the open layout.
              yield
                  _class
                      "fixed max-h-[min(32rem,calc(100dvh-2rem))] w-[min(24rem,calc(100vw-2rem))] overflow-auto rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 text-[var(--fve-text)] shadow-xl [&:not(:popover-open)]:hidden" ]

        ComponentElement.render
            "span"
            [ _id id
              _class "inline-flex"
              _style $"anchor-name:{anchor}"
              _dataSignals $"{{{openSignal}: false}}" ]
            (ComponentHtml.safeAttributes
                [ "side"; "alignment"; "focus-on-open"; "aria-label"; "aria-labelledby" ]
                attributes)
            [ ComponentTrigger.render control (triggerAttributes @ triggerDefaults)
              ComponentElement.render
                  "div"
                  contentDefaults
                  (ComponentHtml.safeAttributes
                      [ "id"; "role"; "popover"; "data-on:keydown"; "data-on:toggle" ]
                      contentAttributes)
                  content.Children ]

/// <category>popover</category>
[<AutoOpen>]
module PopoverDsl =
    let fvePopover = ComponentElementBuilder(Popover.render)
    let fvePopoverTrigger = ComponentTriggerBuilder(Popover.part "trigger")
    let fvePopoverContent = ComponentElementBuilder(Popover.part "content")
