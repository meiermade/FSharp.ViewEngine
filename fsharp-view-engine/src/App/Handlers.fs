namespace App

open System
open System.IO
open Giraffe
open Microsoft.AspNetCore.Http
open Markdig
open FSharp.ViewEngine

module Handlers =
    
    let private markdownPipeline = MarkdownPipelineBuilder().UseAdvancedExtensions().Build()
    
    let private readMarkdownFile (fileName: string) =
        let filePath = Path.Combine(AppContext.BaseDirectory, "docs", fileName + ".md")
        if File.Exists(filePath) then
            let content = File.ReadAllText(filePath)
            Markdown.ToHtml(content, markdownPipeline)
        else
            "<h1>Page Not Found</h1><p>The requested page could not be found.</p>"
    
    let private renderPage (title: string) (fileName: string) : HttpHandler =
        fun next ctx -> task {
            let currentPath = ctx.Request.Path.Value
            let markdownContent = readMarkdownFile fileName
            let content = Views.layout title currentPath markdownContent
            let html = Element.render content
            return! htmlString html next ctx
        }
    
    // Route handlers
    let homeHandler : HttpHandler =
        renderPage "FSharp.ViewEngine Documentation" "home"

    let installationHandler : HttpHandler =
        renderPage "Installation - FSharp.ViewEngine" "installation"

    let quickstartHandler : HttpHandler =
        renderPage "Quickstart - FSharp.ViewEngine" "quickstart"
