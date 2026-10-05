namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>copy-reveal</category>
[<NoEquality; NoComparison>]
type CopyRevealConfig = private { id:string; label:string; value:string; initiallyRevealed:bool; iconButtons:bool }

/// <category>copy-reveal</category>
[<RequireQualifiedAccess>]
module CopyReveal =
    let create id label value =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable copy/reveal ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A copy/reveal label is required."
        { id = id; label = label; value = value; initiallyRevealed = false; iconButtons = false }
    let revealed config = { config with initiallyRevealed = true }
    let withIconButtons config = { config with iconButtons = true }
    let render config =
        let revealSignal = ComponentHtml.signalToken (config.id + "-revealed")
        let messageSignal = ComponentHtml.signalToken (config.id + "-message")
        let revealed = "$" + revealSignal
        let message = "$" + messageSignal
        let initialRevealed = if config.initiallyRevealed then "true" else "false"
        let valueJson = ComponentHtml.javascriptString config.value
        let successJson = ComponentHtml.javascriptString (config.label + " copied.")
        let failureJson = ComponentHtml.javascriptString ("Could not copy " + config.label + ". Check clipboard permissions.")
        let initialRevealLabel = if config.initiallyRevealed then "Hide" else "Reveal"
        let revealIcon =
            span {
                _class "contents"
                span {
                    _dataShow ("!" + revealed)
                    if config.initiallyRevealed then _style "display:none"
                    raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" class="size-4"><path d="M1.75 10s3-5.25 8.25-5.25S18.25 10 18.25 10 15.25 15.25 10 15.25 1.75 10 1.75 10Z"/><circle cx="10" cy="10" r="2.25"/></svg>"""
                }
                span {
                    _dataShow revealed
                    if not config.initiallyRevealed then _style "display:none"
                    raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" class="size-4"><path d="m3 3 14 14M8.47 5.02A8.8 8.8 0 0 1 10 4.75c5.25 0 8.25 5.25 8.25 5.25a13.7 13.7 0 0 1-2.35 2.9M11.53 14.98a8.8 8.8 0 0 1-1.53.27C4.75 15.25 1.75 10 1.75 10A13.7 13.7 0 0 1 4.1 7.1M8.41 8.41a2.25 2.25 0 0 0 3.18 3.18"/></svg>"""
                }
            }
        let copyIcon =
            raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" class="size-4"><rect x="6.25" y="6.25" width="10" height="10" rx="1.5"/><path d="M13.75 6.25v-1.5a1.5 1.5 0 0 0-1.5-1.5h-7.5a1.5 1.5 0 0 0-1.5 1.5v7.5a1.5 1.5 0 0 0 1.5 1.5h1.5"/></svg>"""
        let revealButton =
            Button.create (if config.iconButtons then ButtonContent.Icon (initialRevealLabel, revealIcon) else ButtonContent.Text initialRevealLabel)
            |> Button.withAttributes [
                _title initialRevealLabel
                _dataAttr ("aria-label", revealed + " ? 'Hide' : 'Reveal'")
                _dataAttr ("title", revealed + " ? 'Hide' : 'Reveal'")
                _dataOn ("click", revealed + " = !" + revealed)
                if not config.iconButtons then _dataText (revealed + " ? 'Hide' : 'Reveal'") ]
            |> Button.render
        let copyButton =
            Button.create (if config.iconButtons then ButtonContent.Icon ("Copy", copyIcon) else ButtonContent.Text "Copy")
            |> Button.withAttributes [
                _title "Copy"
                _dataOn ("click", message + " = ''; if (navigator.clipboard?.writeText) { navigator.clipboard.writeText(" + valueJson + ").then(() => { " + message + " = " + successJson + " }).catch(() => { " + message + " = " + failureJson + " }) } else { " + message + " = " + failureJson + " }") ]
            |> Button.render
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
                revealButton
                copyButton
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
