module Example.Application.Marketing.Handler

open Microsoft.AspNetCore.Http
open Giraffe
open Example.Application.Web

let get (services: ServerServices) : HttpHandler =
    fun next context ->
        let path = context.Request.Path.ToString().TrimEnd('/')
        if List.contains path View.paths then
            Common.View.render context (View.title path) (View.render services.config.docsUrl path (Common.View.actions services.config false)) next context
        else System.Threading.Tasks.Task.FromResult None
