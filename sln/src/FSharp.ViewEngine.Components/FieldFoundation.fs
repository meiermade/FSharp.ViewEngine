namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

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
