namespace Example.Server

open System
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.Http
open Ledger.Domain
open Ledger.Operations
open Example.Components.Views.Model

/// Protected workspace session state contains fixture deletions and one consumable action result.
module internal DemoState =
    // Restarting the example resets demo sessions rather than persisting a key ring.
    let private provider = new EphemeralDataProtectionProvider()
    let private protector workspace = provider.CreateProtector("fve-ledger-demo-v1:" + workspaceQuery workspace)
    let private cookieName workspace =
        "fve-ledger-" + (SHA256.HashData(Encoding.UTF8.GetBytes(workspaceQuery workspace)) |> Convert.ToHexString).Substring(0, 20)
    let private options (context: HttpContext) =
        CookieOptions(Path = string context.Request.PathBase + "/examples", HttpOnly = true,
                      SameSite = SameSiteMode.Lax, Secure = context.Request.IsHttps, IsEssential = true)
    let private resourceName = function SelectionResource.Accounts -> "accounts" | SelectionResource.Transactions -> "transactions"
    let private notification resource state count =
        { id = Guid.NewGuid().ToString("N"); resource = resourceName resource; state = state; count = count }

    let private read (context: HttpContext) workspace =
        let value = context.Request.Cookies[cookieName workspace]
        if String.IsNullOrEmpty value || value.Length > 4096 then initialRecords, None
        else
            try
                use document = JsonDocument.Parse((protector workspace).Unprotect value)
                let root = document.RootElement
                let ids name = root.GetProperty(name: string).EnumerateArray() |> Seq.map (fun item -> item.GetInt32()) |> Set.ofSeq
                let removedAccounts, removedTransactions = ids "accountIds", ids "transactionIds"
                let records =
                    { accounts = accounts |> List.filter (fun row -> not (removedAccounts.Contains row.id))
                      transactions = transactions |> List.filter (fun row -> not (removedTransactions.Contains row.id)) }
                let id = root.GetProperty("eventId").GetString()
                let pending =
                    if String.IsNullOrEmpty id then None
                    else Some { id = id; resource = root.GetProperty("resource").GetString()
                                state = root.GetProperty("state").GetString(); count = root.GetProperty("count").GetInt32() }
                records, pending
            with
            | :? CryptographicException
            | :? FormatException
            | :? JsonException
            | :? InvalidOperationException
            | :? Collections.Generic.KeyNotFoundException -> initialRecords, None

    let private write (context: HttpContext) workspace (records: Records) (pending: CollectionNotification option) =
        if records = initialRecords && pending.IsNone then
            context.Response.Cookies.Delete(cookieName workspace, options context)
        else
            let stored =
                {| accountIds = accounts |> List.filter (fun row -> not (records.accounts |> List.exists (fun item -> item.id = row.id))) |> List.map _.id |> List.toArray
                   transactionIds = transactions |> List.filter (fun row -> not (records.transactions |> List.exists (fun item -> item.id = row.id))) |> List.map _.id |> List.toArray
                   eventId = pending |> Option.map _.id |> Option.defaultValue ""
                   resource = pending |> Option.map _.resource |> Option.defaultValue ""
                   state = pending |> Option.map _.state |> Option.defaultValue ""
                   count = pending |> Option.map _.count |> Option.defaultValue 0 |}
            context.Response.Cookies.Append(cookieName workspace, (protector workspace).Protect(JsonSerializer.Serialize stored), options context)

    let apply context (query: Query) =
        let records, _ = read context query.workspace
        { query with records = records }

    // Consume only in the resulting product collection GET, not its surrounding Specification document.
    let takeNotification context (query: Query) resource =
        let records, pending = read context query.workspace
        match pending with
        | Some item when item.resource = resourceName resource ->
            write context query.workspace records None
            { query with notification = Some item }
        | _ -> query

    let notify context (query: Query) resource state =
        write context query.workspace query.records (Some(notification resource state 0))

    let save context workspace resource (result: DeleteSelectionResponse) =
        write context workspace result.records (Some(notification resource "selection-deleted" result.count))

    let reset context workspace resource =
        write context workspace initialRecords (Some(notification resource "selection-reset" 0))
