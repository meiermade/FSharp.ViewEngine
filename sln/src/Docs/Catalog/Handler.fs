namespace Docs.Web

open System
open System.IO
open System.Text.Json
open Docs.Common
open Docs.ComponentPages.Icon
open Docs.Pages
open Docs.Web
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection

open Docs.Web.Response

module Handler =
    let private calendarSelection (context: Microsoft.AspNetCore.Http.HttpContext) : Docs.Common.CalendarExamples.CalendarDemoSelection =
        let query key = context.Request.Query[key].ToString()
        let defaults = Docs.Common.CalendarExamples.defaultCalendarSelection
        let date =
            match DateOnly.TryParseExact(query "calendarDate", "yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None) with
            | true, value when value >= DateOnly(2026, 9, 14) && value <= DateOnly(2026, 9, 20) -> value
            | _ -> defaults.date
        let parsedTime = TimeOnly.TryParseExact(query "selection.start", "HH:mm", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None)
        let parsedMinutes = Int32.TryParse(query "selection.minutes")
        let minutes = match parsedMinutes with true, value when List.contains value [30; 60; 90] -> value | _ -> defaults.minutes
        let start, submitted =
            match parsedTime, parsedMinutes with
            | (true, time), (true, duration) when List.contains duration [30; 60; 90] && time.Hour * 60 + time.Minute >= 360 && time.Hour * 60 + time.Minute + duration <= 1320 && time.Minute % 30 = 0 -> time, true
            | _ -> defaults.start, false
        { date = date; start = start; minutes = minutes; submitted = submitted }

    let private renderPage preview (page: DocPage) : HttpHandler =
        fun next context ->
            let browserState = context.Request.Query["state"].ToString()
            let expanded = context.Request.Query["expanded"].ToString() = "1"

            let response =
                if page.id = Docs.ComponentPages.Browser.View.browserRegistration.id && preview then
                    PreviewUpdate.respond [Docs.ComponentPages.Browser.View.browserReviewPreview expanded browserState] ""
                elif
                    (page.id = Docs.ComponentPages.Browser.View.browserRegistration.id
                     || page.id = Docs.ComponentPages.Phone.View.phoneRegistration.id)
                    && expanded
                then
                    let docsPage = DocumentationPage.create page.id page.title

                    let root =
                        Docs.Catalog.FrameReview.expandedReviewFixture (page.id = Docs.ComponentPages.Phone.View.phoneRegistration.id) browserState

                    View.documentWithContentAndNonce (Security.nonce context) Registry.navigation page docsPage root
                    |> respond
                elif page.id = Docs.ComponentPages.Browser.View.browserRegistration.id then
                    respondWithPage context page (Docs.ComponentPages.Browser.View.browserPageFor browserState)
                elif page.id = Docs.ComponentPages.Icon.View.iconRegistration.id then
                    let count =
                        match Int32.TryParse(context.Request.Query["count"].ToString()) with
                        | true, count -> count
                        | _ -> IconBrowser.batchSize
                    let search = context.Request.Query["search"].ToString()
                    if preview then
                        PreviewUpdate.respond [IconBrowser.render count search] "document.getElementById('icon-browser')?.dispatchEvent(new CustomEvent('docs-icons-updated'))"
                    else
                        respondWithPage context page (Docs.ComponentPages.Icon.View.iconPageFor count search)
                elif page.id = Docs.Guides.ComponentPages.themingRegistration.id then
                    let query key = context.Request.Query[key].ToString()
                    let requestedPage =
                        match Int32.TryParse(query "tokenPage") with
                        | true, number -> number
                        | _ -> 1
                    let state = ThemeTokens.select (query "tokenSearch") (query "tokenGroup") requestedPage
                    if preview then
                        PreviewUpdate.respondWithHistory [ThemeTokens.reference state] "document.getElementById('theme-token-reference').tokenSearch = location.search"
                    else
                        respondWithPage context page (Docs.Guides.ComponentPages.themingPageFor state)
                elif page.id = Docs.ComponentPages.Tabs.View.tabsRegistration.id then
                    let tab = context.Request.Query["tab"].ToString()
                    if preview then PreviewUpdate.respondWithHistory [Docs.ComponentPages.Tabs.View.serverTabsPreview tab] ""
                    else respondWithPage context page (Docs.ComponentPages.Tabs.View.tabsPageFor tab)
                elif page.id = Docs.ComponentPages.DayCalendar.View.dayCalendarRegistration.id then
                    let selection = calendarSelection context
                    if preview then
                        PreviewUpdate.respondWithHistory [Docs.ComponentPages.DayCalendar.View.dayControlsPreview selection; Docs.ComponentPages.DayCalendar.View.dayRangePreview selection] ""
                    else respondWithPage context page (Docs.ComponentPages.DayCalendar.View.dayCalendarPageFor selection)
                elif page.id = Docs.ComponentPages.WeekCalendar.View.weekCalendarRegistration.id then
                    let selection = calendarSelection context
                    if preview then PreviewUpdate.respondWithHistory [Docs.ComponentPages.WeekCalendar.View.weekControlsPreview selection] ""
                    else respondWithPage context page (Docs.ComponentPages.WeekCalendar.View.weekCalendarPageFor selection)
                elif
                    page.id = Docs.ComponentPages.Select.View.selectRegistration.id
                    && context.Request.Query["fixtures"].ToString() = "1"
                then
                    respondWithPage context page Docs.ComponentPages.Select.View.selectFixturePage
                elif page.id = Docs.ComponentPages.Table.View.tableRegistration.id then
                    let sort =
                        Docs.ComponentPages.Table.View.teamMemberSortFromQuery
                            (context.Request.Query["sort"].ToString())
                            (context.Request.Query["direction"].ToString())

                    respondWithPage context page (Docs.ComponentPages.Table.View.tablePageFor sort)
                else
                    respondFor context page

            response next context

    let render page = renderPage false page

    let private interactionRoutes =
        let ids =
            [ Docs.ComponentPages.Browser.View.browserRegistration.id; Docs.ComponentPages.Icon.View.iconRegistration.id
              Docs.Guides.ComponentPages.themingRegistration.id; Docs.ComponentPages.Tabs.View.tabsRegistration.id
              Docs.ComponentPages.DayCalendar.View.dayCalendarRegistration.id; Docs.ComponentPages.WeekCalendar.View.weekCalendarRegistration.id ]
        [ for page in Registry.all do
              if List.contains page.id ids then yield route (page.path + "/patch") >=> renderPage true page ]

    let private pageRoutes =
        Registry.all
        |> List.collect (fun page -> (page.path :: page.aliases) |> List.map (fun path -> route path >=> render page))

    let private previewRoutes =
        Showcase.previewRoutes
        |> Map.toList
        |> List.map (fun (path, html) ->
            match Showcase.tryPreviewPage path with
            | None -> route path >=> htmlString html
            | Some(site, page) ->
                route path
                >=> fun next context ->
                    let site =
                        { site with
                            assets =
                                { site.assets with
                                    productStylesheets = [ BrowserAssets.stylesheet ]
                                    datastarScript = Some BrowserAssets.script
                                    nonce = Docs.Web.Security.nonce context } }

                    let document = Document.create site page |> Document.render
                    respond document next context)

    let notFound: HttpHandler =
        fun next context ->
            let document =
                View.notFound
                    (Docs.Web.Security.nonce context)
                    (string context.Request.PathBase + string context.Request.Path)

            (setStatusCode 404 >=> (document |> Render.toHtmlDocString |> htmlString)) next context

    let postRoutes : HttpHandler =
        choose [
            Docs.ComponentPages.Progress.Handler.post
            Docs.ComponentPages.AccountManagement.Handler.post
            Docs.ComponentPages.Compositions.Handler.post
            Docs.ComponentPages.Dialog.Handler.post
            Docs.ComponentPages.FormLayouts.Handler.post
            Docs.ComponentPages.Select.Handler.post
            Docs.ComponentPages.Checkbox.Handler.post
            Docs.ComponentPages.Switch.Handler.post
            Docs.ComponentPages.RadioGroup.Handler.post
            Docs.ComponentPages.ToggleGroup.Handler.post
            Docs.ComponentPages.TagInput.Handler.post
            Docs.ComponentPages.Notification.Handler.post
        ]

    let routes : HttpHandler =
        choose [
            GET >=> choose [route "/guides/theming/document"; route "/components/theming/document"]
                >=> redirectTo true "/examples/application/profile"
            Docs.ComponentPages.Pagination.Handler.get
            Docs.ComponentPages.Table.Handler.get
            Docs.ComponentPages.AccountManagement.Handler.get Response.respondWithPage
            Docs.ComponentPages.Compositions.Handler.get Response.respondWithPage Docs.ComponentPages.Compositions.Page.pageExamplePageFor
            Docs.ComponentPages.DropdownMenu.Handler.get
            Docs.ComponentPages.Drawer.Handler.get
            Docs.ComponentPages.Tabs.Handler.get
            Docs.ComponentPages.Select.Handler.get
            Docs.ComponentPages.TagInput.Handler.get
            Docs.ComponentPages.Button.Handler.get
            Docs.ComponentPages.InputGroup.Handler.get
            choose (interactionRoutes @ previewRoutes @ pageRoutes)
            notFound
        ]
