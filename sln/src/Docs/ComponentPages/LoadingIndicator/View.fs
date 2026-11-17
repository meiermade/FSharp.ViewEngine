namespace Docs.ComponentPages.LoadingIndicator

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
    let loadingIndicatorRegistration =
        registration
            "components-loading-indicator"
            "/components/loading-indicator"
            "Loading indicator"
            "Loading indicator"

    let defaultLoadingIndicator = fveLoadingIndicator { _ariaLabel "Loading" }

    let visibleLoadingIndicator =
        fveLoadingIndicator {
            _ariaLabel "Refreshing items"
            _size Size.Large
            "Refreshing items"
        }

    let examples () =
            [ sample "loading-indicator" "Default" [ "defaultLoadingIndicator" ] (centered defaultLoadingIndicator)
              sample
                  "loading-indicator-label"
                  "With visible label"
                  [ "visibleLoadingIndicator" ]
                  (centered visibleLoadingIndicator) ]


    let private galleryContent : Content =
        { description = "Communicate indeterminate work with a labelled, reusable loading indicator."
          usage = []
          sections = []
          apiCategory = Some "loading-indicator" }

    let loadingIndicatorPage =
        gallery loadingIndicatorRegistration galleryContent (examples ())
