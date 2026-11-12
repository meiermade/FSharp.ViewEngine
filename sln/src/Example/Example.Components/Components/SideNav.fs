namespace Example.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type internal SideNavHeaderConfig =
    private
        { label: string
          content: HtmlElement
          compactContent: HtmlElement option
          attributes: HtmlAttribute list }

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module internal SideNavHeader =
    let create label =
        if String.IsNullOrWhiteSpace label then
            invalidArg (nameof label) "An accessible side-navigation header label is required."

        { label = label
          content =
            strong {
                _class "truncate text-base font-semibold text-[var(--fve-text)]"
                label
            }
          compactContent = None
          attributes = [] }

    let withContent content (config: SideNavHeaderConfig) = { config with content = content }

    let withCompactContent content (config: SideNavHeaderConfig) =
        { config with
            compactContent = Some content }

[<NoEquality; NoComparison>]
type private SideNavRowDestination<'destination> =
    | Link of 'destination
    | Menu of HtmlElement

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type internal SideNavRowConfig<'destination> =
    private
        { label: string
          body: HtmlElement option
          destination: SideNavRowDestination<'destination>
          leading: HtmlElement option
          description: string option
          trailing: HtmlElement option
          current: bool
          attributes: HtmlAttribute list }

/// <summary>A full-width link or menu row for context and footer slots.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module internal SideNavRow =
    let private create label destination =
        if String.IsNullOrWhiteSpace label then
            invalidArg (nameof label) "A side-navigation row label is required."

        { label = label
          body = None
          destination = destination
          leading = None
          description = None
          trailing = None
          current = false
          attributes = [] }

    let link destination label = create label (Link destination)
    let menu menu label = create label (Menu menu)
    let withLeading leading (row: SideNavRowConfig<'destination>) = { row with leading = Some leading }

    let withDescription description (row: SideNavRowConfig<'destination>) =
        { row with
            description = Some description }

    let withTrailing trailing (row: SideNavRowConfig<'destination>) = { row with trailing = Some trailing }
    let current (row: SideNavRowConfig<'destination>) = { row with current = true }
    let withAttributes attributes (row: SideNavRowConfig<'destination>) = { row with attributes = attributes }

    let render resolve (row: SideNavRowConfig<'destination>) =
        let body menu =
            span {
                _class "flex min-w-0 w-full items-center gap-3"

                match row.leading with
                | Some leading ->
                    span {
                        _ariaHidden true
                        _class "flex shrink-0 items-center justify-center [&>svg]:size-5"
                        leading
                    }
                | None -> ()

                span {
                    _class "min-w-0 flex-1 text-left"

                    span {
                        _class "block truncate"
                        row.body |> Option.defaultWith (fun () -> Html.text row.label)
                    }

                    match row.description with
                    | Some description ->
                        span {
                            _class "block truncate text-xs leading-4 font-normal text-[var(--fve-muted-text)]"
                            description
                        }
                    | None -> ()
                }

                match row.trailing with
                | Some trailing ->
                    span {
                        _class "flex shrink-0 items-center"
                        trailing
                    }
                | None -> ()

                if menu then
                    span {
                        _ariaHidden true
                        _class "size-4 shrink-0"

                        raw
                            """<svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                    }
            }

        match row.destination with
        | Link destination ->
            a {
                _href (resolve destination)

                if row.current then
                    _ariaCurrent "page"

                _class (
                    ComponentHtml.classes
                        [ ComponentHtml.popupControlClasses
                          "flex min-h-10 w-full shrink-0 items-center px-4 py-2 text-sm leading-5 font-semibold no-underline [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] "
                          if row.current then
                              "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)]"
                          else
                              "text-[var(--fve-text)]"
                          ComponentContent.classes row.attributes ]
                )

                for attribute in ComponentHtml.safeAttributes [ "href"; "aria-current"; "class" ] row.attributes do
                    attribute

                body false
            }
        | Menu menu ->
            match menu with
            | :? DropdownMenuElement as menu ->
                menu.Config
                |> DropdownMenu.withTrigger (
                    DropdownMenuTrigger.create
                        row.attributes
                        [ ComponentControlElement(
                              "button",
                              [],
                              (fun attributes ->
                                  ComponentElement.render
                                      "button"
                                      [ _class
                                            "flex min-h-10 w-full items-center rounded-none px-4 py-2 text-left text-sm leading-5 font-semibold text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] " ]
                                      attributes
                                      [ body true ]),
                              NoopElement()
                          ) ]
                )
                |> DropdownMenu.dslAttributes [ _class "w-full" ]
                |> DropdownMenu.render (fun href -> href)
            | _ -> invalidArg "menu" "A navigation menu row requires fveDropdownMenu content."

type internal SideNavNodeKind =
    | Item
    | Section
    | Group

/// <summary>A typed navigation link, static section, or collapsible group.</summary>
/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type internal SideNavNode<'destination> =
    private
        { kind: SideNavNodeKind
          id: string option
          label: string
          body: HtmlElement option
          destination: 'destination option
          leading: HtmlElement option
          badge: HtmlElement option
          action: HtmlElement option
          children: SideNavNode<'destination> list
          expanded: bool
          expandedSignal: string option
          attributes: HtmlAttribute list }

module internal SideNavNodes =
    let create kind label destination children =
        if String.IsNullOrWhiteSpace label then
            invalidArg (nameof label) "A side-navigation label is required."

        if kind <> SideNavNodeKind.Item && List.isEmpty children then
            invalidArg (nameof children) "A navigation section or group requires at least one child."

        { kind = kind
          id = None
          label = label
          body = None
          destination = destination
          leading = None
          badge = None
          action = None
          children = children
          expanded = false
          expandedSignal = None
          attributes = [] }

    let withId id (node: SideNavNode<'destination>) =
        if String.IsNullOrWhiteSpace id then
            invalidArg (nameof id) "A navigation ID is required."

        { node with id = Some id }

/// <summary>Individual navigation destinations, without child guides.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module internal SideNavItem =
    let create destination label =
        SideNavNodes.create SideNavNodeKind.Item label (Some destination) []

    let unavailable<'destination> label : SideNavNode<'destination> =
        SideNavNodes.create SideNavNodeKind.Item label None []

    let withId id node = SideNavNodes.withId id node
    let withLeading leading (node: SideNavNode<'destination>) = { node with leading = Some leading }
    let withBadge badge (node: SideNavNode<'destination>) = { node with badge = Some badge }
    let withAction action (node: SideNavNode<'destination>) = { node with action = Some action }
    let withAttributes attributes (node: SideNavNode<'destination>) = { node with attributes = attributes }

/// <summary>A small, non-collapsible label above unindented navigation children.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module internal SideNavSection =
    let create label children =
        SideNavNodes.create SideNavNodeKind.Section label None children

    let withId id node = SideNavNodes.withId id node

/// <summary>A collapsible group with its child guide centered below the chevron.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module internal SideNavGroup =
    let create label children =
        SideNavNodes.create SideNavNodeKind.Group label None children

    let withId id node = SideNavNodes.withId id node

    /// An existing host-owned expansion signal, without a leading dollar sign.
    let withExpandedSignal (signal: string) (node: SideNavNode<'destination>) =
        if not (System.Text.RegularExpressions.Regex.IsMatch(signal, "^[a-zA-Z_][a-zA-Z0-9_]*$")) then
            invalidArg (nameof signal) "An expansion signal must be a valid Datastar identifier."

        { node with
            expandedSignal = Some signal }

    let withLeading leading node = SideNavItem.withLeading leading node
    let withBadge badge node = SideNavItem.withBadge badge node
    let withAction action node = SideNavItem.withAction action node

    let withAttributes attributes node =
        SideNavItem.withAttributes attributes node

    let expanded (node: SideNavNode<'destination>) = { node with expanded = true }

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
type SideNavWidth =
    | Narrow
    | Standard
    | Wide

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type internal SideNavContentConfig<'destination> =
    private
        { items: SideNavNode<'destination> list
          attributes: HtmlAttribute list }

/// <summary>Ordered links, sections, and groups for the independently scrolling content slot.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module internal SideNavContent =
    let create (items: SideNavNode<'destination> list) =
        let rec destinationsFor (item: SideNavNode<'destination>) =
            [ yield! item.destination |> Option.toList
              for child in item.children do
                  yield! destinationsFor child ]

        let destinations = items |> List.collect destinationsFor

        if destinations.Length <> (destinations |> List.distinct |> List.length) then
            invalidArg (nameof items) "Side-navigation destinations must be unique."

        { items = items; attributes = [] }

/// <category>side-nav</category>
[<NoEquality; NoComparison>]
type internal SideNavConfig<'destination when 'destination: equality> =
    private
        { id: string
          label: string
          persistenceKey: string
          header: SideNavHeaderConfig option
          current: 'destination option
          content: SideNavContentConfig<'destination> option
          width: SideNavWidth
          context: HtmlElement option
          mobileContext: HtmlElement option
          footer: HtmlElement option
          compactFooter: HtmlElement option
          attributes: HtmlAttribute list }

module internal SideNavView =
    let id (config: SideNavConfig<'destination>) = config.id
    let label (config: SideNavConfig<'destination>) = config.label

    let headerLabel (config: SideNavConfig<'destination>) =
        config.header |> Option.map _.label |> Option.defaultValue config.label

    let items (config: SideNavConfig<'destination>) =
        config.content |> Option.map _.items |> Option.defaultValue []

    let current (config: SideNavConfig<'destination>) = config.current

    let destinations (config: SideNavConfig<'destination>) =
        let rec collect item =
            [ yield! item.destination |> Option.toList
              for child in item.children do
                  yield! collect child ]

        items config |> List.collect collect

    let width (config: SideNavConfig<'destination>) = config.width
    let mobileContext (config: SideNavConfig<'destination>) = config.mobileContext

    let standaloneWidthClass (config: SideNavConfig<'destination>) =
        match config.width with
        | SideNavWidth.Narrow -> "w-48"
        | SideNavWidth.Standard -> "w-60"
        | SideNavWidth.Wide -> "w-64"

    let render
        (className: string)
        (attributes: HtmlAttribute list)
        (closeControl: HtmlElement option)
        (collapsedExpression: string option)
        (resolve: 'destination -> string)
        (config: SideNavConfig<'destination>)
        =
        let hiddenWhenCollapsed =
            match collapsedExpression with
            | Some expression -> [ _dataClass ("hidden", expression) ]
            | None -> []

        let rec containsCurrent (item: SideNavNode<'destination>) =
            (match config.current, item.destination with
             | Some current, Some destination -> destination = current
             | _ ->
                 config.current.IsNone
                 && ComponentElement.value "aria-current" item.attributes = Some "page")
            || List.exists containsCurrent item.children

        let rec hasLeading (item: SideNavNode<'destination>) =
            item.leading.IsSome || List.exists hasLeading item.children

        let reserveLeading = items config |> List.exists hasLeading

        let leading (item: SideNavNode<'destination>) =
            match item.leading with
            | Some content ->
                span {
                    _ariaHidden true
                    _class "flex size-5 shrink-0 items-center justify-center"
                    content
                }
            | None when reserveLeading ->
                span {
                    for attribute in hiddenWhenCollapsed do
                        attribute

                    _ariaHidden true
                    _class "size-5 shrink-0"
                }
            | None -> empty

        let label (item: SideNavNode<'destination>) =
            span {
                for attribute in hiddenWhenCollapsed do
                    attribute

                _class "min-w-0 flex-1 whitespace-normal [overflow-wrap:anywhere]"
                item.body |> Option.defaultWith (fun () -> Html.text item.label)
            }

        let badge (item: SideNavNode<'destination>) =
            match item.badge with
            | Some content ->
                span {
                    for attribute in hiddenWhenCollapsed do
                        attribute

                    _class "shrink-0"
                    content
                }
            | None -> empty

        let action (item: SideNavNode<'destination>) =
            match item.action with
            | Some content ->
                span {
                    for attribute in hiddenWhenCollapsed do
                        attribute

                    _attr ("data-fve-side-nav-action", "")
                    _class "shrink-0"
                    content
                }
            | None -> empty

        let itemClasses kind current =
            ComponentHtml.classes
                [ "flex min-h-[var(--fve-navigation-min-height)] min-w-0 flex-1 items-center gap-1 rounded-[var(--fve-radius-control)] pr-2.5 py-[var(--fve-navigation-padding-block)] text-sm leading-5 font-medium no-underline outline-none transition-colors focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]"
                  if kind = SideNavNodeKind.Group then "pl-2" else "pl-3"
                  if current then
                      "bg-[var(--fve-brand-subtle)] font-semibold text-[var(--fve-brand-text)]"
                  else
                      "text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] hover:text-[var(--fve-text)] " ]

        let rec renderItem path (item: SideNavNode<'destination>) =
            let itemId = item.id |> Option.defaultValue (config.id + "-item-" + path)

            let current =
                match config.current, item.destination with
                | Some current, Some destination -> destination = current
                | _ ->
                    config.current.IsNone
                    && ComponentElement.value "aria-current" item.attributes = Some "page"

            li {
                if item.kind = SideNavNodeKind.Section then
                    _class "mt-5 first:mt-0"

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

                                if current then
                                    _ariaCurrent "page"

                                _class (
                                    ComponentHtml.classes
                                        [ itemClasses item.kind current; ComponentContent.classes item.attributes ]
                                )

                                for attribute in
                                    ComponentHtml.safeAttributes
                                        [ "id"; "href"; "aria-current"; "aria-label"; "title"; "class" ]
                                        item.attributes do
                                    attribute

                                leading item
                                label item
                                badge item
                            }
                        | None ->
                            span {
                                _ariaDisabled true
                                _ariaLabel item.label
                                _title item.label

                                _class (
                                    ComponentHtml.classes
                                        [ itemClasses item.kind false
                                          "cursor-not-allowed opacity-50"
                                          ComponentContent.classes item.attributes ]
                                )

                                for attribute in
                                    ComponentHtml.safeAttributes
                                        [ "aria-disabled"; "aria-label"; "title"; "class"; "href" ]
                                        item.attributes do
                                    attribute

                                leading item
                                label item
                                badge item
                            }

                        action item
                    }
                | SideNavNodeKind.Section ->
                    section {
                        _id (itemId + "-section")

                        for attribute in ComponentHtml.safeAttributes [ "id" ] item.attributes do
                            attribute

                        h2 {
                            _id itemId

                            for attribute in hiddenWhenCollapsed do
                                attribute

                            _class "pl-3 pr-2.5 pb-2 text-xs font-medium text-[var(--fve-muted-text)]"
                            item.label
                        }

                        ul {
                            _id (itemId + "-children")
                            _ariaLabel item.label
                            _role "list"
                            _class "grid min-w-0 gap-px"

                            for index, child in List.indexed item.children do
                                renderItem (path + "-" + string index) child
                        }
                    }
                | SideNavNodeKind.Group ->
                    let expanded =
                        string (item.expanded || containsCurrent item) |> _.ToLowerInvariant()

                    let groupKey = item.id |> Option.defaultValue (path + ":" + item.label)
                    details {
                        if item.expanded || containsCurrent item then
                            _open true

                        _id (itemId + "-group")
                        _ariaLabel item.label
                        _attr ("data-fve-side-nav-group", groupKey)
                        _attr ("data-fve-side-nav-default-open", expanded)
                        // This adapter targets an explicitly caller-owned signal, never private widget state.
                        match item.expandedSignal with
                        | Some signal ->
                            _attr ("data-fve-side-nav-external", "")
                            // Core 2026.8.2 has no object/modifier overload; keyed HTML attributes lose caller-owned casing.
                            _attr ("data-signals__ifmissing", $"{{{signal}: {expanded}}}")
                            _dataAttr ("open", $"${signal}")
                            _dataOn ("fve-side-nav-toggle", $"${signal} = evt.detail.open")
                        | None -> ()
                        _dataPreserveAttr "open"
                        _class "group/navigation-item"

                        summary {
                            _id itemId
                            _ariaLabel ("Toggle " + item.label + " section")
                            _ariaControls (itemId + "-children")
                            _ariaExpanded (item.expanded || containsCurrent item)
                            _title item.label

                            _class (
                                itemClasses item.kind false
                                + " cursor-pointer list-none [&::-webkit-details-marker]:hidden "
                                + ComponentContent.classes item.attributes
                                + (if containsCurrent item then
                                       " font-semibold text-[var(--fve-text)]"
                                   else
                                       "")
                            )

                            for attribute in
                                ComponentHtml.safeAttributes
                                    [ "id"; "aria-label"; "aria-expanded"; "aria-controls"; "title"; "class" ]
                                    item.attributes do
                                attribute

                            span {
                                for attribute in hiddenWhenCollapsed do
                                    attribute

                                _ariaHidden true
                                _class "size-4 shrink-0 [[open]>summary>&]:rotate-90 [&>svg]:size-4"

                                raw
                                    """<svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M7.21 4.96a.75.75 0 0 1 1.06 0l4.51 4.51a.75.75 0 0 1 0 1.06l-4.51 4.51a.75.75 0 1 1-1.06-1.06L11.19 10 7.21 6.02a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                            }

                            leading item
                            label item
                            badge item
                            action item
                        }

                        ul {
                            _id (itemId + "-children")
                            _role "list"

                            for attribute in hiddenWhenCollapsed do
                                attribute

                            _class
                                "ml-[calc(1rem-0.5px)] mt-0.5 grid min-w-0 gap-px border-l border-[var(--fve-border)] pl-1"

                            for index, child in List.indexed item.children do
                                renderItem (path + "-" + string index) child
                        }
                    }
            }

        (Html.el "fve-side-nav") {
            _id config.id
            _role "complementary"
            if ComponentElement.value "aria-label" (config.attributes @ attributes) = None then
                _ariaLabel (config.label + " sidebar")
            _attr ("persistence-key", config.persistenceKey)

            _class (
                ComponentHtml.classes
                    [ className
                      ComponentContent.classes config.attributes
                      ComponentContent.classes attributes ]
            )

            match collapsedExpression with
            | Some expression -> _dataAttr ("data-fve-collapsed", $"{expression} ? 'true' : 'false'")
            | None -> ()

            for attribute in
                ComponentElement.attributes (config.attributes @ attributes)
                |> ComponentHtml.safeAttributes [ "id"; "class" ] do
                attribute

            match config.header with
            | Some header ->
                div {
                    _attr ("data-fve-side-nav-header", "true")

                    _class (
                        "shrink-0 border-b border-[var(--fve-border)] "
                        + ComponentContent.classes header.attributes
                    )

                    for attribute in
                        ComponentHtml.safeAttributes [ "class"; "data-fve-side-nav-header" ] header.attributes do
                        attribute

                    div {
                        _class "flex min-h-[calc(var(--fve-shell-bar-min-height)-1px)] items-center gap-3 px-4"

                        div {
                            _class "min-w-0 flex-1"

                            div {
                                for attribute in hiddenWhenCollapsed do
                                    attribute

                                header.content
                            }

                            match header.compactContent, collapsedExpression with
                            | Some content, Some expression ->
                                div {
                                    _dataClass ("hidden", $"!({expression})")
                                    _class "hidden"
                                    content
                                }
                            | _ -> ()
                        }

                        closeControl |> Option.defaultValue empty
                    }
                }
            | None -> ()

            match config.context with
            | Some context ->
                div {
                    for attribute in hiddenWhenCollapsed do
                        attribute

                    _attr ("data-fve-side-nav-context", "true")
                    _class "shrink-0 border-b border-[var(--fve-border)]"
                    context
                }
            | None -> ()

            nav {
                _ariaLabel config.label
                _attr ("data-fve-side-nav-content", "true")
                _class (
                    ComponentHtml.classes
                        [ "min-h-0 min-w-0 flex-1 overflow-x-hidden overflow-y-auto"
                          if config.content.IsSome then
                              "p-3"
                          config.content
                          |> Option.map (fun content -> ComponentContent.classes content.attributes)
                          |> Option.defaultValue "" ]
                )

                match config.content with
                | Some content ->
                    for attribute in
                        ComponentHtml.safeAttributes
                            [ "class"; "aria-label" ]
                            content.attributes do
                        attribute
                | None -> ()

                ul {
                    _role "list"
                    _class "grid min-w-0 gap-px"

                    for index, item in List.indexed (items config) do
                        renderItem (string index) item
                }
            }

            match config.footer with
            | Some footer ->
                div {
                    _attr ("data-fve-side-nav-footer", "true")
                    _class "shrink-0 border-t border-[var(--fve-border)]"

                    div {
                        for attribute in hiddenWhenCollapsed do
                            attribute

                        footer
                    }

                    match config.compactFooter, collapsedExpression with
                    | Some content, Some expression ->
                        div {
                            _dataClass ("hidden", $"!({expression})")
                            _class "hidden"
                            content
                        }
                    | _ -> ()
                }
            | None -> ()
        }

/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module internal SideNav =
    let create id label =
        if String.IsNullOrWhiteSpace id then
            invalidArg (nameof id) "A stable side-navigation ID is required."

        if String.IsNullOrWhiteSpace label then
            invalidArg (nameof label) "An accessible side-navigation label is required."

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
          compactFooter = None
          attributes = [] }

    /// Per-tab preference scope. Defaults to the stable navigation ID; use a distinct key
    /// for unrelated trees, or the same key for responsive presentations of one tree.
    let withPersistenceKey key (config: SideNavConfig<'destination>) =
        if String.IsNullOrWhiteSpace key then
            invalidArg (nameof key) "A navigation persistence key is required."

        { config with persistenceKey = key }

    let withHeader header (config: SideNavConfig<'destination>) = { config with header = Some header }
    let withContent content (config: SideNavConfig<'destination>) = { config with content = Some content }

    let withCurrent current (config: SideNavConfig<'destination>) =
        let rec contains item =
            item.destination = Some current || List.exists contains item.children

        let represented = SideNavView.items config |> List.exists contains

        if not represented then
            invalidArg (nameof current) "The current destination must exist in the side navigation."

        { config with current = Some current }

    let withWidth width (config: SideNavConfig<'destination>) = { config with width = width }

    let withContext (rows: HtmlElement list) (config: SideNavConfig<'destination>) =
        { config with
            context =
                if List.isEmpty rows then
                    None
                else
                    Some(
                        fragment {
                            for row in rows do
                                yield row
                        }
                    ) }

    let withMobileContext context (config: SideNavConfig<'destination>) =
        { config with
            mobileContext = Some context }

    let withFooter (rows: HtmlElement list) (config: SideNavConfig<'destination>) =
        { config with
            footer =
                if List.isEmpty rows then
                    None
                else
                    Some(
                        fragment {
                            for row in rows do
                                yield row
                        }
                    ) }

    let withCompactFooter footer (config: SideNavConfig<'destination>) =
        { config with
            compactFooter = Some footer }

    let render resolve config =
        SideNavView.render
            (ComponentHtml.classes
                [ "flex h-full min-h-0 flex-col border-r border-[var(--fve-border)] bg-[var(--fve-background)] text-[var(--fve-text)]"
                  SideNavView.standaloneWidthClass config ])
            []
            None
            None
            resolve
            config

type internal SideNavNodeElement(node: SideNavNode<string>) =
    inherit HtmlElement()
    member _.Node = node

    override _.Render _ =
        invalidOp "Side-navigation items belong inside fveSideNavContent, fveSideNavSection or fveSideNavGroup."

type internal SideNavPartElement(kind: string, attributes: HtmlAttribute list, children: HtmlElement list) =
    inherit HtmlElement()
    member _.Kind = kind
    member _.Attributes = attributes
    member _.Children = children

    override _.Render _ =
        invalidOp "Side-navigation parts belong inside fveSideNav."

type internal SideNavElement(config: SideNavConfig<string>) =
    inherit HtmlElement()
    member _.Config = config

    override _.Render output =
        (SideNav.render id config).Render output

/// <category>side-nav</category>
[<AutoOpen>]
module SideNavAttributes =
    type Html with
        static member _width(value: SideNavWidth) =
            Html._attr (
                "width",
                match value with
                | SideNavWidth.Narrow -> "narrow"
                | SideNavWidth.Standard -> "standard"
                | SideNavWidth.Wide -> "wide"
            )

        static member _expandedSignal(value: string) = Html._attr ("expanded-signal", value)

module internal SideNavElements =
    let configuration (element: HtmlElement) =
        match element with
        | :? SideNavElement as element -> element.Config
        | _ -> invalidArg "navigation" "Use fveSideNav."

    let part kind attributes children =
        if kind = "header" then
            ComponentElement.required "label" attributes |> ignore

        SideNavPartElement(kind, attributes, children) :> HtmlElement

    let private slot attributes children =
        if List.isEmpty attributes then
            ComponentContent.fragment children
        else
            ComponentElement.render "div" [] attributes children

    let decoration role attributes children =
        ComponentPart("SideNavDecoration", role, slot attributes children) :> HtmlElement

    let private nodes (children: HtmlElement list) =
        children
        |> List.choose (function
            | :? SideNavNodeElement as node -> Some node.Node
            | :? NoopElement -> None
            | _ -> invalidArg "children" "Use side-navigation items, groups or sections.")

    let private flag name attributes =
        match ComponentElement.attribute name attributes with
        | Some { Value = ValueNone } -> true
        | Some { Value = ValueSome value } -> value <> "false"
        | None -> false

    let node kind attributes children =
        let parts, body = ComponentParts.split "SideNavDecoration" children

        let label =
            if kind = SideNavNodeKind.Item then
                ComponentElement.value "label" attributes
                |> Option.defaultWith (fun () ->
                    ComponentContent.plainText "Rich side-navigation items without _label" body)
            else
                ComponentElement.required "label" attributes

        let destination =
            if flag "disabled" attributes || flag "aria-disabled" attributes then
                None
            else
                ComponentElement.value "href" attributes

        let node =
            SideNavNodes.create kind label destination (if kind = SideNavNodeKind.Item then [] else nodes body)

        let node =
            { node with
                id = ComponentElement.value "id" attributes
                body =
                    if kind = SideNavNodeKind.Item && not body.IsEmpty then
                        Some(ComponentContent.fragment body)
                    else
                        None
                leading = Map.tryFind "leading" parts
                badge = Map.tryFind "badge" parts
                action = Map.tryFind "action" parts
                expanded = ComponentElement.value "expanded" attributes = Some "true"
                attributes =
                    ComponentHtml.safeAttributes
                        [ "id"
                          "href"
                          "label"
                          "disabled"
                          "aria-disabled"
                          "expanded"
                          "expanded-signal" ]
                        attributes }

        let node =
            ComponentElement.value "expanded-signal" attributes
            |> Option.map (fun signal -> SideNavGroup.withExpandedSignal signal node)
            |> Option.defaultValue node

        SideNavNodeElement(node) :> HtmlElement

    let row attributes children =
        let parts, body = ComponentParts.split "SideNavDecoration" children

        let menus, contents =
            body
            |> List.partition (fun child ->
                match child with
                | :? DropdownMenuElement -> true
                | _ -> false)

        let label =
            ComponentElement.value "label" attributes
            |> Option.defaultWith (fun () ->
                ComponentContent.plainText "Rich side-navigation rows without _label" contents)

        let row =
            match ComponentElement.value "href" attributes, menus with
            | Some href, [] -> SideNavRow.link href label
            | None, [ menu ] -> SideNavRow.menu menu label
            | _ -> invalidArg "children" "A side-navigation row requires one native href or one fveDropdownMenu."

        { row with
            body =
                if contents.IsEmpty then
                    None
                else
                    Some(ComponentContent.fragment contents)
            leading = Map.tryFind "leading" parts
            trailing = Map.tryFind "trailing" parts
            description = ComponentElement.value "description" attributes
            current = ComponentElement.value "aria-current" attributes = Some "page"
            attributes = ComponentHtml.safeAttributes [ "label"; "description"; "href" ] attributes }
        |> SideNavRow.render id

    let render attributes (children: HtmlElement list) =
        let id = ComponentElement.required "id" attributes

        if id |> Seq.exists Char.IsWhiteSpace then
            invalidArg "id" "A stable side-navigation ID without whitespace is required."

        let parts =
            children
            |> List.choose (function
                | :? SideNavPartElement as part -> Some part
                | :? NoopElement -> None
                | _ -> invalidArg "children" "Use side-navigation header, content, context and footer parts.")

        let optional kind =
            match parts |> List.filter (fun part -> part.Kind = kind) with
            | [] -> None
            | [ part ] -> Some part
            | _ -> invalidArg "children" $"Side navigation accepts one {kind} part."

        let markup kind =
            optional kind |> Option.map (fun part -> slot part.Attributes part.Children)

        let content =
            optional "content"
            |> Option.map (fun part ->
                { SideNavContent.create (nodes part.Children) with
                    attributes = part.Attributes })

        let header =
            optional "header"
            |> Option.map (fun part ->
                let compact, body =
                    part.Children
                    |> List.partition (fun child ->
                        match child with
                        | :? SideNavPartElement as part -> part.Kind = "header-compact"
                        | _ -> false)

                let compact =
                    match compact with
                    | [] -> None
                    | [ (:? SideNavPartElement as part) ] -> Some(slot part.Attributes part.Children)
                    | _ -> invalidArg "children" "A navigation header accepts one compact part."

                let label = ComponentElement.required "label" part.Attributes
                let defaults = SideNavHeader.create label

                { defaults with
                    content =
                        if body.IsEmpty then
                            defaults.content
                        else
                            ComponentContent.fragment body
                    compactContent = compact
                    attributes = ComponentHtml.safeAttributes [ "label" ] part.Attributes })

        let config =
            { SideNav.create id (ComponentElement.required "aria-label" attributes) with
                persistenceKey = ComponentElement.value "persistence-key" attributes |> Option.defaultValue id
                width =
                    match ComponentElement.choice "width" "wide" [ "narrow"; "standard"; "wide" ] attributes with
                    | "narrow" -> SideNavWidth.Narrow
                    | "standard" -> SideNavWidth.Standard
                    | _ -> SideNavWidth.Wide
                content = content
                header = header
                context = markup "context"
                mobileContext = markup "mobile-context"
                footer = markup "footer"
                compactFooter = markup "compact-footer"
                attributes =
                    ComponentHtml.safeAttributes [ "id"; "aria-label"; "value"; "width"; "persistence-key" ] attributes }

        let config =
            ComponentElement.value "value" attributes
            |> Option.map (fun current -> SideNav.withCurrent current config)
            |> Option.defaultValue config

        SideNavElement(config) :> HtmlElement

/// <category>side-nav</category>
[<AutoOpen>]
module SideNavDsl =
    let fveSideNav = ComponentElementBuilder(SideNavElements.render)
    let fveSideNavHeader = ComponentElementBuilder(SideNavElements.part "header")

    let fveSideNavHeaderCompact =
        ComponentElementBuilder(SideNavElements.part "header-compact")

    let fveSideNavContent = ComponentElementBuilder(SideNavElements.part "content")
    let fveSideNavContext = ComponentElementBuilder(SideNavElements.part "context")

    let fveSideNavMobileContext =
        ComponentElementBuilder(SideNavElements.part "mobile-context")

    let fveSideNavFooter = ComponentElementBuilder(SideNavElements.part "footer")

    let fveSideNavCompactFooter =
        ComponentElementBuilder(SideNavElements.part "compact-footer")

    let fveSideNavItem =
        ComponentElementBuilder(SideNavElements.node SideNavNodeKind.Item)

    let fveSideNavGroup =
        ComponentElementBuilder(SideNavElements.node SideNavNodeKind.Group)

    let fveSideNavSection =
        ComponentElementBuilder(SideNavElements.node SideNavNodeKind.Section)

    let fveSideNavRow = ComponentElementBuilder(SideNavElements.row)

    let fveSideNavLeading =
        ComponentElementBuilder(SideNavElements.decoration "leading")

    let fveSideNavBadge = ComponentElementBuilder(SideNavElements.decoration "badge")
    let fveSideNavAction = ComponentElementBuilder(SideNavElements.decoration "action")

    let fveSideNavTrailing =
        ComponentElementBuilder(SideNavElements.decoration "trailing")

/// <summary>Optional first-paint restoration for native SideNav disclosures.</summary>
/// <category>side-nav</category>
[<RequireQualifiedAccess>]
module SideNavPreferences =
    /// Include once in the head, before the application module. The host owns its CSP nonce.
    /// Saved choices are per-tab; native server defaults remain when storage is unavailable.
    let assetsWithNonce nonce =
        script {
            match nonce with Some value -> _attr("nonce", value) | None -> ()
            raw """
(() => {
  const selector = 'details[data-fve-side-nav-group]:not([data-fve-side-nav-external])';
  const initialized = new WeakSet();
  const key = (host, group) => 'fve-side-nav:' + host.getAttribute('persistence-key') + ':group:' + group.dataset.fveSideNavGroup;
  const synchronize = group => group.querySelector(':scope > summary')?.setAttribute('aria-expanded', String(group.open));
  const restore = group => {
    const host = group.closest('fve-side-nav');
    if (!host || !group.matches(selector)) return;
    if (!initialized.has(group)) {
      initialized.add(group);
      try {
        const saved = sessionStorage.getItem(key(host, group));
        if (saved === 'true' || saved === 'false') group.open = saved === 'true';
      } catch {}
      // The module adopts this native state, including interaction before it loads.
      group.dataset.fveSideNavPrepared = key(host, group);
    }
    synchronize(group);
  };
  const observer = new MutationObserver(records => {
    for (const record of records) for (const node of record.addedNodes) {
      if (!(node instanceof Element)) continue;
      if (node.matches(selector)) restore(node);
      node.querySelectorAll(selector).forEach(restore);
      const group = node.closest(selector);
      if (group) restore(group);
    }
  });
  const toggle = event => {
    const group = event.target;
    if (!group.matches?.(selector)) return;
    restore(group);
    const host = group.closest('fve-side-nav');
    if (host?.getClientRects().length) try { sessionStorage.setItem(key(host, group), String(group.open)); } catch {}
    synchronize(group);
  };
  observer.observe(document.documentElement, {childList: true, subtree: true});
  document.addEventListener('toggle', toggle, true);
  document.addEventListener('DOMContentLoaded', () => {
    observer.disconnect();
    document.removeEventListener('toggle', toggle, true);
  }, {once: true});
})();
"""
        }

    /// Initialize first-paint disclosure restoration when no nonce is required.
    let assets () = assetsWithNonce None
