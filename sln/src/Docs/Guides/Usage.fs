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
          path = "/guides/giraffe"
          aliases = ["/usage"]
          navLabel = "Giraffe integration"
          category = "Guides"
          title = "Giraffe integration"
          browserTitle = "Giraffe integration · FSharp.ViewEngine"
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
            Heading { id = "history"; title = "Datastar navigation and history"; level = 2 };
            Paragraph [ Text "A "; Link("Link", "/components/link"); Text " uses Datastar’s GET action. For a native request, return a full document. For a Datastar request, patch the page and then decide whether to change the URL. Register services.AddDatastar() to use these Giraffe handlers:" ];
            CodeBlock("fsharp", """open System.Text.Json
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine

let patchElements element : HttpHandler =
    fun next ctx -> task {
        let ds = ctx.RequestServices.GetRequiredService<IDatastarService>()
        do! ds.PatchElementsAsync(Render.toString element, cancellationToken = ctx.RequestAborted)
        return! next ctx
    }

let pushUrl (path: string) : HttpHandler =
    fun _ ctx -> task {
        let ds = ctx.RequestServices.GetRequiredService<IDatastarService>()
        let url = JsonSerializer.Serialize path
        let script = $"if (location.pathname + location.search !== {url}) history.pushState(history.state, '', {url}); window.renderedPageUrl = location.pathname + location.search;"
        do! ds.ExecuteScriptAsync(script, ExecuteScriptOptions(AutoRemove = true), ctx.RequestAborted)
        return Some ctx
    }

let accounts : HttpHandler =
    fun next ctx ->
        let response =
            if ctx.Request.Headers["Datastar-Request"].ToString() = "true" then
                patchElements (accountsPanel ()) >=> pushUrl "/accounts"
            else
                htmlString (Render.toHtmlDocString (accountsPage ()))
        response next ctx""");
            Paragraph [ Text "Add these lines once to the shared layout so Back and Forward restore the selected page without reloading. Return a fragment rooted at "; InlineContent.Code "main#page-content"; Text " from the Datastar handler so it can update the content without replacing the layout." ];
            HighlightedCodeBlock("fsharp", Some "Layout.fs", """open FSharp.ViewEngine
open type Html
open type Datastar

let layout (pageTitle: string) (content: HtmlElement) =
    html {
        _lang "en"
        head {
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            title { pageTitle }
            link { _rel "stylesheet"; _href "/css/output.css" }
            script { _type "module"; _src "/js/app.js" }
        }
        body {
            // ??= sets the URL of the page on screen only if it hasn't been set yet.
            _dataInit "window.renderedPageUrl ??= location.pathname + location.search"

            // If Back or Forward selects a different page, fetch it.
            _dataOn ("popstate", ["window"], @"
                const selected = location.pathname + location.search;
                if (window.renderedPageUrl !== selected) {
                    @get(location.href);
                }
            ")

            main {
                _id "page-content"
                content
            }
        }
    }""", [ 16; 17; 19; 20; 21; 22; 23; 24; 25 ]);
            Paragraph [ Text "The comparison ignores the fragment, so jumping to a section doesn’t fetch the page. After a patch, "; InlineContent.Code "pushUrl"; Text " records the page now on screen. On Back or Forward the browser has already selected that URL, so the handler doesn’t add another entry." ];
            Paragraph [ Text "A button can patch in-page state without changing the URL. An applied filter can call its own patch endpoint and push the resulting shareable page URL, such as /accounts?type=asset. Restore that state with a GET of the shareable URL, not by replaying the filter request. Use replaceState instead of pushState when you want to update the current entry rather than add one." ];
            Paragraph [ Text "After a successful create POST, return a 303 redirect to the resource detail GET. Native browsers navigate normally; Datastar follows the redirect and the detail handler patches the view before pushing the new URL. On validation errors, return the form patch and leave history alone." ];
          ] }
