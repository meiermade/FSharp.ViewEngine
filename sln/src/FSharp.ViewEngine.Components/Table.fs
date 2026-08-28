namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<NoEquality; NoComparison>]
type TableColumn<'row> =
    private
        { heading:string
          cell:'row -> HtmlElement
          headerClass:string option
          cellClass:string option
          rowHeader:bool }

[<NoEquality; NoComparison>]
type TableConfig<'row> =
    private
        { caption:string
          columns:TableColumn<'row> list
          rows:'row list
          emptyState:HtmlElement
          captionVisible:bool
          density:Density
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Table =
    let column heading cell =
        if String.IsNullOrWhiteSpace heading then invalidArg (nameof heading) "A column heading is required."
        { heading = heading; cell = cell; headerClass = None; cellClass = None; rowHeader = false }

    let alignEnd (column:TableColumn<'row>) =
        { column with headerClass = Some "text-right"; cellClass = Some "text-right" }

    let asRowHeader (column:TableColumn<'row>) = { column with rowHeader = true }

    let create caption columns rows =
        if String.IsNullOrWhiteSpace caption then invalidArg (nameof caption) "A table caption is required."
        if List.isEmpty columns then invalidArg (nameof columns) "At least one table column is required."
        { caption = caption
          columns = columns
          rows = rows
          emptyState = div { _class "p-6 text-center text-sm text-[var(--fve-muted-text)]"; "No records" }
          captionVisible = false
          density = Density.Comfortable
          attributes = [] }

    let withEmptyState emptyState (config:TableConfig<'row>) = { config with emptyState = emptyState }
    let withVisibleCaption (config:TableConfig<'row>) = { config with captionVisible = true }
    let withDensity density (config:TableConfig<'row>) = { config with density = density }
    let withAttributes attributes (config:TableConfig<'row>) = { config with attributes = attributes }

    let render config =
        let cellSpacing =
            match config.density with
            | Density.Compact -> "px-3 py-2"
            | Density.Comfortable -> "px-4 py-3"

        div {
            _role "region"
            _ariaLabel config.caption
            _tabindex 0
            _class "overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] ring-1 ring-[var(--fve-border)]"
            if List.isEmpty config.rows then
                config.emptyState
            else
                table {
                    _class "min-w-full whitespace-nowrap divide-y divide-[var(--fve-border)] text-left text-sm"
                    for attribute in ComponentHtml.safeAttributes [ "class"; "role"; "aria-label"; "aria-labelledby"; "aria-describedby" ] config.attributes do attribute
                    caption {
                        _class (
                            if config.captionVisible then
                                "px-4 py-3 text-left text-sm font-semibold text-[var(--fve-text)]"
                            else
                                "sr-only")
                        config.caption
                    }
                    thead {
                        _class "bg-[var(--fve-surface-subtle)] text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"
                        tr {
                            for column in config.columns do
                                th {
                                    _scope "col"
                                    _class (ComponentHtml.classes [ cellSpacing; column.headerClass |> Option.defaultValue "" ])
                                    column.heading
                                }
                        }
                    }
                    tbody {
                        _class "divide-y divide-[var(--fve-border)] text-[var(--fve-text)]"
                        for row in config.rows do
                            tr {
                                _class "hover:bg-[var(--fve-surface-hover)]"
                                for column in config.columns do
                                    if column.rowHeader then
                                        th {
                                            _scope "row"
                                            _class (ComponentHtml.classes [ cellSpacing; "font-medium"; column.cellClass |> Option.defaultValue "" ])
                                            column.cell row
                                        }
                                    else
                                        td {
                                            _class (ComponentHtml.classes [ cellSpacing; column.cellClass |> Option.defaultValue "" ])
                                            column.cell row
                                        }
                            }
                    }
                }
        }
