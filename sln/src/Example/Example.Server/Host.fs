module Example.Server.Host

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open StarFederation.Datastar.DependencyInjection
open Giraffe

let private useAssets (app: IApplicationBuilder) =
    let publicRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot")
    let serve provider path =
        app.UseStaticFiles(
            StaticFileOptions(FileProvider = provider, RequestPath = PathString path,
                              OnPrepareResponse = Action<_>(Common.BrowserAssets.prepareResponse))) |> ignore
    let files = new PhysicalFileProvider(publicRoot)
    serve files ""
    serve files "/examples"
    // Retain published asset URLs and copied example links while the physical folders stay flat.
    for route, folder in [ "scripts", "js"; "css", "css"; "fonts", "fonts"; "images", "images" ] do
        let provider = new PhysicalFileProvider(Path.Combine(publicRoot, folder))
        serve provider ("/examples/assets/" + route)

let create (args: string array) (config: ServerConfig) routes =
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
