namespace Example.Specification.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Specification.Web
open type Html
open Model
open Workflow

module internal ViewTransaction =
    let workflow: Workflow =
        { key = "transactions/view-transaction"
          label = "View transaction"
          description = "Inspect a transfer’s date, amount, verification status, and source and destination accounts."
          states =
            [ state "default" "Verified" (ApplicationPage.Transaction 201) []
              state "review" "Needs review" (ApplicationPage.Transaction 203) [] ]
          sequence =
            readSequence
                "/examples/application/transactions/201"
                "Find transaction 201 and its related account"
                "transaction detail"
          requirements = [] }
