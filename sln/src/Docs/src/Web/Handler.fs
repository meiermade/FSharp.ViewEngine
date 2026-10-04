namespace Docs.Web

open System
open Docs.Common
open System.Net
open System.IO
open System.Text.Json
open Docs.Pages
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Templates
open FSharp.ViewEngine.Components
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection

module Handler =
    let private productionOrigin = "https://fve.meiermade.com"

    let sitemap =
        let urls =
            (Registry.all |> List.map _.path) @ Examples.paths
            |> List.map (fun path ->
                let location = productionOrigin + path
                $"  <url><loc>{WebUtility.HtmlEncode location}</loc></url>")
            |> String.concat Environment.NewLine
        $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>{Environment.NewLine}<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">{Environment.NewLine}{urls}{Environment.NewLine}</urlset>{Environment.NewLine}"

    let robots = $"User-agent: *{Environment.NewLine}Allow: /{Environment.NewLine}Sitemap: {productionOrigin}/sitemap.xml{Environment.NewLine}"

    let private appMode (context:Microsoft.AspNetCore.Http.HttpContext) =
        let query key = context.Request.Query[key].ToString()
        if query "fveAppMode" <> "app" || String.IsNullOrWhiteSpace(query "fveAppFrame") then None
        else
            let remainingQuery =
                context.Request.Query
                |> Seq.filter (fun entry -> not (List.contains entry.Key [ "fveAppMode"; "fveAppFrame"; "fveAppDock"; "fveAppReturn"; "fveAppTransition"; "datastar" ]))
                |> Seq.collect (fun entry -> entry.Value |> Seq.map (fun value -> System.Collections.Generic.KeyValuePair(entry.Key, string value)))
            let exitHref = context.Request.PathBase.ToString() + context.Request.Path.ToString() + Microsoft.AspNetCore.Http.QueryString.Create(remainingQuery).ToString()
            Some(AppMode.create (query "fveAppFrame") exitHref)

    let private respond intent document (root, metadata) : HttpHandler =
        match intent with
        | Some intent ->
            Navigation.respond intent (Render.toString root) (Render.toString metadata)
        | None ->
            document
            |> Render.toHtmlDocString
            |> htmlString

    let private respondWithPage intent context registration page =
        let mode = appMode context
        respond
            intent
            (View.documentWithPageFor mode Registry.navigation registration page)
            (View.navigationPageWithPage mode Registry.navigation registration page)

    let render (page:DocPage) : HttpHandler =
        fun next context ->
            let intent = Navigation.tryIntent context
            let mode = appMode context
            let response =
                if page.id = Components.tableRegistration.id then
                    let sort = Components.teamMemberSortFromQuery (context.Request.Query["sort"].ToString()) (context.Request.Query["direction"].ToString())
                    respondWithPage intent context page (Components.tablePageFor sort)
                else
                    respond
                        intent
                        (View.documentFor mode Registry.navigation page)
                        (View.navigationPage mode Registry.navigation page)
            response next context

    let private pageRoutes =
        Registry.all
        |> List.collect (fun page ->
            (page.path :: page.aliases)
            |> List.map (fun path -> route path >=> render page))

    let private componentPagination : HttpHandler =
        fun next context ->
            let requestedPage =
                match Int32.TryParse(context.Request.Query["page"].ToString()) with
                | true, page -> page
                | false, _ -> 2
            let html = Components.paginationPreviewRegion requestedPage |> Render.toString
            (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context

    let private componentTableSort : HttpHandler =
        fun next context ->
            let sort = context.Request.Query["sort"].ToString()
            let direction = context.Request.Query["direction"].ToString()
            let html =
                Components.teamMemberSortFromQuery sort direction
                |> Components.sortableTeamTablePreview
                |> Render.toString
            task {
                if context.Request.Headers["datastar-request"].ToString() = "true" then
                    let service = context.RequestServices.GetRequiredService<IDatastarService>()
                    do! service.PatchElementsAsync(html, cancellationToken = context.RequestAborted)
                    return Some context
                else
                    return! (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context
            }

    let private documentationSiteExample : HttpHandler =
        fun next context ->
            let intent = Navigation.tryIntent context
            let state = context.Request.Query["fixtureState"].ToString()
            respondWithPage intent context Showcase.documentationSiteRegistration (Showcase.documentationSitePageFor state) next context

    let private apiReferenceExample : HttpHandler =
        fun next context ->
            let intent = Navigation.tryIntent context
            let state = context.Request.Query["fixtureState"].ToString()
            respondWithPage intent context Showcase.apiPageExampleRegistration (Showcase.apiPageExampleFor state) next context

    let private componentAppShell : HttpHandler =
        fun next context ->
            let intent = Navigation.tryIntent context
            let destination =
                match context.Request.Query["section"].ToString() with
                | "projects" -> Components.Projects
                | "settings" -> Components.Preferences
                | _ -> Components.Dashboard
            respondWithPage intent context Components.appShellRegistration (Components.appShellPageFor destination) next context

    let private accountWorkspace (context:Microsoft.AspNetCore.Http.HttpContext) =
        let workspace =
            match context.Request.Query["view"].ToString() with
            | "account-created" ->
                { Components.defaultAccountWorkspace with
                    feedback = "Account submission validated. This resettable example does not create or retain records." }
            | "account-invalid" ->
                { Components.defaultAccountWorkspace with
                    feedback = "Enter a unique account name and a valid account type." }
            | "settings-saved" ->
                { Components.defaultAccountWorkspace with
                    feedback = "Settings submission validated. This resettable example does not retain submitted values." }
            | "settings-invalid" ->
                { Components.defaultAccountWorkspace with
                    feedback = "Enter a workspace name of 1–80 characters." }
            | _ -> Components.defaultAccountWorkspace
        let filterType = context.Request.Query["accountType"].ToString() |> function | "" -> "all" | value -> value
        let sortColumn = context.Request.Query["sort"].ToString() |> function | "balance" -> "balance" | _ -> "name"
        let sortDirection = context.Request.Query["direction"].ToString() |> function | "desc" -> "desc" | _ -> "asc"
        let page = context.Request.Query["page"].ToString() |> Int32.TryParse |> function | true, value when value > 0 -> value | _ -> 1
        let visibleColumns =
            match context.Request.Query["columns"].ToString() with
            | "" -> Components.defaultAccountWorkspace.visibleColumns
            | value -> value.Split(',', StringSplitOptions.RemoveEmptyEntries) |> Set.ofArray |> Set.intersect (set [ "type"; "commodity"; "balance"; "total" ])
        { workspace with searchQuery=context.Request.Query["query"].ToString(); filterType=filterType; sortColumn=sortColumn; sortDirection=sortDirection; page=page; visibleColumns=visibleColumns }

    let private accountManagement : HttpHandler =
        fun next context ->
            let intent = Navigation.tryIntent context
            let destination =
                context.Request.Query["destination"].ToString()
                |> Components.tryShellDestination
                |> Option.defaultValue Components.LedgerAccounts
            (setHttpHeader "Cache-Control" "private, no-store" >=> respondWithPage intent context Components.accountManagementRegistration (Components.accountManagementPageWith (accountWorkspace context) destination)) next context

    let private exampleQuery (context:Microsoft.AspNetCore.Http.HttpContext) =
        let query key = context.Request.Query[key].ToString()
        PageExamples.queryFromStrings (query "state") (query "item") (query "view") (query "range")

    let private pageExample page : HttpHandler =
        fun next context ->
            let intent = Navigation.tryIntent context
            respondWithPage intent context (PageExamples.registration page) (Components.pageExamplePageFor page (exampleQuery context)) next context

    let private exampleMutation action : HttpHandler =
        fun next context ->
            task {
                let origin = context.Request.Headers.Origin.ToString()
                let expected = context.Request.Scheme + "://" + context.Request.Host.Value
                if origin <> "" && origin <> expected then return! (setStatusCode 403 >=> text "Cross-origin fixture changes are not allowed.") next context
                elif context.Request.ContentLength.GetValueOrDefault() > 3_000_000L then
                    return! (setStatusCode 413 >=> text "The demo form exceeds its 3 MB request limit.") next context
                elif isNull context.Request.ContentType || not (context.Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)) then
                    return! (setStatusCode 415 >=> text "Submit URL-encoded form data. Multipart and file bodies are not accepted by documentation fixtures.") next context
                else
                    let size = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()
                    if not (isNull size) && not size.IsReadOnly then size.MaxRequestBodySize <- Nullable 3_000_000L
                    try
                        let! form = context.Request.ReadFormAsync(context.RequestAborted)
                        context.Response.Headers.CacheControl <- "private, no-store"
                        return! action form next context
                    with
                    | :? Microsoft.AspNetCore.Http.BadHttpRequestException as error ->
                        return! (setStatusCode error.StatusCode >=> text "The demo form is invalid or exceeds its 3 MB request limit.") next context
                    | :? InvalidDataException ->
                        return! (setStatusCode 400 >=> text "Submit a valid form body.") next context
            }

    // Native example forms retain the documentation-owned App-mode envelope, not domain state.
    let private redirectExample (destination:string) : HttpHandler =
        fun next context ->
            let expected = context.Request.Scheme + "://" + context.Request.Host.Value
            let destination =
                match Uri.TryCreate(context.Request.Headers.Referer.ToString(), UriKind.Absolute) with
                | true, referer when referer.GetLeftPart(UriPartial.Authority) = expected ->
                    let query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery referer.Query
                    let value key = match query.TryGetValue key with true, value -> value.ToString() | _ -> ""
                    if value "fveAppMode" = "app" && List.contains (value "fveAppFrame") ["page-workspace";"ledger-workflow"] then
                        let separator = if destination.Contains('?') then "&" else "?"
                        destination + separator + "fveAppMode=app&fveAppFrame=" + value "fveAppFrame"
                    else destination
                | _ -> destination
            redirectTo false destination next context

    let private sendExampleMessage =
        exampleMutation (fun form next context ->
            let query = exampleQuery context
            let body = form["message"].ToString().Trim()
            let view =
                if String.IsNullOrWhiteSpace body then "message-empty"
                elif body.Length > 2000 then "message-too-long"
                else "sent"
            redirectExample (PageExamples.queryUrl PageExamples.Messaging { query with view=view }) next context)

    let private saveExamplePhoto upload =
        exampleMutation (fun form next context ->
            let name = form["name"].ToString().Trim()
            let alt = form["alt"].ToString().Trim()
            let valid = name.Length > 0 && name.Length <= 120 && alt.Length > 0 && alt.Length <= 500
            let query =
                if upload then
                    { PageExamples.defaultQuery with
                        item = if valid then PageExamples.uploadedPhoto.id else ""
                        view = if valid then "uploaded" else "upload-error" }
                else
                    { PageExamples.defaultQuery with
                        item = context.Request.Query["item"].ToString()
                        view = if valid then "saved" else "invalid" }
            redirectExample (PageExamples.queryUrl PageExamples.MediaManagement query) next context)

    let private accountDestination destination view =
        Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(Components.shellDestinationUrl destination, "view", view)

    let private createExampleAccount =
        exampleMutation (fun form next context ->
            let name = form["name"].ToString().Trim()
            let accountType = form["accountType"].ToString()
            let valid =
                name.Length > 0
                && name.Length <= 80
                && List.contains accountType [ "Asset"; "Liability"; "Equity"; "Revenue"; "Expense" ]
                && not (Components.accountNameExists name)
            redirectExample (accountDestination Components.LedgerCreateAccount (if valid then "account-created" else "account-invalid")) next context)

    let private saveExampleSettings =
        exampleMutation (fun form next context ->
            let name = form["workspaceName"].ToString().Trim()
            let valid = name.Length > 0 && name.Length <= 80
            redirectExample (accountDestination Components.LedgerSettings (if valid then "settings-saved" else "settings-invalid")) next context)

    let private componentAppShellFixture : HttpHandler =
        fun next context ->
            let destination =
                context.Request.Query["destination"].ToString()
                |> Components.tryShellDestination
                |> Option.defaultValue Components.LedgerAccounts
            let html = Components.shellFixtureWith (accountWorkspace context) destination |> Render.toString
            (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context

    let private componentDropdownMenuPatch : HttpHandler =
        let html = Components.patchedDropdownMenuRegion |> Render.toString
        setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html

    let private componentDrawerPatch : HttpHandler =
        let html = Components.patchedAccountDrawerContent |> Render.toString
        setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html

    let private componentTabsPatch : HttpHandler =
        let html = Components.reviewTabsRegion true |> Render.toString
        setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html

    let private componentAccountDeletion : HttpHandler =
        fun next context ->
            task {
                do! System.Threading.Tasks.Task.Delay 1500
                let html =
                    Components.accountDeletionContent (Some "Operating cannot be deleted while posted entries are assigned to its open period.")
                    |> Render.toString
                return! (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context
            }

    let private readSignals (context:Microsoft.AspNetCore.Http.HttpContext) =
        task {
            use reader = new StreamReader(context.Request.Body)
            return! reader.ReadToEndAsync()
        }

    let private trySignal (key:string) (body:string) =
        try
            use signals = JsonDocument.Parse body
            let mutable value = Unchecked.defaultof<JsonElement>
            if signals.RootElement.TryGetProperty(key, &value) then Some(value.Clone()) else None
        with :? JsonException -> None

    let private signalString key body =
        trySignal key body
        |> Option.bind (fun value ->
            if value.ValueKind = JsonValueKind.String then value.GetString() |> Option.ofObj
            else None)

    let private signalBool key body =
        trySignal key body
        |> Option.bind (fun value ->
            match value.ValueKind with
            | JsonValueKind.True -> Some true
            | JsonValueKind.False -> Some false
            | JsonValueKind.String ->
                match Boolean.TryParse(value.GetString()) with
                | true, parsed -> Some parsed
                | false, _ -> None
            | _ -> None)
        |> Option.defaultValue false

    let private signalInt key body =
        trySignal key body
        |> Option.bind (fun value ->
            match value.ValueKind with
            | JsonValueKind.Number -> match value.TryGetInt32() with true, parsed -> Some parsed | false, _ -> None
            | JsonValueKind.String -> match Int32.TryParse(value.GetString()) with true, parsed -> Some parsed | false, _ -> None
            | _ -> None)

    let private componentChoicePatch render : HttpHandler =
        fun next context ->
            task {
                let! body = readSignals context
                let html = render body |> Render.toString
                return! (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context
            }

    let private componentContactForm layout : HttpHandler =
        fun next context ->
            task {
                if not context.Request.HasFormContentType then
                    return! (setStatusCode 415 >=> text "Submit ordinary form data.") next context
                else
                    let! values = context.Request.ReadFormAsync(context.RequestAborted)
                    let details : Components.ContactDetails =
                        { name = values["contactName"].ToString()
                          email = values["email"].ToString()
                          notes = values["notes"].ToString() }
                    let errors =
                        [ if String.IsNullOrWhiteSpace details.name then "contactName", "Enter a contact name."
                          match System.Net.Mail.MailAddress.TryCreate details.email with
                          | true, address when address.Address = details.email && details.email.Contains('@') -> ()
                          | _ -> "email", "Enter a valid email address." ]
                        |> Map.ofList
                    let html = Components.contactFormRegion layout details errors (Map.isEmpty errors) |> Render.toString
                    return! (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context
            }

    let private componentSelectChoice =
        componentChoicePatch (fun body ->
            match signalString "components_status_value" body with
            | Some "active" -> Components.selectFormRegion (Some Components.AccountStatus.Active) None (Some "Accepted status: Active.")
            | Some "pending" -> Components.selectFormRegion (Some Components.Pending) None (Some "Accepted status: Pending.")
            | Some "scheduled" -> Components.selectFormRegion (Some Components.Scheduled) None (Some "Accepted status: Scheduled.")
            | _ -> Components.selectFormRegion None (Some "Choose an available status.") None)

    let private componentCheckboxChoice =
        componentChoicePatch (fun body ->
            if signalBool "components_confirm_archived_review_checked" body then
                Components.checkboxFormRegion true None (Some "Archived-account review confirmed.")
            else
                Components.checkboxFormRegion false (Some "Confirm the archived-account review.") None)

    let private componentSwitchChoice =
        componentChoicePatch (fun body ->
            let enabled = signalBool "components_posting_notifications_enabled" body
            let result = if enabled then "Posting notifications enabled." else "Posting notifications disabled."
            Components.switchFormRegion enabled None (Some result))

    let private componentRadioChoice =
        componentChoicePatch (fun body ->
            match signalString "components_posting_mode_value" body with
            | Some "automatic" -> Components.radioGroupFormRegion (Some "automatic") None (Some "Accepted posting mode: Automatic.")
            | Some "manual" -> Components.radioGroupFormRegion (Some "manual") None (Some "Accepted posting mode: Manual review.")
            | _ -> Components.radioGroupFormRegion None (Some "Choose an available posting mode.") None)

    let private componentNotification =
        componentChoicePatch (fun body ->
            let sequence = signalInt "components_notification_sequence" body |> Option.defaultValue 1 |> max 1 |> min 10000
            Components.notificationLeadRegion sequence)

    let private componentMultipleChoice (name:string) render : HttpHandler =
        fun next context ->
            task {
                if not context.Request.HasFormContentType then
                    return! (setStatusCode 415 >=> text "Submit ordinary form data.") next context
                else
                    let! form = context.Request.ReadFormAsync(context.RequestAborted)
                    let requested = form[name] |> Seq.toList |> List.distinct
                    let known = requested |> List.filter (fun value -> List.contains value [ "alex"; "jamie"; "riley"; "taylor" ])
                    let valid = requested = known && known.Length >= 1 && known.Length <= 3
                    let validation = if valid then None else Some "Choose one to three available members."
                    let result = if valid then Some $"Validated {known.Length} members. This example does not save your data." else None
                    let html = render known validation result |> Render.toString
                    return! (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context
            }

    let private componentMemberSearch wholeField : HttpHandler =
        fun next context ->
            let query =
                signalString "components_remote_members_query" (context.Request.Query["datastar"].ToString())
                |> Option.defaultValue ""
            let retry = String.Equals(context.Request.Query["retry"].ToString(), "true", StringComparison.OrdinalIgnoreCase)
            let html = (if wholeField then Components.remoteMembersField query else Components.remoteMembersOptions query retry) |> Render.toString
            (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context

    let private componentAccountSearchSettled : HttpHandler =
        fun next context ->
            task {
                do! System.Threading.Tasks.Task.Delay 1000
                return! setStatusCode 204 next context
            }

    let private componentAccountSearch : HttpHandler =
        fun next context ->
            task {
                let query =
                    try
                        use signals = JsonDocument.Parse(context.Request.Query["datastar"].ToString())
                        signals.RootElement.GetProperty("account_query").GetString()
                        |> Option.ofObj
                        |> Option.defaultValue ""
                    with
                    | :? JsonException
                    | :? InvalidOperationException
                    | :? System.Collections.Generic.KeyNotFoundException -> ""
                let retry = String.Equals(context.Request.Query["retry"].ToString(), "true", StringComparison.OrdinalIgnoreCase)
                let delay =
                    if query.StartsWith("oper", StringComparison.OrdinalIgnoreCase) then 750
                    elif query.StartsWith("tax", StringComparison.OrdinalIgnoreCase) then 50
                    else 0
                if delay > 0 then do! System.Threading.Tasks.Task.Delay delay
                let html = Components.accountComboboxOptions query retry |> Render.toString
                return! (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context
            }

    let private previewRoutes =
        Showcase.previewRoutes
        |> Map.toList
        |> List.map (fun (path, html) ->
            match Showcase.tryPreviewPage path with
            | None -> route path >=> htmlString html
            | Some(site, page) ->
                let site = { site with assets = { site.assets with navigation = Some Navigation.enhancement } }
                let breadcrumbs = FSharp.ViewEngine.Components.Templates.Navigation.breadcrumbs site.navigation site.homeId page.activeId
                let document = Document.create site page |> Document.render
                let root = DocsView.navigationRootWithNavigation site breadcrumbs site.navigation Embedded page
                let metadata = DocsView.documentMetadata site page
                route path >=> fun next context ->
                    respond (Navigation.tryIntent context) document (root, metadata) next context)

    let postRoutes : HttpHandler =
        choose [
            ExampleView.postRoutes
            route "/components/page-examples/account-management/create" >=> createExampleAccount
            route "/components/page-examples/account-management/settings" >=> saveExampleSettings
            route "/components/page-examples/messaging/send" >=> sendExampleMessage
            route "/components/page-examples/media-management/save" >=> saveExamplePhoto false
            route "/components/page-examples/media-management/upload" >=> saveExamplePhoto true
            route "/components/forms/contact" >=> componentContactForm Components.ContactFormLayout.Stacked
            route "/components/forms/contact/grid" >=> componentContactForm Components.TwoColumns
            route "/components/forms/contact/sectioned" >=> componentContactForm Components.Sectioned
            route "/components/choices/select" >=> componentSelectChoice
            route "/components/choices/multiple-select" >=> componentMultipleChoice "reviewerIds" Components.multipleSelectForm
            route "/components/choices/multiple-combobox" >=> componentMultipleChoice "assigneeIds" Components.multipleComboboxForm
            route "/components/choices/checkbox" >=> componentCheckboxChoice
            route "/components/choices/switch" >=> componentSwitchChoice
            route "/components/choices/radio" >=> componentRadioChoice
            route "/components/notifications/show" >=> componentNotification
            route "/components/dialogs/confirm" >=> componentAccountDeletion
        ]

    let private migrationRoutes =
        [ "/components/primitives", "/components"; "/components/application", "/examples"
          "/components/page", "/examples/application"; "/components/app-shell", "/examples/application"
          "/components/collection", "/examples/application/accounts"; "/components/detail", "/examples/application/accounts/101"
          "/components/form-layouts", "/examples/application/accounts/new"; "/components/media-library", "/components/card"
          "/components/bulk-actions", "/examples/application/accounts"
          "/docs", "/examples"; "/docs/components", "/components"; "/docs-components", "/components"
          "/docs/components/layouts", "/examples/specification"; "/docs/components/content", "/components/code-block"
          "/docs/components/navigation", "/components/side-nav"; "/docs/components/api-reference", "/examples/api-documentation"
          "/docs/components/diagrams", "/components/mermaid"
          "/components/fixture", "/examples/specification"
          "/docs/components/fixture", "/examples/specification"; "/docs/components/interactive-examples", "/examples/specification"
          "/docs/page-examples/documentation-site", "/examples/specification"
          "/docs/page-examples/api-reference", "/examples/api-documentation"; "/docs/examples/api-reference", "/examples/api-documentation"
          "/docs/page-examples/executable-specification", "/examples/specification"; "/docs/examples/executable-specification", "/examples/specification" ]
        |> List.map (fun (path,destination) -> route path >=> redirectTo true destination)
        |> choose

    let routes : HttpHandler =
        choose [
            migrationRoutes
            ExampleView.routes
            route "/sitemap.xml" >=> setHttpHeader "Content-Type" "application/xml; charset=utf-8" >=> setBodyFromString sitemap
            route "/robots.txt" >=> setHttpHeader "Content-Type" "text/plain; charset=utf-8" >=> setBodyFromString robots
            route "/components/pagination/page" >=> componentPagination
            route "/components/table/sort" >=> componentTableSort
            route "/components/members/search" >=> componentMemberSearch false
            route "/components/members/field" >=> componentMemberSearch true
            GET >=> route "/components/page-examples/graph-and-trace" >=> redirectTo true "/components/page-examples/dependency-graph"
            GET >=> choose [ for page in PageExamples.pages -> route (PageExamples.url page) >=> pageExample page ]
            route "/components/page-examples/account-management/fixture" >=> componentAppShellFixture
            route "/components/page-examples/account-management" >=> accountManagement
            route "/components/app-shell" >=> componentAppShell
            route "/docs/page-examples/documentation-site" >=> documentationSiteExample
            route "/docs/page-examples/api-reference" >=> apiReferenceExample
            route "/components/menus/actions" >=> componentDropdownMenuPatch
            route "/components/drawers/account" >=> componentDrawerPatch
            route "/components/tabs/review" >=> componentTabsPatch
            route "/components/accounts/search/settled" >=> componentAccountSearchSettled
            route "/components/accounts/search" >=> componentAccountSearch
            choose (previewRoutes @ pageRoutes)
        ]
