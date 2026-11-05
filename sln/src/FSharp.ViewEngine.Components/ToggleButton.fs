namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>toggle-button</category>
[<RequireQualifiedAccess>]
type ToggleButtonVariant = Soft | Outline | Ghost

/// <category>toggle-button</category>
[<AutoOpen>]
module ToggleButtonAttributes =
    type Html with
        static member _variant(value:ToggleButtonVariant) =
            Html._attr("variant", match value with ToggleButtonVariant.Soft -> "soft" | ToggleButtonVariant.Outline -> "outline" | ToggleButtonVariant.Ghost -> "ghost")

module internal ToggleButton =
    let render attributes children =
        ComponentElement.choice "type" "button" ["button"] attributes |> ignore
        let appearance = ComponentElement.choice "variant" "outline" ["soft"; "outline"; "ghost"] attributes
        let variant =
            match appearance with
            | "soft" -> "bg-[var(--fve-surface-subtle)]"
            | "ghost" -> "bg-transparent"
            | _ -> "[--fve-control-border-width:1px] border border-[var(--fve-border)] bg-[var(--fve-surface)]"
        let size = ComponentElement.size None attributes
        let readOnly = ComponentElement.attribute "readonly" attributes |> Option.exists (fun attribute -> attribute.Value <> ValueSome "false")
        let defaults = [
            _type "button"
            _data ("fve-group-control", if appearance = "outline" then "outline" else "plain")
            _ariaPressed false
            if readOnly then _ariaDisabled true
            else _dataOn ("click", "el.setAttribute('aria-pressed', el.getAttribute('aria-pressed') === 'true' ? 'false' : 'true'); el.dispatchEvent(new Event('change', {bubbles:true}))")
            _class ("inline-flex items-center justify-center gap-2 rounded-[var(--fve-radius-control)] font-medium text-[var(--fve-text)] outline-none transition-colors hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)] aria-pressed:bg-[var(--fve-brand-subtle)] aria-pressed:text-[var(--fve-brand-text)] disabled:pointer-events-none disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0 " + ComponentHtml.sizeClasses size + " " + variant) ]
        ComponentElement.render "button" defaults (ComponentHtml.safeAttributes ["size"; "variant"; "readonly"] attributes) children

/// <category>toggle-button</category>
[<AutoOpen>]
module ToggleButtonDsl =
    /// A native pressed-state button. Authored click/binding attributes can own application state.
    let fveToggleButton = ComponentElementBuilder(ToggleButton.render)
