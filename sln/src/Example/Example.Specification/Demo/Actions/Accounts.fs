module Example.Specification.Web.Demo.Actions.Accounts

open System
open Microsoft.AspNetCore.Http
open Example.Specification.Web.Demo
open Example.UI.Views.Model
open Example.UI.Views.ViewData

let submit services context (query: Query) page (form: IFormCollection) =
    let value name = form[name].ToString().Trim()
    match page with
    | ApplicationPage.CreateAccount | ApplicationPage.EditAccount _ ->
        let existing = match page with ApplicationPage.EditAccount id -> Some id | _ -> None
        let draft: AccountDraft =
            { existingAccountId = existing; name = form["name"].ToString(); accountType = value "accountType"
              parentId = value "parentId"; currency = value "currency"; subtype = value "subtype"; observedBalance = value "observedBalance" }
        let state =
            if draft.name.Trim().Length = 0 then "invalid"
            elif query.records.accounts |> List.exists (fun row -> Some row.id <> existing && row.name.Equals(draft.name.Trim(), StringComparison.OrdinalIgnoreCase)) then "duplicate-name"
            elif not (List.contains draft.accountType query.catalog.accountTypes) then "invalid-type"
            elif draft.currency <> "USD" || draft.subtype <> "Generic" || draft.observedBalance <> "not-required" then "invalid-details"
            elif not (query.records.accounts |> List.exists (fun row -> string row.id = draft.parentId && row.accountType = draft.accountType && Some row.id <> existing)) then "invalid-details"
            else "validated"
        Some { Submission.result state with draft = if state = "validated" then None else Some(EditorDraft.Account draft) }
    | ApplicationPage.Accounts -> Collection.submit services context query ApplicationPage.Accounts "accountIds" form
    | ApplicationPage.DeleteAccount id when value "action" = "delete" ->
        let result = Collection.delete services context query ApplicationPage.Accounts [string id]
        Some { result with destination = if result.state = "selection-deleted" then Some ApplicationPage.Accounts else None }
    | _ -> None
