namespace Example.Specification.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Specification.Web
open type Html
open Model
open Workflow

module internal ViewAccount =
    let workflow: Workflow =
        { key = "accounts/view-account"
          label = "View account"
          description =
            "Inspect an account's balance, properties and related transactions, then choose an account operation."
          states =
            [ state "default" "Active account" (ApplicationPage.Account 101) []
              state "root" "Root account" (ApplicationPage.Account 1) []
              state "no-activity" "No activity" (ApplicationPage.Account 105) [] ]
          sequence =
            readSequence
                "/examples/application/accounts/101"
                "Find account 101, its balance and its transactions"
                "account detail with update and delete links"
          requirements = [] }
