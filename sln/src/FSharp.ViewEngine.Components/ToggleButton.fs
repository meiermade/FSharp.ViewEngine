namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type ToggleButtonConfig =
    private
        { id:string
          label:string
          isPressed:bool
          isDisabled:bool
          isPending:bool }

[<RequireQualifiedAccess>]
module ToggleButton =
    let create id label =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable toggle button ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A toggle button label is required."
        { id = id; label = label; isPressed = false; isDisabled = false; isPending = false }

    let pressed (config:ToggleButtonConfig) = { config with isPressed = true }
    let disabled (config:ToggleButtonConfig) = { config with isDisabled = true }
    let pending (config:ToggleButtonConfig) = { config with isPending = true }

    let render (config:ToggleButtonConfig) =
        let signal = $"_{ComponentHtml.signalToken config.id}_pressed"
        let initialValue = if config.isPressed then "true" else "false"
        let unavailable = config.isDisabled || config.isPending
        button {
            _id config.id
            _type "button"
            _disabled unavailable
            _ariaDisabled unavailable
            _ariaPressed config.isPressed
            if config.isPending then _ariaBusy true
            _dataSignals $"{{{signal}: {initialValue}}}"
            _dataAttr ("aria-pressed", $"${signal} ? 'true' : 'false'")
            if not unavailable then _dataOn ("click", $"${signal} = !${signal}")
            _class "inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)] aria-pressed:bg-[var(--fve-brand-subtle)] aria-pressed:text-[var(--fve-brand-text)] disabled:pointer-events-none disabled:opacity-50"
            if config.isPending then ComponentHtml.loadingGlyph ControlSize.Small
            config.label
        }
