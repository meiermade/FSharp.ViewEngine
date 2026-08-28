namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type BadgeConfig =
    private
        { label:string
          tone:Tone
          leading:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Badge =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A badge label is required."
        { label = label; tone = Tone.Neutral; leading = None; attributes = [] }

    let withTone tone (config:BadgeConfig) = { config with tone = tone }
    let withLeading leading (config:BadgeConfig) = { config with leading = Some leading }
    let withAttributes attributes (config:BadgeConfig) = { config with attributes = attributes }

    let render config =
        span {
            _class (ComponentHtml.classes [ "inline-flex items-center gap-1.5 rounded-[var(--fve-radius-control)] px-2 py-1 text-xs font-medium ring-1 ring-inset"; ComponentHtml.toneClasses config.tone ])
            for attribute in ComponentHtml.safeAttributes [ "class" ] config.attributes do attribute
            config.leading |> Option.defaultValue empty
            config.label
        }
