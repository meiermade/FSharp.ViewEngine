namespace Docs.ComponentPages.InputGroup

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.ComponentPages.InputGroup.View

module Handler =
    let checkEmailHandler: HttpHandler =
        fun next context -> task {
            let email = context.Request.Query["email"].ToString()
            let valid = email.Length <= 254 && System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)
            do! System.Threading.Tasks.Task.Delay(1000, context.RequestAborted)
            let html = emailCheckResult valid |> Render.toString
            if context.Request.Headers["datastar-request"].ToString() = "true" then
                let service = context.RequestServices.GetRequiredService<IDatastarService>()
                do! service.PatchElementsAsync(html, cancellationToken = context.RequestAborted)
                return Some context
            else
                return! htmlString html next context
        }

    let checkEmailRoutes: HttpHandler =
        Giraffe.Core.GET >=> route "/components/input-group/check-email" >=> checkEmailHandler

    let get : HttpHandler = choose [checkEmailRoutes]
