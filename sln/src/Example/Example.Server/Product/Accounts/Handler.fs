module Example.Server.Product.Accounts.Handler

open Microsoft.AspNetCore.Http
open Example.Server.Common
open Example.Server.Product
open Example.Components.Views.Model
open Ledger.Domain
open Ledger.Operations

let tryPage = function
    | ["accounts"] -> Some ApplicationPage.Accounts
    | ["accounts"; "new"] -> Some ApplicationPage.CreateAccount
    | ["accounts"; id] -> Request.resourceId tryAccount id |> Option.map ApplicationPage.Account
    | ["accounts"; id; "edit"] -> Request.resourceId tryAccount id |> Option.map ApplicationPage.EditAccount
    | ["accounts"; id; "delete"] -> Request.resourceId tryAccount id |> Option.map ApplicationPage.DeleteAccount
    | _ -> None

let submit services context (query: Query) page (form: IFormCollection) =
    let value name = form[name].ToString().Trim()
    match page with
    | ApplicationPage.CreateAccount | ApplicationPage.EditAccount _ ->
        let existing = match page with ApplicationPage.EditAccount id -> Some id | _ -> None
        let request =
            { existingAccountId = existing; name = form["name"].ToString()
              accountType = value "accountType"; parentId = value "parentId"
              currency = value "currency"; subtype = value "subtype"; observedBalance = value "observedBalance" }
        let result = validateAccount query.records request
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
