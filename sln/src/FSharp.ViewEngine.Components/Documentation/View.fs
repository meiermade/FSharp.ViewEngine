namespace FSharp.ViewEngine.Components.Documentation

open FSharp.ViewEngine
open System
open System.Text.Json
open type Html
open type Datastar

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

    let navigateAction href =
        let encoded = jsString href
        $"$sideNavOpen = false; $breadcrumbMenuOpen = false; window.fsharpDocsNavigation.navigate(evt, {encoded})"

    let themeStyle theme =
        $"--fve-brand-subtle:{theme.accent100};--fve-brand-solid:{theme.accent700};--fve-brand-hover:{theme.accent900};--fve-brand-active:{theme.accent900};--fve-brand-text:{theme.accent900};--fve-brand-ring:{theme.accent500}"

module private ViewStyles =
    let iconButton = "grid size-8 shrink-0 cursor-pointer place-items-center rounded-lg border border-transparent bg-transparent p-1.5 text-[var(--fve-muted-text)] no-underline hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] [&>svg]:size-[1.125rem]"
    let navItem = "flex min-h-7 w-max min-w-full cursor-pointer items-center gap-1.5 rounded-md border-0 bg-transparent px-1.5 py-1 text-left text-sm leading-5 font-medium text-[var(--fve-muted-text)] no-underline hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)]"
    let navList = "m-0 w-max min-w-full list-none p-0 [&>li+li]:mt-px"
    let sectionContent = "flex flex-col gap-4 text-[var(--fve-text)] [&>p]:m-0 [&>p]:[overflow-wrap:anywhere] [&>p]:text-base [&>p]:leading-relaxed [&>p]:text-[var(--fve-muted-text)] [&>ul]:m-0 [&>ul]:list-disc [&>ul]:pl-5 [&>ul]:text-base [&>ul]:leading-relaxed [&>ul]:text-[var(--fve-muted-text)] [&>ol]:m-0 [&>ol]:list-decimal [&>ol]:pl-5 [&>ol]:text-base [&>ol]:leading-relaxed [&>ol]:text-[var(--fve-muted-text)] [&_li+li]:mt-2 [&_li::marker]:text-[var(--fve-brand-ring)] [&_:where(p,li)>a]:font-semibold [&_:where(p,li)>a]:text-[var(--fve-brand-text)] [&_:where(p,li)>a]:underline [&_:where(p,li)>a]:decoration-[var(--fve-brand-ring)] [&_:where(p,li)>a]:underline-offset-[0.18em] [&_:where(p,li)>a:focus-visible]:outline-2 [&_:where(p,li)>a:focus-visible]:outline-offset-2 [&_:where(p,li)>a:focus-visible]:outline-[var(--fve-brand-ring)] [&_table]:w-full [&_table]:border-collapse [&_table]:text-sm [&_th]:bg-[var(--fve-surface-subtle)] [&_th]:p-3 [&_th]:text-left [&_th]:text-xs [&_th]:tracking-wide [&_th]:text-[var(--fve-muted-text)] [&_th]:uppercase [&_td]:border-t [&_td]:border-[var(--fve-border)] [&_td]:p-3 [&_td]:align-top"
    let codeSurface = "bg-[var(--fve-neutral-subtle)] text-[var(--fve-text)]"
    let tocLinks = "flex flex-col gap-2 [&>a]:text-sm [&>a]:leading-5 [&>a]:text-[var(--fve-muted-text)] [&>a]:no-underline [&>a:hover]:text-[var(--fve-brand-text)] [&>a[aria-current=location]]:font-semibold [&>a[aria-current=location]]:text-[var(--fve-brand-text)]"

