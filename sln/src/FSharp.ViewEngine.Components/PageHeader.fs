namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>page-header</category>
[<NoEquality; NoComparison>]
type PageHeaderConfig =
    private
        { title:string
          subtitle:string option
          actions:HtmlElement option
          attributes:HtmlAttribute list }

module internal PageHeaderView =
    let render (config:PageHeaderConfig) =
        header {
            _attr ("data-fve-page-header", "true")
            _class "fve-control-small @container flex flex-wrap items-start justify-between gap-4 px-4 py-4 sm:px-6 lg:px-8"
            for attribute in ComponentHtml.safeAttributes [ "class"; "data-fve-page-header" ] config.attributes do attribute
            div {
                _class "min-w-0 flex-[1_1_12rem]"
                h1 { _class "break-words text-xl font-semibold tracking-tight text-[var(--fve-text)]"; config.title }
                match config.subtitle with
                | Some subtitle -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; subtitle }
                | None -> ()
            }
            match config.actions with
            | Some actions -> actions
            | None -> ()
        }

/// <category>page-header</category>
[<RequireQualifiedAccess>]
module PageHeader =
    let create title =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A page title is required."
        { title = title; subtitle = None; actions = None; attributes = [] }

    let withSubtitle subtitle (config:PageHeaderConfig) =
        if String.IsNullOrWhiteSpace subtitle then invalidArg (nameof subtitle) "A page subtitle cannot be empty."
        { config with subtitle = Some subtitle }

    let withActions actions (config:PageHeaderConfig) = { config with actions = Some actions }
    let withAttributes attributes (config:PageHeaderConfig) = { config with attributes = attributes }

    let render config =
        div { _class "w-full"; PageHeaderView.render config }
