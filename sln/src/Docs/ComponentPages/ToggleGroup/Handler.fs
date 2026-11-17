namespace Docs.ComponentPages.ToggleGroup

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

    let private componentFormattingChoice =
        componentFormChoice (fun form ->
            let requested = form["format"] |> Seq.toList |> List.distinct

            let selected =
                requested |> List.filter (fun value -> List.contains value [ "bold"; "italic" ])

            if selected <> requested || List.isEmpty selected then
                Docs.ComponentPages.ToggleGroup.View.toggleGroupFormRegion selected (Some "Choose at least one available format.") None
            else
                Docs.ComponentPages.ToggleGroup.View.toggleGroupFormRegion
                    selected
                    None
                    (Some("Saved formatting: " + String.concat ", " selected + ".")))

    let post : HttpHandler =
        choose [
            route "/components/choices/formatting" >=> componentFormattingChoice
        ]
