module Example.Application.Product.Transactions.Handler

open Example.UI.Views.Model
open Example.Application.Web.Common
open Ledger.Domain

let tryPage = function
    | ["transactions"] -> Some ApplicationPage.Transactions
    | ["transactions"; id] -> Request.resourceId tryTransaction id |> Option.map ApplicationPage.Transaction
    | ["transactions"; id; "delete"] -> Request.resourceId tryTransaction id |> Option.map ApplicationPage.DeleteTransaction
    | _ -> None
