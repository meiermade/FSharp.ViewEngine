namespace Example.Server.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open Model
open Ledger.Domain
open Workflow

module internal Settings =
    let workflow: Workflow =
        { key = "settings"
          label = "View settings"
          description =
            "Review the selected organization's details, environments, users, API clients, ledgers and billing."
          states =
            [ for key, label in settingsSections do
                  yield state key label (ApplicationPage.SettingsSection key) []
              yield state "environment" "Environment details" (ApplicationPage.Environment "staging") []
              yield state "environment-edit" "Edit environment" (ApplicationPage.EditEnvironment "staging") []
              yield
                  state
                      "environment-invalid"
                      "Environment name validation"
                      (ApplicationPage.EditEnvironment "staging")
                      [ "state", "invalid" ]
              yield state "edit" "Edit organization" ApplicationPage.EditOrganization []
              yield state "invalid" "Name validation" ApplicationPage.EditOrganization [ "state", "invalid" ] ]
          sequence =
            validateSequence
                "/examples/application/settings/general/edit"
                "/examples/application/settings/general/edit"
                "validateOrganization (name, currency)"
          rules =
            [ "Settings SHALL belong to the selected organization; its selector SHALL retain the current settings section and change only the organization context."
              "The top-right Settings gear link SHALL open the selected organization’s Settings area. General SHALL show a Details section; its pencil Edit link SHALL open the Edit organization Drawer with Update and Cancel."
              "Environment rows SHALL open the exact environment's detail page without switching the workspace. Edit environment SHALL open a contextual Drawer for its name, with Update and Cancel. Names SHALL contain 1–80 characters. Successful validation SHALL close the editor and return to that environment's detail; immutable fixtures SHALL remain unchanged."
              "Organization memberships SHALL belong to Profile → Organizations, not organization Settings."
              "Return to Geldos SHALL preserve the prior workspace, including after changing organizations in Settings."
              "Organization name SHALL contain 1–80 characters; reporting commodity SHALL be USD."
              "Invalid values SHALL remain only in the private, no-store response with field errors."
              "Application UI SHALL use ordinary product copy without template commentary or invented live services." ] }
