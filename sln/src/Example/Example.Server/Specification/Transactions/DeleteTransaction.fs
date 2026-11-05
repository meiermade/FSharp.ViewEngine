namespace Example.Server.SpecificationPages

open Example.Server
open Model
open Workflow

module internal DeleteTransaction =
    let workflow: Workflow =
        { key = "transactions/delete-transaction"
          label = "Delete transaction"
          description = "Confirm the exact transaction and remove it from this browser's workspace."
          states =
            [ state "default" "Confirmation" (ApplicationPage.DeleteTransaction 203) []
              state "verified" "Verified transaction" (ApplicationPage.DeleteTransaction 201) [] ]
          sequence =
            "sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Server\n  participant Application as Example.Application\n  Browser->>Server: GET /examples/application/transactions/203/delete\n  Server-->>Browser: 200 HTML — confirmation over originating page\n  Operator->>Browser: Delete\n  Browser->>Server: POST /examples/application/transactions/203/delete (action=delete)\n  Server->>Application: deleteSelection (Transactions, keys, current records)\n  Application-->>Server: Updated snapshot and count\n  Server->>Server: Protect workspace session state\n  Server-->>Browser: 302 Location /examples/application/transactions?state=selection-deleted\n  Browser->>Server: GET Location\n  Server-->>Browser: 200 HTML — transaction removed and deletion count"
          rules =
            [ "Transaction deletion SHALL identify an existing transaction and require a centered confirmation with Cancel initially focused and a destructive Delete button."
              "Verification status SHALL NOT restrict deletion."
              "Deletion SHALL be last and separated in collection overflow menus."
              "The originating page title, breadcrumbs, workspace and collection controls SHALL remain unchanged while confirmation is open."
              "Cancel, Escape or backdrop SHALL return without changing records."
              "Success SHALL return to Transactions, remove the transaction and update related account balances and activity in this browser's workspace; Restore records SHALL recover the example." ] }
