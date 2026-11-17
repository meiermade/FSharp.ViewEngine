namespace Docs.ComponentPages.Notification

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

    let private componentNotification signalName render =
        componentChoicePatch (fun body ->
            let sequence =
                signalInt signalName body
                |> Option.defaultValue 1
                |> max 1
                |> min 10000

            render sequence)

    let post : HttpHandler =
        choose [
            route "/components/notifications/show" >=> componentNotification "components_notification_sequence" View.notificationLeadRegion
            route "/components/notifications/show-timed" >=> componentNotification "components_timed_notification_sequence" View.timedNotificationRegion
        ]
