namespace Example.Components.Views

open FSharp.ViewEngine
open Example.Components
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain

/// Ordinary consumer-owned markup. The same installed controls can be used by a real app or its Spec.
module Layout =
    let theme = ComponentsTheme.custom "geldos-theme"

    let link (href: string) (label: string) =
        a {
            _href href

            _class
                "inline-flex min-h-8 shrink-0 items-center whitespace-nowrap rounded-[var(--fve-radius-control)] px-3 py-1.5 text-sm font-medium text-[var(--fve-brand-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

            label
        }

    let inlineLink (href: string) (label: string) =
        a {
            _href href

            _class
                "font-medium text-[var(--fve-brand-text)] hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

            label
        }

    let secondaryLink (href: string) (label: string) =
        fveLink {
            _variant LinkVariant.Outline
            _color Color.Neutral
            _href href
            label
        }

    let primaryLink (href: string) (label: string) =
        a {
            _href href

            _class
                "inline-flex min-h-8 shrink-0 items-center justify-center whitespace-nowrap rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-text)] px-3 py-1.5 text-sm font-medium text-[light-dark(white,#101828)] hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

            label
        }

    let section (title: string) (content: HtmlElement) =
        fveSection { _label title; content }

    let prose (content: HtmlElement) =
        div {
            _class
                "prose max-w-none text-[var(--fve-text)] dark:prose-invert [&_p:first-child]:mt-0 [&_p:last-child]:mb-0 [&_ul:first-child]:mt-0 [&_ul:last-child]:mb-0"

            content
        }

    let icon (name: IconName) = fveIcon { _name name; _size Size.Medium }

    let sectionWithEdit (title:string) (href:string) (content:HtmlElement) =
        fveSection {
            _label title
            fveSectionActions {
                fveLink {
                    _href href; _variant LinkVariant.Outline; _color Color.Neutral
                    icon IconName.PencilSquare
                    "Edit"
                }
            }
            content
        }

    let private organizationIcon =
        icon IconName.BuildingOffice2

    let documentationSearch label (groups: (string * (string * string) list) list) =
        let id = "template-document-search"
        div {
            _id (id + "-shell")
            _class "whitespace-normal"
            _dataOn (
                "keydown__window",
                $"if ((evt.metaKey || evt.ctrlKey) && evt.key.toLowerCase() === 'k') {{ evt.preventDefault(); const dialog = document.getElementById('{id}-dialog'); const input = document.getElementById('{id}-input'); input.value = ''; input.dispatchEvent(new Event('input', {{ bubbles: true }})); if (!dialog.open) document.getElementById('{id}-dialog-trigger').click(); input.focus(); }}"
            )
            button {
                for attribute in Command.triggerAttributes id do attribute
                _type "button"
                _ariaLabel label
                _title label
                _class "flex h-8 cursor-pointer items-center gap-2 rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-2.5 text-sm text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] max-sm:w-8 max-sm:justify-center max-sm:p-0 [&>svg]:size-4"
                fveIcon { _name IconName.MagnifyingGlass; _size Size.Small }
                kbd {
                    _class "hidden rounded border border-[var(--fve-border)] px-1 py-px font-mono text-xs sm:inline"
                    _dataText "navigator.userAgent.includes('Mac') ? '⌘K' : 'Ctrl+K'"
                    "Ctrl+K"
                }
            }
            fveCommandDialog {
                _id id
                _ariaLabel label
                _placeholder (label + "…")
                for group, entries in groups do
                    fveCommandGroup {
                        _label (if group = "" then "Overview" else group)
                        for href, title in entries do
                            fveCommandItem {
                                _href href
                                _keywords [ group; href.Replace('/', ' ').Replace('-', ' ') ]
                                title
                            }
                    }
            }
        }

    let private documentationTheme () =
        let signal = "_documentation_mode"

        div {
            _dataSignals (signal, ["ifmissing"], "window.fveColorMode.current()")
            _dataInit $"${signal} = window.fveColorMode.current()"
            _dataOn ("fve-color-mode", ["window"], $"${signal} = window.fveColorMode.current()")

            fveDropdownMenu {
                _id "template-document-theme"

                fveDropdownMenuTrigger {
                    fveButton {
                        _variant ButtonVariant.Ghost
                        _ariaLabel "Choose color theme"
                        _title "Choose color theme"
                        _class "shadow-none"

                        span {
                            _class "inline-flex dark:hidden"
                            fveIcon { _name IconName.Sun; _variant IconVariant.Mini }
                        }
                        span {
                            _class "hidden dark:inline-flex"
                            fveIcon { _name IconName.Moon; _variant IconVariant.Mini }
                        }
                    }
                }

                for mode, caption in [ ColorMode.System, "System"; ColorMode.Light, "Light"; ColorMode.Dark, "Dark" ] do
                    let value = ColorMode.value mode
                    let initial = mode = ColorMode.System
                    fveDropdownMenuItem {
                        _role "menuitemradio"
                        _ariaChecked initial
                        _dataAttr ("aria-checked", $"${signal} == '{value}' ? 'true' : 'false'")
                        _dataOn ("click", $"window.fveColorMode.set('{value}')")
                        caption
                    }
            }
        }

    let private chevron = icon IconName.ChevronRight
    // Safari pointer activation needs explicit trigger focus for native dialog restoration.
    let private navigationTrigger dialogId =
        fveButton {
            _variant ButtonVariant.Ghost
            _id (dialogId + "-trigger")
            _ariaLabel "Open navigation"
            _ariaControls dialogId
            _ariaHaspopup "dialog"
            _dataOn ("click", $"el.focus(); document.getElementById('{dialogId}').showModal()")
            fveIcon { _name IconName.Bars3; _variant IconVariant.Mini }
        }

    let private navigationClose dialogId =
        fveButton {
            _variant ButtonVariant.Ghost
            _ariaLabel "Close navigation"
            _autofocus true
            _dataOn ("click", $"document.getElementById('{dialogId}').close()")
            fveIcon { _name IconName.XMark; _variant IconVariant.Mini }
        }

    let private navigationDrawer dialogId label (content: HtmlElement) =
        dialog {
            _id dialogId
            _ariaLabel (label + " navigation")
            _ariaModal true
            _data ("fve-navigation-dialog", "true")

            _dataOn (
                "keydown",
                "if (evt.key === 'Tab') { const controls = [...el.querySelectorAll('a[href], button, input, select, textarea, summary, [tabindex]')].filter(node => node.tabIndex >= 0 && !node.disabled && node.getClientRects().length); const first = controls[0], last = controls.at(-1); if (evt.shiftKey && document.activeElement === first) { evt.preventDefault(); last?.focus(); } else if (!evt.shiftKey && document.activeElement === last) { evt.preventDefault(); first?.focus(); } }"
            )

            _dataOn (
                "click",
                "if (evt.target === el) { const bounds = el.getBoundingClientRect(); if (evt.clientX < bounds.left || evt.clientX >= bounds.right || evt.clientY < bounds.top || evt.clientY >= bounds.bottom) el.close(); }"
            )

            _dataOn (
                "resize__window",
                $"if (el.open && !document.getElementById('{dialogId}-trigger')?.querySelector('button')?.getClientRects().length) el.close()"
            )

            _class
                "fixed inset-y-0 left-0 m-0 h-dvh max-h-none w-[min(18rem,calc(100vw-3rem))] max-w-none border-0 bg-[var(--fve-background)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-black/50 backdrop:backdrop-blur-[2px] [&>aside]:w-full [&>fve-side-nav]:w-full"

            content
        }

    let private workspaceSelector navId current (query: Query) =
        let workspace = query.workspace

        let destination (selected: Workspace) =
            workspaceUrl current selected
            + (if query.specification then
                   querySuffix
                       [ yield "specState", query.specState
                         if query.resource <> "" then
                             yield "resource", query.resource
                         if query.appMode then
                             yield "appMode", "1"
                         if query.fixture then
                             yield "fveFixture", "product" ]
               elif query.embedded then
                   "&embedded=1"
               else
                   "")

        let organizationDialog =
            fveDialog {
                _id (navId + "-organizations")
                _label "Change organization"

                div {
                    _class "grid gap-3"

                    for organization in organizations do
                        let selected =
                            workspaceFromStrings
                                organization.id
                                "production"
                                (if organization.id = workspace.organization.id then
                                     workspace.ledger.id
                                 else
                                     "")

                        a {
                            _href (destination selected)

                            _class
                                "flex min-h-12 items-center justify-between gap-3 rounded-lg border border-[var(--fve-border)] p-3 [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                            span {
                                strong {
                                    _class "block text-sm"
                                    organization.name
                                }

                                span {
                                    _class "text-xs text-[var(--fve-muted-text)]"
                                    organization.role + " · " + selected.ledger.name
                                }
                            }

                            if organization.id = workspace.organization.id then
                                fveBadge { "Current" }
                        }

                    link (applicationHref query ApplicationPage.ProfileOrganizations) "Manage organizations"
                }

                _description "Choose an organization."

                fveDialogFooter {
                    fveButton {
                        _dataOn ("click", $"document.getElementById('{navId}-organizations').close()")
                        "Cancel"
                    }
                }

                _data ("fve-navigation-dialog", "true")
                _dataOn ("close", ["capture"], $"document.getElementById('{navId}-workspace-trigger')?.focus()")
            }

        let environmentDialog =
            fveDialog {
                _id (navId + "-environments")
                _label "Change environment"

                div {
                    _class "grid gap-3"

                    for environment in environments do
                        a {
                            _href (
                                destination
                                    { workspace with
                                        environment = environment }
                            )

                            _class
                                "flex min-h-12 items-center justify-between gap-3 rounded-lg border border-[var(--fve-border)] p-3 [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                            span {
                                strong {
                                    _class "block text-sm"
                                    environment.name
                                }

                                span {
                                    _class "text-xs text-[var(--fve-muted-text)]"
                                    if environment.sandbox then "Sandbox" else "Production"
                                }
                            }

                            if environment.id = workspace.environment.id then
                                fveBadge { "Current" }
                        }

                    link (applicationHref query (ApplicationPage.SettingsSection "environments")) "Manage environments"
                }

                _description "Choose an environment."

                fveDialogFooter {
                    fveButton {
                        _dataOn ("click", $"document.getElementById('{navId}-environments').close()")
                        "Cancel"
                    }
                }

                _data ("fve-navigation-dialog", "true")
                _dataOn ("close", ["capture"], $"document.getElementById('{navId}-workspace-trigger')?.focus()")
            }

        div {
            _class "w-full"

            fveDropdownMenu {
                _id (navId + "-workspace")
                _ariaLabel ("Open workspace menu: " + workspace.ledger.name)

                fveDropdownMenuTrigger {
                    _class
                        "[&_svg]:size-[var(--fve-control-icon-size,1rem)] [&_svg]:shrink-0 inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] ring-1 ring-inset ring-[var(--fve-border)] bg-transparent px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                    fun (triggerAttributes: HtmlAttribute list) ->
                        button {
                            for attribute in triggerAttributes do
                                attribute

                            "Open workspace menu: " + workspace.ledger.name
                        }

                }

                _alignment DropdownMenuAlignment.Start

                fveDropdownMenuGroup {
                    _label "Ledger"

                    for ledger in
                        ledgers
                        |> List.filter (fun ledger -> ledger.organizationId = workspace.organization.id) do
                        fveDropdownMenuItem {
                            _href (destination { workspace with ledger = ledger })
                            ledger.name

                            if ledger.id = workspace.ledger.id then
                                fveDropdownMenuItemTrailing { icon IconName.Check }
                        }
                }

                fveDropdownMenuSeparator { () }

                fveDropdownMenuItem {
                    _dataOn ("click", $"document.getElementById('{navId}-organizations').showModal()")
                    "Organization"
                    _description workspace.organization.name
                    fveDropdownMenuItemLeading { organizationIcon }
                    fveDropdownMenuItemTrailing { chevron }
                }

                fveDropdownMenuItem {
                    _dataOn ("click", $"document.getElementById('{navId}-environments').showModal()")
                    "Environment"
                    _description workspace.environment.name
                    fveDropdownMenuItemLeading {
                        icon IconName.ServerStack
                    }
                    fveDropdownMenuItemTrailing { chevron }
                }
            }
            |> fun menu ->
                fveSideNavRow {
                    _label workspace.ledger.name

                    fveSideNavLeading {
                        icon IconName.Squares2x2
                    }

                    menu
                }

            organizationDialog
            environmentDialog
        }

    let private personalPage =
        function
        | ApplicationPage.Profile
        | ApplicationPage.ProfileOrganizations -> true
        | _ -> false

    let private applicationUtilities (query: Query) (page: ApplicationPage) =
        let settings =
            match page with
            | ApplicationPage.Settings
            | ApplicationPage.SettingsSection _
            | ApplicationPage.Environment _ -> true
            | _ -> false

        fragment {
            for destination, label, image, selected in
                [ ApplicationPage.Settings, "Settings", (fveIcon { Html._name IconName.Cog6Tooth; _variant IconVariant.Mini }), settings
                  ApplicationPage.Profile, "Profile",
                    (fveIcon { Html._name IconName.UserCircle; _variant IconVariant.Mini }),
                    personalPage page ] do
                fveLink {
                    _href (applicationHref query destination)
                    _variant LinkVariant.Ghost
                    _color Color.Neutral
                    _size Size.Medium
                    _ariaLabel label
                    _title label
                    if selected then
                        _ariaCurrent "true"
                        _class "bg-[var(--fve-surface-hover)]"
                    image
                }
        }

    let private organizationSelector navId page (query: Query) =
        fveDropdownMenu {
            _id (navId + "-organization")
            _ariaLabel ("Change organization: " + query.workspace.organization.name)

            fveDropdownMenuTrigger {
                _class
                    "[&_svg]:size-[var(--fve-control-icon-size,1rem)] [&_svg]:shrink-0 inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] ring-1 ring-inset ring-[var(--fve-border)] bg-transparent px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                fun (triggerAttributes: HtmlAttribute list) ->
                    button {
                        for attribute in triggerAttributes do
                            attribute

                        "Change organization: " + query.workspace.organization.name
                    }

            }

            _alignment DropdownMenuAlignment.Start

            for organization in organizations do
                fveDropdownMenuItem {
                    _href (applicationHref (organizationQuery query organization.id) page)
                    _description organization.role
                    organization.name

                    if organization.id = query.workspace.organization.id then
                        fveDropdownMenuItemTrailing { icon IconName.Check }
                }
        }
        |> fun menu ->
            fveSideNavRow {
                _label query.workspace.organization.name
                fveSideNavLeading { organizationIcon }
                menu
            }

    let private appNavigation navId mobile page (current: string) (query: Query) (settingsKey: string option) =
        let url = applicationHref query
        let personal = personalPage page

        let label =
            if personal then "Profile"
            elif settingsKey.IsSome then "Settings"
            else "Geldos"

        let groups =
            if personal then
                [ fveSideNavItem {
                      _href (url ApplicationPage.Profile)
                      "General"
                  }
                  fveSideNavItem {
                      _href (url ApplicationPage.ProfileOrganizations)
                      "Organizations"
                  } ]
            elif settingsKey.IsSome then
                settingsSections
                |> List.map (fun (key, label) ->
                    fveSideNavItem {
                        _href (url (ApplicationPage.SettingsSection key))
                        label
                    })
            else
                [ fveSideNavItem {
                      _href (url ApplicationPage.Home)
                      "Home"
                  }
                  fveSideNavItem {
                      _href (url ApplicationPage.Accounts)
                      "Accounts"
                  }
                  fveSideNavItem {
                      _href (url ApplicationPage.Transactions)
                      "Transactions"
                  } ]

        let header =
            fveSideNavHeader {
                _label label

                div {
                    _class "flex min-w-0 items-center gap-3"

                    div {
                        _class "min-w-0 flex-1"

                        a {
                            _href (
                                if personal || settingsKey.IsSome then
                                    applicationHref (ledgerReturnQuery query) ApplicationPage.Home
                                else
                                    url ApplicationPage.Home
                            )

                            _ariaLabel "Geldos"
                            _title "Geldos"
                            _class
                                "flex min-w-0 items-center gap-3 text-base font-semibold text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                            img {
                                _src "/examples/assets/images/geldos.svg"
                                _alt ""; _ariaHidden true
                                Html._width "20"; Html._height "20"
                                _class "size-5 shrink-0"
                            }

                            span { _class "truncate"; "Geldos" }
                        }
                    }

                    if mobile then
                        navigationClose (elementId query "template-navigation-dialog")
                }
            }

        let currentNav =
            match page with
            | ApplicationPage.Account _
            | ApplicationPage.CreateAccount
            | ApplicationPage.EditAccount _
            | ApplicationPage.DeleteAccount _ -> url ApplicationPage.Accounts
            | ApplicationPage.Transaction _ -> url ApplicationPage.Transactions
            | ApplicationPage.Environment _ -> url (ApplicationPage.SettingsSection "environments")
            | _ -> current

        fveSideNav {
            _id navId
            _ariaLabel label

            _persistenceKey (
                if personal then "ledger-profile-navigation"
                elif settingsKey.IsSome then "ledger-settings-navigation"
                else "ledger-application-navigation"
            )

            header
            fveSideNavContent { yield! groups }
            Html._width SideNavWidth.Standard
            _value currentNav

            fveSideNavContext {
                if personal || settingsKey.IsSome then
                    div {
                        _class ("flex min-h-10 min-w-0 flex-wrap items-center gap-3 px-4 py-0.5" + if personal then "" else " border-b border-[var(--fve-border)]")

                        a {
                            _href (applicationHref (ledgerReturnQuery query) ApplicationPage.Home)
                            _ariaLabel "Back to Geldos"
                            _title "Back to Geldos"
                            _class "-mx-1.5 inline-flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] [&_svg]:size-4"
                            icon IconName.ArrowUturnLeft
                        }

                        h2 {
                            _class "flex min-w-0 flex-1 basis-36 flex-wrap items-center gap-2 text-base font-semibold text-[var(--fve-text)]"
                            span { label }
                        }
                    }

                if settingsKey.IsSome then
                    organizationSelector navId page query
                elif not personal then
                    workspaceSelector navId (current.Split('?')[0]) query
            }

        }

    let private applicationFrame
        previewId
        framed
        (navigationLabel: string)
        (navigation: string -> bool -> HtmlElement)
        (bar: HtmlElement)
        (title: string)
        (actions: HtmlElement)
        collection
        (content: HtmlElement)
        =
        let preview = previewId <> ""
        let bounded = preview || framed || collection

        let localId id =
            if preview then previewId + "-" + id else id

        let body =
            div {
                _class (
                    if collection then
                        "flex h-full min-h-0 flex-col"
                    else
                        "min-w-0"
                )

                div {
                    _class "w-full shrink-0"

                    fvePageHeader {
                        _label title
                        fvePageHeaderActions { actions }
                    }
                }

                div {
                    _class (
                        if collection then
                            "flex min-h-0 w-full flex-1 flex-col px-4 sm:px-6 lg:px-8"
                        else
                            "grid min-w-0 w-full grid-cols-1 gap-8 px-4 pb-8 sm:px-6 lg:px-8"
                    )

                    content
                }
            }

        div {
            _attr ("data-template-shell", "true")

            _class (
                (theme
                 |> ComponentsTheme.withDensity Density.Compact
                 |> ComponentsTheme.withControlSize Size.Medium
                 |> ComponentsTheme.className)
                + " @container/fve-shell flex bg-[var(--fve-background)] text-[var(--fve-text)] "
                + (if preview || framed then "h-full min-h-0"
                   elif collection then "h-dvh min-h-0"
                   else "min-h-dvh")
            )

            navigationDrawer
                (localId "template-navigation-dialog")
                navigationLabel
                (navigation (localId "template-mobile-navigation") true)

            aside {
                _class (
                    "hidden min-h-0 w-60 shrink-0 @3xl/fve-shell:block "
                    + (if bounded then "h-full" else "sticky top-0 h-dvh")
                )

                navigation (localId "template-desktop-navigation") false
            }

            div {
                _class "flex min-h-0 min-w-0 flex-1 flex-col"

                div {
                    _class (if bounded then "shrink-0" else "sticky top-0 z-30 shrink-0")
                    bar
                }

                noscript {
                    details {
                        _class "shrink-0 border-b border-[var(--fve-border)] @3xl/fve-shell:hidden"

                        summary {
                            _class "cursor-pointer px-4 py-3 text-sm font-semibold"
                            text (navigationLabel + " navigation")
                        }

                        div {
                            _class "h-[min(24rem,calc(100dvh-6rem))]"
                            navigation (localId "template-native-navigation") false
                        }
                    }
                }

                if preview then
                    Html.section {
                        _ariaLabel title
                        _attr ("data-fve-page-scroll", "true")

                        _class (
                            if collection then
                                "min-h-0 min-w-0 flex-1 overflow-hidden outline-none"
                            else
                                "min-h-0 min-w-0 flex-1 overflow-y-auto outline-none"
                        )

                        body
                    }
                else
                    main {
                        _id "main-content"
                        _tabindex -1

                        _class (
                            if collection then
                                "min-h-0 min-w-0 flex-1 overflow-hidden outline-none"
                            elif framed then
                                "min-h-0 min-w-0 flex-1 overflow-y-auto outline-none"
                            else
                                "min-w-0 flex-1 outline-none"
                        )

                        _attr ("data-fve-page-scroll", "true")
                        body
                    }
            }
        }

    let documentationSection id (title: string) (content: HtmlElement) =
        Html.section {
            _id id
            _tabindex -1
            _class "scroll-mt-6 outline-none"

            h2 {
                _class "mb-5 text-xl font-semibold tracking-tight"
                title
            }

            div {
                _class "grid min-w-0 gap-4"
                content
            }
        }

    /// Explicit documentation composition, using the same installed controls as the FVE catalog.
    /// Content, hierarchy, routes, and article/workflow selection belong to this consumer.
    let documentationShell
        (name: string)
        current
        (sections: HtmlElement list)
        (crumbs: HtmlElement list)
        (heading: (string * string) option)
        (actions: HtmlElement)
        (contents: (string * string) list)
        wide
        (content: HtmlElement)
        =
        let navigation navId mobile =
            let header =
                fveSideNavHeader {
                    _label name

                    div {
                        _class "flex min-w-0 items-center gap-2.5"

                        img {
                            _src "/examples/assets/images/geldos.svg"
                            _alt ""
                            Html._width "28"
                            Html._height "28"
                            _class "size-7 shrink-0"
                        }

                        span {
                            _class "min-w-0 flex-1 truncate text-sm font-semibold"
                            name
                        }

                        if mobile then
                            navigationClose "template-navigation-dialog"
                    }
                }

            fveSideNav {
                _id navId
                _ariaLabel "Documentation"
                _persistenceKey (name + "-navigation")
                header
                fveSideNavContent { yield! sections }
                _value current
            }

        let toc mobile =
            nav {
                _ariaLabel "On this page"
                _class (if mobile then "grid gap-2" else "grid gap-2 text-sm")

                for href, label in contents do
                    a {
                        _href ("#" + href)

                        _class
                            "min-w-0 whitespace-normal text-[var(--fve-muted-text)] [overflow-wrap:anywhere] hover:text-[var(--fve-brand-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                        label
                    }
            }

        let bar =
            fvePageTopBar {
                div {
                    _class "flex min-w-0 items-center gap-3"

                    span {
                        _class "lg:hidden"
                        navigationTrigger "template-navigation-dialog"
                    }

                    fveBreadcrumbs {
                        _id "template-document-breadcrumbs"
                        _ariaLabel "Documentation breadcrumb"
                        yield! crumbs
                    }
                }

                fvePageTopBarActions {
                    _class "max-w-full flex-wrap"
                    actions
                    documentationTheme ()
                }
            }

        let mainView =
            main {
                _id "main-content"
                _tabindex -1
                _attr ("data-fve-page-scroll", "true")

                _class (
                    "min-h-0 min-w-0 flex-1 overflow-y-auto px-4 outline-none [overflow-wrap:anywhere] sm:px-6 lg:px-10 "
                    + (if heading.IsSome then "py-10" else "py-6")
                )

                div {
                    _class (
                        if wide then
                            "mx-auto grid min-w-0 grid-cols-1 gap-12"
                        else
                            "mx-auto grid min-w-0 max-w-4xl grid-cols-1 gap-12"
                    )

                    match heading with
                    | Some(title, description) ->
                        Html.section {
                            h1 {
                                _class
                                    "m-0 max-w-3xl text-4xl leading-[1.1] font-semibold tracking-tight [overflow-wrap:anywhere]"

                                title
                            }

                            if description <> "" then
                                p {
                                    _class "mt-4 max-w-3xl text-base leading-7 text-[var(--fve-muted-text)]"
                                    description
                                }
                        }
                    | None -> ()

                    if not (List.isEmpty contents) then
                        details {
                            _class "rounded-lg border border-[var(--fve-border)] p-4 xl:hidden"

                            summary {
                                _class "cursor-pointer text-sm font-semibold"
                                "On this page"
                            }

                            div {
                                _class "mt-3"
                                toc true
                            }
                        }

                    content
                }
            }

        let handle =
            "relative z-10 hidden w-1 shrink-0 touch-none cursor-col-resize bg-[var(--fve-border)] outline-none [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"

        let pageContent =
            div {
                _class "flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden"
                bar

                if List.isEmpty contents then
                    mainView
                else
                    let rail =
                        aside {
                            _class
                                "hidden h-full min-h-0 min-w-0 w-full overflow-x-hidden overflow-y-auto px-4 py-8 xl:block"

                            div {
                                _class
                                    "mb-3 text-xs font-semibold tracking-[0.14em] text-[var(--fve-muted-text)] uppercase"

                                "On this page"
                            }

                            toc false
                        }

                    fveResizable {
                        _id "template-content-panels"
                        _ariaLabel "On this page width"
                        _framed false
                        _minSize 55
                        _maxSize 85
                        _initialSize 80
                        _class "w-full flex-1 max-xl:contents"

                        fveResizablePanel {
                            _class "flex flex-col overflow-hidden max-xl:contents"
                            mainView
                        }

                        fveResizableHandle { _class (handle + " xl:flex") }

                        fveResizablePanel {
                            _class "hidden overflow-hidden xl:flex"
                            rail
                        }
                    }
            }

        div {
            _attr ("data-template-shell", "true")

            _class (
                (theme
                 |> ComponentsTheme.withDensity Density.Compact
                 |> ComponentsTheme.withControlSize Size.Medium
                 |> ComponentsTheme.className)
                + " flex h-dvh min-w-0 overflow-hidden bg-[var(--fve-background)] text-[var(--fve-text)]"
            )

            a {
                _href "#main-content"

                _class
                    "fixed top-2 left-2 z-100 -translate-y-[200%] rounded-md bg-[var(--fve-surface)] px-3 py-2 font-semibold focus:translate-y-0"

                "Skip to main content"
            }

            navigationDrawer "template-navigation-dialog" "Documentation" (navigation "template-mobile-navigation" true)

            noscript {
                details {
                    _class "fixed top-0 left-0 z-50 max-h-dvh overflow-y-auto bg-[var(--fve-background)] lg:hidden"

                    summary {
                        _class "px-4 py-3 text-sm font-semibold"
                        name + " navigation"
                    }

                    navigation "template-native-navigation" false
                }
            }

            fveResizable {
                _id "template-navigation-panels"
                _ariaLabel "Documentation navigation width"
                _framed false
                _minSize 12
                _maxSize 28
                _initialSize 19
                _class "h-full w-full"

                fveResizablePanel {
                    _class "overflow-hidden max-lg:contents"

                    div {
                        _class "hidden h-full [&>aside]:w-full [&>fve-side-nav]:w-full lg:block"
                        navigation "template-desktop-navigation" false
                    }
                }

                fveResizableHandle { _class (handle + " lg:flex") }

                fveResizablePanel {
                    _class "flex overflow-hidden"
                    pageContent
                }
            }
        }

    let shellWithNavigation (name: string) current sections overview (title: string) actions content =
        let crumbs =
            [ fveBreadcrumbItem {
                  _href overview
                  "Overview"
              }
              if current <> overview then
                  fveBreadcrumbItem {
                      _href current
                      title
                  } ]

        documentationShell name current sections crumbs None actions [] true content

    let shell (name: string) (current: string) (groups: (string * (string * string) list) list) title actions content =
        let sections =
            groups
            |> List.collect (fun (label, items) ->
                let items =
                    items
                    |> List.map (fun (href, label) ->
                        fveSideNavItem {
                            _href href
                            label
                        })

                if label = "" then
                    items
                else
                    [ fveSideNavSection {
                          _label label
                          yield! items
                      } ])

        shellWithNavigation name current sections (groups.Head |> snd |> List.head |> fst) title actions content

    let applicationShell page current (query: Query) (title: string) (actions: HtmlElement) (content: HtmlElement) =
        let url = applicationHref query

        let settingsKey =
            match page with
            | ApplicationPage.Settings -> Some "general"
            | ApplicationPage.SettingsSection key -> Some key
            | ApplicationPage.Environment _ -> Some "environments"
            | _ -> None

        let personal = personalPage page

        let current =
            if page = ApplicationPage.Settings then
                url (ApplicationPage.SettingsSection "general")
            else
                current

        let crumbs =
            [ if personal then
                  fveBreadcrumbItem {
                      _href (url ApplicationPage.Profile)
                      "Profile"
                  }
              elif settingsKey.IsSome then
                  fveBreadcrumbItem {
                      _href (url ApplicationPage.Settings)
                      "Settings"
                  }
              else
                  fveBreadcrumbItem {
                      _href (url ApplicationPage.Home)
                      "Home"
                  }
              if not personal && settingsKey.IsNone && page <> ApplicationPage.Home then
                  match page with
                  | ApplicationPage.Account _
                  | ApplicationPage.CreateAccount
                  | ApplicationPage.EditAccount _
                  | ApplicationPage.DeleteAccount _ ->
                      fveBreadcrumbItem {
                          _href (url ApplicationPage.Accounts)
                          "Accounts"
                      }
                  | ApplicationPage.Transaction _ ->
                      fveBreadcrumbItem {
                          _href (url ApplicationPage.Transactions)
                          "Transactions"
                      }
                  | _ -> ()
              match page with
              | ApplicationPage.Environment _ ->
                  fveBreadcrumbItem {
                      _href (url (ApplicationPage.SettingsSection "environments"))
                      "Environments"
                  }
              | _ -> ()
              if page <> ApplicationPage.Home then
                  fveBreadcrumbItem {
                      _href current
                      title
                  } ]

        let bar =
            fvePageTopBar {
                div {
                    _class "flex min-w-0 items-center gap-3"

                    span {
                        _class "@3xl/fve-shell:hidden"
                        navigationTrigger (elementId query "template-navigation-dialog")
                    }

                    div {
                        _class "min-w-0 flex-1 overflow-hidden"

                        fveBreadcrumbs {
                            _id (elementId query "template-breadcrumbs")
                            _ariaLabel "Breadcrumb"
                            yield! crumbs
                        }
                    }
                }

                fvePageTopBarActions {
                    fragment {
                        yield! query.topBarActions

                        applicationUtilities query page
                    }
                }
            }

        let collection =
            page = ApplicationPage.Accounts || page = ApplicationPage.Transactions

        let body =
            div {
                _class (
                    if collection then
                        "flex min-h-0 flex-1 flex-col gap-4"
                    else
                        "grid min-w-0 grid-cols-1 gap-6 [overflow-wrap:anywhere]"
                )

                if not personal && settingsKey.IsNone && query.workspace.environment.sandbox then
                    fveNotice {
                        _id (elementId query "template-sandbox")
                        _label ("Sandbox · " + query.workspace.environment.name)
                        p { "You are viewing a sandbox environment." }
                        _color Color.Warning
                    }

                content
            }

        applicationFrame
            query.previewId
            query.specification
            (if personal then "Profile"
             elif settingsKey.IsSome then "Settings"
             else "Geldos")
            (fun navId mobile -> appNavigation navId mobile page current query settingsKey)
            bar
            title
            actions
            collection
            body

    let navigationRoot embedded (content: HtmlElement) =
        div {
            _id "example-navigation-root"
            _attr ("data-fve-navigation-root", "true")
            _attr ("data-example-viewer", "true")
            _attr ("data-template-embedded", if embedded then "true" else "false")
            content
        }

    let canonicalLink url =
        Html.link {
            _id "example-canonical"
            _rel "canonical"
            _href url
        }

    let documentTitle (title: string) =
        Html.title {
            _id "docs-document-title"
            title
        }

    /// Complete standalone host document; no catalog-only viewer chrome is required.
    let documentWithNonce initialScript (bodyAttributes: HtmlAttribute list) (nonce: string option) canonical embedded (title: string) (content: HtmlElement) =
        html {
            _lang "en"

            match nonce with
            | Some value -> _attr ("data-nonce", value)
            | None -> ()

            head {
                meta { _charset "utf-8" }

                meta {
                    _name "viewport"
                    _content "width=device-width, initial-scale=1"
                }

                documentTitle title
                canonicalLink canonical
                Html.link { _rel "icon"; _type "image/svg+xml"; _href "/examples/assets/images/geldos.svg" }
                ColorMode.assetsWithNonce "financial-example-appearance" ColorMode.System nonce
                SideNavPreferences.assetsWithNonce nonce

                Html.link {
                    _rel "stylesheet"
                    _href "/examples/assets/css/output.css"
                }

                CodeBlock.assetsWithNonce
                    None
                    [ "/examples/assets/scripts/prism.1.29.0.min.js"
                      "/examples/assets/scripts/prism-fsharp.1.29.0.min.js"
                      "/examples/assets/scripts/prism-sql.1.29.0.min.js"
                      "/examples/assets/scripts/prism-bash.1.29.0.min.js"
                      "/examples/assets/scripts/prism-json.1.29.0.min.js" ]
                    nonce

                Mermaid.assetsWithNonce "/examples/assets/scripts/mermaid.11.16.0.min.js" nonce

                script {
                    match nonce with
                    | Some value -> _attr ("nonce", value)
                    | None -> ()

                    raw initialScript
                }

                script {
                    _type "module"
                    _src "/examples/assets/scripts/app.js"
                }
            }

            body {
                _class "m-0 bg-[var(--fve-background)] font-sans text-sm text-[var(--fve-text)] antialiased"

                for attribute in bodyAttributes do
                    attribute

                div {
                    _id "docs-navigation-status"
                    _role "status"
                    _hidden true

                    _class (
                        ComponentsTheme.className theme
                        + " fixed right-4 bottom-4 z-[120] max-w-sm rounded-lg border border-[var(--fve-critical-ring)] bg-[var(--fve-surface)] px-4 py-3 text-sm font-semibold text-[var(--fve-critical-text)] shadow-lg"
                    )
                }

                navigationRoot embedded content
            }
        }

    let document title content =
        documentWithNonce "" [] None "/examples/application" false title content
