namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<RequireQualifiedAccess>]
type InputType =
    | Text | Email | Telephone | Password | Number | Search | Url | Date | Time | DateTimeLocal

[<NoEquality; NoComparison>]
type internal TextFieldData =
    { id:string
      name:string
      label:string
      value:string
      description:string option
      validation:string option
      attributes:HtmlAttribute list
      required:bool
      disabled:bool
      pending:bool
      size:ControlSize option
      labelVisuallyHidden:bool }

module internal TextField =
    let requiredText argument value =
        if String.IsNullOrWhiteSpace value then invalidArg argument "Accessible text is required."
        value

    let stableId value =
        if String.IsNullOrWhiteSpace value || (value |> Seq.exists Char.IsWhiteSpace) then
            invalidArg (nameof value) "A stable ID without whitespace is required."
        value

    let create prefix name label =
        { id = prefix + ComponentHtml.optionToken (requiredText (nameof name) name)
          name = name
          label = requiredText (nameof label) label
          value = ""
          description = None
          validation = None
          attributes = []
          required = false
          disabled = false
          pending = false
          size = None
          labelVisuallyHidden = false }

    let classes field =
        ComponentHtml.classes [
            ComponentHtml.controlSizeClass field.size
            "block w-full min-w-0 min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] border bg-[var(--fve-surface)] px-3 py-[max(0px,calc((var(--fve-control-min-height)-var(--fve-control-line-height)-2px)/2))] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] text-[var(--fve-text)] placeholder:text-[var(--fve-muted-text)] focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-50 read-only:bg-[var(--fve-neutral-subtle)]"
            if field.validation.IsSome then "border-[var(--fve-critical-ring)] focus-visible:outline-[var(--fve-critical-ring)]"
            else "border-[var(--fve-border)] focus-visible:outline-[var(--fve-brand-ring)]" ]

    let attributes includeName (controlClasses:string) descriptionIds field =
        [ _id field.id
          if includeName then _name field.name
          _class controlClasses
          _disabled field.disabled
          // Pending fields prevent edits without dropping their submitted values.
          _readonly field.pending
          _required (field.required && not (field.disabled || field.pending))
          _ariaRequired field.required
          _ariaInvalid field.validation.IsSome
          if field.pending then _ariaBusy true
          let describedBy =
              [ yield! descriptionIds
                if field.description.IsSome then field.id + "-description"
                if field.validation.IsSome then field.id + "-validation" ]
              |> String.concat " "
          if describedBy <> "" then _ariaDescribedby describedBy
          yield! ComponentHtml.safeAttributes
              [ "id"; "name"; "class"; "type"; "value"; "rows"; "disabled"; "readonly"; "required"; "role"; "aria-label"; "aria-labelledby"
                "aria-required"; "aria-invalid"; "aria-busy"; "aria-describedby" ] field.attributes ]

    let render (field:TextFieldData) (control:HtmlElement) =
        div {
            _class "grid min-w-0 content-start gap-1.5 [overflow-wrap:anywhere]"
            label {
                _for field.id
                _class (if field.labelVisuallyHidden then "sr-only" else "flex items-center gap-2 text-sm font-medium text-[var(--fve-text)]")
                field.label
                if field.required then
                    span {
                        _ariaHidden "true"
                        _class "text-[var(--fve-critical-text)]"
                        "*"
                    }
                if field.pending then ComponentHtml.loadingGlyph ControlSize.Small
            }
            control
            match field.description with
            | Some description ->
                p {
                    _id (field.id + "-description")
                    _class "text-sm text-[var(--fve-muted-text)]"
                    description
                }
            | None -> ()
            match field.validation with
            | Some message ->
                p {
                    _id (field.id + "-validation")
                    _class "text-sm text-[var(--fve-critical-text)]"
                    message
                }
            | None -> ()
        }

[<NoEquality; NoComparison>]
type InputConfig = private { field:TextFieldData; kind:InputType; leadingIcon:HtmlElement option; prefix:string option; suffix:string option }

