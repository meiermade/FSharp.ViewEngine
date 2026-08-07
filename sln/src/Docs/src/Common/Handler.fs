namespace Docs.Common

open Docs.Pages
open FSharp.ViewEngine
open Giraffe

module Handler =
    let render page : HttpHandler =
        let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
        htmlString html

    let private pageRoutes =
        Registry.all
        |> List.collect (fun page ->
            (page.path :: page.aliases)
            |> List.map (fun path -> route path >=> render page))

    let routes : HttpHandler = choose pageRoutes
