namespace Example.UI.Views

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open type Html
open type Svg
open type Datastar
open Model
open ViewData
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
        let data = homeData query

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
        let rangePickerId = elementId query "home-date-range"
        let resetId = elementId query "home-reset-zoom"
        let zoomed = data.visibleStart <> data.startDate || data.visibleEnd <> data.endDate
        let periods = [ 7; 30; 90 ]
        let earliest = data.endDate.AddDays(-89)

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
    const commit = event => {{
      event.formData.set('account', '{data.account.id}');
      event.formData.set('range', '{data.periodDays}');
      event.formData.set('comparison', '{data.comparison}');
      event.formData.set('startDate', new Date(Math.floor(first / day) * day).toISOString().slice(0, 10));
      event.formData.set('endDate', new Date(Math.floor(last / day) * day).toISOString().slice(0, 10));
    }};
    form.addEventListener('formdata', commit, {{ once: true }});
    try {{ form.requestSubmit(); }} finally {{ form.removeEventListener('formdata', commit); }}
  }}
}}
"""

        let setPreset = """
const { start, end, preset } = evt.detail;
if (preset) el.closest('form').elements.namedItem('range').value = String(Math.round((Date.parse(end) - Date.parse(start)) / 86400000) + 1);
"""

        div {
            _class "relative grid min-w-0 grid-cols-1 gap-6"

            form {
                _id rangeFormId
                Navigation.filterForm
                // Datastar can parse noscript children while morphing. Only submit the active controls.
                _dataOn ("formdata", "for (const name of ['nativeRange', 'nativeStartDate', 'nativeEndDate']) evt.formData.delete(name)")
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

                hidden "range" (string data.periodDays)

                div {
                    _class "[@media(scripting:none)]:hidden"
                    _dataOn ("fve-calendar-change", setPreset)

                    fveField {
                        fveLabel { _id (rangePickerId + "-label"); _for rangePickerId; "Date range" }
                        fveDatePicker {
                            _id rangePickerId
                            _ariaLabel "Date range"
                            _attr ("aria-labelledby", rangePickerId + "-label " + rangePickerId + "-value")
                            _name "startDate"
                            _rangeEndName "endDate"
                            _mode CalendarSelectionMode.Range
                            _value (iso data.visibleStart)
                            _endValue (iso data.visibleEnd)
                            _min (iso earliest)
                            _max (iso data.endDate)
                            _displayMonth data.visibleEnd
                            _rangePresets (periods |> List.map (fun days -> $"Last {days} days", data.endDate.AddDays(1 - days), data.endDate))
                        }
                    }
                }

                noscript {
                    div {
                        _class "flex flex-wrap items-end gap-3 [@media(scripting:enabled)]:hidden"
                        fveField {
                            fveLabel { _for (rangePickerId + "-preset"); "Date range" }
                            Html.select {
                                _id (rangePickerId + "-preset")
                                _name "nativeRange"
                                _class "h-8 rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-2"
                                option { _value ""; "Use start and end dates" }
                                for days in periods do
                                    option { _value (string days); $"Last {days} days" }
                            }
                        }
                        for name, label, value in [ "nativeStartDate", "Start date", data.visibleStart; "nativeEndDate", "End date", data.visibleEnd ] do
                            let id = elementId query name
                            fveField {
                                fveLabel { _for id; label }
                                fveInput {
                                    _id id; _name name; _type "date"; _required true
                                    _value (iso value); _min (iso earliest); _max (iso data.endDate)
                                }
                            }
                    }
                }

                select
                    query
                    "comparison"
                    "Scenario"
                    data.comparison
                    [ "expected", "Expected"
                      "optimistic", "Optimistic"
                      "pessimistic", "Pessimistic" ]

                submit "Apply"
                if zoomed then
                    fveLink {
                        _id resetId
                        _href resetHref
                        Navigation.filterLink
                        _variant LinkVariant.Outline
                        "Reset zoom"
                    }
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
                                    (if data.accountIds.Count > 1 then "Total balance · " else "Balance · ") + data.account.name
                                }

                                p {
                                    _class "mt-1 text-2xl font-semibold tabular-nums"
                                    money data.currentBalance
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

                            if zoomed then
                                _dataOn ("dblclick", $"document.getElementById('{resetId}')?.click()")
                        }

                        p {
                            _class "text-xs text-[var(--fve-muted-text)]"
                            if zoomed then "Drag across the chart to zoom · Double-click to reset."
                            else "Drag across the chart to zoom."
                        }

                        p {
                            _role "status"
                            _ariaLive "polite"
                            _class "sr-only"
                            Html.text ("Chart date range: " + date data.visibleStart + " – " + date data.visibleEnd)
                        }
                    }
                }
            }

        }

    let home (query:Query) =
        let data = homeData query
        fveSectionStack {
            balanceHistory ApplicationPage.Home true query
            fveSection {
                _label "Transactions"
                fveSectionActions { viewAllLink query data.account.id }
                if data.transactions.IsEmpty then
                    fveEmptyState { _label "No transactions in this date range"; _description "Choose different dates or reset zoom." }
                else
                    transactionTable query "Transactions for the selected account" data.transactions (Some data.accountIds)
            }
        }
