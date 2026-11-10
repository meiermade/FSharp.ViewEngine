namespace Example.Server.ApplicationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain
open Controls
open TransactionTable

module internal Home =
    let select query name (label: string) selected options =
        fveField {
            fveLabel {
                _for (elementId query ("template-" + name))
                label
            }

            fveSelect {
                _id (elementId query ("template-" + name))
                _name name
                _value selected

                fveSelectContent {
                    for value, label, disabled in (options |> List.map (fun (value, label) -> (value, label, false))) do
                        fveSelectItem {
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let balanceHistory page showAccountSelector (query: Query) =
        let data =
            Ledger.Home.readHome
                { records = query.records
                  account = query.account
                  period = query.range
                  comparison = query.comparison
                  startDate = query.startDate
                  endDate = query.endDate }

        let iso (date: DateOnly) =
            date.ToString("yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture)

        let scenarioName =
            (match data.comparison with
             | "optimistic" -> "Optimistic"
             | "pessimistic" -> "Pessimistic"
             | _ -> "Expected")
            + " scenario"

        let instant (date: DateOnly) =
            DateTimeOffset(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
                .ToUnixTimeMilliseconds()

        let day = 86400000L

        let series id name dashed (values: (DateOnly * decimal) list) =
            let points =
                values |> List.map (fun (date, value) -> [| box (instant date); box value |])
            // A balance carries through the entire final calendar day, including a one-day zoom.
            let closing =
                match List.tryLast values with
                | Some(date, value) when date = data.endDate -> [ [| box (instant date + day - 1L); box value |] ]
                | _ -> []

            {| id = id
               name = name
               ``type`` = "line"
               showSymbol = false
               connectNulls = false
               clip = true
               step = if dashed then box false else box "end"
               data = points @ closing
               lineStyle =
                {| width = if dashed then 2.0 else 2.5
                   ``type`` = if dashed then "dashed" else "solid" |} |}

        // Dates, currency, carry-forward lines and brush policy belong to this financial page.
        let options =
            {| useUTC = true
               toolbox = {| show = false |}
               aria =
                {| enabled = true
                   description =
                    $"Values in {data.account.currency}, {iso data.visibleStart} to {iso data.visibleEnd}. Use the application date controls to zoom without dragging." |}
               tooltip = {| trigger = "axis" |}
               grid =
                {| left = 8
                   right = 16
                   top = 16
                   bottom = 8
                   containLabel = true |}
               xAxis =
                {| ``type`` = "time"
                   min = instant data.startDate
                   max = instant data.endDate + day - 1L
                   axisTick = {| show = false |}
                   axisLabel = {| hideOverlap = true |} |}
               yAxis = {| ``type`` = "value"; scale = true |}
               brush =
                {| toolbox = Array.empty<string>
                   xAxisIndex = 0
                   brushType = "lineX"
                   brushMode = "single"
                   transformable = false
                   removeOnClick = false
                   inBrush = {| colorAlpha = 1 |}
                   outOfBrush = {| colorAlpha = 1 |} |}
               dataZoom =
                [| {| id = "balance-window"
                      ``type`` = "inside"
                      xAxisIndex = 0
                      filterMode = "none"
                      disabled = true
                      startValue = instant data.visibleStart
                      endValue = instant data.visibleEnd + day - 1L |} |]
               series =
                [| series "actual" "Actual" false data.actual
                   series "scenario" scenarioName true data.scenario |] |}

        let rangeFormId = elementId query "home-chart-range"

        let pairs =
            [ "account", string data.account.id
              "range", string data.periodDays
              "comparison", data.comparison ]

        let resetHref = applicationHref query page + querySuffix pairs

        let rangeChange =
            $"""
const extent = evt.detail.areas?.[0]?.coordRange;
if (extent?.length === 2 && extent.every(Number.isFinite)) {{
  const [startValue, endValue] = extent;
  const day = 86400000;
  const first = Math.max({instant data.startDate}, Math.min(startValue, endValue));
  const last = Math.min({instant data.endDate + day - 1L}, Math.max(startValue, endValue));
  if (first <= last) {{
    const form = document.getElementById('{rangeFormId}');
    form.elements.namedItem('startDate').value = new Date(Math.floor(first / day) * day).toISOString().slice(0, 10);
    form.elements.namedItem('endDate').value = new Date(Math.floor(last / day) * day).toISOString().slice(0, 10);
    form.requestSubmit();
  }}
}}
"""

        div {
            _class "grid min-w-0 grid-cols-1 gap-6"

            form {
                _method "get"
                _action (applicationHref query page)
                _ariaLabel (if showAccountSelector then "Home balance controls" else "Account balance controls")
                _class "flex min-w-0 flex-wrap items-end gap-3"
                contextFields query

                if showAccountSelector then
                    select query "account" "Account" (string data.account.id)
                        (query.records.accounts |> List.map (fun account -> string account.id, account.name))
                else
                    hidden "account" (string data.account.id)

                select
                    query
                    "range"
                    "Period"
                    (string data.periodDays)
                    [ "7", "Last 7 days"; "30", "Last 30 days"; "90", "Last 90 days" ]

                select
                    query
                    "comparison"
                    "Scenario"
                    data.comparison
                    [ "expected", "Expected"
                      "optimistic", "Optimistic"
                      "pessimistic", "Pessimistic" ]

                submit "Apply"
            }

            div {
                _id (elementId query "home-chart")

                div {
                    div {
                        _class "grid min-w-0 grid-cols-1 gap-4 [overflow-wrap:anywhere]"

                        div {
                            _class "flex flex-wrap items-start justify-between gap-3"

                            div {
                                p {
                                    _class "text-xs text-[var(--fve-muted-text)]"
                                    "Balance · " + data.account.name
                                }

                                p {
                                    _class "mt-1 text-2xl font-semibold tabular-nums"
                                    money (balance query.records data.account)
                                }
                            }

                            div {
                                _class "flex flex-wrap gap-x-4 gap-y-1 text-xs text-[var(--fve-muted-text)]"

                                span {
                                    _class "text-[var(--fve-brand-text)]"
                                    "━ Actual"
                                }

                                span { "┄ " + scenarioName }
                            }
                        }

                        fveChart {
                            _id (elementId query "home-balance-chart")
                            _ariaLabel "Actual and scenario account balances"
                            _options options
                            _events [ "brushend" ]
                            _data ("balance-chart-currency", data.account.currency)
                            _dataOn ("chart-brushend", rangeChange)

                            _dataOn (
                                "dblclick",
                                $"""const form = document.getElementById('{rangeFormId}');
form.elements.namedItem('startDate').value = '{iso data.startDate}';
form.elements.namedItem('endDate').value = '{iso data.endDate}';
form.requestSubmit();"""
                            )
                        }

                        p {
                            _class "text-xs text-[var(--fve-muted-text)]"
                            "Drag across the chart to zoom · Double-click to reset, or choose dates below."
                        }

                        div {
                            _class "flex flex-wrap items-end gap-3"

                            form {
                                _id rangeFormId
                                _method "get"
                                _action (applicationHref query page)
                                _ariaLabel "Chart date range"
                                _attr ("data-fve-local-navigation", "true")
                                _class "flex min-w-0 flex-wrap items-end gap-3"
                                contextFields query

                                for name, value in pairs do
                                    hidden name value

                                fveField {
                                    fveLabel {
                                        _for (elementId query "home-start")
                                        "Start date"
                                    }

                                    fveInput {
                                        _size Size.Small
                                        _id (elementId query "home-start")
                                        _name "startDate"
                                        _type "date"
                                        _required true
                                        _value (iso data.visibleStart)
                                        _attr ("min", iso data.startDate)
                                        _attr ("max", iso data.endDate)
                                    }
                                }

                                fveField {
                                    fveLabel {
                                        _for (elementId query "home-end")
                                        "End date"
                                    }

                                    fveInput {
                                        _size Size.Small
                                        _id (elementId query "home-end")
                                        _name "endDate"
                                        _type "date"
                                        _required true
                                        _value (iso data.visibleEnd)
                                        _attr ("min", iso data.startDate)
                                        _attr ("max", iso data.endDate)
                                    }
                                }

                                fveButton {
                                    _type "submit"
                                    _variant ButtonVariant.Outline
                                    _size Size.Small
                                    _class "[&:not(:defined)]:hidden"
                                    "Apply range"
                                }

                                noscript {
                                    button {
                                        _type "submit"
                                        "Apply range"
                                    }
                                }
                            }

                            if data.visibleStart = data.startDate && data.visibleEnd = data.endDate then
                                fveButton {
                                    _disabled true
                                    _variant ButtonVariant.Outline
                                    _size Size.Small
                                    "Reset zoom"
                                }
                            else
                                fveLink {
                                    _href resetHref
                                    _attr ("data-fve-local-navigation", "true")
                                    _variant LinkVariant.Outline
                                    _size Size.Small
                                    "Reset zoom"
                                }
                        }

                        p {
                            _role "status"
                            _ariaLive "polite"
                            _class "text-xs text-[var(--fve-muted-text)]"
                            Html.text (date data.visibleStart + " – " + date data.visibleEnd)
                        }
                    }
                }
            }

        }

    let home (query:Query) =
        let data = Ledger.Home.readHome { records=query.records; account=query.account; period=query.range; comparison=query.comparison; startDate=query.startDate; endDate=query.endDate }
        fveSectionStack {
            balanceHistory ApplicationPage.Home true query
            Layout.section "Transactions"
                (if data.transactions.IsEmpty then
                     fveEmptyState { _label "No transactions in this date range"; _description "Choose different dates or reset zoom." }
                 else transactionTable query "Transactions for the selected account" data.transactions false)
        }
