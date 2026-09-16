namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type FileSelectionConfig =
    private
        { id:string
          name:string
          label:string
          description:string option
          accept:string option
          multiple:bool
          required:bool
          disabled:bool
          validation:string option }

[<RequireQualifiedAccess>]
module FileSelection =
    let create id name label =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable file field ID is required."
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A file form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A file field label is required."
        { id = id; name = name; label = label; description = None; accept = None; multiple = false; required = false; disabled = false; validation = None }
    let withDescription description (config:FileSelectionConfig) = { config with description = Some description }
    let withAccept accept (config:FileSelectionConfig) = { config with accept = Some accept }
    let multiple (config:FileSelectionConfig) = { config with multiple = true }
    let required (config:FileSelectionConfig) = { config with required = true }
    let disabled (config:FileSelectionConfig) = { config with disabled = true }
    let withValidation validation (config:FileSelectionConfig) = { config with validation = Some validation }
    let render config =
        let descriptionId = config.id + "-description"
        let validationId = config.id + "-validation"
        let selectedId = config.id + "-selected"
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        let update =
            "const list = document.getElementById('" + selectedId + "'); "
            + "list.replaceChildren(...Array.from(el.files).map(file => { const item = document.createElement('li'); item.textContent = file.name + ' · ' + Math.ceil(file.size / 1024) + ' KB'; return item })); "
            + "list.hidden = el.files.length == 0"
        div {
            _class "grid gap-2"
            label {
                _for config.id
                _class "text-sm font-medium text-[var(--fve-text)]"
                config.label
            }
            input {
                _id config.id
                _name config.name
                _type "file"
                _multiple config.multiple
                _required (config.required && not config.disabled)
                _disabled config.disabled
                _ariaInvalid config.validation.IsSome
                if describedBy <> "" then _ariaDescribedby describedBy
                match config.accept with
                | Some accept -> _accept accept
                | None -> ()
                _dataOn ("change", update)
                _class "block min-h-[var(--fve-control-min-height)] w-full rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] text-sm text-[var(--fve-text)] ring-1 ring-[var(--fve-border)] file:mr-3 file:min-h-[var(--fve-control-min-height)] file:border-0 file:bg-[var(--fve-neutral-subtle)] file:px-3 file:text-sm file:font-semibold focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50"
            }
            match config.description with
            | Some description ->
                p {
                    _id descriptionId
                    _class "text-sm text-[var(--fve-muted-text)]"
                    description
                }
            | None -> ()
            ul {
                _id selectedId
                _class "grid gap-1 text-sm text-[var(--fve-text)]"
                _hidden true
            }
            button {
                _type "button"
                _disabled config.disabled
                _class "justify-self-start rounded-[var(--fve-radius-control)] px-2 py-1 text-sm font-semibold text-[var(--fve-brand-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                _dataOn ("click", "const field = document.getElementById('" + config.id + "'); field.value = ''; field.dispatchEvent(new Event('change', {bubbles:true})); field.focus()")
                "Clear selected files"
            }
            match config.validation with
            | Some validation ->
                p {
                    _id validationId
                    _role "alert"
                    _class "text-sm text-[var(--fve-critical-text)]"
                    validation
                }
            | None -> ()
        }
