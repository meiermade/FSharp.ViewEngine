module Docs.Tests.Program

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Xml.Linq
open Giraffe
open Microsoft.AspNetCore.Http
open Expecto
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Datastar
open Docs.Common
open Docs.Pages
open Docs.Web

type private ShellTestDestination =
    | Home
    | Accounts
    | Account of int
    | Reports
    | Settings

let private mutationStatus path (origin:string) (contentType:string) contentLength =
    let context = DefaultHttpContext()
    context.Request.Method <- HttpMethods.Post
    context.Request.Scheme <- "https"
    context.Request.Host <- HostString "fve.meiermade.com"
    context.Request.Path <- PathString path
    context.Request.QueryString <- QueryString "?item=beach"
    context.Request.Headers.Origin <- origin
    context.Request.ContentType <- contentType
    context.Request.ContentLength <- Nullable contentLength
    context.Response.Body <- new MemoryStream()
    let next : HttpFunc = fun current -> task { return Some current }
    Handler.postRoutes next context |> Async.AwaitTask |> Async.RunSynchronously |> ignore
    context.Response.StatusCode

let private postFormResponse (reference:string) (fields:(string * string) list) =
    let uri = Uri(Uri "https://fve.meiermade.com", reference)
    let body =
        fields
        |> List.map (fun (key, value) -> Uri.EscapeDataString key + "=" + Uri.EscapeDataString value)
        |> String.concat "&"
        |> Text.Encoding.UTF8.GetBytes
    let context = DefaultHttpContext()
    context.Request.Method <- HttpMethods.Post
    context.Request.Scheme <- uri.Scheme
    context.Request.Host <- HostString uri.Host
    context.Request.Path <- PathString uri.AbsolutePath
    context.Request.QueryString <- QueryString uri.Query
    context.Request.Headers.Origin <- "https://fve.meiermade.com"
    context.Request.Headers.Referer <- Microsoft.Extensions.Primitives.StringValues("https://fve.meiermade.com" + uri.PathAndQuery)
    context.Request.ContentType <- "application/x-www-form-urlencoded"
    context.Request.ContentLength <- Nullable(int64 body.Length)
    context.Request.Body <- new MemoryStream(body)
    context.Response.Body <- new MemoryStream()
    let next : HttpFunc = fun current -> task { return Some current }
    Handler.postRoutes next context |> Async.AwaitTask |> Async.RunSynchronously |> ignore
    context.Response.Body.Position <- 0L
    use reader = new StreamReader(context.Response.Body)
    context.Response.StatusCode, context.Response.Headers.Location.ToString(), context.Response.Headers.SetCookie.ToString(), reader.ReadToEnd(), context.Response.Headers.CacheControl.ToString()

let private postForm reference fields =
    let status,location,cookies,body,_ = postFormResponse reference fields
    status,location,cookies,body

let private shellTestUrl = function
    | Home -> "/"
    | Accounts -> "/accounts"
    | Account id -> $"/accounts/{id}"
    | Reports -> "/reports"
    | Settings -> "/settings"

let private expectedPaths = Registry.all |> List.map _.path |> set

let private routeResponse (reference:string) =
    let uri = Uri(Uri "https://fve.meiermade.com", reference)
    let context = DefaultHttpContext()
    context.Request.Method <- HttpMethods.Get
    context.Request.Scheme <- uri.Scheme
    context.Request.Host <- HostString uri.Host
    context.Request.Path <- PathString uri.AbsolutePath
    context.Request.QueryString <- QueryString uri.Query
    context.Response.Body <- new MemoryStream()
    let next : HttpFunc = fun current -> task { return Some current }
    let result = Handler.routes next context |> Async.AwaitTask |> Async.RunSynchronously
    context.Response.Body.Position <- 0L
    use reader = new StreamReader(context.Response.Body)
    let body = reader.ReadToEnd()
    (if result.IsSome then context.Response.StatusCode else 404), body

let private routeStatus reference = routeResponse reference |> fst

let private referenceStatus (reference:string) =
    let uri = Uri(Uri "https://fve.meiermade.com", reference)
    let webRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "Docs", "wwwroot"))
    let relativePath = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')).Replace('/', Path.DirectorySeparatorChar)
    let filePath = Path.GetFullPath(Path.Combine(webRoot, relativePath))
    if filePath.StartsWith(webRoot, StringComparison.Ordinal) && File.Exists filePath then 200
    else routeStatus uri.PathAndQuery

