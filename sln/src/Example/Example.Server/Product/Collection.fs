module Example.Server.Product.Collection

open Microsoft.AspNetCore.Http
open Example.Components.Views.Model
open Ledger.Operations

let delete services context (query: Query) resource keys =
    match deleteSelection { records = query.records; resource = resource; keys = keys } with
    | Ok result ->
        Session.save services context query.workspace resource result
        Submission.result "selection-deleted"
    | Error DeleteSelectionError.HasActivity ->
        { Submission.result "delete-blocked" with selection = keys |> List.distinct }
    | Error _ -> Submission.result "selection-invalid"

let submit services context (query: Query) resource field (form: IFormCollection) =
    let keys = form[field] |> Seq.toList
    match form["action"].ToString().Trim() with
    | "prepare-delete" | "cancel-deletion" as action ->
        match prepareDeletion { records = query.records; resource = resource; keys = keys } with
        | Error _ -> Some(Submission.result "selection-invalid")
        | Ok prepared ->
            let state =
                if action = "cancel-deletion" then "selected"
                elif prepared.blockedIds.IsEmpty then "delete-confirm"
                else "delete-blocked"
            Some { Submission.result state with selection = prepared.ids |> List.map string }
    | "delete-selected" -> Some(delete services context query resource keys)
    | "reset-records" ->
        Session.reset services context query.workspace resource
        Some(Submission.result "selection-reset")
    | _ -> None
