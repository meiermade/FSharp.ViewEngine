namespace FSharp.ViewEngine.Components.Application

open FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type CollectionConfig<'destination> =
    private
        { title:string
          titleVisible:bool
          description:string option
          actions:ActionClusterConfig<'destination> option
          toolbar:HtmlElement option
          content:HtmlElement }

[<RequireQualifiedAccess>]
module Collection =
    let create title content =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A collection title is required."
        { title = title; titleVisible = true; description = None; actions = None; toolbar = None; content = content }

    let withVisuallyHiddenTitle (config:CollectionConfig<'destination>) = { config with titleVisible = false }
    let withDescription description (config:CollectionConfig<'destination>) = { config with description = Some description }
    let withActions actions (config:CollectionConfig<'destination>) = { config with actions = Some actions }
    let withToolbar toolbar (config:CollectionConfig<'destination>) = { config with toolbar = Some toolbar }

    let render resolve config =
        section {
            _class "grid min-w-0 gap-4"
            header {
                _class (if config.titleVisible || config.actions.IsSome then "@container flex flex-wrap items-start justify-between gap-4" else "sr-only")
                div {
                    _class "min-w-0"
                    h2 { _class (if config.titleVisible then "text-xl font-semibold tracking-tight text-[var(--fve-text)]" else "sr-only"); config.title }
                    match config.description with
                    | Some description -> p { _class (if config.titleVisible then "mt-1 text-sm text-[var(--fve-muted-text)]" else "sr-only"); description }
                    | None -> ()
                }
                match config.actions with
                | Some actions -> ActionCluster.render resolve actions
                | None -> ()
            }
            config.toolbar |> Option.defaultValue empty
            config.content
        }

[<NoEquality; NoComparison>]
type DetailConfig<'destination> =
    private
        { title:string
          titleVisible:bool
          metadata:HtmlElement option
          actions:ActionClusterConfig<'destination> option
          sections:HtmlElement list }

[<RequireQualifiedAccess>]
module Detail =
    let create title sections =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A detail title is required."
        { title = title; titleVisible = true; metadata = None; actions = None; sections = sections }

    let withVisuallyHiddenTitle (config:DetailConfig<'destination>) = { config with titleVisible = false }
    let withMetadata metadata (config:DetailConfig<'destination>) = { config with metadata = Some metadata }
    let withActions actions (config:DetailConfig<'destination>) = { config with actions = Some actions }

    let render resolve config =
        article {
            _class "grid min-w-0 gap-6"
            header {
                _class (if config.titleVisible || config.metadata.IsSome || config.actions.IsSome then "@container flex flex-wrap items-start justify-between gap-4" else "sr-only")
                div {
                    _class "min-w-0"
                    h2 { _class (if config.titleVisible then "text-xl font-semibold tracking-tight text-[var(--fve-text)]" else "sr-only"); config.title }
                    config.metadata |> Option.defaultValue empty
                }
                match config.actions with
                | Some actions -> ActionCluster.render resolve actions
                | None -> ()
            }
            for detailSection in config.sections do detailSection
        }
