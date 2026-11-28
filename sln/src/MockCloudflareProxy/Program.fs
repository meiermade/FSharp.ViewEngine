module MockCloudflareProxy.Program

open System
open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Yarp.ReverseProxy.Forwarder

let private upstream name =
    match Uri.TryCreate(Environment.GetEnvironmentVariable name, UriKind.Absolute) with
    | true, uri when uri.Scheme = Uri.UriSchemeHttp && uri.AbsolutePath = "/"
                     && uri.UserInfo = "" && uri.Query = "" && uri.Fragment = "" -> uri.AbsoluteUri
    | _ -> invalidOp $"{name} must be an HTTP origin."

[<EntryPoint>]
let main args =
    let docs = upstream "MOCK_CLOUDFLARE_DOCS_ORIGIN"
    let example = upstream "MOCK_CLOUDFLARE_EXAMPLE_ORIGIN"
    let spec = upstream "MOCK_CLOUDFLARE_EXAMPLE_SPEC_ORIGIN"
    let exampleDocs = upstream "MOCK_CLOUDFLARE_EXAMPLE_DOCS_ORIGIN"
    let presentation = upstream "MOCK_CLOUDFLARE_EXAMPLE_PRESENTATION_ORIGIN"
    let builder = WebApplication.CreateBuilder(args)
    builder.Services.AddHttpForwarder() |> ignore
    let app = builder.Build()
    let forwarder = app.Services.GetRequiredService<IHttpForwarder>()
    use client = new HttpMessageInvoker(new SocketsHttpHandler(AllowAutoRedirect = false, UseCookies = false, UseProxy = false))
    app.Run(RequestDelegate(fun context -> task {
        // Match the tunnel's ordered rules; /examples itself belongs to the FVE catalog.
        let path = context.Request.Path
        let under prefix = path.StartsWithSegments(PathString prefix, StringComparison.Ordinal)
        let destination =
            if under "/examples/specification" then spec
            elif under "/examples/documentation" then exampleDocs
            elif under "/examples/presentation" then presentation
            elif under "/examples" && path <> PathString "/examples" && path <> PathString "/examples/" then example
            else docs
        let! _ = forwarder.SendAsync(context, destination, client, ForwarderRequestConfig.Empty, HttpTransformer.Empty)
        return ()
    }))
    app.Run()
    0
