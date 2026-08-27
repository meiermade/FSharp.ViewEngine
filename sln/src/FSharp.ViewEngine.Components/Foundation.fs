namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar

[<RequireQualifiedAccess>]
type Tone =
    | Neutral
    | Brand
    | Positive
    | Warning
    | Critical
    | Informative

[<RequireQualifiedAccess>]
type ControlSize =
    | Small
    | Medium
    | Large

[<RequireQualifiedAccess>]
type Radius =
    | None
    | Medium
    | Large
    | Full

[<RequireQualifiedAccess>]
type Density =
    | Compact
    | Comfortable

[<NoEquality; NoComparison>]
type ComponentsTheme =
    private
        { paletteClass:string
          radiusClass:string
          densityClass:string }

[<RequireQualifiedAccess>]
module ComponentsTheme =
    let sky =
        { paletteClass = "fve-theme-sky"
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable" }

    let emerald =
        { paletteClass = "fve-theme-emerald"
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable" }

    let withRadius radius theme =
        let radiusClass =
            match radius with
            | Radius.None -> "fve-radius-none"
            | Radius.Medium -> "fve-radius-medium"
            | Radius.Large -> "fve-radius-large"
            | Radius.Full -> "fve-radius-full"
        { theme with radiusClass = radiusClass }

    let withDensity density theme =
        let densityClass =
            match density with
            | Density.Compact -> "fve-density-compact"
            | Density.Comfortable -> "fve-density-comfortable"
        { theme with densityClass = densityClass }

    let className theme =
        [ "fve-components"; theme.paletteClass; theme.radiusClass; theme.densityClass ]
        |> String.concat " "

    let attributes theme =
        [ _class (className theme) ]

module internal ComponentHtml =
    let classes values = values |> List.filter (String.IsNullOrWhiteSpace >> not) |> String.concat " "

    let safeAttributes reservedNames attributes =
        attributes
        |> List.filter (fun attribute ->
            reservedNames
            |> List.exists (fun reservedName ->
                String.Equals(attribute.Name, reservedName, StringComparison.OrdinalIgnoreCase)
                || (reservedName.EndsWith(':') && attribute.Name.StartsWith(reservedName, StringComparison.OrdinalIgnoreCase)))
            |> not)

    let javascriptString value = System.Text.Json.JsonSerializer.Serialize(value)

    let signalToken value =
        let token = Regex.Replace(value, "[^A-Za-z0-9]+", "_").Trim('_')
        if String.IsNullOrEmpty token then "component" else token.ToLowerInvariant()

    let optionToken (value:string) =
        value
        |> Encoding.UTF8.GetBytes
        |> Array.map (fun character -> character.ToString("x2"))
        |> String.concat ""
        |> (+) "v"

    let toneClasses = function
        | Tone.Neutral -> "bg-[var(--fve-neutral-subtle)] text-[var(--fve-neutral-text)] ring-[var(--fve-border)]"
        | Tone.Brand -> "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)] ring-[var(--fve-brand-ring)]"
        | Tone.Positive -> "bg-[var(--fve-positive-subtle)] text-[var(--fve-positive-text)] ring-[var(--fve-positive-ring)]"
        | Tone.Warning -> "bg-[var(--fve-warning-subtle)] text-[var(--fve-warning-text)] ring-[var(--fve-warning-ring)]"
        | Tone.Critical -> "bg-[var(--fve-critical-subtle)] text-[var(--fve-critical-text)] ring-[var(--fve-critical-ring)]"
        | Tone.Informative -> "bg-[var(--fve-info-subtle)] text-[var(--fve-info-text)] ring-[var(--fve-info-ring)]"

    let sizeClasses = function
        | ControlSize.Small -> "min-h-[calc(var(--fve-control-min-height)-0.25rem)] px-2.5 py-[var(--fve-control-padding-block)] text-xs"
        | ControlSize.Medium -> "min-h-[var(--fve-control-min-height)] px-3 py-[var(--fve-control-padding-block)] text-sm"
        | ControlSize.Large -> "min-h-[calc(var(--fve-control-min-height)+0.5rem)] px-4 py-[var(--fve-control-padding-block)] text-base"