[<Tests>]
let tests =
    testList "Direct F# documentation" [
        testCase "page-example mutation boundaries reject foreign origins, unsupported forms, and oversized bodies" <| fun _ ->
            let path = "/components/page-examples/messaging/send"
            Expect.equal (mutationStatus path "https://foreign.example" "application/x-www-form-urlencoded" 10L) 403 "foreign origin"
            Expect.equal (mutationStatus path "https://fve.meiermade.com" "application/json" 10L) 415 "unsupported content type"
            Expect.equal (mutationStatus path "https://fve.meiermade.com" "application/x-www-form-urlencoded" 3_000_001L) 413 "request limit"

        testCase "page-example messages use finite URL-backed outcomes without retaining submitted text" <| fun _ ->
            let emptyStatus, emptyLocation, emptyCookie, _ = postForm "/components/page-examples/messaging/send?item=beach" [ "message", "  " ]
            Expect.equal emptyStatus 302 "Whitespace submissions redirect to a finite validation state."
            Expect.stringContains emptyLocation "view=message-empty" "The validation state is URL-backed."
            Expect.equal emptyCookie "" "The example does not create a session cookie."

            let marker = "private-message-should-not-be-retained"
            let sentStatus, sentLocation, sentCookie, _ = postForm "/components/page-examples/messaging/send?item=beach" [ "message", marker ]
            Expect.equal sentStatus 302 "Valid submissions redirect to the deterministic sent state."
            Expect.stringContains sentLocation "view=sent" "The sent state is URL-backed."
            Expect.equal sentCookie "" "The example does not create a session cookie."
            let status, html = routeResponse sentLocation
            Expect.equal status 200 "The deterministic state is directly renderable."
            Expect.stringContains html "Meet at the east entrance 15 minutes before we leave." "The authored sent fixture is rendered."
            Expect.isFalse (html.Contains marker) "Submitted message text is not retained or reflected."

        testCase "page-example media never accepts file bodies and uses a repository fixture" <| fun _ ->
            Expect.equal (mutationStatus "/components/page-examples/media-management/upload" "https://fve.meiermade.com" "multipart/form-data; boundary=fixture" 128L) 415 "Multipart bodies are rejected before form parsing."
            let marker = "private-upload-name.png"
            let status, location, cookie, _ = postForm "/components/page-examples/media-management/upload" [ "name", marker; "alt", "Private upload description" ]
            Expect.equal status 302 "Metadata-only submission reaches the deterministic success state."
            Expect.stringContains location "item=photo-uploaded" "The repository fixture has a stable authored identity."
            Expect.stringContains location "view=uploaded" "The success state is URL-backed."
            Expect.equal cookie "" "The example does not create a session cookie."
            let renderedStatus, html = routeResponse location
            Expect.equal renderedStatus 200 "The success fixture is directly renderable."
            Expect.stringContains html "/images/page-examples/violet.png" "The result uses a repository-owned asset."
            Expect.isFalse (html.Contains marker) "Submitted metadata is not retained or reflected."
            Expect.equal (routeStatus "/components/page-examples/images/untrusted") 404 "There is no temporary image-serving route."

        testCase "page-example accounts and settings return deterministic validation states" <| fun _ ->
            let marker = "Private account value"
            let accountStatus, accountLocation, accountCookie, _ = postForm "/components/page-examples/account-management/create" [ "name", marker; "accountType", "Asset" ]
            Expect.equal accountStatus 302 "A valid account form redirects."
            Expect.stringContains accountLocation "view=account-created" "Account success is a finite URL state."
            Expect.equal accountCookie "" "Account submission does not create a session cookie."
            let _, accountHtml = routeResponse accountLocation
            Expect.stringContains accountHtml "does not create or retain records" "The resettable result is explicit."
            Expect.isFalse (accountHtml.Contains marker) "Submitted account values are not retained or reflected."

            let settingsStatus, settingsLocation, settingsCookie, _ = postForm "/components/page-examples/account-management/settings" [ "workspaceName", "Private workspace value" ]
            Expect.equal settingsStatus 302 "A valid settings form redirects."
            Expect.stringContains settingsLocation "view=settings-saved" "Settings success is a finite URL state."
            Expect.equal settingsCookie "" "Settings submission does not create a session cookie."
            let _, settingsHtml = routeResponse settingsLocation
            Expect.stringContains settingsHtml "does not retain submitted values" "The resettable result is explicit."
            Expect.isFalse (settingsHtml.Contains "Private workspace value") "Submitted settings are not retained or reflected."

        test "Operational components retain native form and accessible state contracts" {
            let files =
                FileSelection.create "statements" "statements" "Statements"
                |> FileSelection.multiple
                |> FileSelection.withAccept ".csv"
                |> FileSelection.required
                |> FileSelection.render
                |> Render.toString
            for expected in [ "type=\"file\""; "name=\"statements\""; "multiple"; "accept=\".csv\""; "required"; "Clear selected files" ] do
                Expect.stringContains files expected "native file-selection contract"
            let pendingFiles = FileSelection.create "pending" "pending" "Pending files" |> FileSelection.pending |> FileSelection.render |> Render.toString
            Expect.stringContains pendingFiles "aria-busy=\"true\"" "pending file selection exposes busy state"
            Expect.stringContains pendingFiles "disabled" "pending file selection prevents replacement while work settles"

            let progress = Progress.create "Import" 3 4 |> Progress.withValueText "Three of four" |> Progress.render |> Render.toString
            Expect.stringContains progress "value=\"3\" max=\"4\"" "native progress range"
            Expect.stringContains progress "Three of four" "readable value text"
            Expect.throws (fun () -> Progress.create "Import" 5 4 |> ignore) "out-of-range progress is rejected"

            let steps =
                Steps.create "Close"
                    [ Step.create "Review" StepState.Current
                      Step.create "Post" StepState.Available |> Step.withDestination "/post" ]
                |> Steps.render id
                |> Render.toString
            Expect.stringContains steps "aria-current=\"step\"" "current step is explicit"
            Expect.stringContains steps "href=\"/post\"" "available destination stays a link"

            let credential = CopyReveal.create "credential" "Demo token" "safe-demo" |> CopyReveal.render |> Render.toString
            Expect.stringContains credential "type=\"password\"" "credential starts masked"
            Expect.isFalse (credential.Contains("aria-label=\"safe-demo\"")) "secret value is not an accessible action name"
            let iconCredential = CopyReveal.create "icon-credential" "Icon demo token" "safe-icon-demo" |> CopyReveal.withIconButtons |> CopyReveal.render |> Render.toString
            Expect.stringContains iconCredential "aria-label=\"Reveal\"" "icon reveal action keeps an accessible name"
            Expect.stringContains iconCredential "aria-label=\"Copy\"" "icon copy action keeps an accessible name"
            Expect.equal (Regex.Matches(iconCredential, "class=\"size-4\"").Count) 3 "icon actions render reveal, hide, and copy glyphs"

            let hierarchy =
                Table.create "Tree" [ TableColumn.create "Name" text |> TableColumn.asRowHeader ] [ "Parent"; "Child" ]
                |> Table.withHierarchy (
                    TableHierarchy.create "tree" id id (function "Child" -> [ "Parent" ] | _ -> []) (function "Child" -> 1 | _ -> 0) ((=) "Parent")
                    |> TableHierarchy.withExpandedKeys [ "Parent" ])
                |> Table.render
                |> Render.toString
            Expect.stringContains hierarchy "aria-label=\"Toggle Parent\"" "parent disclosure is named"
            Expect.stringContains hierarchy "data-show=\"$_table_v74726565_expanded.includes(&quot;Parent&quot;)\"" "descendant visibility follows its supplied ancestor"
            Expect.equal (Regex.Matches(hierarchy, "<svg viewBox=\"0 0 20 20\"").Count) 2 "parent disclosure uses chevron-up and chevron-down icons"

            let leading =
                ResizablePanel.create (text "Files")
                |> ResizablePanel.withBounds 25 75
                |> ResizablePanel.withInitialSize 40
                |> ResizablePanel.collapsible 0
            let resizable = Resizable.create "workspace-panels" "Workspace panels" leading (text "Editor") |> Resizable.render |> Render.toString
            Expect.stringContains resizable "role=\"separator\"" "resizable layout exposes its handle as a separator"
            let resizablePageHtml = View.documentWithPage Registry.navigation Components.resizableRegistration Components.resizablePage |> Render.toHtmlDocString
            Expect.stringContains resizablePageHtml "type ResizablePanel" "private panel fields stay hidden in generated API documentation"
            Expect.isFalse (resizablePageHtml.Contains("// opaque")) "generated API documentation avoids internal representation jargon"
            Expect.stringContains resizable "aria-orientation=\"vertical\"" "horizontal panels expose a vertical separator"
            Expect.stringContains resizable "setPointerCapture" "pointer resizing captures the active pointer"
            Expect.stringContains resizable "ArrowLeft" "horizontal resizing supports directional keys"
            Expect.stringContains resizable "data-on:dblclick" "collapsible panels expose an explicit collapse gesture"
            let verticalResizable =
                Resizable.create "stacked-panels" "Stacked panels" leading (text "Timeline")
                |> Resizable.withOrientation ResizableOrientation.Vertical
                |> Resizable.render
                |> Render.toString
            Expect.stringContains verticalResizable "aria-orientation=\"horizontal\"" "vertical panels expose a horizontal separator"
            Expect.stringContains verticalResizable "ArrowUp" "vertical resizing supports directional keys"
            Expect.throws (fun () -> ResizablePanel.create (text "Invalid") |> ResizablePanel.withBounds 80 20 |> ignore) "resizable bounds must be ordered"

            let tooltip = Tooltip.create "help" "Supplementary help" (fun descriptionId -> button { _type "button"; _ariaDescribedby descriptionId; "Details" }) |> Tooltip.render |> Render.toString
            Expect.stringContains tooltip "aria-describedby=\"help-content\"" "tooltip supplements rather than replaces the trigger name"
            Expect.stringContains tooltip "role=\"tooltip\"" "tooltip content has the correct role"
            Expect.stringContains tooltip "popover=\"manual\"" "tooltip lifecycle is component-local"
            let popover = Popover.create "filters" "Open filters" (text "Filters") "Account filters" (Input.create "query" "Query" |> Input.render) |> Popover.render |> Render.toString
            Expect.stringContains popover "popovertarget=\"filters-content\"" "popover trigger owns its content relationship"
            Expect.stringContains popover "role=\"dialog\"" "interactive popover content is a labelled dialog"
            Expect.stringContains popover "popover=\"auto\"" "popover uses top-layer light dismissal"
            Expect.stringContains popover "position-area:block-end span-inline-end" "default popovers use a valid start-aligned logical anchor area"
            Expect.stringContains popover "inset:auto;margin:0.5rem 0" "popover placement is owned next to its trigger instead of inherited from the top layer"
            let monthSelection =
                MonthCalendar.createSelection "dates" "Dates" "startDate" (MonthCalendarSelection.Range(Some(DateOnly(2026, 9, 14)), Some(DateOnly(2026, 9, 18))))
                |> MonthCalendar.withRangeEndName "endDate"
                |> MonthCalendar.withDisplayMonth (DateOnly(2026, 9, 1))
                |> MonthCalendar.withUnavailableDates [ DateOnly(2026, 9, 16) ]
                |> MonthCalendar.renderSelection
                |> Render.toString
            Expect.stringContains monthSelection "name=\"startDate\"" "date ranges submit a canonical start value"
            Expect.stringContains monthSelection "name=\"endDate\"" "date ranges submit a canonical end value"
            Expect.stringContains monthSelection "value=\"2026-09-14\"" "date-only values do not include a timezone"
            Expect.isTrue (Regex.IsMatch(monthSelection, "data-value=\"2026-09-16\"[^>]*disabled")) "unavailable dates cannot be selected"
            Expect.stringContains monthSelection "PageUp" "date grids support keyboard month navigation"
            let captionMonthSelection =
                MonthCalendar.createSelection "birth-date" "Birth date" "birthDate" (MonthCalendarSelection.Single(Some(DateOnly(2000, 6, 15))))
                |> MonthCalendar.withDisplayMonth (DateOnly(2000, 6, 1))
                |> MonthCalendar.withBounds (DateOnly(1920, 1, 1)) (DateOnly(2026, 12, 31))
                |> MonthCalendar.withCaptionLayout MonthCalendarCaptionLayout.Dropdown
                |> MonthCalendar.renderSelection
                |> Render.toString
            Expect.stringContains captionMonthSelection "aria-label=\"Month\"" "dropdown captions expose a labelled month selector"
            Expect.stringContains captionMonthSelection "aria-label=\"Year\"" "dropdown captions expose a labelled year selector"
            Expect.stringContains captionMonthSelection "value=\"1920\"" "bounded dropdown captions begin at the minimum year"
            Expect.stringContains captionMonthSelection "value=\"2026\"" "bounded dropdown captions end at the maximum year"
            Expect.stringContains captionMonthSelection " p-3 " "selection calendars own compact padding"
            let datePicker = DatePicker.create "due" "dueDate" "Due date" (MonthCalendarSelection.Single None) |> DatePicker.withValidation "Choose a due date." |> DatePicker.render |> Render.toString
            Expect.stringContains datePicker "aria-labelledby=\"due-label\"" "date picker trigger is connected to its visible label"
            Expect.stringContains datePicker "aria-invalid=\"true\"" "date picker validation reaches the trigger"
            Expect.stringContains datePicker "popovertarget=\"due-popover-content\"" "date picker composes the public popover"
            Expect.throws (fun () -> MonthCalendar.createSelection "invalid-range" "Dates" "date" (MonthCalendarSelection.Range(Some(DateOnly(2026, 9, 20)), Some(DateOnly(2026, 9, 10)))) |> ignore) "reversed date ranges are rejected"
            let customField =
                Field.create "amount" "Amount"
                |> Field.withDescription "Before fees"
                |> Field.withValidation "Enter an amount."
                |> Field.render (fun attributes -> input { for attribute in attributes do attribute; _name "amount"; _type "number" })
                |> Render.toString
            Expect.equal (Regex.Matches(customField, "<label ").Count) 1 "custom fields render one label"
            Expect.stringContains customField "aria-describedby=\"amount-description amount-validation\"" "custom field controls own help and error relationships"
            Expect.stringContains customField "aria-invalid=\"true\"" "custom field invalid state reaches the control"
            let fieldGroup = FieldGroup.create "Preferences" (text "Controls") |> FieldGroup.render |> Render.toString
            Expect.stringContains fieldGroup "<fieldset" "related fields use native fieldsets"
            Expect.stringContains fieldGroup "<legend" "field groups own their legend"
            let groupedInput =
                InputGroup.create "amount-group" "amount" "Amount"
                |> InputGroup.withValue "125"
                |> InputGroup.withAddon (InputGroupAddon.text InputGroupPosition.Leading "$")
                |> InputGroup.withAddon (InputGroupAddon.action InputGroupPosition.Trailing (button { _type "button"; _ariaLabel "Calculate"; "=" }))
                |> InputGroup.render
                |> Render.toString
            Expect.stringContains groupedInput "aria-hidden=\"true\"" "decorative addons stay outside the accessibility tree"
            Expect.stringContains groupedInput "name=\"amount\"" "only the native control supplies the form value"
            Expect.stringContains groupedInput "aria-label=\"Calculate\"" "interactive addons retain their own name"
            let pendingGroup = InputGroup.create "pending-group" "domain" "Domain" |> InputGroup.pending |> InputGroup.render |> Render.toString
            Expect.stringContains pendingGroup "readonly" "pending groups preserve submitted values while preventing edits"
            Expect.isFalse (pendingGroup.Contains("<input disabled")) "pending groups are distinct from disabled groups"
            let message =
                Message.create "Alex" (text "Ready for review")
                |> Message.withSide MessageSide.Sender
                |> Message.withMetadata "09:12"
                |> Message.withStatus "Delivered"
                |> Message.render
                |> Render.toString
            Expect.stringContains message "aria-label=\"Message from Alex\"" "message sender remains explicit"
            Expect.stringContains message "aria-label=\"Status: Delivered\"" "delivery status remains labelled without owning delivery policy"

            let cards =
                ChoiceCards.multiple "preferences" "preferences" "Preferences" id
                    [ ChoiceCardOption.create "email" "Email" |> ChoiceCardOption.withDescription "Weekly summary"
                      ChoiceCardOption.create "sms" "Text message" |> ChoiceCardOption.disabled ]
                |> ChoiceCards.withSelected [ "email" ]
                |> ChoiceCards.render
                |> Render.toString
            Expect.stringContains cards "type=\"checkbox\"" "multiple cards remain native checkboxes"
            Expect.stringContains cards "name=\"preferences\"" "cards retain native form names"
            Expect.stringContains cards "value=\"email\" checked" "selected cards submit their value"
            Expect.stringContains cards "value=\"sms\" disabled" "disabled cards remain unavailable"

            let tags = TagInput.create "tags" "tags" "Tags" [ "reviewed" ] |> TagInput.render |> Render.toString
            Expect.stringContains tags "field.name = &quot;tags&quot;" "dynamic tags create repeated successful controls"
            Expect.stringContains tags "Enter a tag before adding it." "empty creation has explicit rejection feedback"
            Expect.stringContains tags "That tag has already been added." "duplicates have explicit rejection feedback"
            Expect.stringContains tags "clipboardData" "pasted comma- or line-delimited values are handled explicitly"
            Expect.isFalse (tags.Contains("Backspace")) "Backspace does not remove tags implicitly"

            let calendar = Components.calendarWeekExample |> Render.toString
            Expect.stringContains calendar "data-view=\"week\"" "calendar renders one focused view"
            Expect.isFalse (calendar.Contains("calendar view")) "component examples do not embed connected view navigation"
            Expect.stringContains calendar "Overlaps the trail lesson by one hour" "calendar retains consumer-authored overlap context"
            let loadingCalendar = MonthCalendar.create "Schedule" (DateOnly(2026, 9, 17)) [] |> MonthCalendar.loading |> MonthCalendar.render id |> Render.toString
            Expect.stringContains loadingCalendar "role=\"status\"" "loading calendar announces its state"
            Expect.stringContains loadingCalendar "Loading calendar…" "loading calendar remains readable"
            let errorCalendar = MonthCalendar.create "Schedule" (DateOnly(2026, 9, 17)) [] |> MonthCalendar.withError "Schedule failed." |> MonthCalendar.render id |> Render.toString
            Expect.stringContains errorCalendar "role=\"alert\"" "calendar errors are urgent feedback"
            Expect.stringContains errorCalendar "Schedule failed." "calendar errors preserve consumer-authored messages"
            let unavailableCalendar = MonthCalendar.create "Schedule" (DateOnly(2026, 9, 17)) [] |> MonthCalendar.withUnavailable "Schedule unavailable." |> MonthCalendar.render id |> Render.toString
            Expect.stringContains unavailableCalendar "Schedule unavailable." "unavailable calendar state is explicit"

            let media = Components.mediaLibraryExample |> Render.toString
            Expect.stringContains media "name=\"assetIds\" value=\"social-card\"" "media selection uses native repeated controls"
            Expect.stringContains media "alt=\"FSharp.ViewEngine — Typed HTML views for F#\"" "media thumbnails require consumer-authored alternatives"
            Expect.stringContains media "fve-selection-change" "media selection publishes stable identities to shared bulk actions"
            Expect.stringContains media "No media assets. Upload an image to begin." "media example includes its empty state"
            Expect.stringContains media "Media could not be loaded." "media example includes its recoverable error state"
        }

        test "Calendar dates, overlap lanes and unavailable states are derived from typed input" {
            let day = DateOnly(2024, 2, 29)
            let event id start finish = CalendarEvent.create id id day ("/events/" + id) |> CalendarEvent.withTime start finish
            let events =
                [ event "first" (TimeOnly(9, 0)) (TimeOnly(10, 0))
                  event "overlap" (TimeOnly(9, 30)) (TimeOnly(11, 0))
                  event "connected" (TimeOnly(10, 0)) (TimeOnly(12, 0))
                  event "adjacent" (TimeOnly(12, 0)) (TimeOnly(12, 30))
                  CalendarEvent.create "all-day" "All-day event" day "/events/all-day"
                  CalendarEvent.create "outside" "Outside range" (day.AddMonths 2) "/events/outside" ]
            let config = MonthCalendar.create "Calendar" day events |> MonthCalendar.withToday day "/today" |> MonthCalendar.withSelectedDate day
            let month = config |> MonthCalendar.render id |> Render.toString
            Expect.stringContains month "data-date=\"2024-01-29\"" "month starts on the preceding Monday"
            Expect.stringContains month "data-date=\"2024-03-03\"" "month ends on the trailing Sunday"
            Expect.equal (System.Text.RegularExpressions.Regex.Matches(month, "data-date=").Count) 35 "February leap month has complete weeks"
            Expect.stringContains month "data-date=\"2024-02-29\"" "leap day is an actual date"
            Expect.stringContains month "aria-current=\"date\"" "today is semantic, not inferred from server clock"
            Expect.isFalse (month.Contains("Outside range")) "events outside visible dates are excluded"
            let timedDay = DayCalendar.create "Timed events" day events |> DayCalendar.render id |> Render.toString
            Expect.stringContains timedDay "--fve-calendar-lane:2;--fve-calendar-lanes:2" "overlap has a separate lane"
            Expect.stringContains timedDay "--fve-calendar-start:121;--fve-calendar-duration:120;--fve-calendar-lane:1;--fve-calendar-lanes:2" "connected overlap reuses the ended first lane"
            Expect.stringContains timedDay "--fve-calendar-start:241;--fve-calendar-duration:30;--fve-calendar-lane:1;--fve-calendar-lanes:1" "adjacent non-overlap regains full width"
            let start = DateOnly(2026, 9, 18)
            let range = CalendarEvent.create "range" "Training" start "/events/range" |> CalendarEvent.withEndDate (start.AddDays 3)
            for offset in [-1 .. 4] do
                let html = DayCalendar.create "Range day" (start.AddDays offset) [range] |> DayCalendar.render id |> Render.toString
                Expect.equal (html.Contains("data-event=\"range\"")) (offset >= 0 && offset <= 3) "all-day coverage includes both ends and excludes adjacent dates"
            let rangedMonth = MonthCalendar.create "Range month" start [range] |> MonthCalendar.render id |> Render.toString
            Expect.equal (Regex.Matches(rangedMonth, "<a[^>]*href=\"/events/range\"").Count) 2 "a Friday-to-Monday range splits into two week segments"
            Expect.throws (fun () -> range |> CalendarEvent.withTime (TimeOnly(9, 0)) (TimeOnly(10, 0)) |> ignore) "multi-day events cannot become timed events"
            Expect.throws (fun () -> range |> CalendarEvent.withEndDate (start.AddDays -1) |> ignore) "range ends cannot precede their start"
            let december = MonthCalendar.create "Year end" (DateOnly(2026, 12, 1)) [] |> MonthCalendar.render id |> Render.toString
            Expect.stringContains december "data-date=\"2027-01-03\"" "month geometry crosses the year boundary"
            let nextDay = DayCalendar.create "Next day" (day.AddDays 1) events |> DayCalendar.render id |> Render.toString
            Expect.isFalse (nextDay.Contains("data-event=")) "day filters events by actual date"
            Expect.stringContains nextDay "No events in this range." "filtered empty state is truthful"
            for unavailable in [ MonthCalendar.loading; MonthCalendar.withError "Failed"; MonthCalendar.withUnavailable "Unavailable" ] do
                let html = config |> unavailable |> MonthCalendar.render id |> Render.toString
                Expect.isFalse (html.Contains("data-event=")) "unavailable calendars never expose stale event actions"
            Expect.throws (fun () -> event "bad" (TimeOnly(12, 0)) (TimeOnly(11, 0)) |> ignore) "overnight/reversed intervals need consumer splitting"
            Expect.throws (fun () -> DayCalendar.create "Duplicates" day [events.Head; events.Head] |> ignore) "duplicate event IDs are rejected"
        }

        test "Calendar exposes focused day, week, month and year views" {
            let day = Components.calendarDayExample |> Render.toString
            let week = Components.calendarWeekExample |> Render.toString
            let month = Components.calendarMonthExample |> Render.toString
            let year = Components.calendarYearExample |> Render.toString
            Expect.stringContains day "data-view=\"day\"" "day is a dedicated view"
            Expect.stringContains week "data-view=\"week\"" "week is a dedicated view"
            Expect.stringContains month "data-view=\"month\"" "month is a dedicated view"
            Expect.stringContains year "data-view=\"year\"" "year is a dedicated view"
            Expect.equal (Regex.Matches(year, "data-fve-month-calendar=\"compact\"").Count) 12 "year renders twelve shared compact months"
            Expect.equal (Regex.Matches(year, "role=\"gridcell\"").Count) 441 "2026 has nine five-week months and three six-week months"
            Expect.isFalse (year.Contains("<input")) "display months never create form inputs"
            Expect.isFalse (year.Contains("Previous month")) "year owns navigation rather than twelve month controls"
            Expect.stringContains year "aria-label=\"September 2026 dates\"" "year months retain labelled date grids"
            Expect.stringContains year "2 events: Coastal trail lesson, Beginner riding camp" "year date destinations summarize matching events"
            for registration in [ Components.dayCalendarRegistration; Components.weekCalendarRegistration; Components.monthCalendarRegistration; Components.yearCalendarRegistration ] do
                Expect.equal registration.category "Components" "calendar components are shared data display"
            let compact = Components.compactMonthCalendar |> Render.toString
            Expect.equal (Regex.Matches(compact, "role=\"gridcell\"").Count) 42 "standalone compact month shares the year geometry"
            Expect.isFalse (compact.Contains("<input")) "compact display does not implicitly submit a date"
        }

        test "Native fields preserve encoded values and protect their semantic attributes" {
            let config =
                Input.create "email" "Email address"
                |> Input.withId "customer-email"
                |> Input.withType InputType.Email
                |> Input.withValue "<customer@fve.meiermade.com>"
                |> Input.withDescription "For correspondence."
                |> Input.withValidation "Enter a valid address."
                |> Input.required
                |> Input.withAttributes [ _id "wrong"; _type "hidden"; _name "wrong"; _value "wrong"; _autocomplete "email"; _ariaInvalid false; _role "wrong"; _ariaLabel "wrong"; _ariaLabelledby "wrong" ]
            let html = config |> Input.render |> Render.toString
            Expect.equal (Input.id config) "customer-email" "explicit ID is the actual focus target"
            for expected in [ "type=\"email\""; "name=\"email\""; "value=\"&lt;customer@fve.meiermade.com&gt;\""; "for=\"customer-email\""; "aria-invalid=\"true\""; "autocomplete=\"email\""; "aria-describedby=\"customer-email-description customer-email-validation\"" ] do
                Expect.stringContains html expected "native field contract"
            Expect.isFalse (html.Contains("wrong")) "reserved overrides are removed"
            Expect.isFalse (html.Contains("role=\"alert\"")) "inline descriptions do not duplicate summary announcements"
            Expect.equal (Regex.Matches(html, "type=\"email\"").Count) 1 "one actual input"
            let encoded = Textarea.create "notes" "Notes" |> Textarea.withValue "</textarea><script>alert(1)</script>" |> Textarea.withRows 6 |> Textarea.render |> Render.toString
            Expect.stringContains encoded "rows=\"6\"" "native rows"
            Expect.stringContains encoded "&lt;/textarea&gt;&lt;script&gt;" "content is encoded, not a value attribute or raw HTML"
        }

        test "Pending fields are read-only rather than losing submitted values" {
            let pending = Input.create "reference" "Reference" |> Input.withValue "INV-2048" |> Input.required |> Input.pending |> Input.render |> Render.toString
            let field = Regex.Match(pending, "<input[^>]*>").Value
            Expect.stringContains field "readonly" "pending field cannot be edited"
            Expect.stringContains field "aria-busy=\"true\"" "busy state is truthful"
            Expect.isFalse (Regex.IsMatch(field, "\\sdisabled(?:\\s|>)")) "pending value remains a successful form control"
            Expect.isFalse (Regex.IsMatch(field, "\\srequired(?:\\s|>)")) "read-only control does not carry native required"
            let unavailable = Textarea.create "notes" "Notes" |> Textarea.disabled |> Textarea.render |> Render.toString
            Expect.stringContains unavailable "disabled" "disabled values are conventionally omitted"
            Expect.throws (fun () -> Input.create "" "Label" |> ignore) "name is required"
            Expect.throws (fun () -> Input.create "name" " " |> ignore) "label is required"
            Expect.throws (fun () -> Input.create "name" "Name" |> Input.withId "two ids" |> ignore) "IDs cannot contain whitespace"
            Expect.throws (fun () -> Textarea.create "notes" "Notes" |> Textarea.withRows 0 |> ignore) "rows must be positive"
            Expect.throws (fun () -> Input.create "name" "Name" |> Input.withValidation " " |> ignore) "empty errors are rejected"
            Expect.notEqual (Input.create "a-b" "First" |> Input.id) (Input.create "a_b" "Second" |> Input.id) "default identity does not collapse punctuation"
        }

        test "Error summaries use exact focus targets and encode corrective text" {
            let html =
                ErrorSummary.create "validation-errors" "Check details" [ FieldError.create "contact:email" "Email" "Use <name@fve.meiermade.com>." ]
                |> ErrorSummary.focusOnMount
                |> ErrorSummary.render
                |> Render.toString
            Expect.stringContains html "role=\"alert\"" "one summary announcement"
            Expect.stringContains html "href=\"#contact%3Aemail\"" "fragment safely encodes the exact control ID"
            Expect.stringContains html "data-init=\"el.focus()\"" "focus is explicitly opt-in at insertion"
            Expect.stringContains html "Email: Use &lt;name@fve.meiermade.com&gt;." "corrective content is encoded"
            Expect.throws (fun () -> ErrorSummary.create "errors" "Errors" [] |> ignore) "empty summaries are rejected"
        }

        test "Notice color and presentation do not imply announcement urgency" {
            let notice =
                Notice.create "feedback" "Check details" (p { "Review the account." })
                |> Notice.withColor NoticeColor.Error
                |> Notice.withActions (a { _href "/components/notice"; "Read guidance" })
            for variant in [ NoticeVariant.Solid; NoticeVariant.Soft; NoticeVariant.Outline; NoticeVariant.Ghost ] do
                let notice = notice |> Notice.withVariant variant
                let staticHtml = notice |> Notice.render |> Render.toString
                Expect.isFalse (staticHtml.Contains("role=\"alert\"")) "static notices do not announce"
                Expect.isFalse (staticHtml.Contains("role=\"status\"")) "presentation does not opt into live regions"
                let polite = notice |> Notice.withAnnouncement LiveAnnouncement.Polite |> Notice.render |> Render.toString
                Expect.stringContains polite "role=\"status\"" "polite completion is independent of presentation"
                let urgent = notice |> Notice.withAnnouncement LiveAnnouncement.Assertive |> Notice.render |> Render.toString
                Expect.stringContains urgent "role=\"alert\"" "urgent feedback is explicit"
                Expect.stringContains urgent "aria-atomic=\"true\"" "message is coherent"
                let action = XDocument.Parse(urgent).Descendants(XName.Get "a") |> Seq.exactlyOne
                Expect.equal action.Value "Read guidance" "configured actions persist across variants"
                let withinLiveRegion =
                    action.Ancestors()
                    |> Seq.exists (fun ancestor ->
                        ancestor.Attribute(XName.Get "role")
                        |> Option.ofObj
                        |> Option.exists (fun role -> role.Value = "status" || role.Value = "alert"))
                Expect.isFalse withinLiveRegion "actions stay outside the live message region"
            Expect.throws (fun () -> Notice.create "notice" " " (p { "Content" }) |> ignore) "title is meaningful"
        }

        test "OpenTelemetry log endpoint uses the collector logs path" {
            let endpoint =
                { endpoint = "http://otel-collector.platform.svc.cluster.local:4318/" }

            Expect.equal
                (OpenTelemetryConfig.logsEndpoint endpoint)
                "http://otel-collector.platform.svc.cluster.local:4318/v1/logs"
                "HTTP/protobuf logs use the collector logs endpoint"
        }

        test "Page registry covers every public documentation route" {
            let actual = Registry.all |> List.map _.path |> Set.ofList
            Expect.equal actual expectedPaths "documentation routes"
            Expect.equal Registry.all.Length expectedPaths.Count "one page per route"

            let aliases = Registry.aliases |> Map.ofList
            Expect.equal aliases["/components/section"] "/components/section-header" "section header has its own installable destination"
            for retired in [
                "/components/upload"; "/components/upload-list"; "/components/first-steps"
                "/components/contract"; "/components/chart"; "/components/layouts"
                "/components/row-actions"; "/components/action-cluster"; "/components/confirmation-dialog"
                "/components/status"; "/components/icon-button" ] do
                Expect.isFalse (Map.containsKey retired aliases) $"{retired} has no replacement alias"
                Expect.equal (routeStatus retired) 404 $"{retired} is retired"
        }

        test "Registered pages render complete canonical documents with resolvable local references" {
            let references = Collections.Generic.HashSet<string>(StringComparer.Ordinal)

            for page in Registry.all do
                let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                let canonical = "https://fve.meiermade.com" + (if page.path = "/" then "/" else page.path)
                Expect.equal (routeStatus page.path) 200 $"{page.path} canonical route"
                Expect.stringStarts html "<!DOCTYPE html>" $"{page.path} complete document"
                Expect.stringContains html "<main" $"{page.path} semantic main"
                Expect.stringContains html $"rel=\"canonical\" href=\"{canonical}\"" $"{page.path} canonical"

                for matched in Regex.Matches(html, @"\b(?:href|src|data-docs-preview-src)=""([^""]+)""") do
                    let reference = WebUtility.HtmlDecode(matched.Groups[1].Value)
                    if not (String.IsNullOrWhiteSpace reference)
                       && not (reference.StartsWith('#'))
                       && not (reference.StartsWith("mailto:"))
                       && not (reference.StartsWith("tel:")) then
                        let uri = Uri(Uri("https://fve.meiermade.com" + page.path), reference)
                        if uri.GetLeftPart(UriPartial.Authority) = "https://fve.meiermade.com" then
                            references.Add uri.PathAndQuery |> ignore

            for reference in references do
                Expect.isLessThan (referenceStatus reference) 400 $"{reference} resolves"

            Expect.isGreaterThan references.Count expectedPaths.Count "rendered pages expose additional local assets and destinations"
            Expect.equal (referenceStatus "/not-a-documentation-route") 404 "unknown references remain unresolved"
        }

        test "Retired Fixture documentation preserves redirects into the Specification" {
            for path in ["/components/fixture";"/docs/components/fixture"] do
                Expect.equal (routeStatus path) 301 "old public destinations remain redirects"
            Expect.equal (routeStatus "/examples/specification") 200 "the replacement destination resolves"
        }

        test "Navigation separates Components from the three source-authored templates" {
            Expect.sequenceEqual (Registry.navigation |> List.map _.label)
                ["Getting started"; "Core concepts"; "Integrations"; "Components"; "Project"; "Examples"] "public information architecture"
            let components = Registry.navigation |> List.find (fun section -> section.label="Components")
            Expect.sequenceEqual (components.sections |> List.map _.label)
                ["Guides"; "Actions"; "Feedback"; "Data display"; "Form controls"; "Navigation"; "Overlays"; "Layout"; "Code and diagrams"] "focused component groups"
            let examples = Registry.navigation |> List.find (fun section -> section.label="Examples")
            Expect.sequenceEqual (examples.pages |> List.map _.path) ["/examples"] "one template gallery, not one gallery entry per inner page"
            Expect.sequenceEqual (Examples.templates |> List.map _.path)
                ["/examples/specification"; "/examples/application"; "/examples/api-documentation"] "exactly three cohesive examples"
            for retired in ["Primitives"; "Application"; "Documentation"; "Page examples"; "Integration examples"] do
                Expect.isFalse (components.sections |> List.exists (fun section -> section.label=retired)) "framework categories are not component navigation"
        }

        test "Catalog families have unique ownership, honest indexes and registry-derived pagers" {
            let families = Catalog.navigation
            Expect.sequenceEqual (families |> List.map _.label) ["Actions"; "Feedback"; "Data display"; "Form controls"; "Navigation"; "Overlays"; "Layout"; "Code and diagrams"] "component groups"
            let rec flatten (group:NavSection) = group.pages @ (group.sections |> List.collect flatten)
            let familyPages = families |> List.collect flatten
            Expect.equal (familyPages |> List.map _.path |> Set.ofList |> Set.count) familyPages.Length "each family route occurs exactly once"
            for family in families do
                for page in flatten family do
                    Expect.equal page.category "Components" $"{page.path} metadata agrees with navigation ownership"
            let root = Catalog.overviewPage |> FSharp.ViewEngine.Components.Templates.DocsView.content |> Render.toString
            for family in families do
                Expect.stringContains root ($"href=\"{family.pages.Head.path}\"") "each area has a working root-index link"
            for index, page in List.indexed Registry.all do
                let html = View.document Registry.navigation page |> Render.toString
                let pager = Regex.Match(html, "<nav aria-label=\"Page navigation\"[^>]*>.*?</nav>", RegexOptions.Singleline).Value
                let expectedLinks =
                    [ if index > 0 then yield "prev", Registry.all[index - 1].path
                      if index + 1 < Registry.all.Length then yield "next", Registry.all[index + 1].path ]
                for relation, path in expectedLinks do
                    Expect.stringContains pager ($"rel=\"{relation}\" href=\"{path}\"") $"{page.path} pager follows the registry"
                Expect.equal (Regex.Matches(pager, "<a ").Count) expectedLinks.Length "no extra pager destination"
        }

        test "Public discovery contains canonical pages and excludes aliases and previews" {
            let sitemap = Handler.sitemap
            let robots = Handler.robots

            for page in Registry.all do
                Expect.stringContains sitemap $"<loc>https://fve.meiermade.com{page.path}</loc>" page.path

            Expect.equal (sitemap.Split("<url>").Length - 1) (Registry.all.Length+Examples.paths.Length) "one sitemap entry per canonical page"
            Expect.isFalse (sitemap.Contains("/docs/components</loc>")) "aliases are excluded"
            Expect.isFalse (sitemap.Contains("/docs/previews/")) "previews are excluded"
            Expect.stringContains robots "Allow: /" "public pages are crawlable"
            Expect.stringContains robots "Sitemap: https://fve.meiermade.com/sitemap.xml" "robots advertises sitemap"
        }

        test "Pages publish the application-owned social image" {
            for page in Registry.all do
                let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                Expect.stringContains html "property=\"og:image\" content=\"https://fve.meiermade.com/social-card.png\"" page.path
                Expect.stringContains html "name=\"twitter:card\" content=\"summary_large_image\"" page.path
        }

        test "Every page renders its semantic heading and legacy anchors" {
            for page in Registry.all do
                let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                let encodedTitle = WebUtility.HtmlEncode page.title
                Expect.stringContains html $">{encodedTitle}</h1>" $"{page.path} title"

                if Catalog.tryPage page.path |> Option.orElseWith (fun () -> ComponentDocumentation.tryPage page.path) |> Option.orElseWith (fun () -> if page.path=Examples.registration.path then Some Examples.gallery else None) |> Option.orElseWith (fun () -> Components.tryPage page.path) |> Option.isNone then
                    let description = page.nodes |> List.tryPick (function | Paragraph content -> Some content | _ -> None)
                    Expect.isSome description $"{page.path} has a useful page summary"
                    Expect.isFalse (html.Contains($">{page.category}</p>")) $"{page.path} does not repeat its category as the description"

                for heading in DocPage.headings page do
                    Expect.stringContains html $"id=\"{heading.id}\"" $"{page.path} heading {heading.id}"
                    if heading.level <= 3 then
                        Expect.stringContains html $"href=\"#{heading.id}\"" $"{page.path} TOC {heading.id}"

                if DocPage.tableOfContents page |> List.isEmpty |> not then
                    Expect.stringContains html "data-docs-toc=\"true\"" $"{page.path} mobile TOC"
                    Expect.stringContains html "aria-label=\"On this page\"" $"{page.path} labelled TOC"
        }

        test "Genuinely renderable samples use the shared code and preview component" {
            let pages = [ Svg.page; TailwindElements.page ]

            for page in pages do
                let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                Expect.stringContains html "data-docs-example=\"true\"" $"{page.path} example"
                Expect.stringContains html "role=\"tablist\"" $"{page.path} tab semantics"
                Expect.stringContains html ">Preview</button>" $"{page.path} preview control"
                Expect.stringContains html ">Code</button>" $"{page.path} code control"
                Expect.stringContains html "aria-label=\"Desktop preview\"" $"{page.path} desktop width preset"
                Expect.stringContains html "aria-label=\"Mobile preview\"" $"{page.path} mobile width preset"
                Expect.stringContains html "data-docs-preview-frame=\"true\"" $"{page.path} locally resizable frame"
                Expect.stringContains html "role=\"separator\"" $"{page.path} accessible drag handle"
                Expect.isFalse (html.Contains("type=\"range\"")) $"{page.path} does not use the retired nested range control"
        }

        test "Code examples are encoded and retain Prism language classes" {
            let html = Custom.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "language-fsharp" "F# language class"
            Expect.stringContains html "language-html" "HTML language class"
            Expect.stringContains html "&lt;my-component class=&quot;container&quot;&gt;" "HTML source is encoded"
            Expect.isFalse (html.Contains("<my-component class=\"container\">")) "example markup must not execute"
        }

        test "Rendered pages contain no Markdown fences" {
            for page in Registry.all do
                let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                Expect.isFalse (html.Contains("```")) $"{page.path} contains a Markdown fence"
        }

        test "Docs use only the pinned self-hosted Datastar runtime" {
            let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "/scripts/datastar.1.0.4.js" "pinned Datastar script"
            Expect.stringContains html "type=\"module\"" "Datastar module script"
            Expect.isFalse (html.Contains("alpinejs")) "Alpine runtime removed"
            Expect.isFalse (html.Contains(" x-data=")) "Alpine directives removed"
        }

        test "Homepage uses the product logo and Tailwind Sky primary color" {
            let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "class=\"docs-home-logo\"" "product logo is shown in the page header"
            Expect.stringContains html "src=\"/logo.svg\"" "page uses the canonical logo asset"
            Expect.stringContains html "--fve-brand-ring:#0ea5e9" "site uses Tailwind Sky 500 for focus"
            Expect.stringContains html "--fve-brand-solid:#0369a1" "site uses Tailwind Sky 700 for solid actions"
        }

        test "Docs uses the host stylesheet without implicit external fonts" {
            let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.isFalse (html.Contains("<style")) "the package does not embed its presentation"
            Expect.stringContains html "rel=\"stylesheet\" href=\"/css/output.css\"" "the repository host links its compiled stylesheet"
            Expect.isFalse (html.Contains("fonts.googleapis.com")) "the package makes no Google Fonts request"
            Expect.isFalse (html.Contains("fonts.gstatic.com")) "the package ships no external font source"
        }

        test "Showcase isolates only complete styled documents" {
            Expect.isFalse (Showcase.previewRoutes.ContainsKey "/docs/previews/workflow-states") "state tabs render directly with host styles"
            Expect.isFalse (Showcase.previewRoutes.ContainsKey "/docs/previews/browser-frame") "browser frames render directly without duplicate framing"

            for path, document in Showcase.previewRoutes |> Map.toSeq do
                Expect.stringStarts document "<!DOCTYPE html>" $"{path} is a complete HTML document"
                Expect.isFalse (document.Contains("<style")) $"{path} does not embed the Docs component styles"
                Expect.stringContains document "rel=\"stylesheet\" href=\"/css/output.css\"" $"{path} links the compiled consumer stylesheet"
                Expect.stringContains document "data-docs-shell=\"true\"" $"{path} initializes the Documentation shell"
                for themeClass in [ "fve-components"; "fve-theme-sky"; "fve-density-compact" ] do
                    Expect.stringContains document themeClass $"{path} uses shared component theme class {themeClass}"
        }

        test "Showcase source regions preserve the exact compiled example" {
            let source = """before
    // docs-example:start notice
    let preview =
        div { "Saved" }
    // docs-example:end notice
after"""

            Expect.equal
                (SourceRegion.extract "notice" source)
                "let preview =\n    div { \"Saved\" }"
                "source is extracted and dedented without marker comments"
            Expect.throws
                (fun () -> SourceRegion.extract "missing" source |> ignore)
                "missing source regions fail rather than displaying approximate code"
        }

        test "Input examples render individual native fields and templates submit complete forms" {
            let examples = Components.examplesFor "input"
            let sizing = examples |> List.find (fun example -> example.title = "Consistent control sizes")
            Expect.equal (Regex.Matches(Render.toString sizing.preview, "type=\"search\"").Count) 4 "three inherited sizes and one explicit override"
            for example in examples |> List.filter (fun example -> example.title <> "Consistent control sizes") do
                Expect.equal (Regex.Matches(Render.toString example.preview, "<input ").Count) 1 "one native input per example"
            let _,accountForm = routeResponse "/examples/application/accounts/new"
            Expect.stringContains accountForm "name=\"name\"" "the application template owns the account name field"
            Expect.stringContains accountForm "method=\"post\"" "template submission is a real server action"
        }

        test "Field wrappers top-align content and keep Select help below the control" {
            for field in [
                Input.create "aligned-input" "Name" |> Input.render
                Textarea.create "aligned-textarea" "Notes" |> Textarea.render
                FileSelection.create "aligned-file" "files" "Files" |> FileSelection.render
                TagInput.create "aligned-tags" "tags" "Tags" [] |> TagInput.render ] do
                Expect.stringContains (Render.toString field) "content-start" "a taller sibling must not stretch label and control rows"
            let config =
                Select.create "aligned-select" "Choice" id [ SelectOption.create "a" "A" ]
                |> Select.withDescription "Choose an option."
                |> Select.withValidation "Check this choice."
            for field in [
                config |> Select.render
                config |> Select.multiple |> Select.render
                config |> Select.withSearch SelectSearch.Static |> Select.render
                config |> Select.multiple |> Select.withSearch SelectSearch.Static |> Select.render ] do
                let html = Render.toString field
                Expect.stringContains html "content-start" "all Select variants top-align their contents"
                let help = html.IndexOf("<p id=", StringComparison.Ordinal)
                Expect.isGreaterThan help (html.IndexOf("</button>", StringComparison.Ordinal)) "help text follows the trigger, matching Input and Textarea"
                Expect.stringContains html "-description" "help remains associated with the field"
        }

        test "Input adornments keep native values and accessible context without extra controls" {
            let html =
                Input.create "amount" "Amount"
                |> Input.withId "adorn"
                |> Input.withLeadingIcon (span { "icon" })
                |> Input.withPrefix "<currency>"
                |> Input.withSuffix "USD"
                |> Input.withDescription "Before tax."
                |> Input.withValidation "Enter an amount."
                |> Input.withValue "12.50"
                |> Input.render |> Render.toString
            Expect.equal (Regex.Matches(html, "<input ").Count) 1 "adornments do not create duplicate controls"
            Expect.stringContains html "name=\"amount\"" "original successful control"
            Expect.stringContains html "value=\"12.50\"" "prefix/suffix are not part of the submitted value"
            Expect.stringContains html "&lt;currency&gt;" "context is encoded text"
            Expect.stringContains html "aria-describedby=\"adorn-prefix adorn-suffix adorn-description adorn-validation\"" "all context is associated"
            Expect.stringContains html "aria-hidden=\"true\"" "leading icon is decorative"
            let suffixOnly = Input.create "price" "Price" |> Input.withId "suffix-only" |> Input.withSuffix "USD" |> Input.render |> Render.toString
            Expect.stringContains suffixOnly "aria-describedby=\"suffix-only-suffix\"" "suffix association does not depend on a prefix"
            Expect.throws (fun () -> Input.create "x" "X" |> Input.withPrefix " " |> ignore) "blank prefix is rejected"
            Expect.throws (fun () -> Input.create "x" "X" |> Input.withSuffix " " |> ignore) "blank suffix is rejected"
        }

        test "Control sizes are explicit overrides independent of density" {
            for size, className in [ ControlSize.Small, "fve-control-small"; ControlSize.Medium, "fve-control-medium"; ControlSize.Large, "fve-control-large" ] do
                let theme = ComponentsTheme.sky |> ComponentsTheme.withControlSize size |> ComponentsTheme.withDensity Density.Compact
                Expect.stringContains (ComponentsTheme.className theme) className "density preserves the requested control size"
                for html in [
                    Input.create "sized" "Sized" |> Input.withSize size |> Input.render |> Render.toString
                    Input.create "search-sized" "Search" |> Input.withType InputType.Search |> Input.withSize size |> Input.render |> Render.toString
                    Button.create (ButtonContent.Text "Sized") |> Button.withSize size |> Button.render |> Render.toString
                    Select.create "sized-select" "Sized" id [ SelectOption.create "a" "A" ] |> Select.withSize size |> Select.render |> Render.toString
                    Select.create "sized-multiple" "Sized" id [ SelectOption.create "a" "A" ] |> Select.withSize size |> Select.multiple |> Select.withSearch SelectSearch.Static |> Select.render |> Render.toString ] do
                    Expect.stringContains html className "all control families share the same override"
                    Expect.stringContains html "text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)]" "control typography follows the selected size tokens"
            let action = Button.create (ButtonContent.Text "Action") |> Button.render |> Render.toString
            Expect.stringContains action "font-medium" "actions use medium rather than semibold weight"
        }

        test "Search presentation owns its icon, accessible label and native clear events" {
            let search = Input.create "find" "Find records" |> Input.withType InputType.Search |> Input.withVisuallyHiddenLabel
            let html = search |> Input.render |> Render.toString
            Expect.stringContains html "fve-search" "native-value clear visibility scope"
            Expect.stringContains html "<svg" "search has a built-in magnifying glass"
            Expect.stringContains html "aria-hidden=\"true\"" "icon does not replace the accessible name"
            Expect.stringContains html "sr-only" "optional hidden label remains accessible"
            Expect.stringContains html "aria-label=\"Clear Find records\"" "clear is named"
            Expect.stringContains (System.Net.WebUtility.HtmlDecode html) "new Event('input'" "clearing updates bound queries"
            Expect.stringContains (System.Net.WebUtility.HtmlDecode html) "new Event('change'" "clearing notifies form consumers"
            Expect.isFalse (html.Contains "data-on:input=") "clear visibility does not depend on a second event-maintained value"
            for unavailable in [ Input.disabled; Input.pending ] do
                let output = search |> Input.withValue "saved" |> unavailable |> Input.render |> Render.toString
                Expect.stringContains (Regex.Match(output, "<button[^>]*>").Value) "disabled" "unavailable fields cannot be cleared"
        }

        test "Multiple choices preserve typed identities, labels, and native repeated values" {
            let options = [ SelectOption.create "a&1" "Alex <Ops>"; SelectOption.create "b\"2" "Jamie Lee" ]
            let selected = Select.create "members" "Members" id options |> Select.multiple |> Select.withSelectedMany [ "b\"2"; "a&1"; "b\"2" ]
            let html = selected |> Select.render |> Render.toString
            Expect.stringContains html "aria-multiselectable=\"true\"" "multiple listbox semantics"
            Expect.equal (Regex.Matches(html, "name=\"members\"").Count) 2 "one successful control per distinct selected value"
            Expect.isLessThan (html.IndexOf("value=\"b&quot;2\"")) (html.IndexOf("value=\"a&amp;1\"")) "selection order, not option order"
            Expect.stringContains html "Alex &lt;Ops&gt;" "option labels are encoded"
            let payload = Regex.Match(System.Net.WebUtility.HtmlDecode html, @"concat\((\[\{.*?\}\])\)").Groups[1].Value
            use choices = System.Text.Json.JsonDocument.Parse payload
            Expect.equal (choices.RootElement[0].GetProperty("value").GetString()) "a&1" "runtime payload retains the identity"
            Expect.equal (choices.RootElement[0].GetProperty("label").GetString()) "Alex <Ops>" "internal F# records do not serialize into empty objects"
            Expect.stringContains html "data-signals__ifmissing" "morphs retain edited selection"
            Expect.throws (fun () -> selected |> Select.withSelectedMany [ "unknown" ] |> ignore) "unknown values cannot lose their labels silently"
            Expect.throws (fun () -> Select.create "duplicate" "Duplicate" (fun (_:int) -> "same") [ SelectOption.create 1 "One"; SelectOption.create 2 "Two" ] |> ignore) "encoded keys must be unique"
            let combo =
                Select.create "people" "People" id options
                |> Select.multiple |> Select.withSelectedMany [ "a&1" ]
                |> Select.withSearch (SelectSearch.Remote "/people")
                |> Select.withOptions [ options[1] ]
            let comboHtml = combo |> Select.render |> Render.toString
            Expect.stringContains comboHtml "data-text=\"($people_selected.length" "selected labels survive replacement of the result set in the shared trigger summary"
            Expect.stringContains comboHtml "value=\"a&amp;1\"" "selected identity remains a native value outside results"
            Expect.equal (Regex.Matches(comboHtml, "data-ignore-morph").Count) 1 "only the selected presentation is client-owned, not the whole field"
            let results = combo |> Select.withQuery "Jamie" |> Select.renderOptions |> Render.toString
            Expect.stringContains results "aria-multiselectable=\"true\"" "remote options retain multiple semantics"
            Expect.isFalse (results.Contains("name=\"people\"")) "result morphs do not own the successful controls"
            Expect.stringContains results "Jamie" "remote results carry their query"
        }

        test "Single and multiple selection setters cannot be mixed" {
            let directory = Path.Combine(Path.GetTempPath(), "fve-choice-types-" + Guid.NewGuid().ToString("N"))
            Directory.CreateDirectory directory |> ignore
            try
                for control in [ "Select" ] do
                    for invalid in [ $"{control}.multiple |> {control}.withSelected 1"; $"{control}.withSelectedMany [ 1 ]" ] do
                        let source =
                            $"#r @\"{typeof<Html>.Assembly.Location}\"\n#r @\"{typeof<ControlSize>.Assembly.Location}\"\nopen FSharp.ViewEngine.Components\n{control}.create \"ids\" \"Ids\" string [ SelectOption.create 1 \"One\" ] |> {invalid} |> ignore"
                        let path = Path.Combine(directory, "Invalid.fsx")
                        File.WriteAllText(path, source)
                        let start = System.Diagnostics.ProcessStartInfo("dotnet")
                        for argument in [ "fsi"; "--exec"; path ] do start.ArgumentList.Add argument
                        start.RedirectStandardOutput <- true
                        start.RedirectStandardError <- true
                        use child = System.Diagnostics.Process.Start start
                        let output = child.StandardOutput.ReadToEndAsync()
                        let errors = child.StandardError.ReadToEndAsync()
                        if not (child.WaitForExit(30000)) then
                            child.Kill(true)
                            failtest "Invalid-mode compilation did not settle."
                        Expect.notEqual child.ExitCode 0 $"{control} rejects {invalid}"
                        Expect.stringContains errors.Result "MultipleSelection" "failure is selection-mode incompatibility"
                        output.Result |> ignore
            finally Directory.Delete(directory, true)
        }

        test "Every gallery example compiles using only its copied code and the public packages" {
            let directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fve-component-examples-" + System.Guid.NewGuid().ToString("N"))
            System.IO.Directory.CreateDirectory directory |> ignore
            try
                let examples = Components.allExamples () @ ComponentDocumentation.allExamples ()
                let sources =
                    (examples |> List.map (fun example -> example.id, example.source))
                    @ ([ "typography"; "step-input"; "step-input-help" ]
                       |> List.map (fun id -> "content-" + id, Components.exampleImports + "\n\n" + Showcase.sourceFor id))
                let samples =
                    sources |> List.mapi (fun index (id, copiedSource) ->
                        let repositorySource = copiedSource.Replace("Acme.Components", "FSharp.ViewEngine.Components", StringComparison.Ordinal)
                        $"# 1 \"{id}.fsx\"\nmodule Example{index} =\n" + (repositorySource.Split('\n') |> Array.map (fun line -> "    " + line) |> String.concat "\n"))
                let reference (path:string) = "#r @\"" + path.Replace("\"", "\"\"") + "\"\n"
                let source =
                    reference typeof<Html>.Assembly.Location + reference typeof<ControlSize>.Assembly.Location
                    + "#r \"System.Text.Json\"\n\n" + String.concat "\n\n" samples
                let path = System.IO.Path.Combine(directory, "Examples.fsx")
                System.IO.File.WriteAllText(path, source)
                let start = System.Diagnostics.ProcessStartInfo("dotnet")
                start.ArgumentList.Add "fsi"
                start.ArgumentList.Add "--exec"
                start.ArgumentList.Add path
                start.WorkingDirectory <- directory
                start.UseShellExecute <- false
                start.RedirectStandardOutput <- true
                start.RedirectStandardError <- true
                use child = System.Diagnostics.Process.Start start
                let output = child.StandardOutput.ReadToEndAsync()
                let errors = child.StandardError.ReadToEndAsync()
                if not (child.WaitForExit(60000)) then
                    child.Kill(true)
                    child.WaitForExit()
                    failtest "Standalone Components examples did not compile within 60 seconds."
                Expect.equal child.ExitCode 0 (output.Result + errors.Result)
            finally
                System.IO.Directory.Delete(directory, true)
        }

        test "Selected example declarations preserve source order and reject missing definitions" {
            let source = "module Sample =\n    type Choice = Yes | No\n    let private value = 1\n    // docs-example:start example\n    let example =\n        let nested = value\n        nested\n    // docs-example:end example\n    let unused = 2\n"
            Expect.equal (SourceRegion.declarations [ "example"; "Choice"; "value" ] source)
                "type Choice = Yes | No\n\nlet private value = 1\n\nlet example =\n    let nested = value\n    nested"
                "source order and nested bindings are preserved without extraction markers"
            Expect.throws (fun () -> SourceRegion.declarations [ "missing" ] source |> ignore) "unknown declarations cannot silently disappear"
        }

        test "Three templates render independently of the library shell with complete source" {
            let gallery = Examples.registration |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.equal (Regex.Matches(gallery,"data-example-template=").Count) 3 "exactly three template cards"
            for path in Examples.paths do
                let status,html = routeResponse path
                Expect.equal status 200 path
                Expect.stringContains html "data-example-viewer=" "separate template experience"
                Expect.stringContains html $"rel=\"canonical\" href=\"https://fve.meiermade.com{path}\"" "complete template metadata"
                Expect.stringContains html "id=\"main-content\"" "one semantic content target"
                Expect.isFalse (html.Contains "data-docs-sidebar") "no outer library documentation shell"
                Expect.isFalse (html.Contains "data-example-viewer-bar") "no duplicate preview/code chrome"
                Expect.stringContains html "href=\"/examples/source.zip\"" "complete source is downloadable from the host top bar"
            Expect.equal (Regex.Matches(gallery,"target=\"_blank\" rel=\"noopener\"").Count) 3 "each template opens in an isolated new tab"
            for file in Examples.sourceFiles do
                let status,source = routeResponse ("/examples/source/"+file)
                Expect.equal status 200 file
                Expect.isFalse (String.IsNullOrWhiteSpace source) "complete authored source"
                Expect.isFalse (source.Contains "FSharp.ViewEngine.Components.Templates") "no framework dependency"
                Expect.isFalse (source.Contains "data-example-viewer-bar") "catalog chrome stays out of copied templates"
            for invalid in ["/examples/source/../Program.fs"; "/examples/source/secrets.txt"; "/examples/application/accounts/999"; "/examples/application/accounts/101?view=code&file=secrets.txt"; "/api-reference/render-to-string"; "/specification/render-a-view"] do
                Expect.equal (routeStatus invalid) 404 "unknown routes and source files fail closed"
            for path in ["/examples/specification/accounts/view-accounts";"/examples/specification/accounts/create-account";"/examples/specification/architecture/solution/server"] do
                Expect.equal (routeStatus path) 200 "named workflows and project contracts are public destinations"
            let _,preview = routeResponse "/examples/specification/accounts/create-account"
            let controlIds = Regex.Matches(preview,"<(?:input|button|select|dialog)[^>]* id=\"([^\"]+)\"") |> Seq.cast<Match> |> Seq.map (fun item -> item.Groups[1].Value) |> Seq.toList
            Expect.equal controlIds.Length (controlIds |> List.distinct |> List.length) "inline states do not share control identities"
            let launch = Regex.Matches(preview,"<a[^>]+href=\"([^\"]+)\"[^>]+aria-label=\"Open Create account[^\"]* in App mode\"") |> Seq.cast<Match> |> Seq.map (fun item -> WebUtility.HtmlDecode item.Groups[1].Value) |> Seq.toList
            Expect.isNonEmpty launch "each preview has a real launch link"
            for href in launch do
                let parameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery((Uri(Uri "https://fve.meiermade.com",href)).Query)
                Expect.equal (string parameters["appMode"]) "1" "resource-less launch selects App mode"
                Expect.equal (string parameters["fveAppDock"]) "bottom" "launch includes dock context"
                let status,html = routeResponse href
                Expect.equal status 200 "launch destination resolves"
                Expect.stringContains html "aria-label=\"App mode controls\"" "launch renders operable review controls"
                Expect.isFalse (html.Contains "data-example-viewer-bar") "App mode excludes catalog chrome"
        }

        test "Template forms validate finite outcomes without retaining submissions or changing records" {
            let accountFields name kind = ["name",name;"accountType",kind;"parentType",kind;"currency","USD";"subtype","Generic";"observedBalance","not-required"]
            let before = routeResponse "/examples/application/accounts/105" |> snd
            for path,fields,state,message in [
                "/examples/application/accounts/new",accountFields "Private reserve" "Asset","validated","Account validated"
                "/examples/application/accounts/new",accountFields "Operating checking" "Asset","invalid","Use a unique name between 1 and 80 characters."
                "/examples/application/accounts/new",accountFields "Private reserve" "unavailable","invalid-type","Check account type"
                "/examples/application/accounts/new",accountFields "Private reserve" "Asset" |> List.map (fun (key,value) -> key,if key="currency" then "EUR" else value),"invalid-details","Check account details"
                "/examples/application/accounts/101/edit",accountFields "Operating checking" "Asset","validated","Account validated"
                "/examples/application/accounts/101/edit",accountFields "Tax reserve" "Asset","invalid","Use a unique name between 1 and 80 characters."
                "/examples/application/accounts/101/delete",["action","delete"],"delete-blocked","Account cannot be deleted"
                "/examples/application/accounts/105/delete",["action","delete"],"deleted","Deletion validated"
                "/examples/application/accounts",["action","review-selected";"accountIds","105";"accountIds","group-Asset"],"selection-valid","Selected records checked"
                "/examples/application/accounts",["action","review-selected"],"selection-invalid","Choose records"
                "/examples/application/transactions",["action","review-selected";"transactionIds","201"],"selection-valid","Selected records checked"
                "/examples/application/transactions",["action","review-selected";"transactionIds","999"],"selection-invalid","Choose records"
                "/examples/application/settings/organizations",["workspace","Private workspace";"currency","USD"],"validated","Organization values validated"
                "/examples/application/settings/organizations",["workspace","";"currency","USD"],"invalid","Check organization values"
                "/examples/application/profile",["name","Private person";"email","private@example.invalid";"timezone","UTC"],"validated","Profile values validated"
                "/examples/application/profile",["name","Private person";"email","not-an-email";"timezone","UTC"],"invalid","Check profile values" ] do
                let status,location,cookies,body,cache = postFormResponse path fields
                let editorFailure = path.Contains("/accounts/") && state.StartsWith("invalid", StringComparison.Ordinal)
                Expect.equal status (if editorFailure then 200 else 302) path
                Expect.equal cookies "" "submissions create no session cookies"
                let html =
                    if editorFailure then
                        Expect.equal location "" "failed editor values never enter a redirect URL"
                        Expect.equal cache "private, no-store" "private editor responses cannot be stored"
                        body
                    else
                        let parameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery((Uri(Uri "https://fve.meiermade.com",location)).Query)
                        Expect.equal (string parameters["state"]) state "authoritative finite outcome"
                        let renderStatus,html = routeResponse location
                        Expect.equal renderStatus 200 "redirect destination resolves"
                        html
                Expect.stringContains html message "rendered feedback matches the outcome"
                for privateValue in ["Private reserve";"Private workspace";"Private person";"private@example.invalid"] do
                    Expect.isFalse (location.Contains privateValue) "private values never enter URLs"
                    if not editorFailure then Expect.isFalse (html.Contains privateValue) "successful/non-editor submissions do not retain private values"
            Expect.equal (routeResponse "/examples/application/accounts/105" |> snd) before "eligible deletion does not mutate the record"
        }

        test "Account editor failures retain exact private entries only in the current response" {
            let path = "/examples/application/accounts/101/edit"
            let fields = ["name","  Private draft  ";"accountType","Asset";"parentType","Expense";"currency","USD";"subtype","Generic";"observedBalance","not-required";"from","accounts";"search","operating";"filterAccountType","Expense";"sort","balance"]
            let status,location,cookies,body,cache = postFormResponse path fields
            Expect.equal status 200 "invalid parent/type relationship renders field errors directly"
            Expect.equal cache "private, no-store" "the private draft is not cacheable"
            Expect.equal location "" "there is no draft redirect"
            Expect.equal cookies "" "there is no durable draft session"
            Expect.stringContains body "value=\"  Private draft  \"" "name whitespace survives this response exactly"
            Expect.stringContains body "name=\"filterAccountType\" value=\"Expense\"" "collection filtering is distinct from the editable type"
            let resultTitle = Regex.Match(body,"<title[^>]*>([^<]+)</title>").Groups[1].Value |> WebUtility.HtmlDecode
            Expect.equal resultTitle "Accounts · Application example" "opening/validating the editor keeps its collection title and host identity"
            for href in Regex.Matches(body,"href=\"([^\"]*)\"") |> Seq.cast<Match> do
                Expect.isFalse (WebUtility.HtmlDecode(href.Groups[1].Value).Contains "Private draft") "private values never enter navigation destinations"
            Expect.isFalse ((routeResponse path |> snd).Contains "Private draft") "a new GET cannot recover the draft"
            let success = fields |> List.map (fun (key,value) -> key,if key="parentType" then "Asset" else value)
            let successStatus,redirect,_,_,_ = postFormResponse path success
            Expect.equal successStatus 302 "valid submission returns to the collection"
            let destination = Uri(Uri "https://fve.meiermade.com",redirect)
            Expect.equal destination.AbsolutePath path "the finite result remains on the submitted editor route"
            let resultTitle = Regex.Match(routeResponse redirect |> snd,"<title[^>]*>([^<]+)</title>").Groups[1].Value |> WebUtility.HtmlDecode
            Expect.equal resultTitle "Accounts · Application example" "the originating collection remains the background"
            let query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery destination.Query
            for key,value in ["search","operating";"accountType","Expense";"sort","balance";"from","accounts"] do
                Expect.equal (string query[key]) value ("public context survives: "+key)
        }

        test "Transaction deletion validates existence without restricting verification or mutating fixtures" {
            for id in [201;203] do
                let record = $"/examples/application/transactions/{id}"
                let before = routeResponse record |> snd
                let path = record+"/delete"
                Expect.equal (routeStatus path) 200 "verified and unverified records can both open confirmation"
                let status,location,cookies,_ = postForm path ["action","delete";"from","transactions"]
                Expect.equal status 302 "an existing record validates deletion"
                Expect.equal cookies "" "deletion does not create a session"
                Expect.stringContains location "state=deleted" "deletion outcome is authoritative"
                Expect.stringContains location "from=transactions" "the originating collection is retained"
                Expect.stringContains (routeResponse location |> snd) "Transactions" "the finite outcome renders its collection background"
                Expect.equal (routeResponse record |> snd) before "validation does not mutate seeded records"
            let missing = "/examples/application/transactions/999/delete"
            Expect.equal (routeStatus missing) 404 "missing records cannot open confirmation"
            Expect.equal (postForm missing ["action","delete"] |> fun (status,_,_,_) -> status) 404 "missing records cannot validate deletion"
            Expect.equal (routeStatus "/examples/specification/transactions/delete-transaction") 200 "deletion has an owning Specification workflow"
            Expect.equal (routeStatus "/examples/api-documentation/delete-transaction") 200 "deletion has an owning API contract"
        }

        test "Example ZIP exposes complete consumer source and pinned runtime assets" {
            let webRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "Docs", "wwwroot"))
            use app = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(Microsoft.AspNetCore.Builder.WebApplicationOptions(WebRootPath=webRoot)).Build()
            let context = DefaultHttpContext()
            context.RequestServices <- app.Services
            context.Request.Scheme <- "https"
            context.Request.Host <- HostString "fve.meiermade.com"
            context.Request.Path <- PathString "/examples/source.zip"
            context.Response.Body <- new MemoryStream()
            let next : HttpFunc = fun current -> task { return Some current }
            Handler.routes next context |> Async.AwaitTask |> Async.RunSynchronously |> ignore
            Expect.equal context.Response.StatusCode 200 "ZIP download resolves"
            Expect.equal context.Response.ContentType "application/zip" "download has the native ZIP media type"
            context.Response.Body.Position <- 0L
            use archive = new System.IO.Compression.ZipArchive(context.Response.Body,System.IO.Compression.ZipArchiveMode.Read)
            let entries = archive.Entries |> Seq.map _.FullName |> Set.ofSeq
            for file in Examples.sourceFiles do Expect.isTrue (entries.Contains file) ("complete source: "+file)
            for file in ["wwwroot/scripts/datastar.1.0.4.js";"wwwroot/scripts/mermaid.11.16.0.min.js";"wwwroot/css/prism-tomorrow.1.29.0.min.css"] do
                Expect.isTrue (entries.Contains file) ("pinned runtime asset: "+file)
            for entry in archive.Entries |> Seq.filter (fun entry -> entry.FullName.EndsWith ".fs") do
                use reader = new StreamReader(entry.Open())
                Expect.isFalse (reader.ReadToEnd().Contains "FSharp.ViewEngine.Components.Templates") "copied source has no private Templates dependency"
        }

        test "Specification form outcomes preserve exact resource and submitted workspace mode and dock" {
            let path = "/examples/specification/accounts/update-account?resource=105&appMode=1&fveAppDock=bottom"
            let fields = ["name","Unassigned expense";"accountType","Expense";"parentType","Expense";"currency","USD";"subtype","Generic";"observedBalance","not-required";"organization","client-organization";"environment","staging";"ledger","operating-company";"appMode","1";"fveAppDock","top"]
            let status,location,_,_ = postForm path fields
            Expect.equal status 302 "native form returns a finite destination"
            let destination = Uri(Uri "https://fve.meiermade.com",location)
            Expect.equal destination.AbsolutePath "/examples/specification/accounts/update-account" "result remains in the current workflow"
            let parameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery destination.Query
            for key,value in ["resource","105";"appMode","1";"fveAppDock","top";"organization","client-organization";"environment","staging";"ledger","operating-company";"specState","validated"] do
                Expect.equal (string parameters[key]) value ("redirect retains "+key)
            let status,html = routeResponse location
            Expect.equal status 200 "result resolves"
            Expect.stringContains html "Account validated" "result renders the validation outcome"
            Expect.stringContains html "Unassigned expense" "result is for the submitted resource, not the first account"
        }

        test "Template POST boundary rejects foreign origins unsupported content and the first oversized body" {
            let path = "/examples/application/accounts/new"
            Expect.equal (mutationStatus path "https://foreign.test" "application/x-www-form-urlencoded" 0L) 403 "foreign origin is rejected"
            Expect.equal (mutationStatus path "https://fve.meiermade.com" "application/json" 2L) 415 "unsupported body type is rejected"
            let fields = ["name","Private limit check";"accountType","Asset";"parentType","Asset";"currency","USD";"subtype","Generic";"observedBalance","not-required"]
            let encodedLength = fields |> List.sumBy (fun (key,value) -> (Uri.EscapeDataString key).Length+1+(Uri.EscapeDataString value).Length+1)
            for length,expected in [64000,302;64001,413] do
                let padded = fields @ ["padding",String.replicate (length-encodedLength-"padding=".Length) "x"]
                let status,_,cookies,_ = postForm path padded
                Expect.equal status expected "limit is tested with a real encoded body"
                Expect.equal cookies "" "request boundary creates no session"
        }

        test "Every reusable component page leads with use, teaches installation and usage, and ends with its curated API" {
            let allComponentPages =
                Components.actionRegistrations
                @ Components.feedbackRegistrations
                @ Components.dataDisplayRegistrations
                @ Components.formControlRegistrations
                @ Components.navigationRegistrations
                @ Components.overlayRegistrations
                @ Components.compositionRegistrations
                @ Components.frameRegistrations
                @ Components.applicationNavigationRegistrations
                @ Components.applicationWorkflowRegistrations
                @ Components.applicationResourceRegistrations
            let apiComponentPages = allComponentPages |> List.filter (fun registration -> registration.id <> Components.formLayoutsRegistration.id)
            let renderContent (content:HtmlElement list) = fragment { content } |> Render.toString
            for registration in allComponentPages do
                let page = Components.tryPage registration.path |> Option.get
                Expect.isFalse (String.IsNullOrWhiteSpace page.description) $"{registration.title} has a concise description"
                let leadHtml = renderContent page.lead
                Expect.equal (Regex.Matches(leadHtml, "data-docs-example=\"true\"").Count) 1 $"{registration.title} has one canonical lead example"
                Expect.equal (page.sections |> List.take 2 |> List.map _.id) [ "installation"; "usage" ] $"{registration.title} teaches installation before usage"
                match page.rightRail with
                | TableOfContents -> ()
                | _ -> failtest $"{registration.title} does not use the shared On this page rail"
            for registration in apiComponentPages do
                let page = Components.tryPage registration.path |> Option.get
                let reference = List.last page.sections
                Expect.equal reference.id "api-reference" $"{registration.title} puts its curated public API last"
                let html = renderContent reference.content
                Expect.stringContains html "data-fve-api-reference=\"true\"" $"{registration.title} renders the shared API reference"
                Expect.stringContains html "val " $"{registration.title} exposes public function signatures"
            for registration in ComponentDocumentation.registrations do
                let page = ComponentDocumentation.tryPage registration.path |> Option.get
                Expect.equal (page.sections |> List.take 2 |> List.map _.id) [ "installation"; "usage" ] $"Documentation {registration.title} teaches installation and usage after its preview"
                Expect.equal page.lead.Length 1 $"Documentation {registration.title} has one canonical lead (which may demonstrate a nested Example)"
                Expect.stringContains (renderContent page.lead) "data-docs-example=\"true\"" "lead owns an executable example"
                Expect.equal (List.last page.sections).id "api-reference" $"Documentation {registration.title} puts its curated API last"
                let html = renderContent (List.last page.sections).content
                Expect.stringContains html "data-fve-api-reference=\"true\"" $"Documentation {registration.title} renders the shared API reference"
                Expect.stringContains html "val " $"Documentation {registration.title} exposes public function signatures"

            for registration in [ Components.accessibilityRegistration; Components.themingRegistration ] do
                let page = Components.tryPage registration.path |> Option.get
                Expect.equal page.sections.Head.id "api-reference" $"{registration.title} owns its shared public declarations"

            let buttonReference = Components.tryPage "/components/button" |> Option.get |> _.sections |> List.last |> _.content |> renderContent
            for signature in [ "type ButtonColor"; "type ButtonVariant"; "type ButtonConfig"; "module Button ="; "val create"; "val withColor"; "val withVariant"; "val render" ] do
                Expect.stringContains buttonReference signature $"Button API includes {signature}"
            for unrelated in [ "ButtonGroup"; "DropdownMenu" ] do
                Expect.isFalse (buttonReference.Contains unrelated) $"Button does not claim {unrelated} declarations"
            Expect.isFalse (buttonReference.Contains("<dl")) "XML summaries are not repeated as default-note cards"
            Expect.isFalse (buttonReference.Contains("label: string; color:")) "private ButtonConfig fields stay hidden"
            Expect.isFalse (buttonReference.Contains("// opaque")) "hidden representations do not add internal jargon"
            Expect.stringContains buttonReference "| Primary" "public union cases remain available to consumers"
            for path,helper,cases in [
                "/components/command", "CommandContent", [ "| Link of"; "| Action of" ]
                "/components/dropdown-menu", "MenuItemContent", [ "| Link of"; "| Action of"; "| Radio of"; "| Checkbox of"; "| Group of" ]
                "/components/button-group", "", [ "| Button of"; "| Menu of" ] ] do
                let reference = Components.tryPage path |> Option.get |> _.sections |> List.last |> _.content |> renderContent
                for case in cases do Expect.isFalse (reference.Contains case) $"{path} never advertises inaccessible constructors"
                if helper<>"" then Expect.isFalse (reference.Contains helper) $"{path} never advertises private helper types"

            let buttonGroupReference = Components.tryPage "/components/button-group" |> Option.get |> _.sections |> List.last |> _.content |> renderContent
            for declaration in [ "type ButtonGroupOrientation"; "type ButtonGroupItem"; "type ButtonGroupConfig"; "module ButtonGroupItem ="; "module ButtonGroup =" ] do
                Expect.equal (Regex.Matches(buttonGroupReference, Regex.Escape declaration).Count) 1 $"Button group includes {declaration} once"
            Expect.isFalse (buttonGroupReference.Contains("type &#39;destination")) "generic parameters are never emitted as standalone declarations"
            Expect.isFalse (buttonGroupReference.Contains("type DropdownMenuConfig")) "referenced Dropdown menu declarations stay on their owning page"

            let exactReference = FSharpApiReference.create [ typeof<ButtonConfig> ] |> FSharpApiReference.render |> Render.toString
            Expect.stringContains exactReference "type ButtonConfig" "explicit API roots are rendered"
            Expect.isFalse (exactReference.Contains("module Button")) "explicit API roots never trigger prefix or dependency expansion"

            let apiAssembly = typeof<ButtonConfig>.Assembly
            let xml = XDocument.Load(Path.ChangeExtension(apiAssembly.Location, ".xml"))
            let documentedTypeMembers =
                xml.Descendants(XName.Get "member")
                |> Seq.filter (fun member' ->
                    let name = member'.Attribute(XName.Get "name")
                    not (isNull name) && name.Value.StartsWith("T:", StringComparison.Ordinal))
                |> Seq.toList
            let multiplyOwned =
                documentedTypeMembers
                |> List.choose (fun member' ->
                    let categories = member'.Elements(XName.Get "category") |> Seq.toList
                    if categories.Length > 1 then Some(member'.Attribute(XName.Get "name").Value) else None)
            Expect.isEmpty multiplyOwned "a public declaration has at most one owning API category"
            let documentedTypes =
                documentedTypeMembers
                |> Seq.choose (fun member' ->
                    let name = member'.Attribute(XName.Get "name")
                    let isExplicit = member'.Elements(XName.Get "category") |> Seq.isEmpty |> not || not (isNull (member'.Element(XName.Get "exclude")))
                    if not isExplicit then None else Some(name.Value.Substring(2)))
                |> Set.ofSeq
            let uncategorized =
                apiAssembly.GetExportedTypes()
                |> Array.filter (fun type' ->
                    isNull type'.DeclaringType
                    && not (isNull type'.Namespace)
                    && type'.Namespace.StartsWith("FSharp.ViewEngine.Components", StringComparison.Ordinal)
                    && not (documentedTypes.Contains type'.FullName))
                |> Array.map _.FullName
            Expect.isEmpty uncategorized "every public top-level declaration is explicitly categorized or excluded beside its source declaration"
        }

        test "Components publishes a first-class page for every public component and focused shared guides" {
            let render registration = registration |> View.document Registry.navigation |> Render.toHtmlDocString
            let overview = render Components.overviewRegistration
            let installation = render Components.installationRegistration
            let componentRegistrations =
                Components.actionRegistrations
                @ Components.feedbackRegistrations
                @ Components.dataDisplayRegistrations
                @ Components.formControlRegistrations
                @ Components.navigationRegistrations
                @ Components.overlayRegistrations
                @ Components.compositionRegistrations
                @ Components.frameRegistrations
                @ Components.applicationNavigationRegistrations
                @ Components.applicationWorkflowRegistrations
                @ Components.applicationResourceRegistrations
                @ Components.pageExampleRegistrations
            let renderedComponents = componentRegistrations |> List.map render
            let renderedGuides = Components.guideRegistrations |> List.map render
            let renderedDocumentationControls = ComponentDocumentation.registrations |> List.map render
            let allHtml = String.concat Environment.NewLine (overview :: installation :: renderedComponents @ renderedGuides @ renderedDocumentationControls)

            Expect.isFalse (allHtml.Contains("href=\"/components/chart\"")) "removed Chart stays absent"
            for item in Catalog.componentRegistrations do
                Expect.stringContains overview ($"href=\"{item.path}\"") "every component is discoverable from the index"
            Expect.stringContains overview "href=\"/examples\"" "page assembly goes to the template gallery"
            for framework in ["app-shell"; "collection"; "detail"; "form-layouts"; "media-library"] do
                Expect.isFalse (Catalog.componentRegistrations |> List.exists (fun item -> item.path="/components/"+framework)) "framework assembly is not an installable component"
            Expect.equal (Catalog.componentRegistrations |> List.map _.path |> List.distinct |> List.length) Catalog.componentRegistrations.Length "one owning page per component"
            Expect.stringContains installation "dotnet tool install FSharp.ViewEngine.Cli" "local tool installation"
            Expect.stringContains installation "dotnet fve init" "consumer-owned project initialization"
            Expect.stringContains installation "@source &quot;./src/Acme.Components/Components/**/*.fs&quot;" "direct Tailwind source detection"

            let registrySource =
                Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "FSharp.ViewEngine.Components", "FSharp.ViewEngine.Components.Registry.props"))
                |> File.ReadAllText
            for registration, html in List.zip componentRegistrations renderedComponents do
                Expect.stringContains html "data-docs-layout=\"gallery\"" $"{registration.path} uses the gallery layout"
                let isPageExample = registration.path.StartsWith("/components/page-examples/", StringComparison.Ordinal)
                if isPageExample then
                    for rejected in [ "id=\"installation\""; "id=\"usage\""; "data-docs-toc=\"true\"" ] do
                        Expect.isFalse (html.Contains rejected) $"{registration.path} keeps the Page-example workflow structure without {rejected}"
                else
                    Expect.stringContains html "id=\"installation\"" $"{registration.path} has component-specific installation"
                    Expect.stringContains html "id=\"usage\"" $"{registration.path} has copyable usage"
                    Expect.stringContains html "data-docs-toc=\"true\"" $"{registration.path} has On this page navigation"
                    Expect.stringContains html "class=\"sr-only\" data-docs-example-title=\"true\"" $"{registration.path} leads with one unlisted canonical example"
                    let selectorMatch = Regex.Match(html, "dotnet fve add ([a-z0-9-]+) --config")
                    Expect.isTrue selectorMatch.Success $"{registration.path} renders an exact fve add command"
                    Expect.stringContains registrySource $"FveName=\"{selectorMatch.Groups[1].Value}\"" $"{registration.path} installation selector exists in the canonical source registry"
                Expect.stringContains html "data-docs-example=\"true\"" $"{registration.path} has an executable example"
                Expect.stringContains html "Example: &#39;preview&#39;" $"{registration.path} shows its preview by default"
                let examples = html.Split([| "data-docs-example=\"true\"" |], System.StringSplitOptions.None).Length - 1
                let expectedExamples = Components.examplesFor (registration.path.Substring("/components/".Length)) |> List.length
                Expect.equal examples expectedExamples $"{registration.path} has its focused component examples without repeating Default"
                let ids = Regex.Matches(html, "\\sid=\"([^\"]+)\"") |> Seq.map (fun m -> m.Groups[1].Value) |> Seq.toList
                Expect.equal (List.distinct ids |> List.length) ids.Length $"{registration.path} has no duplicate example or control IDs"

            for path, selector in [
                "/components/avatar", "avatar"
                "/components/copy-reveal", "copy-reveal"
                "/components/input", "input"
                "/components/textarea", "textarea"
                "/components/tag-input", "tag-input"
                "/components/drawer", "drawer"
                "/components/page-header", "page-header"
                "/components/page-top-bar", "page-top-bar" ] do
                Expect.stringContains (render (Components.allRegistrations |> List.find (fun registration -> registration.path = path))) $"dotnet fve add {selector} --config" $"{path} installs its owning source selector"

            for registration, html in List.zip Components.guideRegistrations renderedGuides do
                Expect.stringContains html "data-docs-layout=\"article\"" $"{registration.path} uses the article layout"
                Expect.isFalse (html.Contains("data-docs-example=\"true\"")) $"{registration.path} is focused guidance rather than a duplicate gallery"

            Expect.stringContains allHtml "/components/menus/actions" "DropdownMenu example uses a real Docs-owned patch endpoint"
            Expect.stringContains allHtml "data-signals=\"{_account_open: false, account_query:" "remote Combobox emits local open state and an intentionally submitted query"
            Expect.stringContains allHtml "role=\"switch\"" "Switch preserves switch semantics"
            Expect.stringContains allHtml "aria-pressed=\"true\"" "ToggleButton preserves pressed semantics"
            Expect.stringContains allHtml "type=\"radio\"" "RadioGroup preserves form semantics internally"
            Expect.stringContains allHtml "requestCancellation: &#39;auto&#39;" "remote Combobox documents deterministic newest-request behavior"
            for endpoint in [ "/components/choices/select"; "/components/choices/checkbox"; "/components/choices/switch"; "/components/choices/radio" ] do
                Expect.stringContains allHtml endpoint $"focused example posts to real Docs endpoint {endpoint}"
            Expect.stringContains allHtml "id=\"review-account-dialog-trigger\"" "Dialog renders its connected trigger"
            Expect.stringContains allHtml "data-on:close=\"document.getElementById(&quot;review-account-dialog-trigger&quot;)?.focus()\"" "Dialog close restores trigger focus"
            Expect.stringContains allHtml "role=\"alertdialog\"" "Dialog composition preserves urgent confirmation semantics"
            Expect.stringContains allHtml "data-indicator:_delete_account_confirmation_pending" "the confirmation form owns immediate duplicate-submit protection"
            Expect.stringContains allHtml "id=\"account-settings-drawer\"" "Drawer renders a stable native dialog"
            Expect.stringContains allHtml "Account details refreshed from the server." "Drawer preserves server-patched detail content"
            Expect.stringContains allHtml "/components/dialogs/confirm" "the confirmation form uses a real Docs-owned endpoint"
            Expect.stringContains allHtml "/components/drawers/account" "Drawer uses a real Docs-owned patch endpoint"
            Expect.stringContains allHtml "/components/tabs/review" "Tabs uses a real Docs-owned patch endpoint"
            Expect.stringContains allHtml "data-signals=\"{_components_menu_actions_open: false, _components_menu_actions_typeahead:" "menu IDs become valid isolated interaction signal tokens"
            Expect.isFalse (allHtml.Contains("_components-menu-actions-open")) "DOM IDs are not copied unsafely into expressions"
            Expect.stringContains allHtml "aria-current=\"page\"" "typed navigation retains the current destination"
            Expect.stringContains allHtml "data-fve-gallery-contained=\"true\"" "standalone composition fixtures contain destination navigation"
            Expect.stringContains allHtml "--fve-brand-solid" "consumer theme overrides are documented"
            Expect.stringContains overview "rel=\"prev\" href=\"/extensions/tailwind-elements\"" "Components follows integrations"
            Expect.stringContains overview "rel=\"next\" href=\"/components/installation\"" "overview continues to installation"
            let versioning = render Components.versioningRegistration
            Expect.stringContains versioning "rel=\"prev\" href=\"/components/customization\"" "last guide follows customization"
            Expect.stringContains versioning "rel=\"next\" href=\"/components/button\"" "shared guides precede focused components"
        }

        test "Breadcrumbs, SideNav, PageTopBar, PageHeader, Section, Page, and AppShell preserve typed ownership and responsive semantics" {
            let breadcrumbs =
                Breadcrumbs.create "account-breadcrumbs" "Breadcrumb" [
                    BreadcrumbItem.create Home "Home"
                    BreadcrumbItem.create Accounts "Accounts"
                    BreadcrumbItem.create (Account 2048) "Account 2048" ]
            let breadcrumbsHtml = breadcrumbs |> Breadcrumbs.render shellTestUrl |> Render.toString
            Expect.stringContains breadcrumbsHtml "<nav id=\"account-breadcrumbs\" aria-label=\"Breadcrumb\"" "Breadcrumbs renders a labelled landmark"
            Expect.stringContains breadcrumbsHtml "href=\"/accounts\"" "breadcrumb ancestors use the typed resolver"
            Expect.stringContains breadcrumbsHtml "aria-current=\"page\"" "breadcrumb current location is identified"
            Expect.equal (Regex.Matches(breadcrumbsHtml, "aria-current=\"page\"").Count) 1 "Breadcrumbs identifies one current item"
            Expect.isFalse (breadcrumbsHtml.Contains("href=\"/accounts/2048\"")) "the current breadcrumb is not a redundant link"
            Expect.stringContains breadcrumbsHtml "aria-label=\"Show hidden breadcrumbs\"" "deep paths expose compact overflow access"
            Expect.stringContains breadcrumbsHtml "sm:hidden" "deep paths compact on narrow screens"
            Expect.stringContains breadcrumbsHtml "hidden sm:flex" "short paths remain available on wider screens"
            Expect.throwsT<ArgumentException> (fun () -> breadcrumbs |> Breadcrumbs.withMaxVisibleItems 1 |> ignore) "overflow policy must retain root and current location"
            let twoVisible = breadcrumbs |> Breadcrumbs.withMaxVisibleItems 2 |> Breadcrumbs.render shellTestUrl |> Render.toString
            Expect.stringContains twoVisible "account-breadcrumbs-middle-overflow" "an explicit visible-item policy adds middle overflow"
            Expect.equal (Regex.Matches(twoVisible, "aria-current=\"page\"").Count) 1 "overflow does not duplicate the current page"
            Expect.isFalse (twoVisible.Contains("href=\"/accounts/2048\"")) "current destination remains unlinked even with overflow"

            let icon = span { "L" }
            let grouped =
                SideNav.create
                    "product-navigation"
                    "Product navigation"
                |> SideNav.withHeader (SideNavHeader.create "Ledger" |> SideNavHeader.withContent (strong { icon; " Ledger" }) |> SideNavHeader.withCompactContent icon)
                |> SideNav.withContent (SideNavContent.create [
                    SideNavSection.create "Manage" [
                        SideNavItem.create Home "Dashboard" |> SideNavItem.withLeading icon
                        SideNavItem.create Accounts "Accounts" |> SideNavItem.withBadge (span { "4" }) |> SideNavItem.withAction (button { _type "button"; _ariaLabel "Pin accounts"; "Pin" })
                        SideNavItem.unavailable "Unavailable" ]
                    SideNavSection.create "Analyze" [ SideNavGroup.create "Reporting" [ SideNavItem.create Reports "Reports" ] |> SideNavGroup.withLeading icon |> SideNavGroup.expanded ]
                    SideNavSection.create "Configure" [ SideNavItem.create Settings "Settings" ] ])
                |> SideNav.withCurrent Accounts
                |> SideNav.withWidth SideNavWidth.Standard
                |> SideNav.withContext [p { "Meier Made" }]
                |> SideNav.withMobileContext (p { "Meier Made mobile" })
                |> SideNav.withFooter [a { _href "/account"; "Andrew Meier" }]
                |> SideNav.withCompactFooter (a { _href "/account"; _ariaLabel "Andrew Meier profile"; "AM" })
            let groupedHtml = grouped |> SideNav.render shellTestUrl |> Render.toString
            Expect.stringContains groupedHtml "aria-label=\"Product navigation\"" "SideNav has its consumer label"
            Expect.stringContains groupedHtml "aria-label=\"Manage\"" "group hierarchy is semantic"
            Expect.stringContains groupedHtml "aria-current=\"page\"" "typed current destination is exposed"
            Expect.stringContains groupedHtml "aria-disabled=\"true\"" "unavailable navigation is semantic and non-interactive"
            Expect.stringContains groupedHtml "Meier Made" "optional context renders"
            Expect.stringContains groupedHtml "Andrew Meier" "optional footer renders"
            Expect.stringContains groupedHtml "<details open" "nested navigation groups render with native disclosure semantics"
            Expect.stringContains groupedHtml "aria-label=\"Reporting\"" "nested groups retain an accessible name"
            Expect.stringContains groupedHtml "aria-label=\"Pin accounts\"" "item actions keep an independent accessible name"
            Expect.stringContains groupedHtml "data-on:click__stop=\"true\"" "item actions do not activate their navigation destination"
            Expect.stringContains groupedHtml ">4</span>" "navigation items support consumer-authored badges"
            Expect.equal (Regex.Matches(groupedHtml, "aria-current=\"page\"").Count) 1 "SideNav exposes one current destination"

            let ungrouped =
                SideNav.create "compact-navigation" "Compact navigation"
                |> SideNav.withHeader (SideNavHeader.create "Treasury")
                |> SideNav.withContent (SideNavContent.create [
                    SideNavItem.create Home "Overview"
                    SideNavItem.create Reports "Transactions" ])
            let ungroupedHtml = ungrouped |> SideNav.render shellTestUrl |> Render.toString
            Expect.stringContains ungroupedHtml "aria-label=\"Compact navigation\"" "ungrouped navigation retains its name"
            Expect.isFalse (ungroupedHtml.Contains("uppercase tracking-wide")) "ungrouped navigation adds no invented group heading"
            Expect.isFalse (ungroupedHtml.Contains("aria-current=\"page\"")) "SideNav permits routes without a selected primary destination"

            let pageActions =
                div {
                    _class "flex flex-wrap gap-2"
                    Button.create (ButtonContent.Text "Refresh") |> Button.withAttributes [ _dataOn ("click", "$refreshes++") ] |> Button.render
                    a { _href (shellTestUrl Reports); "View reports" }
                    a { _href (shellTestUrl Settings); "Settings" }
                    DropdownMenu.create "account-page-overflow" "More actions"
                    |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "More actions")
                    |> DropdownMenu.withContent [ DropdownMenuItem.link Home "Home" ]
                    |> DropdownMenu.render shellTestUrl
                }
            let pageHeader =
                PageHeader.create "Account 2048"
                |> PageHeader.withSubtitle "Operating checking"
                |> PageHeader.withActions pageActions
            let pageHeaderHtml = pageHeader |> PageHeader.render |> Render.toString
            Expect.equal (Regex.Matches(pageHeaderHtml, "<h1").Count) 1 "PageHeader emits exactly one h1"
            Expect.stringContains pageHeaderHtml ">Account 2048</h1>" "the required title is visible"
            Expect.isFalse (pageHeaderHtml.Contains("<h1 class=\"sr-only\"")) "PageHeader does not hide the page title"
            Expect.stringContains pageHeaderHtml "Operating checking" "PageHeader renders its optional subtitle"
            Expect.stringContains pageHeaderHtml "data-on:click=\"$refreshes++\"" "page commands remain buttons with trusted Datastar actions"
            Expect.stringContains pageHeaderHtml "href=\"/reports\"" "page destinations remain truthful links"
            Expect.stringContains pageHeaderHtml "aria-label=\"More actions\"" "additional page actions use accessible overflow"

            let nativeActions =
                form { _method "get"; _action "/reports"; input { _name "range"; _value "monthly" }; Button.create (ButtonContent.Text "Apply") |> Button.asSubmit |> Button.render }
            for rendered in [
                PageHeader.create "Reports" |> PageHeader.withActions nativeActions |> PageHeader.render
                SectionHeader.create "Reports" |> SectionHeader.withActions nativeActions |> SectionHeader.render
                Collection.create "Reports" empty |> Collection.withActions nativeActions |> Collection.render
                Detail.create "Reports" [] |> Detail.withActions nativeActions |> Detail.render ] do
                Expect.stringContains (Render.toString rendered) (Render.toString nativeActions) "action slots preserve arbitrary consumer-owned form markup"

            let sectionHtml =
                SectionHeader.create "Activity"
                |> SectionHeader.withDescription "Recent account activity."
                |> SectionHeader.withActions (a { _href (shellTestUrl Reports); "View reports" })
                |> fun sectionHeader -> Section.create sectionHeader (p { "No activity." })
                |> Section.render
                |> Render.toString
            Expect.stringContains sectionHtml "<h2 class=" "SectionHeader defaults to an h2"
            Expect.stringContains sectionHtml "Recent account activity." "SectionHeader renders supporting text"
            Expect.isFalse (sectionHtml.Contains("<h1")) "detail sections do not compete with page identity"

            let collectionHtml =
                Collection.create "Accounts" (div { "Account rows" })
                |> Collection.withVisuallyHiddenTitle
                |> Collection.render
                |> Render.toString
            Expect.stringContains collectionHtml "<h2 class=\"sr-only\">Accounts</h2>" "Collection stays labelled beneath a page-owned visible title"
            Expect.isFalse (collectionHtml.Contains("rounded-[var(--fve-radius-panel)]")) "Collection does not invent a card surface"
            Expect.isFalse (collectionHtml.Contains("px-4")) "Collection inherits the page gutter without double padding"
            Expect.stringContains collectionHtml "<header class=\"sr-only\">" "Hidden collection titles do not leave a grid gap"

            let detailHtml =
                Detail.create "Operating checking" [ section { _ariaLabel "Account details"; "Account fields" } ]
                |> Detail.withVisuallyHiddenTitle
                |> Detail.render
                |> Render.toString
            Expect.stringContains detailHtml "<h2 class=\"sr-only\">Operating checking</h2>" "Detail stays labelled beneath a page-owned visible title"
            Expect.stringContains detailHtml "<section aria-label=\"Account details\">Account fields</section>" "Detail preserves consumer-owned semantic sections"
            Expect.isFalse (detailHtml.Contains("rounded-[var(--fve-radius-panel)]")) "Detail does not wrap every section in a card"
            Expect.isFalse (detailHtml.Contains("border-b")) "Detail uses section spacing instead of edge-to-edge bands"
            Expect.isFalse (detailHtml.Contains("px-4")) "Detail inherits the page gutter without double padding"

            let topBar =
                PageTopBar.create ()
                |> PageTopBar.withContent (div { _class "flex min-h-[var(--fve-shell-bar-min-height)] items-center"; Breadcrumbs.render shellTestUrl breadcrumbs })

            let pageTabs =
                Tabs.create "account-sections" "Account sections" [
                    TabItem.create "summary" "Summary" (p { "Summary panel" })
                    TabItem.create "activity" "Activity" (p { "Activity panel" }) ]
                |> Tabs.withVariant TabsVariant.Underlined
                |> Tabs.render
            let fullBleedPageHtml =
                Page.create pageHeader empty
                |> Page.withTopBar topBar
                |> Page.withTabs pageTabs
                |> Page.withWidth PageWidth.Full
                |> Page.withBodyLayout PageBodyLayout.FullBleed
                |> Page.render
                |> Render.toString
            Expect.stringContains fullBleedPageHtml "data-fve-page-top-bar=\"true\"" "Page owns the stable shell-aligned top bar"
            Expect.stringContains fullBleedPageHtml "<header data-fve-page-top-bar=\"true\"" "PageTopBar uses header semantics rather than claiming navigation"
            Expect.stringContains fullBleedPageHtml "min-h-[var(--fve-shell-bar-min-height)]" "PageTopBar consumes the shared shell-bar height"
            Expect.stringContains fullBleedPageHtml "data-fve-page-scroll=\"true\"" "Page owns its scroll region"
            Expect.isTrue (fullBleedPageHtml.IndexOf("data-fve-page-top-bar", StringComparison.Ordinal) < fullBleedPageHtml.IndexOf("data-fve-page-scroll", StringComparison.Ordinal)) "PageTopBar remains outside scrolling content"
            Expect.stringContains fullBleedPageHtml "max-w-none" "Page owns full content width"
            Expect.isFalse (fullBleedPageHtml.Contains("lg:p-8")) "full-bleed Page omits body padding"
            Expect.stringContains fullBleedPageHtml "role=\"tablist\"" "Page accepts package Tabs as local navigation"

            let defaultPageHtml =
                Page.create pageHeader empty
                |> Page.render
                |> Render.toString
            Expect.stringContains defaultPageHtml "max-w-7xl" "Page uses a constrained application column by default"
            Expect.isFalse (defaultPageHtml.Contains("data-fve-page-top-bar")) "an unconfigured Page does not reserve an empty chrome row"
            let labelledTopBarHtml = Page.create pageHeader empty |> Page.withTopBar (PageTopBar.create () |> PageTopBar.withAttributes [_ariaLabel "Workspace context"]) |> Page.render |> Render.toString
            Expect.stringContains labelledTopBarHtml "aria-label=\"Workspace context\"" "explicitly attributed empty top bars remain intentional"

            let readingPageHtml =
                Page.create pageHeader (p { "Reading content" })
                |> Page.withSectionNavigation (nav { _ariaLabel "Article sections"; a { _href "#summary"; "Summary" } })
                |> Page.withWidth PageWidth.Reading
                |> Page.render
                |> Render.toString
            Expect.stringContains readingPageHtml "max-w-4xl" "Page owns semantic reading width"
            Expect.stringContains readingPageHtml "p-4 sm:p-6 lg:p-8" "padded Page supplies responsive body spacing"
            Expect.stringContains readingPageHtml "aria-label=\"Article sections\"" "Page accepts route section navigation"

            let standaloneBottomNavigationHtml =
                BottomNavigation.create "standalone-quick-navigation" "Quick navigation" [
                    BottomNavigationItem.create Home "Home"
                    BottomNavigationItem.create Accounts "Accounts" ]
                |> BottomNavigation.withCurrent Accounts
                |> BottomNavigation.render shellTestUrl
                |> Render.toString
            Expect.stringContains standaloneBottomNavigationHtml "<nav id=\"standalone-quick-navigation\" aria-label=\"Quick navigation\"" "BottomNavigation is a labelled navigation landmark"
            Expect.stringContains standaloneBottomNavigationHtml "href=\"/accounts\" aria-current=\"page\"" "BottomNavigation marks the supplied current destination"
            Expect.isFalse (standaloneBottomNavigationHtml.Contains("role=\"tablist\"")) "BottomNavigation does not use tablist semantics"

            let shellHtml =
                AppShell.create "product-shell" grouped (div { "Route-owned page" })
                |> AppShell.withTheme ComponentsTheme.emerald
                |> AppShell.withCollapsibleNavigation
                |> AppShell.withMobileBottomNavigation "product-quick-navigation" "Quick navigation" [
                    BottomNavigationItem.create Home "Home"
                    BottomNavigationItem.create Accounts "Accounts"
                    BottomNavigationItem.create Settings "Settings" ]
                |> AppShell.render shellTestUrl
                |> Render.toString
            Expect.equal (Regex.Matches(shellHtml, "<main").Count) 1 "AppShell owns exactly one main landmark"
            Expect.equal (Regex.Matches(shellHtml, "aria-label=\"Product navigation\"").Count) 1 "desktop and mobile share one navigation tree"
            Expect.equal (Regex.Matches(shellHtml, "id=\"product-navigation\"").Count) 1 "responsive placement does not duplicate component IDs"
            Expect.isFalse (shellHtml.Contains("<h1")) "AppShell does not invent page identity"
            Expect.isFalse (shellHtml.Contains("max-w-7xl")) "AppShell does not own page width"
            Expect.stringContains shellHtml "aria-label=\"Open navigation\"" "mobile navigation has a named trigger"
            Expect.stringContains shellHtml "aria-label=\"Close navigation\"" "mobile navigation has a named close control"
            Expect.stringContains shellHtml "aria-label=\"Collapse navigation\"" "opt-in desktop icon rail has a named collapse control"
            Expect.stringContains shellHtml "Expand navigation" "the rail control exposes its restored expanded action"
            Expect.stringContains shellHtml "md:w-[var(--fve-side-nav-width)]" "the compact rail has an explicit desktop width contract"
            Expect.stringContains shellHtml "--fve-side-nav-width: 4rem" "the collapsed rail uses its compact width"
            Expect.stringContains shellHtml "data-attr:data-fve-collapsed" "the navigation exposes collapsed state to authored regions"
            Expect.stringContains shellHtml "evt.key == &#39;Escape&#39;" "mobile navigation handles Escape dismissal"
            Expect.stringContains shellHtml "evt.key == &#39;Tab&#39;" "mobile navigation contains keyboard focus"
            Expect.stringContains shellHtml "?.focus()" "mobile navigation restores or moves focus intentionally"
            Expect.stringContains shellHtml "data-attr:inert" "open mobile navigation removes the page from interaction"
            Expect.stringContains shellHtml "data-on:resize__window" "desktop resize clears stale mobile overlay state"
            Expect.stringContains shellHtml "id=\"product-quick-navigation\" aria-label=\"Quick navigation\"" "AppShell renders the configured mobile bottom navigation"
            Expect.stringContains shellHtml "href=\"/accounts\" aria-current=\"page\"" "mobile bottom navigation derives current state from SideNav"
            Expect.isFalse (shellHtml.Contains("role=\"tablist\"")) "mobile bottom navigation uses destinations rather than tabs"
            Expect.isFalse (shellHtml.Contains("role=\"tab\"")) "mobile bottom navigation does not invent tab semantics"
            Expect.stringContains shellHtml "fve-theme-emerald" "AppShell applies one consumer-controlled semantic theme"

            let embeddedShellHtml =
                AppShell.create "embedded-shell" grouped empty
                |> AppShell.withTheme ComponentsTheme.cyan
                |> AppShell.withBreakpoint AppShellBreakpoint.Large
                |> AppShell.withBoundary AppShellBoundary.Container
                |> AppShell.render shellTestUrl
                |> Render.toString
            Expect.stringContains embeddedShellHtml "h-full min-h-0" "container-bound shells stay within their embedding boundary"
            Expect.stringContains embeddedShellHtml "lg:hidden" "large-breakpoint shells retain mobile controls below large screens"
            Expect.stringContains embeddedShellHtml "lg:w-60" "large-breakpoint shells preserve typed navigation width"
            Expect.stringContains embeddedShellHtml "fve-theme-cyan" "additional semantic palettes are application-selectable"

            let previewShellHtml =
                AppShell.create "preview-shell" grouped empty
                |> AppShell.asPreview "Product preview"
                |> AppShell.render shellTestUrl
                |> Render.toString
            Expect.equal (Regex.Matches(previewShellHtml, "<main").Count) 0 "embedded AppShell previews do not nest main landmarks"
            Expect.stringContains previewShellHtml "<section aria-label=\"Product preview\"" "embedded AppShell previews retain a named landmark"
            Expect.throws (fun () -> AppShell.create "invalid-preview" grouped empty |> AppShell.asPreview " " |> ignore) "embedded AppShell previews require a landmark label"

            let adjacentShells =
                div {
                    AppShell.create "first-shell" grouped (div { "First" }) |> AppShell.render shellTestUrl
                    AppShell.create "second-shell" ungrouped (div { "Second" }) |> AppShell.render shellTestUrl
                }
                |> Render.toString
            let ids = Regex.Matches(adjacentShells, " id=\"([^\"]+)\"") |> Seq.cast<Match> |> Seq.map (fun matched -> matched.Groups[1].Value) |> Seq.toList
            Expect.equal ids.Length (ids |> List.distinct |> List.length) "adjacent shell instances retain independent IDs"
            Expect.stringContains adjacentShells "_app_shell_v66697273742d7368656c6c_navigation_open" "first shell signal is collision-safe"
            Expect.stringContains adjacentShells "_app_shell_v7365636f6e642d7368656c6c_navigation_open" "second shell signal is collision-safe"

            Expect.throws (fun () -> BreadcrumbItem.create Home " " |> ignore) "breadcrumb items require labels"
            Expect.throws (fun () -> Breadcrumbs.create " " "Breadcrumb" [ BreadcrumbItem.create Home "Home" ] |> ignore) "Breadcrumbs requires a stable ID"
            Expect.throws (fun () -> Breadcrumbs.create "crumbs" " " [ BreadcrumbItem.create Home "Home" ] |> ignore) "Breadcrumbs requires an accessible label"
            Expect.throws (fun () -> Breadcrumbs.create "crumbs" "Breadcrumb" [] |> ignore) "Breadcrumbs rejects an empty path"
            Expect.throws (fun () -> SideNavItem.create Home " " |> ignore) "side-navigation items require labels"
            Expect.throws (fun () -> SideNavSection.create "Empty" [] |> ignore) "SideNav rejects an empty section"
            Expect.throws (fun () -> SideNavGroup.create "Manage" [] |> ignore) "SideNav rejects an empty group"
            Expect.throws (fun () -> SideNavHeader.create " " |> ignore) "SideNavHeader requires an accessible identity"
            Expect.throws (fun () -> SideNav.create " " "Navigation" |> SideNav.withContent (SideNavContent.create [ SideNavItem.create Home "Home" ]) |> ignore) "SideNav requires a stable ID"
            Expect.throws (fun () -> SideNav.create "nav" " " |> SideNav.withContent (SideNavContent.create [ SideNavItem.create Home "Home" ]) |> ignore) "SideNav requires an accessible label"
            Expect.throws (fun () -> ungrouped |> SideNav.withCurrent Settings |> ignore) "SideNav rejects an unrepresented current destination"
            Expect.throws (fun () -> SideNav.create "nav" "Navigation" |> SideNav.withContent (SideNavContent.create [ SideNavItem.create Home "Home"; SideNavItem.create Home "Duplicate" ]) |> ignore) "SideNav rejects duplicate destinations"
            Expect.throws (fun () -> PageHeader.create " " |> ignore) "PageHeader requires a route title"
            Expect.throws (fun () -> PageHeader.create "Title" |> PageHeader.withSubtitle " " |> ignore) "PageHeader rejects an empty subtitle"
            Expect.throws (fun () -> SectionHeader.create "Activity" |> SectionHeader.withDescription " " |> ignore) "SectionHeader rejects empty supporting text"
            Expect.throws (fun () -> BottomNavigation.create " " "Quick navigation" [ BottomNavigationItem.create Home "Home" ] |> ignore) "BottomNavigation requires a stable ID"
            Expect.throws (fun () -> BottomNavigation.create "quick-navigation" " " [ BottomNavigationItem.create Home "Home" ] |> ignore) "BottomNavigation requires an accessible label"
            Expect.throws (fun () -> BottomNavigation.create "quick-navigation" "Quick navigation" [] |> ignore) "BottomNavigation rejects an empty item list"
            Expect.throws (fun () -> BottomNavigation.create "quick-navigation" "Quick navigation" [ BottomNavigationItem.create Home "Home"; BottomNavigationItem.create Home "Home again" ] |> ignore) "BottomNavigation rejects duplicate destinations"
            Expect.throws (fun () -> AppShell.create " " grouped empty |> ignore) "AppShell requires a stable ID"
            Expect.throws (fun () -> AppShell.create "same-id" (SideNav.create "same-id" "Navigation" |> SideNav.withContent (SideNavContent.create [ SideNavItem.create Home "Home" ])) empty |> ignore) "AppShell and SideNav IDs must differ"
            Expect.throws (fun () -> AppShell.create "product-shell" grouped empty |> AppShell.withMobileBottomNavigation "product-shell" "Quick navigation" [ BottomNavigationItem.create Home "Home" ] |> ignore) "AppShell requires distinct bottom-navigation IDs"
            Expect.throws (fun () -> AppShell.create "product-shell" grouped empty |> AppShell.withMobileBottomNavigation "quick-navigation" "Quick navigation" [ BottomNavigationItem.create Reports "Reports" ] |> ignore) "AppShell rejects mobile destinations missing from SideNav"
        }

        test "Tabs render typed variants, collision-safe relationships, and isolated automatic activation" {
            let first =
                Tabs.create "account-tabs" "Account sections" [
                    TabItem.create "overview" "Overview" (p { "Summary" })
                    TabItem.create "tax.reserve" "Tax reserve" (a { _href "/tax"; "Tax settings" }) ]
                |> Tabs.withSelected "tax.reserve"
                |> Tabs.withVariant TabsVariant.Underlined
                |> Tabs.render
                |> Render.toString

            Expect.stringContains first "id=\"account-tabs\"" "Tabs retain the consumer stable ID"
            Expect.stringContains first "role=\"tablist\" aria-label=\"Account sections\" aria-orientation=\"horizontal\"" "tab list has required accessible identity"
            Expect.equal (Regex.Matches(first, "role=\"tab\"").Count) 2 "one tab per item"
            Expect.equal (Regex.Matches(first, "role=\"tabpanel\"").Count) 2 "one panel per item"
            Expect.stringContains first "id=\"account-tabs-tab-v7461782e72657365727665\"" "UTF-8 hex identity preserves punctuation without collisions"
            Expect.stringContains first "aria-controls=\"account-tabs-panel-v7461782e72657365727665\"" "tab controls its stable panel"
            Expect.stringContains first "aria-labelledby=\"account-tabs-tab-v7461782e72657365727665\"" "panel is labelled by its tab"
            Expect.stringContains first "aria-selected=\"true\"" "selected tab is exposed"
            Expect.equal (Regex.Matches(first, "tabindex=\"0\"").Count) 3 "one selected tab and both panels remain keyboard reachable"
            Expect.stringContains first "hidden data-attr:hidden" "inactive panels leave interaction and accessibility trees"
            Expect.stringContains first "evt.key == &#39;ArrowLeft&#39;" "Left Arrow is handled"
            Expect.stringContains first "evt.key == &#39;ArrowRight&#39;" "Right Arrow is handled"
            Expect.stringContains first "evt.key == &#39;Home&#39;" "Home is handled"
            Expect.stringContains first "evt.key == &#39;End&#39;" "End is handled"
            Expect.stringContains first "aria-selected:border-[var(--fve-brand-solid)]" "Underlined variant uses semantic selected treatment"

            let adjacent =
                div {
                    Tabs.create "first-tabs" "First views" [ TabItem.create "same" "Same" (p { "First" }) ] |> Tabs.render
                    Tabs.create "second-tabs" "Second views" [ TabItem.create "same" "Same" (p { "Second" }) ] |> Tabs.render
                }
                |> Render.toString
            let ids = Regex.Matches(adjacent, " id=\"([^\"]+)\"") |> Seq.cast<Match> |> Seq.map (fun matched -> matched.Groups[1].Value) |> Seq.toList
            Expect.equal ids.Length (ids |> List.distinct |> List.length) "adjacent instances produce no duplicate IDs"
            Expect.stringContains adjacent "_tabs_v66697273742d74616273_selected" "first signal is collision-safe and local"
            Expect.stringContains adjacent "_tabs_v7365636f6e642d74616273_selected" "second signal is collision-safe and local"

            let duplicateItems = [ TabItem.create "same" "One" (p { "One" }); TabItem.create "same" "Two" (p { "Two" }) ]
            Expect.throws (fun () -> Tabs.create "tabs" "Views" [] |> ignore) "Tabs reject empty item sets"
            Expect.throws (fun () -> Tabs.create "tabs" "Views" duplicateItems |> ignore) "Tabs reject duplicate item IDs"
            Expect.throws (fun () -> Tabs.create "tabs" "Views" [ TabItem.create "one" "One" (p { "One" }) ] |> Tabs.withSelected "missing" |> ignore) "Tabs reject unknown selected items"
            Expect.throws (fun () -> TabItem.create " " "One" (p { "One" }) |> ignore) "Tab rejects whitespace IDs"
            Expect.throws (fun () -> Tabs.create "tabs" " " [ TabItem.create "one" "One" (p { "One" }) ] |> ignore) "Tabs require an accessible group label"
        }

        test "Dialog overlays preserve native modal semantics, safe confirmation state, and responsive drawer identity" {
            let dialogConfig =
                Dialog.create "test-dialog" "Review account" (p { "Review the settings." })
                |> Dialog.withDescription "Settings remain unchanged until saved."
                |> Dialog.withInitialFocus "test-dialog-close"
                |> Dialog.dismissOnBackdrop
                |> Dialog.withAttributes [
                    _role "alertdialog"
                    _dataSignals "{saving: false}"
                    _dataOn ("cancel", "$saving && evt.preventDefault()")
                    _id "override"
                    _class "override"
                    _ariaLabelledby "override"
                    _ariaDescribedby "override"
                    _ariaModal false
                    _dataOn ("close", "override")
                    _dataOn ("click", "override") ]
            let dialogHtml =
                div { dialogConfig |> Dialog.trigger "Review account"; dialogConfig |> Dialog.withFooter (dialogConfig |> Dialog.closeButton "Close") |> Dialog.render }
                |> Render.toString
            Expect.stringContains dialogHtml "<dialog id=\"test-dialog\"" "Dialog remains a native dialog"
            Expect.stringContains dialogHtml "aria-modal=\"true\"" "Dialog exposes modal semantics"
            Expect.stringContains dialogHtml "evt.target == evt.currentTarget" "Dialog can opt into backdrop dismissal"
            Expect.stringContains dialogHtml "document.getElementById(&quot;test-dialog-trigger&quot;)?.focus()" "Dialog restores its connected trigger"

            Expect.stringContains dialogHtml "role=\"alertdialog\"" "consumers can choose alert-dialog semantics"
            Expect.stringContains dialogHtml "data-signals=\"{saving: false}\"" "consumers own transient state"
            Expect.stringContains dialogHtml "data-on:cancel=\"$saving &amp;&amp; evt.preventDefault()\"" "consumers can protect in-flight work"
            Expect.isFalse (dialogHtml.Contains "override") "attributes cannot replace dialog identity, labelling, presentation, or dismissal handlers"

            let confirmationHtml = Components.accountDeletionDialog |> Dialog.render |> Render.toString
            Expect.stringContains confirmationHtml "role=\"alertdialog\"" "the confirmation example composes alert-dialog semantics"
            Expect.stringContains confirmationHtml "aria-describedby=\"delete-account-confirmation-description\"" "the confirmation message remains described"
            Expect.stringContains confirmationHtml "data-indicator:_delete_account_confirmation_pending" "the native form request drives pending state"
            Expect.stringContains confirmationHtml "data-attr:disabled=\"$_delete_account_confirmation_pending\"" "request pending prevents repeated activation"
            Expect.stringContains confirmationHtml "id=\"delete-account-confirmation-cancel\"" "the least-destructive action is consumer-authored"
            Expect.stringContains confirmationHtml "var(--fve-error-solid," "the submit Button retains the Error palette"

            let pendingHtml = Components.pendingDeletionDialog |> Dialog.render |> Render.toString
            Expect.stringContains pendingHtml "aria-busy=\"true\"" "server-rendered pending state is perceivable"
            Expect.stringContains pendingHtml "Confirmation in progress." "pending state retains explanatory text"
            Expect.stringContains pendingHtml "disabled" "pending confirmation cannot submit twice"

            let validationHtml = Components.accountDeletionContent (Some "The value is still referenced.") |> Render.toString
            Expect.stringContains validationHtml "role=\"alert\"" "server validation is announced"
            Expect.stringContains validationHtml "The value is still referenced." "server validation remains visible"

            let drawerBody = nav { _ariaLabel "Account settings"; a { _href "/accounts"; "Accounts" } }
            let endDrawer = Drawer.create "settings-drawer" "Settings" drawerBody |> Drawer.withAttributes [_id "override"; _class "override"; _ariaModal false; _dataOn("close","override"); _dataOn("cancel","evt.preventDefault()")]
            let endDrawerHtml =
                div { endDrawer |> Drawer.trigger "Open settings"; endDrawer |> Drawer.render }
                |> Render.toString
            Expect.stringContains endDrawerHtml "<dialog id=\"settings-drawer\"" "Drawer remains a native dialog"
            Expect.isFalse (endDrawerHtml.Contains "override") "consumer lifecycle attributes cannot replace structural identity or dismissal"
            Expect.stringContains endDrawerHtml "data-on:cancel=\"evt.preventDefault()\"" "consumer lifecycle guards are retained"
            Expect.stringContains endDrawerHtml "inset-y-0 right-0 ml-auto mr-0 h-dvh border-l" "Drawer defaults to the end edge"
            Expect.stringContains endDrawerHtml "w-full max-w-full sm:w-96" "Drawer fills mobile widths and bounds its desktop surface"
            Expect.stringContains endDrawerHtml "aria-label=\"Account settings\"" "consumer landmarks are preserved"
            Expect.stringContains endDrawerHtml "id=\"settings-drawer-close\"" "Drawer owns a stable close target"
            Expect.stringContains endDrawerHtml "evt.target == evt.currentTarget" "Drawer backdrop dismisses explicitly"
            let startDrawerHtml = endDrawer |> Drawer.withSide DrawerSide.Start |> Drawer.render |> Render.toString
            Expect.stringContains startDrawerHtml "inset-y-0 left-0 ml-0 mr-auto h-dvh border-r" "Drawer supports the typed start edge"
            let wideDrawerHtml = endDrawer |> Drawer.withSize DrawerSize.Large |> Drawer.render |> Render.toString
            Expect.stringContains wideDrawerHtml "w-full max-w-full sm:w-[42rem]" "Drawer supports a typed large editing surface"
            let topDrawerHtml = endDrawer |> Drawer.withSide DrawerSide.Top |> Drawer.render |> Render.toString
            Expect.stringContains topDrawerHtml "inset-x-0 top-0" "Drawer supports the typed top edge"
            let bottomDrawerHtml = endDrawer |> Drawer.withSide DrawerSide.Bottom |> Drawer.withSize DrawerSize.Large |> Drawer.render |> Render.toString
            Expect.stringContains bottomDrawerHtml "inset-x-0 bottom-0" "Drawer supports the typed bottom edge"
            Expect.stringContains bottomDrawerHtml "h-[min(36rem,calc(100%-3rem))]" "horizontal drawers use typed heights"
        }

        test "Components foundations preserve accessible names, honest states, and protected structure" {
            let icon = span { "+" }

            let pendingButton =
                Button.create (ButtonContent.Text "Sync accounts")
                |> Button.pending
                |> Button.withAttributes [ _ariaBusy false; _attr "disabled"; _class "override" ]
                |> Button.render
                |> Render.toString
            Expect.stringContains pendingButton "disabled" "pending Button prevents activation"
            Expect.stringContains pendingButton "aria-busy=\"true\"" "pending Button exposes busy state"
            Expect.stringContains pendingButton ">Sync accounts<" "pending Button retains its action label"
            Expect.equal (Regex.Matches(pendingButton, " aria-busy=\"").Count) 1 "Button owns one busy state"
            Expect.isFalse (pendingButton.Contains("override")) "Button protects base presentation"

            let iconButton =
                Button.create (ButtonContent.Icon ("Add account", icon))
                |> Button.withColor ButtonColor.Primary
                |> Button.withVariant ButtonVariant.Solid
                |> Button.render
                |> Render.toString
            Expect.stringContains iconButton "aria-label=\"Add account\"" "icon-only Button requires an accessible name"
            Expect.stringContains iconButton "aria-hidden=\"true\"" "icon-only Button hides decorative icon content"
            Expect.stringContains iconButton "size-[var(--fve-control-min-height)]" "icon-only Button uses square control sizing"
            Expect.throws (fun () -> Button.create (ButtonContent.Text " ") |> ignore) "text Button rejects empty text"
            Expect.throws (fun () -> Button.create (ButtonContent.Icon (" ", icon)) |> ignore) "icon-only Button rejects an empty accessible name"
            Expect.throws (fun () -> Button.create (ButtonContent.IconText (icon, " ")) |> ignore) "IconText Button rejects empty text"
            Expect.throws (fun () -> Button.create (ButtonContent.TextIcon (" ", icon)) |> ignore) "TextIcon Button rejects empty text"

            let linkConfig = Button.create (ButtonContent.Text "Edit") |> Button.withAttributes [_attr("href","/wrong"); _class "override"]
            let link = linkConfig |> Button.renderLink "/accounts/101/edit?from=accounts&sort=name" |> Render.toString
            Expect.stringContains link "<a href=\"/accounts/101/edit?from=accounts&amp;sort=name\"" "styled links keep an encoded native destination"
            Expect.isFalse (link.Contains "<button" || link.Contains "type=\"button\"" || link.Contains "override" || link.Contains "/wrong") "links retain anchor semantics and protect presentation/destination"
            for modifier in [Button.asSubmit; Button.disabled; Button.pending] do
                Expect.throws (fun () -> linkConfig |> modifier |> Button.renderLink "/accounts" |> ignore) "native links cannot claim button-only states"

            let pendingIconButton =
                Button.create (ButtonContent.Icon ("Refresh accounts", icon))
                |> Button.pending
                |> Button.withAttributes [ _ariaLabel "Override"; _ariaBusy false; _class "override" ]
                |> Button.render
                |> Render.toString
            Expect.stringContains pendingIconButton "aria-label=\"Refresh accounts\"" "pending icon-only Button retains its accessible name"
            Expect.stringContains pendingIconButton "aria-busy=\"true\"" "pending icon-only Button exposes busy state"
            Expect.stringContains pendingIconButton "disabled" "pending icon-only Button prevents activation"
            Expect.isFalse (pendingIconButton.Contains("Override")) "Button protects its accessible name"
            Expect.isFalse (pendingIconButton.Contains("override")) "Button protects base presentation"

            let iconTextButton =
                Button.create (ButtonContent.IconText (span { "Before" }, "Continue"))
                |> Button.render
                |> Render.toString
            Expect.isLessThan (iconTextButton.IndexOf("Before")) (iconTextButton.IndexOf("Continue")) "IconText renders its decorative icon before the text"
            Expect.equal (Regex.Matches(iconTextButton, "aria-hidden=\"true\"").Count) 1 "IconText hides its icon from the accessible name"

            let textIconButton =
                Button.create (ButtonContent.TextIcon ("Continue", span { "After" }))
                |> Button.render
                |> Render.toString
            Expect.isLessThan (textIconButton.IndexOf("Continue")) (textIconButton.IndexOf("After")) "TextIcon renders its decorative icon after the text"
            Expect.equal (Regex.Matches(textIconButton, "aria-hidden=\"true\"").Count) 1 "TextIcon hides its icon from the accessible name"

            let pendingIconTextButton =
                Button.create (ButtonContent.IconText (span { "Before" }, "Continue"))
                |> Button.pending
                |> Button.render
                |> Render.toString
            Expect.stringContains pendingIconTextButton ">Continue<" "pending IconText retains its action label"
            Expect.isFalse (pendingIconTextButton.Contains("Before")) "pending IconText replaces its decorative icon with the spinner"

            let customButton =
                Button.create (ButtonContent.Custom (fragment { strong { "Custom" }; span { " content" } }))
                |> Button.pending
                |> Button.render
                |> Render.toString
            Expect.stringContains customButton "<strong>Custom</strong><span> content</span>" "Custom content remains consumer-authored and visible while pending"
            Expect.stringContains customButton "aria-busy=\"true\"" "Custom content retains standard pending semantics"

            for variant, activeClass in [
                ButtonVariant.Solid, "enabled:active:bg-[var(--fve-color-active)]"
                ButtonVariant.Soft, "enabled:active:bg-[var(--fve-color-soft-active)]"
                ButtonVariant.Outline, "enabled:active:bg-[var(--fve-color-soft-active)]"
                ButtonVariant.Ghost, "enabled:active:bg-[var(--fve-color-soft-active)]"
            ] do
                let button =
                    Button.create (ButtonContent.Text "Action")
                    |> Button.withVariant variant
                    |> Button.render
                    |> Render.toString
                let iconAction =
                    Button.create (ButtonContent.Icon ("Icon action", icon))
                    |> Button.withVariant variant
                    |> Button.render
                    |> Render.toString
                Expect.stringContains button activeClass $"{variant} text Button has intentional active styling"
                Expect.stringContains iconAction activeClass $"{variant} icon-only Button has intentional active styling"

            let subtleOutline = Button.create (ButtonContent.Text "Outline") |> Button.render |> Render.toString
            Expect.stringContains subtleOutline "color-mix(in_srgb,var(--fve-color-border)_20%,transparent)" "Button outline uses a subtle palette border"

            let badge =
                Badge.create "Reconciled"
                |> Badge.withColor BadgeColor.Success
                |> Badge.withAttributes [ _class "override" ]
                |> Badge.render
                |> Render.toString
            Expect.stringContains badge "Reconciled" "Badge communicates category through text"
            Expect.stringContains badge "var(--fve-success-solid," "Badge consumes its component-owned Success color"
            Expect.isFalse (badge.Contains("override")) "Badge protects base presentation"

            let subtleBadgeOutline =
                Badge.create "Outline"
                |> Badge.withColor BadgeColor.Success
                |> Badge.withVariant BadgeVariant.Outline
                |> Badge.render
                |> Render.toString
            Expect.stringContains subtleBadgeOutline "color-mix(in_srgb,var(--fve-color-border)_20%,transparent)" "Badge and Button outlines share the subtle palette border"

            let loading =
                LoadingIndicator.create "Loading balances"
                |> LoadingIndicator.withAttributes [ _role "alert"; _ariaLive "assertive"; _class "override" ]
                |> LoadingIndicator.render
                |> Render.toString
            Expect.stringContains loading "role=\"status\"" "LoadingIndicator exposes polite status semantics"
            Expect.stringContains loading "aria-live=\"polite\"" "LoadingIndicator owns its announcement behavior"
            Expect.stringContains loading "Loading balances" "LoadingIndicator retains its accessible label"
            Expect.stringContains loading "class=\"sr-only\"" "compact loading label is visually hidden"
            Expect.isFalse (loading.Contains("alert")) "LoadingIndicator protects its role"
            Expect.isFalse (loading.Contains("override")) "LoadingIndicator protects base presentation"

            let visibleLoading =
                LoadingIndicator.create "Refreshing entries"
                |> LoadingIndicator.withVisibleLabel
                |> LoadingIndicator.render
                |> Render.toString
            Expect.isFalse (visibleLoading.Contains("sr-only")) "visible loading label remains visible"

            let emptyState =
                EmptyState.create "No accounts" "Create an account to begin."
                |> EmptyState.withIcon icon
                |> EmptyState.withActions (Button.create (ButtonContent.Text "Create account") |> Button.withColor ButtonColor.Primary |> Button.withVariant ButtonVariant.Solid |> Button.render)
                |> EmptyState.withAttributes [ _class "override" ]
                |> EmptyState.render
                |> Render.toString
            Expect.stringContains emptyState "No accounts" "EmptyState renders its title"
            Expect.stringContains emptyState "Create an account to begin." "EmptyState renders useful guidance"
            Expect.stringContains emptyState "aria-hidden=\"true\"" "EmptyState icon is decorative"
            Expect.stringContains emptyState "Create account" "EmptyState composes application-owned actions"
            Expect.isFalse (emptyState.Contains("override")) "EmptyState protects base presentation"
        }

        test "Action colors are independent of variants and preserve custom mode pairs and consumer styles" {
            let render config = config |> Button.render |> Render.toString
            let outline = Button.create (ButtonContent.Text "Save") |> Button.withColor ButtonColor.Primary |> render
            let error = Button.create (ButtonContent.Text "Save") |> Button.withColor ButtonColor.Error |> render
            let classes html = Regex.Match(html, "class=\"([^\"]+)\"").Groups[1].Value
            Expect.equal (classes outline) (classes error) "changing color does not change appearance"
            Expect.stringContains outline "var(--fve-primary-solid," "Primary has its own override contract"
            Expect.stringContains error "var(--fve-error-solid," "Error has its own override contract"
            let custom =
                Button.create (ButtonContent.Text "Save <draft>")
                |> Button.withColor (ButtonColor.Custom Components.customButtonPalette)
                |> Button.withVariant ButtonVariant.Soft
                |> Button.withAttributes [ _style "margin:1px" ]
                |> render
            Expect.stringContains custom "--fve-color-solid:light-dark(#9a3412,#fdba74)" "custom palettes keep explicit mode-specific values"
            Expect.stringContains custom "margin:1px;" "ordinary consumer styles survive palette composition"
            Expect.equal (Regex.Matches(custom, " style=\"").Count) 1 "the renderer emits one style attribute"
            Expect.stringContains custom "Save &lt;draft&gt;" "presentation does not bypass label encoding"
            let pending = Button.create (ButtonContent.Text "Delete") |> Button.withColor ButtonColor.Error |> Button.pending |> render
            Expect.stringContains pending "border-current" "pending glyph follows its owning action foreground"
            Expect.isFalse (pending.Contains("border-t-[var(--fve-brand-solid)]")) "pending glyph does not assume brand color"
        }

        test "Table records, selection, canvas, and optional section headers preserve their contracts" {
            let columns = [ TableColumn.create "Name" text |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary ]
            let selection =
                TableSelection.create "selection" id id
                |> TableSelection.withSelectedKeys [ "one"; "off-page" ]
                |> TableSelection.withFormName "keys"
            let render rows =
                Table.create "Records" columns rows
                |> Table.withMobileLayout TableMobileLayout.Records
                |> Table.withSelection selection
                |> Table.render
                |> Render.toString
            let html = render [ "one"; "two"; "disabled" ]
            Expect.equal (Regex.Matches(html, "<table ").Count) 1 "one tree for both layouts"
            Expect.stringContains html "fve-table-records" "record layout is opt-in"
            Expect.stringContains html "data-mobile-cell=\"primary\"" "primary column is explicit"
            Expect.stringContains html "name=\"keys\" value=\"one\" checked" "rendered initial keys submit real values"
            Expect.stringContains html "el.indeterminate" "select-all supports mixed state"
            Expect.stringContains html "fve-table-selection-change" "applications receive selected key changes"
            Expect.stringContains html "data-signals__ifmissing" "morphs preserve local selection"
            let sortedHtml =
                Table.create "Sortable records" [
                    TableColumn.create "Name" text
                    |> TableColumn.asRowHeader
                    |> TableColumn.asMobilePrimary
                    |> TableColumn.withSort (TableSort.ascending "/records?sort=name&direction=desc")
                    TableColumn.create "Role" text
                    |> TableColumn.withSort (TableSort.by "/records?sort=role&direction=asc")
                ] [ "Alex" ]
                |> Table.withMobileLayout TableMobileLayout.Records
                |> Table.render
                |> Render.toString
            Expect.stringContains sortedHtml "aria-sort=\"ascending\"" "only the current column carries aria-sort"
            Expect.stringContains sortedHtml "href=\"/records?sort=name&amp;direction=desc\"" "the consumer-provided next destination is preserved"
            Expect.stringContains sortedHtml "fve-table-mobile-sort" "record layouts include sort controls outside visually hidden headers"
            Expect.stringContains html "1 selected" "off-page initial keys are excluded"
            Expect.stringContains html "[&quot;one&quot;,&quot;two&quot;,&quot;disabled&quot;]" "select all includes every displayed row"
            Expect.throws (fun () -> render [ "one"; "one" ] |> ignore) "duplicate keys are rejected"
            Expect.throws (fun () -> render [ "" ] |> ignore) "empty keys are rejected"
            Expect.throws (fun () -> Table.create "Invalid" [ TableColumn.create "Name" text ] [ "one" ] |> Table.withMobileLayout TableMobileLayout.Records |> Table.render |> ignore) "records require a primary column"
            let empty = render []
            Expect.stringContains empty "No records" "empty data retains the empty state"
            Expect.stringContains empty "0 selected" "empty data clears selection"
            let sectionHtml = Section.withoutHeader "Details" (p { "Content" }) |> Section.render |> Render.toString
            Expect.stringContains sectionHtml "aria-label=\"Details\"" "unheaded sections retain accessible identity"
            Expect.isFalse (sectionHtml.Contains("<header")) "section header is optional"
            let headerHtml = SectionHeader.create "Details" |> SectionHeader.render |> Render.toString
            Expect.isFalse (headerHtml.Contains("border-b")) "section dividers are opt-in"
            let transactionsSection = Section.create (SectionHeader.create "Transactions") (p { "Rows" })
            let labelledSection = transactionsSection |> Section.withLabel "Recent transactions" |> Section.render |> Render.toString
            Expect.stringContains labelledSection "aria-label=\"Recent transactions\"" "sections can distinguish nested landmarks"
            Expect.stringContains labelledSection ">Transactions</h2>" "accessible labels do not replace visible section headings"
            Expect.throws (fun () -> transactionsSection |> Section.withLabel " " |> ignore) "section labels cannot be blank"
            let canvas = Page.create (PageHeader.create "Graph") (div { "Canvas" }) |> Page.withBodyLayout PageBodyLayout.Canvas |> Page.render |> Render.toString
            Expect.stringContains canvas "data-fve-page-canvas=\"true\"" "canvas fills available height"
            Expect.isFalse (canvas.Contains("data-fve-page-scroll")) "canvas owns scrolling instead of nesting document scroll"
            let checkbox = Checkbox.create "mixed" "Select all" |> Checkbox.withIndeterminate |> Checkbox.withVisuallyHiddenLabel |> Checkbox.render |> Render.toString
            Expect.stringContains checkbox "mixed_mixed: true" "standalone checkboxes support mixed state"
            Expect.stringContains checkbox "class=\"sr-only\">Select all" "hidden labels remain accessible"
        }

        test "Components data display preserves native semantics and consumer ownership" {
            let defaultTableHtml =
                Table.create "Accounts" [ TableColumn.create "Account" text |> TableColumn.asRowHeader ] [ "Assets" ]
                |> Table.render
                |> Render.toString
            Expect.stringContains defaultTableHtml "<caption class=\"sr-only\">Accounts</caption>" "Table captions are visually hidden by default"
            Expect.stringContains defaultTableHtml "data-density=\"compact\"" "Table uses compact application density by default"
            Expect.isFalse (defaultTableHtml.Contains("rounded-[var(--fve-radius-panel)]")) "Table uses a full-width plain surface by default"
            Expect.stringContains defaultTableHtml "data-surface=\"plain\"" "Plain table backgrounds follow the page"
            Expect.stringContains defaultTableHtml "bg-[var(--fve-table-background)]" "Overflow, rows, and sticky cells share the table background token"
            let panelTableHtml =
                Table.create "Panel balances" [ TableColumn.create "Account" text ] [ "Operating" ]
                |> Table.withSurface TableSurface.Panel
                |> Table.render
                |> Render.toString
            Expect.stringContains panelTableHtml "data-surface=\"panel\"" "Panel surfaces remain an explicit opt-in"

            let tableHtml =
                Table.create "Account balances" [
                    TableColumn.create "Account" text |> TableColumn.asRowHeader
                    TableColumn.create "Balance" (fun value -> text value) |> TableColumn.alignEnd
                    TableColumn.rowActions (fun value ->
                        DropdownMenu.create "operating-actions" $"More actions for {value}"
                        |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon (span { "More" }))
                        |> DropdownMenu.withContent [ DropdownMenuItem.link Accounts "View account" ]
                        |> DropdownMenu.render shellTestUrl)
                ] [ "Operating" ]
                |> Table.withVisibleCaption
                |> Table.withDensity Density.Compact
                |> Table.withSurface TableSurface.Plain
                |> Table.render
                |> Render.toString
            Expect.stringContains tableHtml "role=\"region\" aria-label=\"Account balances\" tabindex=\"0\"" "Table exposes a labelled keyboard-reachable overflow region"
            Expect.stringContains tableHtml "<caption class=\"px-3 py-2 text-left text-sm font-semibold" "Table can show its native caption"
            Expect.stringContains tableHtml "<th scope=\"row\"" "Table identifies consumer-selected row headers"
            Expect.stringContains tableHtml "<span class=\"sr-only\">Actions</span>" "row actions retain a visually hidden column heading"
            Expect.stringContains tableHtml "sticky right-0" "row actions remain reachable while wide tables scroll"
            Expect.stringContains tableHtml "data-fve-sticky-cell=\"true\"" "row-action cells identify the sticky popup boundary"
            Expect.stringContains tableHtml "aria-label=\"More actions for Operating\"" "row action triggers identify their record"
            Expect.stringContains tableHtml "size-[var(--fve-control-min-height)]" "row actions use the compact overflow trigger"
            Expect.isFalse (tableHtml.Contains("rounded-[var(--fve-radius-panel)] ring-1")) "plain tables avoid an invented panel"
            Expect.stringContains tableHtml "fve-table-cell" "Table uses shared configurable cell spacing"
            Expect.throws (fun () -> Table.create " " [ TableColumn.create "Value" text ] [ "one" ] |> ignore) "Table requires a caption"

            let recordMenuHtml =
                DropdownMenu.create "record-actions" "More actions for Operating"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon (span { "More" }))
                |> DropdownMenu.withContent [
                    DropdownMenuItem.link Accounts "View account"
                    DropdownMenuItem.separator
                    DropdownMenuItem.link Settings "Delete account" |> DropdownMenuItem.withColor DropdownMenuItemColor.Error ]
                |> DropdownMenu.render shellTestUrl
                |> Render.toString
            let viewIndex = recordMenuHtml.IndexOf("View account", StringComparison.Ordinal)
            let separatorIndex = recordMenuHtml.IndexOf("role=\"separator\"", StringComparison.Ordinal)
            let deleteIndex = recordMenuHtml.IndexOf("Delete account", StringComparison.Ordinal)
            Expect.isTrue (viewIndex < separatorIndex && separatorIndex < deleteIndex) "typed menu content preserves explicit destructive grouping"
            Expect.stringContains recordMenuHtml "var(--fve-error-solid," "Error record-menu links retain their component-owned color"

            let detailsHtml =
                DescriptionList.create [
                    DescriptionListItem.text "Type" "Asset"
                    DescriptionListItem.status "State" (Badge.create "Active" |> Badge.withColor BadgeColor.Success |> Badge.render)
                    |> DescriptionListItem.withDescription "Available for posting."
                    |> DescriptionListItem.withAttributes [ _role "button"; _class "override" ]
                ]
                |> DescriptionList.withColumns DescriptionListColumns.Three
                |> DescriptionList.withAttributes [ _role "table"; _class "override" ]
                |> DescriptionList.render
                |> Render.toString
            Expect.stringContains detailsHtml "<dl class=\"grid gap-x-6 gap-y-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3\"" "DescriptionList retains responsive native list semantics"
            Expect.stringContains detailsHtml "<dt" "DescriptionListItem renders a term"
            Expect.stringContains detailsHtml "<dd" "DescriptionListItem renders its value and description"
            Expect.stringContains detailsHtml "Available for posting." "DescriptionListItem preserves supporting context"
            Expect.isFalse (detailsHtml.Contains("override")) "description-list structure protects base classes"
            Expect.isFalse (detailsHtml.Contains("role=")) "description-list structure rejects role replacement"
            Expect.throws (fun () -> DescriptionListItem.text " " "Value" |> ignore) "DescriptionListItem requires a label"
            Expect.throws (fun () -> DescriptionList.create [] |> ignore) "DescriptionList requires fields"
            Expect.stringContains detailsHtml "text-xs font-medium uppercase tracking-wide" "detail labels use the ancillary uppercase role"
            Expect.stringContains detailsHtml "mt-1 break-words text-sm font-normal" "detail values use normal-weight UI text"
            for columns, expected in [
                DescriptionListColumns.One, "grid-cols-1"
                DescriptionListColumns.Two, "grid-cols-1 sm:grid-cols-2"
                DescriptionListColumns.Three, "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
                DescriptionListColumns.Four, "grid-cols-1 sm:grid-cols-2 xl:grid-cols-4" ] do
                let rendered =
                    DescriptionList.create [ DescriptionListItem.text "<Label>" "<Value>" ]
                    |> DescriptionList.withColumns columns
                    |> DescriptionList.render
                    |> Render.toString
                Expect.stringContains rendered $"grid gap-x-6 gap-y-4 {expected}\"" "column choices remain explicit and responsive"
                Expect.stringContains rendered "&lt;Label&gt;" "labels remain encoded"
                Expect.stringContains rendered "&lt;Value&gt;" "values remain encoded"
                Expect.isFalse (rendered.Contains("<h")) "section headings belong to the surrounding composition"

            let metricHtml =
                Metric.create "Available balance" (strong { "$42,800" })
                |> Metric.withTrend "Up 8%"
                |> Metric.withDescription "Operating and reserve accounts"
                |> Metric.withStatus (Badge.create "Current" |> Badge.render)
                |> Metric.withAttributes [ _class "override" ]
                |> Metric.render
                |> Render.toString
            Expect.stringContains metricHtml "Available balance" "Metric exposes its label"
            Expect.stringContains metricHtml "<strong>$42,800</strong>" "Metric preserves custom value content"
            Expect.stringContains metricHtml "<span class=\"sr-only\">Trend: </span>Up 8%" "Metric gives trend text semantic context"
            Expect.stringContains metricHtml "Current" "Metric composes consumer-owned status content"
            Expect.stringContains metricHtml "flex flex-wrap items-center gap-2" "Metric keeps status content adjacent to its label"
            Expect.isFalse (metricHtml.Contains("justify-between")) "Metric does not distribute status toward an adjacent metric"
            Expect.isFalse (metricHtml.Contains("override")) "Metric protects base presentation"
            Expect.throws (fun () -> Metric.text " " "1" |> ignore) "Metric requires a label"

            let resolve destination = $"/accounts?page={destination}"
            let paginationHtml =
                Pagination.create "Account pages" [
                    PaginationItem.link 1 1
                    PaginationItem.current 2
                    PaginationItem.gap
                    PaginationItem.link 8 8
                ]
                |> Pagination.withNext 3
                |> Pagination.withSummary (span { "Showing 26–50" })
                |> Pagination.withAttributes [ _role "menu"; _ariaLabel "Override"; _class "override" ]
                |> Pagination.render resolve
                |> Render.toString
            Expect.stringContains paginationHtml "<nav aria-label=\"Account pages\"" "Pagination requires a labelled navigation landmark"
            Expect.stringContains paginationHtml "aria-current=\"page\" aria-label=\"Page 2, current page\"" "Pagination exposes one current page"
            Expect.stringContains paginationHtml "aria-disabled=\"true\"" "Pagination presents an unavailable previous edge"
            Expect.stringContains paginationHtml "href=\"/accounts?page=3\"" "Pagination resolves consumer-owned destinations"
            Expect.stringContains paginationHtml "Showing 26–50" "Pagination preserves consumer summary content"
            Expect.isFalse (paginationHtml.Contains("Override")) "Pagination protects its accessible label"
            Expect.isFalse (paginationHtml.Contains("override")) "Pagination protects base presentation"
            Expect.isFalse (paginationHtml.Contains("role=\"menu\"")) "Pagination protects its navigation role"
            Expect.throws (fun () -> Pagination.create "Pages" [ PaginationItem.link 1 1 ] |> ignore) "Pagination requires one current page"
            Expect.throws (fun () -> Pagination.create "Pages" [ PaginationItem.current 1; PaginationItem.current 2 ] |> ignore) "Pagination rejects multiple current pages"

            let paginationPage3Html =
                Components.paginationPageFor 3
                |> View.documentWithPage Registry.navigation Components.paginationRegistration
                |> Render.toHtmlDocString
            Expect.stringContains paginationPage3Html "Showing 51–75 of 184 accounts" "Docs pagination derives its summary from local query state"
            Expect.stringContains paginationPage3Html "aria-current=\"page\" aria-label=\"Page 3, current page\"" "Docs pagination renders the requested current page"
            Expect.stringContains paginationPage3Html "href=\"/components/pagination/page?page=4\"" "Docs pagination links to a targeted local next page"

            let paginationPage8Html =
                Components.paginationPageFor 99
                |> View.documentWithPage Registry.navigation Components.paginationRegistration
                |> Render.toHtmlDocString
            Expect.stringContains paginationPage8Html "Showing 176–184 of 184 accounts" "Docs pagination clamps out-of-range requests"
            Expect.stringContains paginationPage8Html "aria-current=\"page\" aria-label=\"Page 8, current page\"" "Docs pagination clamps to the last page"

            for removedType in [ "FSharp.ViewEngine.Components.Chart"; "FSharp.ViewEngine.Components.ChartConfig"; "FSharp.ViewEngine.Components.Chart"; "FSharp.ViewEngine.Components.ChartConfig"; "FSharp.ViewEngine.Components.RowActions"; "FSharp.ViewEngine.Components.RowActionsConfig" ] do
                Expect.isNull (typeof<TableSurface>.Assembly.GetType removedType) "the public chart wrapper API is removed"

        }

        test "DropdownMenu supports icon triggers and reactive menu choices" {
            let items : DropdownMenuItem<unit> list =
                [ DropdownMenuItem.radio "$theme = 'system'" "System"
                  |> DropdownMenuItem.withChecked true
                  |> DropdownMenuItem.withCheckedExpression "$theme == 'system'"
                  DropdownMenuItem.radio "$theme = 'dark'" "Dark" |> DropdownMenuItem.disabled
                  DropdownMenuItem.radio "$theme = 'light'" "Light" |> DropdownMenuItem.pending
                  DropdownMenuItem.checkbox "$totals = !$totals" "Totals"
                  |> DropdownMenuItem.withChecked true
                  |> DropdownMenuItem.withCheckedExpression "$totals" ]
            let rendered =
                DropdownMenu.create "theme" "Choose theme"
                |> DropdownMenu.withTrigger (
                    DropdownMenuTrigger.icon (span { "Icon" })
                    |> DropdownMenuTrigger.withAttributes [ _attr ("data-testid", "theme-trigger"); _attr ("aria-label", "Wrong label"); _role "link" ])
                |> DropdownMenu.withContent items
                |> DropdownMenu.render (fun () -> "")
                |> Render.toString
            Expect.stringContains rendered "aria-label=\"Choose theme\"" "An icon trigger keeps its accessible name"
            Expect.stringContains rendered "data-testid=\"theme-trigger\"" "A trigger preserves safe consumer attributes"
            Expect.isFalse (rendered.Contains "Wrong label" || rendered.Contains "role=\"link\"") "A trigger protects renderer-owned semantics"
            Expect.stringContains rendered "role=\"menuitemradio\" aria-checked=\"true\"" "Radio choices expose initial selection"
            Expect.stringContains rendered "data-attr:aria-checked" "Checked state follows its trusted expression"
            Expect.stringContains rendered "data-show=\"$theme == &#39;system&#39;\"" "The decorative checkmark follows the same state"
            Expect.stringContains rendered ":is([role=menuitem], [role=menuitemradio], [role=menuitemcheckbox]):not([aria-disabled=true])" "Keyboard navigation includes every enabled menu choice"
            Expect.stringContains rendered "role=\"menuitemcheckbox\" aria-checked=\"true\"" "Checkbox choices expose independent checked state"
            Expect.stringContains rendered "data-on:click=\"$totals = !$totals\"" "Checkbox choices remain open while the caller applies a toggle"
            Expect.stringContains rendered "aria-busy=\"true\"" "Radio choices support pending state"
            Expect.isFalse (rendered.Contains "aria-selected=") "Menu choices do not use Select styling hooks"
            Expect.throws (fun () -> DropdownMenuItem.action "noop()" "Action" |> DropdownMenuItem.withChecked true |> ignore) "Only choice items accept checked state"
        }

        test "Components DropdownMenu renders complete item vocabulary and interaction semantics" {
            let leading = span { _attr ("data-test-icon", "review"); "✓" }
            let menuItems =
                [ DropdownMenuItem.group "Account" [
                      DropdownMenuItem.link 1 "Account settings"
                      DropdownMenuItem.action "$reviews++" "Record review"
                      |> DropdownMenuItem.withLeading leading
                      |> DropdownMenuItem.withShortcut "R"
                      DropdownMenuItem.link 2 "Unavailable link" |> DropdownMenuItem.disabled
                      DropdownMenuItem.action "$sync++" "Syncing account" |> DropdownMenuItem.pending ]
                  DropdownMenuItem.separator
                  DropdownMenuItem.action "$delete++" "Delete draft" |> DropdownMenuItem.withColor DropdownMenuItemColor.Error ]
            let html =
                DropdownMenu.create "account-actions" "Actions"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "Actions")
                |> DropdownMenu.withContent menuItems
                |> DropdownMenu.render (fun destination -> $"/accounts/{destination}")
                |> Render.toString

            Expect.stringContains html "aria-haspopup=\"menu\"" "DropdownMenu trigger identifies its popup"
            Expect.stringContains html "aria-controls=\"account-actions-menu\"" "DropdownMenu trigger controls its menu"
            Expect.stringContains html "role=\"menu\" tabindex=\"-1\" aria-label=\"Actions\"" "DropdownMenu container receives focus when no item is active"
            Expect.stringContains html "data-on:pointerleave" "DropdownMenu clears item focus on pointer leave"
            Expect.stringContains html "evt.detail == 0" "DropdownMenu distinguishes keyboard from pointer opening"
            Expect.stringContains html "role=\"group\" aria-labelledby=\"account-actions-menu-entry-0-label\"" "DropdownMenu labels groups"
            Expect.stringContains html ">Account</div>" "DropdownMenu keeps the group label visible"
            Expect.stringContains html "data-test-icon=\"review\"" "DropdownMenu preserves consumer-owned leading content"
            Expect.stringContains html "<kbd aria-hidden=\"true\"" "DropdownMenu presents shortcut hints without changing item names"
            Expect.stringContains html ">R</kbd>" "DropdownMenu preserves shortcut text"
            Expect.stringContains html "role=\"separator\"" "DropdownMenu preserves separators"
            Expect.stringContains html "popover=\"auto\"" "DropdownMenu uses a native auto popover"
            Expect.stringContains html "width: min(16rem, calc(100vw - 2rem))" "DropdownMenu bounds the top-layer popup width"
            Expect.stringContains html "position-area: block-end span-inline-start" "DropdownMenu preserves end alignment by default"
            Expect.stringContains html "position-try-fallbacks: flip-block, flip-inline, flip-block flip-inline" "DropdownMenu provides viewport-edge fallbacks"
            Expect.stringContains html "closest(&#39;[data-fve-sticky-cell=true]&#39;)" "DropdownMenu detects standardized sticky table cells"
            Expect.stringContains html "window.addEventListener(&#39;scroll&#39;, position, true)" "DropdownMenu tracks nested scrolling in its sticky-cell fallback"
            Expect.stringContains html "var(--fve-error-solid," "DropdownMenu preserves the selected Error color"
            Expect.stringContains html "fve-popup-item" "DropdownMenu uses the shared background-based popup focus treatment"
            Expect.stringContains html "fve-popup-control" "DropdownMenu trigger uses the same focus policy"
            Expect.isFalse (html.Contains("focus-visible:ring-2") || html.Contains("focus:ring-2")) "DropdownMenu focus does not use a ring"
            Expect.isFalse (html.Contains("fve-popup-destructive") || html.Contains("focus:bg-[var(--fve-critical-subtle)]")) "Destructive commands use the same neutral focus treatment"
            Expect.stringContains html "data-fve-menu-label=\"record review\"" "DropdownMenu exposes normalized labels for character navigation"
            Expect.stringContains html "_account_actions_typeahead" "DropdownMenu isolates bounded character-navigation state"
            Expect.stringContains html "popovertarget=\"account-actions-menu\"" "DropdownMenu associates its trigger with the native popover"
            Expect.stringContains html "data-on:beforetoggle" "DropdownMenu synchronizes native visibility with its local signal"
            Expect.stringContains html "data-on:click__prevent" "DropdownMenu routes pointer, Enter, and Space activation through the native popover"
            Expect.stringContains html "evt.key == &#39;ArrowDown&#39;" "DropdownMenu trigger opens with ArrowDown"
            Expect.stringContains html "Date.now()" "DropdownMenu bounds its character-navigation buffer"
            Expect.stringContains html "every(character =&gt; character == $_account_actions_typeahead[0])" "DropdownMenu cycles repeated characters"
            Expect.stringContains html ":not([aria-disabled=true])" "DropdownMenu movement skips unavailable items"
            Expect.stringContains html "document.activeElement.click()" "DropdownMenu activates focused items with Enter or Space"
            Expect.stringContains html "data-on:pointermove__window=\"el.dataset.fvePointerX = evt.clientX; el.dataset.fvePointerY = evt.clientY\"" "DropdownMenu remembers the last intentional pointer coordinates"
            Expect.stringContains html "data-preserve-attr=\"data-fve-pointer-x data-fve-pointer-y\"" "DropdownMenu retains pointer coordinates through a server morph"
            Expect.stringContains html "evt.clientX != Number(document.getElementById(&#39;account-actions-menu&#39;).dataset.fvePointerX)" "DropdownMenu ignores stationary pointer events after keyboard focus movement"
            Expect.stringContains html "document.getElementById(&#39;account-actions-trigger&#39;)?.focus()" "DropdownMenu restores its trigger when appropriate"

            let unavailableLink = Regex.Match(html, "<a[^>]*aria-disabled=\"true\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.isNotEmpty unavailableLink "DropdownMenu renders a disabled link item"
            Expect.isFalse (unavailableLink.Contains("href=")) "Disabled menu links cannot navigate"
            Expect.stringContains unavailableLink "cursor-not-allowed opacity-50" "Disabled menu links remain visibly unavailable"

            let pendingButton = Regex.Match(html, "<button[^>]*disabled[^>]*aria-busy=\"true\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.isNotEmpty pendingButton "DropdownMenu renders a native-disabled pending action"
            Expect.stringContains html "animate-spin" "Pending menu actions show a loading indicator"
            Expect.stringContains html "motion-reduce:animate-none" "Pending menu motion respects reduced-motion preferences"

            let startAlignedHtml =
                DropdownMenu.create "start-actions" "Start actions"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "Start actions")
                |> DropdownMenu.withContent [ DropdownMenuItem.link 1 "First" ]
                |> DropdownMenu.withAlignment DropdownMenuAlignment.Start
                |> DropdownMenu.render string
                |> Render.toString
            Expect.stringContains startAlignedHtml "position-area: block-end span-inline-end" "DropdownMenu supports typed start alignment"
            Expect.isFalse (startAlignedHtml.Contains("position-area: block-end span-inline-start")) "Start alignment replaces default end alignment"

            let adjacentHtml =
                div {
                    DropdownMenu.create "first-actions" "First actions"
                    |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "First actions")
                    |> DropdownMenu.withContent [ DropdownMenuItem.link 1 "First" ]
                    |> DropdownMenu.render string
                    DropdownMenu.create "second-actions" "Second actions"
                    |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "Second actions")
                    |> DropdownMenu.withContent [ DropdownMenuItem.link 2 "Second" ]
                    |> DropdownMenu.render string
                }
                |> Render.toString
            Expect.stringContains adjacentHtml "_first_actions_open" "First menu owns a stable signal"
            Expect.stringContains adjacentHtml "_second_actions_open" "Second menu owns an isolated stable signal"
            Expect.equal (Regex.Matches(adjacentHtml, "id=\"first-actions-menu\"").Count) 1 "First menu ID is unique"
            Expect.equal (Regex.Matches(adjacentHtml, "id=\"second-actions-menu\"").Count) 1 "Second menu ID is unique"

            Expect.throws (fun () -> DropdownMenuItem.link 1 " " |> ignore) "Menu links require accessible labels"
            Expect.throws (fun () -> DropdownMenuItem.group " " [ DropdownMenuItem.link 1 "Item" ] |> ignore) "Menu groups require labels"
            Expect.throws (fun () -> DropdownMenuItem.group "Empty" [] |> ignore) "Menu groups require items"
            Expect.throws (fun () -> DropdownMenuItem.group "Outer" [ DropdownMenuItem.group "Inner" [ DropdownMenuItem.link 1 "Item" ] ] |> ignore) "Menu groups cannot nest"
            Expect.throws (fun () -> DropdownMenuItem.separator<int> |> DropdownMenuItem.withShortcut "S" |> ignore) "Separators cannot have item presentation"
            Expect.throws (fun () -> DropdownMenu.create "missing-trigger" "Missing trigger" |> DropdownMenu.withContent [ DropdownMenuItem.link 1 "Item" ] |> DropdownMenu.render string |> ignore) "DropdownMenu requires an explicit trigger"
            Expect.throws (fun () -> DropdownMenu.create "missing-content" "Missing content" |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "Open") |> DropdownMenu.render string |> ignore) "DropdownMenu requires explicit content"
            Expect.throws (fun () -> DropdownMenu.create "empty-content" "Empty content" |> DropdownMenu.withContent [] |> ignore) "DropdownMenu content cannot be empty"

            let docsMenuHtml = Components.dropdownMenuRegion false |> Render.toString
            Expect.stringContains docsMenuHtml "href=\"/components/dropdown-menu#components-dropdown-menu\"" "Docs menu uses a real local typed destination"

            let patchedHtml = Components.patchedDropdownMenuRegion |> Render.toString
            Expect.stringContains patchedHtml "id=\"components-dropdown-menu-region\"" "Docs patch preserves the stable menu region"
            Expect.stringContains patchedHtml "Review refreshed actions" "Docs patch changes server-rendered menu content"
            Expect.stringContains patchedHtml "role=\"status\"" "Docs patch reports its completed server update"
            Expect.isFalse (patchedHtml.Contains("Refresh actions")) "Docs patch replaces the initiating command"
        }

        test "Components Select owns the complete branded select-only form contract" {
            let html =
                Select.create "status" "Status" id [ SelectOption.create "active" "Active"; SelectOption.create "disabled" "Disabled" |> SelectOption.disabled ]
                |> Select.withId "account-status"
                |> Select.withDescription "Account status"
                |> Select.withPlaceholder "Choose status"
                |> Select.withValidation "Choose an available status."
                |> Select.required
                |> Select.pending
                |> Select.render
                |> Render.toString

            Expect.isFalse (html.Contains("<select")) "Select never renders a native select element"
            let trigger = Regex.Match(html, "<button[^>]*role=\"combobox\"[^>]*>", RegexOptions.IgnoreCase).Value
            let submittedValue = Regex.Match(html, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.stringContains trigger "disabled" "pending Select prevents interaction"
            Expect.stringContains trigger "aria-required=\"true\"" "Select exposes required state on the combobox"
            Expect.stringContains trigger "aria-disabled=\"true\"" "Select exposes unavailable state"
            Expect.stringContains trigger "aria-invalid=\"true\"" "Select exposes server validation"
            Expect.stringContains trigger "aria-busy=\"true\"" "pending Select exposes busy state"
            Expect.stringContains trigger "aria-describedby=\"fve-select-account_status-description fve-select-account_status-validation\"" "Select joins help and validation relationships"
            Expect.stringContains submittedValue "name=\"status\"" "Select retains the consumer form name"
            Expect.stringContains submittedValue "disabled" "unavailable Select values are omitted by ordinary FormData"
            Expect.stringContains html "popover=\"auto\"" "Select renders its listbox in the native top layer"
            Expect.stringContains html "showPopover({source: document.getElementById(&#39;fve-select-account_status-trigger&#39;)})" "Select anchors the popup to its trigger"
            Expect.stringContains html "position-try-fallbacks: flip-block, flip-inline, flip-block flip-inline" "Select provides viewport-edge fallbacks"
            Expect.stringContains html "position-area: block-end span-inline-end" "an unselected Select falls back to its trigger start edge"
            let alignedHtml =
                Select.create "frequency" "Frequency" id [ SelectOption.create "daily" "Daily"; SelectOption.create "weekly" "Weekly"; SelectOption.create "monthly" "Monthly" ]
                |> Select.withSelected "weekly"
                |> Select.render
                |> Render.toString
            Expect.stringContains alignedHtml "aria-selected=\"true\"" "selected identities are available to positioning and assistive technology"
            Expect.isFalse (alignedHtml.Contains("rotate-180")) "opening does not rotate the plain Select chevron"
            let endAlignedHtml =
                Select.create "format" "Format" id [ SelectOption.create "csv" "CSV" ]
                |> Select.withPosition SelectPosition.TriggerEnd
                |> Select.render
                |> Render.toString
            Expect.stringContains endAlignedHtml "position-area: block-end span-inline-start" "Select supports typed trigger-end positioning"
            Expect.stringContains html "role=\"listbox\"" "Select renders its branded listbox"
            Expect.stringContains html "role=\"alert\"" "Select validation is announced after a patch"
            Expect.stringContains html "role=\"option\"" "Select renders branded options"
            Expect.stringContains html "aria-disabled=\"true\"" "Select preserves disabled options"
            Expect.stringContains html "_account_status_typeahead" "Select isolates bounded typeahead state"
            Expect.stringContains html "Date.now()" "Select resets typeahead after its bounded interval"
            Expect.stringContains html "every(character =&gt; character == $_account_status_typeahead[0])" "Select cycles repeated-character matches"
            Expect.stringContains html "evt.altKey &amp;&amp; evt.key == &#39;ArrowDown&#39;" "Select implements closed Alt+Down"
            Expect.stringContains html "evt.key == &#39;PageUp&#39;" "Select implements PageUp"
            Expect.stringContains html "evt.key == &#39;PageDown&#39;" "Select implements PageDown"
            Expect.stringContains html "Math.min" "Select clamps forward movement"
            Expect.stringContains html "Math.max" "Select clamps backward movement"
            Expect.stringContains html "evt.key == &#39;Tab&#39;" "Select commits the active option on Tab"
            Expect.stringContains html "document.getElementById($_account_status_active)?.click()" "Select commits active identity through one option path"
        }

        test "Components searchable Select preserves query, selection, async state, and form semantics" {
            let options = [ SelectOption.create 101 "Operating"; SelectOption.create 102 "Tax reserve" |> SelectOption.disabled ]
            let html =
                Select.create "account" "Parent account" string options
                |> Select.withId "parent-account"
                |> Select.withSelected 101
                |> Select.withDescription "Server-owned accounts."
                |> Select.withValidation "Choose an account."
                |> Select.withSearch (SelectSearch.Remote "/accounts/search")
                |> Select.render
                |> Render.toString

            let trigger = Regex.Match(html, "<button[^>]*aria-haspopup=\"dialog\"[^>]*>", RegexOptions.IgnoreCase).Value
            let searchInput = Regex.Match(html, "<input[^>]*type=\"search\"[^>]*>", RegexOptions.IgnoreCase).Value
            let submittedValue = Regex.Match(html, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.stringContains trigger "id=\"fve-select-parent_account\"" "stable ID owns the Select trigger"
            Expect.stringContains trigger "aria-controls=\"fve-select-parent_account-popup\"" "trigger controls the labelled search popup"
            Expect.stringContains trigger "aria-describedby=\"fve-select-parent_account-description fve-select-parent_account-validation\"" "description and validation are joined"
            Expect.stringContains trigger "aria-invalid=\"true\"" "server validation is exposed"
            Expect.stringContains trigger "group flex" "searchable Select uses the same flex trigger layout as Select"
            Expect.stringContains trigger "justify-between" "searchable Select keeps its chevron on the trigger edge"
            Expect.isFalse (html.Contains("rotate-180")) "opening does not rotate the searchable Select chevron"
            Expect.stringContains searchInput "id=\"fve-select-parent_account-search\"" "popup owns a distinct search input"
            Expect.stringContains searchInput "data-bind:parent_account_query" "remote query remains separate from selected identity"
            Expect.stringContains searchInput "requestCancellation: &#39;auto&#39;" "remote requests explicitly cancel older same-endpoint requests"
            Expect.stringContains submittedValue "name=\"account\"" "hidden input preserves the consumer form name"
            Expect.stringContains submittedValue "value=\"101\"" "typed selected identity is explicitly encoded"
            Expect.stringContains submittedValue "data-bind:parent_account_value" "submitted identity remains distinct from query binding"
            Expect.stringContains html "popover=\"auto\"" "searchable Select renders its popup in the native top layer"
            Expect.stringContains html "showPopover({source: document.getElementById(&#39;fve-select-parent_account&#39;)})" "searchable Select anchors the popup to its trigger"
            Expect.stringContains html "position-try-fallbacks: flip-block, flip-inline, flip-block flip-inline" "searchable Select provides viewport-edge fallbacks"
            Expect.stringContains html "role=\"listbox\"" "popup contains the canonical listbox"
            Expect.stringContains html "data-on:pointermove" "pointer and keyboard share one active option"
            Expect.stringContains html "data-on:pointerleave" "pointer leave clears the active option"
            Expect.stringContains html "_parent_account_active: &#39;&#39;" "searchable Select opens without a persistent active option"
            Expect.stringContains html "aria-disabled=\"true\"" "disabled options remain discoverable but unavailable"
            Expect.stringContains html "role=\"alert\"" "form validation is announced"

            let staticHtml =
                Select.create "local" "Local account" string options
                |> Select.withSearch SelectSearch.Static
                |> Select.withEmptyMessage "No local accounts"
                |> Select.render
                |> Render.toString
            Expect.stringContains staticHtml "_local_query" "static query remains private ephemeral state"
            Expect.stringContains staticHtml "includes($_local_query.trim().toLowerCase())" "static options filter locally"
            Expect.stringContains staticHtml "No local accounts" "static empty state is configurable"
            Expect.isFalse (staticHtml.Contains("@get(")) "static filtering has no backend action"

            let errorHtml =
                Select.create "failed" "Failed account" string []
                |> Select.withSearch (SelectSearch.Remote "/accounts/search?retry=true")
                |> Select.withError "Accounts could not be loaded."
                |> Select.render
                |> Render.toString
            Expect.stringContains errorHtml "Accounts could not be loaded." "server-rendered fetch error is visible"
            Expect.stringContains errorHtml ">Retry</button>" "remote error offers a retry action"
            Expect.stringContains errorHtml "requestCancellation: &#39;auto&#39;" "retry preserves the same ordering policy"

            let loadingHtml =
                Select.create "loading" "Loading account" string []
                |> Select.withSearch SelectSearch.Static
                |> Select.withLoadingMessage "Loading accounts"
                |> Select.loading
                |> Select.render
                |> Render.toString
            Expect.stringContains loadingHtml "aria-busy=\"true\"" "loading state is programmatically busy"
            Expect.stringContains loadingHtml "Loading accounts" "loading status remains perceivable"

            let pendingHtml =
                Select.create "pending" "Pending account" string options
                |> Select.withSearch SelectSearch.Static
                |> Select.withSelected 101
                |> Select.pending
                |> Select.render
                |> Render.toString
            let pendingCombobox = Regex.Match(pendingHtml, "<button[^>]*aria-haspopup=\"dialog\"[^>]*>", RegexOptions.IgnoreCase).Value
            let pendingValue = Regex.Match(pendingHtml, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.stringContains pendingCombobox "disabled" "pending control prevents interaction"
            Expect.stringContains pendingCombobox "aria-busy=\"true\"" "pending control exposes busy state"
            Expect.stringContains pendingValue "disabled" "pending selected identity is omitted from FormData"
            Expect.isFalse (pendingHtml.Contains("data-on:keydown")) "unavailable control emits no keyboard action"
        }

        test "Components branded choice controls preserve distinct complete semantics" {
            let searchableSelectHtml =
                Select.create "account" "Account" id [ SelectOption.create "operating" "Operating" ]
                |> Select.withSearch SelectSearch.Static
                |> Select.withSelected "operating"
                |> Select.render
                |> Render.toString
            Expect.stringContains searchableSelectHtml "aria-haspopup=\"dialog\"" "searchable Select remains a Select trigger with a popup"
            Expect.stringContains searchableSelectHtml "type=\"search\"" "searchable Select keeps filtering in its popup"

            let checkboxHtml =
                Checkbox.create "confirmed" "Confirmed"
                |> Checkbox.withId "review-confirmed"
                |> Checkbox.withDescription "Review completed."
                |> Checkbox.withValidation "Confirm the review."
                |> Checkbox.required
                |> Checkbox.render
                |> Render.toString
            let checkbox = Regex.Match(checkboxHtml, "<input[^>]*type=\"checkbox\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.stringContains checkbox "id=\"fve-checkbox-review_confirmed\"" "Checkbox accepts stable instance identity"
            Expect.stringContains checkbox "name=\"confirmed\"" "Checkbox preserves consumer form name"
            Expect.stringContains checkbox "required" "enabled required Checkbox uses native constraint semantics"
            Expect.stringContains checkbox "aria-required=\"true\"" "Checkbox exposes required state"
            Expect.stringContains checkbox "aria-invalid=\"true\"" "Checkbox exposes validation state"
            Expect.stringContains checkbox "aria-describedby=\"fve-checkbox-review_confirmed-description fve-checkbox-review_confirmed-validation\"" "Checkbox joins descriptions"
            Expect.stringContains checkboxHtml "role=\"alert\"" "Checkbox validation is announced"
            Expect.stringContains checkboxHtml "class=\"peer sr-only\"" "Checkbox browser chrome is visually hidden"

            let unavailableCheckboxHtml =
                Checkbox.create "confirmed" "Confirmed"
                |> Checkbox.required
                |> Checkbox.pending
                |> Checkbox.render
                |> Render.toString
            let unavailableCheckbox = Regex.Match(unavailableCheckboxHtml, "<input[^>]*type=\"checkbox\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.stringContains unavailableCheckbox "disabled" "pending Checkbox is natively unavailable"
            Expect.stringContains unavailableCheckbox "aria-busy=\"true\"" "pending Checkbox exposes busy state"
            Expect.isFalse (Regex.IsMatch(unavailableCheckbox, "\\srequired(?:\\s|>)", RegexOptions.IgnoreCase)) "disabled Checkbox does not emit invalid required markup"
            Expect.stringContains unavailableCheckboxHtml "animate-spin" "pending Checkbox renders a reduced-motion-safe indicator"

            let switchHtml =
                Switch.create "notifications" "Notifications"
                |> Switch.withId "account-notifications"
                |> Switch.withChecked true
                |> Switch.withValidation "Could not save."
                |> Switch.pending
                |> Switch.render
                |> Render.toString
            let switchControl = Regex.Match(switchHtml, "<input[^>]*role=\"switch\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.stringContains switchControl "name=\"notifications\"" "Switch retains native form submission"
            Expect.stringContains switchControl "aria-checked=\"true\"" "Switch has switch state"
            Expect.stringContains switchControl "data-attr:aria-checked" "Switch state remains synchronized"
            Expect.stringContains switchControl "disabled" "pending Switch is unavailable"
            Expect.stringContains switchControl "aria-busy=\"true\"" "pending Switch exposes busy state"
            Expect.stringContains switchControl "aria-invalid=\"true\"" "Switch exposes server validation"

            let toggleHtml =
                ToggleButton.create "compact" "Compact rows"
                |> ToggleButton.pressed
                |> ToggleButton.pending
                |> ToggleButton.render
                |> Render.toString
            Expect.stringContains toggleHtml "aria-pressed=\"true\"" "ToggleButton has pressed semantics"
            Expect.stringContains toggleHtml "data-attr:aria-pressed" "ToggleButton pressed state remains synchronized"
            Expect.stringContains toggleHtml "disabled" "pending ToggleButton prevents activation"
            Expect.stringContains toggleHtml "aria-busy=\"true\"" "pending ToggleButton exposes busy state"
            Expect.isFalse (toggleHtml.Contains("data-on:click")) "unavailable ToggleButton has no activation expression"

            let radioHtml =
                RadioGroup.create "mode" "Mode" id [ RadioGroupOption.create "automatic" "Automatic"; RadioGroupOption.create "manual" "Manual"; RadioGroupOption.create "disabled" "Disabled" |> RadioGroupOption.disabled ]
                |> RadioGroup.withId "posting-mode"
                |> RadioGroup.withDescription "Posting behavior."
                |> RadioGroup.withValidation "Choose a mode."
                |> RadioGroup.required
                |> RadioGroup.render
                |> Render.toString
            Expect.stringContains radioHtml "role=\"radiogroup\"" "RadioGroup has grouped choice semantics"
            Expect.stringContains radioHtml "aria-labelledby=\"fve-radio-posting_mode-legend\"" "RadioGroup uses its visible legend as name"
            Expect.stringContains radioHtml "aria-required=\"true\"" "RadioGroup exposes required state on the group"
            Expect.stringContains radioHtml "aria-invalid=\"true\"" "RadioGroup exposes validation state"
            Expect.equal (Regex.Matches(radioHtml, "type=\"radio\"").Count) 3 "RadioGroup retains one radio input per option"
            Expect.equal (Regex.Matches(radioHtml, "name=\"mode\"").Count) 3 "RadioGroup options share the consumer form name"
            Expect.equal (Regex.Matches(radioHtml, " required").Count) 2 "only enabled options participate in required native validation"
            Expect.equal (Regex.Matches(radioHtml, "class=\"peer sr-only\"").Count) 3 "Radio browser chrome is visually hidden"
            Expect.stringContains radioHtml "role=\"alert\"" "RadioGroup validation is announced"
        }

        test "Checked setters replace prior server state in either direction" {
            for isChecked in [ true; false ] do
                let checkboxHtml =
                    Checkbox.create "confirmed" "Confirmed"
                    |> Checkbox.withChecked (not isChecked)
                    |> Checkbox.withChecked isChecked
                    |> Checkbox.render |> Render.toString
                let switchHtml =
                    Switch.create "notifications" "Notifications"
                    |> Switch.withChecked (not isChecked)
                    |> Switch.withChecked isChecked
                    |> Switch.render |> Render.toString
                for html in [ checkboxHtml; switchHtml ] do
                    let input = Regex.Match(html, "<input[^>]*>").Value
                    Expect.equal (Regex.IsMatch(input, "\\schecked(?:\\s|>)")) isChecked "native checked state uses the last Boolean value"
                let expected = if isChecked then "true" else "false"
                Expect.stringContains switchHtml $"aria-checked=\"{expected}\"" "Switch exposes the same accessible state"
                let menuHtml =
                    DropdownMenu.create "checked-menu" "View"
                    |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "View")
                    |> DropdownMenu.withContent [
                        DropdownMenuItem.radio "$compact = !$compact" "Compact"
                        |> DropdownMenuItem.withChecked (not isChecked)
                        |> DropdownMenuItem.withChecked isChecked ]
                    |> DropdownMenu.render id |> Render.toString
                Expect.stringContains menuHtml $"aria-checked=\"{expected}\"" "menu radio items follow the same setter contract"
        }

        test "Components finite-choice stable IDs isolate repeated form names" {
            let html =
                form {
                    Checkbox.create "choice" "First choice" |> Checkbox.withId "first-choice" |> Checkbox.render
                    Checkbox.create "choice" "Second choice" |> Checkbox.withId "second-choice" |> Checkbox.render
                    Switch.create "setting" "First setting" |> Switch.withId "first-setting" |> Switch.render
                    Switch.create "setting" "Second setting" |> Switch.withId "second-setting" |> Switch.render
                    RadioGroup.create "mode" "First mode" id [ RadioGroupOption.create "a" "A" ] |> RadioGroup.withId "first-mode" |> RadioGroup.render
                    RadioGroup.create "mode" "Second mode" id [ RadioGroupOption.create "b" "B" ] |> RadioGroup.withId "second-mode" |> RadioGroup.render
                }
                |> Render.toString

            for expected in [ "choice_checked"; "first_choice_checked"; "second_choice_checked"; "first_setting_enabled"; "second_setting_enabled"; "first_mode_value"; "second_mode_value" ] do
                if expected <> "choice_checked" then Expect.stringContains html expected $"stable instance owns {expected}"
            let ids =
                Regex.Matches(html, "\\sid=\"([^\"]+)\"")
                |> Seq.cast<Match>
                |> Seq.map (fun matched -> matched.Groups[1].Value)
                |> Seq.toList
            Expect.equal ids.Length (ids |> List.distinct |> List.length) "repeated form names produce no duplicate IDs"
            Expect.equal (Regex.Matches(html, "name=\"choice\"").Count) 2 "stable IDs do not change shared Checkbox names"
            Expect.equal (Regex.Matches(html, "name=\"mode\"").Count) 2 "stable IDs do not change shared RadioGroup names"
            Expect.throws (fun () -> Checkbox.create "choice" "Choice" |> Checkbox.withId " " |> ignore) "Checkbox stable IDs reject whitespace"
            Expect.throws (fun () -> Switch.create "setting" "Setting" |> Switch.withId " " |> ignore) "Switch stable IDs reject whitespace"
            Expect.throws (fun () -> RadioGroup.create "mode" "Mode" id [] |> RadioGroup.withId " " |> ignore) "RadioGroup stable IDs reject whitespace"
        }

        test "Components option IDs are stable and collision-free for distinct encoded values" {
            let options = [ SelectOption.create "a/b" "Slash"; SelectOption.create "a-b" "Dash" ]
            let optionIds prefix html =
                Regex.Matches(html, $"id=\"({Regex.Escape(prefix)}-option-[^\"]+)\"")
                |> Seq.cast<Match>
                |> Seq.map (fun matched -> matched.Groups[1].Value)
                |> Seq.toList
            let expectStableDistinct prefix render =
                let first = render () |> optionIds prefix
                let second = render () |> optionIds prefix
                Expect.equal first.Length 2 $"{prefix} renders both adversarial options"
                Expect.equal (first |> List.distinct |> List.length) 2 $"{prefix} option IDs do not collide"
                Expect.equal second first $"{prefix} option IDs are deterministic across renders"

            expectStableDistinct "fve-select-collisionselect" (fun () ->
                Select.create "collisionselect" "Collision select" id options
                |> Select.render
                |> Render.toString)
            expectStableDistinct "fve-select-collisioncombobox" (fun () ->
                Select.create "collisioncombobox" "Collision searchable Select" id options
                |> Select.withSearch SelectSearch.Static
                |> Select.render
                |> Render.toString)
            expectStableDistinct "fve-radio-collisionradio" (fun () ->
                RadioGroup.create "collisionradio" "Collision radio" id [ RadioGroupOption.create "a/b" "Slash"; RadioGroupOption.create "a-b" "Dash" ]
                |> RadioGroup.render
                |> Render.toString)
        }

        test "Components escape hatches preserve authoritative attributes" {
            let openingTag elementName element =
                let html = element |> Render.toString
                let tag = Regex.Match(html, $"<{elementName}[^>]*>", RegexOptions.IgnoreCase).Value
                Expect.isNotEmpty tag $"{elementName} opening tag"
                tag
            let attributeCount name tag =
                Regex.Matches(tag, $"\\s{Regex.Escape(name)}(?:=|\\s|>)", RegexOptions.IgnoreCase).Count

            let buttonTag =
                Button.create (ButtonContent.Text "Save")
                |> Button.disabled
                |> Button.withAttributes [ _attr ("TYPE", "submit"); _attr "disabled"; _class "override" ]
                |> Button.render
                |> openingTag "button"
            Expect.equal (attributeCount "type" buttonTag) 1 "Button owns one type"
            Expect.equal (attributeCount "disabled" buttonTag) 1 "Button owns one disabled state"
            Expect.equal (attributeCount "class" buttonTag) 1 "Button owns one class attribute"
            Expect.stringContains buttonTag "type=\"button\"" "consumer type is ignored"
            Expect.isFalse (buttonTag.Contains("override")) "consumer class is ignored"

            let badgeTag =
                Badge.create "Active"
                |> Badge.withAttributes [ _class "override" ]
                |> Badge.render
                |> openingTag "span"
            Expect.equal (attributeCount "class" badgeTag) 1 "Badge owns one class attribute"
            Expect.isFalse (badgeTag.Contains("override")) "Badge consumer class is ignored"

            let tableTag =
                Table.create "Values" [ TableColumn.create "Value" text ] [ "one" ]
                |> Table.withAttributes [ _role "presentation"; _class "override" ]
                |> Table.render
                |> openingTag "table"
            Expect.equal (attributeCount "class" tableTag) 1 "Table owns one class attribute"
            Expect.isFalse (tableTag.Contains("override")) "Table consumer class is ignored"
            Expect.isFalse (tableTag.Contains("presentation")) "Table consumer role is ignored"

            let selectHtml =
                Select.create "status" "Status" id [ SelectOption.create "active" "Active" ]
                |> Select.withAttributes [
                    _id "other-id"
                    _name "other-name"
                    _dataBind "other"
                    _ariaActivedescendant "other-option"
                    _ariaInvalid false
                    _class "override" ]
                |> Select.render
                |> Render.toString
            let selectTrigger = Regex.Match(selectHtml, "<button[^>]*role=\"combobox\"[^>]*>", RegexOptions.IgnoreCase).Value
            let selectValue = Regex.Match(selectHtml, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.isNotEmpty selectTrigger "Select trigger opening tag"
            Expect.isNotEmpty selectValue "Select hidden value opening tag"
            for name in [ "id"; "type"; "role"; "aria-invalid"; "class" ] do
                Expect.equal (attributeCount name selectTrigger) 1 $"Select owns one trigger {name}"
            Expect.equal (attributeCount "name" selectValue) 1 "Select owns one submitted name"
            Expect.equal (Regex.Matches(selectValue, "\\sdata-bind:[^=\\s>]+(?:=|\\s|>)", RegexOptions.IgnoreCase).Count) 1 "Select owns one submitted Datastar binding"
            for rejected in [ "other-id"; "other-name"; "data-bind:other"; "other-option"; "override" ] do
                Expect.isFalse (selectTrigger.Contains(rejected)) $"Select rejects reserved trigger attribute value {rejected}"

            let comboboxTag =
                Select.create "account" "Account" id [ SelectOption.create "operating" "Operating" ]
                |> Select.withSearch SelectSearch.Static
                |> Select.withAttributes [
                    _id "other-id"
                    _name "other-name"
                    _dataBind "other"
                    _ariaActivedescendant "other-option"
                    _ariaInvalid false
                    _class "override" ]
                |> Select.render
                |> openingTag "button"
            for name in [ "id"; "type"; "aria-invalid"; "class" ] do
                Expect.equal (attributeCount name comboboxTag) 1 $"Searchable Select owns one trigger {name}"
            for rejected in [ "other-id"; "other-name"; "data-bind:other"; "other-option"; "override" ] do
                Expect.isFalse (comboboxTag.Contains(rejected)) $"Searchable Select rejects reserved trigger attribute value {rejected}"
        }

        test "Notification owns local lifetime and NotificationRegion renders zero or one latest item" {
            let basic = Notification.create "notice-default" "Saved" (p { "The record is current." })
            let basicHtml = basic |> Notification.render |> Render.toString
            Expect.stringContains basicHtml "data-fve-notification-ttl=\"5000\"" "default lifetime"
            Expect.stringContains basicHtml "data-on-interval__duration.100ms" "local timer"
            Expect.stringContains basicHtml "document.hidden" "hidden documents pause expiry"
            Expect.stringContains basicHtml "closest(&#39;[data-fve-notification-region]&#39;)" "the whole active region owns interaction pause"
            Expect.stringContains basicHtml ":hover" "hover pauses expiry"
            Expect.stringContains basicHtml ":focus-within" "focus pauses expiry"
            Expect.stringContains basicHtml "bg-[var(--fve-surface)]" "notification card uses the neutral surface"
            Expect.stringContains basicHtml "data-fve-notification-dismiss" "accessible dismissal control"

            let configuredHtml = basic |> Notification.withTtl 1250 |> Notification.render |> Render.toString
            Expect.stringContains configuredHtml "data-fve-notification-ttl=\"1250\"" "custom positive lifetime"
            let persistentHtml = basic |> Notification.persistent |> Notification.render |> Render.toString
            Expect.stringContains persistentHtml "data-fve-notification-persistent=\"true\"" "persistent mode"
            Expect.isFalse (persistentHtml.Contains("data-on-interval")) "persistent notifications do not start a timer"
            Expect.throws (fun () -> basic |> Notification.withTtl 0 |> ignore) "non-positive lifetimes are rejected"

            let regionHtml =
                basic
                |> Some
                |> NotificationRegion.create "current-notification" "Current notification"
                |> NotificationRegion.withinContainer
                |> NotificationRegion.render
                |> Render.toString
            Expect.equal (Regex.Matches(regionHtml, "data-fve-notification=\"true\"").Count) 1 "populated regions render one card"
            Expect.stringContains regionHtml "notice-default" "the current notification is rendered"
            Expect.stringContains regionHtml "bottom-4" "bottom placement"
            Expect.stringContains regionHtml "sm:end-4" "logical inline-end placement"
            Expect.stringContains regionHtml "max-h-[calc(100%-24px)]" "bounded notifications remain available at large text sizes"
            Expect.stringContains regionHtml "overflow-y-auto" "oversized notification content remains reachable"

            let emptyHtml = NotificationRegion.create "empty-notifications" "Notifications" None |> NotificationRegion.render |> Render.toString
            Expect.stringContains emptyHtml "id=\"empty-notifications\"" "empty regions are supported"
            Expect.equal (Regex.Matches(emptyHtml, "data-fve-notification=\"true\"").Count) 0 "empty regions contain no card"
        }

        test "Documentation and controls share one assembly without implicit browser assets" {
            let assembly = typeof<ControlSize>.Assembly
            Expect.equal typeof<FSharp.ViewEngine.Components.Templates.DocsColorMode>.Assembly assembly "Documentation ships inside Components"
            Expect.equal (assembly.GetName().Name) "FSharp.ViewEngine.Components" "one UI assembly"
            let ordinaryPage = Button.create (ButtonContent.Text "Continue") |> Button.render |> Render.toString
            for documentationAsset in [ "Prism"; "mermaid"; "spec-shell"; "data-docs-" ] do
                Expect.isFalse (ordinaryPage.Contains documentationAsset) "ordinary controls do not opt into Documentation assets"
        }

        test "Removed Tailwind documentation route is not registered" {
            Expect.isFalse (Registry.all |> List.exists (fun page -> page.path = "/extensions/tailwind")) "old canonical route"
            Expect.isFalse (Registry.aliases |> List.exists (fun (alias, _) -> alias = "/extensions/tailwind")) "old route alias"
            Expect.equal (routeStatus "/extensions/tailwind") 404 "removed route returns not found"
        }

        test "Compatibility route remains registered" {
            Expect.contains Registry.aliases ("/giraffe", Usage.page.path) "legacy Giraffe route"
        }
    ]

[<EntryPoint>]
let main args = runTestsWithCLIArgs [] args tests
