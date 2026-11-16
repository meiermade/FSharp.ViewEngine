namespace Docs.ComponentPages.BottomNavigation

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
    let bottomNavigationRegistration =
        applicationRegistration
            "components-bottom-navigation"
            "/components/bottom-navigation"
            "Bottom navigation"
            "Bottom navigation"

    let bottomNavigationExample =
        fveBottomNavigation {
            _id "example-bottom-navigation"
            _ariaLabel "Primary navigation"

            fveBottomNavigationItem {
                _href "/components"
                "Home"
            }

            fveBottomNavigationItem {
                _href "/components/table"
                "Items"
            }

            fveBottomNavigationItem {
                _href "/components/metric"
                "Activity"
            }

            fveBottomNavigationItem {
                _href "/guides/theming"
                "Settings"
            }

            _value "/components/table"
        }

    let compactBottomNavigationExample =
        fveBottomNavigation {
            _id "compact-bottom-navigation"
            _ariaLabel "Workspace navigation"

            fveBottomNavigationItem {
                _href "/components"
                "Home"
            }

            fveBottomNavigationItem {
                _href "/components/month-calendar"
                "Schedule"
            }

            fveBottomNavigationItem {
                _href "/components/page-examples/media-management"
                "Media"
            }

            _value "/components/month-calendar"
        }

    let defaultBottomNavigation =
        fveBottomNavigation {
            _id "default-bottom-navigation"
            _ariaLabel "Default primary navigation"

            fveBottomNavigationItem {
                _href "/components"
                "Home"
            }

            fveBottomNavigationItem {
                _href "/components/table"
                "Items"
            }
        }

    let examples () =
            [ sample
                  "bottom-navigation-default"
                  "Default"
                  [ "defaultBottomNavigation" ]
                  (detailsSurface defaultBottomNavigation)
              sample
                  "bottom-navigation"
                  "Four destinations"
                  [ "bottomNavigationExample" ]
                  (detailsSurface bottomNavigationExample)
              sample
                  "bottom-navigation-compact"
                  "Three destinations"
                  [ "compactBottomNavigationExample" ]
                  (detailsSurface compactBottomNavigationExample) ]


    let private galleryContent : Content =
        { description = "Navigate primary mobile destinations from a compact bottom bar."
          usage = []
          sections = []
          apiCategory = Some "bottom-navigation" }

    let bottomNavigationPage =
        gallery bottomNavigationRegistration galleryContent (examples ())
