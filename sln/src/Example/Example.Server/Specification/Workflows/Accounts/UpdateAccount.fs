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

module internal UpdateAccount =
    let workflow: Workflow =
        { key = "accounts/update-account"
          label = "Update account"
          description = "Edit an existing account's authored values and review finite validation outcomes."
          states = editorStates (ApplicationPage.EditAccount 101)
          sequence =
            validateSequence
                "/examples/application/accounts/101/edit"
                "/examples/application/accounts/101/edit"
                "validateAccount (existingAccountId=Some 101)"
          requirements = [] }
