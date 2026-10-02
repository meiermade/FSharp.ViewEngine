namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type SideNavHeaderConfig =
    private
        { label:string
          content:HtmlElement
          compactContent:HtmlElement option }

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNavHeader =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible side-navigation header label is required."
        { label = label; content = strong { _class "truncate text-base font-semibold text-[var(--fve-text)]"; label }; compactContent = None }

    let withContent content (config:SideNavHeaderConfig) = { config with content = content }
    let withCompactContent content (config:SideNavHeaderConfig) = { config with compactContent = Some content }

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type SideNavItem<'destination> =
    private
        { label:string
          destination:'destination option
          leading:HtmlElement option
          badge:HtmlElement option
          action:HtmlElement option
          children:SideNavItem<'destination> list
          expanded:bool
          attributes:HtmlAttribute list }

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNavItem =
    let private item label destination =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A side-navigation item label is required."
        { label = label
          destination = destination
          leading = None
          badge = None
          action = None
          children = []
          expanded = false
          attributes = [] }

    let create destination label = item label (Some destination)
    let unavailable<'destination> label : SideNavItem<'destination> = item label None
    let nested label children =
        if List.isEmpty children then invalidArg (nameof children) "A nested navigation item requires at least one child."
        { item label None with children = children }
    let withLeading leading (item:SideNavItem<'destination>) = { item with leading = Some leading }
    let withBadge badge (item:SideNavItem<'destination>) = { item with badge = Some badge }
    let withAction action (item:SideNavItem<'destination>) = { item with action = Some action }
    let expanded (item:SideNavItem<'destination>) = { item with expanded = true }
    let withAttributes attributes (item:SideNavItem<'destination>) = { item with attributes = attributes }

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type SideNavSection<'destination> =
    private
        { label:string option
          items:SideNavItem<'destination> list }

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNavSection =
    let private requireItems (items:SideNavItem<'destination> list) =
        if List.isEmpty items then invalidArg (nameof items) "A side-navigation section requires at least one item."
        items

    let ungrouped items = { label = None; items = requireItems items }

    let group label items =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A side-navigation section label is required."
        { label = Some label; items = requireItems items }

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
type SideNavWidth =
    | Narrow
    | Standard
    | Wide

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
type SideNavRegionLayout =
    | Padded
    | Flush

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type SideNavConfig<'destination when 'destination:equality> =
    private
        { id:string
          label:string
          header:SideNavHeaderConfig
          showHeader:bool
          current:'destination option
          sections:SideNavSection<'destination> list
          width:SideNavWidth
          context:HtmlElement option
          mobileContext:HtmlElement option
          contextLayout:SideNavRegionLayout
          footer:HtmlElement option
          compactFooter:HtmlElement option
          footerLayout:SideNavRegionLayout }

module internal SideNavView =
    let id (config:SideNavConfig<'destination>) = config.id
    let label (config:SideNavConfig<'destination>) = config.label
    let headerLabel (config:SideNavConfig<'destination>) = config.header.label
    let current (config:SideNavConfig<'destination>) = config.current
    let destinations (config:SideNavConfig<'destination>) =
        let rec collect item = [ yield! item.destination |> Option.toList; for child in item.children do yield! collect child ]
        config.sections |> List.collect _.items |> List.collect collect
    let width (config:SideNavConfig<'destination>) = config.width
    let mobileContext (config:SideNavConfig<'destination>) = config.mobileContext

    let standaloneWidthClass (config:SideNavConfig<'destination>) =
        match config.width with
        | SideNavWidth.Narrow -> "w-48"
        | SideNavWidth.Standard -> "w-60"
        | SideNavWidth.Wide -> "w-64"

    let private regionClasses layout =
        match layout with
        | SideNavRegionLayout.Padded -> "px-4 py-3"
        | SideNavRegionLayout.Flush -> ""

    let render
        (className:string)
        (attributes:HtmlAttribute list)
        (closeControl:HtmlElement option)
        (collapsedExpression:string option)
        (resolve:'destination -> string)
        (config:SideNavConfig<'destination>) =
        let hiddenWhenCollapsed =
            match collapsedExpression with
            | Some expression -> [ _dataClass ("hidden", expression) ]
            | None -> []
        let rec containsCurrent (item:SideNavItem<'destination>) =
            (match config.current, item.destination with Some current, Some destination -> destination = current | _ -> false)
            || List.exists containsCurrent item.children
        let leading (item:SideNavItem<'destination>) =
            match item.leading with
            | Some content -> span { _ariaHidden true; _class "flex size-5 shrink-0 items-center justify-center"; content }
            | None -> empty
        let label (item:SideNavItem<'destination>) =
            span {
                for attribute in hiddenWhenCollapsed do attribute
                _class "min-w-0 flex-1 truncate"
                item.label
            }
        let badge (item:SideNavItem<'destination>) =
            match item.badge with
            | Some content ->
                span {
                    for attribute in hiddenWhenCollapsed do attribute
                    _class "shrink-0"
                    content
                }
            | None -> empty
        let action (item:SideNavItem<'destination>) =
            match item.action with
            | Some content ->
                span {
                    for attribute in hiddenWhenCollapsed do attribute
                    _dataOn ("click", [ "stop" ], "true")
                    _class "shrink-0"
                    content
                }
            | None -> empty
        let itemClasses current =
            ComponentHtml.classes [
                "flex min-h-[var(--fve-navigation-min-height)] min-w-0 flex-1 items-center gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-navigation-padding-block)] text-sm font-semibold no-underline outline-none transition-colors focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]"
                if current then "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)]"
                else "text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] active:bg-[var(--fve-surface-active)]" ]
        let rec renderItem (item:SideNavItem<'destination>) =
            let current =
                match config.current, item.destination with
                | Some current, Some destination -> destination = current
                | _ -> false
            li {
                if item.children.IsEmpty then
                    div {
                        _class "flex min-w-0 items-center gap-1"
                        match item.destination with
                        | Some destination ->
                            a {
                                _href (resolve destination)
                                _ariaLabel item.label
                                _title item.label
                                if current then _ariaCurrent "page"
                                _class (itemClasses current)
                                for attribute in ComponentHtml.safeAttributes [ "href"; "aria-current"; "aria-label"; "title"; "class" ] item.attributes do attribute
                                leading item; label item; badge item
                            }
                        | None ->
                            span {
                                _ariaDisabled true
                                _ariaLabel item.label
                                _title item.label
                                _class "flex min-h-[var(--fve-navigation-min-height)] min-w-0 flex-1 cursor-not-allowed items-center gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-navigation-padding-block)] text-sm font-semibold text-[var(--fve-muted-text)] opacity-50"
                                for attribute in ComponentHtml.safeAttributes [ "aria-disabled"; "aria-label"; "title"; "class"; "href" ] item.attributes do attribute
                                leading item; label item; badge item
                            }
                        action item
                    }
                else
                    details {
                        if item.expanded || containsCurrent item then _open true
                        _class "group/navigation-item"
                        summary {
                            _ariaLabel item.label
                            _title item.label
                            _class (itemClasses (containsCurrent item) + " cursor-pointer list-none [&::-webkit-details-marker]:hidden")
                            leading item; label item; badge item; action item
                            span {
                                for attribute in hiddenWhenCollapsed do attribute
                                _ariaHidden true
                                _class "ml-auto size-4 shrink-0 transition-transform group-open/navigation-item:rotate-90"
                                raw """<svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M7.21 4.96a.75.75 0 0 1 1.06 0l4.51 4.51a.75.75 0 0 1 0 1.06l-4.51 4.51a.75.75 0 1 1-1.06-1.06L11.19 10 7.21 6.02a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                            }
                        }
                        ul {
                            _role "list"
                            for attribute in hiddenWhenCollapsed do attribute
                            _class "ml-5 mt-1 grid gap-1 border-l border-[var(--fve-border)] pl-2"
                            for child in item.children do renderItem child
                        }
                    }
            }

        aside {
            _id config.id
            _class className
            match collapsedExpression with
            | Some expression -> _dataAttr ("data-fve-collapsed", $"{expression} ? 'true' : 'false'")
            | None -> ()
            for attribute in ComponentHtml.safeAttributes [ "id"; "class" ] attributes do attribute
            if config.showHeader then
                div {
                    _attr ("data-fve-side-nav-header", "true")
                    _class "shrink-0 border-b border-[var(--fve-border)]"
                    div {
                        _class "flex min-h-[calc(var(--fve-shell-bar-min-height)-1px)] items-center gap-3 px-4"
                        div {
                            _class "min-w-0 flex-1"
                            div {
                                for attribute in hiddenWhenCollapsed do attribute
                                config.header.content
                            }
                            match config.header.compactContent, collapsedExpression with
                            | Some content, Some expression -> div { _dataClass ("hidden", $"!({expression})"); _class "hidden"; content }
                            | _ -> ()
                        }
                        closeControl |> Option.defaultValue empty
                    }
                }
            match config.context with
            | Some context ->
                div {
                    for attribute in hiddenWhenCollapsed do attribute
                    _class (ComponentHtml.classes [ "shrink-0 border-b border-[var(--fve-border)]"; regionClasses config.contextLayout ])
                    context
                }
            | None -> ()
            nav {
                _ariaLabel config.label
                _class "min-h-0 flex-1 overflow-y-auto px-3 py-4"
                for navigationSection in config.sections do
                    match navigationSection.label with
                    | Some label ->
                        section {
                            _ariaLabel label
                            _class "mb-5 last:mb-0"
                            h2 {
                                for attribute in hiddenWhenCollapsed do attribute
                                _class "px-3 pb-2 text-xs font-normal uppercase tracking-wider text-[var(--fve-muted-text)]"
                                label
                            }
                            ul {
                                _role "list"
                                _class "grid gap-1"
                                for item in navigationSection.items do renderItem item
                            }
                        }
                    | None ->
                        ul {
                            _role "list"
                            _class "mb-5 grid gap-1 last:mb-0"
                            for item in navigationSection.items do renderItem item
                        }
            }
            match config.footer with
            | Some footer ->
                div {
                    _class (ComponentHtml.classes [ "shrink-0 border-t border-[var(--fve-border)]"; regionClasses config.footerLayout ])
                    div {
                        for attribute in hiddenWhenCollapsed do attribute
                        footer
                    }
                    match config.compactFooter, collapsedExpression with
                    | Some content, Some expression -> div { _dataClass ("hidden", $"!({expression})"); _class "hidden"; content }
                    | _ -> ()
                }
            | None -> ()
        }

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNav =
    let create id label header (sections:SideNavSection<'destination> list) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable side-navigation ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible side-navigation label is required."
        if List.isEmpty sections then invalidArg (nameof sections) "At least one side-navigation section is required."

        let rec destinationsFor item = [ yield! item.destination |> Option.toList; for child in item.children do yield! destinationsFor child ]
        let destinations = sections |> List.collect _.items |> List.collect destinationsFor

        if destinations.Length <> (destinations |> List.distinct |> List.length) then
            invalidArg (nameof sections) "Side-navigation destinations must be unique."

        { id = id
          label = label
          header = header
          showHeader = true
          current = None
          sections = sections
          width = SideNavWidth.Wide
          context = None
          mobileContext = None
          contextLayout = SideNavRegionLayout.Padded
          footer = None
          compactFooter = None
          footerLayout = SideNavRegionLayout.Padded }

    let withCurrent current (config:SideNavConfig<'destination>) =
        let rec contains item = item.destination = Some current || List.exists contains item.children
        let represented = config.sections |> List.collect _.items |> List.exists contains
        if not represented then invalidArg (nameof current) "The current destination must exist in the side navigation."
        { config with current = Some current }

    /// Omit duplicate branding when the owning shell supplies a full-width PageTopBar.
    let withoutHeader (config:SideNavConfig<'destination>) = { config with showHeader = false }
    let withWidth width (config:SideNavConfig<'destination>) = { config with width = width }
    let withContext context (config:SideNavConfig<'destination>) = { config with context = Some context }
    let withMobileContext context (config:SideNavConfig<'destination>) = { config with mobileContext = Some context }
    let withContextLayout layout (config:SideNavConfig<'destination>) = { config with contextLayout = layout }
    let withFooter footer (config:SideNavConfig<'destination>) = { config with footer = Some footer }
    let withCompactFooter footer (config:SideNavConfig<'destination>) = { config with compactFooter = Some footer }
    let withFooterLayout layout (config:SideNavConfig<'destination>) = { config with footerLayout = layout }

    let render resolve config =
        SideNavView.render
            (ComponentHtml.classes [ "flex h-full flex-col border-r border-[var(--fve-border)] bg-[var(--fve-background)] text-[var(--fve-text)]"; SideNavView.standaloneWidthClass config ])
            []
            None
            None
            resolve
            config
