namespace FSharp.ViewEngine.Components.Templates

open FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>app-shell</category>
[<RequireQualifiedAccess>]
type AppShellBreakpoint =
    | Medium
    | Large

/// <category>app-shell</category>
[<RequireQualifiedAccess>]
type AppShellBoundary =
    | Viewport
    | Container

/// <category>app-shell</category>
[<RequireQualifiedAccess>]
type AppShellContentLandmark =
    | Main
    | Region of label:string

/// <category>app-shell</category>
[<NoEquality; NoComparison>]
type AppShellConfig =
    private
        { id:string
          navigation:SideNavConfig<string>
          content:HtmlElement
          theme:ComponentsTheme
          breakpoint:AppShellBreakpoint
          boundary:AppShellBoundary
          contentLandmark:AppShellContentLandmark
          mobileBottomNavigation:BottomNavigationConfig option
          collapsibleNavigation:bool
          attributes:HtmlAttribute list }

/// <category>app-shell</category>
[<RequireQualifiedAccess>]
module AppShell =
    let create id (navigation:HtmlElement) content =
        let navigation = SideNavElements.configuration navigation
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable application-shell ID is required."
        if String.Equals(id, SideNavView.id navigation, StringComparison.Ordinal) then
            invalidArg (nameof id) "Application-shell and side-navigation IDs must be distinct."
        { id = id
          navigation = navigation
          content = content
          theme = ComponentsTheme.sky
          breakpoint = AppShellBreakpoint.Medium
          boundary = AppShellBoundary.Viewport
          contentLandmark = AppShellContentLandmark.Main
          mobileBottomNavigation = None
          collapsibleNavigation = false
          attributes = [] }

    let withTheme theme (config:AppShellConfig) = { config with theme = theme }
    let withBreakpoint breakpoint (config:AppShellConfig) = { config with breakpoint = breakpoint }
    let withBoundary boundary (config:AppShellConfig) = { config with boundary = boundary }
    let withCollapsibleNavigation (config:AppShellConfig) = { config with collapsibleNavigation = true }

    let withMobileBottomNavigation (navigation:HtmlElement) (config:AppShellConfig) =
        let bottomNavigation = BottomNavigation.configuration navigation
        let id = BottomNavigationView.id bottomNavigation
        if String.Equals(id, config.id, StringComparison.Ordinal) || String.Equals(id, SideNavView.id config.navigation, StringComparison.Ordinal) then
            invalidArg (nameof navigation) "Bottom-navigation, application-shell, and side-navigation IDs must be distinct."
        let sideNavDestinations = SideNavView.destinations config.navigation
        let missingDestinations =
            BottomNavigationView.destinations bottomNavigation
            |> List.filter (fun destination -> not (List.contains destination sideNavDestinations))

        if not (List.isEmpty missingDestinations) then
            invalidArg (nameof navigation) "Every bottom-navigation destination must also exist in the side navigation."

        let bottomNavigation =
            match bottomNavigation.current, SideNavView.current config.navigation with
            | None, Some current ->
                if not (BottomNavigationView.destinations bottomNavigation |> List.contains current) then invalidArg (nameof navigation) "The current destination must exist in the bottom navigation."
                { bottomNavigation with current = Some current }
            | _ -> bottomNavigation
        { config with mobileBottomNavigation = Some bottomNavigation }

    /// Use only when an AppShell is embedded inside an existing main landmark, such as a Docs preview.
    let asPreview label (config:AppShellConfig) =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An embedded AppShell needs a landmark label."
        { config with contentLandmark = AppShellContentLandmark.Region label }
    let withAttributes attributes (config:AppShellConfig) = { config with attributes = attributes }

    let private menuIcon = fveIcon { _name IconName.Bars3; _variant IconVariant.Mini }

    let private closeIcon = fveIcon { _name IconName.XMark; _variant IconVariant.Mini }

    let private collapseIcon =
        fveIcon { _name IconName.ChevronLeft; _variant IconVariant.Mini; _class "transition-transform group-data-[collapsed=true]:rotate-180" }

    let private responsiveClasses breakpoint mediumClasses largeClasses =
        match breakpoint with
        | AppShellBreakpoint.Medium -> mediumClasses
        | AppShellBreakpoint.Large -> largeClasses

    let private desktopWidthClass breakpoint width =
        match breakpoint, width with
        | AppShellBreakpoint.Medium, SideNavWidth.Narrow -> "md:w-48"
        | AppShellBreakpoint.Medium, SideNavWidth.Standard -> "md:w-60"
        | AppShellBreakpoint.Medium, SideNavWidth.Wide -> "md:w-64"
        | AppShellBreakpoint.Large, SideNavWidth.Narrow -> "lg:w-48"
        | AppShellBreakpoint.Large, SideNavWidth.Standard -> "lg:w-60"
        | AppShellBreakpoint.Large, SideNavWidth.Wide -> "lg:w-64"

    let render (config:AppShellConfig) =
        let openSignal = $"_app_shell_{ComponentHtml.optionToken config.id}_navigation_open"
        let collapsedSignal = $"_app_shell_{ComponentHtml.optionToken config.id}_navigation_collapsed"
        let collapsedExpression = if config.collapsibleNavigation then Some $"${collapsedSignal}" else None
        let navigationId = SideNavView.id config.navigation
        let triggerId = $"{config.id}-navigation-trigger"
        let navigationExpression = $"document.getElementById({ComponentHtml.javascriptString navigationId})"
        let triggerExpression = $"document.getElementById({ComponentHtml.javascriptString triggerId})"
        let closeAndRestore = $"${openSignal} = false, queueMicrotask(() => {triggerExpression}?.focus())"
        let openAndFocus =
            $"${openSignal} = true; queueMicrotask(() => ({navigationExpression}?.querySelector('[aria-current=page]') ?? {navigationExpression}?.querySelector('button, a[href]'))?.focus())"
        let mobileViewport =
            match config.breakpoint with
            | AppShellBreakpoint.Medium -> "window.matchMedia('(max-width: 767px)').matches"
            | AppShellBreakpoint.Large -> "window.matchMedia('(max-width: 1023px)').matches"
        let mobileNavigationLabel = ComponentHtml.javascriptString (SideNavView.label config.navigation)
        let trapFocus =
            $"{mobileViewport} && ${openSignal} && evt.key == 'Tab' && (() => {{ const items = Array.from({navigationExpression}.querySelectorAll('a[href], button:not([disabled]), [tabindex]:not([tabindex=\"-1\"])')).filter(item => item.getClientRects().length); const first = items[0]; const last = items.at(-1); if (evt.shiftKey && document.activeElement == first) {{ evt.preventDefault(); last?.focus(); }} else if (!evt.shiftKey && document.activeElement == last) {{ evt.preventDefault(); first?.focus(); }} }})()"
        let mobileOnly = responsiveClasses config.breakpoint "md:hidden" "lg:hidden"
        let desktopNavigation = responsiveClasses config.breakpoint "md:static md:visible md:translate-x-0 md:shadow-none" "lg:static lg:visible lg:translate-x-0 lg:shadow-none"
        let expandedNavigationWidth = desktopWidthClass config.breakpoint (SideNavView.width config.navigation)
        let collapsibleNavigationWidth = responsiveClasses config.breakpoint "md:w-[var(--fve-side-nav-width)]" "lg:w-[var(--fve-side-nav-width)]"
        let expandedNavigationWidthValue =
            match SideNavView.width config.navigation with
            | SideNavWidth.Narrow -> "12rem"
            | SideNavWidth.Standard -> "15rem"
            | SideNavWidth.Wide -> "16rem"
        let rootBoundary, overlayPosition, navigationPosition =
            match config.boundary with
            | AppShellBoundary.Viewport -> "h-dvh min-h-[36rem]", "fixed", "fixed"
            | AppShellBoundary.Container -> "h-full min-h-0", "absolute", "absolute"
        let closeControl =
            fveButton {
                _variant ButtonVariant.Ghost
                _ariaLabel "Close navigation"
                _class mobileOnly
                _id $"{config.id}-navigation-close"
                _dataOn ("click", closeAndRestore)
                span { _ariaHidden true; _class "inline-flex shrink-0"; closeIcon }
            }
        let collapseControl =
            if config.collapsibleNavigation then
                button {
                    _type "button"
                    _ariaLabel "Collapse navigation"
                    _title "Collapse navigation"
                    _dataAttr ("aria-label", $"${collapsedSignal} ? 'Expand navigation' : 'Collapse navigation'")
                    _dataAttr ("title", $"${collapsedSignal} ? 'Expand navigation' : 'Collapse navigation'")
                    _dataAttr ("data-collapsed", $"${collapsedSignal} ? 'true' : 'false'")
                    _dataOn ("click", $"${collapsedSignal} = !${collapsedSignal}")
                    _class (ComponentHtml.classes [ ComponentHtml.popupControlClasses; "group hidden size-[var(--fve-control-min-height)] shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)]"; responsiveClasses config.breakpoint "md:inline-flex" "lg:inline-flex" ])
                    collapseIcon
                }
            else empty
        let navigationControls = div { _class "flex items-center gap-1"; collapseControl; closeControl }
        let navigationAttributes = [
            _dataClass ("visible", $"${openSignal}")
            _dataClass ("invisible", $"!${openSignal}")
            _dataClass ("translate-x-0", $"${openSignal}")
            _dataClass ("-translate-x-full", $"!${openSignal}")
            // Read the signal before the media query so dependency tracking survives a desktop-first resize.
            _dataAttr ("role", $"${openSignal} && {mobileViewport} ? 'dialog' : null")
            _dataAttr ("aria-label", $"${openSignal} && {mobileViewport} ? {mobileNavigationLabel} : null")
            _dataAttr ("aria-modal", $"${openSignal} && {mobileViewport} ? 'true' : null")
            _dataOn ("keydown", $"evt.key == 'Escape' && (evt.preventDefault(), {closeAndRestore}); {trapFocus}")
            _dataOn ("click", $"evt.target.closest('a[href]') && (${openSignal} = false)")
            if config.collapsibleNavigation then
                _style $"--fve-side-nav-width: {expandedNavigationWidthValue}"
                _dataAttr ("style", $"${collapsedSignal} ? '--fve-side-nav-width: 4rem' : '--fve-side-nav-width: {expandedNavigationWidthValue}'") ]

        div {
            _id config.id
            _class (ComponentHtml.classes [ ComponentsTheme.className config.theme; "relative flex overflow-hidden bg-[var(--fve-background)] text-[var(--fve-text)]"; rootBoundary ])
            for attribute in ComponentHtml.safeAttributes [ "id"; "class"; "data-signals"; "data-on:" ] config.attributes do attribute
            _dataSignals $"{{{openSignal}: false, {collapsedSignal}: false}}"
            _dataOn ("resize", [ "window" ], $"!{mobileViewport} && (${openSignal} = false)")
            div {
                _attr ("data-fve-app-shell-backdrop", "true")
                _ariaHidden true
                _dataClass ("visible", $"${openSignal}")
                _dataClass ("invisible", $"!${openSignal}")
                _dataClass ("opacity-100", $"${openSignal}")
                _dataClass ("opacity-0", $"!${openSignal}")
                _dataOn ("click", closeAndRestore)
                _class (ComponentHtml.classes [ overlayPosition; "inset-0 z-30 invisible bg-[var(--fve-overlay-backdrop)] opacity-0 transition-opacity motion-reduce:transition-none"; mobileOnly ])
            }
            SideNavView.render
                (ComponentHtml.classes [ navigationPosition; "inset-y-0 left-0 z-40 flex w-[min(20rem,calc(100%-3rem))] -translate-x-full invisible flex-col border-r border-[var(--fve-border)] bg-[var(--fve-background)] text-[var(--fve-text)] shadow-xl transition-[width,transform] motion-reduce:transition-none"; desktopNavigation; if config.collapsibleNavigation then collapsibleNavigationWidth else expandedNavigationWidth ])
                navigationAttributes
                (Some navigationControls)
                collapsedExpression
                id
                config.navigation
            div {
                _attr ("data-fve-app-shell-content", "true")
                _dataAttr ("inert", $"${openSignal} ? true : null")
                _dataAttr ("aria-hidden", $"${openSignal} ? 'true' : null")
                _class "flex min-w-0 flex-1 flex-col"
                // This is a navigation control row, not a document-level banner (previews may live in a main landmark).
                div {
                    _class (ComponentHtml.classes [ "flex min-h-[var(--fve-shell-bar-min-height)] shrink-0 items-center gap-3 border-b border-[var(--fve-border)] bg-[var(--fve-background)] px-4"; mobileOnly ])
                    fveButton {
                        _variant ButtonVariant.Ghost
                        _ariaLabel "Open navigation"
                        _id triggerId
                        _ariaControls navigationId
                        _ariaExpanded false
                        _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                        _dataOn ("click", openAndFocus)
                        span { _ariaHidden true; _class "inline-flex shrink-0"; menuIcon }
                    }
                    strong { _class "min-w-0 flex-1 break-words text-sm font-semibold"; SideNavView.headerLabel config.navigation }
                }
                match SideNavView.mobileContext config.navigation with
                | Some context -> div { _class (ComponentHtml.classes [ "shrink-0 border-b border-[var(--fve-border)] bg-[var(--fve-background)] px-4 py-3"; mobileOnly ]); context }
                | None -> ()
                match config.contentLandmark with
                | AppShellContentLandmark.Main ->
                    main {
                        _class "flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden"
                        config.content
                    }
                | AppShellContentLandmark.Region label ->
                    section {
                        _ariaLabel label
                        _class "flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden"
                        config.content
                    }
                match config.mobileBottomNavigation with
                | Some bottomNavigation ->
                    BottomNavigationView.render
                        (ComponentHtml.classes [ "shrink-0"; mobileOnly; "border-t border-[var(--fve-border)] bg-[var(--fve-background)] text-[var(--fve-text)]" ])
                        bottomNavigation
                | None -> ()
            }
        }
