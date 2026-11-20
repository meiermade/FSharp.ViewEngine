module Example.Server.Program

open Microsoft.AspNetCore.Http
open Giraffe

/// Feature routes served by the standalone Example host.
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
            let sitemap : HttpHandler = fun continuation request ->
                let origin = request.Request.Scheme + "://" + request.Request.Host.Value
                (setHttpHeader "Content-Type" "application/xml; charset=utf-8" >=> setBodyFromString (Discovery.sitemap origin)) continuation request
            (choose [ POST >=> Common.Handler.form submissions; GET >=> choose [ route "/examples/sitemap.xml" >=> sitemap; pages ] ]) next context
        else System.Threading.Tasks.Task.FromResult None

[<EntryPoint>]
let main args =
    let config = Config.load ()
    let services = Services.create config
    let health = json {| status = "ok"; commit = config.commit; image = config.image |}
    let endpoints = choose [
        GET >=> choose [ route "/health" >=> health; route "/examples/health" >=> health; route "/" >=> redirectTo false "/examples/application" ]
        routes services
        setStatusCode 404 >=> text "Not found"
    ]
    let app = Host.create args config endpoints
    app.Run()
    0
