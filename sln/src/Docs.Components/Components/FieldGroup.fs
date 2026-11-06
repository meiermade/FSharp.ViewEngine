namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal FieldGroup =
    let render attributes children =
        let layout = ComponentElement.choice "layout" "vertical" ["vertical"; "horizontal"; "responsive"] attributes
        let classes =
            match layout with
            | "horizontal" -> "flex flex-wrap items-start gap-4 [&>p]:w-full"
            | "responsive" -> "grid gap-4 sm:grid-cols-2 [&>p]:col-span-full"
            | _ -> "grid gap-3"
        ComponentElement.render "fieldset" [_class ("min-w-0 [overflow-wrap:anywhere] " + classes)] (ComponentHtml.safeAttributes ["layout"] attributes) children

    let legend attributes children =
        // A native legend sits outside the fieldset's grid; gap does not separate its content.
        ComponentElement.render "legend" [_class "mb-1.5 text-sm font-semibold text-[var(--fve-text)]"] attributes children

/// <category>field-group</category>
[<AutoOpen>]
module FieldGroupDsl =
    /// A native fieldset. Compose its legend, description and fields as ordinary children.
    let fveFieldGroup = ComponentElementBuilder(FieldGroup.render)
    let fveFieldLegend = ComponentElementBuilder(FieldGroup.legend)
