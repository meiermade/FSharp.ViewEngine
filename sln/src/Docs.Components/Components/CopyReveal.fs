namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>copy-reveal</category>
[<AutoOpen>]
module CopyRevealAttributes =
    type Html with
        static member _revealed(value:bool) = Html._attr("revealed", if value then "true" else "false")
        static member _iconButtons(value:bool) = Html._attr("icon-buttons", if value then "true" else "false")

module internal CopyReveal =
    let render attributes children =
        ComponentContent.noChildren "fveCopyReveal" children
        let attributes = ComponentElement.attributes attributes
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "attributes" "A stable CopyReveal ID without whitespace is required."
        let value = ComponentElement.value "value" attributes |> Option.defaultWith (fun () -> invalidArg "attributes" "A CopyReveal value is required.")
        if isNull value then invalidArg "attributes" "A CopyReveal value cannot be null."
        let flag name = ComponentElement.value name attributes |> Option.exists ((=) "true")
        let initiallyRevealed = flag "revealed"
        let disabled = ComponentElement.attribute "disabled" attributes |> Option.exists (fun attribute -> attribute.Value <> ValueSome "false")
        let iconButtons = flag "icon-buttons"
        let size = ComponentElement.size None attributes
        let initial = if initiallyRevealed then "true" else "false"
        let initialLabel = if initiallyRevealed then "Hide" else "Reveal"
        let revealIcon =
            span {
                _class "contents"
                span {
                    _attr ("data-fve-copy-reveal-icon", "reveal")
                    if initiallyRevealed then _style "display:none"
                    raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" class="size-4"><path d="M1.75 10s3-5.25 8.25-5.25S18.25 10 18.25 10 15.25 15.25 10 15.25 1.75 10 1.75 10Z"/><circle cx="10" cy="10" r="2.25"/></svg>"""
                }
                span {
                    _attr ("data-fve-copy-reveal-icon", "hide")
                    if not initiallyRevealed then _style "display:none"
                    raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" class="size-4"><path d="m3 3 14 14M8.47 5.02A8.8 8.8 0 0 1 10 4.75c5.25 0 8.25 5.25 8.25 5.25a13.7 13.7 0 0 1-2.35 2.9M11.53 14.98a8.8 8.8 0 0 1-1.53.27C4.75 15.25 1.75 10 1.75 10A13.7 13.7 0 0 1 4.1 7.1M8.41 8.41a2.25 2.25 0 0 0 3.18 3.18"/></svg>"""
                }
            }
        let copyIcon = raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" class="size-4"><rect x="6.25" y="6.25" width="10" height="10" rx="1.5"/><path d="M13.75 6.25v-1.5a1.5 1.5 0 0 0-1.5-1.5h-7.5a1.5 1.5 0 0 0-1.5 1.5v7.5a1.5 1.5 0 0 0 1.5 1.5h1.5"/></svg>"""
        (Html.el "fve-copy-reveal") {
            _id (id + "-host")
            _attr ("revealed", initial)
            _class ("grid min-w-0 gap-2 " + ComponentHtml.controlSizeClass size)
            div {
                _class "flex min-w-0 flex-wrap gap-2"
                input {
                    for attribute in ComponentElement.attributes (
                        [ _id id; _type (if initiallyRevealed then "text" else "password"); _value value; _readonly true
                          _class "min-h-[var(--fve-control-min-height)] min-w-0 basis-40 flex-1 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 font-mono text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] text-[var(--fve-text)] border border-[var(--fve-border)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50" ]
                        @ ComponentHtml.safeAttributes ["id"; "type"; "value"; "readonly"; "revealed"; "icon-buttons"; "size"] attributes |> ComponentHtml.outerSignals) do attribute
                }
                fveButton {
                    _disabled disabled
                    _title initialLabel
                    _attr ("data-fve-copy-reveal-action", "reveal")
                    if iconButtons then
                        _iconOnly true
                        _ariaLabel initialLabel
                        span { _ariaHidden true; _class "inline-flex shrink-0"; revealIcon }
                    else span { _attr ("data-fve-copy-reveal-label", ""); initialLabel }
                }
                fveButton {
                    _disabled disabled
                    _title "Copy"
                    _attr ("data-fve-copy-reveal-action", "copy")
                    if iconButtons then
                        _iconOnly true
                        _ariaLabel "Copy"
                        span { _ariaHidden true; _class "inline-flex shrink-0"; copyIcon }
                    else "Copy"
                }
            }
            output {
                _role "status"
                _ariaLive "polite"
                _style "display:none"
                _class "text-sm text-[var(--fve-muted-text)]"
            }
        }

/// <category>copy-reveal</category>
[<AutoOpen>]
module CopyRevealDsl =
    /// Read-only native value with reveal/copy actions. The application supplies its label.
    let fveCopyReveal = ComponentElementBuilder(CopyReveal.render)
