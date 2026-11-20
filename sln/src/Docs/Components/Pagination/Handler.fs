namespace Docs.ComponentPages.Pagination

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

    let private componentPagination: HttpHandler =
        fun next context ->
            let requestedPage =
                match Int32.TryParse(context.Request.Query["page"].ToString()) with
                | true, page -> page
                | false, _ -> 2

            let html = Docs.ComponentPages.Pagination.View.paginationPreviewRegion requestedPage |> Render.toString

            (setHttpHeader "Content-Type" "text/html; charset=utf-8"
             >=> setBodyFromString html)
                next
                context

    let get : HttpHandler =
        choose [
            route "/components/pagination/page" >=> componentPagination
        ]
