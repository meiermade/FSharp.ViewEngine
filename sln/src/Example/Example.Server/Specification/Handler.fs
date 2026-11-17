module Example.Server.Specification.Handler

open System
open Microsoft.AspNetCore.Http
open Giraffe
open Example.Server
open Example.Server.Product
open Example.Components.Views.Model

let private tryPage (path: string) =
    let path = path.TrimEnd('/')
    let prefix = "/examples/specification"
    if path = prefix then Some ""
    elif path.StartsWith(prefix + "/", StringComparison.Ordinal) then
        let page = path.Substring(prefix.Length + 1)
        if List.contains page View.pages then Some page else None
    else None

let private query services context page =
    let requested = Common.Request.query context
    let workspace =
        if List.contains page View.workflowPages then (View.productContext page requested).workspace
        else requested.workspace
    { requested with workspace = workspace
                     topBarActions = Common.View.actions services.config (requested.embedded || requested.fixture) }
    |> Session.apply services context

let private renderProduct context workflow _ requested =
    let productQuery =
        { View.productContext workflow requested with
            appMode = false; fixture = true; previewId = ""; topBarActions = [] }
    let content = Product.Handler.render (View.productPage workflow requested) productQuery
    Common.View.render context (View.title workflow) (View.renderProduct workflow requested content)

let get services : HttpHandler =
    fun next context ->
        match tryPage (string context.Request.Path) with
        | Some page ->
            let requested = query services context page
            if requested.fixture then
                if List.contains page View.workflowPages then
                    let productPage = View.productPage page requested
                    if not (Product.Handler.available requested.records productPage) then System.Threading.Tasks.Task.FromResult None
                    else
                        let requested = Product.Handler.collectionNotification services context productPage requested
                        renderProduct context page productPage requested next context
                else (setStatusCode 404 >=> text "Choose a product workflow fixture.") next context
            else Common.View.render context (View.title page) (View.render page requested) next context
        | None -> System.Threading.Tasks.Task.FromResult None

let post services form : HttpHandler =
    fun next context ->
        match tryPage (string context.Request.Path) with
        | Some workflow when List.contains workflow View.workflowPages ->
            let requested = { query services context workflow with specification = true }
            Product.Handler.submit services (fun context -> renderProduct context workflow)
                (View.productPage workflow requested) requested form next context
        | _ -> System.Threading.Tasks.Task.FromResult None
