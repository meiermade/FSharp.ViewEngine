namespace Example.Server.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
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
                "validateProfile (name, email, timeZone)"
            + "\n  Operator->>Browser: Choose System, Light or Dark\n  Browser->>Browser: Apply and store local appearance preference"
          rules =
            [ "The top-right Profile icon link SHALL open a personal Profile area containing General and Organizations, with a return to the prior Ledger workspace. Its sidebar SHALL NOT repeat the avatar or identity block."
              "General SHALL show a Details section; its pencil Edit link SHALL open the Edit profile Drawer with Update and Cancel."
              "Appearance SHALL stay on Profile → General as compact System, Light and Dark radio choices styled like tabs, applying immediately and independently of the edit Drawer. Top navigation SHALL NOT duplicate the appearance selector."
              "Organization memberships and roles SHALL appear on Organizations, with real destinations for each organization's Settings."
              "Name SHALL contain 1–80 characters; email SHALL be valid and time zone SHALL be available."
              "Profile submissions SHALL NOT retain private values outside a private, no-store field-error response."
              "Editor dismissal and navigation SHALL leave directly without unsaved-change prompts." ] }
