namespace Docs.Catalog

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
open Docs.ComponentPages.Phone.View
open Docs.ComponentPages.Browser.View

module FrameReview =
    let expandedReviewFixture phone state =
        div {
            _id "docs-navigation-root"
            _data ("fve-navigation-root", "true")
            _data ("fve-app-mode-root", "true")
            _dataOn ("keydown", ["window"], Example.Server.Common.Navigation.enhancement.appModeExitAction)

            main {
                _id "main-content"
                _tabindex -1
                _class "outline-none"

                if phone then
                    phoneReviewFrame true
                else
                    browserReviewFrame true state
            }
        }
