namespace Docs.Web

open System
open Docs.Common
open Docs.Pages
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Documentation
open type Html

module View =
    let rec private renderInline (content:InlineContent) =
        match content with
        | Text value -> text value
        | Strong children -> strong { for child in children do renderInline child }
        | InlineContent.Code value -> code { value }
        | Link(label, href) -> a { _href href; label }

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

    let private element (node:DocNode) =
        match node with
        | DocNode.Paragraph children -> p { for child in children do renderInline child }
        | DocNode.UnorderedList items ->
            ul { for item in items do li { for child in item do renderInline child } }
        | DocNode.OrderedList items ->
            ol { for item in items do li { for child in item do renderInline child } }
        | DocNode.BarChart chart -> comparisonChart chart
        | DocNode.DataTable(headers, rows) ->
            div {
                _class "overflow-x-auto rounded-xl border border-[var(--fve-border)]"
                table {
                    thead { tr { for header in headers do th { header } } }
                    tbody { for row in rows do tr { for cell in row do td { cell } } }
                }
            }
        | DocNode.CodeBlock(language, source) -> CodeBlock.create language source |> CodeBlock.render
        | DocNode.Example(id, label, language, source, preview) -> Example.codeFirst id label language source preview
        | DocNode.Heading _ -> invalidOp "Headings are converted to documentation sections."

    let private sections (nodes:DocNode list) : DocsSection list =
        let flush isDeclared title id level content sections =
            if not isDeclared && List.isEmpty content then sections
            else
                { id = id; title = title; level = level; content = List.rev content }
                :: sections

        let rec loop isDeclared title id level content sections remaining =
            match remaining with
            | [] -> flush isDeclared title id level content sections |> List.rev
            | DocNode.Heading heading :: tail ->
                let sections = flush isDeclared title id level content sections
                loop true heading.title heading.id heading.level [] sections tail
            | node :: tail -> loop isDeclared title id level (element node :: content) sections tail

        loop false "Overview" "overview" 2 [] [] nodes

    let private slug (value:string) =
        value.ToLowerInvariant()
        |> Seq.map (fun character -> if Char.IsLetterOrDigit character then character else '-')
        |> Seq.toArray
        |> String

    let private navigation (sections:NavSection list) =
        let rec group parentId (section:NavSection) =
            let id = if String.IsNullOrEmpty parentId then slug section.label else parentId + "-" + slug section.label
            let pages =
                section.pages
                |> List.map (fun page -> Nav.page page.id page.navLabel page.path page.path)

            let groups = section.sections |> List.map (group id)

            Nav.group
                id
                section.label
                (section.label = "Getting started")
                (pages @ groups)

        sections |> List.map (group "")

    let private assets =
        { DocsAssets.defaults with
            productStylesheets = [ "/css/output.css" ]
            additionalHead =
                [ link { _rel "icon"; _href "/favicon.svg"; _type "image/svg+xml" }
                  link { _rel "manifest"; _href "/site.webmanifest" }
                  script { _src "/scripts/tailwind-elements-loader.1.0.22.js"; _type "module" } ] }

    let private site (sections:NavSection list) search : DocsSite<string> =
        { name = "FSharp.ViewEngine"
          baseUrl = Some "https://fve.meiermade.com"
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
            DocumentationPage.create page.id page.title
            |> DocumentationPage.withDescription description
            |> DocumentationPage.withSections (sections page.nodes)
            |> DocumentationPage.withMetadata {
                DocsPageMetadata.defaults with
                    browserTitle = Some page.browserTitle
                    socialImage = Some "https://fve.meiermade.com/social-card.png" }
        if page.id = "home" then
            rendered
            |> DocumentationPage.withHeadingAdornment (
                div {
                    _class "docs-home-logo"
                    img { _src "/logo.svg"; _alt "" }
                })
        else rendered

    let private registeredPages (navigation:NavSection list) =
        let rec sectionPages section = section.pages @ (section.sections |> List.collect sectionPages)
        navigation |> List.collect sectionPages

    let private pager navigation activeId =
        let pages = registeredPages navigation
        let linkAt index =
            if index < 0 || index >= pages.Length then None
            else
                let page = pages[index]
                let label = if page.navLabel = "Overview" then page.title else page.navLabel
                Some(DocsPageLink.create label page.path)
        pages
        |> List.tryFindIndex (fun page -> page.id = activeId)
        |> Option.map (fun index -> DocsPager.create (linkAt (index - 1)) (linkAt (index + 1)))

    let private resolvePage (page:DocPage) =
        Catalog.tryPage page.path
        |> Option.orElseWith (fun () -> Components.tryPage page.path)
        |> Option.orElseWith (fun () -> Showcase.tryPage page.path)
        |> Option.defaultWith (fun () -> legacyPage page)

    let private renderResolvedPage (appMode:AppMode option) (sections:NavSection list) (registration:DocPage) (docsPage:DocsPage) =
        let search =
            registeredPages sections
            |> List.map (fun (page:DocPage) ->
                DocsSearchEntry.create page.path (resolvePage page) [ page.category; page.navLabel ])
            |> DocsSearch.index
        let docsPage =
            docsPage
            |> DocumentationPage.withMetadata {
                docsPage.metadata with
                    socialImage = Some "https://fve.meiermade.com/social-card.png" }
        let docsPage = pager sections registration.id |> Option.map (fun value -> DocumentationPage.withPager value docsPage) |> Option.defaultValue docsPage
        let site = site sections search
        let sideNavItems = navigation sections
        let document =
            Document.create site docsPage
            |> Document.withBreadcrumbs (Navigation.breadcrumbs sideNavItems site.homeId docsPage.activeId)
            |> Document.withSideNavItems sideNavItems
        appMode
        |> Option.map (fun mode -> Document.withAppMode mode document)
        |> Option.defaultValue document
        |> Document.render

    let renderPage sections registration =
        renderResolvedPage None sections registration (resolvePage registration)

    let document sections page = renderPage sections page
    let documentFor appMode sections page = renderResolvedPage appMode sections page (resolvePage page)
    let documentWithPage sections registration docsPage = renderResolvedPage None sections registration docsPage
    let documentWithPageFor appMode sections registration docsPage = renderResolvedPage appMode sections registration docsPage
