namespace Docs.ComponentPages.ToggleButton

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
    let toggleButtonRegistration =
        registration "components-toggle-button" "/components/toggle-button" "Toggle button" "Toggle button"

    let stackedSwitch =
        fveField {
            fveLabel {
                _for "automaticExports"
                "Automatic exports"
            }

            fveSwitch {
                _id "automaticExports"
                _name "automaticExports"
                _ariaDescribedby "automaticExports-help"
            }

            fveFieldDescription {
                _id "automaticExports-help"
                "Generate an export when new files are published."
            }
        }

    let compactRows =
        fveToggleButton {
            _id "components-compact-rows"
            "Compact rows"
        }

    let pendingCompactRows =
        fveToggleButton {
            _id "components-pending-compact-rows"
            _readonly true
            _ariaBusy true
            "Applying compact rows"
        }

    let disabledCompactRows =
        fveToggleButton {
            _id "components-disabled-compact-rows"
            _disabled true
            "Compact rows unavailable"
        }

    let toggleButtonPresentation =
        div {
            _class "flex flex-wrap items-center gap-3"

            fveToggleButton {
                _id "toggle-soft"
                _variant ToggleButtonVariant.Soft
                _size Size.Small
                plusIcon
                "Add item"
            }

            fveToggleButton {
                _id "toggle-outline"
                _variant ToggleButtonVariant.Outline
                _ariaPressed true
                plusIcon
                "Add item"
            }

            fveToggleButton {
                _id "toggle-ghost"
                _variant ToggleButtonVariant.Ghost
                _size Size.Large
                plusIcon
                "Add item"
            }
        }

    let examples () =
            [ sample "toggle-button" "Default" [ "compactRows" ] (centered compactRows)
              sample "toggle-button-pending" "Pending" [ "pendingCompactRows" ] (centered pendingCompactRows)
              sample "toggle-button-disabled" "Disabled" [ "disabledCompactRows" ] (centered disabledCompactRows)
              sample
                  "toggle-button-presentation"
                  "Content, sizes and variants"
                  [ "plusIcon"; "toggleButtonPresentation" ]
                  (centered toggleButtonPresentation) ]


    let private galleryContent : Content =
        { description = "Toggle a pressed action with typed presentation, size, and availability states."
          usage = []
          sections = []
          apiCategory = Some "toggle-button" }

    let toggleButtonPage =
        gallery toggleButtonRegistration galleryContent (examples ())
