module Example.Presentation.Web.Common.View

open System
open Microsoft.AspNetCore.Http
open Giraffe
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Presentation.Web

let nonce = Security.nonce

let actions (config: ServerConfig) embedded =
    if embedded then []
    else
        [ fveLink {
            Html._variant LinkVariant.Ghost
            Html._color Color.Neutral
            Html._size Size.Medium
            Html._padding Padding.Uniform
            Html._href config.sourceUrl
            Html._target "_blank"
            Html._rel "noopener"
            Html._ariaLabel "View example source on GitHub"
            Html._title "View example source on GitHub"
            fveIcon { Html._name IconName.GitHub; Html._size Size.Medium }
        } ]

let render (context: HttpContext) (title: string) content =
    let embedded = context.Request.Query["embedded"].ToString() = "1"
    let suffix = "Presentation"
    let title =
        if title.StartsWith("Geldos", StringComparison.Ordinal) then title
        else title + " · Geldos " + suffix
    let canonical =
        context.Request.Scheme + "://" + context.Request.Host.Value
        + string context.Request.PathBase + string context.Request.Path

    let metadata () =
        Html.fragment {
            Layout.documentTitle title
            Layout.canonicalLink canonical
        }
    context.Response.Headers.Append("Vary", "Datastar-Request, X-FVE-Interaction")
    match Navigation.tryIntent context with
    | Some intent ->
        Navigation.respond intent
            (Layout.navigationRoot embedded content |> Render.toString)
            (metadata () |> Render.toString)
    | None ->
        content
        |> Layout.documentWithNonce BrowserAssets.stylesheet BrowserAssets.script Navigation.initialScript Navigation.bodyAttributes (nonce context) canonical embedded title
        |> Render.toHtmlDocString
        |> htmlString
