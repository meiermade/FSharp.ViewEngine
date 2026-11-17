namespace Docs.Guides

open System
open Docs.Common
open Docs.Pages
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

open Docs.Common.Registration

open Docs.Common.Registration

module ComponentPages =
    let themingRegistration =
        { packageRegistration "components-theming" "/guides/theming" "Theming" "Theming" with
            category = "Guides"
            aliases = ["/components/theming"] }

    let interactionRegistration =
        { packageRegistration "components-interaction" "/guides/using-signals" "Using signals" "Using signals" with
            category = "Guides"
            aliases = ["/components/using-signals"] }

    let interactionPage = ComponentGuideExamples.signalsPage interactionRegistration

    let themingPageFor state = ComponentGuideExamples.themingPageFor state themingRegistration

    let themingPage = ComponentGuideExamples.themingPage themingRegistration
