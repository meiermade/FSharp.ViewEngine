open Docs
open Docs.Common
open Docs.Web
open Giraffe
open Serilog

let webApp (config: Config) exampleServices =
    Example.Server.Common.Handler.securityHeaders ["https://cdn.jsdelivr.net"]
    >=> choose [
        Example.Server.Program.routes exampleServices
        GET >=> choose [
            Health.Handler.routes config
            Discovery.Handler.routes
            Handler.routes
        ]
        POST >=> Handler.postRoutes
        Handler.notFound
    ]

[<EntryPoint>]
let main args =
    let config = Config.load ()
    let exampleServices = Example.Server.Config.load () |> Example.Server.Services.create
    Host.configureLogger config
    try
        try
            let app = Host.create args (webApp config exampleServices)
            Log.Information(
                "Starting {AppName} in {DeploymentEnvironment} at commit {ReleaseCommit} from {ReleaseImage}; Core {CoreVersion}, CLI {CliVersion}",
                config.appName, config.deploymentEnvironment, config.commit, config.image,
                config.corePackage.version, config.cliPackage.version)
            app.Run(config.serverUrl)
            0
        with ex ->
            Log.Fatal(ex, "Application start-up failed")
            1
    finally Log.CloseAndFlush()
