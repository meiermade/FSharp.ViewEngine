namespace Example.Components.Views.Pages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
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

        let filters = filterValues query.account
        let accountIds =
            query.records.accounts
            |> List.filter (fun account -> List.contains (string account.id) filters)
            |> List.collect (subtree query.records)
            |> List.map _.id |> Set.ofList
        let parseDate (value: string) =
            match DateOnly.TryParseExact(value, "yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None) with
            | true, date -> Some date
            | _ -> None
        let startDate, endDate = parseDate query.startDate, parseDate query.endDate
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
                && (filters.IsEmpty || involves accountIds transaction)
                && (startDate |> Option.forall (fun date -> transaction.date >= date))
                && (endDate |> Option.forall (fun date -> transaction.date <= date)))
            |> (if query.sort = "date-asc" then
                    List.sortBy (fun row -> row.date, row.id)
                else
                    List.sortByDescending (fun row -> row.date, row.id))

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
                transactionTable query "Transactions" rows None

        collection query "transactions" filters content
