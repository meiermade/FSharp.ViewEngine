module Example.Server.Hosting

open System
open System.IO
open System.Security.Cryptography
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open StarFederation.Datastar.DependencyInjection
open Giraffe
open FSharp.ViewEngine
open Example.Components
open Example.Server
open Model
open Ledger.Domain
open Ledger.Operations

// One standalone example host; the catalog mounts these same routes.
// Headers and nonce lifetime belong to the host, not to reusable controls or domain state.
let nonce (context: HttpContext) =
    match context.Items.TryGetValue "fve-csp-nonce" with
    | true, (:? string as value) -> Some value
    | _ -> None

let securityHeaders (additionalScriptSources: string list) : HttpHandler =
    fun next context ->
        let value = Convert.ToBase64String(RandomNumberGenerator.GetBytes 32)
        context.Items["fve-csp-nonce"] <- value
        let scriptSources = String.concat " " additionalScriptSources

        context.Response.Headers.ContentSecurityPolicy <-
            $"default-src 'self'; script-src 'self' 'nonce-{value}' {scriptSources}; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'self'"

        context.Response.Headers.CacheControl <- "private, no-store"
        context.Response.Headers["X-Content-Type-Options"] <- "nosniff"
        context.Response.Headers["Referrer-Policy"] <- "same-origin"
        next context

let sourceUrl =
    let commit = System.Environment.GetEnvironmentVariable "RELEASE_COMMIT"
    let repository = "https://github.com/meiermade/FSharp.ViewEngine"
    // Unpublished local work has no GitHub directory yet; never fabricate a source revision.
    if String.IsNullOrWhiteSpace commit || commit = "local" then
        repository
    else
        repository + "/tree/" + Uri.EscapeDataString(commit) + "/sln/src/Example"

let private sourceLink =
    Html.a {
        Html._href sourceUrl
        Html._target "_blank"
        Html._rel "noopener"
        Html._ariaLabel "View example source on GitHub"
        Html._title "View example source on GitHub"
        Html._class
            "inline-flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] [&_svg]:size-4"

        fveIcon { Html._name IconName.GitHub; Html._size Size.Small }
    }

let private actions embedded = if embedded then [] else [ sourceLink ]

let private query (context: HttpContext) =
    let value name = context.Request.Query[name].ToString()
    let query = queryFromValues value
    let workspace =
        match Routing.specificationPage (string context.Request.Path) with
        | Some page when List.contains page Specification.workflowPages -> (Specification.productContext page query).workspace
        | Some _ -> query.workspace
        | None -> query.workspace

    { query with
        workspace = workspace
        topBarActions = actions (query.embedded || query.fixture) }
    |> DemoState.apply context

let private available (records: Records) = function
    | ApplicationPage.Account id
    | ApplicationPage.EditAccount id
    | ApplicationPage.DeleteAccount id -> records.accounts |> List.exists (fun row -> row.id = id)
    | ApplicationPage.Transaction id
    | ApplicationPage.DeleteTransaction id -> records.transactions |> List.exists (fun row -> row.id = id)
    | _ -> true

let private collectionNotification context page query =
    match page with
    | ApplicationPage.Accounts -> DemoState.takeNotification context query SelectionResource.Accounts
    | ApplicationPage.Transactions -> DemoState.takeNotification context query SelectionResource.Transactions
    | _ -> query

