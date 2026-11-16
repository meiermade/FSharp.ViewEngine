module Example.Server.Product.Transactions.Handler

open Microsoft.AspNetCore.Http
open Example.Server.Common
open Example.Server.Product
open Example.Components.Views.Model
open Ledger.Domain
open Ledger.Operations

let tryPage = function
    | ["transactions"] -> Some ApplicationPage.Transactions
    | ["transactions"; id] -> Request.resourceId tryTransaction id |> Option.map ApplicationPage.Transaction
    | ["transactions"; id; "delete"] -> Request.resourceId tryTransaction id |> Option.map ApplicationPage.DeleteTransaction
    | _ -> None

let submit services context query page (form: IFormCollection) =
    match page with
    | ApplicationPage.Transactions -> Collection.submit services context query SelectionResource.Transactions "transactionIds" form
    | ApplicationPage.DeleteTransaction id when form["action"].ToString().Trim() = "delete" ->
        let result = Collection.delete services context query SelectionResource.Transactions [string id]
        Some { result with destination = if result.state = "selection-deleted" then Some ApplicationPage.Transactions else None }
    | _ -> None
