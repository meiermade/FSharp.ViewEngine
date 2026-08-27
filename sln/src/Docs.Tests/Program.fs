module Docs.Tests.Program

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
                [ "Getting started"; "Core concepts"; "Integrations"; "FSharp.ViewEngine.Docs"; "FSharp.ViewEngine.Components"; "Project" ]
                "core guidance precedes Components and project pages"

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
            Expect.sequenceEqual (section "FSharp.ViewEngine.Components") [ "Overview" ] "Components documentation is first-class"
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

                if Showcase.tryPage page.path |> Option.isNone then
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

        test "Components publishes consumer-facing documentation with executable examples" {
            let html = Components.page |> View.document Registry.navigation |> Render.toHtmlDocString

            Expect.stringContains html "provides accessible, server-rendered components" "consumer-facing introduction"
            Expect.stringContains html "Using Components" "usage guidance"
            Expect.stringContains html "Required inputs stay visible" "required input policy"
            Expect.stringContains html "Optional behavior is piped" "configuration policy"
            Expect.stringContains html "Custom content stays HTML" "slot policy"
            Expect.stringContains html "Closed choices are typed" "typed variant policy"
            for heading in [ "Actions and feedback"; "Data display"; "Form controls"; "Menus and overlays"; "Compositions" ] do
                Expect.stringContains html heading $"component category {heading}"
            Expect.stringContains html "Interaction and server state" "interaction guide"
            Expect.stringContains html "Theming and density" "theme guide"
            Expect.stringContains html "Tailwind CSS setup" "Tailwind setup guide"
            Expect.stringContains html "Application responsibilities" "application boundary guide"
            Expect.stringContains html "explicit Tailwind v4 source manifest" "Tailwind source manifest"
            Expect.stringContains html "versions independently" "version policy"
            Expect.stringContains html "minimum compatible FSharp.ViewEngine version" "Core compatibility policy"
            Expect.isFalse (html.Contains("Pre-release contract")) "release-process framing is absent"
            Expect.isFalse (html.Contains("Compiled Call Sites")) "examples are not framed as implementation evidence"
            Expect.isFalse (html.Contains("package-spine task")) "internal task language is absent"
            Expect.isFalse (html.Contains("veSelect {")) "Components does not introduce a component CE"
            Expect.isFalse (html.Contains("color &quot;emerald-600&quot;")) "ordinary API does not accept raw palette strings"

            let examples = html.Split([| "data-docs-example=\"true\"" |], System.StringSplitOptions.None).Length - 1
            Expect.equal examples 7 "component categories use code-first examples"
            for source in [
                "Button.primary &quot;Create account&quot;"
                "Table.create &quot;Accounts&quot;"
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
                Expect.stringContains html source source

            Expect.stringContains html "data-signals=\"{_account_open: false, account_query:" "remote Combobox emits local open state and an intentionally submitted query"
            Expect.stringContains html "role=\"switch\"" "Switch preserves switch semantics"
            Expect.stringContains html "aria-pressed=\"true\"" "ToggleButton preserves pressed semantics"
            Expect.stringContains html "type=\"radio\"" "RadioGroup preserves form semantics internally"
            Expect.isFalse (html.Contains("NativeSelect.create")) "the package exposes no NativeSelect API"
            Expect.stringContains html "id=\"review-account-dialog-trigger\"" "Dialog renders its connected trigger"
            Expect.stringContains html "data-on:close=\"document.getElementById(&quot;review-account-dialog-trigger&quot;).focus()\"" "Dialog close restores trigger focus"
            Expect.isFalse (html.Contains("Select.describe")) "Select has no unobservable option-description modifier"
            Expect.stringContains html "data-signals=\"{_components_menu_actions_open: false}" "menu IDs become valid local signal tokens"
            Expect.isFalse (html.Contains("_components-menu-actions-open")) "DOM IDs are not copied unsafely into expressions"
            Expect.stringContains html "aria-current=\"page\"" "AppShell retains typed current destination"
            Expect.stringContains html "--fve-brand-solid" "consumer theme overrides are documented"
            Expect.stringContains html "rel=\"prev\" href=\"/docs/page-examples/executable-specification\"" "Components follows executable Specs"
            Expect.stringContains html "rel=\"next\" href=\"/benchmarks\"" "Components precedes project evidence"
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
                |> Table.withAttributes [ _class "override" ]
                |> Table.render
                |> openingTag "table"
            Expect.equal (attributeCount "class" tableTag) 1 "Table owns one class attribute"
            Expect.isFalse (tableTag.Contains("override")) "Table consumer class is ignored"

            let selectHtml =
                Select.create "status" "Status" id [ Select.option "active" "Active" ]
                |> Select.withAttributes [
                    _id "other-id"
                    _name "other-name"
                    _dataBind "other"
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
            for rejected in [ "other-id"; "other-name"; "data-bind:other"; "override" ] do
                Expect.isFalse (selectTrigger.Contains(rejected)) $"Select rejects reserved trigger attribute value {rejected}"

            let comboboxTag =
                Combobox.create "account" "Account" id [ Select.option "operating" "Operating" ]
                |> Combobox.withAttributes [
                    _id "other-id"
                    _name "other-name"
                    _dataBind "other"
                    _ariaInvalid false
                    _class "override" ]
                |> Combobox.render
                |> openingTag "input"
            for name in [ "id"; "type"; "role"; "aria-invalid"; "class" ] do
                Expect.equal (attributeCount name comboboxTag) 1 $"Combobox owns one input {name}"
            Expect.equal (Regex.Matches(comboboxTag, "\\sdata-bind:[^=\\s>]+(?:=|\\s|>)", RegexOptions.IgnoreCase).Count) 1 "Combobox owns one query binding"
            for rejected in [ "other-id"; "other-name"; "data-bind:other"; "override" ] do
                Expect.isFalse (comboboxTag.Contains(rejected)) $"Combobox rejects reserved input attribute value {rejected}"
        }

        test "Components Tailwind contract is isolated and CI-proven" {
            let contractDirectory = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "Docs", "ComponentsContract"))
            let manifest = File.ReadAllText(Path.Combine(contractDirectory, "FSharp.ViewEngine.Components.tailwind.css"))
            let consumer = File.ReadAllText(Path.Combine(contractDirectory, "consumer.css"))
            let verification = File.ReadAllText(Path.Combine(contractDirectory, "verify-tailwind.sh"))
            let renderer = File.ReadAllText(Path.Combine(contractDirectory, "..", "src", "ComponentsContract.fs"))
            let dockerfile = File.ReadAllText(Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "Dockerfile")))

            Expect.stringContains manifest "@source inline(" "package classes use an explicit source manifest"
            Expect.stringContains manifest "bg-[var(--fve-brand-solid)]" "semantic brand utility is forced"
            Expect.stringContains manifest ".fve-components" "semantic defaults ship with the manifest"
            Expect.stringContains manifest ".dark .fve-components" "dark defaults ship with the manifest"
            Expect.stringContains manifest ".dark .fve-theme-sky" "Sky ships theme-specific dark brand roles"
            Expect.stringContains manifest ".dark .fve-theme-emerald" "Emerald ships theme-specific dark brand roles"
            Expect.stringContains renderer "py-[var(--fve-control-padding-block)]" "renderers consume the semantic density token"
            for role in [ "subtle"; "solid"; "hover"; "text"; "ring" ] do
                Expect.isGreaterThanOrEqual
                    (Regex.Matches(manifest, $"--fve-brand-{role}:").Count)
                    4
                    $"light and dark theme definitions include brand {role}"
            Expect.stringContains consumer "@import \"tailwindcss\" source(none)" "fixture disables automatic source scanning"
            Expect.stringContains consumer "@import \"./FSharp.ViewEngine.Components.tailwind.css\"" "clean consumer imports only the contract"
            Expect.stringContains consumer ".acme-theme" "consumer override is independent"
            Expect.stringContains verification ".bg-\\[var\\(--fve-brand-solid\\)\\]" "verification checks generated package utility"
            Expect.stringContains verification ".acme-theme" "verification checks consumer CSS"
            Expect.stringContains dockerfile "ComponentsContract/verify-tailwind.sh" "container CI executes the clean-consumer proof"

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
