namespace Docs.Common

open System

module Env =
    let variable (key:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrWhiteSpace(value) -> None
        | value -> Some value

    let variableOrDefault (key:string) (defaultValue:string) =
        variable key |> Option.defaultValue defaultValue

type SeqConfig =
    { endpoint:string }

module SeqConfig =
    let load () =
        { endpoint = Env.variableOrDefault "SEQ_ENDPOINT" "http://localhost:5341" }

type ReleaseConfig =
    { coreVersion:string option
      docsVersion:string option
      commit:string }

module ReleaseConfig =
    let load () =
        { coreVersion = Env.variable "RELEASE_CORE_VERSION"
          docsVersion = Env.variable "RELEASE_DOCS_VERSION"
          commit = Env.variableOrDefault "RELEASE_COMMIT" "local" }

type Config =
    { debug:bool
      appName:string
      serverUrl:string
      release:ReleaseConfig
      seq:SeqConfig }

module Config =
    let load () =
        { debug = Env.variableOrDefault "DEBUG" "false" |> Boolean.Parse
          appName = "fsharp-viewengine-docs"
          serverUrl = Env.variableOrDefault "SERVER_URL" "https://localhost:5000"
          release = ReleaseConfig.load ()
          seq = SeqConfig.load () }
