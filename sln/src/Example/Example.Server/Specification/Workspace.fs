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

module internal Workspace =
    let workflow: Workflow =
        { key = "workspace"
          label = "Select workspace"
          description = "Choose an organization, environment and ledger from the full sidebar context row."
          states =
            [ state "default" "Current workspace" ApplicationPage.Home []
              state "sandbox" "Staging" ApplicationPage.Home [ "environment", "staging" ]
              state
                  "ledger"
                  "Jordan ledger"
                  ApplicationPage.Home
                  [ "organization", "example-household"; "ledger", "jordan" ]
              state
                  "organization"
                  "Client organization"
                  ApplicationPage.Home
                  [ "organization", "client-organization"; "ledger", "operating-company" ] ]
          sequence =
            "sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Server\n  participant Domain as Example.Domain\n  Operator->>Browser: Choose ledger, organization or environment\n  Browser->>Server: GET /examples/application?organization=example-household&environment=staging&ledger=jordan\n  Server->>Domain: Resolve available workspace context\n  Domain-->>Server: Organization, environment and matching ledger\n  Server-->>Browser: 200 HTML — current workspace and sandbox notice"
          rules =
            [ "The trigger SHALL fill the sidebar context row and the current ledger SHALL be marked."
              "A ledger SHALL belong to the selected organization; unknown combinations SHALL resolve to available fixtures."
              "Page links, filters, forms and redirects SHALL retain workspace context."
              "Sandbox environments SHALL be clearly labelled; closing a selector dialog SHALL restore trigger focus. Successful workspace navigation SHALL close the selector's native modal state before morphing, leaving the resulting page interactive." ] }
