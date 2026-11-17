module Example.Server.Program

open Microsoft.AspNetCore.Http
open Giraffe

/// Feature routes can be mounted by Docs without starting another host.
let routes services : HttpHandler =
    let pages = choose [
        Product.Handler.get services
        Specification.Handler.get services
        ApiDocumentation.Handler.get services
        Marketing.Handler.get services
        Presentation.Handler.get services
    ]
    let submissions form = choose [
        Product.Handler.post services form
        Specification.Handler.post services form
        setStatusCode 404 >=> text "Example action not found."
    ]
    fun next context ->
        if context.Request.Path.StartsWithSegments(PathString "/examples") && context.Request.Path <> PathString "/examples" then
            (choose [ POST >=> Common.Handler.form submissions; GET >=> pages ]) next context
        else System.Threading.Tasks.Task.FromResult None

[<EntryPoint>]
let main args =
    let config = Config.load ()
    let services = Services.create config
    let endpoints = choose [
        GET >=> route "/" >=> redirectTo false "/examples/application"
        routes services
        setStatusCode 404 >=> text "Not found"
    ]
    let app = Host.create args endpoints
    app.Run()
    0