module MermaidView =
    let private render (classes:string) (source:string) =
        div {
            _class classes
            _data("init", "window.renderMermaid?.(el)")
            _data("docs-diagram", "true")
            _data("mermaid-source", source)
            _data("mermaid-state", "pending")
            _ariaBusy true
            p {
                _class "m-0 text-center text-sm leading-relaxed text-[var(--fve-muted-text)]"
                _data("mermaid-status", "true")
                _role "status"
                "Rendering diagram…"
            }
        }

    let private classes = "mermaid overflow-x-auto rounded-xl border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] p-5 data-[mermaid-state=pending]:grid data-[mermaid-state=pending]:min-h-32 data-[mermaid-state=pending]:place-items-center data-[mermaid-state=failed]:grid data-[mermaid-state=failed]:min-h-32 data-[mermaid-state=failed]:place-items-center [&>svg]:h-auto [&>svg]:max-w-full"

    let diagram source = render classes source

    let c4Diagram source = render $"{classes} max-sm:[&>svg]:min-w-3xl [&_a]:cursor-pointer [&_a:focus-visible]:outline-2 [&_a:focus-visible]:outline-offset-2 [&_a:focus-visible]:outline-[var(--fve-brand-ring)]" source

[<NoEquality; NoComparison>]
type DocsMermaid = private { source:string; c4:bool }

[<RequireQualifiedAccess>]
module Mermaid =
    let create source : DocsMermaid =
        if String.IsNullOrWhiteSpace source then invalidArg (nameof source) "Mermaid source cannot be empty."
        { source = source; c4 = false }

    let withC4 (diagram:DocsMermaid) = { diagram with c4 = true }

    let render (diagram:DocsMermaid) =
        if diagram.c4 then MermaidView.c4Diagram diagram.source
        else MermaidView.diagram diagram.source

[<NoEquality; NoComparison>]
type DocsCallout = private { label:string; content:HtmlElement list }

[<RequireQualifiedAccess>]
module Callout =
    let create label content : DocsCallout =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A callout label is required."
        { label = label; content = content }

    let render (callout:DocsCallout) =
        div {
            _class "border-l-2 border-[var(--fve-brand-ring)] bg-[var(--fve-brand-subtle)] px-4 py-3"
            _data("docs-callout", "true")
            div { _class "text-xs font-semibold tracking-wide text-[var(--fve-brand-text)] uppercase"; callout.label }
            div { _class "mt-1 text-sm leading-relaxed text-[var(--fve-text)]"; callout.content }
        }

