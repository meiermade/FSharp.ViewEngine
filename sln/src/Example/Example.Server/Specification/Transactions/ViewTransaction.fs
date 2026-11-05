namespace Example.Server.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open Model
open Ledger.Domain
open Workflow

module internal ViewTransaction =
    let workflow: Workflow =
        { key = "transactions/view-transaction"
          label = "View transaction"
          description = "Inspect a specific transaction's date, signed amount, verification and related account."
          states =
            [ state "default" "Verified" (ApplicationPage.Transaction 201) []
              state "review" "Needs review" (ApplicationPage.Transaction 203) [] ]
          sequence =
            readSequence
                "/examples/application/transactions/201"
                "Find transaction 201 and its related account"
                "transaction detail"
          rules =
            [ "The resource ID SHALL select the matching transaction."
              "Account links SHALL open that transaction's actual account."
              "Date, amount and verification SHALL match the shared Domain fixture." ] }
