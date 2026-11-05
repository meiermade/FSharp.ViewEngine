module Docs.Tests.Program

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Xml.Linq
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar.DependencyInjection
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

let private mutationStatus path (origin: string) (contentType: string) contentLength =
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
    let next: HttpFunc = fun current -> task { return Some current }

    Handler.postRoutes next context
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> ignore

    context.Response.StatusCode

let private postFormResponse (reference: string) (fields: (string * string) list) =
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

    context.Request.Headers.Referer <-
        Microsoft.Extensions.Primitives.StringValues("https://fve.meiermade.com" + uri.PathAndQuery)

    context.Request.ContentType <- "application/x-www-form-urlencoded"
    context.Request.ContentLength <- Nullable(int64 body.Length)
    context.Request.Body <- new MemoryStream(body)
    context.Response.Body <- new MemoryStream()
    let next: HttpFunc = fun current -> task { return Some current }

    Handler.postRoutes next context
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> ignore

    context.Response.Body.Position <- 0L
    use reader = new StreamReader(context.Response.Body)

    context.Response.StatusCode,
    context.Response.Headers.Location.ToString(),
    context.Response.Headers.SetCookie.ToString(),
    reader.ReadToEnd(),
    context.Response.Headers.CacheControl.ToString()

let private postForm reference fields =
    let status, location, cookies, body, _ = postFormResponse reference fields
    status, location, cookies, body

let private shellTestUrl =
    function
    | Home -> "/"
    | Accounts -> "/accounts"
    | Account id -> $"/accounts/{id}"
    | Reports -> "/reports"
    | Settings -> "/settings"

let private expectedPaths = Registry.all |> List.map _.path |> set

let private routeResponseWithCookie (reference: string) (cookie: string) =
    let uri = Uri(Uri "https://fve.meiermade.com", reference)
    let context = DefaultHttpContext()
    context.Request.Method <- HttpMethods.Get
    context.Request.Scheme <- uri.Scheme
    context.Request.Host <- HostString uri.Host
    context.Request.Path <- PathString uri.AbsolutePath
    context.Request.QueryString <- QueryString uri.Query
    context.Request.Headers.Cookie <- cookie
    context.Response.Body <- new MemoryStream()
    let next: HttpFunc = fun current -> task { return Some current }
    let registrations = ServiceCollection()
    registrations.AddDatastar() |> ignore
    use services = registrations.BuildServiceProvider()
    context.RequestServices <- services
    let accessor = services.GetRequiredService<IHttpContextAccessor>()
    accessor.HttpContext <- context

    try
        let result =
            Handler.routes next context |> Async.AwaitTask |> Async.RunSynchronously

        context.Response.Body.Position <- 0L
        use reader = new StreamReader(context.Response.Body)
        let body = reader.ReadToEnd()
        (if result.IsSome then context.Response.StatusCode else 404), body
    finally
        accessor.HttpContext <- null

let private routeResponse reference = routeResponseWithCookie reference ""

let private routeStatus reference = routeResponse reference |> fst

let private referenceStatus (reference: string) =
    let uri = Uri(Uri "https://fve.meiermade.com", reference)

    let webRoot =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "Docs", "wwwroot"))

    let relativePath =
        Uri
            .UnescapeDataString(uri.AbsolutePath.TrimStart('/'))
            .Replace('/', Path.DirectorySeparatorChar)

    let filePath = Path.GetFullPath(Path.Combine(webRoot, relativePath))

    if filePath.StartsWith(webRoot, StringComparison.Ordinal) && File.Exists filePath then
        200
    else
        routeStatus uri.PathAndQuery

