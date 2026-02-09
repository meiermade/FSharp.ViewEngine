open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Docs.Handlers

let webApp =
    choose [
        GET >=> choose [
            route "/health" >=> text "ok"
            route "/" >=> homeHandler
            route "/installation" >=> installationHandler
            route "/quickstart" >=> quickstartHandler
            route "/custom" >=> customHandler
            route "/giraffe" >=> giraffeHandler
            route "/extensions/alpine" >=> alpineHandler
            route "/extensions/datastar" >=> datastarHandler
            route "/extensions/htmx" >=> htmxHandler
            route "/extensions/svg" >=> svgHandler
            route "/extensions/tailwind" >=> tailwindHandler
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

    let config = Docs.Config.load()
    app.Run(config.serverUrl)

    0 // Exit code

