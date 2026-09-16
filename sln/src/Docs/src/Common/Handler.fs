namespace Docs.Common

open System
open System.Net
open System.IO
open System.Text.Json
open Docs.Pages
open FSharp.ViewEngine
open Giraffe

module Handler =
    let private productionOrigin = "https://fsharpviewengine.meiermade.com"

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

    let private componentAppShell : HttpHandler =
        fun next context ->
            let destination =
                context.Request.Query["destination"].ToString()
                |> Components.tryShellDestination
                |> Option.defaultValue Components.LedgerAccounts
            let html =
                Components.appShellPageFor destination
                |> View.documentWithPage Registry.navigation Components.appShellRegistration
                |> Render.toHtmlDocString
            htmlString html next context

    let private componentAppShellFixture : HttpHandler =
        fun next context ->
            let destination =
                context.Request.Query["destination"].ToString()
                |> Components.tryShellDestination
                |> Option.defaultValue Components.LedgerAccounts
            let html = Components.shellFixtureFor destination |> Render.toString
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
            route "/components/app-shell/fixture" >=> componentAppShellFixture
            route "/components/app-shell" >=> componentAppShell
            // Keep the former page URL reachable while the catalog consolidates its examples into Fixture.
            route "/docs/components/interactive-examples" >=> fixture
            route "/docs/components/fixture" >=> fixture
            route "/components/menus/actions" >=> componentDropdownMenuPatch
            route "/components/drawers/account" >=> componentDrawerPatch
            route "/components/tabs/review" >=> componentTabsPatch
            route "/components/accounts/search/settled" >=> componentAccountSearchSettled
            route "/components/accounts/search" >=> componentAccountSearch
            choose (previewRoutes @ pageRoutes)
        ]
