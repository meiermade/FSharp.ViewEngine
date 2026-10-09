namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>mermaid</category>
module MermaidView =
    let private render (classes:string) (source:string) =
        div {
            _class classes
            _data("init", "window.renderMermaid?.(el)")
            _data("on:datastar-fetch", "if (evt.detail.type === 'datastar-patch-elements') queueMicrotask(() => { if (el.isConnected) window.renderMermaid?.(el, true) })")
            _data("docs-diagram", "true")
            _data("mermaid-source", source)
            _data("mermaid-state", "pending")
            _ariaBusy true
            p {
                _class "m-0 text-center text-sm leading-relaxed text-[var(--fve-muted-text)]"
                _data("mermaid-status", "true")
                _role "status"
                "Rendering diagram…"
            }
        }

    let private classes = "mermaid overflow-x-auto rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] p-5 data-[mermaid-state=pending]:grid data-[mermaid-state=pending]:min-h-32 data-[mermaid-state=pending]:place-items-center data-[mermaid-state=failed]:grid data-[mermaid-state=failed]:min-h-32 data-[mermaid-state=failed]:place-items-center [&>svg]:h-auto [&>svg]:max-w-full"

    let diagram source = render classes source

    let c4Diagram source = render $"{classes} max-sm:[&>svg]:min-w-3xl [&_a]:cursor-pointer [&_a:focus-visible]:outline-2 [&_a:focus-visible]:outline-offset-2 [&_a:focus-visible]:outline-[var(--fve-brand-ring)]" source

/// <category>mermaid</category>
[<NoEquality; NoComparison>]
type MermaidConfig = private { source:string; c4:bool }

/// <category>mermaid</category>
[<RequireQualifiedAccess>]
module Mermaid =
    /// Include once in the document head. The consumer supplies the Mermaid script URL.
    let assets scriptPath = MermaidAssets.render scriptPath

    /// Authorize initialization and the lazy-loaded runtime with the current document's CSP nonce.
    let assetsWithNonce scriptPath nonce = MermaidAssets.renderWithNonce scriptPath nonce

    let create source : MermaidConfig =
        if String.IsNullOrWhiteSpace source then invalidArg (nameof source) "Mermaid source cannot be empty."
        { source = source; c4 = false }

    let withC4 (diagram:MermaidConfig) = { diagram with c4 = true }

    let render (diagram:MermaidConfig) =
        if diagram.c4 then MermaidView.c4Diagram diagram.source
        else MermaidView.diagram diagram.source


namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions

/// <category>mermaid</category>
module SequenceDiagram =
    type Participant =
        private
        | Participant of id:string * label:string

    type Branch =
        private
        | Branch of label:string * steps:Step list

    and Step =
        private
        | Message of sender:Participant * receiver:Participant * text:string * isReply:bool
        | Optional of label:string * steps:Step list
        | Alternatives of Branch list
        | Loop of label:string * steps:Step list

    type Diagram =
        private
        | Diagram of participants:Participant list * steps:Step list

    let private participantId (Participant(id, _)) = id
    let private participantLabel (Participant(_, label)) = label

    let private requireSingleLine name (value:string) =
        if String.IsNullOrWhiteSpace value then
            invalidArg name $"{name} cannot be empty."

        if value.Contains('\n') || value.Contains('\r') then
            invalidArg name $"{name} must be a single line."

    let participant (id:string) (label:string) =
        requireSingleLine (nameof id) id
        requireSingleLine (nameof label) label

        if not (Regex.IsMatch(id, "^[A-Za-z][A-Za-z0-9_]*$")) then
            invalidArg (nameof id) $"Invalid Mermaid participant ID: {id}."

        Participant(id, label)

    let call sender receiver text =
        requireSingleLine (nameof text) text
        Message(sender, receiver, text, false)

    let reply sender receiver text =
        requireSingleLine (nameof text) text
        Message(sender, receiver, text, true)

    let optional label steps =
        requireSingleLine (nameof label) label
        Optional(label, steps)

    let branch label steps =
        requireSingleLine (nameof label) label
        Branch(label, steps)

    let alternatives branches =
        match branches with
        | [] -> invalidArg (nameof branches) "An alternative requires at least one branch."
        | _ -> Alternatives branches

    let loop label steps =
        requireSingleLine (nameof label) label
        Loop(label, steps)

    let private validateReferences participants steps =
        let participantIds = participants |> List.map participantId |> Set.ofList

        let validateParticipant participant =
            let id = participantId participant

            if not (Set.contains id participantIds) then
                invalidArg (nameof steps) $"Sequence step references undeclared participant: {id}."

        let rec validateStep step =
            match step with
            | Message(sender, receiver, _, _) ->
                validateParticipant sender
                validateParticipant receiver
            | Optional(_, nestedSteps)
            | Loop(_, nestedSteps) -> nestedSteps |> List.iter validateStep
            | Alternatives branches ->
                branches
                |> List.iter (fun (Branch(_, nestedSteps)) -> nestedSteps |> List.iter validateStep)

        steps |> List.iter validateStep

    let sequence participants steps =
        let duplicateParticipant =
            participants
            |> List.groupBy participantId
            |> List.tryFind (fun (_, matches) -> List.length matches > 1)

        match duplicateParticipant with
        | Some(id, _) -> invalidArg (nameof participants) $"Duplicate Mermaid participant ID: {id}."
        | None -> ()

        validateReferences participants steps
        Diagram(participants, steps)

    let render (Diagram(participants, steps)) =
        let output = StringBuilder("sequenceDiagram\n    autonumber")

        let appendLine indentation (text:string) =
            output.Append('\n').Append(String(' ', indentation * 4)).Append(text) |> ignore

        for diagramParticipant in participants do
            appendLine 1 $"participant {participantId diagramParticipant} as {participantLabel diagramParticipant}"

        if not (List.isEmpty steps) then
            output.AppendLine().AppendLine() |> ignore

        let rec renderStep indentation step =
            match step with
            | Message(sender, receiver, text, isReply) ->
                let arrow = if isReply then "-->>" else "->>"
                appendLine indentation $"{participantId sender}{arrow}{participantId receiver}: {text}"
            | Optional(label, nestedSteps) ->
                appendLine indentation $"opt {label}"
                nestedSteps |> List.iter (renderStep (indentation + 1))
                appendLine indentation "end"
            | Loop(label, nestedSteps) ->
                appendLine indentation $"loop {label}"
                nestedSteps |> List.iter (renderStep (indentation + 1))
                appendLine indentation "end"
            | Alternatives branches ->
                branches
                |> List.iteri (fun index (Branch(label, nestedSteps)) ->
                    let keyword = if index = 0 then "alt" else "else"
                    appendLine indentation $"{keyword} {label}"
                    nestedSteps |> List.iter (renderStep (indentation + 1)))

                appendLine indentation "end"

        steps |> List.iter (renderStep 1)
        output.ToString()
