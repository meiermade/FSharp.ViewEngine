namespace Docs.Common

open System

module Env =
    let variable (key:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrWhiteSpace(value) -> None
        | value -> Some value

    let variableOrDefault (key:string) (defaultValue:string) =
        variable key |> Option.defaultValue defaultValue

type OpenTelemetryConfig =
    { endpoint:string }

module OpenTelemetryConfig =
    let load () =
        { endpoint = Env.variableOrDefault "OTEL_EXPORTER_OTLP_ENDPOINT" "http://localhost:4318" }

    let logsEndpoint config =
        $"{config.endpoint.TrimEnd([| '/' |])}/v1/logs"

type Config =
    { debug:bool
      appName:string
      serverUrl:string
      commit:string
      openTelemetry:OpenTelemetryConfig }

module Config =
    let load () =
        { debug = Env.variableOrDefault "DEBUG" "false" |> Boolean.Parse
          appName = "fsharp-viewengine-docs"
          serverUrl = Env.variableOrDefault "SERVER_URL" "https://localhost:5000"
          commit = Env.variableOrDefault "RELEASE_COMMIT" "local"
          openTelemetry = OpenTelemetryConfig.load () }
