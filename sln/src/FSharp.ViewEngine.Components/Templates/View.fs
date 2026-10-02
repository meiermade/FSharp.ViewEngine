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
    { initialScript:string
      clickAction:string
      submitAction:string
      restoreAction:string
      fetchLifecycle:string
      appModeExitAction:string }

/// <summary>
/// Runtime and head assets used by the documentation document shell.
/// </summary>
/// <category>layouts</category>
[<NoEquality; NoComparison>]
type DocsAssets =
    { productStylesheets:string list
      prismStylesheet:string option
      prismScripts:string list
      mermaidScript:string option
      datastarScript:string option
      mermaidSecurityLevel:string
      nonce:string option
      additionalHead:HtmlElement list
      navigation:DocsNavigationEnhancement option }

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
          datastarScript = Some "/scripts/datastar.1.0.2.js"
          mermaidSecurityLevel = "antiscript"
          nonce = None
          additionalHead = []
          navigation = None }

/// <summary>
/// Semantic accent colors and browser theme color for the documentation shell.
/// </summary>
/// <category>layouts</category>
type DocsTheme =
    { accent50:string
      accent100:string
      accent500:string
      accent700:string
      accent900:string
      themeColor:string }

/// <summary>
/// An optional repository action displayed in the documentation header.
/// </summary>
/// <category>layouts</category>
[<NoEquality; NoComparison>]
type DocsRepository =
    | GitHubRepository of url:string
    | RepositoryLink of label:string * url:string

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

