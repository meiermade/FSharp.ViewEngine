namespace Docs.ComponentPages.Button

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

    let private componentButtonSettled: HttpHandler =
        fun next context ->
            task {
                do! System.Threading.Tasks.Task.Delay 1000
                return! setStatusCode 204 next context
            }

    let get : HttpHandler =
        choose [
            route "/components/button/settled" >=> componentButtonSettled
        ]
