namespace FSharp.ViewEngine.Components.Templates

open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components
open FSharp.ViewEngine
open type Html

/// <category>page</category>
[<RequireQualifiedAccess>]
type PageWidth =
    | Reading
    | Wide
    | Full

/// <category>page</category>
[<RequireQualifiedAccess>]
type PageBodyLayout =
    | Padded
    | FullBleed
    | Canvas

[<NoEquality; NoComparison>]
type private PageLocalNavigation =
    | SectionNavigation of HtmlElement
    | PageTabs of HtmlElement

/// <category>page</category>
[<NoEquality; NoComparison>]
type PageConfig =
    private
        { topBar:PageTopBarConfig
          header:PageHeaderConfig
          content:HtmlElement
          localNavigation:PageLocalNavigation option
          width:PageWidth
          bodyLayout:PageBodyLayout }

/// <category>page</category>
[<RequireQualifiedAccess>]
module Page =
    let create header content =
        { topBar = PageTopBar.create ()
          header = header
          content = content
          localNavigation = None
          width = PageWidth.Wide
          bodyLayout = PageBodyLayout.Padded }

    let withTopBar topBar (config:PageConfig) = { config with topBar = topBar }

    let withSectionNavigation navigation (config:PageConfig) =
        { config with localNavigation = Some(SectionNavigation navigation) }

    let withTabs tabs (config:PageConfig) =
        { config with localNavigation = Some(PageTabs tabs) }

    let withWidth width (config:PageConfig) = { config with width = width }
    let withBodyLayout layout (config:PageConfig) =
        { config with bodyLayout = layout; width = if layout = PageBodyLayout.Canvas then PageWidth.Full else config.width }

    let private widthClasses = function
        | PageWidth.Reading -> "max-w-4xl"
        | PageWidth.Wide -> "max-w-7xl"
        | PageWidth.Full -> "max-w-none"

    let render config =
        let width = widthClasses config.width
        div {
            _class "flex h-full min-h-0 flex-col bg-[var(--fve-background)] text-[var(--fve-text)]"
            if config.topBar.content.IsSome || not config.topBar.attributes.IsEmpty then
                PageTopBar.render config.topBar
            if config.bodyLayout = PageBodyLayout.Canvas then
                PageHeaderView.render config.header
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
                    PageHeaderView.render config.header
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
