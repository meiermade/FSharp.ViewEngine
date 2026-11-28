module Example.Application.Product.Handler

open Giraffe
open Example.Application.Web
open Example.Application.Web.Common
open Example.UI.Views.Model

let tryPage (path: string) =
    match path.TrimEnd('/').Split('/') |> Array.toList with
    | [""; "examples"; "application"] -> Some ApplicationPage.Home
    | "" :: "examples" :: "application" :: segments ->
        [ Accounts.Handler.tryPage; Transactions.Handler.tryPage; Settings.Handler.tryPage; Profile.Handler.tryPage ]
        |> List.tryPick (fun find -> find segments)
    | _ -> None

let query services context =
    let requested = Request.query context
    { requested with topBarActions = View.actions services.config (requested.embedded || requested.fixture) }
    |> Demo.Session.apply services context

let private renderPage services context page requested = View.render context (Example.UI.Views.Product.pageTitle page requested) (Demo.Workflow.render services context page requested)

let get services : HttpHandler =
    fun next context ->
        match tryPage (string context.Request.Path) with
        | Some page ->
            let requested = query services context
            if not (Demo.Workflow.available requested.records page) then System.Threading.Tasks.Task.FromResult None
            else
                let requested = Demo.Workflow.collectionNotification services context page requested
                renderPage services context page requested next context
        | None -> System.Threading.Tasks.Task.FromResult None

let post services form : HttpHandler =
    fun next context ->
        match tryPage (string context.Request.Path) with
        | Some page -> Demo.Workflow.submit services (renderPage services) page (query services context) form next context
        | None -> System.Threading.Tasks.Task.FromResult None
