namespace Docs.Examples

open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain

/// Ordinary consumer-owned markup. The same installed controls can be used by a real app or its Spec.
module Layout =
    let theme = ComponentsTheme.sky
    let link (href:string) (label:string) =
        a { _href href; _class "inline-flex min-h-8 shrink-0 items-center whitespace-nowrap rounded-[var(--fve-radius-control)] px-3 py-1.5 text-sm font-medium text-[var(--fve-brand-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"; label }
    let secondaryLink (href:string) (label:string) = Button.create (ButtonContent.Text label) |> Button.renderLink href
    let primaryLink (href:string) (label:string) =
        a { _href href; _class "inline-flex min-h-8 shrink-0 items-center justify-center whitespace-nowrap rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-text)] px-3 py-1.5 text-sm font-medium text-[light-dark(white,#101828)] hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"; label }
    let section (title:string) (content:HtmlElement) =
        section { _ariaLabel title; _class "grid min-w-0 grid-cols-1 gap-4"; SectionHeader.create title |> SectionHeader.render; content }
    let prose (content:HtmlElement) = div { _class "prose max-w-none text-[var(--fve-text)] dark:prose-invert [&_p:first-child]:mt-0 [&_p:last-child]:mb-0 [&_ul:first-child]:mt-0 [&_ul:last-child]:mb-0"; content }
    let icon (data:string) =
        svg { _viewBox "0 0 24 24"; _fill "none"; _stroke "currentColor"; _strokeWidth "1.5"; _class "size-5 shrink-0"; _ariaHidden true; path { _strokeLinecap "round"; _strokeLinejoin "round"; _d data } }
    let private chevron = icon "m9 6 6 6-6 6"
    let private footerLink (current:string) (href:string) (label:string) (leading:HtmlElement) =
        SideNavRow.link href label
        |> SideNavRow.withLeading leading
        |> (if current=href then SideNavRow.current else id)
        |> SideNavRow.render id
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
                link (applicationHref query ApplicationPage.ProfileOrganizations) "Manage organizations"
            }) |> Dialog.withDescription "Choose an organization."
            |> Dialog.withFooter (Button.create (ButtonContent.Text "Cancel") |> Button.withAttributes [_data("on:click", $"document.getElementById('{navId}-organizations').close()")] |> Button.render)
            |> Dialog.withAttributes [_data("on:close__capture", $"document.getElementById('{navId}-workspace-trigger').focus()")]
        let environmentDialog =
            Dialog.create (navId+"-environments") "Change environment" (div {
                _class "grid gap-3"
                for environment in environments do
                    a { _href (destination {workspace with environment=environment}); _class "flex min-h-12 items-center justify-between gap-3 rounded-lg border border-[var(--fve-border)] p-3 hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; span { strong { _class "block text-sm"; environment.name }; span { _class "text-xs text-[var(--fve-muted-text)]"; if environment.sandbox then "Sandbox" else "Production" } }; if environment.id=workspace.environment.id then Badge.create "Current" |> Badge.render }
                link (applicationHref query (ApplicationPage.SettingsSection "environments")) "Manage environments"
            }) |> Dialog.withDescription "Choose an environment."
            |> Dialog.withFooter (Button.create (ButtonContent.Text "Cancel") |> Button.withAttributes [_data("on:click", $"document.getElementById('{navId}-environments').close()")] |> Button.render)
            |> Dialog.withAttributes [_data("on:close__capture", $"document.getElementById('{navId}-workspace-trigger').focus()")]
        div {
            _class "w-full"
            DropdownMenu.create (navId+"-workspace") ("Open workspace menu: "+workspace.ledger.name)
            |> DropdownMenu.withAlignment DropdownMenuAlignment.Start
            |> DropdownMenu.withContent [
                DropdownMenuItem.group "Ledger" (ledgers |> List.filter (fun ledger -> ledger.organizationId=workspace.organization.id) |> List.map (fun ledger ->
                    let item = DropdownMenuItem.link (destination {workspace with ledger=ledger}) ledger.name
                    if ledger.id=workspace.ledger.id then item |> DropdownMenuItem.withTrailing (icon "m4.5 12.75 6 6 9-13.5") else item))
                DropdownMenuItem.separator
                DropdownMenuItem.action $"document.getElementById('{navId}-organizations').showModal()" "Organization"
                |> DropdownMenuItem.withDescription workspace.organization.name |> DropdownMenuItem.withTrailing chevron
                DropdownMenuItem.action $"document.getElementById('{navId}-environments').showModal()" "Environment"
                |> DropdownMenuItem.withDescription workspace.environment.name |> DropdownMenuItem.withTrailing chevron ]
            |> fun menu -> SideNavRow.menu menu workspace.ledger.name
            |> SideNavRow.withLeading (icon "M3.75 6A2.25 2.25 0 0 1 6 3.75h2.25A2.25 2.25 0 0 1 10.5 6v2.25a2.25 2.25 0 0 1-2.25 2.25H6a2.25 2.25 0 0 1-2.25-2.25V6ZM13.5 6a2.25 2.25 0 0 1 2.25-2.25H18a2.25 2.25 0 0 1 2.25 2.25v2.25a2.25 2.25 0 0 1-2.25 2.25h-2.25a2.25 2.25 0 0 1-2.25-2.25V6ZM3.75 15.75A2.25 2.25 0 0 1 6 13.5h2.25a2.25 2.25 0 0 1 2.25 2.25V18a2.25 2.25 0 0 1-2.25 2.25H6a2.25 2.25 0 0 1-2.25-2.25v-2.25ZM13.5 15.75a2.25 2.25 0 0 1 2.25-2.25H18a2.25 2.25 0 0 1 2.25 2.25V18a2.25 2.25 0 0 1-2.25 2.25h-2.25a2.25 2.25 0 0 1-2.25-2.25v-2.25Z")
            |> SideNavRow.render id
            organizationDialog |> Dialog.render
            environmentDialog |> Dialog.render
        }
    let private personalPage = function ApplicationPage.Profile | ApplicationPage.ProfileOrganizations -> true | _ -> false
    let private organizationSelector navId page (query:Query) =
        DropdownMenu.create (navId+"-organization") ("Change organization: "+query.workspace.organization.name)
        |> DropdownMenu.withAlignment DropdownMenuAlignment.Start
        |> DropdownMenu.withContent [
            for organization in organizations do
                DropdownMenuItem.link (applicationHref (organizationQuery query organization.id) page) organization.name
                |> DropdownMenuItem.withDescription organization.role
                |> (if organization.id=query.workspace.organization.id then DropdownMenuItem.withTrailing (icon "m4.5 12.75 6 6 9-13.5") else id) ]
        |> fun menu -> SideNavRow.menu menu query.workspace.organization.name
        |> SideNavRow.render id
    let private appNavigation navId page (current:string) (query:Query) (settingsKey:string option) =
        let url = applicationHref query
        let personal = personalPage page
        let label = if personal then "Profile" elif settingsKey.IsSome then "Settings" else "Ledger"
        let groups =
            if personal then [SideNavItem.create (url ApplicationPage.Profile) "Profile";SideNavItem.create (url ApplicationPage.ProfileOrganizations) "Organizations"]
            elif settingsKey.IsSome then settingsSections |> List.map (fun (key,label) -> SideNavItem.create (url (ApplicationPage.SettingsSection key)) label)
            else [SideNavItem.create (url ApplicationPage.Home) "Home"; SideNavSection.create "Accounting" [SideNavItem.create (url ApplicationPage.Accounts) "Accounts";SideNavItem.create (url ApplicationPage.Transactions) "Transactions"]]
        let header = SideNavHeader.create label |> SideNavHeader.withContent (
            if personal || settingsKey.IsSome then link (applicationHref (ledgerReturnQuery query) ApplicationPage.Home) "← Ledger"
            else a { _href (url ApplicationPage.Home); _class "flex items-center gap-3 text-base font-semibold text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; span { _class "text-xl text-[var(--fve-brand-text)]"; "L" }; "Ledger" })
        let currentNav =
            match page with
            | ApplicationPage.Account _ | ApplicationPage.CreateAccount | ApplicationPage.EditAccount _ | ApplicationPage.DeleteAccount _ -> url ApplicationPage.Accounts
            | ApplicationPage.Transaction _ -> url ApplicationPage.Transactions
            | _ -> current
        SideNav.create navId label
        |> SideNav.withPersistenceKey (if personal then "ledger-profile-navigation" elif settingsKey.IsSome then "ledger-settings-navigation" else "ledger-application-navigation")
        |> SideNav.withHeader header
        |> SideNav.withContent (SideNavContent.create groups)
        |> SideNav.withWidth SideNavWidth.Standard
        |> SideNav.withCurrent currentNav
        |> SideNav.withContext [
            if personal then
                div { _class "flex min-w-0 items-center gap-3 p-4"; Avatar.create "Andrew Meier" "AM" |> Avatar.render; div { _class "min-w-0"; strong { _class "block truncate text-sm"; "Andrew Meier" }; span { _class "block truncate text-xs text-[var(--fve-muted-text)]"; "andrew@meiermade.com" } } }
            elif settingsKey.IsSome then organizationSelector navId page query
            else workspaceSelector navId (current.Split('?')[0]) query ]
        |> SideNav.withFooter [
            if not personal then
                if settingsKey.IsNone then
                    yield footerLink current (url ApplicationPage.Settings) "Settings" (icon "M9.594 3.94c.09-.542.56-.94 1.11-.94h2.592c.55 0 1.02.398 1.11.94l.213 1.281 1.94 1.12 1.217-.456 1.37.49 1.296 2.247-.26 1.431-1.003.827v2.24l1.003.827.26 1.43-1.296 2.247-1.37.491-1.217-.456-1.94 1.12-.213 1.281-1.11.94h-2.592l-1.11-.94-.213-1.281-1.94-1.12-1.217.456-1.369-.49-1.296-2.247.26-1.43 1.003-.827v-2.24l-1.003-.827-.26-1.43 1.296-2.247 1.37-.491 1.216.456 1.94-1.12.213-1.281ZM15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z")
                    yield div { _class "border-t border-[var(--fve-border)]" }
                yield footerLink current (url ApplicationPage.Profile) "Andrew Meier" (icon "M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.5 20.12a7.5 7.5 0 0 1 15 0A17.93 17.93 0 0 1 12 21.75c-2.676 0-5.216-.584-7.5-1.63Z")
        ] |> SideNav.render id
    let private applicationFrame previewId (navigationLabel:string) (navigation:string -> HtmlElement) (bar:PageTopBarConfig) (title:string) (actions:HtmlElement) collection (content:HtmlElement) =
        let preview = previewId<>""
        let bounded = preview || collection
        let localId id = if preview then previewId+"-"+id else id
        let body = div {
            _class (if collection then "flex h-full min-h-0 flex-col" else "min-w-0")
            div {
                _class "mx-auto w-full max-w-7xl shrink-0"
                PageHeader.create title |> PageHeader.withActions actions |> PageHeader.render
            }
            div {
                _class (if collection then "mx-auto flex min-h-0 w-full max-w-7xl flex-1 flex-col px-4 sm:px-6 lg:px-8"
                        else "mx-auto grid min-w-0 max-w-7xl gap-8 px-4 pb-8 sm:px-6 lg:px-8")
                content
            }
        }
        div {
            _attr("data-template-shell", "true")
            _class ((theme |> ComponentsTheme.withDensity Density.Compact |> ComponentsTheme.withControlSize ControlSize.Small |> ComponentsTheme.className)+" @container/fve-shell flex bg-[var(--fve-background)] text-[var(--fve-text)] "+(if preview then "h-full min-h-0" elif collection then "h-dvh min-h-0" else "min-h-dvh"))
            aside {
                _class ("hidden min-h-0 w-60 shrink-0 @3xl/fve-shell:block "+(if bounded then "h-full" else "sticky top-0 h-dvh"))
                navigation (localId "template-desktop-navigation")
            }
            div {
                _class "flex min-h-0 min-w-0 flex-1 flex-col"
                div { _class (if bounded then "shrink-0" else "sticky top-0 z-30 shrink-0"); bar |> PageTopBar.render }
                details {
                    _class "shrink-0 border-b border-[var(--fve-border)] @3xl/fve-shell:hidden"
                    summary { _class "cursor-pointer px-4 py-3 text-sm font-semibold focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; text (navigationLabel+" navigation") }
                    div { _class "h-[min(24rem,calc(100dvh-6rem))]"; navigation (localId "template-mobile-navigation") }
                }
                if preview then
                    Html.section { _ariaLabel title; _class (if collection then "min-h-0 min-w-0 flex-1 overflow-hidden outline-none" else "min-h-0 min-w-0 flex-1 overflow-y-auto outline-none"); body }
                else
                    main { _id "main-content"; _tabindex -1; _class (if collection then "min-h-0 min-w-0 flex-1 overflow-hidden outline-none" else "min-w-0 flex-1 outline-none"); body }
            }
        }
    let documentationSection id (title:string) (content:HtmlElement) =
        Html.section {
            _id id; _tabindex -1; _class "scroll-mt-6 outline-none"
            h2 { _class "mb-5 text-xl font-semibold tracking-tight"; title }
            div { _class "grid min-w-0 gap-4"; content }
        }

    /// Explicit documentation composition, using the same installed controls as the FVE catalog.
    /// Content, hierarchy, routes, and article/workflow selection belong to this consumer.
    let documentationShell (name:string) current sections (crumbs:BreadcrumbItem<string> list) (heading:(string*string) option) (actions:HtmlElement) (contents:(string*string) list) wide (content:HtmlElement) =
        let navigation navId mobile =
            let header = SideNavHeader.create name |> SideNavHeader.withContent (div {
                _class "flex min-w-0 items-center gap-2.5"
                span { _ariaHidden true; _class "grid size-7 shrink-0 place-items-center text-lg font-semibold text-[var(--fve-brand-text)]"; "L" }
                span { _class "min-w-0 flex-1 truncate text-sm font-semibold"; name }
                if mobile then
                    Button.create (ButtonContent.Icon ("Close navigation", icon "m6 6 12 12M6 18 18 6"))
                    |> Button.withVariant ButtonVariant.Ghost
                    |> Button.withAttributes [_dataOn("click", "document.getElementById('template-navigation-dialog').close()")] |> Button.render
            })
            SideNav.create navId "Documentation"
            |> SideNav.withPersistenceKey (name + "-navigation")
            |> SideNav.withHeader header
            |> SideNav.withContent (SideNavContent.create sections)
            |> SideNav.withCurrent current
            |> SideNav.render id
        let toc mobile = nav {
            _ariaLabel "On this page"
            _class (if mobile then "grid gap-2" else "grid gap-2 text-sm")
            for href,label in contents do a { _href ("#"+href); _class "min-w-0 whitespace-normal text-[var(--fve-muted-text)] [overflow-wrap:anywhere] hover:text-[var(--fve-brand-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; label }
        }
        let bar = PageTopBar.create ()
                  |> PageTopBar.withContent (div {
                      _class "flex min-w-0 items-center gap-3"
                      span {
                          _class "lg:hidden"
                          Button.create (ButtonContent.Icon ("Open navigation", icon "M3.75 6.75h16.5M3.75 12h16.5M3.75 17.25h16.5"))
                          |> Button.withVariant ButtonVariant.Ghost
                          |> Button.withAttributes [_ariaControls "template-navigation-dialog"; _dataOn("click", "document.getElementById('template-navigation-dialog').showModal()")] |> Button.render
                      }
                      Breadcrumbs.create "template-document-breadcrumbs" "Breadcrumb" crumbs
                      |> Breadcrumbs.withMaxVisibleItems (max 2 crumbs.Length) |> Breadcrumbs.render id
                  })
                  |> PageTopBar.withActions (fragment { actions; ThemeSwitcher.create "template-document-theme" "Choose color theme" |> ThemeSwitcher.render })
        let mainView = main {
            _id "main-content"; _tabindex -1
            _attr("data-fve-page-scroll", "true")
            _class ("min-h-0 min-w-0 flex-1 overflow-y-auto px-4 outline-none sm:px-6 lg:px-10 "+(if heading.IsSome then "py-10" else "py-6"))
            div {
                _class (if wide then "mx-auto grid min-w-0 gap-12" else "mx-auto grid min-w-0 max-w-4xl gap-12")
                match heading with
                | Some (title,description) ->
                    Html.section {
                        h1 { _class "m-0 max-w-3xl text-4xl leading-[1.1] font-semibold tracking-tight [overflow-wrap:anywhere]"; title }
                        if description<>"" then p { _class "mt-4 max-w-3xl text-base leading-7 text-[var(--fve-muted-text)]"; description }
                    }
                | None -> ()
                if not (List.isEmpty contents) then
                    details { _class "rounded-lg border border-[var(--fve-border)] p-4 xl:hidden"; summary { _class "cursor-pointer text-sm font-semibold"; "On this page" }; div { _class "mt-3"; toc true } }
                content
            }
        }
        let handle = "relative z-10 hidden w-1 shrink-0 touch-none cursor-col-resize bg-[var(--fve-border)] outline-none hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
        let pageContent = div {
            _class "flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden"
            bar |> PageTopBar.render
            if List.isEmpty contents then mainView else
                let leading = ResizablePanel.create mainView |> ResizablePanel.withBounds 55 85 |> ResizablePanel.withInitialSize 80
                let rail = aside {
                    _class "hidden h-full min-h-0 min-w-0 w-full overflow-x-hidden overflow-y-auto px-4 py-8 xl:block"
                    div { _class "mb-3 text-xs font-semibold tracking-[0.14em] text-[var(--fve-muted-text)] uppercase"; "On this page" }
                    toc false
                }
                Resizable.create "template-content-panels" "On this page width" leading rail
                |> Resizable.renderWithClasses "flex min-h-0 min-w-0 w-full flex-1 max-xl:contents" "flex min-h-0 min-w-0 shrink-0 flex-col overflow-hidden max-xl:contents" "hidden min-h-0 min-w-0 flex-1 overflow-hidden xl:flex" (handle+" xl:flex")
        }
        div {
            _attr("data-template-shell", "true")
            _class ((theme |> ComponentsTheme.withDensity Density.Compact |> ComponentsTheme.withControlSize ControlSize.Small |> ComponentsTheme.className)+" flex h-dvh min-w-0 overflow-hidden bg-[var(--fve-background)] text-[var(--fve-text)]")
            a { _href "#main-content"; _class "fixed top-2 left-2 z-100 -translate-y-[200%] rounded-md bg-[var(--fve-surface)] px-3 py-2 font-semibold focus:translate-y-0"; "Skip to main content" }
            dialog {
                _id "template-navigation-dialog"; _ariaLabel "Documentation navigation"; _ariaModal true
                _class "fixed inset-y-0 left-0 m-0 h-dvh max-h-none w-[min(18rem,calc(100vw-3rem))] max-w-none border-0 bg-[var(--fve-background)] p-0 shadow-xl backdrop:bg-black/50 [&>aside]:w-full"
                navigation "template-mobile-navigation" true
            }
            noscript {
                details { _class "fixed top-0 left-0 z-50 max-h-dvh overflow-y-auto bg-[var(--fve-background)] lg:hidden"; summary { _class "px-4 py-3 text-sm font-semibold"; name+" navigation" }; navigation "template-native-navigation" false }
            }
            let leading = ResizablePanel.create (div { _class "hidden h-full [&>aside]:w-full lg:block"; navigation "template-desktop-navigation" false }) |> ResizablePanel.withBounds 12 28 |> ResizablePanel.withInitialSize 19
            Resizable.create "template-navigation-panels" "Documentation navigation width" leading pageContent
            |> Resizable.renderWithClasses "flex h-full min-h-0 min-w-0 w-full" "min-h-0 min-w-0 shrink-0 overflow-hidden max-lg:contents" "flex min-h-0 min-w-0 flex-1 overflow-hidden" (handle+" lg:flex")
        }

    let shellWithNavigation (name:string) current sections overview title actions content =
        let crumbs = [BreadcrumbItem.create overview "Overview"; if current<>overview then BreadcrumbItem.create current title]
        documentationShell name current sections crumbs None actions [] true content
    let shell (name:string) (current:string) (groups:(string*(string*string) list) list) title actions content =
        let sections = groups |> List.collect (fun (label,items) ->
            let items = items |> List.map (fun (href,label) -> SideNavItem.create href label)
            if label="" then items else [SideNavSection.create label items])
        shellWithNavigation name current sections (groups.Head |> snd |> List.head |> fst) title actions content
    let applicationShell page current (query:Query) (title:string) (actions:HtmlElement) (content:HtmlElement) =
        let url = applicationHref query
        let settingsKey = match page with ApplicationPage.Settings -> Some "general" | ApplicationPage.SettingsSection key -> Some key | _ -> None
        let personal = personalPage page
        let current = if page=ApplicationPage.Settings then url (ApplicationPage.SettingsSection "general") else current
        let crumbs =
            [ if personal then BreadcrumbItem.create (url ApplicationPage.Profile) "Profile"
              elif settingsKey.IsSome then BreadcrumbItem.create (url ApplicationPage.Settings) "Settings"
              else BreadcrumbItem.create (url ApplicationPage.Home) "Home"
              if not personal && settingsKey.IsNone && page<>ApplicationPage.Home then
                  BreadcrumbItem.unlinked "Accounting"
                  match page with
                  | ApplicationPage.Account _ | ApplicationPage.CreateAccount | ApplicationPage.EditAccount _ | ApplicationPage.DeleteAccount _ -> BreadcrumbItem.create (url ApplicationPage.Accounts) "Accounts"
                  | ApplicationPage.Transaction _ -> BreadcrumbItem.create (url ApplicationPage.Transactions) "Transactions"
                  | _ -> ()
              if page<>ApplicationPage.Home && page<>ApplicationPage.Profile then BreadcrumbItem.create current title ]
        let bar = PageTopBar.create () |> PageTopBar.withContent (Breadcrumbs.create (elementId query "template-breadcrumbs") "Breadcrumb" crumbs |> Breadcrumbs.render id)
                  |> PageTopBar.withActions (fragment { yield! query.topBarActions; ThemeSwitcher.create (elementId query "template-theme") "Choose color theme" |> ThemeSwitcher.render })
        let collection = page=ApplicationPage.Accounts || page=ApplicationPage.Transactions
        let body = div {
            _class (if collection then "flex min-h-0 flex-1 flex-col gap-4" else "grid gap-6")
            if not personal && settingsKey.IsNone && query.workspace.environment.sandbox then Notice.create (elementId query "template-sandbox") ("Sandbox · "+query.workspace.environment.name) (p { "You are viewing a sandbox environment." }) |> Notice.withColor NoticeColor.Warning |> Notice.render
            content
        }
        applicationFrame query.previewId (if personal then "Profile" elif settingsKey.IsSome then "Settings" else "Ledger") (fun navId -> appNavigation navId page current query settingsKey) bar title actions collection body

    let navigationRoot (content:HtmlElement) = div {
        _id "example-navigation-root"
        _attr("data-fve-navigation-root", "true")
        content
    }
    let documentTitle (title:string) = Html.title { _id "docs-document-title"; title }

    /// Complete standalone host document; no catalog-only viewer chrome is required.
    let documentWithNonce (nonce:string option) (title:string) (content:HtmlElement) =
        html {
            _lang "en"
            match nonce with Some value -> _attr("data-nonce", value) | None -> ()
            head {
                meta { _charset "utf-8" }; meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
                documentTitle title
                ThemeSwitcher.assetsWithNonce "financial-example-appearance" ColorMode.System nonce
                Html.link { _rel "stylesheet"; _href "/css/output.css" }
                CodeBlock.assetsWithNonce (Some "/css/prism-tomorrow.1.29.0.min.css") ["/scripts/prism.1.29.0.min.js";"/scripts/prism-fsharp.1.29.0.min.js";"/scripts/prism-sql.1.29.0.min.js";"/scripts/prism-bash.1.29.0.min.js";"/scripts/prism-json.1.29.0.min.js"] nonce
                Mermaid.assetsWithNonce "/scripts/mermaid.11.16.0.min.js" nonce
                script {
                    match nonce with Some value -> _attr("nonce", value) | None -> ()
                    raw Navigation.enhancement.initialScript
                }
                script { _type "module"; _src "/scripts/datastar.1.0.4.js" }
            }
            body {
                _class "m-0 bg-[var(--fve-background)] font-sans text-sm text-[var(--fve-text)] antialiased"
                for attribute in Navigation.bodyAttributes do attribute
                div {
                    _id "docs-navigation-status"; _role "status"; _hidden true
                    _class (ComponentsTheme.className theme + " fixed right-4 bottom-4 z-[120] max-w-sm rounded-lg border border-[var(--fve-critical-ring)] bg-[var(--fve-surface)] px-4 py-3 text-sm font-semibold text-[var(--fve-critical-text)] shadow-lg")
                }
                navigationRoot content
            }
        }

    let document title content = documentWithNonce None title content
