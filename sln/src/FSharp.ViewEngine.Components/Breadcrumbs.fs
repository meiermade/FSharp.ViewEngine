namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>breadcrumbs</category>
[<NoEquality; NoComparison>]
type BreadcrumbItem<'destination> =
    private
        { label:string
          destination:'destination option }

/// <category>breadcrumbs</category>
[<RequireQualifiedAccess>]
module BreadcrumbItem =
    let create destination label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A breadcrumb label is required."
        { label = label; destination = Some destination }

    /// A hierarchy label without a destination, such as a non-page navigation group.
    let unlinked label : BreadcrumbItem<'destination> =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A breadcrumb label is required."
        { label = label; destination = None }

/// <category>breadcrumbs</category>
[<NoEquality; NoComparison>]
type BreadcrumbsConfig<'destination> =
    private
        { id:string
          label:string
          maxVisibleItems:int
          items:BreadcrumbItem<'destination> list }

/// <remarks>
/// withMaxVisibleItems counts path items, not the overflow trigger.
/// The default is three; choose at least two to retain the root and current page.
/// </remarks>
/// <category>breadcrumbs</category>
[<RequireQualifiedAccess>]
module Breadcrumbs =
    let create id label (items:BreadcrumbItem<'destination> list) =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable breadcrumbs ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible breadcrumbs label is required."
        if List.isEmpty items then invalidArg (nameof items) "At least one breadcrumb item is required."
        { id = id; label = label; maxVisibleItems = 3; items = items }

    /// Visible path items on wider screens, excluding the overflow trigger. Keeps the root and most recent items.
    let withMaxVisibleItems count (config:BreadcrumbsConfig<'destination>) =
        if count < 2 then invalidArg (nameof count) "At least the root and current breadcrumb must remain visible."
        { config with maxVisibleItems = count }

    let private separator =
        span {
            _ariaHidden true
            _class "hidden size-4 shrink-0 items-center justify-center text-[var(--fve-muted-text)] sm:flex"
            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 1 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
        }

    let private overflowIcon =
        span {
            _ariaHidden true
            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M3.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM8.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM13.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0Z"/></svg>"""
        }

    let render resolve (config:BreadcrumbsConfig<'destination>) =
        let currentIndex = config.items.Length - 1
        let ancestors = config.items |> List.take currentIndex
        let hiddenCount = max 0 (config.items.Length - config.maxVisibleItems)
        let middleItems = config.items |> List.skip 1 |> List.truncate hiddenCount
        let overflow id items =
            DropdownMenu.create id "Show hidden breadcrumbs"
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon overflowIcon)
            |> DropdownMenu.withContent (items |> List.map (fun item ->
                match item.destination with
                | Some destination -> DropdownMenuItem.link destination item.label
                | None -> DropdownMenuItem.action "void 0" item.label |> DropdownMenuItem.disabled))
            |> DropdownMenu.withAlignment DropdownMenuAlignment.Start
            |> DropdownMenu.render resolve

        nav {
            _id config.id
            _ariaLabel config.label
            _class "@container min-w-0 w-full"
            ol {
                _role "list"
                _class "-ml-1 flex min-w-0 flex-nowrap items-center gap-1 text-sm text-[var(--fve-muted-text)]"
                if ancestors.IsEmpty |> not then
                    li {
                        _class "flex shrink-0 sm:hidden"
                        overflow $"{config.id}-overflow" ancestors
                    }
                for index, item in config.items |> List.indexed |> List.filter (fun (index, _) -> index = 0 || index > hiddenCount) do
                    if index > 0 && index = hiddenCount + 1 && hiddenCount > 0 then
                        li {
                            _class "hidden shrink-0 items-center gap-1 sm:flex"
                            separator
                            overflow $"{config.id}-middle-overflow" middleItems
                        }
                    let current = index = currentIndex
                    let compacted = index < currentIndex
                    li {
                        _class (
                            ComponentHtml.classes [
                                "min-w-0 items-center gap-1"
                                if compacted then "hidden sm:flex" else "flex flex-1" ])
                        if index > 0 then separator
                        if current then
                            span {
                                _ariaCurrent "page"
                                _class "block min-w-0 truncate px-1 py-1 font-semibold text-[var(--fve-text)]"
                                item.label
                            }
                        else
                            match item.destination with
                            | Some destination ->
                                a {
                                    _href (resolve destination)
                                    _class "block min-w-0 truncate rounded-[var(--fve-radius-control)] px-1 py-1 outline-none hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                                    item.label
                                }
                            | None -> span { _class "block min-w-0 truncate px-1 py-1"; item.label }
                    }
            }
        }