[<RequireQualifiedAccess>]
module Input =
    let create name label : InputConfig =
        { field = TextField.create "fve-input-" name label; kind = InputType.Text; leadingIcon = None; prefix = None; suffix = None }
    /// A decorative, non-interactive icon; the visible field label still supplies the accessible name.
    let withLeadingIcon icon (config:InputConfig) = { config with leadingIcon = Some icon }
    /// Non-editable context, associated with the input but not included in its submitted value.
    let withPrefix text (config:InputConfig) = { config with prefix = Some (TextField.requiredText (nameof text) text) }
    /// Non-editable units/context, associated with the input but not included in its submitted value.
    let withSuffix text (config:InputConfig) = { config with suffix = Some (TextField.requiredText (nameof text) text) }
    let withId id (config:InputConfig) = { config with field = { config.field with id = TextField.stableId id } }
    let id (config:InputConfig) = config.field.id
    let withType kind (config:InputConfig) = { config with kind = kind }
    let withSize size (config:InputConfig) = { config with field = { config.field with size = Some size } }
    let withVisuallyHiddenLabel (config:InputConfig) = { config with field = { config.field with labelVisuallyHidden = true } }
    let withValue value (config:InputConfig) = { config with field = { config.field with value = value } }
    let withDescription text (config:InputConfig) = { config with field = { config.field with description = Some (TextField.requiredText (nameof text) text) } }
    let withValidation text (config:InputConfig) = { config with field = { config.field with validation = Some (TextField.requiredText (nameof text) text) } }
    let withAttributes attributes (config:InputConfig) = { config with field = { config.field with attributes = config.field.attributes @ attributes } }
    let required (config:InputConfig) = { config with field = { config.field with required = true } }
    let disabled (config:InputConfig) = { config with field = { config.field with disabled = true } }
    let pending (config:InputConfig) = { config with field = { config.field with pending = true } }

    let private renderPresentation includeName (config:InputConfig) =
        let field = config.field
        let kind =
            match config.kind with
            | InputType.Text -> "text" | InputType.Email -> "email" | InputType.Telephone -> "tel"
            | InputType.Password -> "password" | InputType.Number -> "number" | InputType.Search -> "search"
            | InputType.Url -> "url" | InputType.Date -> "date" | InputType.Time -> "time" | InputType.DateTimeLocal -> "datetime-local"
        let search = config.kind = InputType.Search
        let leadingIcon =
            match config.leadingIcon with
            | None when search ->
                // Heroicons magnifying-glass (decorative; the label names the field).
                Some (raw """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="size-5"><path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.197 5.197a7.5 7.5 0 0 0 10.606 10.606Z"/></svg>""")
            | icon -> icon
        let decorated = leadingIcon.IsSome || config.prefix.IsSome || config.suffix.IsSome
        let control =
            input {
                let classes =
                    if decorated then "block w-full min-w-0 min-h-[calc(var(--fve-control-min-height)-2px)] flex-1 border-0 bg-transparent px-0 py-[max(0px,calc((var(--fve-control-min-height)-var(--fve-control-line-height)-2px)/2))] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] text-[var(--fve-text)] placeholder:text-[var(--fve-muted-text)] outline-none disabled:cursor-not-allowed"
                    else ComponentHtml.classes [ TextField.classes field; if search then "pr-[var(--fve-control-min-height)]" ]
                let descriptionIds =
                    [ if config.prefix.IsSome then yield field.id + "-prefix"
                      if config.suffix.IsSome then yield field.id + "-suffix" ]
                for attribute in TextField.attributes includeName classes descriptionIds field do attribute
                _type kind
                if search && not (field.attributes |> List.exists (fun attribute -> attribute.Name = "placeholder")) then _placeholder "Search…"
                _value field.value
            }
        let framed =
            if not decorated then control
            else
                div {
                    _class (ComponentHtml.classes [
                        ComponentHtml.controlSizeClass field.size
                        "fve-input-frame flex min-w-0 items-center gap-2 rounded-[var(--fve-radius-control)] border pl-3 has-[input:focus-visible]:outline-2 has-[input:focus-visible]:outline-offset-2"
                        if search then "pr-[var(--fve-control-min-height)]" else "pr-3"
                        if field.pending then "bg-[var(--fve-neutral-subtle)]" else "bg-[var(--fve-surface)]"
                        if field.disabled then "cursor-not-allowed opacity-50"
                        if field.validation.IsSome then "border-[var(--fve-critical-ring)] has-[input:focus-visible]:outline-[var(--fve-critical-ring)]"
                        else "border-[var(--fve-border)] has-[input:focus-visible]:outline-[var(--fve-brand-ring)]" ])
                    match leadingIcon with
                    | Some icon -> span { _ariaHidden true; _class "pointer-events-none flex size-5 shrink-0 items-center justify-center text-[var(--fve-muted-text)]"; icon }
                    | None -> ()
                    match config.prefix with
                    | Some text ->
                        span {
                            _id (field.id + "-prefix")
                            _class "min-w-0 max-w-[40%] break-all text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] text-[var(--fve-muted-text)]"
                            text
                        }
                    | None -> ()
                    control
                    match config.suffix with
                    | Some text ->
                        span {
                            _id (field.id + "-suffix")
                            _class "min-w-0 max-w-[40%] break-all text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] text-[var(--fve-muted-text)]"
                            text
                        }
                    | None -> ()
                }
        if not search then framed
        else
            let unavailable = field.disabled || field.pending
            div {
                _class (ComponentHtml.classes [ ComponentHtml.controlSizeClass field.size; "fve-search relative min-w-0" ])
                framed
                button {
                    _type "button"
                    _ariaLabel ("Clear " + field.label)
                    _disabled unavailable
                    _class "absolute inset-y-0 right-1 my-auto flex size-[calc(var(--fve-control-min-height)-0.5rem)] items-center justify-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:invisible"
                    _dataOn ("click", "const field = el.parentElement.querySelector('input'); if (field.disabled || field.readOnly) return; field.value = ''; field.focus(); field.dispatchEvent(new Event('input', {bubbles: true})); field.dispatchEvent(new Event('change', {bubbles: true}))")
                    span {
                        _ariaHidden true
                        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 1 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 0 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""
                    }
                }
            }

    let render (config:InputConfig) =
        TextField.render config.field (renderPresentation true config)

    /// Renders a labelled search control without a form name or field-layout wrapper for use inside another composite control.
    let renderEmbedded (config:InputConfig) =
        if config.kind <> InputType.Search then invalidArg (nameof config) "Only search inputs can be embedded."
        div {
            label { _for config.field.id; _class "sr-only"; config.field.label }
            renderPresentation false config
        }

