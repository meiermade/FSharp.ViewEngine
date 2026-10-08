namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

/// <summary>Selects the button palette independently of its visual treatment.</summary>
/// <category>button</category>
[<RequireQualifiedAccess>]
type ButtonColor =
    | Primary
    | Secondary
    | Success
    | Warning
    | Error
    | Info
    | Neutral
    | Custom of ColorPalette

/// <summary>Selects the button’s visual treatment independently of its palette.</summary>
/// <category>button</category>
[<RequireQualifiedAccess>]
type ButtonVariant =
    | Solid
    | Soft
    | Outline
    | Ghost

/// <summary>Defines the visible and accessible content rendered by a Button.</summary>
/// <category>button</category>
[<RequireQualifiedAccess; NoEquality; NoComparison>]
type ButtonContent =
    /// Renders a visible text label.
    | Text of text:string
    /// Renders a square icon-only control with the supplied accessible name.
    | Icon of accessibleName:string * icon:HtmlElement
    /// Renders a decorative icon before a visible text label.
    | IconText of icon:HtmlElement * text:string
    /// Renders a visible text label before a decorative icon.
    | TextIcon of text:string * icon:HtmlElement
    /// Renders arbitrary noninteractive presentational content using ordinary button spacing.
    | Custom of HtmlElement

/// <summary>Maps a button to its native HTML form behavior.</summary>
/// <category>button</category>
[<RequireQualifiedAccess>]
type ButtonType =
    | Button
    | Submit
    | Reset

/// <summary>Configuration produced and modified by the Button module.</summary>
/// <category>button</category>
[<NoEquality; NoComparison>]
type ButtonConfig =
    private
        { content:ButtonContent
          color:ButtonColor
          variant:ButtonVariant
          size:ControlSize option
          buttonType:ButtonType
          disabled:bool
          pending:bool
          className:string option
          attributes:HtmlAttribute list }

module internal ButtonStyles =
    let buttonTypeValue = function
        | ButtonType.Button -> "button"
        | ButtonType.Submit -> "submit"
        | ButtonType.Reset -> "reset"

    let baseClasses =
        "inline-flex items-center justify-center gap-2 rounded-[var(--fve-radius-control)] font-medium shadow-xs outline-none transition-[color,background-color,border-color,box-shadow,translate] duration-100 motion-reduce:transition-none motion-safe:[&:not(:disabled):not([aria-disabled=true]):not([aria-busy=true]):not([aria-haspopup]):not([data-fve-keyboard-pressed])]:active:translate-y-px motion-safe:[&:not(:disabled):not([aria-disabled=true]):not([aria-busy=true]):not([aria-haspopup])]:data-[fve-keyboard-pressed=true]:translate-y-px focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50"

    let loadingGlyph size =
        span {
            _ariaHidden "true"
            _class ("shrink-0 animate-spin rounded-full border-2 border-current border-r-transparent motion-reduce:animate-none " +
                match size with
                | ControlSize.Small -> "size-3.5"
                | ControlSize.Medium -> "size-4"
                | ControlSize.Large -> "size-5")
        }