let private render (context: HttpContext) (title: string) content =
    let embedded = context.Request.Query["embedded"].ToString() = "1"

    let suffix =
        if
            context.Request.Query["fveFixture"].ToString() = "product"
            || context.Request.Path.StartsWithSegments(PathString "/examples/application")
        then
            "Application"
        elif context.Request.Path.StartsWithSegments(PathString "/examples/specification") then
            "Specification"
        elif context.Request.Path.StartsWithSegments(PathString "/examples/marketing") then
            "Marketing"
        else
            "API documentation"

    let title =
        if title.StartsWith("Geldos", StringComparison.Ordinal) then title
        else title + " · Geldos " + suffix

    let canonical =
        context.Request.Scheme
        + "://"
        + context.Request.Host.Value
        + string context.Request.PathBase
        + string context.Request.Path

    match Navigation.tryIntent context with
    | Some intent ->
        let metadata =
            Html.fragment {
                Layout.documentTitle title
                Layout.canonicalLink canonical
            }
            |> Render.toString

        Navigation.respond
            (if HttpMethods.IsPost context.Request.Method then
                 Navigation.Restore
             else
                 intent)
            (Layout.navigationRoot embedded content |> Render.toString)
            metadata
    | None ->
        content
        |> Layout.documentWithNonce (nonce context) canonical embedded title
        |> Render.toHtmlDocString
        |> htmlString

let private pages: HttpHandler =
    fun next context ->
        let path = context.Request.Path.ToString()

        match Routing.applicationPage path, Routing.specificationPage path, Routing.apiPage path with
        | Some page, _, _ ->
            let requested = query context
            if not (available requested.records page) then System.Threading.Tasks.Task.FromResult None
            else
                let requested = collectionNotification context page requested
                render context (Application.pageTitle page requested) (Application.render page requested) next context
        | _, Some page, _ ->
            let requested = query context

            if requested.fixture then
                if List.contains page Specification.workflowPages then
                    let productPage = Specification.productPage page requested
                    if not (available requested.records productPage) then System.Threading.Tasks.Task.FromResult None
                    else
                        let requested = collectionNotification context productPage requested
                        render context (Specification.title page) (Specification.renderProduct page requested) next context
                else
                    (setStatusCode 404 >=> text "Choose a product workflow fixture.") next context
            else
                render context (Specification.title page) (Specification.render page requested) next context
        | _, _, Some page ->
            render
                context
                (ApiDocumentation.title page)
                (ApiDocumentation.renderWithActions (actions false) page)
                next
                context
        | _ when List.contains (path.TrimEnd('/')) Marketing.paths ->
            render context (Marketing.title path) (Marketing.render (path.TrimEnd('/')) (actions false)) next context
        | _ -> System.Threading.Tasks.Task.FromResult None

