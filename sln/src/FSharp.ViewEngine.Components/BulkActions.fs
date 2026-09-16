namespace FSharp.ViewEngine.Components.Application

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open type Html
open type Datastar

/// An action which operates on the current page's selected table records.
[<NoEquality; NoComparison>]
type BulkAction =
    private
        { label:string
          successMessage:string
          command:string -> string
          variant:ButtonVariant }

[<RequireQualifiedAccess>]
module BulkAction =
    /// `command` receives the Datastar expression containing the selected stable row keys.
    /// `successMessage` is displayed after the caller-owned command completes synchronously.
    let create label successMessage command =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A bulk action label is required."
        if String.IsNullOrWhiteSpace successMessage then invalidArg (nameof successMessage) "A success message is required."
        { label = label; successMessage = successMessage; command = command; variant = ButtonVariant.Secondary }

    let destructive (action:BulkAction) = { action with variant = ButtonVariant.Destructive }
    let primary (action:BulkAction) = { action with variant = ButtonVariant.Primary }

[<NoEquality; NoComparison>]
type BulkActionsConfig =
    private
        { id:string
          tableSelectionId:string
          label:string
          actions:BulkAction list
          content:HtmlElement }

/// Page-scoped bulk action presentation. Table remains the source of truth for selection.
[<RequireQualifiedAccess>]
module BulkActions =
    let create id tableSelectionId label actions content =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then
            invalidArg (nameof id) "A stable bulk action ID without whitespace is required."
        if String.IsNullOrWhiteSpace tableSelectionId then
            invalidArg (nameof tableSelectionId) "A table selection ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A bulk action label is required."
        if List.isEmpty actions then invalidArg (nameof actions) "At least one bulk action is required."
        { id = id; tableSelectionId = tableSelectionId; label = label; actions = actions; content = content }

    let render config =
        let signal = "$" + ComponentHtml.signalToken (config.id + "-selected")
        let result = "$" + ComponentHtml.signalToken (config.id + "-result")
        let signalName = signal.TrimStart '$'
        div {
            _dataSignals $"{{{signalName}: [], {result.TrimStart '$'}: ''}}"
            _dataOn ("fve-table-selection-change", $"{signal} = evt.detail.keys")
            _dataOn ("fve-selection-change", $"{signal} = evt.detail.keys")
            _dataOn ("fve-selection-clear", $"{signal} = []")
            config.content
            section {
                _id config.id
                _role "region"
                _ariaLabel config.label
                _class "mt-3 flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] px-3 py-2 text-sm text-[var(--fve-text)] ring-1 ring-[var(--fve-border)]"
                _dataShow $"{signal}.length > 0"
                _style "display:none"
                div {
                    _class "flex items-center gap-2"
                    strong { _dataText $"{signal}.length + ' selected'"; "0 selected" }
                    button {
                        _type "button"
                        _class "rounded-[var(--fve-radius-control)] px-2 py-1 text-sm text-[var(--fve-brand-text)] underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                        _dataOn ("click", $"document.getElementById({ComponentHtml.javascriptString config.tableSelectionId})?.dispatchEvent(new CustomEvent('fve-selection-clear'))")
                        "Clear selection"
                    }
                }
                div {
                    _class "flex flex-wrap items-center gap-2"
                    for action in config.actions do
                        Button.create action.label
                        |> Button.withVariant action.variant
                        |> Button.withAttributes [ _dataOn ("click", $"{result} = ''; {action.command signal}; {result} = {ComponentHtml.javascriptString action.successMessage}") ]
                        |> Button.render
                }
            }
            output {
                _role "status"
                _ariaLive "polite"
                _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                _dataShow $"{result} != ''"
                _dataText result
                _style "display:none"
            }
        }
