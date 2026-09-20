namespace FSharp.ViewEngine.Components.Application

open FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type PageTopBarConfig =
    private
        { content:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module PageTopBar =
    let create () = { content = None; attributes = [] }
    let withContent content (config:PageTopBarConfig) = { config with content = Some content }
    let withAttributes attributes (config:PageTopBarConfig) = { config with attributes = attributes }

    let render config =
        header {
            _attr ("data-fve-page-top-bar", "true")
            _class "fve-control-small shrink-0 border-b border-[var(--fve-border)] bg-[var(--fve-surface)]"
            for attribute in ComponentHtml.safeAttributes [ "class"; "data-fve-page-top-bar" ] config.attributes do attribute
            div {
                _class "min-h-[var(--fve-shell-bar-min-height)] w-full"
                config.content |> Option.defaultValue empty
            }
        }

[<NoEquality; NoComparison>]
type PageHeaderConfig<'destination> =
    private
        { title:string
          subtitle:string option
          actions:ActionClusterConfig<'destination> option
          attributes:HtmlAttribute list }

module internal PageHeaderView =
    let render resolve (config:PageHeaderConfig<'destination>) =
        header {
            _attr ("data-fve-page-header", "true")
            _class "fve-control-small @container flex flex-wrap items-start justify-between gap-4 px-4 py-4 sm:px-6 lg:px-8"
            for attribute in ComponentHtml.safeAttributes [ "class"; "data-fve-page-header" ] config.attributes do attribute
            div {
                _class "min-w-0 flex-1"
                h1 { _class "text-xl font-semibold tracking-tight text-[var(--fve-text)]"; config.title }
                match config.subtitle with
                | Some subtitle -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; subtitle }
                | None -> ()
            }
            match config.actions with
            | Some actions -> ActionCluster.render resolve actions
            | None -> ()
        }

[<RequireQualifiedAccess>]
module PageHeader =
    let create title =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A page title is required."
        { title = title; subtitle = None; actions = None; attributes = [] }

    let withSubtitle subtitle (config:PageHeaderConfig<'destination>) =
        if String.IsNullOrWhiteSpace subtitle then invalidArg (nameof subtitle) "A page subtitle cannot be empty."
        { config with subtitle = Some subtitle }

    let withActions actions (config:PageHeaderConfig<'destination>) = { config with actions = Some actions }
    let withAttributes attributes (config:PageHeaderConfig<'destination>) = { config with attributes = attributes }

    let render resolve config =
        div { _class "w-full"; PageHeaderView.render resolve config }

[<RequireQualifiedAccess>]
type PageWidth =
    | Reading
    | Wide
    | Full

[<RequireQualifiedAccess>]
type PageBodyLayout =
    | Padded
    | FullBleed
    | Canvas

[<NoEquality; NoComparison>]
type private PageLocalNavigation =
    | SectionNavigation of HtmlElement
    | PageTabs of HtmlElement

[<NoEquality; NoComparison>]
type PageConfig<'destination> =
    private
        { topBar:PageTopBarConfig
          header:PageHeaderConfig<'destination>
          content:HtmlElement
          localNavigation:PageLocalNavigation option
          width:PageWidth
          bodyLayout:PageBodyLayout }

[<RequireQualifiedAccess>]
module Page =
    let create header content =
        { topBar = PageTopBar.create ()
          header = header
          content = content
          localNavigation = None
          width = PageWidth.Wide
          bodyLayout = PageBodyLayout.Padded }

    let withTopBar topBar (config:PageConfig<'destination>) = { config with topBar = topBar }

    let withSectionNavigation navigation (config:PageConfig<'destination>) =
        { config with localNavigation = Some(SectionNavigation navigation) }

    let withTabs tabs (config:PageConfig<'destination>) =
        { config with localNavigation = Some(PageTabs tabs) }

    let withWidth width (config:PageConfig<'destination>) = { config with width = width }
    let withBodyLayout layout (config:PageConfig<'destination>) =
        { config with bodyLayout = layout; width = if layout = PageBodyLayout.Canvas then PageWidth.Full else config.width }

    let private widthClasses = function
        | PageWidth.Reading -> "max-w-4xl"
        | PageWidth.Wide -> "max-w-7xl"
        | PageWidth.Full -> "max-w-none"

    let render resolve config =
        let width = widthClasses config.width
        div {
            _class "flex h-full min-h-0 flex-col bg-[var(--fve-page)] text-[var(--fve-text)]"
            PageTopBar.render config.topBar
            if config.bodyLayout = PageBodyLayout.Canvas then
                PageHeaderView.render resolve config.header
                match config.localNavigation with
                | Some(SectionNavigation navigation)
                | Some(PageTabs navigation) -> div { _class "shrink-0 px-4 sm:px-6 lg:px-8"; navigation }
                | None -> ()
                div {
                    _attr ("data-fve-page-canvas", "true")
                    _class "min-h-0 min-w-0 flex-1 overflow-hidden"
                    config.content
                }
            else div {
                _attr ("data-fve-page-scroll", "true")
                _class "min-h-0 flex-1 overflow-y-auto"
                div {
                    _class (ComponentHtml.classes [ "mx-auto w-full"; width ])
                    PageHeaderView.render resolve config.header
                    match config.localNavigation with
                    | Some(SectionNavigation sectionNavigation)
                    | Some(PageTabs sectionNavigation) ->
                        div { _class "px-4 sm:px-6 lg:px-8"; sectionNavigation }
                    | None -> ()
                    div {
                        _class (
                            match config.bodyLayout with
                            | PageBodyLayout.Padded -> "p-4 sm:p-6 lg:p-8"
                            | PageBodyLayout.FullBleed
                            | PageBodyLayout.Canvas -> "")
                        config.content
                    }
                }
            }
        }
