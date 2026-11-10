namespace FSharp.ViewEngine.Components.Templates

open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components
open System
open FSharp.ViewEngine
open type Html

/// <exclude />
[<RequireQualifiedAccess>]
type SectionSurface =
    | Plain
    | Panel

/// <exclude />
[<NoEquality; NoComparison>]
type SectionConfig =
    private
        { header:HtmlElement option
          label:string
          content:HtmlElement
          surface:SectionSurface }

/// <exclude />
[<RequireQualifiedAccess>]
module Section =
    let create (header:HtmlElement) content =
        { header = Some header; label = SectionHeader.title header; content = content; surface = SectionSurface.Plain }

    let withoutHeader label content : SectionConfig =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An unheaded section requires an accessible label."
        { header = None; label = label; content = content; surface = SectionSurface.Plain }
    let withLabel label (config:SectionConfig) =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A section label requires accessible text."
        { config with label = label }

    let withSurface surface (config:SectionConfig) = { config with surface = surface }

    let render config =
        section {
            _ariaLabel config.label
            _class (
                match config.surface with
                | SectionSurface.Plain -> "grid gap-4"
                | SectionSurface.Panel -> "grid gap-4 rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-5 ring-1 ring-[var(--fve-border)]")
            match config.header with
            | Some header -> header
            | None -> ()
            config.content
        }
