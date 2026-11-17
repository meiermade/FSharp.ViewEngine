namespace Docs.ComponentPages.TagInput

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

    let private componentTagChoice =
        componentFormChoice (fun form ->
            let values =
                form["submittedTags"]
                |> Seq.map (fun value -> value.Trim())
                |> Seq.filter (String.IsNullOrWhiteSpace >> not)
                |> Seq.distinct
                |> Seq.toList

            if List.isEmpty values || values.Length > 5 then
                Docs.ComponentPages.TagInput.View.tagInputFormRegion values (Some "Choose one to five tags.") None
            else
                Docs.ComponentPages.TagInput.View.tagInputFormRegion values None (Some("Saved tags: " + String.concat ", " values + ".")))

    let get : HttpHandler =
        choose [
            route "/components/tags/reset" >=> componentChoicePatch (fun _ -> View.tagInputFormRegion [ "reviewed" ] None (Some "Tags reset."))
        ]

    let post : HttpHandler =
        choose [
            route "/components/tags" >=> componentTagChoice
        ]
