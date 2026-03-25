open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Serilog
open Serilog.Events
open Serilog.Sinks.OpenTelemetry
open Docs.Handlers

let webApp =
    choose [
        GET >=> choose [
            route "/health" >=> text "ok"
            route "/" >=> homeHandler
            route "/installation" >=> installationHandler
            route "/custom" >=> customHandler
            route "/usage" >=> usageHandler
            route "/giraffe" >=> usageHandler
            route "/extensions/alpine" >=> alpineHandler
            route "/extensions/datastar" >=> datastarHandler
            route "/extensions/htmx" >=> htmxHandler
            route "/extensions/svg" >=> svgHandler
            route "/extensions/tailwind" >=> tailwindHandler
        ]
    ]

let configureLogger (config: Docs.Config) =
    let initialLogLevel =
        if config.debug then LogEventLevel.Debug
        else LogEventLevel.Information

    let logger =
        LoggerConfiguration()
            .MinimumLevel.Is(initialLogLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.OpenTelemetry(fun opts ->
                opts.Endpoint <- config.seq.endpoint + "/ingest/otlp/v1/logs"
                opts.Protocol <- OtlpProtocol.HttpProtobuf
                opts.ResourceAttributes <- dict [ "service.name", box config.appName ])
            .CreateLogger()

    Log.Logger <- logger

let configureApp (app : IApplicationBuilder) =
    app
        .UseSerilogRequestLogging(fun opts ->
            opts.GetLevel <- fun ctx _ _ ->
                if ctx.Request.Path.Value = "/health" then LogEventLevel.Verbose
                else LogEventLevel.Information)
        .UseStaticFiles() |> ignore
    app.UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services
        .AddSerilog()
        .AddGiraffe() |> ignore

[<EntryPoint>]
let main _args =
    let config = Docs.Config.load()
    configureLogger config

    try
        try
            let builder = WebApplication.CreateBuilder()
            configureServices builder.Services

            let app = builder.Build()

            if app.Environment.IsDevelopment() then
                app.UseDeveloperExceptionPage() |> ignore

            configureApp app

            Log.Information("Starting {AppName}", config.appName)
            app.Run(config.serverUrl)
            0
        with ex ->
            Log.Fatal(ex, "Application start-up failed")
            1
    finally
        Log.CloseAndFlush()
