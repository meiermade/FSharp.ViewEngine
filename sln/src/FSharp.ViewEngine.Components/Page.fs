namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type PageHeaderConfig<'destination> =
    private
        { title:string
          breadcrumbs:BreadcrumbsConfig<'destination>
          actions:HtmlElement option }

module internal PageHeaderView =
    let render contentWidth resolve (config:PageHeaderConfig<'destination>) =
        header {
            _class "shrink-0 border-b border-[var(--fve-border)] bg-[var(--fve-surface)]"
            div {
                _class (ComponentHtml.classes [ "mx-auto flex min-h-16 w-full flex-wrap items-center justify-between gap-x-4 gap-y-3 px-4 py-3 sm:px-6"; contentWidth ])
                h1 { _class "sr-only"; config.title }
                div { _class "w-full min-w-0 sm:w-auto sm:flex-1"; Breadcrumbs.render resolve config.breadcrumbs }
                match config.actions with
                | Some actions -> div { _class "flex w-full min-w-0 flex-wrap items-center gap-2 sm:w-auto sm:shrink-0 sm:justify-end"; actions }
                | None -> ()
            }
        }

[<RequireQualifiedAccess>]
module PageHeader =
    let create title breadcrumbs =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A page title is required."
        { title = title; breadcrumbs = breadcrumbs; actions = None }

    let withActions actions (config:PageHeaderConfig<'destination>) = { config with actions = Some actions }

    let render resolve config =
        PageHeaderView.render "max-w-7xl" resolve config

[<RequireQualifiedAccess>]
type PageWidth =
    | Reading
    | Wide
    | Full

[<RequireQualifiedAccess>]
type PageBodyLayout =
    | Padded
    | FullBleed

[<NoEquality; NoComparison>]
type private PageLocalNavigation =
    | SectionNavigation of HtmlElement
    | PageTabs of HtmlElement

[<NoEquality; NoComparison>]
type PageConfig<'destination> =
    private
        { header:PageHeaderConfig<'destination>
          content:HtmlElement
          localNavigation:PageLocalNavigation option
          width:PageWidth
          bodyLayout:PageBodyLayout }

[<RequireQualifiedAccess>]
module Page =
    let create header content =
        { header = header
          content = content
          localNavigation = None
          width = PageWidth.Wide
          bodyLayout = PageBodyLayout.Padded }

    let withSectionNavigation navigation config =
        { config with localNavigation = Some(SectionNavigation navigation) }

    let withTabs tabs config =
        { config with localNavigation = Some(PageTabs tabs) }

    let withWidth width config = { config with width = width }
    let withBodyLayout layout config = { config with bodyLayout = layout }

    let private widthClasses = function
        | PageWidth.Reading -> "max-w-4xl"
        | PageWidth.Wide -> "max-w-7xl"
        | PageWidth.Full -> "max-w-none"

    let render resolve config =
        let width = widthClasses config.width
        div {
            _class "flex h-full min-h-0 flex-col bg-[var(--fve-page)] text-[var(--fve-text)]"
            PageHeaderView.render width resolve config.header
            div {
                _attr ("data-fve-page-scroll", "true")
                _class "min-h-0 flex-1 overflow-y-auto"
                div {
                    _class (
                        ComponentHtml.classes [
                            "mx-auto w-full"
                            if config.localNavigation.IsSome then "grid gap-6"
                            width
                            match config.bodyLayout with
                            | PageBodyLayout.Padded -> "p-4 sm:p-6 lg:p-8"
                            | PageBodyLayout.FullBleed -> "" ])
                    match config.localNavigation with
                    | Some(SectionNavigation sectionNavigation) -> sectionNavigation
                    | Some(PageTabs tabs) -> tabs
                    | None -> ()
                    config.content
                }
            }
        }
