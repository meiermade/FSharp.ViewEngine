namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type SideNavigationItem<'destination> =
    private
        { label:string
          destination:'destination
          leading:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module SideNavigationItem =
    let create destination label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A side-navigation item label is required."
        { label = label
          destination = destination
          leading = None
          attributes = [] }

    let withLeading leading (item:SideNavigationItem<'destination>) = { item with leading = Some leading }
    let withAttributes attributes (item:SideNavigationItem<'destination>) = { item with attributes = attributes }

[<NoEquality; NoComparison>]
type SideNavigationSection<'destination> =
    private
        { label:string option
          items:SideNavigationItem<'destination> list }

[<RequireQualifiedAccess>]
module SideNavigationSection =
    let private requireItems (items:SideNavigationItem<'destination> list) =
        if List.isEmpty items then invalidArg (nameof items) "A side-navigation section requires at least one item."
        items

    let ungrouped items =
        { label = None; items = requireItems items }

    let group label items =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A side-navigation section label is required."
        { label = Some label; items = requireItems items }

[<NoEquality; NoComparison>]
type SideNavigationConfig<'destination when 'destination:equality> =
    private
        { id:string
          label:string
          productName:string
          current:'destination
          sections:SideNavigationSection<'destination> list
          mark:HtmlElement option
          context:HtmlElement option
          footer:HtmlElement option }

module internal SideNavigationView =
    let productName (config:SideNavigationConfig<'destination>) = config.productName
    let id (config:SideNavigationConfig<'destination>) = config.id

    let render
        (className:string)
        (attributes:HtmlAttribute list)
        (closeControl:HtmlElement option)
        (resolve:'destination -> string)
        (config:SideNavigationConfig<'destination>) = 
        let renderItem (item:SideNavigationItem<'destination>) =
            let current = item.destination = config.current
            li {
                a {
                    _href (resolve item.destination)
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
            }

        aside {
            _id config.id
            _class className
            for attribute in ComponentHtml.safeAttributes [ "id"; "class" ] attributes do attribute
            div {
                _class "flex min-h-16 shrink-0 items-center gap-3 border-b border-[var(--fve-border)] px-4"
                match config.mark with
                | Some mark -> span { _ariaHidden true; _class "flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white"; mark }
                | None -> ()
                strong { _class "min-w-0 flex-1 truncate text-base font-semibold text-[var(--fve-text)]"; config.productName }
                closeControl |> Option.defaultValue empty
            }
            match config.context with
            | Some context -> div { _class "shrink-0 border-b border-[var(--fve-border)] px-4 py-3"; context }
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
            | Some footer -> div { _class "shrink-0 border-t border-[var(--fve-border)] p-3"; footer }
            | None -> ()
        }

[<RequireQualifiedAccess>]
module SideNavigation =
    let create id label productName current (sections:SideNavigationSection<'destination> list) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable side-navigation ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible side-navigation label is required."
        if String.IsNullOrWhiteSpace productName then invalidArg (nameof productName) "A product name is required."
        if List.isEmpty sections then invalidArg (nameof sections) "At least one side-navigation section is required."

        let items = sections |> List.collect _.items
        let destinations = items |> List.map _.destination
        if destinations.Length <> (destinations |> List.distinct |> List.length) then
            invalidArg (nameof sections) "Side-navigation destinations must be unique."
        if items |> List.exists (fun item -> item.destination = current) |> not then
            invalidArg (nameof current) "The current destination must exist in the side navigation."

        { id = id
          label = label
          productName = productName
          current = current
          sections = sections
          mark = None
          context = None
          footer = None }

    let withMark mark (config:SideNavigationConfig<'destination>) = { config with mark = Some mark }
    let withContext context (config:SideNavigationConfig<'destination>) = { config with context = Some context }
    let withFooter footer (config:SideNavigationConfig<'destination>) = { config with footer = Some footer }

    let render resolve config =
        SideNavigationView.render
            "flex h-full w-64 flex-col border-r border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)]"
            []
            None
            resolve
            config
