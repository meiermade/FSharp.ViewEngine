namespace Docs.Web

open System
open System.IO
open System.IO.Compression
open System.Text
open Docs.Common
open Docs.Pages
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Templates
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Giraffe
open type Html

/// Catalog-only source delivery. Copied templates do not depend on this module.
module ExampleView =
    let private sourceArchive webRoot =
        use stream = new MemoryStream()
        do
            use archive = new ZipArchive(stream, ZipArchiveMode.Create, true)
            for file in Examples.sourceFiles do
                let entry = archive.CreateEntry(file, CompressionLevel.Fastest)
                use writer = new StreamWriter(entry.Open(), UTF8Encoding(false))
                writer.Write(Examples.source file)
            for file in [
                "fonts/noto-sans-latin.woff2"; "fonts/noto-sans-latin-italic.woff2"
                "fonts/noto-sans-mono-latin.woff2"; "fonts/OFL.txt"
                "css/prism-tomorrow.1.29.0.min.css"
                "scripts/datastar.1.0.4.js"; "scripts/mermaid.11.16.0.min.js"
                "scripts/prism.1.29.0.min.js"; "scripts/prism-fsharp.1.29.0.min.js"
                "scripts/prism-sql.1.29.0.min.js"; "scripts/prism-bash.1.29.0.min.js"
                "scripts/prism-json.1.29.0.min.js" ] do
                archive.CreateEntryFromFile(Path.Combine(webRoot, file), "wwwroot/"+file, CompressionLevel.Fastest) |> ignore
        stream.ToArray()

    let private downloadSource =
        a {
            _href "/examples/source.zip"
            _attr("download", "ledger-examples.zip")
            _ariaLabel "Download example source ZIP"
            _attr("title", "Download source ZIP")
            _class "inline-flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
            Docs.Examples.Layout.icon "M3 16.5v2.25A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75V16.5M12 3v12m0 0-4.5-4.5M12 15l4.5-4.5"
        }

    let private root (context:HttpContext) (template:Examples.ExampleTemplate) (title:string) (content:HtmlElement) =
        div {
            _id "docs-navigation-root"
            _attr("data-fve-navigation-root","true")
            _attr("data-example-viewer",template.name)
            _attr("data-template-embedded",if context.Request.Query["embedded"].ToString()="1" then "true" else "false")
            _class "min-h-dvh bg-[var(--fve-background)] text-[var(--fve-text)]"
            content
            span { _class "sr-only"; title }
        }

    let routes : HttpHandler =
        fun next context ->
            let path = context.Request.Path.ToString()
            if path="/examples/source.zip" then
                let webRoot = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath
                context.Response.Headers.CacheControl <- "private, no-store"
                (setHttpHeader "Content-Type" "application/zip"
                 >=> setHttpHeader "Content-Disposition" "attachment; filename=\"ledger-examples.zip\""
                 >=> setBody (sourceArchive webRoot)) next context
            elif path.StartsWith("/examples/source/",StringComparison.Ordinal) then
                let file = path.Substring("/examples/source/".Length)
                if List.contains file Examples.sourceFiles then
                    (setHttpHeader "Content-Type" "text/plain; charset=utf-8" >=> setHttpHeader "Content-Disposition" ("attachment; filename=\""+Path.GetFileName(file)+"\"") >=> setBodyFromString (Examples.source file)) next context
                else (setStatusCode 404 >=> Giraffe.Core.text "Source file not found.") next context
            else
                let value key = context.Request.Query[key].ToString()
                let query = Docs.Examples.Model.queryFromValues value
                let query = { query with topBarActions = if query.embedded then [] else [downloadSource] }
                let rendered =
                    match Docs.Examples.Routing.applicationPage path, Docs.Examples.Routing.specificationPage path, Docs.Examples.Routing.apiPage path with
                    | Some page,_,_ -> Some(Docs.Examples.Application.pageTitle page query,Docs.Examples.Application.render page query)
                    | _,Some page,_ -> Some(Docs.Examples.Specification.title page,Docs.Examples.Specification.render page query)
                    | _,_,Some page -> Some(Docs.Examples.ApiDocumentation.title page,Docs.Examples.ApiDocumentation.renderWithActions query.topBarActions page)
                    | _ -> None
                match rendered, Examples.templateForPath path with
                | Some(title,content),Some template when value "file"="" || List.contains (value "file") Examples.sourceFiles ->
                    let registration = { Examples.registration with id="example-"+path.Replace('/','-'); path=path; title=title; browserTitle=title+" · "+template.name+" example" }
                    let page = DocumentationPage.create registration.id title |> DocumentationPage.withMetadata {DocsPageMetadata.defaults with browserTitle=Some registration.browserTitle}
                    let root = root context template title content
                    context.Response.Headers.CacheControl <- "private, no-store"
                    match Navigation.tryIntent context with
                    | Some intent -> Navigation.respond intent (Render.toString root) (View.contentMetadata Registry.navigation registration page |> Render.toString) next context
                    | None -> (View.documentWithContentAndNonce (Docs.Examples.Hosting.nonce context) Registry.navigation registration page root |> Render.toHtmlDocString |> htmlString) next context
                | _ when path.StartsWith("/examples/",StringComparison.Ordinal) -> (setStatusCode 404 >=> Giraffe.Core.text "Example page not found.") next context
                | _ -> skipPipeline

    let postRoutes : HttpHandler =
        fun next context ->
            if context.Request.Path.ToString().StartsWith("/examples/application",StringComparison.Ordinal) || context.Request.Path.ToString().StartsWith("/examples/specification",StringComparison.Ordinal) then
                let renderDocument (context:HttpContext) title content =
                    let path = context.Request.Path.ToString()
                    match Examples.templateForPath path with
                    | Some template ->
                        let registration = { Examples.registration with id="example-"+path.Replace('/','-'); path=path; title=title; browserTitle=title+" · "+template.name+" example" }
                        let page = DocumentationPage.create registration.id title |> DocumentationPage.withMetadata {DocsPageMetadata.defaults with browserTitle=Some registration.browserTitle}
                        View.documentWithContentAndNonce (Docs.Examples.Hosting.nonce context) Registry.navigation registration page (root context template title content)
                    | None -> Docs.Examples.Layout.documentWithNonce (Docs.Examples.Hosting.nonce context) title content
                Docs.Examples.Hosting.routesWithDocument renderDocument [downloadSource] next context
            else skipPipeline
