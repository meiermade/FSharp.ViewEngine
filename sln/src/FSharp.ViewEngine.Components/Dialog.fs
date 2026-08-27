namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<NoEquality; NoComparison>]
type DialogConfig =
    private
        { id:string
          title:string
          body:HtmlElement
          description:string option
          footer:HtmlElement option
          initialFocusId:string option }

[<RequireQualifiedAccess>]
module Dialog =
    let create id title body =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable dialog ID is required."
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A dialog title is required."
        { id = id; title = title; body = body; description = None; footer = None; initialFocusId = None }

    let withDescription description (config:DialogConfig) = { config with description = Some description }
    let withFooter footer (config:DialogConfig) = { config with footer = Some footer }
    let withInitialFocus initialFocusId (config:DialogConfig) = { config with initialFocusId = Some initialFocusId }

    let trigger label (config:DialogConfig) =
        let dialogId = ComponentHtml.javascriptString config.id
        let openExpression =
            match config.initialFocusId with
            | Some initialFocusId ->
                let focusId = ComponentHtml.javascriptString initialFocusId
                $"document.getElementById({dialogId}).showModal(); queueMicrotask(() => document.getElementById({focusId}).focus())"
            | None -> $"document.getElementById({dialogId}).showModal()"
        Button.create label
        |> Button.withAttributes [ _id $"{config.id}-trigger"; _ariaHaspopup "dialog"; _ariaControls config.id; _dataOn ("click", openExpression) ]
        |> Button.render

    let closeButton label (config:DialogConfig) =
        let dialogId = ComponentHtml.javascriptString config.id
        Button.create label
        |> Button.withAttributes [ _id $"{config.id}-close"; _dataOn ("click", $"document.getElementById({dialogId}).close()") ]
        |> Button.render

    let render config =
        let titleId = $"{config.id}-title"
        let descriptionId = $"{config.id}-description"
        let triggerId = ComponentHtml.javascriptString $"{config.id}-trigger"
        dialog {
            _id config.id
            _ariaLabelledby titleId
            if config.description.IsSome then _ariaDescribedby descriptionId
            _dataOn ("close", $"document.getElementById({triggerId}).focus()")
            _class "m-auto w-[min(32rem,calc(100%-2rem))] rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-slate-950/50"
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
