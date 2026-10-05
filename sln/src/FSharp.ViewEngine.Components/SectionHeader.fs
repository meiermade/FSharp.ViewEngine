namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>section-header</category>
[<RequireQualifiedAccess>]
type SectionHeadingLevel =
    | H2
    | H3

/// <category>section-header</category>
[<NoEquality; NoComparison>]
type SectionHeaderConfig =
    private
        { title:string
          description:string option
          level:SectionHeadingLevel
          actions:HtmlElement option
          divider:bool }

/// <category>section-header</category>
[<RequireQualifiedAccess>]
module SectionHeader =
    let create title =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A section title is required."
        { title = title; description = None; level = SectionHeadingLevel.H2; actions = None; divider = false }

    let withDescription description (config:SectionHeaderConfig) =
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "A section description cannot be empty."
        { config with description = Some description }

    let withLevel level (config:SectionHeaderConfig) = { config with level = level }
    let withActions actions (config:SectionHeaderConfig) = { config with actions = Some actions }

    let withDivider (config:SectionHeaderConfig) = { config with divider = true }

    let internal title (config:SectionHeaderConfig) = config.title

    let render config =
        header {
            _attr ("data-fve-section-header", "true")
            _class (ComponentHtml.classes [ "fve-control-small @container flex flex-wrap items-start justify-between gap-4"; if config.divider then "border-b border-[var(--fve-border)] pb-2" ])
            div {
                _class "min-w-0"
                match config.level with
                | SectionHeadingLevel.H2 -> h2 { _class "text-lg font-semibold text-[var(--fve-text)]"; config.title }
                | SectionHeadingLevel.H3 -> h3 { _class "text-base font-semibold text-[var(--fve-text)]"; config.title }
                match config.description with
                | Some description -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                | None -> ()
            }
            match config.actions with
            | Some actions -> actions
            | None -> ()
        }
