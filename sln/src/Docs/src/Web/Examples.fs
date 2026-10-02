namespace Docs.Web

open System
open Docs.Common
open Docs.Pages
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open Microsoft.AspNetCore.Http
open Giraffe
open type Html

/// Catalog-only Preview/Code chrome. The template files contain no dependency on this module.
module ExampleView =
    let private viewUrl (context:HttpContext) mode file =
        let pairs = context.Request.Query |> Seq.filter (fun pair -> not (List.contains pair.Key ["view"; "file"; "datastar"; "embedded"])) |> Seq.collect (fun pair -> pair.Value |> Seq.map (fun value -> System.Collections.Generic.KeyValuePair(pair.Key,string value))) |> Seq.toList
        let pairs = pairs @ (if mode="code" then [System.Collections.Generic.KeyValuePair("view","code"); System.Collections.Generic.KeyValuePair("file",file)] else [])
        context.Request.Path.ToString()+QueryString.Create(pairs).ToString()
    let private codeView context file =
        main {
            _id "main-content"; _tabindex -1; _class "mx-auto grid w-full min-w-0 max-w-7xl gap-5 p-4 outline-none sm:p-6"
            h1 { _class "text-xl font-semibold"; "Template source" }
            p { _class "text-sm text-[var(--fve-muted-text)]"; "Complete consumer-owned files. Start with Setup.md; download the project and source files together. The catalog viewer bar is not included." }
            nav {
                _ariaLabel "Template source files"; _class "flex min-w-0 flex-wrap gap-1"
                for sourceFile in Examples.sourceFiles do
                    a {
                        _href (viewUrl context "code" sourceFile)
                        _class "inline-flex min-h-8 items-center rounded-md px-3 text-sm font-medium hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] aria-[current=page]:bg-[var(--fve-surface-subtle)]"
                        if sourceFile=file then _ariaCurrent "page"
                        sourceFile
                    }
            }
            div { _class "flex min-w-0 flex-wrap items-center justify-between gap-3"; h2 { _class "font-mono text-sm font-semibold"; file }; a { _href ("/examples/source/"+file); _attr("download",file); _class "text-sm font-medium text-[var(--fve-brand-text)] underline underline-offset-2 focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; "Download file" } }
            CodeBlock.create (if file.EndsWith(".fs") then "fsharp" elif file.EndsWith(".fsproj") then "xml" else "text") (Examples.source file) |> CodeBlock.render
            p { _class "text-sm text-[var(--fve-muted-text)]"; "Install the listed components, compile your Tailwind stylesheet, and provide the assets configured in Layout.fs." }
        }
    let private root (context:HttpContext) (template:Examples.ExampleTemplate) (title:string) (content:HtmlElement) =
        let code = context.Request.Query["view"].ToString()="code"
        let requestedFile = context.Request.Query["file"].ToString()
        let file = if requestedFile="" then template.sourceFile else requestedFile
        let embedded = context.Request.Query["embedded"].ToString()="1"
        let appMode = template.template=Examples.Template.Specification && (context.Request.Query["appMode"].ToString()="1" || context.Request.Query["fveAppMode"].ToString()="app") && not code
        let chrome = not embedded && not appMode
        div {
            _id "docs-navigation-root"
            _attr("data-example-viewer",template.name)
            _data("on:financial-example-color-mode__window", "$colorMode = evt.detail")
            _attr("data-template-embedded",if embedded then "true" else "false")
            _class "min-h-screen bg-[var(--fve-background)] text-[var(--fve-text)]"
            _style (if chrome then "--example-chrome-height:3rem" else "--example-chrome-height:0px")
            if chrome then
                header {
                    _attr("data-example-viewer-bar","true")
                    _class "sticky top-0 z-40 flex min-h-12 min-w-0 flex-wrap items-center justify-between gap-2 border-b border-[var(--fve-border)] bg-[var(--fve-surface)] px-4 py-1"
                    div { _class "flex min-w-0 flex-wrap items-center gap-3"; Docs.Examples.Layout.link "/examples" "← Examples"; strong { _class "text-sm font-semibold"; template.name } }
                    div {
                        _class "flex min-w-0 flex-wrap items-center gap-2"
                        nav {
                            _ariaLabel "Example view"; _class "flex rounded-md bg-[var(--fve-surface-subtle)] p-0.5"
                            for mode,label in ["preview","Preview"; "code","Code"] do
                                a {
                                    _href (viewUrl context mode file)
                                    _class "inline-flex min-h-8 items-center rounded-md px-3 text-sm font-medium text-[var(--fve-muted-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] aria-[current=page]:bg-[var(--fve-surface)] aria-[current=page]:text-[var(--fve-text)] aria-[current=page]:shadow-sm"
                                    if (mode="code")=code then _ariaCurrent "page"
                                    label
                                }
                        }
                        Button.create (ButtonContent.Text "Theme") |> Button.withSize ControlSize.Small |> Button.withVariant ButtonVariant.Ghost |> Button.withAttributes [_ariaLabel "Toggle color mode"; _data("on:click","$colorMode = document.documentElement.classList.contains('dark') ? 'light' : 'dark'")] |> Button.render
                    }
                }
            if code then codeView context file else content
            span { _class "sr-only"; title }
        }
    let routes : HttpHandler =
        fun next context ->
            let path = context.Request.Path.ToString()
            if path.StartsWith("/examples/source/",StringComparison.Ordinal) then
                let file = path.Substring("/examples/source/".Length)
                if List.contains file Examples.sourceFiles then
                    (setHttpHeader "Content-Type" "text/plain; charset=utf-8" >=> setHttpHeader "Content-Disposition" ("attachment; filename=\""+System.IO.Path.GetFileName(file)+"\"") >=> setBodyFromString (Examples.source file)) next context
                else (setStatusCode 404 >=> Giraffe.Core.text "Source file not found.") next context
            else
                let value key = context.Request.Query[key].ToString()
                let query = Docs.Examples.Model.queryFromValues value
                let rendered =
                    match Docs.Examples.Routing.applicationPage path, Docs.Examples.Routing.specificationPage path, Docs.Examples.Routing.apiPage path with
                    | Some page,_,_ -> Some(Docs.Examples.Application.title page,Docs.Examples.Application.render page query)
                    | _,Some page,_ -> Some(Docs.Examples.Specification.title page,Docs.Examples.Specification.render page query)
                    | _,_,Some page -> Some(Docs.Examples.ApiDocumentation.title page,Docs.Examples.ApiDocumentation.render page)
                    | _ -> None
                match rendered, Examples.templateForPath path with
                | Some(title,content),Some template when value "file"="" || List.contains (value "file") Examples.sourceFiles ->
                    let registration = { Examples.registration with id="example-"+path.Replace('/','-'); path=path; title=title; browserTitle=title+" · "+template.name+" example" }
                    let page = DocumentationPage.create registration.id title |> DocumentationPage.withMetadata {DocsPageMetadata.defaults with browserTitle=Some registration.browserTitle}
                    let root = root context template title content
                    context.Response.Headers.CacheControl <- "private, no-store"
                    match Navigation.tryIntent context with
                    | Some intent -> Navigation.respond intent (Render.toString root) (View.contentMetadata Registry.navigation registration page |> Render.toString) next context
                    | None -> (View.documentWithContent Registry.navigation registration page root |> Render.toHtmlDocString |> htmlString) next context
                | _ when path.StartsWith("/examples/",StringComparison.Ordinal) -> (setStatusCode 404 >=> Giraffe.Core.text "Example page not found.") next context
                | _ -> skipPipeline
    let postRoutes : HttpHandler =
        fun next context ->
            if context.Request.Path.ToString().StartsWith("/examples/application",StringComparison.Ordinal) || context.Request.Path.ToString().StartsWith("/examples/specification",StringComparison.Ordinal) then Docs.Examples.Hosting.routes next context
            else skipPipeline
