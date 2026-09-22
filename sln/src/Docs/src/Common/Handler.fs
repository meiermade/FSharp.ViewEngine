namespace Docs.Common

open System
open System.Net
open System.IO
open System.Text.Json
open Docs.Pages
open FSharp.ViewEngine
open Giraffe

module Handler =
    let private productionOrigin = "https://fve.meiermade.com"

    let sitemap =
        let urls =
            Registry.all
            |> List.map (fun page ->
                let location = productionOrigin + (if page.path = "/" then "/" else page.path)
                $"  <url><loc>{WebUtility.HtmlEncode location}</loc></url>")
            |> String.concat Environment.NewLine
        $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>{Environment.NewLine}<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">{Environment.NewLine}{urls}{Environment.NewLine}</urlset>{Environment.NewLine}"

    let robots = $"User-agent: *{Environment.NewLine}Allow: /{Environment.NewLine}Sitemap: {productionOrigin}/sitemap.xml{Environment.NewLine}"

    let render page : HttpHandler =
        let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
        htmlString html

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
            (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context

    let private fixture : HttpHandler =
        fun next context ->
            let fixtureState = context.Request.Query["fixtureState"].ToString()
            let html =
                Showcase.fixturePageFor fixtureState
                |> View.documentWithPage Registry.navigation Showcase.fixtureRegistration
                |> Render.toHtmlDocString
            htmlString html next context

    let private documentationSiteExample : HttpHandler =
        fun next context ->
            let state = context.Request.Query["fixtureState"].ToString()
            let html =
                Showcase.documentationSitePageFor state
                |> View.documentWithPage Registry.navigation Showcase.documentationSiteRegistration
                |> Render.toHtmlDocString
            htmlString html next context

    let private apiReferenceExample : HttpHandler =
        fun next context ->
            let state = context.Request.Query["fixtureState"].ToString()
            let html =
                Showcase.apiPageExampleFor state
                |> View.documentWithPage Registry.navigation Showcase.apiPageExampleRegistration
                |> Render.toHtmlDocString
            htmlString html next context

    let private componentAppShell : HttpHandler =
        fun next context ->
            let destination =
                match context.Request.Query["section"].ToString() with
                | "projects" -> Components.Projects
                | "settings" -> Components.Preferences
                | _ -> Components.Dashboard
            let html =
                Components.appShellPageFor destination
                |> View.documentWithPage Registry.navigation Components.appShellRegistration
                |> Render.toHtmlDocString
            htmlString html next context

    let private accountWorkspace (context:Microsoft.AspNetCore.Http.HttpContext) =
        let workspace = (PageExampleSession.get context).accounts
        { workspace with searchQuery=context.Request.Query["query"].ToString(); filterType=(let value=context.Request.Query["accountType"].ToString() in if value="" then "all" else value) }

    let private accountManagement : HttpHandler =
        fun next context ->
            let destination =
                context.Request.Query["destination"].ToString()
                |> Components.tryShellDestination
                |> Option.defaultValue Components.LedgerAccounts
            let html =
                Components.accountManagementPageWith (accountWorkspace context) destination
                |> View.documentWithPage Registry.navigation Components.accountManagementRegistration
                |> Render.toHtmlDocString
            (setHttpHeader "Cache-Control" "private, no-store" >=> htmlString html) next context

    let private exampleQuery (context:Microsoft.AspNetCore.Http.HttpContext) =
        let query key = context.Request.Query[key].ToString()
        PageExamples.queryFromStrings (query "state") (query "item") (query "view") (query "range")

    let private pageExample page : HttpHandler =
        fun next context ->
            let session = PageExampleSession.get context
            let html = Components.pageExamplePageFor page (exampleQuery context) session.messages session.photos |> View.documentWithPage Registry.navigation (PageExamples.registration page) |> Render.toHtmlDocString
            (setHttpHeader "Cache-Control" "private, no-store" >=> htmlString html) next context

    let private exampleMutation action : HttpHandler =
        fun next context ->
            task {
                let origin = context.Request.Headers.Origin.ToString()
                let expected = context.Request.Scheme + "://" + context.Request.Host.Value
                if origin <> "" && origin <> expected then return! (setStatusCode 403 >=> text "Cross-origin fixture changes are not allowed.") next context
                elif context.Request.ContentLength.GetValueOrDefault() > 3_000_000L then
                    return! (setStatusCode 413 >=> text "The demo form exceeds its 3 MB request limit.") next context
                elif not context.Request.HasFormContentType then return! (setStatusCode 415 >=> text "Submit form data.") next context
                else
                    let size = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()
                    if not (isNull size) && not size.IsReadOnly then size.MaxRequestBodySize <- Nullable 3_000_000L
                    try
                        let! form = context.Request.ReadFormAsync()
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
        exampleMutation (fun form next context -> task {
            let query = exampleQuery context
            let body = form["message"].ToString()
            let conversation = PageExamples.currentConversation query
            let result = PageExampleSession.change context (PageExampleSession.send conversation.id body)
            if context.Request.Headers.ContainsKey "Datastar-Request" then
                let messages,draft,error = match result with Ok messages -> messages,"",None | Error error -> (PageExampleSession.get context).messages,body,Some error
                let html = PageExamples.conversationPanel query messages draft error |> Render.toString
                return! (setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context
            else
                match result with
                | Ok _ -> return! redirectExample (PageExamples.queryUrl PageExamples.Messaging query) next context
                | Error error -> return! (setStatusCode 400 >=> text error) next context
        })

    let private saveExamplePhoto upload =
        exampleMutation (fun form next context -> task {
            let file = form.Files.GetFile "image"
            let! image = task {
                if isNull file || file.Length=0L || file.Length>2_000_000L then return None
                else
                    use stream = new MemoryStream()
                    do! file.CopyToAsync stream
                    return PageExampleSession.imageFromBytes (stream.ToArray())
            }
            let invalidFile = not (isNull file) && file.Length > 0L && image.IsNone
            let result =
                if invalidFile then None
                else PageExampleSession.change context (PageExampleSession.savePhoto (if upload then "new" else context.Request.Query["item"].ToString()) (form["name"].ToString()) (form["alt"].ToString()) image)
            let query =
                match result with
                | Some id -> { PageExamples.defaultQuery with item=id; view="saved" }
                | None -> { PageExamples.defaultQuery with item=(if upload then "" else context.Request.Query["item"].ToString()); view=(if upload then "upload-error" else "invalid") }
            return! redirectExample (PageExamples.queryUrl PageExamples.MediaManagement query) next context
        })

    let private createExampleAccount =
        exampleMutation (fun form next context ->
            let created = PageExampleSession.change context (PageExampleSession.createAccount (form["name"].ToString()) (form["accountType"].ToString()))
            let destination = created |> Option.map Components.LedgerAccount |> Option.defaultValue Components.LedgerCreateAccount
            redirectExample (Components.shellDestinationUrl destination) next context)

    let private saveExampleSettings =
        exampleMutation (fun form next context ->
            PageExampleSession.change context (PageExampleSession.saveSettings (form["workspaceName"].ToString()) (form.ContainsKey "emailUpdates")) |> ignore
            redirectExample (Components.shellDestinationUrl Components.LedgerSettings) next context)

    let private exampleImage imageId : HttpHandler =
        fun next context ->
            match (PageExampleSession.get context).images |> Map.tryFind imageId with
            | Some image -> (setHttpHeader "Cache-Control" "private, no-store" >=> setHttpHeader "X-Content-Type-Options" "nosniff" >=> setHttpHeader "Content-Type" image.contentType >=> setBody image.bytes) next context
            | None -> (setStatusCode 404 >=> text "Image not found in this demo session.") next context

    let private componentAppShellFixture : HttpHandler =
        fun next context ->
            let destination =
                context.Request.Query["destination"].ToString()
                |> Components.tryShellDestination
                |> Option.defaultValue Components.LedgerAccounts
            let html = Components.shellFixtureWith (accountWorkspace context) destination |> Render.toString
            (setHttpHeader "Cache-Control" "private, no-store" >=> setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html) next context

    let private componentDropdownMenuPatch : HttpHandler =
        let html = Components.patchedDropdownMenuRegion |> Render.toString
        setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html

    let private componentDrawerPatch : HttpHandler =
        let html = Components.patchedAccountDrawerContent |> Render.toString
        setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html

    let private componentTabsPatch : HttpHandler =
        let html = Components.reviewTabsRegion true |> Render.toString
        setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html

    let private componentConfirmationDialog : HttpHandler =
        fun next context ->
            task {
                do! System.Threading.Tasks.Task.Delay 1500
                let html =
                    Components.confirmationDialogContent (Some "Operating cannot be deleted while posted entries are assigned to its open period.")
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
            | Some "active" -> Components.selectFormRegion (Some Components.Active) None (Some "Accepted status: Active.")
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
        |> List.map (fun (path, html) -> route path >=> htmlString html)

    let postRoutes : HttpHandler =
        choose [
            route "/components/page-examples/account-management/create" >=> createExampleAccount
            route "/components/page-examples/account-management/settings" >=> saveExampleSettings
            route "/components/page-examples/messaging/send" >=> sendExampleMessage
            route "/components/page-examples/media-management/save" >=> saveExamplePhoto false
            route "/components/page-examples/media-management/upload" >=> saveExamplePhoto true
            route "/components/forms/contact" >=> componentContactForm Components.Stacked
            route "/components/forms/contact/grid" >=> componentContactForm Components.TwoColumns
            route "/components/forms/contact/sectioned" >=> componentContactForm Components.Sectioned
            route "/components/choices/select" >=> componentSelectChoice
            route "/components/choices/multiple-select" >=> componentMultipleChoice "reviewerIds" Components.multipleSelectForm
            route "/components/choices/multiple-combobox" >=> componentMultipleChoice "assigneeIds" Components.multipleComboboxForm
            route "/components/choices/checkbox" >=> componentCheckboxChoice
            route "/components/choices/switch" >=> componentSwitchChoice
            route "/components/choices/radio" >=> componentRadioChoice
            route "/components/dialogs/confirm" >=> componentConfirmationDialog
        ]

    let routes : HttpHandler =
        choose [
            route "/sitemap.xml" >=> setHttpHeader "Content-Type" "application/xml; charset=utf-8" >=> setBodyFromString sitemap
            route "/robots.txt" >=> setHttpHeader "Content-Type" "text/plain; charset=utf-8" >=> setBodyFromString robots
            route "/components/pagination/page" >=> componentPagination
            route "/components/table/sort" >=> componentTableSort
            route "/components/members/search" >=> componentMemberSearch false
            route "/components/members/field" >=> componentMemberSearch true
            GET >=> route "/components/page-examples/graph-and-trace" >=> redirectTo true "/components/page-examples/dependency-graph"
            GET >=> routef "/components/page-examples/images/%s" exampleImage
            GET >=> choose [ for page in PageExamples.pages -> route (PageExamples.url page) >=> pageExample page ]
            route "/components/page-examples/account-management/fixture" >=> componentAppShellFixture
            route "/components/page-examples/account-management" >=> accountManagement
            route "/components/app-shell" >=> componentAppShell
            // Keep the former page URL reachable while the catalog consolidates its examples into Fixture.
            route "/docs/components/interactive-examples" >=> fixture
            route "/docs/components/fixture" >=> fixture
            route "/docs/page-examples/documentation-site" >=> documentationSiteExample
            route "/docs/page-examples/api-reference" >=> apiReferenceExample
            route "/components/menus/actions" >=> componentDropdownMenuPatch
            route "/components/drawers/account" >=> componentDrawerPatch
            route "/components/tabs/review" >=> componentTabsPatch
            route "/components/accounts/search/settled" >=> componentAccountSearchSettled
            route "/components/accounts/search" >=> componentAccountSearch
            choose (previewRoutes @ pageRoutes)
        ]
