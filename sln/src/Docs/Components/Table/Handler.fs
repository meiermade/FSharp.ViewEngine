namespace Docs.ComponentPages.Table

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
open Docs.ComponentPages.Table.View

open System.IO
open System.Text.Json
open Docs.Web
open Docs.Common.Handler

module Handler =
    let tableMoreHandler: HttpHandler =
        fun _ context ->
            task {
                match Int32.TryParse(context.Request.Query["offset"].ToString()) with
                | true, offset when offset > 0 && offset < infiniteTeamMembers.Length && offset % 8 = 0 ->
                    do! System.Threading.Tasks.Task.Delay(300, context.RequestAborted)
                    let datastar = context.RequestServices.GetRequiredService<IDatastarService>()

                    do!
                        datastar.PatchElementsAsync(
                            infiniteTeamRows offset |> Render.toString,
                            PatchElementsOptions(
                                Selector = $"#people-more-{offset}",
                                PatchMode = ElementPatchMode.Replace
                            ),
                            context.RequestAborted
                        )

                    return Some context
                | _ ->
                    context.Response.StatusCode <- 400
                    return Some context
            }

    let tableMoreRoutes: HttpHandler =
        Giraffe.Core.GET >=> route "/components/table/more" >=> tableMoreHandler

    let private componentTableSort: HttpHandler =
        fun next context ->
            let sort = context.Request.Query["sort"].ToString()
            let direction = context.Request.Query["direction"].ToString()

            let html =
                Docs.ComponentPages.Table.View.teamMemberSortFromQuery sort direction
                |> Docs.ComponentPages.Table.View.sortableTeamTablePreview
                |> Render.toString

            task {
                if context.Request.Headers["datastar-request"].ToString() = "true" then
                    let service = context.RequestServices.GetRequiredService<IDatastarService>()
                    do! service.PatchElementsAsync(html, cancellationToken = context.RequestAborted)
                    return Some context
                else
                    return!
                        (setHttpHeader "Content-Type" "text/html; charset=utf-8"
                         >=> setBodyFromString html)
                            next
                            context
            }

    let get : HttpHandler =
        choose [
            route "/components/table/sort" >=> componentTableSort
            tableMoreRoutes
        ]
