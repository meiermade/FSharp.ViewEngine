namespace Docs.Web

open System
open System.Security.Cryptography
open Giraffe
open Microsoft.AspNetCore.Http

module Security =
    let nonce (context: HttpContext) =
        match context.Items.TryGetValue "fve-csp-nonce" with
        | true, (:? string as value) -> Some value
        | _ -> None

    let headers (additionalScriptSources: string list) : HttpHandler =
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
