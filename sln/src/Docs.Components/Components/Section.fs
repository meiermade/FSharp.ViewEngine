namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal Section =
    let actions attributes children =
        ComponentPart("section", "actions", ComponentContent.fragment children |> fun content -> ComponentElement.render "div" [] attributes [content]) :> HtmlElement

    let render attributes children =
        let title = ComponentElement.required "label" attributes
        let parts, content = ComponentParts.split "section" children
        ComponentElement.render "section"
            [_ariaLabel title; _data("fve-section", ""); _class "grid min-w-0 grid-cols-1 content-start gap-4"]
            (ComponentHtml.safeAttributes ["label"; "description"; "heading-level"] attributes)
            [ fveSectionHeader {
                  _label title
                  yield! attributes |> List.filter (fun attribute -> List.contains attribute.Name ["description"; "heading-level"])
                  match Map.tryFind "actions" parts with
                  | Some actions -> fveSectionHeaderActions { actions }
                  | None -> ()
              }
              yield! content ]

    let stack attributes children =
        ComponentElement.render "div"
            [_data("fve-section-stack", ""); _class "grid min-w-0 grid-cols-1 content-start gap-12 @3xl/fve-shell:gap-16"]
            attributes children

/// <summary>Borderless sections with consistent header/content and responsive inter-section spacing.</summary>
/// <category>section</category>
[<AutoOpen>]
module SectionDsl =
    let fveSection = ComponentElementBuilder(Section.render)
    let fveSectionActions = ComponentElementBuilder(Section.actions)
    let fveSectionStack = ComponentElementBuilder(Section.stack)
