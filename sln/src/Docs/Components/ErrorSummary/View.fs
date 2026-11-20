namespace Docs.ComponentPages.ErrorSummary

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
open Docs.Common.SharedExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let errorSummaryRegistration =
        registration "components-error-summary" "/components/error-summary" "Error summary" "Error summary"

    let errorSummaryExample =
        div {
            _class "grid min-w-0 max-w-xl gap-4 p-4"
            fveErrorSummary {
                _id "example-errors"
                _label "Check your details"
                _style "display:none"
                _dataShow "$_summary_email_error"
                fveErrorSummaryItem {
                    _href "#summary-email"
                    _dataText "'Email address: ' + $_summary_email_error"
                }
            }
            fveField {
                fveLabel { _for "summary-email"; "Email address" }
                fveInput {
                    _id "summary-email"
                    _name "email"
                    _type InputType.Email
                    _value "not-an-email"
                    yield! nativeValidationAttributes "_summary_email" true "Enter a valid email address."
                    _dataAttr ("aria-describedby", "$_summary_email_error ? 'summary-email-validation' : null")
                }
                fveFieldError {
                    _id "summary-email-validation"
                    _style "display:none"
                    _dataShow "$_summary_email_error"
                    _dataText "$_summary_email_error"
                }
            }
        }

    let examples () =
            [ sample
                  "error-summary"
                  "Linked field errors"
                  [ "nativeValidationAttributes"; "errorSummaryExample" ]
                  (fullBleedThemedSurface errorSummaryExample) ]


    let private galleryContent : Content =
        { description = "Summarize validation errors and link users to the affected fields."
          usage = []
          sections = []
          apiCategory = Some "error-summary" }

    let errorSummaryPage =
        gallery errorSummaryRegistration galleryContent (examples ())
