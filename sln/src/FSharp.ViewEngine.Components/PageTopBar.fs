namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>page-top-bar</category>
[<NoEquality; NoComparison>]
type PageTopBarConfig =
    private
        { content:HtmlElement option
          brand:HtmlElement option
          actions:HtmlElement option
          attributes:HtmlAttribute list }

/// <category>page-top-bar</category>
[<RequireQualifiedAccess>]
module PageTopBar =
    let create () = { content = None; brand = None; actions = None; attributes = [] }
    let withContent content (config:PageTopBarConfig) = { config with content = Some content }
    /// Sidebar-aligned branding in a full-width shell bar. The parent supplies @container/fve-shell;
    /// --fve-shell-sidebar-width defaults to 15rem and can match the owning sidebar's width.
    let withBrand brand (config:PageTopBarConfig) = { config with brand = Some brand }
    let withActions actions (config:PageTopBarConfig) = { config with actions = Some actions }
    let withAttributes attributes (config:PageTopBarConfig) = { config with attributes = attributes }

    let render config =
        header {
            _attr ("data-fve-page-top-bar", "true")
            _class "fve-control-small flex w-full shrink-0 border-b border-[var(--fve-border)] bg-[var(--fve-background)]"
            for attribute in ComponentHtml.safeAttributes [ "class"; "data-fve-page-top-bar" ] config.attributes do attribute
            match config.brand with
            | Some brand ->
                div {
                    _attr ("data-fve-page-top-bar-brand", "true")
                    _class "hidden w-[var(--fve-shell-sidebar-width,15rem)] shrink-0 items-center border-r border-[var(--fve-border)] px-4 @3xl/fve-shell:flex"
                    brand
                }
            | None -> ()
            div {
                _class "flex min-h-[calc(var(--fve-shell-bar-min-height)-1px)] min-w-0 flex-1 items-center gap-3 px-4 py-1 sm:px-6 lg:px-8"
                div { _class "min-w-0 flex-1"; config.content |> Option.defaultValue empty }
                match config.actions with
                | Some actions -> div { _attr ("data-fve-page-top-bar-actions", "true"); _class "flex shrink-0 items-center gap-2 whitespace-nowrap"; actions }
                | None -> ()
            }
        }
