namespace Example.Server.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open Model
open Ledger.Domain
open Workflow

module internal ViewAccount =
    let workflow: Workflow =
        { key = "accounts/view-account"
          label = "View account"
          description =
            "Inspect an account's balance, properties and related transactions, then choose an account operation."
          states =
            [ state "default" "Active account" (ApplicationPage.Account 101) []
              state "no-activity" "No activity" (ApplicationPage.Account 105) [] ]
          sequence =
            readSequence
                "/examples/application/accounts/101"
                "Find account 101, its balance and its transactions"
                "account detail with update and delete links"
          rules =
            [ "The resource ID SHALL select the matching seeded account."
              "Balance SHALL include only the account's opening balance and signed activity."
              "Related transaction links SHALL open their exact records."
              "Update and Delete destinations SHALL retain the viewed account's ID and workspace." ] }
