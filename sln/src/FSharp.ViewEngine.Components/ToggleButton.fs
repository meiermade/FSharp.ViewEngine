namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>toggle-button</category>
[<RequireQualifiedAccess>]
type ToggleButtonVariant =
    | Soft
    | Outline
    | Ghost

/// <category>toggle-button</category>
[<NoEquality; NoComparison>]
type ToggleButtonConfig =
    private
        { id:string
          label:string
          content:HtmlElement
          leading:HtmlElement option
          size:ControlSize
          variant:ToggleButtonVariant
          isPressed:bool
          isDisabled:bool
          isPending:bool }

/// <category>toggle-button</category>
[<RequireQualifiedAccess>]
module ToggleButton =
    let create id label =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable toggle button ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A toggle button label is required."
        { id = id
          label = label
          content = Html.text label
          leading = None
          size = ControlSize.Medium
          variant = ToggleButtonVariant.Outline
          isPressed = false
          isDisabled = false
          isPending = false }

    let withContent content (config:ToggleButtonConfig) = { config with content = content }
    let withLeading leading (config:ToggleButtonConfig) = { config with leading = Some leading }
    let withSize size (config:ToggleButtonConfig) = { config with size = size }
    let withVariant variant (config:ToggleButtonConfig) = { config with variant = variant }
    let pressed (config:ToggleButtonConfig) = { config with isPressed = true }
    let disabled (config:ToggleButtonConfig) = { config with isDisabled = true }
    let pending (config:ToggleButtonConfig) = { config with isPending = true }

    let render (config:ToggleButtonConfig) =
        let signal = $"_{ComponentHtml.signalToken config.id}_pressed"
        let initialValue = if config.isPressed then "true" else "false"
        let unavailable = config.isDisabled || config.isPending
        let variantClasses =
            match config.variant with
            | ToggleButtonVariant.Soft -> "border-0 bg-[var(--fve-surface-subtle)] hover:bg-[var(--fve-surface-hover)] aria-pressed:bg-[var(--fve-brand-subtle)]"
            | ToggleButtonVariant.Outline -> "bg-[var(--fve-surface)] ring-1 ring-inset ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] aria-pressed:bg-[var(--fve-brand-subtle)]"
            | ToggleButtonVariant.Ghost -> "border-0 bg-transparent hover:bg-[var(--fve-surface-hover)] aria-pressed:bg-[var(--fve-brand-subtle)]"
        button {
            _id config.id
            _type "button"
            _disabled unavailable
            _ariaDisabled unavailable
            _ariaLabel config.label
            _ariaPressed config.isPressed
            if config.isPending then _ariaBusy true
            _dataSignals $"{{{signal}: {initialValue}}}"
            _dataAttr ("aria-pressed", $"${signal} ? 'true' : 'false'")
            if not unavailable then _dataOn ("click", $"${signal} = !${signal}")
            _class (ComponentHtml.classes [ "inline-flex items-center justify-center gap-2 rounded-[var(--fve-radius-control)] px-3 font-medium text-[var(--fve-text)] outline-none transition-colors focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)] aria-pressed:text-[var(--fve-brand-text)] disabled:pointer-events-none disabled:opacity-50"; ComponentHtml.sizeClasses (Some config.size); variantClasses ])
            if config.isPending then ComponentHtml.loadingGlyph ControlSize.Small
            else
                match config.leading with
                | Some leading -> span { _ariaHidden true; _class "flex shrink-0 items-center justify-center"; leading }
                | None -> ()
            config.content
        }
