module Docs.Health.Handler

open Docs.Common
open Giraffe

let routes (config: Config) =
    route "/health" >=> json {|
        status = "ok"
        environment = config.deploymentEnvironment
        origin = config.publicOrigin
        commit = config.commit
        image = config.image
        packages = {| core = config.corePackage; cli = config.cliPackage |}
    |}
