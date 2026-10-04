namespace Docs.Examples

open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Svg
open Model
open Ledger.Domain

/// Ordinary consumer-owned markup. The same installed controls can be used by a real app or its Spec.
module Layout =
    let theme = ComponentsTheme.sky
    let link (href:string) (label:string) =
        a { _href href; _class "inline-flex min-h-8 shrink-0 items-center whitespace-nowrap rounded-[var(--fve-radius-control)] px-3 py-1.5 text-sm font-medium text-[var(--fve-brand-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"; label }
    let primaryLink (href:string) (label:string) =
        a { _href href; _class "inline-flex min-h-8 shrink-0 items-center justify-center whitespace-nowrap rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-text)] px-3 py-1.5 text-sm font-medium text-[light-dark(white,#101828)] hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"; label }
    let section (title:string) (content:HtmlElement) =
        section { _ariaLabel title; _class "grid min-w-0 grid-cols-1 gap-4"; SectionHeader.create title |> SectionHeader.render; content }
    let prose (content:HtmlElement) = div { _class "prose max-w-none text-[var(--fve-text)] dark:prose-invert"; content }
    let icon (data:string) =
        svg { _viewBox "0 0 24 24"; _fill "none"; _stroke "currentColor"; _strokeWidth "1.5"; _class "size-5 shrink-0"; _ariaHidden true; path { _strokeLinecap "round"; _strokeLinejoin "round"; _d data } }
    let private chevron = icon "m9 6 6 6-6 6"
    let private footerLink (current:string) (href:string) (label:string) (leading:HtmlElement) =
        a {
            _href href
            if current=href then _ariaCurrent "page"
            _class ("flex min-h-12 w-full items-center gap-3 px-4 text-sm font-semibold no-underline hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-[var(--fve-brand-ring)] " + (if current=href then "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)]" else "text-[var(--fve-text)]"))
            leading; span { _class "min-w-0 flex-1 truncate"; label }; chevron
        }
    let private workspaceSelector navId current (query:Query) =
        let workspace = query.workspace
        let destination (selected:Workspace) =
            workspaceUrl current selected
            + (if query.specification then
                   querySuffix [
                       yield "specState",query.specState
                       if query.resource<>"" then yield "resource",query.resource
                       if query.appMode then yield "appMode","1"
                       yield "fveAppDock",query.dock ]
               elif query.embedded then "&embedded=1" else "")
        let organizationDialog =
            Dialog.create (navId+"-organizations") "Change organization" (div {
                _class "grid gap-3"
                for organization in organizations do
                    let selected = workspaceFromStrings organization.id "production" (if organization.id=workspace.organization.id then workspace.ledger.id else "")
                    a { _href (destination selected); _class "flex min-h-12 items-center justify-between gap-3 rounded-lg border border-[var(--fve-border)] p-3 hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; span { strong { _class "block text-sm"; organization.name }; span { _class "text-xs text-[var(--fve-muted-text)]"; organization.role+" · "+selected.ledger.name } }; if organization.id=workspace.organization.id then Badge.create "Current" |> Badge.render }
                link (applicationHref query (ApplicationPage.SettingsSection "organizations")) "Manage organizations"
            }) |> Dialog.withDescription "Choose an organization and its example ledger. No live memberships or financial data are changed."
            |> Dialog.withFooter (Button.create (ButtonContent.Text "Cancel") |> Button.withAttributes [_data("on:click", $"document.getElementById('{navId}-organizations').close()")] |> Button.render)
            |> Dialog.withAttributes [_data("on:close__capture", $"document.getElementById('{navId}-workspace-trigger').focus()")]
        let environmentDialog =
            Dialog.create (navId+"-environments") "Change environment" (div {
                _class "grid gap-3"
                for environment in environments do
                    a { _href (destination {workspace with environment=environment}); _class "flex min-h-12 items-center justify-between gap-3 rounded-lg border border-[var(--fve-border)] p-3 hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; span { strong { _class "block text-sm"; environment.name }; span { _class "text-xs text-[var(--fve-muted-text)]"; if environment.sandbox then "Sandbox · Seeded example data" else "Production context · Seeded example data" } }; if environment.id=workspace.environment.id then Badge.create "Current" |> Badge.render }
                link (applicationHref query (ApplicationPage.SettingsSection "environments")) "Manage environments"
            }) |> Dialog.withDescription "Switch the server-rendered workspace context. Every environment in this template uses resettable example records."
            |> Dialog.withFooter (Button.create (ButtonContent.Text "Cancel") |> Button.withAttributes [_data("on:click", $"document.getElementById('{navId}-environments').close()")] |> Button.render)
            |> Dialog.withAttributes [_data("on:close__capture", $"document.getElementById('{navId}-workspace-trigger').focus()")]
        div {
            _class "w-full"
            DropdownMenu.create (navId+"-workspace") ("Open workspace menu: "+workspace.ledger.name)
            |> DropdownMenu.withAlignment DropdownMenuAlignment.Start
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.content (span {
                _class "flex w-full items-center gap-3"
                icon "M3.75 6A2.25 2.25 0 0 1 6 3.75h2.25A2.25 2.25 0 0 1 10.5 6v2.25a2.25 2.25 0 0 1-2.25 2.25H6a2.25 2.25 0 0 1-2.25-2.25V6ZM13.5 6a2.25 2.25 0 0 1 2.25-2.25H18A2.25 2.25 0 0 1 20.25 6v2.25a2.25 2.25 0 0 1-2.25 2.25h-2.25a2.25 2.25 0 0 1-2.25-2.25V6ZM3.75 15.75A2.25 2.25 0 0 1 6 13.5h2.25a2.25 2.25 0 0 1 2.25 2.25V18a2.25 2.25 0 0 1-2.25 2.25H6a2.25 2.25 0 0 1-2.25-2.25v-2.25ZM13.5 15.75a2.25 2.25 0 0 1 2.25-2.25H18a2.25 2.25 0 0 1 2.25 2.25V18a2.25 2.25 0 0 1-2.25 2.25h-2.25a2.25 2.25 0 0 1-2.25-2.25v-2.25Z"
                span { _class "min-w-0 flex-1 truncate text-left"; workspace.ledger.name }; icon "m6 9 6 6 6-6"
            }) |> DropdownMenuTrigger.asFullRow)
            |> DropdownMenu.withContent [
                DropdownMenuItem.group "Ledger" (ledgers |> List.filter (fun ledger -> ledger.organizationId=workspace.organization.id) |> List.map (fun ledger ->
                    let item = DropdownMenuItem.link (destination {workspace with ledger=ledger}) ledger.name
                    if ledger.id=workspace.ledger.id then item |> DropdownMenuItem.withTrailing (icon "m4.5 12.75 6 6 9-13.5") else item))
                DropdownMenuItem.separator
                DropdownMenuItem.action $"document.getElementById('{navId}-organizations').showModal()" "Organization"
                |> DropdownMenuItem.withDescription workspace.organization.name |> DropdownMenuItem.withTrailing chevron
                DropdownMenuItem.action $"document.getElementById('{navId}-environments').showModal()" "Environment"
                |> DropdownMenuItem.withDescription workspace.environment.name |> DropdownMenuItem.withTrailing chevron ]
            |> DropdownMenu.render id
            organizationDialog |> Dialog.render
            environmentDialog |> Dialog.render
        }
    let private appNavigation navId page (current:string) (query:Query) (settingsKey:string option) =
        let url = applicationHref query
        let groups =
            match settingsKey with
            | Some _ -> [SideNavSection.ungrouped (settingsSections |> List.map (fun (key,label) -> SideNavItem.create (url (ApplicationPage.SettingsSection key)) label))]
            | None -> [SideNavSection.ungrouped [SideNavItem.create (url ApplicationPage.Home) "Home"]; SideNavSection.group "Accounting" [SideNavItem.create (url ApplicationPage.Accounts) "Accounts";SideNavItem.create (url ApplicationPage.Transactions) "Transactions"]]
        let header =
            SideNavHeader.create "Ledger" |> SideNavHeader.withContent (
                match settingsKey with
                | Some _ -> link (url ApplicationPage.Home) "← Ledger"
                | None -> a { _href (url ApplicationPage.Home); _class "flex items-center gap-3 text-base font-semibold text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; span { _class "text-xl text-[var(--fve-brand-text)]"; "L" }; "Ledger" })
        let nav = SideNav.create navId (if settingsKey.IsSome then "Settings" else "Ledger") header groups |> SideNav.withWidth SideNavWidth.Standard |> SideNav.withoutHeader
        let currentNav =
            match page with
            | ApplicationPage.Account _ | ApplicationPage.CreateAccount | ApplicationPage.EditAccount _ | ApplicationPage.DeleteAccount _ -> url ApplicationPage.Accounts
            | ApplicationPage.Transaction _ -> url ApplicationPage.Transactions
            | _ -> current
        // Profile is a footer destination, not an item in the primary navigation.
        let nav = if current= url ApplicationPage.Profile then nav else nav |> SideNav.withCurrent currentNav
        nav
        |> SideNav.withContextLayout SideNavRegionLayout.Flush
        |> SideNav.withContext (match settingsKey with Some _ -> div { _class "grid gap-1 p-4"; strong { _class "text-sm"; query.workspace.organization.name }; span { _class "text-xs text-[var(--fve-muted-text)]"; "Organization settings" } } | None -> workspaceSelector navId (current.Split('?')[0]) query)
        |> SideNav.withFooterLayout SideNavRegionLayout.Flush
        |> SideNav.withFooter (div {
            if settingsKey.IsNone then footerLink current (url ApplicationPage.Settings) "Settings" (icon "M9.594 3.94c.09-.542.56-.94 1.11-.94h2.592c.55 0 1.02.398 1.11.94l.213 1.281 1.94 1.12 1.217-.456 1.37.49 1.296 2.247-.26 1.431-1.003.827v2.24l1.003.827.26 1.43-1.296 2.247-1.37.491-1.217-.456-1.94 1.12-.213 1.281-1.11.94h-2.592l-1.11-.94-.213-1.281-1.94-1.12-1.217.456-1.369-.49-1.296-2.247.26-1.43 1.003-.827v-2.24l-1.003-.827-.26-1.43 1.296-2.247 1.37-.491 1.216.456 1.94-1.12.213-1.281ZM15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z")
            div { if settingsKey.IsNone then _class "border-t border-[var(--fve-border)]"
                  footerLink current (url ApplicationPage.Profile) "Andrew Meier" (icon "M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.5 20.12a7.5 7.5 0 0 1 15 0A17.93 17.93 0 0 1 12 21.75c-2.676 0-5.216-.584-7.5-1.63Z") }
        }) |> SideNav.render id
    let private frame previewId theme (name:string) (navigation:string -> HtmlElement) (bar:PageTopBarConfig) (title:string) (description:string) (actions:HtmlElement) collection (content:HtmlElement) =
        let preview = previewId<>""
        let bounded = preview || collection
        let localId id = if preview then previewId+"-"+id else id
        let body = div {
            _class (if collection then "flex h-full min-h-0 flex-col" else "min-w-0")
            div {
                _class "mx-auto w-full max-w-7xl shrink-0"
                let heading = PageHeader.create title |> PageHeader.withActions actions
                (if name="Ledger" then heading else heading |> PageHeader.withSubtitle description) |> PageHeader.render
            }
            div {
                _class (if collection then "mx-auto flex min-h-0 w-full max-w-7xl flex-1 flex-col px-4 sm:px-6 lg:px-8"
                        elif name="Ledger API" then "min-w-0"
                        elif name="Financial specification" then "grid min-w-0 grid-cols-1 gap-8 px-4 pb-8 sm:px-6 lg:px-8"
                        else "mx-auto grid min-w-0 max-w-7xl gap-8 px-4 pb-8 sm:px-6 lg:px-8")
                content
            }
        }
        div {
            _attr("data-template-shell", "true")
            _class ((theme |> ComponentsTheme.withDensity Density.Compact |> ComponentsTheme.withControlSize ControlSize.Small |> ComponentsTheme.className)+" @container/fve-shell flex flex-col bg-[var(--fve-background)] text-[var(--fve-text)] "+(if preview then "h-full min-h-0" elif collection then "h-[calc(100dvh-var(--example-chrome-height,0px))] min-h-0" else "min-h-[calc(100dvh-var(--example-chrome-height,0px))]"))
            if preview then _style "--example-chrome-height:0px"
            div { _class (if bounded then "shrink-0" else "sticky top-[var(--example-chrome-height,0px)] z-30 shrink-0"); bar |> PageTopBar.render }
            details { _class "shrink-0 border-b border-[var(--fve-border)] open:max-h-[40dvh] open:overflow-y-auto @3xl/fve-shell:hidden"; summary { _class "cursor-pointer px-4 py-3 text-sm font-semibold focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; name+" navigation" }; navigation (localId "template-mobile-navigation") }
            div {
                _class ("flex w-full items-stretch "+(if bounded then "min-h-0 flex-1 overflow-hidden" else "min-h-[calc(100dvh-var(--example-chrome-height,0px)-var(--fve-shell-bar-min-height))]"))
                aside { _class ("hidden w-60 shrink-0 @3xl/fve-shell:block "+(if bounded then "h-full overflow-y-auto" else "sticky top-[calc(var(--example-chrome-height,0px)+var(--fve-shell-bar-min-height))] h-[calc(100dvh-var(--example-chrome-height,0px)-var(--fve-shell-bar-min-height))]")); navigation (localId "template-desktop-navigation") }
                if preview then
                    Html.section { _ariaLabel title; _class (if collection then "min-h-0 min-w-0 flex-1 overflow-hidden outline-none" else "min-w-0 flex-1 overflow-y-auto outline-none"); body }
                else
                    main { _id "main-content"; _tabindex -1; _class (if collection then "min-h-0 min-w-0 flex-1 overflow-hidden outline-none" else "min-w-0 flex-1 outline-none"); body }
            }
        }
    let shellWithNavigation (name:string) (current:string) sections overview (title:string) (description:string) (actions:HtmlElement) (content:HtmlElement) =
        let navigation navId =
            SideNav.create navId name (SideNavHeader.create name) sections
            |> SideNav.withWidth SideNavWidth.Standard |> SideNav.withoutHeader |> SideNav.withCurrent current |> SideNav.render id
        let bar =
            PageTopBar.create ()
            |> PageTopBar.withBrand (strong { _class "truncate text-base font-semibold"; if name="Financial specification" then "Ledger specification" else name })
            |> PageTopBar.withContent (Breadcrumbs.create "template-document-breadcrumbs" "Breadcrumb" [
                BreadcrumbItem.create overview "Overview"
                if current<>overview then BreadcrumbItem.create current title ] |> Breadcrumbs.render id)
        frame "" theme name navigation bar title description actions false content
    let shell (name:string) (current:string) (groups:(string*(string*string) list) list) title description actions content =
        let sections = groups |> List.map (fun (label,items) ->
            let items = items |> List.map (fun (href,label) -> SideNavItem.create href label)
            if label="" then SideNavSection.ungrouped items else SideNavSection.group label items)
        shellWithNavigation name current sections (groups.Head |> snd |> List.head |> fst) title description actions content
    let applicationShell page current (query:Query) (title:string) (actions:HtmlElement) (content:HtmlElement) =
        let url = applicationHref query
        let settingsKey = match page with ApplicationPage.Settings -> Some "organizations" | ApplicationPage.SettingsSection key -> Some key | _ -> None
        let current = if page=ApplicationPage.Settings then url (ApplicationPage.SettingsSection "organizations") else current
        let crumbs =
            [ BreadcrumbItem.create (url ApplicationPage.Home) "Home"
              if settingsKey.IsSome then BreadcrumbItem.create (url ApplicationPage.Settings) "Settings"
              elif page<>ApplicationPage.Home && page<>ApplicationPage.Profile then BreadcrumbItem.create (url ApplicationPage.Accounts) "Accounting"
              if page<>ApplicationPage.Home then BreadcrumbItem.create current title ]
        let brand =
            match settingsKey with
            | Some _ -> link (url ApplicationPage.Home) "← Ledger"
            | None -> a { _href (url ApplicationPage.Home); _class "flex items-center gap-3 whitespace-nowrap text-base font-semibold"; span { _class "text-xl text-[var(--fve-brand-text)]"; "L" }; "Ledger" }
        let bar = PageTopBar.create () |> PageTopBar.withBrand brand |> PageTopBar.withContent (Breadcrumbs.create (elementId query "template-breadcrumbs") "Breadcrumb" crumbs |> Breadcrumbs.render id)
        let collection = page=ApplicationPage.Accounts || page=ApplicationPage.Transactions
        let body = div {
            _class (if collection then "flex min-h-0 flex-1 flex-col gap-4" else "grid gap-6")
            if query.workspace.environment.sandbox then Notice.create (elementId query "template-sandbox") ("Sandbox · "+query.workspace.environment.name) (p { "This template uses seeded records. No bank synchronization or money movement is performed." }) |> Notice.withColor NoticeColor.Warning |> Notice.render
            content
        }
        frame query.previewId theme "Ledger" (fun navId -> appNavigation navId page current query settingsKey) bar title (query.workspace.organization.name+" · "+query.workspace.environment.name+" · "+query.workspace.ledger.name) actions collection body

    /// Complete standalone host document; no catalog-only viewer chrome is required.
    let document (title:string) (content:HtmlElement) =
        html {
            _lang "en"
            head {
                meta { _charset "utf-8" }; meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
                Html.title { title }; Html.link { _rel "stylesheet"; _href "/css/output.css" }
                CodeBlock.assets (Some "/css/prism-tomorrow.1.29.0.min.css") ["/scripts/prism.1.29.0.min.js";"/scripts/prism-fsharp.1.29.0.min.js";"/scripts/prism-sql.1.29.0.min.js";"/scripts/prism-bash.1.29.0.min.js";"/scripts/prism-json.1.29.0.min.js"]
                Mermaid.assets "/scripts/mermaid.11.16.0.min.js"
                script { _type "module"; _src "/scripts/datastar.1.0.2.js" }
                script { raw "const applyAppearance = () => { const mode = localStorage.getItem('financial-example-appearance') || 'system'; const dark = mode == 'dark' || (mode == 'system' && matchMedia('(prefers-color-scheme: dark)').matches); document.documentElement.classList.toggle('dark', dark); document.documentElement.style.colorScheme = dark ? 'dark' : 'light'; document.documentElement.dataset.colorMode = mode; window.dispatchEvent(new CustomEvent('financial-example-color-mode', {detail: mode})); }; applyAppearance(); matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => { if ((localStorage.getItem('financial-example-appearance') || 'system') == 'system') applyAppearance(); });" }
            }
            body {
                _class "m-0 bg-[var(--fve-background)] font-sans text-sm text-[var(--fve-text)] antialiased"
                _data("on:financial-example-color-mode__window", "const dark = evt.detail == 'dark' || (evt.detail == 'system' && matchMedia('(prefers-color-scheme: dark)').matches); document.documentElement.classList.toggle('dark', dark); document.documentElement.style.colorScheme = dark ? 'dark' : 'light'; document.documentElement.dataset.colorMode = evt.detail")
                content
            }
        }
