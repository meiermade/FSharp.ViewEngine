namespace FSharp.ViewEngine.Components.Templates

open FSharp.ViewEngine.Components

open FSharp.ViewEngine
open System
open System.Text.Json
open type Html
open type Datastar

/// <summary>
/// Opaque Datastar expressions supplied by a documentation host that owns enhanced navigation transport.
/// </summary>
/// <category>navigation</category>
[<NoEquality; NoComparison>]
type DocsNavigationEnhancement =
    { initialScript: string
      clickAction: string
      submitAction: string
      restoreAction: string
      fetchLifecycle: string
      appModeExitAction: string }

/// <summary>
/// Runtime and head assets used by the documentation document shell.
/// </summary>
/// <category>layouts</category>
[<NoEquality; NoComparison>]
type DocsAssets =
    { productStylesheets: string list
      prismStylesheet: string option
      prismScripts: string list
      mermaidScript: string option
      datastarScript: string option
      mermaidSecurityLevel: string
      nonce: string option
      additionalHead: HtmlElement list
      navigation: DocsNavigationEnhancement option }

/// <category>layouts</category>
module DocsAssets =
    let defaults =
        { productStylesheets = [ "/css/compiled.css" ]
          prismStylesheet = None
          prismScripts =
            [ "/scripts/prism.1.29.0.min.js"
              "/scripts/prism-fsharp.1.29.0.min.js"
              "/scripts/prism-sql.1.29.0.min.js" ]
          mermaidScript = Some "/scripts/mermaid.11.16.0.min.js"
          datastarScript = Some "/assets/app.js"
          mermaidSecurityLevel = "antiscript"
          nonce = None
          additionalHead = []
          navigation = None }

/// <summary>
/// Semantic accent colors and browser theme color for the documentation shell.
/// </summary>
/// <category>layouts</category>
type DocsTheme =
    { accent50: string
      accent100: string
      accent500: string
      accent700: string
      accent900: string
      themeColor: string }

/// <summary>
/// An optional repository action displayed in the documentation header.
/// </summary>
/// <category>layouts</category>
[<NoEquality; NoComparison>]
type DocsRepository =
    | GitHubRepository of url: string
    | RepositoryLink of label: string * url: string

/// <category>layouts</category>
module DocsRepository =
    let github url = GitHubRepository url
    let link label url = RepositoryLink(label, url)

/// <category>layouts</category>
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

/// <summary>
/// Consumer-owned branding, navigation, theme, assets, and typed destinations for a documentation site.
/// </summary>
/// <category>layouts</category>
[<NoEquality; NoComparison>]
type DocsSite<'destination> =
    { name: string
      baseUrl: string option
      description: string option
      repository: DocsRepository option
      brandMark: HtmlElement
      homeId: string
      navigation: NavNode<'destination> list
      storageKey: string
      defaultColorMode: DocsColorMode
      theme: DocsTheme
      assets: DocsAssets
      search: DocsSearchResult list }

