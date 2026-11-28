namespace Example.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>table</category>
[<RequireQualifiedAccess>]
type Surface = Panel | Plain

/// <category>table</category>
[<RequireQualifiedAccess>]
type MobileLayout = Scroll | Records

/// <category>table</category>
[<RequireQualifiedAccess>]
type Mobile = Field | Primary | Summary | Actions

/// <summary>Typed attributes for Table presentation.</summary>
/// <category>table</category>
[<AutoOpen>]
module TableAttributes =
    type Html with
        static member _surface(value:Surface) =
            Html._attr("surface", match value with Surface.Panel -> "panel" | Surface.Plain -> "plain")
        static member _mobileLayout(value:MobileLayout) =
            Html._attr("mobile-layout", match value with MobileLayout.Scroll -> "scroll" | MobileLayout.Records -> "records")
        static member _mobile(value:Mobile) =
            Html._attr("data-mobile-cell", match value with Mobile.Field -> "field" | Mobile.Primary -> "primary" | Mobile.Summary -> "summary" | Mobile.Actions -> "actions")
        static member _scrollRows(value:bool) = Html._attr("scroll-rows", if value then "true" else "false")
        static member _hoverRows(value:bool) = Html._attr("hover-rows", if value then "true" else "false")

[<NoEquality; NoComparison>]
type internal TableElementConfig = private { children:HtmlElement list; attributes:HtmlAttribute list }

module internal TableElements =
    let create children = { children = children; attributes = [] }
    let attributes attributes config = { config with attributes = ComponentElement.attributes attributes |> ComponentHtml.outerSignals }
    let attribute name config = ComponentElement.attribute name config.attributes
    let value name config = ComponentElement.value name config.attributes
    let render config =
        if value "aria-label" config |> Option.forall String.IsNullOrWhiteSpace then
            invalidArg "attributes" "An accessible Table label is required."
        for name, allowed in ["density", ["compact"; "comfortable"]; "surface", ["plain"; "panel"]; "mobile-layout", ["scroll"; "records"]; "scroll-rows", ["true"; "false"]; "hover-rows", ["true"; "false"]] do
            match attribute name config with
            | None -> ()
            | Some { Value = ValueSome value } when List.contains value allowed -> ()
            | _ -> invalidArg "attributes" $"Unsupported Table {name}."
        if attribute "role" config |> Option.isSome then invalidArg "attributes" "Table supplies native table semantics; do not assign a role to its host."
        (Html.el "fve-table") {
            _attr("data-fve-table", "true")
            for attribute in ComponentHtml.safeAttributes ["data-fve-table"] config.attributes do attribute
            config.children
        }
    let part part config =
        template {
            _attr("data-fve-table-part", part)
            for attribute in config.attributes do attribute
            config.children
        }
    let native tag marker config =
        (Html.el tag) {
            _class (ComponentHtml.classes [marker; ComponentContent.classes config.attributes])
            if tag = "th" && (value "scope" config).IsNone then _scope "col"
            for attribute in ComponentHtml.safeAttributes ["class"; "label"] config.attributes do attribute
            match value "label" config with
            | Some label when tag = "td" || tag = "th" -> span { _ariaHidden true; _class "fve-table-mobile-label hidden"; label }
            | _ -> ()
            config.children
        }

/// <summary>Ordinary attributes and native table parts. The application owns records, selection, hierarchy, sorting and loading policy.</summary>
/// <category>table</category>
type TableBuilder internal () =
    inherit ComponentElementBuilder(fun attributes children -> TableElements.create children |> TableElements.attributes attributes |> TableElements.render)

/// <summary>Inert source for a native table section, activated by Assets/table.js.</summary>
/// <category>table</category>
type TableSectionBuilder internal (part:string) =
    inherit ComponentElementBuilder(fun attributes children -> TableElements.create children |> TableElements.attributes attributes |> TableElements.part part)

/// <summary>A native tr, th or td, also usable in an ordinary Datastar row replacement.</summary>
/// <category>table</category>
type TablePartBuilder internal (tag:string, marker:string) =
    inherit ComponentElementBuilder(fun attributes children -> TableElements.create children |> TableElements.attributes attributes |> TableElements.native tag marker)

/// <category>table</category>
[<AutoOpen>]
module TableDsl =
    let fveTable = TableBuilder()
    let fveTableHeader = TableSectionBuilder("head")
    let fveTableBody = TableSectionBuilder("body")
    let fveTableFooter = TableSectionBuilder("foot")
    let fveTableCaption = TableSectionBuilder("caption")
    let fveTableEmpty = TableSectionBuilder("empty")
    let fveTableRow = TablePartBuilder("tr", "fve-table-row")
    let fveTableHead = TablePartBuilder("th", "fve-table-cell")
    let fveTableCell = TablePartBuilder("td", "fve-table-cell")
