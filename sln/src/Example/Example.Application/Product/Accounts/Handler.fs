module Example.Application.Product.Accounts.Handler

open Example.UI.Views.Model
open Example.Application.Web.Common
open Ledger.Domain

let tryPage = function
    | ["accounts"] -> Some ApplicationPage.Accounts
    | ["accounts"; "new"] -> Some ApplicationPage.CreateAccount
    | ["accounts"; id] -> Request.resourceId tryAccount id |> Option.map ApplicationPage.Account
    | ["accounts"; id; "edit"] -> Request.resourceId tryAccount id |> Option.map ApplicationPage.EditAccount
    | ["accounts"; id; "delete"] -> Request.resourceId tryAccount id |> Option.map ApplicationPage.DeleteAccount
    | _ -> None
