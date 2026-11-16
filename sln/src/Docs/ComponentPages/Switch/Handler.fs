namespace Docs.ComponentPages.Switch

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

    let private componentSwitchChoice =
        componentFormChoice (fun form ->
            let enabled = form["notifications"].ToString() = "true"

            let result =
                if enabled then
                    "Notifications enabled."
                else
                    "Notifications disabled."

            Docs.ComponentPages.Switch.View.switchFormRegion enabled None (Some result))

    let post : HttpHandler =
        choose [
            route "/components/choices/switch" >=> componentSwitchChoice
        ]
