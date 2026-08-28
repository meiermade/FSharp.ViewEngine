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
        "/components/radio-group"
        "/components/dropdown-menu"
        "/components/dialog"
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
            Expect.sequenceEqual (section "Menus and overlays") [ "Dropdown menu"; "Dialog" ] "menu and overlay components"
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
                @ Components.menuOverlayRegistrations
                @ Components.compositionRegistrations
            let renderedComponents = componentRegistrations |> List.map render
            let renderedGuides = Components.guideRegistrations |> List.map render
            let allHtml = String.concat Environment.NewLine (overview :: installation :: renderedComponents @ renderedGuides)

            Expect.equal Components.allRegistrations.Length 30 "overview, installation, twenty-two components, and six guides"
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
                "PaginationItem.current 2"
                "Chart.create &quot;operating-balance&quot;"
                "Chart.empty &quot;new-account-balance&quot;"
                "Select.create &quot;status&quot; &quot;Status&quot; statusValue statusOptions"
                "Combobox.create &quot;account&quot; &quot;Parent account&quot; string"
                "Combobox.withSearch (ComboboxSearch.Remote &quot;/components/accounts/search&quot;)"
                "Combobox.renderOptions"
                "Checkbox.create &quot;includeArchived&quot; &quot;Include archived accounts&quot;"
                "Switch.create &quot;postingNotifications&quot; &quot;Posting notifications&quot;"
                "ToggleButton.create &quot;components-compact-rows&quot; &quot;Compact rows&quot;"
                "RadioGroup.create &quot;postingMode&quot; &quot;Posting mode&quot; id"
                "DropdownMenu.create &quot;components-menu-actions&quot; &quot;Actions&quot;"
                "Dialog.create &quot;review-account-dialog&quot; &quot;Review account&quot;"
                "Dialog.withInitialFocus &quot;review-account-dialog-close&quot;"
                "Dialog.trigger &quot;Review account&quot;"
                "Dialog.closeButton &quot;Close&quot;"
                "Collection.create &quot;Accounts&quot; accountTable"
                "Detail.create &quot;Operating&quot;"
                "AppShell.create &quot;Ledger&quot; Accounts" ] do
                Expect.stringContains allHtml source source

            Expect.stringContains allHtml "data-signals=\"{_account_open: false, account_query:" "remote Combobox emits local open state and an intentionally submitted query"
            Expect.stringContains allHtml "role=\"switch\"" "Switch preserves switch semantics"
            Expect.stringContains allHtml "aria-pressed=\"true\"" "ToggleButton preserves pressed semantics"
            Expect.stringContains allHtml "type=\"radio\"" "RadioGroup preserves form semantics internally"
            Expect.isFalse (allHtml.Contains("NativeSelect.create")) "the package exposes no NativeSelect API"
            Expect.stringContains allHtml "id=\"review-account-dialog-trigger\"" "Dialog renders its connected trigger"
            Expect.stringContains allHtml "data-on:close=\"document.getElementById(&quot;review-account-dialog-trigger&quot;).focus()\"" "Dialog close restores trigger focus"
            Expect.isFalse (allHtml.Contains("Select.describe")) "Select has no unobservable option-description modifier"
            Expect.stringContains allHtml "data-signals=\"{_components_menu_actions_open: false}" "menu IDs become valid local signal tokens"
            Expect.isFalse (allHtml.Contains("_components-menu-actions-open")) "DOM IDs are not copied unsafely into expressions"
            Expect.stringContains allHtml "aria-current=\"page\"" "AppShell retains typed current destination"
            Expect.stringContains allHtml "--fve-brand-solid" "consumer theme overrides are documented"
            Expect.stringContains overview "rel=\"prev\" href=\"/extensions/tailwind-elements\"" "Components follows integrations"
            Expect.stringContains overview "rel=\"next\" href=\"/components/installation\"" "overview continues to installation"
            let versioning = render Components.versioningRegistration
            Expect.stringContains versioning "rel=\"prev\" href=\"/components/customization\"" "last guide follows customization"
            Expect.stringContains versioning "rel=\"next\" href=\"/docs\"" "Components precedes the specialized Docs toolkit"
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

        test "Components Select owns branded presentation instead of native browser chrome" {
            let html =
                Select.create "status" "Status" id [ Select.option "active" "Active" ]
                |> Select.withSelected "active"
                |> Select.render
                |> Render.toString

            Expect.isFalse (html.Contains("<select")) "Select never renders a native select element"
            Expect.stringContains html "role=\"combobox\"" "Select renders a select-only combobox trigger"
            Expect.stringContains html "role=\"listbox\"" "Select renders its branded listbox"
            Expect.stringContains html "data-attr:aria-activedescendant" "Select synchronizes its active descendant"
            Expect.stringContains html "_status_typeahead" "Select retains bounded typeahead state"
            Expect.stringContains html "Date.now()" "Select resets typeahead after its bounded interval"
            Expect.stringContains html "every(character =&gt; character == $_status_typeahead[0])" "Select cycles repeated-character matches"
            Expect.stringContains html "type=\"hidden\"" "Select retains ordinary form submission"
        }

        test "Components branded choice controls preserve distinct semantics" {
            let comboboxHtml =
                Combobox.create "account" "Account" id [ Select.option "operating" "Operating" ]
                |> Combobox.withSelected "operating"
                |> Combobox.render
                |> Render.toString
            Expect.stringContains comboboxHtml "type=\"search\" role=\"combobox\"" "Combobox exposes an editable combobox"
            Expect.stringContains comboboxHtml "aria-autocomplete=\"list\"" "Combobox identifies list autocomplete"
            Expect.stringContains comboboxHtml "data-attr:aria-activedescendant" "Combobox synchronizes its active descendant"
            Expect.stringContains comboboxHtml "data-init=\"queueMicrotask" "Combobox repairs active identity after option morphs"
            Expect.stringContains comboboxHtml "type=\"hidden\" name=\"account\"" "Combobox submits selected identity separately from query text"
            Expect.stringContains comboboxHtml "role=\"option\"" "Combobox renders branded options"

            let checkboxHtml = Checkbox.create "archived" "Include archived" |> Checkbox.withChecked |> Checkbox.render |> Render.toString
            Expect.stringContains checkboxHtml "type=\"checkbox\"" "Checkbox retains native checkbox semantics internally"
            Expect.stringContains checkboxHtml "class=\"peer sr-only\"" "Checkbox browser chrome is visually hidden"

            let switchHtml = Switch.create "notifications" "Notifications" |> Switch.withChecked |> Switch.render |> Render.toString
            Expect.stringContains switchHtml "role=\"switch\"" "Switch has switch semantics"
            Expect.stringContains switchHtml "data-attr:aria-checked" "Switch state remains synchronized"
            Expect.stringContains switchHtml "class=\"peer sr-only\"" "Switch browser chrome is visually hidden"

            let toggleHtml = ToggleButton.create "compact" "Compact rows" |> ToggleButton.pressed |> ToggleButton.render |> Render.toString
            Expect.stringContains toggleHtml "aria-pressed=\"true\"" "ToggleButton has pressed semantics"
            Expect.stringContains toggleHtml "data-attr:aria-pressed" "ToggleButton pressed state remains synchronized"

            let radioHtml =
                RadioGroup.create "mode" "Mode" id [ RadioGroup.option "automatic" "Automatic"; RadioGroup.option "manual" "Manual" ]
                |> RadioGroup.withSelected "automatic"
                |> RadioGroup.render
                |> Render.toString
            Expect.equal (Regex.Matches(radioHtml, "type=\"radio\"").Count) 2 "RadioGroup retains one radio input per option"
            Expect.equal (Regex.Matches(radioHtml, "class=\"peer sr-only\"").Count) 2 "Radio browser chrome is visually hidden"
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
            Expect.stringContains verification ".lg\\:grid-cols-3" "verification checks responsive detail columns"
            Expect.stringContains verification ".size-9" "verification checks pagination sizing"
            Expect.stringContains verification ".acme-theme" "verification checks consumer CSS"
            Expect.stringContains docsStyles ".docs-components-preview .fve-components" "Docs owns the example theme adapter"
            Expect.stringContains docsStyles "--fve-page: var(--spec-bg)" "component examples inherit the Docs page surface"
            Expect.stringContains docsStyles "--fve-brand-solid: var(--spec-accent-500)" "component examples inherit the Docs sky accent"
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
