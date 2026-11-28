module Example.Documentation.Handler

open Giraffe
open Example.Documentation.Web

let get (config: ServerConfig) : HttpHandler =
    fun next context ->
        let path = (string context.Request.Path).TrimEnd('/')
        match View.navigation |> List.tryFind (fun (href, _) -> href = path) with
        | Some _ ->
            let page = if path = View.home then "" else path.Substring(View.home.Length + 1)
            Common.View.render context (View.title page)
                (View.renderWithActions (Common.View.actions config false) page) next context
        | None -> skipPipeline
