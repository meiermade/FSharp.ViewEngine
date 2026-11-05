namespace Example.Components

open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>checkbox</category>
[<AutoOpen>]
module CheckboxAttributes =
    type Html with
        static member _indeterminate(value:bool) = Html._attr("indeterminate", if value then "true" else "false")

module internal Checkbox =
    let private flag name attributes =
        ComponentElement.attribute name attributes |> Option.exists (fun attribute -> attribute.Value <> ValueSome "false")

    let render attributes children =
        ComponentContent.noChildren "fveCheckbox" children
        ComponentElement.choice "type" "checkbox" ["checkbox"] attributes |> ignore
        let readOnly = flag "readonly" attributes
        let defaults = [
            _type "checkbox"
            _value "true"
            _class "mt-0.5 size-5 shrink-0 cursor-pointer [&[readonly]]:cursor-default appearance-none rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] bg-center bg-no-repeat align-middle text-[var(--fve-text)] checked:border-[var(--fve-brand-solid)] checked:bg-[var(--fve-brand-solid)] checked:bg-[url('data:image/svg+xml,%3Csvg%20xmlns=%22http://www.w3.org/2000/svg%22%20viewBox=%220%200%2020%2020%22%3E%3Cpath%20d=%22m4.5%2010%203.5%203.5%207.5-9%22%20fill=%22none%22%20stroke=%22white%22%20stroke-width=%222.5%22%20stroke-linecap=%22round%22%20stroke-linejoin=%22round%22/%3E%3C/svg%3E')] indeterminate:border-[var(--fve-brand-solid)] indeterminate:bg-[var(--fve-brand-solid)] indeterminate:bg-[url('data:image/svg+xml,%3Csvg%20xmlns=%22http://www.w3.org/2000/svg%22%20viewBox=%220%200%2020%2020%22%3E%3Cpath%20d=%22M5%2010h10%22%20fill=%22none%22%20stroke=%22white%22%20stroke-width=%222.5%22%20stroke-linecap=%22round%22/%3E%3C/svg%3E')] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] aria-invalid:border-[var(--fve-critical-ring)] aria-invalid:focus-visible:outline-[var(--fve-critical-ring)] disabled:cursor-not-allowed disabled:opacity-50 forced-colors:appearance-auto forced-colors:bg-none"
            if flag "indeterminate" attributes then _dataInit "el.indeterminate = true"
            if readOnly then
                _ariaReadonly true
                if flag "required" attributes then _ariaRequired true ]
        let authored = ComponentHtml.safeAttributes (["indeterminate"] @ if readOnly then ["required"] else []) attributes
        input {
            for attribute in ComponentElement.attributes (defaults @ ComponentHtml.readOnlyGuards [] authored) do attribute
        }

/// <category>checkbox</category>
[<AutoOpen>]
module CheckboxDsl =
    /// A native form control. Compose its visible label and feedback with Field.
    let fveCheckbox = ComponentElementBuilder(Checkbox.render)
