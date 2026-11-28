module Example.Application.Web.Demo.Actions.Accounts

open Microsoft.AspNetCore.Http
open Example.Application.Web.Common
open Example.Application.Web.Demo
open Example.UI.Views.Model
open Example.Application.Web
open Example.UI.Views.ViewData
open Ledger.Operations

let submit services context (query: Query) page (form: IFormCollection) =
    let value name = form[name].ToString().Trim()
    match page with
    | ApplicationPage.CreateAccount | ApplicationPage.EditAccount _ ->
        let existing = match page with ApplicationPage.EditAccount id -> Some id | _ -> None
        let request: AccountDraft =
            { existingAccountId = existing; name = form["name"].ToString()
              accountType = value "accountType"; parentId = value "parentId"
              currency = value "currency"; subtype = value "subtype"; observedBalance = value "observedBalance" }
        let result = Projection.validateAccount (Session.records services context query.workspace) request
        let state =
            match result with
            | Ok _ -> "validated"
            | Error ValidateAccountError.DuplicateName -> "duplicate-name"
            | Error ValidateAccountError.InvalidType -> "invalid-type"
            | Error ValidateAccountError.InvalidDetails -> "invalid-details"
            | Error _ -> "invalid"
        Some { Submission.result state with draft = if Result.isError result then Some(EditorDraft.Account request) else None }
    | ApplicationPage.Accounts -> Collection.submit services context query SelectionResource.Accounts "accountIds" form
    | ApplicationPage.DeleteAccount id when value "action" = "delete" ->
        let result = Collection.delete services context query SelectionResource.Accounts [string id]
        Some { result with destination = if result.state = "selection-deleted" then Some ApplicationPage.Accounts else None }
    | _ -> None
