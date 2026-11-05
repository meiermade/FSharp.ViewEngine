namespace Example.Server

open FSharp.ViewEngine
open Example.Components
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain

/// Ordinary consumer-owned markup. The same installed controls can be used by a real app or its Spec.
module Layout =
    let theme = ComponentsTheme.sky

    let link (href: string) (label: string) =
        a {
            _href href

            _class
                "inline-flex min-h-8 shrink-0 items-center whitespace-nowrap rounded-[var(--fve-radius-control)] px-3 py-1.5 text-sm font-medium text-[var(--fve-brand-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

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
        section {
            _ariaLabel title
            _class "grid min-w-0 grid-cols-1 gap-4"
            fveSectionHeader { _label title }
            content
        }

    let prose (content: HtmlElement) =
        div {
            _class
                "prose max-w-none text-[var(--fve-text)] dark:prose-invert [&_p:first-child]:mt-0 [&_p:last-child]:mb-0 [&_ul:first-child]:mt-0 [&_ul:last-child]:mb-0"

            content
        }

    let icon (data: string) =
        svg {
            _viewBox "0 0 24 24"
            _fill "none"
            _stroke "currentColor"
            _strokeWidth "1.5"
            _class "size-5 shrink-0"
            _ariaHidden true

            path {
                _strokeLinecap "round"
                _strokeLinejoin "round"
                _d data
            }
        }

    let private settingsIcon =
        icon
            "M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.325.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 0 1 1.37.49l1.296 2.247a1.125 1.125 0 0 1-.26 1.431l-1.003.827c-.293.241-.438.613-.43.992a7.723 7.723 0 0 1 0 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.955.26 1.43l-1.298 2.247a1.125 1.125 0 0 1-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.47 6.47 0 0 1-.22.128c-.331.183-.581.495-.644.869l-.213 1.281c-.09.543-.56.94-1.11.94h-2.594c-.55 0-1.019-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 0 1-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 0 1-1.369-.49l-1.297-2.247a1.125 1.125 0 0 1 .26-1.431l1.004-.827c.292-.24.437-.613.43-.991a6.932 6.932 0 0 1 0-.255c.007-.38-.138-.751-.43-.992l-1.004-.827a1.125 1.125 0 0 1-.26-1.43l1.297-2.247a1.125 1.125 0 0 1 1.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.086.22-.128.332-.183.582-.495.644-.869l.214-1.28Z M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z"

    let private organizationIcon =
        icon "M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3.375c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21M3 3h12m-.75 4.5H21m-3.75 3.75h.008v.008h-.008v-.008Zm0 3h.008v.008h-.008v-.008Zm0 3h.008v.008h-.008v-.008Z"

    let private documentationTheme () =
        let signal = "_documentation_mode"

        div {
            _attr ("data-signals__ifmissing", $"{{{signal}: window.fveColorMode.current()}}")
            _dataInit $"${signal} = window.fveColorMode.current()"
            _dataOn ("fve-color-mode__window", $"${signal} = window.fveColorMode.current()")

            fveDropdownMenu {
                _id "template-document-theme"

                fveDropdownMenuTrigger {
                    fveButton {
                        _variant ButtonVariant.Ghost
                        _iconOnly true
                        _ariaLabel "Choose color theme"
                        _title "Choose color theme"
                        _class "shadow-none"

                        span {
                            _class "dark:hidden"
                            icon "M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z"
                        }
                        span {
                            _class "hidden dark:block"
                            icon "M21.752 15.002A9.718 9.718 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 2.25 12c0 5.385 4.365 9.75 9.75 9.75a9.753 9.753 0 0 0 9.752-6.748Z"
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

    let private chevron = icon "m9 6 6 6-6 6"
    // Safari pointer activation needs explicit trigger focus for native dialog restoration.
    let private navigationTrigger dialogId =
        fveButton {
            _variant ButtonVariant.Ghost
            _id (dialogId + "-trigger")
            _ariaLabel "Open navigation"
            _ariaControls dialogId
            _ariaHaspopup "dialog"
            _dataOn ("click", $"el.focus(); document.getElementById('{dialogId}').showModal()")
            icon "M3.75 6.75h16.5M3.75 12h16.5M3.75 17.25h16.5"
        }

    let private navigationClose dialogId =
        fveButton {
            _variant ButtonVariant.Ghost
            _ariaLabel "Close navigation"
            _autofocus true
            _dataOn ("click", $"document.getElementById('{dialogId}').close()")
            icon "m6 6 12 12M6 18 18 6"
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
                "fixed inset-y-0 left-0 m-0 h-dvh max-h-none w-[min(18rem,calc(100vw-3rem))] max-w-none border-0 bg-[var(--fve-background)] p-0 text-[var(--fve-text)] shadow-xl backdrop:bg-black/50 backdrop:backdrop-blur-[2px] [&>aside]:w-full"

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
                                "flex min-h-12 items-center justify-between gap-3 rounded-lg border border-[var(--fve-border)] p-3 hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

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
                        _data ("on:click", $"document.getElementById('{navId}-organizations').close()")
                        "Cancel"
                    }
                }

                _data ("fve-navigation-dialog", "true")
                _data ("on:close__capture", $"document.getElementById('{navId}-workspace-trigger')?.focus()")
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
                                "flex min-h-12 items-center justify-between gap-3 rounded-lg border border-[var(--fve-border)] p-3 hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

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
                        _data ("on:click", $"document.getElementById('{navId}-environments').close()")
                        "Cancel"
                    }
                }

                _data ("fve-navigation-dialog", "true")
                _data ("on:close__capture", $"document.getElementById('{navId}-workspace-trigger')?.focus()")
            }

        div {
            _class "w-full"

            fveDropdownMenu {
                _id (navId + "-workspace")
                _ariaLabel ("Open workspace menu: " + workspace.ledger.name)

                fveDropdownMenuTrigger {
                    _class
                        "[&_svg]:size-[var(--fve-control-icon-size,1rem)] [&_svg]:shrink-0 inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] ring-1 ring-inset ring-[var(--fve-border)] bg-transparent px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

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
                                fveDropdownMenuItemTrailing { icon "m4.5 12.75 6 6 9-13.5" }
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
                        icon "M5.25 14.25h13.5m-13.5 0a3 3 0 0 1-3-3m3 3a3 3 0 1 0 0 6h13.5a3 3 0 1 0 0-6m-16.5-3a3 3 0 0 1 3-3h13.5a3 3 0 0 1 3 3m-19.5 0a4.5 4.5 0 0 1 .9-2.7L5.737 5.1a3.375 3.375 0 0 1 2.7-1.35h7.126c1.062 0 2.062.5 2.7 1.35l2.587 3.45a4.5 4.5 0 0 1 .9 2.7m0 0a3 3 0 0 1-3 3m0 3h.008v.008h-.008v-.008Zm0-6h.008v.008h-.008v-.008Zm-3 6h.008v.008h-.008v-.008Zm0-6h.008v.008h-.008v-.008Z"
                    }
                    fveDropdownMenuItemTrailing { chevron }
                }
            }
            |> fun menu ->
                fveSideNavRow {
                    _label workspace.ledger.name

                    fveSideNavLeading {
                        icon
                            "M3.75 6A2.25 2.25 0 0 1 6 3.75h2.25A2.25 2.25 0 0 1 10.5 6v2.25a2.25 2.25 0 0 1-2.25 2.25H6a2.25 2.25 0 0 1-2.25-2.25V6ZM13.5 6a2.25 2.25 0 0 1 2.25-2.25H18a2.25 2.25 0 0 1 2.25 2.25v2.25a2.25 2.25 0 0 1-2.25 2.25h-2.25a2.25 2.25 0 0 1-2.25-2.25V6ZM3.75 15.75A2.25 2.25 0 0 1 6 13.5h2.25a2.25 2.25 0 0 1 2.25 2.25V18a2.25 2.25 0 0 1-2.25 2.25H6a2.25 2.25 0 0 1-2.25-2.25v-2.25ZM13.5 15.75a2.25 2.25 0 0 1 2.25-2.25H18a2.25 2.25 0 0 1 2.25 2.25V18a2.25 2.25 0 0 1-2.25 2.25h-2.25a2.25 2.25 0 0 1-2.25-2.25v-2.25Z"
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
                [ ApplicationPage.Settings, "Settings", settingsIcon, settings
                  ApplicationPage.Profile, "Profile",
                    icon "M17.982 18.725A7.488 7.488 0 0 0 12 15.75a7.488 7.488 0 0 0-5.982 2.975m11.963 0a9 9 0 1 0-11.963 0m11.963 0A8.966 8.966 0 0 1 12 21a8.966 8.966 0 0 1-5.982-2.275M15 9.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z",
                    personalPage page ] do
                a {
                    _href (applicationHref query destination)
                    _ariaLabel label
                    _title label
                    if selected then _ariaCurrent "true"
                    _class (
                        "inline-flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] [&_svg]:size-4"
                        + if selected then " bg-[var(--fve-surface-hover)]" else ""
                    )
                    image
                }
        }

    let private organizationSelector navId page (query: Query) =
        fveDropdownMenu {
            _id (navId + "-organization")
            _ariaLabel ("Change organization: " + query.workspace.organization.name)

            fveDropdownMenuTrigger {
                _class
                    "[&_svg]:size-[var(--fve-control-icon-size,1rem)] [&_svg]:shrink-0 inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] ring-1 ring-inset ring-[var(--fve-border)] bg-transparent px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

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
                        fveDropdownMenuItemTrailing { icon "m4.5 12.75 6 6 9-13.5" }
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
            else "Ledger"

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

                            _ariaLabel "Ledger"
                            _title "Ledger"
                            _class
                                "flex min-w-0 items-center gap-3 text-base font-semibold text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                            span {
                                _ariaHidden true
                                _class "inline-flex size-5 shrink-0 items-center justify-center text-xl text-[var(--fve-brand-text)]"
                                "L"
                            }

                            span { _class "truncate"; "Ledger" }
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
                            _ariaLabel "Back to Ledger"
                            _title "Back to Ledger"
                            _class "-mx-1.5 inline-flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] [&_svg]:size-4"
                            icon "M9 15 3 9m0 0 6-6M3 9h12a6 6 0 0 1 0 12h-3"
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

                        span {
                            _ariaHidden true

                            _class
                                "grid size-7 shrink-0 place-items-center text-lg font-semibold text-[var(--fve-brand-text)]"

                            "L"
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
                    "min-h-0 min-w-0 flex-1 overflow-y-auto px-4 outline-none sm:px-6 lg:px-10 "
                    + (if heading.IsSome then "py-10" else "py-6")
                )

                div {
                    _class (
                        if wide then
                            "mx-auto grid min-w-0 gap-12"
                        else
                            "mx-auto grid min-w-0 max-w-4xl gap-12"
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
            "relative z-10 hidden w-1 shrink-0 touch-none cursor-col-resize bg-[var(--fve-border)] outline-none hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"

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
                        _class "hidden h-full [&>aside]:w-full lg:block"
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
             else "Ledger")
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
    let documentWithNonce (nonce: string option) canonical embedded (title: string) (content: HtmlElement) =
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
                ColorMode.assetsWithNonce "financial-example-appearance" ColorMode.System nonce

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

                    raw Navigation.enhancement.initialScript
                }

                script {
                    _type "module"
                    _src "/examples/assets/scripts/app.js"
                }
            }

            body {
                _class "m-0 bg-[var(--fve-background)] font-sans text-sm text-[var(--fve-text)] antialiased"

                for attribute in Navigation.bodyAttributes do
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
        documentWithNonce None "/examples/application" false title content