/// <category>layouts</category>
[<RequireQualifiedAccess>]
module DocsSite =
    let create name homeId : DocsSite<'destination> =
        { name = name
          baseUrl = None
          description = None
          repository = None
          brandMark =
            span {
                _ariaHidden true
                name
            }
          homeId = homeId
          navigation = []
          storageKey = "documentation"
          defaultColorMode = DocsColorMode.System
          theme = DocsTheme.amber
          assets = DocsAssets.defaults
          search = [] }

    let withNavigation navigation (site: DocsSite<'destination>) = { site with navigation = navigation }
    let withBrandMark brandMark (site: DocsSite<'destination>) = { site with brandMark = brandMark }

    let withDescription description (site: DocsSite<'destination>) =
        { site with
            description = Some description }

    let withBaseUrl baseUrl (site: DocsSite<'destination>) = { site with baseUrl = Some baseUrl }
    let withStorageKey storageKey (site: DocsSite<'destination>) = { site with storageKey = storageKey }
    let withTheme theme (site: DocsSite<'destination>) = { site with theme = theme }
    let withAssets assets (site: DocsSite<'destination>) = { site with assets = assets }
    let withSearch search (site: DocsSite<'destination>) = { site with search = search }

module private Icons =
    let menu = fveIcon { _name IconName.Bars3; _variant IconVariant.Mini }
    let close = fveIcon { _name IconName.XMark; _variant IconVariant.Mini }
    let chevron = fveIcon { _name IconName.ChevronRight; _variant IconVariant.Mini }
    let breadcrumbChevron = fveIcon { _name IconName.ChevronRight; _variant IconVariant.Micro }
    let ellipsis = fveIcon { _name IconName.EllipsisHorizontal; _variant IconVariant.Mini }
    let externalLink = fveIcon { _name IconName.ArrowTopRightOnSquare; _variant IconVariant.Micro }
    let github = fveIcon { _name IconName.GitHub; _size Size.Medium }
    let sun = fveIcon { _name IconName.Sun; _variant IconVariant.Mini }
    let moon = fveIcon { _name IconName.Moon; _variant IconVariant.Mini }
    let monitor = fveIcon { _name IconName.ComputerDesktop; _variant IconVariant.Mini }
    let check = fveIcon { _name IconName.Check; _variant IconVariant.Mini }

module private ViewHelpers =
    let jsString (value: string) = JsonSerializer.Serialize(value)

    let themeStyle theme =
        $"--fve-brand-subtle:{theme.accent100};--fve-brand-solid:{theme.accent700};--fve-brand-hover:{theme.accent900};--fve-brand-active:{theme.accent900};--fve-brand-text:{theme.accent900};--fve-brand-ring:{theme.accent500}"

module private ViewStyles =
    let iconButton =
        "grid size-8 shrink-0 cursor-pointer place-items-center rounded-lg border border-transparent bg-transparent p-1 text-[var(--fve-muted-text)] no-underline [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] [&_svg]:size-[1.125rem]"

    let sectionContent = "flex flex-col gap-4 text-[var(--fve-text)]"

    let codeSurface =
        "bg-[var(--fve-docs-code-surface,var(--fve-background))] text-[var(--fve-text)]"

    let tocLinks =
        "flex min-w-0 flex-col gap-2 [&>a]:min-w-0 [&>a]:whitespace-normal [&>a]:[overflow-wrap:anywhere] [&>a]:text-sm [&>a]:leading-5 [&>a]:text-[var(--fve-muted-text)] [&>a]:no-underline [&>a:hover]:text-[var(--fve-brand-text)] [&>a[aria-current=location]]:font-semibold [&>a[aria-current=location]]:text-[var(--fve-brand-text)]"

module private DocsSectionView =
    let render showHeading (docSection: DocumentationSectionConfig) =
        section {
            if not showHeading then
                _id docSection.id
                _tabindex -1

            if showHeading then
                div {
                    _id docSection.id
                    _class "mb-5 scroll-mt-20"
                    _tabindex -1

                    let classes =
                        if docSection.level <= 2 then
                            "m-0 text-xl font-semibold tracking-tight text-[var(--fve-text)]"
                        elif docSection.level = 3 then
                            "m-0 text-base font-semibold tracking-tight text-[var(--fve-text)]"
                        else
                            "m-0 text-sm font-semibold tracking-tight text-[var(--fve-text)]"

                    if docSection.level <= 2 then
                        h2 {
                            _class classes
                            docSection.title
                        }
                    elif docSection.level = 3 then
                        h3 {
                            _class classes
                            docSection.title
                        }
                    else
                        h4 {
                            _class classes
                            docSection.title
                        }
                }

            div {
                _class ViewStyles.sectionContent
                _data ("docs-section-content", "true")

                for element in docSection.content do
                    element
            }
        }

module private ColorModeView =
    let mode =
        function
        | DocsColorMode.System -> ColorMode.System
        | DocsColorMode.Light -> ColorMode.Light
        | DocsColorMode.Dark -> ColorMode.Dark

    let render defaultMode =
        div {
            _dataSignals ("_docs_color_mode_mode", ["ifmissing"], "window.fveColorMode.current()")
            _dataInit "$_docs_color_mode_mode = window.fveColorMode.current()"
            _dataOn ("fve-color-mode", ["window"], "$_docs_color_mode_mode = window.fveColorMode.current()")

            fveDropdownMenu {
                _id "docs-color-mode"

                fveDropdownMenuTrigger {
                    _ariaLabel "Choose color theme"

                    fun (attributes: HtmlAttribute list) ->
                        button {
                            for attribute in attributes do
                                attribute

                            _class ViewStyles.iconButton

                            span {
                                _class "inline-flex dark:hidden"
                                Icons.sun
                            }

                            span {
                                _class "hidden dark:inline-flex"
                                Icons.moon
                            }
                        }
                }

                for choice, caption in [ ColorMode.System, "System"; ColorMode.Light, "Light"; ColorMode.Dark, "Dark" ] do
                    let value = ColorMode.value choice
                    let isChecked = choice = mode defaultMode

                    fveDropdownMenuItem {
                        _role "menuitemradio"
                        _ariaChecked isChecked
                        _dataAttr ("aria-checked", $"$_docs_color_mode_mode == '{value}' ? 'true' : 'false'")
                        _dataOn ("click", $"window.fveColorMode.set('{value}')")
                        caption
                    }
            }
        }

module private RepositoryView =
    let render =
        function
        | GitHubRepository url ->
            a {
                _href url
                _ariaLabel "View repository on GitHub"
                _title "View repository on GitHub"
                _class ViewStyles.iconButton
                Icons.github
            }
        | RepositoryLink(label, url) ->
            a {
                _href url

                _class
                    "shrink-0 rounded-lg border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 py-1.5 text-sm font-semibold text-[var(--fve-muted-text)] no-underline shadow-sm [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                label
            }

module private NavigationView =
    open ViewHelpers

    let sideNav (site: DocsSite<'destination>) (items: NavNode<'destination> list) activeId =
        let rec item =
            function
            | NavNode.Page page ->
                fveSideNavItem {
                    _href page.href
                    page.label
                    _id $"nav-{page.id}"

                    _data ("docs-nav-link", "true")
                    _data ("selected", string (page.id = activeId) |> _.ToLowerInvariant())
                }
            | NavNode.ExternalLink(id, label, href) ->
                fveSideNavItem {
                    _href href
                    label
                    _id $"nav-{id}"

                    fveSideNavBadge {
                        span {
                            _id $"nav-{id}-hint"
                            _class "flex size-4 items-center justify-center"
                            _title "Opens in a new tab"
                            Icons.externalLink

                            span {
                                _class "sr-only"
                                "Opens in a new tab"
                            }
                        }
                    }

                    _target "_blank"
                    _rel "noopener"
                    _ariaDescribedby $"nav-{id}-hint"
                }
            | NavNode.Group group ->
                fveSideNavGroup {
                    _label group.label
                    _id $"nav-{group.id}"
                    _expanded group.defaultOpen

                    for child in group.children do
                        item child
                }

        let header =
            fveSideNavHeader {
                _label site.name

                div {
                    _class "flex min-w-0 items-center gap-2.5"

                    div {
                        _class
                            "flex size-7 shrink-0 items-center justify-center [&>img]:max-h-full [&>img]:max-w-full [&>svg]:max-h-full [&>svg]:max-w-full"

                        site.brandMark
                    }

                    span {
                        _class "min-w-0 truncate text-sm font-semibold"
                        site.name
                    }
                }
            }

        let nodes = List.map item items

        let current =
            items
            |> NavNode.collectPages
            |> List.tryFind (NavNode.id >> (=) activeId)
            |> Option.bind NavNode.href

        let config =
            fveSideNav {
                _id "side-nav"
                _ariaLabel "Documentation"
                _persistenceKey site.storageKey

                match current with
                | Some href -> _value href
                | None -> ()

                header
                fveSideNavContent { yield! nodes }
            }
            |> SideNavElements.configuration

        let close =
            button {
                _type "button"
                _ariaLabel "Close navigation"
                _class $"{ViewStyles.iconButton} lg:hidden"
                _data ("docs-nav-close", "true")
                _dataOn ("click", "$sideNavOpen = false; window.fsharpDocsMobileNav.close()")
                Icons.close
            }

        SideNavView.render
            "fixed inset-y-0 left-0 z-50 flex hidden h-dvh w-[min(18rem,calc(100vw-3rem))] flex-col border-r border-[var(--fve-border)] bg-[var(--fve-background)] shadow-xl lg:relative lg:flex lg:h-full lg:w-full lg:shadow-none"
            [ _ariaLabel "Documentation navigation"
              _dataClass ("hidden", "!$sideNavOpen")
              _data ("docs-side-nav", "true") ]
            (Some close)
            None
            id
            config

    let topNav (site: DocsSite<'destination>) (breadcrumbs: Breadcrumb list) =
        let items =
            breadcrumbs
            |> List.map (fun crumb ->
                match crumb.href with
                | Some href ->
                    fveBreadcrumbItem {
                        _href href
                        crumb.label
                    }
                | None -> fveBreadcrumbItem { crumb.label })

        fvePageTopBar {
            _data ("docs-top-nav", "true")

            div {
                _class "flex min-w-0 items-center gap-3"

                button {
                    _type "button"
                    _ariaLabel "Open navigation"
                    _ariaControls "side-nav"
                    _dataAttr ("aria-expanded", "$sideNavOpen ? 'true' : 'false'")
                    _class $"{ViewStyles.iconButton} lg:hidden"
                    _dataOn ("click", "$sideNavOpen = true; window.fsharpDocsMobileNav.open(evt.currentTarget)")
                    Icons.menu
                }

                fveBreadcrumbs {
                    _id "docs-breadcrumbs"
                    _ariaLabel "Breadcrumb"
                    yield! items
                }
            }

            fvePageTopBarActions {
                div {
                    _class "flex shrink-0 items-center gap-1.5 max-sm:gap-0.5"
                    _data ("docs-top-actions", "true")

                    if not site.search.IsEmpty then
                        SearchView.render site.search

                    ColorModeView.render site.defaultColorMode

                    match site.repository with
                    | Some repository -> RepositoryView.render repository
                    | None -> ()
                }
            }
        }

type private TocItem =
    { level: int
      label: string
      href: string }

module private TocView =
    let private links (className: string) items =
        nav {
            _ariaLabel "On this page"
            _class className
            _data ("docs-toc", "true")

            for item in items do
                a {
                    _href item.href

                    _data (
                        "on:click",
                        "window.fsharpDocsFragments.navigate(evt, evt.currentTarget.getAttribute('href'))"
                    )

                    _class (
                        if item.level <= 2 then ""
                        elif item.level = 3 then "pl-3"
                        else "pl-6"
                    )

                    item.label
                }
        }

    let desktop (items: TocItem list) =
        aside {
            _class "hidden h-full min-h-0 min-w-0 w-full overflow-x-hidden overflow-y-auto px-4 py-8 xl:block"
            _data ("docs-toc-rail", "true")

            div {
                _class "min-w-0"

                div {
                    _class "mb-3 text-xs font-semibold tracking-[0.14em] text-[var(--fve-muted-text)] uppercase"
                    "On this page"
                }

                links ViewStyles.tocLinks items
            }
        }

    let mobile (items: TocItem list) =
        details {
            _class
                "mb-6 block rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] xl:hidden [&>summary]:cursor-pointer [&>summary]:px-4 [&>summary]:py-3 [&>summary]:text-sm [&>summary]:font-semibold [&>summary]:text-[var(--fve-text)] [&>summary:focus-visible]:outline-2 [&>summary:focus-visible]:outline-offset-2 [&>summary:focus-visible]:outline-[var(--fve-brand-ring)]"

            _data ("docs-mobile-toc", "true")
            _ariaLabel "On this page"
            summary { "On this page" }

            links
                $"{ViewStyles.tocLinks} border-t border-[var(--fve-border)] p-2 gap-0.5 [&>a]:rounded-md [&>a]:px-2 [&>a]:py-2 [&>a:hover]:bg-[var(--fve-surface-hover)] [&>a:focus-visible]:bg-[var(--fve-surface-hover)] [&>a:focus-visible]:outline-none"
                items
        }

module private PagerView =
    open ViewHelpers

    let private renderLink (direction: string) (relation: string) (isNext: bool) (link: DocsPageLink) =
        let linkAlignment = if isNext then "sm:col-start-2 text-right" else ""
        let directionAlignment = if isNext then "justify-end" else ""

        a {
            _rel relation
            _href link.href

            _class
                $"flex min-w-0 flex-col gap-1 rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 text-[var(--fve-text)] no-underline shadow-sm hover:border-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] {linkAlignment}"

            span {
                _class
                    $"flex items-center gap-1 text-xs font-semibold text-[var(--fve-muted-text)] {directionAlignment}"

                if relation = "prev" then
                    span {
                        _class "grid rotate-180 place-items-center text-[var(--fve-muted-text)] [&>svg]:size-3.5"
                        Icons.chevron
                    }

                span { direction }

                if relation = "next" then
                    span {
                        _class "grid place-items-center text-[var(--fve-muted-text)] [&>svg]:size-3.5"
                        Icons.chevron
                    }
            }

            span {
                _class "[overflow-wrap:anywhere] text-base font-semibold text-[var(--fve-brand-text)]"
                link.label
            }
        }

    let render (pager: DocsPager) =
        nav {
            _ariaLabel "Page navigation"
            _class "grid grid-cols-1 gap-4 border-t border-[var(--fve-border)] pt-6 sm:grid-cols-2"

            match pager.previousPage with
            | Some previousPage -> renderLink "Previous" "prev" false previousPage
            | None -> ()

            match pager.nextPage with
            | Some nextPage -> renderLink "Next" "next" true nextPage
            | None -> ()
        }

/// <category>layouts</category>
module DocsView =
    open ViewHelpers

    /// Catalog-only theme menu shared by the documentation shell and example viewer chrome.
    let colorModeControl defaultMode = ColorModeView.render defaultMode

    let private tocItems (page: DocumentationPageConfig) : TocItem list =
        page.sections
        |> List.map (fun section ->
            { level = section.level
              label = section.title
              href = $"#{section.id}" })

    let content (page: DocumentationPageConfig) =
        div {
            _class (
                match page.layout with
                | Gallery -> "flex flex-col gap-8"
                | Article
                | Reference
                | Canvas -> "flex flex-col gap-12"
            )

            match page.heading with
            | Visible ->
                section {
                    match page.headingAdornment with
                    | Some adornment -> adornment
                    | None -> ()

                    h1 {
                        _class
                            "m-0 max-w-3xl [overflow-wrap:anywhere] text-4xl leading-[1.1] font-semibold tracking-tight text-[var(--fve-text)]"

                        page.title
                    }

                    if page.metadata.version.IsSome || page.metadata.deprecated then
                        div {
                            _class "mt-3.5 flex flex-wrap gap-2"

                            match page.metadata.version with
                            | Some version ->
                                span {
                                    _class
                                        "inline-flex rounded-full bg-[var(--fve-brand-subtle)] px-2 py-1 text-xs font-bold text-[var(--fve-brand-text)]"

                                    _data ("docs-version", version)
                                    version
                                }
                            | None -> ()

                            if page.metadata.deprecated then
                                span {
                                    _class
                                        "inline-flex rounded-full bg-amber-100 px-2 py-1 text-xs font-bold text-amber-800 dark:bg-amber-950 dark:text-amber-200"

                                    _data ("docs-deprecated", "true")
                                    "Deprecated"
                                }
                        }

                    if not (String.IsNullOrWhiteSpace page.description) then
                        p {
                            _class "mt-4 max-w-3xl leading-7 text-[var(--fve-muted-text)]"
                            page.descriptionContent |> Option.defaultWith (fun () -> text page.description)
                        }

                    if page.metadata.lastUpdated.IsSome || page.metadata.editUrl.IsSome then
                        div {
                            _class
                                "mt-3 flex flex-wrap gap-4 text-sm text-[var(--fve-muted-text)] [&_a]:font-semibold [&_a]:text-[var(--fve-brand-text)] [&_a]:underline [&_a]:underline-offset-2"

                            match page.metadata.lastUpdated with
                            | Some lastUpdated ->
                                span {
                                    "Last updated "

                                    time {
                                        _datetime lastUpdated
                                        lastUpdated
                                    }
                                }
                            | None -> ()

                            match page.metadata.editUrl with
                            | Some editUrl ->
                                a {
                                    _href editUrl
                                    "Edit this page"
                                }
                            | None -> ()
                        }
                }
            | VisuallyHidden ->
                h1 {
                    _class "sr-only"
                    _data ("docs-visually-hidden-heading", "true")
                    page.title
                }

            for element in page.lead do
                element

            let items = tocItems page

            match page.rightRail with
            | TableOfContents when not items.IsEmpty -> TocView.mobile items
            | _ -> ()

            for section in page.sections do
                DocsSectionView.render (page.layout <> Gallery) section

            match page.pager with
            | Some pager -> PagerView.render pager
            | None -> ()
        }

    let private defaultBreadcrumbs (site: DocsSite<'destination>) (page: DocumentationPageConfig) =
        Navigation.breadcrumbs site.navigation site.homeId page.activeId

    let sideNavWith (site: DocsSite<'destination>) (items: NavNode<'destination> list) (page: DocumentationPageConfig) =
        NavigationView.sideNav site items page.activeId

    let sideNav (site: DocsSite<'destination>) (page: DocumentationPageConfig) = sideNavWith site site.navigation page

    let private railResizeHandle =
        "group relative z-10 hidden w-1 shrink-0 touch-none cursor-col-resize items-center justify-center bg-[var(--fve-border)] outline-none [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"

    let pageContentWith (site: DocsSite<'destination>) (breadcrumbs: Breadcrumb list) (page: DocumentationPageConfig) =
        let items = tocItems page

        let layoutName, layoutClasses, mainClasses, mainInnerClasses =
            match page.layout with
            | Article -> "article", "", "", "max-w-4xl"
            | Reference -> "reference", "max-xl:block max-xl:overflow-y-auto", "max-xl:overflow-visible", "max-w-4xl"
            | Canvas -> "canvas", "", "", "max-w-none"
            | Gallery -> "gallery", "", "", "max-w-none"

        div {
            _id "page-content"
            _class "flex min-w-0 flex-1 flex-col overflow-hidden"
            NavigationView.topNav site breadcrumbs

            div {
                _class "min-h-0 flex-1 overflow-hidden"
                _data ("docs-page-viewport", "true")

                div {
                    _class $"flex h-full min-h-0 {layoutClasses}"
                    _data ("docs-page-layout", "true")
                    _data ("docs-layout", layoutName)

                    let mainView =
                        main {
                            _id "main-content"

                            _class
                                $"min-h-0 min-w-0 flex-1 overflow-y-auto bg-[var(--fve-background)] px-4 py-10 outline-none sm:px-6 lg:px-10 {mainClasses}"

                            _data ("docs-main", "true")
                            _tabindex -1

                            div {
                                _class $"mx-auto {mainInnerClasses}"
                                _data ("docs-main-inner", "true")
                                content page
                            }
                        }

                    match page.rightRail with
                    | TableOfContents when not items.IsEmpty ->
                        fveResizable {
                            _id "docs-content-panels"
                            _ariaLabel "On this page width"
                            _framed false
                            _minSize 55
                            _maxSize 85
                            _initialSize 80
                            _persistenceKey "fsharp-viewengine-docs-toc-width"
                            _class "h-full w-full max-xl:contents"

                            fveResizablePanel {
                                _class "flex flex-col overflow-hidden max-xl:contents"
                                mainView
                            }

                            fveResizableHandle { _class (railResizeHandle + " xl:flex") }

                            fveResizablePanel {
                                _class "hidden overflow-hidden xl:flex"
                                TocView.desktop items
                            }
                        }
                    | TableOfContents
                    | NoRail -> mainView
                    | CustomRail rail ->
                        mainView

                        aside {
                            _class
                                $"w-128 shrink-0 overflow-y-auto border-l border-[var(--fve-border)] {ViewStyles.codeSurface} max-xl:w-auto max-xl:overflow-visible max-xl:border-t max-xl:border-l-0"

                            _data ("docs-custom-rail", "true")

                            div {
                                _class
                                    "flex min-h-full flex-col gap-4 px-6 py-8 max-xl:mx-auto max-xl:min-h-0 max-xl:max-w-4xl max-sm:px-4 max-sm:py-5 [&>div]:flex [&>div]:min-w-0 [&>div]:flex-col [&>div]:gap-4"

                                rail
                            }
                        }
                }
            }
        }

    let pageContent (site: DocsSite<'destination>) (page: DocumentationPageConfig) =
        pageContentWith site (defaultBreadcrumbs site page) page

    let pageWithNavigation
        (site: DocsSite<'destination>)
        (breadcrumbs: Breadcrumb list)
        (sideNavItems: NavNode<'destination> list)
        (docPage: DocumentationPageConfig)
        =
        div {
            _id "page"
            _class "flex h-dvh overflow-hidden bg-[var(--fve-background)]"
            _data ("docs-shell", "true")

            a {
                _href "#main-content"

                _class
                    "fixed top-2 left-2 z-100 -translate-y-[200%] rounded-md bg-[var(--fve-surface)] px-3 py-2 font-semibold text-[var(--fve-text)] no-underline shadow-lg focus:translate-y-0"

                "Skip to main content"
            }

            button {
                _type "button"
                _ariaLabel "Close navigation overlay"
                _class "fixed inset-0 z-40 hidden border-0 bg-black/50 backdrop-blur-[2px] lg:hidden"
                _data ("docs-overlay", "true")
                _dataClass ("hidden", "!$sideNavOpen")
                _dataOn ("click", "$sideNavOpen = false; window.fsharpDocsMobileNav.close()")
            }

            fveResizable {
                _id "docs-navigation-panels"
                _ariaLabel "Documentation navigation width"
                _framed false
                _minSize 12
                _maxSize 28
                _initialSize 19
                _persistenceKey "fsharp-viewengine-docs-navigation-width"
                _class "h-full w-full"

                fveResizablePanel {
                    _class "overflow-hidden max-lg:contents"
                    sideNavWith site sideNavItems docPage
                }

                fveResizableHandle { _class (railResizeHandle + " lg:flex") }

                fveResizablePanel {
                    _class "flex overflow-hidden"
                    pageContentWith site breadcrumbs docPage
                }
            }
        }

    let page (site: DocsSite<'destination>) (docPage: DocumentationPageConfig) =
        pageWithNavigation site (defaultBreadcrumbs site docPage) site.navigation docPage

    let private previousIcon = fveIcon { _name IconName.ChevronLeft; _variant IconVariant.Mini }

    let private nextIcon = fveIcon { _name IconName.ChevronRight; _variant IconVariant.Mini }

    let private dockIcons =
        fragment {
            span {
                _dataShow "!$appModeDockTop"
                _class "inline-flex"

                fveIcon { _name IconName.ArrowUp; _variant IconVariant.Mini }
            }

            span {
                _dataShow "$appModeDockTop"
                _class "inline-flex"
                _style "display:none"

                fveIcon { _name IconName.ArrowDown; _variant IconVariant.Mini }
            }
        }

    let private appModeDirection (frameId: string) (label: string) (icon: HtmlElement) (link: FixtureLink option) =
        match link with
        | Some value ->
            let accessibleLabel = $"{label}: {FixtureLink.label value}"

            a {
                _href (Fixture.appModeHref frameId (FixtureLink.href value))
                _ariaLabel accessibleLabel
                _title accessibleLabel
                icon
            }
        | None ->
            span {
                _ariaDisabled true
                _ariaLabel $"No {label.ToLowerInvariant()} workflow step"
                icon
            }

    let private appModeView (site: DocsSite<'destination>) (request: AppMode) (fixture: FixtureConfig) =
        let frameId = Fixture.id fixture
        let states = Fixture.states fixture
        let currentState = states |> List.tryFind FixtureState.isCurrent

        let rootClasses =
            "fixed inset-0 z-100 overflow-auto bg-[var(--fve-background)] text-[var(--fve-text)]"

        fragment {
            div {
                _id "fve-app-mode-root"
                _class rootClasses
                _attr ("data-fve-app-mode-root", "true")
                _attr ("data-fve-app-mode-frame", frameId)
                _ariaLabel (Fixture.label fixture)
                Fixture.fullscreenContent fixture
            }

            nav {
                _id "fve-app-mode-controls"

                _class (
                    FSharp.ViewEngine.Components.ComponentsTheme.sky
                    |> FSharp.ViewEngine.Components.ComponentsTheme.withDensity
                        FSharp.ViewEngine.Components.Density.Compact
                    |> FSharp.ViewEngine.Components.ComponentsTheme.withControlSize
                        FSharp.ViewEngine.Components.Size.Small
                    |> FSharp.ViewEngine.Components.ComponentsTheme.className
                    |> fun theme ->
                        theme
                        + " fixed right-[max(0.75rem,env(safe-area-inset-right))] bottom-[max(0.5rem,env(safe-area-inset-bottom))] z-[110] flex min-h-10 max-w-[calc(100vw-16px)] flex-wrap items-center justify-end gap-0.5 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[color-mix(in_srgb,var(--fve-surface)_92%,transparent)] p-1 text-xs leading-none font-semibold text-[var(--fve-text)] shadow-[0_12px_32px_rgb(0_0_0/20%)] backdrop-blur-[14px] data-[fve-app-dock=top]:top-[max(0.5rem,env(safe-area-inset-top))] data-[fve-app-dock=top]:bottom-auto max-[32rem]:right-2 max-[32rem]:bottom-2 max-[32rem]:data-[fve-app-dock=top]:top-2 [&>a]:grid [&>a]:size-[32px] [&>a]:shrink-0 [&>a]:cursor-pointer [&>a]:place-items-center [&>a]:rounded-[var(--fve-radius-control)] [&>a]:border-0 [&>a]:bg-transparent [&>a]:text-inherit [&>a]:no-underline [&>a:hover]:bg-[var(--fve-surface-hover)] [&>a:focus-visible]:outline-2 [&>a:focus-visible]:outline-offset-2 [&>a:focus-visible]:outline-[var(--fve-brand-ring)] [&>button]:grid [&>button]:size-[32px] [&>button]:shrink-0 [&>button]:cursor-pointer [&>button]:place-items-center [&>button]:rounded-[var(--fve-radius-control)] [&>button]:border-0 [&>button]:bg-transparent [&>button]:text-inherit [&>button:hover]:bg-[var(--fve-surface-hover)] [&>button:focus-visible]:outline-2 [&>button:focus-visible]:outline-offset-2 [&>button:focus-visible]:outline-[var(--fve-brand-ring)] [&>span[aria-disabled=true]]:grid [&>span[aria-disabled=true]]:size-[32px] [&>span[aria-disabled=true]]:place-items-center [&>span[aria-disabled=true]]:text-[var(--fve-muted-text)] [&_svg]:size-[16px]"
                )

                _attr ("data-fve-app-mode-controls", "true")
                // One viewer exists at a time; disconnect its previous observer before a navigation morph.
                _dataInit
                    "window.fsharpDocsDockObserver?.disconnect(); const measure = () => { if (!el.isConnected) { observer.disconnect(); return }; const root = document.getElementById('fve-app-mode-root'); if (!root) return; const rect = el.getBoundingClientRect(); const mobile = matchMedia('(width < 48rem)').matches; const top = mobile && el.dataset.fveAppDock === 'top'; root.style.top = top ? (rect.bottom + 8) + 'px' : '0px'; root.style.bottom = !mobile || top ? '0px' : (innerHeight - rect.top + 8) + 'px' }; const observer = new ResizeObserver(measure); window.fsharpDocsDockObserver = observer; el.fveMeasureDock = measure; observer.observe(el)"

                _dataEffect "$appModeDockTop; requestAnimationFrame(() => el.fveMeasureDock?.())"
                _dataOn ("resize", ["window"], "el.fveMeasureDock?.()")
                _dataAttr ("data-fve-app-dock", "$appModeDockTop ? 'top' : 'bottom'")

                _dataAttr (
                    "data-fve-color-mode",
                    "($colorMode == 'dark' || ($colorMode == 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches)) ? 'light' : 'dark'"
                )

                _data (
                    "on:fve-color-mode__window",
                    "document.getElementById('fve-app-mode-controls')?.setAttribute('data-fve-color-mode', document.documentElement.classList.contains('dark') ? 'light' : 'dark')"
                )

                _ariaLabel "App mode controls"

                let hasWorkflow =
                    Fixture.previous fixture |> Option.isSome
                    || Fixture.next fixture |> Option.isSome

                if hasWorkflow then
                    appModeDirection frameId "Previous" previousIcon (Fixture.previous fixture)

                if not states.IsEmpty then
                    div {
                        _class
                            "block min-w-0 max-w-full max-[32rem]:order-first max-[32rem]:basis-full [&_.fve-app-mode-state-trigger>span]:overflow-hidden [&_.fve-app-mode-state-trigger>span]:text-ellipsis [&_.fve-app-mode-state-trigger>span]:whitespace-nowrap [&_[role=menu]]:z-[120] [&_[role=menu]]:min-w-40 [&_[role=menu]]:border [&_[role=menu]]:border-[var(--fve-border)] [&_[role=menu]]:bg-[var(--fve-surface)] [&_[role=menu]]:shadow-xl"

                        _attr ("data-fve-app-mode-state-select", "true")

                        fveDropdownMenu {
                            _id $"fve-app-mode-{frameId}-state"
                            _ariaLabel "Review state"

                            fveDropdownMenuTrigger {
                                fveButton {
                                    _variant ButtonVariant.Ghost
                                    _size Size.Small
                                    _class "fve-app-mode-state-trigger w-auto max-w-full px-2 shadow-none"

                                    span {
                                        currentState
                                        |> Option.map FixtureState.label
                                        |> Option.defaultValue "Review state"
                                    }
                                }
                            }

                            yield!
                                (states
                                 |> List.map (fun state ->
                                     fveDropdownMenuItem {
                                         _href (Fixture.appModeHref frameId (FixtureState.href state))
                                         FixtureState.label state
                                     }))

                            _alignment FSharp.ViewEngine.Components.DropdownMenuAlignment.End
                        }
                    }

                if hasWorkflow then
                    appModeDirection frameId "Next" nextIcon (Fixture.next fixture)

                    span {
                        _class "mx-0.5 h-5 w-px bg-[var(--fve-border)]"
                        _attr ("data-fve-app-mode-divider", "true")
                        _ariaHidden true
                    }

                ColorModeView.render site.defaultColorMode

                button {
                    _type "button"

                    _dataAttr (
                        "aria-label",
                        "$appModeDockTop ? 'Move App mode controls to bottom' : 'Move App mode controls to top'"
                    )

                    _dataAttr (
                        "title",
                        "$appModeDockTop ? 'Move App mode controls to bottom' : 'Move App mode controls to top'"
                    )

                    _dataOn ("click", "$appModeDockTop = !$appModeDockTop")
                    dockIcons
                }

                a {
                    _href (Fixture.returnFocusHref frameId (AppMode.exitHref request))
                    _attr ("data-fve-app-mode-exit", "true")
                    _ariaLabel "Exit App mode"
                    _title "Exit App mode"
                    "×"
                }
            }
        }

    let documentMetadata (site: DocsSite<'destination>) (docPage: DocumentationPageConfig) =
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

        let browserTitle =
            docPage.metadata.browserTitle |> Option.defaultValue docPage.title

        let description =
            if String.IsNullOrWhiteSpace docPage.description then
                site.description
            else
                Some docPage.description

        fragment {
            title {
                _id "docs-document-title"
                browserTitle
            }

            match canonicalUrl with
            | Some url ->
                link {
                    _id "docs-canonical-url"
                    _rel "canonical"
                    _href url
                }

                meta {
                    _id "docs-og-url"
                    _property "og:url"
                    _content url
                }
            | None -> ()

            match description with
            | Some value ->
                meta {
                    _id "docs-description"
                    _name "description"
                    _content value
                }

                meta {
                    _id "docs-og-description"
                    _property "og:description"
                    _content value
                }

                meta {
                    _id "docs-twitter-description"
                    _name "twitter:description"
                    _content value
                }
            | None -> ()

            meta {
                _id "docs-og-type"
                _property "og:type"
                _content "website"
            }

            meta {
                _id "docs-og-site-name"
                _property "og:site_name"
                _content site.name
            }

            meta {
                _id "docs-og-title"
                _property "og:title"
                _content browserTitle
            }

            meta {
                _id "docs-twitter-title"
                _name "twitter:title"
                _content browserTitle
            }

            match docPage.metadata.socialImage with
            | Some image ->
                meta {
                    _id "docs-og-image"
                    _property "og:image"
                    _content image
                }

                meta {
                    _id "docs-og-image-alt"
                    _property "og:image:alt"
                    _content docPage.title
                }

                meta {
                    _id "docs-twitter-image"
                    _name "twitter:image"
                    _content image
                }

                meta {
                    _id "docs-twitter-image-alt"
                    _name "twitter:image:alt"
                    _content docPage.title
                }

                meta {
                    _id "docs-twitter-card"
                    _name "twitter:card"
                    _content "summary_large_image"
                }
            | None -> ()

            meta {
                _id "docs-robots"
                _name "robots"

                _content (
                    if docPage.metadata.noIndex then
                        "noindex"
                    else
                        "index,follow"
                )
            }
        }

    let navigationRootWithNavigation
        (site: DocsSite<'destination>)
        (breadcrumbs: Breadcrumb list)
        (sideNavItems: NavNode<'destination> list)
        (renderMode: FixtureRenderMode)
        (docPage: DocumentationPageConfig)
        =
        let initialize =
            [ "$sideNavOpen = false"
              "window.fveColorMode?.refresh()"
              "window.initializeDocsToc?.()"
              "window.fsharpDocsFragments?.show(window.location.hash)" ]
            |> String.concat "; "

        div {
            _id "docs-navigation-root"
            _attr ("data-fve-navigation-root", "true")
            _dataInit initialize

            match renderMode with
            | Fullscreen request ->
                match
                    docPage.fixtures
                    |> List.tryFind (fun fixture -> Fixture.id fixture = AppMode.frameId request)
                with
                | Some fixture -> appModeView site request fixture
                | None -> pageWithNavigation site breadcrumbs sideNavItems docPage
            | Embedded -> pageWithNavigation site breadcrumbs sideNavItems docPage
        }

    let private documentWithRoot
        (site: DocsSite<'destination>)
        (breadcrumbs: Breadcrumb list)
        (sideNavItems: NavNode<'destination> list)
        (renderMode: FixtureRenderMode)
        (docPage: DocumentationPageConfig)
        (customRoot: HtmlElement option)
        =
        let signals =
            "{ sideNavOpen: false, appModeDockTop: false, navigationPending: false, navigationTarget: '', colorMode: window.fveColorMode.current() }"

        let activeAppMode =
            match renderMode with
            | Embedded -> None
            | Fullscreen request ->
                docPage.fixtures
                |> List.tryFind (fun fixture -> Fixture.id fixture = AppMode.frameId request)
                |> Option.map (fun fixture -> request, fixture)

        let mermaidSecurity = jsString site.assets.mermaidSecurityLevel

        let mermaidScript =
            site.assets.mermaidScript |> Option.map jsString |> Option.defaultValue "null"

        let prismStylesheet =
            site.assets.prismStylesheet |> Option.map jsString |> Option.defaultValue "null"

        let prismScripts =
            site.assets.prismScripts
            |> List.map jsString
            |> String.concat ", "
            |> fun sources -> $"[{sources}]"

        let assetNonce =
            site.assets.nonce |> Option.map jsString |> Option.defaultValue "null"

        let colorMode = ColorModeView.mode site.defaultColorMode

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
window.fsharpDocsMermaid.security = __SECURITY__;
            """
            |> fun source ->
                source
                    .Replace("__MERMAID_SCRIPT__", mermaidScript)
                    .Replace("__ASSET_NONCE__", assetNonce)
                    .Replace("__SECURITY__", mermaidSecurity)

        let navigationScript =
            """
const markDocsCodeUnloading = () => { window.fsharpDocsCode.unloading = true; };
window.fsharpDocsCode = window.fsharpDocsCode ?? {
  stylesheet: __PRISM_STYLESHEET__,
  scripts: __PRISM_SCRIPTS__,
  nonce: __ASSET_NONCE__,
  loading: null,
  unloading: false,
  abandonOrReject(resolve, reject, source) {
    if (this.unloading) resolve();
    else reject(new Error(`Unable to load Prism asset: ${source}`));
  },
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
      link.addEventListener('error', () => this.abandonOrReject(resolve, reject, source), { once: true });
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
      script.addEventListener('error', () => this.abandonOrReject(resolve, reject, source), { once: true });
      document.head.append(script);
    });
  },
  async ensure() {
    if (!this.loading) {
      this.unloading = false;
      window.addEventListener('beforeunload', markDocsCodeUnloading);
      this.loading = (async () => {
        if (this.stylesheet) await this.loadStylesheet(this.stylesheet);
        if (this.unloading || window.Prism?.languages?.fsharp) return;
        window.Prism = window.Prism || {};
        window.Prism.manual = true;
        for (const source of this.scripts) {
          if (this.unloading) return;
          await this.loadScript(source);
        }
      })().finally(() => window.removeEventListener('beforeunload', markDocsCodeUnloading));
    }
    await this.loading;
  }
};
window.addEventListener('pagehide', markDocsCodeUnloading);
window.addEventListener('pageshow', () => { window.fsharpDocsCode.unloading = false; });
window.fsharpDocsPreviewColorMode = window.fsharpDocsPreviewColorMode ?? {
  resolved() {
    return document.documentElement.classList.contains('dark') ? 'dark' : 'light';
  },
  apply(frame) {
    try {
      const preview = frame.contentWindow;
      if (!preview || preview.location.origin !== window.location.origin || !preview.fveColorMode) return;
      const mode = this.resolved();
      const root = preview.document.documentElement;
      if (root.dataset.colorMode === mode && root.classList.contains('dark') === (mode === 'dark')) return;
      preview.fveColorMode.set(mode);
    } catch {}
  },
  wire(frame) {
    if (frame.dataset.docsColorModeWired !== 'true') {
      frame.dataset.docsColorModeWired = 'true';
      frame.addEventListener('load', () => this.apply(frame));
    }
    this.apply(frame);
  },
  sync(root) {
    for (const frame of root?.querySelectorAll?.('iframe[data-docs-preview-src]') ?? []) this.wire(frame);
  }
};
window.addEventListener('fve-color-mode', () => window.fsharpDocsPreviewColorMode.sync(document));
window.renderDocsPreview = (el) => {
  for (const frame of el?.querySelectorAll?.('iframe[data-docs-preview-src]') ?? []) {
    window.fsharpDocsPreviewColorMode.wire(frame);
    if (!frame.getAttribute('src')) frame.setAttribute('src', frame.dataset.docsPreviewSrc);
  }

};
window.renderInitialDocsPreviews = (el) => Promise.all(
  Array.from(el?.querySelectorAll?.('[data-docs-preview-initial="true"]') ?? [])
    .map(preview => window.renderDocsPreview(preview))
);
document.addEventListener('fve-example-preview', event => window.renderDocsPreview(event.detail.panel));
window.fsharpDocsFragments = {
  setCurrent(id) {
    for (const link of document.querySelectorAll('[data-docs-toc] a[href^="#"]')) {
      if (link.getAttribute('href') === `#${id}`) link.setAttribute('aria-current', 'location');
      else link.removeAttribute('aria-current');
    }
  },
  show(href) {
    if (!href?.startsWith('#')) return false;
    const target = document.getElementById(decodeURIComponent(href.slice(1)));
    if (!target) return false;
    target.scrollIntoView({ block: 'start' });
    target.focus({ preventScroll: true });
    this.setCurrent(target.id);
    return true;
  },
  navigate(event, href) {
    if (!href?.startsWith('#') || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) return;
    if (!document.getElementById(decodeURIComponent(href.slice(1)))) return;
    event.preventDefault();
    const scrollRoot = document.querySelector('[data-docs-main], [data-fve-app-mode-root]');
    const documentToken = document.getElementById('docs-navigation-root')?.dataset.navigationDocument;
    window.history.replaceState(Object.assign({}, window.history.state || {}, { fveDocsDocument: documentToken, fveDocsScroll: [scrollRoot?.scrollLeft ?? window.scrollX, scrollRoot?.scrollTop ?? window.scrollY] }), '', window.location.href);
    window.history.pushState({ fveDocsDocument: documentToken, fveDocsScroll: [0, 0] }, '', href);
    this.show(href);
    window.history.replaceState({ fveDocsDocument: documentToken, fveDocsScroll: [scrollRoot?.scrollLeft ?? window.scrollX, scrollRoot?.scrollTop ?? window.scrollY] }, '', window.location.href);
    event.currentTarget.closest('details')?.removeAttribute('open');
  }
};
window.fsharpDocsToc = window.fsharpDocsToc ?? {
  observer: null,
  layoutObserver: null,
  scrollRoot: null,
  scrollHandler: null,
  initialize() {
    this.observer?.disconnect();
    this.layoutObserver?.disconnect();
    if (this.scrollRoot && this.scrollHandler) this.scrollRoot.removeEventListener('scroll', this.scrollHandler);
    const root = document.querySelector('[data-docs-main]');
    const links = Array.from(document.querySelectorAll('[data-docs-toc] a[href^="#"]'));
    const sections = links.map(link => document.getElementById(decodeURIComponent(link.hash.slice(1)))).filter(Boolean);
    if (!root || sections.length === 0) return;
    const finalSection = sections.at(-1);
    const update = () => {
      if (!root.isConnected || !finalSection.isConnected) return;
      const rootBounds = root.getBoundingClientRect();
      const finalBounds = finalSection.getBoundingClientRect();
      const atEnd = root.scrollTop + root.clientHeight >= root.scrollHeight - 2;
      const finalSectionVisible = finalBounds.top < rootBounds.bottom && finalBounds.bottom > rootBounds.top;
      const current = atEnd || finalSectionVisible ? finalSection : (sections.filter(section => section.getBoundingClientRect().top <= rootBounds.top + 160).at(-1) ?? sections[0]);
      window.fsharpDocsFragments.setCurrent(current.id);
    };
    this.observer = new IntersectionObserver(update, { root, rootMargin: '-96px 0px -65% 0px', threshold: [0, 1] });
    for (const section of sections) this.observer.observe(section);
    this.layoutObserver = new ResizeObserver(update);
    this.layoutObserver.observe(root.querySelector('[data-docs-main-inner]') ?? root);
    this.scrollRoot = root;
    this.scrollHandler = update;
    root.addEventListener('scroll', update, { passive: true });
    document.fonts?.ready.then(() => { if (this.scrollRoot === root) update(); });
    update();
  }
};
window.initializeDocsToc = () => window.fsharpDocsToc.initialize();
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
    requestAnimationFrame(() => document.querySelector('#side-nav [data-docs-nav-close]')?.focus());
  },
  close() {
    const content = document.getElementById('page-content');
    content?.removeAttribute('inert');
    requestAnimationFrame(() => this.opener?.focus());
  },
  trap(event) {
    if (event.key !== 'Tab' || !document.getElementById('side-nav') || document.getElementById('side-nav').classList.contains('hidden')) return;
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

            """
            |> fun source ->
                source
                    .Replace("__PRISM_STYLESHEET__", prismStylesheet)
                    .Replace("__PRISM_SCRIPTS__", prismScripts)
                    .Replace("__ASSET_NONCE__", assetNonce)

        let nonceAttribute () =
            match site.assets.nonce with
            | Some nonce -> _attr ("nonce", nonce)
            | None -> Html.EmptyAttr

        html {
            _lang "en"

            match site.assets.nonce with
            | Some nonce -> _attr ("data-nonce", nonce)
            | None -> ()

            _style (themeStyle site.theme)

            head {
                meta { _charset "utf-8" }

                meta {
                    _name "viewport"
                    _content "width=device-width, initial-scale=1"
                }

                meta {
                    _name "theme-color"
                    _content site.theme.themeColor
                }

                documentMetadata site docPage
                ColorMode.assetsWithNonce $"{site.storageKey}-color-mode" colorMode site.assets.nonce
                SideNavPreferences.assetsWithNonce site.assets.nonce

                match site.assets.prismStylesheet with
                | Some stylesheet ->
                    link {
                        _rel "stylesheet"
                        _href stylesheet
                    }
                | None -> ()

                for stylesheet in site.assets.productStylesheets do
                    link {
                        _rel "stylesheet"
                        _href stylesheet
                    }

                match site.assets.mermaidScript with
                | Some _ ->
                    script {
                        nonceAttribute ()
                        raw mermaidInitialization
                    }
                | None -> ()

                script {
                    nonceAttribute ()
                    raw navigationScript
                }

                match site.assets.navigation with
                | Some enhancement ->
                    script {
                        nonceAttribute ()
                        raw enhancement.initialScript
                    }
                | None -> ()

                match site.assets.datastarScript with
                | Some source ->
                    script {
                        _type "module"
                        _src source
                        nonceAttribute ()
                    }
                | None -> ()

                for element in site.assets.additionalHead do
                    element
            }

            body {
                _class (
                    FSharp.ViewEngine.Components.ComponentsTheme.sky
                    |> FSharp.ViewEngine.Components.ComponentsTheme.withDensity
                        FSharp.ViewEngine.Components.Density.Compact
                    |> FSharp.ViewEngine.Components.ComponentsTheme.className
                    |> fun theme ->
                        theme
                        + " m-0 [--fve-docs-code-surface:color-mix(in_oklch,var(--fve-background)_72%,var(--fve-surface-subtle))] bg-[var(--fve-background)] font-sans text-[var(--fve-text)] antialiased"
                )

                _dataSignals signals
                _dataOn ("fve-color-mode", ["window"], "$colorMode = window.fveColorMode.current()")

                match site.assets.navigation with
                | Some enhancement ->
                    _dataOn ("click", enhancement.clickAction)
                    _dataOn ("submit", enhancement.submitAction)
                    _dataOn ("popstate", ["window"], enhancement.restoreAction)
                    _dataOn ("datastar-fetch", enhancement.fetchLifecycle)
                | None -> ()

                match activeAppMode, site.assets.navigation with
                | Some(request, _), Some enhancement ->
                    _attr ("data-fve-app-mode-frame", AppMode.frameId request)
                    _dataOn ("keydown", ["window"], enhancement.appModeExitAction)
                | Some(request, _), None -> _attr ("data-fve-app-mode-frame", AppMode.frameId request)
                | None, _ ->
                    _data (
                        "on:keydown__window",
                        "evt.key == 'Escape' ? ($sideNavOpen = false, window.fsharpDocsMobileNav.close()) : window.fsharpDocsMobileNav.trap(evt)"
                    )

                match site.assets.navigation with
                | Some _ ->
                    div {
                        _id "docs-navigation-status"
                        _role "status"
                        _hidden true

                        _class
                            "fixed right-4 bottom-4 z-[120] max-w-sm rounded-lg border border-[var(--fve-critical-ring)] bg-[var(--fve-surface)] px-4 py-3 text-sm font-semibold text-[var(--fve-critical-text)] shadow-lg"
                    }
                | None -> ()

                match customRoot with
                | Some root -> root
                | None -> navigationRootWithNavigation site breadcrumbs sideNavItems renderMode docPage

                script {
                    nonceAttribute ()

                    raw
                        "document.addEventListener('DOMContentLoaded', () => { window.initializeDocsToc?.(); window.fsharpDocsFragments?.show(window.location.hash); });"
                }
            }
        }

    let documentWithNavigation site breadcrumbs sideNavItems renderMode docPage =
        documentWithRoot site breadcrumbs sideNavItems renderMode docPage None

    // Catalog host reuse only: templates supply their own full-page markup, not the documentation shell.
    let documentWithContent site docPage root =
        documentWithRoot site [] site.navigation Embedded docPage (Some root)

    let document (site: DocsSite<'destination>) (docPage: DocumentationPageConfig) =
        documentWithNavigation site (defaultBreadcrumbs site docPage) site.navigation Embedded docPage

/// <summary>
/// Immutable builders for article, reference, canvas, and gallery documentation pages.
/// </summary>
/// <category>layouts</category>
[<RequireQualifiedAccess>]
module DocumentationPage =
    let create activeId title =
        DocumentationPageModel.create activeId title "" Visible Article TableOfContents []

    let validate (page: DocumentationPageConfig) = DocumentationPageModel.validate page

    let withDescription description (page: DocumentationPageConfig) = { page with description = description }
    let withDescriptionContent content (page: DocumentationPageConfig) = { page with descriptionContent = Some content }
    let withLayout layout (page: DocumentationPageConfig) = { page with layout = layout }
    let withRightRail rightRail (page: DocumentationPageConfig) = { page with rightRail = rightRail }
    /// Render content after the page heading and before outlined sections.
    let withLead lead (page: DocumentationPageConfig) = { page with lead = lead }
    let withSections sections (page: DocumentationPageConfig) = { page with sections = sections }
    let withHiddenHeading (page: DocumentationPageConfig) = { page with heading = VisuallyHidden }

    let withHeadingAdornment adornment (page: DocumentationPageConfig) =
        { page with
            headingAdornment = Some adornment }

    let withPager pager (page: DocumentationPageConfig) = { page with pager = Some pager }
    let withFixtures fixtures (page: DocumentationPageConfig) = { page with fixtures = fixtures }
    let withMetadata metadata (page: DocumentationPageConfig) = { page with metadata = metadata }
    let render (page: DocumentationPageConfig) = DocsView.content page

/// <summary>
/// A configured document whose page is required and whose shell/navigation are explicit modifiers.
/// </summary>
/// <category>layouts</category>
[<NoEquality; NoComparison>]
type DocumentConfig<'destination> =
    private
        { page: DocumentationPageConfig
          site: DocsSite<'destination>
          breadcrumbs: Breadcrumb list option
          sideNavItems: NavNode<'destination> list option
          renderMode: FixtureRenderMode }

/// <summary>
/// Public immutable builders for a complete documentation document.
/// </summary>
/// <category>layouts</category>
[<RequireQualifiedAccess>]
module Document =
    let create site page : DocumentConfig<'destination> =
        { page = page
          site = site
          breadcrumbs = None
          sideNavItems = None
          renderMode = Embedded }

    let withBreadcrumbs breadcrumbs (document: DocumentConfig<'destination>) =
        if List.isEmpty breadcrumbs then
            invalidArg (nameof breadcrumbs) "At least one breadcrumb is required."

        { document with
            breadcrumbs = Some breadcrumbs }

    let withSideNavItems items (document: DocumentConfig<'destination>) =
        if List.isEmpty items then
            invalidArg (nameof items) "At least one side-navigation item is required."

        { document with
            sideNavItems = Some items }

    let withRenderMode renderMode (document: DocumentConfig<'destination>) =
        { document with
            renderMode = renderMode }

    let withAppMode appMode (document: DocumentConfig<'destination>) =
        document |> withRenderMode (Fullscreen appMode)

    let render (document: DocumentConfig<'destination>) =
        let site = document.site

        let breadcrumbs =
            document.breadcrumbs
            |> Option.defaultWith (fun () -> Navigation.breadcrumbs site.navigation site.homeId document.page.activeId)

        let sideNavItems = document.sideNavItems |> Option.defaultValue site.navigation
        DocsView.documentWithNavigation site breadcrumbs sideNavItems document.renderMode document.page
