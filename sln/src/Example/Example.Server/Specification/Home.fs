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
          rules =
            [ "Account, period, scenario, selected start/end dates and workspace SHALL be URL-backed."
              "The Rocket/ECharts chart and transaction list SHALL use the same selected account and inclusive visible dates. Drag completion SHALL submit one server read, not hide already-loaded rows or query on every pointer movement."
              "Date controls SHALL provide the equivalent keyboard workflow. Reset zoom or double-click SHALL restore the configured period. Refresh, Back/Forward, appearance changes and unrelated morphs SHALL retain the selected range."
              "Example.Components SHALL own the chart presentation and pinned assets; Example.Application SHALL calculate balances and filter transactions. The Specification SHALL use the same implementation as the Application."
              "Solid balances SHALL carry earlier activity forward and derive from opening balance and signed transactions. Dashed lines SHALL identify the selected scenario." ] }