[<NoEquality; NoComparison>]
type TextareaConfig = private { field:TextFieldData; rows:int }

[<RequireQualifiedAccess>]
module Textarea =
    let create name label : TextareaConfig = { field = TextField.create "fve-textarea-" name label; rows = 4 }
    let withId id (config:TextareaConfig) = { config with field = { config.field with id = TextField.stableId id } }
    let id (config:TextareaConfig) = config.field.id
    let withValue value (config:TextareaConfig) = { config with field = { config.field with value = value } }
    let withRows rows (config:TextareaConfig) =
        if rows < 1 then invalidArg (nameof rows) "A textarea needs at least one row."
        { config with rows = rows }
    let withDescription text (config:TextareaConfig) = { config with field = { config.field with description = Some (TextField.requiredText (nameof text) text) } }
    let withValidation text (config:TextareaConfig) = { config with field = { config.field with validation = Some (TextField.requiredText (nameof text) text) } }
    let withAttributes attributes (config:TextareaConfig) = { config with field = { config.field with attributes = config.field.attributes @ attributes } }
    let required (config:TextareaConfig) = { config with field = { config.field with required = true } }
    let disabled (config:TextareaConfig) = { config with field = { config.field with disabled = true } }
    let pending (config:TextareaConfig) = { config with field = { config.field with pending = true } }
    let render (config:TextareaConfig) =
        let control =
            textarea {
                for attribute in TextField.attributes true (TextField.classes config.field) [] config.field do attribute
                _rows config.rows
                config.field.value
            }
        TextField.render config.field control

[<NoEquality; NoComparison>]
type FieldError = private { fieldId:string; label:string; message:string }

[<RequireQualifiedAccess>]
module FieldError =
    let create fieldId label message =
        { fieldId = TextField.stableId fieldId
          label = TextField.requiredText (nameof label) label
          message = TextField.requiredText (nameof message) message }

[<NoEquality; NoComparison>]
type ErrorSummaryConfig = private { id:string; title:string; errors:FieldError list; focusOnMount:bool }

[<RequireQualifiedAccess>]
module ErrorSummary =
    let create id title errors =
        if List.isEmpty errors then invalidArg (nameof errors) "An error summary needs at least one error."
        { id = TextField.stableId id; title = TextField.requiredText (nameof title) title; errors = errors; focusOnMount = false }
    /// Focus a newly inserted summary after a server validation response. Does not continually steal focus.
    let focusOnMount (config:ErrorSummaryConfig) = { config with focusOnMount = true }
    let render (config:ErrorSummaryConfig) =
        div {
            _id config.id
            _role "alert"
            _tabindex -1
            _ariaLabelledby (config.id + "-title")
            _class "[overflow-wrap:anywhere] rounded-[var(--fve-radius-control)] border border-[var(--fve-critical-ring)] bg-[var(--fve-critical-subtle)] p-4 text-sm text-[var(--fve-critical-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-critical-ring)]"
            if config.focusOnMount then _dataInit "el.focus()"
            p {
                _id (config.id + "-title")
                _class "font-semibold"
                config.title
            }
            ul {
                _class "mt-2 list-disc space-y-1 pl-5"
                for error in config.errors do
                    li {
                        a {
                            _href ("#" + Uri.EscapeDataString error.fieldId)
                            _class "underline underline-offset-2 focus-visible:outline-2 focus-visible:outline-offset-2"
                            _dataOn ("click", $"document.getElementById({ComponentHtml.javascriptString error.fieldId})?.focus()")
                            error.label + ": " + error.message
                        }
                    }
            }
        }
