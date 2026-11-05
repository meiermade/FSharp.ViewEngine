namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

module internal ErrorSummary =
    let item attributes children =
        let href = ComponentElement.required "href" attributes
        if not (href.StartsWith('#')) || href.Length = 1 then invalidArg "attributes" "An error link requires a field fragment destination."
        let fieldId = Uri.UnescapeDataString(href.Substring 1)
        li {
            ComponentElement.render "a"
                [_class "underline underline-offset-2 focus-visible:outline-2 focus-visible:outline-offset-2"; _dataOn ("click", $"document.getElementById({ComponentHtml.javascriptString fieldId})?.focus()")]
                attributes children
        }

    let render attributes (children:HtmlElement list) =
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "attributes" "A stable error summary ID without whitespace is required."
        let title = ComponentElement.required "label" attributes
        if children |> List.forall (function :? NoopElement -> true | _ -> false) then empty
        else
            let defaults = [
                _role "alert"; _tabindex -1; _ariaLabelledby (id + "-title")
                _class "[overflow-wrap:anywhere] rounded-[var(--fve-radius-control)] border border-[var(--fve-critical-ring)] bg-[var(--fve-critical-subtle)] p-4 text-sm text-[var(--fve-critical-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-critical-ring)]"
                if (ComponentElement.attribute "autofocus" attributes).IsSome then _dataInit "el.focus()" ]
            ComponentElement.render "div" defaults (ComponentHtml.safeAttributes ["label"] attributes) [
                p { _id (id + "-title"); _class "font-semibold"; title }
                ul { _class "mt-2 list-disc space-y-1 pl-5"; children } ]

/// <category>error-summary</category>
[<AutoOpen>]
module ErrorSummaryDsl =
    /// Compose real field links; autofocus is opt-in for a newly inserted summary.
    let fveErrorSummary = ComponentElementBuilder(ErrorSummary.render)
    let fveErrorSummaryItem = ComponentElementBuilder(ErrorSummary.item)
