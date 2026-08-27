namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<NoEquality; NoComparison>]
type StatusConfig =
    private
        { label:string
          tone:Tone
          leading:HtmlElement option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module Status =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A status label is required."
        { label = label; tone = Tone.Neutral; leading = None; attributes = [] }

    let withTone tone (config:StatusConfig) = { config with tone = tone }
    let withLeading leading (config:StatusConfig) = { config with leading = Some leading }
    let withAttributes attributes (config:StatusConfig) = { config with attributes = attributes }

    let render config =
        span {
            _class (ComponentHtml.classes [ "inline-flex items-center gap-1.5 rounded-full px-2 py-1 text-xs font-medium ring-1 ring-inset"; ComponentHtml.toneClasses config.tone ])
            for attribute in ComponentHtml.safeAttributes [ "class" ] config.attributes do attribute
            config.leading |> Option.defaultValue empty
            config.label
        }

    let positive label = create label |> withTone Tone.Positive |> render
    let warning label = create label |> withTone Tone.Warning |> render
