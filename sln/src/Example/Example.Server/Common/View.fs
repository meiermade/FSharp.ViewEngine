module Example.Server.Common.View

open System
open Microsoft.AspNetCore.Http
open Giraffe
open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open Example.Server

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
    let suffix =
        if context.Request.Query["fveFixture"].ToString() = "product"
           || context.Request.Path.StartsWithSegments(PathString "/examples/application") then "Application"
        elif context.Request.Path.StartsWithSegments(PathString "/examples/specification") then "Specification"
        elif context.Request.Path.StartsWithSegments(PathString "/examples/marketing") then "Marketing"
        elif context.Request.Path = PathString "/examples/presentation" then "Presentation"
        else "API documentation"
    let title =
        if title.StartsWith("Geldos", StringComparison.Ordinal) then title
        else title + " · Geldos " + suffix
    let canonical =
        context.Request.Scheme + "://" + context.Request.Host.Value
        + string context.Request.PathBase + string context.Request.Path

    context.Response.Headers.Append("Vary", "Datastar-Request, X-FVE-Interaction")
    match Navigation.tryIntent context with
    | Some intent ->
        let metadata =
            Html.fragment {
                Layout.documentTitle title
                Layout.canonicalLink canonical
            } |> Render.toString
        Navigation.respond intent
            (Layout.navigationRoot embedded content |> Render.toString)
            metadata
    | None ->
        content
        |> Layout.documentWithNonce Navigation.initialScript Navigation.bodyAttributes (nonce context) canonical embedded title
        |> Render.toHtmlDocString
        |> htmlString
