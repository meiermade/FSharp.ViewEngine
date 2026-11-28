module Example.Application.Web.Demo.Actions.Transactions

open Microsoft.AspNetCore.Http
open Example.Application.Web.Common
open Example.Application.Web.Demo
open Example.UI.Views.Model
open Ledger.Domain
open Ledger.Operations

let submit services context query page (form: IFormCollection) =
    match page with
    | ApplicationPage.Transactions -> Collection.submit services context query SelectionResource.Transactions "transactionIds" form
    | ApplicationPage.DeleteTransaction id when form["action"].ToString().Trim() = "delete" ->
        let result = Collection.delete services context query SelectionResource.Transactions [string id]
        Some { result with destination = if result.state = "selection-deleted" then Some ApplicationPage.Transactions else None }
    | _ -> None
