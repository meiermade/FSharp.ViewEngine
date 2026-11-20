namespace Docs.ComponentPages.Breadcrumbs

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
open Docs.ComponentPages.SideNav.View
open Docs.Common.SharedExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let breadcrumbsRegistration =
        registration "components-breadcrumbs" "/components/breadcrumbs" "Breadcrumbs" "Breadcrumbs"

    let breadcrumbsPreview =
        div { breadcrumbsExample id "/components" "/components/table" "/components/card" }
        |> themedSurface

    let breadcrumbTrail =
        [ fveBreadcrumbItem {
              _href "/"
              "Home"
          }
          fveBreadcrumbItem {
              _href "/components"
              "Components"
          }
          fveBreadcrumbItem {
              _href "/components/side-nav"
              "Navigation"
          }
          fveBreadcrumbItem {
              _href "/components/breadcrumbs"
              "Breadcrumbs"
          } ]

    let middleBreadcrumbsExample =
        fveBreadcrumbs {
            _id "middle-breadcrumbs"
            _ariaLabel "Middle overflow breadcrumb"
            _class "max-w-xs"
            yield! breadcrumbTrail
        }

    let visibleBreadcrumbsExample =
        fveBreadcrumbs {
            _id "visible-breadcrumbs"
            _ariaLabel "Available space breadcrumb"
            yield! breadcrumbTrail
        }

    let examples () =
            [ sample "breadcrumbs" "Default" [ "breadcrumbsExample" ] breadcrumbsPreview
              sample
                  "breadcrumbs-middle"
                  "Middle overflow"
                  [ "breadcrumbTrail"; "middleBreadcrumbsExample" ]
                  (themedSurface middleBreadcrumbsExample)
              sample
                  "breadcrumbs-visible"
                  "Available space"
                  [ "breadcrumbTrail"; "visibleBreadcrumbsExample" ]
                  (themedSurface visibleBreadcrumbsExample)
              |> note "The full trail stays visible while it fits. Resize the preview to collapse older ancestors into the ellipsis; they return when space is available. Width, not item count or viewport breakpoints, owns this behavior." ]


    let private galleryContent : Content =
        { description = "Show the current hierarchy with semantic links and responsive overflow."
          usage = []
          sections = []
          apiCategory = Some "breadcrumbs" }

    let breadcrumbsPage = gallery breadcrumbsRegistration galleryContent (examples ())
