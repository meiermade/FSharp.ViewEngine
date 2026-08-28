namespace FSharp.ViewEngine.Docs

open FSharp.ViewEngine
open System
open System.Text.Json
open type Html

/// Runtime and head assets used by the documentation document shell.
[<NoEquality; NoComparison>]
type DocsAssets =
    { productStylesheets:string list
      prismStylesheet:string option
      prismScripts:string list
      mermaidScript:string option
      datastarScript:string option
      mermaidSecurityLevel:string
      nonce:string option
      additionalHead:HtmlElement list }

module DocsAssets =
    let defaults =
        { productStylesheets = [ "/css/compiled.css" ]
          prismStylesheet = None
          prismScripts =
            [ "/scripts/prism.1.29.0.min.js"
              "/scripts/prism-fsharp.1.29.0.min.js"
              "/scripts/prism-sql.1.29.0.min.js" ]
          mermaidScript = Some "/scripts/mermaid.11.16.0.min.js"
          datastarScript = Some "/scripts/datastar.1.0.2.js"
          mermaidSecurityLevel = "antiscript"
          nonce = None
          additionalHead = [] }

/// Semantic accent colors and browser theme color for the documentation shell.
type DocsTheme =
    { accent50:string
      accent100:string
      accent500:string
      accent700:string
      accent900:string
      themeColor:string }

/// An optional repository action displayed in the documentation header.
[<NoEquality; NoComparison>]
type DocsRepository =
    | GitHubRepository of url:string
    | RepositoryLink of label:string * url:string

module DocsRepository =
    let github url = GitHubRepository url
    let link label url = RepositoryLink(label, url)

module DocsTheme =
    let amber =
        { accent50 = "#fffbeb"
          accent100 = "#fef3c7"
          accent500 = "#f59e0b"
          accent700 = "#b45309"
          accent900 = "#78350f"
          themeColor = "#fafafa" }

    let sky =
        { accent50 = "#f0f9ff"
          accent100 = "#e0f2fe"
          accent500 = "#0ea5e9"
          accent700 = "#0369a1"
          accent900 = "#0c4a6e"
          themeColor = "#fafafa" }

    let emerald =
        { accent50 = "#ecfdf5"
          accent100 = "#d1fae5"
          accent500 = "#10b981"
          accent700 = "#047857"
          accent900 = "#064e3b"
          themeColor = "#fafafa" }

/// Consumer-owned branding, navigation, theme, assets, and typed destinations for a documentation site.
[<NoEquality; NoComparison>]
type DocsSite<'destination> =
    { name:string
      baseUrl:string option
      description:string option
      repository:DocsRepository option
      brandMark:HtmlElement
      homeId:string
      navigation:NavNode<'destination> list
      storageKey:string
      defaultColorMode:DocsColorMode
      theme:DocsTheme
      assets:DocsAssets
      search:DocsSearchResult list }

