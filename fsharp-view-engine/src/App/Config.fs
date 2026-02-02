namespace App

open System

type Config =
    { serverUrl: string }

module Config =
    let load () =
        { serverUrl =
            Environment.GetEnvironmentVariable("SERVER_URL")
            |> Option.ofObj
            |> Option.defaultValue "https://localhost:5000" }
