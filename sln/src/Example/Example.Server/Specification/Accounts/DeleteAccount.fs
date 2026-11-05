namespace Example.Server.SpecificationPages

open Example.Server
open Model
open Workflow

module internal DeleteAccount =
    let workflow: Workflow =
        { key = "accounts/delete-account"
          label = "Delete account"
          description = "Confirm an unused account and remove it from this browser's workspace."
          states =
            [ state "default" "Confirmation" (ApplicationPage.DeleteAccount 105) []
              state "blocked" "Has activity" (ApplicationPage.DeleteAccount 101) [ "state", "delete-blocked" ] ]
          sequence =
            "sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Server\n  participant Application as Example.Application\n  Browser->>Server: GET /examples/application/accounts/105/delete\n  Server-->>Browser: 200 HTML — confirmation over originating page\n  Operator->>Browser: Delete\n  Browser->>Server: POST /examples/application/accounts/105/delete (action=delete)\n  Server->>Application: deleteSelection (Accounts, keys, current records)\n  alt Account has balance or transactions\n    Application-->>Server: HasActivity\n    Server-->>Browser: 302 Location with state=delete-blocked\n  else Eligible account\n    Application-->>Server: Updated snapshot and count\n    Server->>Server: Protect workspace session state\n    Server-->>Browser: 302 Location /examples/application/accounts?state=selection-deleted\n    Browser->>Server: GET Location\n    Server-->>Browser: 200 HTML — account removed and deletion count\n  end"
          rules =
            [ "Deletion SHALL require confirmation, an existing account, zero balance and no transactions."
              "An account with activity SHALL display the blocking reason and SHALL NOT offer a confirmation submission."
              "Account 105 SHALL be the eligible fixture; account 101 SHALL demonstrate the blocked case."
              "Delete SHALL be last and separated in collection overflow menus."
              "Confirmation SHALL focus Cancel and keep the originating page title and breadcrumbs unchanged."
              "Cancel, Escape or backdrop SHALL return to the originating collection or detail without changing records."
              "Success SHALL remove the account in this browser's workspace, return to Accounts with its public controls retained and make the removed resource unavailable until Restore records." ] }
