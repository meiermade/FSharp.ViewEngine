namespace Example.Specification.SpecificationPages

open Example.Specification.Web
open Example.UI.Views.Model
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
            "sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Application\n  participant Application as Application use cases\n  Browser->>Server: GET /examples/application/transactions/203/delete\n  Server-->>Browser: 200 HTML — confirmation over originating page\n  Operator->>Browser: Delete\n  Browser->>Server: POST /examples/application/transactions/203/delete (action=delete)\n  Server->>Application: deleteSelection (Transactions, keys, current records)\n  Application-->>Server: Updated snapshot and count\n  Server->>Server: Protect workspace session state\n  Server-->>Browser: 302 Location /examples/application/transactions?state=selection-deleted\n  Browser->>Server: GET Location\n  Server-->>Browser: 200 HTML — transaction removed and deletion count"
          requirements = [] }
