open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open FSharp.ViewEngine
open Acme.Components
open type FSharp.ViewEngine.Html
open type FSharp.ViewEngine.Datastar

let button =
    fveButton {
        _color Color.Primary
        _variant ButtonVariant.Solid
        "Create account"
    }

let refreshIcon =
    raw
        """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M15.312 11.424a5.5 5.5 0 0 1-9.201 2.42.75.75 0 0 0-1.022 1.098 7 7 0 0 0 11.89-3.518h.771a.75.75 0 0 0 .53-1.28l-1.75-1.75a.75.75 0 0 0-1.06 0l-1.75 1.75a.75.75 0 0 0 .53 1.28h1.062Z" clip-rule="evenodd"/><path fill-rule="evenodd" d="M4.688 8.576a5.5 5.5 0 0 1 9.201-2.42.75.75 0 0 0 1.022-1.098A7 7 0 0 0 3.021 8.576H2.25a.75.75 0 0 0-.53 1.28l1.75 1.75a.75.75 0 0 0 1.06 0l1.75-1.75a.75.75 0 0 0-.53-1.28H4.688Z" clip-rule="evenodd"/></svg>"""

let iconButton =
    fveButton {
        _ariaLabel "Refresh accounts"
        _size Size.Small

        span {
            _ariaHidden true
            _class "inline-flex shrink-0"
            refreshIcon
        }
    }

// Exercise the complete public size API in each actual SDK-installed consumer.
let sizeExamples =
    div {
        _id "generated-button-sizes"
        _class "flex flex-wrap items-center gap-3"
        for size, label in [ Size.ExtraSmall, "XS"; Size.Small, "S"; Size.Medium, "M"; Size.Large, "L"; Size.ExtraLarge, "XL" ] do
            fveButton {
                _size size
                _ariaLabel ("Size " + label)
                refreshIcon
            }
    }

let leadingButton =
    fveButton {
        span {
            _ariaHidden true
            _class "inline-flex shrink-0"
            refreshIcon
        }

        "Refresh balances"
    }

let trailingButton =
    fveButton {
        "Sync now"

        span {
            _ariaHidden true
            _class "inline-flex shrink-0"
            refreshIcon
        }
    }

let customButton =
    fveButton {
        strong { "Custom" }
        span { " content" }
    }

let popover =
    fvePopover {
        _id "generated-popover"
        fvePopoverTrigger { fveButton { "Open details" } }
        fvePopoverContent {
            _class "grid gap-3"
            p { "Generated details" }
            p { "Caller-owned grid spacing" }
        }
    }

let datePicker =
    fveField {
        fveLabel { _for "generated-date"; "Generated date" }
        fveDatePicker {
            _id "generated-date"; _name "generatedDate"; _value "2026-09-18"
            _today (DateOnly(2026, 11, 5))
            _clearable true
        }
    }

let calendarRanges =
    section {
        _ariaLabel "Calendar range intervals"
        _class "grid min-w-0 gap-6"
        for id, hours, step, start, minutes, durations in
            [ "generated-step-range", (8, 18), 45, TimeOnly(9, 0), 45, [45]
              "generated-duration-range", (6, 22), 30, TimeOnly(21, 30), 30, [30; 45] ] do
            let available = [
                for at in 0 .. step .. 1439 do
                    for length in durations do
                        if at >= fst hours * 60 && at + length <= snd hours * 60 then
                            yield TimeOnly(at / 60, at % 60), length
            ]
            form {
                _id (id + "-form"); _ariaLabel id; _method "get"; _action "/"
                fveDayCalendar {
                    _id id; _label id; _date (DateOnly(2026, 9, 17)); _hours hours
                    _hourHeight 80; _viewportHeight 320; _dayHeading false; _fluidWidth true
                    fveCalendarRange {
                        _name "selection"; _startTime start; _minutes minutes
                        _minuteStep step; _durations durations
                        _availableRanges available
                    }
                }
                Html.button { _type "reset"; "Reset range" }
            }
    }

