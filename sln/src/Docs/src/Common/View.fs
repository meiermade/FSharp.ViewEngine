namespace Docs.Common

open System
open Docs.Pages
open FSharp.ViewEngine
open FSharp.ViewEngine.Docs
open type Html

module View =
    let rec private renderInline (content:InlineContent) =
        match content with
        | Text value -> text value
        | Strong children -> strong { for child in children do renderInline child }
        | InlineContent.Code value -> code { value }
        | Link(label, href) -> a { _href href; _class "spec-content-link"; label }

    let private comparisonChart (chart:ComparisonChart) =
        figure {
            _ariaLabel chart.label
            _class "docs-comparison-chart"
            figcaption {
                strong { chart.title }
                p { chart.description }
                span { _class "docs-comparison-direction"; "Mean duration · Lower is better" }
            }
            div {
                _class "docs-comparison-bars"
                for bar in chart.bars do
                    div {
                        _class "docs-comparison-row"
                        div {
                            _class "docs-comparison-labels"
                            strong { bar.label }
                            span { _class "docs-comparison-value"; bar.duration }
                        }
                        div {
                            _class "docs-comparison-track"
                            _ariaHidden true
                            div {
                                _class (if bar.highlighted then "docs-comparison-bar docs-comparison-bar-highlighted" else "docs-comparison-bar")
                                _style ("width:" + string (Math.Clamp(bar.widthPercent, 0, 100)) + "%")
                            }
                        }
                        span { _class "docs-comparison-note"; bar.comparison }
                    }
            }
        }

    let private block (node:DocNode) =
        match node with
        | DocNode.Paragraph children -> docsCustom (p { _class "spec-paragraph"; for child in children do renderInline child })
        | DocNode.UnorderedList items ->
            docsCustom (ul { _class "spec-bullets"; for item in items do li { for child in item do renderInline child } })
        | DocNode.OrderedList items ->
            docsCustom (ol { _class "spec-bullets"; for item in items do li { for child in item do renderInline child } })
        | DocNode.BarChart chart -> docsCustom (comparisonChart chart)
        | DocNode.DataTable(headers, rows) -> docsTable headers rows
        | DocNode.CodeBlock(language, source) -> docsCode language source
        | DocNode.Example(id, label, language, source, preview) -> docsCustom (docsExample id label language source preview)
        | DocNode.Heading _ -> invalidOp "Headings are converted to documentation sections."

    let private sections (nodes:DocNode list) : DocsSection list =
        let flush isDeclared title id level blocks sections =
            if not isDeclared && List.isEmpty blocks then sections
            else
                { id = id; title = title; level = level; blocks = List.rev blocks }
                :: sections

        let rec loop isDeclared title id level blocks sections remaining =
            match remaining with
            | [] -> flush isDeclared title id level blocks sections |> List.rev
            | DocNode.Heading heading :: tail ->
                let sections = flush isDeclared title id level blocks sections
                loop true heading.title heading.id heading.level [] sections tail
            | node :: tail -> loop isDeclared title id level (block node :: blocks) sections tail

        loop false "Overview" "overview" 2 [] [] nodes

    let private slug (value:string) =
        value.ToLowerInvariant()
        |> Seq.map (fun character -> if Char.IsLetterOrDigit character then character else '-')
        |> Seq.toArray
        |> String

    let private navigation (sections:NavSection list) =
        let rec group (section:NavSection) =
            let pages =
                section.pages
                |> List.map (fun page -> docsNavPage page.id page.navLabel page.path page.path)

            let groups = section.sections |> List.map group

            docsNavGroup
                (slug section.label)
                section.label
                (section.label = "Getting started")
                (pages @ groups)

        sections |> List.map group

    let private assets =
        { DocsAssets.defaults with
            productStylesheets = [ "/css/output.css" ]
            additionalHead =
                [ link { _rel "icon"; _href "/favicon.svg"; _type "image/svg+xml" }
                  link { _rel "manifest"; _href "/site.webmanifest" }
                  script { _src "https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22"; _type "module" } ] }

    let private site (sections:NavSection list) search : DocsSite<string> =
        { name = "FSharp.ViewEngine"
          baseUrl = Some "https://fsharpviewengine.meiermade.com"
          description = Some "Documentation, API reference, and executable specifications for FSharp.ViewEngine."
          repository = Some(DocsRepository.github "https://github.com/meiermade/FSharp.ViewEngine")
          brandMark = img { _src "/logo.svg"; _alt "" }
          homeId = "home"
          navigation = navigation sections
          storageKey = "fsharp-viewengine-docs-navigation"
          defaultColorMode = DocsColorMode.System
          theme = DocsTheme.sky
          assets = assets
          search = search }

    let private inlineText content =
        let rec collect = function
            | Text value -> value
            | Strong children -> children |> List.map collect |> String.concat ""
            | InlineContent.Code value -> value
            | Link(label, _) -> label
        content |> List.map collect |> String.concat ""

    let private legacyPage (page:DocPage) =
        let description =
            page.nodes
            |> List.tryPick (function | DocNode.Paragraph content -> Some(inlineText content) | _ -> None)
            |> Option.defaultValue page.title
        let rendered =
            docsArticle page.id page.title description (sections page.nodes)
            |> docsWithMetadata {
                DocsPageMetadata.defaults with
                    browserTitle = Some page.browserTitle
                    socialImage = Some "https://fsharpviewengine.meiermade.com/social-card.png" }
        if page.id = "home" then
            rendered
            |> docsWithHeadingAdornment (
                div {
                    _class "docs-home-logo"
                    img { _src "/logo.svg"; _alt "" }
                })
        else rendered

    let private pageLink label href = Some(docsPageLink label href)

    let private componentsPager activeId =
        Components.allRegistrations
        |> List.tryFindIndex (fun page -> page.id = activeId)
        |> Option.map (fun index ->
            let previous =
                if index = 0 then pageLink "Tailwind Plus Elements" "/extensions/tailwind-elements"
                else
                    let page = Components.allRegistrations[index - 1]
                    pageLink page.navLabel page.path
            let next =
                if index = Components.allRegistrations.Length - 1 then pageLink "FSharp.ViewEngine.Docs" "/docs"
                else
                    let page = Components.allRegistrations[index + 1]
                    pageLink page.navLabel page.path
            docsPager previous next)

    let private pager activeId =
        match activeId with
        | "home" -> Some(docsPager None (pageLink "Installation" "/installation"))
        | "installation" -> Some(docsPager (pageLink "Introduction" "/") (pageLink "Build your first view" "/getting-started/first-view"))
        | "first-view" -> Some(docsPager (pageLink "Installation" "/installation") (pageLink "Elements and attributes" "/guides/elements-and-attributes"))
        | "elements-and-attributes" -> Some(docsPager (pageLink "Build your first view" "/getting-started/first-view") (pageLink "Composition and control flow" "/guides/composition-and-control-flow"))
        | "composition-control-flow" -> Some(docsPager (pageLink "Elements and attributes" "/guides/elements-and-attributes") (pageLink "Rendering" "/guides/rendering"))
        | "rendering" -> Some(docsPager (pageLink "Composition and control flow" "/guides/composition-and-control-flow") (pageLink "Encoding and trusted content" "/guides/encoding-and-trusted-content"))
        | "encoding" -> Some(docsPager (pageLink "Rendering" "/guides/rendering") (pageLink "Accessibility" "/guides/accessibility"))
        | "accessibility" -> Some(docsPager (pageLink "Encoding and trusted content" "/guides/encoding-and-trusted-content") (pageLink "Custom elements and extensions" "/custom"))
        | "custom" -> Some(docsPager (pageLink "Accessibility" "/guides/accessibility") (pageLink "Giraffe" "/usage"))
        | "usage" -> Some(docsPager (pageLink "Custom elements and extensions" "/custom") (pageLink "SVG" "/extensions/svg"))
        | "svg" -> Some(docsPager (pageLink "Giraffe" "/usage") (pageLink "Datastar" "/extensions/datastar"))
        | "datastar" -> Some(docsPager (pageLink "SVG" "/extensions/svg") (pageLink "HTMX" "/extensions/htmx"))
        | "htmx" -> Some(docsPager (pageLink "Datastar" "/extensions/datastar") (pageLink "Alpine" "/extensions/alpine"))
        | "alpine" -> Some(docsPager (pageLink "HTMX" "/extensions/htmx") (pageLink "Tailwind Plus Elements" "/extensions/tailwind-elements"))
        | "tailwind-elements" -> Some(docsPager (pageLink "Alpine" "/extensions/alpine") (pageLink "Components" "/components"))
        | "docs-overview" -> Some(docsPager (pageLink "Versioning" "/components/versioning") (pageLink "Layouts" "/docs/components/layouts"))
        | "docs-layouts" -> Some(docsPager (pageLink "Overview" "/docs") (pageLink "Content" "/docs/components/content"))
        | "docs-content" -> Some(docsPager (pageLink "Layouts" "/docs/components/layouts") (pageLink "Navigation" "/docs/components/navigation"))
        | "docs-navigation" -> Some(docsPager (pageLink "Content" "/docs/components/content") (pageLink "Interactive examples" "/docs/components/interactive-examples"))
        | "docs-interactive" -> Some(docsPager (pageLink "Navigation" "/docs/components/navigation") (pageLink "API reference components" "/docs/components/api-reference"))
        | "docs-api-components" -> Some(docsPager (pageLink "Interactive examples" "/docs/components/interactive-examples") (pageLink "Diagrams" "/docs/components/diagrams"))
        | "docs-diagrams" -> Some(docsPager (pageLink "API reference components" "/docs/components/api-reference") (pageLink "Documentation site" "/docs/page-examples/documentation-site"))
        | "docs-page-documentation-site" -> Some(docsPager (pageLink "Diagrams" "/docs/components/diagrams") (pageLink "API reference page" "/docs/page-examples/api-reference"))
        | "docs-page-api-reference" -> Some(docsPager (pageLink "Documentation site" "/docs/page-examples/documentation-site") (pageLink "Executable specification page" "/docs/page-examples/executable-specification"))
        | "docs-page-executable-specification" -> Some(docsPager (pageLink "API reference page" "/docs/page-examples/api-reference") (pageLink "Benchmarks" "/benchmarks"))
        | componentId when componentId.StartsWith("components-", StringComparison.Ordinal) -> componentsPager componentId
        | "benchmarks" -> Some(docsPager (pageLink "Executable specification page" "/docs/page-examples/executable-specification") (pageLink "Changelog" "/changelog"))
        | "changelog" -> Some(docsPager (pageLink "Benchmarks" "/benchmarks") None)
        | _ -> None

    let renderPage (navigation:NavSection list) (legacy:DocPage) =
        let registered =
            let rec sectionPages section = section.pages @ (section.sections |> List.collect sectionPages)
            navigation |> List.collect sectionPages
        let resolve (page:DocPage) =
            Components.tryPage page.path
            |> Option.orElseWith (fun () -> Showcase.tryPage page.path)
            |> Option.defaultWith (fun () -> legacyPage page)
        let search =
            registered
            |> List.map (fun (page:DocPage) ->
                docsSearchEntry page.path (resolve page) [ page.category; page.navLabel ])
            |> DocsSearch.index
        let docsPage = resolve legacy
        let docsPage =
            docsPage
            |> docsWithMetadata {
                docsPage.metadata with
                    socialImage = Some "https://fsharpviewengine.meiermade.com/social-card.png" }
        let docsPage = pager legacy.id |> Option.map (fun value -> docsWithPager value docsPage) |> Option.defaultValue docsPage
        docsDocument (site navigation search) docsPage

    let document navigation page = renderPage navigation page
