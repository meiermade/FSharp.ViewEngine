namespace Docs.ComponentPages.RadioGroup

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
open Docs.Common.ChoicesExamples
open Docs.Common.SamplesExamples
open Docs.Common.SourceListsExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let radioGroupRegistration =
        registration "components-radio-group" "/components/radio-group" "Radio group" "Radio group"

    let basicRadioGroup =
        fveRadioGroup {
            fveFieldLegend { "Contact method" }

            for value, caption in [ "email", "Email"; "phone", "Phone" ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("contact-" + value)
                        _name "contactMethod"
                        _value value
                    }

                    fveLabel {
                        _for ("contact-" + value)
                        caption
                    }
                }
        }

    let radioGroupWithHelp =
        fveRadioGroup {
            _ariaDescribedby "delivery-help"
            fveFieldLegend { "Delivery" }

            fveFieldDescription {
                _id "delivery-help"
                "Choose how you would like your order delivered."
            }

            for value, caption, description in
                [ "standard", "Standard", "Arrives in three to five business days."
                  "express", "Express", "Arrives the next business day." ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("shipping-" + value)
                        _name "delivery"
                        _value value
                        _ariaDescribedby ("shipping-" + value + "-help")
                    }

                    fveFieldContent {
                        fveLabel {
                            _for ("shipping-" + value)
                            caption
                        }

                        fveFieldDescription {
                            _id ("shipping-" + value + "-help")
                            description
                        }
                    }
                }
        }

    let horizontalRadioGroup =
        fveRadioGroup {
            _orientation Orientation.Horizontal
            fveFieldLegend { "Table density" }

            for value, caption in [ "compact", "Compact"; "comfortable", "Comfortable"; "spacious", "Spacious" ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("density-" + value)
                        _name "density"
                        _value value

                        if value = "comfortable" then
                            _checked true
                    }

                    fveLabel {
                        _for ("density-" + value)
                        caption
                    }
                }
        }

    let private updateModeOptions =
        [ "automatic", "Automatic", false
          "manual", "Manual review", false
          "scheduled", "Scheduled", true ]

    let radioGroupFormRegion selected (validation: string option) result =
        div {
            _id "components-radio-form-region"
            _class "grid max-w-sm gap-3"

            form {
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/radio', {contentType: 'form'})")
                _class "grid gap-3"

                fveRadioGroup {
                    _id "components-update-mode"
                    _ariaRequired true
                    _ariaInvalid validation.IsSome

                    _ariaDescribedby (
                        String.concat
                            " "
                            [ "update-mode-help"
                              if validation.IsSome then
                                  "update-mode-error" ]
                    )

                    fveFieldLegend { "Update mode" }

                    fveFieldDescription {
                        _id "update-mode-help"
                        "Choose how updates are applied."
                    }

                    for value, caption, disabled in updateModeOptions do
                        fveField {
                            _layout FieldLayout.Horizontal

                            fveRadio {
                                _id ("update-mode-" + value)
                                _name "updateMode"
                                _value value

                                if selected = Some value then
                                    _checked true

                                _disabled disabled
                                _required true
                                _ariaInvalid validation.IsSome

                                _ariaDescribedby (
                                    String.concat
                                        " "
                                        [ "update-mode-help"
                                          if validation.IsSome then
                                              "update-mode-error" ]
                                )
                            }

                            fveLabel {
                                _for ("update-mode-" + value)
                                caption
                            }
                        }

                    match validation with
                    | Some message ->
                        fveFieldError {
                            _id "update-mode-error"
                            message
                        }
                    | None -> ()
                }

                choiceSubmitButton "components-radio-submit" "Validate choice"
            }

            choiceResult "components-radio-result" result
        }

    let updateMode = radioGroupFormRegion None None None

    let pendingUpdateMode =
        fveRadioGroup {
            _ariaBusy true
            fveFieldLegend { "Saving update mode" }

            for value, caption, disabled in updateModeOptions do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("pending-mode-" + value)
                        _name "pendingUpdateMode"
                        _value value

                        if value = "automatic" then
                            _checked true

                        _disabled disabled
                        _readonly true
                    }

                    fveLabel {
                        _for ("pending-mode-" + value)
                        caption
                    }
                }
        }

    let disabledUpdateMode =
        fveRadioGroup {
            _disabled true
            fveFieldLegend { "Update mode unavailable" }

            for value, caption, _ in updateModeOptions do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("disabled-mode-" + value)
                        _name "disabledUpdateMode"
                        _value value

                        if value = "manual" then
                            _checked true
                    }

                    fveLabel {
                        _for ("disabled-mode-" + value)
                        caption
                    }
                }
        }

    let examples () =
            [ sample "radio-group" "Default" [ "basicRadioGroup" ] (fieldSurface basicRadioGroup)
              sample "radio-group-help" "Option descriptions" [ "radioGroupWithHelp" ] (fieldSurface radioGroupWithHelp)
              sample
                  "radio-group-horizontal"
                  "Horizontal orientation"
                  [ "horizontalRadioGroup" ]
                  (fieldSurface horizontalRadioGroup)
              sample
                  "radio-group-validation"
                  "Required choice with validation"
                  (formSource @ [ "updateModeOptions"; "radioGroupFormRegion"; "updateMode" ])
                  (fieldSurface updateMode)
              sample
                  "radio-group-pending"
                  "Pending"
                  [ "updateModeOptions"; "pendingUpdateMode" ]
                  (fieldSurface pendingUpdateMode)
              sample
                  "radio-group-disabled"
                  "Disabled"
                  [ "updateModeOptions"; "disabledUpdateMode" ]
                  (fieldSurface disabledUpdateMode) ]


    let private galleryContent : Content =
        { description = "Choose one submitted value from an accessible typed radio group."
          usage = []
          sections = []
          apiCategory = Some "radio-group" }

    let radioGroupPage = gallery radioGroupRegistration galleryContent (examples ())
