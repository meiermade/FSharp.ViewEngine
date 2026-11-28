module Example.Specification.Web.Demo.Collection

open Microsoft.AspNetCore.Http
open Example.UI.Views.Model

let private selection (query: Query) page keys =
    let choices =
        match page with
        | ApplicationPage.Accounts -> query.records.accounts |> List.map (fun row -> string row.id, row.canDelete)
        | _ -> query.records.transactions |> List.map (fun row -> string row.id, true)
    let keys = List.distinct keys
    let selected = choices |> List.filter (fun (key, _) -> List.contains key keys)
    if keys.IsEmpty || selected.Length <> keys.Length then None
    else Some (selected |> List.exists (snd >> not), keys)

let delete services context (query: Query) page keys =
    match selection query page keys with
    | None -> Submission.result "selection-invalid"
    | Some (true, selected) -> { Submission.result "delete-blocked" with selection = selected }
    | Some (false, selected) ->
        let keep id = not (List.contains (string id) selected)
        let records =
            match page with
            | ApplicationPage.Accounts -> { query.records with accounts = query.records.accounts |> List.filter (_.id >> keep); unchanged = false }
            | _ -> { query.records with transactions = query.records.transactions |> List.filter (_.id >> keep); unchanged = false }
        Session.save services context query.workspace page records selected.Length
        Submission.result "selection-deleted"

let submit services context (query: Query) page field (form: IFormCollection) =
    let keys = form[field] |> Seq.toList
    match form["action"].ToString().Trim() with
    | "prepare-delete" | "cancel-deletion" as action ->
        match selection query page keys with
        | None -> Some (Submission.result "selection-invalid")
        | Some (blocked, selected) ->
            let state = if action = "cancel-deletion" then "selected" elif blocked then "delete-blocked" else "delete-confirm"
            Some { Submission.result state with selection = selected }
    | "delete-selected" -> Some (delete services context query page keys)
    | "reset-records" ->
        Session.reset services context query.workspace page
        Some (Submission.result "selection-reset")
    | _ -> None
