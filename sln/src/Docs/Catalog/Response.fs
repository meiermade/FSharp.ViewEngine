namespace Docs.Web

open System
open System.IO
open System.Text.Json
open Docs.Common
open Docs.Pages
open Docs.Web
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection

module Response =
    let appMode (context: Microsoft.AspNetCore.Http.HttpContext) =
        let query key = context.Request.Query[key].ToString()

        if query "fveAppMode" <> "app" || String.IsNullOrWhiteSpace(query "fveAppFrame") then
            None
        else
            let remainingQuery =
                context.Request.Query
                |> Seq.filter (fun entry ->
                    not (
                        List.contains
                            entry.Key
                            [ "fveAppMode"
                              "fveAppFrame"
                              "fveAppDock"
                              "fveAppReturn"
                              "fveAppTransition"
                              "datastar" ]
                    ))
                |> Seq.collect (fun entry ->
                    entry.Value
                    |> Seq.map (fun value -> System.Collections.Generic.KeyValuePair(entry.Key, string value)))

            let exitHref =
                context.Request.PathBase.ToString()
                + context.Request.Path.ToString()
                + Microsoft.AspNetCore.Http.QueryString.Create(remainingQuery).ToString()

            Some(AppMode.create (query "fveAppFrame") exitHref)

    let respond document = document |> Render.toHtmlDocString |> htmlString

    let private pageResponse (context: HttpContext) registration page =
        context.Response.Headers.Append("Vary", "Datastar-Request")
        let mode = appMode context
        if History.requested context then
            let patch = View.pagePatch mode Registry.navigation registration page |> Render.toString
            let url = History.urlFor (string context.Request.Path) context
            History.patchPage patch
            >=> History.pushPageUrl url "window.fveColorMode?.refresh(); window.initializeDocsToc?.(); if (previous.split('?')[0] !== url.split('?')[0]) { const main = document.getElementById('main-content'); main?.scrollTo(0, 0); requestAnimationFrame(() => main?.focus({ preventScroll: true })); }"
        else
            match page with
            | Some page -> View.documentWithPageForAndNonce (Security.nonce context) mode Registry.navigation registration page |> respond
            | None -> View.documentForWithNonce (Security.nonce context) mode Registry.navigation registration |> respond

    let respondWithPage context registration page = pageResponse context registration (Some page)
    let respondFor context registration = pageResponse context registration None
