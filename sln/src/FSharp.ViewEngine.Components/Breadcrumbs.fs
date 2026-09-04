namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type BreadcrumbItem<'destination> =
    private
        { label:string
          destination:'destination }

[<RequireQualifiedAccess>]
module BreadcrumbItem =
    let create destination label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A breadcrumb label is required."
        { label = label; destination = destination }

[<NoEquality; NoComparison>]
type BreadcrumbsConfig<'destination> =
    private
        { id:string
          label:string
          items:BreadcrumbItem<'destination> list }

[<RequireQualifiedAccess>]
module Breadcrumbs =
    let create id label (items:BreadcrumbItem<'destination> list) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable breadcrumbs ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible breadcrumbs label is required."
        if List.isEmpty items then invalidArg (nameof items) "At least one breadcrumb item is required."
        { id = id; label = label; items = items }

    let private separator =
        span {
            _ariaHidden true
            _class "flex size-4 shrink-0 items-center justify-center text-[var(--fve-muted-text)]"
            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 1 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
        }

    let private overflowIcon =
        span {
            _ariaHidden true
            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M3.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM8.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM13.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0Z"/></svg>"""
        }

    let render resolve (config:BreadcrumbsConfig<'destination>) =
        let currentIndex = config.items.Length - 1
        let hiddenItems = config.items |> List.take currentIndex

        nav {
            _id config.id
            _ariaLabel config.label
            _class "min-w-0"
            ol {
                _role "list"
                _class "flex min-w-0 items-center gap-1 text-sm text-[var(--fve-muted-text)]"
                if hiddenItems.IsEmpty |> not then
                    li {
                        _class "flex shrink-0 sm:hidden"
                        DropdownMenu.create
                            $"{config.id}-overflow"
                            "Show hidden breadcrumbs"
                            (hiddenItems |> List.map (fun item -> MenuItem.link item.destination item.label))
                        |> DropdownMenu.withAlignment MenuAlignment.Start
                        |> DropdownMenu.withTriggerContent overflowIcon
                        |> DropdownMenu.render resolve
                    }
                for index, item in config.items |> List.indexed do
                    let current = index = currentIndex
                    let compacted = index < currentIndex
                    li {
                        _class (ComponentHtml.classes [ "min-w-0 items-center gap-1"; if compacted then "hidden sm:flex" else "flex" ])
                        if index > 0 then separator
                        if current then
                            span {
                                _ariaCurrent "page"
                                _class "block min-w-0 truncate font-semibold text-[var(--fve-text)]"
                                item.label
                            }
                        else
                            a {
                                _href (resolve item.destination)
                                _class "block min-w-0 truncate rounded-[var(--fve-radius-control)] px-1 py-1 outline-none hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                                item.label
                            }
                    }
            }
        }
