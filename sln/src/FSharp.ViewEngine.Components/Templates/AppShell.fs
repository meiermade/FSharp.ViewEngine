namespace FSharp.ViewEngine.Components.Templates

open FSharp.ViewEngine.Components

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
type AppShellConfig<'destination when 'destination:equality> =
    private
        { id:string
          navigation:SideNavConfig<'destination>
          content:HtmlElement
          theme:ComponentsTheme
          breakpoint:AppShellBreakpoint
          boundary:AppShellBoundary
          contentLandmark:AppShellContentLandmark
          mobileBottomNavigation:BottomNavigationConfig<'destination> option
          collapsibleNavigation:bool
          attributes:HtmlAttribute list }

/// <category>app-shell</category>
[<RequireQualifiedAccess>]
module AppShell =
    let create id navigation content =
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

    let withTheme theme (config:AppShellConfig<'destination>) = { config with theme = theme }
    let withBreakpoint breakpoint (config:AppShellConfig<'destination>) = { config with breakpoint = breakpoint }
    let withBoundary boundary (config:AppShellConfig<'destination>) = { config with boundary = boundary }
    let withCollapsibleNavigation (config:AppShellConfig<'destination>) = { config with collapsibleNavigation = true }

    let withMobileBottomNavigation id label items (config:AppShellConfig<'destination>) =
        if String.Equals(id, config.id, StringComparison.Ordinal) || String.Equals(id, SideNavView.id config.navigation, StringComparison.Ordinal) then
            invalidArg (nameof id) "Bottom-navigation, application-shell, and side-navigation IDs must be distinct."

        let bottomNavigation = BottomNavigation.create id label items
        let sideNavDestinations = SideNavView.destinations config.navigation
        let missingDestinations =
            BottomNavigationView.destinations bottomNavigation
            |> List.filter (fun destination -> not (List.contains destination sideNavDestinations))

        if not (List.isEmpty missingDestinations) then
            invalidArg (nameof items) "Every bottom-navigation destination must also exist in the side navigation."

        let bottomNavigation =
            SideNavView.current config.navigation
            |> Option.map (fun current -> BottomNavigation.withCurrent current bottomNavigation)
            |> Option.defaultValue bottomNavigation

        { config with mobileBottomNavigation = Some bottomNavigation }

    /// Use only when an AppShell is embedded inside an existing main landmark, such as a Docs preview.
    let asPreview label (config:AppShellConfig<'destination>) =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An embedded AppShell needs a landmark label."
        { config with contentLandmark = AppShellContentLandmark.Region label }
    let withAttributes attributes (config:AppShellConfig<'destination>) = { config with attributes = attributes }

    let private menuIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5" aria-hidden="true"><path fill-rule="evenodd" d="M2.75 5A.75.75 0 0 1 3.5 4.25h13a.75.75 0 0 1 0 1.5h-13A.75.75 0 0 1 2.75 5Zm0 5a.75.75 0 0 1 .75-.75h13a.75.75 0 0 1 0 1.5h-13a.75.75 0 0 1-.75-.75Zm0 5a.75.75 0 0 1 .75-.75h13a.75.75 0 0 1 0 1.5h-13a.75.75 0 0 1-.75-.75Z" clip-rule="evenodd"/></svg>"""

    let private closeIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5" aria-hidden="true"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 1 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""

    let private collapseIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5 transition-transform group-data-[collapsed=true]:rotate-180" aria-hidden="true"><path fill-rule="evenodd" d="M12.78 4.22a.75.75 0 0 1 0 1.06L8.06 10l4.72 4.72a.75.75 0 1 1-1.06 1.06l-5.25-5.25a.75.75 0 0 1 0-1.06l5.25-5.25a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>"""

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

    let render resolve (config:AppShellConfig<'destination>) =
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
            Button.create (ButtonContent.Icon ("Close navigation", closeIcon))
            |> Button.withVariant ButtonVariant.Ghost
            |> Button.withClass mobileOnly
            |> Button.withAttributes [ _id $"{config.id}-navigation-close"; _dataOn ("click", closeAndRestore) ]
            |> Button.render
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
                resolve
                config.navigation
            div {
                _attr ("data-fve-app-shell-content", "true")
                _dataAttr ("inert", $"${openSignal} ? true : null")
                _dataAttr ("aria-hidden", $"${openSignal} ? 'true' : null")
                _class "flex min-w-0 flex-1 flex-col"
                // This is a navigation control row, not a document-level banner (previews may live in a main landmark).
                div {
                    _class (ComponentHtml.classes [ "flex min-h-[var(--fve-shell-bar-min-height)] shrink-0 items-center gap-3 border-b border-[var(--fve-border)] bg-[var(--fve-background)] px-4"; mobileOnly ])
                    Button.create (ButtonContent.Icon ("Open navigation", menuIcon))
                    |> Button.withVariant ButtonVariant.Ghost
                    |> Button.withAttributes [
                        _id triggerId
                        _ariaControls navigationId
                        _ariaExpanded false
                        _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                        _dataOn ("click", openAndFocus) ]
                    |> Button.render
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
                        resolve
                        bottomNavigation
                | None -> ()
            }
        }
