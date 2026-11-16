namespace Docs.ComponentPages.Checkbox

open System
open System.IO
open System.Text.Json
open Docs.Common
open Docs.Web
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection

open Docs.Common.Handler

module Handler =

    let private componentCheckboxChoice =
        componentFormChoice (fun form ->
            if form["confirmArchivedReview"].ToString() = "true" then
                Docs.ComponentPages.Checkbox.View.checkboxFormRegion true None (Some "Review confirmed.")
            else
                Docs.ComponentPages.Checkbox.View.checkboxFormRegion false (Some "Confirm the review.") None)

    let post : HttpHandler =
        choose [
            route "/components/choices/checkbox" >=> componentCheckboxChoice
        ]
