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
            let html =
                Components.paginationPageFor requestedPage
                |> View.documentWithPage Registry.navigation Components.paginationRegistration
                |> Render.toHtmlDocString
            htmlString html next context

    let private componentDropdownMenuPatch : HttpHandler =
        let html = Components.patchedDropdownMenuRegion |> Render.toString
        setHttpHeader "Content-Type" "text/html; charset=utf-8" >=> setBodyFromString html

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
            route "/components/choices/select" >=> componentSelectChoice
            route "/components/choices/checkbox" >=> componentCheckboxChoice
            route "/components/choices/switch" >=> componentSwitchChoice
            route "/components/choices/radio" >=> componentRadioChoice
        ]

    let routes : HttpHandler =
        choose [
            route "/sitemap.xml" >=> setHttpHeader "Content-Type" "application/xml; charset=utf-8" >=> setBodyFromString sitemap
            route "/robots.txt" >=> setHttpHeader "Content-Type" "text/plain; charset=utf-8" >=> setBodyFromString robots
            route "/components/pagination" >=> componentPagination
            route "/components/menus/actions" >=> componentDropdownMenuPatch
            route "/components/accounts/search" >=> componentAccountSearch
            choose (previewRoutes @ pageRoutes)
        ]
