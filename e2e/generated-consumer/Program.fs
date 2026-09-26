open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open FSharp.ViewEngine
open Acme.Components.Primitives
open type FSharp.ViewEngine.Html

let button = Button.primary "Create account"

let select =
    Select.create "status" "Status" id [
        Select.option "active" "Active"
        Select.option "paused" "Paused"
    ]
    |> Select.withId "generated-status"
    |> Select.withSelected "active"
    |> Select.render

let notice =
    Notice.create "generated-notice" "Generated source is active" (p { "This UI was copied by the packed fve tool." })
    |> Notice.withTone Tone.Positive
    |> Notice.render

let notification =
    Notification.create "generated-notification" "Ready for review" (p { "The generated project compiled successfully." })
    |> Notification.withTone Tone.Informative
    |> Notification.render

let browser =
    Browser.create (div { _class "p-5 text-sm text-[var(--fve-text)]"; "Consumer-owned browser content" })
    |> Browser.withAddress "https://acme.example/generated"
    |> Browser.render

let phone =
    Phone.create (
        div {
            _class "grid gap-4 p-5"
            h2 { _class "text-lg font-semibold text-[var(--fve-text)]"; "Owned mobile source" }
            p { _class "text-sm text-[var(--fve-muted-text)]"; "Responsive utilities came from copied F# files." }
            button
        })
    |> Phone.render

let document =
    html {
        _lang "en"
        head {
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            title { "Generated fve consumer" }
            link { _rel "stylesheet"; _href "/output.css" }
        }
        body {
            _class "m-0"
            div {
                _class (ComponentsTheme.className ComponentsTheme.sky + " min-h-screen bg-[var(--fve-page)] text-[var(--fve-text)]")
                _data("generated-consumer", "true")
                main {
                    _class "mx-auto grid min-h-screen max-w-6xl gap-8 p-6 lg:grid-cols-[minmax(0,1fr)_20rem]"
                    section {
                        _class "grid content-start gap-6"
                        h1 { _class "text-3xl font-bold tracking-tight"; "Generated consumer" }
                        p { _class "max-w-2xl text-[var(--fve-muted-text)]"; "Rendered from source installed into an isolated project by the packaged CLI." }
                        div { _class "flex flex-wrap items-center gap-3"; button }
                        div { _class "max-w-md"; select }
                        notice
                        notification
                        browser
                    }
                    aside { _class "min-w-0"; phone }
                }
            }
        }
    }
    |> Render.toString
    |> fun markup -> "<!doctype html>" + markup

let builder = WebApplication.CreateBuilder()
builder.Services.AddRouting() |> ignore
let app = builder.Build()
app.UseStaticFiles() |> ignore
app.MapGet("/", Func<IResult>(fun () -> Results.Content(document, "text/html; charset=utf-8"))) |> ignore
app.MapGet("/health", Func<IResult>(fun () -> Results.Json {| status = "ok" |})) |> ignore
app.Run()
