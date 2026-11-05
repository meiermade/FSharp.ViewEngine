namespace Example.Components

open System
open FSharp.ViewEngine
open type Html

module internal PageTopBar =
    let part name attributes children =
        if String.IsNullOrWhiteSpace (Render.toString (ComponentContent.fragment children)) then empty
        else
            let classes =
                if name = "brand" then "hidden w-[var(--fve-shell-sidebar-width,15rem)] shrink-0 items-center border-r border-[var(--fve-border)] px-4 @3xl/fve-shell:flex"
                else "flex shrink-0 items-center gap-2 whitespace-nowrap"
            ComponentPart("PageTopBar", name, ComponentElement.render "div" [_attr("data-fve-page-top-bar-" + name, "true"); _class classes] attributes children) :> HtmlElement

    let render attributes children =
        let parts, content = ComponentParts.split "PageTopBar" children
        ComponentElement.render "header"
            [_attr("data-fve-page-top-bar", "true"); _class "fve-control-small flex w-full shrink-0 border-b border-[var(--fve-border)] bg-[var(--fve-background)]"] attributes [
                match Map.tryFind "brand" parts with Some brand -> yield brand | None -> ()
                yield div {
                    _class "flex min-h-[calc(var(--fve-shell-bar-min-height)-1px)] min-w-0 flex-1 flex-wrap items-center gap-3 px-[16px] py-1 sm:px-6 lg:px-8"
                    div { _class "min-w-0 flex-[1_1_12rem]"; content }
                    match Map.tryFind "actions" parts with Some actions -> actions | None -> ()
                } ]

/// <category>page-top-bar</category>
[<AutoOpen>]
module PageTopBarDsl =
    let fvePageTopBar = ComponentElementBuilder(PageTopBar.render)
    let fvePageTopBarBrand = ComponentElementBuilder(PageTopBar.part "brand")
    let fvePageTopBarActions = ComponentElementBuilder(PageTopBar.part "actions")
