namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal Separator =
    let render attributes children =
        ComponentContent.noChildren "Separator" children
        let orientation = ComponentElement.choice "orientation" "horizontal" ["horizontal"; "vertical"] attributes
        let role = ComponentElement.choice "role" "presentation" ["separator"; "presentation"; "none"] attributes
        let semantic = role = "separator" || ComponentElement.value "aria-hidden" attributes = Some "false"
        let classes =
            if orientation = "vertical" then "h-full min-h-4 w-px shrink-0 self-stretch bg-[var(--fve-border)]"
            else "h-px w-full shrink-0 bg-[var(--fve-border)]"
        let semantics =
            if semantic then [_role "separator"; _ariaOrientation orientation]
            else [_ariaHidden true]
        ComponentElement.render "div" ([_class classes] @ semantics) (ComponentHtml.safeAttributes ["orientation"] attributes) []

/// <category>separator</category>
[<AutoOpen>]
module SeparatorDsl =
    /// A decorative separator by default. Use role=separator for semantic separation.
    let fveSeparator = ComponentElementBuilder(Separator.render)