module private DocsSectionView =
    let render showHeading (docSection:DocsSection) =
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
        let items : FSharp.ViewEngine.Components.Primitives.MenuItem<unit> list =
            [ for mode, label in [ System, "System"; Light, "Light"; Dark, "Dark" ] do
                let value = DocsColorMode.value mode
                FSharp.ViewEngine.Components.Primitives.MenuItem.radio $"$colorMode = '{value}'" label
                |> FSharp.ViewEngine.Components.Primitives.MenuItem.withChecked (mode = defaultMode)
                |> FSharp.ViewEngine.Components.Primitives.MenuItem.withCheckedExpression $"$colorMode == '{value}'" ]
        div {
            _class "relative shrink-0"
            _data("on:fsharpdocs:colormode__window", "$colorMode = window.fsharpDocsColorMode.current()")
            FSharp.ViewEngine.Components.Primitives.DropdownMenu.create "docs-color-mode" "Choose color theme" items
            |> FSharp.ViewEngine.Components.Primitives.DropdownMenu.withIconTrigger icon
            |> FSharp.ViewEngine.Components.Primitives.DropdownMenu.render (fun () -> "")
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
                    _class (ViewStyles.navList + " mt-0.5 ml-3 min-w-[calc(100%-0.75rem)] border-l border-[var(--fve-border)]")
                    _data("show", $"${signal}")
                    if not (group.defaultOpen || containsActive) then _style "display:none"
                    for child in group.children do node activeId child
                }
            | Page page ->
                a {
                    _id $"nav-{page.id}"
                    _href page.href
                    _data("on:click", navigateAction page.href)
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
            _class "fixed inset-y-0 left-0 z-50 hidden h-dvh w-[min(18rem,calc(100vw-3rem))] border-r border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] shadow-xl lg:sticky lg:block lg:w-[min(18rem,24vw)] lg:shadow-none"
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
                    ul { _class ViewStyles.navList; for section in items do node activeId section }
                }
            }
        }

    let topNav (site:DocsSite<'destination>) (breadcrumbs:Breadcrumb list) =
        let hiddenCount = if breadcrumbs.Length > 2 then breadcrumbs.Length - 1 else 0
        let hiddenBreadcrumbs = breadcrumbs |> List.take hiddenCount

        header {
            _class "relative z-30 flex h-12 shrink-0 items-center justify-between gap-3 border-b border-[var(--fve-border)] bg-[color-mix(in_srgb,var(--fve-page)_96%,transparent)] px-4 backdrop-blur-lg sm:pr-2.5 lg:px-8"
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
                                        | Some href -> a { _href href; _data("on:click", navigateAction href); crumb.label }
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
                _class (FSharp.ViewEngine.Components.Primitives.ComponentsTheme.sky |> FSharp.ViewEngine.Components.Primitives.ComponentsTheme.withDensity FSharp.ViewEngine.Components.Primitives.Density.Compact |> FSharp.ViewEngine.Components.Primitives.ComponentsTheme.withControlSize FSharp.ViewEngine.Components.Primitives.ControlSize.Small |> FSharp.ViewEngine.Components.Primitives.ComponentsTheme.className |> fun theme -> theme + " flex shrink-0 items-center gap-1.5 max-sm:gap-0.5")
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
                    _data("on:click", "window.fsharpDocsNavigation.navigateToFragment(evt, evt.currentTarget.getAttribute('href'))")
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
            _data("on:click", navigateAction link.href)
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

module DocsView =
    open ViewHelpers

    let private tocItems (page:DocsPage) : TocItem list =
        page.sections
        |> List.map (fun section -> { level = section.level; label = section.title; href = $"#{section.id}" })

    let content (page:DocsPage) =
        div {
            _class "flex flex-col gap-12"
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
            let items = tocItems page
            match page.rightRail with
            | TableOfContents when not items.IsEmpty -> TocView.mobile items
            | _ -> ()
            for section in page.sections do DocsSectionView.render (page.layout <> Gallery) section
            match page.pager with
            | Some pager -> PagerView.render pager
            | None -> ()
        }

    let private defaultBreadcrumbs (site:DocsSite<'destination>) (page:DocsPage) =
        Navigation.breadcrumbs site.navigation site.homeId page.activeId

    let sideNavWith (site:DocsSite<'destination>) (items:NavNode<'destination> list) (page:DocsPage) =
        NavigationView.sideNav site items page.activeId

    let sideNav (site:DocsSite<'destination>) (page:DocsPage) = sideNavWith site site.navigation page

    let pageContentWith (site:DocsSite<'destination>) (breadcrumbs:Breadcrumb list) (page:DocsPage) =
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
                        _class $"min-w-0 flex-1 overflow-y-auto bg-[var(--fve-page)] px-4 py-10 sm:px-6 lg:px-10 {mainClasses}"
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

    let pageContent (site:DocsSite<'destination>) (page:DocsPage) =
        pageContentWith site (defaultBreadcrumbs site page) page

    let pageWithNavigation (site:DocsSite<'destination>) (breadcrumbs:Breadcrumb list) (sideNavItems:NavNode<'destination> list) (docPage:DocsPage) =
        div {
            _id "page"
            _class "flex h-dvh overflow-hidden bg-[var(--fve-page)]"
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

    let page (site:DocsSite<'destination>) (docPage:DocsPage) =
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
        let rootClasses =
            if Fixture.surface fixture = "phone" then
                "fixed inset-0 z-100 grid place-items-center overflow-auto bg-[var(--fve-surface-subtle)] p-4 text-[var(--fve-text)] max-[32rem]:place-items-stretch max-[32rem]:p-0 [&>*]:max-h-full [&>*]:w-[min(100%,21.5rem)] [&_.fve-floating-panel]:bottom-[max(4rem,calc(env(safe-area-inset-bottom)+3.5rem))]! [&_.fve-floating-panel-trigger]:bottom-[max(4rem,calc(env(safe-area-inset-bottom)+3.5rem))]! max-[32rem]:[&>*]:max-h-none max-[32rem]:[&>*]:w-full max-[32rem]:[&_[data-fve-phone=true]]:h-dvh max-[32rem]:[&_[data-fve-phone=true]]:max-h-none max-[32rem]:[&_[data-fve-phone=true]]:w-full max-[32rem]:[&_[data-fve-phone=true]]:rounded-none max-[32rem]:[&_[data-fve-phone=true]]:border-0 max-[32rem]:[&_[data-fve-phone=true]]:p-0 max-[32rem]:[&_[data-fve-phone=true]]:shadow-none max-[32rem]:[&_[data-fve-phone-screen=true]]:rounded-none max-[32rem]:[&_[data-fve-phone-side-button=true]]:hidden"
            else
                "fixed inset-0 z-100 overflow-auto bg-[var(--fve-page)] text-[var(--fve-text)] [&>*]:min-h-dvh! [&>*]:min-w-full [&_.fve-floating-panel]:bottom-[max(4rem,calc(env(safe-area-inset-bottom)+3.5rem))]! [&_.fve-floating-panel-trigger]:bottom-[max(4rem,calc(env(safe-area-inset-bottom)+3.5rem))]!"
        fragment {
            div {
                _id "fve-app-mode-root"
                _class rootClasses
                _attr ("data-fve-app-mode-root", "true")
                _attr ("data-fve-app-mode-surface", Fixture.surface fixture)
                _attr ("data-fve-app-mode-frame", frameId)
                _ariaLabel (Fixture.label fixture)
                Fixture.fullscreenContent fixture
            }
            nav {
                _id "fve-app-mode-controls"
                _class (FSharp.ViewEngine.Components.Primitives.ComponentsTheme.sky |> FSharp.ViewEngine.Components.Primitives.ComponentsTheme.withDensity FSharp.ViewEngine.Components.Primitives.Density.Compact |> FSharp.ViewEngine.Components.Primitives.ComponentsTheme.withControlSize FSharp.ViewEngine.Components.Primitives.ControlSize.Small |> FSharp.ViewEngine.Components.Primitives.ComponentsTheme.className |> fun theme -> theme + " fixed right-[max(0.75rem,env(safe-area-inset-right))] bottom-[max(0.5rem,env(safe-area-inset-bottom))] z-[110] flex min-h-10 max-w-[calc(100vw-1.5rem)] items-center gap-0.5 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[color-mix(in_srgb,var(--fve-surface)_92%,transparent)] p-1 text-xs leading-none font-semibold text-[var(--fve-text)] shadow-[0_12px_32px_rgb(0_0_0/20%)] backdrop-blur-[14px] data-[fve-app-dock=top]:top-[max(0.5rem,env(safe-area-inset-top))] data-[fve-app-dock=top]:bottom-auto max-[32rem]:right-2 max-[32rem]:bottom-2 max-[32rem]:data-[fve-app-dock=top]:top-2 [&>a]:grid [&>a]:size-8 [&>a]:shrink-0 [&>a]:cursor-pointer [&>a]:place-items-center [&>a]:rounded-[var(--fve-radius-control)] [&>a]:border-0 [&>a]:bg-transparent [&>a]:text-inherit [&>a]:no-underline [&>a:hover]:bg-[var(--fve-surface-hover)] [&>a:focus-visible]:outline-2 [&>a:focus-visible]:outline-offset-2 [&>a:focus-visible]:outline-[var(--fve-brand-ring)] [&>button]:grid [&>button]:size-8 [&>button]:shrink-0 [&>button]:cursor-pointer [&>button]:place-items-center [&>button]:rounded-[var(--fve-radius-control)] [&>button]:border-0 [&>button]:bg-transparent [&>button]:text-inherit [&>button:hover]:bg-[var(--fve-surface-hover)] [&>button:focus-visible]:outline-2 [&>button:focus-visible]:outline-offset-2 [&>button:focus-visible]:outline-[var(--fve-brand-ring)] [&>span[aria-disabled=true]]:grid [&>span[aria-disabled=true]]:size-8 [&>span[aria-disabled=true]]:place-items-center [&>span[aria-disabled=true]]:text-[var(--fve-muted-text)] [&_svg]:size-4")
                _attr ("data-fve-app-mode-controls", "true")
                _dataAttr ("data-fve-app-dock", "$appModeDockTop ? 'top' : 'bottom'")
                _dataAttr ("data-fve-color-mode", "($colorMode == 'dark' || ($colorMode == 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches)) ? 'light' : 'dark'")
                _data("on:fsharpdocs:colormode__window", "document.getElementById('fve-app-mode-controls')?.setAttribute('data-fve-color-mode', document.documentElement.classList.contains('dark') ? 'light' : 'dark')")
                _ariaLabel "App mode controls"
                let hasWorkflow = Fixture.previous fixture |> Option.isSome || Fixture.next fixture |> Option.isSome
                if hasWorkflow then
                    appModeDirection frameId "Previous" previousIcon (Fixture.previous fixture)
                if not states.IsEmpty then
                    div {
                        _class "block min-w-0 max-w-[min(14rem,42vw)] [&_.fve-popup-control]:min-h-8 [&_.fve-popup-control]:w-auto [&_.fve-popup-control]:max-w-full [&_.fve-popup-control]:border-0 [&_.fve-popup-control]:bg-transparent [&_.fve-popup-control]:px-2 [&_.fve-popup-control]:shadow-none [&_.fve-popup-control>span]:overflow-hidden [&_.fve-popup-control>span]:text-ellipsis [&_.fve-popup-control>span]:whitespace-nowrap [&_[role=menu]]:z-[120] [&_[role=menu]]:min-w-40 [&_[role=menu]]:border [&_[role=menu]]:border-[var(--fve-border)] [&_[role=menu]]:bg-[var(--fve-surface)] [&_[role=menu]]:shadow-xl"
                        _attr ("data-fve-app-mode-state-select", "true")
                        states
                        |> List.map (fun state -> FSharp.ViewEngine.Components.Primitives.MenuItem.link (Fixture.appModeHref frameId (FixtureState.href state)) (FixtureState.label state))
                        |> FSharp.ViewEngine.Components.Primitives.DropdownMenu.create $"fve-app-mode-{frameId}-state" "Review state"
                        |> FSharp.ViewEngine.Components.Primitives.DropdownMenu.withAlignment FSharp.ViewEngine.Components.Primitives.MenuAlignment.End
                        |> FSharp.ViewEngine.Components.Primitives.DropdownMenu.withTriggerContent (span { currentState |> Option.map FixtureState.label |> Option.defaultValue "Review state" })
                        |> FSharp.ViewEngine.Components.Primitives.DropdownMenu.render id
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

    let documentWithNavigation (site:DocsSite<'destination>) (breadcrumbs:Breadcrumb list) (sideNavItems:NavNode<'destination> list) (renderMode:FixtureRenderMode) (docPage:DocsPage) =
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

        let navGroups = NavNode.collectGroups sideNavItems
        let navSignals =
            navGroups
            |> List.map (fun node ->
                let signal = signalName (NavNode.id node)
                let shouldOpen = NavNode.defaultOpen node || NavNode.containsActive docPage.activeId node
                let containsActive = NavNode.containsActive docPage.activeId node
                $"{signal}: window.fsharpDocsNav.initial({jsString (NavNode.id node)}, {shouldOpen.ToString().ToLowerInvariant()}, {containsActive.ToString().ToLowerInvariant()})")

        let signals = "{ sideNavOpen: false, breadcrumbMenuOpen: false, appModeDockTop: false, colorMode: window.fsharpDocsColorMode.current()" + (if navSignals.IsEmpty then "" else ", " + String.concat ", " navSignals) + " }"
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
const wireMermaidLinks = node => {
  for (const link of node.querySelectorAll('svg a')) {
    const href = link.getAttribute('href') ?? link.getAttribute('xlink:href');
    if (!href?.startsWith('/')) continue;
    const encodedHref = JSON.stringify(href);
    link.setAttribute('data-on:click', `window.fsharpDocsNavigation.navigate(evt, ${encodedHref})`);
  }
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
        wireMermaidLinks(node);
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
window.fsharpDocsNavigation = {
  pending: null,
  controller: null,
  documentUrl: window.location.pathname + window.location.search,
  currentUrl() {
    return window.location.pathname + window.location.search;
  },
  committedHref(href) {
    const target = new URL(href, window.location.origin);
    target.searchParams.delete('fveAppReturn');
    target.searchParams.delete('fveAppTransition');
    return `${target.pathname}${target.search}${target.hash}`;
  },
  eligible(event, href) {
    const link = event?.target?.closest?.('a[href]');
    if (event?.defaultPrevented || event?.button !== 0 || event?.metaKey || event?.ctrlKey || event?.shiftKey || event?.altKey || !link || link.target && link.target !== '_self' || link.hasAttribute('download')) return null;
    const target = new URL(href ?? link.href, window.location.origin);
    if (target.origin !== window.location.origin || !target.protocol.startsWith('http') || target.hash || target.pathname === '/logout' || target.pathname === '/login') return null;
    const frame = document.body.dataset.fveAppModeFrame;
    if (frame && !link.hasAttribute('data-fve-app-mode-exit')) {
      target.searchParams.set('fveAppMode', 'app');
      target.searchParams.set('fveAppFrame', frame);
    }
    return `${target.pathname}${target.search}`;
  },
  request(href, intent) {
    if (this.pending?.href === href && this.pending?.intent === intent) return;
    this.controller?.abort();
    this.controller = new AbortController();
    this.pending = { href, intent };
    document.documentElement.dataset.fsharpDocsNavigationPending = 'true';
    document.body.dispatchEvent(new CustomEvent('fsharpdocs:navigate', { bubbles: true, detail: { href, controller: this.controller } }));
  },
  navigate(event, href) {
    const target = this.eligible(event, href);
    if (!target) return false;
    event.preventDefault();
    this.request(target, 'push');
    return true;
  },
  restore() {
    const target = this.currentUrl();
    if (target !== this.documentUrl) this.request(target, 'restore');
  },
  observer: null,
  layoutObserver: null,
  scrollRoot: null,
  scrollHandler: null,
  setCurrentFragment(id) {
    for (const link of document.querySelectorAll('[data-docs-toc] a[href^="#"]')) {
      if (link.getAttribute('href') === `#${id}`) link.setAttribute('aria-current', 'location');
      else link.removeAttribute('aria-current');
    }
  },
  initializeToc() {
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
      this.setCurrentFragment(current.id);
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
  },
  showFragment(href) {
    if (!href?.startsWith('#')) return false;
    const target = document.getElementById(decodeURIComponent(href.slice(1)));
    if (!target) return false;
    target.scrollIntoView({ block: 'start' });
    target.focus({ preventScroll: true });
    this.setCurrentFragment(target.id);
    return true;
  },
  navigateToFragment(event, href) {
    if (!href?.startsWith('#') || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) return;
    if (!document.getElementById(decodeURIComponent(href.slice(1)))) return;
    event.preventDefault();
    window.history.pushState(null, '', href);
    this.showFragment(href);
    event.currentTarget.closest('details')?.removeAttribute('open');
  },
  async complete() {
    window.fsharpDocsColorMode?.apply(window.fsharpDocsColorMode.current());
    const pending = this.pending;
    if (!pending) return;
    this.pending = null;
    this.controller = null;
    delete document.documentElement.dataset.fsharpDocsNavigationPending;
    const committedHref = this.committedHref(pending.href);
    const returnFrame = new URL(pending.href, window.location.origin).searchParams.get('fveAppReturn');
    if (pending.intent === 'push' && this.currentUrl() !== committedHref) window.history.pushState(null, '', committedHref);
    this.documentUrl = committedHref;
    for (const element of document.querySelectorAll('[data-docs-main], [data-docs-page-viewport], [data-docs-page-layout], [data-docs-custom-rail]')) {
      element.scrollTo({ top: 0, left: 0, behavior: 'instant' });
    }
    window.scrollTo({ top: 0, left: 0, behavior: 'instant' });
    const content = document.getElementById('page-content') ?? document.getElementById('fve-app-mode-root');
    // Datastar may preserve a Mermaid host whose data-init expression has
    // already run. Complete independent enhancement lifecycles together.
    const [codeResult] = await Promise.allSettled([
      window.renderCode?.(content),
      window.renderMermaid?.(content, true),
      window.renderInitialDocsPreviews?.(content)
    ]);
    this.initializeToc();
    this.showFragment(window.location.hash);
    if (returnFrame) document.getElementById(`fve-fixture-${returnFrame}-launcher`)?.focus();
    if (codeResult.status === 'rejected') throw codeResult.reason;
  },
  fail() {
    if (!this.pending) return;
    const pending = this.pending;
    this.pending = null;
    this.controller = null;
    delete document.documentElement.dataset.fsharpDocsNavigationPending;
    if (pending.intent === 'restore') window.location.assign(this.documentUrl);
  }
};
document.addEventListener('submit', event => {
  const frame = document.body.dataset.fveAppModeFrame;
  const form = event.target;
  if (!frame || event.defaultPrevented || !(form instanceof HTMLFormElement)) return;
  const submitter = event.submitter;
  const method = submitter?.hasAttribute('formmethod') ? submitter.formMethod : form.method;
  const target = submitter?.hasAttribute('formtarget') ? submitter.formTarget : form.target;
  const action = new URL(submitter?.hasAttribute('formaction') ? submitter.formAction : form.action);
  if (method.toLowerCase() !== 'get' || target && target !== '_self' || action.origin !== window.location.origin) return;
  for (const [name, value] of [['fveAppMode', 'app'], ['fveAppFrame', frame]]) {
    let input = form.querySelector(`input[type=hidden][name=${name}]`);
    if (!input) {
      input = document.createElement('input');
      input.type = 'hidden';
      input.name = name;
      form.append(input);
    }
    input.value = value;
  }
}, true);
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
document.addEventListener('datastar-fetch', event => {
  if (event.detail?.el?.tagName !== 'BODY') return;
  if (event.detail.type === 'finished') window.fsharpDocsNavigation.complete();
  if (event.detail.type === 'error' || event.detail.type === 'retries-failed') window.fsharpDocsNavigation.fail();
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
                _class (FSharp.ViewEngine.Components.Primitives.ComponentsTheme.sky |> FSharp.ViewEngine.Components.Primitives.ComponentsTheme.withDensity FSharp.ViewEngine.Components.Primitives.Density.Compact |> FSharp.ViewEngine.Components.Primitives.ComponentsTheme.className |> fun theme -> theme + " m-0 bg-[var(--fve-page)] font-sans text-[var(--fve-text)] antialiased")
                _data("signals", signals)
                _data("effect", "window.fsharpDocsColorMode.set($colorMode)")
                _data("on-signal-patch", $"window.fsharpDocsNav.save({navState})")
                _data("on:click", "window.fsharpDocsNavigation.navigate(evt)")
                _data("on:fsharpdocs:navigate", "@get(evt.detail.href, { filterSignals: { exclude: /.*/ }, requestCancellation: evt.detail.controller, retry: 'never', retryMaxCount: 0 })")
                _data("on:popstate__window", "window.fsharpDocsNavigation.restore()")
                match activeAppMode with
                | Some (request, _) ->
                    _attr ("data-fve-app-mode-frame", AppMode.frameId request)
                    _data("on:keydown__window", "evt.key == 'Escape' && !evt.defaultPrevented && !document.querySelector(':popover-open, [data-fve-app-mode-root] button[aria-controls][aria-expanded=true], [data-fve-app-mode-root] dialog[open]') ? window.fsharpDocsNavigation.request(document.querySelector('[data-fve-app-mode-exit]')?.getAttribute('href'), 'push') : null")
                | None ->
                    _data("on:keydown__window", "evt.key == 'Escape' ? ($sideNavOpen = false, $breadcrumbMenuOpen = false, window.fsharpDocsMobileNav.close()) : window.fsharpDocsMobileNav.trap(evt)")
                match activeAppMode with
                | Some (request, fixture) -> appModeView site request fixture
                | None -> pageWithNavigation site breadcrumbs sideNavItems docPage
                script { nonceAttribute (); raw "document.addEventListener('DOMContentLoaded', async () => { const content = document.getElementById('page-content') ?? document.getElementById('fve-app-mode-root'); await Promise.all([window.renderCode?.(content), window.renderMermaid?.(content, true), window.renderInitialDocsPreviews?.(content)]); window.fsharpDocsNavigation.initializeToc(); });" }
            }
        }

    let document (site:DocsSite<'destination>) (docPage:DocsPage) =
        documentWithNavigation site (defaultBreadcrumbs site docPage) site.navigation Embedded docPage

/// Immutable builders for article, reference, canvas, and gallery documentation pages.
[<RequireQualifiedAccess>]
module DocumentationPage =
    let create activeId title =
        DocsPage.create activeId title "" Visible Article TableOfContents []

    let withDescription description (page:DocsPage) = { page with description = description }
    let withLayout layout (page:DocsPage) = { page with layout = layout }
    let withRightRail rightRail (page:DocsPage) = { page with rightRail = rightRail }
    let withSections sections (page:DocsPage) = { page with sections = sections }
    let withHiddenHeading (page:DocsPage) = { page with heading = VisuallyHidden }
    let withHeadingAdornment adornment (page:DocsPage) = { page with headingAdornment = Some adornment }
    let withPager pager (page:DocsPage) = { page with pager = Some pager }
    let withFixtures fixtures (page:DocsPage) = { page with fixtures = fixtures }
    let withMetadata metadata (page:DocsPage) = { page with metadata = metadata }
    let render (page:DocsPage) = DocsView.content page

/// A configured document whose page is required and whose shell/navigation are explicit modifiers.
[<NoEquality; NoComparison>]
type DocsDocument<'destination> =
    private
        { page:DocsPage
          site:DocsSite<'destination>
          breadcrumbs:Breadcrumb list option
          sideNavItems:NavNode<'destination> list option
          renderMode:FixtureRenderMode }

/// Public immutable builders for a complete documentation document.
[<RequireQualifiedAccess>]
module Document =
    let create site page : DocsDocument<'destination> =
        { page = page
          site = site
          breadcrumbs = None
          sideNavItems = None
          renderMode = Embedded }

    let withBreadcrumbs breadcrumbs (document:DocsDocument<'destination>) =
        if List.isEmpty breadcrumbs then invalidArg (nameof breadcrumbs) "At least one breadcrumb is required."
        { document with breadcrumbs = Some breadcrumbs }

    let withSideNavItems items (document:DocsDocument<'destination>) =
        if List.isEmpty items then invalidArg (nameof items) "At least one side-navigation item is required."
        { document with sideNavItems = Some items }

    let withRenderMode renderMode (document:DocsDocument<'destination>) =
        { document with renderMode = renderMode }

    let withAppMode appMode (document:DocsDocument<'destination>) =
        document |> withRenderMode (Fullscreen appMode)

    let render (document:DocsDocument<'destination>) =
        let site = document.site
        let breadcrumbs =
            document.breadcrumbs
            |> Option.defaultWith (fun () -> Navigation.breadcrumbs site.navigation site.homeId document.page.activeId)
        let sideNavItems = document.sideNavItems |> Option.defaultValue site.navigation
        DocsView.documentWithNavigation site breadcrumbs sideNavItems document.renderMode document.page
