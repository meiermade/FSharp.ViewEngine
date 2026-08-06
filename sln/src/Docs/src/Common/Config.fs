namespace Docs.Common

open System

module Env =
    let variableOrDefault (key:string) (defaultValue:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrEmpty(value) -> defaultValue
        | value -> value

type SeqConfig =
    { endpoint:string }

module SeqConfig =
    let load () =
        { endpoint = Env.variableOrDefault "SEQ_ENDPOINT" "http://localhost:5341" }

type ReleaseConfig =
    { version:string
      commit:string }

module ReleaseConfig =
    let load () =
        { version = Env.variableOrDefault "RELEASE_VERSION" "development"
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
