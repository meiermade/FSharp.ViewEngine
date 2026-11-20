namespace Docs.ComponentPages.Badge

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
    let badgeRegistration =
        registration "components-badge" "/components/badge" "Badge" "Badge"

    let badgeExample = fveBadge { "Internal" }

    let badgeColorExamples =
        div {
            _class "flex flex-wrap items-center gap-3"

            for label, color in
                [ "Primary", Color.Primary
                  "Secondary", Color.Secondary
                  "Success", Color.Success
                  "Warning", Color.Warning
                  "Error", Color.Error
                  "Info", Color.Info
                  "Neutral", Color.Neutral ] do
                fveBadge {
                    _color color
                    label
                }
        }

    let badgeVariantExamples =
        div {
            _class "flex flex-wrap items-center gap-3"

            for label, variant in
                [ "Solid", BadgeVariant.Solid
                  "Soft", BadgeVariant.Soft
                  "Outline", BadgeVariant.Outline
                  "Ghost", BadgeVariant.Ghost ] do
                fveBadge {
                    _color Color.Success
                    _variant variant
                    label
                }
        }

    let reviewStatus =
        fveBadge {
            _color Color.Warning
            "Needs review"
        }

    let examples () =
            [ sample "badge" "Default" [ "badgeExample" ] (centered badgeExample)
              sample "badge-colors" "Colors" [ "badgeColorExamples" ] (centered badgeColorExamples)
              |> note
                  "Soft semantic badges blend their fill and text with the surrounding theme for quieter status cues. Neutral and Custom palettes retain their supplied colors; Solid, Outline and Ghost are unchanged."
              sample "badge-variants" "Variants" [ "badgeVariantExamples" ] (centered badgeVariantExamples) ]


    let private galleryContent : Content =
        { description = "Label compact metadata and semantic states with typed colors and presentation variants."
          usage = []
          sections = []
          apiCategory = Some "badge" }

    let badgePage = gallery badgeRegistration galleryContent (examples ())
