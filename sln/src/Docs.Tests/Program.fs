module Docs.Tests.Program

open System
open System.IO
open System.Net
open System.Text.RegularExpressions
open Expecto
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Datastar
open Docs.Common
open Docs.Pages

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
        "/docs/components/interactive-examples"
        "/docs/components/api-reference"
        "/docs/components/diagrams"
        "/docs/page-examples/documentation-site"
        "/docs/page-examples/api-reference"
        "/docs/page-examples/executable-specification"
        "/components"
        "/components/installation"
        "/components/button"
        "/components/icon-button"
        "/components/badge"
        "/components/status"
        "/components/loading-indicator"
        "/components/empty-state"
        "/components/table"
        "/components/description-list"
        "/components/metric"
        "/components/pagination"
        "/components/chart"
        "/components/select"
        "/components/combobox"
        "/components/checkbox"
        "/components/switch"
        "/components/toggle-button"
        "/components/tabs"
        "/components/radio-group"
        "/components/dropdown-menu"
        "/components/dialog"
        "/components/confirmation-dialog"
        "/components/drawer"
        "/components/collection"
        "/components/detail"
        "/components/app-shell"
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
                [ "Getting started"; "Core concepts"; "Integrations"; "FSharp.ViewEngine.Components"; "FSharp.ViewEngine.Docs"; "Project" ]
                "general Components precede the specialized Docs toolkit and project pages"

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
            Expect.sequenceEqual (section "FSharp.ViewEngine.Docs") [ "Overview" ] "toolkit overview is distinct"
            Expect.sequenceEqual
                (section "Components")
                [ "Layouts"; "Content"; "Navigation"; "Interactive examples"; "API reference"; "Diagrams" ]
                "component categories follow the catalog order"
            Expect.sequenceEqual
                (section "Page examples")
                [ "Documentation site"; "API reference"; "Executable specification" ]
                "page examples are grouped separately"
            Expect.sequenceEqual (section "FSharp.ViewEngine.Components") [ "Overview"; "Installation" ] "Components starts with overview and installation"
            Expect.sequenceEqual
                (section "Actions and feedback")
                [ "Button"; "Icon button"; "Badge"; "Status"; "Loading indicator"; "Empty state" ]
                "action and feedback foundations"
            Expect.sequenceEqual
                (section "Data display")
                [ "Table"; "Description list"; "Metric"; "Pagination"; "Chart" ]
                "data-display components"
            Expect.sequenceEqual (section "Form controls") [ "Select"; "Combobox"; "Checkbox"; "Switch"; "Toggle button"; "Radio group" ] "form controls"
            Expect.sequenceEqual (section "Navigation") [ "Tabs" ] "navigation components"
            Expect.sequenceEqual (section "Menus and overlays") [ "Dropdown menu"; "Dialog"; "Confirmation dialog"; "Drawer" ] "menu and overlay components"
            Expect.sequenceEqual (section "Compositions") [ "Collection"; "Detail"; "App shell" ] "page compositions"
            Expect.sequenceEqual (section "Guides") [ "Interaction and server state"; "Accessibility"; "Theming and density"; "Tailwind CSS"; "Customization"; "Versioning" ] "shared Components guides"
            Expect.sequenceEqual (section "Project") [ "Benchmarks"; "Changelog" ] "project order"
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

                if Showcase.tryPage page.path |> Option.orElseWith (fun () -> Components.tryPage page.path) |> Option.isNone then
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
            Expect.stringContains home ".spec-content-link{text-decoration:underline" "inline links have a non-color affordance"
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
            Expect.stringContains home "--docs-code-bg:#f6f8fa" "light code palette is embedded"
            Expect.stringContains home "--docs-code-bg:#0d1117" "dark code palette is embedded"
            Expect.stringContains home "/scripts/prism-fsharp.1.29.0.min.js" "pinned FSharp grammar"
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
                Expect.stringContains document "<style>" $"{path} includes the Docs component styles"
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

        test "Docs toolkit is organized into component and page-example catalogs" {
            let render registration = registration |> View.document Registry.navigation |> Render.toHtmlDocString
            let overview = render Showcase.overviewRegistration

            Expect.equal Showcase.componentRegistrations.Length 6 "six component categories"
            Expect.equal Showcase.pageExampleRegistrations.Length 3 "three page-example categories"
            Expect.stringContains overview "FSharp.ViewEngine.Docs" "package overview"
            Expect.stringContains overview "This documentation site is built with FSharp.ViewEngine.Docs" "site dogfoods the package"
            Expect.stringContains overview "Browse components" "component catalog link"
            Expect.stringContains overview "Browse page examples" "page-example catalog link"
            Expect.isFalse (overview.Contains("Example content")) "overview omits the old fixture callout"
            Expect.stringContains overview "rel=\"prev\" href=\"/components/versioning\"" "Docs follows the Components guides"
            Expect.stringContains overview "rel=\"next\" href=\"/docs/components/layouts\"" "overview continues to layouts"

            for registration in Showcase.componentRegistrations @ Showcase.pageExampleRegistrations do
                let html = render registration
                Expect.stringContains html "docs-article-layout" $"{registration.path} uses the catalog layout"
                Expect.stringContains html "data-docs-example=\"true\"" $"{registration.path} includes examples"
                Expect.stringContains html ">Code</button>" $"{registration.path} code toggle"
                Expect.stringContains html ">Preview</button>" $"{registration.path} preview toggle"

            let api = render Showcase.apiPageExampleRegistration
            Expect.stringContains api "does not expose an HTTP /v1/render endpoint" "fictional API is labeled near its example"
            Expect.stringContains api "let endpoint = docsApiEndpoint POST" "API page uses the compiled endpoint definition"
            Expect.stringContains api "data-docs-preview-src=\"/docs/previews/api-reference-page-example\"" "API preview lazily uses an isolated route"
            Expect.isTrue (Showcase.previewRoutes.ContainsKey "/docs/previews/api-reference-page-example") "isolated API preview is registered"

            let interactive = render Showcase.interactiveRegistration
            Expect.stringContains interactive "component-workflow-states" "state-tabs code uses the preview's actual identifier"
            Expect.stringContains interactive "productScreen &quot;ready&quot;" "state-tabs code uses the actual compiled child view"
            Expect.isFalse (interactive.Contains("docsStateTabs &quot;workflow-states&quot;")) "approximate state-tabs source is removed"

            let specification = render Showcase.specificationPageExampleRegistration
            Expect.stringContains specification "role=\"tablist\"" "specification preview uses state tabs"
            Expect.stringContains specification "spec-browser-frame" "specification preview uses a browser frame"
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
            let renderedComponents = componentRegistrations |> List.map render
            let renderedGuides = Components.guideRegistrations |> List.map render
            let allHtml = String.concat Environment.NewLine (overview :: installation :: renderedComponents @ renderedGuides)

            Expect.equal Components.allRegistrations.Length 33 "overview, installation, twenty-five components, and six guides"
            Expect.stringContains overview "Accessible, server-rendered Tailwind components" "consumer-facing introduction"
            Expect.stringContains overview "Browse components" "overview is a component catalog"
            Expect.stringContains overview "href=\"/components/button\"" "catalog deep-links Button"
            Expect.stringContains overview "href=\"/components/app-shell\"" "catalog deep-links App shell"
            Expect.stringContains overview "Required inputs stay visible" "required input policy"
            Expect.stringContains overview "Optional behavior is piped" "configuration policy"
            Expect.stringContains overview "Custom content stays HTML" "slot policy"
            Expect.stringContains overview "Closed choices are typed" "typed variant policy"
            Expect.stringContains installation "dotnet add package FSharp.ViewEngine.Components" "package installation"
            Expect.stringContains installation "contentFiles/any/any" "packaged Tailwind manifest location"

            for registration, html in List.zip componentRegistrations renderedComponents do
                Expect.stringContains html "docs-article-layout" $"{registration.path} uses the article layout"
                Expect.stringContains html "data-docs-example=\"true\"" $"{registration.path} has an executable example"
                let examples = html.Split([| "data-docs-example=\"true\"" |], System.StringSplitOptions.None).Length - 1
                Expect.equal examples 1 $"{registration.path} has one focused example"

            for registration, html in List.zip Components.guideRegistrations renderedGuides do
                Expect.stringContains html "docs-article-layout" $"{registration.path} uses the article layout"
                Expect.isFalse (html.Contains("data-docs-example=\"true\"")) $"{registration.path} is focused guidance rather than a duplicate gallery"

            Expect.stringContains allHtml "Interaction and server state" "interaction guide"
            Expect.stringContains allHtml "aria-activedescendant identifies the visually active option" "APG focus relationship"
            Expect.stringContains allHtml "cycles options when the same character is repeated" "Select typeahead behavior"
            Expect.stringContains allHtml "bounded character-prefix navigation and repeated-character cycling skip disabled and pending items" "DropdownMenu character-navigation behavior"
            Expect.stringContains allHtml "server-rendered Datastar patch replaces the example region" "DropdownMenu patch continuity guidance"
            Expect.stringContains allHtml "/components/menus/actions" "DropdownMenu example uses a real Docs-owned patch endpoint"
            Expect.stringContains allHtml "Theming and density" "theme guide"
            Expect.stringContains allHtml "Tailwind CSS setup" "Tailwind setup guide"
            Expect.stringContains allHtml "Application responsibilities" "application boundary guidance"
            Expect.stringContains allHtml "explicit Tailwind v4 source manifest" "Tailwind source manifest"
            Expect.stringContains allHtml "versions independently" "version policy"
            Expect.stringContains allHtml "minimum compatible FSharp.ViewEngine" "Core compatibility policy"
            Expect.isFalse (allHtml.Contains("Pre-release contract")) "release-process framing is absent"
            Expect.isFalse (allHtml.Contains("Compiled Call Sites")) "examples are not framed as implementation evidence"
            Expect.isFalse (allHtml.Contains("package-spine task")) "internal task language is absent"
            Expect.isFalse (allHtml.Contains("veSelect {")) "Components does not introduce a component CE"
            Expect.isFalse (allHtml.Contains("color &quot;emerald-600&quot;")) "ordinary API does not accept raw palette strings"

            for source in [
                "Button.primary &quot;Create account&quot;"
                "Button.pending"
                "IconButton.create &quot;Add account&quot; plusIcon"
                "Badge.create &quot;Internal&quot;"
                "Status.create &quot;Needs review&quot;"
                "LoadingIndicator.create &quot;Loading account balances&quot;"
                "EmptyState.create &quot;No accounts yet&quot;"
                "Table.create &quot;Accounts&quot;"
                "Table.asRowHeader"
                "DescriptionList.create"
                "DetailField.status &quot;Status&quot;"
                "Metric.text &quot;Available balance&quot; &quot;$42,800&quot;"
                "Pagination.create &quot;Accounts pages&quot;"
                "PaginationItem.current page"
                "Chart.create &quot;operating-balance&quot;"
                "Chart.empty &quot;new-account-balance&quot;"
                "Select.create &quot;status&quot; &quot;Status&quot; statusValue selectStatusOptions"
                "Combobox.create &quot;account&quot; &quot;Parent account&quot; string"
                "Combobox.withSearch (ComboboxSearch.Remote &quot;/components/accounts/search&quot;)"
                "Combobox.renderOptions"
                "Checkbox.create &quot;includeArchived&quot; &quot;Include archived accounts&quot;"
                "Switch.create &quot;postingNotifications&quot; &quot;Posting notifications&quot;"
                "ToggleButton.create &quot;components-compact-rows&quot; &quot;Compact rows&quot;"
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
                "Collection.create &quot;Accounts&quot; accountTable"
                "Detail.create &quot;Operating&quot;"
                "AppShell.create &quot;Ledger&quot; Accounts" ] do
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
            for api in [ "Combobox.clearable"; "Combobox.loading"; "Combobox.withError"; "Combobox.disabled"; "Combobox.pending" ] do
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
            Expect.stringContains allHtml "aria-current=\"page\"" "AppShell retains typed current destination"
            Expect.stringContains allHtml "--fve-brand-solid" "consumer theme overrides are documented"
            Expect.stringContains overview "rel=\"prev\" href=\"/extensions/tailwind-elements\"" "Components follows integrations"
            Expect.stringContains overview "rel=\"next\" href=\"/components/installation\"" "overview continues to installation"
            let versioning = render Components.versioningRegistration
            Expect.stringContains versioning "rel=\"prev\" href=\"/components/customization\"" "last guide follows customization"
            Expect.stringContains versioning "rel=\"next\" href=\"/docs\"" "Components precedes the specialized Docs toolkit"
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

        test "Components data display preserves native semantics and consumer ownership" {
            let tableHtml =
                Table.create "Account balances" [
                    Table.column "Account" text |> Table.asRowHeader
                    Table.column "Balance" (fun value -> text value) |> Table.alignEnd
                ] [ "Operating" ]
                |> Table.withVisibleCaption
                |> Table.withDensity Density.Compact
                |> Table.render
                |> Render.toString
            Expect.stringContains tableHtml "role=\"region\" aria-label=\"Account balances\" tabindex=\"0\"" "Table exposes a labelled keyboard-reachable overflow region"
            Expect.stringContains tableHtml "<caption class=\"px-4 py-3 text-left text-sm font-semibold" "Table can show its native caption"
            Expect.stringContains tableHtml "<th scope=\"row\"" "Table identifies consumer-selected row headers"
            Expect.stringContains tableHtml "px-3 py-2" "Table compact density reduces cell spacing"
            Expect.throws (fun () -> Table.create " " [ Table.column "Value" text ] [ "one" ] |> ignore) "Table requires a caption"

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
            Expect.stringContains detailsHtml "<dl class=\"grid gap-x-6 gap-y-5 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3\"" "DescriptionList retains responsive native list semantics"
            Expect.stringContains detailsHtml "<dt" "DetailField renders a term"
            Expect.stringContains detailsHtml "<dd" "DetailField renders its value and description"
            Expect.stringContains detailsHtml "Available for posting." "DetailField preserves supporting context"
            Expect.isFalse (detailsHtml.Contains("override")) "description-list structure protects base classes"
            Expect.isFalse (detailsHtml.Contains("role=")) "description-list structure rejects role replacement"
            Expect.throws (fun () -> DetailField.text " " "Value" |> ignore) "DetailField requires a label"
            Expect.throws (fun () -> DescriptionList.create [] |> ignore) "DescriptionList requires fields"

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
            Expect.stringContains paginationPage3Html "href=\"/components/pagination?page=4#components-pagination-panel-preview\"" "Docs pagination links to a real local next page"
            Expect.isFalse (paginationPage3Html.Contains("ledger.example.test")) "Docs pagination does not expose externally fake destinations"

            let paginationPage8Html =
                Components.paginationPageFor 99
                |> View.documentWithPage Registry.navigation Components.paginationRegistration
                |> Render.toHtmlDocString
            Expect.stringContains paginationPage8Html "Showing 176–184 of 184 accounts" "Docs pagination clamps out-of-range requests"
            Expect.stringContains paginationPage8Html "aria-current=\"page\" aria-label=\"Page 8, current page\"" "Docs pagination clamps to the last page"

            let visual = raw "<svg aria-hidden=\"true\"></svg>"
            let summary = table { caption { "Balance data" }; tbody { tr { th { _scope "row"; "August" }; td { "$42,800" } } } }
            let chartHtml =
                Chart.create "balance-chart" "Balance history" summary visual
                |> Chart.withUnits "USD"
                |> Chart.withLegend (span { "Actual balance" })
                |> Chart.withAnnotations (span { "August closes at $42,800." })
                |> Chart.withVisibleSummary
                |> Chart.withAttributes [ _role "img"; _ariaLabelledby "override"; _class "override" ]
                |> Chart.render
                |> Render.toString
            Expect.stringContains chartHtml "<figure aria-labelledby=\"balance-chart-title\" aria-describedby=\"balance-chart-summary\"" "Chart connects native figure title and summary"
            Expect.stringContains chartHtml "<figcaption id=\"balance-chart-title\"" "Chart uses a native caption"
            Expect.stringContains chartHtml "aria-label=\"Legend\"" "Chart names its legend region"
            Expect.stringContains chartHtml "aria-label=\"Annotations\"" "Chart names its annotation region"
            Expect.stringContains chartHtml "<table>" "Chart preserves consumer-supplied accessible data"
            Expect.isFalse (chartHtml.Contains("override")) "Chart protects relationships and base presentation"
            Expect.isFalse (chartHtml.Contains("role=\"img\"")) "Chart protects its native figure semantics"
            Expect.throws (fun () -> Chart.create "invalid id" "Title" summary visual |> ignore) "Chart requires a stable valid ID"
            Expect.throws (fun () -> Chart.create "valid-id" " " summary visual |> ignore) "Chart requires a title"

            let emptyChartHtml =
                Chart.empty "empty-chart" "Balance history" (p { "No data is available." }) (EmptyState.create "No history" "Post an entry first." |> EmptyState.render)
                |> Chart.render
                |> Render.toString
            Expect.stringContains emptyChartHtml "No history" "Chart composes an explicit empty state"
            Expect.stringContains emptyChartHtml "class=\"sr-only\"" "Chart summary remains accessible when visually hidden"
            Expect.isFalse (chartHtml.Contains("canvas")) "Chart adds no drawing runtime or canvas policy"
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
            Expect.stringContains html "role=\"menu\" aria-label=\"Actions\"" "DropdownMenu popup has menu semantics and a name"
            Expect.stringContains html "role=\"group\" aria-labelledby=\"account-actions-menu-entry-0-label\"" "DropdownMenu labels groups"
            Expect.stringContains html ">Account</div>" "DropdownMenu keeps the group label visible"
            Expect.stringContains html "data-test-icon=\"review\"" "DropdownMenu preserves consumer-owned leading content"
            Expect.stringContains html "<kbd aria-hidden=\"true\"" "DropdownMenu presents shortcut hints without changing item names"
            Expect.stringContains html ">R</kbd>" "DropdownMenu preserves shortcut text"
            Expect.stringContains html "role=\"separator\"" "DropdownMenu preserves separators"
            Expect.stringContains html "absolute top-full z-30 mt-2 w-64" "DropdownMenu bounds popup width"
            Expect.stringContains html "right-0" "DropdownMenu preserves end alignment by default"
            Expect.stringContains html "text-[var(--fve-critical-text)]" "DropdownMenu preserves destructive tone"
            Expect.stringContains html "data-fve-menu-label=\"record review\"" "DropdownMenu exposes normalized labels for character navigation"
            Expect.stringContains html "_account_actions_typeahead" "DropdownMenu isolates bounded character-navigation state"
            Expect.stringContains html "data-on:fve-dropdown-open__window" "DropdownMenu listens for adjacent menu openings without sharing signals"
            Expect.stringContains html "new CustomEvent(&#39;fve-dropdown-open&#39;" "DropdownMenu announces its stable instance when opening"
            Expect.stringContains html "evt.key == &#39;Enter&#39; || evt.key == &#39; &#39; || evt.key == &#39;ArrowDown&#39;" "DropdownMenu trigger opens with Enter, Space, or ArrowDown"
            Expect.stringContains html "Date.now()" "DropdownMenu bounds its character-navigation buffer"
            Expect.stringContains html "every(character =&gt; character == $_account_actions_typeahead[0])" "DropdownMenu cycles repeated characters"
            Expect.stringContains html ":not([aria-disabled=true])" "DropdownMenu movement skips unavailable items"
            Expect.stringContains html "document.activeElement.click()" "DropdownMenu activates focused items with Enter or Space"
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
            Expect.stringContains startAlignedHtml "left-0" "DropdownMenu supports typed start alignment"
            Expect.isFalse (startAlignedHtml.Contains("right-0")) "Start alignment replaces default end alignment"

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

            let docsMenuHtml = Components.dropdownMenuPreview |> Render.toString
            Expect.stringContains docsMenuHtml "href=\"/components/dropdown-menu#keyboard\"" "Docs menu uses a real local typed destination"
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

        test "Components Combobox preserves query, selection, async state, and form semantics" {
            let options = [ Select.option 101 "Operating"; Select.option 102 "Tax reserve" |> Select.disable ]
            let html =
                Combobox.create "account" "Parent account" string options
                |> Combobox.withId "parent-account"
                |> Combobox.withSelected 101
                |> Combobox.withDescription "Server-owned accounts."
                |> Combobox.withValidation "Choose an account."
                |> Combobox.withSearch (ComboboxSearch.Remote "/accounts/search")
                |> Combobox.clearable
                |> Combobox.render
                |> Render.toString

            let combobox = Regex.Match(html, "<input[^>]*role=\"combobox\"[^>]*>", RegexOptions.IgnoreCase).Value
            let submittedValue = Regex.Match(html, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.stringContains combobox "id=\"fve-combobox-parent_account\"" "stable ID owns the editable input"
            Expect.stringContains combobox "aria-controls=\"fve-combobox-parent_account-options\"" "input controls the canonical listbox"
            Expect.stringContains combobox "aria-autocomplete=\"list\"" "editable input exposes list autocomplete"
            Expect.stringContains combobox "aria-describedby=\"fve-combobox-parent_account-description fve-combobox-parent_account-validation\"" "description and validation are joined"
            Expect.stringContains combobox "aria-invalid=\"true\"" "server validation is exposed"
            Expect.stringContains combobox "data-indicator:_parent_account_request_pending" "remote request owns a local loading indicator"
            Expect.stringContains combobox "requestCancellation: &#39;auto&#39;" "remote requests explicitly cancel older same-endpoint requests"
            Expect.stringContains submittedValue "name=\"account\"" "hidden input preserves the consumer form name"
            Expect.stringContains submittedValue "value=\"101\"" "typed selected identity is explicitly encoded"
            Expect.stringContains submittedValue "data-bind:parent_account_value" "submitted identity remains distinct from query binding"
            Expect.stringContains html "data-bind:parent_account_query" "editable remote query has its own signal"
            Expect.stringContains html "aria-label=\"Clear Parent account\"" "clear action has a derived accessible name"
            Expect.stringContains html "$parent_account_query = &#39;&#39;; $parent_account_value = &#39;&#39;" "clear removes query and submitted identity together"
            Expect.stringContains html "role=\"listbox\"" "popup contains the canonical listbox"
            Expect.stringContains html "aria-disabled=\"true\"" "disabled options remain discoverable but unavailable"
            Expect.stringContains html "role=\"alert\"" "form validation is announced"

            let staticHtml =
                Combobox.create "local" "Local account" string options
                |> Combobox.withEmptyMessage "No local accounts"
                |> Combobox.render
                |> Render.toString
            Expect.stringContains staticHtml "_local_query" "static query remains private ephemeral state"
            Expect.stringContains staticHtml "includes($_local_query.trim().toLowerCase())" "static options filter locally"
            Expect.stringContains staticHtml "No local accounts" "static empty state is configurable"
            Expect.isFalse (staticHtml.Contains("@get(")) "static filtering has no backend action"

            let errorHtml =
                Combobox.create "failed" "Failed account" string []
                |> Combobox.withSearch (ComboboxSearch.Remote "/accounts/search?retry=true")
                |> Combobox.withError "Accounts could not be loaded."
                |> Combobox.render
                |> Render.toString
            Expect.stringContains errorHtml "Accounts could not be loaded." "server-rendered fetch error is visible"
            Expect.stringContains errorHtml ">Retry</button>" "remote error offers a retry action"
            Expect.stringContains errorHtml "requestCancellation: &#39;auto&#39;" "retry preserves the same ordering policy"

            let loadingHtml =
                Combobox.create "loading" "Loading account" string []
                |> Combobox.withLoadingMessage "Loading accounts"
                |> Combobox.loading
                |> Combobox.render
                |> Render.toString
            Expect.stringContains loadingHtml "aria-busy=\"true\"" "loading state is programmatically busy"
            Expect.stringContains loadingHtml "Loading accounts" "loading status remains perceivable"

            let pendingHtml =
                Combobox.create "pending" "Pending account" string options
                |> Combobox.withSelected 101
                |> Combobox.pending
                |> Combobox.render
                |> Render.toString
            let pendingCombobox = Regex.Match(pendingHtml, "<input[^>]*role=\"combobox\"[^>]*>", RegexOptions.IgnoreCase).Value
            let pendingValue = Regex.Match(pendingHtml, "<input[^>]*type=\"hidden\"[^>]*>", RegexOptions.IgnoreCase).Value
            Expect.stringContains pendingCombobox "disabled" "pending control prevents interaction"
            Expect.stringContains pendingCombobox "aria-busy=\"true\"" "pending control exposes busy state"
            Expect.stringContains pendingValue "disabled" "pending selected identity is omitted from FormData"
            Expect.isFalse (pendingHtml.Contains("data-on:keydown")) "unavailable control emits no keyboard action"
        }

        test "Components branded choice controls preserve distinct complete semantics" {
            let comboboxHtml =
                Combobox.create "account" "Account" id [ Select.option "operating" "Operating" ]
                |> Combobox.withSelected "operating"
                |> Combobox.render
                |> Render.toString
            Expect.stringContains comboboxHtml "type=\"search\" role=\"combobox\"" "Combobox remains a distinct editable combobox"

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
            expectStableDistinct "fve-combobox-collisioncombobox" (fun () ->
                Combobox.create "collisioncombobox" "Collision combobox" id options
                |> Combobox.render
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
                Combobox.create "account" "Account" id [ Select.option "operating" "Operating" ]
                |> Combobox.withAttributes [
                    _id "other-id"
                    _name "other-name"
                    _dataBind "other"
                    _ariaActivedescendant "other-option"
                    _ariaInvalid false
                    _class "override" ]
                |> Combobox.render
                |> openingTag "input"
            for name in [ "id"; "type"; "role"; "aria-invalid"; "class" ] do
                Expect.equal (attributeCount name comboboxTag) 1 $"Combobox owns one input {name}"
            Expect.equal (Regex.Matches(comboboxTag, "\\sdata-bind:[^=\\s>]+(?:=|\\s|>)", RegexOptions.IgnoreCase).Count) 1 "Combobox owns one query binding"
            for rejected in [ "other-id"; "other-name"; "data-bind:other"; "other-option"; "override" ] do
                Expect.isFalse (comboboxTag.Contains(rejected)) $"Combobox rejects reserved input attribute value {rejected}"
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
            Expect.stringContains verification ".aria-selected\\:bg-\\[var\\(--fve-surface\\)\\]" "verification checks segmented Tabs selection"
            Expect.stringContains verification ".aria-selected\\:border-\\[var\\(--fve-brand-solid\\)\\]" "verification checks underlined Tabs selection"
            Expect.stringContains verification ".backdrop\\:bg-\\[var\\(--fve-overlay-backdrop\\)\\]" "verification checks semantic overlay backdrop utility"
            Expect.stringContains verification ".sm\\:w-96" "verification checks responsive drawer width"
            Expect.stringContains verification ".lg\\:grid-cols-3" "verification checks responsive detail columns"
            Expect.stringContains verification ".size-9" "verification checks pagination sizing"
            Expect.stringContains verification ".peer-focus-visible\\:ring-\\[var\\(--fve-critical-ring\\)\\]" "verification checks invalid native-control focus treatment"
            Expect.stringContains verification ".acme-theme" "verification checks consumer CSS"
            Expect.stringContains docsStyles ".docs-components-preview .fve-components" "Docs owns the example theme adapter"
            Expect.stringContains docsStyles "--fve-page: var(--spec-bg)" "component examples inherit the Docs page surface"
            Expect.stringContains docsStyles "--fve-brand-solid: var(--spec-accent-500)" "component examples inherit the Docs sky accent"
            Expect.stringContains docsStyles "--fve-overlay-backdrop:" "component examples inherit a Docs-owned overlay backdrop"
            Expect.stringContains dockerfile "FSharp.ViewEngine.Components/verify-tailwind.sh" "container CI executes the clean-consumer proof"
            Expect.stringContains componentsProject "..\\FSharp.ViewEngine\\FSharp.ViewEngine.fsproj" "Components depends on Core"
            Expect.isFalse (componentsProject.Contains("FSharp.ViewEngine.Docs")) "Components remains independent from Docs"
            Expect.stringContains docsProject "..\\FSharp.ViewEngine.Components\\FSharp.ViewEngine.Components.fsproj" "Docs consumes Components as a project"
            Expect.isFalse (docsProject.Contains("ComponentsContract.fs")) "Docs does not compile an internal Components implementation"

            let manifestClasses =
                Regex.Match(manifest, "@source inline\\(\\\"([^\\\"]*)\\\"\\)").Groups[1].Value.Split(' ')
                |> Set.ofArray
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
                    token <> "" && not (token.StartsWith("fve-")) && not (ignoredTokens.Contains token))
                |> Set.ofArray
            let missingClasses = Set.difference rendererClasses manifestClasses
            Expect.isEmpty missingClasses "every renderer-owned utility is present in the Tailwind source manifest"
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
            Expect.stringContains html "src=\"https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22\"" "pinned Elements runtime"
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
