namespace Docs.ComponentPages.InputGroup

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
    let inputGroupRegistration =
        registration "components-input-group" "/components/input-group" "Input group" "Input group"

    let weightInputGroup =
        fveField {
            fveLabel {
                _for "package-weight"
                "Weight"
            }

            fveInputGroup {
                fveInputGroupAddon {
                    _ariaHidden true
                    "Approx."
                }

                fveInput {
                    _id "package-weight"
                    _name "packageWeight"
                    _value "12.5"
                    _ariaDescribedby "package-weight-description"
                }

                fveInputGroupAddon {
                    _position InputGroupPosition.InlineEnd
                    _ariaHidden true
                    "kg"
                }
            }

            fveFieldDescription {
                _id "package-weight-description"
                "Whole numbers or decimals are accepted."
            }
        }

    let searchIcon = fveIcon { _name IconName.MagnifyingGlass; _size Size.Small }

    let searchInputGroup =
        fveField {
            fveLabel {
                _for "item-search"
                "Search items"
            }

            fveInputGroup {
                fveInput {
                    _id "item-search"
                    _name "query"
                    _type InputType.Search
                    _placeholder "Search items…"
                }
                fveInputGroupAddon { _ariaHidden true; searchIcon }
                fveInputGroupAddon {
                    _position InputGroupPosition.InlineEnd
                    _ariaHidden true

                    fveKbdShortcut {
                        fveKbd { "⌘" }
                        fveKbd { "K" }
                    }
                }
            }
        }

    let inputGroupWithHelp =
        fveField {
            fveLabel {
                _for "help-reference"
                "Reference with help"
            }

            fveInputGroup {
                fveInput {
                    _id "help-reference"
                    _name "reference"
                }

                fveInputGroupAddon {
                    _position InputGroupPosition.InlineStart

                    fvePopover {
                        _id "input-group-help"

                        fvePopoverTrigger {
                            fveButton {
                                _size Size.Small
                                _variant ButtonVariant.Ghost
                                _ariaLabel "Reference help"
                                "Help"
                            }
                        }

                        fvePopoverContent {
                            fveField {
                                fveLabel {
                                    _for "help-limit"
                                    "Maximum length"
                                }

                                fveInput {
                                    _id "help-limit"
                                    _name "limit"
                                    _value "10"
                                }
                            }
                        }
                    }
                }
            }
        }

    let textareaInputGroup =
        fveField {
            fveLabel {
                _for "message-draft"
                "Message"
            }

            fveInputGroup {
                fveTextarea {
                    _id "message-draft"
                    _dataRef ("_message_draft", ["root"])
                    _name "message"
                    _rows 4
                    _maxlength 2000
                    "The latest project notes are ready for review."
                }
                fveInputGroupAddon {
                    _position InputGroupPosition.BlockStart
                    _class "border-b border-[var(--fve-border)]"
                    "Message draft"
                }
                fveInputGroupAddon {
                    _position InputGroupPosition.BlockEnd
                    _class "justify-between border-t border-[var(--fve-border)]"
                    span { "Maximum 2,000 characters." }
                    fveButton {
                        _variant ButtonVariant.Ghost
                        _size Size.Small
                        _dataOn ("click", "$_message_draft.value = ''; $_message_draft.dispatchEvent(new Event('input', {bubbles: true})); $_message_draft.focus()")
                        "Clear message"
                    }
                }
            }
        }

    let invalidInputGroup =
        fveField {
            fveLabel { _for "item-reference-code"; "Reference" }
            fveInputGroup {
                fveInput {
                    _id "item-reference-code"
                    _name "referenceCode"
                    _required true
                    _pattern "[0-9]{9}"
                    _inputmode "numeric"
                    yield! nativeValidationAttributes "_reference_code" true "Enter a nine-digit reference."
                    _dataAttr ("aria-describedby", "$_reference_code_error ? 'item-reference-code-validation' : null")
                }
                fveInputGroupAddon { _ariaHidden true; "ID" }
            }
            fveFieldError {
                _id "item-reference-code-validation"
                _style "display:none"
                _dataShow "$_reference_code_error"
                _dataText "$_reference_code_error"
            }
        }

    let emailCheckResult valid =
        p {
            _id "input-group-check-result"
            _dataRef ("_email_check_result", ["root"])
            _role "status"
            _class "text-sm text-[var(--fve-muted-text)]"
            if valid then "Email format is valid." else "Enter a valid email address."
        }

    let checkEmailInputGroup =
        fveField {
            _attr ("data-signals__root", "{_email_check_pending: false}")
            fveLabel { _for "email-to-check"; "Email to check" }
            fveInputGroup {
                fveInput {
                    _id "email-to-check"
                    _dataRef ("_email_check_input", ["root"])
                    _dataOn ("input", "$_email_check_result.textContent = ''")
                    _name "email"
                    _type InputType.Email
                    _value "alex@fve.meiermade.com"
                    _required true
                    _maxlength 254
                    _dataAttr ("readonly", "$_email_check_pending")
                    _dataAttr ("aria-busy", "$_email_check_pending ? 'true' : null")
                }
                fveInputGroupAddon {
                    _position InputGroupPosition.BlockEnd
                    _class "justify-between border-t border-[var(--fve-border)]"
                    fveButton {
                        _size Size.Small
                        _variant ButtonVariant.Ghost
                        _dataIndicator ("_email_check_pending", ["root"])
                        _dataAttr ("disabled", "$_email_check_pending")
                        _dataOn ("click", "$_email_check_input.reportValidity() && @get('/components/input-group/check-email?email=' + encodeURIComponent($_email_check_input.value), {requestCancellation: 'cleanup', retry: 'never'})")
                        "Check email"
                    }
                    span {
                        _style "display:none"
                        _dataShow "$_email_check_pending"
                        _role "status"
                        _ariaLabel "Checking email format"
                        "Checking…"
                    }
                }
            }
            p { _id "input-group-check-result"; _dataRef ("_email_check_result", ["root"]); _role "status" }
        }

    let examples () =
            [ sample "input-group-default" "Prefix" [ "inputWithPrefix" ] (fieldSurface inputWithPrefix)
              sample "input-group-icon" "Icon" [ "emailIcon"; "inputWithIcon" ] (fieldSurface inputWithIcon)
              sample "input-group-suffix" "Suffix" [ "inputWithSuffix" ] (fieldSurface inputWithSuffix)
              sample "input-group" "Text addons" [ "weightInputGroup" ] (fieldSurface weightInputGroup)
              sample
                  "input-group-search"
                  "Icon and keyboard hint"
                  [ "searchIcon"; "searchInputGroup" ]
                  (fieldSurface searchInputGroup)
              sample
                  "input-group-help"
                  "Addon popup keeps its own controls"
                  [ "inputGroupWithHelp" ]
                  (fieldSurface inputGroupWithHelp)
              sample "input-group-textarea" "Textarea header and footer" [ "textareaInputGroup" ] (fieldSurface textareaInputGroup)
              sample "input-group-validation" "Validation error" [ "nativeValidationAttributes"; "invalidInputGroup" ] (fieldSurface invalidInputGroup)
              serverSample "input-group-check" "Checking with visible feedback"
                  [ "emailCheckResult"; "checkEmailInputGroup"; "checkEmailHandler"; "checkEmailRoutes" ]
                  (fieldSurface checkEmailInputGroup)
              |> note "The server checks email format after a one-second demonstration delay. The request indicator owns temporary busy/readonly feedback, then restores editing. Register checkEmailRoutes and services.AddDatastar() in your application." ]


    let private galleryContent : Content =
        { description = "One shared frame around an Input or Textarea, with icons, text, actions or content above and below. Field owns the external label and feedback."
          usage = [
              prose "Compose ordinary fveInput or fveTextarea with fveInputGroupAddon. _position selects InlineStart (default), InlineEnd, BlockStart or BlockEnd. The control renders before addons in the DOM so Tab reaches the input before addon actions; logical positions follow the writing direction. Give meaningful text an ID and connect it with _ariaDescribedby; mark decorative icons _ariaHidden. Reuse ordinary buttons and menus—there are no group-specific control wrappers. Use Button Group instead to join separately bordered controls."
          ]
          sections = []
          apiCategory = Some "input-group" }

    let inputGroupPage = gallery inputGroupRegistration galleryContent (examples ())
