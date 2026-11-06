namespace FSharp.ViewEngine.Components.Templates

open FSharp.ViewEngine.Components

open FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>collection</category>
[<NoEquality; NoComparison>]
type CollectionConfig =
    private
        { title:string
          titleVisible:bool
          description:string option
          actions:HtmlElement option
          toolbar:HtmlElement option
          content:HtmlElement }

/// <category>collection</category>
[<RequireQualifiedAccess>]
module Collection =
    let create title content =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A collection title is required."
        { title = title; titleVisible = true; description = None; actions = None; toolbar = None; content = content }

    let withVisuallyHiddenTitle (config:CollectionConfig) = { config with titleVisible = false }
    let withDescription description (config:CollectionConfig) = { config with description = Some description }
    let withActions actions (config:CollectionConfig) = { config with actions = Some actions }
    let withToolbar toolbar (config:CollectionConfig) = { config with toolbar = Some toolbar }

    let render config =
        section {
            _class "grid min-w-0 grid-cols-1 gap-4"
            header {
                _class (if config.titleVisible || config.actions.IsSome then "fve-control-small @container flex flex-wrap items-start justify-between gap-4" else "sr-only")
                div {
                    _class "min-w-0"
                    h2 { _class (if config.titleVisible then "text-xl font-semibold tracking-tight text-[var(--fve-text)]" else "sr-only"); config.title }
                    match config.description with
                    | Some description -> p { _class (if config.titleVisible then "mt-1 text-sm text-[var(--fve-muted-text)]" else "sr-only"); description }
                    | None -> ()
                }
                match config.actions with
                | Some actions -> actions
                | None -> ()
            }
            match config.toolbar with
            | Some toolbar -> div { _class "fve-control-small"; toolbar }
            | None -> ()
            config.content
        }

/// <category>detail</category>
[<NoEquality; NoComparison>]
type DetailConfig =
    private
        { title:string
          titleVisible:bool
          metadata:HtmlElement option
          actions:HtmlElement option
          sections:HtmlElement list }

/// <category>detail</category>
[<RequireQualifiedAccess>]
module Detail =
    let create title sections =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A detail title is required."
        { title = title; titleVisible = true; metadata = None; actions = None; sections = sections }

    let withVisuallyHiddenTitle (config:DetailConfig) = { config with titleVisible = false }
    let withMetadata metadata (config:DetailConfig) = { config with metadata = Some metadata }
    let withActions actions (config:DetailConfig) = { config with actions = Some actions }

    let render config =
        article {
            _class "grid min-w-0 grid-cols-1 gap-6"
            header {
                _class (if config.titleVisible || config.metadata.IsSome || config.actions.IsSome then "fve-control-small @container flex flex-wrap items-start justify-between gap-4" else "sr-only")
                div {
                    _class "min-w-0"
                    h2 { _class (if config.titleVisible then "text-xl font-semibold tracking-tight text-[var(--fve-text)]" else "sr-only"); config.title }
                    config.metadata |> Option.defaultValue empty
                }
                match config.actions with
                | Some actions -> actions
                | None -> ()
            }
            for detailSection in config.sections do detailSection
        }
