namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>section-header</category>
[<RequireQualifiedAccess>]
type SectionHeadingLevel = H2 | H3

/// <category>section-header</category>
[<AutoOpen>]
module SectionHeaderAttributes =
    type Html with
        static member _headingLevel(value:SectionHeadingLevel) = Html._attr("heading-level", if value = SectionHeadingLevel.H2 then "h2" else "h3")
        static member _divider(value:bool) = Html._attr("divider", if value then "true" else "false")

// Preserve the title metadata used by the catalog's named section composition,
// without exposing a configuration pipeline or inspecting the rendered HTML tree.
type internal SectionHeaderElement(title:string, element:HtmlElement) =
    inherit HtmlElement()
    member _.Title = title
    override _.Render(writer) = element.Render writer

module internal SectionHeader =
    let title (element:HtmlElement) =
        match element with
        | :? SectionHeaderElement as header -> header.Title
        | _ -> invalidArg "header" "A section header must be authored with fveSectionHeader."

    let actions attributes children =
        if String.IsNullOrWhiteSpace (Render.toString (ComponentContent.fragment children)) then empty
        else ComponentPart("SectionHeader", "actions", ComponentElement.render "div" [_class "flex shrink-0 flex-wrap items-center gap-2"] attributes children) :> HtmlElement

    let render attributes children =
        let title = ComponentElement.required "label" attributes
        let description = ComponentElement.attribute "description" attributes |> Option.map (fun _ -> ComponentElement.required "description" attributes)
        let tag = ComponentElement.choice "heading-level" "h2" ["h2"; "h3"] attributes
        let divider = ComponentElement.value "divider" attributes = Some "true"
        let parts, content = ComponentParts.split "SectionHeader" children
        let element =
            ComponentElement.render "header"
                [_attr("data-fve-section-header", "true"); _class ("fve-control-small @container flex flex-wrap items-start justify-between gap-4" + (if divider then " border-b border-[var(--fve-border)] pb-2" else ""))]
                (ComponentHtml.safeAttributes ["label"; "description"; "heading-level"; "divider"] attributes) [
                    yield div {
                        _class "min-w-0"
                        ComponentElement.render tag [_class ((if tag = "h2" then "text-2xl" else "text-base") + " font-semibold text-[var(--fve-text)]")] [] [text title]
                        match description with
                        | Some description -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                        | None -> ()
                        content
                    }
                    match Map.tryFind "actions" parts with Some actions -> yield actions | None -> () ]
        SectionHeaderElement(title, element) :> HtmlElement

/// <category>section-header</category>
[<AutoOpen>]
module SectionHeaderDsl =
    let fveSectionHeader = ComponentElementBuilder(SectionHeader.render)
    let fveSectionHeaderActions = ComponentElementBuilder(SectionHeader.actions)
