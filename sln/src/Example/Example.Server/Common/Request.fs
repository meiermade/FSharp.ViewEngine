namespace Example.Server.Common

open System
open Microsoft.AspNetCore.Http
open Ledger.Domain
open Example.Components.Views.Model

/// HTTP query/form parsing belongs to the host, not the shared views.
module Request =
    let queryFromValues (value: string -> string) =
        let fallback key defaultValue = if value key = "" then defaultValue else value key
        { defaultQuery with
            search = value "search"
            accountType = fallback "accountType" "all"
            filters =
                (value "filters").Split(',', StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList |> List.distinct
                |> List.filter (fun name -> List.contains name ["accountType"; "status"; "account"])
            sort = fallback "sort" "name"
            state = value "state"
            embedded = value "embedded" = "1"
            workspace = workspaceFromStrings (value "organization") (value "environment") (value "ledger")
            account = fallback "account" "all"
            range = fallback "range" "90"
            comparison = fallback "comparison" "expected"
            startDate = value "startDate"
            endDate = value "endDate"
            status = fallback "status" "all"
            transactionPage = (match Int32.TryParse(value "transactionPage") with true, page when page > 0 -> page | _ -> 1)
            appMode = (value "appMode" = "1" || value "fveAppMode" = "app")
            specState = value "specState"
            resource = value "resource"
            fixture = value "fveFixture" = "product"
            overlayFrom = (match value "from" with "accounts" -> "accounts" | "transactions" -> "transactions" | _ -> "")
            returnWorkspace =
                if value "returnOrganization" = "" then None
                else Some(workspaceFromStrings (value "returnOrganization") (value "returnEnvironment") (value "returnLedger")) }

    let query (context: HttpContext) = queryFromValues (fun name -> context.Request.Query[name].ToString())

    let submitted (context: HttpContext) (form: IFormCollection) =
        queryFromValues (fun name ->
            if name = "accountType" && form.ContainsKey "filterAccountType" then
                form["filterAccountType"].ToString().Trim()
            elif form.ContainsKey name then form[name].ToString().Trim()
            else context.Request.Query[name].ToString())

    let resourceId find (value: string) =
        match Int32.TryParse value with
        | true, id when find id |> Option.isSome -> Some id
        | _ -> None
