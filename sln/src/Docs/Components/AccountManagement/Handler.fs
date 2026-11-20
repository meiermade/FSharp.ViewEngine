namespace Docs.ComponentPages.AccountManagement

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

    let private accountWorkspace (context: Microsoft.AspNetCore.Http.HttpContext) =
        let workspace =
            match context.Request.Query["view"].ToString() with
            | "account-created" ->
                { Docs.Common.SharedExamples.defaultAccountWorkspace with
                    feedback =
                        "Account submission validated. This resettable example does not create or retain records." }
            | "account-invalid" ->
                { Docs.Common.SharedExamples.defaultAccountWorkspace with
                    feedback = "Enter a unique account name and a valid account type." }
            | "settings-saved" ->
                { Docs.Common.SharedExamples.defaultAccountWorkspace with
                    feedback =
                        "Settings submission validated. This resettable example does not retain submitted values." }
            | "settings-invalid" ->
                { Docs.Common.SharedExamples.defaultAccountWorkspace with
                    feedback = "Enter a workspace name of 1–80 characters." }
            | _ -> Docs.Common.SharedExamples.defaultAccountWorkspace

        let filterType =
            context.Request.Query["accountType"].ToString()
            |> function
                | "" -> "all"
                | value -> value

        let sortColumn =
            context.Request.Query["sort"].ToString()
            |> function
                | "balance" -> "balance"
                | _ -> "name"

        let sortDirection =
            context.Request.Query["direction"].ToString()
            |> function
                | "desc" -> "desc"
                | _ -> "asc"

        let page =
            context.Request.Query["page"].ToString()
            |> Int32.TryParse
            |> function
                | true, value when value > 0 -> value
                | _ -> 1

        let visibleColumns =
            match context.Request.Query["columns"].ToString() with
            | "" -> Docs.Common.SharedExamples.defaultAccountWorkspace.visibleColumns
            | value ->
                value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                |> Set.ofArray
                |> Set.intersect (set [ "type"; "commodity"; "balance"; "total" ])

        { workspace with
            searchQuery = context.Request.Query["query"].ToString()
            filterType = filterType
            sortColumn = sortColumn
            sortDirection = sortDirection
            page = page
            visibleColumns = visibleColumns }

    let private accountManagement respondWithPage: HttpHandler =
        fun next context ->
            let destination =
                context.Request.Query["destination"].ToString()
                |> Docs.Common.SharedExamples.tryShellDestination
                |> Option.defaultValue Docs.Common.SharedExamples.LedgerAccounts

            (setHttpHeader "Cache-Control" "private, no-store"
             >=> respondWithPage
                     context
                     Docs.ComponentPages.AccountManagement.View.accountManagementRegistration
                     (Docs.ComponentPages.AccountManagement.View.accountManagementPageWith (accountWorkspace context) destination))
                next
                context

    let private accountDestination destination view =
        Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            Docs.Common.SharedExamples.shellDestinationUrl destination,
            "view",
            view
        )

    let private createExampleAccount =
        exampleMutation (fun form next context ->
            let name = form["name"].ToString().Trim()
            let accountType = form["accountType"].ToString()

            let valid =
                name.Length > 0
                && name.Length <= 80
                && List.contains accountType [ "Asset"; "Liability"; "Equity"; "Revenue"; "Expense" ]
                && not (Docs.ComponentPages.AccountManagement.Examples.accountNameExists name)

            redirectExample
                (accountDestination
                    Docs.Common.SharedExamples.LedgerCreateAccount
                    (if valid then "account-created" else "account-invalid"))
                next
                context)

    let private saveExampleSettings =
        exampleMutation (fun form next context ->
            let name = form["workspaceName"].ToString().Trim()
            let valid = name.Length > 0 && name.Length <= 80

            redirectExample
                (accountDestination Docs.Common.SharedExamples.LedgerSettings (if valid then "settings-saved" else "settings-invalid"))
                next
                context)

    let private componentAppShellFixture: HttpHandler =
        fun next context ->
            let destination =
                context.Request.Query["destination"].ToString()
                |> Docs.Common.SharedExamples.tryShellDestination
                |> Option.defaultValue Docs.Common.SharedExamples.LedgerAccounts

            let html =
                Docs.ComponentPages.AccountManagement.View.shellFixtureWith (accountWorkspace context) destination
                |> Render.toString

            (setHttpHeader "Content-Type" "text/html; charset=utf-8"
             >=> setBodyFromString html)
                next
                context

    let get renderPage : HttpHandler =
        choose [
            route "/components/page-examples/account-management/fixture" >=> componentAppShellFixture
            route "/components/page-examples/account-management" >=> accountManagement renderPage
        ]

    let post : HttpHandler =
        choose [
            route "/components/page-examples/account-management/create" >=> createExampleAccount
            route "/components/page-examples/account-management/settings" >=> saveExampleSettings
        ]
