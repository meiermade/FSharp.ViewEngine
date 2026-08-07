module Docs.Tests.Program

open System.Net
open Expecto
open FSharp.ViewEngine
open Docs.Common
open Docs.Pages

let private expectedPaths =
    set [
        "/"
        "/installation"
        "/custom"
        "/usage"
        "/extensions/alpine"
        "/extensions/datastar"
        "/extensions/htmx"
        "/extensions/svg"
        "/extensions/tailwind-elements"
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
        }

        test "Navigation keeps extensions and project pages in the documented order" {
            let section label =
                Registry.navigation
                |> List.find (fun candidate -> candidate.label = label)
                |> _.pages
                |> List.map _.navLabel

            Expect.sequenceEqual
                (section "Extensions")
                [ "SVG"; "Datastar"; "HTMX"; "Alpine"; "Tailwind Plus Elements" ]
                "extension order"
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
            Expect.stringContains html "<table" "semantic results table"
            Expect.stringContains html "./fake.sh Benchmark" "measurement command"
            Expect.stringContains html "./fake.sh BenchmarkSmoke" "validation command"

            let appendixIndex = html.IndexOf("Appendix: Detailed Results", System.StringComparison.Ordinal)
            let firstTableIndex = html.IndexOf("<table", System.StringComparison.Ordinal)
            Expect.isGreaterThan appendixIndex 0 "appendix heading"
            Expect.isGreaterThan firstTableIndex appendixIndex "detailed tables follow analysis"
        }

        test "Every page renders its eyebrow and heading anchors" {
            for page in Registry.all do
                let html = page |> View.document Registry.navigation |> Render.toHtmlDocString
                let encodedTitle = WebUtility.HtmlEncode page.title
                Expect.stringContains html page.category $"{page.path} eyebrow"
                Expect.stringContains html $">{encodedTitle}</h1>" $"{page.path} title"

                for heading in DocPage.headings page do
                    Expect.stringContains html $"id=\"{heading.id}\"" $"{page.path} heading {heading.id}"
                    if heading.level <= 3 then
                        Expect.stringContains html $"href=\"#{heading.id}\"" $"{page.path} TOC {heading.id}"
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
            let datastarHtml = Datastar.page |> View.document Registry.navigation |> Render.toHtmlDocString

            Expect.stringContains customHtml "Trusted Content Boundaries" "trusted-content guidance"
            Expect.stringContains usageHtml "titleBuilder" "title builder guidance"
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

        test "Homepage quick example is Datastar-first" {
            let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "open type Datastar" "Datastar API"
            Expect.stringContains html "_dataOn" "Datastar interaction"
            Expect.isFalse (html.Contains("open type Htmx")) "HTMX is not used by the quick example"
            Expect.isFalse (html.Contains("_hxGet")) "HTMX is not used as the attribute example"
        }

        test "Docs use the pinned Prism runtime and FSharp grammar" {
            let html = Home.page |> View.document Registry.navigation |> Render.toHtmlDocString
            let prismBase = "https://cdnjs.cloudflare.com/ajax/libs/prism/1.30.0"
            Expect.stringContains html $"{prismBase}/prism.min.js" "pinned Prism script"
            Expect.stringContains html $"{prismBase}/themes/prism-tomorrow.min.css" "pinned Prism theme"
            Expect.stringContains html $"{prismBase}/components/prism-fsharp.min.js" "pinned FSharp grammar"
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
            Expect.stringContains html "open type TailwindElements" "current API name"
            Expect.isFalse (html.Contains("open type Tailwind\n")) "removed Tailwind API"
        }

        test "Removed Tailwind documentation route is not registered" {
            Expect.isFalse (Registry.all |> List.exists (fun page -> page.path = "/extensions/tailwind")) "old canonical route"
            Expect.isFalse (Registry.aliases |> List.exists (fun (alias, _) -> alias = "/extensions/tailwind")) "old route alias"
        }

        test "Changelog identifies unreleased breaking changes and migrations" {
            let html = Changelog.page |> View.document Registry.navigation |> Render.toHtmlDocString
            Expect.stringContains html "Unreleased" "unreleased section"
            Expect.stringContains html "Breaking:" "breaking change marker"
            Expect.stringContains html "_dataBind" "removed data-bind overload"
            Expect.stringContains html "_dataAnimate" "changed data-animate signature"
            Expect.stringContains html "_dataRocket" "removed legacy helper"
            Expect.stringContains html "_dataScrollIntoView ()" "presence-only migration"
            Expect.stringContains html "Alpine Migration" "Alpine migration section"
            Expect.stringContains html "_by" "removed Alpine helper"
            Expect.stringContains html "_xModel" "Alpine modifier migration"
            Expect.stringContains html "Tailwind Plus Elements Migration" "Tailwind migration section"
            Expect.stringContains html "TailwindElements" "renamed Tailwind type"
            Expect.stringContains html "public API" "package compatibility validation"
            Expect.stringContains html "Source Link" "portable source-debugging symbols"
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
