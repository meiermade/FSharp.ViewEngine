namespace Docs.Pages

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open FSharp.ViewEngine.Components.Application
open FSharp.ViewEngine.Components.Documentation
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

    let section (heading: string) (content: HtmlElement) =
        Section.create (SectionHeader.create heading |> SectionHeader.withDivider) content
        |> Section.render id

    let details (fields: (string * string) list) =
        DescriptionList.create [ for label, value in fields -> DetailField.text label value ]
        |> DescriptionList.withColumns DescriptionListColumns.Three
        |> DescriptionList.render

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

                LoadingIndicator.create ("Loading " + (title page).ToLowerInvariant())
                |> LoadingIndicator.withVisibleLabel
                |> LoadingIndicator.render
            }
        | Empty ->
            EmptyState.create "Nothing to show yet" "Change the current selection or return to the populated workspace."
            |> EmptyState.withActions (link (queryUrl page { query with state = Ready }) "Return to workspace")
            |> EmptyState.render
        | Failed ->
            Notice.create
                "workspace-error"
                "This view could not be loaded"
                (p { "Your selection is preserved. Try loading it again." })
            |> Notice.withTone Tone.Critical
            |> Notice.withActions (link (queryUrl page { query with state = Ready }) "Try again")
            |> Notice.render

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

    let workspace page heading subtitle canvas actions content =
        let product, workspaceName, destinations =
            match page with
            | DependencyGraph
            | ExecutionDetail ->
                "Relay", "Customer analytics", [ DependencyGraph, "Dependencies"; ExecutionDetail, "Executions" ]
            | FinancialReporting -> "Ledger", "Northwind Studio", [ FinancialReporting, "Overview" ]
            | Messaging -> "Gather", "Northwind Outdoor", [ Messaging, "Messages" ]
            | _ ->
                "Fieldwork", "Northwind Outdoor", [ Operations, "Overview"; Scheduling, "Schedule"; MediaManagement, "Photos" ]

        let items =
            [ for destination, label in destinations -> SideNavItem.create destination label ]

        let navigation =
            SideNav.create
                (slug page + "-nav")
                (product + " navigation")
                (SideNavHeader.create product)
                [ SideNavSection.ungrouped items ]
            |> SideNav.withCurrent page
            |> SideNav.withWidth SideNavWidth.Narrow
            |> SideNav.withContext (sideNavWorkspace workspaceName)
            |> SideNav.withMobileContext (sideNavWorkspace workspaceName)
            |> SideNav.withFooter (
                div {
                    _class "flex min-w-0 items-center gap-3"
                    Avatar.create "Andy Meier" "AM" |> Avatar.render

                    p {
                        _class "truncate text-sm font-medium"
                        "Andy Meier"
                    }
                }
            )

        let header =
            PageHeader.create heading
            |> PageHeader.withSubtitle subtitle
            |> PageHeader.withActions (ActionCluster.create (slug page + "-actions") actions)

        let topBar =
            PageTopBar.create ()
            |> PageTopBar.withContent (
                div {
                    _class "flex min-h-[var(--fve-shell-bar-min-height)] items-center px-4 sm:px-6 lg:px-8"

                    Breadcrumbs.create
                        (slug page + "-breadcrumbs")
                        (product + " breadcrumb")
                        [ BreadcrumbItem.create page product; BreadcrumbItem.create page heading ]
                    |> Breadcrumbs.render url
                }
            )

        let body =
            Page.create header content
            |> Page.withTopBar topBar
            |> Page.withWidth PageWidth.Full
            |> Page.withBodyLayout (
                if canvas then
                    PageBodyLayout.Canvas
                else
                    PageBodyLayout.Padded
            )
            |> Page.render id

        AppShell.create (slug page + "-shell") navigation body
        |> AppShell.withTheme (
            ComponentsTheme.sky
            |> ComponentsTheme.withDensity Density.Compact
            |> ComponentsTheme.withControlSize ControlSize.Small)
        |> AppShell.withBoundary AppShellBoundary.Container
        |> AppShell.asPreview (product + " workspace")
        |> AppShell.render url

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
        Status.create status
        |> Status.withTone (
            match status with
            | "Succeeded" -> Tone.Positive
            | "Failed" -> Tone.Critical
            | _ -> Tone.Warning
        )
        |> Status.render

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
                _class "flex flex-wrap items-end gap-3 border-y border-[var(--fve-border)] px-4 py-3 sm:px-6 lg:px-8"

                div {
                    _class "min-w-0 flex-1"

                    Input.create "graph-search" "Search dependencies"
                    |> Input.withType InputType.Search
                    |> Input.withAttributes [ _dataBind "_graph-query" ]
                    |> Input.render
                }

                Button.create "Zoom out"
                |> Button.withAttributes [ _dataOn ("click", "$_graphZoom = Math.max(0.75, $_graphZoom - 0.25)") ]
                |> Button.render

                Button.create "Zoom in"
                |> Button.withAttributes [ _dataOn ("click", "$_graphZoom = Math.min(1.5, $_graphZoom + 0.25)") ]
                |> Button.render

                Button.create "Reset view"
                |> Button.withAttributes [ _dataOn ("click", "$_graphZoom = 1; $_graphQuery = ''") ]
                |> Button.render

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
                    _class "grid gap-6 p-4 lg:grid-cols-2 sm:p-6 lg:p-8"

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
            "Dependencies"
            "Production · Customer analytics · 6 dependencies"
            true
            [ ApplicationAction.link
                  (queryUrl
                      ExecutionDetail
                      { defaultQuery with
                          item = (selectedNode query).run })
                  "Inspect execution" ]
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

            details
                [ "Dependency", node.name
                  "Worker", "warehouse-worker-01"
                  "Started",
                  (if node.status = "Blocked" then
                       "Not started"
                   else
                       "17 Sep 2026, 09:42:18 UTC")
                  "Duration", (if node.status = "Blocked" then "—" else "1.24 s")
                  "Attempt", (if node.status = "Blocked" then "0" else "1")
                  "Execution", node.run ]

            div {
                _class "flex flex-wrap items-center gap-3"
                nodeStatus node.status
                link (queryUrl DependencyGraph { defaultQuery with item = node.key }) "View in dependency graph"
            }

            if node.status = "Blocked" then
                Notice.create
                    "blocked-run"
                    "Waiting for an upstream dependency"
                    (p { "warehouse.orders must succeed before this execution can start." })
                |> Notice.withTone Tone.Warning
                |> Notice.withActions (
                    link (queryUrl ExecutionDetail { defaultQuery with item = "run-2408" }) "Inspect failed dependency"
                )
                |> Notice.render
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

                            Table.create
                                "Execution spans"
                                [ Table.column "Operation" (fun (item: TraceSpan) ->
                                      div {
                                          _style $"padding-left:{item.depth}rem"
                                          link (queryUrl ExecutionDetail { query with view = item.id }) item.operation
                                      })
                                  |> Table.asRowHeader
                                  Table.column "Service" (fun item -> text item.service)
                                  Table.column "Start" (fun item -> text $"{item.start} ms") |> Table.alignEnd
                                  Table.column "Duration" (fun item -> text $"{item.duration} ms")
                                  |> Table.alignEnd
                                  Table.column "0 — 620 — 1,240 ms" (fun item ->
                                      div {
                                          _ariaHidden "true"
                                          _class "relative h-6 min-w-64 rounded bg-[var(--fve-surface-subtle)]"

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
                                      }) ]
                                spans
                            |> Table.withDensity Density.Compact
                            |> Table.render
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
            [ ApplicationAction.link (queryUrl DependencyGraph { defaultQuery with item = node.key }) "View dependency" ]
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

        let yPercent amount =
            100. - float amount / 50000. * 100.

        let chartX index =
            xPercent index * 6.

        let chartY amount =
            yPercent amount * 1.8

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
                        _class "flex h-52 flex-col justify-between text-right text-xs leading-none text-[var(--fve-muted-text)]"

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
                                _class "absolute size-3 -translate-x-1/2 -translate-y-1/2 rounded-full bg-[var(--fve-brand-solid)]"
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

                Table.create
                    "Monthly closing balances (USD)"
                    [ Table.column "Month" (fun (point: BalancePoint) -> text point.month)
                      |> Table.asRowHeader
                      |> Table.asMobilePrimary
                      Table.column "Actual" (fun point -> text (currency point.actual))
                      |> Table.alignEnd
                      Table.column "Plan" (fun point -> text (currency point.planned))
                      |> Table.alignEnd ]
                    points
                |> Table.withMobileLayout TableMobileLayout.Records
                |> Table.render
            }
        }

    let financialReporting query =
        let content =
            div {
                _class "grid gap-8"

                div {
                    _class "grid gap-5 border-b border-[var(--fve-border)] pb-6 sm:grid-cols-3"

                    for label, value, description in
                        [ "Checking balance", "$38,442.11", "As of September 17"
                          "Change since April", "+$14,242.11", "58.9% increase"
                          "Against September plan", "−$1,557.89", "3.9% below plan" ] do
                        Metric.text label value |> Metric.withDescription description |> Metric.render
                }

                balanceChart query

                section
                    "Recent transactions"
                    (Table.create
                        "Checking transactions"
                        [ Table.column "Date" (fun (date, _, _, _) -> text date)
                          Table.column "Description" (fun (_, name, id, _) ->
                              link
                                  ("/components/page-examples/account-management?destination=ledger-transaction-"
                                   + string id)
                                  name)
                          |> Table.asRowHeader
                          |> Table.asMobilePrimary
                          Table.column "Amount" (fun (_, _, _, amount) -> text (currency amount))
                          |> Table.alignEnd ]
                        [ "Jul 28", "Northwind payment", 201, 4800M
                          "Jul 27", "Cloud hosting", 202, 386.42M
                          "Jul 26", "ACH withdrawal", 203, 1240M ]
                     |> Table.withMobileLayout TableMobileLayout.Records
                     |> Table.render)
            }

        workspace
            FinancialReporting
            "Financial overview"
            "Northwind Studio · Operating checking · USD"
            false
            [ ApplicationAction.link
                  "/components/page-examples/account-management?destination=ledger-account-2048"
                  "View account"
              ApplicationAction.link "/components/page-examples/account-management" "All accounts" ]
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
            initialMessages @ [ { sentMessage with conversation = (currentConversation query).id } ]
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
                div {
                    _id ("message-" + message.id)
                    _dataInit "el.parentElement.scrollTop = el.parentElement.scrollHeight"

                    _class (
                        if message.outgoing then
                            "ml-auto grid max-w-[85%] justify-items-end gap-1"
                        else
                            "mr-auto grid max-w-[85%] gap-1"
                    )

                    p {
                        _class "text-xs text-[var(--fve-muted-text)]"
                        message.author + " · " + message.time
                    }

                    p {
                        _class (
                            if message.outgoing then
                                "whitespace-pre-wrap break-words rounded-2xl rounded-br-sm bg-[var(--fve-brand-solid)] px-4 py-3 text-base text-white"
                            else
                                "whitespace-pre-wrap break-words rounded-2xl rounded-bl-sm bg-[var(--fve-surface-subtle)] px-4 py-3 text-base text-[var(--fve-text)]"
                        )

                        message.body
                    }
                }
        }

    let conversationPanel query messages draft error =
        let conversation = currentConversation query

        div {
            _id "conversation-panel"
            _class "flex h-full min-h-0 flex-col"

            header {
                _class "flex items-center gap-3 border-b border-[var(--fve-border)] p-4"
                Avatar.create conversation.name conversation.initials |> Avatar.render

                div {
                    h2 {
                        _class "text-base font-semibold"
                        conversation.name
                    }

                    p {
                        _class "text-xs text-[var(--fve-muted-text)]"
                        conversation.participants
                    }
                }
            }

            messageHistory conversation messages

            form {
                _method "post"
                _action (url Messaging + "/send?item=" + conversation.id)
                _class "grid shrink-0 gap-3 border-t border-[var(--fve-border)] bg-[var(--fve-surface)] p-4"

                let field =
                    Textarea.create "message" "Message"
                    |> Textarea.withId "message-compose"
                    |> Textarea.withRows 2
                    |> Textarea.withValue draft
                    |> Textarea.required
                    |> Textarea.withAttributes [ _maxlength 2000 ]

                (match error with
                 | Some message -> field |> Textarea.withValidation message
                 | None -> field)
                |> Textarea.render

                div {
                    _class "flex flex-wrap items-center justify-between gap-3"

                    p {
                        _role "status"
                        _class "text-xs text-[var(--fve-muted-text)]"

                        if query.view = "sent" then
                            "Message accepted. This deterministic example does not retain submitted text."
                        else
                            "Only participants in this conversation can see your reply."
                    }

                    Button.create "Send message"
                    |> Button.withVariant ButtonVariant.Primary
                    |> Button.asSubmit
                    |> Button.render
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
            SideNav.create
                "gather-conversations"
                "Conversations"
                (SideNavHeader.create "Gather")
                [ SideNavSection.group
                      "Messages"
                      [ for conversation in conversations -> SideNavItem.create conversation.id conversation.name ] ]
            |> SideNav.withCurrent current.id
            |> SideNav.withWidth SideNavWidth.Wide
            |> SideNav.withContext (sideNavWorkspace "Northwind Outdoor")
            |> SideNav.withMobileContext (sideNavWorkspace "Northwind Outdoor")
            |> SideNav.withFooter (
                div {
                    _class "flex items-center gap-3"
                    Avatar.create "Andy Meier" "AM" |> Avatar.render

                    span {
                        _class "text-sm font-medium"
                        "Andy Meier"
                    }
                }
            )

        let content =
            Html.section {
                _ariaLabel "Current conversation"
                _class "h-full min-h-0"
                conversationPanel query messages "" error
            }

        let page =
            Page.create
                (PageHeader.create "Messages"
                 |> PageHeader.withSubtitle "Your conversations, together in one place.")
                (stateContent Messaging query content)
            |> Page.withTopBar (
                PageTopBar.create ()
                |> PageTopBar.withContent (
                    div {
                        _class "flex min-h-[var(--fve-shell-bar-min-height)] items-center px-4 sm:px-6 lg:px-8"

                        Breadcrumbs.create
                            "gather-breadcrumbs"
                            "Gather breadcrumb"
                            [ BreadcrumbItem.create "beach" "Gather"
                              BreadcrumbItem.create current.id "Messages" ]
                        |> Breadcrumbs.render destination
                    }
                )
            )
            |> Page.withBodyLayout PageBodyLayout.Canvas
            |> Page.render destination

        AppShell.create "messaging-shell" navigation page
        |> AppShell.withTheme (
            ComponentsTheme.sky
            |> ComponentsTheme.withDensity Density.Compact
            |> ComponentsTheme.withControlSize ControlSize.Small)
        |> AppShell.withBoundary AppShellBoundary.Container
        |> AppShell.asPreview "Gather workspace"
        |> AppShell.render destination

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

    let eventTable events =
        Table.create
            "Upcoming events"
            [ Table.column "Event" (fun (event: ScheduledEvent) -> link (eventUrl event) event.name)
              |> Table.asRowHeader
              |> Table.asMobilePrimary
              Table.column "Date" (fun event ->
                  text (event.date.ToString("MMM d", Globalization.CultureInfo.InvariantCulture)))
              Table.column "Time" (fun event -> text event.time)
              Table.column "People" (fun event -> text event.people) ]
            events
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.withDensity Density.Compact
        |> Table.render

    let operations query =
        let content =
            div {
                _class "grid gap-8"

                div {
                    _class "grid gap-6 border-b border-[var(--fve-border)] pb-6 sm:grid-cols-3"

                    for label, value, detail in
                        [ "Today's sessions", "2", "One lesson · One camp"
                          "Riders expected", "5", "September 17"
                          "Upcoming sessions", "4", "Through September 19" ] do
                        Metric.text label value |> Metric.withDescription detail |> Metric.render
                }

                section
                    "Today's schedule"
                    (eventTable (scheduledEvents |> List.filter (fun event -> event.date = DateOnly(2026, 9, 17))))

                div {
                    _class "grid gap-8 lg:grid-cols-2"

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
                    FirstSteps.create
                        "fieldwork-first-steps"
                        "Finish your workspace"
                        [ FirstStep.create "schedule" "Review this week's schedule"
                          |> FirstStep.withAction (link (url Scheduling) "Open schedule")
                          FirstStep.create "photos" "Prepare your session photographs"
                          |> FirstStep.withDescription "Review descriptions before sharing a collection."
                          |> FirstStep.withAction (link (url MediaManagement) "Open photos") ]
                    |> FirstSteps.render
            }

        workspace
            Operations
            "Good morning, Andy"
            "Thursday, September 17 · Northwind Outdoor"
            false
            [ ApplicationAction.link (url Scheduling) "View schedule"
              |> ApplicationAction.withVariant ButtonVariant.Primary ]
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
                        "Session photographs"
                        (link
                            (queryUrl MediaManagement { defaultQuery with item = event.photo })
                            "View matching photograph")

                    link (url Scheduling) "Back to schedule"
                },
                event.name
            | None ->
                let view =
                    match query.view with
                    | "day" -> CalendarView.Day
                    | "week" -> CalendarView.Week
                    | "year" -> CalendarView.Year
                    | _ -> CalendarView.Month

                let today = DateOnly(2026, 9, 17)
                let offset =
                    match Int32.TryParse query.range with
                    | true, value -> Math.Clamp(value, -366, 366)
                    | _ -> 0
                let start = today.AddDays offset
                let step direction =
                    let target =
                        match view with
                        | CalendarView.Day -> start.AddDays direction
                        | CalendarView.Week -> start.AddDays(direction * 7)
                        | CalendarView.Month -> start.AddMonths direction
                        | CalendarView.Year -> start.AddYears direction
                    target.DayNumber - today.DayNumber

                Calendar.create
                    "Schedule"
                    view
                    start
                    [ for event in scheduledEvents ->
                          CalendarEvent.create
                              event.id
                              event.name
                              event.date
                              (eventUrl event)
                          |> CalendarEvent.withTime event.start event.finish
                          |> CalendarEvent.withDetail event.people ]
                |> Calendar.withToday today (queryUrl Scheduling { query with range = "0" })
                |> Calendar.withSelectedDate start
                |> Calendar.withDateDestination (fun date -> queryUrl Scheduling { query with view = "day"; range = string (date.DayNumber - today.DayNumber) })
                |> (if step -1 >= -366 then Calendar.withPrevious (queryUrl Scheduling { query with range = string (step -1) }) else id)
                |> (if step 1 <= 366 then Calendar.withNext (queryUrl Scheduling { query with range = string (step 1) }) else id)
                |> Calendar.withViewDestinations
                    [ for value, label in
                          [ CalendarView.Day, "day"
                            CalendarView.Week, "week"
                            CalendarView.Month, "month"
                            CalendarView.Year, "year" ] ->
                          value, queryUrl Scheduling { query with view = label } ]
                |> Calendar.render id,
                "Schedule"

        workspace
            Scheduling
            heading
            "Northwind Outdoor · Times in America/New_York"
            false
            [ ApplicationAction.link (url Operations) "Overview" ]
            (stateContent Scheduling query content)

    type WorkspacePhoto =
        { id: string
          name: string
          source: string
          alt: string
          eventId: string }

    let initialPhotos =
        [ { id = "photo-301"
            name = "Camp on the coastal trail"
            source = "/images/page-examples/blue.png"
            alt = "Solid blue background"
            eventId = "camp-017" }
          { id = "photo-302"
            name = "Taking the first turn"
            source = "/images/page-examples/teal.png"
            alt = "Solid teal background"
            eventId = "lesson-202" }
          { id = "photo-303"
            name = "Before the lesson"
            source = "/images/page-examples/amber.png"
            alt = "Solid amber background"
            eventId = "lesson-201" }
          { id = "photo-304"
            name = "A quiet afternoon"
            source = "/images/page-examples/green.png"
            alt = "Solid green background"
            eventId = "ride-019" }
          { id = "photo-305"
            name = "A moment to learn"
            source = "/images/page-examples/coral.png"
            alt = "Solid coral background"
            eventId = "camp-017" }
          { id = "photo-306"
            name = "Building confidence"
            source = "/images/page-examples/violet.png"
            alt = "Solid violet background"
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

                Input.create "name" "Photo name"
                |> Input.withValue photo.name
                |> Input.required
                |> Input.withAttributes [ _maxlength 120 ]
                |> Input.render

                Textarea.create "alt" "Image description"
                |> Textarea.withValue photo.alt
                |> Textarea.withRows 3
                |> Textarea.withDescription "Describe the scene for people who cannot see the photograph."
                |> Textarea.required
                |> Textarea.withAttributes [ _maxlength 500 ]
                |> Textarea.render

                div {
                    _role "status"
                    _class "text-sm text-[var(--fve-muted-text)]"
                    feedback
                }

                div {
                    _class "flex flex-wrap items-center gap-3"

                    Button.create "Save changes"
                    |> Button.withVariant ButtonVariant.Primary
                    |> Button.asSubmit
                    |> Button.render

                    link (url MediaManagement) "Back to photos"
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

                    Input.create "name" "Photo name"
                    |> Input.withId "media-upload-name"
                    |> Input.required
                    |> Input.withAttributes [ _maxlength 120 ]
                    |> Input.render

                    Textarea.create "alt" "Image description"
                    |> Textarea.withId "media-upload-description"
                    |> Textarea.withRows 3
                    |> Textarea.required
                    |> Textarea.withAttributes [ _maxlength 500 ]
                    |> Textarea.render

                    if query.view = "upload-error" then
                        p {
                            _role "alert"
                            _class "text-sm text-[var(--fve-critical-text)]"
                            "Provide a name and image description."
                        }
                }

                FileSelection.create "media-upload-image" "image" "Photograph"
                |> FileSelection.withAccept "image/jpeg,image/png,image/webp"
                |> FileSelection.withDescription
                    "Selected files stay in your browser. This documentation fixture never submits or stores their contents."
                |> FileSelection.render
            }

        let uploadDrawer =
            Drawer.create "media-upload-drawer" "Upload photograph" uploadBody
            |> Drawer.withDescription "Review a safe upload workflow using a repository-owned success fixture."
            |> Drawer.withInitialFocus "media-upload-name"
            |> Drawer.withFooter (
                fragment {
                    Button.create "Cancel"
                    |> Button.withAttributes [
                        _dataOn ("click", "document.getElementById('media-upload-drawer')?.close()") ]
                    |> Button.render

                    Button.create "Upload"
                    |> Button.asSubmit
                    |> Button.withVariant ButtonVariant.Primary
                    |> Button.withAttributes [ _form "media-upload-form" ]
                    |> Button.render
                }
            )

        let uploadAction =
            ApplicationAction.command
                "document.getElementById('media-upload-drawer')?.showModal(); queueMicrotask(() => document.getElementById('media-upload-name')?.focus())"
                "Upload"
            |> ApplicationAction.withVariant ButtonVariant.Primary
            |> ApplicationAction.withAttributes [
                _id "media-upload-drawer-trigger"
                _ariaHaspopup "dialog"
                _ariaControls "media-upload-drawer" ]

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

                    MediaLibrary.create "fieldwork-photos" "Session photographs" "photoIds" assets
                    |> MediaLibrary.render id

                    if query.view = "upload-error" then
                        div {
                            _class "hidden"
                            _dataEffect "(() => { const drawer = document.getElementById('media-upload-drawer'); if (drawer && !drawer.open) drawer.showModal(); queueMicrotask(() => document.getElementById('media-upload-name')?.focus()) })()"
                        }

                    uploadDrawer |> Drawer.render
                },
                "Photos"

        let actions =
            [ if query.state = Ready && selected.IsNone then uploadAction
              ApplicationAction.link (url Scheduling) "View schedule" ]

        workspace
            MediaManagement
            heading
            "Northwind Outdoor · Session library"
            false
            actions
            (stateContent MediaManagement query content)

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
        div {
            _class "h-[44rem]"
            product page query
        }
        |> Browser.create
        |> Browser.withAddress ("https://fve.meiermade.com" + queryUrl page query)
        |> Fixture.browser "page-workspace" (title page) (queryUrl page query)
        |> Fixture.withStates
            [ for state, label in reviewStates page ->
                  FixtureState.create label (queryUrl page { query with state = state })
                  |> fun item ->
                      if state = query.state then FixtureState.current item
                      else item ]

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
                                if state = query.state then _ariaCurrent "page"
                                _class (if state = query.state then "inline-flex min-h-11 items-center whitespace-nowrap border-b-2 border-[var(--fve-brand-solid)] font-semibold text-[var(--fve-brand-text)]" else "inline-flex min-h-11 items-center whitespace-nowrap border-b-2 border-transparent font-medium text-[var(--fve-muted-text)] hover:border-[var(--fve-border)] hover:text-[var(--fve-text)]")
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
        + "\"\n|> Fixture.browser \"page-workspace\" \"Workspace\" \""
        + queryUrl page defaultQuery
        + "\"\n|> Fixture.render"
