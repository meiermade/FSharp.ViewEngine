namespace Docs.ComponentPages.TagInput

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
    let tagInputRegistration =
        registration "components-tag-input" "/components/tag-input" "Tag input" "Tag input"

    let tagInputExample =
        fveField {
            fveLabel {
                _for "item-tags"
                "Tags"
            }

            fveTagInput {
                _id "item-tags"
                _name "tags"
                _values []
            }
        }

    let invalidTagInputExample =
        fveField {
            fveLabel {
                _for "required-tags"
                "Required tags"
            }

            fveTagInput {
                _id "required-tags"
                _name "requiredTags"
                _ariaRequired true
                _ariaInvalid true
                _ariaDescribedby "required-tags-help required-tags-error"
            }

            fveFieldDescription {
                _id "required-tags-help"
                "Add at least one classification."
            }

            fveFieldError {
                _id "required-tags-error"
                "Add a tag before continuing."
            }
        }

    let pendingTagInputExample =
        fveField {
            fveLabel {
                _for "pending-tags"
                "Updating tags"
            }

            fveTagInput {
                _id "pending-tags"
                _name "pendingTags"
                _values [ "reviewed" ]
                _readonly true
                _ariaBusy true
            }
        }

    let disabledTagInputExample =
        fveField {
            fveLabel {
                _for "disabled-tags"
                "Unavailable tags"
            }

            fveTagInput {
                _id "disabled-tags"
                _name "disabledTags"
                _values [ "archived" ]
                _disabled true
            }
        }

    let tagInputFormRegion values (validation: string option) (result: string option) =
        div {
            _id "components-tag-form-region"
            _class "grid gap-3"

            form {
                _class "grid gap-3"
                _dataOn ("submit", "@post('/components/tags', {contentType: 'form'})")

                fveField {
                    fveLabel {
                        _for "submitted-tags"
                        "Submitted tags"
                    }

                    fveTagInput {
                        _id "submitted-tags"
                        _name "submittedTags"
                        _values values
                        _ariaInvalid validation.IsSome

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ "submitted-tags-help"
                                  if validation.IsSome then
                                      "submitted-tags-error" ]
                        )
                    }

                    fveFieldDescription {
                        _id "submitted-tags-help"
                        "Choose one to five tags."
                    }

                    match validation with
                    | Some message ->
                        fveFieldError {
                            _id "submitted-tags-error"
                            message
                        }
                    | None -> ()
                }

                fveButton {
                    _type "submit"
                    "Save tags"
                }

                fveButton {
                    _type "button"
                    _dataOn ("click", "@get('/components/tags/reset')")
                    "Reset tags from server"
                }
            }

            match result with
            | Some message ->
                output {
                    _role "status"
                    message
                }
            | None -> ()
        }

    let examples () =
            [ sample "tag-input" "Default" [ "tagInputExample" ] (fieldSurface tagInputExample)
              |> note
                  "Import Assets/tag-input.js from your application entry. The light-DOM host owns editing and form reset; initial tags are server-rendered native form values."
              sample
                  "tag-input-submission"
                  "Submission"
                  [ "tagInputFormRegion" ]
                  (fieldSurface (tagInputFormRegion [ "reviewed" ] None None))
              sample
                  "tag-input-validation"
                  "Validation"
                  [ "invalidTagInputExample" ]
                  (fieldSurface invalidTagInputExample)
              sample "tag-input-pending" "Pending" [ "pendingTagInputExample" ] (fieldSurface pendingTagInputExample)
              sample
                  "tag-input-disabled"
                  "Disabled"
                  [ "disabledTagInputExample" ]
                  (fieldSurface disabledTagInputExample) ]


    let private galleryContent : Content =
        { description = "Add and remove submitted text tags with native labels, validation, and read-only states."
          usage = []
          sections = []
          apiCategory = Some "tag-input" }

    let tagInputPage = gallery tagInputRegistration galleryContent (examples ())
