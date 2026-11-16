namespace Docs.ComponentPages.Dialog

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

    let private componentItemDeletion: HttpHandler =
        fun next context ->
            task {
                do! System.Threading.Tasks.Task.Delay 1500

                let html =
                    Docs.ComponentPages.Dialog.View.itemDeletionContent (
                        Some "This item is still in use. Remove its links before deleting it."
                    )
                    |> Render.toString

                return!
                    (setHttpHeader "Content-Type" "text/html; charset=utf-8"
                     >=> setBodyFromString html)
                        next
                        context
            }

    let post : HttpHandler =
        choose [
            route "/components/dialogs/confirm" >=> componentItemDeletion
        ]
