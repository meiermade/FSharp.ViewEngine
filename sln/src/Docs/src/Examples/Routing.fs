namespace Docs.Examples

open System
open Model
open Ledger.Domain

module Routing =
    let private idFrom text find =
        match Int32.TryParse(text:string) with true,id when find id |> Option.isSome -> Some id | _ -> None
    let applicationPage path =
        match (path:string).TrimEnd('/').Split('/') |> Array.toList with
        | [""; "examples"; "application"] -> Some ApplicationPage.Home
        | [""; "examples"; "application"; "accounts"] -> Some ApplicationPage.Accounts
        | [""; "examples"; "application"; "accounts"; "new"] -> Some ApplicationPage.CreateAccount
        | [""; "examples"; "application"; "accounts"; id] -> idFrom id tryAccount |> Option.map ApplicationPage.Account
        | [""; "examples"; "application"; "accounts"; id; "edit"] -> idFrom id tryAccount |> Option.map ApplicationPage.EditAccount
        | [""; "examples"; "application"; "accounts"; id; "delete"] -> idFrom id tryAccount |> Option.map ApplicationPage.DeleteAccount
        | [""; "examples"; "application"; "transactions"] -> Some ApplicationPage.Transactions
        | [""; "examples"; "application"; "transactions"; id] -> idFrom id tryTransaction |> Option.map ApplicationPage.Transaction
        | [""; "examples"; "application"; "settings"] -> Some ApplicationPage.Settings
        | [""; "examples"; "application"; "settings"; key] when settingsSections |> List.exists (fst >> (=) key) -> Some (ApplicationPage.SettingsSection key)
        | [""; "examples"; "application"; "profile"] -> Some ApplicationPage.Profile
        | _ -> None
    let private documentPage prefix pages path =
        let path = (path:string).TrimEnd('/')
        if path=prefix then Some ""
        elif path.StartsWith(prefix+"/",StringComparison.Ordinal) then
            let page = path.Substring(prefix.Length+1)
            if List.contains page pages then Some page else None
        else None
    let specificationPage path = documentPage "/examples/specification" Specification.pages path
    let apiPage path = documentPage "/examples/api-documentation" ["list-accounts"; "create-account"; "update-account"; "delete-account"; "list-transactions"; "get-transaction"] path
