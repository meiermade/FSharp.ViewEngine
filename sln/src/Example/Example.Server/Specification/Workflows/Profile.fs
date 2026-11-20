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

module internal Profile =
    let workflow: Workflow =
        { key = "profile"
          label = "View profile"
          description = "Review identity and workspace access, validate profile fields and choose local appearance."
          states =
            [ state "default" "General" ApplicationPage.Profile []
              state "organizations" "Organizations" ApplicationPage.ProfileOrganizations []
              state "edit" "Edit profile" ApplicationPage.EditProfile []
              state "invalid" "Validation" ApplicationPage.EditProfile [ "state", "invalid" ] ]
          sequence =
            validateSequence
                "/examples/application/profile/edit"
                "/examples/application/profile/edit"
                "validateProfile (name, email, timeZone, dailySummaryTime)"
            + "\n  Operator->>Browser: Choose System, Light or Dark\n  Browser->>Browser: Apply and store local appearance preference"
          requirements = [] }
