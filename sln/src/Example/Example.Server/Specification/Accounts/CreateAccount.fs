namespace Example.Server.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open Model
open Ledger.Domain
open Workflow

module internal CreateAccount =
    let workflow: Workflow =
        { key = "accounts/create-account"
          label = "Create account"
          description = "Enter a new account's name, type and details and validate them on the server."
          states = editorStates ApplicationPage.CreateAccount
          sequence =
            validateSequence
                "/examples/application/accounts/new"
                "/examples/application/accounts/new"
                "validateAccount (existingAccountId=None)"
          rules =
            accountRules
            @ [ "Cancel or Escape SHALL return to Accounts without submitting or confirmation." ] }
