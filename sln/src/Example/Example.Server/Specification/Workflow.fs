namespace Example.Server.SpecificationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open Model
open Ledger.Domain

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
          rules: string list }

    let state key label page parameters =
        { key = key
          label = label
          page = page
          parameters = parameters }

    let readSequence endpoint lookup response =
        $"sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Server\n  participant Domain as Example.Domain\n  Operator->>Browser: Open page or apply controls\n  Browser->>Server: GET {endpoint}\n  Server->>Domain: {lookup}\n  Domain-->>Server: Immutable financial fixtures\n  Server-->>Browser: 200 HTML — {response}"

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

        $"sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Server\n  participant Application as Example.Application\n  Operator->>Browser: Open form\n  Browser->>Server: GET {getEndpoint}\n  Server-->>Browser: 200 HTML — labelled form\n  Operator->>Browser: Submit values\n  Browser->>Server: POST {postEndpoint} (URL-encoded)\n  Server->>Application: {operation}\n  alt Invalid values\n    Application-->>Server: Typed validation error\n    Server-->>Browser: {invalidResponse}\n  else Valid values\n    Application-->>Server: Validated response\n    Server-->>Browser: 302 Location {successEndpoint}\n    Browser->>Server: GET Location\n    Server-->>Browser: 200 HTML — originating page, editor closed\n  end"

    let selectionSequence endpoint response resource field =
        $"sequenceDiagram\n  actor Operator\n  participant Browser\n  participant Server as Example.Server\n  participant Application as Example.Application\n  participant Domain as Example.Domain\n  Browser->>Server: GET {endpoint}\n  Server->>Domain: Read immutable collection facts\n  Domain-->>Server: Financial fixtures\n  Server->>Server: Apply URL search, filters and sort\n  Server-->>Browser: 200 HTML — {response}\n  Operator->>Browser: Change filters or submit Search on Enter\n  Browser->>Server: GET {endpoint} with collection controls\n  Server-->>Browser: 200 HTML — updated rows and applied filter groups\n  Operator->>Browser: Select keyed rows\n  Browser->>Browser: Show count and bulk commands in the toolbar\n  Operator->>Browser: Delete selected\n  Browser->>Server: POST {endpoint} (action=prepare-delete, {field})\n  Server->>Application: prepareDeletion ({resource}, keys, current records)\n  Application-->>Server: Available records and deletion restrictions\n  Server-->>Browser: 200 HTML — confirmation with count and records\n  alt Cancel\n    Browser->>Browser: Close confirmation and retain selection\n  else Confirm deletion\n    Browser->>Server: POST {endpoint} (action=delete-selected, {field})\n    Server->>Application: deleteSelection (revalidate all keys and restrictions)\n    Application-->>Server: Updated immutable snapshot or typed error\n    Server->>Server: Protect workspace-scoped browser-session deletion state\n    Server-->>Browser: 302 Location {endpoint}?state=selection-deleted\n    Browser->>Server: GET Location\n    Server-->>Browser: 200 HTML — rows removed, selection cleared, controls retained, one-shot Notification\n  end"

    let editorStates page =
        [ state "default" "Editor" page []
          state "duplicate-name" "Name already exists" page [ "state", "duplicate-name" ] ]

    let accountRules =
        [ "Names SHALL contain 1–80 characters and be unique ignoring case, excluding the account being updated."
          "Account type SHALL be one of Asset, Liability, Equity, Revenue or Expense; parent SHALL match that type root."
          "Commodity SHALL be USD, subtype SHALL be Generic and month-end observation SHALL be not required."
          "Native required validation SHALL precede submission; server validation SHALL reject invalid requests independently."
          "Create and Edit SHALL use a single-column contextual Drawer with a scrolling body and fixed header/footer. Create and Update SHALL submit; the request SHALL drive the button spinner via a Datastar indicator signal, with a one-second example-only server delay. Successful submissions SHALL close the editor and return to the originating page; Cancel, close, Escape and backdrop SHALL return without submitting or asking to discard changes."
          "A failed submission SHALL return the entered values in that response only, with field errors. Submitted values SHALL NOT enter URLs, cookies or durable storage."
          "Opening an overlay SHALL preserve its originating collection/detail title, breadcrumbs, workspace, search, filters and sorting." ]

    let shellRules =
        [ "Application, Specification and API documentation SHALL share the documentation's sky brand theme; success, warning and error colors SHALL remain semantic."
          "PageTopBar SHALL contain brand and breadcrumbs; a visible PageHeader SHALL contain the page title and page actions." ]

    let collectionRules =
        [ "Search and Add filter SHALL precede the table. Search SHALL submit a native GET on Enter; added filters SHALL appear beneath as compact ButtonGroups joining a muted, noninteractive filter-name label, dropdown values and icon-remove action without gaps. DropdownMenu SHALL own checkbox-menu semantics and keyboard behavior; consumer-owned GET forms SHALL apply multiple choices, with native checkboxes and Apply when JavaScript is unavailable. Each menu SHALL contain one small Clear selection action at the top and no All or Select all option. Choice toggles SHALL immediately render matching results while keeping the menu open and focus on the toggled choice; Escape SHALL close it and restore trigger focus."
          "Each structured filter SHALL appear at most once. Choices within a filter SHALL compose using OR; different filters SHALL compose using AND. No selected choices SHALL mean unrestricted results and a muted value segment labelled Any. Selected choices SHALL highlight the chip and show its values, or a count for more than two. Clear selection SHALL leave the chip present but inactive; Remove SHALL remove it. Clear all SHALL appear only while filter chips are present, remove them and preserve Search."
          "Sorting SHALL use column-heading destinations and SHALL retain active search, filters and workspace."
          "Selecting rows SHALL replace Search and Add filter with X selected, Delete selected and Clear selection in the same toolbar; applied filters SHALL remain visible. At zero selection, count and bulk commands SHALL be hidden."
          "The application SHALL own keyed selection, count, deletion and screen-reader feedback. Delete selected SHALL first show a confirmation with the affected records and count; Cancel, close, Escape and backdrop SHALL preserve selection. Confirmation SHALL revalidate every key and restriction server-side and SHALL delete all selected records or none. The native fallback SHALL support the same flow with ordinary forms."
          "The toolbar SHALL remain above the row-scrolling viewport without a bottom separator. Column headings SHALL remain sticky with no border above them; the divider below headings and between rows SHALL remain. Counts, commands and pagination controls SHALL NOT appear below the table. Action feedback SHALL appear in a fixed Notification region without changing table position. Success SHALL dismiss after five active seconds; empty or stale selection errors SHALL persist until dismissed. Notifications SHALL announce without taking focus and SHALL NOT replay on refresh or Back; blocking explanations SHALL remain in the confirmation. Native forms SHALL receive fixed, dismissible feedback without JavaScript."
          "This finite example SHALL NOT imply an infinite-scroll endpoint or all-matching selection. The composition SHALL allow a consumer to supply additional stable-keyed rows without a table footer."
          "Filter changes SHALL resolve a new server-rendered collection with selection cleared. Deletion SHALL retain search, filters, sort and workspace, remove the affected rows, clear selection and show the actual deleted count. Protected session cookies SHALL isolate deletions by browser and workspace without changing shared fixtures; Restore records SHALL reset this workspace from a stable PageHeader action, disabled when no records have been removed. Selected keys SHALL stay out of URLs. Row-action icon controls SHALL remain visually distinct on hovered/selected rows, indicate an open menu, and retain visible keyboard focus." ]
