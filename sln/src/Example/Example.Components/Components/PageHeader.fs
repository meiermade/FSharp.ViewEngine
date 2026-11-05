namespace Example.Components

open System
open FSharp.ViewEngine
open type Html

module internal PageHeader =
    let actions attributes children =
        if String.IsNullOrWhiteSpace (Render.toString (ComponentContent.fragment children)) then empty
        else ComponentPart("PageHeader", "actions", ComponentElement.render "div" [_class "flex shrink-0 flex-wrap items-center gap-2"] attributes children) :> HtmlElement

    let render attributes children =
        let title = ComponentElement.required "label" attributes
        let description = ComponentElement.attribute "description" attributes |> Option.map (fun _ -> ComponentElement.required "description" attributes)
        let parts, content = ComponentParts.split "PageHeader" children
        ComponentElement.render "header"
            [_attr("data-fve-page-header", "true"); _class "fve-control-small @container flex w-full flex-wrap items-start justify-between gap-4 px-[16px] py-4 sm:px-6 lg:px-8"]
            (ComponentHtml.safeAttributes ["label"; "description"] attributes) [
                yield div {
                    _class "min-w-0 flex-[1_1_12rem]"
                    h1 { _class "break-words text-3xl font-semibold tracking-tight text-[var(--fve-text)]"; title }
                    match description with
                    | Some description -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                    | None -> ()
                    content
                }
                match Map.tryFind "actions" parts with Some actions -> yield actions | None -> () ]

/// <category>page-header</category>
[<AutoOpen>]
module PageHeaderDsl =
    let fvePageHeader = ComponentElementBuilder(PageHeader.render)
    let fvePageHeaderActions = ComponentElementBuilder(PageHeader.actions)
