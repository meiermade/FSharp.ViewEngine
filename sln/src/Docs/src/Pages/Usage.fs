namespace Docs.Pages

open Docs.Common

module Usage =
    let page =
        { id = "usage"
          path = "/usage"
          aliases = [ "/giraffe" ]
          navLabel = "Usage"
          category = "Getting started"
          title = "Usage"
          browserTitle = "Usage - FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "FSharp.ViewEngine integrates with "; Link("Giraffe", "https://giraffe.wiki/"); Text " by rendering elements to an HTML string and returning it via Giraffe's "; InlineContent.Code "htmlString"; Text " handler." ];
            Heading { id = "minimal-example"; title = "Minimal Example"; level = 2 };
            CodeBlock("fsharp", """open Microsoft.AspNetCore.Builder
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
    0""");
            Heading { id = "how-it-works"; title = "How It Works"; level = 2 };
            OrderedList [
                [ Text "Build your HTML using FSharp.ViewEngine elements" ];
                [ Text "Call "; InlineContent.Code "Render.toHtmlDocString"; Text " to get a "; InlineContent.Code "<!DOCTYPE html>"; Text " string (or "; InlineContent.Code "Render.toString"; Text " for a fragment without the doctype)" ];
                [ Text "Return it with Giraffe's "; InlineContent.Code "htmlString"; Text " handler" ]
            ];
            Paragraph [ Text "That's it — no special adapter or middleware needed." ];
            Heading { id = "title-elements"; title = "Title Elements"; level = 2 };
            Paragraph [ Text "Use "; InlineContent.Code "title \"My App\""; Text " for the common text-only form. Use "; InlineContent.Code "titleBuilder"; Text " when the title needs attributes or computation-expression content:" ];
            CodeBlock("fsharp", """head {
    titleBuilder {
        _lang "en"
        "My App"
    }
}""");
          ] }
