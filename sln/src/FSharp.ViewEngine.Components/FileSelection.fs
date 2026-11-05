namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

module internal FileSelection =
    let render attributes children =
        ComponentContent.noChildren "fveFileSelection" children
        let attributes = ComponentElement.attributes attributes
        let id = ComponentElement.required "id" attributes
        if id |> Seq.exists Char.IsWhiteSpace then invalidArg "attributes" "A stable file control ID without whitespace is required."
        ComponentElement.required "name" attributes |> ignore
        ComponentElement.choice "type" "file" ["file"] attributes |> ignore
        let flag name = ComponentElement.attribute name attributes |> Option.exists (fun attribute -> attribute.Value <> ValueSome "false")
        let disabled, readOnly = flag "disabled", flag "readonly"
        let defaults = [
            _type "file"
            if readOnly then
                _ariaDisabled true
            _class "block min-h-[var(--fve-control-min-height)] w-full min-w-0 cursor-pointer rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] text-sm text-[var(--fve-text)] aria-invalid:border-[var(--fve-critical-ring)] aria-invalid:focus-visible:outline-[var(--fve-critical-ring)] user-invalid:border-[var(--fve-critical-ring)] user-invalid:focus-visible:outline-[var(--fve-critical-ring)] file:me-3 file:min-h-[calc(var(--fve-control-min-height)-2px)] file:cursor-pointer file:border-0 file:bg-[var(--fve-neutral-subtle)] file:px-3 file:text-sm file:font-semibold file:text-[var(--fve-text)] file:hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-50 disabled:file:cursor-not-allowed" ]
        let update =
            "const field = el.querySelector('input[type=file]'); const clear = el.querySelector('[data-fve-file-clear]'); const labelled = (field.getAttribute('aria-labelledby') || '').split(/\\s+/).map(id => field.ownerDocument.getElementById(id)?.textContent?.trim()).filter(Boolean).join(' '); const label = labelled || field.getAttribute('aria-label')?.trim() || Array.from(field.labels || []).map(label => label.textContent.trim()).filter(Boolean).join(' ') || 'file'; clear.setAttribute('aria-label', 'Clear ' + label + ' selection' + (field.files.length === 1 ? ': ' + field.files[0].name : '')); clear.hidden = field.files.length === 0;"
        div {
            _attr ("data-fve-file-selection", "")
            _class "flex min-w-0 flex-wrap items-center gap-2 [&>input]:flex-[1_0_min(12rem,100%)] [&>input]:w-auto has-[input:disabled]:[&>button]:hidden has-[input[readonly]]:[&>button]:hidden"
            _dataInit update
            _dataOn ("change", update)
            _dataOn ("reset__window", "if (evt.target === el.querySelector('input').form) queueMicrotask(() => { if (!evt.defaultPrevented) { " + update + " } })")
            input {
                for attribute in ComponentElement.attributes (defaults @ (ComponentHtml.safeAttributes (if disabled || readOnly then ["required"] else []) attributes |> ComponentHtml.readOnlyGuards ["Enter"; " "])) do attribute
            }
            button {
                _type "button"
                _id (id + "-clear")
                _dataPreserveAttr "hidden aria-label"
                _attr ("data-fve-file-clear", "")
                _ariaLabel "Clear file selection"
                _hidden true
                _class "min-h-8 shrink-0 rounded-[var(--fve-radius-control)] px-2 py-1 text-sm text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                _dataOn ("click", "const field = el.parentElement.querySelector('input[type=file]'); if (field.disabled || field.readOnly) return; field.value = ''; field.dispatchEvent(new Event('input', {bubbles:true})); field.dispatchEvent(new Event('change', {bubbles:true})); field.focus()")
                "Clear"
            }
        }

/// <category>file-selection</category>
[<AutoOpen>]
module FileSelectionDsl =
    /// Native file selection with contextual clearing. Field owns its label, help and feedback.
    let fveFileSelection = ComponentElementBuilder(FileSelection.render)
