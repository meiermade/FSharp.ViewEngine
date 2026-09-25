open Docs.Common
open Docs.Web
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Serilog
open Serilog.Events
open Serilog.Sinks.OpenTelemetry

let webApp (config:Config) =
    choose [
        GET >=> choose [
            route "/health" >=> json {|
                status = "ok"
                environment = config.deploymentEnvironment
                origin = config.publicOrigin
                commit = config.commit
                image = config.image
                packages = {|
                    core = config.corePackage
                    cli = config.cliPackage
                |}
            |}
            Handler.routes
        ]
        POST >=> Handler.postRoutes
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
                options.Endpoint <- OpenTelemetryConfig.logsEndpoint config.openTelemetry
                options.Protocol <- OtlpProtocol.HttpProtobuf
                options.ResourceAttributes <- dict [ "service.name", box config.appName ])
            .CreateLogger()

let configureApp (config:Config) (app:IApplicationBuilder) =
    app
        .UseForwardedHeaders()
        .UseSerilogRequestLogging(fun options ->
            options.GetLevel <- fun context _ _ ->
                if context.Request.Path.Value = "/health" then LogEventLevel.Verbose
                else LogEventLevel.Information)
        .UseStaticFiles()
    |> ignore
    app.UseGiraffe(webApp config)

let configureServices (services:IServiceCollection) =
    services.Configure<ForwardedHeadersOptions>(fun (options:ForwardedHeadersOptions) ->
        options.ForwardedHeaders <- ForwardedHeaders.XForwardedProto)
    |> ignore

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

            configureApp config app
            Log.Information(
                "Starting {AppName} in {DeploymentEnvironment} at commit {ReleaseCommit} from {ReleaseImage}; Core {CoreVersion}, CLI {CliVersion}",
                config.appName,
                config.deploymentEnvironment,
                config.commit,
                config.image,
                config.corePackage.version,
                config.cliPackage.version)

            app.Run(config.serverUrl)
            0
        with ex ->
            Log.Fatal(ex, "Application start-up failed")
            1
    finally
        Log.CloseAndFlush()
