namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

/// <category>separator</category>
[<RequireQualifiedAccess>]
type SeparatorOrientation =
    | Horizontal
    | Vertical

/// <category>separator</category>
[<NoEquality; NoComparison>]
type SeparatorConfig =
    private
        { orientation:SeparatorOrientation
          isDecorative:bool
          attributes:HtmlAttribute list }

/// <category>separator</category>
[<RequireQualifiedAccess>]
module Separator =
    /// Creates a decorative horizontal separator.
    let create () =
        { orientation = SeparatorOrientation.Horizontal
          isDecorative = true
          attributes = [] }

    let withOrientation orientation (config:SeparatorConfig) = { config with orientation = orientation }
    let semantic (config:SeparatorConfig) = { config with isDecorative = false }
    let withAttributes attributes (config:SeparatorConfig) = { config with attributes = attributes }

    let render (config:SeparatorConfig) =
        let classes =
            match config.orientation with
            | SeparatorOrientation.Horizontal -> "h-px w-full shrink-0 bg-[var(--fve-border)]"
            | SeparatorOrientation.Vertical -> "h-full min-h-4 w-px shrink-0 self-stretch bg-[var(--fve-border)]"
        div {
            if config.isDecorative then
                _ariaHidden true
            else
                _role "separator"
                _ariaOrientation (match config.orientation with SeparatorOrientation.Horizontal -> "horizontal" | SeparatorOrientation.Vertical -> "vertical")
            _class classes
            for attribute in ComponentHtml.safeAttributes [ "class"; "role"; "aria-hidden"; "aria-orientation" ] config.attributes do attribute
        }
