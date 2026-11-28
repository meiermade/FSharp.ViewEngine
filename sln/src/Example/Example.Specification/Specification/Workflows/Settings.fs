namespace Example.Specification.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Specification.Web
open type Html
open Model
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
          requirements = [] }
