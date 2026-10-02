namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

/// <category>field</category>
[<RequireQualifiedAccess>]
type FieldLayout =
    | Vertical
    | Horizontal
    | Responsive

/// <category>field</category>
[<NoEquality; NoComparison>]
type FieldConfig =
    private
        { id:string
          label:string
          description:string option
          validation:string option
          layout:FieldLayout
          required:bool
          disabled:bool
          pending:bool
          attributes:HtmlAttribute list }

/// <summary>
/// Label, description, error, and layout ownership for a consumer-authored form control.
/// </summary>
/// <category>field</category>
[<RequireQualifiedAccess>]
module Field =
    let create id label =
        { id = TextField.stableId id
          label = TextField.requiredText (nameof label) label
          description = None
          validation = None
          layout = FieldLayout.Vertical
          required = false
          disabled = false
          pending = false
          attributes = [] }

    let withDescription description (config:FieldConfig) = { config with description = Some(TextField.requiredText (nameof description) description) }
    let withValidation message (config:FieldConfig) = { config with validation = Some(TextField.requiredText (nameof message) message) }
    let withLayout layout (config:FieldConfig) = { config with layout = layout }
    let required (config:FieldConfig) = { config with required = true }
    let disabled (config:FieldConfig) = { config with disabled = true }
    let pending (config:FieldConfig) = { config with pending = true }
    let withAttributes attributes (config:FieldConfig) = { config with attributes = attributes }

    let render (control:HtmlAttribute list -> HtmlElement) (config:FieldConfig) =
        let descriptionId = config.id + "-description"
        let validationId = config.id + "-validation"
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        let controlAttributes =
            [ _id config.id
              _disabled config.disabled
              _readonly config.pending
              _required (config.required && not (config.disabled || config.pending))
              _ariaRequired config.required
              _ariaInvalid config.validation.IsSome
              if config.pending then _ariaBusy true
              if describedBy <> "" then _ariaDescribedby describedBy ]
        let label =
            label {
                _for config.id
                _class "flex items-center gap-2 text-sm font-medium text-[var(--fve-text)]"
                config.label
                if config.required then span { _ariaHidden true; _class "text-[var(--fve-critical-text)]"; "*" }
                if config.pending then ComponentHtml.loadingGlyph ControlSize.Small
            }
        let details =
            div {
                _class "grid min-w-0 content-start gap-1.5"
                control controlAttributes
                match config.description with
                | Some description -> p { _id descriptionId; _class "text-sm text-[var(--fve-muted-text)]"; description }
                | None -> ()
                match config.validation with
                | Some message -> p { _id validationId; _role "alert"; _class "text-sm text-[var(--fve-critical-text)]"; message }
                | None -> ()
            }
        div {
            _class (ComponentHtml.classes [
                "min-w-0 [overflow-wrap:anywhere]"
                match config.layout with
                | FieldLayout.Vertical -> "grid content-start gap-1.5"
                | FieldLayout.Horizontal -> "grid grid-cols-[minmax(8rem,0.4fr)_minmax(0,1fr)] items-start gap-x-4 gap-y-1.5"
                | FieldLayout.Responsive -> "grid content-start gap-1.5 sm:grid-cols-[minmax(8rem,0.4fr)_minmax(0,1fr)] sm:items-start sm:gap-x-4" ])
            for attribute in ComponentHtml.safeAttributes [ "class" ] config.attributes do attribute
            label
            details
        }
