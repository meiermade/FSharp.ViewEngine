namespace Docs.ComponentPages.Input

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
open Docs.ComponentPages.Button.View
open Docs.Common.SharedExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let inputRegistration =
        registration "components-input" "/components/input" "Input" "Input"

    let labelledInput =
        fveField {
            fveLabel {
                _for "exampleEmail"
                "Email"
            }

            fveInput {
                _id "exampleEmail"
                _name "exampleEmail"
            }
        }

    let inputWithHelp =
        fveField {
            fveLabel {
                _for "helpEmail"
                "Email"
            }

            fveInput {
                _id "helpEmail"
                _name "helpEmail"
                _type InputType.Email
                _placeholder "you@fve.meiermade.com"
                _autocomplete "email"
                _ariaDescribedby "helpEmail-description"
            }

            fveFieldDescription {
                _id "helpEmail-description"
                "We will use this address for updates."
            }
        }

    let nativeValidationAttributes key initiallyShown (message: string) =
        let checkedState = "$" + key + "_checked"
        let error = "$" + key + "_error"
        let refresh = error + " = el.validity.valid ? '' : " + System.Text.Json.JsonSerializer.Serialize(message)
        let validate = checkedState + " = true; " + refresh
        [ _attr ("data-signals__root", $"{{{key}_checked: false, {key}_error: ''}}")
          _dataOn ("blur", validate)
          _dataOn ("input", $"if ({checkedState}) {{ {refresh} }}")
          _dataOn ("invalid", validate)
          _dataOn ("reset", ["window"], $"if (evt.target === el.form) queueMicrotask(() => {{ if (!evt.defaultPrevented) {{ {checkedState} = false; {error} = ''; }} }})")
          _dataAttr ("aria-invalid", error + " ? 'true' : null")
          if initiallyShown then _dataInit validate ]

    let requiredInput =
        fveField {
            fveLabel { _for "requiredEmail"; "Email (required)" }
            fveInput {
                _id "requiredEmail"
                _name "requiredEmail"
                _type InputType.Email
                _required true
                _ariaDescribedby "required-email-help"
                yield! nativeValidationAttributes "_required_email" false "Enter a valid email address."
                _dataAttr ("aria-describedby", "$_required_email_error ? 'required-email-help required-email-error' : 'required-email-help'")
            }
            fveFieldDescription {
                _id "required-email-help"
                "Leave the field to check it. Correcting the email clears the error as you type."
            }
            fveFieldError {
                _id "required-email-error"
                _style "display:none"
                _dataShow "$_required_email_error"
                _dataText "$_required_email_error"
            }
        }

    let optionalInput =
        fveField {
            fveLabel {
                _for "company"
                "Company (optional)"
            }

            fveInput {
                _id "company"
                _name "company"
                _autocomplete "organization"
            }
        }

    let invalidInput =
        fveField {
            fveLabel { _for "invalidEmail"; "Email" }
            fveInput {
                _id "invalidEmail"
                _name "invalidEmail"
                _type InputType.Email
                _value "not-an-email"
                yield! nativeValidationAttributes "_invalid_email" true "Enter a valid email address."
                _dataAttr ("aria-describedby", "$_invalid_email_error ? 'invalidEmail-validation' : null")
            }
            fveFieldError {
                _id "invalidEmail-validation"
                _style "display:none"
                _dataShow "$_invalid_email_error"
                _dataText "$_invalid_email_error"
            }
        }

    let emailIcon = fveIcon { _name IconName.Envelope; _size Size.Medium }

    let inputWithIcon =
        fveField {
            fveLabel {
                _for "iconEmail"
                "Email"
            }

            fveInputGroup {
                fveInput {
                    _id "iconEmail"
                    _name "iconEmail"
                    _type InputType.Email
                    _placeholder "you@fve.meiermade.com"
                }
                fveInputGroupAddon { _ariaHidden true; emailIcon }
            }
        }

    let inputWithPrefix =
        fveField {
            fveLabel {
                _for "website"
                "Website"
            }

            fveInputGroup {
                fveInput {
                    _id "website"
                    _name "website"
                    _placeholder "fve.meiermade.com"
                    _autocomplete "off"
                    _ariaDescribedby "website-scheme"
                }
                fveInputGroupAddon { _id "website-scheme"; "https://" }
            }
        }

    let inputWithSuffix =
        fveField {
            fveLabel {
                _for "distance"
                "Distance"
            }

            fveInputGroup {
                fveInput {
                    _id "distance"
                    _name "distance"
                    _inputmode "decimal"
                    _placeholder "0.00"
                    _ariaDescribedby "distance-unit"
                }
                fveInputGroupAddon {
                    _position InputGroupPosition.InlineEnd
                    _id "distance-unit"
                    "km"
                }
            }
        }

    let disabledInput =
        fveField {
            fveLabel {
                _for "disabledEmail"
                "Email"
            }

            fveInput {

                _name "disabledEmail"
                _value "alex@fve.meiermade.com"
                _disabled true

                _id "disabledEmail"
            }
        }

    let readOnlyInput =
        fveField {
            fveLabel { _for "readOnlyEmail"; "Read-only email" }
            fveInput {
                _id "readOnlyEmail"
                _name "readOnlyEmail"
                _value "alex@fve.meiermade.com"
                _readonly true
            }
        }

    let searchInputExample =
        fveField {
            fveLabel {
                _for "field-search"
                "Search"
            }

            fveInput {

                _name "query"
                _id "field-search"
                _type "search"
                _placeholder "Search…"
                _autocomplete "off"
            }
        }

    let inputSizeExamples =
        div {
            _class "grid min-w-0 gap-6"

            for size, name in [ Size.ExtraSmall, "Extra small"; Size.Small, "Compact"; Size.Medium, "Standard"; Size.Large, "Large"; Size.ExtraLarge, "Extra large" ] do
                let key = name.ToLowerInvariant().Replace(" ", "-")
                div {
                    _class (Size.className size + " flex flex-wrap items-end gap-3")

                    fveField {
                        fveLabel {
                            _for ("sizing-" + key)
                            Html.text (name + " search")
                        }

                        fveInput {
                            _id ("sizing-" + key)
                            _name ("sizing-" + key)
                            _type InputType.Search
                        }
                    }

                    fveField {
                        fveLabel {
                            _for ("sizing-type-" + key)
                            Html.text (name + " type")
                        }

                        fveSelect {
                            _id ("sizing-type-" + key)
                            _name ("sizing-type-" + key)
                            _value "all"

                            fveSelectContent {
                                for value, label, disabled in ([ ("all", "All types", false) ]) do
                                    fveSelectItem {
                                        _value value
                                        _label label
                                        _disabled disabled
                                    }
                            }
                        }
                    }

                    div {
                        _class "flex flex-wrap items-center gap-3"
                        fveButton { Html.text (name + " action") }

                        fveButton {
                            _ariaLabel (name + " refresh")

                            span {
                                _ariaHidden true
                                _class "inline-flex shrink-0"
                                refreshIcon
                            }
                        }
                    }
                }

            div {
                _class (Size.className Size.Small + " flex flex-wrap items-end gap-3")

                fveField {
                    fveLabel {
                        _for "override-search"
                        "Larger search in a compact region"
                    }

                    fveInput {
                        _id "override-search"
                        _name "override-search"
                        _type InputType.Search
                        _size Size.Large
                    }
                }

                fveField {
                    fveLabel {
                        _for "override-type"
                        "Larger type"
                    }

                    fveSelect {
                        _id "override-type"
                        _name "override-type"
                        _value "all"
                        _size Size.Large

                        fveSelectContent {
                            for value, label, disabled in ([ ("all", "All types", false) ]) do
                                fveSelectItem {
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }
                    }
                }

                div {
                    _class "flex flex-wrap items-center gap-3"

                    fveButton {
                        _size Size.Large
                        "Larger action"
                    }

                    fveButton {
                        _size Size.Large
                        _ariaLabel "Larger refresh"

                        span {
                            _ariaHidden true
                            _class "inline-flex shrink-0"
                            refreshIcon
                        }
                    }
                }
            }
        }

    let examples () =
            [ sample "input" "Default" [ "labelledInput" ] (fieldSurface labelledInput)
              sample
                  "input-sizes"
                  "Consistent control sizes"
                  [ "refreshIcon"; "inputSizeExamples" ]
                  (detailsSurface inputSizeExamples)
              sample "input-help" "With help text" [ "inputWithHelp" ] (fieldSurface inputWithHelp)
              sample "input-required" "Required" [ "nativeValidationAttributes"; "requiredInput" ] (fieldSurface requiredInput)
              sample "input-optional" "Optional" [ "optionalInput" ] (fieldSurface optionalInput)
              sample "input-validation" "Validation error" [ "nativeValidationAttributes"; "invalidInput" ] (fieldSurface invalidInput)
              sample
                  "search-input"
                  "Native search"
                  [ "searchInputExample" ]
                  (fieldSurface searchInputExample)
              sample "input-disabled" "Disabled" [ "disabledInput" ] (fieldSurface disabledInput)
              sample "input-readonly" "Read-only" [ "readOnlyInput" ] (fieldSurface readOnlyInput) ]


    let private galleryContent : Content =
        { description = "One native input. Use Field for labels and feedback, and Input Group for icons, text and actions inside a shared frame."
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
          apiCategory = Some "input" }

    let inputPage = gallery inputRegistration galleryContent (examples ())