/// <category>layouts</category>
[<RequireQualifiedAccess>]
module DocsSite =
    let create name homeId : DocsSite<'destination> =
        { name = name
          baseUrl = None
          description = None
          repository = None
          brandMark = span { _ariaHidden true; name }
          homeId = homeId
          navigation = []
          storageKey = "documentation"
          defaultColorMode = DocsColorMode.System
          theme = DocsTheme.amber
          assets = DocsAssets.defaults
          search = [] }

    let withNavigation navigation (site:DocsSite<'destination>) = { site with navigation = navigation }
    let withBrandMark brandMark (site:DocsSite<'destination>) = { site with brandMark = brandMark }
    let withDescription description (site:DocsSite<'destination>) = { site with description = Some description }
    let withBaseUrl baseUrl (site:DocsSite<'destination>) = { site with baseUrl = Some baseUrl }
    let withStorageKey storageKey (site:DocsSite<'destination>) = { site with storageKey = storageKey }
    let withTheme theme (site:DocsSite<'destination>) = { site with theme = theme }
    let withAssets assets (site:DocsSite<'destination>) = { site with assets = assets }
    let withSearch search (site:DocsSite<'destination>) = { site with search = search }

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

    let themeStyle theme =
        $"--fve-brand-subtle:{theme.accent100};--fve-brand-solid:{theme.accent700};--fve-brand-hover:{theme.accent900};--fve-brand-active:{theme.accent900};--fve-brand-text:{theme.accent900};--fve-brand-ring:{theme.accent500}"

module private ViewStyles =
    let iconButton = "grid size-8 shrink-0 cursor-pointer place-items-center rounded-lg border border-transparent bg-transparent p-1.5 text-[var(--fve-muted-text)] no-underline hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] [&>svg]:size-[1.125rem]"
    let navItem = "flex min-h-7 w-full min-w-0 cursor-pointer items-center gap-1.5 rounded-md border-0 bg-transparent px-2.5 py-1 text-left text-sm leading-5 font-medium text-[var(--fve-muted-text)] no-underline hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)]"
    let navList = "m-0 min-w-0 list-none p-0 [&>li+li]:mt-px"
    let sectionContent = "flex flex-col gap-4 text-[var(--fve-text)]"
    let codeSurface = "bg-[var(--fve-docs-code-surface,var(--fve-background))] text-[var(--fve-text)]"
    let tocLinks = "flex flex-col gap-2 [&>a]:text-sm [&>a]:leading-5 [&>a]:text-[var(--fve-muted-text)] [&>a]:no-underline [&>a:hover]:text-[var(--fve-brand-text)] [&>a[aria-current=location]]:font-semibold [&>a[aria-current=location]]:text-[var(--fve-brand-text)]"

module private DocsSectionView =
    let render showHeading (docSection:DocumentationSectionConfig) =
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
                        if docSection.level <= 2 then "m-0 text-xl font-semibold tracking-tight text-[var(--fve-text)]"
                        elif docSection.level = 3 then "m-0 text-base font-semibold tracking-tight text-[var(--fve-text)]"
                        else "m-0 text-sm font-semibold tracking-tight text-[var(--fve-text)]"
                    if docSection.level <= 2 then h2 { _class classes; docSection.title }
                    elif docSection.level = 3 then h3 { _class classes; docSection.title }
                    else h4 { _class classes; docSection.title }
                }
            div { _class ViewStyles.sectionContent; _data("docs-section-content", "true"); for element in docSection.content do element }
        }

module private ColorModeView =
    let render defaultMode =
        let icon =
            raw """<svg class="size-4 shrink-0 dark:hidden" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z"/></svg><svg class="hidden size-4 shrink-0 dark:block" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M21.752 15.002A9.718 9.718 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 2.25 12c0 5.385 4.365 9.75 9.75 9.75a9.753 9.753 0 0 0 9.752-6.748Z"/></svg>"""
        let items : FSharp.ViewEngine.Components.DropdownMenuItem<unit> list =
            [ for mode, label in [ System, "System"; Light, "Light"; Dark, "Dark" ] do
                let value = DocsColorMode.value mode
                FSharp.ViewEngine.Components.DropdownMenuItem.radio $"$colorMode = '{value}'" label
                |> FSharp.ViewEngine.Components.DropdownMenuItem.withChecked (mode = defaultMode)
                |> FSharp.ViewEngine.Components.DropdownMenuItem.withCheckedExpression $"$colorMode == '{value}'" ]
        div {
            _class "relative shrink-0"
            _data("on:fsharpdocs:colormode__window", "$colorMode = window.fsharpDocsColorMode.current()")
            FSharp.ViewEngine.Components.DropdownMenu.create "docs-color-mode" "Choose color theme"
            |> FSharp.ViewEngine.Components.DropdownMenu.withTrigger (FSharp.ViewEngine.Components.DropdownMenuTrigger.icon icon)
            |> FSharp.ViewEngine.Components.DropdownMenu.withContent items
            |> FSharp.ViewEngine.Components.DropdownMenu.render (fun () -> "")
        }

module private RepositoryView =
    let render = function
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
                _class "shrink-0 rounded-lg border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 py-1.5 text-sm font-semibold text-[var(--fve-muted-text)] no-underline shadow-sm hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
                label
            }

module private NavigationView =
    open ViewHelpers

    let rec node activeId (navNode:NavNode<'destination>) =
        let isActive = NavNode.id navNode = activeId
        li {
            match navNode with
            | NavNode.Group group ->
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
                    _class $"{ViewStyles.navItem} data-[active=true]:font-semibold data-[active=true]:text-[var(--fve-text)]"
                    span {
                        _class "grid size-4 shrink-0 place-items-center text-[var(--fve-muted-text)] transition-transform data-[open=true]:rotate-90 [&>svg]:size-4"
                        _data("attr:data-open", $"${signal} ? 'true' : 'false'")
                        Icons.chevron
                    }
                    span { _class "min-w-0 flex-1 whitespace-nowrap"; group.label }
                }
                ul {
                    _id $"nav-children-{group.id}"
                    _class (ViewStyles.navList + " mt-0.5 ml-3 w-[calc(100%-0.75rem)] border-l border-[var(--fve-border)]")
                    _data("show", $"${signal}")
                    // After initial rendering, the expansion signal owns visibility across morphs.
                    _dataPreserveAttr "style"
                    if not (group.defaultOpen || containsActive) then _style "display:none"
                    for child in group.children do node activeId child
                }
            | NavNode.Page page ->
                a {
                    _id $"nav-{page.id}"
                    _href page.href
                    _data("docs-nav-link", "true")
                    _data("selected", isActive.ToString().ToLowerInvariant())
                    if isActive then _ariaCurrent "page"
                    _class $"{ViewStyles.navItem} data-[selected=true]:bg-[var(--fve-brand-subtle)] data-[selected=true]:font-semibold data-[selected=true]:text-[var(--fve-brand-text)]"
                    span { _class "grid size-4 shrink-0 place-items-center"; _ariaHidden "true" }
                    span { _class "min-w-0 flex-1 whitespace-nowrap"; page.label }
                }
        }

    let sideNav (site:DocsSite<'destination>) (items:NavNode<'destination> list) activeId =
        aside {
            _id "side-nav"
            _class "fixed inset-y-0 left-0 z-50 hidden h-dvh w-[min(18rem,calc(100vw-3rem))] border-r border-[var(--fve-border)] bg-[var(--fve-background)] shadow-xl lg:sticky lg:block lg:w-[min(18rem,24vw)] lg:shadow-none"
            _ariaLabel "Documentation navigation"
            _data("class:hidden", "!$sideNavOpen")
            _data("docs-side-nav", "true")
            div {
                _class "flex h-full min-h-0 flex-col"
                div {
                    _class "flex h-12 shrink-0 items-center gap-2.5 border-b border-[var(--fve-border)] px-4"
                    div { _class "flex size-7 items-center justify-center [&>img]:max-h-full [&>img]:max-w-full [&>svg]:max-h-full [&>svg]:max-w-full"; site.brandMark }
                    div { _class "text-sm font-semibold"; site.name }
                    div { _class "flex-1" }
                    button {
                        _type "button"
                        _ariaLabel "Close navigation"
                        _class $"{ViewStyles.iconButton} lg:hidden"
                        _data("docs-nav-close", "true")
                        _data("on:click", "$sideNavOpen = false; window.fsharpDocsMobileNav.close()")
                        Icons.close
                    }
                }
                nav {
                    _ariaLabel "Documentation"
                    _class "min-h-0 flex-1 overflow-auto p-3"
                    ul { _class (ViewStyles.navList + " w-full"); for section in items do node activeId section }
                }
            }
        }

    let topNav (site:DocsSite<'destination>) (breadcrumbs:Breadcrumb list) =
        let hiddenCount = if breadcrumbs.Length > 2 then breadcrumbs.Length - 1 else 0
        let hiddenBreadcrumbs = breadcrumbs |> List.take hiddenCount

        header {
            _class "relative z-30 flex h-12 shrink-0 items-center justify-between gap-3 border-b border-[var(--fve-border)] bg-[color-mix(in_srgb,var(--fve-background)_96%,transparent)] px-4 backdrop-blur-lg sm:pr-2.5 lg:px-8"
            _data("docs-top-nav", "true")
            div {
                _class "flex min-w-0 flex-1 items-center gap-3"
                button {
                    _type "button"
                    _ariaLabel "Open navigation"
                    _ariaControls "side-nav"
                    _data("attr:aria-expanded", "$sideNavOpen ? 'true' : 'false'")
                    _class $"{ViewStyles.iconButton} lg:hidden"
                    _data("on:click", "$sideNavOpen = true; window.fsharpDocsMobileNav.open(evt.currentTarget)")
                    Icons.menu
                }
                nav {
                    _ariaLabel "Breadcrumb"
                    _class "min-w-0"
                    ol {
                        _role "list"
                        _class "m-0 flex min-w-0 list-none items-center gap-1.5 p-0 text-sm"
                        if not hiddenBreadcrumbs.IsEmpty then
                            li {
                                _class "relative sm:hidden"
                                button {
                                    _type "button"
                                    _ariaLabel "Show hidden breadcrumbs"
                                    _ariaControls "docs-breadcrumb-menu"
                                    _data("attr:aria-expanded", "$breadcrumbMenuOpen ? 'true' : 'false'")
                                    _data("on:click", "$breadcrumbMenuOpen = !$breadcrumbMenuOpen")
                                    _class ViewStyles.iconButton
                                    Icons.ellipsis
                                }
                                div {
                                    _id "docs-breadcrumb-menu"
                                    _class "absolute top-full left-0 z-60 mt-2 w-56 overflow-hidden rounded-lg border border-[var(--fve-border)] bg-[var(--fve-surface)] py-1 shadow-xl [&>a]:block [&>a]:overflow-hidden [&>a]:px-3 [&>a]:py-2 [&>a]:text-ellipsis [&>a]:whitespace-nowrap [&>a]:text-[var(--fve-muted-text)] [&>a]:no-underline [&>a:hover]:bg-[var(--fve-surface-hover)] [&>a:hover]:text-[var(--fve-text)] [&>span]:block [&>span]:overflow-hidden [&>span]:px-3 [&>span]:py-2 [&>span]:text-ellipsis [&>span]:whitespace-nowrap [&>span]:text-[var(--fve-muted-text)]"
                                    _data("show", "$breadcrumbMenuOpen")
                                    _style "display:none"
                                    for crumb in hiddenBreadcrumbs do
                                        match crumb.href with
                                        | Some href -> a { _href href; crumb.label }
                                        | None -> span { crumb.label }
                                }
                            }
                        for index, crumb in List.indexed breadcrumbs do
                            let isCurrent = index = breadcrumbs.Length - 1
                            let hiddenOnMobile = index < hiddenCount
                            if index > 0 then
                                li {
                                    _class (if hiddenOnMobile then "shrink-0 text-[var(--fve-muted-text)] max-sm:hidden [&>svg]:size-4" else "shrink-0 text-[var(--fve-muted-text)] [&>svg]:size-4")
                                    Icons.breadcrumbChevron
                                }
                            li {
                                _class (if hiddenOnMobile then "min-w-0 max-sm:hidden [&>a]:block [&>a]:overflow-hidden [&>a]:text-ellipsis [&>a]:whitespace-nowrap [&>a]:text-[var(--fve-muted-text)] [&>a]:no-underline [&>a:hover]:text-[var(--fve-text)] [&>span]:block [&>span]:overflow-hidden [&>span]:text-ellipsis [&>span]:whitespace-nowrap [&>span]:text-[var(--fve-muted-text)] [&>[aria-current=page]]:font-semibold [&>[aria-current=page]]:text-[var(--fve-text)]" else "min-w-0 [&>a]:block [&>a]:overflow-hidden [&>a]:text-ellipsis [&>a]:whitespace-nowrap [&>a]:text-[var(--fve-muted-text)] [&>a]:no-underline [&>a:hover]:text-[var(--fve-text)] [&>span]:block [&>span]:overflow-hidden [&>span]:text-ellipsis [&>span]:whitespace-nowrap [&>span]:text-[var(--fve-muted-text)] [&>[aria-current=page]]:font-semibold [&>[aria-current=page]]:text-[var(--fve-text)]")
                                match crumb.href with
                                | Some href when not isCurrent -> a { _href href; crumb.label }
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
                _class (FSharp.ViewEngine.Components.ComponentsTheme.sky |> FSharp.ViewEngine.Components.ComponentsTheme.withDensity FSharp.ViewEngine.Components.Density.Compact |> FSharp.ViewEngine.Components.ComponentsTheme.withControlSize FSharp.ViewEngine.Components.ControlSize.Small |> FSharp.ViewEngine.Components.ComponentsTheme.className |> fun theme -> theme + " flex shrink-0 items-center gap-1.5 max-sm:gap-0.5")
                _data("docs-top-actions", "true")
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
            _data("docs-toc", "true")
            for item in items do
                a {
                    _href item.href
                    _data("on:click", "window.fsharpDocsFragments.navigate(evt, evt.currentTarget.getAttribute('href'))")
                    _class (if item.level <= 2 then "" elif item.level = 3 then "pl-3" else "pl-6")
                    item.label
                }
        }

    let desktop (items:TocItem list) =
        aside {
            _class "hidden w-[min(16rem,20vw)] shrink-0 overflow-y-auto border-l border-[var(--fve-border)] px-6 py-8 xl:block"
            _data("docs-toc-rail", "true")
            div {
                _class "sticky top-8"
                div { _class "mb-3 text-xs font-semibold tracking-[0.14em] text-[var(--fve-muted-text)] uppercase"; "On this page" }
                links ViewStyles.tocLinks items
            }
        }

    let mobile (items:TocItem list) =
        details {
            _class "mb-6 block rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] xl:hidden [&>summary]:cursor-pointer [&>summary]:px-4 [&>summary]:py-3 [&>summary]:text-sm [&>summary]:font-semibold [&>summary]:text-[var(--fve-text)] [&>summary:focus-visible]:outline-2 [&>summary:focus-visible]:outline-offset-2 [&>summary:focus-visible]:outline-[var(--fve-brand-ring)]"
            _data("docs-mobile-toc", "true")
            _ariaLabel "On this page"
            summary { "On this page" }
            links $"{ViewStyles.tocLinks} border-t border-[var(--fve-border)] p-2 gap-0.5 [&>a]:rounded-md [&>a]:px-2 [&>a]:py-2 [&>a:hover]:bg-[var(--fve-surface-hover)] [&>a:focus-visible]:bg-[var(--fve-surface-hover)] [&>a:focus-visible]:outline-none" items
        }

module private PagerView =
    open ViewHelpers

    let private renderLink (direction:string) (relation:string) (isNext:bool) (link:DocsPageLink) =
        let linkAlignment = if isNext then "sm:col-start-2 text-right" else ""
        let directionAlignment = if isNext then "justify-end" else ""
        a {
            _rel relation
            _href link.href
            _class $"flex min-w-0 flex-col gap-1 rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface)] p-4 text-[var(--fve-text)] no-underline shadow-sm hover:border-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] {linkAlignment}"
            span {
                _class $"flex items-center gap-1 text-xs font-semibold text-[var(--fve-muted-text)] {directionAlignment}"
                if relation = "prev" then
                    span { _class "grid rotate-180 place-items-center text-[var(--fve-muted-text)] [&>svg]:size-3.5"; Icons.chevron }
                span { direction }
                if relation = "next" then
                    span { _class "grid place-items-center text-[var(--fve-muted-text)] [&>svg]:size-3.5"; Icons.chevron }
            }
            span { _class "[overflow-wrap:anywhere] text-base font-semibold text-[var(--fve-brand-text)]"; link.label }
        }

    let render (pager:DocsPager) =
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

    let private tocItems (page:DocumentationPageConfig) : TocItem list =
        page.sections
        |> List.map (fun section -> { level = section.level; label = section.title; href = $"#{section.id}" })

    let content (page:DocumentationPageConfig) =
        div {
            _class (match page.layout with Gallery -> "flex flex-col gap-8" | Article | Reference | Canvas -> "flex flex-col gap-12")
            match page.heading with
            | Visible ->
                section {
                    match page.headingAdornment with
                    | Some adornment -> adornment
                    | None -> ()
                    h1 { _class "m-0 max-w-3xl [overflow-wrap:anywhere] text-4xl leading-[1.1] font-semibold tracking-tight text-[var(--fve-text)]"; page.title }
                    if page.metadata.version.IsSome || page.metadata.deprecated then
                        div {
                            _class "mt-3.5 flex flex-wrap gap-2"
                            match page.metadata.version with
                            | Some version -> span { _class "inline-flex rounded-full bg-[var(--fve-brand-subtle)] px-2 py-1 text-xs font-bold text-[var(--fve-brand-text)]"; _data("docs-version", version); version }
                            | None -> ()
                            if page.metadata.deprecated then span { _class "inline-flex rounded-full bg-amber-100 px-2 py-1 text-xs font-bold text-amber-800 dark:bg-amber-950 dark:text-amber-200"; _data("docs-deprecated", "true"); "Deprecated" }
                        }
                    if not (String.IsNullOrWhiteSpace page.description) then
                        p { _class "mt-4 max-w-3xl leading-7 text-[var(--fve-muted-text)]"; page.description }
                    if page.metadata.lastUpdated.IsSome || page.metadata.editUrl.IsSome then
                        div {
                            _class "mt-3 flex flex-wrap gap-4 text-sm text-[var(--fve-muted-text)] [&_a]:font-semibold [&_a]:text-[var(--fve-brand-text)] [&_a]:underline [&_a]:underline-offset-2"
                            match page.metadata.lastUpdated with
                            | Some lastUpdated -> span { "Last updated "; time { _datetime lastUpdated; lastUpdated } }
                            | None -> ()
                            match page.metadata.editUrl with
                            | Some editUrl -> a { _href editUrl; "Edit this page" }
                            | None -> ()
                        }
                }
            | VisuallyHidden -> h1 { _class "sr-only"; _data("docs-visually-hidden-heading", "true"); page.title }
            for element in page.lead do element
            let items = tocItems page
            match page.rightRail with
            | TableOfContents when not items.IsEmpty -> TocView.mobile items
            | _ -> ()
            for section in page.sections do DocsSectionView.render (page.layout <> Gallery) section
            match page.pager with
            | Some pager -> PagerView.render pager
            | None -> ()
        }

    let private defaultBreadcrumbs (site:DocsSite<'destination>) (page:DocumentationPageConfig) =
        Navigation.breadcrumbs site.navigation site.homeId page.activeId

    let sideNavWith (site:DocsSite<'destination>) (items:NavNode<'destination> list) (page:DocumentationPageConfig) =
        NavigationView.sideNav site items page.activeId

    let sideNav (site:DocsSite<'destination>) (page:DocumentationPageConfig) = sideNavWith site site.navigation page

    let pageContentWith (site:DocsSite<'destination>) (breadcrumbs:Breadcrumb list) (page:DocumentationPageConfig) =
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
                _data("docs-page-viewport", "true")
                div {
                    _class $"flex h-full min-h-0 {layoutClasses}"
                    _data("docs-page-layout", "true")
                    _data("docs-layout", layoutName)
                    main {
                        _id "main-content"
                        _class $"min-w-0 flex-1 overflow-y-auto bg-[var(--fve-background)] px-4 py-10 outline-none sm:px-6 lg:px-10 {mainClasses}"
                        _data("docs-main", "true")
                        _tabindex -1
                        div { _class $"mx-auto {mainInnerClasses}"; _data("docs-main-inner", "true"); content page }
                    }
                    match page.rightRail with
                    | TableOfContents when not items.IsEmpty -> TocView.desktop items
                    | TableOfContents -> ()
                    | NoRail -> ()
                    | CustomRail rail ->
                        aside {
                            _class $"w-128 shrink-0 overflow-y-auto border-l border-[var(--fve-border)] {ViewStyles.codeSurface} max-xl:w-auto max-xl:overflow-visible max-xl:border-t max-xl:border-l-0"
                            _data("docs-custom-rail", "true")
                            div { _class "flex min-h-full flex-col gap-4 px-6 py-8 max-xl:mx-auto max-xl:min-h-0 max-xl:max-w-4xl max-sm:px-4 max-sm:py-5 [&>div]:flex [&>div]:min-w-0 [&>div]:flex-col [&>div]:gap-4"; rail }
                        }
                }
            }
        }

    let pageContent (site:DocsSite<'destination>) (page:DocumentationPageConfig) =
        pageContentWith site (defaultBreadcrumbs site page) page

    let pageWithNavigation (site:DocsSite<'destination>) (breadcrumbs:Breadcrumb list) (sideNavItems:NavNode<'destination> list) (docPage:DocumentationPageConfig) =
        div {
            _id "page"
            _class "flex h-dvh overflow-hidden bg-[var(--fve-background)]"
            _data("docs-shell", "true")
            a { _href "#main-content"; _class "fixed top-2 left-2 z-100 -translate-y-[200%] rounded-md bg-[var(--fve-surface)] px-3 py-2 font-semibold text-[var(--fve-text)] no-underline shadow-lg focus:translate-y-0"; "Skip to main content" }
            button {
                _type "button"
                _ariaLabel "Close navigation overlay"
                _class "fixed inset-0 z-40 hidden border-0 bg-black/50 backdrop-blur-[2px] lg:hidden"
                _data("docs-overlay", "true")
                _data("class:hidden", "!$sideNavOpen")
                _data("on:click", "$sideNavOpen = false; window.fsharpDocsMobileNav.close()")
            }
            sideNavWith site sideNavItems docPage
            pageContentWith site breadcrumbs docPage
        }

    let page (site:DocsSite<'destination>) (docPage:DocumentationPageConfig) =
        pageWithNavigation site (defaultBreadcrumbs site docPage) site.navigation docPage

    let private previousIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M11.78 5.22a.75.75 0 0 1 0 1.06L8.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06l-4.25-4.25a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>"""

    let private nextIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 1 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>"""

    let private dockIcons =
        fragment {
            span {
                _dataShow "!$appModeDockTop"
                raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M10.53 4.47a.75.75 0 0 0-1.06 0l-4.25 4.25a.75.75 0 0 0 1.06 1.06L9.25 6.81V15a.75.75 0 0 0 1.5 0V6.81l2.97 2.97a.75.75 0 1 0 1.06-1.06l-4.25-4.25Z" clip-rule="evenodd"/></svg>"""
            }
            span {
                _dataShow "$appModeDockTop"
                _style "display:none"
                raw """<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M9.47 15.53a.75.75 0 0 0 1.06 0l4.25-4.25a.75.75 0 1 0-1.06-1.06l-2.97 2.97V5a.75.75 0 0 0-1.5 0v8.19l-2.97-2.97a.75.75 0 1 0-1.06 1.06l4.25 4.25Z" clip-rule="evenodd"/></svg>"""
            }
        }

    let private appModeDirection (frameId:string) (label:string) (icon:HtmlElement) (link:FixtureLink option) =
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

    let private appModeView (site:DocsSite<'destination>) (request:AppMode) (fixture:FixtureConfig) =
        let frameId = Fixture.id fixture
        let states = Fixture.states fixture
        let currentState = states |> List.tryFind FixtureState.isCurrent
        let rootClasses = "fixed inset-0 z-100 overflow-auto bg-[var(--fve-background)] text-[var(--fve-text)]"
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
                _class (FSharp.ViewEngine.Components.ComponentsTheme.sky |> FSharp.ViewEngine.Components.ComponentsTheme.withDensity FSharp.ViewEngine.Components.Density.Compact |> FSharp.ViewEngine.Components.ComponentsTheme.withControlSize FSharp.ViewEngine.Components.ControlSize.Small |> FSharp.ViewEngine.Components.ComponentsTheme.className |> fun theme -> theme + " fixed right-[max(0.75rem,env(safe-area-inset-right))] bottom-[max(0.5rem,env(safe-area-inset-bottom))] z-[110] flex min-h-10 max-w-[calc(100vw-16px)] flex-wrap items-center justify-end gap-0.5 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[color-mix(in_srgb,var(--fve-surface)_92%,transparent)] p-1 text-xs leading-none font-semibold text-[var(--fve-text)] shadow-[0_12px_32px_rgb(0_0_0/20%)] backdrop-blur-[14px] data-[fve-app-dock=top]:top-[max(0.5rem,env(safe-area-inset-top))] data-[fve-app-dock=top]:bottom-auto max-[32rem]:right-2 max-[32rem]:bottom-2 max-[32rem]:data-[fve-app-dock=top]:top-2 [&>a]:grid [&>a]:size-[32px] [&>a]:shrink-0 [&>a]:cursor-pointer [&>a]:place-items-center [&>a]:rounded-[var(--fve-radius-control)] [&>a]:border-0 [&>a]:bg-transparent [&>a]:text-inherit [&>a]:no-underline [&>a:hover]:bg-[var(--fve-surface-hover)] [&>a:focus-visible]:outline-2 [&>a:focus-visible]:outline-offset-2 [&>a:focus-visible]:outline-[var(--fve-brand-ring)] [&>button]:grid [&>button]:size-[32px] [&>button]:shrink-0 [&>button]:cursor-pointer [&>button]:place-items-center [&>button]:rounded-[var(--fve-radius-control)] [&>button]:border-0 [&>button]:bg-transparent [&>button]:text-inherit [&>button:hover]:bg-[var(--fve-surface-hover)] [&>button:focus-visible]:outline-2 [&>button:focus-visible]:outline-offset-2 [&>button:focus-visible]:outline-[var(--fve-brand-ring)] [&>span[aria-disabled=true]]:grid [&>span[aria-disabled=true]]:size-[32px] [&>span[aria-disabled=true]]:place-items-center [&>span[aria-disabled=true]]:text-[var(--fve-muted-text)] [&_svg]:size-[16px]")
                _attr ("data-fve-app-mode-controls", "true")
                // One viewer exists at a time; disconnect its previous observer before a navigation morph.
                _dataInit "window.fsharpDocsDockObserver?.disconnect(); const measure = () => { if (!el.isConnected) { observer.disconnect(); return }; const root = document.getElementById('fve-app-mode-root'); if (!root) return; const rect = el.getBoundingClientRect(); const mobile = matchMedia('(width < 48rem)').matches; const top = mobile && el.dataset.fveAppDock === 'top'; root.style.top = top ? (rect.bottom + 8) + 'px' : '0px'; root.style.bottom = !mobile || top ? '0px' : (innerHeight - rect.top + 8) + 'px' }; const observer = new ResizeObserver(measure); window.fsharpDocsDockObserver = observer; el.fveMeasureDock = measure; observer.observe(el)"
                _dataEffect "$appModeDockTop; requestAnimationFrame(() => el.fveMeasureDock?.())"
                _dataOn ("resize__window", "el.fveMeasureDock?.()")
                _dataAttr ("data-fve-app-dock", "$appModeDockTop ? 'top' : 'bottom'")
                _dataAttr ("data-fve-color-mode", "($colorMode == 'dark' || ($colorMode == 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches)) ? 'light' : 'dark'")
                _data("on:fsharpdocs:colormode__window", "document.getElementById('fve-app-mode-controls')?.setAttribute('data-fve-color-mode', document.documentElement.classList.contains('dark') ? 'light' : 'dark')")
                _ariaLabel "App mode controls"
                let hasWorkflow = Fixture.previous fixture |> Option.isSome || Fixture.next fixture |> Option.isSome
                if hasWorkflow then
                    appModeDirection frameId "Previous" previousIcon (Fixture.previous fixture)
                if not states.IsEmpty then
                    div {
                        _class "block min-w-0 max-w-full max-[32rem]:order-first max-[32rem]:basis-full [&_.fve-popup-control]:min-h-8 [&_.fve-popup-control]:w-auto [&_.fve-popup-control]:max-w-full [&_.fve-popup-control]:border-0 [&_.fve-popup-control]:bg-transparent [&_.fve-popup-control]:px-2 [&_.fve-popup-control]:shadow-none [&_.fve-popup-control>span]:overflow-hidden [&_.fve-popup-control>span]:text-ellipsis [&_.fve-popup-control>span]:whitespace-nowrap [&_[role=menu]]:z-[120] [&_[role=menu]]:min-w-40 [&_[role=menu]]:border [&_[role=menu]]:border-[var(--fve-border)] [&_[role=menu]]:bg-[var(--fve-surface)] [&_[role=menu]]:shadow-xl"
                        _attr ("data-fve-app-mode-state-select", "true")
                        FSharp.ViewEngine.Components.DropdownMenu.create $"fve-app-mode-{frameId}-state" "Review state"
                        |> FSharp.ViewEngine.Components.DropdownMenu.withTrigger (FSharp.ViewEngine.Components.DropdownMenuTrigger.content (span { currentState |> Option.map FixtureState.label |> Option.defaultValue "Review state" }))
                        |> FSharp.ViewEngine.Components.DropdownMenu.withContent (
                            states
                            |> List.map (fun state -> FSharp.ViewEngine.Components.DropdownMenuItem.link (Fixture.appModeHref frameId (FixtureState.href state)) (FixtureState.label state)))
                        |> FSharp.ViewEngine.Components.DropdownMenu.withAlignment FSharp.ViewEngine.Components.DropdownMenuAlignment.End
                        |> FSharp.ViewEngine.Components.DropdownMenu.render id
                    }
                if hasWorkflow then
                    appModeDirection frameId "Next" nextIcon (Fixture.next fixture)
                    span { _class "mx-0.5 h-5 w-px bg-[var(--fve-border)]"; _attr ("data-fve-app-mode-divider", "true"); _ariaHidden true }
                ColorModeView.render site.defaultColorMode
                button {
                    _type "button"
                    _dataAttr ("aria-label", "$appModeDockTop ? 'Move App mode controls to bottom' : 'Move App mode controls to top'")
                    _dataAttr ("title", "$appModeDockTop ? 'Move App mode controls to bottom' : 'Move App mode controls to top'")
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

    let documentMetadata (site:DocsSite<'destination>) (docPage:DocumentationPageConfig) =
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
        let browserTitle = docPage.metadata.browserTitle |> Option.defaultValue docPage.title
        let description = if String.IsNullOrWhiteSpace docPage.description then site.description else Some docPage.description
        fragment {
            title { _id "docs-document-title"; browserTitle }
            match canonicalUrl with
            | Some url ->
                link { _id "docs-canonical-url"; _rel "canonical"; _href url }
                meta { _id "docs-og-url"; _property "og:url"; _content url }
            | None -> ()
            match description with
            | Some value ->
                meta { _id "docs-description"; _name "description"; _content value }
                meta { _id "docs-og-description"; _property "og:description"; _content value }
                meta { _id "docs-twitter-description"; _name "twitter:description"; _content value }
            | None -> ()
            meta { _id "docs-og-type"; _property "og:type"; _content "website" }
            meta { _id "docs-og-site-name"; _property "og:site_name"; _content site.name }
            meta { _id "docs-og-title"; _property "og:title"; _content browserTitle }
            meta { _id "docs-twitter-title"; _name "twitter:title"; _content browserTitle }
            match docPage.metadata.socialImage with
            | Some image ->
                meta { _id "docs-og-image"; _property "og:image"; _content image }
                meta { _id "docs-og-image-alt"; _property "og:image:alt"; _content docPage.title }
                meta { _id "docs-twitter-image"; _name "twitter:image"; _content image }
                meta { _id "docs-twitter-image-alt"; _name "twitter:image:alt"; _content docPage.title }
                meta { _id "docs-twitter-card"; _name "twitter:card"; _content "summary_large_image" }
            | None -> ()
            meta { _id "docs-robots"; _name "robots"; _content (if docPage.metadata.noIndex then "noindex" else "index,follow") }
        }

    let navigationRootWithNavigation (site:DocsSite<'destination>) (breadcrumbs:Breadcrumb list) (sideNavItems:NavNode<'destination> list) (renderMode:FixtureRenderMode) (docPage:DocumentationPageConfig) =
        let activeGroupSignals =
            sideNavItems
            |> NavNode.collectGroups
            |> List.filter (NavNode.containsActive docPage.activeId)
            |> List.map (fun node -> $"${signalName (NavNode.id node)} = true")
        let initialize =
            [ "$sideNavOpen = false"
              "$breadcrumbMenuOpen = false"
              yield! activeGroupSignals
              "window.fsharpDocsColorMode?.apply(window.fsharpDocsColorMode.current())"
              "window.initializeDocsToc?.()"
              "window.fsharpDocsFragments?.show(window.location.hash)" ]
            |> String.concat "; "
        div {
            _id "docs-navigation-root"
            _data("init", initialize)
            match renderMode with
            | Fullscreen request ->
                match docPage.fixtures |> List.tryFind (fun fixture -> Fixture.id fixture = AppMode.frameId request) with
                | Some fixture -> appModeView site request fixture
                | None -> pageWithNavigation site breadcrumbs sideNavItems docPage
            | Embedded -> pageWithNavigation site breadcrumbs sideNavItems docPage
        }

    let private documentWithRoot (site:DocsSite<'destination>) (breadcrumbs:Breadcrumb list) (sideNavItems:NavNode<'destination> list) (renderMode:FixtureRenderMode) (docPage:DocumentationPageConfig) (customRoot:HtmlElement option) =
        let navGroups = NavNode.collectGroups sideNavItems
        let navSignals =
            navGroups
            |> List.map (fun node ->
                let signal = signalName (NavNode.id node)
                let shouldOpen = NavNode.defaultOpen node || NavNode.containsActive docPage.activeId node
                let containsActive = NavNode.containsActive docPage.activeId node
                $"{signal}: window.fsharpDocsNav.initial({jsString (NavNode.id node)}, {shouldOpen.ToString().ToLowerInvariant()}, {containsActive.ToString().ToLowerInvariant()})")

        let signals = "{ sideNavOpen: false, breadcrumbMenuOpen: false, appModeDockTop: false, navigationPending: false, navigationTarget: '', colorMode: window.fsharpDocsColorMode.current()" + (if navSignals.IsEmpty then "" else ", " + String.concat ", " navSignals) + " }"
        let activeAppMode =
            match renderMode with
            | Embedded -> None
            | Fullscreen request ->
                docPage.fixtures
                |> List.tryFind (fun fixture -> Fixture.id fixture = AppMode.frameId request)
                |> Option.map (fun fixture -> request, fixture)
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
        // Same-origin document previews inherit the host mode before their first paint.
        if (window.parent !== window && window.parent.location.origin === window.location.origin && window.frameElement?.hasAttribute('data-docs-preview-src')) {
          return window.parent.document.documentElement.classList.contains('dark') ? 'dark' : 'light';
        }
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
  status.className = 'm-0 text-center text-sm leading-relaxed text-[var(--fve-muted-text)]';
  status.dataset.mermaidStatus = 'true';
  status.setAttribute('role', role);
  status.textContent = message;
  return status;
};
const setMermaidPending = node => {
  node.dataset.mermaidState = 'pending';
  delete node.dataset.mermaidRenderedSource;
  node.setAttribute('aria-busy', 'true');
  node.replaceChildren(mermaidStatus('status', 'Rendering diagram…'));
};
const setMermaidFailed = node => {
  node.dataset.mermaidState = 'failed';
  node.removeAttribute('aria-busy');
  node.replaceChildren(mermaidStatus('alert', 'Diagram unavailable.'));
};
window.renderMermaid = (el, pendingOnly = false) => {
  const render = async () => {
    const candidates = el?.matches?.('.mermaid') ? [el] : Array.from(el?.querySelectorAll?.('.mermaid') ?? []);
    const nodes = candidates.filter(node => !pendingOnly || node.dataset.mermaidState !== 'rendered' || node.dataset.mermaidRenderedSource !== (node.dataset.mermaidSource ?? '') || !node.querySelector('svg'));
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
      const source = node.dataset.mermaidSource ?? '';
      try {
        const id = `fsharp-docs-mermaid-${++mermaidRenderId}`;
        const { svg, bindFunctions } = await window.mermaid.render(id, source);
        if (!node.isConnected) continue;
        if ((node.dataset.mermaidSource ?? '') !== source) {
          setMermaidPending(node);
          continue;
        }
        node.innerHTML = svg;
        bindFunctions?.(node);
        node.dataset.mermaidState = 'rendered';
        node.dataset.mermaidRenderedSource = source;
        node.removeAttribute('aria-busy');
      } catch {
        if (!node.isConnected) continue;
        if ((node.dataset.mermaidSource ?? '') !== source) setMermaidPending(node);
        else setMermaidFailed(node);
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
  },
  async render(el) {
    const root = el ?? document;
    if (!root.querySelector?.('code[class*="language-"]') && !root.matches?.('code[class*="language-"]')) return;
    await this.ensure();
    window.Prism?.highlightAllUnder?.(root);
  }
};
window.addEventListener('pagehide', markDocsCodeUnloading);
window.addEventListener('pageshow', () => { window.fsharpDocsCode.unloading = false; });
window.renderCode = el => window.fsharpDocsCode.render(el);
window.fsharpDocsPreviewColorMode = window.fsharpDocsPreviewColorMode ?? {
  resolved() {
    return document.documentElement.classList.contains('dark') ? 'dark' : 'light';
  },
  apply(frame) {
    try {
      const preview = frame.contentWindow;
      if (!preview || preview.location.origin !== window.location.origin || !preview.fsharpDocsColorMode) return;
      const mode = this.resolved();
      const root = preview.document.documentElement;
      if (root.dataset.colorMode === mode && root.classList.contains('dark') === (mode === 'dark')) return;
      preview.fsharpDocsColorMode.set(mode);
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
window.addEventListener('fsharpdocs:colormode', () => window.fsharpDocsPreviewColorMode.sync(document));
window.renderDocsPreview = (el, pendingOnly = false) => {
  for (const frame of el?.querySelectorAll?.('iframe[data-docs-preview-src]') ?? []) {
    window.fsharpDocsPreviewColorMode.wire(frame);
    if (!frame.getAttribute('src')) frame.setAttribute('src', frame.dataset.docsPreviewSrc);
  }
  return window.renderMermaid?.(el, pendingOnly);
};
window.renderInitialDocsPreviews = (el) => Promise.all(
  Array.from(el?.querySelectorAll?.('[data-docs-preview-initial="true"]') ?? [])
    .map(preview => window.renderDocsPreview(preview, true))
);
window.fsharpDocsCopy = async button => {
  const source = button.closest('[data-docs-copyable-code]')?.querySelector('[data-docs-copy-source]')?.textContent ?? '';
  const label = button.querySelector('[data-docs-copy-label]');
  window.clearTimeout(button.docsCopyReset);
  delete button.dataset.copied;
  delete button.dataset.copyError;
  try {
    await navigator.clipboard.writeText(source);
    if (label) label.textContent = 'Copied';
    button.title = 'Copied';
    button.dataset.copied = 'true';
  } catch {
    if (label) label.textContent = 'Copy failed';
    button.title = 'Copy failed';
    button.dataset.copyError = 'true';
  }
  button.docsCopyReset = window.setTimeout(() => {
    if (label) label.textContent = '';
    button.title = button.getAttribute('aria-label');
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
                documentMetadata site docPage
                script { nonceAttribute (); raw colorModeScript }
                match site.assets.prismStylesheet with
                | Some stylesheet -> link { _rel "stylesheet"; _href stylesheet }
                | None -> ()
                for stylesheet in site.assets.productStylesheets do link { _rel "stylesheet"; _href stylesheet }
                match site.assets.mermaidScript with
                | Some _ -> script { nonceAttribute (); raw mermaidInitialization }
                | None -> ()
                script { nonceAttribute (); raw navigationScript }
                match site.assets.navigation with
                | Some enhancement -> script { nonceAttribute (); raw enhancement.initialScript }
                | None -> ()
                match site.assets.datastarScript with
                | Some source -> script { _type "module"; _src source; nonceAttribute () }
                | None -> ()
                for element in site.assets.additionalHead do element
            }
            body {
                _class (FSharp.ViewEngine.Components.ComponentsTheme.sky |> FSharp.ViewEngine.Components.ComponentsTheme.withDensity FSharp.ViewEngine.Components.Density.Compact |> FSharp.ViewEngine.Components.ComponentsTheme.className |> fun theme -> theme + " m-0 [--fve-docs-code-surface:color-mix(in_oklch,var(--fve-background)_72%,var(--fve-surface-subtle))] bg-[var(--fve-background)] font-sans text-[var(--fve-text)] antialiased")
                _data("signals", signals)
                _data("effect", "window.fsharpDocsColorMode.set($colorMode)")
                _data("on-signal-patch", $"window.fsharpDocsNav.save({navState})")
                match site.assets.navigation with
                | Some enhancement ->
                    _data("on:click", enhancement.clickAction)
                    _data("on:submit", enhancement.submitAction)
                    _data("on:popstate__window", enhancement.restoreAction)
                    _data("on:datastar-fetch", enhancement.fetchLifecycle)
                | None -> ()
                match activeAppMode, site.assets.navigation with
                | Some (request, _), Some enhancement ->
                    _attr ("data-fve-app-mode-frame", AppMode.frameId request)
                    _data("on:keydown__window", enhancement.appModeExitAction)
                | Some (request, _), None ->
                    _attr ("data-fve-app-mode-frame", AppMode.frameId request)
                | None, _ ->
                    _data("on:keydown__window", "evt.key == 'Escape' ? ($sideNavOpen = false, $breadcrumbMenuOpen = false, window.fsharpDocsMobileNav.close()) : window.fsharpDocsMobileNav.trap(evt)")
                match site.assets.navigation with
                | Some _ ->
                    div {
                        _id "docs-navigation-status"
                        _role "status"
                        _hidden true
                        _class "fixed right-4 bottom-4 z-[120] max-w-sm rounded-lg border border-[var(--fve-critical-ring)] bg-[var(--fve-surface)] px-4 py-3 text-sm font-semibold text-[var(--fve-critical-text)] shadow-lg"
                    }
                | None -> ()
                match customRoot with
                | Some root -> root
                | None -> navigationRootWithNavigation site breadcrumbs sideNavItems renderMode docPage
                script { nonceAttribute (); raw "document.addEventListener('DOMContentLoaded', () => { window.initializeDocsToc?.(); window.fsharpDocsFragments?.show(window.location.hash); });" }
            }
        }

    let documentWithNavigation site breadcrumbs sideNavItems renderMode docPage =
        documentWithRoot site breadcrumbs sideNavItems renderMode docPage None

    // Catalog host reuse only: templates supply their own full-page markup, not the documentation shell.
    let documentWithContent site docPage root =
        documentWithRoot site [] site.navigation Embedded docPage (Some root)

    let document (site:DocsSite<'destination>) (docPage:DocumentationPageConfig) =
        documentWithNavigation site (defaultBreadcrumbs site docPage) site.navigation Embedded docPage

/// <summary>
/// Immutable builders for article, reference, canvas, and gallery documentation pages.
/// </summary>
/// <category>layouts</category>
[<RequireQualifiedAccess>]
module DocumentationPage =
    let create activeId title =
        DocumentationPageModel.create activeId title "" Visible Article TableOfContents []

    let validate (page:DocumentationPageConfig) = DocumentationPageModel.validate page

    let withDescription description (page:DocumentationPageConfig) = { page with description = description }
    let withLayout layout (page:DocumentationPageConfig) = { page with layout = layout }
    let withRightRail rightRail (page:DocumentationPageConfig) = { page with rightRail = rightRail }
    /// Render content after the page heading and before outlined sections.
    let withLead lead (page:DocumentationPageConfig) = { page with lead = lead }
    let withSections sections (page:DocumentationPageConfig) = { page with sections = sections }
    let withHiddenHeading (page:DocumentationPageConfig) = { page with heading = VisuallyHidden }
    let withHeadingAdornment adornment (page:DocumentationPageConfig) = { page with headingAdornment = Some adornment }
    let withPager pager (page:DocumentationPageConfig) = { page with pager = Some pager }
    let withFixtures fixtures (page:DocumentationPageConfig) = { page with fixtures = fixtures }
    let withMetadata metadata (page:DocumentationPageConfig) = { page with metadata = metadata }
    let render (page:DocumentationPageConfig) = DocsView.content page

/// <summary>
/// A configured document whose page is required and whose shell/navigation are explicit modifiers.
/// </summary>
/// <category>layouts</category>
[<NoEquality; NoComparison>]
type DocumentConfig<'destination> =
    private
        { page:DocumentationPageConfig
          site:DocsSite<'destination>
          breadcrumbs:Breadcrumb list option
          sideNavItems:NavNode<'destination> list option
          renderMode:FixtureRenderMode }

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

    let withBreadcrumbs breadcrumbs (document:DocumentConfig<'destination>) =
        if List.isEmpty breadcrumbs then invalidArg (nameof breadcrumbs) "At least one breadcrumb is required."
        { document with breadcrumbs = Some breadcrumbs }

    let withSideNavItems items (document:DocumentConfig<'destination>) =
        if List.isEmpty items then invalidArg (nameof items) "At least one side-navigation item is required."
        { document with sideNavItems = Some items }

    let withRenderMode renderMode (document:DocumentConfig<'destination>) =
        { document with renderMode = renderMode }

    let withAppMode appMode (document:DocumentConfig<'destination>) =
        document |> withRenderMode (Fullscreen appMode)

    let render (document:DocumentConfig<'destination>) =
        let site = document.site
        let breadcrumbs =
            document.breadcrumbs
            |> Option.defaultWith (fun () -> Navigation.breadcrumbs site.navigation site.homeId document.page.activeId)
        let sideNavItems = document.sideNavItems |> Option.defaultValue site.navigation
        DocsView.documentWithNavigation site breadcrumbs sideNavItems document.renderMode document.page
