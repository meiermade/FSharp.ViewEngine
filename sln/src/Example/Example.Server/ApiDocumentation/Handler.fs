module Example.Server.ApiDocumentation.Handler

open System
open Giraffe
open Example.Server

let private tryPage (path: string) =
    let path = path.TrimEnd('/')
    let prefix = "/examples/api-documentation"
    if path = prefix then Some ""
    elif path.StartsWith(prefix + "/", StringComparison.Ordinal) then
        let page = path.Substring(prefix.Length + 1)
        if View.navigation |> List.exists (fun (href, _) -> href = path) then Some page else None
    else None

let get (services: ServerServices) : HttpHandler =
    fun next context ->
        match tryPage (string context.Request.Path) with
        | Some page ->
            Common.View.render context (View.title page)
                (View.renderWithActions (Common.View.actions services.config false) page) next context
        | None -> System.Threading.Tasks.Task.FromResult None
