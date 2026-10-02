namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>callout</category>
[<NoEquality; NoComparison>]
type CalloutConfig = private { label:string; content:HtmlElement list }

/// <category>callout</category>
[<RequireQualifiedAccess>]
module Callout =
    let create label content : CalloutConfig =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A callout label is required."
        { label = label; content = content }

    let render (callout:CalloutConfig) =
        div {
            _class "border-l-2 border-[var(--fve-brand-ring)] bg-[var(--fve-brand-subtle)] px-4 py-3"
            _data("docs-callout", "true")
            div { _class "text-xs font-semibold tracking-wide text-[var(--fve-brand-text)] uppercase"; callout.label }
            div { _class "mt-1 text-sm leading-relaxed text-[var(--fve-text)]"; callout.content }
        }
