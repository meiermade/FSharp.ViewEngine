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

type PackageRelease =
    { id:string
      version:string
      tag:string }

type Config =
    { debug:bool
      appName:string
      serverUrl:string
      publicOrigin:string
      deploymentEnvironment:string
      commit:string
      image:string
      corePackage:PackageRelease
      componentsPackage:PackageRelease
      openTelemetry:OpenTelemetryConfig }

module Config =
    let load () =
        let coreVersion = Env.variableOrDefault "CORE_PACKAGE_VERSION" "unreleased"
        let componentsVersion = Env.variableOrDefault "COMPONENTS_PACKAGE_VERSION" "unreleased"

        { debug = Env.variableOrDefault "DEBUG" "false" |> Boolean.Parse
          appName = "fsharp-viewengine-docs"
          serverUrl = Env.variableOrDefault "DOCS_SERVER_URL" "http://127.0.0.1:5054"
          publicOrigin = Env.variableOrDefault "DOCS_PUBLIC_ORIGIN" "http://127.0.0.1:5054"
          deploymentEnvironment = Env.variableOrDefault "DEPLOYMENT_ENVIRONMENT" "local"
          commit = Env.variableOrDefault "RELEASE_COMMIT" "local"
          image = Env.variableOrDefault "RELEASE_IMAGE" "local"
          corePackage =
            { id = "FSharp.ViewEngine"
              version = coreVersion
              tag = Env.variableOrDefault "CORE_PACKAGE_TAG" "unreleased" }
          componentsPackage =
            { id = "FSharp.ViewEngine.Components"
              version = componentsVersion
              tag = Env.variableOrDefault "COMPONENTS_PACKAGE_TAG" "unreleased" }
          openTelemetry = OpenTelemetryConfig.load () }
