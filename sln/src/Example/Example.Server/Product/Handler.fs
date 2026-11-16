module Example.Server.Product.Handler

open System
open Microsoft.AspNetCore.Http
open Giraffe
open FSharp.ViewEngine
open Example.Server
open Example.Server.Common
open Example.Server.Product
open Example.Components.Views.Model
open Ledger.Domain
open Ledger.Operations

let navigation = Example.Components.Views.Product.navigation
let pageTitle = Example.Components.Views.Product.pageTitle

let tryPage (path: string) =
    match path.TrimEnd('/').Split('/') |> Array.toList with
    | [""; "examples"; "application"] -> Some ApplicationPage.Home
    | "" :: "examples" :: "application" :: segments ->
        [ Accounts.Handler.tryPage; Transactions.Handler.tryPage; Settings.Handler.tryPage; Profile.Handler.tryPage ]
        |> List.tryPick (fun find -> find segments)
    | _ -> None

let query services context =
    let requested = Request.query context
    { requested with topBarActions = View.actions services.config (requested.embedded || requested.fixture) }
    |> Session.apply services context

let available (records: Records) = function
    | ApplicationPage.Account id | ApplicationPage.EditAccount id | ApplicationPage.DeleteAccount id ->
        records.accounts |> List.exists (fun row -> row.id = id)
    | ApplicationPage.Transaction id | ApplicationPage.DeleteTransaction id ->
        records.transactions |> List.exists (fun row -> row.id = id)
    | _ -> true

let collectionNotification services context page query =
    match page with
    | ApplicationPage.Accounts -> Session.takeNotification services context query SelectionResource.Accounts
    | ApplicationPage.Transactions -> Session.takeNotification services context query SelectionResource.Transactions
    | _ -> query

/// The HTTP edge runs use cases; the shared Components renderer receives their typed outcomes.
let render page (query: Query) =
    let background = backgroundPage query page
    let query =
        match background with
        | ApplicationPage.Account id -> { query with account = string id }
        | _ -> query
    let home =
        match background with
        | ApplicationPage.Home | ApplicationPage.Account _ ->
            Some(Ledger.Home.readHome
                { records = query.records; account = query.account; period = query.range
                  comparison = query.comparison; startDate = query.startDate; endDate = query.endDate })
        | _ -> None
    let query =
        { query with
            submitAction = Navigation.formSubmitAction
            home = home
            profileValidation =
                if page = ApplicationPage.EditProfile && query.state = "invalid" then
                    Some(Ledger.Operations.validateProfile (profileRequest query)) else None
            organizationValidation =
                if page = ApplicationPage.EditOrganization && query.state = "invalid" then
                    Some(Ledger.Operations.validateOrganization (organizationRequest query)) else None
            environmentValidation =
                match page with
                | ApplicationPage.EditEnvironment id when query.state = "invalid" ->
                    Some(Ledger.Operations.validateEnvironment (environmentRequest query id))
                | _ -> None }
    Example.Components.Views.Product.render page query

let private isEditor = function
    | ApplicationPage.CreateAccount | ApplicationPage.EditAccount _ | ApplicationPage.EditProfile
    | ApplicationPage.EditOrganization | ApplicationPage.EditEnvironment _ -> true
    | _ -> false

let private selectionResource = function
    | ApplicationPage.Accounts | ApplicationPage.DeleteAccount _ -> SelectionResource.Accounts
    | _ -> SelectionResource.Transactions

