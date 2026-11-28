module Example.Presentation.Program

open System
open System.IO
open System.Net
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting
open StarFederation.Datastar.DependencyInjection
open Giraffe
open Example.Presentation.Web

let private useAssets (app: WebApplication) =
    let root = if app.Environment.IsDevelopment() then app.Environment.ContentRootPath else AppContext.BaseDirectory
    let assets = new PhysicalFileProvider(Path.Combine(root, "wwwroot"))
    let serve provider path =
        app.UseStaticFiles(
            StaticFileOptions(FileProvider = provider, RequestPath = PathString path,
                              OnPrepareResponse = Action<_>(Common.BrowserAssets.prepareResponse))) |> ignore
    serve assets ""
    serve assets "/examples/presentation"
    for route, folder in [ "scripts", "js"; "css", "css"; "fonts", "fonts"; "images", "images" ] do
        let directory = Path.Combine(assets.Root, folder)
        if Directory.Exists directory then
            serve (new PhysicalFileProvider(directory)) ("/examples/presentation/assets/" + route)
    serve assets "/examples"
    serve (new PhysicalFileProvider(Path.Combine(assets.Root, "images"))) "/examples/assets/images"

let private create (args: string array) (config: ServerConfig) routes =
    let builder = WebApplication.CreateBuilder(args)
    builder.Services.AddGiraffe().AddDatastar() |> ignore
    let app = builder.Build()
    match config.publicOrigin with
    | Some origin ->
        // Deployment provides the public authority; never infer it from arbitrary forwarded headers.
        app.Use(fun (context: HttpContext) (next: RequestDelegate) ->
            context.Request.Scheme <- origin.Scheme
            context.Request.Host <- HostString origin.Authority
            next.Invoke context) |> ignore
    | None -> ()
    useAssets app
    app.UseGiraffe(Common.Handler.securityHeaders [] >=> routes)
    app

let private sitemap paths : HttpHandler =
    fun next context ->
        let origin = context.Request.Scheme + "://" + context.Request.Host.Value
        let urls = paths |> List.map (fun path -> $"  <url><loc>{WebUtility.HtmlEncode(origin + path)}</loc></url>")
        let xml = String.concat "\n" ([ "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"; "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">" ] @ urls @ [ "</urlset>"; "" ])
        (setHttpHeader "Content-Type" "application/xml; charset=utf-8" >=> setBodyFromString xml) next context

let private run args config home (routes: HttpHandler) =
    let health = json {| status = "ok"; commit = config.commit; image = config.image |}
    let endpoints = choose [
        GET >=> choose [ route "/health" >=> health; route "/examples/health" >=> health; route "/" >=> redirectTo false home ]
        routes
        setStatusCode 404 >=> text "Not found"
    ]
    let app = create args config endpoints
    app.Run()
    0

let private presentationRoutes config =
    let links: View.HostedLinks =
        { application =
            match System.Environment.GetEnvironmentVariable "EXAMPLE_APP_URL" with
            | value when not (System.String.IsNullOrWhiteSpace value) -> value
            | _ -> "https://fve.meiermade.com/examples/application"
          docs = config.docsUrl }
    GET >=> choose [
        Handler.get config links
        route (View.home + "/sitemap.xml") >=> sitemap [ View.home ]
    ]

let routes () = presentationRoutes (Config.load ())

[<EntryPoint>]
let main args =
    let config = Config.load ()
    run args config View.home (presentationRoutes config)
