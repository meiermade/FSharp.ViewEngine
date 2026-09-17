namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type AvatarConfig = private { name:string; source:string option; fallback:string; decorative:bool; sizeClass:string }

[<RequireQualifiedAccess>]
module Avatar =
    let create name fallback =
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "An avatar name is required."
        if String.IsNullOrWhiteSpace fallback then invalidArg (nameof fallback) "Avatar fallback text is required."
        { name = name; source = None; fallback = fallback; decorative = false; sizeClass = "size-10" }
    let withImage source config = { config with source = Some source }
    let decorative config = { config with decorative = true }
    let small config = { config with sizeClass = "size-8" }
    let large config = { config with sizeClass = "size-12" }
    let render config =
        let accessibleName = if config.decorative then None else Some config.name
        span {
            match accessibleName with
            | Some name -> _role "img"; _ariaLabel name
            | None -> _ariaHidden true
            _class ("relative inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-[var(--fve-neutral-subtle)] font-semibold text-[var(--fve-neutral-text)] " + config.sizeClass)
            match config.source with
            | Some source ->
                img {
                    _src source
                    _alt ""
                    _class "size-full object-cover"
                }
            | None ->
                span {
                    _ariaHidden true
                    _class "text-sm"
                    config.fallback
                }
        }

[<NoEquality; NoComparison>]
type CopyRevealConfig = private { id:string; label:string; value:string; initiallyRevealed:bool }

[<RequireQualifiedAccess>]
module CopyReveal =
    let create id label value =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable copy/reveal ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A copy/reveal label is required."
        { id = id; label = label; value = value; initiallyRevealed = false }
    let revealed config = { config with initiallyRevealed = true }
    let render config =
        let revealSignal = ComponentHtml.signalToken (config.id + "-revealed")
        let messageSignal = ComponentHtml.signalToken (config.id + "-message")
        let revealed = "$" + revealSignal
        let message = "$" + messageSignal
        let initialRevealed = if config.initiallyRevealed then "true" else "false"
        let valueJson = ComponentHtml.javascriptString config.value
        let successJson = ComponentHtml.javascriptString (config.label + " copied.")
        let failureJson = ComponentHtml.javascriptString ("Could not copy " + config.label + ". Check clipboard permissions.")
        div {
            _dataSignals ("{" + revealSignal + ": " + initialRevealed + ", " + messageSignal + ": ''}")
            _class "grid gap-2"
            label {
                _for config.id
                _class "text-sm font-medium text-[var(--fve-text)]"
                config.label
            }
            div {
                _class "flex min-w-0 flex-wrap gap-2"
                input {
                    _id config.id
                    _type (if config.initiallyRevealed then "text" else "password")
                    _value config.value
                    _readonly true
                    _dataAttr ("type", revealed + " ? 'text' : 'password'")
                    _class "min-h-[var(--fve-control-min-height)] min-w-0 flex-1 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 font-mono text-sm text-[var(--fve-text)] ring-1 ring-[var(--fve-border)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                }
                button {
                    _type "button"
                    _class "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm font-semibold ring-1 ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                    _dataOn ("click", revealed + " = !" + revealed)
                    _dataText (revealed + " ? 'Hide' : 'Reveal'")
                    if config.initiallyRevealed then "Hide" else "Reveal"
                }
                button {
                    _type "button"
                    _class "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm font-semibold ring-1 ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                    _dataOn ("click", message + " = ''; if (navigator.clipboard?.writeText) { navigator.clipboard.writeText(" + valueJson + ").then(() => { " + message + " = " + successJson + " }).catch(() => { " + message + " = " + failureJson + " }) } else { " + message + " = " + failureJson + " }")
                    "Copy"
                }
            }
            output {
                _role "status"
                _ariaLive "polite"
                _dataText message
                _dataShow (message + " != ''")
                _style "display:none"
                _class "text-sm text-[var(--fve-muted-text)]"
            }
        }
