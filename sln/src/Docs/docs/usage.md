# Usage

FSharp.ViewEngine integrates with [Giraffe](https://giraffe.wiki/) by rendering elements to an HTML string and returning it via Giraffe's `htmlString` handler.

## Minimal Example

```fsharp
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe
open FSharp.ViewEngine
open type Html

let indexView =
    html {
        _lang "en"
        head {
            title "My App"
            meta { _charset "utf-8" }
        }
        body {
            h1 { "Hello from FSharp.ViewEngine!" }
            p { "Served by Giraffe." }
        }
    }

let indexHandler : HttpHandler =
    fun next ctx ->
        let html = Render.toHtmlDocString indexView
        htmlString html next ctx

let webApp =
    choose [
        GET >=> route "/" >=> indexHandler
    ]

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    builder.Services.AddGiraffe() |> ignore

    let app = builder.Build()
    app.UseGiraffe(webApp)
    app.Run()
    0
```

## How It Works

1. Build your HTML using FSharp.ViewEngine elements
2. Call `Render.toHtmlDocString` to get a `<!DOCTYPE html>` string (or `Render.toString` for a fragment without the doctype)
3. Return it with Giraffe's `htmlString` handler

That's it — no special adapter or middleware needed.
