module Docs.Examples.Hosting

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Giraffe
open FSharp.ViewEngine
open Docs.Examples
open Model
open Ledger.Domain
open Ledger.Operations

// Standalone template host. Catalog-only source downloads are not part of this host.
let private query (context:HttpContext) =
    let value name = context.Request.Query[name].ToString()
    queryFromValues value
let private render title content = content |> Layout.document title |> Render.toHtmlDocString |> htmlString
let private pages : HttpHandler =
    fun next context ->
        let path = context.Request.Path.ToString()
        match Routing.applicationPage path, Routing.specificationPage path, Routing.apiPage path with
        | Some page,_,_ -> render (Application.pageTitle page (query context)) (Application.render page (query context)) next context
        | _,Some page,_ -> render (Specification.title page) (Specification.render page (query context)) next context
        | _,_,Some page -> render (ApiDocumentation.title page) (ApiDocumentation.render page) next context
        | _ -> (setStatusCode 404 >=> text "Example page not found.") next context

let private submit renderDocument topBarActions : HttpHandler =
    fun next context -> task {
        let expected = context.Request.Scheme+"://"+context.Request.Host.Value
        let origin = context.Request.Headers.Origin.ToString()
        if origin<>"" && origin<>expected then return! (setStatusCode 403 >=> text "Cross-origin example submissions are not allowed.") next context
        elif context.Request.ContentLength.GetValueOrDefault()>64_000L then return! (setStatusCode 413 >=> text "Example form exceeds 64 KB.") next context
        elif isNull context.Request.ContentType || not (context.Request.ContentType.StartsWith("application/x-www-form-urlencoded",StringComparison.OrdinalIgnoreCase)) then return! (setStatusCode 415 >=> text "Submit a URL-encoded form.") next context
        else
            let size = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()
            if not (isNull size) && not size.IsReadOnly then size.MaxRequestBodySize <- Nullable 64_000L
            let path = context.Request.Path.ToString()
            let requested = query context
            let applicationPage =
                match Routing.applicationPage path with
                | Some page -> Some page
                | None -> Routing.specificationPage path |> Option.filter (fun page -> Specification.workflowPages |> List.contains page) |> Option.map (fun page -> Specification.productPage page requested)
            try
                let! form = context.Request.ReadFormAsync(context.RequestAborted)
                let value name = form[name].ToString().Trim()
                let submittedContext = queryFromValues (fun name -> if name="accountType" && form.ContainsKey "filterAccountType" then value "filterAccountType" elif form.ContainsKey name then value name else context.Request.Query[name].ToString())
                let outcome =
                    match applicationPage with
                    | Some(ApplicationPage.CreateAccount) | Some(ApplicationPage.EditAccount _) as matched ->
                        let existing = match matched with Some(ApplicationPage.EditAccount id) -> Some id | _ -> None
                        let request = { existingAccountId=existing; name=form["name"].ToString(); accountType=value "accountType"; parentType=value "parentType"; currency=value "currency"; subtype=value "subtype"; observedBalance=value "observedBalance" }
                        let result = validateAccount request
                        let state =
                            match result with
                            | Ok _ -> "validated"
                            | Error ValidateAccountError.InvalidType -> "invalid-type"
                            | Error ValidateAccountError.InvalidDetails -> "invalid-details"
                            | Error _ -> "invalid"
                        Some(path,state,if Result.isError result then Some request else None)
                    | Some(ApplicationPage.Accounts) when value "action"="review-selected" ->
                        let result = reviewSelection {resource=SelectionResource.Accounts;keys=form["accountIds"] |> Seq.toList}
                        Some(path, (if Result.isOk result then "selection-valid" else "selection-invalid"), None)
                    | Some(ApplicationPage.Transactions) when value "action"="review-selected" ->
                        let result = reviewSelection {resource=SelectionResource.Transactions;keys=form["transactionIds"] |> Seq.toList}
                        Some(path, (if Result.isOk result then "selection-valid" else "selection-invalid"), None)
                    | Some(ApplicationPage.DeleteAccount id) when value "action"="delete" ->
                        Some(path, (if validateDeletion {accountId=id} |> Result.isOk then "deleted" else "delete-blocked"), None)
                    | Some(ApplicationPage.DeleteTransaction id) when value "action"="delete" ->
                        Some(path, (if validateTransactionDeletion {transactionId=id} |> Result.isOk then "deleted" else "delete-blocked"), None)
                    | Some(ApplicationPage.Settings) | Some(ApplicationPage.SettingsSection "organizations") ->
                        Some(path, (if validateOrganization {name=value "workspace";currency=value "currency"} |> Result.isOk then "validated" else "invalid"), None)
                    | Some(ApplicationPage.Profile) ->
                        Some(path, (if validateProfile {name=value "name";email=value "email";timeZone=value "timezone"} |> Result.isOk then "validated" else "invalid"), None)
                    | _ -> None
                match outcome with
                | Some(_,state,Some draft) ->
                    context.Response.Headers.CacheControl <- "private, no-store"
                    let page = applicationPage |> Option.get
                    let workflow,variant,resource = specificationDestination page state
                    let responseQuery = {submittedContext with state=state;specState=variant;resource=resource;accountDraft=Some draft;topBarActions=topBarActions}
                    // Error values live only in this no-store response, never a URL, cookie or durable store.
                    match Routing.specificationPage path with
                    | Some _ -> return! (renderDocument context (Specification.title workflow) (Specification.render workflow responseQuery) |> Render.toHtmlDocString |> htmlString) next context
                    | None -> return! (renderDocument context (Application.pageTitle page responseQuery) (Application.render page responseQuery) |> Render.toHtmlDocString |> htmlString) next context
                | Some(destination,state,None) ->
                    context.Response.Headers.CacheControl <- "private, no-store"
                    // Successful submissions and non-editor outcomes use finite states, without private submitted values.
                    let collectionContext =
                        match applicationPage with
                        | Some ApplicationPage.Accounts | Some ApplicationPage.Transactions
                        | Some ApplicationPage.CreateAccount | Some (ApplicationPage.EditAccount _) | Some (ApplicationPage.DeleteAccount _) | Some (ApplicationPage.DeleteTransaction _) ->
                            querySuffix (collectionQueryPairs submittedContext @ [if submittedContext.overlayFrom<>"" then "from",submittedContext.overlayFrom])
                        | _ -> ""
                    let destination =
                        if Routing.specificationPage path |> Option.isSome then
                            let page = applicationPage |> Option.get
                            let workflow,variant,resource = specificationDestination page state
                            specificationHref workflow variant {submittedContext with resource=resource;specification=true}+querySuffix ["state",state]+collectionContext
                        else workspaceUrl destination submittedContext.workspace+querySuffix [ yield "state",state; if submittedContext.embedded then yield "embedded","1" ]+collectionContext
                    return! redirectTo false destination next context
                | None -> return! (setStatusCode 404 >=> text "Example action not found.") next context
            with
            | :? BadHttpRequestException as error -> return! (setStatusCode error.StatusCode >=> text "Invalid example form.") next context
            | :? InvalidDataException -> return! (setStatusCode 400 >=> text "Invalid example form.") next context
    }
// The embedding host owns document identity and assets, including response-only validation errors.
let routesWithDocument renderDocument topBarActions : HttpHandler = choose [POST >=> submit renderDocument topBarActions; GET >=> pages]
let routesWithActions topBarActions : HttpHandler = routesWithDocument (fun _ title content -> Layout.document title content) topBarActions
let routes : HttpHandler = routesWithActions []

let main (args:string array) =
    let builder = WebApplication.CreateBuilder(args)
    builder.Services.AddGiraffe() |> ignore
    let app = builder.Build()
    app.UseStaticFiles() |> ignore
    app.UseGiraffe routes
    app.Run()
    0