module private Icons =
    let menu =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M2 5.75A.75.75 0 0 1 2.75 5h14.5a.75.75 0 0 1 0 1.5H2.75A.75.75 0 0 1 2 5.75Zm0 4A.75.75 0 0 1 2.75 9h14.5a.75.75 0 0 1 0 1.5H2.75A.75.75 0 0 1 2 9.75Zm.75 3.25a.75.75 0 0 0 0 1.5h14.5a.75.75 0 0 0 0-1.5H2.75Z" clip-rule="evenodd"/></svg>"""

    let close =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 1 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""

    let chevron =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 0 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""

    let breadcrumbChevron =
        raw """<svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M6.22 4.22a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06l-3.25 3.25a.75.75 0 0 1-1.06-1.06L8.94 8 6.22 5.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""

    let ellipsis =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M3.75 8.75a1.25 1.25 0 1 0 0 2.5 1.25 1.25 0 0 0 0-2.5Zm6.25 0a1.25 1.25 0 1 0 0 2.5 1.25 1.25 0 0 0 0-2.5Zm6.25 0a1.25 1.25 0 1 0 0 2.5 1.25 1.25 0 0 0 0-2.5Z"/></svg>"""

    let github =
        raw """<svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82A7.65 7.65 0 0 1 8 3.86c.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8Z"/></svg>"""

    let sun =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M10 2a.75.75 0 0 1 .75.75v1.5a.75.75 0 0 1-1.5 0v-1.5A.75.75 0 0 1 10 2Zm0 5a3 3 0 1 0 0 6 3 3 0 0 0 0-6Zm0 8a.75.75 0 0 1 .75.75v1.5a.75.75 0 0 1-1.5 0v-1.5A.75.75 0 0 1 10 15ZM4.34 4.34a.75.75 0 0 1 1.06 0l1.06 1.06A.75.75 0 1 1 5.4 6.46L4.34 5.4a.75.75 0 0 1 0-1.06Zm9.2 9.2a.75.75 0 0 1 1.06 0l1.06 1.06a.75.75 0 1 1-1.06 1.06l-1.06-1.06a.75.75 0 0 1 0-1.06ZM2 10a.75.75 0 0 1 .75-.75h1.5a.75.75 0 0 1 0 1.5h-1.5A.75.75 0 0 1 2 10Zm13 0a.75.75 0 0 1 .75-.75h1.5a.75.75 0 0 1 0 1.5h-1.5A.75.75 0 0 1 15 10Zm-.4-5.66a.75.75 0 0 1 1.06 1.06L14.6 6.46a.75.75 0 1 1-1.06-1.06l1.06-1.06ZM5.4 13.54a.75.75 0 0 1 1.06 1.06L5.4 15.66a.75.75 0 1 1-1.06-1.06l1.06-1.06Z"/></svg>"""

    let moon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M17.293 13.293A8 8 0 0 1 6.707 2.707a8.001 8.001 0 1 0 10.586 10.586Z"/></svg>"""

    let monitor =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M2 4.75A1.75 1.75 0 0 1 3.75 3h12.5A1.75 1.75 0 0 1 18 4.75v8.5A1.75 1.75 0 0 1 16.25 15h-5.5v1.5h2a.75.75 0 0 1 0 1.5h-5.5a.75.75 0 0 1 0-1.5h2V15h-5.5A1.75 1.75 0 0 1 2 13.25v-8.5Zm1.5 0v8.5c0 .14.11.25.25.25h12.5c.14 0 .25-.11.25-.25v-8.5a.25.25 0 0 0-.25-.25H3.75a.25.25 0 0 0-.25.25Z" clip-rule="evenodd"/></svg>"""

    let check =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M16.7 5.3a.75.75 0 0 1 0 1.06l-8 8a.75.75 0 0 1-1.06 0l-4-4A.75.75 0 0 1 4.7 9.3l3.47 3.47 7.47-7.47a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>"""

module private ViewHelpers =
    let signalName (id:string) =
        let token =
            id
            |> Seq.map (fun character -> if Char.IsLetterOrDigit character then character else '_')
            |> Seq.toArray
            |> String
        token + "Open"

    let jsString (value:string) = JsonSerializer.Serialize(value)

    let navigateAction href =
        let encoded = jsString href
        $"if (!evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) {{ evt.preventDefault(); $sideNavOpen = false; $breadcrumbMenuOpen = false; window.fsharpDocsNavigation?.begin(); window.history.pushState(null, '', {encoded}); @get({encoded}) }}"

    let themeStyle theme =
        $"--spec-accent-50:{theme.accent50};--spec-accent-100:{theme.accent100};--spec-accent-500:{theme.accent500};--spec-accent-700:{theme.accent700};--spec-accent-900:{theme.accent900}"

module MermaidView =
    let private render (classes:string) (source:string) =
        div {
            _class classes
            _data("init", "window.renderMermaid?.(el)")
            _data("mermaid-source", source)
            _data("mermaid-state", "pending")
            _ariaBusy true
            p {
                _class "spec-diagram-status"
                _data("mermaid-status", "true")
                _role "status"
                "Rendering diagram…"
            }
        }

    let diagram source = render "mermaid spec-diagram" source

    let c4Diagram source = render "mermaid spec-diagram spec-c4-diagram" source

module private DocsBlockView =
    let rec private renderInline value =
        match value with
        | InlineText text -> text |> Html.text
        | InlineCode source -> code { source }
        | InlineLink(label, href) -> a { _href href; _class "spec-content-link"; label }
        | InlineStrong content -> strong { for child in content do renderInline child }
        | InlineEmphasis content -> em { for child in content do renderInline child }

    let render block =
        match block with
        | Paragraph text -> p { _class "spec-paragraph"; text }
        | RichParagraph content -> p { _class "spec-paragraph"; for child in content do renderInline child }
        | Bullets items ->
            ul { _class "spec-bullets"; for item in items do li { item } }
        | RichBullets(ordered, items) ->
            if ordered then ol { _class "spec-bullets"; for item in items do li { for child in item do renderInline child } }
            else ul { _class "spec-bullets"; for item in items do li { for child in item do renderInline child } }
        | Table(headers, rows) ->
            div {
                _class "spec-table-wrap"
                table {
                    _class "spec-table"
                    thead { tr { for header in headers do th { header } } }
                    tbody { for row in rows do tr { for cell in row do td { cell } } }
                }
            }
        | RichTable(headers, rows) ->
            div {
                _class "spec-table-wrap"
                table {
                    _class "spec-table"
                    thead { tr { for header in headers do th { for child in header do renderInline child } } }
                    tbody { for row in rows do tr { for cell in row do td { for child in cell do renderInline child } } }
                }
            }
        | Code(language, source) ->
            let normalized = if String.IsNullOrWhiteSpace language then "text" else language.Trim().ToLowerInvariant()
            let prismLanguage = if normalized = "fs" then "fsharp" else normalized
            CopyableCode.render ($"spec-code language-{prismLanguage}") ($"language-{prismLanguage}") "code" source
        | Diagram source -> MermaidView.diagram source
        | C4Diagram source -> MermaidView.c4Diagram source
        | Sequence diagram -> diagram |> SequenceDiagram.render |> MermaidView.diagram
        | Callout(label, text) ->
            div {
                _class "spec-callout"
                div { _class "spec-callout-label"; label }
                p { _class "spec-callout-text"; text }
            }
        | RichCallout(label, content) ->
            div {
                _class "spec-callout"
                div { _class "spec-callout-label"; for child in label do renderInline child }
                p { _class "spec-callout-text"; for child in content do renderInline child }
            }
        | Custom element -> element

module private DocsSectionView =
    let render (docSection:DocsSection) =
        section {
            div {
                _id docSection.id
                _class "spec-section-anchor"
                _tabindex -1
                let classes = $"spec-section-title spec-section-title-level-{docSection.level}"
                if docSection.level <= 2 then h2 { _class classes; docSection.title }
                elif docSection.level = 3 then h3 { _class classes; docSection.title }
                else h4 { _class classes; docSection.title }
            }
            div { _class "spec-section-blocks"; for block in docSection.blocks do DocsBlockView.render block }
        }

module private ColorModeView =
    let private optionKeydown =
        """if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(evt.key)) { evt.preventDefault(); const items = [...document.querySelectorAll('#spec-color-mode-menu [role=menuitemradio]')]; const current = items.indexOf(evt.currentTarget); const next = evt.key === 'Home' ? 0 : evt.key === 'End' ? items.length - 1 : (current + (evt.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length; items[next]?.focus(); } else if (evt.key === 'Escape') { evt.preventDefault(); $colorModeMenuOpen = false; document.getElementById('spec-color-mode-button')?.focus(); } else if (evt.key === 'Tab') { $colorModeMenuOpen = false; }"""

    let private option (defaultMode:DocsColorMode) (mode:DocsColorMode) (label:string) (icon:HtmlElement) =
        let value = DocsColorMode.value mode
        let isDefault = mode = defaultMode
        button {
            _type "button"
            _role "menuitemradio"
            _tabindex -1
            _ariaChecked isDefault
            _data("attr:aria-checked", $"$colorMode === '{value}' ? 'true' : 'false'")
            _data("attr:data-selected", $"$colorMode === '{value}' ? 'true' : 'false'")
            _data("on:click", $"$colorMode = '{value}'; $colorModeMenuOpen = false; document.getElementById('spec-color-mode-button')?.focus()")
            _data("on:keydown", optionKeydown)
            _class "spec-color-mode-option"
            span { _class "spec-color-mode-option-icon"; icon }
            span { _class "spec-color-mode-option-label"; label }
            span { _class "spec-color-mode-check"; Icons.check }
        }

    let render defaultMode =
        div {
            _class "spec-color-mode"
            _data("on:click__outside", "$colorModeMenuOpen = false")
            button {
                _id "spec-color-mode-button"
                _type "button"
                _ariaLabel "Choose color theme"
                _ariaHaspopup "menu"
                _ariaControls "spec-color-mode-menu"
                _data("attr:aria-expanded", "$colorModeMenuOpen ? 'true' : 'false'")
                _data("on:click", "$colorModeMenuOpen = !$colorModeMenuOpen; if ($colorModeMenuOpen) queueMicrotask(() => document.querySelector('#spec-color-mode-menu [data-selected=true]')?.focus())")
                _data("on:keydown", "if (evt.key === 'ArrowDown' || evt.key === 'ArrowUp') { evt.preventDefault(); $colorModeMenuOpen = true; queueMicrotask(() => { const items = [...document.querySelectorAll('#spec-color-mode-menu [role=menuitemradio]')]; (evt.key === 'ArrowDown' ? items[0] : items[items.length - 1])?.focus(); }); }")
                _class "spec-icon-button spec-color-mode-button"
                span { _class "spec-color-mode-icon spec-color-mode-icon-light"; Icons.sun }
                span { _class "spec-color-mode-icon spec-color-mode-icon-dark"; Icons.moon }
            }
            div {
                _id "spec-color-mode-menu"
                _role "menu"
                _ariaLabel "Color theme"
                _class "spec-color-mode-menu"
                _data("show", "$colorModeMenuOpen")
                _style "display:none"
                option defaultMode System "System" Icons.monitor
                option defaultMode Light "Light" Icons.sun
                option defaultMode Dark "Dark" Icons.moon
            }
        }

module private RepositoryView =
    let render = function
        | GitHubRepository url ->
            a {
                _href url
                _ariaLabel "View repository on GitHub"
                _title "View repository on GitHub"
                _class "spec-repository spec-icon-button"
                Icons.github
            }
        | RepositoryLink(label, url) -> a { _href url; _class "spec-repository spec-repository-link"; label }

module private NavigationView =
    open ViewHelpers

    let rec node activeId (navNode:NavNode<'destination>) =
        let isActive = NavNode.id navNode = activeId
        li {
            match navNode with
            | Group group ->
                let signal = signalName group.id
                let containsActive = NavNode.containsActive activeId navNode
                button {
                    _id $"nav-{group.id}"
                    _type "button"
                    _ariaLabel $"Toggle {group.label} section"
                    _ariaControls $"nav-children-{group.id}"
                    _data("attr:aria-expanded", $"${signal} ? 'true' : 'false'")
                    _data("on:click", $"${signal} = !${signal}")
                    _data("active", containsActive.ToString().ToLowerInvariant())
                    _class "spec-nav-group-button"
                    span {
                        _class "spec-nav-chevron"
                        _data("attr:data-open", $"${signal} ? 'true' : 'false'")
                        Icons.chevron
                    }
                    span { _class "spec-nav-label"; group.label }
                }
                ul {
                    _id $"nav-children-{group.id}"
                    _class "spec-nav-children"
                    _data("show", $"${signal}")
                    if not (group.defaultOpen || containsActive) then _style "display:none"
                    for child in group.children do node activeId child
                }
            | Page page ->
                a {
                    _id $"nav-{page.id}"
                    _href page.href
                    _data("on:click", navigateAction page.href)
                    _data("selected", isActive.ToString().ToLowerInvariant())
                    if isActive then _ariaCurrent "page"
                    _class "spec-nav-link"
                    span { _class "spec-nav-chevron-spacer"; _ariaHidden "true" }
                    span { _class "spec-nav-label"; page.label }
                }
        }

    let sideNav (site:DocsSite<'destination>) activeId =
        aside {
            _id "side-nav"
            _class "spec-side-nav spec-hidden"
            _ariaLabel "Documentation navigation"
            _data("class:spec-hidden", "!$sideNavOpen")
            div {
                _class "spec-side-nav-inner"
                div {
                    _class "spec-brand"
                    div { _class "spec-brand-mark"; site.brandMark }
                    div { _class "spec-brand-name"; site.name }
                    div { _class "spec-grow" }
                    button {
                        _type "button"
                        _ariaLabel "Close navigation"
                        _class "spec-nav-close"
                        _data("on:click", "$sideNavOpen = false; window.fsharpDocsMobileNav.close()")
                        Icons.close
                    }
                }
                nav {
                    _ariaLabel "Documentation"
                    _class "spec-nav-scroll"
                    ul { _class "spec-nav-list"; for section in site.navigation do node activeId section }
                }
            }
        }

    let topNav (site:DocsSite<'destination>) activeId =
        let breadcrumbs: Breadcrumb list = Navigation.breadcrumbs site.navigation site.homeId activeId
        let hiddenCount = if breadcrumbs.Length > 2 then breadcrumbs.Length - 1 else 0
        let hiddenBreadcrumbs = breadcrumbs |> List.take hiddenCount

        header {
            _class "spec-top-nav"
            div {
                _class "spec-top-left"
                button {
                    _type "button"
                    _ariaLabel "Open navigation"
                    _ariaControls "side-nav"
                    _data("attr:aria-expanded", "$sideNavOpen ? 'true' : 'false'")
                    _class "spec-nav-open"
                    _data("on:click", "$sideNavOpen = true; window.fsharpDocsMobileNav.open(evt.currentTarget)")
                    Icons.menu
                }
                nav {
                    _ariaLabel "Breadcrumb"
                    _class "spec-breadcrumbs"
                    ol {
                        _role "list"
                        _class "spec-breadcrumb-list"
                        if not hiddenBreadcrumbs.IsEmpty then
                            li {
                                _class "spec-breadcrumb-menu-wrap"
                                button {
                                    _type "button"
                                    _ariaLabel "Show hidden breadcrumbs"
                                    _ariaControls "spec-breadcrumb-menu"
                                    _data("attr:aria-expanded", "$breadcrumbMenuOpen ? 'true' : 'false'")
                                    _data("on:click", "$breadcrumbMenuOpen = !$breadcrumbMenuOpen")
                                    _class "spec-breadcrumb-menu-button"
                                    Icons.ellipsis
                                }
                                div {
                                    _id "spec-breadcrumb-menu"
                                    _class "spec-breadcrumb-menu"
                                    _data("show", "$breadcrumbMenuOpen")
                                    _style "display:none"
                                    for crumb in hiddenBreadcrumbs do
                                        match crumb.href with
                                        | Some href -> a { _href href; _data("on:click", navigateAction href); crumb.label }
                                        | None -> span { crumb.label }
                                }
                            }
                        for index, crumb in List.indexed breadcrumbs do
                            let isCurrent = index = breadcrumbs.Length - 1
                            let hiddenOnMobile = index < hiddenCount
                            if index > 0 then
                                li { _class (if hiddenOnMobile then "spec-breadcrumb-separator spec-mobile-hidden" else "spec-breadcrumb-separator"); Icons.breadcrumbChevron }
                            li {
                                _class (if hiddenOnMobile then "spec-breadcrumb spec-mobile-hidden" else "spec-breadcrumb")
                                match crumb.href with
                                | Some href when not isCurrent -> a { _href href; _data("on:click", navigateAction href); crumb.label }
                                | _ ->
                                    span {
                                        if isCurrent then _ariaCurrent "page"
                                        crumb.label
                                    }
                            }
                    }
                }
            }
            div {
                _class "spec-top-actions"
                if not site.search.IsEmpty then SearchView.render site.search
                ColorModeView.render site.defaultColorMode
                match site.repository with
                | Some repository -> RepositoryView.render repository
                | None -> ()
            }
        }

type private TocItem =
    { level:int
      label:string
      href:string }

module private TocView =
    let private links (className:string) items =
        nav {
            _ariaLabel "On this page"
            _class className
            for item in items do
                a {
                    _href item.href
                    _data("on:click", "window.fsharpDocsNavigation.navigateToFragment(evt, evt.currentTarget.getAttribute('href'))")
                    _class (if item.level <= 2 then "" else $"spec-toc-level-{item.level}")
                    item.label
                }
        }

    let desktop (items:TocItem list) =
        aside {
            _class "spec-toc"
            div {
                _class "spec-toc-inner"
                div { _class "spec-toc-title"; "On this page" }
                links "spec-toc-nav" items
            }
        }

    let mobile (items:TocItem list) =
        details {
            _class "spec-mobile-toc"
            _ariaLabel "On this page"
            summary { "On this page" }
            links "spec-mobile-toc-nav" items
        }

module private PagerView =
    open ViewHelpers

    let private renderLink (direction:string) (relation:string) (className:string) (link:DocsPageLink) =
        a {
            _rel relation
            _href link.href
            _data("on:click", navigateAction link.href)
            _class $"spec-pager-link {className}"
            span {
                _class "spec-pager-direction"
                if relation = "prev" then
                    span { _class "spec-pager-arrow spec-pager-arrow-previous"; Icons.chevron }
                span { direction }
                if relation = "next" then
                    span { _class "spec-pager-arrow"; Icons.chevron }
            }
            span { _class "spec-pager-title"; link.label }
        }

    let render (pager:DocsPager) =
        nav {
            _ariaLabel "Page navigation"
            _class "spec-page-pager"
            match pager.previousPage with
            | Some previousPage -> renderLink "Previous" "prev" "spec-pager-previous" previousPage
            | None -> ()
            match pager.nextPage with
            | Some nextPage -> renderLink "Next" "next" "spec-pager-next" nextPage
            | None -> ()
        }

module DocsView =
    open ViewHelpers

    let private tocItems (page:DocsPage) : TocItem list =
        page.sections
        |> List.map (fun section -> { level = section.level; label = section.title; href = $"#{section.id}" })

    let content (page:DocsPage) =
        div {
            _class "spec-page-body"
            match page.heading with
            | Visible ->
                section {
                    match page.headingAdornment with
                    | Some adornment -> adornment
                    | None -> ()
                    h1 { _class "spec-page-heading"; page.title }
                    if page.metadata.version.IsSome || page.metadata.deprecated then
                        div {
                            _class "docs-page-badges"
                            match page.metadata.version with
                            | Some version -> span { _class "docs-page-badge"; _data("docs-version", version); version }
                            | None -> ()
                            if page.metadata.deprecated then span { _class "docs-page-badge docs-page-badge-warning"; _data("docs-deprecated", "true"); "Deprecated" }
                        }
                    if not (String.IsNullOrWhiteSpace page.description) then
                        p { _class "spec-page-description"; page.description }
                    if page.metadata.lastUpdated.IsSome || page.metadata.editUrl.IsSome then
                        div {
                            _class "docs-page-maintenance"
                            match page.metadata.lastUpdated with
                            | Some lastUpdated -> span { "Last updated "; time { _datetime lastUpdated; lastUpdated } }
                            | None -> ()
                            match page.metadata.editUrl with
                            | Some editUrl -> a { _href editUrl; "Edit this page" }
                            | None -> ()
                        }
                }
            | VisuallyHidden -> h1 { _class "spec-heading-visually-hidden"; page.title }
            let items = tocItems page
            match page.rightRail with
            | TableOfContents when not items.IsEmpty -> TocView.mobile items
            | _ -> ()
            for section in page.sections do DocsSectionView.render section
            match page.pager with
            | Some pager -> PagerView.render pager
            | None -> ()
        }

    let sideNav (site:DocsSite<'destination>) (page:DocsPage) = NavigationView.sideNav site page.activeId

    let pageContent (site:DocsSite<'destination>) (page:DocsPage) =
        let items = tocItems page
        let layoutClass =
            match page.layout with
            | Article -> "docs-article-layout"
            | Reference -> "docs-reference-layout"
            | Canvas -> "docs-canvas-layout"

        div {
            _id "page-content"
            _class "spec-page-content"
            NavigationView.topNav site page.activeId
            div {
                _class "spec-page-viewport"
                div {
                    _class $"spec-page-layout {layoutClass}"
                    main {
                        _id "main-content"
                        _class "spec-main"
                        _tabindex -1
                        div { _class "spec-main-inner"; content page }
                    }
                    match page.rightRail with
                    | TableOfContents when not items.IsEmpty -> TocView.desktop items
                    | TableOfContents -> ()
                    | NoRail -> ()
                    | CustomRail rail ->
                        aside {
                            _class "docs-custom-rail"
                            div { _class "docs-custom-rail-inner"; rail }
                        }
                }
            }
        }

    let page (site:DocsSite<'destination>) (docPage:DocsPage) =
        div {
            _id "page"
            _class "spec-shell"
            a { _href "#main-content"; _class "docs-skip-link"; "Skip to main content" }
            button {
                _type "button"
                _ariaLabel "Close navigation overlay"
                _class "spec-overlay spec-hidden"
                _data("class:spec-hidden", "!$sideNavOpen")
                _data("on:click", "$sideNavOpen = false; window.fsharpDocsMobileNav.close()")
            }
            sideNav site docPage
            pageContent site docPage
        }

    let document (site:DocsSite<'destination>) (docPage:DocsPage) =
        let pageHref =
            site.navigation
            |> NavNode.collectPages
            |> List.tryFind (NavNode.id >> (=) docPage.activeId)
            |> Option.bind NavNode.href

        let canonicalUrl =
            match docPage.metadata.canonicalUrl with
            | Some url -> Some url
            | None ->
                match site.baseUrl, pageHref with
                | Some baseUrl, Some href -> Some(baseUrl.TrimEnd('/') + href)
                | _ -> None

        let navGroups = NavNode.collectGroups site.navigation
        let navSignals =
            navGroups
            |> List.map (fun node ->
                let signal = signalName (NavNode.id node)
                let shouldOpen = NavNode.defaultOpen node || NavNode.containsActive docPage.activeId node
                let containsActive = NavNode.containsActive docPage.activeId node
                $"{signal}: window.fsharpDocsNav.initial({jsString (NavNode.id node)}, {shouldOpen.ToString().ToLowerInvariant()}, {containsActive.ToString().ToLowerInvariant()})")

        let signals = "{ sideNavOpen: false, breadcrumbMenuOpen: false, colorModeMenuOpen: false, colorMode: window.fsharpDocsColorMode.current()" + (if navSignals.IsEmpty then "" else ", " + String.concat ", " navSignals) + " }"
        let navState =
            navGroups
            |> List.map (fun node -> $"{jsString (NavNode.id node)}: ${signalName (NavNode.id node)}")
            |> String.concat ", "
            |> fun properties -> $"{{ {properties} }}"

        let mermaidSecurity = jsString site.assets.mermaidSecurityLevel
        let mermaidScript = site.assets.mermaidScript |> Option.map jsString |> Option.defaultValue "null"
        let storageKey = jsString site.storageKey
        let colorModeStorageKey = jsString $"{site.storageKey}-color-mode"
        let defaultColorMode = site.defaultColorMode |> DocsColorMode.value |> jsString
        let prismStylesheet = site.assets.prismStylesheet |> Option.map jsString |> Option.defaultValue "null"
        let prismScripts = site.assets.prismScripts |> List.map jsString |> String.concat ", " |> fun sources -> $"[{sources}]"
        let assetNonce = site.assets.nonce |> Option.map jsString |> Option.defaultValue "null"
        let colorModeScript =
            """
(() => {
  const validModes = new Set(['system', 'light', 'dark']);
  const media = window.matchMedia('(prefers-color-scheme: dark)');
  window.fsharpDocsColorMode = {
    storageKey: __STORAGE_KEY__,
    defaultMode: __DEFAULT_MODE__,
    current() {
      try {
        const stored = window.localStorage.getItem(this.storageKey);
        return validModes.has(stored) ? stored : this.defaultMode;
      } catch { return this.defaultMode; }
    },
    apply(mode) {
      const selected = validModes.has(mode) ? mode : this.defaultMode;
      const dark = selected === 'dark' || (selected === 'system' && media.matches);
      document.documentElement.classList.toggle('dark', dark);
      document.documentElement.dataset.colorMode = selected;
      return selected;
    },
    set(mode) {
      const selected = this.apply(mode);
      try { window.localStorage.setItem(this.storageKey, selected); } catch {}
      window.dispatchEvent(new CustomEvent('fsharpdocs:colormode', { detail: { mode: selected } }));
      return selected;
    }
  };
  window.fsharpDocsColorMode.apply(window.fsharpDocsColorMode.current());
  media.addEventListener?.('change', () => {
    if (window.fsharpDocsColorMode.current() === 'system') {
      window.fsharpDocsColorMode.apply('system');
      window.dispatchEvent(new CustomEvent('fsharpdocs:colormode', { detail: { mode: 'system' } }));
    }
  });
  window.addEventListener('storage', event => {
    if (event.key === window.fsharpDocsColorMode.storageKey) {
      window.fsharpDocsColorMode.apply(window.fsharpDocsColorMode.current());
      window.dispatchEvent(new CustomEvent('fsharpdocs:colormode'));
    }
  });
})();
            """
            |> fun source -> source.Replace("__STORAGE_KEY__", colorModeStorageKey).Replace("__DEFAULT_MODE__", defaultColorMode)

        let mermaidInitialization =
            """
window.fsharpDocsMermaid = window.fsharpDocsMermaid ?? {
  source: __MERMAID_SCRIPT__,
  nonce: __ASSET_NONCE__,
  loading: null,
  loadScript(source) {
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = source;
      script.dataset.docsMermaidAsset = 'true';
      if (this.nonce) script.nonce = this.nonce;
      script.addEventListener('load', resolve, { once: true });
      script.addEventListener('error', () => reject(new Error(`Unable to load Mermaid asset: ${source}`)), { once: true });
      document.head.append(script);
    });
  },
  hasApi() {
    return typeof window.mermaid?.initialize === 'function' && typeof window.mermaid?.render === 'function';
  },
  async ensure() {
    if (this.hasApi() || !this.source) return;
    if (!this.loading) this.loading = this.loadScript(this.source);
    await this.loading;
  }
};
let mermaidRenderQueue = Promise.resolve();
let mermaidRenderId = 0;
const mermaidStatus = (role, message) => {
  const status = document.createElement('p');
  status.className = 'spec-diagram-status';
  status.dataset.mermaidStatus = 'true';
  status.setAttribute('role', role);
  status.textContent = message;
  return status;
};
const setMermaidPending = node => {
  node.dataset.mermaidState = 'pending';
  node.setAttribute('aria-busy', 'true');
  node.replaceChildren(mermaidStatus('status', 'Rendering diagram…'));
};
const setMermaidFailed = node => {
  node.dataset.mermaidState = 'failed';
  node.removeAttribute('aria-busy');
  node.replaceChildren(mermaidStatus('alert', 'Diagram unavailable.'));
};
const wireMermaidLinks = node => {
  for (const link of node.querySelectorAll('svg a')) {
    const href = link.getAttribute('href') ?? link.getAttribute('xlink:href');
    if (!href?.startsWith('/')) continue;
    const encodedHref = JSON.stringify(href);
    link.setAttribute('data-on:click', `if (!evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) { evt.preventDefault(); $sideNavOpen = false; $breadcrumbMenuOpen = false; window.fsharpDocsNavigation?.begin(); window.history.pushState(null, '', ${encodedHref}); @get(${encodedHref}) }`);
  }
};
window.renderMermaid = (el) => {
  const render = async () => {
    const nodes = el?.matches?.('.mermaid') ? [el] : Array.from(el?.querySelectorAll?.('.mermaid') ?? []);
    if (nodes.length === 0) return;
    for (const node of nodes) setMermaidPending(node);
    try {
      await window.fsharpDocsMermaid.ensure();
      if (!window.fsharpDocsMermaid.hasApi()) throw new Error('Mermaid is unavailable.');
      window.mermaid.initialize({ startOnLoad: false, theme: document.documentElement.classList.contains('dark') ? 'dark' : 'neutral', securityLevel: __SECURITY__, suppressErrorRendering: true });
    } catch {
      for (const node of nodes) if (node.isConnected) setMermaidFailed(node);
      return;
    }
    for (const node of nodes) {
      if (!node.isConnected) continue;
      try {
        const id = `fsharp-docs-mermaid-${++mermaidRenderId}`;
        const { svg, bindFunctions } = await window.mermaid.render(id, node.dataset.mermaidSource ?? '');
        if (!node.isConnected) continue;
        node.innerHTML = svg;
        bindFunctions?.(node);
        node.dataset.mermaidState = 'rendered';
        node.removeAttribute('aria-busy');
        wireMermaidLinks(node);
      } catch {
        if (node.isConnected) setMermaidFailed(node);
      }
    }
  };
  mermaidRenderQueue = mermaidRenderQueue.then(render, render);
  return mermaidRenderQueue;
};
window.addEventListener('fsharpdocs:colormode', () => window.renderMermaid?.(document));
            """
            |> fun source ->
                source
                    .Replace("__MERMAID_SCRIPT__", mermaidScript)
                    .Replace("__ASSET_NONCE__", assetNonce)
                    .Replace("__SECURITY__", mermaidSecurity)

        let navigationScript =
            """
