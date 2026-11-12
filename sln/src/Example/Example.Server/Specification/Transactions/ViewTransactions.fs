namespace Example.Server.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open Example.Server
open type Html
open Model
open Ledger.Domain
open Workflow

module internal ViewTransactions =
    let workflow: Workflow =
        { key = "transactions/view-transactions"
          label = "View transactions"
          description = "Find dated activity by account, verification status and search, then confirm deletion of selected transactions."
          states =
            [ state "default" "All transactions" ApplicationPage.Transactions [ "sort", "date-desc" ]
              state "filter-added" "Verification filter added" ApplicationPage.Transactions [ "filters", "status" ]
              state "review" "Needs review" ApplicationPage.Transactions [ "status", "unverified"; "filters", "status" ]
              state
                  "account"
                  "Operating checking"
                  ApplicationPage.Transactions
                  [ "account", "101"; "filters", "account" ]
              state
                  "combined"
                  "Account and verification"
                  ApplicationPage.Transactions
                  [ "account", "101"; "status", "verified"; "filters", "account,status" ]
              state "sorted" "Date ascending" ApplicationPage.Transactions [ "sort", "date-asc" ]
              state "selected" "Rows selected" ApplicationPage.Transactions [ "state", "selected" ]
              state "empty" "No matches" ApplicationPage.Transactions [ "search", "No matching transaction" ] ]
          sequence =
            selectionSequence
                "/examples/application/transactions"
                "matching transactions or empty state"
                "Transactions"
                "transactionIds"
          rules =
            collectionRules
            @ [ "Amounts SHALL be signed decimal USD values; dates SHALL be date-only values."
                "Verification SHALL be independent of amount."
                "Empty results SHALL preserve filter controls."
                "Deletion SHALL validate current transaction IDs, remove the confirmed selection from this browser's workspace snapshot and update the related account balances, chart and transaction views." ] }
