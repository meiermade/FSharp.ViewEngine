namespace Docs.ComponentPages.ChoiceCards

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
    let choiceCardsRegistration =
        registration "components-choice-cards" "/components/choice-cards" "Choice cards" "Choice cards"

    let defaultChoiceCards =
        fveChoiceCards {
            _id "default-assistance-choice"
            fveFieldLegend { "Booking assistance" }

            fveChoiceCardsContent {
                for value, caption in [ "self", "Self-service"; "staff", "Staff assisted" ] do
                    fveChoiceCard {
                        fveRadio {
                            _name "defaultAssistance"
                            _value value
                        }

                        fveChoiceCardTitle { caption }
                    }
            }
        }

    let richChoiceCards =
        fveChoiceCards {
            _id "assistance-choice"
            fveFieldLegend { "Booking assistance" }

            fveChoiceCardsContent {
                for value, caption, description, metadata in
                    [ "self",
                      "Self-service",
                      "The participant completes details and waivers before arrival.",
                      "No staff coordination required"
                      "staff",
                      "Staff assisted",
                      "A coordinator confirms equipment and participant details.",
                      "Recommended for group bookings"
                      "unavailable", "Managed service", "Available only to contracted organizations.", "" ] do
                    fveChoiceCard {
                        fveRadio {
                            _name "assistance"
                            _value value
                            _required true

                            if value = "staff" then
                                _checked true

                            if value = "unavailable" then
                                _disabled true
                        }

                        fveChoiceCardContent {
                            fveChoiceCardTitle { caption }
                            fveChoiceCardDescription { description }

                            if metadata <> "" then
                                fveChoiceCardMetadata { metadata }
                        }
                    }
            }
        }

    let multipleChoiceCards =
        fveChoiceCards {
            _id "equipment-choice"
            fveFieldLegend { "Equipment" }

            fveChoiceCardsContent {
                for value, caption, description in
                    [ "helmet", "Helmet", "Required protective equipment."
                      "pads", "Protective pads", "Knee and elbow protection."
                      "radio", "Trail radio", "Unavailable for this booking." ] do
                    fveChoiceCard {
                        fveCheckbox {
                            _name "equipmentIds"
                            _value value
                            if value = "radio" then _disabled true else _checked true
                        }

                        fveChoiceCardContent {
                            fveChoiceCardTitle { caption }
                            fveChoiceCardDescription { description }

                            if value = "helmet" then
                                fveChoiceCardMetadata { "Available" }
                        }
                    }
            }
        }

    let invalidChoiceCards =
        fveChoiceCards {
            _id "invalid-assistance-choice"
            _ariaInvalid true
            _ariaDescribedby "invalid-assistance-error"
            fveFieldLegend { "Booking assistance" }

            fveChoiceCardsContent {
                for value, caption in [ "self", "Self-service"; "staff", "Staff assisted" ] do
                    fveChoiceCard {
                        fveRadio {
                            _name "invalidAssistance"
                            _value value
                            _required true
                            _ariaInvalid true
                            _ariaDescribedby "invalid-assistance-error"
                        }

                        fveChoiceCardTitle { caption }
                    }
            }

            fveFieldError {
                _id "invalid-assistance-error"
                "Choose how this booking will be supported."
            }
        }

    let examples () =
            [ sample "choice-cards-default" "Default" [ "defaultChoiceCards" ] (detailsSurface defaultChoiceCards)
              sample "choice-cards" "Single choice" [ "richChoiceCards" ] (detailsSurface richChoiceCards)
              sample
                  "choice-cards-multiple"
                  "Multiple choices"
                  [ "multipleChoiceCards" ]
                  (detailsSurface multipleChoiceCards)
              sample "choice-cards-validation" "Validation" [ "invalidChoiceCards" ] (detailsSurface invalidChoiceCards) ]


    let private galleryContent : Content =
        { description = "Choose one rich option from labelled cards with descriptions and supporting content."
          usage = []
          sections = []
          apiCategory = Some "choice-cards" }

    let choiceCardsPage = gallery choiceCardsRegistration galleryContent (examples ())
