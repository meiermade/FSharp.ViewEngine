module Docs.Tests.Program

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open Expecto
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open FSharp.ViewEngine.Components.Application
open type Html
open type Datastar
open Docs.Common
open Docs.Pages

let private docsTailwindManifest () =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "FSharp.ViewEngine.Components", "Documentation", "Documentation.tailwind.css"))
    |> File.ReadAllText

type private ShellTestDestination =
    | Home
    | Accounts
    | Account of int
    | Reports
    | Settings

let private shellTestUrl = function
    | Home -> "/"
    | Accounts -> "/accounts"
    | Account id -> $"/accounts/{id}"
    | Reports -> "/reports"
    | Settings -> "/settings"

let private expectedPaths =
    set [
        "/"
        "/installation"
        "/getting-started/first-view"
        "/guides/elements-and-attributes"
        "/guides/composition-and-control-flow"
        "/guides/rendering"
        "/guides/encoding-and-trusted-content"
        "/guides/accessibility"
        "/custom"
        "/usage"
        "/extensions/alpine"
        "/extensions/datastar"
        "/extensions/htmx"
        "/extensions/svg"
        "/extensions/tailwind-elements"
        "/docs"
        "/docs/components/layouts"
        "/docs/components/content"
        "/docs/components/navigation"
        "/docs/components/fixture"
        "/docs/components/api-reference"
        "/docs/components/diagrams"
        "/docs/page-examples/documentation-site"
        "/docs/page-examples/api-reference"
        "/docs/page-examples/executable-specification"
        "/components"
        "/components/primitives"
        "/components/application"
        "/components/installation"
        "/components/button"
        "/components/icon-button"
        "/components/badge"
        "/components/status"
        "/components/loading-indicator"
        "/components/progress"
        "/components/empty-state"
        "/components/action-cluster"
        "/components/row-actions"
        "/components/table"
        "/components/description-list"
        "/components/metric"
        "/components/pagination"
        "/components/avatar"
        "/components/copy-reveal"
        "/components/input"
        "/components/file-selection"
        "/components/tag-input"
        "/components/form-layouts"
        "/components/textarea"
        "/components/error-summary"
        "/components/notice"
        "/components/select"
        "/components/checkbox"
        "/components/switch"
        "/components/toggle-button"
        "/components/breadcrumbs"
        "/components/side-nav"
        "/components/tabs"
        "/components/radio-group"
        "/components/choice-cards"
        "/components/dropdown-menu"
        "/components/dialog"
        "/components/confirmation-dialog"
        "/components/drawer"
        "/components/page-top-bar"
        "/components/page-header"
        "/components/section"
        "/components/browser"
        "/components/phone"
        "/components/page"
        "/components/collection"
        "/components/detail"
        "/components/app-shell"
        "/components/bottom-navigation"
        "/components/bulk-actions"
        "/components/upload"
        "/components/steps"
        "/components/first-steps"
        "/components/calendar"
        "/components/media-library"
        "/components/integrations/graph-and-trace"
        "/components/integrations/financial-chart"
        "/components/integrations/messaging"
        "/components/interaction-and-server-state"
        "/components/accessibility"
        "/components/theming"
        "/components/tailwind-css"
        "/components/customization"
        "/components/versioning"
        "/benchmarks"
        "/changelog"
    ]

