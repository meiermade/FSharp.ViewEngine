namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html
open type Datastar

module internal Switch =
    let private flag name attributes =
        ComponentElement.attribute name attributes |> Option.exists (fun attribute -> attribute.Value <> ValueSome "false")

    let render attributes children =
        ComponentContent.noChildren "fveSwitch" children
        ComponentElement.choice "type" "checkbox" ["checkbox"] attributes |> ignore
        let track, thumb, translation =
            match ComponentElement.size (Some Size.Medium) attributes |> Option.get with
            | Size.ExtraSmall -> "h-3.5 w-6", "after:size-2.5", "peer-checked:after:translate-x-2.5"
            | Size.Small -> "h-4 w-7", "after:size-3", "peer-checked:after:translate-x-3"
            | Size.Medium -> "h-5 w-9", "after:size-4", "peer-checked:after:translate-x-4"
            | Size.Large -> "h-6 w-11", "after:size-5", "peer-checked:after:translate-x-5"
            | Size.ExtraLarge -> "h-7 w-13", "after:size-6", "peer-checked:after:translate-x-6"
        let readOnly = flag "readonly" attributes
        let defaults = [
            _type "checkbox"; _role "switch"; _value "true"
            _class "peer absolute inset-0 z-10 m-0 size-full cursor-pointer opacity-0 disabled:cursor-not-allowed [&[readonly]]:cursor-default"
            if readOnly then
                _ariaReadonly true ]
        let controlAttributes = ComponentHtml.safeAttributes (["size"] @ if readOnly || flag "disabled" attributes then ["required"] else []) attributes
        (Html.el "fve-switch") {
            _class "relative mt-0.5 inline-flex shrink-0 align-middle"
            match ComponentElement.value "id" attributes with Some id -> _id (id + "-host") | None -> ()
            input { for attribute in ComponentElement.attributes (defaults @ ComponentHtml.outerSignals controlAttributes) do attribute }
            span {
                _ariaHidden true
                _class ("pointer-events-none relative block rounded-full bg-[var(--fve-neutral-subtle)] ring-1 ring-inset ring-[var(--fve-border)] peer-[:enabled:not(:checked):not([readonly]):not([aria-invalid=true]):hover]:ring-[var(--fve-muted-text)] transition-colors peer-checked:bg-[var(--fve-brand-solid)] peer-checked:ring-[var(--fve-brand-solid)] peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-[var(--fve-brand-ring)] peer-aria-invalid:ring-[var(--fve-critical-ring)] peer-disabled:opacity-50 after:absolute after:left-0.5 after:top-0.5 after:rounded-full after:bg-white after:shadow-sm after:transition-transform after:content-[''] forced-colors:ring-[ButtonText] forced-colors:peer-checked:bg-[Highlight] " + track + " " + thumb + " " + translation)
            }
        }

/// <category>switch</category>
[<AutoOpen>]
module SwitchDsl =
    /// Native checkbox with switch semantics. Compose its label and feedback with Field.
    let fveSwitch = ComponentElementBuilder(Switch.render)
