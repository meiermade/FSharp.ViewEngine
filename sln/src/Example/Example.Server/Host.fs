module Example.Server.Host

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open StarFederation.Datastar.DependencyInjection
open Giraffe

/// The same published assets are used standalone and when mounted by Docs.
let useAssets (app: IApplicationBuilder) =
    let directory = Path.Combine(AppContext.BaseDirectory, "wwwroot", "examples", "assets")
    app.UseStaticFiles(
        StaticFileOptions(FileProvider = new PhysicalFileProvider(directory), RequestPath = PathString "/examples/assets")
    ) |> ignore

let create (args: string array) routes =
    let builder = WebApplication.CreateBuilder(args)
    builder.Services.AddGiraffe().AddDatastar() |> ignore
    let app = builder.Build()
    useAssets app
    app.UseGiraffe(Common.Handler.securityHeaders [] >=> routes)
    app
