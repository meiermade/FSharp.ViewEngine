namespace Example.Server.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open Model
open Ledger.Domain
open Workflow

module internal ViewAccounts =
    let workflow: Workflow =
        { key = "accounts/view-accounts"
          label = "View accounts"
          description = "Browse the type hierarchy, filter and sort accounts, and delete a keyed selection with confirmation."
          states =
            [ state "default" "Hierarchy" ApplicationPage.Accounts []
              state "filter-added" "Type filter added" ApplicationPage.Accounts [ "filters", "accountType" ]
              state "assets" "Assets" ApplicationPage.Accounts [ "accountType", "Asset"; "filters", "accountType" ]
              state
                  "multiple-types"
                  "Multiple types"
                  ApplicationPage.Accounts
                  [ "accountType", "Asset,Liability"; "filters", "accountType" ]
              state "sorted" "Balance descending" ApplicationPage.Accounts [ "sort", "balance" ]
              state "selected" "Rows selected" ApplicationPage.Accounts [ "state", "selected" ]
              state "empty" "No matches" ApplicationPage.Accounts [ "search", "No matching account" ] ]
          sequence =
            selectionSequence
                "/examples/application/accounts"
                "type roots and matching accounts"
                "Accounts"
                "accountIds"
          rules =
            collectionRules
            @ [ "Type roots SHALL begin at hierarchy depth zero; accounts SHALL sit one level below their root."
                "Search, type filter and sort SHALL be server-rendered URL controls."
                "Record links SHALL identify the exact account and open View account."
                "The application SHALL own keyed selection; the page SHALL own deletion, confirmation and feedback."
                "Type-group checkboxes SHALL select their real descendant accounts, not submit group headings as records. Accounts with balances or recorded transactions SHALL block the entire deletion without partially deleting eligible records." ] }
