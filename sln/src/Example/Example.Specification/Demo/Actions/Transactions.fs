module Example.Specification.Web.Demo.Actions.Transactions

open Microsoft.AspNetCore.Http
open Example.Specification.Web.Common
open Example.Specification.Web.Demo
open Example.UI.Views.Model

let submit services context query page (form: IFormCollection) =
    match page with
    | ApplicationPage.Transactions -> Collection.submit services context query ApplicationPage.Transactions "transactionIds" form
    | ApplicationPage.DeleteTransaction id when form["action"].ToString().Trim() = "delete" ->
        let result = Collection.delete services context query ApplicationPage.Transactions [string id]
        Some { result with destination = if result.state = "selection-deleted" then Some ApplicationPage.Transactions else None }
    | _ -> None
