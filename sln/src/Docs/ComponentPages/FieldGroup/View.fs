namespace Docs.ComponentPages.FieldGroup

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

open Docs.Common.BlockExamples

module View =
    let fields =
        fveFieldGroup {
            _ariaDescribedby "delivery-description"
            fveFieldLegend { "Delivery preferences" }

            fveFieldDescription {
                _id "delivery-description"
                "Choose how to receive updates."
            }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "emailUpdates"
                    _name "emailUpdates"
                }

                fveLabel {
                    _for "emailUpdates"
                    "Email updates"
                }
            }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "printedUpdates"
                    _name "printedUpdates"
                }

                fveLabel {
                    _for "printedUpdates"
                    "Printed updates"
                }
            }
        }

    let fieldsWithoutDescription =
        fveFieldGroup {
            fveFieldLegend { "Optional updates" }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "optionalUpdates"
                    _name "optionalUpdates"
                }

                fveLabel {
                    _for "optionalUpdates"
                    "Send occasional updates"
                }
            }
        }

    let disabledFields =
        fveFieldGroup {
            _disabled true
            fveFieldLegend { "Archived preferences" }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "archivedUpdates"
                    _name "archivedUpdates"
                    _checked true
                }

                fveLabel {
                    _for "archivedUpdates"
                    "Email updates"
                }
            }
        }

    let horizontalFields =
        fveFieldGroup {
            _layout FieldLayout.Horizontal
            fveFieldLegend { "Notification channels" }

            for value, caption in [ "email", "Email"; "sms", "Text message" ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveCheckbox {
                        _id ("channel-" + value)
                        _name "channels"
                        _value value
                    }

                    fveLabel {
                        _for ("channel-" + value)
                        caption
                    }
                }
        }

    let responsiveFields =
        fveFieldGroup {
            _layout FieldLayout.Responsive
            _ariaDescribedby "responsive-delivery-description"
            fveFieldLegend { "Delivery preferences" }

            fveFieldDescription {
                _id "responsive-delivery-description"
                "Options stack on small screens."
            }

            for value, caption in [ "email", "Email updates"; "printed", "Printed updates" ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveCheckbox {
                        _id ("delivery-" + value)
                        _name "delivery"
                        _value value
                    }

                    fveLabel {
                        _for ("delivery-" + value)
                        caption
                    }
                }
        }

    let fieldGroupRegistration = registration "field-group" "Field group"

    let examples () =
            [ sample "fields" "Related fields" fields
              sample "fieldsWithoutDescription" "Without description" fieldsWithoutDescription
              sample "disabledFields" "Disabled fieldset" disabledFields
              sample "horizontalFields" "Horizontal fields" horizontalFields
              sample "responsiveFields" "Responsive fields" responsiveFields ]


    let page = pageWithExamples fieldGroupRegistration [] (examples ())
