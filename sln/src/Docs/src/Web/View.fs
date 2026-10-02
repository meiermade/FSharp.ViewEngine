namespace Docs.Web

open System
open Docs.Common
open Docs.Pages
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Templates
open FSharp.ViewEngine.Components
open type Html

module View =
    let rec private renderInline (content:InlineContent) =
        match content with
        | Text value -> text value
        | Strong children -> strong { for child in children do renderInline child }
        | InlineContent.Code value -> code { _class "whitespace-normal [overflow-wrap:anywhere]"; value }
        | InlineContent.Link(label, href) -> a { _href href; _class "font-semibold text-[var(--fve-brand-text)] underline decoration-[var(--fve-brand-ring)] underline-offset-[0.18em] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"; label }

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
        | DocNode.Paragraph children -> p { _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)] [overflow-wrap:anywhere]"; for child in children do renderInline child }
        | DocNode.UnorderedList items ->
            ul { _class "m-0 list-disc pl-5 text-base leading-relaxed text-[var(--fve-muted-text)] marker:text-[var(--fve-brand-ring)]"; for item in items do li { _class "mt-2 first:mt-0"; for child in item do renderInline child } }
        | DocNode.OrderedList items ->
            ol { _class "m-0 list-decimal pl-5 text-base leading-relaxed text-[var(--fve-muted-text)] marker:text-[var(--fve-brand-ring)]"; for item in items do li { _class "mt-2 first:mt-0"; for child in item do renderInline child } }
        | DocNode.BarChart chart -> comparisonChart chart
        | DocNode.DataTable(headers, rows) ->
            div {
                _class "overflow-x-auto rounded-xl border border-[var(--fve-border)]"
                table {
                    _class "w-full border-collapse text-sm"
                    thead { tr { for header in headers do th { _class "bg-[var(--fve-surface-subtle)] p-3 text-left text-xs tracking-wide text-[var(--fve-muted-text)] uppercase"; header } } }
                    tbody { for row in rows do tr { for cell in row do td { _class "border-t border-[var(--fve-border)] p-3 align-top"; cell } } }
                }
            }
        | DocNode.CodeBlock(language, source) -> CodeBlock.create language source |> CodeBlock.render
        | DocNode.Example(id, label, language, source, preview) -> Example.codeFirst id label language source preview
        | DocNode.Heading _ -> invalidOp "Headings are converted to documentation sections."

    let private sections (nodes:DocNode list) : DocumentationSectionConfig list =
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
            match section.pages, section.sections with
            | [page], [] when String.IsNullOrEmpty parentId && section.label=page.navLabel ->
                Nav.page page.id page.navLabel page.path page.path
            | _ ->
                let id = if String.IsNullOrEmpty parentId then slug section.label else parentId + "-" + slug section.label
                let pages =
                    section.pages
                    |> List.map (fun page -> Nav.page page.id page.navLabel page.path page.path)
                let groups = section.sections |> List.map (group id)
                Nav.group id section.label (section.label = "Getting started") (pages @ groups)

        sections |> List.map (group "")

    let private assets =
        { DocsAssets.defaults with
            productStylesheets = [ "/css/output.css" ]
            prismStylesheet = Some "/css/prism-tomorrow.1.29.0.min.css"
            prismScripts = DocsAssets.defaults.prismScripts @ ["/scripts/prism-bash.1.29.0.min.js";"/scripts/prism-json.1.29.0.min.js"]
            navigation = Some Docs.Web.Navigation.enhancement
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
            | InlineContent.Link(label, _) -> label
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
        |> Option.orElseWith (fun () -> ComponentDocumentation.tryPage page.path)
        |> Option.orElseWith (fun () -> if page.path=Examples.registration.path then Some Examples.gallery else None)
        |> Option.orElseWith (fun () -> Components.tryPage page.path)
        |> Option.orElseWith (fun () -> Showcase.tryPage page.path)
        |> Option.defaultWith (fun () -> legacyPage page)

    let private prepareResolvedPage (appMode:AppMode option) (sections:NavSection list) (registration:DocPage) (docsPage:DocumentationPageConfig) =
        let search =
            registeredPages sections
            |> List.map (fun (page:DocPage) ->
                let entry = DocsSearchEntry.create page.path (resolvePage page) [ page.category; page.navLabel ]
                let isComponent =
                    (Components.allRegistrations |> List.exists (fun item -> item.path = page.path))
                    && page.path <> Components.overviewRegistration.path
                    && page.path <> Components.installationRegistration.path
                    && not (Components.guideRegistrations |> List.exists (fun item -> item.path = page.path))
                    && not (page.path.StartsWith("/components/page-examples/", StringComparison.Ordinal))
                    || (ComponentDocumentation.registrations |> List.exists (fun item -> item.path = page.path))
                if isComponent then entry |> DocsSearchEntry.withGroup "Components" else entry)
            |> DocsSearch.index
        let site = site sections search
        let docsPage =
            docsPage
            |> DocumentationPage.withMetadata {
                docsPage.metadata with
                    canonicalUrl =
                        docsPage.metadata.canonicalUrl
                        |> Option.orElseWith (fun () -> site.baseUrl |> Option.map (fun baseUrl -> baseUrl.TrimEnd('/') + registration.path))
                    socialImage = Some "https://fve.meiermade.com/social-card.png" }
        let docsPage = pager sections registration.id |> Option.map (fun value -> DocumentationPage.withPager value docsPage) |> Option.defaultValue docsPage
        let sideNavItems = navigation sections
        let breadcrumbs = Navigation.breadcrumbs sideNavItems site.homeId docsPage.activeId
        let renderMode = appMode |> Option.map Fullscreen |> Option.defaultValue Embedded
        site, sideNavItems, breadcrumbs, renderMode, docsPage

    let private renderResolvedPage appMode sections registration docsPage =
        let site, sideNavItems, breadcrumbs, renderMode, docsPage = prepareResolvedPage appMode sections registration docsPage
        DocsView.documentWithNavigation site breadcrumbs sideNavItems renderMode docsPage

    let private renderResolvedNavigation appMode sections registration docsPage =
        let site, sideNavItems, breadcrumbs, renderMode, docsPage = prepareResolvedPage appMode sections registration docsPage
        DocsView.navigationRootWithNavigation site breadcrumbs sideNavItems renderMode docsPage,
        DocsView.documentMetadata site docsPage

    let renderPage sections registration =
        renderResolvedPage None sections registration (resolvePage registration)

    let navigationPage appMode sections registration =
        renderResolvedNavigation appMode sections registration (resolvePage registration)

    let navigationPageWithPage appMode sections registration docsPage =
        renderResolvedNavigation appMode sections registration docsPage

    let documentWithContent sections registration page root =
        let site,_,_,_,page = prepareResolvedPage None sections registration page
        DocsView.documentWithContent site page root

    let contentMetadata sections registration page =
        let site,_,_,_,page = prepareResolvedPage None sections registration page
        DocsView.documentMetadata site page

    let document sections page = renderPage sections page
    let documentFor appMode sections page = renderResolvedPage appMode sections page (resolvePage page)
    let documentWithPage sections registration docsPage = renderResolvedPage None sections registration docsPage
    let documentWithPageFor appMode sections registration docsPage = renderResolvedPage appMode sections registration docsPage