let structuralExamples =
    div {
        _class "grid min-w-0 gap-3"
        fveBreadcrumbs {
            _id "generated-breadcrumbs"; _ariaLabel "Generated breadcrumb"
            fveBreadcrumbItem { _id "generated-code-link"; _href "/code"; _target "_blank"; _rel "noopener"; _dataOn ("click", "el.dataset.activated = 'true'"); "Code" }
            fveBreadcrumbItem { _href "/code/plain"; "Plain code" }
            fveBreadcrumbItem { "Generated consumer" }
        }
        fveField {
            fveLabel { _for "generated-tags"; "Generated tags" }
            fveTagInput { _id "generated-tags"; _name "generatedTags"; _values ["reviewed"] }
        }
        fveTable {
            _id "generated-table"; _ariaLabel "Generated table"
            _dataSignals "{_generated_table_value: 'before'}"
            fveTableHeader { fveTableRow { fveTableHead { "Value" }; fveTableHead { "Echo" } } }
            fveTableBody {
                fveTableRow {
                    fveTableCell { fveInput { _ariaLabel "Table binding"; _dataBind "_generated_table_value" } }
                    fveTableCell { span { _id "generated-table-echo"; _dataText "$_generated_table_value" } }
                }
            }
        }
    }

let select =
    fveField {
        fveLabel {
            _for "generated-status"
            "Status"
        }

        fveSelect {
            _id "generated-status"
            _name "status"
            _value "active"

            fveSelectContent {
                for value, label, disabled in ([ ("active", "Active", false); ("paused", "Paused", false) ]) do
                    fveSelectItem {
                        _value value
                        _label label
                        _disabled disabled
                    }
            }
        }
    }

// Keep edge-case compositions in the installed consumer, not the public gallery.
let inputGroup =
    form {
        _id "generated-input-group"
        _class "min-w-0 max-w-sm"

        fveField {
            fveLabel {
                _for "record-search"
                "Search records"
            }

            fveInputGroup {
                fveInputGroupAddon {
                    _ariaHidden true
                    span { "In" }
                    span { "Accounts" }
                }

                fveInput {
                    _id "record-search"
                    _name "recordSearch"
                    _type InputType.Text
                }

                fveInputGroupAddon {
                    _position InputGroupPosition.InlineEnd

                    Html.button {
                        _type "reset"
                        _class "rounded border px-3 py-1 text-sm"
                        "Clear"
                    }
                }
            }
        }
    }

let fileSelection =
    fveField {
        _id "generated-file-selection"
        fveLabel { _for "generated-file"; "Statement file" }
        fveFileSelection {
            _id "generated-file"
            _name "statement"
        }
    }

let notice =
    fveNotice {
        _id "generated-notice"
        _label "Generated source is active"
        p { "This UI was copied by the packed fve tool." }
        _color Color.Success
        _variant NoticeVariant.Outline
    }

let skeleton =
    fveSkeletonRegion {
        _ariaLabel "Loading account preview"
        _id "generated-skeleton"

        div {
            _class "grid min-w-0 gap-2"
            fveSkeleton { _class "h-4 w-full" }

            fveSkeleton {
                Html._width "75%"
                Html._height "1rem"
            }
        }
    }

let notification =
    fveNotificationRegion {
        _id "generated-notifications"
        _ariaLabel "Build notifications"

        fveNotification {
            _id "generated-notification"
            _label "Ready for review"
            _color Color.Info
            _persistent true
            p { "The generated project compiled successfully." }
        }
    }

let browser =
    fveBrowser {
        _address "/"

        div {
            _class "p-5 text-sm text-[var(--fve-text)]"
            "Consumer-owned browser content"
        }
    }

let phone =
    fvePhone {
        div {
            _class "grid gap-4 p-5"

            h2 {
                _class "text-lg font-semibold text-[var(--fve-text)]"
                "Owned mobile source"
            }

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Responsive utilities came from copied F# files."
            }

            button
        }
    }

