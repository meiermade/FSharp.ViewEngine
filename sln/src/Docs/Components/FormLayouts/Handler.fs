namespace Docs.ComponentPages.FormLayouts

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

    let private componentContactForm layout : HttpHandler =
        fun next context ->
            task {
                if not context.Request.HasFormContentType then
                    return! (setStatusCode 415 >=> text "Submit ordinary form data.") next context
                else
                    let! values = context.Request.ReadFormAsync(context.RequestAborted)

                    let details: Docs.ComponentPages.FormLayouts.View.ContactDetails =
                        { name = values["contactName"].ToString()
                          email = values["email"].ToString()
                          notes = values["notes"].ToString() }

                    let errors =
                        [ if String.IsNullOrWhiteSpace details.name then
                              "contactName", "Enter a contact name."
                          match System.Net.Mail.MailAddress.TryCreate details.email with
                          | true, address when address.Address = details.email && details.email.Contains('@') -> ()
                          | _ -> "email", "Enter a valid email address." ]
                        |> Map.ofList

                    let html =
                        Docs.ComponentPages.FormLayouts.View.contactFormRegion layout details errors (Map.isEmpty errors)
                        |> Render.toString

                    return!
                        (setHttpHeader "Content-Type" "text/html; charset=utf-8"
                         >=> setBodyFromString html)
                            next
                            context
            }

    let post : HttpHandler =
        choose [
            route "/components/forms/contact" >=> componentContactForm View.ContactFormLayout.Stacked
            route "/components/forms/contact/grid" >=> componentContactForm View.TwoColumns
            route "/components/forms/contact/sectioned" >=> componentContactForm View.Sectioned
        ]
