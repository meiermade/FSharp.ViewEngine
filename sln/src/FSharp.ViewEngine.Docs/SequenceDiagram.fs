namespace FSharp.ViewEngine.Docs

open System
open System.Text
open System.Text.RegularExpressions

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
