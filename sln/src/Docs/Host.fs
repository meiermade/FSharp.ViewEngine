module Docs.Host

open Docs.Common
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Serilog
open Serilog.Events
open Serilog.Sinks.OpenTelemetry
open StarFederation.Datastar.DependencyInjection

let configureLogger (config: Config) =
    let initialLogLevel = if config.debug then LogEventLevel.Debug else LogEventLevel.Information
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

let private configureServices (services: IServiceCollection) =
    services.Configure<ForwardedHeadersOptions>(fun (options: ForwardedHeadersOptions) ->
        options.ForwardedHeaders <- ForwardedHeaders.XForwardedProto) |> ignore
    services.AddSerilog().AddGiraffe().AddDatastar() |> ignore

let create (args: string array) routes =
    let builder = WebApplication.CreateBuilder(args)
    configureServices builder.Services
    let app = builder.Build()
    if app.Environment.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore
    app.UseForwardedHeaders()
        .UseSerilogRequestLogging(fun options ->
            options.GetLevel <- fun context _ _ ->
                if context.Request.Path.Value = "/health" then LogEventLevel.Verbose else LogEventLevel.Information)
        .UseStaticFiles(StaticFileOptions(OnPrepareResponse = System.Action<_>(BrowserAssets.prepareResponse))) |> ignore
    app.UseGiraffe(routes)
    app
