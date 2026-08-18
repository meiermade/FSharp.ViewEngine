namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open type Html

module Usage =
    let private minimalExampleSource = """open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe
open FSharp.ViewEngine
open type Html

let indexView =
    html {
        _lang "en"
        head {
            title { "My App" }
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
    0"""

    let private minimalExamplePreview =
        div {
            _style "min-height:12rem;border-radius:.65rem;background:white;padding:2rem;color:#111827;box-shadow:inset 0 0 0 1px #e5e7eb"
            h2 { _style "margin:0;font-size:1.5rem"; "Hello from FSharp.ViewEngine!" }
            p { _style "margin:.75rem 0 0;color:#4b5563"; "Served by Giraffe." }
        }

    let page =
        { id = "usage"
          path = "/usage"
          aliases = [ "/giraffe" ]
          navLabel = "Giraffe"
          category = "Integrations"
          title = "Giraffe"
          browserTitle = "Giraffe · FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "FSharp.ViewEngine integrates with "; Link("Giraffe", "https://giraffe.wiki/"); Text " by rendering elements to an HTML string and returning it via Giraffe's "; InlineContent.Code "htmlString"; Text " handler." ];
            Heading { id = "minimal-example"; title = "Minimal Example"; level = 2 };
            CodeBlock("fsharp", minimalExampleSource);
            Heading { id = "how-it-works"; title = "How It Works"; level = 2 };
            OrderedList [
                [ Text "Build your HTML using FSharp.ViewEngine elements" ];
                [ Text "Call "; InlineContent.Code "Render.toHtmlDocString"; Text " to get a "; InlineContent.Code "<!DOCTYPE html>"; Text " string (or "; InlineContent.Code "Render.toString"; Text " for a fragment without the doctype)" ];
                [ Text "Return it with Giraffe's "; InlineContent.Code "htmlString"; Text " handler" ]
            ];
            Paragraph [ Text "That's it — no special adapter or middleware needed." ];
            Heading { id = "title-elements"; title = "Title Elements"; level = 2 };
            Paragraph [ Text "Use the same computation-expression syntax as other regular HTML elements. The title accepts encoded text and global attributes:" ];
            CodeBlock("fsharp", """head {
    title {
        _lang "en"
        "My App"
    }
}""");
          ] }
