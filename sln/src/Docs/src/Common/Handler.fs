namespace Docs.Common

open System
open System.Net
open System.Text.Json
open Docs.Pages
open FSharp.ViewEngine
open Giraffe

module Handler =
    let private productionOrigin = "https://fsharpviewengine.meiermade.com"

    let sitemap =
        let urls =
            Registry.all
            |> List.map (fun page ->
                let location = productionOrigin + (if page.path = "/" then "/" else page.path)
                $"  <url><loc>{WebUtility.HtmlEncode location}</loc></url>")
            |> String.concat Environment.NewLine
        $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>{Environment.NewLine}<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">{Environment.NewLine}{urls}{Environment.NewLine}</urlset>{Environment.NewLine}"

    let robots = $"User-agent: *{Environment.NewLine}Allow: /{Environment.NewLine}Sitemap: {productionOrigin}/sitemap.xml{Environment.NewLine}"

    let render page : HttpHandler =
        let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
        htmlString html

    let private pageRoutes =
        Registry.all
        |> List.collect (fun page ->
            (page.path :: page.aliases)
            |> List.map (fun path -> route path >=> render page))

    let private componentAccountSearch : HttpHandler =
        fun next context ->
            let query =
                try
                    use signals = JsonDocument.Parse(context.Request.Query["datastar"].ToString())
                    signals.RootElement.GetProperty("account_query").GetString()
                    |> Option.ofObj
                    |> Option.defaultValue ""
                with
                | :? JsonException
                | :? InvalidOperationException
                | :? System.Collections.Generic.KeyNotFoundException -> ""
            let html = Components.accountComboboxOptions query |> Render.toString
            setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html <| next <| context

    let private previewRoutes =
        Showcase.previewRoutes
        |> Map.toList
        |> List.map (fun (path, html) -> route path >=> htmlString html)

    let routes : HttpHandler =
        choose [
            route "/sitemap.xml" >=> setHttpHeader "Content-Type" "application/xml; charset=utf-8" >=> setBodyFromString sitemap
            route "/robots.txt" >=> setHttpHeader "Content-Type" "text/plain; charset=utf-8" >=> setBodyFromString robots
            route "/components/contract/accounts/search" >=> componentAccountSearch
            choose (previewRoutes @ pageRoutes)
        ]
