namespace Example.Server.ApplicationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain
open Controls
open Collection
open TransactionTable

module internal ViewTransactions =
    let transactionsPage (query: Query) =
        let query =
            if List.contains query.sort [ "date-asc"; "date-desc" ] then
                query
            else
                { query with sort = "date-desc" }

        let rows =
            query.records.transactions
            |> List.filter (fun transaction ->
                transaction.description.Contains(query.search, StringComparison.OrdinalIgnoreCase)
                && filterMatches
                    query.status
                    (if transaction.status = TransactionStatus.Verified then
                         "verified"
                     else
                         "unverified")
                && filterMatches query.account (string transaction.accountId))
            |> (if query.sort = "date-asc" then
                    List.sortBy _.date
                else
                    List.sortByDescending _.date)

        let filters =
            [ { name = "status"
                label = "Verification"
                value = query.status
                options = [ "verified", "Verified"; "unverified", "Unverified" ] }
              { name = "account"
                label = "Account"
                value = query.account
                options = query.records.accounts |> List.map (fun account -> string account.id, account.name) } ]

        let content =
            if rows.IsEmpty then
                fveEmptyState {
                    _label "No matching transactions"
                    _description "Adjust the search, account, or verification filter."
                }
            else
                transactionTable query "Transactions" rows true

        collection query "transactions" filters content
