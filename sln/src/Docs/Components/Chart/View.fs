namespace Docs.ComponentPages.Chart

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let chartRegistration =
        registration "components-chart" "/components/chart" "Chart" "Chart"

    let lineChartOptions =
        {| tooltip = {| trigger = "axis" |}
           grid =
            {| left = 8
               right = 16
               top = 16
               bottom = 8
               containLabel = true |}
           xAxis =
            {| ``type`` = "category"
               data = [| "Jan"; "Feb"; "Mar" |] |}
           yAxis = {| ``type`` = "value" |}
           series =
            [| {| name = "Revenue"
                  ``type`` = "line"
                  data = [| 24; 36; 42 |] |} |] |}

    let defaultChart =
        fveChart {
            _id "line-chart"
            _ariaLabel "Revenue: January 24, February 36, March 42"
            _options lineChartOptions
        }

    let columnChartOptions =
        {| tooltip = {| trigger = "axis" |}
           grid =
            {| left = 8
               right = 16
               top = 16
               bottom = 8
               containLabel = true |}
           xAxis =
            {| ``type`` = "category"
               data = [| "Jan"; "Feb"; "Mar" |] |}
           yAxis = {| ``type`` = "value" |}
           series =
            [| {| name = "Revenue"
                  ``type`` = "bar"
                  data = [| 24; 36; 42 |] |} |] |}

    let columnChart =
        fveChart {
            _id "column-chart"
            _ariaLabel "Revenue by month, as columns"
            _options columnChartOptions
        }

    let barChartOptions =
        {| tooltip = {| trigger = "axis" |}
           grid =
            {| left = 8
               right = 16
               top = 16
               bottom = 8
               containLabel = true |}
           xAxis = {| ``type`` = "value" |}
           yAxis =
            {| ``type`` = "category"
               data = [| "Jan"; "Feb"; "Mar" |] |}
           series =
            [| {| name = "Revenue"
                  ``type`` = "bar"
                  data = [| 24; 36; 42 |] |} |] |}

    let barChart =
        fveChart {
            _id "bar-chart"
            _ariaLabel "Revenue by month, as horizontal bars"
            _options barChartOptions
        }

    let sankeyChartOptions =
        {| tooltip = {| trigger = "item" |}
           series =
            [| {| ``type`` = "sankey"
                  left = 8
                  right = 8
                  top = 16
                  bottom = 16
                  data =
                   [| {| name = "Income"
                         label = {| position = "right" |} |}
                      {| name = "Costs"
                         label = {| position = "left" |} |}
                      {| name = "Savings"
                         label = {| position = "left" |} |} |]
                  links =
                   [| {| source = "Income"
                         target = "Costs"
                         value = 70 |}
                      {| source = "Income"
                         target = "Savings"
                         value = 30 |} |]
                  lineStyle = {| color = "gradient"; opacity = 0.35 |} |} |] |}

    let sankeyChart =
        fveChart {
            _id "sankey-chart"
            _ariaLabel "Income 100 flows into costs 70 and savings 30"
            _options sankeyChartOptions
        }

    let chartTypeControl (label: string) options =
        fveButton {
            _dataOn (
                "click",
                $"$_chart_element.setAttribute('options', JSON.stringify({System.Text.Json.JsonSerializer.Serialize(options)}))"
            )

            label
        }

    let interactiveChart =
        div {
            _class "grid min-w-0 gap-3"
            _attr ("data-signals__root__ifmissing", "{ _chart_selection: 'Click a point or column.' }")

            fveChart {
                _id "interactive-values-chart"
                _dataRef ("_chart_element", ["root"])
                _ariaLabel "Revenue with application-owned chart type and selection"
                _options lineChartOptions
                _events [ "click" ]
                _dataOn ("chart-click", "$_chart_selection = evt.detail.name + ': ' + evt.detail.value")
            }

            div {
                _class "flex flex-wrap gap-2"
                chartTypeControl "Show line" lineChartOptions
                chartTypeControl "Show columns" columnChartOptions
            }

            p {
                _role "status"
                _dataText "$_chart_selection"
                "Click a point or column."
            }
        }

    let emptyChart =
        fveChart {
            _id "empty-values-chart"
            _ariaLabel "Revenue with no available data"
            _options {| series = Array.empty<obj> |}
            _empty true
        }

    let examples () =
            [ sample "chart-default" "Line" [ "lineChartOptions"; "defaultChart" ] defaultChart
              sample "chart-column" "Column" [ "columnChartOptions"; "columnChart" ] columnChart
              sample "chart-bar" "Horizontal bar" [ "barChartOptions"; "barChart" ] barChart
              sample "chart-sankey" "Sankey" [ "sankeyChartOptions"; "sankeyChart" ] sankeyChart
              sample
                  "chart-interaction"
                  "Updating options and handling events"
                  [ "lineChartOptions"
                    "columnChartOptions"
                    "chartTypeControl"
                    "interactiveChart" ]
                  interactiveChart
              sample "chart-empty" "Empty data" [ "emptyChart" ] emptyChart ]


    let private galleryContent : Content =
        { description = "Create line, column, bar, Sankey and other ECharts charts with ordinary F# options. The host handles lazy loading, theme, resizing and cleanup; your application owns the data and interactions."
          usage = [
              prose
                  "_options accepts JSON-compatible F# records or anonymous records using ECharts option names. Column and horizontal-bar charts both use type = \"bar\"; the category axis chooses their direction. Sankey uses nodes and links instead of axes. --fve-chart-height controls height. The default palette and text follow the owning theme; explicit ECharts colors remain caller-owned."

              p {
                  "The value passed to "
                  Html.code { "_options" }
                  " follows the "
                  a {
                      _href "https://echarts.apache.org/en/option.html"
                      _class "text-[var(--fve-brand-text)] underline"
                      "ECharts options API"
                  }
                  "."
              }

              prose
                  "Use _events to opt into ECharts events: click becomes chart-click, brushend becomes chart-brushend. evt.detail contains the original payload. After each options/theme update, chart-render exposes evt.detail.chart for application-owned JavaScript formatters or actions. _empty true displays the empty state without interpreting your datasets."

              p {
                  "See the "

                  a {
                      _href "/examples/application"
                      _class "text-[var(--fve-brand-text)] underline"
                      "Application template"
                  }

                  " for currency formatting, date-range brushing and server-owned filtering."
              }
          ]
          sections = []
          apiCategory = Some "chart" }

    let chartPage = gallery chartRegistration galleryContent (examples ())
