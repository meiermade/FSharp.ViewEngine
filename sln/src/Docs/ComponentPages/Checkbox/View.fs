namespace Docs.ComponentPages.Checkbox

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
    let checkboxRegistration =
        registration "components-checkbox" "/components/checkbox" "Checkbox" "Checkbox"

    let basicCheckbox =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "emailUpdates"
                _name "emailUpdates"
            }

            fveLabel {
                _for "emailUpdates"
                "Email me product updates"
            }
        }

    let checkboxWithHelp =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "includeSummary"
                _name "includeSummary"
                _ariaDescribedby "includeSummary-description"
            }

            fveFieldContent {
                fveLabel {
                    _for "includeSummary"
                    "Include a summary"
                }

                fveFieldDescription {
                    _id "includeSummary-description"
                    "Add a short overview at the beginning of the report."
                }
            }
        }

    let indeterminateCheckbox =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "mixedSelection"
                _name "mixedSelection"
                _indeterminate true
            }

            fveLabel {
                _for "mixedSelection"
                "Select all items"
            }
        }

    let checkboxFormRegion confirmed (validation: string option) result =
        div {
            _id "components-checkbox-form-region"
            _class "grid max-w-sm gap-3"

            form {
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/checkbox', {contentType: 'form'})")
                _class "grid gap-3"

                fveField {
                    _layout FieldLayout.Horizontal

                    fveCheckbox {
                        _id "components-confirm-archived-review"
                        _name "confirmArchivedReview"
                        _required true
                        _checked confirmed
                        _ariaInvalid validation.IsSome

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ "confirmed-description"
                                  if validation.IsSome then
                                      "confirmed-validation" ]
                        )
                    }

                    fveFieldContent {
                        fveLabel {
                            _for "components-confirm-archived-review"
                            "Confirm review"
                        }

                        fveFieldDescription {
                            _id "confirmed-description"
                            "Required before continuing."
                        }

                        match validation with
                        | Some message ->
                            fveFieldError {
                                _id "confirmed-validation"
                                message
                            }
                        | None -> ()
                    }
                }

                choiceSubmitButton "components-checkbox-submit" "Validate confirmation"
            }

            choiceResult "components-checkbox-result" result
        }

    let includeArchived =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "includeArchived"
                _name "includeArchived"
                _checked true
                _ariaDescribedby "includeArchived-description"
            }

            fveFieldContent {
                fveLabel {
                    _for "includeArchived"
                    "Include archived items"
                }

                fveFieldDescription {
                    _id "includeArchived-description"
                    "Archived items remain read-only."
                }
            }
        }

    let pendingArchivedReview =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "components-pending-archived-review"
                _name "confirmArchivedReview"
                _checked true
                _readonly true
                _ariaBusy true
            }

            fveLabel {
                _for "components-pending-archived-review"
                "Saving review"
            }
        }

    let disabledArchivedReview =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "components-disabled-archived-review"
                _name "confirmArchivedReview"
                _disabled true
            }

            fveLabel {
                _for "components-disabled-archived-review"
                "Archived review unavailable"
            }
        }

    let examples () =
            [ sample "checkbox" "Default" [ "basicCheckbox" ] (fieldSurface basicCheckbox)
              sample "checkbox-help" "With help text" [ "checkboxWithHelp" ] (fieldSurface checkboxWithHelp)
              sample
                  "checkbox-indeterminate"
                  "Indeterminate"
                  [ "indeterminateCheckbox" ]
                  (fieldSurface indeterminateCheckbox)
              sample
                  "checkbox-validation"
                  "Required confirmation with validation"
                  (formSource @ [ "checkboxFormRegion" ])
                  (fieldSurface (checkboxFormRegion false None None))
              sample "checkbox-checked" "Checked" [ "includeArchived" ] (fieldSurface includeArchived)
              sample "checkbox-pending" "Pending" [ "pendingArchivedReview" ] (fieldSurface pendingArchivedReview)
              sample "checkbox-disabled" "Disabled" [ "disabledArchivedReview" ] (fieldSurface disabledArchivedReview) ]


    let private galleryContent : Content =
        { description = "Submit a checked native value with explicit Field labels and feedback. Unchecked controls follow native form semantics."
          usage = []
          sections = []
          apiCategory = Some "checkbox" }

    let checkboxPage = gallery checkboxRegistration galleryContent (examples ())
