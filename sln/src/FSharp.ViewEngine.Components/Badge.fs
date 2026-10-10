namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>badge</category>
[<RequireQualifiedAccess>]
type BadgeColor =
    | Primary
    | Secondary
    | Success
    | Warning
    | Error
    | Info
    | Neutral
    | Custom of ColorPalette

/// <category>badge</category>
[<RequireQualifiedAccess>]
type BadgeVariant =
    | Solid
    | Soft
    | Outline
    | Ghost

/// <category>badge</category>
[<NoEquality; NoComparison>]
type BadgeConfig =
    private
        { label:string
          color:BadgeColor
          variant:BadgeVariant
          leading:HtmlElement option
          attributes:HtmlAttribute list }

/// <category>badge</category>
[<RequireQualifiedAccess>]
module Badge =
    /// Defaults to a borderless Neutral + Soft badge. Badges never announce status changes.
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A badge label is required."
        { label = label; color = BadgeColor.Neutral; variant = BadgeVariant.Soft; leading = None; attributes = [] }

    let withColor color (config:BadgeConfig) = { config with color = color }
    let withVariant variant (config:BadgeConfig) = { config with variant = variant }
    let withLeading leading (config:BadgeConfig) = { config with leading = Some leading }
    let withAttributes attributes (config:BadgeConfig) = { config with attributes = attributes }

    let private palette = function
        | BadgeColor.Primary -> ComponentColors.primary
        | BadgeColor.Secondary -> ComponentColors.secondary
        | BadgeColor.Success -> ComponentColors.success
        | BadgeColor.Warning -> ComponentColors.warning
        | BadgeColor.Error -> ComponentColors.error
        | BadgeColor.Info -> ComponentColors.info
        | BadgeColor.Neutral -> ComponentColors.neutral
        | BadgeColor.Custom colors -> colors

    let render config =
        span {
            _style (ComponentColors.style (palette config.color) config.attributes)
            _class (ComponentHtml.classes [
                "inline-flex items-center gap-1.5 rounded-[var(--fve-radius-control)] px-2 py-1 text-xs font-medium"
                match config.variant with
                | BadgeVariant.Solid -> ComponentColors.solid
                | BadgeVariant.Soft ->
                    match config.color with
                    | BadgeColor.Custom _ | BadgeColor.Neutral -> ComponentColors.soft
                    | _ -> "bg-[color-mix(in_oklab,var(--fve-color-soft)_45%,var(--fve-surface))] text-[color-mix(in_oklab,var(--fve-color-text)_65%,var(--fve-text))]"
                | BadgeVariant.Outline -> ComponentColors.outline
                | BadgeVariant.Ghost -> ComponentColors.ghost ])
            for attribute in ComponentHtml.safeAttributes [ "class"; "style" ] config.attributes do attribute
            config.leading |> Option.defaultValue empty
            config.label
        }