window.fsharpDocsCode = window.fsharpDocsCode ?? {
  stylesheet: __PRISM_STYLESHEET__,
  scripts: __PRISM_SCRIPTS__,
  nonce: __ASSET_NONCE__,
  loading: null,
  loadStylesheet(source) {
    const href = new URL(source, document.baseURI).href;
    const existing = Array.from(document.querySelectorAll('link[rel="stylesheet"]')).find(link => link.href === href);
    if (existing?.sheet) return Promise.resolve();
    return new Promise((resolve, reject) => {
      const link = existing ?? document.createElement('link');
      link.rel = 'stylesheet';
      link.href = source;
      link.dataset.docsPrismAsset = 'true';
      link.addEventListener('load', resolve, { once: true });
      link.addEventListener('error', () => reject(new Error(`Unable to load Prism asset: ${source}`)), { once: true });
      if (!existing) document.head.append(link);
    });
  },
  loadScript(source) {
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = source;
      script.dataset.docsPrismAsset = 'true';
      if (this.nonce) script.nonce = this.nonce;
      script.addEventListener('load', resolve, { once: true });
      script.addEventListener('error', () => reject(new Error(`Unable to load Prism asset: ${source}`)), { once: true });
      document.head.append(script);
    });
  },
  async ensure() {
    if (!this.loading) {
      this.loading = (async () => {
        if (this.stylesheet) await this.loadStylesheet(this.stylesheet);
        if (window.Prism?.languages?.fsharp) return;
        window.Prism = window.Prism || {};
        window.Prism.manual = true;
        for (const source of this.scripts) await this.loadScript(source);
      })();
    }
    await this.loading;
  },
  async render(el) {
    const root = el ?? document;
    if (!root.querySelector?.('code[class*="language-"]') && !root.matches?.('code[class*="language-"]')) return;
    await this.ensure();
    window.Prism?.highlightAllUnder?.(root);
  }
};
window.renderCode = el => window.fsharpDocsCode.render(el);
window.renderDocsPreview = (el) => {
  for (const frame of el?.querySelectorAll?.('iframe[data-docs-preview-src]') ?? []) {
    if (!frame.getAttribute('src')) frame.setAttribute('src', frame.dataset.docsPreviewSrc);
  }
  return window.renderMermaid?.(el);
};
window.fsharpDocsCopy = async button => {
  const source = button.closest('.docs-copyable-code')?.querySelector('[data-docs-copy-source]')?.textContent ?? '';
  const label = button.querySelector('[data-docs-copy-label]');
  try {
    await navigator.clipboard.writeText(source);
    if (label) label.textContent = 'Copied';
    button.dataset.copied = 'true';
  } catch {
    if (label) label.textContent = 'Copy failed';
    button.dataset.copyError = 'true';
  }
  window.setTimeout(() => {
    if (label) label.textContent = 'Copy';
    delete button.dataset.copied;
    delete button.dataset.copyError;
  }, 1600);
};
window.fsharpDocsNav = {
  storageKey: __STORAGE_KEY__,
  read() {
    try {
      const value = window.localStorage.getItem(this.storageKey);
      return value === null ? null : new Set(JSON.parse(value));
    } catch { return null; }
  },
  initial(id, fallback, containsActive) {
    const stored = this.read();
    return stored === null ? fallback : containsActive || stored.has(id);
  },
  save(state) {
    try {
      const expanded = Object.entries(state).filter(([, value]) => value).map(([id]) => id);
      window.localStorage.setItem(this.storageKey, JSON.stringify(expanded));
    } catch {}
  }
};
window.fsharpDocsNavigation = {
  pending: false,
  begin() { this.pending = true; },
  observer: null,
  scrollRoot: null,
  scrollHandler: null,
  setCurrentFragment(id) {
    for (const link of document.querySelectorAll('.spec-toc-nav a[href^="#"], .spec-mobile-toc-nav a[href^="#"]')) {
      if (link.getAttribute('href') === `#${id}`) link.setAttribute('aria-current', 'location');
      else link.removeAttribute('aria-current');
    }
  },
  initializeToc() {
    this.observer?.disconnect();
    if (this.scrollRoot && this.scrollHandler) this.scrollRoot.removeEventListener('scroll', this.scrollHandler);
    const root = document.querySelector('.spec-main');
    const links = Array.from(document.querySelectorAll('.spec-toc-nav a[href^="#"]'));
    const sections = links.map(link => document.getElementById(decodeURIComponent(link.hash.slice(1)))).filter(Boolean);
    if (!root || sections.length === 0) return;
    const update = () => {
      const rootTop = root.getBoundingClientRect().top;
      const atEnd = root.scrollTop + root.clientHeight >= root.scrollHeight - 2;
      const current = atEnd ? sections.at(-1) : (sections.filter(section => section.getBoundingClientRect().top <= rootTop + 160).at(-1) ?? sections[0]);
      this.setCurrentFragment(current.id);
    };
    this.observer = new IntersectionObserver(update, { root, rootMargin: '-96px 0px -65% 0px', threshold: [0, 1] });
    for (const section of sections) this.observer.observe(section);
    this.scrollRoot = root;
    this.scrollHandler = update;
    root.addEventListener('scroll', update, { passive: true });
    update();
  },
  navigateToFragment(event, href) {
    if (!href?.startsWith('#') || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) return;
    const target = document.getElementById(decodeURIComponent(href.slice(1)));
    if (!target) return;
    event.preventDefault();
    window.history.pushState(null, '', href);
    target.scrollIntoView({ block: 'start' });
    target.focus({ preventScroll: true });
    event.currentTarget.closest('details')?.removeAttribute('open');
    this.setCurrentFragment(target.id);
  },
  async complete() {
    window.fsharpDocsColorMode?.apply(window.fsharpDocsColorMode.current());
    if (!this.pending) return;
    this.pending = false;
    for (const element of document.querySelectorAll('.spec-main, .spec-page-viewport, .spec-page-layout, .docs-custom-rail')) {
      element.scrollTo({ top: 0, left: 0, behavior: 'instant' });
    }
    window.scrollTo({ top: 0, left: 0, behavior: 'instant' });
    const content = document.getElementById('page-content');
    await window.renderCode?.(content);
    this.initializeToc();
  }
};
window.fsharpDocsMobileNav = {
  opener: null,
  focusable() {
    const nav = document.getElementById('side-nav');
    return Array.from(nav?.querySelectorAll('a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])') ?? [])
      .filter(element => element.getClientRects().length > 0);
  },
  open(opener) {
    this.opener = opener;
    const content = document.getElementById('page-content');
    content?.setAttribute('inert', '');
    requestAnimationFrame(() => document.querySelector('#side-nav .spec-nav-close')?.focus());
  },
  close() {
    const content = document.getElementById('page-content');
    content?.removeAttribute('inert');
    requestAnimationFrame(() => this.opener?.focus());
  },
  trap(event) {
    if (event.key !== 'Tab' || !document.getElementById('side-nav') || document.getElementById('side-nav').classList.contains('spec-hidden')) return;
    const focusable = this.focusable();
    if (focusable.length === 0) return;
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }
};
document.addEventListener('datastar-fetch', event => {
  if (event.detail?.type === 'finished') window.fsharpDocsNavigation.complete();
});
            """
            |> fun source ->
                source
                    .Replace("__STORAGE_KEY__", storageKey)
                    .Replace("__PRISM_STYLESHEET__", prismStylesheet)
                    .Replace("__PRISM_SCRIPTS__", prismScripts)
                    .Replace("__ASSET_NONCE__", assetNonce)

        let nonceAttribute () =
            match site.assets.nonce with
            | Some nonce -> _attr("nonce", nonce)
            | None -> Html.EmptyAttr

        html {
            _lang "en"
            _style (themeStyle site.theme)
            head {
                meta { _charset "utf-8" }
                meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
                meta { _name "theme-color"; _content site.theme.themeColor }
                title { docPage.metadata.browserTitle |> Option.defaultValue docPage.title }
                match canonicalUrl with
                | Some url ->
                    link { _rel "canonical"; _href url }
                    meta { _property "og:url"; _content url }
                | None -> ()
                let description = if String.IsNullOrWhiteSpace docPage.description then site.description else Some docPage.description
                match description with
                | Some value ->
                    meta { _name "description"; _content value }
                    meta { _property "og:description"; _content value }
                    meta { _name "twitter:description"; _content value }
                | None -> ()
                meta { _property "og:type"; _content "website" }
                meta { _property "og:site_name"; _content site.name }
                meta { _property "og:title"; _content (docPage.metadata.browserTitle |> Option.defaultValue docPage.title) }
                meta { _name "twitter:title"; _content (docPage.metadata.browserTitle |> Option.defaultValue docPage.title) }
                match docPage.metadata.socialImage with
                | Some image ->
                    meta { _property "og:image"; _content image }
                    meta { _property "og:image:alt"; _content docPage.title }
                    meta { _name "twitter:image"; _content image }
                    meta { _name "twitter:image:alt"; _content docPage.title }
                    meta { _name "twitter:card"; _content "summary_large_image" }
                | None -> ()
                if docPage.metadata.noIndex then meta { _name "robots"; _content "noindex" }
                script { nonceAttribute (); raw colorModeScript }
                style { nonceAttribute (); raw DefaultStyles.css }
                for stylesheet in site.assets.productStylesheets do link { _rel "stylesheet"; _href stylesheet }
                match site.assets.prismStylesheet with
                | Some stylesheet -> link { _rel "stylesheet"; _href stylesheet }
                | None -> ()
                match site.assets.mermaidScript with
                | Some _ -> script { nonceAttribute (); raw mermaidInitialization }
                | None -> ()
                script { nonceAttribute (); raw navigationScript }
                match site.assets.datastarScript with
                | Some source -> script { _type "module"; _src source; nonceAttribute () }
                | None -> ()
                for element in site.assets.additionalHead do element
            }
            body {
                _class "spec-document"
                _data("signals", signals)
                _data("effect", "window.fsharpDocsColorMode.set($colorMode)")
                _data("on-signal-patch", $"window.fsharpDocsNav.save({navState})")
                _data("on:popstate__window", "window.fsharpDocsNavigation.begin(); @get(window.location.pathname + window.location.search)")
                _data("on:keydown__window", "evt.key == 'Escape' ? ($sideNavOpen = false, $breadcrumbMenuOpen = false, $colorModeMenuOpen = false, window.fsharpDocsMobileNav.close()) : window.fsharpDocsMobileNav.trap(evt)")
                page site docPage
                script { nonceAttribute (); raw "document.addEventListener('DOMContentLoaded', async () => { const content = document.getElementById('page-content'); await window.renderCode?.(content); window.fsharpDocsNavigation.initializeToc(); });" }
            }
        }
