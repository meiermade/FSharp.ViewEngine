namespace Docs.ComponentPages.Section

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let sectionRegistration =
        registration "components-section" "/components/section" "Section" "Section"

    let defaultSection =
        fveSection {
            _label "Details"
            fveSectionActions { fveLink { _href "/components/section-header"; "Header reference" } }
            p { "A section keeps its heading and content aligned." }
        }

    let sectionStackExample =
        fveSectionStack {
            fveSection { _label "Details"; _headingLevel SectionHeadingLevel.H3; p { "Description and properties." } }
            fveSection { _label "History"; _headingLevel SectionHeadingLevel.H3; p { "Related activity." } }
        }

    let periodNote =
        Section.withoutHeader
            "Supporting note"
            (p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Items are shown in the order they were added."
            })
        |> Section.render

    let examples () =
            [ sample "section-default" "Default" [ "defaultSection" ] (detailsSurface defaultSection)
              sample "section-stack" "Section spacing" [ "sectionStackExample" ] (detailsSurface sectionStackExample)
              |> note "fveSection uses 16px between its header and content. fveSectionStack uses 48px between sections, increasing to 64px inside a wide fve-shell container. Actions belong in fveSectionActions; use SectionHeadingLevel.H3 when nesting sections." ]


    let private galleryContent : Content =
        { description = "Compose borderless sections with consistent heading, action and content spacing."
          usage = []
          sections = []
          apiCategory = Some "section" }

    let sectionPage = gallery sectionRegistration galleryContent (examples ())
