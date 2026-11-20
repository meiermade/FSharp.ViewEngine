namespace Docs.ComponentPages.Field

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
    let fieldRegistration =
        registration "components-field" "/components/field" "Field" "Field"

    let defaultField =
        fveField {
            fveLabel {
                _for "reference"
                "Reference"
            }

            fveInput {
                _id "reference"
                _name "reference"
            }
        }

    let customQuantityField =
        fveField {
            fveLabel {
                _for "item-quantity"
                "Quantity"
            }

            input {
                _id "item-quantity"
                _name "quantity"
                _type "number"
                _required true
                _step "0.01"
                _ariaDescribedby "item-quantity-description"

                _class
                    "min-h-[var(--fve-control-min-height)] w-full rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
            }

            fveFieldDescription {
                _id "item-quantity-description"
                "Enter the number of items."
            }
        }

    let responsiveFieldRows =
        div {
            _class "grid gap-4"

            fveField {
                _layout FieldLayout.Responsive

                fveLabel {
                    _for "legal-name"
                    "Legal name"
                }

                fveInput {
                    _id "legal-name"
                    _name "legalName"
                    _ariaDescribedby "legal-name-description"
                }

                fveFieldDescription {
                    _id "legal-name-description"
                    "Use the name on your profile."
                }
            }

            fveField {
                _layout FieldLayout.Responsive

                fveLabel {
                    _for "item-reference"
                    "Reference"
                }

                fveInput {
                    _id "item-reference"
                    _name "itemReference"
                    _ariaInvalid true
                    _ariaDescribedby "item-reference-validation"
                }

                fveFieldError {
                    _id "item-reference-validation"
                    "Enter the nine-character reference."
                }
            }
        }

    let groupedFields =
        fveFieldGroup {
            _ariaDescribedby "delivery-preferences-description"
            fveFieldLegend { "Delivery preferences" }

            fveFieldDescription {
                _id "delivery-preferences-description"
                "Choose one or both delivery methods."
            }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "emailDelivery"
                    _name "emailDelivery"
                    _checked true
                }

                fveLabel {
                    _for "emailDelivery"
                    "Email updates"
                }
            }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "paperDelivery"
                    _name "paperDelivery"
                }

                fveLabel {
                    _for "paperDelivery"
                    "Printed updates"
                }
            }
        }

    let examples () =
            [ sample "field-default" "Default" [ "defaultField" ] (fieldSurface defaultField)
              sample "field" "Custom control" [ "customQuantityField" ] (fieldSurface customQuantityField)
              sample
                  "field-responsive"
                  "Responsive label and control rows"
                  [ "responsiveFieldRows" ]
                  (fieldSurface responsiveFieldRows)
              sample "field-group" "Semantic fieldset" [ "groupedFields" ] (fieldSurface groupedFields) ]


    let private galleryContent : Content =
        { description = "Own labels, descriptions, validation, orientation, and custom form-control composition."
          usage = []
          sections = [
              titledSection "accessibility" "Labels and focus"
                  [ prose "Connect fveLabel's _for to the control's _id. Connect descriptions and errors with _ariaDescribedby, and set _ariaInvalid when the field is invalid; the examples show those relationships explicitly." ]
          ]
          apiCategory = Some "field" }

    let fieldPage = gallery fieldRegistration galleryContent (examples ())
