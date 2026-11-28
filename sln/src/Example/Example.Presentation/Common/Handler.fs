module Example.Presentation.Web.Common.Handler

open System
open System.IO
open Microsoft.AspNetCore.Http
open Giraffe

let securityHeaders = Security.headers

/// One HTTP form boundary for native and enhanced submissions, before feature dispatch.
let form (handle: IFormCollection -> HttpHandler) : HttpHandler =
    fun next context ->
        task {
            let expected = context.Request.Scheme + "://" + context.Request.Host.Value
            let origin = context.Request.Headers.Origin.ToString()
            if origin <> "" && origin <> expected then
                return! (setStatusCode 403 >=> text "Cross-origin example submissions are not allowed.") next context
            elif context.Request.ContentLength.GetValueOrDefault() > 64_000L then
                return! (setStatusCode 413 >=> text "Example form exceeds 64 KB.") next context
            elif isNull context.Request.ContentType
                 || not (context.Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)) then
                return! (setStatusCode 415 >=> text "Submit a URL-encoded form.") next context
            else
                let size = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()
                if not (isNull size) && not size.IsReadOnly then size.MaxRequestBodySize <- Nullable 64_000L
                try
                    let! values = context.Request.ReadFormAsync(context.RequestAborted)
                    return! handle values next context
                with
                | :? BadHttpRequestException as error ->
                    return! (setStatusCode error.StatusCode >=> text "Invalid example form.") next context
                | :? InvalidDataException ->
                    return! (setStatusCode 400 >=> text "Invalid example form.") next context
                | :? OperationCanceledException when context.RequestAborted.IsCancellationRequested ->
                    return Some context
        }
