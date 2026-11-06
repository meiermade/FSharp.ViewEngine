namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

type internal BreadcrumbItemElement(attributes: HtmlAttribute list, label: string, children: HtmlElement list) =
    inherit HtmlElement()
    member _.Attributes = attributes
    member _.Label = label
    member _.Children = children
    member _.Href = ComponentElement.value "href" attributes
    override _.Render _ = invalidOp "fveBreadcrumbItem belongs inside fveBreadcrumbs."

module internal Breadcrumbs =
    let item attributes children =
        let label =
            match ComponentElement.value "label" attributes with
            | Some label when not (String.IsNullOrWhiteSpace label) -> label
            | Some _ -> invalidArg "label" "A breadcrumb label is required."
            | None -> ComponentContent.plainText "fveBreadcrumbItem without _label" children
        if String.IsNullOrWhiteSpace label then invalidArg "children" "A breadcrumb label is required."
        let children = if children.IsEmpty then [ Html.text label ] else children
        BreadcrumbItemElement(ComponentHtml.safeAttributes [ "label" ] attributes, label, children) :> HtmlElement

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

    let render attributes (children: HtmlElement list) =
        let id = ComponentElement.required "id" attributes
        let label = ComponentElement.required "aria-label" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "id" "A stable breadcrumbs ID without whitespace is required."
        if ComponentElement.attribute "max-visible-items" attributes |> Option.isSome then
            invalidArg "attributes" "Breadcrumbs fits the available width; max-visible-items is retired."
        let items =
            children |> List.choose (function
                | :? BreadcrumbItemElement as item -> Some item
                | :? NoopElement -> None
                | _ -> invalidArg "children" "Breadcrumbs accepts fveBreadcrumbItem children.")
        if items.IsEmpty then invalidArg "children" "At least one breadcrumb item is required."
        let currentIndex = items.Length - 1
        let ancestors = items |> List.take currentIndex
        (Html.el "fve-breadcrumbs") {
            _id (id + "-host")
            _class "block min-w-0 w-full"
            ComponentElement.render "nav"
                [ _id id; _ariaLabel label; _class "min-w-0 w-full" ]
                attributes
                [ ol {
                    _role "list"
                    _class "-ml-1 flex min-w-0 flex-nowrap items-center gap-1 overflow-x-auto text-sm text-[var(--fve-muted-text)]"
                    for index, item in items |> List.indexed do
                        if index = 1 then
                            li {
                                _attr ("data-fve-breadcrumb-overflow", "")
                                _style "display:none"
                                _class "flex shrink-0 items-center gap-1"
                                span { _attr ("data-fve-breadcrumb-overflow-separator", ""); separator }
                                fveDropdownMenu {
                                    _id (id + "-overflow")
                                    _ariaLabel "Show hidden breadcrumbs"
                                    _alignment DropdownMenuAlignment.Start
                                    fveDropdownMenuTrigger {
                                        fveButton {
                                            _iconOnly true
                                            _variant ButtonVariant.Ghost
                                            _class "shadow-none"
                                            overflowIcon
                                        }
                                    }
                                    for ancestor in ancestors do
                                        fveDropdownMenuItem {
                                            yield! ComponentHtml.safeAttributes [ "id"; "role"; "tabindex"; "aria-current"; "class"; "style" ] ancestor.Attributes
                                            match ancestor.Href with
                                            | Some _ -> ()
                                            | None -> _disabled true
                                            ancestor.Label
                                        }
                                }
                            }
                        li {
                            _attr ("data-fve-breadcrumb-item", string index)
                            _class "flex min-w-0 shrink-0 items-center gap-1"
                            if index > 0 then separator
                            if index = currentIndex then
                                ComponentElement.render "span"
                                    [ _ariaCurrent "page"; _tabindex -1; _title item.Label
                                      _class "block min-w-0 truncate px-1 py-1 font-semibold text-[var(--fve-text)]" ]
                                    (ComponentHtml.safeAttributes [ "href"; "aria-current" ] item.Attributes)
                                    item.Children
                            else
                                ComponentElement.render (if item.Href.IsSome then "a" else "span")
                                    [ _title item.Label; _tabindex (if item.Href.IsSome then 0 else -1)
                                      _class "block rounded-[var(--fve-radius-control)] px-1 py-1 outline-none hover:text-[var(--fve-text)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]" ]
                                    item.Attributes item.Children
                        }
                } ]
        }

/// <category>breadcrumbs</category>
[<AutoOpen>]
module BreadcrumbsDsl =
    let fveBreadcrumbs = ComponentElementBuilder(Breadcrumbs.render)
    let fveBreadcrumbItem = ComponentElementBuilder(Breadcrumbs.item)
