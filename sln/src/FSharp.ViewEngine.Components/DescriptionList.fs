namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

/// <category>description-list</category>
[<RequireQualifiedAccess>]
type DescriptionListColumns = One | Two | Three | Four

/// <category>description-list</category>
[<AutoOpen>]
module DescriptionListAttributes =
    type Html with
        static member _columns(value:DescriptionListColumns) =
            Html._attr("columns", match value with DescriptionListColumns.One -> "1" | DescriptionListColumns.Two -> "2" | DescriptionListColumns.Three -> "3" | DescriptionListColumns.Four -> "4")

module internal DescriptionList =
    let render attributes children =
        let columns =
            match ComponentElement.choice "columns" "2" ["1"; "2"; "3"; "4"] attributes with
            | "1" -> "grid-cols-1"
            | "3" -> "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
            | "4" -> "grid-cols-1 sm:grid-cols-2 xl:grid-cols-4"
            | _ -> "grid-cols-1 sm:grid-cols-2"
        ComponentElement.render "dl" [_class ("grid gap-x-6 gap-y-4 " + columns)] (ComponentHtml.safeAttributes ["columns"; "role"] attributes) children

    let item attributes (children:HtmlElement list) =
        let label = ComponentElement.required "label" attributes
        ComponentElement.render "div" [_class "min-w-0"] (ComponentHtml.safeAttributes ["label"; "role"] attributes) [
            dt { _class "break-words text-xs font-medium uppercase tracking-wide text-[var(--fve-muted-text)]"; label }
            dd { _class "mt-1 break-words text-sm font-normal text-[var(--fve-text)]"; children } ]

    let description attributes children =
        ComponentElement.render "p" [_class "mt-1 text-xs font-normal text-[var(--fve-muted-text)]"] attributes children

/// <category>description-list</category>
[<AutoOpen>]
module DescriptionListDsl =
    let fveDescriptionList = ComponentElementBuilder(DescriptionList.render)
    let fveDescriptionListItem = ComponentElementBuilder(DescriptionList.item)
    let fveDescriptionListDescription = ComponentElementBuilder(DescriptionList.description)
