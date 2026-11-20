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

    let respondWithPage context registration page =
        View.documentWithPageForAndNonce (Security.nonce context) (appMode context) Registry.navigation registration page
        |> respond
