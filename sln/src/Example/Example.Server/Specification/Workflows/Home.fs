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

module internal Home =
    let workflow: Workflow =
        { key = "home"
          label = "View home"
          description = "Inspect one account's balance and related activity for an explicit period and scenario."
          states =
            [ state "default" "Default" ApplicationPage.Home []
              state "account" "Tax reserve" ApplicationPage.Home [ "account", "102" ]
              state "period" "Last 30 days" ApplicationPage.Home [ "account", "101"; "range", "30" ]
              state
                  "scenario"
                  "Pessimistic scenario"
                  ApplicationPage.Home
                  [ "account", "101"; "comparison", "pessimistic" ] ]
          sequence =
            readSequence
                "/examples/application?account=101&range=30&comparison=expected"
                "Resolve workspace, account, balance and dated activity"
                "chart and related transactions"
          requirements =
            [ "Chart range selection SHALL trigger one server read when a drag completes or the equivalent keyboard selection is committed, not on every pointer movement." ] }