[<Tests>]
let tests =
    testList
        "Direct F# documentation"
        [ testCase "page-example mutation boundaries reject foreign origins, unsupported forms, and oversized bodies"
          <| fun _ ->
              let path = "/components/page-examples/messaging/send"

              Expect.equal
                  (mutationStatus path "https://foreign.example" "application/x-www-form-urlencoded" 10L)
                  403
                  "foreign origin"

              Expect.equal
                  (mutationStatus path "https://fve.meiermade.com" "application/json" 10L)
                  415
                  "unsupported content type"

              Expect.equal
                  (mutationStatus path "https://fve.meiermade.com" "application/x-www-form-urlencoded" 3_000_001L)
                  413
                  "request limit"

          testCase "page-example messages use finite URL-backed outcomes without retaining submitted text"
          <| fun _ ->
              let emptyStatus, emptyLocation, emptyCookie, _ =
                  postForm "/components/page-examples/messaging/send?item=beach" [ "message", "  " ]

              Expect.equal emptyStatus 302 "Whitespace submissions redirect to a finite validation state."
              Expect.stringContains emptyLocation "view=message-empty" "The validation state is URL-backed."
              Expect.equal emptyCookie "" "The example does not create a session cookie."

              let marker = "private-message-should-not-be-retained"

              let sentStatus, sentLocation, sentCookie, _ =
                  postForm "/components/page-examples/messaging/send?item=beach" [ "message", marker ]

              Expect.equal sentStatus 302 "Valid submissions redirect to the deterministic sent state."
              Expect.stringContains sentLocation "view=sent" "The sent state is URL-backed."
              Expect.equal sentCookie "" "The example does not create a session cookie."
              let status, html = routeResponse sentLocation
              Expect.equal status 200 "The deterministic state is directly renderable."

              Expect.stringContains
                  html
                  "Meet at the east entrance 15 minutes before we leave."
                  "The authored sent fixture is rendered."

              Expect.isFalse (html.Contains marker) "Submitted message text is not retained or reflected."

          testCase "page-example media never accepts file bodies and uses a repository fixture"
          <| fun _ ->
              Expect.equal
                  (mutationStatus
                      "/components/page-examples/media-management/upload"
                      "https://fve.meiermade.com"
                      "multipart/form-data; boundary=fixture"
                      128L)
                  415
                  "Multipart bodies are rejected before form parsing."

              let marker = "private-upload-name.png"

              let status, location, cookie, _ =
                  postForm
                      "/components/page-examples/media-management/upload"
                      [ "name", marker; "alt", "Private upload description" ]

              Expect.equal status 302 "Metadata-only submission reaches the deterministic success state."

              Expect.stringContains
                  location
                  "item=photo-uploaded"
                  "The repository fixture has a stable authored identity."

              Expect.stringContains location "view=uploaded" "The success state is URL-backed."
              Expect.equal cookie "" "The example does not create a session cookie."
              let renderedStatus, html = routeResponse location
              Expect.equal renderedStatus 200 "The success fixture is directly renderable."
              Expect.stringContains html "/images/page-examples/violet.png" "The result uses a repository-owned asset."
              Expect.isFalse (html.Contains marker) "Submitted metadata is not retained or reflected."

              Expect.equal
                  (routeStatus "/components/page-examples/images/untrusted")
                  404
                  "There is no temporary image-serving route."

          testCase "page-example accounts and settings return deterministic validation states"
          <| fun _ ->
              let marker = "Private account value"

              let accountStatus, accountLocation, accountCookie, _ =
                  postForm
                      "/components/page-examples/account-management/create"
                      [ "name", marker; "accountType", "Asset" ]

              Expect.equal accountStatus 302 "A valid account form redirects."
              Expect.stringContains accountLocation "view=account-created" "Account success is a finite URL state."
              Expect.equal accountCookie "" "Account submission does not create a session cookie."
              let _, accountHtml = routeResponse accountLocation
              Expect.stringContains accountHtml "does not create or retain records" "The resettable result is explicit."
              Expect.isFalse (accountHtml.Contains marker) "Submitted account values are not retained or reflected."

              let settingsStatus, settingsLocation, settingsCookie, _ =
                  postForm
                      "/components/page-examples/account-management/settings"
                      [ "workspaceName", "Private workspace value" ]

              Expect.equal settingsStatus 302 "A valid settings form redirects."
              Expect.stringContains settingsLocation "view=settings-saved" "Settings success is a finite URL state."
              Expect.equal settingsCookie "" "Settings submission does not create a session cookie."
              let _, settingsHtml = routeResponse settingsLocation
              Expect.stringContains settingsHtml "does not retain submitted values" "The resettable result is explicit."

              Expect.isFalse
                  (settingsHtml.Contains "Private workspace value")
                  "Submitted settings are not retained or reflected."

          test "Operational components retain native form and accessible state contracts" {
              let files =
                  fveFileSelection {
                      _id "statements"
                      _name "statements"
                      _ariaLabel "Statements"
                      _multiple true
                      _accept ".csv"
                      _required true
                  }
                  |> Render.toString

              for expected in
                  [ "type=\"file\""
                    "name=\"statements\""
                    "multiple"
                    "accept=\".csv\""
                    "required" ] do
                  Expect.stringContains files expected "native file-selection contract"

              Expect.equal (Regex.Matches(files, "<input ").Count) 1 "the native file control remains authoritative"
              Expect.stringContains files "data-fve-file-clear" "clearing is built in without an option"
              Expect.isTrue (Regex.IsMatch(files, "<button[^>]* hidden")) "the clear action starts hidden"
              Expect.isFalse (files.Contains "<ul") "selection details are not duplicated"

              let pendingFiles =
                  fveFileSelection {
                      _id "pending"
                      _name "pending"
                      _ariaLabel "Pending files"
                      _readonly true
                      _ariaBusy true
                  }
                  |> Render.toString

              Expect.stringContains pendingFiles "aria-busy=\"true\"" "pending file selection exposes busy state"

              Expect.stringContains pendingFiles "readonly" "read-only selection keeps its successful value"
              Expect.stringContains pendingFiles "aria-disabled=\"true\"" "read-only selection communicates unavailable editing"
              Expect.isFalse (Regex.IsMatch(pendingFiles, "<input[^>]*\\sdisabled(?:\\s|=|>)")) "read-only is not disabled"
              let busyOnly = fveFileSelection { _id "busy-files"; _name "files"; _ariaBusy true } |> Render.toString
              Expect.isFalse (Regex.IsMatch(busyOnly, "<input[^>]*\\s(?:readonly|disabled)(?:\\s|=|>)")) "busy alone does not lock selection"

              let progress =
                  fveProgress {
                      _ariaLabel "Import"
                      _value "3"
                      _max "4"
                      _ariaValuetext "Three of four"
                  }
                  |> Render.toString

              let nativeProgress = Regex.Match(progress, "<progress[^>]*>").Value
              Expect.stringContains nativeProgress "value=\"3\"" "native progress value"
              Expect.stringContains nativeProgress "max=\"4\"" "native progress maximum"
              Expect.stringContains progress "Three of four" "readable value text"

              Expect.throws
                  (fun () ->
                      fveProgress {
                          _ariaLabel "Import"
                          _value "5"
                          _max "4"
                      }
                      |> ignore)
                  "out-of-range progress is rejected"

              let credential =
                  fveCopyReveal {
                      _id "credential"
                      _ariaLabel "Demo token"
                      _value "safe-demo"
                  }
                  |> Render.toString

              Expect.stringContains credential "type=\"password\"" "credential starts masked"

              Expect.isFalse
                  (credential.Contains("aria-label=\"safe-demo\""))
                  "secret value is not an accessible action name"

              let iconCredential =
                  fveCopyReveal {
                      _id "icon-credential"
                      _ariaLabel "Icon demo token"
                      _value "safe-icon-demo"
                      _iconButtons true
                  }
                  |> Render.toString

              Expect.stringContains iconCredential "aria-label=\"Reveal\"" "icon reveal action keeps an accessible name"
              Expect.stringContains iconCredential "aria-label=\"Copy\"" "icon copy action keeps an accessible name"

              Expect.equal
                  (Regex.Matches(iconCredential, "class=\"size-4\"").Count)
                  3
                  "icon actions render reveal, hide, and copy glyphs"

              let hierarchy =
                  fveTable {
                      _ariaLabel "Tree"
                      _dataSignals "{treeExpanded:true}"

                      fveTableBody {
                          fveTableRow {
                              fveTableHead {
                                  _scope "row"

                                  button {
                                      _type "button"
                                      _ariaLabel "Toggle Parent"
                                      _dataAttr ("aria-expanded", "$treeExpanded")
                                      _dataOn ("click", "$treeExpanded = !$treeExpanded")
                                      "Parent"
                                  }
                              }
                          }

                          fveTableRow {
                              _dataShow "$treeExpanded"
                              fveTableCell { "Child" }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains
                  hierarchy
                  "aria-label=\"Toggle Parent\""
                  "the application owns its native disclosure control"

              Expect.stringContains
                  hierarchy
                  "data-show=\"$treeExpanded\""
                  "Table preserves caller-owned hierarchy visibility"

              Expect.stringContains
                  hierarchy
                  "data-attr:aria-expanded=\"$treeExpanded\""
                  "disclosure state remains application-owned"

              let panels id label orientation =
                  fveResizable {
                      _id id
                      _ariaLabel label
                      _orientation orientation
                      _minSize 25
                      _maxSize 75
                      _initialSize 40
                      _collapsedSize 0
                      fveResizablePanel { "Files" }
                      fveResizablePanel { "Editor" }
                  }

              let resizable =
                  panels "workspace-panels" "Workspace panels" Orientation.Horizontal
                  |> Render.toString

              Expect.stringContains resizable "role=\"separator\"" "resizable layout exposes its handle as a separator"

              let resizablePageHtml =
                  View.documentWithPage Registry.navigation Components.resizableRegistration Components.resizablePage
                  |> Render.toHtmlDocString

              Expect.stringContains
                  resizablePageHtml
                  "fveResizablePanel"
                  "generated API documentation includes the native panel builder"

              Expect.isFalse
                  (resizablePageHtml.Contains("// opaque"))
                  "generated API documentation avoids internal representation jargon"

              Expect.stringContains
                  resizable
                  "aria-orientation=\"vertical\""
                  "horizontal panels expose a vertical separator"

              Expect.stringContains resizable "setPointerCapture" "pointer resizing captures the active pointer"
              Expect.stringContains resizable "ArrowLeft" "horizontal resizing supports directional keys"

              Expect.stringContains
                  resizable
                  "data-on:dblclick"
                  "collapsible panels expose an explicit collapse gesture"

              let verticalResizable =
                  panels "stacked-panels" "Stacked panels" Orientation.Vertical |> Render.toString

              Expect.stringContains
                  verticalResizable
                  "aria-orientation=\"horizontal\""
                  "vertical panels expose a horizontal separator"

              Expect.stringContains verticalResizable "ArrowUp" "vertical resizing supports directional keys"

              Expect.throws
                  (fun () ->
                      fveResizable {
                          _id "invalid-panels"
                          _ariaLabel "Invalid"
                          _minSize 80
                          _maxSize 20
                          fveResizablePanel { "One" }
                          fveResizablePanel { "Two" }
                      }
                      |> ignore)
                  "resizable bounds must be ordered"

              let tooltip =
                  fveTooltip {
                      yield _id ("help")
                      yield _description ("Supplementary help")

                      yield
                          (button {
                              _type "button"
                              _ariaDescribedby ("help" + "-content")
                              "Details"
                          })
                  }
                  |> Render.toString

              Expect.stringContains
                  tooltip
                  "aria-describedby=\"help-content\""
                  "tooltip supplements rather than replaces the trigger name"

              Expect.stringContains tooltip "role=\"tooltip\"" "tooltip content has the correct role"
              Expect.stringContains tooltip "popover=\"manual\"" "tooltip lifecycle is component-local"

              let popover =
                  fvePopover {
                      yield _id ("filters")

                      yield
                          fvePopoverTrigger {
                              _class
                                  "inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                              _ariaLabel ("Open filters")

                              fun (triggerAttributes: HtmlAttribute list) ->
                                  button {
                                      for attribute in triggerAttributes do
                                          attribute

                                      text "Filters"
                                  }

                          }

                      yield
                          fvePopoverContent {
                              yield _ariaLabel ("Account filters")

                              yield
                                  ((fveInput {
                                      _name "query"
                                      _ariaLabel "Query"
                                  })
                                  : HtmlElement)
                          }
                  }
                  |> Render.toString

              Expect.stringContains
                  popover
                  "popovertarget=\"filters-content\""
                  "popover trigger owns its content relationship"

              Expect.stringContains popover "role=\"dialog\"" "interactive popover content is a labelled dialog"
              Expect.stringContains popover "popover=\"auto\"" "popover uses top-layer light dismissal"

              Expect.stringContains
                  popover
                  "position-area:block-end span-inline-end"
                  "default popovers use a valid start-aligned logical anchor area"

              Expect.stringContains
                  popover
                  "inset:auto;margin:0.5rem 0"
                  "popover placement is owned next to its trigger instead of inherited from the top layer"

              let monthSelection =
                  fveMonthCalendarSelection {
                      yield _id ("dates")
                      yield _name ("startDate")
                      yield _ariaLabel ("Dates")
                      yield _mode CalendarSelectionMode.Range

                      yield
                          _value (
                              ((DateOnly(2026, 9, 14)): System.DateOnly)
                                  .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                          )

                      yield
                          _endValue (
                              ((DateOnly(2026, 9, 18)): System.DateOnly)
                                  .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                          )

                      yield _rangeEndName ("endDate")
                      yield _displayMonth ((DateOnly(2026, 9, 1)))

                      yield
                          _unavailableDates (
                              [ DateOnly(2026, 9, 16) ]
                              |> List.map (fun (date: System.DateOnly) ->
                                  date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
                          )
                  }
                  |> Render.toString

              Expect.stringContains monthSelection "name=\"startDate\"" "date ranges submit a canonical start value"
              Expect.stringContains monthSelection "name=\"endDate\"" "date ranges submit a canonical end value"
              Expect.stringContains monthSelection "value=\"2026-09-14\"" "date-only values do not include a timezone"

              Expect.isTrue
                  (Regex.IsMatch(monthSelection, "data-value=\"2026-09-16\"[^>]*disabled"))
                  "unavailable dates cannot be selected"

              Expect.stringContains monthSelection "PageUp" "date grids support keyboard month navigation"

              let captionMonthSelection =
                  fveMonthCalendarSelection {
                      yield _id ("birth-date")
                      yield _name ("birthDate")
                      yield _ariaLabel ("Birth date")

                      yield
                          _value (
                              ((DateOnly(2000, 6, 15)): System.DateOnly)
                                  .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                          )

                      yield _displayMonth ((DateOnly(2000, 6, 1)))

                      yield
                          _min (
                              ((DateOnly(1920, 1, 1)): System.DateOnly)
                                  .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                          )

                      yield
                          _max (
                              ((DateOnly(2026, 12, 31)): System.DateOnly)
                                  .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                          )

                      yield _captionLayout (MonthCalendarCaptionLayout.Dropdown)
                  }
                  |> Render.toString

              Expect.stringContains
                  captionMonthSelection
                  "aria-label=\"Month\""
                  "dropdown captions expose a labelled month selector"

              Expect.stringContains
                  captionMonthSelection
                  "aria-label=\"Year\""
                  "dropdown captions expose a labelled year selector"

              Expect.stringContains
                  captionMonthSelection
                  "value=\"1920\""
                  "bounded dropdown captions begin at the minimum year"

              Expect.stringContains
                  captionMonthSelection
                  "value=\"2026\""
                  "bounded dropdown captions end at the maximum year"

              Expect.stringContains captionMonthSelection " p-3 " "selection calendars own compact padding"

              let datePicker =
                  fveField {
                      yield
                          fveLabel {
                              yield _id ("due" + "-label")
                              yield _for ("due")
                              yield ("Due date": string)
                          }

                      yield
                          fveFieldContent {
                              yield
                                  (fveDatePicker {
                                      yield _id ("due")
                                      yield _name ("dueDate")
                                      yield _ariaDescribedby (String.concat " " [ ("due" + "-validation") ])
                                      yield _ariaInvalid true
                                  }
                                  : HtmlElement)
                          }

                      yield
                          fveFieldError {
                              yield _id ("due" + "-validation")
                              yield _role "alert"
                              yield ("Choose a due date.": string)
                          }
                  }
                  |> Render.toString

              Expect.stringContains
                  datePicker
                  "for=\"due\""
                  "the caller's native label targets the actual picker button"

              Expect.isTrue
                  (Regex.IsMatch(datePicker, "<button[^>]*id=\"due\""))
                  "the public ID belongs to the native control"

              Expect.stringContains datePicker "aria-invalid=\"true\"" "date picker validation reaches the trigger"

              Expect.stringContains
                  datePicker
                  "popovertarget=\"due-popover-content\""
                  "date picker composes the public popover"

              Expect.throws
                  (fun () ->
                      fveMonthCalendarSelection {
                          yield _id ("invalid-range")
                          yield _name ("date")
                          yield _ariaLabel ("Dates")
                          yield _mode CalendarSelectionMode.Range

                          yield
                              _value (
                                  ((DateOnly(2026, 9, 20)): System.DateOnly)
                                      .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                              )

                          yield
                              _endValue (
                                  ((DateOnly(2026, 9, 10)): System.DateOnly)
                                      .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                              )
                      }
                      |> ignore)
                  "reversed date ranges are rejected"

              let customField =
                  fveField {
                      fveLabel {
                          _for "amount"
                          "Amount"
                      }

                      input {
                          _id "amount"
                          _name "amount"
                          _type "number"
                          _ariaInvalid true
                          _ariaDescribedby "amount-description amount-validation"
                      }

                      fveFieldDescription {
                          _id "amount-description"
                          "Before fees"
                      }

                      fveFieldError {
                          _id "amount-validation"
                          "Enter an amount."
                      }
                  }
                  |> Render.toString

              Expect.equal
                  (Regex.Matches(customField, "<label ").Count)
                  1
                  "custom controls compose with exactly one label"

              Expect.stringContains
                  customField
                  "aria-describedby=\"amount-description amount-validation\""
                  "field relationships are native attributes"

              let fieldGroup =
                  fveFieldGroup {
                      fveFieldLegend { "Preferences" }
                      "Controls"
                  }
                  |> Render.toString

              Expect.stringContains fieldGroup "<fieldset" "related fields use native fieldsets"
              Expect.stringContains fieldGroup "<legend" "field groups own their legend"

              let groupedInput =
                  fveInputGroup {
                      fveInputGroupAddon {
                          _ariaHidden true
                          "$"
                      }

                      fveInput {
                          _name "amount"
                          _value "125"
                          _ariaLabel "Amount"
                      }

                      fveInputGroupAddon {
                          _position InputGroupPosition.InlineEnd

                          button {
                              _type "button"
                              _ariaLabel "Calculate"
                              "="
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains
                  groupedInput
                  "aria-hidden=\"true\""
                  "decorative addons remain outside the accessibility tree"

              Expect.stringContains groupedInput "name=\"amount\"" "the native control owns its form value"

              Expect.stringContains
                  groupedInput
                  "aria-label=\"Calculate\""
                  "interactive addons retain their accessible name"

              let message =
                  fveMessage {
                      yield _label ("Alex")
                      yield ((text "Ready for review"))
                      yield _side (MessageSide.Sender)
                      yield fveMessageMetadata { yield ("09:12") }
                      yield fveMessageStatus { yield ("Delivered") }
                  }
                  |> Render.toString

              Expect.stringContains message "aria-label=\"Message from Alex\"" "message sender remains explicit"

              Expect.stringContains
                  message
                  ">Delivered</span>"
                  "delivery status stays visible without owning delivery policy"

              let cards =
                  fveChoiceCards {
                      _id "preferences"
                      fveFieldLegend { "Preferences" }

                      fveChoiceCardsContent {
                          fveChoiceCard {
                              fveCheckbox {
                                  _name "preferences"
                                  _value "email"
                                  _checked true
                              }

                              fveChoiceCardContent {
                                  fveChoiceCardTitle { "Email" }
                                  fveChoiceCardDescription { "Weekly summary" }
                              }
                          }

                          fveChoiceCard {
                              fveCheckbox {
                                  _name "preferences"
                                  _value "sms"
                                  _disabled true
                              }

                              fveChoiceCardTitle { "Text message" }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains cards "type=\"checkbox\"" "multiple cards remain native checkboxes"
              Expect.stringContains cards "name=\"preferences\"" "cards retain native form names"

              Expect.isTrue
                  (Regex.IsMatch(cards, "<input[^>]*value=\"email\"[^>]* checked"))
                  "selected cards submit their value"

              Expect.isTrue
                  (Regex.IsMatch(cards, "<input[^>]*value=\"sms\"[^>]* disabled"))
                  "disabled cards remain unavailable"

              let tags =
                  fveTagInput {
                      _id "tags"
                      _name "tags"
                      _ariaLabel "Tags"
                      _values [ "reviewed" ]
                  }
                  |> Render.toString

              Expect.stringContains tags "<fve-tag-input" "the light-DOM owner supplies local editing"

              Expect.stringContains
                  tags
                  "type=\"hidden\" name=\"tags\" value=\"reviewed\""
                  "initial tags are successful native controls before JavaScript"

              Expect.stringContains tags "aria-label=\"Remove reviewed\"" "removal has a native accessible name"
              Expect.isFalse (tags.Contains("<label")) "the application owns labels and field feedback"

              let calendar = Components.calendarWeekExample |> Render.toString
              Expect.stringContains calendar "data-view=\"week\"" "calendar renders one focused view"

              Expect.isFalse
                  (calendar.Contains("calendar view"))
                  "component examples do not embed connected view navigation"

              Expect.stringContains
                  calendar
                  "Overlaps the trail lesson by one hour"
                  "calendar retains consumer-authored overlap context"

              let loadingCalendar =
                  fveMonthCalendar {
                      yield _label ("Schedule")
                      yield _date ((DateOnly(2026, 9, 17)))
                      yield! ([]: CalendarEvent<string> list)
                      yield _state CalendarState.Loading
                  }
                  |> Render.toString

              Expect.stringContains loadingCalendar "role=\"status\"" "loading calendar announces its state"
              Expect.stringContains loadingCalendar "Loading calendar…" "loading calendar remains readable"

              let errorCalendar =
                  fveMonthCalendar {
                      yield _label ("Schedule")
                      yield _date ((DateOnly(2026, 9, 17)))
                      yield! ([]: CalendarEvent<string> list)
                      yield _state (CalendarState.Error("Schedule failed."))
                  }
                  |> Render.toString

              Expect.stringContains errorCalendar "role=\"alert\"" "calendar errors are urgent feedback"

              Expect.stringContains
                  errorCalendar
                  "Schedule failed."
                  "calendar errors preserve consumer-authored messages"

              let unavailableCalendar =
                  fveMonthCalendar {
                      yield _label ("Schedule")
                      yield _date ((DateOnly(2026, 9, 17)))
                      yield! ([]: CalendarEvent<string> list)
                      yield _state (CalendarState.Unavailable("Schedule unavailable."))
                  }
                  |> Render.toString

              Expect.stringContains unavailableCalendar "Schedule unavailable." "unavailable calendar state is explicit"

              let media = Components.mediaLibraryExample |> Render.toString

              Expect.stringContains
                  media
                  "name=\"assetIds\" value=\"social-card\""
                  "media selection uses native repeated controls"

              Expect.stringContains
                  media
                  "alt=\"FSharp.ViewEngine — Typed HTML views for F#\""
                  "media thumbnails require consumer-authored alternatives"

              Expect.stringContains
                  media
                  "fve-selection-change"
                  "media selection publishes stable identities to shared bulk actions"

              Expect.stringContains
                  media
                  "No media assets. Upload an image to begin."
                  "media example includes its empty state"

              Expect.stringContains
                  media
                  "Media could not be loaded."
                  "media example includes its recoverable error state"
          }

          test "Calendar dates, overlap lanes and unavailable states are derived from typed input" {
              let day = DateOnly(2024, 2, 29)

              let event id start finish =
                  CalendarEvent.create id id day ("/events/" + id)
                  |> CalendarEvent.withTime start finish

              let events =
                  [ event "first" (TimeOnly(9, 0)) (TimeOnly(10, 0))
                    event "overlap" (TimeOnly(9, 30)) (TimeOnly(11, 0))
                    event "connected" (TimeOnly(10, 0)) (TimeOnly(12, 0))
                    event "adjacent" (TimeOnly(12, 0)) (TimeOnly(12, 30))
                    CalendarEvent.create "all-day" "All-day event" day "/events/all-day"
                    CalendarEvent.create "outside" "Outside range" (day.AddMonths 2) "/events/outside" ]

              let calendar (state: CalendarState) =
                  fveMonthCalendar {
                      _label "Calendar"
                      _date day
                      _state state
                      yield! events

                      fveCalendarToday {
                          _date day
                          _href "/today"
                      }

                      _selectedDate day
                  }

              let month = calendar CalendarState.Ready |> Render.toString
              Expect.stringContains month "data-date=\"2024-01-29\"" "month starts on the preceding Monday"
              Expect.stringContains month "data-date=\"2024-03-03\"" "month ends on the trailing Sunday"

              Expect.equal
                  (System.Text.RegularExpressions.Regex.Matches(month, "data-date=").Count)
                  35
                  "February leap month has complete weeks"

              Expect.stringContains month "data-date=\"2024-02-29\"" "leap day is an actual date"
              Expect.stringContains month "aria-current=\"date\"" "today is semantic, not inferred from server clock"
              Expect.isFalse (month.Contains("Outside range")) "events outside visible dates are excluded"

              let timedDay =
                  fveDayCalendar {
                      yield _label ("Timed events")
                      yield _date (day)
                      yield! (events: CalendarEvent<string> list)
                  }
                  |> Render.toString

              Expect.stringContains
                  timedDay
                  "--fve-calendar-lane:2;--fve-calendar-lanes:2"
                  "overlap has a separate lane"

              Expect.stringContains
                  timedDay
                  "--fve-calendar-start:121;--fve-calendar-duration:120;--fve-calendar-lane:1;--fve-calendar-lanes:2"
                  "connected overlap reuses the ended first lane"

              Expect.stringContains
                  timedDay
                  "--fve-calendar-start:241;--fve-calendar-duration:30;--fve-calendar-lane:1;--fve-calendar-lanes:1"
                  "adjacent non-overlap regains full width"

              let start = DateOnly(2026, 9, 18)

              let range =
                  CalendarEvent.create "range" "Training" start "/events/range"
                  |> CalendarEvent.withEndDate (start.AddDays 3)

              for offset in [ -1 .. 4 ] do
                  let html =
                      fveDayCalendar {
                          yield _label ("Range day")
                          yield _date ((start.AddDays offset))
                          yield! ([ range ]: CalendarEvent<string> list)
                      }
                      |> Render.toString

                  Expect.equal
                      (html.Contains("data-event=\"range\""))
                      (offset >= 0 && offset <= 3)
                      "all-day coverage includes both ends and excludes adjacent dates"

              let rangedMonth =
                  fveMonthCalendar {
                      yield _label ("Range month")
                      yield _date (start)
                      yield! ([ range ]: CalendarEvent<string> list)
                  }
                  |> Render.toString

              Expect.equal
                  (Regex.Matches(rangedMonth, "<a[^>]*href=\"/events/range\"").Count)
                  2
                  "a Friday-to-Monday range splits into two week segments"

              Expect.throws
                  (fun () -> range |> CalendarEvent.withTime (TimeOnly(9, 0)) (TimeOnly(10, 0)) |> ignore)
                  "multi-day events cannot become timed events"

              Expect.throws
                  (fun () -> range |> CalendarEvent.withEndDate (start.AddDays -1) |> ignore)
                  "range ends cannot precede their start"

              let december =
                  fveMonthCalendar {
                      yield _label ("Year end")
                      yield _date ((DateOnly(2026, 12, 1)))
                      yield! ([]: CalendarEvent<string> list)
                  }
                  |> Render.toString

              Expect.stringContains december "data-date=\"2027-01-03\"" "month geometry crosses the year boundary"

              let nextDay =
                  fveDayCalendar {
                      yield _label ("Next day")
                      yield _date ((day.AddDays 1))
                      yield! (events: CalendarEvent<string> list)
                  }
                  |> Render.toString

              Expect.isFalse (nextDay.Contains("data-event=")) "day filters events by actual date"
              Expect.stringContains nextDay "No events in this range." "filtered empty state is truthful"

              for unavailable in
                  [ CalendarState.Loading
                    CalendarState.Error "Failed"
                    CalendarState.Unavailable "Unavailable" ] do
                  let html = calendar unavailable |> Render.toString
                  Expect.isFalse (html.Contains("data-event=")) "unavailable calendars never expose stale event actions"

              Expect.throws
                  (fun () -> event "bad" (TimeOnly(12, 0)) (TimeOnly(11, 0)) |> ignore)
                  "overnight/reversed intervals need consumer splitting"

              Expect.throws
                  (fun () ->
                      fveDayCalendar {
                          yield _label ("Duplicates")
                          yield _date (day)
                          yield! ([ events.Head; events.Head ]: CalendarEvent<string> list)
                      }
                      |> ignore)
                  "duplicate event IDs are rejected"
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

              Expect.equal
                  (Regex.Matches(year, "data-fve-month-calendar=\"compact\"").Count)
                  12
                  "year renders twelve shared compact months"

              Expect.equal
                  (Regex.Matches(year, "role=\"gridcell\"").Count)
                  441
                  "2026 has nine five-week months and three six-week months"

              Expect.isFalse (year.Contains("<input")) "display months never create form inputs"
              Expect.isFalse (year.Contains("Previous month")) "year owns navigation rather than twelve month controls"
              Expect.stringContains year "aria-label=\"September 2026 dates\"" "year months retain labelled date grids"

              Expect.stringContains
                  year
                  "2 events: Coastal trail lesson, Beginner riding camp"
                  "year date destinations summarize matching events"

              for registration in
                  [ Components.dayCalendarRegistration
                    Components.weekCalendarRegistration
                    Components.monthCalendarRegistration
                    Components.yearCalendarRegistration ] do
                  Expect.equal registration.category "Components" "calendar components are shared data display"

              let compact = Components.compactMonthCalendar |> Render.toString

              Expect.equal
                  (Regex.Matches(compact, "role=\"gridcell\"").Count)
                  42
                  "standalone compact month shares the year geometry"

              Expect.isFalse (compact.Contains("<input")) "compact display does not implicitly submit a date"
          }

          test "Error summaries use exact focus targets and encode corrective text" {
              let html =
                  fveErrorSummary {
                      _id "validation-errors"
                      _label "Check details"
                      _autofocus true

                      fveErrorSummaryItem {
                          _href "#contact%3Aemail"
                          "Email: Use <name@fve.meiermade.com>."
                      }
                  }
                  |> Render.toString

              Expect.stringContains html "role=\"alert\"" "one summary announcement"
              Expect.stringContains html "href=\"#contact%3Aemail\"" "fragment safely encodes the exact control ID"
              Expect.stringContains html "data-init=\"el.focus()\"" "focus is explicitly opt-in at insertion"
              Expect.stringContains html "Email: Use &lt;name@fve.meiermade.com&gt;." "corrective content is encoded"

              Expect.equal
                  (fveErrorSummary {
                      _id "errors"
                      _label "Errors"
                   }
                   |> Render.toString)
                  ""
                  "empty summaries do not create an empty alert"
          }

          test "Notice color and presentation do not imply announcement urgency" {
              let notice (variant: NoticeVariant) announcement =
                  fveNotice {
                      _id "feedback"
                      _label "Check details"
                      _color Color.Error
                      _variant variant
                      _announcement announcement
                      p { "Review the account." }

                      fveNoticeActions {
                          a {
                              _href "/components/notice"
                              "Read guidance"
                          }
                      }
                  }

              for variant in
                  [ NoticeVariant.Solid
                    NoticeVariant.Soft
                    NoticeVariant.Outline
                    NoticeVariant.Ghost ] do
                  let staticHtml = notice variant LiveAnnouncement.Static |> Render.toString
                  Expect.isFalse (staticHtml.Contains("role=\"alert\"")) "static notices do not announce"
                  Expect.isFalse (staticHtml.Contains("role=\"status\"")) "presentation does not opt into live regions"
                  let polite = notice variant LiveAnnouncement.Polite |> Render.toString
                  Expect.stringContains polite "role=\"status\"" "polite completion is independent of presentation"
                  let urgent = notice variant LiveAnnouncement.Assertive |> Render.toString
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

              Expect.throws
                  (fun () ->
                      fveNotice {
                          _id "notice"
                          _label " "
                          p { "Content" }
                      }
                      |> ignore)
                  "title is meaningful"
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
              Expect.equal (routeStatus "/components/link") 200 "Link owns a real documentation route"

              Expect.isTrue
                  (Components.actionRegistrations
                   |> List.exists (fun page -> page.path = "/components/link"))
                  "Link is discoverable under Actions"

              Expect.isTrue
                  (Catalog.componentRegistrations
                   |> List.exists (fun page -> page.path = "/components/link"))
                  "Link is an independently documented catalog component"

              let aliases = Registry.aliases |> Map.ofList

              Expect.isFalse (Map.containsKey "/components/section" aliases) "retired section alias is not registered"

              for retired in
                  [ "/components/upload"
                    "/components/upload-list"
                    "/components/first-steps"
                    "/components/contract"
                    "/components/layouts"
                    "/components/row-actions"
                    "/components/action-cluster"
                    "/components/confirmation-dialog"
                    "/components/status"
                    "/components/icon-button" ] do
                  Expect.isFalse (Map.containsKey retired aliases) $"{retired} has no replacement alias"
                  Expect.equal (routeStatus retired) 404 $"{retired} is retired"
          }

          test "Registered pages render complete canonical documents with resolvable local references" {
              let references = Collections.Generic.HashSet<string>(StringComparer.Ordinal)

              for page in Registry.all do
                  let html = page |> View.document Registry.navigation |> Render.toHtmlDocString

                  let canonical =
                      "https://fve.meiermade.com" + (if page.path = "/" then "/" else page.path)

                  Expect.equal (routeStatus page.path) 200 $"{page.path} canonical route"
                  Expect.stringStarts html "<!DOCTYPE html>" $"{page.path} complete document"
                  Expect.stringContains html "<main" $"{page.path} semantic main"
                  Expect.stringContains html $"rel=\"canonical\" href=\"{canonical}\"" $"{page.path} canonical"

                  for matched in Regex.Matches(html, @"\b(?:href|src|data-docs-preview-src)=""([^""]+)""") do
                      let reference = WebUtility.HtmlDecode(matched.Groups[1].Value)

                      if
                          not (String.IsNullOrWhiteSpace reference)
                          && not (reference.StartsWith('#'))
                          && not (reference.StartsWith("mailto:"))
                          && not (reference.StartsWith("tel:"))
                      then
                          let uri = Uri(Uri("https://fve.meiermade.com" + page.path), reference)

                          if uri.GetLeftPart(UriPartial.Authority) = "https://fve.meiermade.com" then
                              references.Add uri.PathAndQuery |> ignore

              let unresolved =
                  references |> Seq.filter (fun reference -> referenceStatus reference >= 400) |> Seq.toList

              Expect.isEmpty unresolved "rendered local references resolve"

              Expect.isGreaterThan
                  references.Count
                  expectedPaths.Count
                  "rendered pages expose additional local assets and destinations"

              Expect.equal (referenceStatus "/not-a-documentation-route") 404 "unknown references remain unresolved"
          }

          test "Navigation separates Components from the three source-authored templates" {
              Expect.sequenceEqual
                  (Registry.navigation |> List.map _.label)
                  [ "Getting started"
                    "Core concepts"
                    "Integrations"
                    "Components"
                    "Project"
                    "Examples" ]
                  "public information architecture"

              let components =
                  Registry.navigation |> List.find (fun section -> section.label = "Components")

              Expect.sequenceEqual
                  (components.sections |> List.map _.label)
                  [ "Guides"
                    "Actions"
                    "Feedback"
                    "Data display"
                    "Form controls"
                    "Navigation"
                    "Overlays"
                    "Layout"
                    "Code and diagrams" ]
                  "focused component groups"

              let examples =
                  Registry.navigation |> List.find (fun section -> section.label = "Examples")

              Expect.sequenceEqual
                  (examples.pages |> List.map _.path)
                  [ "/examples" ]
                  "one template gallery, not one gallery entry per inner page"

              Expect.sequenceEqual
                  (Examples.templates |> List.map _.path)
                  [ "/examples/specification"
                    "/examples/application"
                    "/examples/api-documentation" ]
                  "exactly three cohesive examples"

              for retired in
                  [ "Primitives"
                    "Application"
                    "Documentation"
                    "Page examples"
                    "Integration examples" ] do
                  Expect.isFalse
                      (components.sections |> List.exists (fun section -> section.label = retired))
                      "framework categories are not component navigation"
          }

          test "Catalog families have unique ownership, honest indexes and registry-derived pagers" {
              let families = Catalog.navigation

              Expect.sequenceEqual
                  (families |> List.map _.label)
                  [ "Actions"
                    "Feedback"
                    "Data display"
                    "Form controls"
                    "Navigation"
                    "Overlays"
                    "Layout"
                    "Code and diagrams" ]
                  "component groups"

              let rec flatten (group: NavSection) =
                  group.pages @ (group.sections |> List.collect flatten)

              let familyPages = families |> List.collect flatten

              Expect.equal
                  (familyPages |> List.map _.path |> Set.ofList |> Set.count)
                  familyPages.Length
                  "each family route occurs exactly once"

              for family in families do
                  for page in flatten family do
                      Expect.equal page.category "Components" $"{page.path} metadata agrees with navigation ownership"

              let root =
                  Catalog.overviewPage
                  |> FSharp.ViewEngine.Components.Templates.DocsView.content
                  |> Render.toString

              for family in families do
                  Expect.stringContains
                      root
                      ($"href=\"{family.pages.Head.path}\"")
                      "each area has a working root-index link"

              for index, page in List.indexed Registry.all do
                  let html = View.document Registry.navigation page |> Render.toString

                  let pager =
                      Regex
                          .Match(html, "<nav aria-label=\"Page navigation\"[^>]*>.*?</nav>", RegexOptions.Singleline)
                          .Value

                  let expectedLinks =
                      [ if index > 0 then
                            yield "prev", Registry.all[index - 1].path
                        if index + 1 < Registry.all.Length then
                            yield "next", Registry.all[index + 1].path ]

                  for relation, path in expectedLinks do
                      Expect.stringContains
                          pager
                          ($"rel=\"{relation}\" href=\"{path}\"")
                          $"{page.path} pager follows the registry"

                  Expect.equal (Regex.Matches(pager, "<a ").Count) expectedLinks.Length "no extra pager destination"
          }

          test "Catalog release discovery uses external links without maintained history" {
              let html = View.document Registry.navigation Home.page |> Render.toString

              for id, label, href in
                  [ "changelog", "Changelog", Registry.releasesUrl
                    "packages", "Packages", Registry.packagesUrl ] do
                  let link =
                      Regex
                          .Match(html, $"<a\\b[^>]*id=\"nav-{id}\"[^>]*>.*?</a>", RegexOptions.Singleline)
                          .Value

                  Expect.stringContains link $"href=\"{WebUtility.HtmlEncode href}\"" "genuine external destination"
                  Expect.stringContains link "target=\"_blank\"" "new tab"
                  Expect.stringContains link "rel=\"noopener\"" "safe opener"
                  Expect.stringContains link "title=\"Opens in a new tab\"" "new-tab hint"
                  Expect.stringContains link label "visible label"
                  Expect.stringContains link "<svg" "external-link icon"
                  Expect.isFalse (link.Contains "data-docs-nav-link") "not intercepted by internal navigation"

              Expect.isFalse (expectedPaths.Contains "/changelog") "maintained history is retired"
              Expect.equal (routeStatus "/changelog") 404 "retired history route is not maintained"
          }

          test "Public discovery contains canonical pages and excludes aliases and previews" {
              let sitemap = Handler.sitemap
              let robots = Handler.robots

              for page in Registry.all do
                  Expect.stringContains sitemap $"<loc>https://fve.meiermade.com{page.path}</loc>" page.path

              Expect.equal
                  (sitemap.Split("<url>").Length - 1)
                  (Registry.all.Length + Examples.paths.Length)
                  "one sitemap entry per canonical page"

              Expect.isFalse (sitemap.Contains("/docs/components</loc>")) "aliases are excluded"
              Expect.isFalse (sitemap.Contains("/docs/previews/")) "previews are excluded"
              Expect.stringContains robots "Allow: /" "public pages are crawlable"
              Expect.stringContains robots "Sitemap: https://fve.meiermade.com/sitemap.xml" "robots advertises sitemap"
          }

          test "Pages publish the application-owned social image" {
              for page in Registry.all do
                  let html = page |> View.document Registry.navigation |> Render.toHtmlDocString

                  Expect.stringContains
                      html
                      "property=\"og:image\" content=\"https://fve.meiermade.com/social-card.png\""
                      page.path

                  Expect.stringContains html "name=\"twitter:card\" content=\"summary_large_image\"" page.path
          }

          test "Every page renders its semantic heading and legacy anchors" {
              for page in Registry.all do
                  let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                  let encodedTitle = WebUtility.HtmlEncode page.title
                  Expect.stringContains html $">{encodedTitle}</h1>" $"{page.path} title"

                  if
                      Catalog.tryPage page.path
                      |> Option.orElseWith (fun () -> ComponentDocumentation.tryPage page.path)
                      |> Option.orElseWith (fun () ->
                          if page.path = Examples.registration.path then
                              Some Examples.gallery
                          else
                              None)
                      |> Option.orElseWith (fun () -> Components.tryPage page.path)
                      |> Option.isNone
                  then
                      let description =
                          page.nodes
                          |> List.tryPick (function
                              | Paragraph content -> Some content
                              | _ -> None)

                      Expect.isSome description $"{page.path} has a useful page summary"

                      Expect.isFalse
                          (html.Contains($">{page.category}</p>"))
                          $"{page.path} does not repeat its category as the description"

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

                  Expect.isFalse
                      (html.Contains("type=\"range\""))
                      $"{page.path} does not use the retired nested range control"
          }

          test "Code examples are encoded and retain Prism language classes" {
              let html =
                  Custom.page |> View.document Registry.navigation |> Render.toHtmlDocString

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

          test "Docs loads its installed components through one Rocket application entry" {
              let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
              Expect.stringContains html "src=\"/assets/app.js\"" "host-owned bundled component entry"

              Expect.isFalse
                  (html.Contains "src=\"/scripts/datastar.1.0.4.js\"")
                  "ordinary Datastar is not loaded alongside Rocket"

              Expect.stringContains html "type=\"module\"" "application ES module"
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

              Expect.stringContains
                  html
                  "rel=\"stylesheet\" href=\"/css/output.css\""
                  "the repository host links its compiled stylesheet"

              Expect.isFalse (html.Contains("fonts.googleapis.com")) "the package makes no Google Fonts request"
              Expect.isFalse (html.Contains("fonts.gstatic.com")) "the package ships no external font source"
          }

          test "Showcase isolates only complete styled documents" {
              Expect.isFalse
                  (Showcase.previewRoutes.ContainsKey "/docs/previews/workflow-states")
                  "state tabs render directly with host styles"

              Expect.isFalse
                  (Showcase.previewRoutes.ContainsKey "/docs/previews/browser-frame")
                  "browser frames render directly without duplicate framing"

              for path, document in Showcase.previewRoutes |> Map.toSeq do
                  Expect.stringStarts document "<!DOCTYPE html>" $"{path} is a complete HTML document"
                  Expect.isFalse (document.Contains("<style")) $"{path} does not embed the Docs component styles"

                  Expect.stringContains
                      document
                      "rel=\"stylesheet\" href=\"/css/output.css\""
                      $"{path} links the compiled consumer stylesheet"

                  Expect.stringContains
                      document
                      "data-docs-shell=\"true\""
                      $"{path} initializes the Documentation shell"

                  for themeClass in [ "fve-components"; "fve-theme-sky"; "fve-density-compact" ] do
                      Expect.stringContains document themeClass $"{path} uses shared component theme class {themeClass}"
          }

          test "Showcase source regions preserve the exact compiled example" {
              let source =
                  """before
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

          test "Calendar marks today by default without selecting it or adding a shortcut" {
              let render (attributes: HtmlAttribute list) =
                  fveMonthCalendarSelection {
                      _id "current-date-contract"; _name "date"; _ariaLabel "Date"
                      for attribute in attributes do attribute
                  } |> Render.toString
              let marker html = Regex.Matches(html, "<button(?=[^>]*role=\"gridcell\")(?=[^>]*aria-current=\"date\")[^>]*>")
              let plain = render []
              Expect.equal (marker plain).Count 1 "the default current month marks today's cell"
              Expect.stringContains (marker plain).[0].Value (DateTime.Today.ToString("yyyy-MM-dd")) "the default uses the local rendering date"
              Expect.isFalse (plain.Contains "Select today,") "marking today does not add a shortcut"
              let leapDay = render [_currentDate (DateOnly(2024, 2, 29))]
              Expect.stringContains (marker leapDay).[0].Value "2024-02-29" "the caller can override the current date without a shortcut"
              Expect.stringContains (marker leapDay).[0].Value "aria-selected=\"false\"" "today is not implicitly selected"
              Expect.stringContains leapDay "value=\"\"" "today does not populate the submitted value"
              Expect.isFalse (Regex.IsMatch(leapDay, "<section[^>]*current-date=")) "configuration does not leak into native attributes"
          }

          test "Calendar Today shortcut is explicit, independent of Clear and constrained like dates" {
              let today = DateOnly(2024, 2, 29)
              let render (attributes: HtmlAttribute list) =
                  fveMonthCalendarSelection {
                      _id "today-contract"; _name "date"; _ariaLabel "Date"
                      _today today
                      for attribute in attributes do attribute
                  } |> Render.toString
              let plain = fveMonthCalendarSelection { _id "plain-contract"; _name "date"; _ariaLabel "Date" } |> Render.toString
              Expect.isFalse (plain.Contains "Select today,") "the shortcut is opt-in"
              let html = render []
              Expect.stringContains html "February 2024" "an empty selection opens on the caller-supplied current month"
              Expect.stringContains html "data-value=\"2024-02-29\"" "the shortcut retains its exact DateOnly, including leap day"
              Expect.isFalse (html.Contains ">Clear</button>") "Today does not imply Clear"
              let both = render [_clearable true]
              Expect.stringContains both ">Clear</button>" "the contextual action has the concise label"
              Expect.isLessThan (both.IndexOf ">Clear</button>") (both.IndexOf "Select today,") "Clear precedes Today in logical footer order"
              for attributes, disabled in
                  [ [_min "2024-03-01"], true
                    [_max "2024-02-28"], true
                    [_unavailableDates ["2024-02-29"]], true
                    [_readonly true], true
                    [_disabled true], true
                    [_ariaBusy true], false ] do
                  let markup = render attributes
                  let button = Regex.Match(markup, "<button(?=[^>]*aria-label=\"Select today,)[^>]*>").Value
                  Expect.isNotEmpty button "the configured shortcut remains present"
                  Expect.equal (Regex.IsMatch(button, @"\bdisabled(?:=|\s|>)")) disabled "availability is independent of busy state"
              let caption = render [_value "1500-01-01"; _captionLayout MonthCalendarCaptionLayout.Dropdown]
              Expect.stringContains caption "value=\"2024\"" "the dropdown year window includes a valid Today shortcut outside its initial century"
              let range = render [_mode CalendarSelectionMode.Range; _rangeEndName "end"]
              Expect.stringContains range "name=\"end\"" "range submission keeps both native values"
          }

          test "Popover and Dropdown menu teach distinct content and menu compositions" {
              let popovers = Components.examplesFor "popover"
              Expect.sequenceEqual (popovers |> List.map _.title)
                  ["Content panel"; "With form"; "Side and alignment"; "Custom native trigger"]
                  "ordinary Button triggers and content precede the native factory"
              for example in popovers |> List.take 3 do
                  Expect.stringContains example.source "fvePopoverTrigger { fveButton" "basic recipes reuse Button"
                  Expect.isFalse (example.source.Contains "triggerAttributes") "standard examples do not recreate native Button presentation"
              let menus = Components.examplesFor "dropdown-menu" |> List.map _.title
              Expect.sequenceEqual (menus |> List.skip 1 |> List.take 6)
                  ["Icons"; "Keyboard hints"; "Checkboxes"; "Radio choices"; "Disabled items"; "Destructive action"]
                  "focused capabilities precede complex recipes"
              Expect.equal menus[menus.Length - 1] "Row actions" "record composition stays last"
              for path, linked in ["/components/popover", "/components/dropdown-menu"; "/components/dropdown-menu", "/components/popover"] do
                  let _, html = routeResponse path
                  Expect.stringContains html ($"href=\"{linked}\"") "usage links to the other interaction pattern"
          }

          test "Button documentation leads with sizes, colors and variants before compositions" {
              let examples = Components.examplesFor "button"
              Expect.sequenceEqual (examples |> List.skip 1 |> List.take 3 |> List.map _.title)
                  ["Sizes"; "Colors"; "Variants"] "fundamentals precede state and composition recipes"
              let sizes = examples |> List.find (fun example -> example.title = "Sizes")
              let html = Render.toString sizes.preview
              for value in ["extra-small"; "small"; "medium"; "large"; "extra-large"] do
                  Expect.stringContains html ($"size=\"{value}\"") "each shared size is demonstrated"
              Expect.stringContains sizes.source "_iconOnly true" "the copied recipe teaches square controls without padding hacks"
              for key in ["button"; "dropdown-menu"; "table"] do
                  let titles = Components.examplesFor key |> List.map _.title
                  Expect.isGreaterThan (titles |> List.findIndex ((=) "Row actions")) 3 "row actions follow basic component capabilities"
          }

          test "Input examples render individual native fields and templates submit complete forms" {
              let examples = Components.examplesFor "input"

              let sizing =
                  examples
                  |> List.find (fun example -> example.title = "Consistent control sizes")

              Expect.equal
                  (Regex
                      .Matches(Render.toString sizing.preview, "<input(?=[^>]*type=\"search\")(?=[^>]*name=\")[^>]*>")
                      .Count)
                  6
                  "five inherited sizes and one explicit override"

              for example in
                  examples
                  |> List.filter (fun example -> example.title <> "Consistent control sizes") do
                  Expect.equal
                      (Regex.Matches(Render.toString example.preview, "<input ").Count)
                      1
                      "one native input per example"

              let _, accountForm = routeResponse "/examples/application/accounts/new"
              Expect.stringContains accountForm "name=\"name\"" "the application template owns the account name field"
              Expect.stringContains accountForm "method=\"post\"" "template submission is a real server action"
          }

          test "Field wrappers top-align content and keep Select help below the control" {
              for field in
                  [ fveField {
                        fveLabel {
                            _for ("aligned-input")
                            "Name"
                        }

                        fveInput {
                            _id ("aligned-input")
                            _name ("aligned-input")
                        }
                    }
                    fveField {
                        fveLabel {
                            _for ("aligned-textarea")
                            "Notes"
                        }

                        fveTextarea {
                            _id ("aligned-textarea")
                            _name ("aligned-textarea")
                        }
                    }
                    fveField {
                        fveLabel { _for "aligned-file"; "Files" }
                        fveFileSelection {
                            _id "aligned-file"
                            _name "files"
                        }
                    }
                    fveTagInput {
                        _id "aligned-tags"
                        _name "tags"
                        _ariaLabel "Tags"
                    } ] do
                  Expect.stringContains
                      (Render.toString field)
                      "content-start"
                      "a taller sibling must not stretch label and control rows"

              let config =
                  fveField {
                      fveLabel {
                          _for ("aligned-select")
                          "Choice"
                      }

                      fveSelect {
                          _id ("aligned-select")
                          _name ("aligned-select")
                          _ariaInvalid (Option.isSome (Some("Check this choice.")))

                          _ariaDescribedby (
                              String.concat
                                  " "
                                  [ ("aligned-select") + "-description"
                                    if Option.isSome (Some("Check this choice.")) then
                                        ("aligned-select") + "-validation" ]
                          )

                          fveSelectContent {
                              for value, label, disabled in ([ ("a", "A", false) ]) do
                                  fveSelectItem {
                                      _value (value)
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }

                      fveFieldDescription {
                          _id (("aligned-select") + "-description")
                          "Choose an option."
                      }

                      match Some("Check this choice.") with
                      | Some message ->
                          fveFieldError {
                              _id (("aligned-select") + "-validation")
                              message
                          }
                      | None -> ()
                  }

              for field in [ config ] do
                  let html = Render.toString field
                  Expect.stringContains html "content-start" "all Select variants top-align their contents"
                  let help = html.IndexOf("Choose an option.", StringComparison.Ordinal)

                  Expect.isGreaterThan
                      help
                      (html.IndexOf("</button>", StringComparison.Ordinal))
                      "help text follows the trigger, matching Input and Textarea"

                  Expect.stringContains html "-description" "help remains associated with the field"
          }

          test "Input is one native control and InputGroup owns logically positioned addons" {
              for kind in [ InputType.Text; InputType.Email; InputType.Search; InputType.Password ] do
                  let html = fveInput { _name "query"; _type kind; _required true; _ariaBusy true } |> Render.toString
                  Expect.equal (Regex.Matches(html, "<input ").Count) 1 "one native control"
                  Expect.isFalse (html.Contains "<div" || html.Contains "<button" || html.Contains "<svg") "no implicit frame, icon or clear action"
                  Expect.stringContains html "required" "native constraints survive"
                  Expect.stringContains html "aria-busy=\"true\"" "busy is independent of editing policy"
                  Expect.isFalse (Regex.IsMatch(html, "\\s(?:readonly|disabled)(?:\\s|=|>)")) "busy does not lock the control"
              Expect.throws (fun () -> fveInput { span { "icon" } } |> ignore) "Input cannot contain decoration"
              let html =
                  fveInputGroup {
                      fveInputGroupAddon { _id "amount-context"; "<currency>" }
                      fveInput { _id "amount"; _name "amount"; _value "12.50"; _ariaDescribedby "amount-context amount-unit" }
                      fveInputGroupAddon { _position InputGroupPosition.InlineEnd; _id "amount-unit"; "USD" }
                      fveInputGroupAddon { _position InputGroupPosition.BlockStart; "Amount details" }
                      fveInputGroupAddon { _position InputGroupPosition.BlockEnd; button { _type "button"; "Calculate" } }
                  }
                  |> Render.toString
              Expect.equal (Regex.Matches(html, "<input ").Count) 1 "addons do not duplicate the control"
              Expect.stringContains html "value=\"12.50\"" "addons are outside the submitted value"
              Expect.stringContains html "&lt;currency&gt;" "addon text is encoded"
              Expect.stringContains html "aria-describedby=\"amount-context amount-unit\"" "the caller connects meaningful context"
              for position in [ "inline-start"; "inline-end"; "block-start"; "block-end" ] do
                  let addon = html.IndexOf($"data-fve-input-group-addon=\"{position}\"", StringComparison.Ordinal)
                  Expect.isGreaterThan addon (html.IndexOf("<input", StringComparison.Ordinal)) "the control precedes every addon in DOM order"
              Expect.throws (fun () -> fveInputGroup { () } |> ignore) "a frame needs a control"
          }

          test "Control sizes are explicit overrides independent of density" {
              for size, className in
                  [ Size.Small, "fve-control-small"
                    Size.Medium, "fve-control-medium"
                    Size.Large, "fve-control-large" ] do
                  let theme =
                      ComponentsTheme.sky
                      |> ComponentsTheme.withControlSize size
                      |> ComponentsTheme.withDensity Density.Compact

                  Expect.stringContains
                      (ComponentsTheme.className theme)
                      className
                      "density preserves the requested control size"

                  for html in
                      [ fveField {
                            fveLabel {
                                _for ("sized")
                                "Sized"
                            }

                            fveInput {
                                _id ("sized")
                                _name ("sized")
                                _size (size)
                            }
                        }
                        |> Render.toString
                        fveField {
                            fveLabel {
                                _for ("search-sized")
                                "Search"
                            }

                            fveInput {
                                _id ("search-sized")
                                _name ("search-sized")
                                _type (InputType.Search)
                                _size (size)
                            }
                        }
                        |> Render.toString
                        fveField {
                            fveLabel {
                                _for ("sized-select")
                                "Sized"
                            }

                            fveSelect {
                                _id ("sized-select")
                                _name ("sized-select")
                                _size (size)

                                fveSelectContent {
                                    for value, label, disabled in ([ ("a", "A", false) ]) do
                                        fveSelectItem {
                                            _value (value)
                                            _label label
                                            _disabled disabled
                                        }
                                }
                            }
                        }
                        |> Render.toString
                        fveField {
                            fveLabel {
                                _for ("sized-multiple")
                                "Sized"
                            }

                            fveMultiSelect {
                                _id ("sized-multiple")
                                _name ("sized-multiple")
                                _size (size)
                                _search (SelectSearch.Static)

                                fveSelectContent {
                                    for value, label, disabled in ([ ("a", "A", false) ]) do
                                        fveSelectItem {
                                            _value (value)
                                            _label label
                                            _disabled disabled
                                        }
                                }
                            }
                        }
                        |> Render.toString ] do
                      Expect.stringContains html className "all control families share the same override"

                      Expect.stringContains
                          html
                          "text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)]"
                          "control typography follows the selected size tokens"

              let action = fveButton { "Action" } |> Render.toString
              Expect.stringContains action ">Action</fve-button>" "Button leaves native typography to its runtime"
          }

          test "The registered InputGroup check route returns actual server validation feedback" {
              for email, message in [ "alex%40fve.meiermade.com", "Email format is valid."; "invalid", "Enter a valid email address." ] do
                  let status, html = routeResponse ("/components/input-group/check-email?email=" + email)
                  Expect.equal status 200 "the running route is registered"
                  Expect.stringContains html "id=\"input-group-check-result\"" "the response replaces the existing status"
                  Expect.stringContains html message "the server checks the submitted value"
          }

          test "Multiple choices preserve encoded identities and selection order" {
              let html =
                  fveMultiSelect {
                      _name "members"
                      _ariaLabel "Members"
                      _values [ "b\"2"; "a&1"; "b\"2" ]

                      fveSelectContent {
                          fveSelectItem {
                              _value "a&1"
                              "Alex <Ops>"
                          }

                          fveSelectItem {
                              _value "b\"2"
                              "Jamie Lee"
                          }
                      }
                  }
                  |> Render.toString

              Expect.equal
                  (Regex.Matches(html, "name=\"members\"").Count)
                  2
                  "one successful control per distinct selected value"

              Expect.isLessThan
                  (html.IndexOf("value=\"b&quot;2\""))
                  (html.IndexOf("value=\"a&amp;1\""))
                  "selection order, not option order"

              Expect.stringContains html "Alex &lt;Ops&gt;" "item labels are encoded"

              Expect.stringContains
                  html
                  "data-fve-option-value=\"a&amp;1\""
                  "item identity is encoded independently of presentation"
          }

          test "Component attributes and domain conversions remain typed" {
              let directory =
                  Path.Combine(Path.GetTempPath(), "fve-choice-types-" + Guid.NewGuid().ToString("N"))

              Directory.CreateDirectory directory |> ignore

              try
                  let invalidExamples =
                      [ "_density \"comfortable\"", "Density"
                        "_variant \"solid\"", "ButtonVariant"
                        "_size \"small\"", "Size"
                        "_color Density.Compact", "Density"
                        "_surface \"panel\"", "Surface"
                        "_mobileLayout \"records\"", "MobileLayout"
                        "_mobile \"primary\"", "Mobile"
                        "fveSelectItem { _value 1; \"One\" }", "string"
                        "fveMultiSelect { _name \"ids\"; _values [1]; fveSelectContent { () } }", "string" ]

                  for invalid, expectedDiagnostic in invalidExamples do
                      let source =
                          $"#r @\"{typeof<Html>.Assembly.Location}\"\n#r @\"{typeof<Size>.Assembly.Location}\"\nopen FSharp.ViewEngine\nopen FSharp.ViewEngine.Components\nopen type Html\n{invalid} |> ignore"

                      let path = Path.Combine(directory, "Invalid.fsx")
                      File.WriteAllText(path, source)
                      let start = System.Diagnostics.ProcessStartInfo("dotnet")

                      for argument in [ "fsi"; "--exec"; path ] do
                          start.ArgumentList.Add argument

                      start.RedirectStandardOutput <- true
                      start.RedirectStandardError <- true
                      use child = System.Diagnostics.Process.Start start
                      let output = child.StandardOutput.ReadToEndAsync()
                      let errors = child.StandardError.ReadToEndAsync()

                      if not (child.WaitForExit(30000)) then
                          child.Kill(true)
                          failtest "Invalid component API compilation did not settle."

                      Expect.notEqual child.ExitCode 0 $"The component API rejects {invalid}"

                      Expect.stringContains
                          errors.Result
                          expectedDiagnostic
                          "failure is a value or selection-mode incompatibility, not an unrelated error"

                      output.Result |> ignore
              finally
                  Directory.Delete(directory, true)
          }

          test "Table row batches replace their sentinel with native rows and the next sentinel" {
              for offset, count in [ 8, 9; 16, 8 ] do
                  let status, body = routeResponse $"/components/table/more?offset={offset}"
                  Expect.equal status 200 "the documented endpoint serves the requested batch"

                  Expect.stringContains
                      body
                      $"data: selector #people-more-{offset}\n"
                      "only the requesting row is targeted"

                  Expect.stringContains
                      body
                      "data: mode replace\n"
                      "the loading row replaces itself, rather than appending or rebuilding the table"

                  Expect.equal
                      (Regex.Matches(body, "<tr ").Count)
                      count
                      "the response contains eight data rows and only a non-final batch has a new sentinel"

                  Expect.isFalse (body.Contains "<fve-table") "row patches contain no table wrapper"
                  Expect.isFalse (body.Contains "id=\"infinite-person-1\"") "already-loaded rows are not resent"

                  Expect.equal
                      (body.Contains "data-on-intersect__once")
                      (offset = 8)
                      "the last batch stops requesting more rows"

              for offset in [ ""; "0"; "7"; "-8"; "24"; "2147483648"; "unknown" ] do
                  Expect.equal
                      (routeStatus ($"/components/table/more?offset={offset}"))
                      400
                      "invalid batches do not patch any table"
          }

          test "Retired and unknown routes render the shared not-found document" {
              for path in
                  [ "/components/item"
                    "/components/steps"
                    "/components/theme-switcher"
                    "/components/accessibility"
                    "/components/tailwind-css"
                    "/components/customization"
                    "/components/versioning"
                    "/components/interaction-and-server-state"
                    "/components/fixture"
                    "/components/app-shell"
                    "/components/section"
                    "/components/page-examples/graph-and-trace"
                    "/docs"
                    "/docs/components"
                    "/docs-components"
                    "/docs/components/fixture"
                    "/docs/page-examples/documentation-site"
                    "/docs/page-examples/api-reference"
                    "/docs/examples/api-reference"
                    "/docs/examples/executable-specification"
                    "/giraffe"
                    "/changelog"
                    "/not-a-documentation-route?query=missing"
                    "/components/not-a-component"
                    "/examples/not-a-template" ] do
                  let status, html = routeResponse path
                  Expect.equal status 404 $"{path} is not a redirect or a successful page"
                  Expect.stringContains html "<!DOCTYPE html>" "complete error document"
                  Expect.stringContains html "Page not found" "shared error heading"
                  Expect.stringContains html "content=\"noindex\"" "missing pages are not indexed"
                  Expect.stringContains html "href=\"/components\"" "catalog recovery link"
                  Expect.stringContains html "href=\"/\"" "home recovery link"
          }

          test "Retired row and workflow compositions have no public catalog or API" {
              for path in [ "/components/item"; "/components/steps" ] do
                  Expect.isNone (Components.tryPage path) "there is no replacement component page"
                  Expect.isFalse (Catalog.componentRegistrations |> List.exists (fun page -> page.path = path)) "retired compositions do not appear in navigation"
              let exported = typeof<CardBuilder>.Assembly.GetExportedTypes() |> Array.map _.Name |> Set.ofArray
              for name in [ "ItemVariant"; "StepState"; "ItemDsl"; "StepsDsl"; "ItemAttributes"; "StepAttributes" ] do
                  Expect.isFalse (exported.Contains name) "retired builders and types are not compatibility aliases"
          }

          test "Appearance is documented as a focused guide with a separate runnable document" {
              Expect.isFalse
                  (Catalog.componentRegistrations
                   |> List.exists (fun page -> page.path = "/components/theme-switcher"))
                  "appearance is not an installable UI component"

              Expect.isTrue
                  (Components.guideRegistrations
                   |> List.exists (fun page -> page.path = "/components/theming"))
                  "appearance belongs in Guides"

              let page = Components.tryPage "/components/theming" |> Option.get

              Expect.equal page.sections.Head.id "theme" "the guide leads with a focused working theme example"

              let html =
                  Appearance.appearanceDocument (Some "appearance-page-nonce")
                  |> Render.toHtmlDocString

              let initializer = html.IndexOf("window.fveColorMode =", StringComparison.Ordinal)
              let stylesheet = html.IndexOf("/css/output.css", StringComparison.Ordinal)
              let entry = html.IndexOf("/assets/appearance.js", StringComparison.Ordinal)

              Expect.isTrue
                  (initializer >= 0 && initializer < stylesheet && stylesheet < entry)
                  "first-paint initialization precedes CSS and the application entry"

              Expect.stringContains
                  html
                  "storageKey: \"appearance-guide\""
                  "the running page declares its actual browser storage key"

              Expect.equal
                  (Regex.Matches(html, "\\snonce=\"appearance-page-nonce\"").Count)
                  2
                  "both scripts use the host-provided nonce"

              Expect.stringContains
                  html
                  "data-nonce=\"appearance-page-nonce\""
                  "the full document enables Datastar nonce compilation"

              Expect.stringContains
                  html
                  "fve-color-mode"
                  "the ordinary controls explicitly synchronize with the initializer"

              Expect.isFalse
                  (html.Contains "/assets/app.js")
                  "the copyable page uses its own minimal entry, not hidden catalog bootstrap"

              Expect.stringContains
                  html
                  "document-segmented-appearance"
                  "the complete page includes segmented native choices"
          }

          test "Every gallery example compiles using only its copied code and the public packages" {
              let directory =
                  System.IO.Path.Combine(
                      System.IO.Path.GetTempPath(),
                      "fve-component-examples-" + System.Guid.NewGuid().ToString("N")
                  )

              System.IO.Directory.CreateDirectory directory |> ignore

              try
                  let examples = Components.allExamples () @ ComponentDocumentation.allExamples ()

                  let sources =
                      (examples |> List.map (fun example -> example.id, example.source))
                      @ Appearance.copiedSources ()
                      @ ComponentGuideExamples.copiedSources ()
                      @ ([ "typography"; "step-input"; "step-input-help" ]
                         |> List.map (fun id ->
                             "content-" + id, Components.exampleImports + "\n\n" + Showcase.sourceFor id))

                  let samples =
                      sources
                      |> List.mapi (fun index (id, copiedSource) ->
                          let repositorySource =
                              copiedSource.Replace(
                                  "Acme.Components",
                                  "FSharp.ViewEngine.Components",
                                  StringComparison.Ordinal
                              )

                          $"# 1 \"{id}.fsx\"\nmodule Example{index} =\n"
                          + (repositorySource.Split('\n')
                             |> Array.map (fun line -> "    " + line)
                             |> String.concat "\n"))

                  let reference (path: string) =
                      "#r @\"" + path.Replace("\"", "\"\"") + "\"\n"

                  let source =
                      reference typeof<Html>.Assembly.Location
                      + reference typeof<Size>.Assembly.Location
                      + reference typeof<Microsoft.AspNetCore.Http.HttpContext>.Assembly.Location
                      + reference typeof<Microsoft.AspNetCore.Http.IQueryCollection>.Assembly.Location
                      + reference typeof<Microsoft.AspNetCore.Http.Features.IFeatureCollection>.Assembly.Location
                      + reference typeof<Microsoft.Extensions.Primitives.StringValues>.Assembly.Location
                      + reference typeof<ServiceProviderServiceExtensions>.Assembly.Location
                      + reference typeof<IDatastarService>.Assembly.Location
                      + reference typeof<StarFederation.Datastar.FSharp.ElementPatchMode>.Assembly.Location
                      + reference (
                          AppDomain.CurrentDomain.GetAssemblies()
                          |> Array.find (fun assembly -> assembly.GetName().Name = "Giraffe")
                          |> _.Location
                      )
                      + "#r \"System.Text.Json\"\n\n"
                      + String.concat "\n\n" samples

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
              let source =
                  "module Sample =\n    type Choice = Yes | No\n    let private value = 1\n    // docs-example:start example\n    let example =\n        let nested = value\n        nested\n    // docs-example:end example\n    let unused = 2\n"

              Expect.equal
                  (SourceRegion.declarations [ "example"; "Choice"; "value" ] source)
                  "type Choice = Yes | No\n\nlet private value = 1\n\nlet example =\n    let nested = value\n    nested"
                  "source order and nested bindings are preserved without extraction markers"

              Expect.throws
                  (fun () -> SourceRegion.declarations [ "missing" ] source |> ignore)
                  "unknown declarations cannot silently disappear"
          }

          test "Three templates render independently of the library shell with GitHub source" {
              let gallery =
                  Examples.registration
                  |> View.document Registry.navigation
                  |> Render.toHtmlDocString

              Expect.equal (Regex.Matches(gallery, "data-example-template=").Count) 3 "exactly three template cards"

              for path in Examples.paths do
                  let status, html = routeResponse path
                  Expect.equal status 200 path
                  Expect.stringContains html "data-example-viewer=" "separate template experience"

                  Expect.stringContains
                      html
                      $"rel=\"canonical\" href=\"https://fve.meiermade.com{path}\""
                      "complete template metadata"

                  Expect.stringContains html "id=\"main-content\"" "one semantic content target"
                  Expect.isFalse (html.Contains "data-docs-sidebar") "no outer library documentation shell"
                  Expect.isFalse (html.Contains "data-example-viewer-bar") "no duplicate preview/code chrome"

                  Expect.stringContains
                      html
                      ("href=\"" + Example.Server.Hosting.sourceUrl + "\"")
                      "the top bar links to the real consumer project"

                  Expect.stringContains
                      html
                      "src=\"/examples/assets/scripts/app.js\""
                      "the example owns its component entry and shared Rocket runtime"

                  Expect.isFalse
                      (html.Contains "src=\"/scripts/datastar.1.0.4.js\"")
                      "ordinary Datastar is not loaded alongside Rocket"

              for template in Examples.templates do
                  Expect.stringContains
                      gallery
                      $"href=\"{template.path}\" target=\"_blank\" rel=\"noopener\""
                      "each template opens in an isolated new tab"

              for invalid in
                  [ "/examples/source/../Program.fs"
                    "/examples/source/secrets.txt"
                    "/examples/application/accounts/999"
                    "/examples/application/settings/environments/unknown"
                    "/examples/application/settings/environments/unknown/edit"
                    "/examples/source.zip"
                    "/api-reference/render-to-string"
                    "/specification/render-a-view" ] do
                  Expect.equal (routeStatus invalid) 404 "unknown routes and source files fail closed"

              for path in
                  [ "/examples/specification/accounts/view-accounts"
                    "/examples/specification/accounts/create-account"
                    "/examples/specification/architecture/solution/server"
                    "/examples/application/profile/organizations"
                    "/examples/application/settings/general"
                    "/examples/application/profile/edit"
                    "/examples/application/settings/general/edit"
                    "/examples/application/settings/environments/production"
                    "/examples/application/settings/environments/staging/edit"
                    "/examples/application/settings/environments/review-testing" ] do
                  Expect.equal (routeStatus path) 200 "named workflows and project contracts are public destinations"

              let _, preview = routeResponse "/examples/specification/accounts/create-account"
              let fixture = Regex.Match(preview, "<iframe[^>]+src=\"([^\"]+)\"")
              let _, product = routeResponse (WebUtility.HtmlDecode fixture.Groups[1].Value)
              // Identities are local to each document; inert sources are not active controls.
              for document in [ preview; product ] do
                  for inactive in [ "noscript"; "template" ] do
                      let active =
                          Regex.Replace(document, $"<{inactive}\\b[^>]*>.*?</{inactive}>", "", RegexOptions.Singleline)

                      let controlIds =
                          Regex.Matches(active, "<(?:input|button|select|dialog)[^>]* id=\"([^\"]+)\"")
                          |> Seq.cast<Match>
                          |> Seq.map (fun item -> item.Groups[1].Value)
                          |> Seq.toList

                      Expect.equal
                          controlIds.Length
                          (controlIds |> List.distinct |> List.length)
                          "each document has unique control identities in either scripting mode"

              let launch =
                  Regex.Matches(preview, "<a[^>]+href=\"([^\"]+)\"[^>]+aria-label=\"Expand Create account[^\"]*\"")
                  |> Seq.cast<Match>
                  |> Seq.map (fun item -> WebUtility.HtmlDecode item.Groups[1].Value)
                  |> Seq.toList

              Expect.isNonEmpty launch "each preview has a real launch link"

              for href in launch do
                  let parameters =
                      Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(
                          (Uri(Uri "https://fve.meiermade.com", href)).Query
                      )

                  Expect.equal (string parameters["appMode"]) "1" "resource-less launch selects App mode"
                  Expect.isFalse (parameters.ContainsKey "fveAppDock") "expansion has no obsolete docking state"
                  let status, html = routeResponse href
                  Expect.equal status 200 "launch destination resolves"

                  Expect.stringContains
                      html
                      "aria-label=\"App mode controls\""
                      "launch renders operable review controls"

                  Expect.isFalse (html.Contains "data-example-viewer-bar") "App mode excludes catalog chrome"

                  Expect.isFalse
                      (Regex.IsMatch(html, "<div[^>]*data-docs-diagram="))
                      "expanded review does not initialize hidden documentation diagrams"
          }

          test "Specification frames complete product documents without duplicating review chrome" {
              for page in Example.Server.Specification.workflowPages do
                  let status, html = routeResponse ("/examples/specification/" + page)
                  Expect.equal status 200 "the owning review page resolves"
                  let fixture = Regex.Match(html, "<iframe[^>]+src=\"([^\"]+)\"")
                  Expect.isTrue fixture.Success (page + " has a product document")

                  Expect.isFalse
                      (html.Contains "id=\"spec-product-document\"")
                      "product HTML is not inlined into review chrome"

                  let source = WebUtility.HtmlDecode fixture.Groups[1].Value
                  let productStatus, product = routeResponse source
                  Expect.equal productStatus 200 source
                  Expect.stringContains product "<!DOCTYPE html>" "the frame gets a complete document"

                  Expect.stringContains
                      product
                      "id=\"spec-product-document\""
                      "the real application owns product content"

                  Expect.isFalse (product.Contains "data-review-fixture=") "no nested review frame"

                  Expect.isFalse
                      (product.Contains "aria-label=\"App mode controls\"")
                      "review controls stay outside the product"

                  Expect.isFalse
                      (product.Contains "View example source on GitHub")
                      "catalog source chrome is not copied into the product"

              Expect.equal
                  (routeStatus "/examples/specification/architecture?fveFixture=product")
                  404
                  "only product workflows can be framed"

              let status, _, cookies, invalid =
                  postForm
                      "/examples/specification/accounts/update-account?resource=105&fveFixture=product"
                      [ "name", "Tax reserve"
                        "accountType", "Expense"
                        "parentType", "Expense"
                        "currency", "USD"
                        "subtype", "Generic"
                        "observedBalance", "not-required" ]

              Expect.equal status 200 "invalid product forms return their private field-error document"
              Expect.equal cookies "" "private values do not create a session"
              Expect.stringContains invalid "value=\"Tax reserve\"" "the entered value survives validation in the frame"
              Expect.stringContains invalid "already exists." "the typed validation error is rendered"

              Expect.isFalse
                  (invalid.Contains "data-review-fixture=")
                  "validation never nests the Spec host inside its iframe"
          }

          test "Progress streams native HTML from its registered demo route" {
              let context = DefaultHttpContext()
              context.Request.Method <- HttpMethods.Post
              context.Request.Path <- PathString "/components/progress/run"
              context.Request.Headers["Datastar-Request"] <- "true"
              context.Response.Body <- new MemoryStream()
              let registrations = ServiceCollection()
              registrations.AddDatastar() |> ignore
              use services = registrations.BuildServiceProvider()
              context.RequestServices <- services
              let accessor = services.GetRequiredService<IHttpContextAccessor>()
              accessor.HttpContext <- context
              let next: HttpFunc = fun current -> task { return Some current }

              try
                  let result = Handler.postRoutes next context |> Async.AwaitTask |> Async.RunSynchronously
                  Expect.isSome result "the registered progress route handles the request"
                  Expect.stringContains context.Response.ContentType "text/event-stream" "one streaming response"
                  context.Response.Body.Position <- 0L
                  use reader = new StreamReader(context.Response.Body)
                  let stream = reader.ReadToEnd()
                  for value in [ 0; 50; 100 ] do
                      Expect.stringContains stream ($"value=\"{value}\"") "the backend sends incremental native values"
                  Expect.stringContains stream "Complete" "the server renders the terminal state"
                  Expect.stringContains stream "components_progress_done" "the server enables the replay label"
              finally
                  accessor.HttpContext <- null
          }

          test "Notification examples expose independently replayable region endpoints" {
              for path, region, notification in
                  [ "/components/notifications/show", "save-notifications", "saved-notification-1"
                    "/components/notifications/show-timed", "timed-notifications", "review-notification-1" ] do
                  let status, _, _, html = postForm path []
                  Expect.equal status 200 "the example trigger resolves"
                  Expect.stringContains html ($"id=\"{region}\"") "the response updates its own region"
                  Expect.stringContains html ($"id=\"{notification}\"") "the first trigger renders a fresh notification"
          }

          test "Specification Profile renders the terminal journey affordance" {
              for path in
                  [ "/examples/specification/profile"
                    "/examples/specification/profile?appMode=1" ] do
                  let status, html = routeResponse path
                  Expect.equal status 200 "the real Profile destination resolves embedded and expanded"

                  let terminal =
                      Regex.Match(html, "<span[^>]*aria-label=\"No next journey step\"[^>]*>")

                  Expect.isTrue terminal.Success "the terminal affordance is rendered"

                  Expect.stringContains
                      terminal.Value
                      "aria-disabled=\"true\""
                      "the terminal affordance itself is disabled"

                  Expect.isFalse (html.Contains "aria-label=\"Next:") "the terminal journey has no forward link"
          }

          test "Deletion prepares real records and rejects invalid or partially blocked batches atomically" {
              let status, _, cookies, html = postForm "/examples/application/transactions"
                                                ["action", "prepare-delete"; "transactionIds", "201"; "transactionIds", "203"]
              Expect.equal status 200 "preparation renders a confirmation, not a success redirect"
              Expect.equal cookies "" "preparation does not change session state"
              Expect.stringContains html "Delete 2 transactions?" "confirmation identifies the actual count"
              Expect.stringContains html "Client payment" "confirmation includes the first record"
              Expect.stringContains html "Office supplies" "confirmation includes the second record"
              for path, fields in
                  [ "/examples/application/accounts", ["accountIds", "105"; "accountIds", "101"]
                    "/examples/application/accounts", ["accountIds", "group-Asset"]
                    "/examples/application/transactions", ["transactionIds", "201"; "transactionIds", "999"]
                    "/examples/application/transactions", [] ] do
                  let _, _, cookies, _ = postForm path (("action", "delete-selected") :: fields)
                  let _, unchanged = routeResponseWithCookie path (cookies.Split(';')[0])
                  let retained =
                      if path.EndsWith "accounts" then ["Select Unassigned expense"; "Select Operating checking"]
                      else ["Select Client payment"; "Select Office supplies"]
                  for name in retained do
                      Expect.stringContains unchanged name "rejected batches retain every selected available record; a feedback cookie is not a deletion"
              let blockedStatus, _, _, blocked = postForm "/examples/application/accounts"
                                                    ["action", "prepare-delete"; "accountIds", "105"; "accountIds", "101"]
              Expect.equal blockedStatus 200 "blocked preparation remains on the collection"
              Expect.stringContains blocked "Nothing was deleted" "explain the all-or-none result"
          }

          test "Deleted records use protected workspace session state without changing shared fixtures" {
              let status, location, cookies, _ = postForm "/examples/application/transactions?sort=date-asc&search=payment"
                                                    ["action", "delete-selected"; "transactionIds", "201"; "transactionIds", "201"]
              Expect.equal status 302 "confirmed deletion uses PRG"
              Expect.stringContains cookies "httponly" "session state is not exposed to page scripts"
              Expect.isFalse (cookies.Contains "expires=") "the cookie is browser-session scoped"
              let cookie = cookies.Split(';')[0]
              let visibleStatus, html = routeResponseWithCookie location cookie
              Expect.equal visibleStatus 200 "the resulting state resolves"
              Expect.stringContains html "1 transaction deleted" "duplicates count only once"
              Expect.isFalse (html.Contains "Select Client payment") "the deleted row is absent"
              Expect.equal (routeResponseWithCookie "/examples/application/transactions/201" cookie |> fst) 404 "deleted resources are unavailable"
              let _, fresh = routeResponse "/examples/application/transactions"
              Expect.stringContains fresh "Select Client payment" "other browser sessions retain their records"
              let _, other = routeResponseWithCookie "/examples/application/transactions?ledger=jordan" cookie
              Expect.stringContains other "Select Client payment" "other workspaces retain their records"
              let equal = cookie.IndexOf('=')
              let tampered = cookie.Substring(0, equal + 1) + "!" + cookie.Substring(equal + 2)
              let _, rejected = routeResponseWithCookie "/examples/application/transactions" tampered
              Expect.stringContains rejected "Select Client payment" "modified state is rejected"
              Expect.equal Ledger.Domain.initialRecords.transactions.Length 17 "the shared model stays immutable"
          }

          test "Template editors validate finite outcomes without retaining submissions or changing records" {
              let accountFields name kind =
                  [ "name", name
                    "accountType", kind
                    "parentType", kind
                    "currency", "USD"
                    "subtype", "Generic"
                    "observedBalance", "not-required" ]

              let before = routeResponse "/examples/application/accounts/105" |> snd

              for path, fields, state, message in
                  [ "/examples/application/accounts/new", accountFields "Private reserve" "Asset", "success", "Accounts"
                    "/examples/application/accounts/new",
                    accountFields "Operating checking" "Asset",
                    "duplicate-name",
                    "already exists."
                    "/examples/application/accounts/new",
                    accountFields "" "Asset",
                    "invalid",
                    "Enter a name between 1 and 80 characters."
                    "/examples/application/accounts/new",
                    accountFields "Private reserve" "unavailable",
                    "invalid-type",
                    "Choose an available account type."
                    "/examples/application/accounts/new",
                    accountFields "Private reserve" "Asset"
                    |> List.map (fun (key, value) -> key, if key = "currency" then "EUR" else value),
                    "invalid-details",
                    "Check account details"
                    "/examples/application/accounts/101/edit",
                    accountFields "Operating checking" "Asset",
                    "success",
                    "Operating checking"
                    "/examples/application/accounts/101/edit",
                    accountFields "Tax reserve" "Asset",
                    "duplicate-name",
                    "already exists."
                    "/examples/application/accounts/101/delete",
                    [ "action", "delete" ],
                    "delete-blocked",
                    "Account cannot be deleted"
                    "/examples/application/settings/environments/staging/edit",
                    [ "name", "Private environment" ],
                    "success",
                    "Staging"
                    "/examples/application/settings/environments/staging/edit",
                    [ "name", String.replicate 80 "a" ],
                    "success",
                    "Staging"
                    "/examples/application/settings/environments/staging/edit",
                    [ "name", String.replicate 81 "a" ],
                    "invalid",
                    "Enter a name between 1 and 80 characters."
                    "/examples/application/settings/environments/staging/edit",
                    [ "name", "  " ],
                    "invalid",
                    "Enter a name between 1 and 80 characters."
                    "/examples/application/settings/general/edit",
                    [ "workspace", "Private workspace"; "currency", "USD" ],
                    "success",
                    "General"
                    "/examples/application/settings/general/edit",
                    [ "workspace", ""; "currency", "USD" ],
                    "invalid",
                    "Enter a name between 1 and 80 characters."
                    "/examples/application/settings/general/edit",
                    [ "workspace", "Private workspace"; "currency", "EUR" ],
                    "invalid",
                    "Choose US dollar (USD)."
                    "/examples/application/profile/edit",
                    [ "name", "Private person"
                      "email", "private@example.invalid"
                      "timezone", "UTC" ],
                    "success",
                    "Profile"
                    "/examples/application/profile/edit",
                    [ "name", "Private person"; "email", "not-an-email"; "timezone", "UTC" ],
                    "invalid",
                    "Enter a valid email address."
                    "/examples/application/profile/edit",
                    [ "name", ""; "email", "private@example.invalid"; "timezone", "UTC" ],
                    "invalid",
                    "Enter a name between 1 and 80 characters."
                    "/examples/application/profile/edit",
                    [ "name", "Private person"
                      "email", "private@example.invalid"
                      "timezone", "unknown" ],
                    "invalid",
                    "Choose an available time zone." ] do
                  let status, location, cookies, body, cache = postFormResponse path fields

                  let editorFailure =
                      (path.Contains("/accounts/") || path.EndsWith("/edit", StringComparison.Ordinal))
                      && (state.StartsWith("invalid", StringComparison.Ordinal)
                          || state = "duplicate-name")

                  Expect.equal status (if editorFailure then 200 else 302) path
                  Expect.equal cookies "" "submissions create no session cookies"

                  let html =
                      if editorFailure then
                          Expect.equal location "" "failed editor values never enter a redirect URL"
                          Expect.equal cache "private, no-store" "private editor responses cannot be stored"
                          body
                      else
                          let parameters =
                              Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(
                                  (Uri(Uri "https://fve.meiermade.com", location)).Query
                              )

                          if state = "success" then
                              Expect.isFalse (parameters.ContainsKey "state") "success has no validation interstitial"
                          else
                              Expect.equal (string parameters["state"]) state "authoritative finite outcome"

                          let renderStatus, html = routeResponse location
                          Expect.equal renderStatus 200 "redirect destination resolves"
                          html

                  Expect.stringContains html message "rendered feedback matches the outcome"

                  if state = "duplicate-name" then
                      let enteredName = fields |> List.find (fst >> (=) "name") |> snd

                      Expect.stringContains
                          html
                          ("value=\"" + WebUtility.HtmlEncode(enteredName) + "\"")
                          "duplicate errors preserve entered values"

                      Expect.stringContains html "aria-invalid=\"true\"" "duplicate errors are attached to the field"
                      Expect.isFalse (html.Contains "data-fve-notice") "one field error has no duplicated form banner"

                  for privateValue in
                      [ "Private reserve"
                        "Private workspace"
                        "Private environment"
                        "Private person"
                        "private@example.invalid" ] do
                      Expect.isFalse (location.Contains privateValue) "private values never enter URLs"

                      if not editorFailure then
                          Expect.isFalse
                              (html.Contains privateValue)
                              "successful/non-editor submissions do not retain private values"

              Expect.equal
                  (routeResponse "/examples/application/accounts/105" |> snd)
                  before
                  "eligible deletion does not mutate the record"
          }

          test "Account editor failures retain exact private entries only in the current response" {
              let path = "/examples/application/accounts/101/edit"

              let fields =
                  [ "name", "  Private draft  "
                    "accountType", "Asset"
                    "parentType", "Expense"
                    "currency", "USD"
                    "subtype", "Generic"
                    "observedBalance", "not-required"
                    "from", "accounts"
                    "search", "operating"
                    "filterAccountType", "Expense"
                    "sort", "balance" ]

              let status, location, cookies, body, cache = postFormResponse path fields
              Expect.equal status 200 "invalid parent/type relationship renders field errors directly"
              Expect.equal cache "private, no-store" "the private draft is not cacheable"
              Expect.equal location "" "there is no draft redirect"
              Expect.equal cookies "" "there is no durable draft session"
              Expect.stringContains body "value=\"  Private draft  \"" "name whitespace survives this response exactly"

              Expect.stringContains
                  body
                  "name=\"filterAccountType\" value=\"Expense\""
                  "collection filtering is distinct from the editable type"

              let resultTitle =
                  Regex.Match(body, "<title[^>]*>([^<]+)</title>").Groups[1].Value
                  |> WebUtility.HtmlDecode

              Expect.equal
                  resultTitle
                  "Accounts · Application example"
                  "opening/validating the editor keeps its collection title and host identity"

              for href in Regex.Matches(body, "href=\"([^\"]*)\"") |> Seq.cast<Match> do
                  Expect.isFalse
                      (WebUtility.HtmlDecode(href.Groups[1].Value).Contains "Private draft")
                      "private values never enter navigation destinations"

              Expect.isFalse ((routeResponse path |> snd).Contains "Private draft") "a new GET cannot recover the draft"

              let success =
                  fields
                  |> List.map (fun (key, value) -> key, if key = "parentType" then "Asset" else value)

              let successStatus, redirect, _, _, _ = postFormResponse path success
              Expect.equal successStatus 302 "valid submission returns to the collection"
              let destination = Uri(Uri "https://fve.meiermade.com", redirect)

              Expect.equal
                  destination.AbsolutePath
                  "/examples/application/accounts"
                  "success closes the collection-origin editor"

              let resultTitle =
                  Regex
                      .Match(routeResponse redirect |> snd, "<title[^>]*>([^<]+)</title>")
                      .Groups[1]
                      .Value
                  |> WebUtility.HtmlDecode

              Expect.equal
                  resultTitle
                  "Accounts · Application example"
                  "the originating collection remains the background"

              let query =
                  Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery destination.Query

              for key, value in [ "search", "operating"; "accountType", "Expense"; "sort", "balance" ] do
                  Expect.equal (string query[key]) value ("public context survives: " + key)
          }

          test "Transaction deletion persists browser-session changes independently of verification" {
              for id in [ 201; 203 ] do
                  let record = $"/examples/application/transactions/{id}"
                  let before = routeResponse record |> snd
                  let path = record + "/delete"
                  Expect.equal (routeStatus path) 200 "verified and unverified records can both open confirmation"

                  let status, location, cookies, _ =
                      postForm path [ "action", "delete"; "from", "transactions" ]

                  Expect.equal status 302 "an existing record can be deleted"
                  Expect.stringContains cookies "httponly" "deletion creates protected session state"
                  Expect.stringContains location "state=selection-deleted" "deletion outcome is authoritative"
                  Expect.stringContains location "/examples/application/transactions?" "return to the originating collection"
                  Expect.equal (routeResponseWithCookie record (cookies.Split(';')[0]) |> fst) 404 "the record is gone in this session"

                  Expect.stringContains
                      (routeResponse location |> snd)
                      "Transactions"
                      "the finite outcome renders its collection background"

                  Expect.equal (routeResponse record |> snd) before "validation does not mutate seeded records"

              let missing = "/examples/application/transactions/999/delete"
              Expect.equal (routeStatus missing) 404 "missing records cannot open confirmation"

              Expect.equal
                  (postForm missing [ "action", "delete" ] |> fun (status, _, _, _) -> status)
                  404
                  "missing records cannot validate deletion"

              Expect.equal
                  (routeStatus "/examples/specification/transactions/delete-transaction")
                  200
                  "deletion has an owning Specification workflow"

              Expect.equal
                  (routeStatus "/examples/api-documentation/delete-transaction")
                  200
                  "deletion has an owning API contract"
          }

          test "Home reads share inclusive dates, carried balances and complete matching activity" {
              let request: Ledger.Home.ReadHomeRequest =
                  { records = Ledger.Domain.initialRecords
                    account = "101"
                    period = "90"
                    comparison = "expected"
                    startDate = "2026-09-14"
                    endDate = "2026-09-17" }

              let data = Ledger.Home.readHome request

              Expect.sequenceEqual
                  (data.transactions |> List.map _.id)
                  [ 202; 217; 216 ]
                  "both selected boundary days are included"

              Expect.equal
                  (data.actual |> List.find (fst >> (=) (DateOnly(2026, 9, 17))) |> snd)
                  20466M
                  "balances include earlier activity outside the zoom"

              let week =
                  Ledger.Home.readHome
                      { request with
                          period = "7"
                          startDate = ""
                          endDate = "" }

              Expect.equal
                  week.actual.Head
                  (DateOnly(2026, 9, 12), 20676M)
                  "a shorter configured period carries earlier balances"

              Expect.equal week.actual.Length 7 "Last 7 days includes exactly seven calendar dates"

              let single =
                  Ledger.Home.readHome
                      { request with
                          startDate = "2026-09-18"
                          endDate = "2026-09-18" }

              Expect.sequenceEqual (single.transactions |> List.map _.id) [ 201 ] "one-day ranges work"

              for first, last in
                  [ "2026-09-17", "2026-09-14"
                    "2026-09-31", "2026-09-31"
                    "2025-01-01", "2026-09-17"
                    "", "2026-09-17" ] do
                  let normalized =
                      Ledger.Home.readHome
                          { request with
                              startDate = first
                              endDate = last }

                  Expect.equal
                      (normalized.visibleStart, normalized.visibleEnd)
                      (DateOnly(2026, 6, 21), DateOnly(2026, 9, 18))
                      "malformed or out-of-period URL ranges fall back coherently"

              let empty = Ledger.Home.readHome { request with account = "105" }
              Expect.isEmpty empty.transactions "accounts without activity have a real empty result"

              let _, html =
                  routeResponse "/examples/application?account=101&range=90&startDate=2026-09-14&endDate=2026-09-17"

              Expect.stringContains html "Cloud hosting" "server-rendered HTML includes matching activity"

              Expect.isFalse
                  (html.Contains "Client payment")
                  "filtering happens on the server, not by hiding rendered rows"
          }

          test "Standalone example routes use consumer-owned components without a Docs dependency" {
              let owned = typeof<Example.Components.ButtonBuilder>.Assembly

              Expect.equal
                  (owned.GetName().Name)
                  "Example.Components"
                  "the running example uses installed editable source"

              for assembly in [ owned; typeof<Example.Server.Model.Query>.Assembly ] do
                  let dependencies = assembly.GetReferencedAssemblies() |> Seq.map _.Name |> Set.ofSeq
                  Expect.isFalse (dependencies.Contains "Docs") "the example runs without the catalog"

                  Expect.isFalse
                      (dependencies.Contains "FSharp.ViewEngine.Components")
                      "there is no private catalog component dependency"

              for path in
                  [ "/examples/application"
                    "/examples/specification"
                    "/examples/api-documentation" ] do
                  let context = DefaultHttpContext()
                  context.Request.Method <- HttpMethods.Get
                  context.Request.Scheme <- "https"
                  context.Request.Host <- HostString "fve.meiermade.com"
                  context.Request.Path <- PathString path
                  context.Response.Body <- new MemoryStream()
                  let next: HttpFunc = fun current -> task { return Some current }

                  Example.Server.Hosting.routes next context
                  |> Async.AwaitTask
                  |> Async.RunSynchronously
                  |> ignore

                  Expect.equal context.Response.StatusCode 200 "the standalone route resolves"
                  context.Response.Body.Position <- 0L
                  use reader = new StreamReader(context.Response.Body)
                  let html = reader.ReadToEnd()
                  Expect.stringContains html "id=\"main-content\"" "standalone hosting renders the full template"

                  Expect.stringContains
                      html
                      "/examples/assets/scripts/app.js"
                      "the standalone host supplies the same component entry"
          }

          test "Specification form outcomes preserve exact resource and submitted workspace mode" {
              let path = "/examples/specification/accounts/update-account?resource=105&appMode=1"

              let fields =
                  [ "name", "Unassigned expense"
                    "accountType", "Expense"
                    "parentType", "Expense"
                    "currency", "USD"
                    "subtype", "Generic"
                    "observedBalance", "not-required"
                    "organization", "client-organization"
                    "environment", "staging"
                    "ledger", "operating-company"
                    "appMode", "1" ]

              let status, location, _, _ = postForm path fields
              Expect.equal status 302 "native form returns a finite destination"
              let destination = Uri(Uri "https://fve.meiermade.com", location)

              Expect.equal
                  destination.AbsolutePath
                  "/examples/specification/accounts/view-account"
                  "success returns to the account"

              let parameters =
                  Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery destination.Query

              for key, value in
                  [ "resource", "105"
                    "appMode", "1"
                    "organization", "client-organization"
                    "environment", "staging"
                    "ledger", "operating-company"
                    "specState", "default" ] do
                  Expect.equal (string parameters[key]) value ("redirect retains " + key)

              let status, html = routeResponse location
              Expect.equal status 200 "result resolves"

              Expect.isFalse
                  (html.Contains "Account validated"
                   || html.Contains "id=\"template-account-editor\"")
                  "success closes the editor without a validation banner"

              let fixture = Regex.Match(html, "<iframe[^>]+src=\"([^\"]+)\"")
              Expect.isTrue fixture.Success "the review document frames the product"

              let productStatus, product =
                  routeResponse (WebUtility.HtmlDecode fixture.Groups[1].Value)

              Expect.equal productStatus 200 "the complete product document resolves"

              Expect.isFalse
                  (product.Contains "Account validated"
                   || product.Contains "id=\"template-account-editor\"")
                  "the product itself closes the successful editor without a validation banner"

              Expect.stringContains
                  product
                  "Unassigned expense"
                  "result is for the submitted resource, not the first account"
          }

          test "Template POST boundary rejects foreign origins unsupported content and the first oversized body" {
              let path = "/examples/application/accounts/new"

              Expect.equal
                  (mutationStatus path "https://foreign.test" "application/x-www-form-urlencoded" 0L)
                  403
                  "foreign origin is rejected"

              Expect.equal
                  (mutationStatus path "https://fve.meiermade.com" "application/json" 2L)
                  415
                  "unsupported body type is rejected"

              let fields =
                  [ "name", "Private limit check"
                    "accountType", "Asset"
                    "parentType", "Asset"
                    "currency", "USD"
                    "subtype", "Generic"
                    "observedBalance", "not-required" ]

              let encodedLength =
                  fields
                  |> List.sumBy (fun (key, value) ->
                      (Uri.EscapeDataString key).Length + 1 + (Uri.EscapeDataString value).Length + 1)

              for length, expected in [ 64000, 302; 64001, 413 ] do
                  let padded =
                      fields
                      @ [ "padding", String.replicate (length - encodedLength - "padding=".Length) "x" ]

                  let status, _, cookies, _ = postForm path padded
                  Expect.equal status expected "limit is tested with a real encoded body"
                  Expect.equal cookies "" "request boundary creates no session"
          }

          test
              "Every reusable component page leads with use, teaches installation and usage, and ends with its curated API" {
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

              let apiComponentPages =
                  allComponentPages
                  |> List.filter (fun registration -> registration.id <> Components.formLayoutsRegistration.id)

              let renderContent (content: HtmlElement list) = fragment { content } |> Render.toString

              let assertInstallation (registration: DocPage) (page: DocumentationPageConfig) =
                  let html =
                      page.sections
                      |> List.find (fun section -> section.id = "installation")
                      |> _.content
                      |> renderContent

                  Expect.stringContains
                      html
                      ">Installation</h2>"
                      $"{registration.title} has a visible installation heading"

                  Expect.stringContains
                      html
                      "Copy this component and its required dependencies"
                      $"{registration.title} uses the shared introduction"

                  Expect.stringContains
                      html
                      "The CLI resolves dependencies and preserves consumer-authored files."
                      $"{registration.title} uses the shared setup guidance"

                  Expect.stringContains
                      html
                      "href=\"/components/installation\""
                      $"{registration.title} links to full installation instructions"

                  let selector = registration.path.Substring("/components/".Length)

                  Expect.stringContains
                      html
                      $"dotnet fve add {selector} "
                      $"{registration.title} installs its own component"

                  let command = Regex.Match(html, @"dotnet fve add ([a-z0-9-]+(?: [a-z0-9-]+)*) --config")
                  Expect.equal command.Groups[1].Value selector "installation adds only the owning component"

                  let requiredModule =
                      match selector with
                      | "button" | "copy-reveal" | "dialog" | "drawer" | "command" -> Some "button.js"
                      | "table" -> Some "table.js"
                      | "chart" -> Some "chart.js"
                      | "tag-input" -> Some "tag-input.js"
                      | "breadcrumbs" -> Some "breadcrumbs.js"
                      | _ -> None
                  match requiredModule with
                  | Some file ->
                      Expect.stringContains (System.Net.WebUtility.HtmlDecode html) $"import '../../Acme.Components/Components/Assets/{file}';" "the component shows its required browser module, including transitive dependencies"
                      Expect.stringContains html "src/Acme.Web/js/app.js" "the import names the owning entry file"
                  | None ->
                      Expect.stringContains html "No additional JavaScript import is required." "components without an extra module say so"

                  for repeatedSetup in [ "npm run build:js"; "--bundle"; "script { _type" ] do
                      Expect.isFalse (html.Contains repeatedSetup) "shared build and layout setup stays on Installation"
                  if List.contains selector [ "code-block"; "example"; "fsharp-api-reference" ] then
                      Expect.stringContains html "CodeBlock.assets None []" "code-producing components include their required head setup"
                  if selector = "mermaid" then
                      Expect.stringContains html "Mermaid.assets" "Mermaid shows its actual head setup, not an invented import"

              for registration in allComponentPages do
                  let page = Components.tryPage registration.path |> Option.get
                  assertInstallation registration page

                  Expect.isFalse
                      (String.IsNullOrWhiteSpace page.description)
                      $"{registration.title} has a concise description"

                  let leadHtml = renderContent page.lead

                  Expect.equal
                      (Regex.Matches(leadHtml, "data-docs-example=\"true\"").Count)
                      1
                      $"{registration.title} has one canonical lead example"

                  Expect.equal
                      (page.sections |> List.take 2 |> List.map _.id)
                      [ "installation"; "usage" ]
                      $"{registration.title} teaches installation before usage"

                  match page.rightRail with
                  | TableOfContents -> ()
                  | _ -> failtest $"{registration.title} does not use the shared On this page rail"

              for registration in apiComponentPages do
                  let page = Components.tryPage registration.path |> Option.get
                  let reference = List.last page.sections
                  Expect.equal reference.id "api-reference" $"{registration.title} puts its curated public API last"
                  let html = renderContent reference.content

                  Expect.stringContains
                      html
                      "data-fve-api-reference=\"true\""
                      $"{registration.title} renders the shared API reference"

                  Expect.stringContains html "val " $"{registration.title} exposes public function signatures"

              for registration in ComponentDocumentation.registrations do
                  let page = ComponentDocumentation.tryPage registration.path |> Option.get
                  assertInstallation registration page

                  Expect.equal
                      (page.sections |> List.take 2 |> List.map _.id)
                      [ "installation"; "usage" ]
                      $"Documentation {registration.title} teaches installation and usage after its preview"

                  Expect.stringContains
                      (renderContent page.lead)
                      "data-docs-example=\"true\""
                      "lead owns an executable example"

                  Expect.equal
                      (List.last page.sections).id
                      "api-reference"
                      $"Documentation {registration.title} puts its curated API last"

                  let html = renderContent (List.last page.sections).content

                  Expect.stringContains
                      html
                      "data-fve-api-reference=\"true\""
                      $"Documentation {registration.title} renders the shared API reference"

                  Expect.stringContains
                      html
                      "val "
                      $"Documentation {registration.title} exposes public function signatures"

              for registration in [ Components.noticeRegistration; Components.themingRegistration ] do
                  let page = Components.tryPage registration.path |> Option.get

                  Expect.isTrue
                      (page.sections |> List.exists (fun section -> section.id = "api-reference"))
                      $"{registration.title} owns its shared public declarations without forcing the API ahead of setup"

              let chartPage = Components.tryPage "/components/chart" |> Option.get
              let chartSection id =
                  chartPage.sections |> List.find (fun section -> section.id = id) |> _.content |> renderContent
              Expect.isFalse ((chartSection "installation").Contains "button.js") "Chart itself does not require Button's module"
              Expect.isFalse ((renderContent chartPage.lead).Contains "/components/button#installation") "the basic Chart example has no Button dependency"
              Expect.stringContains (chartSection "components-chart-interaction") "/components/button#installation" "the switching example owns its optional Button setup"
              Expect.isFalse ((chartSection "usage").Contains "npm run build:js") "Chart usage does not repeat the shared build instructions"
              Expect.isFalse ((System.Net.WebUtility.HtmlDecode(chartSection "usage")).Contains "import '../../Acme.Components") "Chart's import belongs to Installation, not Usage"

              let buttonReference =
                  Components.tryPage "/components/button"
                  |> Option.get
                  |> _.sections
                  |> List.last
                  |> _.content
                  |> renderContent

              for signature in [ "type ButtonBuilder"; "module ButtonDsl ="; "val fveButton : ButtonBuilder" ] do
                  Expect.stringContains buttonReference signature $"Button API includes {signature}"

              for unrelated in [ "ButtonGroup"; "DropdownMenu" ] do
                  Expect.isFalse (buttonReference.Contains unrelated) $"Button does not claim {unrelated} declarations"

              Expect.isFalse (buttonReference.Contains("<dl")) "XML summaries are not repeated as default-note cards"

              for obsolete in
                  [ "ButtonContent"
                    "ButtonType"
                    "ButtonConfig"
                    "val create"
                    "val withColor"
                    "val render" ] do
                  Expect.isFalse
                      (buttonReference.Contains obsolete)
                      "Button exposes its CE, not the removed pipeline API"

              Expect.isFalse (buttonReference.Contains("// opaque")) "hidden representations do not add internal jargon"

              for retired in [ "ButtonColor"; "buttonColor"; "buttonVariant"; "controlSize" ] do
                  Expect.isFalse
                      (buttonReference.Contains retired)
                      "Button API does not advertise retired styling operations or enums"

              let linkReference =
                  Components.tryPage "/components/link"
                  |> Option.get
                  |> _.sections
                  |> List.last
                  |> _.content
                  |> renderContent

              Expect.stringContains
                  linkReference
                  "val fveLink : LinkBuilder"
                  "module values expose the public computation-expression entry point"

              for obsolete in
                  [ "type LinkColor"
                    "type LinkConfig"
                    "linkVariant"
                    "linkColor"
                    "controlSize" ] do
                  Expect.isFalse
                      (linkReference.Contains obsolete)
                      "Link exposes ordinary shared attributes, not legacy configuration"

              for unrelated in [ "ButtonConfig"; "ButtonBuilder"; "pendingSignal" ] do
                  Expect.isFalse (linkReference.Contains unrelated) "Link does not inherit Button configuration"

              let selectReference =
                  Components.tryPage "/components/select"
                  |> Option.get
                  |> _.sections
                  |> List.last
                  |> _.content
                  |> renderContent

              for declaration in
                  [ "val fveSelect"
                    "val fveMultiSelect"
                    "val fveSelectContent"
                    "val fveSelectGroup"
                    "val fveSelectItem"
                    "val fveSelectSelectedItem" ] do
                  Expect.stringContains
                      selectReference
                      declaration
                      "Select documents its builders and supporting option data"

              for legacy in
                  [ "module Select ="
                    "type SelectConfig"
                    "val withSearch"
                    "SelectContext"
                    "val fveSelectTrigger"
                    "val fveSelectValue"
                    "selectContext"
                    "val parts" ] do
                  Expect.isFalse
                      (selectReference.Contains legacy)
                      "Select does not teach the legacy construction pipeline"

              for path, helper, cases in
                  [ "/components/command", "CommandContent", [ "| Link of"; "| Action of" ]
                    "/components/dropdown-menu",
                    "MenuItemContent",
                    [ "| Link of"; "| Action of"; "| Radio of"; "| Checkbox of"; "| Group of" ]
                    "/components/button-group", "", [ "| Button of"; "| Menu of" ] ] do
                  let reference =
                      Components.tryPage path
                      |> Option.get
                      |> _.sections
                      |> List.last
                      |> _.content
                      |> renderContent

                  for case in cases do
                      Expect.isFalse (reference.Contains case) $"{path} never advertises inaccessible constructors"

                  if helper <> "" then
                      Expect.isFalse (reference.Contains helper) $"{path} never advertises private helper types"

              let buttonGroupReference =
                  Components.tryPage "/components/button-group"
                  |> Option.get
                  |> _.sections
                  |> List.last
                  |> _.content
                  |> renderContent

              for declaration in
                  [ "type ButtonGroupBuilder"
                    "module ButtonGroupDsl ="
                    "val fveButtonGroup : ButtonGroupBuilder" ] do
                  Expect.equal
                      (Regex.Matches(buttonGroupReference, Regex.Escape declaration).Count)
                      1
                      $"Button group includes {declaration} once"

              Expect.isFalse
                  (buttonGroupReference.Contains("type &#39;destination"))
                  "generic parameters are never emitted as standalone declarations"

              Expect.isFalse
                  (buttonGroupReference.Contains("type DropdownMenuConfig"))
                  "referenced Dropdown menu declarations stay on their owning page"

              let exactReference =
                  fveFSharpApiReference { yield! ([ typeof<ButtonBuilder> ]: System.Type list) }
                  |> Render.toString

              Expect.stringContains exactReference "type ButtonBuilder" "explicit API roots are rendered"

              Expect.isFalse
                  (exactReference.Contains("module Button"))
                  "explicit API roots never trigger prefix or dependency expansion"

              let apiAssembly = typeof<ButtonBuilder>.Assembly
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

                      if categories.Length > 1 then
                          Some(member'.Attribute(XName.Get "name").Value)
                      else
                          None)

              Expect.isEmpty multiplyOwned "a public declaration has at most one owning API category"

              let documentedTypes =
                  documentedTypeMembers
                  |> Seq.choose (fun member' ->
                      let name = member'.Attribute(XName.Get "name")

                      let isExplicit =
                          member'.Elements(XName.Get "category") |> Seq.isEmpty |> not
                          || not (isNull (member'.Element(XName.Get "exclude")))

                      if not isExplicit then
                          None
                      else
                          Some(name.Value.Substring(2)))
                  |> Set.ofSeq

              let uncategorized =
                  apiAssembly.GetExportedTypes()
                  |> Array.filter (fun type' ->
                      isNull type'.DeclaringType
                      && not (isNull type'.Namespace)
                      && type'.Namespace.StartsWith("FSharp.ViewEngine.Components", StringComparison.Ordinal)
                      && not (documentedTypes.Contains type'.FullName))
                  |> Array.map _.FullName

              Expect.isEmpty
                  uncategorized
                  "every public top-level declaration is explicitly categorized or excluded beside its source declaration"
          }

          test "Components publishes a first-class page for every public component and focused shared guides" {
              let render registration =
                  registration |> View.document Registry.navigation |> Render.toHtmlDocString

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

              let renderedDocumentationControls =
                  ComponentDocumentation.registrations |> List.map render

              let allHtml =
                  String.concat
                      Environment.NewLine
                      (overview :: installation :: renderedComponents
                       @ renderedGuides
                       @ renderedDocumentationControls)

              Expect.stringContains
                  allHtml
                  "href=\"/components/chart\""
                  "the promoted Chart owns a discoverable catalog route"

              Expect.equal (routeStatus "/components/chart") 200 "Chart has its own documentation route"

              Expect.isTrue
                  (Components.dataDisplayRegistrations
                   |> List.exists (fun page -> page.path = "/components/chart"))
                  "Chart belongs to Data display"

              for item in Catalog.componentRegistrations do
                  Expect.stringContains
                      overview
                      ($"href=\"{item.path}\"")
                      "every component is discoverable from the index"

              Expect.stringContains overview "href=\"/examples\"" "page assembly goes to the template gallery"

              Expect.stringContains
                  overview
                  "Opinionated, Tailwind-styled components for F#"
                  "the catalog explains its styling approach"

              for html in [ overview; installation ] do
                  for destination in [ "https://tailwindcss.com/"; "https://data-star.dev/" ] do
                      Expect.stringContains
                          html
                          ($"href=\"{destination}")
                          "setup links to the underlying styling and interaction tools"

              for framework in [ "app-shell"; "collection"; "detail"; "form-layouts"; "media-library" ] do
                  Expect.isFalse
                      (Catalog.componentRegistrations
                       |> List.exists (fun item -> item.path = "/components/" + framework))
                      "framework assembly is not an installable component"

              Expect.equal
                  (Catalog.componentRegistrations
                   |> List.map _.path
                   |> List.distinct
                   |> List.length)
                  Catalog.componentRegistrations.Length
                  "one owning page per component"

              Expect.stringContains installation "dotnet tool install FSharp.ViewEngine.Cli" "local tool installation"
              Expect.stringContains installation "dotnet fve init" "consumer-owned project initialization"

              for source in
                  [ "../Acme.Components/Components/**/*.fs"
                    "../Acme.Components/Components/Assets/**/*.js"
                    "./**/*.fs" ] do
                  Expect.stringContains
                      installation
                      ($"@source &quot;{source}&quot;")
                      "Tailwind scans installed F#, component JavaScript and application views relative to the web project"

              Expect.stringContains
                  installation
                  "npm install --save-dev tailwindcss@4 @tailwindcss/cli@4 @tailwindcss/typography esbuild"
                  "Tailwind and JavaScript build tools are installed together"

              Expect.stringContains
                  installation
                  "@plugin &quot;@tailwindcss/typography&quot;"
                  "the installed Typography plugin is enabled"

              Expect.stringContains
                  installation
                  "id=\"runtime\""
                  "the shared installation guide owns browser runtime setup"

              for setup in
                  [ "Assets/button.js"
                    "build:css"
                    "build:js"
                    "watch:css"
                    "watch:js"
                    "/css/output.css"
                    "/assets/app.js"
                    "ComponentsTheme.attributes" ] do
                  Expect.stringContains
                      installation
                      setup
                      "default setup covers CSS, JavaScript, development watchers and a themed view"

              let button = render Components.buttonRegistration

              Expect.isFalse
                  (button.Contains "id=\"runtime\"")
                  "Button links to shared setup rather than duplicating it"

              let registrySource =
                  Path.GetFullPath(
                      Path.Combine(
                          __SOURCE_DIRECTORY__,
                          "..",
                          "FSharp.ViewEngine.Components",
                          "FSharp.ViewEngine.Components.Registry.props"
                      )
                  )
                  |> File.ReadAllText

              for registration, html in List.zip componentRegistrations renderedComponents do
                  Expect.stringContains
                      html
                      "data-docs-layout=\"gallery\""
                      $"{registration.path} uses the gallery layout"

                  let isPageExample =
                      registration.path.StartsWith("/components/page-examples/", StringComparison.Ordinal)

                  if isPageExample then
                      for rejected in [ "id=\"installation\""; "id=\"usage\""; "data-docs-toc=\"true\"" ] do
                          Expect.isFalse
                              (html.Contains rejected)
                              $"{registration.path} keeps the Page-example workflow structure without {rejected}"
                  else
                      Expect.stringContains
                          html
                          "id=\"installation\""
                          $"{registration.path} has component-specific installation"

                      Expect.stringContains html "id=\"usage\"" $"{registration.path} has copyable usage"

                      for example in Components.examplesFor (registration.path.Substring("/components/".Length)) do
                          let financialCopy =
                              @"\b(accounts?|transactions?|ledger|treasury|balances?|reconciliation|reconcile|statements?|invoice|payroll|receivables|USD|finance|financial|tax)\b"

                          let content = Render.toString example.preview + "\n" + example.source

                          let connectedRowActions =
                              Set.contains example.id (set ["components-button-row-actions"; "components-dropdown-menu-row-actions"; "components-table-row-actions"])
                          if connectedRowActions then
                              Expect.stringContains example.source "fveDropdownMenu" "row-action recipes compose real menu controls"
                              Expect.isFalse (content.Contains "applicationShell") "the connected recipe remains a small component composition"
                          else
                              Expect.isFalse
                                  (Regex.IsMatch(content, financialCopy, RegexOptions.IgnoreCase))
                                  $"{registration.path} keeps primitive previews and copied code generic"

                      Expect.stringContains
                          html
                          "data-docs-toc=\"true\""
                          $"{registration.path} has On this page navigation"

                      Expect.stringContains
                          html
                          "class=\"sr-only\" data-docs-example-title=\"true\""
                          $"{registration.path} leads with one unlisted canonical example"

                      let selectorMatch =
                          Regex.Match(html, "dotnet fve add ([a-z0-9-]+(?: [a-z0-9-]+)*) --config")

                      Expect.isTrue selectorMatch.Success $"{registration.path} renders an exact fve add command"

                      for selector in selectorMatch.Groups[1].Value.Split(' ') do
                          Expect.stringContains
                              registrySource
                              $"FveName=\"{selector}\""
                              $"{registration.path} installation selector exists in the canonical source registry"

                  Expect.stringContains
                      html
                      "data-docs-example=\"true\""
                      $"{registration.path} has an executable example"

                  Expect.stringContains
                      html
                      "Example: &#39;preview&#39;"
                      $"{registration.path} shows its preview by default"

                  let examples =
                      html
                          .Split([| "data-docs-example=\"true\"" |], System.StringSplitOptions.None)
                          .Length
                      - 1

                  let expectedExamples =
                      Components.examplesFor (registration.path.Substring("/components/".Length))
                      |> List.length

                  Expect.equal
                      examples
                      expectedExamples
                      $"{registration.path} has its focused component examples without repeating Default"

                  let ids =
                      Regex.Matches(html, "\\sid=\"([^\"]+)\"")
                      |> Seq.map (fun m -> m.Groups[1].Value)
                      |> Seq.toList

                  let duplicates =
                      ids
                      |> List.countBy id
                      |> List.filter (fun (_, count) -> count > 1)
                      |> List.map fst

                  Expect.isEmpty
                      duplicates
                      (registration.path
                       + " has no duplicate example or control IDs: "
                       + String.concat ", " duplicates)

              for path, selector in
                  [ "/components/avatar", "avatar"
                    "/components/copy-reveal", "copy-reveal"
                    "/components/input", "input"
                    "/components/textarea", "textarea"
                    "/components/tag-input", "tag-input"
                    "/components/drawer", "drawer"
                    "/components/page-header", "page-header"
                    "/components/page-top-bar", "page-top-bar" ] do
                  Expect.stringContains
                      (render (
                          Components.allRegistrations
                          |> List.find (fun registration -> registration.path = path)
                      ))
                      $"dotnet fve add {selector} "
                      $"{path} installs its owning source selector"

              for registration, html in List.zip Components.guideRegistrations renderedGuides do
                  Expect.stringContains
                      html
                      "data-docs-layout=\"article\""
                      $"{registration.path} uses the article layout"

                  Expect.stringContains
                      html
                      "data-docs-example=\"true\""
                      $"{registration.path} teaches with working, copyable examples"

              Expect.stringContains
                  allHtml
                  "/components/menus/actions"
                  "DropdownMenu example uses a real Docs-owned patch endpoint"

              Expect.stringContains
                  allHtml
                  "data-fve-select-query=\"item_query\""
                  "remote Combobox emits local open state and an intentionally submitted query"

              Expect.stringContains allHtml "role=\"switch\"" "Switch preserves switch semantics"
              Expect.stringContains allHtml "aria-pressed=\"true\"" "ToggleButton preserves pressed semantics"
              Expect.stringContains allHtml "type=\"radio\"" "RadioGroup preserves form semantics internally"

              for endpoint in
                  [ "/components/choices/select"
                    "/components/choices/checkbox"
                    "/components/choices/switch"
                    "/components/choices/radio" ] do
                  Expect.stringContains allHtml endpoint $"focused example posts to real Docs endpoint {endpoint}"

              Expect.stringContains
                  allHtml
                  "id=\"review-changes-dialog-trigger\""
                  "Dialog renders its connected trigger"

              Expect.stringContains
                  allHtml
                  "document.getElementById(&quot;review-changes-dialog-trigger&quot;)?.focus()"
                  "Dialog close retains a connected-trigger fallback"

              Expect.stringContains
                  allHtml
                  "dialog?._fveOpener"
                  "Dialog close restores the actual opener before falling back to its default trigger"

              Expect.stringContains
                  allHtml
                  "role=\"alertdialog\""
                  "Dialog composition preserves urgent confirmation semantics"

              Expect.stringContains
                  allHtml
                  "data-indicator:_delete_item_confirmation_pending"
                  "the confirmation form owns immediate duplicate-submit protection"

              Expect.stringContains allHtml "id=\"item-details-drawer\"" "Drawer renders a stable native dialog"
              Expect.stringContains allHtml "Details refreshed." "Drawer preserves server-patched detail content"

              Expect.stringContains
                  allHtml
                  "/components/dialogs/confirm"
                  "the confirmation form uses a real Docs-owned endpoint"

              Expect.stringContains allHtml "/components/drawers/item" "Drawer uses a real Docs-owned patch endpoint"
              Expect.stringContains allHtml "/components/tabs/review" "Tabs uses a real Docs-owned patch endpoint"

              Expect.stringContains
                  allHtml
                  "data-signals=\"{_components_menu_actions_open: false, _components_menu_actions_typeahead:"
                  "menu IDs become valid isolated interaction signal tokens"

              Expect.isFalse
                  (allHtml.Contains("_components-menu-actions-open"))
                  "DOM IDs are not copied unsafely into expressions"

              Expect.stringContains allHtml "aria-current=\"page\"" "typed navigation retains the current destination"

              Expect.stringContains
                  allHtml
                  "data-fve-gallery-contained=\"true\""
                  "standalone composition fixtures contain destination navigation"

              Expect.stringContains allHtml "--fve-brand-solid" "consumer theme overrides are documented"

              Expect.stringContains
                  overview
                  "rel=\"prev\" href=\"/extensions/tailwind-elements\""
                  "Components follows integrations"

              Expect.stringContains
                  overview
                  "rel=\"next\" href=\"/components/installation\""
                  "overview continues to installation"

              Expect.equal
                  (Components.guideRegistrations |> List.map _.title)
                  [ "Using signals"; "Theming" ]
                  "only component-specific, example-led guides remain"

              let theming = render Components.themingRegistration
              Expect.stringContains theming "rel=\"prev\" href=\"/components/using-signals\"" "theming follows signals"

              Expect.stringContains
                  theming
                  "rel=\"next\" href=\"/components/button\""
                  "focused components follow the two guides"

              Expect.stringContains installation "id=\"updating\"" "installation owns source updates"

              for guide in renderedGuides do
                  for duplicated in
                      [ "npm install"
                        "dotnet fve init"
                        "@source"
                        "Renderer-owned variables"
                        "appearanceDocument" ] do
                      Expect.isFalse
                          (guide.Contains duplicated)
                          "guides do not repeat installation or full document scaffolding"

              for _, source in ComponentGuideExamples.copiedSources () do
                  Expect.isLessThanOrEqual (source.Split('\n').Length) 40 "each recipe stays short and self-contained"
          }

          test "SideNavRow distinguishes navigation links from menus and supports profile and trailing content" {
              let settings =
                  fveSideNavRow {
                      yield _href (shellTestUrl (Settings))
                      yield ("Settings": string)
                      yield _ariaCurrent "page"
                  }

              let settingsHtml = settings |> Render.toString
              Expect.stringContains settingsHtml "href=\"/settings\"" "link destinations use the typed resolver"
              Expect.stringContains settingsHtml "aria-current=\"page\"" "current links retain their navigation state"
              Expect.isFalse (settingsHtml.Contains "<svg") "plain links do not imply a menu or disclosure"
              Expect.isFalse (settingsHtml.Contains "aria-haspopup") "plain links remain links"

              let profileHtml =
                  fveSideNavRow {
                      yield _href (shellTestUrl (Settings))
                      yield ("Andrew Meier": string)

                      yield
                          fveSideNavLeading {
                              yield
                                  ((fveAvatar {
                                      _ariaLabel "Andrew Meier"
                                      _ariaHidden true
                                      "AM"
                                  })
                                  : HtmlElement)
                          }

                      yield _description ("andrew+<private>@meiermade.com")
                      yield fveSideNavTrailing { yield ((fveBadge { "New" }): HtmlElement) }
                  }
                  |> Render.toString

              Expect.stringContains profileHtml "Andrew Meier" "the profile name is rendered"

              Expect.stringContains
                  profileHtml
                  "andrew+&lt;private&gt;@meiermade.com"
                  "secondary text is safely encoded"

              Expect.stringContains profileHtml ">AM</span>" "leading content supports an avatar"
              Expect.stringContains profileHtml ">New</span>" "trailing content is explicitly supplied"
              Expect.isFalse (profileHtml.Contains "<svg") "custom trailing content does not add a link chevron"

              let menuHtml =
                  fveDropdownMenu {
                      yield _id ("row-workspace")
                      yield _ariaLabel ("Choose workspace")
                      yield fveDropdownMenuTrigger { fveButton { yield ("Choose workspace": string) } }

                      yield!
                          ([ fveDropdownMenuItem {
                                 yield _href (shellTestUrl (Home))
                                 yield ("Home": string)
                             } ])
                  }
                  |> fun menu ->
                      fveSideNavRow {
                          _label "Workspace"
                          fveSideNavTrailing { fveBadge { "2" } }
                          menu
                      }
                  |> Render.toString

              Expect.stringContains menuHtml "aria-haspopup=\"menu\"" "menu rows retain their interaction semantics"

              Expect.equal
                  (Regex.Matches(menuHtml, "<svg").Count)
                  1
                  "menus retain their disclosure indicator alongside optional trailing content"

              Expect.stringContains menuHtml ">2</span>" "menus support the same trailing slot"
          }

          test
              "Breadcrumbs, SideNav, PageTopBar, PageHeader, Section, Page, and AppShell preserve typed ownership and responsive semantics" {
              let breadcrumbs () =
                  fveBreadcrumbs {
                      yield _id ("account-breadcrumbs")
                      yield _ariaLabel ("Breadcrumb")

                      yield!
                          ([ fveBreadcrumbItem {
                                 yield _href (shellTestUrl (Home))
                                 yield ("Home": string)
                             }
                             fveBreadcrumbItem {
                                 yield _href (shellTestUrl (Accounts))
                                 yield ("Accounts": string)
                             }
                             fveBreadcrumbItem {
                                 yield _href (shellTestUrl ((Account 2048)))
                                 yield ("Account 2048": string)
                             } ])
                  }

              let breadcrumbsHtml = breadcrumbs () |> Render.toString

              Expect.stringContains
                  breadcrumbsHtml
                  "<nav id=\"account-breadcrumbs\" aria-label=\"Breadcrumb\""
                  "Breadcrumbs renders a labelled landmark"

              Expect.stringContains breadcrumbsHtml "href=\"/accounts\"" "breadcrumb ancestors use the typed resolver"
              Expect.stringContains breadcrumbsHtml "aria-current=\"page\"" "breadcrumb current location is identified"

              Expect.equal
                  (Regex.Matches(breadcrumbsHtml, "aria-current=\"page\"").Count)
                  1
                  "Breadcrumbs identifies one current item"

              Expect.isFalse
                  (breadcrumbsHtml.Contains("href=\"/accounts/2048\""))
                  "the current breadcrumb is not a redundant link"

              Expect.stringContains
                  breadcrumbsHtml
                  "aria-label=\"Show hidden breadcrumbs\""
                  "deep paths expose compact overflow access"

              Expect.stringContains breadcrumbsHtml "<fve-breadcrumbs" "Rocket owns the sizing lifecycle"
              Expect.stringContains breadcrumbsHtml "overflow-x-auto" "the native fallback retains the complete trail"
              Expect.isFalse (breadcrumbsHtml.Contains "sm:hidden") "ancestors are not hidden by viewport breakpoints"
              Expect.stringContains breadcrumbsHtml "display:none" "overflow is initially unavailable until measurement"
              Expect.equal (Regex.Matches(breadcrumbsHtml, "data-fve-breadcrumb-item=").Count) 3 "every original item is rendered"

              let icon = span { "L" }

              let grouped =
                  fveSideNav {
                      yield _id ("product-navigation")
                      yield _ariaLabel ("Product navigation")

                      yield
                          ((fveSideNavHeader {
                              yield _label ("Ledger")

                              yield
                                  ((strong {
                                      icon
                                      " Ledger"
                                  }))

                              yield fveSideNavHeaderCompact { yield (icon: HtmlElement) }
                          }))

                      yield
                          ((fveSideNavContent {
                              yield!
                                  ([ fveSideNavSection {
                                         yield _label ("Manage")

                                         yield!
                                             ([ fveSideNavItem {
                                                    yield _href (shellTestUrl (Home))
                                                    yield ("Dashboard": string)
                                                    yield fveSideNavLeading { yield (icon: HtmlElement) }
                                                }
                                                fveSideNavItem {
                                                    yield _href (shellTestUrl (Accounts))
                                                    yield ("Accounts": string)
                                                    yield fveSideNavBadge { yield ((span { "4" }): HtmlElement) }

                                                    yield
                                                        fveSideNavAction {
                                                            yield
                                                                ((button {
                                                                    _type "button"
                                                                    _ariaLabel "Pin accounts"
                                                                    "Pin"
                                                                })
                                                                : HtmlElement)
                                                        }
                                                }
                                                fveSideNavItem { yield ("Unavailable": string) } ])
                                     }
                                     fveSideNavSection {
                                         yield _label ("Analyze")

                                         yield!
                                             ([ fveSideNavGroup {
                                                    yield _label ("Reporting")

                                                    yield!
                                                        ([ fveSideNavItem {
                                                               yield _href (shellTestUrl (Reports))
                                                               yield ("Reports": string)
                                                           } ])

                                                    yield fveSideNavLeading { yield (icon: HtmlElement) }
                                                    yield _expanded true
                                                } ])
                                     }
                                     fveSideNavSection {
                                         yield _label ("Configure")

                                         yield!
                                             ([ fveSideNavItem {
                                                    yield _href (shellTestUrl (Settings))
                                                    yield ("Settings": string)
                                                } ])
                                     } ])
                          }))

                      yield _value (shellTestUrl (Accounts))
                      yield Html._width SideNavWidth.Standard
                      yield fveSideNavContext { yield! ([ p { "Meier Made" } ]) }
                      yield fveSideNavMobileContext { yield ((p { "Meier Made mobile" }): HtmlElement) }

                      yield
                          fveSideNavFooter {
                              yield!
                                  ([ a {
                                         _href "/account"
                                         "Andrew Meier"
                                     } ])
                          }

                      yield
                          fveSideNavCompactFooter {
                              yield
                                  ((a {
                                      _href "/account"
                                      _ariaLabel "Andrew Meier profile"
                                      "AM"
                                  })
                                  : HtmlElement)
                          }
                  }

              let groupedHtml = grouped |> Render.toString
              Expect.stringContains groupedHtml "aria-label=\"Product navigation\"" "SideNav has its consumer label"
              Expect.stringContains groupedHtml "aria-label=\"Manage\"" "group hierarchy is semantic"
              Expect.stringContains groupedHtml "aria-current=\"page\"" "typed current destination is exposed"

              Expect.stringContains
                  groupedHtml
                  "aria-disabled=\"true\""
                  "unavailable navigation is semantic and non-interactive"

              Expect.stringContains groupedHtml "Meier Made" "optional context renders"
              Expect.stringContains groupedHtml "Andrew Meier" "optional footer renders"

              Expect.stringContains
                  groupedHtml
                  "<details open"
                  "nested navigation groups render with native disclosure semantics"

              Expect.stringContains groupedHtml "aria-label=\"Reporting\"" "nested groups retain an accessible name"

              Expect.stringContains
                  groupedHtml
                  "aria-label=\"Pin accounts\""
                  "item actions keep an independent accessible name"

              Expect.stringContains
                  groupedHtml
                  "data-on:click__stop=\"true\""
                  "item actions do not activate their navigation destination"

              Expect.stringContains groupedHtml ">4</span>" "navigation items support consumer-authored badges"

              Expect.equal
                  (Regex.Matches(groupedHtml, "aria-current=\"page\"").Count)
                  1
                  "SideNav exposes one current destination"

              let ungrouped =
                  fveSideNav {
                      yield _id ("compact-navigation")
                      yield _ariaLabel ("Compact navigation")
                      yield ((fveSideNavHeader { yield _label ("Treasury") }))

                      yield
                          ((fveSideNavContent {
                              yield!
                                  ([ fveSideNavItem {
                                         yield _href (shellTestUrl (Home))
                                         yield ("Overview": string)
                                     }
                                     fveSideNavItem {
                                         yield _href (shellTestUrl (Reports))
                                         yield ("Transactions": string)
                                     } ])
                          }))
                  }

              let ungroupedHtml = ungrouped |> Render.toString

              Expect.stringContains
                  ungroupedHtml
                  "aria-label=\"Compact navigation\""
                  "ungrouped navigation retains its name"

              Expect.isFalse
                  (ungroupedHtml.Contains("uppercase tracking-wide"))
                  "ungrouped navigation adds no invented group heading"

              Expect.isFalse
                  (ungroupedHtml.Contains("aria-current=\"page\""))
                  "SideNav permits routes without a selected primary destination"

              let pageActions =
                  div {
                      _class "flex flex-wrap gap-2"

                      fveButton {
                          _dataOn ("click", "$refreshes++")
                          "Refresh"
                      }

                      a {
                          _href (shellTestUrl Reports)
                          "View reports"
                      }

                      a {
                          _href (shellTestUrl Settings)
                          "Settings"
                      }

                      fveDropdownMenu {
                          yield _id ("account-page-overflow")
                          yield _ariaLabel ("More actions")
                          yield ((fveDropdownMenuTrigger { fveButton { yield ("More actions": string) } }))

                          yield!
                              ([ fveDropdownMenuItem {
                                     yield _href (shellTestUrl (Home))
                                     yield ("Home": string)
                                 } ])
                      }
                  }

              let pageHeader =
                  fvePageHeader {
                      yield _label ("Account 2048")
                      yield _description ("Operating checking")
                      yield fvePageHeaderActions { yield (pageActions) }
                  }

              let pageHeaderHtml = pageHeader |> Render.toString
              Expect.equal (Regex.Matches(pageHeaderHtml, "<h1").Count) 1 "PageHeader emits exactly one h1"
              Expect.stringContains pageHeaderHtml ">Account 2048</h1>" "the required title is visible"

              Expect.isFalse
                  (pageHeaderHtml.Contains("<h1 class=\"sr-only\""))
                  "PageHeader does not hide the page title"

              Expect.stringContains pageHeaderHtml "Operating checking" "PageHeader renders its optional subtitle"

              Expect.stringContains
                  pageHeaderHtml
                  "data-on:click=\"$refreshes++\""
                  "page commands remain buttons with trusted Datastar actions"

              Expect.stringContains pageHeaderHtml "href=\"/reports\"" "page destinations remain truthful links"

              Expect.stringContains
                  pageHeaderHtml
                  "aria-label=\"More actions\""
                  "additional page actions use accessible overflow"

              let nativeActions =
                  form {
                      _method "get"
                      _action "/reports"

                      input {
                          _name "range"
                          _value "monthly"
                      }

                      fveButton {
                          _type "submit"
                          "Apply"
                      }
                  }

              for rendered in
                  [ fvePageHeader {
                        yield _label ("Reports")
                        yield fvePageHeaderActions { yield (nativeActions) }
                    }
                    fveSectionHeader {
                        yield _label ("Reports")
                        yield fveSectionHeaderActions { yield (nativeActions) }
                    }
                    Collection.create "Reports" empty
                    |> Collection.withActions nativeActions
                    |> Collection.render
                    Detail.create "Reports" [] |> Detail.withActions nativeActions |> Detail.render ] do
                  Expect.stringContains
                      (Render.toString rendered)
                      (Render.toString nativeActions)
                      "action slots preserve arbitrary consumer-owned form markup"

              let sectionHtml =
                  fveSectionHeader {
                      yield _label ("Activity")
                      yield _description ("Recent account activity.")

                      yield
                          fveSectionHeaderActions {
                              yield
                                  ((a {
                                      _href (shellTestUrl Reports)
                                      "View reports"
                                  }))
                          }
                  }
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

              Expect.stringContains
                  collectionHtml
                  "<h2 class=\"sr-only\">Accounts</h2>"
                  "Collection stays labelled beneath a page-owned visible title"

              Expect.isFalse
                  (collectionHtml.Contains("rounded-[var(--fve-radius-panel)]"))
                  "Collection does not invent a card surface"

              Expect.isFalse
                  (collectionHtml.Contains("px-4"))
                  "Collection inherits the page gutter without double padding"

              Expect.stringContains
                  collectionHtml
                  "<header class=\"sr-only\">"
                  "Hidden collection titles do not leave a grid gap"

              let detailHtml =
                  Detail.create
                      "Operating checking"
                      [ section {
                            _ariaLabel "Account details"
                            "Account fields"
                        } ]
                  |> Detail.withVisuallyHiddenTitle
                  |> Detail.render
                  |> Render.toString

              Expect.stringContains
                  detailHtml
                  "<h2 class=\"sr-only\">Operating checking</h2>"
                  "Detail stays labelled beneath a page-owned visible title"

              Expect.stringContains
                  detailHtml
                  "<section aria-label=\"Account details\">Account fields</section>"
                  "Detail preserves consumer-owned semantic sections"

              Expect.isFalse
                  (detailHtml.Contains("rounded-[var(--fve-radius-panel)]"))
                  "Detail does not wrap every section in a card"

              Expect.isFalse
                  (detailHtml.Contains("border-b"))
                  "Detail uses section spacing instead of edge-to-edge bands"

              Expect.isFalse (detailHtml.Contains("px-4")) "Detail inherits the page gutter without double padding"

              let topBar =
                  fvePageTopBar {
                      yield
                          ((div {
                              _class "flex min-h-[var(--fve-shell-bar-min-height)] items-center"
                              breadcrumbs ()
                          }))
                  }

              let pageTabs =
                  fveTabs {
                      yield _id ("account-sections")
                      yield _ariaLabel ("Account sections")

                      yield!
                          ([ fveTabsItem {
                                 yield _value ("summary")
                                 yield fveTabsTrigger { yield ("Summary": string) }
                                 yield ((p { "Summary panel" }): HtmlElement)
                             }
                             fveTabsItem {
                                 yield _value ("activity")
                                 yield fveTabsTrigger { yield ("Activity": string) }
                                 yield ((p { "Activity panel" }): HtmlElement)
                             } ])

                      yield _variant (TabsVariant.Underlined)
                  }

              let fullBleedPageHtml =
                  Page.create pageHeader empty
                  |> Page.withTopBar topBar
                  |> Page.withTabs pageTabs
                  |> Page.withWidth PageWidth.Full
                  |> Page.withBodyLayout PageBodyLayout.FullBleed
                  |> Page.render
                  |> Render.toString

              Expect.stringContains
                  fullBleedPageHtml
                  "data-fve-page-top-bar=\"true\""
                  "Page owns the stable shell-aligned top bar"

              Expect.stringContains
                  fullBleedPageHtml
                  "<header data-fve-page-top-bar=\"true\""
                  "PageTopBar uses header semantics rather than claiming navigation"

              Expect.stringContains
                  fullBleedPageHtml
                  "min-h-[var(--fve-shell-bar-min-height)]"
                  "PageTopBar consumes the shared shell-bar height"

              Expect.stringContains fullBleedPageHtml "data-fve-page-scroll=\"true\"" "Page owns its scroll region"

              Expect.isTrue
                  (fullBleedPageHtml.IndexOf("data-fve-page-top-bar", StringComparison.Ordinal) < fullBleedPageHtml
                      .IndexOf("data-fve-page-scroll", StringComparison.Ordinal))
                  "PageTopBar remains outside scrolling content"

              Expect.stringContains fullBleedPageHtml "max-w-none" "Page owns full content width"
              Expect.isFalse (fullBleedPageHtml.Contains("lg:p-8")) "full-bleed Page omits body padding"
              Expect.stringContains fullBleedPageHtml "role=\"tablist\"" "Page accepts package Tabs as local navigation"

              let defaultPageHtml = Page.create pageHeader empty |> Page.render |> Render.toString
              Expect.stringContains defaultPageHtml "max-w-7xl" "Page uses a constrained application column by default"

              Expect.isFalse
                  (defaultPageHtml.Contains("data-fve-page-top-bar"))
                  "an unconfigured Page does not reserve an empty chrome row"

              let labelledTopBarHtml =
                  Page.create pageHeader empty
                  |> Page.withTopBar (fvePageTopBar { _ariaLabel "Workspace context" })
                  |> Page.render
                  |> Render.toString

              Expect.stringContains
                  labelledTopBarHtml
                  "aria-label=\"Workspace context\""
                  "explicitly attributed empty top bars remain intentional"

              let readingPageHtml =
                  Page.create pageHeader (p { "Reading content" })
                  |> Page.withSectionNavigation (
                      nav {
                          _ariaLabel "Article sections"

                          a {
                              _href "#summary"
                              "Summary"
                          }
                      }
                  )
                  |> Page.withWidth PageWidth.Reading
                  |> Page.render
                  |> Render.toString

              Expect.stringContains readingPageHtml "max-w-4xl" "Page owns semantic reading width"
              Expect.stringContains readingPageHtml "p-4 sm:p-6 lg:p-8" "padded Page supplies responsive body spacing"

              Expect.stringContains
                  readingPageHtml
                  "aria-label=\"Article sections\""
                  "Page accepts route section navigation"

              let standaloneBottomNavigationHtml =
                  fveBottomNavigation {
                      yield _id ("standalone-quick-navigation")
                      yield _ariaLabel ("Quick navigation")

                      yield!
                          ([ fveBottomNavigationItem {
                                 yield _href (shellTestUrl (Home))
                                 yield ("Home": string)
                             }
                             fveBottomNavigationItem {
                                 yield _href (shellTestUrl (Accounts))
                                 yield ("Accounts": string)
                             } ])

                      yield _value (shellTestUrl (Accounts))
                  }
                  |> Render.toString

              Expect.stringContains
                  standaloneBottomNavigationHtml
                  "<nav id=\"standalone-quick-navigation\" aria-label=\"Quick navigation\""
                  "BottomNavigation is a labelled navigation landmark"

              Expect.stringContains
                  standaloneBottomNavigationHtml
                  "href=\"/accounts\" aria-current=\"page\""
                  "BottomNavigation marks the supplied current destination"

              Expect.isFalse
                  (standaloneBottomNavigationHtml.Contains("role=\"tablist\""))
                  "BottomNavigation does not use tablist semantics"

              let shellHtml =
                  (AppShell.create ("product-shell") (grouped) ((div { "Route-owned page" }))
                   |> AppShell.withTheme (ComponentsTheme.emerald)
                   |> AppShell.withCollapsibleNavigation
                   |> AppShell.withMobileBottomNavigation (
                       fveBottomNavigation {
                           yield _id ("product-quick-navigation")
                           yield _ariaLabel ("Quick navigation")

                           yield!
                               ([ fveBottomNavigationItem {
                                      yield _href (shellTestUrl (Home))
                                      yield ("Home": string)
                                  }
                                  fveBottomNavigationItem {
                                      yield _href (shellTestUrl (Accounts))
                                      yield ("Accounts": string)
                                  }
                                  fveBottomNavigationItem {
                                      yield _href (shellTestUrl (Settings))
                                      yield ("Settings": string)
                                  } ])
                       }
                   )
                   |> AppShell.render)
                  |> Render.toString

              Expect.equal (Regex.Matches(shellHtml, "<main").Count) 1 "AppShell owns exactly one main landmark"

              Expect.equal
                  (Regex.Matches(shellHtml, "aria-label=\"Product navigation\"").Count)
                  1
                  "desktop and mobile share one navigation tree"

              Expect.equal
                  (Regex.Matches(shellHtml, "id=\"product-navigation\"").Count)
                  1
                  "responsive placement does not duplicate component IDs"

              Expect.isFalse (shellHtml.Contains("<h1")) "AppShell does not invent page identity"
              Expect.isFalse (shellHtml.Contains("max-w-7xl")) "AppShell does not own page width"
              Expect.stringContains shellHtml "aria-label=\"Open navigation\"" "mobile navigation has a named trigger"

              Expect.stringContains
                  shellHtml
                  "aria-label=\"Close navigation\""
                  "mobile navigation has a named close control"

              Expect.stringContains
                  shellHtml
                  "aria-label=\"Collapse navigation\""
                  "opt-in desktop icon rail has a named collapse control"

              Expect.stringContains
                  shellHtml
                  "Expand navigation"
                  "the rail control exposes its restored expanded action"

              Expect.stringContains
                  shellHtml
                  "md:w-[var(--fve-side-nav-width)]"
                  "the compact rail has an explicit desktop width contract"

              Expect.stringContains shellHtml "--fve-side-nav-width: 4rem" "the collapsed rail uses its compact width"

              Expect.stringContains
                  shellHtml
                  "data-attr:data-fve-collapsed"
                  "the navigation exposes collapsed state to authored regions"

              Expect.stringContains shellHtml "evt.key == &#39;Escape&#39;" "mobile navigation handles Escape dismissal"
              Expect.stringContains shellHtml "evt.key == &#39;Tab&#39;" "mobile navigation contains keyboard focus"
              Expect.stringContains shellHtml "?.focus()" "mobile navigation restores or moves focus intentionally"

              Expect.stringContains
                  shellHtml
                  "data-attr:inert"
                  "open mobile navigation removes the page from interaction"

              Expect.stringContains
                  shellHtml
                  "data-on:resize__window"
                  "desktop resize clears stale mobile overlay state"

              Expect.stringContains
                  shellHtml
                  "id=\"product-quick-navigation\" aria-label=\"Quick navigation\""
                  "AppShell renders the configured mobile bottom navigation"

              Expect.stringContains
                  shellHtml
                  "href=\"/accounts\" aria-current=\"page\""
                  "mobile bottom navigation derives current state from SideNav"

              Expect.isFalse
                  (shellHtml.Contains("role=\"tablist\""))
                  "mobile bottom navigation uses destinations rather than tabs"

              Expect.isFalse
                  (shellHtml.Contains("role=\"tab\""))
                  "mobile bottom navigation does not invent tab semantics"

              Expect.stringContains
                  shellHtml
                  "fve-theme-emerald"
                  "AppShell applies one consumer-controlled semantic theme"

              let embeddedShellHtml =
                  (AppShell.create ("embedded-shell") (grouped) (empty)
                   |> AppShell.withTheme (ComponentsTheme.cyan)
                   |> AppShell.withBreakpoint (AppShellBreakpoint.Large)
                   |> AppShell.withBoundary (AppShellBoundary.Container)
                   |> AppShell.render)
                  |> Render.toString

              Expect.stringContains
                  embeddedShellHtml
                  "h-full min-h-0"
                  "container-bound shells stay within their embedding boundary"

              Expect.stringContains
                  embeddedShellHtml
                  "lg:hidden"
                  "large-breakpoint shells retain mobile controls below large screens"

              Expect.stringContains
                  embeddedShellHtml
                  "lg:w-60"
                  "large-breakpoint shells preserve typed navigation width"

              Expect.stringContains
                  embeddedShellHtml
                  "fve-theme-cyan"
                  "additional semantic palettes are application-selectable"

              let previewShellHtml =
                  (AppShell.create ("preview-shell") (grouped) (empty)
                   |> AppShell.asPreview ("Product preview")
                   |> AppShell.render)
                  |> Render.toString

              Expect.equal
                  (Regex.Matches(previewShellHtml, "<main").Count)
                  0
                  "embedded AppShell previews do not nest main landmarks"

              Expect.stringContains
                  previewShellHtml
                  "<section aria-label=\"Product preview\""
                  "embedded AppShell previews retain a named landmark"

              Expect.throws
                  (fun () ->
                      (AppShell.create ("invalid-preview") (grouped) (empty)
                       |> AppShell.asPreview (" "))
                      |> ignore)
                  "embedded AppShell previews require a landmark label"

              let adjacentShells =
                  div {
                      yield (AppShell.create ("first-shell") (grouped) ((div { "First" })) |> AppShell.render)

                      yield
                          (AppShell.create ("second-shell") (ungrouped) ((div { "Second" }))
                           |> AppShell.render)
                  }
                  |> Render.toString

              let ids =
                  Regex.Matches(adjacentShells, " id=\"([^\"]+)\"")
                  |> Seq.cast<Match>
                  |> Seq.map (fun matched -> matched.Groups[1].Value)
                  |> Seq.toList

              Expect.equal
                  ids.Length
                  (ids |> List.distinct |> List.length)
                  "adjacent shell instances retain independent IDs"

              Expect.stringContains
                  adjacentShells
                  "_app_shell_v66697273742d7368656c6c_navigation_open"
                  "first shell signal is collision-safe"

              Expect.stringContains
                  adjacentShells
                  "_app_shell_v7365636f6e642d7368656c6c_navigation_open"
                  "second shell signal is collision-safe"

              Expect.throws
                  (fun () ->
                      fveBreadcrumbItem {
                          yield _href (shellTestUrl (Home))
                          yield (" ": string)
                      }
                      |> ignore)
                  "breadcrumb items require labels"

              Expect.throws
                  (fun () ->
                      fveBreadcrumbs {
                          yield _id (" ")
                          yield _ariaLabel ("Breadcrumb")

                          yield!
                              ([ fveBreadcrumbItem {
                                     yield _href (shellTestUrl (Home))
                                     yield ("Home": string)
                                 } ])
                      }
                      |> ignore)
                  "Breadcrumbs requires a stable ID"

              Expect.throws
                  (fun () ->
                      fveBreadcrumbs {
                          yield _id ("crumbs")
                          yield _ariaLabel (" ")

                          yield!
                              ([ fveBreadcrumbItem {
                                     yield _href (shellTestUrl (Home))
                                     yield ("Home": string)
                                 } ])
                      }
                      |> ignore)
                  "Breadcrumbs requires an accessible label"

              Expect.throws
                  (fun () ->
                      fveBreadcrumbs {
                          _id "crumbs"
                          _ariaLabel "Breadcrumb"
                      }
                      |> ignore)
                  "Breadcrumbs rejects an empty path"

              Expect.throws
                  (fun () ->
                      fveSideNavItem {
                          yield _href (shellTestUrl (Home))
                          yield (" ": string)
                      }
                      |> ignore)
                  "side-navigation items require labels"

              Expect.throws
                  (fun () -> fveSideNavSection { yield _label ("Empty") } |> ignore)
                  "SideNav rejects an empty section"

              Expect.throws
                  (fun () -> fveSideNavGroup { yield _label ("Manage") } |> ignore)
                  "SideNav rejects an empty group"

              Expect.throws
                  (fun () -> fveSideNavHeader { yield _label (" ") } |> ignore)
                  "SideNavHeader requires an accessible identity"

              Expect.throws
                  (fun () ->
                      fveSideNav {
                          yield _id (" ")
                          yield _ariaLabel ("Navigation")

                          yield
                              ((fveSideNavContent {
                                  yield!
                                      ([ fveSideNavItem {
                                             yield _href (shellTestUrl (Home))
                                             yield ("Home": string)
                                         } ])
                              }))
                      }
                      |> ignore)
                  "SideNav requires a stable ID"

              Expect.throws
                  (fun () ->
                      fveSideNav {
                          yield _id ("nav")
                          yield _ariaLabel (" ")

                          yield
                              ((fveSideNavContent {
                                  yield!
                                      ([ fveSideNavItem {
                                             yield _href (shellTestUrl (Home))
                                             yield ("Home": string)
                                         } ])
                              }))
                      }
                      |> ignore)
                  "SideNav requires an accessible label"

              Expect.throws
                  (fun () ->
                      fveSideNav {
                          _id "missing-current"
                          _ariaLabel "Navigation"
                          _value (shellTestUrl Settings)

                          fveSideNavContent {
                              fveSideNavItem {
                                  _href (shellTestUrl Home)
                                  "Home"
                              }
                          }
                      }
                      |> ignore)
                  "SideNav rejects an unrepresented current destination"

              Expect.throws
                  (fun () ->
                      fveSideNav {
                          yield _id ("nav")
                          yield _ariaLabel ("Navigation")

                          yield
                              ((fveSideNavContent {
                                  yield!
                                      ([ fveSideNavItem {
                                             yield _href (shellTestUrl (Home))
                                             yield ("Home": string)
                                         }
                                         fveSideNavItem {
                                             yield _href (shellTestUrl (Home))
                                             yield ("Duplicate": string)
                                         } ])
                              }))
                      }
                      |> ignore)
                  "SideNav rejects duplicate destinations"

              Expect.throws (fun () -> fvePageHeader { _label " " } |> ignore) "PageHeader requires a route title"

              Expect.throws
                  (fun () ->
                      fvePageHeader {
                          _label "Title"
                          _description " "
                      }
                      |> ignore)
                  "PageHeader rejects an empty subtitle"

              Expect.throws
                  (fun () ->
                      fveSectionHeader {
                          _label "Activity"
                          _description " "
                      }
                      |> ignore)
                  "SectionHeader rejects empty supporting text"

              Expect.throws
                  (fun () ->
                      fveBottomNavigation {
                          yield _id (" ")
                          yield _ariaLabel ("Quick navigation")

                          yield!
                              ([ fveBottomNavigationItem {
                                     yield _href (shellTestUrl (Home))
                                     yield ("Home": string)
                                 } ])
                      }
                      |> ignore)
                  "BottomNavigation requires a stable ID"

              Expect.throws
                  (fun () ->
                      fveBottomNavigation {
                          yield _id ("quick-navigation")
                          yield _ariaLabel (" ")

                          yield!
                              ([ fveBottomNavigationItem {
                                     yield _href (shellTestUrl (Home))
                                     yield ("Home": string)
                                 } ])
                      }
                      |> ignore)
                  "BottomNavigation requires an accessible label"

              Expect.throws
                  (fun () ->
                      fveBottomNavigation {
                          yield _id ("quick-navigation")
                          yield _ariaLabel ("Quick navigation")
                      }
                      |> ignore)
                  "BottomNavigation rejects an empty item list"

              Expect.throws
                  (fun () ->
                      fveBottomNavigation {
                          yield _id ("quick-navigation")
                          yield _ariaLabel ("Quick navigation")

                          yield!
                              ([ fveBottomNavigationItem {
                                     yield _href (shellTestUrl (Home))
                                     yield ("Home": string)
                                 }
                                 fveBottomNavigationItem {
                                     yield _href (shellTestUrl (Home))
                                     yield ("Home again": string)
                                 } ])
                      }
                      |> ignore)
                  "BottomNavigation rejects duplicate destinations"

              Expect.throws
                  (fun () -> (AppShell.create (" ") (grouped) (empty)) |> ignore)
                  "AppShell requires a stable ID"

              Expect.throws
                  (fun () ->
                      (AppShell.create
                          ("same-id")
                          ((fveSideNav {
                              yield _id ("same-id")
                              yield _ariaLabel ("Navigation")

                              yield
                                  ((fveSideNavContent {
                                      yield!
                                          ([ fveSideNavItem {
                                                 yield _href (shellTestUrl (Home))
                                                 yield ("Home": string)
                                             } ])
                                  }))
                          }))
                          (empty))
                      |> ignore)
                  "AppShell and SideNav IDs must differ"

              Expect.throws
                  (fun () ->
                      (AppShell.create ("product-shell") (grouped) (empty)
                       |> AppShell.withMobileBottomNavigation (
                           fveBottomNavigation {
                               yield _id ("product-shell")
                               yield _ariaLabel ("Quick navigation")

                               yield!
                                   ([ fveBottomNavigationItem {
                                          yield _href (shellTestUrl (Home))
                                          yield ("Home": string)
                                      } ])
                           }
                       ))
                      |> ignore)
                  "AppShell requires distinct bottom-navigation IDs"

              Expect.throws
                  (fun () ->
                      (AppShell.create ("product-shell") (grouped) (empty)
                       |> AppShell.withMobileBottomNavigation (
                           fveBottomNavigation {
                               yield _id ("quick-navigation")
                               yield _ariaLabel ("Quick navigation")

                               yield!
                                   ([ fveBottomNavigationItem {
                                          yield _href (shellTestUrl (Reports))
                                          yield ("Reports": string)
                                      } ])
                           }
                       ))
                      |> ignore)
                  "AppShell rejects mobile destinations missing from SideNav"
          }

          test "Tabs render typed variants, collision-safe relationships, and isolated automatic activation" {
              let first =
                  fveTabs {
                      yield _id ("account-tabs")
                      yield _ariaLabel ("Account sections")

                      yield!
                          ([ fveTabsItem {
                                 yield _value ("overview")
                                 yield fveTabsTrigger { yield ("Overview": string) }
                                 yield ((p { "Summary" }): HtmlElement)
                             }
                             fveTabsItem {
                                 yield _value ("tax.reserve")
                                 yield fveTabsTrigger { yield ("Tax reserve": string) }

                                 yield
                                     ((a {
                                         _href "/tax"
                                         "Tax settings"
                                     })
                                     : HtmlElement)
                             } ])

                      yield _value ("tax.reserve")
                      yield _variant (TabsVariant.Underlined)
                  }
                  |> Render.toString

              Expect.stringContains first "id=\"account-tabs\"" "Tabs retain the consumer stable ID"

              Expect.stringContains
                  first
                  "role=\"tablist\" aria-label=\"Account sections\" aria-orientation=\"horizontal\""
                  "tab list has required accessible identity"

              Expect.equal (Regex.Matches(first, "role=\"tab\"").Count) 2 "one tab per item"
              Expect.equal (Regex.Matches(first, "role=\"tabpanel\"").Count) 2 "one panel per item"

              Expect.stringContains
                  first
                  "id=\"account-tabs-tab-v7461782e72657365727665\""
                  "UTF-8 hex identity preserves punctuation without collisions"

              Expect.stringContains
                  first
                  "aria-controls=\"account-tabs-panel-v7461782e72657365727665\""
                  "tab controls its stable panel"

              Expect.stringContains
                  first
                  "aria-labelledby=\"account-tabs-tab-v7461782e72657365727665\""
                  "panel is labelled by its tab"

              Expect.stringContains first "aria-selected=\"true\"" "selected tab is exposed"

              Expect.equal
                  (Regex.Matches(first, "tabindex=\"0\"").Count)
                  3
                  "one selected tab and both panels remain keyboard reachable"

              Expect.stringContains
                  first
                  "hidden data-attr:hidden"
                  "inactive panels leave interaction and accessibility trees"

              Expect.stringContains first "evt.key == &#39;ArrowLeft&#39;" "Left Arrow is handled"
              Expect.stringContains first "evt.key == &#39;ArrowRight&#39;" "Right Arrow is handled"
              Expect.stringContains first "evt.key == &#39;Home&#39;" "Home is handled"
              Expect.stringContains first "evt.key == &#39;End&#39;" "End is handled"

              Expect.stringContains
                  first
                  "aria-selected:border-[var(--fve-brand-solid)]"
                  "Underlined variant uses semantic selected treatment"

              let adjacent =
                  div {
                      fveTabs {
                          yield _id ("first-tabs")
                          yield _ariaLabel ("First views")

                          yield!
                              ([ fveTabsItem {
                                     yield _value ("same")
                                     yield fveTabsTrigger { yield ("Same": string) }
                                     yield ((p { "First" }): HtmlElement)
                                 } ])
                      }

                      fveTabs {
                          yield _id ("second-tabs")
                          yield _ariaLabel ("Second views")

                          yield!
                              ([ fveTabsItem {
                                     yield _value ("same")
                                     yield fveTabsTrigger { yield ("Same": string) }
                                     yield ((p { "Second" }): HtmlElement)
                                 } ])
                      }
                  }
                  |> Render.toString

              let ids =
                  Regex.Matches(adjacent, " id=\"([^\"]+)\"")
                  |> Seq.cast<Match>
                  |> Seq.map (fun matched -> matched.Groups[1].Value)
                  |> Seq.toList

              Expect.equal
                  ids.Length
                  (ids |> List.distinct |> List.length)
                  "adjacent instances produce no duplicate IDs"

              Expect.stringContains
                  adjacent
                  "_tabs_v66697273742d74616273_selected"
                  "first signal is collision-safe and local"

              Expect.stringContains
                  adjacent
                  "_tabs_v7365636f6e642d74616273_selected"
                  "second signal is collision-safe and local"

              let duplicateItems =
                  [ fveTabsItem {
                        yield _value ("same")
                        yield fveTabsTrigger { yield ("One": string) }
                        yield ((p { "One" }): HtmlElement)
                    }
                    fveTabsItem {
                        yield _value ("same")
                        yield fveTabsTrigger { yield ("Two": string) }
                        yield ((p { "Two" }): HtmlElement)
                    } ]

              Expect.throws
                  (fun () ->
                      fveTabs {
                          _id "tabs"
                          _ariaLabel "Views"
                      }
                      |> ignore)
                  "Tabs reject empty item sets"

              Expect.throws
                  (fun () ->
                      fveTabs {
                          yield _id ("tabs")
                          yield _ariaLabel ("Views")
                          yield! (duplicateItems)
                      }
                      |> ignore)
                  "Tabs reject duplicate item IDs"

              Expect.throws
                  (fun () ->
                      fveTabs {
                          yield _id ("tabs")
                          yield _ariaLabel ("Views")

                          yield!
                              ([ fveTabsItem {
                                     yield _value ("one")
                                     yield fveTabsTrigger { yield ("One": string) }
                                     yield ((p { "One" }): HtmlElement)
                                 } ])

                          yield _value ("missing")
                      }
                      |> ignore)
                  "Tabs reject unknown selected items"

              Expect.throws
                  (fun () ->
                      fveTabsItem {
                          yield _value (" ")
                          yield fveTabsTrigger { yield ("One": string) }
                          yield ((p { "One" }): HtmlElement)
                      }
                      |> ignore)
                  "Tab rejects whitespace IDs"

              Expect.throws
                  (fun () ->
                      fveTabs {
                          yield _id ("tabs")
                          yield _ariaLabel (" ")

                          yield!
                              ([ fveTabsItem {
                                     yield _value ("one")
                                     yield fveTabsTrigger { yield ("One": string) }
                                     yield ((p { "One" }): HtmlElement)
                                 } ])
                      }
                      |> ignore)
                  "Tabs require an accessible group label"
          }

          test
              "Dialog overlays preserve native modal semantics, safe confirmation state, and responsive drawer identity" {
              let dialogConfig =
                  fveDialog {
                      _id "test-dialog"
                      _label "Review account"
                      _description "Settings remain unchanged until saved."
                      _initialFocus "test-dialog-close"
                      _dismissOnBackdrop true
                      _role "alertdialog"
                      _dataSignals "{saving: false}"
                      _dataOn ("cancel", "$saving && evt.preventDefault()")
                      _class "custom-dialog"
                      _ariaLabelledby "ignored-label"
                      _ariaDescribedby "external-help"
                      _ariaModal false
                      _dataOn ("close", "$closed = true")
                      _dataOn ("click", "$clicked = true")
                      p { "Review the settings." }

                      fveDialogFooter {
                          fveDialogClose {
                              _id "test-dialog-close"
                              _ariaControls "test-dialog"
                              "Close"
                          }
                      }
                  }

              let dialogHtml =
                  div {
                      fveDialogTrigger {
                          fveButton {
                              _ariaControls "test-dialog"
                              "Review account"
                          }
                      }

                      dialogConfig
                  }
                  |> Render.toString

              Expect.stringContains dialogHtml "<dialog id=\"test-dialog\"" "Dialog remains a native dialog"
              Expect.stringContains dialogHtml "aria-modal=\"true\"" "Dialog exposes modal semantics"

              Expect.stringContains
                  dialogHtml
                  "evt.target == evt.currentTarget"
                  "Dialog can opt into backdrop dismissal"

              Expect.stringContains
                  dialogHtml
                  "document.getElementById(&quot;test-dialog-trigger&quot;)?.focus()"
                  "Dialog restores its connected trigger"

              Expect.stringContains dialogHtml "role=\"alertdialog\"" "consumers can choose alert-dialog semantics"
              Expect.stringContains dialogHtml "data-signals=\"{saving: false}\"" "consumers own transient state"

              Expect.stringContains
                  dialogHtml
                  "data-on:cancel=\"$saving &amp;&amp; evt.preventDefault()\""
                  "consumers can protect in-flight work"

              Expect.isFalse
                  (dialogHtml.Contains "ignored-label")
                  "the generated visible title owns the accessible dialog name"

              for expected in [ "custom-dialog"; "external-help"; "$closed = true"; "$clicked = true" ] do
                  Expect.stringContains
                      dialogHtml
                      expected
                      "ordinary styling, descriptions and lifecycle actions compose with owned behavior"

              let confirmationHtml = Components.itemDeletionDialog |> Render.toString

              Expect.stringContains
                  confirmationHtml
                  "role=\"alertdialog\""
                  "the confirmation example composes alert-dialog semantics"

              Expect.stringContains
                  confirmationHtml
                  "aria-describedby=\"delete-item-confirmation-description\""
                  "the confirmation message remains described"

              Expect.stringContains
                  confirmationHtml
                  "data-indicator:_delete_item_confirmation_pending"
                  "the native form request drives pending state"

              Expect.stringContains
                  confirmationHtml
                  "data-attr:disabled=\"$_delete_item_confirmation_pending\""
                  "request pending prevents repeated activation"

              Expect.stringContains
                  confirmationHtml
                  "id=\"delete-item-confirmation-cancel\""
                  "the least-destructive action is consumer-authored"

              Expect.stringContains confirmationHtml "color=\"error\"" "the submit Button retains the Error palette"

              Expect.stringContains
                  confirmationHtml
                  "Confirmation in progress."
                  "request pending retains explanatory text"

              Expect.stringContains
                  confirmationHtml
                  "data-show=\"$_delete_item_confirmation_pending\""
                  "pending presentation reads the real request signal"

              let validationHtml =
                  Components.itemDeletionContent (Some "The value is still referenced.")
                  |> Render.toString

              Expect.stringContains validationHtml "role=\"alert\"" "server validation is announced"
              Expect.stringContains validationHtml "The value is still referenced." "server validation remains visible"

              let drawerBody =
                  nav {
                      _ariaLabel "Account settings"

                      a {
                          _href "/accounts"
                          "Accounts"
                      }
                  }

              let drawer (side: DrawerSide) (size: DrawerSize) (container: string option) opened =
                  fveDrawer {
                      _id "settings-drawer"
                      _label "Settings"
                      _side side
                      _size size

                      match container with
                      | Some id -> _container id
                      | None -> ()

                      _open opened
                      _class "custom-drawer"
                      _ariaModal false
                      _dataOn ("close", "$closed = true")
                      _dataOn ("cancel", "evt.preventDefault()")
                      drawerBody
                  }

              let endDrawer = drawer DrawerSide.End DrawerSize.Standard None false

              let endDrawerHtml =
                  div {
                      fveDrawerTrigger {
                          fveButton {
                              _ariaControls "settings-drawer"
                              "Open settings"
                          }
                      }

                      endDrawer
                  }
                  |> Render.toString

              Expect.stringContains endDrawerHtml "<dialog id=\"settings-drawer\"" "Drawer remains a native dialog"

              Expect.stringContains
                  endDrawerHtml
                  "custom-drawer"
                  "consumer classes compose with native drawer presentation"

              Expect.stringContains
                  endDrawerHtml
                  "$closed = true"
                  "consumer close actions compose with focus restoration"

              Expect.stringContains
                  endDrawerHtml
                  "data-on:cancel=\"evt.preventDefault()\""
                  "consumer lifecycle guards are retained"

              Expect.stringContains
                  endDrawerHtml
                  "inset-y-0 right-0 ml-auto mr-0 h-dvh border-l"
                  "Drawer defaults to the end edge"

              Expect.stringContains
                  endDrawerHtml
                  "w-full max-w-full sm:w-96"
                  "Drawer fills mobile widths and bounds its desktop surface"

              Expect.stringContains endDrawerHtml "aria-label=\"Account settings\"" "consumer landmarks are preserved"
              Expect.stringContains endDrawerHtml "id=\"settings-drawer-close\"" "Drawer owns a stable close target"

              Expect.stringContains
                  endDrawerHtml
                  "evt.target == evt.currentTarget"
                  "Drawer backdrop dismisses explicitly"

              let startDrawerHtml =
                  drawer DrawerSide.Start DrawerSize.Standard None false |> Render.toString

              Expect.stringContains
                  startDrawerHtml
                  "inset-y-0 left-0 ml-0 mr-auto h-dvh border-r"
                  "Drawer supports the typed start edge"

              let wideDrawerHtml =
                  drawer DrawerSide.End DrawerSize.Large None false |> Render.toString

              Expect.stringContains
                  wideDrawerHtml
                  "w-full max-w-full sm:w-[42rem]"
                  "Drawer supports a typed large editing surface"

              let topDrawerHtml =
                  drawer DrawerSide.Top DrawerSize.Standard None false |> Render.toString

              Expect.stringContains topDrawerHtml "inset-x-0 top-0" "Drawer supports the typed top edge"

              let bottomDrawerHtml =
                  drawer DrawerSide.Bottom DrawerSize.Large None false |> Render.toString

              Expect.stringContains bottomDrawerHtml "inset-x-0 bottom-0" "Drawer supports the typed bottom edge"

              Expect.stringContains
                  bottomDrawerHtml
                  "h-[min(36rem,calc(100%-3rem))]"
                  "horizontal drawers use typed heights"

              let contained =
                  drawer DrawerSide.End DrawerSize.Standard (Some "app-background") true

              let containedHtml =
                  div {
                      fveDrawerTrigger {
                          fveButton {
                              _ariaControls "settings-drawer"
                              "Open settings"
                          }
                      }

                      contained
                  }
                  |> Render.toString

              Expect.isTrue
                  (Regex.IsMatch(containedHtml, "<dialog[^>]*aria-modal=\"false\"[^>]* open(?:\\s|>)"))
                  "a server-owned contained state is natively open, not document-modal"

              Expect.stringContains
                  containedHtml
                  "data-fve-overlay-background=\"app-background\""
                  "only the named app background is affected"

              Expect.stringContains
                  containedHtml
                  "data-fve-contained=\"true\""
                  "the trigger reads the actual dialog's contained presentation"

              let containedDialogHtml =
                  fveDialog {
                      yield _id ("app-confirmation")
                      yield _label ("Confirm")
                      yield ((text "Continue?"): HtmlElement)
                      yield _container ("app-background")
                      yield _open (true)
                  }
                  |> Render.toString

              Expect.stringContains
                  containedDialogHtml
                  "aria-modal=\"false\" open"
                  "contained confirmation has the same native visibility contract"

              Expect.throws
                  (fun () -> drawer DrawerSide.End DrawerSize.Standard None true |> ignore)
                  "initial contained visibility cannot silently degrade into a non-modal standalone drawer"
          }

          test "Components foundations preserve accessible names, honest states, and protected structure" {
              let icon = span { "+" }

              let pendingButton =
                  fveButton {
                      _ariaBusy false
                      _class "override"
                      _dataOn ("click", "@post('/components/dialogs/confirm')")
                      _dataIndicator "_sync_pending"
                      "Sync accounts"
                  }
                  |> Render.toString

              Expect.stringContains pendingButton "disabled" "pending Button prevents activation"

              Expect.stringContains
                  pendingButton
                  "data-attr:aria-busy=\"$_sync_pending ? &#39;true&#39; : null\""
                  "signal drives busy state"

              Expect.stringContains pendingButton "data-indicator:_sync_pending" "consumer indicator passes through"
              Expect.stringContains pendingButton "data-on:click" "consumer action passes through"

              Expect.isFalse
                  (pendingButton.Contains "data-fve-button-spinner")
                  "Rocket creates the spinner rather than the F# renderer"

              Expect.isFalse (pendingButton.Contains " disabled=") "pending is not permanently disabled"

              Expect.throws
                  (fun () ->
                      fveButton {
                          _dataIndicator "$bad"
                          "Update"
                      }
                      |> ignore)
                  "signal names exclude expression syntax"

              Expect.stringContains pendingButton ">Sync accounts<" "pending Button retains its action label"

              Expect.equal
                  (Regex.Matches(pendingButton, " data-attr:aria-busy=\"").Count)
                  1
                  "Button owns one busy binding"

              Expect.stringContains pendingButton "override" "consumer classes extend Button presentation"

              let iconButton =
                  fveButton {
                      _color Color.Primary
                      _variant ButtonVariant.Solid
                      _ariaLabel "Add account"

                      span {
                          _ariaHidden true
                          _class "inline-flex shrink-0"
                          icon
                      }
                  }
                  |> Render.toString

              Expect.stringContains
                  iconButton
                  "aria-label=\"Add account\""
                  "icon-only Button requires an accessible name"

              Expect.stringContains iconButton "aria-hidden=\"true\"" "icon-only Button hides decorative icon content"
              Expect.isFalse (iconButton.Contains "min-h-") "Rocket owns button layout and appearance"

              Expect.throws
                  (fun () ->
                      fveButton {
                          _ariaLabel " "
                          icon
                      }
                      |> ignore)
                  "explicit accessible names cannot be blank"

              let pendingIconButton =
                  fveButton {
                      _dataIndicator "_icon_pending"
                      _ariaLabel "Refresh accounts"
                      _ariaBusy false
                      _class "override"

                      span {
                          _ariaHidden true
                          _class "inline-flex shrink-0"
                          icon
                      }
                  }
                  |> Render.toString

              Expect.stringContains
                  pendingIconButton
                  "aria-label=\"Refresh accounts\""
                  "pending icon-only Button retains its accessible name"

              Expect.stringContains
                  pendingIconButton
                  "data-attr:aria-busy=\"$_icon_pending ? &#39;true&#39; : null\""
                  "pending icon-only Button exposes busy state"

              Expect.stringContains pendingIconButton "disabled" "pending icon-only Button prevents activation"

              Expect.equal
                  (Regex.Matches(pendingIconButton, " aria-label=").Count)
                  1
                  "pending content keeps one native accessible name"

              Expect.stringContains pendingIconButton "override" "icon-only content also supports consumer classes"

              let iconTextButton =
                  fveButton {
                      span {
                          _ariaHidden true
                          "Before"
                      }

                      "Continue"
                  }
                  |> Render.toString

              Expect.isLessThan
                  (iconTextButton.IndexOf("Before"))
                  (iconTextButton.IndexOf("Continue"))
                  "IconText renders its decorative icon before the text"

              Expect.equal
                  (Regex.Matches(iconTextButton, "aria-hidden=\"true\"").Count)
                  1
                  "IconText hides its icon from the accessible name"

              let textIconButton =
                  fveButton {
                      "Continue"

                      span {
                          _ariaHidden true
                          "After"
                      }
                  }
                  |> Render.toString

              Expect.isLessThan
                  (textIconButton.IndexOf("Continue"))
                  (textIconButton.IndexOf("After"))
                  "TextIcon renders its decorative icon after the text"

              Expect.equal
                  (Regex.Matches(textIconButton, "aria-hidden=\"true\"").Count)
                  1
                  "TextIcon hides its icon from the accessible name"

              let pendingIconTextButton =
                  fveButton {
                      _dataIndicator "_icon_text_pending"

                      span {
                          _ariaHidden true
                          "Before"
                      }

                      "Continue"
                  }
                  |> Render.toString

              Expect.stringContains pendingIconTextButton ">Continue<" "pending IconText retains its action label"

              Expect.isFalse
                  (pendingIconTextButton.Contains "data-fve-button-content")
                  "ordinary content is not pre-wrapped in generated internals"

              let customButton =
                  fveButton {
                      _dataIndicator "_custom_pending"
                      strong { "Custom" }
                      span { " content" }
                  }
                  |> Render.toString

              Expect.stringContains
                  customButton
                  "<strong>Custom</strong><span> content</span>"
                  "Custom content remains consumer-authored and in layout while pending"

              Expect.stringContains
                  customButton
                  "data-attr:aria-busy=\"$_custom_pending ? &#39;true&#39; : null\""
                  "Custom content retains standard pending semantics"

              for wire, variant in
                  [ "solid", ButtonVariant.Solid
                    "soft", ButtonVariant.Soft
                    "outline", ButtonVariant.Outline
                    "ghost", ButtonVariant.Ghost ] do
                  let button =
                      fveButton {
                          _variant variant
                          "Action"
                      }
                      |> Render.toString

                  Expect.stringContains button ($"variant=\"{wire}\"") "appearance is a real host attribute"
                  Expect.isFalse (button.Contains "<button") "native internals are runtime-owned"

              let badge =
                  fveBadge {
                      _color Color.Success
                      _class "override"
                      "Reconciled"
                  }
                  |> Render.toString

              Expect.stringContains badge "Reconciled" "Badge communicates category through text"
              Expect.stringContains badge "var(--fve-success-solid," "Badge consumes its component-owned Success color"
              Expect.stringContains badge "override" "Badge composes consumer classes"
              Expect.stringContains badge "inline-flex" "Badge retains its base presentation"

              let subtleBadgeOutline =
                  fveBadge {
                      _color Color.Success
                      _variant BadgeVariant.Outline
                      "Outline"
                  }
                  |> Render.toString

              Expect.stringContains
                  subtleBadgeOutline
                  "color-mix(in_srgb,var(--fve-color-border)_20%,transparent)"
                  "Badge and Button outlines share the subtle palette border"

              let loading =
                  fveLoadingIndicator {
                      _ariaLabel "Loading balances"
                      _role "alert"
                      _ariaLive "assertive"
                      _class "override"
                  }
                  |> Render.toString

              Expect.stringContains loading "role=\"status\"" "LoadingIndicator exposes polite status semantics"
              Expect.stringContains loading "aria-live=\"polite\"" "LoadingIndicator owns its announcement behavior"
              Expect.stringContains loading "Loading balances" "LoadingIndicator retains its accessible label"
              Expect.stringContains loading "class=\"sr-only\"" "compact loading label is visually hidden"
              Expect.isFalse (loading.Contains("alert")) "LoadingIndicator protects its role"
              Expect.stringContains loading "override" "LoadingIndicator composes consumer classes"
              Expect.stringContains loading "inline-flex" "LoadingIndicator retains its base presentation"

              let visibleLoading =
                  fveLoadingIndicator {
                      _ariaLabel "Refreshing entries"
                      "Refreshing entries"
                  }
                  |> Render.toString

              Expect.isFalse (visibleLoading.Contains("sr-only")) "visible loading label remains visible"

              let emptyState =
                  fveEmptyState {
                      _label "No accounts"
                      _description "Create an account to begin."
                      _class "override"
                      fveEmptyStateIcon { icon }

                      fveEmptyStateActions {
                          fveButton {
                              _color Color.Primary
                              _variant ButtonVariant.Solid
                              "Create account"
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains emptyState "No accounts" "EmptyState renders its title"
              Expect.stringContains emptyState "Create an account to begin." "EmptyState renders useful guidance"
              Expect.stringContains emptyState "aria-hidden=\"true\"" "EmptyState icon is decorative"
              Expect.stringContains emptyState "Create account" "EmptyState composes application-owned actions"
              Expect.stringContains emptyState "override" "EmptyState composes consumer classes"
              Expect.stringContains emptyState "justify-items-center" "EmptyState retains its base presentation"
          }

          test "Button serializes palette data and preserves consumer attributes and encoded content" {
              let custom =
                  fveButton {
                      _color Color.Custom
                      _palette Components.customButtonPalette
                      _variant ButtonVariant.Soft
                      _style "margin:1px"
                      "Save <draft>"
                  }
                  |> Render.toString

              for expected in
                  [ "color=\"custom\""
                    "variant=\"soft\""
                    "palette="
                    "#9a3412"
                    "#fdba74"
                    "margin:1px"
                    "Save &lt;draft&gt;" ] do
                  Expect.stringContains
                      custom
                      expected
                      "the thin host preserves the consumer's complete public contract"

              Expect.equal (Regex.Matches(custom, " style=\"").Count) 1 "consumer styles have one authored owner"

              for generated in
                  [ "<button"
                    "--fve-color-solid"
                    "data-fve-button-spinner"
                    "data-fve-button-content" ] do
                  Expect.isFalse (custom.Contains generated) "rendered HTML does not contain Rocket-owned internals"
          }

          test "Table records, selection, canvas, and optional section headers preserve their contracts" {
              let html = Components.selectableTeamTable |> Render.toString
              Expect.equal (Regex.Matches(html, "<fve-table ").Count) 1 "one presentation host owns the native parts"
              Expect.stringContains html "name=\"memberIds\"" "application-owned native checkboxes submit record keys"
              Expect.stringContains html "el.indeterminate" "the application composes its mixed select-all state"
              Expect.stringContains html "fve-table-selection-change" "applications publish selected key changes"
              Expect.stringContains html "tableMemberSelection" "selection remains outside a component signal scope"

              let sortedHtml =
                  fveTable {
                      _ariaLabel "Sortable records"
                      _mobileLayout MobileLayout.Records

                      fveTableHeader {
                          fveTableRow {
                              fveTableHead {
                                  _ariaSort "ascending"

                                  a {
                                      _href "/records?sort=name&direction=desc"
                                      "Name"
                                  }
                              }

                              fveTableHead {
                                  a {
                                      _href "/records?sort=role&direction=asc"
                                      "Role"
                                  }
                              }
                          }
                      }

                      fveTableBody {
                          fveTableRow {
                              fveTableHead {
                                  _scope "row"
                                  _mobile Mobile.Primary
                                  "Alex"
                              }

                              fveTableCell {
                                  _label "Role"
                                  "Member"
                              }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains sortedHtml "aria-sort=\"ascending\"" "the caller selects the current sort column"

              Expect.stringContains
                  sortedHtml
                  "href=\"/records?sort=name&amp;direction=desc\""
                  "the consumer-provided next destination is preserved"

              Expect.stringContains sortedHtml "mobile-layout=\"records\"" "record presentation is explicitly requested"
              Expect.stringContains sortedHtml "data-mobile-cell=\"primary\"" "the caller selects its primary cell"
              let empty = Components.emptyTeamTable |> Render.toString
              Expect.stringContains empty "No team members" "empty content remains caller-owned"

              let sectionHtml =
                  Section.withoutHeader "Details" (p { "Content" })
                  |> Section.render
                  |> Render.toString

              Expect.stringContains sectionHtml "aria-label=\"Details\"" "unheaded sections retain accessible identity"
              Expect.isFalse (sectionHtml.Contains("<header")) "section header is optional"
              let headerHtml = fveSectionHeader { _label "Details" } |> Render.toString
              Expect.isFalse (headerHtml.Contains("border-b")) "section dividers are opt-in"

              let transactionsSection =
                  Section.create (fveSectionHeader { _label "Transactions" }) (p { "Rows" })

              let labelledSection =
                  transactionsSection
                  |> Section.withLabel "Recent transactions"
                  |> Section.render
                  |> Render.toString

              Expect.stringContains
                  labelledSection
                  "aria-label=\"Recent transactions\""
                  "sections can distinguish nested landmarks"

              Expect.stringContains
                  labelledSection
                  ">Transactions</h2>"
                  "accessible labels do not replace visible section headings"

              Expect.throws
                  (fun () -> transactionsSection |> Section.withLabel " " |> ignore)
                  "section labels cannot be blank"

              let canvas =
                  Page.create (fvePageHeader { _label "Graph" }) (div { "Canvas" })
                  |> Page.withBodyLayout PageBodyLayout.Canvas
                  |> Page.render
                  |> Render.toString

              Expect.stringContains canvas "data-fve-page-canvas=\"true\"" "canvas fills available height"

              Expect.isFalse
                  (canvas.Contains("data-fve-page-scroll"))
                  "canvas owns scrolling instead of nesting document scroll"

              let checkbox =
                  fveCheckbox {
                      _name "mixed"
                      _ariaLabel "Select all"
                      _indeterminate true
                  }
                  |> Render.toString

              Expect.stringContains checkbox "el.indeterminate = true" "mixed state initializes the native property"
              Expect.stringContains checkbox "aria-label=\"Select all\"" "controls accept an explicit accessible name"
          }

          test "Components data display preserves native semantics and consumer ownership" {
              let panelTableHtml =
                  fveTable {
                      _ariaLabel "Panel balances"
                      _surface Surface.Panel
                  }
                  |> Render.toString

              Expect.stringContains
                  panelTableHtml
                  "surface=\"panel\""
                  "panel presentation is an explicit host attribute"

              let tableHtml =
                  fveTable {
                      _ariaLabel "Account balances"
                      _density Density.Compact
                      _surface Surface.Plain

                      fveTableCaption {
                          _class "px-3 py-2 text-left text-sm font-semibold"
                          "Account balances"
                      }

                      fveTableHeader {
                          fveTableRow {
                              fveTableHead { "Account" }
                              fveTableHead { "Balance" }

                              fveTableHead {
                                  span {
                                      _class "sr-only"
                                      "Actions"
                                  }
                              }
                          }
                      }

                      fveTableBody {
                          fveTableRow {
                              fveTableHead {
                                  _scope "row"
                                  "Operating"
                              }

                              fveTableCell {
                                  _class "text-right"
                                  "100"
                              }

                              fveTableCell {
                                  _mobile Mobile.Actions
                                  _class "fve-table-actions text-right"
                                  _attr ("data-fve-sticky-cell", "true")

                                  fveDropdownMenu {
                                      _id "operating-actions"
                                      _ariaLabel "More actions for Operating"

                                      fveDropdownMenuTrigger {
                                          fveButton {
                                              _class "aspect-square px-0"
                                              _variant ButtonVariant.Ghost
                                              span { "More" }
                                          }
                                      }

                                      fveDropdownMenuItem {
                                          _href (shellTestUrl Accounts)
                                          "View account"
                                      }
                                  }
                              }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains
                  tableHtml
                  "<template data-fve-table-part=\"caption\""
                  "the component receives a consumer-authored caption"

              Expect.stringContains tableHtml "scope=\"row\"" "row headers stay native and caller-selected"

              Expect.stringContains
                  tableHtml
                  "<span class=\"sr-only\">Actions</span>"
                  "row actions retain their native hidden heading"

              Expect.stringContains
                  tableHtml
                  "data-fve-sticky-cell=\"true\""
                  "row actions identify their presentation boundary"

              Expect.stringContains
                  tableHtml
                  "aria-label=\"More actions for Operating\""
                  "row actions remain labelled for their record"

              Expect.stringContains
                  tableHtml
                  "surface=\"plain\""
                  "plain presentation is explicit without server-generated layout chrome"

              Expect.throws (fun () -> fveTable { _ariaLabel " " } |> ignore) "Table requires an accessible name"

              let recordMenuHtml =
                  fveDropdownMenu {
                      yield _id ("record-actions")
                      yield _ariaLabel ("More actions for Operating")

                      yield
                          ((fveDropdownMenuTrigger {
                              fveButton {
                                  yield _class "aspect-square px-0"
                                  yield _variant ButtonVariant.Ghost
                                  yield ((span { "More" }))
                              }
                          }))

                      yield!
                          ([ fveDropdownMenuItem {
                                 yield _href (shellTestUrl (Accounts))
                                 yield ("View account": string)
                             }
                             fveDropdownMenuSeparator { () }
                             fveDropdownMenuItem {
                                 yield _href (shellTestUrl (Settings))
                                 yield ("Delete account": string)
                                 yield _color (Color.Error)
                             } ])
                  }
                  |> Render.toString

              let viewIndex = recordMenuHtml.IndexOf("View account", StringComparison.Ordinal)

              let separatorIndex =
                  recordMenuHtml.IndexOf("role=\"separator\"", StringComparison.Ordinal)

              let deleteIndex = recordMenuHtml.IndexOf("Delete account", StringComparison.Ordinal)

              Expect.isTrue
                  (viewIndex < separatorIndex && separatorIndex < deleteIndex)
                  "typed menu content preserves explicit destructive grouping"

              Expect.stringContains
                  recordMenuHtml
                  "var(--fve-error-solid,"
                  "Error record-menu links retain their component-owned color"

              let detailsHtml =
                  fveDescriptionList {
                      yield!
                          ([ fveDescriptionListItem {
                                 yield _label ("Type")
                                 yield ("Asset")
                             }
                             fveDescriptionListItem {
                                 yield _label ("State")

                                 yield
                                     ((fveBadge {
                                         _color Color.Success
                                         "Active"
                                     }))

                                 yield fveDescriptionListDescription { yield ("Available for posting.") }
                                 yield! ([ _role "button"; _class "override" ])
                             } ])

                      yield _columns (DescriptionListColumns.Three)
                      yield! ([ _role "table"; _class "override" ])
                  }
                  |> Render.toString

              Expect.stringContains
                  detailsHtml
                  "<dl class=\"grid gap-x-6 gap-y-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
                  "DescriptionList retains responsive native list semantics"

              Expect.stringContains detailsHtml "<dt" "DescriptionListItem renders a term"
              Expect.stringContains detailsHtml "<dd" "DescriptionListItem renders its value and description"

              Expect.stringContains
                  detailsHtml
                  "Available for posting."
                  "DescriptionListItem preserves supporting context"

              Expect.stringContains detailsHtml "override" "description-list styling composes with consumer classes"
              Expect.isFalse (detailsHtml.Contains("role=")) "description-list structure rejects role replacement"

              Expect.throws
                  (fun () ->
                      fveDescriptionListItem {
                          _label " "
                          "Value"
                      }
                      |> ignore)
                  "DescriptionListItem requires a label"

              Expect.stringContains
                  detailsHtml
                  "text-xs font-medium uppercase tracking-wide"
                  "detail labels use the ancillary uppercase role"

              Expect.stringContains
                  detailsHtml
                  "mt-1 break-words text-sm font-normal"
                  "detail values use normal-weight UI text"

              for columns, expected in
                  [ DescriptionListColumns.One, "grid-cols-1"
                    DescriptionListColumns.Two, "grid-cols-1 sm:grid-cols-2"
                    DescriptionListColumns.Three, "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
                    DescriptionListColumns.Four, "grid-cols-1 sm:grid-cols-2 xl:grid-cols-4" ] do
                  let rendered =
                      fveDescriptionList {
                          yield!
                              ([ fveDescriptionListItem {
                                     yield _label ("<Label>")
                                     yield ("<Value>")
                                 } ])

                          yield _columns (columns)
                      }
                      |> Render.toString

                  Expect.stringContains
                      rendered
                      $"grid gap-x-6 gap-y-4 {expected}\""
                      "column choices remain explicit and responsive"

                  Expect.stringContains rendered "&lt;Label&gt;" "labels remain encoded"
                  Expect.stringContains rendered "&lt;Value&gt;" "values remain encoded"
                  Expect.isFalse (rendered.Contains("<h")) "section headings belong to the surrounding composition"

              let metricHtml =
                  fveMetric {
                      _label "Available balance"
                      _trend "Up 8%"
                      _description "Operating and reserve accounts"
                      _class "override"
                      strong { "$42,800" }
                      fveMetricStatus { fveBadge { "Current" } }
                  }
                  |> Render.toString

              Expect.stringContains metricHtml "Available balance" "Metric exposes its label"
              Expect.stringContains metricHtml "<strong>$42,800</strong>" "Metric preserves custom value content"

              Expect.stringContains
                  metricHtml
                  "<span class=\"sr-only\">Trend: </span>Up 8%"
                  "Metric gives trend text semantic context"

              Expect.stringContains metricHtml "Current" "Metric composes consumer-owned status content"

              Expect.stringContains
                  metricHtml
                  "flex flex-wrap items-center gap-2"
                  "Metric keeps status content adjacent to its label"

              Expect.isFalse
                  (metricHtml.Contains("justify-between"))
                  "Metric does not distribute status toward an adjacent metric"

              Expect.stringContains metricHtml "override" "Metric composes consumer classes"
              Expect.stringContains metricHtml "min-w-0" "Metric retains its structural base"

              Expect.throws
                  (fun () ->
                      fveMetric {
                          _label " "
                          "1"
                      }
                      |> ignore)
                  "Metric requires a label"

              let resolve destination = $"/accounts?page={destination}"

              let paginationHtml =
                  fvePagination {
                      _ariaLabel "Account pages"
                      _role "menu"
                      _class "override"

                      fvePaginationContent {
                          fvePaginationItem {
                              _page 1
                              _href (resolve 1)
                          }

                          fvePaginationItem {
                              _page 2
                              _ariaCurrent "page"
                          }

                          fvePaginationEllipsis { () }

                          fvePaginationItem {
                              _page 8
                              _href (resolve 8)
                          }
                      }

                      fvePaginationNext { _href (resolve 3) }
                      fvePaginationSummary { span { "Showing 26–50" } }
                  }
                  |> Render.toString

              Expect.stringContains
                  paginationHtml
                  "aria-label=\"Account pages\""
                  "Pagination requires a labelled navigation landmark"

              Expect.stringContains
                  paginationHtml
                  "aria-current=\"page\" aria-label=\"Page 2, current page\""
                  "Pagination exposes one current page"

              Expect.stringContains
                  paginationHtml
                  "aria-disabled=\"true\""
                  "Pagination presents an unavailable previous edge"

              Expect.stringContains
                  paginationHtml
                  "href=\"/accounts?page=3\""
                  "Pagination resolves consumer-owned destinations"

              Expect.stringContains paginationHtml "Showing 26–50" "Pagination preserves consumer summary content"
              Expect.isFalse (paginationHtml.Contains("Override")) "Pagination protects its accessible label"
              Expect.stringContains paginationHtml "override" "Pagination composes consumer presentation"
              Expect.isFalse (paginationHtml.Contains("role=\"menu\"")) "Pagination protects its navigation role"

              Expect.throws
                  (fun () ->
                      fvePagination {
                          _ariaLabel "Pages"

                          fvePaginationContent {
                              fvePaginationItem {
                                  _page 1
                                  _href "/accounts?page=1"
                              }
                          }
                      }
                      |> ignore)
                  "Pagination requires one current page"

              Expect.throws
                  (fun () ->
                      fvePagination {
                          _ariaLabel "Pages"

                          fvePaginationContent {
                              fvePaginationItem {
                                  _page 1
                                  _ariaCurrent "page"
                              }

                              fvePaginationItem {
                                  _page 2
                                  _ariaCurrent "page"
                              }
                          }
                      }
                      |> ignore)
                  "Pagination rejects multiple current pages"

              let paginationPage3Html =
                  Components.paginationPageFor 3
                  |> View.documentWithPage Registry.navigation Components.paginationRegistration
                  |> Render.toHtmlDocString

              Expect.stringContains
                  paginationPage3Html
                  "Showing 51–75 of 184 items"
                  "Docs pagination derives its summary from local query state"

              Expect.stringContains
                  paginationPage3Html
                  "aria-current=\"page\" aria-label=\"Page 3, current page\""
                  "Docs pagination renders the requested current page"

              Expect.stringContains
                  paginationPage3Html
                  "href=\"/components/pagination/page?page=4\""
                  "Docs pagination links to a targeted local next page"

              let paginationPage8Html =
                  Components.paginationPageFor 99
                  |> View.documentWithPage Registry.navigation Components.paginationRegistration
                  |> Render.toHtmlDocString

              Expect.stringContains
                  paginationPage8Html
                  "Showing 176–184 of 184 items"
                  "Docs pagination clamps out-of-range requests"

              Expect.stringContains
                  paginationPage8Html
                  "aria-current=\"page\" aria-label=\"Page 8, current page\""
                  "Docs pagination clamps to the last page"

              for removedType in
                  [ "FSharp.ViewEngine.Components.ChartConfig"
                    "FSharp.ViewEngine.Components.ButtonConfig"
                    "FSharp.ViewEngine.Components.TableConfig`1"
                    "FSharp.ViewEngine.Components.TableColumn`1"
                    "FSharp.ViewEngine.Components.TableSelectionConfig`1"
                    "FSharp.ViewEngine.Components.TableHierarchyConfig`1"
                    "FSharp.ViewEngine.Components.RowActions"
                    "FSharp.ViewEngine.Components.RowActionsConfig" ] do
                  Expect.isNull
                      (typeof<Surface>.Assembly.GetType removedType)
                      "superseded configuration wrappers stay removed"

          }

          test "DropdownMenu supports icon triggers and reactive menu choices" {
              let items: HtmlElement list =
                  [ fveDropdownMenuItem {
                        yield _dataOn ("click", "$theme = 'system'")
                        yield _role "menuitemradio"
                        yield ("System": string)
                        yield _ariaChecked ((true))
                        yield _dataAttr ("aria-checked", "(" + ("$theme == 'system'") + ") ? 'true' : 'false'")
                    }
                    fveDropdownMenuItem {
                        yield _dataOn ("click", "$theme = 'dark'")
                        yield _role "menuitemradio"
                        yield ("Dark": string)
                        yield _disabled true
                    }
                    fveDropdownMenuItem {
                        yield _dataOn ("click", "$theme = 'light'")
                        yield _role "menuitemradio"
                        yield ("Light": string)
                        yield _readonly true
                        yield _ariaBusy true
                    }
                    fveDropdownMenuItem {
                        yield _dataOn ("click", "$totals = !$totals")
                        yield _role "menuitemcheckbox"
                        yield ("Totals": string)
                        yield _ariaChecked ((true))
                        yield _dataAttr ("aria-checked", "(" + ("$totals") + ") ? 'true' : 'false'")
                    } ]

              let rendered =
                  fveDropdownMenu {
                      yield _id ("theme")
                      yield _ariaLabel ("Choose theme")

                      yield
                          ((fveDropdownMenuTrigger {
                              yield _role "link"

                              yield
                                  (fveButton {
                                      yield _class "aspect-square px-0"
                                      yield _variant ButtonVariant.Ghost
                                      yield ((span { "Icon" }))

                                      yield!
                                          ([ _attr ("data-testid", "theme-trigger")
                                             _ariaLabel "Trigger-specific theme" ])
                                  })
                          }))

                      yield! (items)
                  }
                  |> Render.toString

              Expect.stringContains
                  rendered
                  "aria-label=\"Trigger-specific theme\""
                  "The explicit native trigger name overrides the root default"

              Expect.stringContains
                  rendered
                  "data-testid=\"theme-trigger\""
                  "A trigger preserves safe consumer attributes"

              Expect.isFalse (rendered.Contains "role=\"link\"") "A trigger protects renderer-owned button semantics"

              Expect.stringContains
                  rendered
                  "role=\"menuitemradio\" aria-checked=\"true\""
                  "Radio choices expose initial selection"

              Expect.stringContains rendered "data-attr:aria-checked" "Checked state follows its trusted expression"

              Expect.stringContains
                  rendered
                  "group-aria-checked:visible"
                  "The decorative checkmark follows native aria-checked state without a second binding"

              Expect.stringContains
                  rendered
                  ":is([role=menuitem], [role=menuitemradio], [role=menuitemcheckbox]):not([aria-disabled=true])"
                  "Keyboard navigation includes every enabled menu choice"

              Expect.stringContains
                  rendered
                  "role=\"menuitemcheckbox\" aria-checked=\"true\""
                  "Checkbox choices expose independent checked state"

              Expect.stringContains
                  rendered
                  "data-on:click=\"$totals = !$totals\""
                  "Checkbox choices remain open while the caller applies a toggle"

              Expect.stringContains rendered "aria-busy=\"true\"" "Radio choices support pending state"
              Expect.isFalse (rendered.Contains "aria-selected=") "Menu choices do not use Select styling hooks"

              Expect.throws
                  (fun () ->
                      fveDropdownMenuItem {
                          yield _dataOn ("click", "noop()")
                          yield ("Action": string)
                          yield _ariaChecked ((true))
                      }
                      |> ignore)
                  "Only choice items accept checked state"
          }

          test "Components DropdownMenu renders complete item vocabulary and interaction semantics" {
              let leading =
                  span {
                      _attr ("data-test-icon", "review")
                      "✓"
                  }

              let menuItems =
                  [ fveDropdownMenuGroup {
                        yield _label ("Account")

                        yield!
                            ([ fveDropdownMenuItem {
                                   _href (string 1)
                                   "Account settings"
                               }
                               fveDropdownMenuItem {
                                   yield _dataOn ("click", "$reviews++")
                                   yield ("Record review": string)
                                   yield fveDropdownMenuItemLeading { yield (leading) }
                                   yield fveDropdownMenuItemShortcut { yield ("R") }
                               }
                               fveDropdownMenuItem {
                                   _href (string 2)
                                   _disabled true
                                   "Unavailable link"
                               }
                               fveDropdownMenuItem {
                                   yield _dataOn ("click", "$sync++")
                                   yield ("Syncing account": string)
                                   yield _readonly true
                                   yield _ariaBusy true
                               } ])
                    }
                    fveDropdownMenuSeparator { () }
                    fveDropdownMenuItem {
                        yield _dataOn ("click", "$delete++")
                        yield ("Delete draft": string)
                        yield _color (Color.Error)
                    } ]

              let html =
                  fveDropdownMenu {
                      yield _id ("account-actions")
                      yield _ariaLabel ("Actions")
                      yield ((fveDropdownMenuTrigger { fveButton { yield ("Actions": string) } }))
                      yield! (menuItems)
                  }
                  |> Render.toString

              Expect.stringContains html "aria-haspopup=\"menu\"" "DropdownMenu trigger identifies its popup"

              Expect.stringContains
                  html
                  "aria-controls=\"account-actions-menu\""
                  "DropdownMenu trigger controls its menu"

              Expect.stringContains
                  html
                  "role=\"menu\" tabindex=\"-1\" aria-labelledby=\"account-actions-trigger\""
                  "DropdownMenu container receives focus when no item is active"

              Expect.stringContains html "data-on:pointerleave" "DropdownMenu clears item focus on pointer leave"
              Expect.stringContains html "evt.detail == 0" "DropdownMenu distinguishes keyboard from pointer opening"

              Expect.stringContains
                  html
                  "role=\"group\" aria-labelledby=\"account-actions-menu-entry-0-label\""
                  "DropdownMenu labels groups"

              Expect.stringContains html ">Account</div>" "DropdownMenu keeps the group label visible"

              Expect.stringContains
                  html
                  "data-test-icon=\"review\""
                  "DropdownMenu preserves consumer-owned leading content"

              Expect.stringContains
                  html
                  "<kbd aria-hidden=\"true\""
                  "DropdownMenu presents shortcut hints without changing item names"

              Expect.stringContains html ">R</kbd>" "DropdownMenu preserves shortcut text"
              Expect.stringContains html "role=\"separator\"" "DropdownMenu preserves separators"
              Expect.stringContains html "popover=\"auto\"" "DropdownMenu uses a native auto popover"

              Expect.stringContains
                  html
                  "width: min(16rem, calc(100vw - 2rem))"
                  "DropdownMenu bounds the top-layer popup width"

              Expect.stringContains
                  html
                  "position-area: block-end span-inline-start"
                  "DropdownMenu preserves end alignment by default"

              Expect.stringContains
                  html
                  "position-try-fallbacks: flip-block, flip-inline, flip-block flip-inline"
                  "DropdownMenu provides viewport-edge fallbacks"

              Expect.stringContains
                  html
                  "closest(&#39;[data-fve-sticky-cell=true]&#39;)"
                  "DropdownMenu detects standardized sticky table cells"

              Expect.stringContains
                  html
                  "data-on:scroll__window__capture=\"el._fvePosition?.()\""
                  "DropdownMenu tracks nested scrolling with lifecycle-owned bindings"

              Expect.isFalse
                  (html.Contains "window.addEventListener")
                  "Removed menu elements cannot retain manual position listeners"

              Expect.stringContains html "var(--fve-error-solid," "DropdownMenu preserves the selected Error color"

              Expect.stringContains
                  html
                  "fve-popup-item"
                  "DropdownMenu uses the shared background-based popup focus treatment"

              Expect.stringContains
                  html
                  "data-fve-control-id=\"account-actions-trigger\""
                  "DropdownMenu relationships name the Button-owned native control"

              Expect.isFalse
                  (html.Contains("focus-visible:ring-2") || html.Contains("focus:ring-2"))
                  "DropdownMenu focus does not use a ring"

              Expect.isFalse
                  (html.Contains("fve-popup-destructive")
                   || html.Contains("focus:bg-[var(--fve-critical-subtle)]"))
                  "Destructive commands use the same neutral focus treatment"

              Expect.stringContains
                  html
                  "data-fve-menu-label=\"record review\""
                  "DropdownMenu exposes normalized labels for character navigation"

              Expect.stringContains
                  html
                  "_account_actions_typeahead"
                  "DropdownMenu isolates bounded character-navigation state"

              Expect.stringContains
                  html
                  "popovertarget=\"account-actions-menu\""
                  "DropdownMenu associates its trigger with the native popover"

              Expect.stringContains
                  html
                  "data-on:beforetoggle"
                  "DropdownMenu synchronizes native visibility with its local signal"

              let triggerClick =
                  Regex.Match(html, "<fve-button[^>]* data-on:click=\"([^\"]*)\"").Groups[1].Value

              Expect.stringContains
                  triggerClick
                  "evt.preventDefault()"
                  "the trigger cancels the native toggle before its menu focus behavior"

              Expect.stringContains html "evt.key == &#39;ArrowDown&#39;" "DropdownMenu trigger opens with ArrowDown"
              Expect.stringContains html "Date.now()" "DropdownMenu bounds its character-navigation buffer"

              Expect.stringContains
                  html
                  "every(character =&gt; character == $_account_actions_typeahead[0])"
                  "DropdownMenu cycles repeated characters"

              Expect.stringContains html ":not([aria-disabled=true])" "DropdownMenu movement skips unavailable items"

              Expect.stringContains
                  html
                  "document.activeElement.click()"
                  "DropdownMenu activates focused items with Enter or Space"

              Expect.stringContains
                  html
                  "data-on:pointermove__window=\"el.dataset.fvePointerX = evt.clientX; el.dataset.fvePointerY = evt.clientY\""
                  "DropdownMenu remembers the last intentional pointer coordinates"

              Expect.stringContains
                  html
                  "data-preserve-attr=\"data-fve-pointer-x data-fve-pointer-y\""
                  "DropdownMenu retains pointer coordinates through a server morph"

              Expect.stringContains
                  html
                  "evt.clientX != Number(document.getElementById(&quot;account-actions-menu&quot;).dataset.fvePointerX)"
                  "DropdownMenu ignores stationary pointer events after keyboard focus movement"

              Expect.stringContains
                  html
                  "document.getElementById(&quot;account-actions-trigger&quot;)?.focus()"
                  "DropdownMenu restores its trigger when appropriate"

              let unavailableLink =
                  Regex
                      .Match(html, "<a[^>]*aria-disabled=\"true\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              Expect.isNotEmpty unavailableLink "DropdownMenu renders a disabled link item"
              Expect.isFalse (unavailableLink.Contains("href=")) "Disabled menu links cannot navigate"

              Expect.stringContains
                  unavailableLink
                  "cursor-not-allowed opacity-50"
                  "Disabled menu links remain visibly unavailable"

              let pendingButton =
                  Regex
                      .Match(
                          html,
                          "<button[^>]*aria-disabled=\"true\"[^>]*aria-busy=\"true\"[^>]*>",
                          RegexOptions.IgnoreCase
                      )
                      .Value

              Expect.isNotEmpty pendingButton "DropdownMenu exposes a read-only busy action"

              Expect.isFalse
                  (Regex.IsMatch(pendingButton, "\\sdisabled(?:\\s|=|>)"))
                  "Read-only is distinct from native disabled"

              Expect.stringContains html "animate-spin" "Pending menu actions show a loading indicator"

              Expect.stringContains
                  html
                  "motion-reduce:animate-none"
                  "Pending menu motion respects reduced-motion preferences"

              let startAlignedHtml =
                  fveDropdownMenu {
                      yield _id ("start-actions")
                      yield _ariaLabel ("Start actions")
                      yield ((fveDropdownMenuTrigger { fveButton { yield ("Start actions": string) } }))

                      yield!
                          ([ fveDropdownMenuItem {
                                 yield _href (string (1))
                                 yield ("First": string)
                             } ])

                      yield _alignment (DropdownMenuAlignment.Start)
                  }
                  |> Render.toString

              Expect.stringContains
                  startAlignedHtml
                  "position-area: block-end span-inline-end"
                  "DropdownMenu supports typed start alignment"

              Expect.isFalse
                  (startAlignedHtml.Contains("position-area: block-end span-inline-start"))
                  "Start alignment replaces default end alignment"

              let adjacentHtml =
                  div {
                      fveDropdownMenu {
                          yield _id ("first-actions")
                          yield _ariaLabel ("First actions")
                          yield ((fveDropdownMenuTrigger { fveButton { yield ("First actions": string) } }))

                          yield!
                              ([ fveDropdownMenuItem {
                                     yield _href (string (1))
                                     yield ("First": string)
                                 } ])
                      }

                      fveDropdownMenu {
                          yield _id ("second-actions")
                          yield _ariaLabel ("Second actions")
                          yield ((fveDropdownMenuTrigger { fveButton { yield ("Second actions": string) } }))

                          yield!
                              ([ fveDropdownMenuItem {
                                     yield _href (string (2))
                                     yield ("Second": string)
                                 } ])
                      }
                  }
                  |> Render.toString

              Expect.stringContains adjacentHtml "_first_actions_open" "First menu owns a stable signal"
              Expect.stringContains adjacentHtml "_second_actions_open" "Second menu owns an isolated stable signal"
              Expect.equal (Regex.Matches(adjacentHtml, "id=\"first-actions-menu\"").Count) 1 "First menu ID is unique"

              Expect.equal
                  (Regex.Matches(adjacentHtml, "id=\"second-actions-menu\"").Count)
                  1
                  "Second menu ID is unique"

              Expect.throws
                  (fun () ->
                      fveDropdownMenuItem {
                          _href (string 1)
                          " "
                      }
                      |> ignore)
                  "Menu links require accessible labels"

              Expect.throws
                  (fun () ->
                      fveDropdownMenuGroup {
                          _label " "

                          fveDropdownMenuItem {
                              _href (string 1)
                              "Item"
                          }
                      }
                      |> ignore)
                  "Menu groups require labels"

              Expect.throws (fun () -> fveDropdownMenuGroup { _label "Empty" } |> ignore) "Menu groups require items"

              Expect.throws
                  (fun () ->
                      fveDropdownMenuGroup {
                          _label "Outer"

                          fveDropdownMenuGroup {
                              _label "Inner"

                              fveDropdownMenuItem {
                                  _href (string 1)
                                  "Item"
                              }
                          }
                      }
                      |> ignore)
                  "Menu groups cannot nest"

              Expect.throws (fun () -> fveDropdownMenuSeparator { "Item" } |> ignore) "Separators reject item content"

              Expect.throws
                  (fun () ->
                      fveDropdownMenu {
                          yield _id ("missing-trigger")
                          yield _ariaLabel ("Missing trigger")

                          yield!
                              ([ fveDropdownMenuItem {
                                     yield _href (string (1))
                                     yield ("Item": string)
                                 } ])
                      }
                      |> ignore)
                  "DropdownMenu requires an explicit trigger"

              Expect.throws
                  (fun () ->
                      fveDropdownMenu {
                          yield _id ("missing-content")
                          yield _ariaLabel ("Missing content")

                          yield ((fveDropdownMenuTrigger { fveButton { "Open" } }))
                      }
                      |> ignore)
                  "DropdownMenu requires explicit content"

              Expect.throws
                  (fun () ->
                      fveDropdownMenu {
                          _id "empty-content"
                          fveDropdownMenuTrigger { fveButton { "Empty content" } }
                      }
                      |> ignore)
                  "DropdownMenu content cannot be empty"

              let docsMenuHtml = Components.dropdownMenuRegion false |> Render.toString

              Expect.stringContains
                  docsMenuHtml
                  "href=\"/components/dropdown-menu#components-dropdown-menu\""
                  "Docs menu uses a real local typed destination"

              let patchedHtml = Components.patchedDropdownMenuRegion |> Render.toString

              Expect.stringContains
                  patchedHtml
                  "id=\"components-dropdown-menu-region\""
                  "Docs patch preserves the stable menu region"

              Expect.stringContains
                  patchedHtml
                  "Review refreshed actions"
                  "Docs patch changes server-rendered menu content"

              Expect.stringContains patchedHtml "role=\"status\"" "Docs patch reports its completed server update"
              Expect.isFalse (patchedHtml.Contains("Refresh actions")) "Docs patch replaces the initiating command"
          }

          test "Components Select owns the complete branded select-only form contract" {
              let html =
                  fveField {
                      fveLabel {
                          _for ("account-status")
                          "Status"
                      }

                      fveSelect {
                          _id ("account-status")
                          _name ("status")
                          _required true
                          _readonly true
                          _ariaBusy true
                          _ariaInvalid (Option.isSome (Some("Choose an available status.")))

                          _ariaDescribedby (
                              String.concat
                                  " "
                                  [ ("account-status") + "-description"
                                    if Option.isSome (Some("Choose an available status.")) then
                                        ("account-status") + "-validation" ]
                          )

                          fveSelectContent {
                              for value, label, disabled in
                                  ([ ("active", "Active", false); ("disabled", "Disabled", true) ]) do
                                  fveSelectItem {
                                      _value (value)
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }

                      fveFieldDescription {
                          _id (("account-status") + "-description")
                          "Account status"
                      }

                      match Some("Choose an available status.") with
                      | Some message ->
                          fveFieldError {
                              _id (("account-status") + "-validation")
                              message
                          }
                      | None -> ()
                  }
                  |> Render.toString

              Expect.isFalse (html.Contains("<select")) "Select never renders a native select element"

              let trigger =
                  Regex
                      .Match(html, "<button[^>]*role=\"combobox\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              let submittedValue =
                  Regex
                      .Match(html, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              Expect.stringContains trigger "disabled" "pending Select prevents interaction"
              Expect.stringContains trigger "aria-required=\"true\"" "Select exposes required state on the combobox"
              Expect.stringContains trigger "aria-disabled=\"true\"" "Select exposes unavailable state"
              Expect.stringContains trigger "aria-invalid=\"true\"" "Select exposes server validation"
              Expect.stringContains trigger "aria-busy=\"true\"" "pending Select exposes busy state"

              Expect.stringContains
                  trigger
                  "aria-describedby=\"account-status-description account-status-validation\""
                  "Select joins help and validation relationships"

              Expect.stringContains submittedValue "name=\"status\"" "Select retains the consumer form name"

              Expect.isFalse
                  (Regex.IsMatch(submittedValue, "\\sdisabled(?:=|\\s|>)"))
                  "read-only Select preserves its successful value"

              Expect.stringContains html "popover=\"auto\"" "Select renders its listbox in the native top layer"

              Expect.stringContains
                  html
                  "data-fve-select-trigger=\"account-status\""
                  "independent content resolves its owning native trigger"

              Expect.stringContains
                  html
                  "position-try-fallbacks:flip-block, flip-inline, flip-block flip-inline"
                  "Select provides viewport-edge fallbacks"

              Expect.stringContains
                  html
                  "position-area:block-end span-inline-end"
                  "an unselected Select falls back to its trigger start edge"

              let alignedHtml =
                  fveField {
                      fveLabel {
                          _for ("frequency")
                          "Frequency"
                      }

                      fveSelect {
                          _id ("frequency")
                          _name ("frequency")
                          _value ("weekly")

                          fveSelectContent {
                              for value, label, disabled in
                                  ([ ("daily", "Daily", false)
                                     ("weekly", "Weekly", false)
                                     ("monthly", "Monthly", false) ]) do
                                  fveSelectItem {
                                      _value (value)
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains
                  alignedHtml
                  "data-attr:aria-selected"
                  "items expose the owning selection to positioning and assistive technology"

              Expect.isFalse (alignedHtml.Contains("rotate-180")) "opening does not rotate the plain Select chevron"

              let endAlignedHtml =
                  fveField {
                      fveLabel {
                          _for ("format")
                          "Format"
                      }

                      fveSelect {
                          _id ("format")
                          _name ("format")
                          _position (SelectPosition.TriggerEnd)

                          fveSelectContent {
                              for value, label, disabled in ([ ("csv", "CSV", false) ]) do
                                  fveSelectItem {
                                      _value (value)
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains
                  endAlignedHtml
                  "block-end span-inline-start"
                  "Select supports typed trigger-end positioning"

              Expect.stringContains html "role=\"listbox\"" "Select renders its branded listbox"
              Expect.stringContains html "role=\"alert\"" "Select validation is announced after a patch"
              Expect.stringContains html "role=\"option\"" "Select renders branded options"
              Expect.stringContains html "aria-disabled=\"true\"" "Select preserves disabled options"
              Expect.stringContains html "_fveSelect.account_status.typeahead" "Select isolates bounded typeahead state"
              Expect.stringContains html "Date.now()" "Select resets typeahead after its bounded interval"

              Expect.stringContains
                  html
                  "every(character =&gt; character == $_fveSelect.account_status.typeahead[0])"
                  "Select cycles repeated-character matches"

              Expect.stringContains
                  html
                  "evt.altKey &amp;&amp; evt.key == &#39;ArrowDown&#39;"
                  "Select implements closed Alt+Down"

              Expect.stringContains html "evt.key == &#39;PageUp&#39;" "Select implements PageUp"
              Expect.stringContains html "evt.key == &#39;PageDown&#39;" "Select implements PageDown"
              Expect.stringContains html "Math.min" "Select clamps forward movement"
              Expect.stringContains html "Math.max" "Select clamps backward movement"
              Expect.stringContains html "evt.key == &#39;Tab&#39;" "Select commits the active option on Tab"

              Expect.stringContains
                  html
                  "document.getElementById($_fveSelect.account_status.active)?.click()"
                  "Select commits active identity through one option path"
          }

          test "Select examples each teach one control with complete remote source" {
              let examples = Components.examplesFor "select"

              Expect.sequenceEqual
                  (examples |> List.map _.title)
                  [ "Basic"
                    "Custom items and groups"
                    "Multiple selection"
                    "Local search"
                    "Remote search"
                    "Form validation"
                    "Disabled"
                    "Pending" ]
                  "public examples teach capabilities rather than a state matrix"

              let basic = examples |> List.head
              Expect.stringContains basic.source "fveSelectContent {" "ordinary composition is introduced first"

              Expect.stringContains
                  basic.source
                  "fveSelectItem {
                "
                  "items are explicit, not auto-generated"

              for example in examples do
                  Expect.equal
                      (Regex.Matches(Render.toString example.preview, "data-fve-select=\"").Count)
                      1
                      $"{example.title} shows one Select"

                  Expect.equal
                      (Regex.Matches(example.source, @"\bfve(?:Multi)?Select\s*\{").Count)
                      1
                      $"{example.title} copies only one Select construction"

                  Expect.isFalse
                      (example.source.Contains "encodeValue")
                      $"{example.title} passes string values directly without encoding setup"

                  Expect.isFalse
                      (example.source.Contains "ChoiceStatus")
                      $"{example.title} does not require a domain type to demonstrate the control"

                  for obsolete in [ "parts (fun"; "selectContext"; "fveSelectTrigger"; "fveSelectValue" ] do
                      Expect.isFalse
                          (example.source.Contains obsolete)
                          "copied source contains no old context or callback plumbing"

              let remote = examples |> List.find (fun example -> example.title = "Remote search")

              for runningCode in [ "let itemContent"; "let itemSearchHandler"; "let itemSearchRoutes" ] do
                  Expect.stringContains
                      remote.source
                      runningCode
                      "the one-field example includes its actual running content, handler, and route"

              for unrelated in
                  [ "remoteMembers"
                    "itemSearchTargets"
                    "itemFixtureSearchRoutes"
                    "isolatedItemComboboxes" ] do
                  Expect.isFalse
                      (remote.source.Contains unrelated)
                      "copied remote source excludes other modes and regression-fixture wiring"

              let page = Components.tryPage "/components/select" |> Option.get
              let html = fragment { page.sections |> List.collect _.content } |> Render.toString

              Expect.isFalse
                  (html.Contains "Optional root configuration")
                  "configuration is not presented as a separate setup step"
          }

          test "Remote Select routes return only known content targets and reject malformed search state" {
              let response (payload: string) =
                  routeResponse ("/components/items/search?datastar=" + Uri.EscapeDataString payload)

              for signal, target in [ "item_query", "item"; "seconditem_query", "seconditem" ] do
                  let status, body = response ($"{{\"{signal}\":\"shared\"}}")
                  Expect.equal status 200 "known fields receive HTML results"

                  Expect.stringContains
                      body
                      $"id=\"fve-select-{target}-popup\""
                      "the response patches the requesting field's content"

                  Expect.stringContains body "Shared files" "the handler searches the requested data"
                  Expect.isFalse (body.Contains("Project notes")) "nonmatching records are not returned"

                  Expect.isFalse
                      (body.Contains($"name=\"{target}\""))
                      "content responses never duplicate the successful value control"

              for invalid in [ "{}"; "[]"; "{"; "{\"untrusted_query\":\"shared\"}" ] do
                  Expect.equal (response invalid |> fst) 400 "client state cannot choose an arbitrary patch target"
          }

          test "Components searchable Select preserves query, selection, async state, and form semantics" {
              let options = [ (101, "Operating", false); (102, "Tax reserve", true) ]

              let html =
                  fveField {
                      fveLabel {
                          _for ("parent-account")
                          "Parent account"
                      }

                      fveSelect {
                          _id ("parent-account")
                          _name ("account")
                          _value ((string) (101))
                          _search ((SelectSearch.Remote "/accounts/search"))
                          _ariaInvalid (Option.isSome (Some("Choose an account.")))

                          _ariaDescribedby (
                              String.concat
                                  " "
                                  [ ("parent-account") + "-description"
                                    if Option.isSome (Some("Choose an account.")) then
                                        ("parent-account") + "-validation" ]
                          )

                          fveSelectContent {
                              for value, label, disabled in (options) do
                                  fveSelectItem {
                                      _value ((string) (value))
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }

                      fveFieldDescription {
                          _id (("parent-account") + "-description")
                          "Server-owned accounts."
                      }

                      match Some("Choose an account.") with
                      | Some message ->
                          fveFieldError {
                              _id (("parent-account") + "-validation")
                              message
                          }
                      | None -> ()
                  }
                  |> Render.toString

              let trigger =
                  Regex
                      .Match(html, "<button[^>]*aria-haspopup=\"dialog\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              let searchInput =
                  Regex
                      .Match(html, "<input[^>]*type=\"search\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              let submittedValue =
                  Regex
                      .Match(html, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              Expect.stringContains trigger "id=\"parent-account\"" "stable ID owns the Select trigger"

              Expect.stringContains
                  trigger
                  "aria-controls=\"fve-select-parent_account-popup\""
                  "trigger controls the labelled search popup"

              Expect.stringContains
                  trigger
                  "aria-describedby=\"parent-account-description parent-account-validation\""
                  "description and validation are joined"

              Expect.stringContains trigger "aria-invalid=\"true\"" "server validation is exposed"
              Expect.stringContains trigger "group flex" "searchable Select uses the same flex trigger layout as Select"
              Expect.stringContains trigger "justify-between" "searchable Select keeps its chevron on the trigger edge"
              Expect.isFalse (html.Contains("rotate-180")) "opening does not rotate the searchable Select chevron"

              Expect.stringContains
                  searchInput
                  "id=\"fve-select-parent_account-search\""
                  "popup owns a distinct search input"

              Expect.stringContains
                  searchInput
                  "data-on:input"
                  "input updates the nearest owner's query rather than the selected identity"

              Expect.stringContains submittedValue "name=\"account\"" "hidden input preserves the consumer form name"
              Expect.stringContains submittedValue "value=\"101\"" "typed selected identity is explicitly encoded"

              Expect.stringContains
                  submittedValue
                  "data-bind:parent_account_value"
                  "submitted identity remains distinct from query binding"

              Expect.stringContains
                  html
                  "popover=\"auto\""
                  "searchable Select renders its popup in the native top layer"

              Expect.stringContains
                  html
                  "data-fve-select-trigger=\"parent-account\""
                  "searchable content resolves its owning native trigger"

              Expect.stringContains
                  html
                  "position-try-fallbacks:flip-block, flip-inline, flip-block flip-inline"
                  "searchable Select provides viewport-edge fallbacks"

              Expect.stringContains html "role=\"listbox\"" "popup contains the canonical listbox"
              Expect.stringContains html "data-on:pointermove" "pointer and keyboard share one active option"
              Expect.stringContains html "data-on:pointerleave" "pointer leave clears the active option"

              Expect.stringContains
                  html
                  "active:&#39;&#39;"
                  "searchable Select opens without a persistent active option"

              Expect.stringContains html "aria-disabled=\"true\"" "disabled options remain discoverable but unavailable"
              Expect.stringContains html "role=\"alert\"" "form validation is announced"

              let staticHtml =
                  fveField {
                      fveLabel {
                          _for ("local")
                          "Local account"
                      }

                      fveSelect {
                          _id ("local")
                          _name ("local")
                          _search (SelectSearch.Static)

                          fveSelectContent {
                              _emptyMessage "No local accounts"

                              for value, label, disabled in (options) do
                                  fveSelectItem {
                                      _value ((string) (value))
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains staticHtml "_local_query" "static query remains private ephemeral state"
              Expect.stringContains staticHtml "data-show=" "static options filter locally"
              Expect.stringContains staticHtml "No local accounts" "static empty state is configurable"
              Expect.isFalse (staticHtml.Contains("@get(")) "static filtering has no backend action"

              let errorHtml =
                  fveField {
                      fveLabel {
                          _for ("failed")
                          "Failed account"
                      }

                      fveSelect {
                          _id ("failed")
                          _name ("failed")
                          _search ((SelectSearch.Remote "/accounts/search?retry=true"))

                          fveSelectContent {
                              _error "Accounts could not be loaded."
                              _retry "/accounts/search?retry=true"

                              for value, label, disabled in ([]) do
                                  fveSelectItem {
                                      _value ((string) (value))
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains errorHtml "Accounts could not be loaded." "server-rendered fetch error is visible"
              Expect.stringContains errorHtml ">Retry</button>" "remote error offers a retry action"

              let loadingHtml =
                  fveField {
                      fveLabel {
                          _for ("loading")
                          "Loading account"
                      }

                      fveSelect {
                          _id ("loading")
                          _name ("loading")
                          _search (SelectSearch.Static)

                          fveSelectContent {
                              _loading true
                              _loadingMessage "Loading accounts"

                              for value, label, disabled in ([]) do
                                  fveSelectItem {
                                      _value ((string) (value))
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains loadingHtml "data-fve-select-loading" "loading state is programmatically busy"
              Expect.stringContains loadingHtml "Loading accounts" "loading status remains perceivable"

              let pendingHtml =
                  fveField {
                      fveLabel {
                          _for ("pending")
                          "Pending account"
                      }

                      fveSelect {
                          _id ("pending")
                          _name ("pending")
                          _search (SelectSearch.Static)
                          _value ((string) (101))
                          _readonly true
                          _ariaBusy true

                          fveSelectContent {
                              for value, label, disabled in (options) do
                                  fveSelectItem {
                                      _value ((string) (value))
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString

              let pendingCombobox =
                  Regex
                      .Match(pendingHtml, "<button[^>]*aria-haspopup=\"dialog\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              let pendingValue =
                  Regex
                      .Match(pendingHtml, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              Expect.stringContains pendingCombobox "disabled" "pending control prevents interaction"
              Expect.stringContains pendingCombobox "aria-busy=\"true\"" "pending control exposes busy state"

              Expect.isFalse
                  (Regex.IsMatch(pendingValue, "\\sdisabled(?:=|\\s|>)"))
                  "read-only selected identity remains successful in FormData"

              Expect.isFalse
                  (pendingCombobox.Contains("data-on:keydown"))
                  "unavailable trigger emits no keyboard action"
          }

          test "Components branded choice controls preserve distinct complete semantics" {
              let searchableSelectHtml =
                  fveField {
                      fveLabel {
                          _for ("account")
                          "Account"
                      }

                      fveSelect {
                          _id ("account")
                          _name ("account")
                          _search (SelectSearch.Static)
                          _value ("operating")

                          fveSelectContent {
                              for value, label, disabled in ([ ("operating", "Operating", false) ]) do
                                  fveSelectItem {
                                      _value (value)
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString

              Expect.stringContains
                  searchableSelectHtml
                  "aria-haspopup=\"dialog\""
                  "searchable Select remains a Select trigger with a popup"

              Expect.stringContains
                  searchableSelectHtml
                  "type=\"search\""
                  "searchable Select keeps filtering in its popup"

              let checkboxHtml =
                  fveField {
                      _layout FieldLayout.Horizontal

                      fveCheckbox {
                          _id "review-confirmed"
                          _name "confirmed"
                          _required true
                          _ariaInvalid true
                          _ariaDescribedby "review-description review-validation"
                      }

                      fveFieldContent {
                          fveLabel {
                              _for "review-confirmed"
                              "Confirmed"
                          }

                          fveFieldDescription {
                              _id "review-description"
                              "Review completed."
                          }

                          fveFieldError {
                              _id "review-validation"
                              "Confirm the review."
                          }
                      }
                  }
                  |> Render.toString

              let checkbox =
                  Regex
                      .Match(checkboxHtml, "<input[^>]*type=\"checkbox\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              for expected in
                  [ "id=\"review-confirmed\""
                    "name=\"confirmed\""
                    "required"
                    "aria-invalid=\"true\""
                    "aria-describedby=\"review-description review-validation\"" ] do
                  Expect.stringContains checkbox expected "a native checkbox owns its form attributes"

              Expect.equal
                  (Regex.Matches(checkboxHtml, "<label ").Count)
                  1
                  "the field supplies exactly one native label"

              let pendingCheckbox =
                  fveCheckbox {
                      _name "confirmed"
                      _required true
                      _checked true
                      _readonly true
                      _ariaBusy true
                  }
                  |> Render.toString

              Expect.stringContains pendingCheckbox "readonly" "read-only checkbox blocks edits"
              Expect.stringContains pendingCheckbox "aria-busy=\"true\"" "busy state is explicit"

              Expect.isFalse
                  (Regex.IsMatch(pendingCheckbox, "\\sdisabled(?:\\s|>)"))
                  "a checked read-only checkbox remains successful"

              Expect.isFalse
                  (Regex.IsMatch(pendingCheckbox, "\\srequired(?:\\s|>)"))
                  "read-only checkbox cannot trap native validation"

              let switchHtml =
                  fveSwitch {
                      _id "account-notifications"
                      _name "notifications"
                      _checked true
                      _ariaLabel "Notifications"
                      _ariaInvalid true
                      _ariaBusy true
                      _readonly true
                  }
                  |> Render.toString

              let switchControl =
                  Regex
                      .Match(switchHtml, "<input[^>]*role=\"switch\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              Expect.stringContains switchControl "name=\"notifications\"" "Switch retains native form submission"
              Expect.stringContains switchControl "checked" "Switch uses native checked state"
              Expect.isFalse (Regex.IsMatch(switchControl, "\\sdisabled(?:\\s|>)")) "read-only values remain successful"
              Expect.stringContains switchControl "readonly" "read-only Switch prevents edits"
              Expect.stringContains switchControl "aria-busy=\"true\"" "pending Switch exposes busy state"
              Expect.stringContains switchControl "aria-invalid=\"true\"" "Switch exposes server validation"

              let toggleHtml =
                  fveToggleButton {
                      _id "compact"
                      _ariaPressed true
                      _readonly true
                      _ariaBusy true
                      "Compact rows"
                  }
                  |> Render.toString

              Expect.stringContains toggleHtml "aria-pressed=\"true\"" "ToggleButton has pressed semantics"
              Expect.stringContains toggleHtml ">Compact rows</button>" "visible content supplies the accessible name"
              Expect.stringContains toggleHtml "disabled" "pending ToggleButton prevents activation"
              Expect.stringContains toggleHtml "aria-busy=\"true\"" "pending ToggleButton exposes busy state"

              Expect.isFalse
                  (toggleHtml.Contains("data-on:click"))
                  "unavailable ToggleButton has no activation expression"

              let radioHtml =
                  fveRadioGroup {
                      _id "posting-mode"
                      _ariaRequired true
                      _ariaInvalid true
                      fveFieldLegend { "Mode" }
                      fveFieldDescription { "Posting behavior." }

                      for value in [ "automatic"; "manual"; "disabled" ] do
                          fveRadio {
                              _name "mode"
                              _value value
                              _ariaLabel value
                              _required true

                              if value = "disabled" then
                                  _disabled true
                          }

                      fveFieldError { "Choose a mode." }
                  }
                  |> Render.toString

              Expect.stringContains radioHtml "role=\"radiogroup\"" "RadioGroup has grouped choice semantics"
              Expect.stringContains radioHtml ">Mode</legend>" "RadioGroup uses its native legend as name"
              Expect.stringContains radioHtml "aria-required=\"true\"" "RadioGroup exposes required state on the group"
              Expect.stringContains radioHtml "aria-invalid=\"true\"" "RadioGroup exposes validation state"

              Expect.equal
                  (Regex.Matches(radioHtml, "type=\"radio\"").Count)
                  3
                  "RadioGroup retains one radio input per option"

              Expect.equal
                  (Regex.Matches(radioHtml, "name=\"mode\"").Count)
                  3
                  "RadioGroup options share the consumer form name"

              Expect.equal
                  (Regex.Matches(radioHtml, " required").Count)
                  2
                  "only enabled options participate in required native validation"

              Expect.stringContains radioHtml "role=\"alert\"" "RadioGroup validation is announced"
          }

          test "Checked setters replace prior server state in either direction" {
              for isChecked in [ true; false ] do
                  let checkboxHtml =
                      fveCheckbox {
                          _name "confirmed"
                          _checked isChecked
                      }
                      |> Render.toString

                  let switchHtml =
                      fveSwitch {
                          _name "notifications"
                          _checked isChecked
                      }
                      |> Render.toString

                  for html in [ checkboxHtml; switchHtml ] do
                      let input = Regex.Match(html, "<input[^>]*>").Value

                      Expect.equal
                          (Regex.IsMatch(input, "\\schecked(?:\\s|>)"))
                          isChecked
                          "native checked state uses the last Boolean value"

                  let expected = if isChecked then "true" else "false"

                  let menuHtml =
                      fveDropdownMenu {
                          yield _id ("checked-menu")
                          yield _ariaLabel ("View")
                          yield ((fveDropdownMenuTrigger { fveButton { yield ("View": string) } }))

                          yield!
                              ([ fveDropdownMenuItem {
                                     yield _dataOn ("click", "$compact = !$compact")
                                     yield _role "menuitemradio"
                                     yield ("Compact": string)
                                     yield _ariaChecked (((not isChecked)))
                                     yield _ariaChecked ((isChecked))
                                 } ])
                      }
                      |> Render.toString

                  Expect.stringContains
                      menuHtml
                      $"aria-checked=\"{expected}\""
                      "menu radio items follow the same setter contract"
          }

          test "Components finite-choice stable IDs isolate repeated form names" {
              let html =
                  form {
                      fveCheckbox {
                          _id "first-choice"
                          _name "choice"
                          _ariaLabel "First choice"
                      }

                      fveCheckbox {
                          _id "second-choice"
                          _name "choice"
                          _ariaLabel "Second choice"
                      }

                      fveSwitch {
                          _id "first-setting"
                          _name "setting"
                          _ariaLabel "First setting"
                      }

                      fveSwitch {
                          _id "second-setting"
                          _name "setting"
                          _ariaLabel "Second setting"
                      }

                      fveRadio {
                          _id "first-mode"
                          _name "mode"
                          _value "a"
                          _ariaLabel "First mode"
                      }

                      fveRadio {
                          _id "second-mode"
                          _name "mode"
                          _value "b"
                          _ariaLabel "Second mode"
                      }
                  }
                  |> Render.toString

              for expected in
                  [ "first-choice"
                    "second-choice"
                    "first-setting"
                    "second-setting"
                    "first-mode"
                    "second-mode" ] do
                  Expect.stringContains html expected $"stable instance owns {expected}"

              let ids =
                  Regex.Matches(html, "\\sid=\"([^\"]+)\"")
                  |> Seq.cast<Match>
                  |> Seq.map (fun matched -> matched.Groups[1].Value)
                  |> Seq.toList

              Expect.equal
                  ids.Length
                  (ids |> List.distinct |> List.length)
                  "repeated form names produce no duplicate IDs"

              Expect.equal
                  (Regex.Matches(html, "name=\"choice\"").Count)
                  2
                  "stable IDs do not change shared Checkbox names"

              Expect.equal
                  (Regex.Matches(html, "name=\"mode\"").Count)
                  2
                  "stable IDs do not change shared RadioGroup names"
          }

          test "Components option IDs are stable and collision-free for distinct encoded values" {
              let options = [ ("a/b", "Slash", false); ("a-b", "Dash", false) ]

              let optionIds prefix html =
                  Regex.Matches(html, $"id=\"({Regex.Escape(prefix)}-(?:option|toggle)-[^\"]+)\"")
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
                  fveField {
                      fveLabel {
                          _for ("collisionselect")
                          "Collision select"
                      }

                      fveSelect {
                          _id ("collisionselect")
                          _name ("collisionselect")

                          fveSelectContent {
                              for value, label, disabled in (options) do
                                  fveSelectItem {
                                      _value (value)
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString)

              expectStableDistinct "fve-select-collisioncombobox" (fun () ->
                  fveField {
                      fveLabel {
                          _for ("collisioncombobox")
                          "Collision searchable Select"
                      }

                      fveSelect {
                          _id ("collisioncombobox")
                          _name ("collisioncombobox")
                          _search (SelectSearch.Static)

                          fveSelectContent {
                              for value, label, disabled in (options) do
                                  fveSelectItem {
                                      _value (value)
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString)

              expectStableDistinct "collisiontoggles" (fun () ->
                  fveToggleGroup {
                      _id "collisiontoggles"
                      _name "choice"
                      _ariaLabel "Collision toggles"

                      fveToggleGroupItem {
                          _value "a/b"
                          "Slash"
                      }

                      fveToggleGroupItem {
                          _value "a-b"
                          "Dash"
                      }
                  }
                  |> Render.toString)
          }

          test "Components escape hatches preserve authoritative attributes" {
              let openingTag elementName element =
                  let html = element |> Render.toString
                  let tag = Regex.Match(html, $"<{elementName}[^>]*>", RegexOptions.IgnoreCase).Value
                  Expect.isNotEmpty tag $"{elementName} opening tag"
                  tag

              let attributeCount name tag =
                  Regex
                      .Matches(tag, $"\\s{Regex.Escape(name)}(?:=|\\s|>)", RegexOptions.IgnoreCase)
                      .Count

              let buttonTag =
                  fveButton {
                      _disabled true
                      _attr ("TYPE", "submit")
                      _attr "disabled"
                      _class "override"
                      "Save"
                  }
                  |> openingTag "fve-button"

              Expect.equal (attributeCount "type" buttonTag) 1 "Button owns one type"
              Expect.equal (attributeCount "disabled" buttonTag) 1 "Button owns one disabled state"
              Expect.equal (attributeCount "class" buttonTag) 1 "Button owns one class attribute"

              Expect.stringContains
                  (buttonTag.ToLowerInvariant())
                  "type=\"submit\""
                  "native attributes are recognized case-insensitively"

              Expect.stringContains buttonTag "override" "consumer classes are merged, not duplicated"

              let badgeTag =
                  fveBadge {
                      _class "override"
                      "Active"
                  }
                  |> openingTag "span"

              Expect.equal (attributeCount "class" badgeTag) 1 "Badge owns one class attribute"
              Expect.stringContains badgeTag "override" "Badge merges consumer classes"

              let tableTag =
                  fveTable {
                      _ariaLabel "Values"
                      _class "border"
                      _class "override"
                  }
                  |> openingTag "fve-table"

              Expect.equal (attributeCount "class" tableTag) 1 "Table owns one class attribute"
              Expect.stringContains tableTag "border override" "Table composes ordinary native host classes"

              Expect.throws
                  (fun () ->
                      fveTable {
                          _ariaLabel "Values"
                          _role "presentation"
                      }
                      |> ignore)
                  "Table rejects a conflicting host role"

              let selectHtml =
                  fveField {
                      fveLabel {
                          _for ("status")
                          "Status"
                      }

                      fveSelect {
                          _id ("status")
                          _name ("status")

                          yield!
                              ([ _id "other-id"
                                 _name "other-name"
                                 _dataBind "other"
                                 _ariaActivedescendant "other-option"
                                 _ariaInvalid false
                                 _class "override" ])

                          fveSelectContent {
                              for value, label, disabled in ([ ("active", "Active", false) ]) do
                                  fveSelectItem {
                                      _value (value)
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> Render.toString

              let selectTrigger =
                  Regex
                      .Match(selectHtml, "<button[^>]*role=\"combobox\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              Expect.stringContains
                  selectTrigger
                  "id=\"other-id\""
                  "ordinary id attributes use their last authored value"

              Expect.stringContains selectTrigger "override" "ordinary classes compose with the control presentation"

              let selectValue =
                  Regex
                      .Match(selectHtml, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase)
                      .Value

              Expect.isNotEmpty selectTrigger "Select trigger opening tag"
              Expect.isNotEmpty selectValue "Select hidden value opening tag"

              for name in [ "id"; "type"; "role"; "aria-invalid"; "class" ] do
                  Expect.equal (attributeCount name selectTrigger) 1 $"Select owns one trigger {name}"

              Expect.equal (attributeCount "name" selectValue) 1 "Select owns one submitted name"

              Expect.equal
                  (Regex
                      .Matches(selectValue, "\\sdata-bind:[^=\\s>]+(?:=|\\s|>)", RegexOptions.IgnoreCase)
                      .Count)
                  1
                  "Select owns one submitted Datastar binding"

              for rejected in [ "data-bind:other"; "other-option" ] do
                  Expect.isFalse
                      (selectTrigger.Contains(rejected))
                      $"Select rejects reserved trigger attribute value {rejected}"

              let comboboxTag =
                  fveField {
                      fveLabel {
                          _for ("account")
                          "Account"
                      }

                      fveSelect {
                          _id ("account")
                          _name ("account")
                          _search (SelectSearch.Static)

                          yield!
                              ([ _id "other-id"
                                 _name "other-name"
                                 _dataBind "other"
                                 _ariaActivedescendant "other-option"
                                 _ariaInvalid false
                                 _class "override" ])

                          fveSelectContent {
                              for value, label, disabled in ([ ("operating", "Operating", false) ]) do
                                  fveSelectItem {
                                      _value (value)
                                      _label label
                                      _disabled disabled
                                  }
                          }
                      }
                  }
                  |> openingTag "button"

              for name in [ "id"; "type"; "aria-invalid"; "class" ] do
                  Expect.equal (attributeCount name comboboxTag) 1 $"Searchable Select owns one trigger {name}"

              for rejected in [ "data-bind:other"; "other-option" ] do
                  Expect.isFalse
                      (comboboxTag.Contains(rejected))
                      $"Searchable Select rejects reserved trigger attribute value {rejected}"
          }

          test "Notification owns local lifetime and NotificationRegion renders zero or one latest item" {
              let basic ttl persistent =
                  fveNotification {
                      _id "notice-default"
                      _label "Saved"
                      _persistent persistent

                      match ttl with
                      | Some value -> _ttl value
                      | None -> ()

                      p { "The record is current." }
                  }

              let basicHtml = basic None false |> Render.toString
              Expect.stringContains basicHtml "data-fve-notification-ttl=\"5000\"" "default lifetime"
              Expect.stringContains basicHtml "data-on-interval__duration.100ms" "local timer"
              Expect.stringContains basicHtml "document.hidden" "hidden documents pause expiry"

              Expect.stringContains
                  basicHtml
                  "closest(&#39;[data-fve-notification-region]&#39;)"
                  "the whole active region owns interaction pause"

              Expect.stringContains basicHtml ":hover" "hover pauses expiry"
              Expect.stringContains basicHtml ":focus-within" "focus pauses expiry"
              Expect.stringContains basicHtml "bg-[var(--fve-surface)]" "notification card uses the neutral surface"
              Expect.stringContains basicHtml "data-fve-notification-dismiss" "accessible dismissal control"

              let configuredHtml = basic (Some 1250) false |> Render.toString
              Expect.stringContains configuredHtml "data-fve-notification-ttl=\"1250\"" "custom positive lifetime"
              let persistentHtml = basic None true |> Render.toString
              Expect.stringContains persistentHtml "data-fve-notification-persistent=\"true\"" "persistent mode"

              Expect.isFalse
                  (persistentHtml.Contains("data-on-interval"))
                  "persistent notifications do not start a timer"

              Expect.throws (fun () -> basic (Some 0) false |> ignore) "non-positive lifetimes are rejected"

              let regionHtml =
                  fveNotificationRegion {
                      _id "current-notification"
                      _ariaLabel "Current notification"
                      _withinContainer true
                      basic None false
                  }
                  |> Render.toString

              Expect.equal
                  (Regex.Matches(regionHtml, "data-fve-notification=\"true\"").Count)
                  1
                  "populated regions render one card"

              Expect.stringContains regionHtml "notice-default" "the current notification is rendered"
              Expect.stringContains regionHtml "bottom-4" "bottom placement"
              Expect.stringContains regionHtml "sm:end-4" "logical inline-end placement"

              Expect.stringContains
                  regionHtml
                  "max-h-[calc(100%-24px)]"
                  "bounded notifications remain available at large text sizes"

              Expect.stringContains regionHtml "overflow-y-auto" "oversized notification content remains reachable"

              let emptyHtml =
                  fveNotificationRegion {
                      _id "empty-notifications"
                      _ariaLabel "Notifications"
                  }
                  |> Render.toString

              Expect.stringContains emptyHtml "id=\"empty-notifications\"" "empty regions are supported"

              Expect.equal
                  (Regex.Matches(emptyHtml, "data-fve-notification=\"true\"").Count)
                  0
                  "empty regions contain no card"
          }

          test "Documentation and controls share one assembly without implicit browser assets" {
              let assembly = typeof<Size>.Assembly

              Expect.equal
                  typeof<FSharp.ViewEngine.Components.Templates.DocsColorMode>.Assembly
                  assembly
                  "Documentation ships inside Components"

              Expect.equal
                  (assembly.GetName().Name)
                  "Docs.Components"
                  "the catalog uses CLI-installed source, not the authoring assembly"

              let ordinaryPage = fveButton { "Continue" } |> Render.toString

              for documentationAsset in [ "Prism"; "mermaid"; "spec-shell"; "data-docs-" ] do
                  Expect.isFalse
                      (ordinaryPage.Contains documentationAsset)
                      "ordinary controls do not opt into Documentation assets"
          }

          test "Removed Tailwind documentation route is not registered" {
              Expect.isFalse
                  (Registry.all |> List.exists (fun page -> page.path = "/extensions/tailwind"))
                  "old canonical route"

              Expect.isFalse
                  (Registry.aliases
                   |> List.exists (fun (alias, _) -> alias = "/extensions/tailwind"))
                  "old route alias"

              Expect.equal (routeStatus "/extensions/tailwind") 404 "removed route returns not found"
          }

          test "Catalog pages do not maintain compatibility aliases" {
              Expect.isEmpty Registry.aliases "only current destinations are registered"
          } ]

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args (testList "Docs" [ ComponentDslTests.tests; tests ])
