namespace Docs.Pages

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open FSharp.ViewEngine.Components.Templates
open Docs.Common
open type Html
open type Svg
open type Datastar

/// Consumer-owned example data and composition, not additional package APIs.
module PageExamples =
    type ExamplePage =
        | DependencyGraph
        | ExecutionDetail
        | FinancialReporting
        | Messaging
        | Operations
        | Scheduling
        | MediaManagement

    type ReviewState =
        | Setup
        | Ready
        | Loading
        | Empty
        | Failed

    type ExampleQuery =
        { state: ReviewState
          item: string
          view: string
          range: string }

    let defaultQuery =
        { state = Ready
          item = ""
          view = ""
          range = "" }

    let slug =
        function
        | DependencyGraph -> "dependency-graph"
        | ExecutionDetail -> "execution-detail"
        | FinancialReporting -> "financial-reporting"
        | Messaging -> "messaging"
        | Operations -> "operations-dashboard"
        | Scheduling -> "scheduling"
        | MediaManagement -> "media-management"

    let title =
        function
        | DependencyGraph -> "Dependency graph"
        | ExecutionDetail -> "Execution detail"
        | FinancialReporting -> "Financial reporting"
        | Messaging -> "Messaging"
        | Operations -> "Operations dashboard"
        | Scheduling -> "Scheduling"
        | MediaManagement -> "Media management"

    let pages =
        [ DependencyGraph
          ExecutionDetail
          FinancialReporting
          Messaging
          Operations
          Scheduling
          MediaManagement ]

    let url page =
        "/components/page-examples/" + slug page

    let stateKey =
        function
        | Setup -> "setup"
        | Ready -> "ready"
        | Loading -> "loading"
        | Empty -> "empty"
        | Failed -> "error"

    let queryUrl page query =
        let pairs =
            [ "state", stateKey query.state
              "item", query.item
              "view", query.view
              "range", query.range ]

        url page
        + "?"
        + (pairs
           |> List.filter (snd >> String.IsNullOrEmpty >> not)
           |> List.map (fun (key, value) -> key + "=" + Uri.EscapeDataString value)
           |> String.concat "&")

    let queryFromStrings state item view range =
        { state =
            match state with
            | "setup" -> Setup
            | "loading" -> Loading
            | "empty" -> Empty
            | "error" -> Failed
            | _ -> Ready
          item = item
          view = view
          range = range }

    let registration page : DocPage =
        { id = "components-page-" + slug page
          path = url page
          aliases = []
          navLabel = title page
          category = "Application"
          title = title page
          browserTitle = title page + " · FSharp.ViewEngine.Components"
          nodes = [] }

    let link (href: string) (label: string) : HtmlElement =
        a {
            _href href

            _class
                "font-medium text-[var(--fve-brand-text)] underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

            label
        }

    // Ordinary consumer-authored anchors for compact page-header destinations.
    let pageLink (href: string) (label: string) =
        a {
            _href href

            _class
                "inline-flex min-h-8 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-1.5 text-sm font-medium leading-5 text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

            label
        }

    let section (heading: string) (content: HtmlElement) =
        Section.create
            (fveSectionHeader {
                _label heading
                _divider true
            })
            content
        |> Section.render

    let details (fields: (string * string) list) =
        fveDescriptionList {
            for label, value in fields do
                fveDescriptionListItem {
                    _label label
                    value
                }

            _columns DescriptionListColumns.Three
        }

    let reviewStates page =
        (if page = Operations then [ Setup, "Setup" ] else [])
        @ [ Ready, "Populated"; Loading, "Loading"; Empty, "Empty"; Failed, "Error" ]

    let stateContent page query content =
        match query.state with
        | Setup
        | Ready -> content
        | Loading ->
            div {
                _class "p-8"

                let label = "Loading " + (title page).ToLowerInvariant()

                fveLoadingIndicator {
                    _ariaLabel label
                    label
                }
            }
        | Empty ->
            let heading, description, action =
                match page with
                | DependencyGraph ->
                    "No dependencies found", "There are no dependencies for this selection.", "View dependencies"
                | ExecutionDetail ->
                    "No execution selected",
                    "Choose an execution to inspect its timing and logs.",
                    "View latest execution"
                | FinancialReporting ->
                    "No activity for this period",
                    "Choose a period with recorded balances and transactions.",
                    "View recent activity"
                | Messaging ->
                    "No messages in this conversation",
                    "Choose a conversation to read its messages.",
                    "View conversations"
                | Operations -> "No sessions scheduled", "Check the schedule for upcoming sessions.", "View schedule"
                | Scheduling ->
                    "No sessions in this period",
                    "Return to the current period to see upcoming sessions.",
                    "View current schedule"
                | MediaManagement ->
                    "No media in this selection", "Browse the library to choose an asset.", "Browse media"

            fveEmptyState {
                _label heading
                _description description

                fveEmptyStateActions {
                    link (queryUrl (if page = Operations then Scheduling else page) { query with state = Ready }) action
                }
            }
        | Failed ->
            fveNotice {
                _id "workspace-error"
                _label "This view could not be loaded"
                p { "Your selection is preserved. Try loading it again." }
                _color Color.Error
                fveNoticeActions { link (queryUrl page { query with state = Ready }) "Try again" }
            }

    /// Place durable workspace identity above the scrollable destination list.
    let sideNavWorkspace (name: string) =
        div {
            p {
                _class "text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"
                "Workspace"
            }

            p {
                _class "mt-1 truncate text-sm font-semibold"
                name
            }
        }

    let workspace page heading subtitle canvas (actions: HtmlElement list) content =
        let product, workspaceName, destinations =
            match page with
            | DependencyGraph
            | ExecutionDetail ->
                "Relay", "Customer analytics", [ DependencyGraph, "Dependencies"; ExecutionDetail, "Executions" ]
            | FinancialReporting -> "Ledger", "Meier Made", [ FinancialReporting, "Overview" ]
            | Messaging -> "Gather", "Northwind Outdoor", [ Messaging, "Messages" ]
            | _ ->
                "Fieldwork",
                "Northwind Outdoor",
                [ Operations, "Overview"; Scheduling, "Schedule"; MediaManagement, "Media" ]

        let items =
            [ for destination, label in destinations ->
                  fveSideNavItem {
                      _href (url destination)
                      label
                  } ]

        let navigation =
            fveSideNav {
                _id (slug page + "-nav")
                _ariaLabel (product + " navigation")
                fveSideNavHeader { _label product }
                fveSideNavContent { yield! items }
                _value (url page)
                Html._width SideNavWidth.Narrow
                fveSideNavContext { sideNavWorkspace workspaceName }

                fveSideNavFooter {
                    div {
                        _class "flex min-w-0 items-center gap-3"

                        fveAvatar {
                            _ariaLabel "Andy Meier"
                            "AM"
                        }

                        p {
                            _class "truncate text-sm font-medium"
                            "Andy Meier"
                        }
                    }
                }
            }

        let header =
            fvePageHeader {
                _label heading
                _description subtitle

                if not (List.isEmpty actions) then
                    fvePageHeaderActions {
                        div {
                            _id (slug page + "-actions")
                            _class "flex flex-wrap items-center gap-3"

                            for action in actions do
                                action
                        }
                    }
            }

        let topBar =
            fvePageTopBar {
                div {
                    _class "flex min-w-0 flex-1 items-center"

                    fveBreadcrumbs {
                        _id (slug page + "-breadcrumbs")
                        _ariaLabel (product + " breadcrumb")

                        fveBreadcrumbItem {
                            _href (url page)
                            product
                        }

                        fveBreadcrumbItem {
                            _href (url page)
                            heading
                        }
                    }
                }
            }

        let body =
            Page.create header content
            |> Page.withWidth PageWidth.Full
            |> Page.withBodyLayout (
                if canvas then
                    PageBodyLayout.Canvas
                else
                    PageBodyLayout.Padded
            )
            |> Page.render

        let framedBody =
            div {
                _class "flex h-full min-h-0 flex-col"

                div {
                    _class "hidden shrink-0 md:block"
                    topBar
                }

                div {
                    _class "min-h-0 flex-1"
                    body
                }
            }

        (AppShell.create ((slug page + "-shell")) (navigation) (framedBody)
         |> AppShell.withTheme (
             (ComponentsTheme.sky
              |> ComponentsTheme.withDensity Density.Compact
              |> ComponentsTheme.withControlSize Size.Small)
         )
         |> AppShell.withBoundary (AppShellBoundary.Container)
         |> AppShell.asPreview ((product + " workspace"))
         |> AppShell.render)

    type DependencyNode =
        { key: string
          name: string
          operation: string
          status: string
          x: int
          y: int
          run: string }

    let dependencyNodes =
        [ { key = "customers"
            name = "source.customers"
            operation = "Fetch customer records"
            status = "Succeeded"
            x = 1
            y = 2
            run = "run-2401" }
          { key = "orders"
            name = "source.orders"
            operation = "Fetch recent orders"
            status = "Succeeded"
            x = 1
            y = 15
            run = "run-2402" }
          { key = "customer-load"
            name = "warehouse.customers"
            operation = "Load customer dimension"
            status = "Succeeded"
            x = 20
            y = 2
            run = "run-2407" }
          { key = "order-load"
            name = "warehouse.orders"
            operation = "Load order facts"
            status = "Failed"
            x = 20
            y = 15
            run = "run-2408" }
          { key = "revenue"
            name = "reporting.revenue"
            operation = "Build revenue summary"
            status = "Blocked"
            x = 39
            y = 2
            run = "run-2410" }
          { key = "retention"
            name = "reporting.retention"
            operation = "Calculate returning customers"
            status = "Succeeded"
            x = 39
            y = 15
            run = "run-2411" } ]

    let nodeStatus status =
        fveBadge {
            _color (
                match status with
                | "Succeeded" -> Color.Success
                | "Failed" -> Color.Error
                | _ -> Color.Warning
            )

            status
        }

    let selectedNode query =
        dependencyNodes
        |> List.tryFind (fun node -> node.key = query.item || node.run = query.item)
        |> Option.defaultValue dependencyNodes[2]

    let graphContent query =
        let selected = selectedNode query

        let nodeUrl node =
            queryUrl DependencyGraph { query with item = node.key }

        let matches (node: DependencyNode) =
            "('"
            + node.name
            + " "
            + node.status
            + "').toLowerCase().includes($_graphQuery.toLowerCase())"

        div {
            _dataSignals "{_graphQuery:'', _graphZoom:1}"
            _class "flex h-full min-h-0 flex-col"

            div {
                _class
                    "flex shrink-0 flex-wrap items-end gap-1 border-y border-[var(--fve-border)] p-[12px] sm:px-6 sm:py-3 lg:px-8"

                div {
                    _class "min-w-0 basis-full md:flex-1 md:basis-48"

                    fveField {
                        fveLabel {
                            _for "graph-search"
                            "Search dependencies"
                        }

                        fveInput {
                            _id "graph-search"
                            _name "graph-search"
                            _type InputType.Search
                            _dataBind "_graph-query"
                        }
                    }
                }

                fveButton {
                    _ariaLabel "Zoom out"
                    _dataOn ("click", "$_graphZoom = Math.max(0.75, $_graphZoom - 0.25)")

                    span {
                        _ariaHidden true
                        _class "inline-flex shrink-0"

                        fveIcon { _name IconName.Minus; _size Size.Small }
                    }
                }

                fveButton {
                    _ariaLabel "Zoom in"
                    _dataOn ("click", "$_graphZoom = Math.min(1.5, $_graphZoom + 0.25)")

                    span {
                        _ariaHidden true
                        _class "inline-flex shrink-0"

                        fveIcon { _name IconName.Plus; _size Size.Small }
                    }
                }

                fveButton {
                    _ariaLabel "Reset view"
                    _dataOn ("click", "$_graphZoom = 1; $_graphQuery = ''")

                    span {
                        _ariaHidden true
                        _class "inline-flex shrink-0"

                        fveIcon { _name IconName.ArrowPath; _size Size.Small }
                    }
                }

                output {
                    _ariaLabel "Graph zoom"
                    _class "text-sm tabular-nums"
                    _dataText "Math.round($_graphZoom * 100) + '%'"
                    "100%"
                }
            }

            div {
                _class "min-h-0 flex-1 overflow-auto"

                div {
                    _role "region"
                    _ariaLabel "Dependency canvas"
                    _tabindex 0
                    _class "overflow-auto border-b border-[var(--fve-border)] bg-[var(--fve-surface-subtle)]"

                    div {
                        _style "width:54rem;height:25rem"

                        _dataAttr (
                            "style",
                            "'width:' + (54 * $_graphZoom) + 'rem;height:' + (25 * $_graphZoom) + 'rem'"
                        )

                        div {
                            _class "relative"
                            _style "width:54rem;height:25rem;transform-origin:top left"

                            _dataAttr (
                                "style",
                                "'width:54rem;height:25rem;transform-origin:top left;transform:scale(' + $_graphZoom + ')'"
                            )

                            svg {
                                _viewBox "0 0 864 400"
                                _ariaHidden "true"
                                _class "absolute inset-0 h-full w-full"

                                defs {
                                    raw
                                        """<marker id="dependency-arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0 0L8 4L0 8Z" fill="var(--fve-muted-text)"/></marker>"""
                                }

                                for d in
                                    [ "M240 88H318"
                                      "M240 296H318"
                                      "M544 88H622"
                                      "M544 296H578V100H622"
                                      "M544 88H566V296H622" ] do
                                    path {
                                        _d d
                                        _fill "none"
                                        _stroke "var(--fve-muted-text)"
                                        _strokeWidth 1.5
                                        _attr ("marker-end", "url(#dependency-arrow)")
                                    }
                            }

                            for node in dependencyNodes do
                                a {
                                    _href (nodeUrl node)
                                    _ariaLabel (node.name + ", " + node.status)

                                    if node.key = selected.key then
                                        _ariaCurrent "true"

                                    _class
                                        "absolute grid justify-items-start gap-2 data-[match=false]:opacity-30 rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 shadow-sm outline-none hover:ring-2 hover:ring-[var(--fve-brand-ring)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)] aria-current:ring-2 aria-current:ring-[var(--fve-brand-ring)]"

                                    _style $"left:{node.x}rem;top:{node.y}rem;width:14rem"
                                    _dataAttr ("data-match", matches node)

                                    strong {
                                        _class "break-all font-mono text-sm"
                                        node.name
                                    }

                                    span {
                                        _class "text-xs text-[var(--fve-muted-text)]"
                                        node.operation
                                    }

                                    nodeStatus node.status
                                }
                        }
                    }
                }

                div {
                    _class "grid items-start gap-6 p-4 lg:grid-cols-2 sm:p-6 lg:p-8"

                    section
                        "Selected dependency"
                        (div {
                            _class "grid justify-items-start gap-4"

                            h2 {
                                _class "break-all font-mono text-base font-semibold"
                                selected.name
                            }

                            details
                                [ "Operation", selected.operation
                                  "Latest execution", selected.run
                                  "Environment", "Production" ]

                            nodeStatus selected.status

                            link
                                (queryUrl
                                    ExecutionDetail
                                    { defaultQuery with
                                        item = selected.run })
                                "Inspect execution"
                        })

                    section
                        "Dependencies"
                        (ul {
                            _class "divide-y divide-[var(--fve-border)]"

                            for node in dependencyNodes do
                                li {
                                    _dataShow (matches node)
                                    _class "flex flex-wrap items-center justify-between gap-2 py-3 text-sm"
                                    link (nodeUrl node) node.name
                                    nodeStatus node.status
                                }

                            li {
                                _dataShow
                                    "!['source.customers succeeded','source.orders succeeded','warehouse.customers succeeded','warehouse.orders failed','reporting.revenue blocked','reporting.retention succeeded'].some(value => value.includes($_graphQuery.toLowerCase()))"

                                _style "display:none"
                                _role "status"
                                _class "py-3 text-sm"
                                "No dependencies match your search."
                            }
                        })
                }
            }
        }

    let dependencyGraph query =
        workspace
            DependencyGraph
            "Dependency graph"
            "Customer analytics · 6 dependencies"
            true
            []
            (stateContent DependencyGraph query (graphContent query))

    type TraceSpan =
        { id: string
          operation: string
          service: string
          start: int
          duration: int
          depth: int }

    let traceSpans =
        [ { id = "worker"
            operation = "Execute dependency"
            service = "worker-01"
            start = 0
            duration = 1240
            depth = 0 }
          { id = "auth"
            operation = "Check credentials"
            service = "identity"
            start = 20
            duration = 90
            depth = 1 }
          { id = "fetch"
            operation = "GET /customers"
            service = "source-api"
            start = 140
            duration = 460
            depth = 1 }
          { id = "parse"
            operation = "Decode 1,842 records"
            service = "worker-01"
            start = 600
            duration = 120
            depth = 1 }
          { id = "write"
            operation = "INSERT customer_dimension"
            service = "postgres"
            start = 740
            duration = 420
            depth = 1 }
          { id = "commit"
            operation = "Commit and publish"
            service = "worker-01"
            start = 1160
            duration = 80
            depth = 1 } ]

    let spansFor node =
        let order = node.name.Contains("orders", StringComparison.Ordinal)

        traceSpans
        |> List.map (fun span ->
            { span with
                operation =
                    match span.id with
                    | "fetch" when order -> "GET /orders"
                    | "parse" when order -> "Decode 962 order records"
                    | "write" when order -> "INSERT order_facts"
                    | "commit" when node.status = "Failed" -> "Roll back transaction"
                    | _ -> span.operation })

    let executionContent query =
        let node = selectedNode query
        let spans = spansFor node

        let selectedSpan =
            spans
            |> List.tryFind (fun span -> span.id = query.view)
            |> Option.defaultValue spans[4]

        let failed = node.status = "Failed"

        div {
            _class "grid gap-6"

            fveDescriptionList {
                fveDescriptionListItem {
                    _label "Duration"
                    if node.status = "Blocked" then "—" else "1.24 s"
                }

                fveDescriptionListItem {
                    _label "Started"

                    if node.status = "Blocked" then
                        "Not started"
                    else
                        "17 Sep 2026, 09:42:18 UTC"
                }
            }

            Html.details {
                summary {
                    _class "cursor-pointer text-sm font-medium text-[var(--fve-brand-text)]"
                    "Execution metadata"
                }

                div {
                    _class "mt-3"

                    details
                        [ "Dependency", node.name
                          "Worker", "warehouse-worker-01"
                          "Attempt", (if node.status = "Blocked" then "0" else "1") ]
                }
            }

            div {
                _class "flex flex-wrap items-center gap-3"
                nodeStatus node.status
                link (queryUrl DependencyGraph { defaultQuery with item = node.key }) "View in dependency graph"
            }

            if node.status = "Blocked" then
                fveNotice {
                    _id "blocked-run"
                    _label "Waiting for an upstream dependency"
                    p { "warehouse.orders must succeed before this execution can start." }
                    _color Color.Warning

                    fveNoticeActions {
                        link
                            (queryUrl ExecutionDetail { defaultQuery with item = "run-2408" })
                            "Inspect failed dependency"
                    }
                }
            else
                section
                    "Trace waterfall"
                    (div {
                        _class "grid gap-3"

                        p {
                            _class "text-sm text-[var(--fve-muted-text)]"
                            "Select a span to inspect its timing and service. All timings are relative to execution start."
                        }

                        div {
                            _role "region"
                            _ariaLabel "Trace timings"
                            _tabindex 0
                            _class "overflow-x-auto"

                            fveTable {
                                _ariaLabel "Execution spans"
                                _density Density.Compact

                                fveTableHeader {
                                    fveTableRow {
                                        fveTableHead { "Operation" }
                                        fveTableHead { "Service" }

                                        fveTableHead {
                                            _class "text-right"
                                            "Start"
                                        }

                                        fveTableHead {
                                            _class "text-right"
                                            "Duration"
                                        }

                                        fveTableHead { "0 — 620 — 1,240 ms" }
                                    }
                                }

                                fveTableBody {
                                    for item in spans do
                                        fveTableRow {
                                            fveTableHead {
                                                _scope "row"
                                                _label "Operation"

                                                div {
                                                    _style $"padding-left:{item.depth}rem"

                                                    link
                                                        (queryUrl ExecutionDetail { query with view = item.id })
                                                        item.operation
                                                }
                                            }

                                            fveTableCell {
                                                _label "Service"
                                                item.service
                                            }

                                            fveTableCell {
                                                _label "Start"
                                                _class "text-right"
                                                $"{item.start} ms"
                                            }

                                            fveTableCell {
                                                _label "Duration"
                                                _class "text-right"
                                                $"{item.duration} ms"
                                            }

                                            fveTableCell {
                                                _label "0 — 620 — 1,240 ms"

                                                div {
                                                    _ariaHidden true

                                                    _class
                                                        "relative h-6 min-w-64 rounded bg-[var(--fve-surface-subtle)]"

                                                    div {
                                                        _class (
                                                            if failed && item.id = "write" then
                                                                "absolute top-1 h-4 rounded bg-[var(--fve-critical-text)]"
                                                            else
                                                                "absolute top-1 h-4 rounded bg-[var(--fve-brand-solid)]"
                                                        )

                                                        _style (
                                                            sprintf
                                                                "left:%.2f%%;width:%.2f%%"
                                                                (float item.start / 12.4)
                                                                (float item.duration / 12.4)
                                                        )
                                                    }
                                                }
                                            }
                                        }
                                }
                            }
                        }
                    })

                section
                    ("Span · " + selectedSpan.operation)
                    (details
                        [ "Service", selectedSpan.service
                          "Start offset", $"{selectedSpan.start} ms"
                          "Duration", $"{selectedSpan.duration} ms"
                          "Span ID", selectedSpan.id
                          "Result",
                          (if failed && selectedSpan.id = "write" then
                               "Failed · unique constraint"
                           else
                               "Succeeded")
                          "Execution", node.run ])

                section
                    "Correlated logs"
                    (ul {
                        _class "divide-y divide-[var(--fve-border)] font-mono text-xs"

                        for time, level, message in
                            [ "09:42:18.000", "INFO", "Execution started on warehouse-worker-01"
                              "09:42:18.600",
                              "INFO",
                              (if failed then
                                   "Fetched 962 order records"
                               else
                                   "Fetched 1,842 source records")
                              "09:42:19.160",
                              (if failed then "ERROR" else "INFO"),
                              (if failed then
                                   "Duplicate order key: ORD-1042. Transaction rolled back."
                               else
                                   "Committed 1,842 records to customer_dimension") ] do
                            li {
                                _class "grid gap-2 py-3 sm:grid-cols-[8rem_4rem_1fr]"
                                span { time }
                                strong { level }

                                span {
                                    _class "break-words"
                                    message
                                }
                            }
                    })
        }

    let executionDetail query =
        let node = selectedNode query

        workspace
            ExecutionDetail
            node.run
            (node.name + " · Production")
            false
            [ pageLink (queryUrl DependencyGraph { defaultQuery with item = node.key }) "View dependency" ]
            (stateContent ExecutionDetail query (executionContent query))

    type BalancePoint =
        { month: string
          actual: decimal
          planned: decimal }

    let balances =
        [ { month = "Apr"
            actual = 24200M
            planned = 24200M }
          { month = "May"
            actual = 28100M
            planned = 27000M }
          { month = "Jun"
            actual = 26400M
            planned = 30000M }
          { month = "Jul"
            actual = 33200M
            planned = 33000M }
          { month = "Aug"
            actual = 35800M
            planned = 36000M }
          { month = "Sep"
            actual = 38442.11M
            planned = 40000M } ]

    let currency (value: decimal) =
        value.ToString("C2", Globalization.CultureInfo.GetCultureInfo("en-US"))

    let balanceChart query =
        let points =
            if query.range = "3m" then
                balances |> List.skip 3
            else
                balances

        let xPercent index =
            float index * 100. / float (points.Length - 1)

        let yPercent amount = 100. - float amount / 50000. * 100.

        let chartX index = xPercent index * 6.

        let chartY amount = yPercent amount * 1.8

        let linePoints select =
            points
            |> List.mapi (fun index point -> sprintf "%.2f,%.2f" (chartX index) (chartY (select point)))
            |> String.concat " "

        div {
            _class "grid gap-4"

            div {
                _class "flex flex-wrap items-center justify-between gap-3"

                div {
                    h2 {
                        _class "text-base font-semibold"
                        "Operating checking"
                    }

                    p {
                        _class "mt-1 text-sm text-[var(--fve-muted-text)]"

                        if query.range = "3m" then
                            "Closing balance · USD · July–September 2026"
                        else
                            "Closing balance · USD · April–September 2026"
                    }
                }

                nav {
                    _ariaLabel "Balance period"
                    _class "flex gap-3 text-sm"

                    for value, label in [ "3m", "3 months"; "6m", "6 months" ] do
                        a {
                            _href (queryUrl FinancialReporting { query with range = value })

                            if (if query.range = "3m" then "3m" else "6m") = value then
                                _ariaCurrent "page"

                            _class
                                "inline-flex min-h-[var(--fve-control-min-height)] items-center rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] ring-1 ring-[var(--fve-border)] aria-[current=page]:bg-[var(--fve-brand-subtle)] aria-[current=page]:text-[var(--fve-brand-text)]"

                            label
                        }
                }
            }

            div {
                _class "flex flex-wrap gap-5 text-xs text-[var(--fve-muted-text)]"

                span {
                    _class "flex items-center gap-2"

                    span {
                        _ariaHidden "true"
                        _class "h-1 w-5 rounded bg-[var(--fve-brand-solid)]"
                    }

                    "Actual"
                }

                span {
                    _class "flex items-center gap-2"

                    span {
                        _ariaHidden "true"
                        _class "w-5 border-t-2 border-dashed border-[var(--fve-muted-text)]"
                    }

                    "Plan"
                }
            }

            div {
                _role "region"
                _ariaLabel "Account balance chart"
                _tabindex 0
                _class "overflow-x-auto"

                div {
                    _class "grid min-w-[36rem] grid-cols-[3.5rem_minmax(0,1fr)] gap-x-3"

                    div {
                        _ariaHidden true

                        _class
                            "flex h-52 flex-col justify-between text-right text-xs leading-none text-[var(--fve-muted-text)]"

                        for value in [ 50000M; 40000M; 30000M; 20000M; 10000M; 0M ] do
                            span { "$" + string (value / 1000M) + "k" }
                    }

                    div {
                        _class "relative h-52"

                        svg {
                            _viewBox "0 0 600 180"
                            _attr ("preserveAspectRatio", "none")
                            _role "img"

                            _ariaLabel
                                "Operating checking: actual and planned closing balances in USD. Values are available in the data table below."

                            _class "absolute inset-0 h-full w-full overflow-visible"

                            for value in [ 0M; 10000M; 20000M; 30000M; 40000M; 50000M ] do
                                line {
                                    _x1 0
                                    _x2 600
                                    _y1 (chartY value)
                                    _y2 (chartY value)
                                    _stroke "var(--fve-border)"
                                    _vectorEffect "non-scaling-stroke"
                                }

                            polyline {
                                _points (linePoints _.planned)
                                _fill "none"
                                _stroke "var(--fve-muted-text)"
                                _strokeWidth 2
                                _attr ("stroke-dasharray", "6 5")
                                _vectorEffect "non-scaling-stroke"
                            }

                            polyline {
                                _points (linePoints _.actual)
                                _fill "none"
                                _stroke "var(--fve-brand-solid)"
                                _strokeWidth 3
                                _vectorEffect "non-scaling-stroke"
                            }
                        }

                        for index, point in List.indexed points do
                            span {
                                _ariaHidden true

                                _class
                                    "absolute size-3 -translate-x-1/2 -translate-y-1/2 rounded-full bg-[var(--fve-brand-solid)]"

                                _style (sprintf "left:%.2f%%;top:%.2f%%" (xPercent index) (yPercent point.actual))
                            }
                    }

                    span { _ariaHidden true }

                    div {
                        _ariaHidden true
                        _class "flex justify-between pt-3 text-xs leading-none text-[var(--fve-muted-text)]"

                        for point in points do
                            span { point.month }
                    }
                }
            }

            Html.details {
                summary {
                    _class "cursor-pointer text-sm font-medium text-[var(--fve-brand-text)]"
                    "View balance data"
                }

                fveTable {
                    _ariaLabel "Monthly closing balances (USD)"
                    _mobileLayout MobileLayout.Records

                    fveTableHeader {
                        fveTableRow {
                            fveTableHead { "Month" }

                            fveTableHead {
                                _class "text-right"
                                "Actual"
                            }

                            fveTableHead {
                                _class "text-right"
                                "Plan"
                            }
                        }
                    }

                    fveTableBody {
                        for point in points do
                            fveTableRow {
                                fveTableHead {
                                    _scope "row"
                                    _mobile Mobile.Primary
                                    point.month
                                }

                                fveTableCell {
                                    _label "Actual"
                                    _class "text-right"
                                    currency point.actual
                                }

                                fveTableCell {
                                    _label "Plan"
                                    _class "text-right"
                                    currency point.planned
                                }
                            }
                    }
                }
            }
        }

    let financialReporting query =
        let content =
            div {
                _class "grid gap-8"

                div {
                    _class
                        "grid grid-cols-[repeat(auto-fit,minmax(min(100%,10rem),1fr))] gap-5 border-b border-[var(--fve-border)] pb-6"

                    for label, value, description in
                        [ "Checking balance", "$38,442.11", "As of September 17"
                          "Change since April", "+$14,242.11", "58.9% increase"
                          "Against September plan", "−$1,557.89", "3.9% below plan" ] do
                        fveMetric {
                            _label label
                            _description description
                            value
                        }
                }

                balanceChart query

                section
                    "Recent transactions"
                    (fveTable {
                        _ariaLabel "Checking transactions"
                        _mobileLayout MobileLayout.Records

                        fveTableHeader {
                            fveTableRow {
                                fveTableHead { "Date" }
                                fveTableHead { "Description" }

                                fveTableHead {
                                    _class "text-right"
                                    "Amount"
                                }
                            }
                        }

                        fveTableBody {
                            for date, name, id, amount in
                                [ "Jul 28", "Northwind payment", 201, 4800M
                                  "Jul 27", "Cloud hosting", 202, 386.42M
                                  "Jul 26", "ACH withdrawal", 203, 1240M ] do
                                fveTableRow {
                                    fveTableCell {
                                        _label "Date"
                                        date
                                    }

                                    fveTableHead {
                                        _scope "row"
                                        _mobile Mobile.Primary

                                        link
                                            ("/components/page-examples/account-management?destination=ledger-transaction-"
                                             + string id)
                                            name
                                    }

                                    fveTableCell {
                                        _label "Amount"
                                        _class "text-right"
                                        currency amount
                                    }
                                }
                        }
                    })
            }

        workspace
            FinancialReporting
            "Financial overview"
            "Operating checking · USD"
            false
            [ pageLink "/components/page-examples/account-management?destination=ledger-account-2048" "View account" ]
            (stateContent FinancialReporting query content)

    type Conversation =
        { id: string
          name: string
          initials: string
          participants: string
          preview: string }

    type Message =
        { id: string
          conversation: string
          author: string
          body: string
          time: string
          outgoing: bool }

    let conversations =
        [ { id = "beach"
            name = "Beach weekend"
            initials = "BW"
            participants = "Andy, Jordan, Kella"
            preview = "I found a place near the beach." }
          { id = "studio"
            name = "Studio team"
            initials = "ST"
            participants = "Andy, Maya"
            preview = "The new photographs look great." }
          { id = "jordan"
            name = "Jordan Lee"
            initials = "JL"
            participants = "Jordan Lee"
            preview = "See you at 10:30 tomorrow." } ]

    let initialMessages =
        [ { id = "m1"
            conversation = "beach"
            author = "Jordan"
            body = "Are we still thinking Saturday for the coast? The weather looks good."
            time = "09:12"
            outgoing = false }
          { id = "m2"
            conversation = "beach"
            author = "Andy Meier"
            body = "Yes! Let's leave after breakfast. I'll bring the picnic."
            time = "09:14"
            outgoing = true }
          { id = "m3"
            conversation = "beach"
            author = "Kella"
            body = "I found a place near the beach. There's parking and a short trail down to the water."
            time = "09:18"
            outgoing = false }
          { id = "m4"
            conversation = "beach"
            author = "Andy Meier"
            body = "That sounds perfect. Could you send us the meeting point?"
            time = "09:20"
            outgoing = true }
          { id = "m5"
            conversation = "studio"
            author = "Maya"
            body = "The new photographs look great. Can you send the final selection this afternoon?"
            time = "08:45"
            outgoing = false }
          { id = "m6"
            conversation = "jordan"
            author = "Jordan"
            body = "See you at 10:30 tomorrow. I'll meet you by the entrance."
            time = "Yesterday"
            outgoing = false } ]

    let private sentMessage =
        { id = "message-sent"
          conversation = "beach"
          author = "Andy Meier"
          body = "Meet at the east entrance 15 minutes before we leave."
          time = "Just now"
          outgoing = true }

    let currentConversation query =
        conversations
        |> List.tryFind (fun item -> item.id = query.item)
        |> Option.defaultValue conversations.Head

    let messagesFor query =
        if query.view = "sent" then
            initialMessages
            @ [ { sentMessage with
                    conversation = (currentConversation query).id } ]
        else
            initialMessages

    let messageHistory conversation messages =
        div {
            _id "message-history"
            _tabindex 0
            _role "log"
            _ariaLabel (conversation.name + " message history")
            _ariaLive "polite"
            _class "min-h-0 flex-1 space-y-5 overflow-y-auto p-4 sm:p-6"

            p {
                _class "text-center text-xs text-[var(--fve-muted-text)]"
                "Thursday, September 17"
            }

            for message in messages |> List.filter (fun message -> message.conversation = conversation.id) do
                fveMessage {
                    _id ("message-" + message.id)
                    _label message.author

                    _side (
                        if message.outgoing then
                            MessageSide.Sender
                        else
                            MessageSide.Receiver
                    )

                    _dataInit "el.parentElement.scrollTop = el.parentElement.scrollHeight"

                    p {
                        _class "whitespace-pre-wrap break-words"
                        message.body
                    }

                    fveMessageMetadata { message.author + " · " + message.time }
                }
        }

    let conversationPanel query messages draft error =
        let conversation = currentConversation query

        div {
            _id "conversation-panel"
            _class "flex h-full min-h-0 flex-col"

            messageHistory conversation messages

            form {
                _method "post"
                _action (url Messaging + "/send?item=" + conversation.id)

                _class
                    "flex shrink-0 flex-wrap items-end gap-2 border-t border-[var(--fve-border)] bg-[var(--fve-surface)] p-[12px]"

                let field =
                    fveField {
                        fveLabel {
                            _for "message-compose"
                            _class "sr-only"
                            "Message"
                        }

                        fveTextarea {
                            _id "message-compose"
                            _name "message"
                            _rows 1
                            _value draft
                            _required true
                            _maxlength 2000
                            _placeholder "Write a message…"
                            _ariaInvalid (Option.isSome error)

                            if Option.isSome error then
                                _ariaDescribedby "message-compose-validation"
                        }

                        match error with
                        | Some message ->
                            fveFieldError {
                                _id "message-compose-validation"
                                Html.text message
                            }
                        | None -> ()
                    }

                div {
                    _class "min-w-0 flex-[1_1_12rem]"
                    field
                }

                fveButton {
                    _color Color.Primary
                    _variant ButtonVariant.Solid
                    _type "submit"
                    "Send message"
                }

                if query.view = "sent" then
                    p {
                        _role "status"
                        _class "w-full text-xs text-[var(--fve-muted-text)]"
                        "Message added."
                    }
            }
        }

    let messaging query =
        let current = currentConversation query
        let messages = messagesFor query

        let error =
            match query.view with
            | "message-empty" -> Some "Enter a message before sending."
            | "message-too-long" -> Some "Keep your message under 2,000 characters."
            | _ -> None

        let destination id =
            queryUrl Messaging { defaultQuery with item = id }

        let navigation =
            fveSideNav {
                _id "gather-conversations"
                _ariaLabel "Conversations"
                fveSideNavHeader { _label "Gather" }

                fveSideNavContent {
                    fveSideNavSection {
                        _label "Messages"

                        for conversation in conversations do
                            fveSideNavItem {
                                _href (destination conversation.id)
                                conversation.name
                            }
                    }
                }

                _value (destination current.id)
                Html._width SideNavWidth.Wide
                fveSideNavContext { sideNavWorkspace "Northwind Outdoor" }

                fveSideNavFooter {
                    div {
                        _class "flex items-center gap-3"

                        fveAvatar {
                            _ariaLabel "Andy Meier"
                            "AM"
                        }

                        span {
                            _class "text-sm font-medium"
                            "Andy Meier"
                        }
                    }
                }
            }

        let content =
            Html.section {
                _ariaLabel "Current conversation"
                _class "h-full min-h-0"
                conversationPanel query messages "" error
            }

        let topBar =
            fvePageTopBar {
                fveBreadcrumbs {
                    _id "gather-breadcrumbs"
                    _ariaLabel "Gather breadcrumb"

                    fveBreadcrumbItem {
                        _href (destination "beach")
                        "Gather"
                    }

                    fveBreadcrumbItem {
                        _href (destination current.id)
                        "Messages"
                    }
                }
            }

        let page =
            Page.create
                (fvePageHeader {
                    _label current.name
                    _description current.participants
                })
                (stateContent Messaging query content)
            |> Page.withBodyLayout PageBodyLayout.Canvas
            |> Page.render

        let framedPage =
            div {
                _class "flex h-full min-h-0 flex-col"

                div {
                    _class "hidden shrink-0 md:block"
                    topBar
                }

                div {
                    _class "min-h-0 flex-1"
                    page
                }
            }

        (AppShell.create ("messaging-shell") (navigation) (framedPage)
         |> AppShell.withTheme (
             (ComponentsTheme.sky
              |> ComponentsTheme.withDensity Density.Compact
              |> ComponentsTheme.withControlSize Size.Small)
         )
         |> AppShell.withBoundary (AppShellBoundary.Container)
         |> AppShell.asPreview ("Gather workspace")
         |> AppShell.render)

    type ScheduledEvent =
        { id: string
          name: string
          date: DateOnly
          time: string
          start: TimeOnly
          finish: TimeOnly
          people: string
          kind: string
          photo: string }

    let scheduledEvents =
        [ { id = "lesson-201"
            name = "Coastal trail lesson"
            date = DateOnly(2026, 9, 17)
            time = "10:00–11:30 AM"
            start = TimeOnly(10, 0)
            finish = TimeOnly(11, 30)
            people = "Maya and Sam · Instructor: Andy Meier"
            kind = "Lesson"
            photo = "photo-303" }
          { id = "camp-017"
            name = "Beginner riding camp"
            date = DateOnly(2026, 9, 17)
            time = "10:30 AM–12:00 PM"
            start = TimeOnly(10, 30)
            finish = TimeOnly(12, 0)
            people = "3 riders · Instructor: Jordan"
            kind = "Camp"
            photo = "photo-301" }
          { id = "lesson-202"
            name = "Cornering fundamentals"
            date = DateOnly(2026, 9, 18)
            time = "9:00–10:00 AM"
            start = TimeOnly(9, 0)
            finish = TimeOnly(10, 0)
            people = "Riley · Instructor: Andy Meier"
            kind = "Lesson"
            photo = "photo-302" }
          { id = "ride-019"
            name = "Open track practice"
            date = DateOnly(2026, 9, 19)
            time = "1:00–3:00 PM"
            start = TimeOnly(13, 0)
            finish = TimeOnly(15, 0)
            people = "6 riders · Supervisor: Jordan"
            kind = "Practice"
            photo = "photo-304" }
          { id = "return-020"
            name = "Equipment return"
            date = DateOnly(2026, 9, 24)
            time = "4:00–4:30 PM"
            start = TimeOnly(16, 0)
            finish = TimeOnly(16, 30)
            people = "Equipment team · Andy Meier"
            kind = "Equipment"
            photo = "photo-304" } ]

    let eventUrl event =
        queryUrl Scheduling { defaultQuery with item = event.id }

    let eventTable (events: ScheduledEvent list) =
        fveTable {
            _ariaLabel "Upcoming events"
            _mobileLayout MobileLayout.Records
            _density Density.Compact

            fveTableHeader {
                fveTableRow {
                    fveTableHead { "Event" }
                    fveTableHead { "Date" }
                    fveTableHead { "Time" }
                    fveTableHead { "People" }
                }
            }

            fveTableBody {
                for event in events do
                    fveTableRow {
                        fveTableHead {
                            _scope "row"
                            _mobile Mobile.Primary
                            link (eventUrl event) event.name
                        }

                        fveTableCell {
                            _label "Date"
                            event.date.ToString("MMM d", Globalization.CultureInfo.InvariantCulture)
                        }

                        fveTableCell {
                            _label "Time"
                            event.time
                        }

                        fveTableCell {
                            _label "People"
                            event.people
                        }
                    }
            }
        }

    let operations query =
        let content =
            div {
                _class "grid gap-8"

                div {
                    _class
                        "order-2 grid grid-cols-[repeat(auto-fit,minmax(min(100%,6.5rem),1fr))] gap-4 border-b border-[var(--fve-border)] pb-4 md:order-none"

                    for label, value, detail in
                        [ "Today's sessions", "2", "One lesson · One camp"
                          "Riders expected", "5", "September 17"
                          "Upcoming sessions", "4", "Through September 19" ] do
                        fveMetric {
                            _label label
                            _description detail
                            value
                        }
                }

                div {
                    _class "order-3 md:order-none"

                    section
                        "Today's schedule"
                        (eventTable (scheduledEvents |> List.filter (fun event -> event.date = DateOnly(2026, 9, 17))))
                }

                div {
                    _class "order-1 grid gap-8 md:order-none lg:grid-cols-2"

                    section
                        "Next up"
                        (div {
                            _class "grid gap-4"

                            h2 {
                                _class "text-lg font-semibold"
                                "Coastal trail lesson"
                            }

                            p {
                                _class "text-sm text-[var(--fve-muted-text)]"
                                "10:00 AM · Maya and Sam are arriving for their first trail session."
                            }

                            link (eventUrl scheduledEvents.Head) "Review lesson details"
                        })
                }

                if query.state = Setup then
                    fveFloatingPanel {
                        _id "fieldwork-workspace-guide"
                        _label "Finish your workspace"

                        ol {
                            _class "m-0 grid list-none gap-4 p-0 text-sm"

                            li {
                                p {
                                    _class "font-medium"
                                    "Review this week's schedule"
                                }

                                link (url Scheduling) "Open schedule"
                            }

                            li {
                                p {
                                    _class "font-medium"
                                    "Prepare your session assets"
                                }

                                p {
                                    _class "text-[var(--fve-muted-text)]"
                                    "Review descriptions before sharing a collection."
                                }

                                link (url MediaManagement) "Open photos"
                            }
                        }
                    }
            }

        workspace
            Operations
            "Good morning, Andy"
            "Thursday, September 17"
            false
            [ a {
                  _href (url Scheduling)

                  _class
                      "inline-flex min-h-8 items-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] px-3 text-sm font-medium text-white hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                  "View schedule"
              } ]
            (stateContent Operations query content)

    let scheduling query =
        let event = scheduledEvents |> List.tryFind (fun event -> event.id = query.item)

        let content, heading =
            match event with
            | Some event ->
                div {
                    _class "grid gap-8"

                    details
                        [ "Date", event.date.ToString("MMMM d, yyyy", Globalization.CultureInfo.InvariantCulture)
                          "Time", event.time
                          "Type", event.kind
                          "Participants", event.people
                          "Location", "Coastal training grounds"
                          "Status", "Confirmed" ]

                    section
                        "Session preparation"
                        (ul {
                            _class "list-disc space-y-2 pl-5 text-sm"
                            li { "Check helmets and protective equipment before riding." }
                            li { "Meet at the east entrance 15 minutes before the session." }
                            li { "Water and a shaded rest area are available beside the track." }
                        })

                    section
                        "Session assets"
                        (link (queryUrl MediaManagement { defaultQuery with item = event.photo }) "View matching asset")

                    link (url Scheduling) "Back to schedule"
                },
                event.name
            | None ->
                let view =
                    match query.view with
                    | "day"
                    | "week"
                    | "year" -> query.view
                    | _ -> "month"

                let today = DateOnly(2026, 9, 17)

                let offset =
                    match Int32.TryParse query.range with
                    | true, value -> Math.Clamp(value, -366, 366)
                    | _ -> 0

                let start = today.AddDays offset

                let startValue =
                    start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)

                let step direction =
                    let target =
                        match view with
                        | "day" -> start.AddDays direction
                        | "week" -> start.AddDays(direction * 7)
                        | "year" -> start.AddYears direction
                        | _ -> start.AddMonths direction

                    target.DayNumber - today.DayNumber

                let events =
                    [ for event in scheduledEvents ->
                          CalendarEvent.create event.id event.name event.date (eventUrl event)
                          |> CalendarEvent.withTime event.start event.finish
                          |> CalendarEvent.withDetail event.people ]

                let dateDestination (date: DateOnly) =
                    queryUrl
                        Scheduling
                        { query with
                            view = "day"
                            range = string (date.DayNumber - today.DayNumber) }

                let todayDestination = queryUrl Scheduling { query with range = "0" }
                let previous = queryUrl Scheduling { query with range = string (step -1) }
                let next = queryUrl Scheduling { query with range = string (step 1) }

                let navigation calendarView =
                    [ fveCalendarToday {
                          _date today
                          _href todayDestination
                      }
                      if step -1 >= -366 then
                          fveCalendarPrevious { _href previous }
                      if step 1 <= 366 then
                          fveCalendarNext { _href next }
                      for day in Calendar.dates calendarView start do
                          fveCalendarDate {
                              _date day
                              _href (dateDestination day)
                          } ]

                let calendar =
                    match view with
                    | "day" ->
                        fveDayCalendar {
                            _label "Daily sessions"
                            _date start
                            _selectedDate start
                            yield! events
                            yield! navigation CalendarView.Day
                        }
                    | "week" ->
                        fveWeekCalendar {
                            _label "Weekly sessions"
                            _date start
                            _selectedDate start
                            yield! events
                            yield! navigation CalendarView.Week
                        }
                    | "year" ->
                        fveYearCalendar {
                            _label "Annual sessions"
                            _date start
                            _selectedDate start
                            yield! events
                            yield! navigation CalendarView.Year
                        }
                    | _ ->
                        fveMonthCalendar {
                            _label "Monthly sessions"
                            _date start
                            _selectedDate start
                            yield! events
                            yield! navigation CalendarView.Month
                        }

                Html.section {
                    _ariaLabel "Schedule"
                    _attr ("data-view", view)
                    _class "grid min-w-0 gap-4"

                    nav {
                        _ariaLabel "Schedule calendar view"
                        _class "flex flex-wrap gap-1"

                        for value, label in [ "day", "Day"; "week", "Week"; "month", "Month"; "year", "Year" ] do
                            a {
                                _href (queryUrl Scheduling { query with view = value })

                                if view = value then
                                    _ariaCurrent "page"

                                _class
                                    "inline-flex min-h-8 items-center rounded-[var(--fve-radius-control)] px-3 py-1 text-sm font-medium no-underline hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] aria-[current=page]:bg-[var(--fve-brand-subtle)] aria-[current=page]:text-[var(--fve-brand-text)]"

                                label
                            }
                    }

                    div {
                        _id ("scheduling-calendar-" + view + "-" + startValue)

                        _dataInit
                            $"el.fveCalendarObserver?.disconnect(); const cell = el.querySelector('[data-date=\"{startValue}\"]'); const region = cell?.closest('[role=region]'); if (cell && region) {{ const observer = new ResizeObserver(() => {{ if (!el.isConnected) {{ observer.disconnect(); return }}; if (!region.clientWidth) return; const c = cell.getBoundingClientRect(), r = region.getBoundingClientRect(); const hours = region.querySelector('.fve-calendar-hours')?.clientWidth || 0; region.scrollLeft += c.left - r.left - Math.max(hours, (region.clientWidth - c.width) / 2) }}); el.fveCalendarObserver = observer; observer.observe(region) }}"

                        calendar
                    }
                },
                "Schedule"

        workspace Scheduling heading "Eastern time" false [] (stateContent Scheduling query content)

    type WorkspacePhoto =
        { id: string
          name: string
          source: string
          alt: string
          eventId: string }

    let initialPhotos =
        [ { id = "photo-301"
            name = "Social card"
            source = "/social-card.png"
            alt = "FSharp.ViewEngine — Typed HTML views for F#"
            eventId = "camp-017" }
          { id = "photo-302"
            name = "App icon"
            source = "/android-chrome-512x512.png"
            alt = "FSharp.ViewEngine blue code-mark icon"
            eventId = "lesson-202" }
          { id = "photo-303"
            name = "Touch icon"
            source = "/apple-touch-icon.png"
            alt = "FSharp.ViewEngine blue code-mark touch icon"
            eventId = "lesson-201" }
          { id = "photo-304"
            name = "Browser icon"
            source = "/favicon-32x32.png"
            alt = "FSharp.ViewEngine blue code-mark browser icon"
            eventId = "ride-019" }
          { id = "photo-305"
            name = "Small favicon"
            source = "/favicon-16x16.png"
            alt = "FSharp.ViewEngine blue code-mark favicon"
            eventId = "camp-017" }
          { id = "photo-306"
            name = "Vector logo"
            source = "/logo.svg"
            alt = "FSharp.ViewEngine code-mark logo"
            eventId = "lesson-201" } ]

    let uploadedPhoto =
        { id = "photo-uploaded"
          name = "Uploaded fixture preview"
          source = "/images/page-examples/violet.png"
          alt = "Solid violet background used for the upload success fixture"
          eventId = "" }

    let photosFor query =
        if query.item = uploadedPhoto.id || query.view = "uploaded" then
            initialPhotos @ [ uploadedPhoto ]
        else
            initialPhotos

    let mediaEditor (photo: WorkspacePhoto) (feedback: string) =
        div {
            _id "media-editor"
            _class "grid gap-6 lg:grid-cols-2"

            figure {
                img {
                    _src photo.source
                    _alt photo.alt
                    _class "aspect-video w-full rounded-xl object-cover"
                }

                figcaption {
                    _class "mt-3 text-xs text-[var(--fve-muted-text)]"
                    photo.name
                }

            }

            form {
                _method "post"
                _action (url MediaManagement + "/save?item=" + photo.id)
                _class "grid content-start gap-5"

                fveField {
                    fveLabel {
                        _for "name"
                        "Asset name"
                    }

                    fveInput {
                        _id "name"
                        _name "name"
                        _value photo.name
                        _required true
                        _maxlength 120
                    }
                }

                fveField {
                    fveLabel {
                        _for "alt"
                        "Image description"
                    }

                    fveTextarea {
                        _id "alt"
                        _name "alt"
                        _value photo.alt
                        _rows 3
                        _required true
                        _maxlength 500
                        _ariaDescribedby (String.concat " " [ ("alt") + "-description" ])
                    }

                    fveFieldDescription {
                        _id ("alt" + "-description")
                        "Describe the image for people who cannot see it."
                    }
                }

                div {
                    _role "status"
                    _class "text-sm text-[var(--fve-muted-text)]"
                    feedback
                }

                div {
                    _class "flex flex-wrap items-center gap-3"

                    fveButton {
                        _color Color.Primary
                        _variant ButtonVariant.Solid
                        _type "submit"
                        "Save changes"
                    }

                    link (url MediaManagement) "Back to media"
                }

                match scheduledEvents |> List.tryFind (fun event -> event.id = photo.eventId) with
                | Some event -> link (eventUrl event) ("Related session: " + event.name)
                | None -> ()
            }
        }

    let mediaManagement query =
        let photos = photosFor query

        let selected =
            photos |> List.tryFind (fun (photo: WorkspacePhoto) -> photo.id = query.item)

        let uploadBody =
            div {
                _class "grid gap-5"

                form {
                    _id "media-upload-form"
                    _method "post"
                    _action (url MediaManagement + "/upload")
                    _class "grid gap-5"

                    fveField {
                        fveLabel {
                            _for "media-upload-name"
                            "Asset name"
                        }

                        fveInput {
                            _id "media-upload-name"
                            _name "name"
                            _required true
                            _maxlength 120
                        }
                    }

                    fveField {
                        fveLabel {
                            _for "media-upload-description"
                            "Image description"
                        }

                        fveTextarea {
                            _id "media-upload-description"
                            _name "alt"
                            _rows 3
                            _required true
                            _maxlength 500
                        }
                    }

                    if query.view = "upload-error" then
                        p {
                            _role "alert"
                            _class "text-sm text-[var(--fve-critical-text)]"
                            "Provide a name and image description."
                        }
                }

                fveField {
                    fveLabel {
                        _for "media-upload-image"
                        "Image file"
                    }

                    fveFileSelection {
                        _id "media-upload-image"
                        _name "image"
                        _accept "image/jpeg,image/png,image/webp"
                        _ariaDescribedby "media-upload-image-help"
                    }

                    fveFieldDescription {
                        _id "media-upload-image-help"
                        "Selected files stay in your browser. This documentation fixture never submits or stores their contents."
                    }
                }
            }

        let uploadDrawer =
            fveDrawer {
                _id "media-upload-drawer"
                _label "Upload asset"
                uploadBody
                _description "Review a safe upload workflow using a repository-owned success fixture."
                _initialFocus "media-upload-name"

                fveDrawerFooter {
                    fragment {
                        fveButton {
                            _dataOn ("click", "document.getElementById('media-upload-drawer')?.close()")
                            "Cancel"
                        }

                        fveButton {
                            _color Color.Primary
                            _variant ButtonVariant.Solid
                            _type "submit"
                            _form "media-upload-form"
                            "Upload"
                        }
                    }
                }
            }

        let uploadAction =
            fveButton {
                _color Color.Primary
                _variant ButtonVariant.Solid

                _dataOn (
                    "click",
                    "document.getElementById('media-upload-drawer')?.showModal(); queueMicrotask(() => document.getElementById('media-upload-name')?.focus())"
                )

                _id "media-upload-drawer-trigger"
                _ariaHaspopup "dialog"
                _ariaControls "media-upload-drawer"
                "Upload"
            }

        let content, heading =
            match selected with
            | Some photo ->
                mediaEditor
                    photo
                    (if query.view = "uploaded" then
                         "Demo image added. The selected local file was not submitted."
                     elif query.view = "saved" then
                         "Changes validated. This resettable example continues to use seeded metadata."
                     elif query.view = "invalid" then
                         "Check the name and description. Submitted values are not retained."
                     else
                         ""),
                photo.name
            | None ->
                let assets =
                    [ for photo in photos ->
                          MediaAsset.create
                              photo.id
                              photo.name
                              photo.source
                              photo.alt
                              (queryUrl MediaManagement { defaultQuery with item = photo.id })
                          |> MediaAsset.withDetail ("Session · " + photo.eventId) ]

                div {
                    _class "grid gap-6"

                    MediaLibrary.create "fieldwork-photos" "Session assets" "photoIds" assets
                    |> MediaLibrary.render id

                    if query.view = "upload-error" then
                        div {
                            _class "hidden"

                            _dataEffect
                                "(() => { const drawer = document.getElementById('media-upload-drawer'); if (drawer && !drawer.open) drawer.showModal(); queueMicrotask(() => document.getElementById('media-upload-name')?.focus()) })()"
                        }

                    uploadDrawer
                },
                "Media"

        let actions =
            [ if query.state = Ready && selected.IsNone then
                  uploadAction ]

        workspace MediaManagement heading "Session assets" false actions (stateContent MediaManagement query content)

    let product page query =
        match page with
        | DependencyGraph -> dependencyGraph query
        | ExecutionDetail -> executionDetail query
        | FinancialReporting -> financialReporting query
        | Messaging -> messaging query
        | Operations -> operations query
        | Scheduling -> scheduling query
        | MediaManagement -> mediaManagement query

    let fixture page query =
        fveBrowser {
            _address ("https://fve.meiermade.com" + queryUrl page query)

            div {
                _class "h-[44rem]"
                product page query
            }
        }
        |> Fixture.create "page-workspace" (title page) (queryUrl page query)
        |> Fixture.withFullscreenContent (product page query)
        |> Fixture.withStates
            [ for state, label in reviewStates page ->
                  FixtureState.create label (queryUrl page { query with state = state })
                  |> fun item ->
                      if state = query.state then
                          FixtureState.current item
                      else
                          item ]

    let preview page query =
        div {
            _attr ("data-page-example-preview", "true")
            // URL-backed review tabs remain outside the product and preserve shareable state.
            nav {
                _ariaLabel "Example review state"
                _attr ("data-page-example-state-tabs", "true")
                _class "mb-5 border-b border-[var(--fve-border)]"

                ul {
                    _class "-mb-px flex list-none gap-5 overflow-x-auto p-0"

                    for state, label in reviewStates page do
                        li {
                            a {
                                _href (queryUrl page { query with state = state })

                                if state = query.state then
                                    _ariaCurrent "page"

                                _class (
                                    if state = query.state then
                                        "inline-flex min-h-11 items-center whitespace-nowrap border-b-2 border-[var(--fve-brand-solid)] font-semibold text-[var(--fve-brand-text)]"
                                    else
                                        "inline-flex min-h-11 items-center whitespace-nowrap border-b-2 border-transparent font-medium text-[var(--fve-muted-text)] hover:border-[var(--fve-border)] hover:text-[var(--fve-text)]"
                                )

                                label
                            }
                        }
                }
            }

            fixture page query |> Fixture.render
        }

    let source page =
        let common =
            [ "ExamplePage"
              "ReviewState"
              "ExampleQuery"
              "defaultQuery"
              "slug"
              "title"
              "url"
              "stateKey"
              "queryUrl"
              "link"
              "pageLink"
              "section"
              "details"
              "reviewStates"
              "stateContent"
              "sideNavWorkspace"
              "workspace" ]

        let declarations, usage =
            match page with
            | DependencyGraph ->
                [ "DependencyNode"
                  "dependencyNodes"
                  "nodeStatus"
                  "selectedNode"
                  "graphContent"
                  "dependencyGraph" ],
                "dependencyGraph defaultQuery"
            | ExecutionDetail ->
                [ "DependencyNode"
                  "dependencyNodes"
                  "nodeStatus"
                  "selectedNode"
                  "TraceSpan"
                  "traceSpans"
                  "spansFor"
                  "executionContent"
                  "executionDetail" ],
                "executionDetail defaultQuery"
            | FinancialReporting ->
                [ "BalancePoint"; "balances"; "currency"; "balanceChart"; "financialReporting" ],
                "financialReporting defaultQuery"
            | Messaging ->
                [ "Conversation"
                  "Message"
                  "conversations"
                  "initialMessages"
                  "sentMessage"
                  "currentConversation"
                  "messagesFor"
                  "messageHistory"
                  "conversationPanel"
                  "messaging" ],
                "messaging defaultQuery"
            | Operations ->
                [ "ScheduledEvent"; "scheduledEvents"; "eventUrl"; "eventTable"; "operations" ],
                "operations defaultQuery"
            | Scheduling -> [ "ScheduledEvent"; "scheduledEvents"; "eventUrl"; "scheduling" ], "scheduling defaultQuery"
            | MediaManagement ->
                [ "ScheduledEvent"
                  "scheduledEvents"
                  "eventUrl"
                  "WorkspacePhoto"
                  "initialPhotos"
                  "uploadedPhoto"
                  "photosFor"
                  "mediaEditor"
                  "mediaManagement" ],
                "mediaManagement defaultQuery"

        let text =
            SourceRegion.readEmbedded (typeof<DocPage>.Assembly) "Docs.Pages.PageExamples.fs"

        "open System\nopen FSharp.ViewEngine\nopen Acme.Components.Primitives\nopen Acme.Components.Application\nopen Acme.Components.Documentation\nopen type Html\nopen type Svg\nopen type Datastar\n\n"
        + SourceRegion.declarations (common @ declarations) text
        + "\n\ndiv { _class \"h-[44rem]\"; "
        + usage
        + " }\n|> Browser.create\n|> Browser.withAddress \"https://fve.meiermade.com"
        + queryUrl page defaultQuery
        + "\"\n|> Browser.render\n|> Fixture.create \"page-workspace\" \"Workspace\" \""
        + queryUrl page defaultQuery
        + "\"\n|> Fixture.withFullscreenContent ("
        + usage
        + ")\n|> Fixture.render"
