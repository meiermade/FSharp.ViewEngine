namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

module internal Textarea =
    let render attributes (children:HtmlElement list) =
        let rows = ComponentElement.value "rows" attributes |> Option.defaultValue "4"
        match Int32.TryParse rows with
        | true, count when count > 0 -> ()
        | _ -> invalidArg "attributes" "A textarea needs at least one row."
        if ComponentElement.attribute "value" attributes |> Option.isSome && not children.IsEmpty then
            invalidArg "children" "Use either a value attribute or textarea content, not both."
        textarea {
            for attribute in TextField.attributes [_class (TextField.classes attributes); _attr("data-fve-input-control", ""); _attr("rows", rows)] (ComponentHtml.safeAttributes ["value"] attributes) do attribute
            match ComponentElement.value "value" attributes with
            | Some value -> value
            | None -> children
        }

/// <category>textarea</category>
[<AutoOpen>]
module TextareaDsl =
    /// A native textarea. Compose its label and help with fveField.
    let fveTextarea = ComponentElementBuilder(Textarea.render)
