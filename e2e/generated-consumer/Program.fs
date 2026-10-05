open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open FSharp.ViewEngine
open Acme.Components
open type FSharp.ViewEngine.Html

let button = Button.create (ButtonContent.Text "Create account") |> Button.withColor ButtonColor.Primary |> Button.withVariant ButtonVariant.Solid |> Button.render
let refreshIcon = raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M15.312 11.424a5.5 5.5 0 0 1-9.201 2.42.75.75 0 0 0-1.022 1.098 7 7 0 0 0 11.89-3.518h.771a.75.75 0 0 0 .53-1.28l-1.75-1.75a.75.75 0 0 0-1.06 0l-1.75 1.75a.75.75 0 0 0 .53 1.28h1.062Z" clip-rule="evenodd"/><path fill-rule="evenodd" d="M4.688 8.576a5.5 5.5 0 0 1 9.201-2.42.75.75 0 0 0 1.022-1.098A7 7 0 0 0 3.021 8.576H2.25a.75.75 0 0 0-.53 1.28l1.75 1.75a.75.75 0 0 0 1.06 0l1.75-1.75a.75.75 0 0 0-.53-1.28H4.688Z" clip-rule="evenodd"/></svg>"""
let iconButton = Button.create (ButtonContent.Icon ("Refresh accounts", refreshIcon)) |> Button.render
let leadingButton = Button.create (ButtonContent.IconText (refreshIcon, "Refresh balances")) |> Button.render
let trailingButton = Button.create (ButtonContent.TextIcon ("Sync now", refreshIcon)) |> Button.render
let customButton = Button.create (ButtonContent.Custom (fragment { strong { "Custom" }; span { " content" } })) |> Button.render

let select =
    Select.create "status" "Status" id [
        SelectOption.create "active" "Active"
        SelectOption.create "paused" "Paused"
    ]
    |> Select.withId "generated-status"
    |> Select.withSelected "active"
    |> Select.render

// Keep edge-case compositions in the installed consumer, not the public gallery.
let inputGroup =
    form {
        _id "generated-input-group"
        _class "min-w-0 max-w-sm"
        InputGroup.create "record-search" "recordSearch" "Search records"
        |> InputGroup.withInputType InputType.Search
        |> InputGroup.withAddon (InputGroupAddon.text InputGroupPosition.Leading "In")
        |> InputGroup.withAddon (InputGroupAddon.text InputGroupPosition.Leading "Accounts")
        |> InputGroup.withAddon (InputGroupAddon.action InputGroupPosition.Trailing (
            Html.button { _type "reset"; _class "rounded border px-3 py-1 text-sm"; "Clear" }))
        |> InputGroup.render
    }

let notice =
    Notice.create "generated-notice" "Generated source is active" (p { "This UI was copied by the packed fve tool." })
    |> Notice.withColor NoticeColor.Success
    |> Notice.withVariant NoticeVariant.Outline
    |> Notice.render

let skeleton =
    SkeletonRegion.create "Loading account preview" (
        div {
            _class "grid min-w-0 gap-2"
            Skeleton.create () |> Skeleton.render
            Skeleton.create () |> Skeleton.withSize "75%" "1rem" |> Skeleton.render
        })
    |> SkeletonRegion.withAttributes [ _id "generated-skeleton" ]
    |> SkeletonRegion.render

let notification =
    Notification.create "generated-notification" "Ready for review" (p { "The generated project compiled successfully." })
    |> Notification.withColor NotificationColor.Info
    |> Notification.persistent
    |> Some
    |> NotificationRegion.create "generated-notifications" "Build notifications"
    |> NotificationRegion.render

let browser =
    Browser.create (div { _class "p-5 text-sm text-[var(--fve-text)]"; "Consumer-owned browser content" })
    |> Browser.withAddress "/"
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
                _class (ComponentsTheme.className ComponentsTheme.sky + " min-h-screen bg-[var(--fve-background)] text-[var(--fve-text)]")
                _data("generated-consumer", "true")
                main {
                    _class "mx-auto grid min-h-screen max-w-6xl gap-8 p-6 lg:grid-cols-[minmax(0,1fr)_20rem]"
                    section {
                        _class "grid min-w-0 content-start gap-6"
                        h1 { _class "text-3xl font-bold tracking-tight"; "Generated consumer" }
                        p { _class "max-w-2xl text-[var(--fve-muted-text)]"; "Rendered from source installed into an isolated project by the packaged CLI." }
                        div { _class "flex flex-wrap items-center gap-3"; button; iconButton; leadingButton; trailingButton; customButton }
                        div { _class "max-w-md"; select }
                        inputGroup
                        notice
                        skeleton
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
