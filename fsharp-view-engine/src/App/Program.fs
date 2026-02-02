open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open App.Handlers

let webApp =
    choose [
        GET >=> choose [
            route "/" >=> homeHandler
            route "/installation" >=> installationHandler
            route "/quickstart" >=> quickstartHandler
        ]
    ]

let configureApp (app : IApplicationBuilder) =
    app.UseStaticFiles() |> ignore
    app.UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    configureServices builder.Services

    let app = builder.Build()

    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    configureApp app

    let config = App.Config.load()
    app.Run(config.serverUrl)

    0 // Exit code

