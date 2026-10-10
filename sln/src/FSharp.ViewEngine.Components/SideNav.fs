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

[<NoEquality; NoComparison>]
type private SideNavRowDestination<'destination> =
    | Link of 'destination
    | Menu of DropdownMenuConfig<'destination>

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type SideNavRowConfig<'destination> =
    private
        { label:string
          destination:SideNavRowDestination<'destination>
          leading:HtmlElement option
          current:bool
          attributes:HtmlAttribute list }

/// <summary>A full-width link or menu row for context and footer slots.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNavRow =
    let private create label destination =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A side-navigation row label is required."
        { label = label; destination = destination; leading = None; current = false; attributes = [] }

    let link destination label = create label (Link destination)
    let menu menu label = create label (Menu menu)
    let withLeading leading (row:SideNavRowConfig<'destination>) = { row with leading = Some leading }
    let current (row:SideNavRowConfig<'destination>) = { row with current = true }
    let withAttributes attributes (row:SideNavRowConfig<'destination>) = { row with attributes = attributes }

    let render resolve (row:SideNavRowConfig<'destination>) =
        let body menu = span {
            _class "flex min-w-0 w-full items-center gap-3"
            match row.leading with
            | Some leading -> span { _ariaHidden true; _class "flex size-5 shrink-0 items-center justify-center [&>svg]:size-5"; leading }
            | None -> ()
            span { _class "min-w-0 flex-1 truncate text-left"; row.label }
            span {
                _ariaHidden true
                _class "size-4 shrink-0"
                if menu then
                    raw """<svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                else
                    raw """<svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M7.22 4.22a.75.75 0 0 1 1.06 0l5.25 5.25a.75.75 0 0 1 0 1.06l-5.25 5.25a.75.75 0 1 1-1.06-1.06L11.94 10 7.22 5.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
            }
        }
        match row.destination with
        | Link destination ->
            a {
                _href (resolve destination)
                if row.current then _ariaCurrent "page"
                _class (ComponentHtml.classes [ ComponentHtml.popupControlClasses; "flex min-h-10 w-full shrink-0 items-center px-4 py-2 text-sm leading-5 font-semibold no-underline hover:bg-[var(--fve-surface-hover)] active:bg-[var(--fve-surface-active)]"; if row.current then "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)]" else "text-[var(--fve-text)]" ])
                for attribute in ComponentHtml.safeAttributes [ "href"; "aria-current"; "class" ] row.attributes do attribute
                body false
            }
        | Menu menu ->
            menu
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.content (body true) |> DropdownMenuTrigger.asFullRow |> DropdownMenuTrigger.withAttributes row.attributes)
            |> DropdownMenu.render resolve

type internal SideNavNodeKind = Item | Section | Group

/// <summary>A typed navigation link, static section, or collapsible group.</summary>
/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type SideNavNode<'destination> =
    private
        { kind:SideNavNodeKind
          id:string option
          label:string
          destination:'destination option
          leading:HtmlElement option
          badge:HtmlElement option
          action:HtmlElement option
          children:SideNavNode<'destination> list
          expanded:bool
          expandedSignal:string option
          attributes:HtmlAttribute list }

module internal SideNavNodes =
    let create kind label destination children =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A side-navigation label is required."
        if kind <> SideNavNodeKind.Item && List.isEmpty children then
            invalidArg (nameof children) "A navigation section or group requires at least one child."
        { kind = kind; id = None; label = label; destination = destination
          leading = None; badge = None; action = None; children = children
          expanded = false; expandedSignal = None; attributes = [] }
    let withId id (node:SideNavNode<'destination>) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A navigation ID is required."
        { node with id = Some id }

/// <summary>Individual navigation destinations, without child guides.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNavItem =
    let create destination label = SideNavNodes.create SideNavNodeKind.Item label (Some destination) []
    let unavailable<'destination> label : SideNavNode<'destination> = SideNavNodes.create SideNavNodeKind.Item label None []
    let withId id node = SideNavNodes.withId id node
    let withLeading leading (node:SideNavNode<'destination>) = { node with leading = Some leading }
    let withBadge badge (node:SideNavNode<'destination>) = { node with badge = Some badge }
    let withAction action (node:SideNavNode<'destination>) = { node with action = Some action }
    let withAttributes attributes (node:SideNavNode<'destination>) = { node with attributes = attributes }

/// <summary>A small, non-collapsible label above unindented navigation children.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNavSection =
    let create label children = SideNavNodes.create SideNavNodeKind.Section label None children
    let withId id node = SideNavNodes.withId id node

/// <summary>A collapsible group with its child guide centered below the chevron.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNavGroup =
    let create label children = SideNavNodes.create SideNavNodeKind.Group label None children
    let withId id node = SideNavNodes.withId id node
    /// An existing host-owned expansion signal, without a leading dollar sign.
    let withExpandedSignal (signal:string) (node:SideNavNode<'destination>) =
        if not (System.Text.RegularExpressions.Regex.IsMatch(signal, "^[a-zA-Z_][a-zA-Z0-9_]*$")) then
            invalidArg (nameof signal) "An expansion signal must be a valid Datastar identifier."
        { node with expandedSignal = Some signal }
    let withLeading leading node = SideNavItem.withLeading leading node
    let withBadge badge node = SideNavItem.withBadge badge node
    let withAction action node = SideNavItem.withAction action node
    let withAttributes attributes node = SideNavItem.withAttributes attributes node
    let expanded (node:SideNavNode<'destination>) = { node with expanded = true }

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
type SideNavWidth =
    | Narrow
    | Standard
    | Wide

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type SideNavContentConfig<'destination> = private { items:SideNavNode<'destination> list }

/// <summary>Ordered links, sections, and groups for the independently scrolling content slot.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNavContent =
    let create (items:SideNavNode<'destination> list) =
        let rec destinationsFor (item:SideNavNode<'destination>) = [ yield! item.destination |> Option.toList; for child in item.children do yield! destinationsFor child ]
        let destinations = items |> List.collect destinationsFor
        if destinations.Length <> (destinations |> List.distinct |> List.length) then
            invalidArg (nameof items) "Side-navigation destinations must be unique."
        { items = items }

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type SideNavConfig<'destination when 'destination:equality> =
    private
        { id:string
          label:string
          persistenceKey:string
          header:SideNavHeaderConfig option
          current:'destination option
          content:SideNavContentConfig<'destination> option
          width:SideNavWidth
          context:HtmlElement option
          mobileContext:HtmlElement option
          footer:HtmlElement option
          compactFooter:HtmlElement option }

module internal SideNavView =
    let id (config:SideNavConfig<'destination>) = config.id
    let label (config:SideNavConfig<'destination>) = config.label
    let headerLabel (config:SideNavConfig<'destination>) = config.header |> Option.map _.label |> Option.defaultValue config.label
    let items (config:SideNavConfig<'destination>) = config.content |> Option.map _.items |> Option.defaultValue []
    let current (config:SideNavConfig<'destination>) = config.current
    let destinations (config:SideNavConfig<'destination>) =
        let rec collect item = [ yield! item.destination |> Option.toList; for child in item.children do yield! collect child ]
        items config |> List.collect collect
    let width (config:SideNavConfig<'destination>) = config.width
    let mobileContext (config:SideNavConfig<'destination>) = config.mobileContext

    let standaloneWidthClass (config:SideNavConfig<'destination>) =
        match config.width with
        | SideNavWidth.Narrow -> "w-48"
        | SideNavWidth.Standard -> "w-60"
        | SideNavWidth.Wide -> "w-64"

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
        let rec containsCurrent (item:SideNavNode<'destination>) =
            (match config.current, item.destination with Some current, Some destination -> destination = current | _ -> false)
            || List.exists containsCurrent item.children
        let rec hasLeading (item:SideNavNode<'destination>) = item.leading.IsSome || List.exists hasLeading item.children
        let reserveLeading = items config |> List.exists hasLeading
        let leading (item:SideNavNode<'destination>) =
            match item.leading with
            | Some content -> span { _ariaHidden true; _class "flex size-5 shrink-0 items-center justify-center"; content }
            | None when reserveLeading ->
                span {
                    for attribute in hiddenWhenCollapsed do attribute
                    _ariaHidden true; _class "size-5 shrink-0"
                }
            | None -> empty
        let label (item:SideNavNode<'destination>) =
            span {
                for attribute in hiddenWhenCollapsed do attribute
                _class "min-w-0 flex-1 whitespace-normal [overflow-wrap:anywhere]"
                item.label
            }
        let badge (item:SideNavNode<'destination>) =
            match item.badge with
            | Some content ->
                span {
                    for attribute in hiddenWhenCollapsed do attribute
                    _class "shrink-0"
                    content
                }
            | None -> empty
        let action (item:SideNavNode<'destination>) =
            match item.action with
            | Some content ->
                span {
                    for attribute in hiddenWhenCollapsed do attribute
                    _dataOn ("click", [ "stop" ], "true")
                    _class "shrink-0"
                    content
                }
            | None -> empty
        let itemClasses kind current =
            ComponentHtml.classes [
                "flex min-h-[var(--fve-navigation-min-height)] min-w-0 flex-1 items-center gap-1 rounded-[var(--fve-radius-control)] pr-2.5 py-[var(--fve-navigation-padding-block)] text-sm leading-5 font-medium no-underline outline-none transition-colors focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]"
                if kind = SideNavNodeKind.Group then "pl-2" else "pl-3"
                if current then "bg-[var(--fve-brand-subtle)] font-semibold text-[var(--fve-brand-text)]"
                else "text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] active:bg-[var(--fve-surface-active)]" ]
        let storagePrefix = "fve-side-nav:" + config.persistenceKey
        let scrollKey = ComponentHtml.javascriptString (storagePrefix + ":scroll")
        let saveScroll = $"if (el.clientHeight > 0) {{ try {{ sessionStorage.setItem({scrollKey}, String(el.scrollTop)); }} catch {{}} }}"
        let rec renderItem path (item:SideNavNode<'destination>) =
            let itemId = item.id |> Option.defaultValue (config.id + "-item-" + path)
            let current =
                match config.current, item.destination with
                | Some current, Some destination -> destination = current
                | _ -> false
            li {
                if item.kind = SideNavNodeKind.Section then _class "mt-5 first:mt-0"
                match item.kind with
                | SideNavNodeKind.Item ->
                    div {
                        _class "flex min-w-0 items-center gap-1"
                        match item.destination with
                        | Some destination ->
                            a {
                                _id itemId
                                _href (resolve destination)
                                _ariaLabel item.label
                                _title item.label
                                if current then _ariaCurrent "page"
                                _class (itemClasses item.kind current)
                                for attribute in ComponentHtml.safeAttributes [ "id"; "href"; "aria-current"; "aria-label"; "title"; "class" ] item.attributes do attribute
                                leading item; label item; badge item
                            }
                        | None ->
                            span {
                                _ariaDisabled true
                                _ariaLabel item.label
                                _title item.label
                                _class (itemClasses item.kind false + " cursor-not-allowed opacity-50")
                                for attribute in ComponentHtml.safeAttributes [ "aria-disabled"; "aria-label"; "title"; "class"; "href" ] item.attributes do attribute
                                leading item; label item; badge item
                            }
                        action item
                    }
                | SideNavNodeKind.Section ->
                    section {
                        _id (itemId + "-section")
                        h2 {
                            _id itemId
                            for attribute in hiddenWhenCollapsed do attribute
                            _class "pl-3 pr-2.5 pb-2 text-xs font-medium text-[var(--fve-muted-text)]"
                            item.label
                        }
                        ul {
                            _id (itemId + "-children")
                            _ariaLabel item.label
                            _role "list"
                            _class "grid min-w-0 gap-px"
                            for index, child in List.indexed item.children do renderItem (path + "-" + string index) child
                        }
                    }
                | SideNavNodeKind.Group ->
                    let expanded = string (item.expanded || containsCurrent item) |> _.ToLowerInvariant()
                    let groupKey = item.id |> Option.defaultValue (path + ":" + item.label)
                    let key = storagePrefix + ":group:" + groupKey
                    let storageKey = ComponentHtml.javascriptString key
                    let signal = item.expandedSignal |> Option.defaultValue ("_side_nav_" + ComponentHtml.optionToken key)
                    // Host-owned signals keep their existing initialization and persistence policy.
                    let initial =
                        if item.expandedSignal.IsSome then expanded
                        else $"(() => {{ try {{ const saved = sessionStorage.getItem({storageKey}); const value = saved === 'true' ? true : saved === 'false' ? false : {expanded}; sessionStorage.setItem({storageKey}, String(value)); return value; }} catch {{ return {expanded}; }} }})()"
                    let toggle =
                        $"evt.preventDefault(); ${signal} = !${signal}"
                        + (if item.expandedSignal.IsSome then "" else $"; try {{ sessionStorage.setItem({storageKey}, String(${signal})); }} catch {{}}")
                    details {
                        if item.expanded || containsCurrent item then _open true
                        _id (itemId + "-group")
                        _ariaLabel item.label
                        _attr ("data-signals__ifmissing", $"{{{signal}: {initial}}}")
                        _dataAttr ("open", $"${signal}")
                        _dataPreserveAttr "open"
                        _class "group/navigation-item"
                        summary {
                            _id itemId
                            _ariaLabel ("Toggle " + item.label + " section")
                            _ariaControls (itemId + "-children")
                            _dataAttr ("aria-expanded", $"${signal} ? 'true' : 'false'")
                            _dataOn ("click", toggle)
                            _title item.label
                            _class (itemClasses item.kind false + " cursor-pointer list-none [&::-webkit-details-marker]:hidden" + (if containsCurrent item then " font-semibold text-[var(--fve-text)]" else ""))
                            for attribute in ComponentHtml.safeAttributes [ "id"; "aria-label"; "aria-expanded"; "aria-controls"; "title"; "class" ] item.attributes do attribute
                            span {
                                for attribute in hiddenWhenCollapsed do attribute
                                _ariaHidden true
                                _class "size-4 shrink-0 [[open]>summary>&]:rotate-90 [&>svg]:size-4"
                                raw """<svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M7.21 4.96a.75.75 0 0 1 1.06 0l4.51 4.51a.75.75 0 0 1 0 1.06l-4.51 4.51a.75.75 0 1 1-1.06-1.06L11.19 10 7.21 6.02a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                            }
                            leading item; label item; badge item; action item
                        }
                        ul {
                            _id (itemId + "-children")
                            _role "list"
                            for attribute in hiddenWhenCollapsed do attribute
                            _class "ml-[calc(1rem-0.5px)] mt-0.5 grid min-w-0 gap-px border-l border-[var(--fve-border)] pl-1"
                            for index, child in List.indexed item.children do renderItem (path + "-" + string index) child
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
            match config.header with
            | Some header ->
                div {
                    _attr ("data-fve-side-nav-header", "true")
                    _class "shrink-0 border-b border-[var(--fve-border)]"
                    div {
                        _class "flex min-h-[calc(var(--fve-shell-bar-min-height)-1px)] items-center gap-3 px-4"
                        div {
                            _class "min-w-0 flex-1"
                            div {
                                for attribute in hiddenWhenCollapsed do attribute
                                header.content
                            }
                            match header.compactContent, collapsedExpression with
                            | Some content, Some expression -> div { _dataClass ("hidden", $"!({expression})"); _class "hidden"; content }
                            | _ -> ()
                        }
                        closeControl |> Option.defaultValue empty
                    }
                }
            | None -> ()
            match config.context with
            | Some context ->
                div {
                    for attribute in hiddenWhenCollapsed do attribute
                    _attr ("data-fve-side-nav-context", "true")
                    _class "shrink-0 border-b border-[var(--fve-border)]"
                    context
                }
            | None -> ()
            nav {
                _ariaLabel config.label
                _attr ("data-fve-side-nav-content", "true")
                // Restore only when visible, including a mobile panel first opened after load.
                _attr ("data-on-intersect__once", $"try {{ const saved = Number(sessionStorage.getItem({scrollKey})); if (Number.isFinite(saved) && saved >= 0) el.scrollTop = saved; }} catch {{}}")
                _dataOn ("scroll", ["passive"], saveScroll)
                _dataOn ("click", ["capture"], saveScroll)
                _class (ComponentHtml.classes [ "min-h-0 min-w-0 flex-1 overflow-x-hidden overflow-y-auto"; if config.content.IsSome then "p-3" ])
                ul {
                    _role "list"
                    _class "grid min-w-0 gap-px"
                    for index, item in List.indexed (items config) do renderItem (string index) item
                }
            }
            match config.footer with
            | Some footer ->
                div {
                    _attr ("data-fve-side-nav-footer", "true")
                    _class "shrink-0 border-t border-[var(--fve-border)]"
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
    let create id label =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable side-navigation ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible side-navigation label is required."
        { id = id
          label = label
          persistenceKey = id
          header = None
          current = None
          content = None
          width = SideNavWidth.Wide
          context = None
          mobileContext = None
          footer = None
          compactFooter = None }

    /// Per-tab preference scope. Defaults to the stable navigation ID; use a distinct key
    /// for unrelated trees, or the same key for responsive presentations of one tree.
    let withPersistenceKey key (config:SideNavConfig<'destination>) =
        if String.IsNullOrWhiteSpace key then invalidArg (nameof key) "A navigation persistence key is required."
        { config with persistenceKey = key }

    let withHeader header (config:SideNavConfig<'destination>) = { config with header = Some header }
    let withContent content (config:SideNavConfig<'destination>) = { config with content = Some content }

    let withCurrent current (config:SideNavConfig<'destination>) =
        let rec contains item = item.destination = Some current || List.exists contains item.children
        let represented = SideNavView.items config |> List.exists contains
        if not represented then invalidArg (nameof current) "The current destination must exist in the side navigation."
        { config with current = Some current }

    let withWidth width (config:SideNavConfig<'destination>) = { config with width = width }
    let withContext (rows:HtmlElement list) (config:SideNavConfig<'destination>) = { config with context = if List.isEmpty rows then None else Some (fragment { for row in rows do yield row }) }
    let withMobileContext context (config:SideNavConfig<'destination>) = { config with mobileContext = Some context }
    let withFooter (rows:HtmlElement list) (config:SideNavConfig<'destination>) = { config with footer = if List.isEmpty rows then None else Some (fragment { for row in rows do yield row }) }
    let withCompactFooter footer (config:SideNavConfig<'destination>) = { config with compactFooter = Some footer }

    let render resolve config =
        SideNavView.render
            (ComponentHtml.classes [ "flex h-full min-h-0 flex-col border-r border-[var(--fve-border)] bg-[var(--fve-background)] text-[var(--fve-text)]"; SideNavView.standaloneWidthClass config ])
            []
            None
            None
            resolve
            config
