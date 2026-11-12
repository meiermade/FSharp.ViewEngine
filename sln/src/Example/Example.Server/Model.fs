namespace Example.Server

open System
open System.Globalization
open Ledger.Domain
open Example.Components.Views.Model

/// HTTP input parsing and illustrative API payloads belong to the host, not the shared views.
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

    let private jsonOptions = System.Text.Json.JsonSerializerOptions(WriteIndented = true)

    let accountPayload (account: Account) =
        System.Text.Json.JsonSerializer.Serialize(
            {| id = account.id; name = account.name; accountType = string account.accountType
               currency = account.currency; balance = balance initialRecords account |}, jsonOptions)

    let transactionPayload (transaction: Transaction) =
        System.Text.Json.JsonSerializer.Serialize(
            {| id = transaction.id; date = transaction.date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
               description = transaction.description; accountId = transaction.accountId
               amount = transaction.amount; status = string transaction.status |}, jsonOptions)