/// <summary>Builds and renders semantic button controls.</summary>
/// <remarks>
/// Icon-only content requires an accessible name; Custom content must not contain interactive controls.
/// Custom palettes must supply contrasting foreground, background, border and focus colors in both themes.
/// </remarks>
/// <category>button</category>
[<RequireQualifiedAccess>]
module Button =
    let private validatedContent = function
        | ButtonContent.Text text when String.IsNullOrWhiteSpace text -> invalidArg (nameof text) "Button text is required."
        | ButtonContent.Icon (accessibleName, _) when String.IsNullOrWhiteSpace accessibleName -> invalidArg (nameof accessibleName) "An icon-only Button requires an accessible name."
        | ButtonContent.IconText (_, text)
        | ButtonContent.TextIcon (text, _) when String.IsNullOrWhiteSpace text -> invalidArg (nameof text) "Button text is required."
        | content -> content

    /// <summary>Creates a native button from explicit content. Defaults to Neutral + Outline; color and appearance can be changed independently.</summary>
    let create content =
        { content = validatedContent content
          color = ButtonColor.Neutral
          variant = ButtonVariant.Outline
          size = None
          buttonType = ButtonType.Button
          disabled = false
          pending = false
          className = None
          attributes = [] }

    /// Selects the component-owned color palette.
    let withColor color (config:ButtonConfig) = { config with color = color }
    /// Selects the visual treatment without changing button behavior.
    let withVariant variant (config:ButtonConfig) = { config with variant = variant }
    /// Overrides the inherited control size.
    let withSize size (config:ButtonConfig) = { config with size = Some size }
    /// Uses native submit-button behavior.
    let asSubmit (config:ButtonConfig) = { config with buttonType = ButtonType.Submit }
    /// Makes the button unavailable without presenting work in progress.
    let disabled (config:ButtonConfig) = { config with disabled = true }
    /// Makes the button busy and unavailable while retaining its form value.
    let pending (config:ButtonConfig) = { config with pending = true }
    /// Adds consumer presentation classes without replacing component-owned classes.
    let withClass className (config:ButtonConfig) = { config with className = Some className }
    /// Adds safe consumer-authored HTML attributes.
    let withAttributes attributes (config:ButtonConfig) = { config with attributes = attributes }
    let internal withGroupClass className (config:ButtonConfig) = { config with className = Some(ComponentHtml.classes [ config.className |> Option.defaultValue ""; className ]) }

    let private palette = function
        | ButtonColor.Primary -> ComponentColors.primary
        | ButtonColor.Secondary -> ComponentColors.secondary
        | ButtonColor.Success -> ComponentColors.success
        | ButtonColor.Warning -> ComponentColors.warning
        | ButtonColor.Error -> ComponentColors.error
        | ButtonColor.Info -> ComponentColors.info
        | ButtonColor.Neutral -> ComponentColors.neutral
        | ButtonColor.Custom colors -> colors

    let internal variantClasses = function
        | ButtonVariant.Solid -> ComponentColors.solid + " " + ComponentColors.solidInteraction
        | ButtonVariant.Soft -> ComponentColors.soft + " " + ComponentColors.softInteraction
        | ButtonVariant.Outline -> ComponentColors.outline + " " + ComponentColors.softInteraction
        | ButtonVariant.Ghost -> ComponentColors.ghost + " " + ComponentColors.softInteraction

    let private decorative (content:HtmlElement) : HtmlElement =
        span {
            _ariaHidden true
            _class "shrink-0"
            content
        }

    let private accessibleName = function
        | ButtonContent.Icon (name, _) -> Some name
        | _ -> None

    let private iconOnly = function
        | ButtonContent.Icon _ -> true
        | _ -> false

    let private renderAvailable (content:ButtonContent) : HtmlElement =
        match content with
        | ButtonContent.Text text -> Html.text text
        | ButtonContent.Icon (_, icon) -> decorative icon
        | ButtonContent.IconText (icon, text) ->
            fragment {
                decorative icon
                text
            }
        | ButtonContent.TextIcon (text, icon) ->
            fragment {
                text
                decorative icon
            }
        | ButtonContent.Custom content -> content

    let private renderPending (content:ButtonContent) : HtmlElement =
        match content with
        | ButtonContent.Icon _ -> empty
        | ButtonContent.Text text
        | ButtonContent.IconText (_, text)
        | ButtonContent.TextIcon (text, _) -> Html.text text
        | ButtonContent.Custom content -> content

    let private classesFor appearance (config:ButtonConfig) =
        ComponentHtml.classes [
            ButtonStyles.baseClasses
            (if iconOnly config.content then ComponentHtml.iconButtonSizeClasses config.size else ComponentHtml.sizeClasses config.size)
            appearance
            ComponentColors.focus
            config.className |> Option.defaultValue "" ]

    /// Renders a native destination link with Button presentation. Links cannot submit, be disabled or be pending.
    let renderLink href (config:ButtonConfig) =
        if String.IsNullOrWhiteSpace href then invalidArg (nameof href) "A native Button link requires a destination."
        if config.disabled || config.pending || config.buttonType<>ButtonType.Button then
            invalidArg (nameof config) "Native Button links do not support disabled, pending or form-submit behavior."
        let interaction =
            match config.variant with
            | ButtonVariant.Solid -> ComponentColors.solid + " hover:bg-[var(--fve-color-hover)] active:bg-[var(--fve-color-active)]"
            | ButtonVariant.Soft -> ComponentColors.soft + " hover:bg-[var(--fve-color-soft-hover)] active:bg-[var(--fve-color-soft-active)]"
            | ButtonVariant.Outline -> ComponentColors.outline + " hover:bg-[var(--fve-color-soft-hover)] active:bg-[var(--fve-color-soft-active)]"
            | ButtonVariant.Ghost -> ComponentColors.ghost + " hover:bg-[var(--fve-color-soft-hover)] active:bg-[var(--fve-color-soft-active)]"
        a {
            _href href
            match accessibleName config.content with Some name -> _ariaLabel name | None -> ()
            _style (ComponentColors.style (palette config.color) config.attributes)
            _class (classesFor interaction config)
            for attribute in ComponentHtml.safeAttributes [ "href"; "type"; "aria-label"; "disabled"; "aria-busy"; "class"; "style" ] config.attributes do attribute
            renderAvailable config.content
        }

    let internal renderGroupedLink href groupClass config = config |> withGroupClass groupClass |> renderLink href

    /// Renders the configured native button.
    let render config =
        let unavailable = config.disabled || config.pending
        button {
            _type (ButtonStyles.buttonTypeValue config.buttonType)
            match accessibleName config.content with Some name -> _ariaLabel name | None -> ()
            _disabled unavailable
            if config.pending then _ariaBusy true
            _style (ComponentColors.style (palette config.color) config.attributes)
            _class (classesFor (variantClasses config.variant) config)
            if not unavailable then
                _dataOn ("keydown", [ "capture" ], "if (evt.target === el && (evt.key === ' ' || evt.key === 'Enter') && !evt.altKey && !evt.ctrlKey && !evt.metaKey && !el.hasAttribute('aria-haspopup') && el.getAttribute('aria-disabled') !== 'true' && el.getAttribute('aria-busy') !== 'true') el.dataset.fveKeyboardPressed = 'true'")
                // WebKit can retain native :active after a keyboard press loses focus.
                // Keep the released marker until pointer input resumes native press styling.
                _dataOn ("keyup", [ "capture" ], "if ((evt.key === ' ' || evt.key === 'Enter') && el.dataset.fveKeyboardPressed) el.dataset.fveKeyboardPressed = 'false'")
                _dataOn ("blur", [ "capture" ], "if (el.dataset.fveKeyboardPressed) el.dataset.fveKeyboardPressed = 'false'")
                _dataOn ("blur", [ "window" ], "if (el.dataset.fveKeyboardPressed) el.dataset.fveKeyboardPressed = 'false'")
                _dataOn ("pointerdown", [ "capture" ], "delete el.dataset.fveKeyboardPressed")
            for attribute in ComponentHtml.safeAttributes [ "type"; "aria-label"; "disabled"; "aria-busy"; "class"; "style"; "data-fve-keyboard-pressed"; "data-on:keydown__capture"; "data-on:keyup__capture"; "data-on:blur__capture"; "data-on:blur__window"; "data-on:pointerdown__capture" ] config.attributes do attribute
            if config.pending then
                ButtonStyles.loadingGlyph (config.size |> Option.defaultValue ControlSize.Medium)
                renderPending config.content
            else
                renderAvailable config.content
        }
