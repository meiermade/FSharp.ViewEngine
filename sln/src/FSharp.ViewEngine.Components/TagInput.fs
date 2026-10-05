namespace FSharp.ViewEngine.Components

open System
open System.Text.Json
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>tag-input</category>
[<NoEquality; NoComparison>]
type TagInputConfig =
    private
        { id:string
          name:string
          label:string
          values:string list
          description:string option
          validation:string option
          disabled:bool
          pending:bool }

/// <category>tag-input</category>
[<RequireQualifiedAccess>]
module TagInput =
    let create id name label values =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable tag-input ID is required."
        if String.IsNullOrWhiteSpace name then invalidArg (nameof name) "A tag form name is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A tag-input label is required."
        if values |> List.exists String.IsNullOrWhiteSpace then invalidArg (nameof values) "Tag values cannot be empty."
        if List.distinct values |> List.length <> values.Length then invalidArg (nameof values) "Initial tag values must be unique."
        { id = id; name = name; label = label; values = values; description = None; validation = None; disabled = false; pending = false }
    let withDescription description (config:TagInputConfig) = { config with description = Some description }
    let withValidation validation (config:TagInputConfig) = { config with validation = Some validation }
    let disabled (config:TagInputConfig) = { config with disabled = true }
    let pending (config:TagInputConfig) = { config with pending = true }
    let render config =
        let valuesSignal = ComponentHtml.signalToken (config.id + "-values")
        let messageSignal = ComponentHtml.signalToken (config.id + "-message")
        let invalidSignal = ComponentHtml.signalToken (config.id + "-invalid")
        let values = "$" + valuesSignal
        let message = "$" + messageSignal
        let invalid = "$" + invalidSignal
        let inputId = config.id + "-entry"
        let listId = config.id + "-values"
        let descriptionId = config.id + "-description"
        let validationId = config.id + "-validation"
        let unavailable = config.disabled || config.pending
        let add =
            "(() => { const field = document.getElementById('" + inputId + "'); const candidates = field.value.split(',').map(value => value.trim()).filter(Boolean); "
            + "const additions = candidates.filter((value, index) => !" + values + ".includes(value) && candidates.indexOf(value) == index); "
            + "if (!candidates.length) { " + invalid + " = true; " + message + " = 'Enter a tag before adding it.' } "
            + "else if (!additions.length) { " + invalid + " = true; " + message + " = 'That tag has already been added.' } "
            + "else { " + values + " = [..." + values + ", ...additions]; field.value = ''; " + invalid + " = false; " + message + " = additions.join(', ') + ' added.'; field.focus() } })()"
        let paste =
            "(() => { const pasted = evt.clipboardData?.getData('text') || ''; if (!/[,\\n]/.test(pasted)) return; evt.preventDefault(); "
            + "const candidates = pasted.split(/[,\\n]+/).map(value => value.trim()).filter(Boolean); const additions = candidates.filter((value, index) => !" + values + ".includes(value) && candidates.indexOf(value) == index); "
            + "if (!additions.length) { " + invalid + " = true; " + message + " = 'That tag has already been added.' } else { " + values + " = [..." + values + ", ...additions]; " + invalid + " = false; " + message + " = additions.join(', ') + ' added.' } })()"
        let reconcile =
            "const existing = new Map(Array.from(el.children).map(node => [node.dataset.fveTagValue, node])); "
            + "for (const [index, value] of " + values + ".entries()) { let node = existing.get(value); if (!node) { "
            + "node = document.createElement('li'); node.dataset.fveTagValue = value; node.className = 'inline-flex min-h-8 items-center gap-1 rounded-full bg-[var(--fve-neutral-subtle)] py-1 pr-1 pl-3 text-sm leading-5 text-[var(--fve-text)]'; "
            + "const label = document.createElement('span'); label.textContent = value; node.append(label); "
            + "const field = document.createElement('input'); field.type = 'hidden'; field.name = " + ComponentHtml.javascriptString config.name + "; field.value = value; field.disabled = " + (if unavailable then "true" else "false") + "; node.append(field); "
            + "const remove = document.createElement('button'); remove.type = 'button'; remove.dataset.fveRemoveTag = value; remove.ariaLabel = 'Remove ' + value; remove.textContent = '×'; remove.className = 'inline-grid size-6 cursor-pointer place-items-center rounded-full border-0 bg-transparent p-0 text-base leading-none text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50'; remove.disabled = " + (if unavailable then "true" else "false") + "; node.append(remove); } "
            + "if (el.children[index] !== node) el.insertBefore(node, el.children[index] || null); existing.delete(value) } for (const node of existing.values()) node.remove()"
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        div {
            _dataSignals ("{" + valuesSignal + ": " + JsonSerializer.Serialize(config.values) + ", " + messageSignal + ": '', " + invalidSignal + ": " + (if config.validation.IsSome then "true" else "false") + "}")
            if config.pending then _ariaBusy true
            _class "grid min-w-0 content-start gap-2"
            label {
                _for inputId
                _class "text-sm font-medium text-[var(--fve-text)]"
                config.label
            }
            ul {
                _id listId
                _class "flex flex-wrap gap-2"
                _dataEffect reconcile
                _dataOn ("click", "const button = evt.target.closest('[data-fve-remove-tag]'); if (button) { " + values + " = " + values + ".filter(value => value != button.dataset.fveRemoveTag); " + message + " = button.dataset.fveRemoveTag + ' removed.'; document.getElementById('" + inputId + "')?.focus() }")
            }
            div {
                _class "flex min-w-0 flex-wrap gap-2"
                input {
                    _id inputId
                    _type "text"
                    _disabled unavailable
                    if describedBy <> "" then _ariaDescribedby describedBy
                    _dataAttr ("aria-invalid", invalid + " ? 'true' : 'false'")
                    _autocomplete "off"
                    _class "min-h-[var(--fve-control-min-height)] min-w-0 basis-40 flex-1 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] text-[var(--fve-text)] ring-1 ring-[var(--fve-border)] aria-invalid:ring-[var(--fve-critical-ring)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50"
                    _dataOn ("keydown", "(evt.key == 'Enter' || evt.key == ',') && (evt.preventDefault(), " + add + ")")
                    _dataOn ("paste", paste)
                }
                button {
                    _type "button"
                    _disabled unavailable
                    _class "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm font-semibold ring-1 ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                    _dataOn ("click", add)
                    "Add tag"
                }
            }
            if config.pending then
                p {
                    _role "status"
                    _ariaLive "polite"
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Updating tags…"
                }
            match config.description with
            | Some description ->
                p {
                    _id descriptionId
                    _class "text-sm text-[var(--fve-muted-text)]"
                    description
                }
            | None -> ()
            output {
                _role "status"
                _ariaLive "polite"
                _dataText message
                _dataShow (message + " != ''")
                _style "display:none"
                _class "text-sm text-[var(--fve-muted-text)]"
            }
            match config.validation with
            | Some validation ->
                p {
                    _id validationId
                    _role "alert"
                    _dataShow invalid
                    _class "text-sm text-[var(--fve-critical-text)]"
                    validation
                }
            | None -> ()
        }
