namespace Docs.ComponentPages.Textarea

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
open Docs.ComponentPages.Input.View
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let textareaRegistration =
        registration "components-textarea" "/components/textarea" "Textarea" "Textarea"

    let labelledTextarea =
        fveField {
            fveLabel {
                _for "message"
                "Message"
            }

            fveTextarea {
                _id "message"
                _name "message"
            }
        }

    let composerTextarea =
        fveField {
            fveLabel {
                _for "reply"
                _class "sr-only"
                "Message"
            }

            fveTextarea {
                _id "reply"
                _name "reply"
                _rows 1
                _required true
                _placeholder "Write a message…"
            }
        }

    let invalidTextarea =
        fveField {
            fveLabel { _for "invalidMessage"; "Message" }
            fveTextarea {
                _id "invalidMessage"
                _name "invalidMessage"
                _required true
                yield! nativeValidationAttributes "_invalid_message" true "Enter a message before continuing."
                _dataAttr ("aria-describedby", "$_invalid_message_error ? 'invalidMessage-validation' : null")
            }
            fveFieldError {
                _id "invalidMessage-validation"
                _style "display:none"
                _dataShow "$_invalid_message_error"
                _dataText "$_invalid_message_error"
            }
        }

    let editableInstructions =
        fveField {
            fveLabel {
                _for "instructions-editable"
                "Instructions"
            }

            fveTextarea {
                _id "instructions-editable"
                _name "instructions"
                _value "Include the reference number when you reply."
                _maxlength 400
                _ariaDescribedby (String.concat " " [ ("instructions-editable") + "-description" ])
            }

            fveFieldDescription {
                _id ("instructions-editable" + "-description")
                "Maximum 400 characters."
            }
        }

    let readOnlyNotes =
        fveField {
            fveLabel { _for "readOnlyNotes"; "Read-only notes" }
            fveTextarea {
                _id "readOnlyNotes"
                _name "readOnlyNotes"
                _value "Please include reference ITEM-2048."
                _readonly true
            }
        }

    let unavailableNotes =
        fveField {
            fveLabel {
                _for "unavailableNotes"
                "Unavailable notes"
            }

            fveTextarea {
                _id "unavailableNotes"
                _name "unavailableNotes"
                _disabled true
            }
        }

    let examples () =
            [ sample "textarea" "Default" [ "labelledTextarea" ] (fieldSurface labelledTextarea)
              sample "textarea-composer" "Visually hidden label" [ "composerTextarea" ] (fieldSurface composerTextarea)
              sample "textarea-help" "With instructions" [ "editableInstructions" ] (fieldSurface editableInstructions)
              sample "textarea-validation" "Validation error" [ "nativeValidationAttributes"; "invalidTextarea" ] (fieldSurface invalidTextarea)
              sample "textarea-readonly" "Read-only" [ "readOnlyNotes" ] (fieldSurface readOnlyNotes)
              sample "textarea-disabled" "Disabled" [ "unavailableNotes" ] (fieldSurface unavailableNotes) ]


    let private galleryContent : Content =
        { description = "Collect multiline text with a native textarea. Compose labels and feedback with Field."
          usage = [
              prose "required, type, pattern and length constraints use the browser's validity rules. :invalid matches any currently invalid value; :user-invalid delays native error styling until interaction/submission. These examples use Datastar to show inline errors after blur or invalid submission, then recheck on input so the border, aria-invalid, associated message and summary clear together as soon as the value is valid. Form reset clears this temporary feedback."
              prose "The nativeValidationAttributes helper is example-owned presentation, not an Input API or a second value model. Always validate submissions on the server. For server-only rules, recheck on the server and render _ariaInvalid with an associated Field error; a syntactically valid email does not prove that a server-only error is resolved. Read-only values remain submitted; disabled values do not."
              p {
                  "Use "
                  a { _href "/components/input-group"; _class "text-[var(--fve-brand-text)] underline"; "Input Group" }
                  " for icons, prefixes, suffixes and actions inside the control frame."
              }
          ]
          sections = []
          apiCategory = Some "textarea" }

    let textareaPage = gallery textareaRegistration galleryContent (examples ())
