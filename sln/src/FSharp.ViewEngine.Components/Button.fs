namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar
[<RequireQualifiedAccess>]
type ButtonVariant =
    | Primary
    | Secondary
    | Ghost
    | Destructive

[<RequireQualifiedAccess>]
type ButtonType =
    | Button
    | Submit
    | Reset

[<NoEquality; NoComparison>]
type ButtonConfig =
    private
        { label:string
          variant:ButtonVariant
          size:ControlSize
          buttonType:ButtonType
          leading:HtmlElement option
          trailing:HtmlElement option
          disabled:bool
          pending:bool
          className:string option
          attributes:HtmlAttribute list }

module internal ButtonStyles =
    let variantClasses = function
        | ButtonVariant.Primary -> "bg-[var(--fve-brand-solid)] text-white hover:bg-[var(--fve-brand-hover)] active:bg-[var(--fve-brand-active)] focus-visible:ring-[var(--fve-brand-ring)]"
        | ButtonVariant.Secondary -> "bg-[var(--fve-surface)] text-[var(--fve-text)] ring-1 ring-inset ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] active:bg-[var(--fve-surface-active)] focus-visible:ring-[var(--fve-brand-ring)]"
        | ButtonVariant.Ghost -> "bg-transparent text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] active:bg-[var(--fve-surface-active)] focus-visible:ring-[var(--fve-brand-ring)]"
        | ButtonVariant.Destructive -> "bg-[var(--fve-critical-solid)] text-white hover:bg-[var(--fve-critical-hover)] active:bg-[var(--fve-critical-active)] focus-visible:ring-[var(--fve-critical-ring)]"

    let buttonTypeValue = function
        | ButtonType.Button -> "button"
        | ButtonType.Submit -> "submit"
        | ButtonType.Reset -> "reset"

    let baseClasses =
        "inline-flex items-center justify-center gap-2 rounded-[var(--fve-radius-control)] font-semibold shadow-sm outline-none transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50"

[<RequireQualifiedAccess>]
module Button =
    let create label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A button label is required."
        { label = label
          variant = ButtonVariant.Secondary
          size = ControlSize.Medium
          buttonType = ButtonType.Button
          leading = None
          trailing = None
          disabled = false
          pending = false
          className = None
          attributes = [] }

    let withVariant variant config = { config with variant = variant }
    let withSize size config = { config with size = size }
    let asSubmit config = { config with buttonType = ButtonType.Submit }
    let withLeading leading config = { config with leading = Some leading }
    let withTrailing trailing config = { config with trailing = Some trailing }
    let disabled config = { config with disabled = true }
    let pending config = { config with pending = true }
    let withClass className config = { config with className = Some className }
    let withAttributes attributes config = { config with attributes = attributes }

    let render config =
        let unavailable = config.disabled || config.pending
        button {
            _type (ButtonStyles.buttonTypeValue config.buttonType)
            _disabled unavailable
            if config.pending then _ariaBusy true
            _class (
                ComponentHtml.classes [
                    ButtonStyles.baseClasses
                    ComponentHtml.sizeClasses config.size
                    ButtonStyles.variantClasses config.variant
                    config.className |> Option.defaultValue "" ])
            for attribute in ComponentHtml.safeAttributes [ "type"; "disabled"; "aria-busy"; "class" ] config.attributes do attribute
            if config.pending then
                ComponentHtml.loadingGlyph config.size
            else
                config.leading |> Option.defaultValue empty
            config.label
            if config.pending |> not then
                config.trailing |> Option.defaultValue empty
        }

    let primary label = create label |> withVariant ButtonVariant.Primary |> render
    let secondary label = create label |> withVariant ButtonVariant.Secondary |> render