let document =
    html {
        _lang "en"

        head {
            meta { _charset "utf-8" }

            meta {
                _name "viewport"
                _content "width=device-width, initial-scale=1"
            }

            title { "Generated fve consumer" }

            link {
                _rel "stylesheet"
                _href "/output.css"
            }

            script {
                _type "module"
                _src "/assets/app.js"
            }
        }

        body {
            _class "m-0"

            div {
                _class (
                    ComponentsTheme.className ComponentsTheme.sky
                    + " min-h-screen bg-[var(--fve-background)] text-[var(--fve-text)]"
                )

                _data ("generated-consumer", "true")

                main {
                    _class
                        "mx-auto grid min-h-screen min-w-0 max-w-6xl grid-cols-1 gap-8 p-6 [overflow-wrap:anywhere] lg:grid-cols-[minmax(0,1fr)_20rem]"

                    section {
                        _class "grid min-w-0 content-start gap-6"

                        h1 {
                            _class "text-3xl font-bold tracking-tight"
                            "Generated consumer"
                        }

                        p {
                            _class "max-w-2xl text-[var(--fve-muted-text)]"
                            "Rendered from source installed into an isolated project by the packaged CLI."
                        }

                        div {
                            _class "flex flex-wrap items-center gap-3"
                            button
                            iconButton
                            leadingButton
                            trailingButton
                            customButton
                        }

                        sizeExamples
                        popover
                        datePicker
                        structuralExamples
                        calendarRanges

                        div {
                            _class "max-w-md"
                            select
                        }

                        inputGroup
                        fileSelection
                        notice
                        skeleton
                        notification
                        browser
                    }

                    aside {
                        _class "min-w-0"
                        phone
                    }
                }
            }
        }
    }
    |> Render.toString
    |> fun markup -> "<!doctype html>" + markup

// No Docs stylesheet or Prism theme: only consumer-compiled Tailwind and owned runtime assets.
let codeDocument highlighting =
    html {
        _lang "en"
        _attr ("data-nonce", "generated-code-nonce")

        head {
            meta { _charset "utf-8" }

            meta {
                _name "viewport"
                _content "width=device-width, initial-scale=1"
            }

            title { "Generated code blocks" }

            link {
                _rel "stylesheet"
                _href "/output.css"
            }

            let prismScripts =
                if highlighting then
                    [ "/scripts/prism.1.29.0.min.js"
                      "/scripts/prism-fsharp.1.29.0.min.js"
                      "/scripts/prism-python.1.29.0.min.js"
                      "/scripts/prism-typescript.1.29.0.min.js"
                      "/scripts/prism-bash.1.29.0.min.js"
                      "/scripts/prism-sql.1.29.0.min.js" ]
                else
                    []

            CodeBlock.assetsWithNonce None prismScripts (Some "generated-code-nonce")

            script {
                _type "module"
                _src "/assets/app.js"
                _attr ("nonce", "generated-code-nonce")
            }
        }

        body {
            _class (
                ComponentsTheme.className ComponentsTheme.sky
                + " m-0 bg-[var(--fve-background)] p-6 text-[var(--fve-text)]"
            )

            main {
                _class "mx-auto grid max-w-3xl gap-6"
                h1 { "Generated code blocks" }

                fveCodeBlock {
                    _language "text"
                    "Build succeeded.\n0 warnings\n0 errors"
                }

                if highlighting then
                    fveCodeBlock {
                        _language "fsharp"
                        "let greeting = \"Hello\"\nlet count = 42"
                    }

                    fveCodeBlock {
                        _language "python"
                        "from decimal import Decimal\ntotal = Decimal(\"125.50\")"
                    }

                    fveCodeBlock {
                        _language "typescript"

                        "type Account = { balance: number };\nconst account: Account = { balance: 125.50 };"
                    }

                    fveCodeBlock {
                        _language "bash"

                        "#!/usr/bin/env bash\nset -euo pipefail\nfor project in Domain Server; do echo \"$project\"; done"
                    }

                    fveCodeBlock {
                        _language "sql"
                        "select account_id from accounts where currency = 'USD';"
                    }
            }
        }
    }
    |> Render.toHtmlDocString

let builder = WebApplication.CreateBuilder()
builder.Services.AddRouting() |> ignore
let app = builder.Build()
app.UseStaticFiles() |> ignore

app.MapGet("/", Func<IResult>(fun () -> Results.Content(document, "text/html; charset=utf-8")))
|> ignore

for route, highlighting in [ "/code", true; "/code/plain", false ] do
    app.MapGet(
        route,
        Func<HttpContext, IResult>(fun context ->
            context.Response.Headers.ContentSecurityPolicy <-
                "default-src 'self'; script-src 'nonce-generated-code-nonce' 'strict-dynamic'; style-src 'self' 'unsafe-inline'; object-src 'none'"

            Results.Content(codeDocument highlighting, "text/html; charset=utf-8"))
    )
    |> ignore

app.MapGet("/health", Func<IResult>(fun () -> Results.Json {| status = "ok" |}))
|> ignore

app.Run()
