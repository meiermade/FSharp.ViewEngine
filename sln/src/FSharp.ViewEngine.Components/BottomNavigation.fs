namespace FSharp.ViewEngine.Components.Application

open FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type BottomNavigationItem<'destination> =
    private
        { label:string
          destination:'destination
          leading:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module BottomNavigationItem =
    let create destination label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A bottom-navigation item label is required."
        { label = label
          destination = destination
          leading = None
          attributes = [] }

    let withLeading leading (item:BottomNavigationItem<'destination>) = { item with leading = Some leading }
    let withAttributes attributes (item:BottomNavigationItem<'destination>) = { item with attributes = attributes }

[<NoEquality; NoComparison>]
type BottomNavigationConfig<'destination when 'destination:equality> =
    private
        { id:string
          label:string
          items:BottomNavigationItem<'destination> list
          current:'destination option
          attributes:HtmlAttribute list }

module internal BottomNavigationView =
    let id (config:BottomNavigationConfig<'destination>) = config.id
    let destinations (config:BottomNavigationConfig<'destination>) = config.items |> List.map _.destination

    let render (className:string) resolve (config:BottomNavigationConfig<'destination>) =
        nav {
            _id config.id
            _ariaLabel config.label
            _class className
            for attribute in ComponentHtml.safeAttributes [ "id"; "aria-label"; "class" ] config.attributes do attribute
            ul {
                _role "list"
                _class "flex min-h-[calc(var(--fve-control-min-height)+1rem)] items-stretch"
                for item in config.items do
                    let isCurrent = config.current = Some item.destination
                    li {
                        _class "flex min-w-0 flex-1"
                        a {
                            _href (resolve item.destination)
                            if isCurrent then _ariaCurrent "page"
                            _class (
                                ComponentHtml.classes [
                                    "flex min-w-0 flex-1 flex-col items-center justify-center gap-1 px-2 py-2 text-center text-xs font-semibold outline-none transition-colors focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--fve-brand-ring)]"
                                    if isCurrent then "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)]"
                                    else "text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] active:bg-[var(--fve-surface-active)]" ])
                            for attribute in ComponentHtml.safeAttributes [ "href"; "aria-current"; "class" ] item.attributes do attribute
                            match item.leading with
                            | Some leading -> span { _ariaHidden true; _class "flex size-5 shrink-0 items-center justify-center"; leading }
                            | None -> ()
                            span { _class "max-w-full truncate"; item.label }
                        }
                    }
            }
        }

[<RequireQualifiedAccess>]
module BottomNavigation =
    let create id label (items:BottomNavigationItem<'destination> list) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable bottom-navigation ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible bottom-navigation label is required."
        if List.isEmpty items then invalidArg (nameof items) "Bottom navigation requires at least one destination."
        let destinations = items |> List.map _.destination
        if destinations.Length <> (destinations |> List.distinct |> List.length) then
            invalidArg (nameof items) "Bottom-navigation destinations must be unique."
        { id = id
          label = label
          items = items
          current = None
          attributes = [] }

    let withCurrent current (config:BottomNavigationConfig<'destination>) =
        if not (BottomNavigationView.destinations config |> List.contains current) then
            invalidArg (nameof current) "The current destination must exist in the bottom navigation."
        { config with current = Some current }

    let withAttributes attributes (config:BottomNavigationConfig<'destination>) = { config with attributes = attributes }

    let render resolve config =
        BottomNavigationView.render
            "border-t border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)]"
            resolve
            config