[<Tests>]
let tests =
    testList "Direct F# documentation" [
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

            let hierarchy =
                Table.create "Tree" [ Table.column "Name" text |> Table.asRowHeader ] [ "Parent"; "Child" ]
                |> Table.withHierarchy (
                    TableHierarchy.create "tree" id id (function "Child" -> [ "Parent" ] | _ -> []) (function "Child" -> 1 | _ -> 0) ((=) "Parent")
                    |> TableHierarchy.withExpandedKeys [ "Parent" ])
                |> Table.render
                |> Render.toString
            Expect.stringContains hierarchy "aria-label=\"Toggle Parent\"" "parent disclosure is named"
            Expect.stringContains hierarchy "data-show=\"$_table_v74726565_expanded.includes(&quot;Parent&quot;)\"" "descendant visibility follows its supplied ancestor"

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

            let calendar = Components.calendarExample CalendarView.Week 0 |> Render.toString
            Expect.stringContains calendar "aria-label=\"Calendar view\"" "calendar view navigation is a labelled native nav"
            Expect.stringContains calendar "aria-current=\"page\"" "calendar identifies the current linked view"
            Expect.stringContains calendar ">Week</a>" "calendar preserves the selected view label"
            Expect.stringContains calendar "Overlaps Northwind by one hour" "calendar retains consumer-authored overlap context"
            let loadingCalendar = Calendar.create "Schedule" CalendarView.List "September" [] |> Calendar.loading |> Calendar.render id |> Render.toString
            Expect.stringContains loadingCalendar "role=\"status\"" "loading calendar announces its state"
            Expect.stringContains loadingCalendar "Loading calendar…" "loading calendar remains readable"
            let errorCalendar = Calendar.create "Schedule" CalendarView.List "September" [] |> Calendar.withError "Schedule failed." |> Calendar.render id |> Render.toString
            Expect.stringContains errorCalendar "role=\"alert\"" "calendar errors are urgent feedback"
            Expect.stringContains errorCalendar "Schedule failed." "calendar errors preserve consumer-authored messages"
            let unavailableCalendar = Calendar.create "Schedule" CalendarView.List "September" [] |> Calendar.withUnavailable "Schedule unavailable." |> Calendar.render id |> Render.toString
            Expect.stringContains unavailableCalendar "Schedule unavailable." "unavailable calendar state is explicit"

            let media = Components.mediaLibraryExample |> Render.toString
            Expect.stringContains media "name=\"assetIds\" value=\"trail-front\"" "media selection uses native repeated controls"
            Expect.stringContains media "alt=\"Blue trail pack shown from the front\"" "media thumbnails require consumer-authored alternatives"
            Expect.stringContains media "fve-selection-change" "media selection publishes stable identities to shared bulk actions"
            Expect.stringContains media "No media assets. Upload an image to begin." "media example includes its empty state"
            Expect.stringContains media "Media could not be loaded." "media example includes its recoverable error state"
        }

        test "Native fields preserve encoded values and protect their semantic attributes" {
            let config =
                Input.create "email" "Email address"
                |> Input.withId "customer-email"
                |> Input.withType InputType.Email
                |> Input.withValue "<customer@example.test>"
                |> Input.withDescription "For correspondence."
                |> Input.withValidation "Enter a valid address."
                |> Input.required
                |> Input.withAttributes [ _id "wrong"; _type "hidden"; _name "wrong"; _value "wrong"; _autocomplete "email"; _ariaInvalid false; _role "wrong"; _ariaLabel "wrong"; _ariaLabelledby "wrong" ]
            let html = config |> Input.render |> Render.toString
            Expect.equal (Input.id config) "customer-email" "explicit ID is the actual focus target"
            for expected in [ "type=\"email\""; "name=\"email\""; "value=\"&lt;customer@example.test&gt;\""; "for=\"customer-email\""; "aria-invalid=\"true\""; "autocomplete=\"email\""; "aria-describedby=\"customer-email-description customer-email-validation\"" ] do
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
                ErrorSummary.create "validation-errors" "Check details" [ FieldError.create "contact:email" "Email" "Use <name@example.test>." ]
                |> ErrorSummary.focusOnMount
                |> ErrorSummary.render
                |> Render.toString
            Expect.stringContains html "role=\"alert\"" "one summary announcement"
            Expect.stringContains html "href=\"#contact%3Aemail\"" "fragment safely encodes the exact control ID"
            Expect.stringContains html "data-init=\"el.focus()\"" "focus is explicitly opt-in at insertion"
            Expect.stringContains html "Email: Use &lt;name@example.test&gt;." "corrective content is encoded"
            Expect.throws (fun () -> ErrorSummary.create "errors" "Errors" [] |> ignore) "empty summaries are rejected"
        }

        test "Notice tone does not imply announcement urgency" {
            let notice = Notice.create "feedback" "Check details" (p { "Review the account." }) |> Notice.withTone Tone.Critical
            let staticHtml = notice |> Notice.render |> Render.toString
            Expect.isFalse (staticHtml.Contains("role=\"alert\"")) "static critical notice does not announce"
            let polite = notice |> Notice.withAnnouncement NoticeAnnouncement.Polite |> Notice.render |> Render.toString
            Expect.stringContains polite "role=\"status\"" "polite completion is a status"
            let urgent = notice |> Notice.withAnnouncement NoticeAnnouncement.Assertive |> Notice.render |> Render.toString
            Expect.stringContains urgent "role=\"alert\"" "urgent feedback is explicit"
            Expect.stringContains urgent "aria-atomic=\"true\"" "message is coherent"
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
            Expect.equal aliases["/docs/components"] "/docs/components/layouts" "old component catalog route"
            Expect.equal aliases["/docs-components"] "/docs/components/layouts" "old component lab route"
            Expect.equal aliases["/docs/examples/api-reference"] "/docs/page-examples/api-reference" "old API example route"
            Expect.equal aliases["/docs/examples/executable-specification"] "/docs/page-examples/executable-specification" "old specification route"
        }

        test "Navigation exposes the core learning path before integrations and project pages" {
            Expect.sequenceEqual
                (Registry.navigation |> List.map _.label)
                [ "Getting started"; "Core concepts"; "Integrations"; "FSharp.ViewEngine.Components"; "Project" ]
                "package catalog is nested while Project remains top-level"
            let components = Registry.navigation |> List.find (fun section -> section.label = "FSharp.ViewEngine.Components")
            Expect.sequenceEqual
                (components.sections |> List.map _.label)
                [ "Guides"; "Primitives"; "Application"; "Documentation" ]
                "delivered catalog areas belong to the Components package"

            let rec findSection label sections =
                sections
                |> List.tryPick (fun candidate ->
                    if candidate.label = label then Some candidate
                    else findSection label candidate.sections)

            let section label =
                Registry.navigation
                |> findSection label
                |> Option.defaultWith (fun () -> failtest $"Missing navigation section: {label}")
                |> _.pages
                |> List.map _.navLabel

            Expect.sequenceEqual
                (section "Getting started")
                [ "Introduction"; "Installation"; "Build your first view" ]
                "getting-started order"
            Expect.sequenceEqual
                (section "Core concepts")
                [ "Elements and attributes"; "Composition and control flow"; "Rendering"; "Encoding and trusted content"; "Accessibility"; "Custom elements and extensions" ]
                "core concept order"
            Expect.sequenceEqual
                (section "Integrations")
                [ "Giraffe"; "SVG"; "Datastar"; "HTMX"; "Alpine"; "Tailwind Plus Elements" ]
                "integration order"
            Expect.sequenceEqual (section "Documentation") [ "Overview" ] "toolkit overview is distinct"
            Expect.sequenceEqual
                (section "Components")
                [ "Layouts"; "Content"; "Navigation"; "Fixture"; "API reference"; "Diagrams" ]
                "component categories follow the catalog order"
            Expect.sequenceEqual
                (section "Page examples")
                [ "Documentation site"; "API reference"; "Executable specification" ]
                "page examples are grouped separately"
            Expect.sequenceEqual (section "FSharp.ViewEngine.Components") [ "Overview"; "Installation" ] "Components starts with overview and installation"
            Expect.sequenceEqual
                (section "Actions and feedback")
                [ "Button"; "Icon button"; "Action cluster"; "Row actions"; "Badge"; "Status"; "Notice"; "Loading indicator"; "Progress"; "Empty state" ]
                "action and feedback foundations"
            Expect.sequenceEqual
                (section "Data display")
                [ "Table"; "Description list"; "Metric"; "Pagination"; "Avatar"; "Copy and reveal" ]
                "data-display components"
            Expect.sequenceEqual (section "Form controls") [ "Input"; "Textarea"; "File selection"; "Tag input"; "Error summary"; "Select"; "Checkbox"; "Switch"; "Toggle button"; "Radio group"; "Choice cards" ] "form controls"
            Expect.sequenceEqual (section "Navigation") [ "Breadcrumbs"; "Side nav"; "Tabs" ] "navigation components"
            Expect.sequenceEqual (section "Menus and overlays") [ "Dropdown menu"; "Dialog"; "Confirmation dialog"; "Drawer" ] "menu and overlay components"
            Expect.sequenceEqual (section "Layout foundations") [ "Section"; "Browser"; "Phone" ] "shared framing primitives are catalogued with Section"
            Expect.sequenceEqual (section "Shells and pages") [ "App shell"; "Bottom navigation"; "Page"; "Page top bar"; "Page header" ] "Application shell and page compositions"
            Expect.sequenceEqual (section "Collections and details") [ "Collection"; "Detail"; "Bulk actions" ] "Application record compositions"
            Expect.sequenceEqual (section "Forms") [ "Form layouts"; "Upload" ] "Application form compositions"
            Expect.sequenceEqual (section "Workflows") [ "Steps"; "First steps" ] "Application workflow compositions"
            Expect.sequenceEqual (section "Resources") [ "Calendar"; "Media library" ] "Application resource compositions"
            Expect.sequenceEqual (section "Integration examples") [ "Graph and trace"; "Financial chart"; "Messaging" ] "bounded recipes are discoverable without claiming universal component ownership"
            for area in [ "Primitives"; "Application" ] do
                Expect.sequenceEqual (section area) [ "Overview" ] $"{area} has a real index destination"
            Expect.sequenceEqual (section "Guides") [ "Interaction and server state"; "Accessibility"; "Theming and density"; "Tailwind CSS"; "Customization"; "Versioning" ] "shared Components guides"
            Expect.sequenceEqual (section "Project") [ "Benchmarks"; "Changelog" ] "project order"
        }

        test "Catalog families have unique ownership, honest indexes and registry-derived pagers" {
            let families = Catalog.navigation
            Expect.sequenceEqual (families |> List.map _.label) [ "Primitives"; "Application"; "Documentation" ] "delivered public family names"
            let rec flatten (group:NavSection) = group.pages @ (group.sections |> List.collect flatten)
            let familyPages = families |> List.collect flatten
            Expect.equal (familyPages |> List.map _.path |> Set.ofList |> Set.count) familyPages.Length "each family route occurs exactly once"
            for family in families do
                for page in flatten family do
                    Expect.equal page.category family.label $"{page.path} metadata agrees with navigation ownership"
            let root = Catalog.overviewPage |> FSharp.ViewEngine.Components.Documentation.DocsView.content |> Render.toString
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

        test "Benchmark documentation records methodology, versions, and results" {
            let benchmarkPage = Registry.all |> List.find (fun page -> page.path = "/benchmarks")
            let html = benchmarkPage |> View.document Registry.navigation |> Render.toHtmlDocString

            Expect.stringContains html "BenchmarkDotNet 0.15.8" "measurement framework"
            Expect.stringContains html "Oxpecker.ViewEngine 2.0.1" "Oxpecker comparison version"
            Expect.stringContains html "Giraffe.ViewEngine 1.4.0" "Giraffe comparison version"
            Expect.stringContains html "Feliz.ViewEngine 1.0.3" "Feliz comparison version"
            Expect.stringContains html "Typical Render Times" "typical render-time summary"
            Expect.stringContains html "1.585 μs" "typical build-and-render time"
            Expect.stringContains html "833.5 ns" "typical render-only time"
            Expect.stringContains html "not HTTP requests per second" "throughput limitation"
            Expect.stringContains html "How the Benchmarks Were Run" "methodology heading"
            Expect.stringContains html "How to Run the Benchmarks" "reproduction heading"
            Expect.isFalse (html.Contains "Which Scenario Matches My App?") "scenario guide removed"
            Expect.isFalse (html.Contains "What the Results Suggest") "results interpretation section removed"
            Expect.stringContains html "1.35&#215; as long" "Oxpecker relative comparison"
            Expect.stringContains html "2.35&#215; as long" "Feliz relative comparison"
            Expect.stringContains html "not CI regression thresholds" "results interpretation"
            Expect.stringContains html "<figure" "visual comparison"
            Expect.stringContains html "Build and render comparison" "accessible comparison label"
            Expect.stringContains html "docs-comparison-chart" "comparison uses theme-aware semantic styling"
            Expect.stringContains html "Lower is better" "chart direction is explicit"
            Expect.isFalse (html.Contains("background:#fafafa")) "chart does not hard-code a light surface"
            Expect.stringContains html "<table" "semantic results table"
            Expect.stringContains html "./fake.sh Benchmark" "measurement command"
            Expect.stringContains html "./fake.sh BenchmarkSmoke" "validation command"

            let appendixIndex = html.IndexOf("Appendix: Detailed Results", System.StringComparison.Ordinal)
            let firstTableIndex = html.IndexOf("<table", System.StringComparison.Ordinal)
            Expect.isGreaterThan appendixIndex 0 "appendix heading"
            Expect.isGreaterThan firstTableIndex appendixIndex "detailed tables follow analysis"
        }

        test "Public discovery contains canonical pages and excludes aliases and previews" {
            let sitemap = Handler.sitemap
            let robots = Handler.robots

            for page in Registry.all do
                Expect.stringContains sitemap $"<loc>https://fsharpviewengine.meiermade.com{page.path}</loc>" page.path

            Expect.equal (sitemap.Split("<url>").Length - 1) Registry.all.Length "one sitemap entry per canonical page"
            Expect.isFalse (sitemap.Contains("/docs/components</loc>")) "aliases are excluded"
            Expect.isFalse (sitemap.Contains("/docs/previews/")) "previews are excluded"
            Expect.stringContains robots "Allow: /" "public pages are crawlable"
            Expect.stringContains robots "Sitemap: https://fsharpviewengine.meiermade.com/sitemap.xml" "robots advertises sitemap"
        }

        test "Pages publish the application-owned social image" {
            for page in Registry.all do
                let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                Expect.stringContains html "property=\"og:image\" content=\"https://fsharpviewengine.meiermade.com/social-card.png\"" page.path
                Expect.stringContains html "name=\"twitter:card\" content=\"summary_large_image\"" page.path
        }

        test "Every page renders its semantic heading and legacy anchors" {
            for page in Registry.all do
                let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                let encodedTitle = WebUtility.HtmlEncode page.title
                Expect.stringContains html $">{encodedTitle}</h1>" $"{page.path} title"

                if Catalog.tryPage page.path |> Option.orElseWith (fun () -> Showcase.tryPage page.path) |> Option.orElseWith (fun () -> Components.tryPage page.path) |> Option.isNone then
                    let description = page.nodes |> List.tryPick (function | Paragraph content -> Some content | _ -> None)
                    Expect.isSome description $"{page.path} has a useful page summary"
                    Expect.isFalse (html.Contains($"<p class=\"spec-page-description\">{page.category}</p>")) $"{page.path} does not repeat its category as the description"

                for heading in DocPage.headings page do
                    Expect.stringContains html $"id=\"{heading.id}\"" $"{page.path} heading {heading.id}"
                    if heading.level <= 3 then
                        Expect.stringContains html $"href=\"#{heading.id}\"" $"{page.path} TOC {heading.id}"

                if DocPage.tableOfContents page |> List.isEmpty |> not then
                    Expect.stringContains html "class=\"spec-mobile-toc\"" $"{page.path} mobile TOC"
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
        }

        test "Inline prose links are visibly identifiable" {
            let home = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let installation = Installation.page |> View.document Registry.navigation |> Render.toHtmlDocString

            Expect.stringContains home "href=\"/installation\" class=\"spec-content-link\"" "Installation is a styled inline link"
            Expect.stringContains home "href=\"/getting-started/first-view\" class=\"spec-content-link\"" "first-view guide is a styled inline link"
            Expect.stringContains installation "href=\"/getting-started/first-view\" class=\"spec-content-link\"" "next-step first-view link is styled"
            let manifest = docsTailwindManifest ()
            Expect.stringContains manifest ".spec-content-link {" "inline links have package styling"
            Expect.stringContains manifest "text-decoration: underline;" "inline links have a non-color affordance"
        }

        test "Code examples are encoded and retain Prism language classes" {
            let html = Custom.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "language-fsharp" "F# language class"
            Expect.stringContains html "language-html" "HTML language class"
            Expect.stringContains html "&lt;my-component class=&quot;container&quot;&gt;" "HTML source is encoded"
            Expect.isFalse (html.Contains("<my-component class=\"container\">")) "example markup must not execute"
        }

        test "Migrated pages retain recent documentation updates" {
            let customHtml = Custom.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let usageHtml = Usage.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let compositionHtml = CoreGuides.composition |> View.document Registry.navigation |> Render.toHtmlDocString
            let renderingHtml = CoreGuides.rendering |> View.document Registry.navigation |> Render.toHtmlDocString
            let datastarHtml = Datastar.page |> View.document Registry.navigation |> Render.toHtmlDocString

            Expect.stringContains customHtml "Trusted Content Boundaries" "trusted-content guidance"
            Expect.stringContains usageHtml "title {" "title computation-expression guidance"
            Expect.isFalse (usageHtml.Contains("titleBuilder")) "obsolete title builder guidance is removed"
            Expect.stringContains compositionHtml "yield!" "collection composition guidance"
            Expect.stringContains renderingHtml "fragment {" "fragment computation-expression guidance"
            Expect.stringContains renderingHtml "Html.fragment nodes" "fragment migration guidance"
            Expect.stringContains renderingHtml "titleBuilder" "title migration guidance"
            Expect.stringContains datastarHtml "Datastar 1.0.2" "pinned Datastar reference"
        }

        test "Rendered pages contain no Markdown fences" {
            for page in Registry.all do
                let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                Expect.isFalse (html.Contains("```")) $"{page.path} contains a Markdown fence"
        }

        test "HTMX docs cover every dedicated stable helper" {
            let html = Htmx.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let helpers =
                [ "_hxBoost"; "_hxConfirm"; "_hxDelete"; "_hxDisable"; "_hxDisabledElt"
                  "_hxDisinherit"; "_hxEncoding"; "_hxExt"; "_hxGet"; "_hxHeaders"
                  "_hxHistory"; "_hxHistoryElt"; "_hxInclude"; "_hxIndicator"; "_hxInherit"
                  "_hxOn"; "_hxParams"; "_hxPatch"; "_hxPost"; "_hxPreserve"; "_hxPrompt"
                  "_hxPushUrl"; "_hxPut"; "_hxReplaceUrl"; "_hxRequest"; "_hxSelect"
                  "_hxSelectOOB"; "_hxSwap"; "_hxSwapOOB"; "_hxSync"; "_hxTarget"
                  "_hxTrigger"; "_hxValidate"; "_hxVals" ]

            for helper in helpers do
                Expect.stringContains html helper helper

            Expect.stringContains html "htmx:before-request" "kebab-case HTMX event"
            Expect.isFalse (html.Contains("htmx:beforeRequest")) "camelCase event names fail after DOM normalization"
        }

        test "Alpine docs cover core and plugin directives" {
            let html = Alpine.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let coreHelpers =
                [ "_xBind"; "_xCloak"; "_xData"; "_xEffect"; "_xFor"; "_xHtml"
                  "_xId"; "_xIf"; "_xIgnore"; "_xInit"; "_xModel"; "_xModelable"
                  "_xOn"; "_xRef"; "_xShow"; "_xTeleport"; "_xText"; "_xTransition" ]
            let pluginHelpers =
                [ "_xMask"; "_xMaskDynamic"; "_xIntersect"; "_xResize"; "_xCollapse"
                  "_xTrap"; "_xAnchor"; "_xSort"; "_xSortItem"; "_xSortGroup"
                  "_xSortConfig"; "_xSortHandle"; "_xSortIgnore" ]

            for helper in coreHelpers @ pluginHelpers do
                Expect.stringContains html helper helper

            Expect.stringContains html "Focus plugin" "x-trap dependency"
            Expect.stringContains html "Anchor plugin" "x-anchor dependency"
            Expect.stringContains html "$persist" "Persist has no directive helper"
            Expect.stringContains html "Alpine.morph" "Morph has no directive helper"
        }

        test "Docs use only the pinned self-hosted Datastar runtime" {
            let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "/scripts/datastar.1.0.2.js" "pinned Datastar script"
            Expect.stringContains html "type=\"module\"" "Datastar module script"
            Expect.isFalse (html.Contains("alpinejs")) "Alpine runtime removed"
            Expect.isFalse (html.Contains(" x-data=")) "Alpine directives removed"
        }

        test "Homepage uses the product logo and Tailwind Sky primary color" {
            let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "class=\"docs-home-logo\"" "product logo is shown in the page header"
            Expect.stringContains html "src=\"/logo.svg\"" "page uses the canonical logo asset"
            Expect.stringContains html "--spec-accent-500:#0ea5e9" "site uses Tailwind Sky 500"
            Expect.stringContains html "--spec-accent-700:#0369a1" "site uses Tailwind Sky 700"
            Expect.isFalse (html.Contains("--spec-accent-500:#10b981")) "emerald primary is removed"
        }

        test "Docs manifest uses semantic typography roles with consumer-owned Noto preferences" {
            let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let manifest = docsTailwindManifest ()
            Expect.stringContains manifest "--docs-text-ancillary: 0.75rem" "ancillary text uses the 12px baseline"
            Expect.stringContains manifest "--docs-text-ui: 0.875rem" "documentation UI uses the 14px baseline"
            Expect.stringContains manifest "--docs-text-reading: 1rem" "reading and form text use the 16px baseline"
            Expect.stringContains manifest "--docs-text-code: 0.875rem" "code remains compact and readable at 14px"
            Expect.stringContains manifest "min-width: 2rem" "copy controls retain a compact minimum target"
            Expect.stringContains manifest "white-space: nowrap" "copy labels remain intact on constrained screens"
            Expect.stringContains manifest "--docs-font-sans: \"Noto Sans\", ui-sans-serif, system-ui, sans-serif" "Noto Sans remains preferred with system fallbacks"
            Expect.stringContains manifest "--docs-font-mono:" "the semantic monospace variable is declared"
            Expect.stringContains manifest "\"Noto Sans Mono\", ui-monospace" "Noto Sans Mono remains preferred with system fallbacks"
            for forbidden in [ "font-size: 0.625rem"; "font-size: 0.6875rem"; "font-size: 0.8125rem" ] do
                Expect.isFalse (manifest.Contains forbidden) $"package styles omit non-semantic size {forbidden}"
            Expect.isFalse (html.Contains("<style")) "the package does not embed its presentation"
            Expect.stringContains html "rel=\"stylesheet\" href=\"/css/output.css\"" "the repository host links its compiled stylesheet"
            Expect.isFalse (html.Contains("fonts.googleapis.com")) "the package makes no Google Fonts request"
            Expect.isFalse (html.Contains("fonts.gstatic.com")) "the package ships no external font source"
        }

        test "Homepage quick example is Datastar-first" {
            let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "open type Datastar" "Datastar API"
            Expect.stringContains html "_dataOn" "Datastar interaction"
            Expect.isFalse (html.Contains("open type Htmx")) "HTMX is not used by the quick example"
            Expect.isFalse (html.Contains("_hxGet")) "HTMX is not used as the attribute example"
        }

        test "Docs embed dual Prism palettes and lazily use pinned self-hosted scripts" {
            let home = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let diagrams = Showcase.previewRoutes["/docs/previews/mermaid-diagram"]
            Expect.stringContains home "/scripts/prism.1.29.0.min.js" "pinned Prism script"
            Expect.isFalse (home.Contains("prism-tomorrow.1.29.0.min.css")) "dark-only Prism theme is not loaded"
            let manifest = docsTailwindManifest ()
            Expect.stringContains manifest "--docs-code-bg: #f6f8fa" "light code palette ships in the manifest"
            Expect.stringContains manifest "--docs-code-bg: #0d1117" "dark code palette ships in the manifest"
            Expect.stringContains home "/scripts/prism-fsharp.1.29.0.min.js" "pinned FSharp grammar"
            Expect.stringContains home "unloading: false" "Prism distinguishes active documents from documents being abandoned"
            Expect.stringContains home "if (this.unloading) resolve()" "only positively identified unloading documents suppress canceled asset errors"
            Expect.isFalse (home.Contains("document.visibilityState === 'hidden'")) "backgrounded active documents retain Prism asset failure reporting"
            Expect.stringContains home "window.addEventListener('pagehide', markDocsCodeUnloading)" "full-document navigation marks Prism loading as abandoned"
            Expect.stringContains home "window.addEventListener('beforeunload', markDocsCodeUnloading)" "pending Prism loading observes the earliest navigation boundary"
            Expect.stringContains home "window.removeEventListener('beforeunload', markDocsCodeUnloading)" "completed Prism loading restores back-forward cache eligibility"
            Expect.stringContains home "window.addEventListener('pageshow'" "restored documents resume active Prism error reporting"
            Expect.stringContains home "new ResizeObserver(update)" "table-of-contents tracking observes content reflow"
            Expect.stringContains home "document.fonts?.ready.then" "table-of-contents tracking follows preferred-font layout completion"
            Expect.stringContains home "!finalSection.isConnected" "stale font callbacks cannot overwrite a morphed table of contents"
            Expect.stringContains home "finalSectionVisible" "a visible final section remains the current table-of-contents location"
            Expect.stringContains home "window.fsharpDocsMermaid" "pages without diagrams configure lazy Mermaid for later navigation"
            Expect.stringContains home "/scripts/mermaid.11.16.0.min.js" "pages without diagrams retain the pinned Mermaid source"
            Expect.isFalse (home.Contains("src=\"/scripts/mermaid.11.16.0.min.js\"")) "pages without diagrams do not eagerly load Mermaid"
            Expect.stringContains diagrams "data-init=\"window.renderMermaid?.(el)\"" "diagram elements initialize through Datastar"
            Expect.isFalse (diagrams.Contains("src=\"/scripts/mermaid.11.16.0.min.js\"")) "diagram pages also load pinned Mermaid lazily"
            Expect.isFalse (home.Contains("cdnjs.cloudflare.com")) "documentation assets are self-hosted"
        }

        test "Showcase isolates only complete styled documents" {
            Expect.isFalse (Showcase.previewRoutes.ContainsKey "/docs/previews/workflow-states") "state tabs render directly with host styles"
            Expect.isFalse (Showcase.previewRoutes.ContainsKey "/docs/previews/browser-frame") "browser frames render directly without duplicate framing"

            for path, document in Showcase.previewRoutes |> Map.toSeq do
                Expect.stringStarts document "<!DOCTYPE html>" $"{path} is a complete HTML document"
                Expect.isFalse (document.Contains("<style")) $"{path} does not embed the Docs component styles"
                Expect.stringContains document "rel=\"stylesheet\" href=\"/css/output.css\"" $"{path} links the compiled consumer stylesheet"
                Expect.stringContains document "class=\"spec-document\"" $"{path} initializes the Docs document body"
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

        test "Components code exposes its API rather than only a private fixture helper" {
            for id, api in [
                "button", "Button.create"; "icon-button", "IconButton.create"; "badge", "Badge.create"
                "status", "Status.create"; "loading-indicator", "LoadingIndicator.create"; "empty-state", "EmptyState.create"
                "table", "Table.create"; "description-list", "DescriptionList.create"; "metric", "Metric.text"
                "pagination", "Pagination.create"; "input", "Input.create"; "textarea", "Textarea.create"
                "error-summary", "ErrorSummary.create"; "notice", "Notice.create"; "search-input", "InputType.Search"
                "select", "Select.create"; "checkbox", "Checkbox.create"
                "switch", "Switch.create"; "toggle-button", "ToggleButton.create"; "tabs", "Tabs.create"
                "radio-group", "RadioGroup.create"; "dropdown-menu", "DropdownMenu.create"; "dialog", "Dialog.create"
                "confirmation-dialog", "ConfirmationDialog.create"; "drawer", "Drawer.create"
                "breadcrumbs", "Breadcrumbs.create"; "side-nav", "SideNav.create"; "page-top-bar", "PageTopBar.create"
                "page-header", "PageHeader.create"; "section", "Section.create"; "page", "Page.create"
                "collection", "Collection.create"; "detail", "Detail.create"; "app-shell", "AppShell.create" ] do
                let example = Components.allExamples () |> List.find (fun example -> example.id = "components-" + id)
                Expect.stringContains example.source api $"{id} shows its component construction"
                Expect.stringContains example.source "open FSharp.ViewEngine.Components" $"{id} includes imports in its own code"
            for example in Components.allExamples () do
                for forbidden in [ "themedSurface"; "fullBleedThemedSurface"; "themedPreview"; "FSharp.ViewEngine.Docs"; "shellDocumentNavigationAttributes" ] do
                    Expect.isFalse (example.source.Contains forbidden) $"{example.id} excludes Docs-only helper {forbidden}"
            let select = Components.examplesFor "select" |> List.head
            Expect.stringContains select.source "Select.option" "basic Select includes its actual finite options"
            Expect.isFalse (select.source.Contains "selectFormRegion") "basic Select does not require a validation workflow"
        }

        test "Input galleries teach individual fields and Application owns complete forms" {
            let examples = Components.examplesFor "input"
            Expect.equal (examples |> List.map _.title) [ "With label"; "With help text"; "Required"; "Optional"; "With validation error"; "With leading icon"; "With prefix"; "With suffix"; "Search with clear action"; "Read-only"; "Disabled"; "Pending" ] "basic before variations and states"
            for example in examples do
                Expect.equal (Regex.Matches(Render.toString example.preview, "<input ").Count) 1 "one native input per example"
                Expect.isLessThan (example.source.Split('\n').Length) 35 "short independently copyable input code"
                for forbidden in [ "ContactDetails"; "contactFormRegion"; "accountOptions"; "@post" ] do
                    Expect.isFalse (example.source.Contains forbidden) "input examples exclude complete workflows"
            for slug in [ "textarea"; "select"; "checkbox"; "switch"; "radio-group" ] do
                Expect.equal (Components.examplesFor slug |> List.head).title "With label" "other form galleries also start with the basic control"
            Expect.equal (Components.examplesFor "tag-input" |> List.map _.title) [ "Free-form tags"; "Validation"; "Pending"; "Disabled" ] "Tag input owns its focused states"
            Expect.equal Components.formLayoutsRegistration.category "Application" "forms are Application composition"
            Expect.equal (Components.examplesFor "form-layouts" |> List.length) 4 "three complete forms and the retained result-search example"
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

        test "Multiple choices preserve typed identities, labels, and native repeated values" {
            let options = [ Select.option "a&1" "Alex <Ops>"; Select.option "b\"2" "Jamie Lee" ]
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
            Expect.throws (fun () -> Select.create "duplicate" "Duplicate" (fun (_:int) -> "same") [ Select.option 1 "One"; Select.option 2 "Two" ] |> ignore) "encoded keys must be unique"
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
                            $"#r @\"{typeof<Html>.Assembly.Location}\"\n#r @\"{typeof<Tone>.Assembly.Location}\"\nopen FSharp.ViewEngine.Components.Primitives\n{control}.create \"ids\" \"Ids\" string [ Select.option 1 \"One\" ] |> {invalid} |> ignore"
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

        test "Table galleries isolate eight features without financial fixture dependencies" {
            let examples = Components.examplesFor "table"
            Expect.equal (examples |> List.map _.title) [ "Simple"; "Comfortable rows"; "With status values"; "With checkboxes"; "Stacked on mobile"; "Sortable records"; "Hierarchical accounts and aggregates"; "Empty state" ] "each table feature has a named example"
            for example in examples do
                Expect.stringContains example.source "Table.create" "the construction is directly visible"
                Expect.isLessThan (example.source.Split('\n').Length) 51 "copied table code stays focused"
                for forbidden in [ "ShellDestination"; "shellDestination"; "JsonSerializer"; "RowActions"; "navigator.clipboard"; "accountTable" ] do
                    Expect.isFalse (example.source.Contains forbidden) "the table gallery excludes application plumbing"
                let preview = Render.toString example.preview
                if example.title <> "Sortable records" && example.title <> "Hierarchical accounts and aggregates" then
                    Expect.isFalse (preview.Contains("<a ")) "ordinary first-column values are plain text"
            Expect.stringContains examples[3].source "TableSelection.withDisabledRows" "selection demonstrates disabled-row exclusion"
            Expect.stringContains examples[4].source "TableMobileLayout.Records" "responsive records are an explicit opt-in"
            Expect.stringContains examples[5].source "TableSort.ascending" "sorting exposes the current sort direction"
            Expect.stringContains examples[5].source "Table.withSort" "sorting keeps the next destination consumer-owned"
            Expect.stringContains examples[6].source "Table.withHierarchy" "hierarchy is an explicit consumer-authored opt-in"
            Expect.stringContains examples[7].source "Table.withEmptyState" "empty-state composition stays visible"
        }

        test "Every gallery example compiles using only its copied code and the public packages" {
            let directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fve-component-examples-" + System.Guid.NewGuid().ToString("N"))
            System.IO.Directory.CreateDirectory directory |> ignore
            try
                let examples = Components.allExamples ()
                let samples =
                    examples |> List.mapi (fun index example ->
                        $"# 1 \"{example.id}.fsx\"\nmodule Example{index} =\n" + (example.source.Split('\n') |> Array.map (fun line -> "    " + line) |> String.concat "\n"))
                let reference (path:string) = "#r @\"" + path.Replace("\"", "\"\"") + "\"\n"
                let source =
                    reference typeof<Html>.Assembly.Location + reference typeof<Tone>.Assembly.Location
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
                Expect.equal examples.Length 151 "all focused variants from fifty-four galleries compile against the public packages"
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

        test "Docs toolkit is organized into component and page-example catalogs" {
            let render registration = registration |> View.document Registry.navigation |> Render.toHtmlDocString
            let overview = render Showcase.overviewRegistration

            Expect.equal Showcase.componentRegistrations.Length 6 "six component categories"
            Expect.equal Showcase.pageExampleRegistrations.Length 3 "three page-example categories"
            Expect.stringContains overview "FSharp.ViewEngine.Components.Documentation" "unified namespace overview"
            Expect.stringContains overview "This documentation site is built with FSharp.ViewEngine.Components.Documentation" "site dogfoods the package"
            Expect.stringContains overview "Browse components" "component catalog link"
            Expect.stringContains overview "Browse page examples" "page-example catalog link"
            Expect.isFalse (overview.Contains("Example content")) "overview omits the old fixture callout"
            Expect.stringContains overview "rel=\"prev\" href=\"/components/integrations/messaging\"" "Documentation follows the Application catalog in the family order"
            Expect.stringContains overview "rel=\"next\" href=\"/docs/components/layouts\"" "overview continues to layouts"

            for registration in Showcase.componentRegistrations @ Showcase.pageExampleRegistrations do
                let html = render registration
                Expect.stringContains html "docs-article-layout" $"{registration.path} uses the catalog layout"
                Expect.stringContains html "data-docs-example=\"true\"" $"{registration.path} includes examples"
                Expect.stringContains html "Example: &#39;preview&#39;" $"{registration.path} shows previews by default"
                Expect.stringContains html ">Code</button>" $"{registration.path} code toggle"
                Expect.stringContains html ">Preview</button>" $"{registration.path} preview toggle"

            let api = render Showcase.apiPageExampleRegistration
            Expect.stringContains api "does not expose an HTTP /v1/render endpoint" "fictional API is labeled near its example"
            Expect.stringContains api "Endpoint.create POST &quot;/v1/render&quot;" "API page uses the compiled endpoint builder"
            Expect.stringContains api "Endpoint.withDescription &quot;Renders an HTML element.&quot;" "API page uses named endpoint configuration"
            Expect.stringContains api "data-docs-preview-src=\"/docs/previews/api-reference-page-example\"" "API preview lazily uses an isolated route"
            Expect.isTrue (Showcase.previewRoutes.ContainsKey "/docs/previews/api-reference-page-example") "isolated API preview is registered"

            let fixture = render Showcase.fixtureRegistration
            Expect.stringContains fixture "Tabs.create &quot;component-workflow-states&quot; &quot;Workflow states&quot;" "tabs code uses the preview's actual identifier"
            Expect.stringContains fixture "Tabs.withVariant TabsVariant.Underlined" "fixture uses the shared tabs primitive"
            Expect.stringContains fixture "productScreen &quot;ready&quot;" "tabs code uses the actual compiled child view"
            Expect.stringContains fixture "aria-label=\"Copy Workflow fixture code\"" "Fixture source has a direct copy action"
            Expect.isFalse (fixture.Contains("docsStateTabs")) "retired state-tabs API is absent"

            let specification = render Showcase.specificationPageExampleRegistration
            Expect.stringContains specification "role=\"tablist\"" "specification preview uses state tabs"
            Expect.stringContains specification "spec-browser-frame" "specification preview uses a browser frame"
        }

        test "Repository guidance preserves one documentation page per public component" {
            let guidance =
                Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "..", "AGENTS.md"))
                |> File.ReadAllText
            Expect.stringContains guidance "every consumer-facing reusable component a dedicated documentation route and navigation entry" "repository agents retain the catalog ownership rule"
            Expect.stringContains guidance "must never be the only documentation location for a component" "composition examples cannot hide component documentation"
            Expect.stringContains guidance "Integration examples" "bounded recipes retain separate catalog ownership"
        }

        test "Components publishes a first-class page for every public component and focused shared guides" {
            let render registration = registration |> View.document Registry.navigation |> Render.toHtmlDocString
            let overview = render Components.overviewRegistration
            let installation = render Components.installationRegistration
            let componentRegistrations =
                Components.actionRegistrations
                @ Components.dataDisplayRegistrations
                @ Components.formControlRegistrations
                @ Components.navigationRegistrations
                @ Components.menuOverlayRegistrations
                @ Components.compositionRegistrations
                @ Components.frameRegistrations
                @ Components.applicationNavigationRegistrations
                @ Components.applicationWorkflowRegistrations
                @ Components.applicationResourceRegistrations
                @ Components.integrationExampleRegistrations
            let renderedComponents = componentRegistrations |> List.map render
            let renderedGuides = Components.guideRegistrations |> List.map render
            let allHtml = String.concat Environment.NewLine (overview :: installation :: renderedComponents @ renderedGuides)

            Expect.equal Components.allRegistrations.Length 62 "overview, installation, fifty-four galleries, and six guides"
            Expect.isFalse (allHtml.Contains("href=\"/components/chart\"")) "removed Chart is absent from the catalog and navigation"
            Expect.isFalse (allHtml.Contains("href=\"/components/layouts\"")) "redundant layout guide is absent from navigation and search"
            Expect.stringContains overview "Accessible, server-rendered Tailwind components" "consumer-facing introduction"
            Expect.stringContains overview "Explore the library" "overview is a five-area catalog"
            let index path = Catalog.tryPage path |> Option.get |> FSharp.ViewEngine.Components.Documentation.DocsView.content |> Render.toString
            Expect.stringContains (index "/components/primitives") "href=\"/components/button\"" "Primitives index deep-links Button"
            Expect.stringContains (index "/components/primitives") "href=\"/components/browser\"" "Primitives index deep-links Browser"
            Expect.stringContains (index "/components/primitives") "href=\"/components/phone\"" "Primitives index deep-links Phone"
            Expect.stringContains (index "/components/application") "href=\"/components/app-shell\"" "Application index deep-links App shell"
            Expect.stringContains (index "/components/application") "href=\"/components/calendar\"" "Application index deep-links Calendar"
            Expect.stringContains (index "/components/application") "href=\"/components/media-library\"" "Application index deep-links Media library"
            Expect.stringContains (index "/components/application") "href=\"/components/integrations/graph-and-trace\"" "Application index deep-links bounded integration recipes"
            Expect.stringContains (index "/components/application") "connects the financial workspace, collections, matching record details, forms, actions, reports and settings" "Application index describes the delivered connected journey"
            Expect.sequenceEqual (Components.examplesFor "app-shell" |> List.map _.title) [ "Sidebar application" ] "App shell owns only shell examples"
            Expect.stringContains overview "Required inputs stay visible" "required input policy"
            Expect.stringContains overview "Optional behavior is piped" "configuration policy"
            Expect.stringContains overview "Custom content stays HTML" "slot policy"
            Expect.stringContains overview "Closed choices are typed" "typed variant policy"
            Expect.stringContains installation "dotnet add package FSharp.ViewEngine.Components" "package installation"
            Expect.stringContains installation "contentFiles/any/any" "packaged Tailwind manifest location"

            for registration, html in List.zip componentRegistrations renderedComponents do
                Expect.stringContains html "docs-gallery-layout" $"{registration.path} uses the gallery layout"
                for rejected in [ "Example setup"; "Imports and supporting code"; "id=\"usage\""; "class=\"docs-toc" ] do
                    Expect.isFalse (html.Contains rejected) $"{registration.path} omits article-only {rejected}"
                Expect.stringContains html "data-docs-example=\"true\"" $"{registration.path} has an executable example"
                Expect.stringContains html "Example: &#39;preview&#39;" $"{registration.path} shows its preview by default"
                let examples = html.Split([| "data-docs-example=\"true\"" |], System.StringSplitOptions.None).Length - 1
                let expectedExamples = Components.examplesFor (registration.path.Substring("/components/".Length)) |> List.length
                Expect.equal examples expectedExamples $"{registration.path} has its focused component examples"
                let ids = Regex.Matches(html, "\\sid=\"([^\"]+)\"") |> Seq.map (fun m -> m.Groups[1].Value) |> Seq.toList
                Expect.equal (List.distinct ids |> List.length) ids.Length $"{registration.path} has no duplicate example or control IDs"

            for registration, html in List.zip Components.guideRegistrations renderedGuides do
                Expect.stringContains html "docs-article-layout" $"{registration.path} uses the article layout"
                Expect.isFalse (html.Contains("data-docs-example=\"true\"")) $"{registration.path} is focused guidance rather than a duplicate gallery"

            Expect.stringContains allHtml "Interaction and server state" "interaction guide"
            Expect.stringContains allHtml "aria-activedescendant identifies the visually active option" "APG focus relationship"
            Expect.stringContains allHtml "cycles options when the same character is repeated" "Select typeahead behavior"
            Expect.stringContains allHtml "Refresh actions fetches new menu content" "DropdownMenu has only its non-obvious behavior note"
            Expect.stringContains allHtml "/components/menus/actions" "DropdownMenu example uses a real Docs-owned patch endpoint"
            Expect.stringContains allHtml "Theming and density" "theme guide"
            Expect.stringContains allHtml "Tailwind CSS setup" "Tailwind setup guide"
            Expect.stringContains allHtml "Application boundaries" "application boundary guidance"
            Expect.stringContains allHtml "explicit Tailwind v4 source manifest" "Tailwind source manifest"
            Expect.stringContains allHtml "versions independently" "version policy"
            Expect.stringContains allHtml "minimum compatible FSharp.ViewEngine" "Core compatibility policy"
            Expect.isFalse (allHtml.Contains("Pre-release contract")) "release-process framing is absent"
            Expect.isFalse (allHtml.Contains("Compiled Call Sites")) "examples are not framed as implementation evidence"
            Expect.isFalse (allHtml.Contains("package-spine task")) "internal task language is absent"
            Expect.isFalse (allHtml.Contains("veSelect {")) "Components does not introduce a component CE"
            Expect.isFalse (allHtml.Contains("color &quot;emerald-600&quot;")) "ordinary API does not accept raw palette strings"

            for source in [
                "Button.create &quot;Create account&quot;"
                "Button.pending"
                "IconButton.create &quot;Add account&quot; plusIcon"
                "Badge.create &quot;Internal&quot;"
                "Status.create &quot;Needs review&quot;"
                "LoadingIndicator.create &quot;Loading account balances&quot;"
                "EmptyState.create &quot;No accounts yet&quot;"
                "Table.create &quot;Team members&quot;"
                "Table.asRowHeader"
                "Table.withMobileLayout"
                "TableSelection.create"
                "DescriptionList.create"
                "DetailField.status &quot;Status&quot;"
                "Metric.text &quot;Available balance&quot; &quot;$42,800&quot;"
                "Pagination.create &quot;Accounts pages&quot;"
                "PaginationItem.current page"
                "Select.create &quot;status&quot; &quot;Status&quot; statusValue selectStatusOptions"
                "Select.create &quot;account&quot; &quot;Parent account&quot; string"
                "Select.withSearch (SelectSearch.Remote &quot;/components/accounts/search&quot;)"
                "Select.renderOptions"
                "Checkbox.create &quot;includeArchived&quot; &quot;Include archived accounts&quot;"
                "Switch.create &quot;postingNotifications&quot; &quot;Posting notifications&quot;"
                "ToggleButton.create &quot;components-compact-rows&quot; &quot;Compact rows&quot;"
                "Breadcrumbs.render resolve"
                "SideNav.render shellDestinationUrl"
                "PageTopBar.withContent"
                "PageHeader.create &quot;Account 2048&quot;"
                "ActionCluster.create"
                "SectionHeader.create &quot;Upcoming transactions&quot;"
                "SectionHeader.withDescription"
                "Page.withBodyLayout PageBodyLayout.Canvas"
                "Tabs.create &quot;components-example-format&quot; &quot;Example format&quot;"
                "Tabs.withVariant TabsVariant.Underlined"
                "Tab.create &quot;activity&quot; &quot;Activity&quot; activity"
                "RadioGroup.create &quot;postingMode&quot; &quot;Posting mode&quot; id"
                "DropdownMenu.create &quot;components-menu-actions&quot; &quot;Actions&quot;"
                "Dialog.create &quot;review-account-dialog&quot; &quot;Review account&quot;"
                "Dialog.withInitialFocus &quot;review-account-dialog-close&quot;"
                "Dialog.trigger &quot;Review account&quot;"
                "Dialog.closeButton &quot;Close&quot;"
                "ConfirmationDialog.create"
                "ConfirmationDialog.renderContent"
                "ConfirmationDialog.pending"
                "Drawer.create &quot;account-settings-drawer&quot; &quot;Account settings&quot;"
                "Drawer.withSide DrawerSide.Start"
                "Collection.create &quot;Accounts&quot; table"
                "Detail.create &quot;Operating checking&quot;"
                "AppShell.create &quot;ledger-app-shell&quot;"
                "AppShell.create &quot;treasury-app-shell&quot;" ] do
                Expect.stringContains allHtml source source

            Expect.stringContains allHtml "data-signals=\"{_account_open: false, account_query:" "remote Combobox emits local open state and an intentionally submitted query"
            Expect.stringContains allHtml "role=\"switch\"" "Switch preserves switch semantics"
            Expect.stringContains allHtml "aria-pressed=\"true\"" "ToggleButton preserves pressed semantics"
            Expect.stringContains allHtml "type=\"radio\"" "RadioGroup preserves form semantics internally"
            Expect.stringContains allHtml "Select.required" "Select example exposes required state"
            Expect.stringContains allHtml "Select.pending" "Select example exposes pending state"
            Expect.stringContains allHtml "Checkbox.required" "Checkbox example exposes native required state"
            Expect.stringContains allHtml "Checkbox.withValidation" "Checkbox example exposes server validation"
            Expect.stringContains allHtml "Switch.pending" "Switch example exposes pending state"
            Expect.stringContains allHtml "ToggleButton.pending" "ToggleButton example exposes pending state"
            Expect.stringContains allHtml "RadioGroup.required" "RadioGroup example exposes grouped required state"
            Expect.stringContains allHtml "RadioGroup.withValidation" "RadioGroup example exposes server validation"
            for api in [ "Select.loading"; "Select.withError"; "Select.disabled"; "Select.pending" ] do
                Expect.stringContains allHtml api $"Combobox example exposes {api}"
            Expect.stringContains allHtml "requestCancellation: &#39;auto&#39;" "remote Combobox documents deterministic newest-request behavior"
            for endpoint in [ "/components/choices/select"; "/components/choices/checkbox"; "/components/choices/switch"; "/components/choices/radio" ] do
                Expect.stringContains allHtml endpoint $"focused example posts to real Docs endpoint {endpoint}"
            Expect.isFalse (allHtml.Contains("NativeSelect.create")) "the package exposes no NativeSelect API"
            Expect.stringContains allHtml "id=\"review-account-dialog-trigger\"" "Dialog renders its connected trigger"
            Expect.stringContains allHtml "data-on:close=\"document.getElementById(&quot;review-account-dialog-trigger&quot;)?.focus()\"" "Dialog close restores trigger focus"
            Expect.stringContains allHtml "role=\"alertdialog\"" "ConfirmationDialog preserves urgent confirmation semantics"
            Expect.stringContains allHtml "data-indicator:_delete_account_confirmation_pending" "ConfirmationDialog owns immediate duplicate-submit protection"
            Expect.stringContains allHtml "id=\"account-settings-drawer\"" "Drawer renders a stable native dialog"
            Expect.stringContains allHtml "aria-label=\"Account settings\"" "Drawer preserves consumer-owned navigation landmarks"
            Expect.stringContains allHtml "/components/dialogs/confirm" "ConfirmationDialog uses a real Docs-owned endpoint"
            Expect.stringContains allHtml "/components/drawers/account" "Drawer uses a real Docs-owned patch endpoint"
            Expect.stringContains allHtml "/components/tabs/review" "Tabs uses a real Docs-owned patch endpoint"
            Expect.isFalse (allHtml.Contains("Select.describe")) "Select has no unobservable option-description modifier"
            Expect.stringContains allHtml "data-signals=\"{_components_menu_actions_open: false, _components_menu_actions_typeahead:" "menu IDs become valid isolated interaction signal tokens"
            Expect.isFalse (allHtml.Contains("_components-menu-actions-open")) "DOM IDs are not copied unsafely into expressions"
            Expect.stringContains allHtml "aria-current=\"page\"" "typed navigation retains the current destination"
            Expect.stringContains allHtml "let ledgerShellExample" "Ledger shell construction is visible"
            Expect.stringContains allHtml "let treasuryShellExample" "Treasury shell construction is visible"
            Expect.stringContains (index "/components/application") "one main landmark" "Application index explains AppShell ownership"
            Expect.stringContains allHtml "id=\"components-app-shell-fixture\"" "AppShell example exposes one stable destination morph region"
            Expect.stringContains allHtml "/components/app-shell/fixture?" "AppShell destination links request only the stable fixture region"
            Expect.stringContains allHtml "window.fsharpDocsNavigation.navigate(evt" "standalone composition fixtures use Docs-owned Datastar document navigation"
            Expect.stringContains allHtml "--fve-brand-solid" "consumer theme overrides are documented"
            Expect.stringContains overview "rel=\"prev\" href=\"/extensions/tailwind-elements\"" "Components follows integrations"
            Expect.stringContains overview "rel=\"next\" href=\"/components/installation\"" "overview continues to installation"
            let versioning = render Components.versioningRegistration
            Expect.stringContains versioning "rel=\"prev\" href=\"/components/customization\"" "last guide follows customization"
            Expect.stringContains versioning "rel=\"next\" href=\"/components/primitives\"" "shared guides precede the five catalog families"
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
            Expect.stringContains breadcrumbsHtml "hidden sm:flex" "the complete path remains available on wider screens"

            let icon = span { "L" }
            let grouped =
                SideNav.create
                    "product-navigation"
                    "Product navigation"
                    (SideNavHeader.create "Ledger" |> SideNavHeader.withContent (strong { icon; " Ledger" }))
                    [ SideNavSection.group "Manage" [
                          SideNavItem.create Home "Dashboard" |> SideNavItem.withLeading icon
                          SideNavItem.create Accounts "Accounts"
                          SideNavItem.unavailable "Unavailable" ]
                      SideNavSection.group "Analyze" [ SideNavItem.create Reports "Reports" ]
                      SideNavSection.group "Configure" [ SideNavItem.create Settings "Settings" ] ]
                |> SideNav.withCurrent Accounts
                |> SideNav.withWidth SideNavWidth.Standard
                |> SideNav.withContext (p { "Meier Made" })
                |> SideNav.withMobileContext (p { "Meier Made mobile" })
                |> SideNav.withFooter (a { _href "/account"; "Andrew Meier" })
            let groupedHtml = grouped |> SideNav.render shellTestUrl |> Render.toString
            Expect.stringContains groupedHtml "aria-label=\"Product navigation\"" "SideNav has its consumer label"
            Expect.stringContains groupedHtml "aria-label=\"Manage\"" "group hierarchy is semantic"
            Expect.stringContains groupedHtml "aria-current=\"page\"" "typed current destination is exposed"
            Expect.stringContains groupedHtml "aria-disabled=\"true\"" "unavailable navigation is semantic and non-interactive"
            Expect.stringContains groupedHtml "Meier Made" "optional context renders"
            Expect.stringContains groupedHtml "Andrew Meier" "optional footer renders"
            Expect.equal (Regex.Matches(groupedHtml, "aria-current=\"page\"").Count) 1 "SideNav exposes one current destination"

            let ungrouped =
                SideNav.create "compact-navigation" "Compact navigation" (SideNavHeader.create "Treasury") [
                    SideNavSection.ungrouped [
                        SideNavItem.create Home "Overview"
                        SideNavItem.create Reports "Transactions" ] ]
            let ungroupedHtml = ungrouped |> SideNav.render shellTestUrl |> Render.toString
            Expect.stringContains ungroupedHtml "aria-label=\"Compact navigation\"" "ungrouped navigation retains its name"
            Expect.isFalse (ungroupedHtml.Contains("uppercase tracking-wide")) "ungrouped navigation adds no invented group heading"
            Expect.isFalse (ungroupedHtml.Contains("aria-current=\"page\"")) "SideNav permits routes without a selected primary destination"

            let pageActions =
                ActionCluster.create "account-page-actions" [
                    ApplicationAction.command "$refreshes++" "Refresh"
                    ApplicationAction.link Reports "View reports" ]
                |> ActionCluster.withOverflow [ MenuItem.link Settings "Settings" ]
            let pageHeader =
                PageHeader.create "Account 2048"
                |> PageHeader.withSubtitle "Operating checking"
                |> PageHeader.withActions pageActions
            let pageHeaderHtml = pageHeader |> PageHeader.render shellTestUrl |> Render.toString
            Expect.equal (Regex.Matches(pageHeaderHtml, "<h1").Count) 1 "PageHeader emits exactly one h1"
            Expect.stringContains pageHeaderHtml ">Account 2048</h1>" "the required title is visible"
            Expect.isFalse (pageHeaderHtml.Contains("<h1 class=\"sr-only\"")) "PageHeader does not hide the page title"
            Expect.stringContains pageHeaderHtml "Operating checking" "PageHeader renders its optional subtitle"
            Expect.stringContains pageHeaderHtml "data-on:click=\"$refreshes++\"" "page commands remain buttons with trusted Datastar actions"
            Expect.stringContains pageHeaderHtml "href=\"/reports\"" "page destinations remain truthful links"
            Expect.stringContains pageHeaderHtml "aria-label=\"More actions\"" "additional page actions use accessible overflow"

            let destructiveOverflowHtml =
                ActionCluster.create "ordered-actions" []
                |> ActionCluster.withOverflow [ MenuItem.destructiveAction "$delete()" "Delete"; MenuItem.link Settings "Settings" ]
                |> ActionCluster.render shellTestUrl
                |> Render.toString
            let settingsIndex = destructiveOverflowHtml.IndexOf("Settings", StringComparison.Ordinal)
            let separatorIndex = destructiveOverflowHtml.IndexOf("role=\"separator\"", StringComparison.Ordinal)
            let deleteIndex = destructiveOverflowHtml.IndexOf("Delete", StringComparison.Ordinal)
            Expect.isTrue (settingsIndex < separatorIndex && separatorIndex < deleteIndex) "destructive overflow actions render last and separated"

            let sectionHtml =
                SectionHeader.create "Activity"
                |> SectionHeader.withDescription "Recent account activity."
                |> SectionHeader.withActions (ActionCluster.create "activity-actions" [ ApplicationAction.link Reports "View reports" ])
                |> fun sectionHeader -> Section.create sectionHeader (p { "No activity." })
                |> Section.render shellTestUrl
                |> Render.toString
            Expect.stringContains sectionHtml "<h2 class=" "SectionHeader defaults to an h2"
            Expect.stringContains sectionHtml "Recent account activity." "SectionHeader renders supporting text"
            Expect.isFalse (sectionHtml.Contains("<h1")) "detail sections do not compete with page identity"

            let collectionHtml =
                Collection.create "Accounts" (div { "Account rows" })
                |> Collection.withVisuallyHiddenTitle
                |> Collection.render shellTestUrl
                |> Render.toString
            Expect.stringContains collectionHtml "<h2 class=\"sr-only\">Accounts</h2>" "Collection stays labelled beneath a page-owned visible title"
            Expect.isFalse (collectionHtml.Contains("rounded-[var(--fve-radius-panel)]")) "Collection does not invent a card surface"
            Expect.isFalse (collectionHtml.Contains("px-4")) "Collection inherits the page gutter without double padding"
            Expect.stringContains collectionHtml "<header class=\"sr-only\">" "Hidden collection titles do not leave a grid gap"

            let detailHtml =
                Detail.create "Operating checking" [ section { _ariaLabel "Account details"; "Account fields" } ]
                |> Detail.withVisuallyHiddenTitle
                |> Detail.render shellTestUrl
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
                    Tab.create "summary" "Summary" (p { "Summary panel" })
                    Tab.create "activity" "Activity" (p { "Activity panel" }) ]
                |> Tabs.withVariant TabsVariant.Underlined
                |> Tabs.render
            let fullBleedPageHtml =
                Page.create pageHeader empty
                |> Page.withTopBar topBar
                |> Page.withTabs pageTabs
                |> Page.withWidth PageWidth.Full
                |> Page.withBodyLayout PageBodyLayout.FullBleed
                |> Page.render shellTestUrl
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
                |> Page.render shellTestUrl
                |> Render.toString
            Expect.stringContains defaultPageHtml "max-w-7xl" "Page uses a constrained application column by default"

            let readingPageHtml =
                Page.create pageHeader (p { "Reading content" })
                |> Page.withSectionNavigation (nav { _ariaLabel "Article sections"; a { _href "#summary"; "Summary" } })
                |> Page.withWidth PageWidth.Reading
                |> Page.render shellTestUrl
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
            Expect.throws (fun () -> SideNavSection.ungrouped [] |> ignore) "SideNav rejects an empty ungrouped section"
            Expect.throws (fun () -> SideNavSection.group "Manage" [] |> ignore) "SideNav rejects an empty group"
            Expect.throws (fun () -> SideNavHeader.create " " |> ignore) "SideNavHeader requires an accessible identity"
            Expect.throws (fun () -> SideNav.create " " "Navigation" (SideNavHeader.create "Product") [ SideNavSection.ungrouped [ SideNavItem.create Home "Home" ] ] |> ignore) "SideNav requires a stable ID"
            Expect.throws (fun () -> SideNav.create "nav" " " (SideNavHeader.create "Product") [ SideNavSection.ungrouped [ SideNavItem.create Home "Home" ] ] |> ignore) "SideNav requires an accessible label"
            Expect.throws (fun () -> ungrouped |> SideNav.withCurrent Settings |> ignore) "SideNav rejects an unrepresented current destination"
            Expect.throws (fun () -> SideNav.create "nav" "Navigation" (SideNavHeader.create "Product") [ SideNavSection.ungrouped [ SideNavItem.create Home "Home"; SideNavItem.create Home "Duplicate" ] ] |> ignore) "SideNav rejects duplicate destinations"
            Expect.throws (fun () -> PageHeader.create " " |> ignore) "PageHeader requires a route title"
            Expect.throws (fun () -> PageHeader.create "Title" |> PageHeader.withSubtitle " " |> ignore) "PageHeader rejects an empty subtitle"
            Expect.throws (fun () -> ActionCluster.create "too-many" [ ApplicationAction.link Home "One"; ApplicationAction.link Accounts "Two"; ApplicationAction.link Reports "Three" ] |> ignore) "ActionCluster limits direct actions"
            Expect.throws (fun () -> ActionCluster.create "too-primary" [ ApplicationAction.link Home "One" |> ApplicationAction.withVariant ButtonVariant.Primary; ApplicationAction.link Accounts "Two" |> ApplicationAction.withVariant ButtonVariant.Primary ] |> ignore) "ActionCluster permits at most one primary action"
            Expect.throws (fun () -> SectionHeader.create "Activity" |> SectionHeader.withDescription " " |> ignore) "SectionHeader rejects empty supporting text"
            Expect.throws (fun () -> BottomNavigation.create " " "Quick navigation" [ BottomNavigationItem.create Home "Home" ] |> ignore) "BottomNavigation requires a stable ID"
            Expect.throws (fun () -> BottomNavigation.create "quick-navigation" " " [ BottomNavigationItem.create Home "Home" ] |> ignore) "BottomNavigation requires an accessible label"
            Expect.throws (fun () -> BottomNavigation.create "quick-navigation" "Quick navigation" [] |> ignore) "BottomNavigation rejects an empty item list"
            Expect.throws (fun () -> BottomNavigation.create "quick-navigation" "Quick navigation" [ BottomNavigationItem.create Home "Home"; BottomNavigationItem.create Home "Home again" ] |> ignore) "BottomNavigation rejects duplicate destinations"
            Expect.throws (fun () -> AppShell.create " " grouped empty |> ignore) "AppShell requires a stable ID"
            Expect.throws (fun () -> AppShell.create "same-id" (SideNav.create "same-id" "Navigation" (SideNavHeader.create "Product") [ SideNavSection.ungrouped [ SideNavItem.create Home "Home" ] ]) empty |> ignore) "AppShell and SideNav IDs must differ"
            Expect.throws (fun () -> AppShell.create "product-shell" grouped empty |> AppShell.withMobileBottomNavigation "product-shell" "Quick navigation" [ BottomNavigationItem.create Home "Home" ] |> ignore) "AppShell requires distinct bottom-navigation IDs"
            Expect.throws (fun () -> AppShell.create "product-shell" grouped empty |> AppShell.withMobileBottomNavigation "quick-navigation" "Quick navigation" [ BottomNavigationItem.create Reports "Reports" ] |> ignore) "AppShell rejects mobile destinations missing from SideNav"
        }

        test "Tabs render typed variants, collision-safe relationships, and isolated automatic activation" {
            let first =
                Tabs.create "account-tabs" "Account sections" [
                    Tab.create "overview" "Overview" (p { "Summary" })
                    Tab.create "tax.reserve" "Tax reserve" (a { _href "/tax"; "Tax settings" }) ]
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
            Expect.stringContains first "aria-selected=\"true\" tabindex=\"0\"" "selected tab owns the composite tab stop"
            Expect.stringContains first "hidden data-attr:hidden" "inactive panels leave interaction and accessibility trees"
            Expect.stringContains first "evt.key == &#39;ArrowLeft&#39;" "Left Arrow is handled"
            Expect.stringContains first "evt.key == &#39;ArrowRight&#39;" "Right Arrow is handled"
            Expect.stringContains first "evt.key == &#39;Home&#39;" "Home is handled"
            Expect.stringContains first "evt.key == &#39;End&#39;" "End is handled"
            Expect.stringContains first "aria-selected:border-[var(--fve-brand-solid)]" "Underlined variant uses semantic selected treatment"

            let adjacent =
                div {
                    Tabs.create "first-tabs" "First views" [ Tab.create "same" "Same" (p { "First" }) ] |> Tabs.render
                    Tabs.create "second-tabs" "Second views" [ Tab.create "same" "Same" (p { "Second" }) ] |> Tabs.render
                }
                |> Render.toString
            let ids = Regex.Matches(adjacent, " id=\"([^\"]+)\"") |> Seq.cast<Match> |> Seq.map (fun matched -> matched.Groups[1].Value) |> Seq.toList
            Expect.equal ids.Length (ids |> List.distinct |> List.length) "adjacent instances produce no duplicate IDs"
            Expect.stringContains adjacent "_tabs_v66697273742d74616273_selected" "first signal is collision-safe and local"
            Expect.stringContains adjacent "_tabs_v7365636f6e642d74616273_selected" "second signal is collision-safe and local"

            let duplicateItems = [ Tab.create "same" "One" (p { "One" }); Tab.create "same" "Two" (p { "Two" }) ]
            Expect.throws (fun () -> Tabs.create "tabs" "Views" [] |> ignore) "Tabs reject empty item sets"
            Expect.throws (fun () -> Tabs.create "tabs" "Views" duplicateItems |> ignore) "Tabs reject duplicate item IDs"
            Expect.throws (fun () -> Tabs.create "tabs" "Views" [ Tab.create "one" "One" (p { "One" }) ] |> Tabs.withSelected "missing" |> ignore) "Tabs reject unknown selected items"
            Expect.throws (fun () -> Tab.create " " "One" (p { "One" }) |> ignore) "Tab rejects whitespace IDs"
            Expect.throws (fun () -> Tabs.create "tabs" " " [ Tab.create "one" "One" (p { "One" }) ] |> ignore) "Tabs require an accessible group label"
        }

        test "Dialog overlays preserve native modal semantics, safe confirmation state, and responsive drawer identity" {
            let dialogConfig =
                Dialog.create "test-dialog" "Review account" (p { "Review the settings." })
                |> Dialog.withDescription "Settings remain unchanged until saved."
                |> Dialog.withInitialFocus "test-dialog-close"
                |> Dialog.dismissOnBackdrop
            let dialogHtml =
                div { dialogConfig |> Dialog.trigger "Review account"; dialogConfig |> Dialog.withFooter (dialogConfig |> Dialog.closeButton "Close") |> Dialog.render }
                |> Render.toString
            Expect.stringContains dialogHtml "<dialog id=\"test-dialog\"" "Dialog remains a native dialog"
            Expect.stringContains dialogHtml "aria-modal=\"true\"" "Dialog exposes modal semantics"
            Expect.stringContains dialogHtml "evt.target == evt.currentTarget" "Dialog can opt into backdrop dismissal"
            Expect.stringContains dialogHtml "document.getElementById(&quot;test-dialog-trigger&quot;)?.focus()" "Dialog restores its connected trigger"

            let confirmation =
                ConfirmationDialog.create "delete-value" "Delete value?" "This cannot be undone." "Keep value" "Delete value" "@post('/values/delete')"
            let confirmationHtml =
                div { confirmation |> ConfirmationDialog.trigger "Delete value"; confirmation |> ConfirmationDialog.render }
                |> Render.toString
            Expect.stringContains confirmationHtml "role=\"alertdialog\"" "destructive confirmation has alert-dialog semantics"
            Expect.stringContains confirmationHtml "aria-describedby=\"delete-value-message delete-value-validation\"" "message and validation remain described"
            Expect.stringContains confirmationHtml "data-signals=\"{_delete_value_pending: false}\"" "confirmation pending signal is instance-local"
            Expect.stringContains confirmationHtml "data-indicator:_delete_value_pending" "form request drives pending state"
            Expect.stringContains confirmationHtml "data-attr:disabled=\"$_delete_value_pending\"" "request pending prevents repeated activation"
            Expect.stringContains confirmationHtml "id=\"delete-value-cancel\"" "least-destructive action has a stable initial-focus target"
            Expect.stringContains confirmationHtml "id=\"delete-value-confirm\"" "destructive submit has stable identity"
            Expect.stringContains confirmationHtml "bg-[var(--fve-critical-solid)]" "confirmation is visually destructive"

            let pendingHtml = confirmation |> ConfirmationDialog.pending |> ConfirmationDialog.render |> Render.toString
            Expect.stringContains pendingHtml "aria-busy=\"true\"" "server-rendered pending state is perceivable"
            Expect.stringContains pendingHtml "Confirmation in progress." "pending state retains explanatory text"
            Expect.stringContains pendingHtml "disabled" "pending confirmation cannot submit twice"

            let validationHtml =
                confirmation
                |> ConfirmationDialog.withValidation "The value is still referenced."
                |> ConfirmationDialog.renderContent
                |> Render.toString
            Expect.stringContains validationHtml "role=\"alert\"" "server validation is announced"
            Expect.stringContains validationHtml "The value is still referenced." "server validation remains visible"

            let drawerBody = nav { _ariaLabel "Account settings"; a { _href "/accounts"; "Accounts" } }
            let endDrawer = Drawer.create "settings-drawer" "Settings" drawerBody
            let endDrawerHtml =
                div { endDrawer |> Drawer.trigger "Open settings"; endDrawer |> Drawer.render }
                |> Render.toString
            Expect.stringContains endDrawerHtml "<dialog id=\"settings-drawer\"" "Drawer remains a native dialog"
            Expect.stringContains endDrawerHtml "right-0 ml-auto mr-0 border-l" "Drawer defaults to the end edge"
            Expect.stringContains endDrawerHtml "w-[min(24rem,calc(100%-3rem))]" "Drawer reserves narrow viewport space"
            Expect.stringContains endDrawerHtml "aria-label=\"Account settings\"" "consumer landmarks are preserved"
            Expect.stringContains endDrawerHtml "id=\"settings-drawer-close\"" "Drawer owns a stable close target"
            Expect.stringContains endDrawerHtml "evt.target == evt.currentTarget" "Drawer backdrop dismisses explicitly"
            let startDrawerHtml = endDrawer |> Drawer.withSide DrawerSide.Start |> Drawer.render |> Render.toString
            Expect.stringContains startDrawerHtml "left-0 ml-0 mr-auto border-r" "Drawer supports the typed start edge"
        }

        test "Components foundations preserve accessible names, honest states, and protected structure" {
            let icon = span { "+" }

            let pendingButton =
                Button.create "Sync accounts"
                |> Button.pending
                |> Button.withAttributes [ _ariaBusy false; _attr "disabled"; _class "override" ]
                |> Button.render
                |> Render.toString
            Expect.stringContains pendingButton "disabled" "pending Button prevents activation"
            Expect.stringContains pendingButton "aria-busy=\"true\"" "pending Button exposes busy state"
            Expect.stringContains pendingButton ">Sync accounts<" "pending Button retains its action label"
            Expect.equal (Regex.Matches(pendingButton, "aria-busy=").Count) 1 "Button owns one busy state"
            Expect.isFalse (pendingButton.Contains("override")) "Button protects base presentation"

            let iconButton =
                IconButton.create "Add account" icon
                |> IconButton.withVariant ButtonVariant.Primary
                |> IconButton.render
                |> Render.toString
            Expect.stringContains iconButton "aria-label=\"Add account\"" "IconButton requires an accessible name"
            Expect.stringContains iconButton "aria-hidden=\"true\"" "IconButton hides decorative icon content"
            Expect.throws (fun () -> IconButton.create " " icon |> ignore) "IconButton rejects an empty accessible name"

            let pendingIconButton =
                IconButton.create "Refresh accounts" icon
                |> IconButton.pending
                |> IconButton.withAttributes [ _ariaLabel "Override"; _ariaBusy false; _class "override" ]
                |> IconButton.render
                |> Render.toString
            Expect.stringContains pendingIconButton "aria-label=\"Refresh accounts\"" "pending IconButton retains its accessible name"
            Expect.stringContains pendingIconButton "aria-busy=\"true\"" "pending IconButton exposes busy state"
            Expect.stringContains pendingIconButton "disabled" "pending IconButton prevents activation"
            Expect.isFalse (pendingIconButton.Contains("Override")) "IconButton protects its accessible name"
            Expect.isFalse (pendingIconButton.Contains("override")) "IconButton protects base presentation"

            for variant, activeClass in [
                ButtonVariant.Primary, "active:bg-[var(--fve-brand-active)]"
                ButtonVariant.Secondary, "active:bg-[var(--fve-surface-active)]"
                ButtonVariant.Ghost, "active:bg-[var(--fve-surface-active)]"
                ButtonVariant.Destructive, "active:bg-[var(--fve-critical-active)]"
            ] do
                let button =
                    Button.create "Action"
                    |> Button.withVariant variant
                    |> Button.render
                    |> Render.toString
                let iconAction =
                    IconButton.create "Icon action" icon
                    |> IconButton.withVariant variant
                    |> IconButton.render
                    |> Render.toString
                Expect.stringContains button activeClass $"{variant} Button has intentional active styling"
                Expect.stringContains iconAction activeClass $"{variant} IconButton has intentional active styling"

            let badge =
                Badge.create "Reconciled"
                |> Badge.withTone Tone.Positive
                |> Badge.withAttributes [ _class "override" ]
                |> Badge.render
                |> Render.toString
            Expect.stringContains badge "Reconciled" "Badge communicates category through text"
            Expect.stringContains badge "var(--fve-positive-text)" "Badge consumes a semantic tone"
            Expect.isFalse (badge.Contains("override")) "Badge protects base presentation"

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
                |> EmptyState.withActions (Button.primary "Create account")
                |> EmptyState.withAttributes [ _class "override" ]
                |> EmptyState.render
                |> Render.toString
            Expect.stringContains emptyState "No accounts" "EmptyState renders its title"
            Expect.stringContains emptyState "Create an account to begin." "EmptyState renders useful guidance"
            Expect.stringContains emptyState "aria-hidden=\"true\"" "EmptyState icon is decorative"
            Expect.stringContains emptyState "Create account" "EmptyState composes application-owned actions"
            Expect.isFalse (emptyState.Contains("override")) "EmptyState protects base presentation"
        }

        test "Table records, selection, canvas, and optional section headers preserve their contracts" {
            let columns = [ Table.column "Name" text |> Table.asRowHeader |> Table.asMobilePrimary ]
            let selection =
                TableSelection.create "selection" id id
                |> TableSelection.withSelectedKeys [ "one"; "off-page"; "disabled" ]
                |> TableSelection.withDisabledRows ((=) "disabled")
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
            Expect.stringContains html "name=\"keys\" value=\"one\" checked" "eligible initial keys submit real values"
            Expect.stringContains html "el.indeterminate" "select-all supports mixed state"
            Expect.stringContains html "fve-table-selection-change" "applications receive selected key changes"
            Expect.stringContains html "data-signals__ifmissing" "morphs preserve local selection"
            let sortedHtml =
                Table.create "Sortable records" [
                    Table.column "Name" text
                    |> Table.asRowHeader
                    |> Table.asMobilePrimary
                    |> Table.withSort (TableSort.ascending "/records?sort=name&direction=desc")
                    Table.column "Role" text
                    |> Table.withSort (TableSort.by "/records?sort=role&direction=asc")
                ] [ "Alex" ]
                |> Table.withMobileLayout TableMobileLayout.Records
                |> Table.render
                |> Render.toString
            Expect.stringContains sortedHtml "aria-sort=\"ascending\"" "only the current column carries aria-sort"
            Expect.stringContains sortedHtml "href=\"/records?sort=name&amp;direction=desc\"" "the consumer-provided next destination is preserved"
            Expect.stringContains sortedHtml "fve-table-mobile-sort" "record layouts include sort controls outside visually hidden headers"
            Expect.stringContains html "1 selected on this page" "off-page and disabled initial keys are excluded"
            Expect.throws (fun () -> render [ "one"; "one" ] |> ignore) "duplicate keys are rejected"
            Expect.throws (fun () -> render [ "" ] |> ignore) "empty keys are rejected"
            Expect.throws (fun () -> Table.create "Invalid" [ Table.column "Name" text ] [ "one" ] |> Table.withMobileLayout TableMobileLayout.Records |> Table.render |> ignore) "records require a primary column"
            let empty = render []
            Expect.stringContains empty "No records" "empty data retains the empty state"
            Expect.stringContains empty "0 selected on this page" "empty data clears selection"
            let sectionHtml = Section.withoutHeader "Details" (p { "Content" }) |> Section.render id |> Render.toString
            Expect.stringContains sectionHtml "aria-label=\"Details\"" "unheaded sections retain accessible identity"
            Expect.isFalse (sectionHtml.Contains("<header")) "section header is optional"
            let headerHtml = SectionHeader.create "Details" |> SectionHeader.render id |> Render.toString
            Expect.isFalse (headerHtml.Contains("border-b")) "section dividers are opt-in"
            let transactionsSection = Section.create (SectionHeader.create "Transactions") (p { "Rows" })
            let labelledSection = transactionsSection |> Section.withLabel "Recent transactions" |> Section.render id |> Render.toString
            Expect.stringContains labelledSection "aria-label=\"Recent transactions\"" "sections can distinguish nested landmarks"
            Expect.stringContains labelledSection ">Transactions</h2>" "accessible labels do not replace visible section headings"
            Expect.throws (fun () -> transactionsSection |> Section.withLabel " " |> ignore) "section labels cannot be blank"
            let canvas = Page.create (PageHeader.create "Graph") (div { "Canvas" }) |> Page.withBodyLayout PageBodyLayout.Canvas |> Page.render id |> Render.toString
            Expect.stringContains canvas "data-fve-page-canvas=\"true\"" "canvas fills available height"
            Expect.isFalse (canvas.Contains("data-fve-page-scroll")) "canvas owns scrolling instead of nesting document scroll"
            let checkbox = Checkbox.create "mixed" "Select all" |> Checkbox.withIndeterminate |> Checkbox.withVisuallyHiddenLabel |> Checkbox.render |> Render.toString
            Expect.stringContains checkbox "mixed_mixed: true" "standalone checkboxes support mixed state"
            Expect.stringContains checkbox "class=\"sr-only\">Select all" "hidden labels remain accessible"
        }

        test "Components data display preserves native semantics and consumer ownership" {
            let defaultTableHtml =
                Table.create "Accounts" [ Table.column "Account" text |> Table.asRowHeader ] [ "Assets" ]
                |> Table.render
                |> Render.toString
            Expect.stringContains defaultTableHtml "<caption class=\"sr-only\">Accounts</caption>" "Table captions are visually hidden by default"
            Expect.stringContains defaultTableHtml "data-density=\"compact\"" "Table uses compact application density by default"
            Expect.isFalse (defaultTableHtml.Contains("rounded-[var(--fve-radius-panel)]")) "Table uses a full-width plain surface by default"
            Expect.stringContains defaultTableHtml "data-surface=\"plain\"" "Plain table backgrounds follow the page"
            Expect.stringContains defaultTableHtml "bg-[var(--fve-table-background)]" "Overflow, rows, and sticky cells share the table background token"
            let panelTableHtml =
                Table.create "Panel balances" [ Table.column "Account" text ] [ "Operating" ]
                |> Table.withSurface TableSurface.Panel
                |> Table.render
                |> Render.toString
            Expect.stringContains panelTableHtml "data-surface=\"panel\"" "Panel surfaces remain an explicit opt-in"

            let tableHtml =
                Table.create "Account balances" [
                    Table.column "Account" text |> Table.asRowHeader
                    Table.column "Balance" (fun value -> text value) |> Table.alignEnd
                    Table.rowActionsColumn (fun value ->
                        RowActions.create "operating-actions" value [ MenuItem.link Accounts "View account" ]
                        |> RowActions.render shellTestUrl)
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
            Expect.throws (fun () -> Table.create " " [ Table.column "Value" text ] [ "one" ] |> ignore) "Table requires a caption"

            let destructiveRowActionsHtml =
                RowActions.create "record-actions" "Operating" [
                    MenuItem.destructiveLink Settings "Delete account"
                    MenuItem.link Accounts "View account" ]
                |> RowActions.render shellTestUrl
                |> Render.toString
            let viewIndex = destructiveRowActionsHtml.IndexOf("View account", StringComparison.Ordinal)
            let separatorIndex = destructiveRowActionsHtml.IndexOf("role=\"separator\"", StringComparison.Ordinal)
            let deleteIndex = destructiveRowActionsHtml.IndexOf("Delete account", StringComparison.Ordinal)
            Expect.isTrue (viewIndex < separatorIndex && separatorIndex < deleteIndex) "destructive row actions render last and separated"
            Expect.stringContains destructiveRowActionsHtml "text-[var(--fve-critical-text)]" "destructive row-action links retain their semantic tone"

            let detailsHtml =
                DescriptionList.create [
                    DetailField.text "Type" "Asset"
                    DetailField.status "State" (Status.positive "Active")
                    |> DetailField.withDescription "Available for posting."
                    |> DetailField.withAttributes [ _role "button"; _class "override" ]
                ]
                |> DescriptionList.withColumns DescriptionListColumns.Three
                |> DescriptionList.withAttributes [ _role "table"; _class "override" ]
                |> DescriptionList.render
                |> Render.toString
            Expect.stringContains detailsHtml "<dl class=\"grid gap-x-6 gap-y-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3\"" "DescriptionList retains responsive native list semantics"
            Expect.stringContains detailsHtml "<dt" "DetailField renders a term"
            Expect.stringContains detailsHtml "<dd" "DetailField renders its value and description"
            Expect.stringContains detailsHtml "Available for posting." "DetailField preserves supporting context"
            Expect.isFalse (detailsHtml.Contains("override")) "description-list structure protects base classes"
            Expect.isFalse (detailsHtml.Contains("role=")) "description-list structure rejects role replacement"
            Expect.throws (fun () -> DetailField.text " " "Value" |> ignore) "DetailField requires a label"
            Expect.throws (fun () -> DescriptionList.create [] |> ignore) "DescriptionList requires fields"
            Expect.stringContains detailsHtml "text-xs font-medium uppercase tracking-wide" "detail labels use the ancillary uppercase role"
            Expect.stringContains detailsHtml "mt-1 break-words text-sm font-normal" "detail values use normal-weight UI text"
            for columns, expected in [
                DescriptionListColumns.One, "grid-cols-1"
                DescriptionListColumns.Two, "grid-cols-1 sm:grid-cols-2"
                DescriptionListColumns.Three, "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
                DescriptionListColumns.Four, "grid-cols-1 sm:grid-cols-2 xl:grid-cols-4" ] do
                let rendered =
                    DescriptionList.create [ DetailField.text "<Label>" "<Value>" ]
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
            Expect.isFalse (paginationPage3Html.Contains("ledger.example.test")) "Docs pagination does not expose externally fake destinations"

            let paginationPage8Html =
                Components.paginationPageFor 99
                |> View.documentWithPage Registry.navigation Components.paginationRegistration
                |> Render.toHtmlDocString
            Expect.stringContains paginationPage8Html "Showing 176–184 of 184 accounts" "Docs pagination clamps out-of-range requests"
            Expect.stringContains paginationPage8Html "aria-current=\"page\" aria-label=\"Page 8, current page\"" "Docs pagination clamps to the last page"

            for removedType in [ "FSharp.ViewEngine.Components.Chart"; "FSharp.ViewEngine.Components.ChartConfig"; "FSharp.ViewEngine.Components.Primitives.Chart"; "FSharp.ViewEngine.Components.Primitives.ChartConfig" ] do
                Expect.isNull (typeof<TableSurface>.Assembly.GetType removedType) "the public chart wrapper API is removed"

        }

        test "DropdownMenu supports icon triggers and reactive radio choices" {
            let items : MenuItem<unit> list =
                [ MenuItem.radio "$theme = 'system'" "System"
                  |> MenuItem.withChecked true
                  |> MenuItem.withCheckedExpression "$theme == 'system'"
                  MenuItem.radio "$theme = 'dark'" "Dark" |> MenuItem.disabled
                  MenuItem.radio "$theme = 'light'" "Light" |> MenuItem.pending ]
            let rendered =
                DropdownMenu.create "theme" "Choose theme" items
                |> DropdownMenu.withIconTrigger (span { "Icon" })
                |> DropdownMenu.render (fun () -> "")
                |> Render.toString
            Expect.stringContains rendered "aria-label=\"Choose theme\"" "An icon trigger keeps its accessible name"
            Expect.stringContains rendered "role=\"menuitemradio\" aria-checked=\"true\"" "Radio choices expose initial selection"
            Expect.stringContains rendered "data-attr:aria-checked" "Checked state follows its trusted expression"
            Expect.stringContains rendered "data-show=\"$theme == &#39;system&#39;\"" "The decorative checkmark follows the same state"
            Expect.stringContains rendered ":is([role=menuitem], [role=menuitemradio]):not([aria-disabled=true])" "Keyboard navigation includes enabled radio choices"
            Expect.stringContains rendered "aria-busy=\"true\"" "Radio choices support pending state"
            Expect.isFalse (rendered.Contains "aria-selected=") "Menu choices do not use Select styling hooks"
            Expect.throws (fun () -> MenuItem.action "noop()" "Action" |> MenuItem.withChecked true |> ignore) "Only radio items accept checked state"
        }

        test "Components DropdownMenu renders complete item vocabulary and interaction semantics" {
            let leading = span { _attr ("data-test-icon", "review"); "✓" }
            let menuItems =
                [ MenuItem.group "Account" [
                      MenuItem.link 1 "Account settings"
                      MenuItem.action "$reviews++" "Record review"
                      |> MenuItem.withLeading leading
                      |> MenuItem.withShortcut "R"
                      MenuItem.link 2 "Unavailable link" |> MenuItem.disabled
                      MenuItem.action "$sync++" "Syncing account" |> MenuItem.pending ]
                  MenuItem.separator
                  MenuItem.destructiveAction "$delete++" "Delete draft" ]
            let html =
                DropdownMenu.create "account-actions" "Actions" menuItems
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
            Expect.stringContains html "text-[var(--fve-critical-text)]" "DropdownMenu preserves destructive tone"
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
                DropdownMenu.create "start-actions" "Start actions" [ MenuItem.link 1 "First" ]
                |> DropdownMenu.withAlignment MenuAlignment.Start
                |> DropdownMenu.render string
                |> Render.toString
            Expect.stringContains startAlignedHtml "position-area: block-end span-inline-end" "DropdownMenu supports typed start alignment"
            Expect.isFalse (startAlignedHtml.Contains("position-area: block-end span-inline-start")) "Start alignment replaces default end alignment"

            let adjacentHtml =
                div {
                    DropdownMenu.create "first-actions" "First actions" [ MenuItem.link 1 "First" ] |> DropdownMenu.render string
                    DropdownMenu.create "second-actions" "Second actions" [ MenuItem.link 2 "Second" ] |> DropdownMenu.render string
                }
                |> Render.toString
            Expect.stringContains adjacentHtml "_first_actions_open" "First menu owns a stable signal"
            Expect.stringContains adjacentHtml "_second_actions_open" "Second menu owns an isolated stable signal"
            Expect.equal (Regex.Matches(adjacentHtml, "id=\"first-actions-menu\"").Count) 1 "First menu ID is unique"
            Expect.equal (Regex.Matches(adjacentHtml, "id=\"second-actions-menu\"").Count) 1 "Second menu ID is unique"

            Expect.throws (fun () -> MenuItem.link 1 " " |> ignore) "Menu links require accessible labels"
            Expect.throws (fun () -> MenuItem.group " " [ MenuItem.link 1 "Item" ] |> ignore) "Menu groups require labels"
            Expect.throws (fun () -> MenuItem.group "Empty" [] |> ignore) "Menu groups require items"
            Expect.throws (fun () -> MenuItem.group "Outer" [ MenuItem.group "Inner" [ MenuItem.link 1 "Item" ] ] |> ignore) "Menu groups cannot nest"
            Expect.throws (fun () -> MenuItem.separator<int> |> MenuItem.withShortcut "S" |> ignore) "Separators cannot have item presentation"

            let docsMenuHtml = Components.dropdownMenuRegion false |> Render.toString
            Expect.stringContains docsMenuHtml "href=\"/components/dropdown-menu#components-dropdown-menu\"" "Docs menu uses a real local typed destination"
            Expect.isFalse (docsMenuHtml.Contains("ledger.example.test")) "Docs menu exposes no fake external destination"

            let patchedHtml = Components.patchedDropdownMenuRegion |> Render.toString
            Expect.stringContains patchedHtml "id=\"components-dropdown-menu-region\"" "Docs patch preserves the stable menu region"
            Expect.stringContains patchedHtml "Review refreshed actions" "Docs patch changes server-rendered menu content"
            Expect.stringContains patchedHtml "role=\"status\"" "Docs patch reports its completed server update"
            Expect.isFalse (patchedHtml.Contains("Refresh actions")) "Docs patch replaces the initiating command"
        }

        test "Components Select owns the complete branded select-only form contract" {
            let html =
                Select.create "status" "Status" id [ Select.option "active" "Active"; Select.option "disabled" "Disabled" |> Select.disable ]
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
            let options = [ Select.option 101 "Operating"; Select.option 102 "Tax reserve" |> Select.disable ]
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
            Expect.stringContains html "group-aria-expanded:rotate-180" "searchable Select uses the normal Select chevron state"
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
                Select.create "account" "Account" id [ Select.option "operating" "Operating" ]
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
                |> Switch.withChecked
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
                RadioGroup.create "mode" "Mode" id [ RadioGroup.option "automatic" "Automatic"; RadioGroup.option "manual" "Manual"; RadioGroup.option "disabled" "Disabled" |> RadioGroup.disable ]
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

        test "Components finite-choice stable IDs isolate repeated form names" {
            let html =
                form {
                    Checkbox.create "choice" "First choice" |> Checkbox.withId "first-choice" |> Checkbox.render
                    Checkbox.create "choice" "Second choice" |> Checkbox.withId "second-choice" |> Checkbox.render
                    Switch.create "setting" "First setting" |> Switch.withId "first-setting" |> Switch.render
                    Switch.create "setting" "Second setting" |> Switch.withId "second-setting" |> Switch.render
                    RadioGroup.create "mode" "First mode" id [ RadioGroup.option "a" "A" ] |> RadioGroup.withId "first-mode" |> RadioGroup.render
                    RadioGroup.create "mode" "Second mode" id [ RadioGroup.option "b" "B" ] |> RadioGroup.withId "second-mode" |> RadioGroup.render
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
            let options = [ Select.option "a/b" "Slash"; Select.option "a-b" "Dash" ]
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
                RadioGroup.create "collisionradio" "Collision radio" id options
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
                Button.create "Save"
                |> Button.disabled
                |> Button.withAttributes [ _attr ("TYPE", "submit"); _attr "disabled"; _class "override" ]
                |> Button.render
                |> openingTag "button"
            Expect.equal (attributeCount "type" buttonTag) 1 "Button owns one type"
            Expect.equal (attributeCount "disabled" buttonTag) 1 "Button owns one disabled state"
            Expect.equal (attributeCount "class" buttonTag) 1 "Button owns one class attribute"
            Expect.stringContains buttonTag "type=\"button\"" "consumer type is ignored"
            Expect.isFalse (buttonTag.Contains("override")) "consumer class is ignored"

            let statusTag =
                Status.create "Active"
                |> Status.withAttributes [ _class "override" ]
                |> Status.render
                |> openingTag "span"
            Expect.equal (attributeCount "class" statusTag) 1 "Status owns one class attribute"
            Expect.isFalse (statusTag.Contains("override")) "Status consumer class is ignored"

            let tableTag =
                Table.create "Values" [ Table.column "Value" text ] [ "one" ]
                |> Table.withAttributes [ _role "presentation"; _class "override" ]
                |> Table.render
                |> openingTag "table"
            Expect.equal (attributeCount "class" tableTag) 1 "Table owns one class attribute"
            Expect.isFalse (tableTag.Contains("override")) "Table consumer class is ignored"
            Expect.isFalse (tableTag.Contains("presentation")) "Table consumer role is ignored"

            let selectHtml =
                Select.create "status" "Status" id [ Select.option "active" "Active" ]
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
                Select.create "account" "Account" id [ Select.option "operating" "Operating" ]
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

        test "Documentation and controls share one assembly without implicit browser assets" {
            let assembly = typeof<Tone>.Assembly
            Expect.equal typeof<FSharp.ViewEngine.Components.Documentation.DocsColorMode>.Assembly assembly "Documentation ships inside Components"
            Expect.equal (assembly.GetName().Name) "FSharp.ViewEngine.Components" "one UI assembly"
            Expect.isFalse
                (assembly.GetReferencedAssemblies() |> Array.exists (fun reference -> reference.Name = "FSharp.ViewEngine.Docs"))
                "no legacy Docs dependency"
            for publicType in assembly.GetExportedTypes() do
                Expect.isFalse (publicType.Namespace = "FSharp.ViewEngine.Docs") "no legacy namespace facade"
            let ordinaryPage = Button.create "Continue" |> Button.render |> Render.toString
            for documentationAsset in [ "Prism"; "mermaid"; "spec-shell"; "data-docs-" ] do
                Expect.isFalse (ordinaryPage.Contains documentationAsset) "ordinary controls do not opt into Documentation assets"
        }

        test "Components Tailwind contract is isolated and CI-proven" {
            let packageDirectory = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "FSharp.ViewEngine.Components"))
            let manifest = File.ReadAllText(Path.Combine(packageDirectory, "FSharp.ViewEngine.Components.tailwind.css"))
            let consumer = File.ReadAllText(Path.Combine(packageDirectory, "consumer.css"))
            let verification = File.ReadAllText(Path.Combine(packageDirectory, "verify-tailwind.sh"))
            let renderer =
                Directory.EnumerateFiles(packageDirectory, "*.fs")
                |> Seq.sort
                |> Seq.map File.ReadAllText
                |> String.concat "\n"
            let componentsProject = File.ReadAllText(Path.Combine(packageDirectory, "FSharp.ViewEngine.Components.fsproj"))
            let docsProject = File.ReadAllText(Path.Combine(__SOURCE_DIRECTORY__, "..", "Docs", "Docs.fsproj"))
            let docsStyles = File.ReadAllText(Path.Combine(__SOURCE_DIRECTORY__, "..", "Docs", "input.css"))
            let dockerfile = File.ReadAllText(Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "Dockerfile")))

            Expect.stringContains manifest "@source inline(" "package classes use an explicit source manifest"
            Expect.stringContains manifest "bg-[var(--fve-brand-solid)]" "semantic brand utility is forced"
            Expect.stringContains manifest ".fve-components" "semantic defaults ship with the manifest"
            Expect.stringContains manifest ".dark .fve-components" "dark defaults ship with the manifest"
            Expect.stringContains manifest ".dark .fve-theme-sky" "Sky ships theme-specific dark brand roles"
            Expect.stringContains manifest ".dark .fve-theme-emerald" "Emerald ships theme-specific dark brand roles"
            Expect.stringContains manifest ".dark .fve-theme-cyan" "Cyan ships theme-specific dark brand roles"
            Expect.stringContains manifest ".dark .fve-theme-neutral" "Neutral ships theme-specific dark brand roles"
            Expect.stringContains manifest "--fve-shell-bar-min-height" "shell bars share one semantic height token"
            Expect.stringContains manifest "input[type=\"search\"]::-webkit-search-cancel-button" "branded Combobox clear action replaces duplicate WebKit search chrome"
            Expect.stringContains manifest "aria-selected:bg-[var(--fve-surface)]" "segmented Tabs selected surface is forced"
            Expect.stringContains manifest "aria-selected:border-[var(--fve-brand-solid)]" "underlined Tabs selected border is forced"
            Expect.stringContains renderer "py-[var(--fve-control-padding-block)]" "renderers consume the semantic density token"
            for role in [ "subtle"; "solid"; "hover"; "active"; "text"; "ring" ] do
                Expect.isGreaterThanOrEqual
                    (Regex.Matches(manifest, $"--fve-brand-{role}:").Count)
                    4
                    $"light and dark theme definitions include brand {role}"
            Expect.stringContains consumer "@import \"tailwindcss\" source(none)" "fixture disables automatic source scanning"
            Expect.stringContains consumer "@import \"./FSharp.ViewEngine.Components.tailwind.css\"" "clean consumer imports only the contract"
            Expect.stringContains consumer ".acme-theme" "consumer override is independent"
            Expect.stringContains consumer "--fve-brand-active" "consumer override includes pressed feedback"
            Expect.stringContains verification ".bg-\\[var\\(--fve-brand-solid\\)\\]" "verification checks generated package utility"
            Expect.stringContains verification ".active\\:bg-\\[var\\(--fve-brand-active\\)\\]" "verification checks generated active-state utility"
            Expect.stringContains verification ".overflow-x-auto" "verification checks data-display overflow utility"
            Expect.stringContains verification ".sticky" "verification checks sticky table-action columns"
            Expect.stringContains verification ".aria-selected\\:bg-\\[var\\(--fve-surface\\)\\]" "verification checks segmented Tabs selection"
            Expect.stringContains verification ".aria-selected\\:border-\\[var\\(--fve-brand-solid\\)\\]" "verification checks underlined Tabs selection"
            Expect.stringContains verification ".backdrop\\:bg-\\[var\\(--fve-overlay-backdrop\\)\\]" "verification checks semantic overlay backdrop utility"
            Expect.stringContains verification ".sm\\:w-96" "verification checks responsive drawer width"
            Expect.stringContains verification ".md\\:visible" "verification checks medium responsive shell navigation"
            Expect.stringContains verification ".lg\\:visible" "verification checks large responsive shell navigation"
            Expect.stringContains verification ".max-w-4xl" "verification checks Page width contracts"
            Expect.stringContains verification ".lg\\:grid-cols-3" "verification checks responsive detail columns"
            Expect.stringContains verification ".size-9" "verification checks pagination sizing"
            Expect.stringContains verification ".peer-focus-visible\\:ring-\\[var\\(--fve-critical-ring\\)\\]" "verification checks invalid native-control focus treatment"
            Expect.stringContains verification ".acme-theme" "verification checks consumer CSS"
            Expect.stringContains docsStyles ".docs-components-preview .fve-components" "Docs owns the example theme adapter"
            Expect.stringContains docsStyles "--fve-page: var(--spec-bg)" "component examples inherit the Docs page surface"
            Expect.stringContains docsStyles "--fve-brand-solid: var(--spec-accent-700)" "component examples use a contrast-safe Docs sky accent"
            Expect.stringContains docsStyles "--fve-overlay-backdrop:" "component examples inherit a Docs-owned overlay backdrop"
            Expect.stringContains dockerfile "FSharp.ViewEngine.Components/verify-tailwind.sh" "container CI executes the clean-consumer proof"
            Expect.stringContains componentsProject "..\\FSharp.ViewEngine\\FSharp.ViewEngine.fsproj" "Components depends on Core"
            Expect.isFalse (componentsProject.Contains("FSharp.ViewEngine.Docs")) "Components remains independent from Docs"
            Expect.stringContains docsProject "..\\FSharp.ViewEngine.Components\\FSharp.ViewEngine.Components.fsproj" "Docs consumes Components as a project"
            Expect.isFalse (docsProject.Contains("ComponentsContract.fs")) "Docs does not compile an internal Components implementation"

            let manifestClasses =
                Regex.Matches(manifest, "@source inline\\(\\\"([^\\\"]*)\\\"\\)")
                |> Seq.collect (fun matched -> matched.Groups[1].Value.Split(' '))
                |> Set.ofSeq
            let ignoredTokens = set [ "No"; "records"; "button"; "submit"; "reset"; "menuitem"; "separator" ]
            let rendererClasses =
                renderer.Replace("\r\n", "\n").Split('\n')
                |> Array.filter (fun line ->
                    [ "_class"; "Variant."; "Tone."; "ControlSize."; "headerClass"; "cellClass"; "className config.theme" ]
                    |> List.exists line.Contains)
                |> Array.collect (fun line ->
                    Regex.Matches(line, "\"([^\"]*)\"")
                    |> Seq.collect (fun matched -> matched.Groups[1].Value.Split(' '))
                    |> Seq.toArray)
                |> Array.filter (fun token ->
                    token <> "" && not (token.StartsWith("fve-")) && not (token.StartsWith("spec-")) && not (ignoredTokens.Contains token))
                |> Set.ofArray
            let missingClasses = Set.difference rendererClasses manifestClasses
            Expect.isEmpty missingClasses $"every renderer-owned utility is present in the Tailwind source manifest; missing: {missingClasses}"
        }

        test "Datastar docs cover every stable helper and modifier shapes" {
            let html = Datastar.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let helpers =
                [ "_dataAnimate"; "_dataAttr"; "_dataBind"; "_dataClass"; "_dataComputed"
                  "_dataCustomValidity"; "_dataEffect"; "_dataIgnore"; "_dataIgnoreMorph"
                  "_dataIndicator"; "_dataInit"; "_dataJsonSignals"; "_dataMatchMedia"; "_dataOn"
                  "_dataOnIntersect"; "_dataOnInterval"; "_dataOnRaf"; "_dataOnResize"
                  "_dataOnSignalPatch"; "_dataOnSignalPatchFilter"; "_dataPersist"
                  "_dataPreserveAttr"; "_dataQueryString"; "_dataRef"; "_dataReplaceUrl"
                  "_dataScrollIntoView"; "_dataShow"; "_dataSignals"; "_dataStyle"; "_dataText"
                  "_dataViewTransition" ]

            for helper in helpers do
                Expect.stringContains html helper helper

            Expect.stringContains html "debounce.200ms" "keyed modifier example"
            Expect.stringContains html "smooth" "no-value modifier example"
            Expect.isFalse (html.Contains("_dataRocket")) "removed data-rocket helper"
        }

        test "SVG docs cover the maintained production subset" {
            let html = Svg.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let elements =
                [ "circle"; "clipPath"; "defs"; "desc"; "ellipse"; "g"; "line"
                  "linearGradient"; "mask"; "path"; "polygon"; "polyline"
                  "radialGradient"; "rect"; "stop"; "svg"; "symbol"; "textElement"
                  "titleElement"; "tspan"; "useElement" ]
            let attributes =
                [ "_clipPath"; "_clipPathUnits"; "_cx"; "_cy"; "_dominantBaseline"
                  "_fillOpacity"; "_gradientTransform"; "_gradientUnits"; "_maskContentUnits"
                  "_maskUnits"; "_pathLength"; "_preserveAspectRatio"; "_spreadMethod"
                  "_stopColor"; "_stopOpacity"; "_strokeDasharray"; "_strokeDashoffset"
                  "_strokeMiterlimit"; "_strokeOpacity"; "_textAnchor"; "_textLength"
                  "_vectorEffect"; "_xmlns" ]

            for helper in elements @ attributes do
                Expect.stringContains html helper helper

            Expect.stringContains html "production subset" "support policy"
            Expect.stringContains html "Html.el" "unsupported element escape hatch"
            Expect.stringContains html "_attr" "unsupported attribute escape hatch"
            Expect.stringContains html "_ariaLabelledby" "informative accessibility pattern"
            Expect.stringContains html "_ariaHidden" "decorative accessibility pattern"
            Expect.stringContains html "xlink:href" "deprecated linking guidance"
            let exampleCount = html.Split([| "data-docs-example=\"true\"" |], System.StringSplitOptions.None).Length - 1
            Expect.isGreaterThanOrEqual exampleCount 3 "icon, chart, and resource examples have previews"
        }

        test "Tailwind Plus Elements docs cover the complete 1.0.22 API" {
            let html = TailwindElements.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let helpers =
                [ "elAutocomplete"; "elCommandGroup"; "elCommandList"; "elCommandPalette"
                  "elCommandPreview"; "elCopyable"; "elDefaults"; "elDialog"
                  "elDialogBackdrop"; "elDialogPanel"; "elDisclosure"; "elDropdown"
                  "elMenu"; "elNoResults"; "elOption"; "elOptions"; "elPopover"
                  "elPopoverGroup"; "elSelect"; "elSelectedContent"; "elTabGroup"
                  "elTabList"; "elTabPanels"; "_anchorStrategy" ]

            for helper in helpers do
                Expect.stringContains html helper helper

            Expect.stringContains html "@tailwindplus/elements@1.0.22" "pinned Elements installation"
            Expect.stringContains html "src=\"/scripts/tailwind-elements-loader.1.0.22.js\" type=\"module\"" "parse-safe Elements loader"
            let loader =
                Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "Docs", "wwwroot", "scripts", "tailwind-elements-loader.1.0.22.js"))
                |> File.ReadAllText
            Expect.stringContains loader "void loading.catch(() => undefined)" "optional module rejection is handled during outgoing navigation"
            Expect.stringContains loader "loading," "the original observable module promise remains available"
            Expect.stringContains html "<el-autocomplete" "previews render the actual custom elements"
            Expect.isFalse (html.Contains("Native initial-state preview")) "previews are not static approximations"
            Expect.stringContains html "open type TailwindElements" "current API name"
            Expect.isFalse (html.Contains("open type Tailwind\n")) "removed Tailwind API"
        }

        test "Removed Tailwind documentation route is not registered" {
            Expect.isFalse (Registry.all |> List.exists (fun page -> page.path = "/extensions/tailwind")) "old canonical route"
            Expect.isFalse (Registry.aliases |> List.exists (fun (alias, _) -> alias = "/extensions/tailwind")) "old route alias"
        }

        test "Changelog contains released package versions only" {
            let html = Changelog.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "FSharp.ViewEngine.Docs 2026.8.1" "latest released Docs package"
            Expect.stringContains html "FSharp.ViewEngine 2026.8.2" "latest released Core package"
            Expect.stringContains html "FSharp.ViewEngine 2026.8.1" "previous released Core package"
            Expect.stringContains html "release docs/v2026.8.1" "immutable Docs release link"
            Expect.stringContains html "release v2026.8.2" "immutable Core release link"
            Expect.isFalse (html.Contains("Unreleased")) "unreleased changes are not published"
            Expect.isFalse (html.Contains("Datastar Migration")) "unreleased migration notes are not published"
        }

        test "Installation docs distinguish package assets from runtime support" {
            let html = Installation.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "net8.0 compatibility asset" "package asset baseline"
            Expect.stringContains html ".NET 8, .NET 9, and .NET 10" "tested runtime matrix"
            Expect.stringContains html "November 10, 2026" "net8/net9 support horizon"
            Expect.stringContains html "November 14, 2028" ".NET 10 support horizon"
            Expect.stringContains html "Source Link" "source debugging support"
        }

        test "Compatibility route remains registered" {
            Expect.contains Registry.aliases ("/giraffe", Usage.page.path) "legacy Giraffe route"
        }
    ]

[<EntryPoint>]
let main args = runTestsWithCLIArgs [] args tests
