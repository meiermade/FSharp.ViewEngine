namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<RequireQualifiedAccess>]
type SectionHeadingLevel =
    | H2
    | H3

[<NoEquality; NoComparison>]
type SectionHeaderConfig<'destination> =
    private
        { title:string
          description:string option
          level:SectionHeadingLevel
          actions:ActionClusterConfig<'destination> option
          divider:bool }

[<RequireQualifiedAccess>]
module SectionHeader =
    let create title =
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A section title is required."
        { title = title; description = None; level = SectionHeadingLevel.H2; actions = None; divider = false }

    let withDescription description (config:SectionHeaderConfig<'destination>) =
        if String.IsNullOrWhiteSpace description then invalidArg (nameof description) "A section description cannot be empty."
        { config with description = Some description }

    let withLevel level (config:SectionHeaderConfig<'destination>) = { config with level = level }
    let withActions actions (config:SectionHeaderConfig<'destination>) = { config with actions = Some actions }

    let withDivider (config:SectionHeaderConfig<'destination>) = { config with divider = true }

    let render resolve config =
        header {
            _attr ("data-fve-section-header", "true")
            _class (ComponentHtml.classes [ "@container flex flex-wrap items-start justify-between gap-4"; if config.divider then "border-b border-[var(--fve-border)] pb-2" ])
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
            | Some actions -> ActionCluster.render resolve actions
            | None -> ()
        }

[<RequireQualifiedAccess>]
type SectionSurface =
    | Plain
    | Panel

[<NoEquality; NoComparison>]
type SectionConfig<'destination> =
    private
        { header:SectionHeaderConfig<'destination> option
          label:string
          content:HtmlElement
          surface:SectionSurface }

[<RequireQualifiedAccess>]
module Section =
    let create (header:SectionHeaderConfig<'destination>) content =
        { header = Some header; label = header.title; content = content; surface = SectionSurface.Plain }

    let withoutHeader label content : SectionConfig<'destination> =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An unheaded section requires an accessible label."
        { header = None; label = label; content = content; surface = SectionSurface.Plain }
    let withLabel label (config:SectionConfig<'destination>) =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A section label requires accessible text."
        { config with label = label }

    let withSurface surface (config:SectionConfig<'destination>) = { config with surface = surface }

    let render resolve config =
        section {
            _ariaLabel config.label
            _class (
                match config.surface with
                | SectionSurface.Plain -> "grid gap-4"
                | SectionSurface.Panel -> "grid gap-4 rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-5 ring-1 ring-[var(--fve-border)]")
            match config.header with
            | Some header -> SectionHeader.render resolve header
            | None -> ()
            config.content
        }
