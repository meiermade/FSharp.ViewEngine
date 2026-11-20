namespace Docs.ComponentPages.Metric

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
    let metricRegistration =
        registration "components-metric" "/components/metric" "Metric" "Metric"

    let defaultMetric =
        fveMetric {
            _label "Total items"
            "1,280"
        }

    let totalItemsMetric =
        fveMetric {
            _label "Total items"
            _trend "Up 8% from last month"
            _description "Across all projects"
            "1,280"

            fveMetricStatus {
                fveBadge {
                    _color Color.Success
                    "Current"
                }
            }
        }

    let pendingItemsMetric =
        fveMetric {
            _label "Pending items"
            _description "Require review before publishing"
            "14"

            fveMetricStatus {
                fveBadge {
                    _color Color.Warning
                    "Needs review"
                }
            }
        }

    let multipleMetrics =
        div {
            _role "group"
            _ariaLabel "Monthly activity metrics"

            _class
                "grid w-full min-w-0 max-w-4xl grid-cols-[repeat(auto-fit,minmax(min(100%,12rem),1fr))] justify-items-center gap-8"

            for label, value in [ "Views", "48,000"; "Downloads", "3,120"; "Items", "1,280" ] do
                fveMetric {
                    _label label
                    _description "September 2026"
                    value
                }
        }

    let examples () =
            [ sample "metric-default" "Default" [ "defaultMetric" ] (centered defaultMetric)
              sample "metric" "With trend and status" [ "totalItemsMetric" ] (centered totalItemsMetric)
              sample "metric-pending" "With review status" [ "pendingItemsMetric" ] (centered pendingItemsMetric)
              sample "metric-multiple" "Multiple metrics" [ "multipleMetrics" ] (centered multipleMetrics) ]


    let private galleryContent : Content =
        { description = "Present a labelled value, trend, and supporting context as a compact metric."
          usage = []
          sections = []
          apiCategory = Some "metric" }

    let metricPage = gallery metricRegistration galleryContent (examples ())