/// Native pages and Specification fixtures share the same form execution and outcome policy.
/// Only the resulting page renderer differs; no feature depends on Specification markup.
let submit services renderPage page (requested: Query) (form: IFormCollection) : HttpHandler =
    fun next context ->
        task {
            // Example-only latency keeps the actual request indicator observable.
            if isEditor page then do! System.Threading.Tasks.Task.Delay(1000, context.RequestAborted)
            let submitted = Request.submitted context form |> Session.apply services context
            let submitted = { submitted with specification = requested.specification }
            let topBarActions = View.actions services.config (context.Request.Query["embedded"].ToString() = "1")
            let outcome =
                if not (available submitted.records page) then None
                else
                    match page with
                    | ApplicationPage.Accounts | ApplicationPage.Account _ | ApplicationPage.CreateAccount
                    | ApplicationPage.EditAccount _ | ApplicationPage.DeleteAccount _ ->
                        Accounts.Handler.submit services context submitted page form
                    | ApplicationPage.Transactions | ApplicationPage.Transaction _ | ApplicationPage.DeleteTransaction _ ->
                        Transactions.Handler.submit services context submitted page form
                    | ApplicationPage.EditOrganization | ApplicationPage.EditEnvironment _ -> Settings.Handler.submit page form
                    | ApplicationPage.EditProfile -> Profile.Handler.submit page form
                    | _ -> None
            match outcome with
            | Some result when result.draft.IsNone
                               && List.contains result.state ["delete-confirm"; "delete-blocked"; "selected"]
                               && (page = ApplicationPage.Accounts || page = ApplicationPage.Transactions) ->
                let response = { submitted with state = result.state; selection = result.selection; topBarActions = topBarActions }
                return! renderPage context page response next context
            | Some ({ draft = Some draft } as result) ->
                context.Response.Headers.CacheControl <- "private, no-store"
                // SSE initializes no-cache headers; keep private drafts no-store at flush too.
                context.Response.OnStarting(fun () ->
                    context.Response.Headers.CacheControl <- "private, no-store"
                    System.Threading.Tasks.Task.CompletedTask)
                let _, variant, resource = specificationDestination page result.state
                let response =
                    { submitted with state = result.state; specState = variant; resource = resource
                                     editorDraft = Some draft; topBarActions = topBarActions }
                return! renderPage context page response next context
            | Some result when result.state = "validated" && isEditor page ->
                let destination =
                    collectionHref
                        { submitted with state = ""; editorDraft = None; overlayFrom = "" }
                        (backgroundPage submitted page)
                return! redirectTo false destination next context
            | Some result ->
                if result.state = "selection-invalid" then
                    Session.notify services context submitted (selectionResource page) result.state
                context.Response.Headers.CacheControl <- "private, no-store"
                let collectionContext =
                    match page with
                    | ApplicationPage.Accounts | ApplicationPage.Transactions | ApplicationPage.CreateAccount
                    | ApplicationPage.EditAccount _ | ApplicationPage.DeleteAccount _ | ApplicationPage.DeleteTransaction _ ->
                        querySuffix (
                            collectionQueryPairs submitted
                            @ [ if not submitted.specification && submitted.overlayFrom <> "" && result.state <> "selection-deleted" then
                                    "from", submitted.overlayFrom ])
                    | _ -> ""
                let destination =
                    if submitted.specification then
                        let destinationPage = result.destination |> Option.defaultValue page
                        let workflow, variant, resource = specificationDestination destinationPage result.state
                        specificationHref workflow variant
                            { submitted with resource = resource
                                             overlayFrom = (if result.state = "selection-deleted" then "" else submitted.overlayFrom) }
                        + querySuffix [ "state", result.state ] + collectionContext
                    else
                        let path = result.destination |> Option.map applicationUrl |> Option.defaultValue (string context.Request.Path)
                        workspaceUrl path submitted.workspace
                        + querySuffix
                            [ yield "state", result.state
                              if submitted.embedded then yield "embedded", "1"
                              yield! returnWorkspacePairs submitted ]
                        + collectionContext
                return! redirectTo false destination next context
            | None -> return! (setStatusCode 404 >=> text "Example action not found.") next context
        }

let private renderPage context page requested = View.render context (pageTitle page requested) (render page requested)

let get services : HttpHandler =
    fun next context ->
        match tryPage (string context.Request.Path) with
        | Some page ->
            let requested = query services context
            if not (available requested.records page) then System.Threading.Tasks.Task.FromResult None
            else
                let requested = collectionNotification services context page requested
                renderPage context page requested next context
        | None -> System.Threading.Tasks.Task.FromResult None

let post services form : HttpHandler =
    fun next context ->
        match tryPage (string context.Request.Path) with
        | Some page -> submit services renderPage page (query services context) form next context
        | None -> System.Threading.Tasks.Task.FromResult None
