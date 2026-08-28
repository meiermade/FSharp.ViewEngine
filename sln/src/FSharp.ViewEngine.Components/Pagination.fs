namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type PaginationItem<'destination> =
    private
    | PageLink of page:int * destination:'destination
    | CurrentPage of page:int
    | Gap

[<NoEquality; NoComparison>]
type PaginationConfig<'destination> =
    private
        { label:string
          items:PaginationItem<'destination> list
          previous:'destination option
          next:'destination option
          summary:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module PaginationItem =
    let private requirePositive page =
        if page < 1 then invalidArg (nameof page) "A page number must be positive."
        page

    let link page destination = PageLink(requirePositive page, destination)
    let current page = CurrentPage(requirePositive page)
    let gap<'destination> : PaginationItem<'destination> = Gap

[<RequireQualifiedAccess>]
module Pagination =
    let create label items =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible pagination label is required."
        if List.isEmpty items then invalidArg (nameof items) "At least one pagination item is required."

        let currentPages =
            items
            |> List.choose (function | CurrentPage page -> Some page | _ -> None)

        if currentPages.Length <> 1 then invalidArg (nameof items) "Pagination requires exactly one current page."

        let numberedPages =
            items
            |> List.choose (function | PageLink(page, _) | CurrentPage page -> Some page | Gap -> None)

        if (numberedPages |> Set.ofList |> Set.count) <> numberedPages.Length then
            invalidArg (nameof items) "Pagination page numbers must be unique."

        { label = label
          items = items
          previous = None
          next = None
          summary = None
          attributes = [] }

    let withPrevious destination (config:PaginationConfig<'destination>) = { config with previous = Some destination }
    let withNext destination (config:PaginationConfig<'destination>) = { config with next = Some destination }
    let withSummary summary (config:PaginationConfig<'destination>) = { config with summary = Some summary }
    let withAttributes attributes (config:PaginationConfig<'destination>) = { config with attributes = attributes }

    let private edgeLink (resolve:'destination -> string) (labelText:string) (destination:'destination option) : HtmlElement =
        match destination with
        | Some target ->
            a {
                _href (resolve target)
                _class "inline-flex min-h-9 items-center rounded-[var(--fve-radius-control)] px-3 text-sm font-medium text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                labelText
            }
        | None ->
            span {
                _ariaDisabled true
                _class "inline-flex min-h-9 cursor-default items-center rounded-[var(--fve-radius-control)] px-3 text-sm font-medium text-[var(--fve-muted-text)] opacity-50 ring-1 ring-inset ring-[var(--fve-border)]"
                labelText
            }

    let render (resolve:'destination -> string) (config:PaginationConfig<'destination>) =
        nav {
            _ariaLabel config.label
            _class "flex flex-wrap items-center justify-between gap-3"
            for attribute in ComponentHtml.safeAttributes [ "class"; "role"; "aria-label" ] config.attributes do attribute
            match config.summary with
            | Some summary -> div { _class "min-w-0 text-sm text-[var(--fve-muted-text)]"; summary }
            | None -> ()
            div {
                _class "flex flex-wrap items-center gap-1"
                edgeLink resolve "Previous" config.previous
                ol {
                    _class "flex flex-wrap items-center gap-1"
                    for item in config.items do
                        li {
                            match item with
                            | PageLink(page, destination) ->
                                a {
                                    _href (resolve destination)
                                    _ariaLabel $"Page {page}"
                                    _class "inline-flex size-9 items-center justify-center rounded-[var(--fve-radius-control)] text-sm font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                                    string page
                                }
                            | CurrentPage page ->
                                span {
                                    _ariaCurrent "page"
                                    _ariaLabel $"Page {page}, current page"
                                    _class "inline-flex size-9 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-subtle)] text-sm font-semibold text-[var(--fve-brand-text)] ring-1 ring-inset ring-[var(--fve-brand-ring)]"
                                    string page
                                }
                            | Gap ->
                                span {
                                    _ariaHidden "true"
                                    _class "inline-flex size-9 items-center justify-center text-[var(--fve-muted-text)]"
                                    "…"
                                }
                        }
                }
                edgeLink resolve "Next" config.next
            }
        }
