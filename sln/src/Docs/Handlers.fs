namespace Docs

open System
open System.IO
open System.Text.RegularExpressions
open Giraffe
open Microsoft.AspNetCore.Http
open Markdig
open FSharp.ViewEngine

module Handlers =

    let private markdownPipeline =
        MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build()

    let private readMarkdownFile (fileName: string) =
        let filePath = Path.Combine(AppContext.BaseDirectory, "docs", fileName + ".md")
        if File.Exists(filePath) then
            let content = File.ReadAllText(filePath)
            Markdown.ToHtml(content, markdownPipeline)
        else
            "<h1>Page Not Found</h1><p>The requested page could not be found.</p>"

    let private extractHeadings (html: string) =
        let pattern = """<h[23]\s+id="([^"]+)"[^>]*>([^<]+)</h[23]>"""
        Regex.Matches(html, pattern)
        |> Seq.cast<Match>
        |> Seq.map (fun m -> (m.Groups.[2].Value.Trim(), m.Groups.[1].Value))
        |> Seq.toList

    let private renderPage (title: string) (fileName: string) : HttpHandler =
        fun next ctx -> task {
            let currentPath = ctx.Request.Path.Value
            let markdownContent = readMarkdownFile fileName
            let headings = extractHeadings markdownContent
            let content = Views.layout title currentPath headings markdownContent
            let html = Render.toHtmlDocString content
            return! htmlString html next ctx
        }

    // Route handlers
    let homeHandler : HttpHandler =
        renderPage "FSharp.ViewEngine Documentation" "home"

    let installationHandler : HttpHandler =
        renderPage "Installation - FSharp.ViewEngine" "installation"

    let quickstartHandler : HttpHandler =
        renderPage "Quickstart - FSharp.ViewEngine" "quickstart"

    let alpineHandler : HttpHandler =
        renderPage "Alpine.js - FSharp.ViewEngine" "alpine"

    let datastarHandler : HttpHandler =
        renderPage "Datastar - FSharp.ViewEngine" "datastar"

    let htmxHandler : HttpHandler =
        renderPage "HTMX - FSharp.ViewEngine" "htmx"

    let svgHandler : HttpHandler =
        renderPage "SVG - FSharp.ViewEngine" "svg"

    let giraffeHandler : HttpHandler =
        renderPage "Giraffe - FSharp.ViewEngine" "giraffe"

    let tailwindHandler : HttpHandler =
        renderPage "Tailwind - FSharp.ViewEngine" "tailwind"
