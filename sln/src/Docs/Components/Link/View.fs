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

    let textUnderlineExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"
            fveLink {
                _href "/components"
                _underline LinkUnderline.Always
                "Always underlined"
            }
            fveLink {
                _href "/components"
                "Underline on interaction (default)"
            }
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

    let plainLink =
        fveNotice {
            _label "Inherited presentation"
            _color Color.Info
            _variant NoticeVariant.Solid
            fveLink {
                _href "/components/notice"
                _variant LinkVariant.Plain
                _class "underline"
                "Read notice documentation"
            }
        }

    let uniformPaddingLinks =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            for size, label in [ Size.ExtraSmall, "Extra small"; Size.Small, "Small"; Size.Medium, "Medium"; Size.Large, "Large"; Size.ExtraLarge, "Extra large" ] do
                for variant, treatment in [ "Outline", LinkVariant.Outline; "Ghost", LinkVariant.Ghost ] do
                    fveLink {
                        _href "/examples/application/settings"
                        _native true
                        _variant treatment
                        _size size
                        _padding Padding.Uniform
                        _ariaLabel (label + " " + variant + " settings")
                        fveIcon { _name IconName.Cog6Tooth; _size Size.Small }
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

    let navigationModes =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"
            fveLink { _href "/components/input"; "Navigate in place" }
            fveLink { _href "/components/button"; "Open Button docs" }
            fveLink { _href "/components"; _native true; "Load a new page" }
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
              sample "link-underline" "Text underline" [ "textUnderlineExamples" ] (connectedCentered textUnderlineExamples)
              |> note "Text links default to LinkUnderline.OnInteraction: no underline at rest, with an underline on hover or keyboard focus. Use _underline LinkUnderline.Always for a resting underline. Both retain the same keyboard focus ring. In prose, keep a visible resting cue: use Always where the link color does not contrast sufficiently with surrounding text."
              sample "link-plain" "Inherited presentation" [ "plainLink" ] (connectedCentered plainLink)
              |> note "Plain keeps the same navigation behavior and a current-color keyboard outline without imposing typography, color or control geometry. Use it for branded/card links and links inside an already colored surface; the consumer owns that composition."
              sample "link-sizes" "Sizes" [ "linkSizeExamples" ] (connectedCentered linkSizeExamples)
              sample "link-padding" "Uniform padding" [ "uniformPaddingLinks" ] (connectedCentered uniformPaddingLinks)
              |> note "Padding.Uniform uses the size’s vertical inset on every side, producing a square control around a square child. Default padding is unchanged. Uniform applies to control variants; Text remains an inline link without control padding."
              sample "link-colors" "Colors" [ "linkColorExamples" ] (connectedCentered linkColorExamples)
              sample "link-variants" "Variants" [ "linkVariantExamples" ] (connectedCentered linkVariantExamples)
              |> note "Ghost links stay flat, without a decorative shadow at rest, on hover or while pressed. They use the same navigation behavior and keyboard focus ring as other variants."
              sample "link-color-variants" "All color variants" [ "actionColors"; "actionPaletteGrid"; "linkPaletteExamples" ] (connectedCentered linkPaletteExamples)
              |> note "Hover and press retain native link behavior. Text links strengthen their foreground/underline; control-like variants use the same action palette as Button, including visibly tinted built-in light-mode Soft fills. Custom palettes and soft-color token overrides retain their authored values."
              sample "link-icon-variants" "Icon-only variants" [ "linkIconVariants" ] (connectedCentered linkIconVariants)
              sample "link-icon-sizes" "Icon-only sizes" [ "linkIconSizes" ] (connectedCentered linkIconSizes)
              |> note "Use an ordinary Link with a square Icon child and an accessible name. Control variants use the same horizontal/vertical padding and minimum height as Button; Text keeps the icon's own dimensions without control padding. Icon-only links use the same navigation rules as text links."
              sample "link-new-tab" "Native new-tab navigation" [ "newTabLink" ] (connectedCentered newTabLink)
              sample "link-navigation" "Navigation modes" [ "navigationModes" ] (connectedCentered navigationModes) ]


    let private galleryContent : Content =
        { description = "A real anchor with in-place navigation, browser history, and optional link styling."
          usage = [ prose "fveLink uses Datastar @get() on the real href by default. Use _native true for a normal browser page load. Plain anchors, external links, modified clicks, new tabs, downloads, and fragment links stay native." ]
          sections = [
              titledSection "page-navigation" "Server responses"
                  [ p {
                        "Link doesn’t change the URL itself. The server decides what to patch and whether to update history. See "
                        fveLink { _href "/guides/giraffe#history"; _underline LinkUnderline.Always; "Server-owned navigation in Giraffe" }
                        " for pushUrl and Back/Forward."
                    } ]
          ]
          apiCategory = Some "link" }

    let linkPage = gallery linkRegistration galleryContent (examples ())
