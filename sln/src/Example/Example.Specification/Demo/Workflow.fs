module internal Example.Specification.Web.Demo.Workflow

open Microsoft.AspNetCore.Http
open Giraffe
open Example.Specification.Web.Common
open Example.UI.Views.Model
open Example.UI.Views.ViewData
open Example.Specification.Web

let available (records: Records) = function
    | ApplicationPage.Account id -> records.accounts |> List.exists (fun row -> row.id = id)
    | ApplicationPage.EditAccount id | ApplicationPage.DeleteAccount id ->
        records.accounts |> List.exists (fun row -> row.id = id && not (isRoot row))
    | ApplicationPage.Transaction id | ApplicationPage.DeleteTransaction id ->
        records.transactions |> List.exists (fun row -> row.id = id)
    | _ -> true

let collectionNotification services context page query =
    match page with
    | ApplicationPage.Accounts -> Session.takeNotification services context query ApplicationPage.Accounts
    | ApplicationPage.Transactions -> Session.takeNotification services context query ApplicationPage.Transactions
    | _ -> query

/// The HTTP edge runs use cases; the shared UI renderer receives their typed outcomes.
let render docsUrl page (query: Query) =
    let background = backgroundPage query page
    let query =
        match background with
        | ApplicationPage.Account id -> { query with account = string id }
        | _ -> query
    let home =
        match background with
        | ApplicationPage.Home | ApplicationPage.Account _ ->
            Some(Fixtures.home query)
        | _ -> None
    let query =
        { query with
            submitAction = Navigation.formSubmitAction
            home = home
            profileValidation =
                if page = ApplicationPage.EditProfile && query.state = "invalid" then
                    Actions.Profile.error (profileRequest query) else None
            organizationValidation =
                if page = ApplicationPage.EditOrganization && query.state = "invalid" then
                    Actions.Settings.organizationError (organizationRequest query) else None
            environmentValidation =
                match page with
                | ApplicationPage.EditEnvironment id when query.state = "invalid" ->
                    Actions.Settings.environmentError (environmentRequest query id)
                | _ -> None }
    Example.UI.Views.Product.render docsUrl page query

let private isEditor = function
    | ApplicationPage.CreateAccount | ApplicationPage.EditAccount _ | ApplicationPage.EditProfile
    | ApplicationPage.EditOrganization | ApplicationPage.EditEnvironment _ -> true
    | _ -> false

let private selectionResource = function
    | ApplicationPage.Accounts | ApplicationPage.DeleteAccount _ -> ApplicationPage.Accounts
    | _ -> ApplicationPage.Transactions

/// Apply fixture form outcomes without executing an Application use case.
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
                        Actions.Accounts.submit services context submitted page form
                    | ApplicationPage.Transactions | ApplicationPage.Transaction _ | ApplicationPage.DeleteTransaction _ ->
                        Actions.Transactions.submit services context submitted page form
                    | ApplicationPage.EditOrganization | ApplicationPage.EditEnvironment _ -> Actions.Settings.submit page form
                    | ApplicationPage.EditProfile -> Actions.Profile.submit page form
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
                return! Navigation.redirect destination next context
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
                return! Navigation.redirect destination next context
            | None -> return! (setStatusCode 404 >=> text "Example action not found.") next context
        }
