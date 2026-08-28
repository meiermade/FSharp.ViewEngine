namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type IconButtonConfig =
    private
        { label:string
          icon:HtmlElement
          variant:ButtonVariant
          size:ControlSize
          buttonType:ButtonType
          disabled:bool
          pending:bool
          className:string option
          attributes:HtmlAttribute list }

[<RequireQualifiedAccess>]
module IconButton =
    let create label icon =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "An accessible icon button label is required."
        { label = label
          icon = icon
          variant = ButtonVariant.Secondary
          size = ControlSize.Medium
          buttonType = ButtonType.Button
          disabled = false
          pending = false
          className = None
          attributes = [] }

    let withVariant variant (config:IconButtonConfig) = { config with variant = variant }
    let withSize size (config:IconButtonConfig) = { config with size = size }
    let asSubmit (config:IconButtonConfig) = { config with buttonType = ButtonType.Submit }
    let disabled (config:IconButtonConfig) = { config with disabled = true }
    let pending (config:IconButtonConfig) = { config with pending = true }
    let withClass className (config:IconButtonConfig) = { config with className = Some className }
    let withAttributes attributes (config:IconButtonConfig) = { config with attributes = attributes }

    let render config =
        let unavailable = config.disabled || config.pending
        button {
            _type (ButtonStyles.buttonTypeValue config.buttonType)
            _ariaLabel config.label
            _disabled unavailable
            if config.pending then _ariaBusy true
            _class (
                ComponentHtml.classes [
                    ButtonStyles.baseClasses
                    ComponentHtml.iconButtonSizeClasses config.size
                    ButtonStyles.variantClasses config.variant
                    config.className |> Option.defaultValue "" ])
            for attribute in ComponentHtml.safeAttributes [ "type"; "aria-label"; "disabled"; "aria-busy"; "class" ] config.attributes do attribute
            if config.pending then
                ComponentHtml.loadingGlyph config.size
            else
                span { _ariaHidden "true"; config.icon }
        }
