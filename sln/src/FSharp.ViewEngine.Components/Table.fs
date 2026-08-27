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
          cellClass:string option }

[<NoEquality; NoComparison>]
type TableConfig<'row> =
    private
        { caption:string
          columns:TableColumn<'row> list
          rows:'row list
          emptyState:HtmlElement
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Table =
    let column heading cell =
        if String.IsNullOrWhiteSpace heading then invalidArg (nameof heading) "A column heading is required."
        { heading = heading; cell = cell; headerClass = None; cellClass = None }

    let alignEnd (column:TableColumn<'row>) =
        { column with headerClass = Some "text-right"; cellClass = Some "text-right" }

    let create caption columns rows =
        if String.IsNullOrWhiteSpace caption then invalidArg (nameof caption) "A table caption is required."
        if List.isEmpty columns then invalidArg (nameof columns) "At least one table column is required."
        { caption = caption
          columns = columns
          rows = rows
          emptyState = div { _class "p-6 text-center text-sm text-[var(--fve-muted-text)]"; "No records" }
          attributes = [] }

    let withEmptyState emptyState (config:TableConfig<'row>) = { config with emptyState = emptyState }
    let withAttributes attributes (config:TableConfig<'row>) = { config with attributes = attributes }

    let render config =
        div {
            _class "overflow-hidden rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] ring-1 ring-[var(--fve-border)]"
            if List.isEmpty config.rows then
                config.emptyState
            else
                table {
                    _class "min-w-full divide-y divide-[var(--fve-border)] text-left text-sm"
                    for attribute in ComponentHtml.safeAttributes [ "class" ] config.attributes do attribute
                    caption { _class "sr-only"; config.caption }
                    thead {
                        _class "bg-[var(--fve-surface-subtle)] text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"
                        tr {
                            for column in config.columns do
                                th {
                                    _scope "col"
                                    _class (ComponentHtml.classes [ "px-4 py-3"; column.headerClass |> Option.defaultValue "" ])
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
                                    td {
                                        _class (ComponentHtml.classes [ "px-4 py-3"; column.cellClass |> Option.defaultValue "" ])
                                        column.cell row
                                    }
                            }
                    }
                }
        }
