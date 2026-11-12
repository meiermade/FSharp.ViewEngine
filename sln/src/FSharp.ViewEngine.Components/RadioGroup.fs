namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html
open type Datastar

module internal RadioGroup =
    let render attributes children =
        let orientation = ComponentElement.choice "orientation" "vertical" ["vertical"; "horizontal"] attributes
        let layout = if orientation = "horizontal" then FieldLayout.Horizontal else FieldLayout.Vertical
        FieldGroup.render ([_role "radiogroup"; _layout layout] @ ComponentHtml.safeAttributes ["orientation"] attributes) children

    let radio attributes children =
        ComponentContent.noChildren "fveRadio" children
        ComponentElement.choice "type" "radio" ["radio"] attributes |> ignore
        let flag name = ComponentElement.attribute name attributes |> Option.exists (fun attribute -> attribute.Value <> ValueSome "false")
        let readOnly = flag "readonly"
        let defaults = [
            _type "radio"
            _class "mt-0.5 size-5 shrink-0 cursor-pointer appearance-none rounded-full border border-[var(--fve-border)] not-checked:not-disabled:not-aria-disabled:not-aria-invalid:[&:not([readonly])]:hover:border-[var(--fve-muted-text)] bg-[var(--fve-surface)] checked:border-[var(--fve-brand-solid)] checked:bg-[radial-gradient(var(--fve-brand-solid)_0_45%,transparent_50%)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] aria-invalid:border-[var(--fve-critical-ring)] disabled:cursor-not-allowed disabled:opacity-50 [&[readonly]]:cursor-default forced-colors:appearance-auto forced-colors:bg-none"
            if readOnly then
                _ariaDisabled true ]
        (Html.el "fve-radio") {
            _class "contents"
            match ComponentElement.value "id" attributes with Some id -> _id (id + "-host") | None -> ()
            input {
                for attribute in ComponentElement.attributes (defaults @ (ComponentHtml.safeAttributes (if readOnly || flag "disabled" then ["required"] else []) attributes |> ComponentHtml.outerSignals)) do attribute
            }
        }

/// <category>radio-group</category>
[<AutoOpen>]
module RadioGroupDsl =
    /// Native fieldset/legend semantics with ordinary Field and Radio children.
    let fveRadioGroup = ComponentElementBuilder(RadioGroup.render)
    /// Native string-valued radio. The application owns checked state and name grouping.
    let fveRadio = ComponentElementBuilder(RadioGroup.radio)
