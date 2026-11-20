namespace Docs.ComponentPages.Compositions

open System
open System.IO
open System.Text.Json
open Docs.Common
open Docs.Web
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection

open Docs.Common.Handler

module Handler =

    let private exampleQuery (context: Microsoft.AspNetCore.Http.HttpContext) =
        let query key = context.Request.Query[key].ToString()
        Docs.ComponentPages.Compositions.View.queryFromStrings (query "state") (query "item") (query "view") (query "range")

    let private pageExample respondWithPage buildPage page : HttpHandler =
        fun next context ->
            respondWithPage
                context
                (Docs.ComponentPages.Compositions.View.registration page)
                (buildPage page (exampleQuery context))
                next
                context

    let private sendExampleMessage =
        exampleMutation (fun form next context ->
            let query = exampleQuery context
            let body = form["message"].ToString().Trim()

            let view =
                if String.IsNullOrWhiteSpace body then "message-empty"
                elif body.Length > 2000 then "message-too-long"
                else "sent"

            redirectExample (Docs.ComponentPages.Compositions.View.queryUrl Docs.ComponentPages.Compositions.View.Messaging { query with view = view }) next context)

    let private saveExamplePhoto upload =
        exampleMutation (fun form next context ->
            let name = form["name"].ToString().Trim()
            let alt = form["alt"].ToString().Trim()

            let valid =
                name.Length > 0 && name.Length <= 120 && alt.Length > 0 && alt.Length <= 500

            let query =
                if upload then
                    { Docs.ComponentPages.Compositions.View.defaultQuery with
                        item = if valid then Docs.ComponentPages.Compositions.View.uploadedPhoto.id else ""
                        view = if valid then "uploaded" else "upload-error" }
                else
                    { Docs.ComponentPages.Compositions.View.defaultQuery with
                        item = context.Request.Query["item"].ToString()
                        view = if valid then "saved" else "invalid" }

            redirectExample (Docs.ComponentPages.Compositions.View.queryUrl Docs.ComponentPages.Compositions.View.MediaManagement query) next context)

    let get renderPage buildPage : HttpHandler =
        choose [
            for page in Docs.ComponentPages.Compositions.View.pages -> route (Docs.ComponentPages.Compositions.View.url page) >=> pageExample renderPage buildPage page
        ]

    let post : HttpHandler =
        choose [
            route "/components/page-examples/messaging/send" >=> sendExampleMessage
            route "/components/page-examples/media-management/save" >=> saveExamplePhoto false
            route "/components/page-examples/media-management/upload" >=> saveExamplePhoto true
        ]
