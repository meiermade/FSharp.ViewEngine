open Docs.Common
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Serilog
open Serilog.Events
open Serilog.Sinks.OpenTelemetry

let webApp =
    choose [
        GET >=> choose [
            route "/health" >=> text "ok"
            Handler.routes
        ]
        setStatusCode 404 >=> text "Not found"
    ]

let configureLogger (config:Config) =
    let initialLogLevel =
        if config.debug then LogEventLevel.Debug
        else LogEventLevel.Information

    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Is(initialLogLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.OpenTelemetry(fun options ->
                options.Endpoint <- config.seq.endpoint + "/ingest/otlp/v1/logs"
                options.Protocol <- OtlpProtocol.HttpProtobuf
                options.ResourceAttributes <- dict [ "service.name", box config.appName ])
            .CreateLogger()

let configureApp (app:IApplicationBuilder) =
    app
        .UseSerilogRequestLogging(fun options ->
            options.GetLevel <- fun context _ _ ->
                if context.Request.Path.Value = "/health" then LogEventLevel.Verbose
                else LogEventLevel.Information)
        .UseStaticFiles()
    |> ignore
    app.UseGiraffe webApp

let configureServices (services:IServiceCollection) =
    services
        .AddSerilog()
        .AddGiraffe()
    |> ignore

[<EntryPoint>]
let main args =
    let config = Config.load ()
    configureLogger config

    try
        try
            let builder = WebApplication.CreateBuilder(args)
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
