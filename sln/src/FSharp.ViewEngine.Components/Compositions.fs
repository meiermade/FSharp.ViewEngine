namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<NoEquality; NoComparison>]
type CollectionConfig =
    private
        { title:string
          description:string option
          actions:HtmlElement option
          toolbar:HtmlElement option
          content:HtmlElement }

[<RequireQualifiedAccess>]
module Collection =
    let create title content =
        { title = title; description = None; actions = None; toolbar = None; content = content }

    let withDescription description (config:CollectionConfig) = { config with description = Some description }
    let withActions actions (config:CollectionConfig) = { config with actions = Some actions }
    let withToolbar toolbar (config:CollectionConfig) = { config with toolbar = Some toolbar }

    let render config =
        section {
            _class "grid gap-6"
            header {
                _class "flex flex-wrap items-start justify-between gap-4"
                div {
                    h1 { _class "text-2xl font-semibold tracking-tight text-[var(--fve-text)]"; config.title }
                    match config.description with
                    | Some description -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                    | None -> ()
                }
                config.actions |> Option.defaultValue empty
            }
            config.toolbar |> Option.defaultValue empty
            config.content
        }

[<NoEquality; NoComparison>]
type DetailConfig =
    private
        { title:string
          metadata:HtmlElement option
          actions:HtmlElement option
          sections:HtmlElement list }

[<RequireQualifiedAccess>]
module Detail =
    let create title sections = { title = title; metadata = None; actions = None; sections = sections }
    let withMetadata metadata (config:DetailConfig) = { config with metadata = Some metadata }
    let withActions actions (config:DetailConfig) = { config with actions = Some actions }

    let render config =
        article {
            _class "grid gap-6"
            header {
                _class "flex flex-wrap items-start justify-between gap-4 border-b border-[var(--fve-border)] pb-5"
                div {
                    h1 { _class "text-2xl font-semibold tracking-tight text-[var(--fve-text)]"; config.title }
                    config.metadata |> Option.defaultValue empty
                }
                config.actions |> Option.defaultValue empty
            }
            for detailSection in config.sections do
                section { _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-5 ring-1 ring-[var(--fve-border)]"; detailSection }
        }