let private submit
    (renderResponse: HttpContext -> string -> HtmlElement -> HttpHandler)
    (topBarActions: HtmlElement list)
    : HttpHandler =
    fun next context ->
        task {
            let expected = context.Request.Scheme + "://" + context.Request.Host.Value
            let origin = context.Request.Headers.Origin.ToString()

            if origin <> "" && origin <> expected then
                return! (setStatusCode 403 >=> text "Cross-origin example submissions are not allowed.") next context
            elif context.Request.ContentLength.GetValueOrDefault() > 64_000L then
                return! (setStatusCode 413 >=> text "Example form exceeds 64 KB.") next context
            elif
                isNull context.Request.ContentType
                || not (
                    context.Request.ContentType.StartsWith(
                        "application/x-www-form-urlencoded",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            then
                return! (setStatusCode 415 >=> text "Submit a URL-encoded form.") next context
            else
                let size =
                    context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()

                if not (isNull size) && not size.IsReadOnly then
                    size.MaxRequestBodySize <- Nullable 64_000L

                let path = context.Request.Path.ToString()
                let requested = query context

                let applicationPage =
                    match Routing.applicationPage path with
                    | Some page -> Some page
                    | None ->
                        Routing.specificationPage path
                        |> Option.filter (fun page -> Specification.workflowPages |> List.contains page)
                        |> Option.map (fun page -> Specification.productPage page requested)

                try
                    let! form = context.Request.ReadFormAsync(context.RequestAborted)
                    let value name = form[name].ToString().Trim()
                    // An example-only delay makes the real request indicator observable; no client timer owns it.
                    match applicationPage with
                    | Some ApplicationPage.CreateAccount
                    | Some(ApplicationPage.EditAccount _)
                    | Some ApplicationPage.EditProfile
                    | Some ApplicationPage.EditOrganization
                    | Some(ApplicationPage.EditEnvironment _) ->
                        do! System.Threading.Tasks.Task.Delay(1000, context.RequestAborted)
                    | _ -> ()

                    let submittedContext =
                        queryFromValues (fun name ->
                            if name = "accountType" && form.ContainsKey "filterAccountType" then
                                value "filterAccountType"
                            elif form.ContainsKey name then
                                value name
                            else
                                context.Request.Query[name].ToString())
                        |> DemoState.apply context

                    let mutable selection = []
                    let delete resource keys =
                        match deleteSelection { records = submittedContext.records; resource = resource; keys = keys } with
                        | Ok result ->
                            DemoState.save context submittedContext.workspace resource result
                            "selection-deleted"
                        | Error DeleteSelectionError.HasActivity ->
                            selection <- keys |> List.distinct
                            "delete-blocked"
                        | Error _ -> "selection-invalid"

                    let outcome =
                        match applicationPage with
                        | Some page when not (available submittedContext.records page) -> None
                        | Some(ApplicationPage.CreateAccount)
                        | Some(ApplicationPage.EditAccount _) as matched ->
                            let existing =
                                match matched with
                                | Some(ApplicationPage.EditAccount id) -> Some id
                                | _ -> None

                            let request =
                                { existingAccountId = existing
                                  name = form["name"].ToString()
                                  accountType = value "accountType"
                                  parentType = value "parentType"
                                  currency = value "currency"
                                  subtype = value "subtype"
                                  observedBalance = value "observedBalance" }

                            let result = validateAccount request

                            let state =
                                match result with
                                | Ok _ -> "validated"
                                | Error ValidateAccountError.DuplicateName -> "duplicate-name"
                                | Error ValidateAccountError.InvalidType -> "invalid-type"
                                | Error ValidateAccountError.InvalidDetails -> "invalid-details"
                                | Error _ -> "invalid"

                            Some(
                                path,
                                state,
                                if Result.isError result then
                                    Some(EditorDraft.Account request)
                                else
                                    None
                            )
                        | Some page when (page = ApplicationPage.Accounts || page = ApplicationPage.Transactions) ->
                            let resource, field =
                                if page = ApplicationPage.Accounts then SelectionResource.Accounts, "accountIds"
                                else SelectionResource.Transactions, "transactionIds"
                            let keys = form[field] |> Seq.toList
                            match value "action" with
                            | "prepare-delete"
                            | "cancel-deletion" ->
                                match prepareDeletion { records = submittedContext.records; resource = resource; keys = keys } with
                                | Error _ -> Some(path, "selection-invalid", None)
                                | Ok prepared ->
                                    selection <- prepared.ids |> List.map string
                                    let state =
                                        if value "action" = "cancel-deletion" then "selected"
                                        elif prepared.blockedIds.IsEmpty then "delete-confirm"
                                        else "delete-blocked"
                                    Some(path, state, None)
                            | "delete-selected" -> Some(path, delete resource keys, None)
                            | "reset-records" ->
                                DemoState.reset context submittedContext.workspace resource
                                Some(path, "selection-reset", None)
                            | _ -> None
                        | Some(ApplicationPage.DeleteAccount id) when value "action" = "delete" ->
                            let state = delete SelectionResource.Accounts [string id]
                            Some((if state = "selection-deleted" then applicationUrl ApplicationPage.Accounts else path), state, None)
                        | Some(ApplicationPage.DeleteTransaction id) when value "action" = "delete" ->
                            let state = delete SelectionResource.Transactions [string id]
                            Some((if state = "selection-deleted" then applicationUrl ApplicationPage.Transactions else path), state, None)
                        | Some ApplicationPage.EditOrganization ->
                            let request =
                                { name = form["workspace"].ToString()
                                  currency = form["currency"].ToString() }

                            let valid = validateOrganization request |> Result.isOk

                            Some(
                                path,
                                (if valid then "validated" else "invalid"),
                                if valid then
                                    None
                                else
                                    Some(EditorDraft.Organization request)
                            )
                        | Some(ApplicationPage.EditEnvironment id) ->
                            let request =
                                { environmentId = id
                                  name = form["name"].ToString() }

                            let valid = validateEnvironment request |> Result.isOk

                            Some(
                                path,
                                (if valid then "validated" else "invalid"),
                                if valid then
                                    None
                                else
                                    Some(EditorDraft.Environment request)
                            )
                        | Some ApplicationPage.EditProfile ->
                            let request =
                                { name = form["name"].ToString()
                                  email = form["email"].ToString()
                                  timeZone = form["timezone"].ToString()
                                  dailySummaryTime = form["dailySummaryTime"].ToString() }

                            let valid = validateProfile request |> Result.isOk

                            Some(
                                path,
                                (if valid then "validated" else "invalid"),
                                if valid then None else Some(EditorDraft.Profile request)
                            )
                        | _ -> None

                    match outcome with
                    | Some(_, state, None) when
                        (state = "delete-confirm" || state = "delete-blocked" || state = "selected")
                        && (applicationPage = Some ApplicationPage.Accounts || applicationPage = Some ApplicationPage.Transactions) ->
                        let page = applicationPage.Value
                        let responseQuery = { submittedContext with state = state; selection = selection; topBarActions = topBarActions }
                        match Routing.specificationPage path with
                        | Some workflow ->
                            return! (renderResponse context (Specification.title workflow) (Specification.renderProduct workflow responseQuery)) next context
                        | None ->
                            return! (renderResponse context (Application.pageTitle page responseQuery) (Application.render page responseQuery)) next context
                    | Some(_, state, Some draft) ->
                        context.Response.Headers.CacheControl <- "private, no-store"
                        // The SSE transport initializes no-cache headers; private drafts require no-store at flush.
                        context.Response.OnStarting(fun () ->
                            context.Response.Headers.CacheControl <- "private, no-store"
                            System.Threading.Tasks.Task.CompletedTask)

                        let page = applicationPage |> Option.get
                        let workflow, variant, resource = specificationDestination page state

                        let responseQuery =
                            { submittedContext with
                                state = state
                                specState = variant
                                resource = resource
                                editorDraft = Some draft
                                topBarActions = topBarActions }
                        // Error values live only in this no-store response, never a URL, cookie or durable store.
                        match Routing.specificationPage path with
                        | Some _ ->
                            return!
                                (renderResponse
                                    context
                                    (Specification.title workflow)
                                    (Specification.renderProduct workflow responseQuery))
                                    next
                                    context
                        | None ->
                            return!
                                (renderResponse
                                    context
                                    (Application.pageTitle page responseQuery)
                                    (Application.render page responseQuery))
                                    next
                                    context
                    | Some(_, "validated", None) when
                        applicationPage
                        |> Option.exists (function
                            | ApplicationPage.CreateAccount
                            | ApplicationPage.EditAccount _
                            | ApplicationPage.EditProfile
                            | ApplicationPage.EditOrganization
                            | ApplicationPage.EditEnvironment _ -> true
                            | _ -> false)
                        ->
                        // Successful editors return to their origin; there is no validation-success interstitial.
                        let page = applicationPage |> Option.get
                        let origin = backgroundPage submittedContext page

                        let destination =
                            collectionHref
                                { submittedContext with
                                    state = ""
                                    editorDraft = None
                                    overlayFrom = ""
                                    specification = (Routing.specificationPage path |> Option.isSome) }
                                origin

                        return! redirectTo false destination next context
                    | Some(destination, state, None) ->
                        if state = "selection-invalid" then
                            let resource =
                                match applicationPage with
                                | Some ApplicationPage.Accounts
                                | Some(ApplicationPage.DeleteAccount _) -> SelectionResource.Accounts
                                | _ -> SelectionResource.Transactions
                            DemoState.notify context submittedContext resource state
                        context.Response.Headers.CacheControl <- "private, no-store"
                        // Successful submissions and non-editor outcomes use finite states, without private submitted values.
                        let collectionContext =
                            match applicationPage with
                            | Some ApplicationPage.Accounts
                            | Some ApplicationPage.Transactions
                            | Some ApplicationPage.CreateAccount
                            | Some(ApplicationPage.EditAccount _)
                            | Some(ApplicationPage.DeleteAccount _)
                            | Some(ApplicationPage.DeleteTransaction _) ->
                                querySuffix (
                                    collectionQueryPairs submittedContext
                                    @ [ if
                                            (Routing.specificationPage path |> Option.isNone)
                                            && submittedContext.overlayFrom <> ""
                                            && state <> "selection-deleted"
                                        then
                                            "from", submittedContext.overlayFrom ]
                                )
                            | _ -> ""

                        let destination =
                            if Routing.specificationPage path |> Option.isSome then
                                let page =
                                    match applicationPage.Value with
                                    | ApplicationPage.DeleteAccount _ when state = "selection-deleted" -> ApplicationPage.Accounts
                                    | ApplicationPage.DeleteTransaction _ when state = "selection-deleted" -> ApplicationPage.Transactions
                                    | page -> page
                                let workflow, variant, resource = specificationDestination page state

                                specificationHref
                                    workflow
                                    variant
                                    { submittedContext with
                                        resource = resource
                                        overlayFrom = (if state = "selection-deleted" then "" else submittedContext.overlayFrom)
                                        specification = true }
                                + querySuffix [ "state", state ]
                                + collectionContext
                            else
                                workspaceUrl destination submittedContext.workspace
                                + querySuffix
                                    [ yield "state", state
                                      if submittedContext.embedded then
                                          yield "embedded", "1"
                                      yield! returnWorkspacePairs submittedContext ]
                                + collectionContext

                        return! redirectTo false destination next context
                    | None -> return! (setStatusCode 404 >=> text "Example action not found.") next context
                with
                | :? BadHttpRequestException as error ->
                    return! (setStatusCode error.StatusCode >=> text "Invalid example form.") next context
                | :? InvalidDataException -> return! (setStatusCode 400 >=> text "Invalid example form.") next context
                | :? OperationCanceledException when context.RequestAborted.IsCancellationRequested ->
                    return Some context
        }
// The embedding host owns document identity and assets, including response-only validation errors.
let routesWithResponse renderResponse topBarActions : HttpHandler =
    choose [ POST >=> submit renderResponse topBarActions; GET >=> pages ]

let routes: HttpHandler =
    fun next context ->
        if
            context.Request.Path.StartsWithSegments(PathString "/examples")
            && context.Request.Path <> PathString "/examples"
        then
            routesWithResponse render (actions (context.Request.Query["embedded"].ToString() = "1")) next context
        else
            System.Threading.Tasks.Task.FromResult None

/// The same published assets are used standalone and when mounted by Docs.
let useAssets (app: IApplicationBuilder) =
    let directory =
        Path.Combine(AppContext.BaseDirectory, "wwwroot", "examples", "assets")

    app.UseStaticFiles(
        StaticFileOptions(
            FileProvider = new PhysicalFileProvider(directory),
            RequestPath = PathString "/examples/assets"
        )
    )
    |> ignore

let main (args: string array) =
    let builder = WebApplication.CreateBuilder(args)
    builder.Services.AddGiraffe().AddDatastar() |> ignore
    let app = builder.Build()
    useAssets app

    app.UseGiraffe(
        securityHeaders []
        >=> choose
                [ GET >=> route "/" >=> redirectTo false "/examples/application"
                  routes
                  setStatusCode 404 >=> text "Not found" ]
    )

    app.Run()
    0
