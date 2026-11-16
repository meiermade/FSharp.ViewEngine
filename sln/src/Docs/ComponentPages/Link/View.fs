namespace Docs.ComponentPages.Link

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
open Docs.ComponentPages.Button.View
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let linkRegistration =
        registration "components-link" "/components/link" "Link" "Link"

    let basicLink =
        fveLink {
            _href "/components"
            "Read documentation"
        }

    let linkVariantExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, treatment in
                [ "Text", LinkVariant.Text
                  "Solid", LinkVariant.Solid
                  "Soft", LinkVariant.Soft
                  "Outline", LinkVariant.Outline
                  "Ghost", LinkVariant.Ghost ] do
                fveLink {
                    _variant treatment
                    _href "/components"
                    label
                }
        }

    let linkColorExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, palette in
                [ "Primary", Color.Primary
                  "Secondary", Color.Secondary
                  "Success", Color.Success
                  "Warning", Color.Warning
                  "Error", Color.Error
                  "Info", Color.Info
                  "Neutral", Color.Neutral ] do
                fveLink {
                    _color palette
                    _href "/components"
                    label
                }
        }

    let linkSizeExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, size in [ "Extra small", Size.ExtraSmall; "Small", Size.Small; "Medium", Size.Medium; "Large", Size.Large; "Extra large", Size.ExtraLarge ] do
                fveLink {
                    _variant LinkVariant.Solid
                    _size size
                    _href "/examples"
                    label
                }
        }

    let newTabLink =
        fveLink {
            _variant LinkVariant.Outline
            _href "/examples"
            _target "_blank"
            _rel "noopener"
            "Open examples in a new tab"
        }

    let linkPaletteExamples =
        [ for variant, treatment in [ "Text", LinkVariant.Text; "Solid", LinkVariant.Solid; "Soft", LinkVariant.Soft; "Outline", LinkVariant.Outline; "Ghost", LinkVariant.Ghost ] do
              variant,
              [ for color, label in actionColors do
                    fveLink {
                        _href "/components/link"
                        _variant treatment; _color color
                        _ariaLabel (variant + " " + label)
                        _data("action-color", label.ToLowerInvariant())
                        label
                    } ] ]
        |> actionPaletteGrid

    let examples () =
            [ sample "link-basic" "Default" [ "basicLink" ] (connectedCentered basicLink)
              sample "link-sizes" "Sizes" [ "linkSizeExamples" ] (connectedCentered linkSizeExamples)
              sample "link-colors" "Colors" [ "linkColorExamples" ] (connectedCentered linkColorExamples)
              sample "link-variants" "Variants" [ "linkVariantExamples" ] (connectedCentered linkVariantExamples)
              sample "link-color-variants" "All color variants" [ "actionColors"; "actionPaletteGrid"; "linkPaletteExamples" ] (connectedCentered linkPaletteExamples)
              |> note "Hover and press retain native link behavior. Text links strengthen their foreground/underline; control-like variants use the same action palette as Button. Custom palette colors remain consumer-owned."
              sample "link-icon-variants" "Icon-only variants" [ "linkIconVariants" ] (connectedCentered linkIconVariants)
              sample "link-icon-sizes" "Icon-only sizes" [ "linkIconSizes" ] (connectedCentered linkIconSizes)
              |> note "Use an ordinary Link with a square Icon child and an accessible name. Control variants use the same horizontal/vertical padding and minimum height as Button; Text keeps the icon's own dimensions without control padding. Icon-only links retain native navigation, modified clicks and focus behavior."
              sample "link-new-tab" "Native new-tab navigation" [ "newTabLink" ] (connectedCentered newTabLink) ]


    let private galleryContent : Content =
        { description = $"Use the {linkRegistration.title} component in typed F# interfaces."
          usage = []
          sections = []
          apiCategory = Some "link" }

    let linkPage = gallery linkRegistration galleryContent (examples ())
