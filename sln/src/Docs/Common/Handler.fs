namespace Docs.Common

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

module Handler =
    let exampleMutation action : HttpHandler =
        fun next context ->
            task {
                let origin = context.Request.Headers.Origin.ToString()
                let expected = context.Request.Scheme + "://" + context.Request.Host.Value

                if origin <> "" && origin <> expected then
                    return! (setStatusCode 403 >=> text "Cross-origin fixture changes are not allowed.") next context
                elif context.Request.ContentLength.GetValueOrDefault() > 3_000_000L then
                    return! (setStatusCode 413 >=> text "The demo form exceeds its 3 MB request limit.") next context
                elif
                    isNull context.Request.ContentType
                    || not (
                        context.Request.ContentType.StartsWith(
                            "application/x-www-form-urlencoded",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                then
                    return!
                        (setStatusCode 415
                         >=> text
                                 "Submit URL-encoded form data. Multipart and file bodies are not accepted by documentation fixtures.")
                            next
                            context
                else
                    let size =
                        context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()

                    if not (isNull size) && not size.IsReadOnly then
                        size.MaxRequestBodySize <- Nullable 3_000_000L

                    try
                        let! form = context.Request.ReadFormAsync(context.RequestAborted)
                        context.Response.Headers.CacheControl <- "private, no-store"
                        return! action form next context
                    with
                    | :? Microsoft.AspNetCore.Http.BadHttpRequestException as error ->
                        return!
                            (setStatusCode error.StatusCode
                             >=> text "The demo form is invalid or exceeds its 3 MB request limit.")
                                next
                                context
                    | :? InvalidDataException ->
                        return! (setStatusCode 400 >=> text "Submit a valid form body.") next context
            }

    // Native example forms retain the documentation-owned App-mode envelope, not domain state.

    let redirectExample (destination: string) : HttpHandler =
        fun next context ->
            let expected = context.Request.Scheme + "://" + context.Request.Host.Value

            let destination =
                match Uri.TryCreate(context.Request.Headers.Referer.ToString(), UriKind.Absolute) with
                | true, referer when referer.GetLeftPart(UriPartial.Authority) = expected ->
                    let query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery referer.Query

                    let value key =
                        match query.TryGetValue key with
                        | true, value -> value.ToString()
                        | _ -> ""

                    if
                        value "fveAppMode" = "app"
                        && List.contains (value "fveAppFrame") [ "page-workspace"; "ledger-workflow" ]
                    then
                        let separator = if destination.Contains('?') then "&" else "?"
                        destination + separator + "fveAppMode=app&fveAppFrame=" + value "fveAppFrame"
                    else
                        destination
                | _ -> destination

            redirectTo false destination next context

    let readSignals (context: Microsoft.AspNetCore.Http.HttpContext) =
        task {
            use reader = new StreamReader(context.Request.Body)
            return! reader.ReadToEndAsync()
        }

    let trySignal (key: string) (body: string) =
        try
            use signals = JsonDocument.Parse body
            let mutable value = Unchecked.defaultof<JsonElement>

            if signals.RootElement.TryGetProperty(key, &value) then
                Some(value.Clone())
            else
                None
        with :? JsonException ->
            None

    let signalString key body =
        trySignal key body
        |> Option.bind (fun value ->
            if value.ValueKind = JsonValueKind.String then
                value.GetString() |> Option.ofObj
            else
                None)

    let signalInt key body =
        trySignal key body
        |> Option.bind (fun value ->
            match value.ValueKind with
            | JsonValueKind.Number ->
                match value.TryGetInt32() with
                | true, parsed -> Some parsed
                | false, _ -> None
            | JsonValueKind.String ->
                match Int32.TryParse(value.GetString()) with
                | true, parsed -> Some parsed
                | false, _ -> None
            | _ -> None)

    let componentChoicePatch render : HttpHandler =
        fun next context ->
            task {
                let! body = readSignals context
                let html = render body |> Render.toString

                return!
                    (setHttpHeader "Content-Type" "text/html; charset=utf-8"
                     >=> setBodyFromString html)
                        next
                        context
            }

    let componentFormChoice render : HttpHandler =
        fun next context ->
            task {
                if not context.Request.HasFormContentType then
                    return! (setStatusCode 415 >=> text "Submit ordinary form data.") next context
                else
                    let! form = context.Request.ReadFormAsync(context.RequestAborted)
                    let html = render form |> Render.toString

                    return!
                        (setHttpHeader "Content-Type" "text/html; charset=utf-8"
                         >=> setBodyFromString html)
                            next
                            context
            }

    let componentMultipleChoice (name: string) render : HttpHandler =
        fun next context ->
            task {
                if not context.Request.HasFormContentType then
                    return! (setStatusCode 415 >=> text "Submit ordinary form data.") next context
                else
                    let! form = context.Request.ReadFormAsync(context.RequestAborted)
                    let requested = form[name] |> Seq.toList |> List.distinct

                    let known =
                        requested
                        |> List.filter (fun value -> List.contains value [ "alex"; "jamie"; "riley"; "taylor" ])

                    let valid = requested = known && known.Length >= 1 && known.Length <= 3

                    let validation =
                        if valid then
                            None
                        else
                            Some "Choose one to three available members."

                    let result =
                        if valid then
                            Some $"Validated {known.Length} members."
                        else
                            None

                    let html = render known validation result |> Render.toString

                    return!
                        (setHttpHeader "Content-Type" "text/html; charset=utf-8"
                         >=> setBodyFromString html)
                            next
                            context
            }
