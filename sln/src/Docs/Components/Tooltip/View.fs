namespace Docs.ComponentPages.Tooltip

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
    let tooltipRegistration =
        registration "components-tooltip" "/components/tooltip" "Tooltip" "Tooltip"

    let basicTooltip =
        fveTooltip {
            _id "archive-tooltip"
            _description "Moves the item out of active lists."

            fveButton {
                _ariaDescribedby ("archive-tooltip" + "-content")
                "Archive"
            }
        }

    let bottomTooltip =
        fveTooltip {
            _id "bottom-placement-tooltip"
            _description "Moves the item out of active lists."
            _delay 150
            _side TooltipSide.Bottom

            fveButton {
                _variant ButtonVariant.Outline
                _ariaDescribedby ("bottom-placement-tooltip" + "-content")
                "Archive"
            }
        }

    let describedTooltip =
        div {
            _class "grid gap-3"

            p {
                _id "archive-description"
                _class "text-sm text-[var(--fve-muted-text)]"
                "Archived items remain searchable."
            }

            fveTooltip {
                _id "described-archive-tooltip"
                _description "Moves the item out of active lists."

                fveButton {
                    _ariaDescribedby ("archive-description " + ("described-archive-tooltip" + "-content"))
                    "Archive"
                }
            }
        }

    let examples () =
            [ sample "tooltip" "Default" [ "basicTooltip" ] (centered basicTooltip)
              sample "tooltip-described" "Existing description" [ "describedTooltip" ] (centered describedTooltip)
              sample "tooltip-bottom" "Bottom placement" [ "bottomTooltip" ] (centered bottomTooltip) ]


    let private galleryContent : Content =
        { description = "Provide delayed supplementary text for a labelled control without replacing its accessible name."
          usage = []
          sections = []
          apiCategory = Some "tooltip" }

    let tooltipPage = gallery tooltipRegistration galleryContent (examples ())
