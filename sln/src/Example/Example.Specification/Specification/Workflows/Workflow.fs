namespace Example.Specification.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Specification.Web
open type Html
open Model

module internal Workflow =
    type State =
        { key: string
          label: string
          page: ApplicationPage
          parameters: (string * string) list }

    type Workflow =
        { key: string
          label: string
          description: string
          states: State list
          sequence: string
          requirements: string list }

    let state key label page parameters =
        { key = key
          label = label
          page = page
          parameters = parameters }

    let readSequence endpoint lookup response =
        $"sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Application\n  participant Domain as Example.Domain\n  Operator->>Browser: Open page or apply controls\n  Browser->>Server: GET {endpoint}\n  Server->>Domain: {lookup}\n  Domain-->>Server: Immutable financial fixtures\n  Server-->>Browser: 200 HTML — {response}"

    let validateSequence (getEndpoint: string) postEndpoint (operation: string) =
        let successEndpoint =
            if getEndpoint.EndsWith("/new", StringComparison.Ordinal) then
                getEndpoint.Substring(0, getEndpoint.Length - 4)
            else
                getEndpoint.Substring(0, getEndpoint.Length - 5)

        let invalidResponse =
            if
                [ "validateAccount"; "validateProfile"; "validateOrganization" ]
                |> List.exists (fun name -> operation.StartsWith(name, StringComparison.Ordinal))
            then
                "200 HTML — field errors and entered values (private, no-store)"
            else
                $"302 Location {getEndpoint}?state=invalid\n    Browser->>Server: GET Location\n    Server-->>Browser: 200 HTML — validation outcome"

        $"sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Application\n  participant Application as Application use cases\n  Operator->>Browser: Open form\n  Browser->>Server: GET {getEndpoint}\n  Server-->>Browser: 200 HTML — labelled form\n  Operator->>Browser: Submit values\n  Browser->>Server: POST {postEndpoint} (URL-encoded)\n  Server->>Application: {operation}\n  alt Invalid values\n    Application-->>Server: Typed validation error\n    Server-->>Browser: {invalidResponse}\n  else Valid values\n    Application-->>Server: Validated response\n    Server-->>Browser: 302 Location {successEndpoint}\n    Browser->>Server: GET Location\n    Server-->>Browser: 200 HTML — originating page, editor closed\n  end"

    let selectionSequence endpoint response resource field =
        $"sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Application\n  participant Application as Application use cases\n  participant Domain as Example.Domain\n  Browser->>Server: GET {endpoint}\n  Server->>Domain: Read immutable collection facts\n  Domain-->>Server: Financial fixtures\n  Server->>Server: Apply URL search, filters and sort\n  Server-->>Browser: 200 HTML — {response}\n  Operator->>Browser: Change filters or submit Search on Enter\n  Browser->>Server: GET {endpoint} with collection controls\n  Server-->>Browser: 200 HTML — updated rows and applied filter groups\n  Operator->>Browser: Select keyed rows\n  Browser->>Browser: Show count and bulk commands in the toolbar\n  Operator->>Browser: Delete selected\n  Browser->>Server: POST {endpoint} (action=prepare-delete, {field})\n  Server->>Application: prepareDeletion ({resource}, keys, current records)\n  Application-->>Server: Available records and deletion restrictions\n  Server-->>Browser: 200 HTML — confirmation with count and records\n  alt Cancel\n    Browser->>Browser: Close confirmation and retain selection\n  else Confirm deletion\n    Browser->>Server: POST {endpoint} (action=delete-selected, {field})\n    Server->>Application: deleteSelection (revalidate all keys and restrictions)\n    Application-->>Server: Updated immutable snapshot or typed error\n    Server->>Server: Protect workspace-scoped browser-session deletion state\n    Server-->>Browser: 302 Location {endpoint}?state=selection-deleted\n    Browser->>Server: GET Location\n    Server-->>Browser: 200 HTML — rows removed, selection cleared, controls retained, one-shot Notification\n  end"

    let editorStates page =
        [ state "default" "Editor" page []
          state "duplicate-name" "Name already exists" page [ "state", "duplicate-name" ] ]
