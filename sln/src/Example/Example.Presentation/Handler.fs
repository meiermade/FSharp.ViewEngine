module Example.Presentation.Handler

open Giraffe
open Example.Presentation.Web
open Example.UI.Views

let get (config: ServerConfig) links : HttpHandler =
    fun next context ->
        if (string context.Request.Path).TrimEnd('/') = View.home then
            Common.View.render context View.title
                (View.render links Layout.theme (Common.View.actions config false)) next context
        else skipPipeline
