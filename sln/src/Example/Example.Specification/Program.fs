module Example.Specification.Program

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
open Example.Specification.Web

let private useAssets (app: WebApplication) =
    let root = if app.Environment.IsDevelopment() then app.Environment.ContentRootPath else AppContext.BaseDirectory
    let assets = new PhysicalFileProvider(Path.Combine(root, "wwwroot"))
    let serve provider path =
        app.UseStaticFiles(
            StaticFileOptions(FileProvider = provider, RequestPath = PathString path,
                              OnPrepareResponse = Action<_>(Common.BrowserAssets.prepareResponse))) |> ignore
    serve assets ""
    serve assets "/examples/specification"
    for route, folder in [ "scripts", "js"; "css", "css"; "fonts", "fonts"; "images", "images" ] do
        let directory = Path.Combine(assets.Root, folder)
        if Directory.Exists directory then
            serve (new PhysicalFileProvider(directory)) ("/examples/specification/assets/" + route)
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

let private home = "/examples/specification"

let private specRoutes config catalogDemo =
    let services = Services.create config home
    let getPages = GET >=> choose [
        Specification.Handler.get services catalogDemo
        route (home + "/sitemap.xml") >=> sitemap (Specification.View.navigation |> List.map fst)
    ]
    let postFixture = POST >=> Common.Handler.form (fun form ->
        choose [ Specification.Handler.post services form; setStatusCode 404 >=> text "Example action not found." ])
    fun (next: HttpFunc) (context: HttpContext) ->
        if context.Request.Method = HttpMethods.Post && context.Request.Path.StartsWithSegments(PathString home) then
            postFixture next context
        else getPages next context

let routes catalogDemo = specRoutes (Config.load ()) catalogDemo

[<EntryPoint>]
let main args =
    let config = Config.load ()
    run args config home (specRoutes config (System.Environment.GetEnvironmentVariable "EXAMPLE_SPEC_CATALOG_DEMO" = "true"))
