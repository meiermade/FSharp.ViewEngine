namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type SideNavHeaderConfig =
    private
        { label:string
          content:HtmlElement }

[<RequireQualifiedAccess>]
module SideNavHeader =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible side-navigation header label is required."
        { label = label; content = strong { _class "truncate text-base font-semibold text-[var(--fve-text)]"; label } }

    let withContent content (config:SideNavHeaderConfig) = { config with content = content }

[<NoEquality; NoComparison>]
type SideNavItem<'destination> =
    private
        { label:string
          destination:'destination option
          leading:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module SideNavItem =
    let private item label destination =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A side-navigation item label is required."
        { label = label
          destination = destination
          leading = None
          attributes = [] }

    let create destination label = item label (Some destination)
    let unavailable<'destination> label : SideNavItem<'destination> = item label None
    let withLeading leading (item:SideNavItem<'destination>) = { item with leading = Some leading }
    let withAttributes attributes (item:SideNavItem<'destination>) = { item with attributes = attributes }

[<NoEquality; NoComparison>]
type SideNavSection<'destination> =
    private
        { label:string option
          items:SideNavItem<'destination> list }

[<RequireQualifiedAccess>]
module SideNavSection =
    let private requireItems (items:SideNavItem<'destination> list) =
        if List.isEmpty items then invalidArg (nameof items) "A side-navigation section requires at least one item."
        items

    let ungrouped items = { label = None; items = requireItems items }

    let group label items =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A side-navigation section label is required."
        { label = Some label; items = requireItems items }

[<RequireQualifiedAccess>]
type SideNavWidth =
    | Narrow
    | Standard
    | Wide

[<RequireQualifiedAccess>]
type SideNavRegionLayout =
    | Padded
    | Flush

[<NoEquality; NoComparison>]
type SideNavConfig<'destination when 'destination:equality> =
    private
        { id:string
          label:string
          header:SideNavHeaderConfig
          current:'destination option
          sections:SideNavSection<'destination> list
          width:SideNavWidth
          context:HtmlElement option
          mobileContext:HtmlElement option
          contextLayout:SideNavRegionLayout
          footer:HtmlElement option
          footerLayout:SideNavRegionLayout }

module internal SideNavView =
    let id (config:SideNavConfig<'destination>) = config.id
    let label (config:SideNavConfig<'destination>) = config.label
    let headerLabel (config:SideNavConfig<'destination>) = config.header.label
    let current (config:SideNavConfig<'destination>) = config.current
    let destinations (config:SideNavConfig<'destination>) =
        config.sections |> List.collect _.items |> List.choose _.destination
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
        (resolve:'destination -> string)
        (config:SideNavConfig<'destination>) =
        let renderItem (item:SideNavItem<'destination>) =
            let current =
                match config.current, item.destination with
                | Some current, Some destination -> destination = current
                | _ -> false

            li {
                match item.destination with
                | Some destination ->
                    a {
                        _href (resolve destination)
                        if current then _ariaCurrent "page"
                        _class (
                            ComponentHtml.classes [
                                "flex min-h-[var(--fve-control-min-height)] items-center gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold outline-none transition-colors focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]"
                                if current then
                                    "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)]"
                                else
                                    "text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] active:bg-[var(--fve-surface-active)]" ])
                        for attribute in ComponentHtml.safeAttributes [ "href"; "aria-current"; "class" ] item.attributes do attribute
                        match item.leading with
                        | Some leading -> span { _ariaHidden true; _class "flex size-5 shrink-0 items-center justify-center"; leading }
                        | None -> ()
                        span { _class "min-w-0 truncate"; item.label }
                    }
                | None ->
                    span {
                        _ariaDisabled true
                        _class "flex min-h-[var(--fve-control-min-height)] cursor-not-allowed items-center gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-muted-text)] opacity-50"
                        for attribute in ComponentHtml.safeAttributes [ "aria-disabled"; "class"; "href" ] item.attributes do attribute
                        match item.leading with
                        | Some leading -> span { _ariaHidden true; _class "flex size-5 shrink-0 items-center justify-center"; leading }
                        | None -> ()
                        span { _class "min-w-0 truncate"; item.label }
                    }
            }

        aside {
            _id config.id
            _class className
            for attribute in ComponentHtml.safeAttributes [ "id"; "class" ] attributes do attribute
            div {
                _attr ("data-fve-side-nav-header", "true")
                _class "shrink-0 border-b border-[var(--fve-border)]"
                div {
                    _class "flex min-h-[var(--fve-shell-bar-min-height)] items-center gap-3 px-4"
                    div { _class "min-w-0 flex-1"; config.header.content }
                    closeControl |> Option.defaultValue empty
                }
            }
            match config.context with
            | Some context -> div { _class (ComponentHtml.classes [ "shrink-0 border-b border-[var(--fve-border)]"; regionClasses config.contextLayout ]); context }
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
                            h2 { _class "px-3 pb-2 text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"; label }
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
            | Some footer -> div { _class (ComponentHtml.classes [ "shrink-0 border-t border-[var(--fve-border)]"; regionClasses config.footerLayout ]); footer }
            | None -> ()
        }

[<RequireQualifiedAccess>]
module SideNav =
    let create id label header (sections:SideNavSection<'destination> list) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable side-navigation ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible side-navigation label is required."
        if List.isEmpty sections then invalidArg (nameof sections) "At least one side-navigation section is required."

        let destinations = sections |> List.collect _.items |> List.choose _.destination

        if destinations.Length <> (destinations |> List.distinct |> List.length) then
            invalidArg (nameof sections) "Side-navigation destinations must be unique."

        { id = id
          label = label
          header = header
          current = None
          sections = sections
          width = SideNavWidth.Wide
          context = None
          mobileContext = None
          contextLayout = SideNavRegionLayout.Padded
          footer = None
          footerLayout = SideNavRegionLayout.Padded }

    let withCurrent current (config:SideNavConfig<'destination>) =
        let represented =
            config.sections
            |> List.collect _.items
            |> List.exists (fun item -> item.destination = Some current)
        if not represented then invalidArg (nameof current) "The current destination must exist in the side navigation."
        { config with current = Some current }

    let withWidth width (config:SideNavConfig<'destination>) = { config with width = width }
    let withContext context (config:SideNavConfig<'destination>) = { config with context = Some context }
    let withMobileContext context (config:SideNavConfig<'destination>) = { config with mobileContext = Some context }
    let withContextLayout layout (config:SideNavConfig<'destination>) = { config with contextLayout = layout }
    let withFooter footer (config:SideNavConfig<'destination>) = { config with footer = Some footer }
    let withFooterLayout layout (config:SideNavConfig<'destination>) = { config with footerLayout = layout }

    let render resolve config =
        SideNavView.render
            (ComponentHtml.classes [ "flex h-full flex-col border-r border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)]"; SideNavView.standaloneWidthClass config ])
            []
            None
            resolve
            config
