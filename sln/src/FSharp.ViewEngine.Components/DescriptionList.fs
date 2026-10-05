namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>description-list</category>
[<RequireQualifiedAccess>]
type DescriptionListColumns =
    | One
    | Two
    | Three
    | Four

/// <category>description-list</category>
[<NoEquality; NoComparison>]
type DescriptionListItem =
    private
        { label:string
          value:HtmlElement
          description:string option
          attributes:HtmlAttribute list }

/// <category>description-list</category>
[<NoEquality; NoComparison>]
type DescriptionListConfig =
    private
        { fields:DescriptionListItem list
          columns:DescriptionListColumns
          attributes:HtmlAttribute list }

/// <category>description-list</category>
[<RequireQualifiedAccess>]
module DescriptionListItem =
    let create label value =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A detail-field label is required."
        { label = label
          value = value
          description = None
          attributes = [] }

    let text (label:string) (value:string) = create label (span { value })
    let status (label:string) (status:HtmlElement) = create label status
    let withDescription (description:string) (config:DescriptionListItem) = { config with description = Some description }
    let withAttributes attributes (config:DescriptionListItem) = { config with attributes = attributes }

    let internal render (config:DescriptionListItem) =
        div {
            _class "min-w-0"
            for attribute in ComponentHtml.safeAttributes [ "class"; "role" ] config.attributes do attribute
            dt { _class "break-words text-xs font-medium uppercase tracking-wide text-[var(--fve-muted-text)]"; config.label }
            dd {
                _class "mt-1 break-words text-sm font-normal text-[var(--fve-text)]"
                config.value
                match config.description with
                | Some description -> p { _class "mt-1 text-xs font-normal text-[var(--fve-muted-text)]"; description }
                | None -> ()
            }
        }

/// <category>description-list</category>
[<RequireQualifiedAccess>]
module DescriptionList =
    let create fields =
        if List.isEmpty fields then invalidArg (nameof fields) "At least one detail field is required."
        { fields = fields
          columns = DescriptionListColumns.Two
          attributes = [] }

    let withColumns columns (config:DescriptionListConfig) = { config with columns = columns }
    let withAttributes attributes (config:DescriptionListConfig) = { config with attributes = attributes }

    let render (config:DescriptionListConfig) =
        let columns =
            match config.columns with
            | DescriptionListColumns.One -> "grid-cols-1"
            | DescriptionListColumns.Two -> "grid-cols-1 sm:grid-cols-2"
            | DescriptionListColumns.Three -> "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
            | DescriptionListColumns.Four -> "grid-cols-1 sm:grid-cols-2 xl:grid-cols-4"

        dl {
            _class (ComponentHtml.classes [ "grid gap-x-6 gap-y-4"; columns ])
            for attribute in ComponentHtml.safeAttributes [ "class"; "role" ] config.attributes do attribute
            for field in config.fields do DescriptionListItem.render field
        }
