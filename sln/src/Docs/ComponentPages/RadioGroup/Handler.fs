namespace Docs.ComponentPages.RadioGroup

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

    let private componentRadioChoice =
        componentFormChoice (fun form ->
            match form["updateMode"].ToString() with
            | "automatic" ->
                Docs.ComponentPages.RadioGroup.View.radioGroupFormRegion (Some "automatic") None (Some "Accepted update mode: Automatic.")
            | "manual" ->
                Docs.ComponentPages.RadioGroup.View.radioGroupFormRegion (Some "manual") None (Some "Accepted update mode: Manual review.")
            | _ -> Docs.ComponentPages.RadioGroup.View.radioGroupFormRegion None (Some "Choose an available update mode.") None)

    let post : HttpHandler =
        choose [
            route "/components/choices/radio" >=> componentRadioChoice
        ]
