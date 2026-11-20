namespace Docs.ComponentPages.Select

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
open Docs.ComponentPages.Select.View

open System.IO
open System.Text.Json
open Docs.Web
open Docs.Common.Handler

module Handler =
    let selectSearchQuery signalName (context: Microsoft.AspNetCore.Http.HttpContext) =
        try
            use signals =
                System.Text.Json.JsonDocument.Parse(context.Request.Query["datastar"].ToString())

            match signals.RootElement.TryGetProperty(signalName: string) with
            | true, value when value.ValueKind = System.Text.Json.JsonValueKind.String -> Some(value.GetString())
            | _ -> None
        with
        | :? System.Text.Json.JsonException
        | :? InvalidOperationException -> None

    let remoteMemberSearch wholeField : HttpHandler =
        fun next context ->
            task {
                let searchText =
                    selectSearchQuery "components_remote_members_query" context
                    |> Option.defaultValue ""

                let retry = context.Request.Query["retry"].ToString() = "true"
                do! System.Threading.Tasks.Task.Delay(300, context.RequestAborted)

                let view =
                    if wholeField then
                        remoteMembersField searchText
                    else
                        remoteMembersOptions searchText retry

                return! htmlString (Render.toString view) next context
            }

    let remoteMembersRoutes: HttpHandler =
        choose
            [ Giraffe.Core.GET
              >=> route "/components/members/search"
              >=> remoteMemberSearch false
              Giraffe.Core.GET
              >=> route "/components/members/field"
              >=> remoteMemberSearch true ]

    let itemSearchHandler signalName contentId : HttpHandler =
        fun next context ->
            task {
                match selectSearchQuery signalName context with
                | None ->
                    return! (setStatusCode 400 >=> Giraffe.Core.text "A known search field is required.") next context
                | Some searchText ->
                    let delay =
                        if searchText.StartsWith("proj", StringComparison.OrdinalIgnoreCase) then
                            750
                        else
                            300

                    do! System.Threading.Tasks.Task.Delay(delay, context.RequestAborted)
                    let retry = context.Request.Query["retry"].ToString() = "true"
                    return! htmlString (itemComboboxOptions contentId searchText retry |> Render.toString) next context
            }

    let itemSearchRoutes: HttpHandler =
        Giraffe.Core.GET
        >=> route "/components/items/search"
        >=> itemSearchHandler "item_query" "fve-select-item-popup"

    let itemFixtureSearchRoutes: HttpHandler =
        Giraffe.Core.GET
        >=> route "/components/items/search"
        >=> fun next context ->
            // Additional field identities belong to regression fixtures, not the public example.
            match
                itemSearchTargets
                |> List.tryFind (fun (signal, _) -> signal <> "item_query" && (selectSearchQuery signal context).IsSome)
            with
            | Some(signal, contentId) -> itemSearchHandler signal contentId next context
            | None -> System.Threading.Tasks.Task.FromResult None

    let private componentSelectChoice =
        componentFormChoice (fun form ->
            match form["status"].ToString() with
            | "active" -> Docs.ComponentPages.Select.View.selectFormRegion (Some "active") None (Some "Accepted status: Active.")
            | "pending" -> Docs.ComponentPages.Select.View.selectFormRegion (Some "pending") None (Some "Accepted status: Pending.")
            | "scheduled" -> Docs.ComponentPages.Select.View.selectFormRegion (Some "scheduled") None (Some "Accepted status: Scheduled.")
            | _ -> Docs.ComponentPages.Select.View.selectFormRegion None (Some "Choose an available status.") None)

    let get : HttpHandler =
        choose [
            remoteMembersRoutes
            itemFixtureSearchRoutes
            itemSearchRoutes
        ]

    let post : HttpHandler =
        choose [
            route "/components/choices/select" >=> componentSelectChoice
            route "/components/choices/multiple-select" >=> componentMultipleChoice "reviewerIds" View.multipleSelectForm
            route "/components/choices/multiple-combobox" >=> componentMultipleChoice "assigneeIds" View.multipleComboboxForm
        ]
