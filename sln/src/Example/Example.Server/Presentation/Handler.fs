module Example.Server.Presentation.Handler

open Giraffe
open Example.Server
open Example.Components.Views

let get (services: ServerServices) : HttpHandler =
    fun next context ->
        if context.Request.Path.ToString().TrimEnd('/') = "/examples/presentation" then
            Common.View.render context "Treasury review"
                (Example.Presentation.View.render Layout.theme (Common.View.actions services.config false)) next context
        else System.Threading.Tasks.Task.FromResult None
