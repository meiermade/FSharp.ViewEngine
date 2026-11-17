namespace Docs.ComponentPages.FormLayouts

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
open Docs.Common.SharedExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let formLayoutsRegistration =
        applicationRegistration "components-form-layouts" "/components/form-layouts" "Form layouts" "Form layouts"

    type ContactDetails =
        { name: string
          email: string
          notes: string }

    type ContactFormLayout =
        | Stacked
        | TwoColumns
        | Sectioned

    let contactFormRegion layout (details: ContactDetails) (errors: Map<string, string>) accepted =
        let key, title =
            match layout with
            | Stacked -> "", "Contact details"
            | TwoColumns -> "grid", "Contact details in two columns"
            | Sectioned -> "sectioned", "Contact details in sections"

        let prefix = if key = "" then "contact-" else "contact-" + key + "-"
        let endpoint = "/components/forms/contact" + (if key = "" then "" else "/" + key)

        let name =
            fveField {
                fveLabel {
                    _for (prefix + "name")
                    "Contact name"
                }

                fveInput {
                    _id (prefix + "name")
                    _name "contactName"
                    _value details.name
                    _required true
                    _autocomplete "name"
                    _ariaInvalid (Option.isSome (Map.tryFind "contactName" errors))

                    _ariaDescribedby (
                        String.concat
                            " "
                            [ if Option.isSome (Map.tryFind "contactName" errors) then
                                  ((prefix + "name")) + "-validation" ]
                    )
                }

                match Map.tryFind "contactName" errors with
                | Some message ->
                    fveFieldError {
                        _id ((prefix + "name") + "-validation")
                        message
                    }
                | None -> ()
            }

        let email =
            fveField {
                fveLabel {
                    _for (prefix + "email")
                    "Email address"
                }

                fveInput {
                    _id (prefix + "email")
                    _name "email"
                    _type InputType.Email
                    _value details.email
                    _required true
                    _autocomplete "email"
                    _ariaInvalid (Option.isSome (Map.tryFind "email" errors))

                    _ariaDescribedby (
                        String.concat
                            " "
                            [ ((prefix + "email")) + "-description"
                              if Option.isSome (Map.tryFind "email" errors) then
                                  ((prefix + "email")) + "-validation" ]
                    )
                }

                fveFieldDescription {
                    _id ((prefix + "email") + "-description")
                    "Use the address where you receive updates."
                }

                match Map.tryFind "email" errors with
                | Some message ->
                    fveFieldError {
                        _id ((prefix + "email") + "-validation")
                        message
                    }
                | None -> ()
            }

        let identityFields =
            div {
                _class (
                    if layout = Stacked then
                        "grid gap-4"
                    else
                        "grid gap-4 sm:grid-cols-2"
                )

                name
                email
            }

        let notes =
            fveField {
                fveLabel {
                    _for (prefix + "notes")
                    "Notes"
                }

                fveTextarea {
                    _id (prefix + "notes")
                    _name "notes"
                    _value details.notes
                    _ariaDescribedby (String.concat " " [ ((prefix + "notes")) + "-description" ])
                }

                fveFieldDescription {
                    _id ((prefix + "notes") + "-description")
                    "Any additional information we should know."
                }
            }

        div {
            _id ("components-" + prefix + "region")

            _class (
                "mx-auto grid w-full min-w-0 gap-6 p-6 sm:p-8 "
                + if layout = Stacked then "max-w-xl" else "max-w-4xl"
            )

            if not (Map.isEmpty errors) then
                fveErrorSummary {
                    _id (prefix + "errors")
                    _label "Check your contact details"
                    _autofocus true

                    match Map.tryFind "contactName" errors with
                    | Some message ->
                        fveErrorSummaryItem {
                            _href ("#" + prefix + "name")
                            "Contact name: " + message
                        }
                    | None -> ()

                    match Map.tryFind "email" errors with
                    | Some message ->
                        fveErrorSummaryItem {
                            _href ("#" + prefix + "email")
                            "Email address: " + message
                        }
                    | None -> ()
                }

            if accepted then
                fveNotice {
                    _id (prefix + "result")
                    _label "Details are valid"
                    p { "Validation succeeded." }
                    _color Color.Success
                    _announcement LiveAnnouncement.Polite
                }

            form {
                _ariaLabel title
                _class "grid gap-6"
                _attr ("novalidate", "")
                _dataOn ("submit", $"@post('{endpoint}', {{contentType: 'form'}})")

                if layout = Sectioned then
                    Section.create
                        (fveSectionHeader {
                            _label "Contact information"
                            _description "Who should we contact?"
                            _divider true
                        })
                        identityFields
                    |> Section.render

                    Section.create
                        (fveSectionHeader {
                            _label "Additional details"
                            _divider true
                        })
                        notes
                    |> Section.render
                else
                    identityFields
                    notes

                div {
                    _class "flex justify-end"
                    choiceSubmitButton (prefix + "submit") "Validate details"
                }
            }
        }

    let private emptyContact = { name = ""; email = ""; notes = "" }

    let contactFormExample = contactFormRegion Stacked emptyContact Map.empty false

    let twoColumnFormExample = contactFormRegion TwoColumns emptyContact Map.empty false

    let sectionedFormExample = contactFormRegion Sectioned emptyContact Map.empty false

    let examples () =
            [ sample
                  "form-layouts"
                  "Stacked with server validation"
                  [ "choiceSubmitButton"
                    "ContactDetails"
                    "ContactFormLayout"
                    "contactFormRegion"
                    "emptyContact"
                    "contactFormExample" ]
                  (fullBleedThemedSurface contactFormExample)
              sample
                  "form-layouts-grid"
                  "Two-column form"
                  [ "choiceSubmitButton"
                    "ContactDetails"
                    "ContactFormLayout"
                    "contactFormRegion"
                    "emptyContact"
                    "twoColumnFormExample" ]
                  (fullBleedThemedSurface twoColumnFormExample)
              sample
                  "form-layouts-sections"
                  "Sectioned form"
                  [ "choiceSubmitButton"
                    "ContactDetails"
                    "ContactFormLayout"
                    "contactFormRegion"
                    "emptyContact"
                    "sectionedFormExample" ]
                  (fullBleedThemedSurface sectionedFormExample) ]


    let private galleryContent : Content =
        { description = "Arrange complete fields into consistent stacked, inline, and responsive form layouts."
          usage = []
          sections = []
          apiCategory = None }

    let formLayoutsPage = gallery formLayoutsRegistration galleryContent (examples ())
