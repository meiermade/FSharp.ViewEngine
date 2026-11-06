namespace Example.Server.ApplicationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain
open Controls
open Overlay

module internal EditProfile =
    let render (query: Query) =
        let page = ApplicationPage.EditProfile
        let editorId = "template-profile-editor"
        let focusId = "template-profile-name"
        let formId = elementId query (editorId + "-form")
        let requestSignal = "_example_profile_pending"

        let body =
            div {
                _class "grid gap-5"

                form {
                    _id formId
                    _method "post"
                    _action (applicationHref query page)
                    _ariaLabel (title page)
                    _dataIndicator requestSignal
                    _dataOn ("submit", Navigation.formSubmitAction requestSignal)
                    _class "grid gap-5 [--fve-control-min-height:2.5rem] [--fve-control-font-size:1rem]"
                    contextFields query

                    let request: Ledger.Operations.ValidateProfileRequest =
                        match query.editorDraft with
                        | Some(EditorDraft.Profile draft) -> draft
                        | _ ->
                            { name = "Andrew Meier"
                              email =
                                (if query.state = "invalid" then
                                     "not-an-email"
                                 else
                                     "andrew@meiermade.com")
                              timeZone = "America/Chicago" }

                    let error =
                        if query.state = "invalid" then
                            Some(Ledger.Operations.validateProfile request)
                        else
                            None

                    fveField {
                        fveLabel {
                            _for (elementId query "template-profile-name")
                            "Name"
                        }

                        fveInput {
                            _id (elementId query "template-profile-name")
                            _name "name"
                            _value request.name
                            _required true
                            _maxlength "80"

                            _ariaInvalid (
                                Option.isSome (
                                    if error = Some(Error Ledger.Operations.ValidateProfileError.InvalidName) then
                                        Some "Enter a name between 1 and 80 characters."
                                    else
                                        None
                                )
                            )

                            _ariaDescribedby (
                                String.concat
                                    " "
                                    [ if
                                          Option.isSome (
                                              (if
                                                   error = Some(
                                                       Error Ledger.Operations.ValidateProfileError.InvalidName
                                                   )
                                               then
                                                   Some("Enter a name between 1 and 80 characters.")
                                               else
                                                   None)
                                          )
                                      then
                                          ((elementId query "template-profile-name")) + "-validation" ]
                            )
                        }

                        match
                            (if error = Some(Error Ledger.Operations.ValidateProfileError.InvalidName) then
                                 Some "Enter a name between 1 and 80 characters."
                             else
                                 None)
                        with
                        | Some message ->
                            fveFieldError {
                                _id ((elementId query "template-profile-name") + "-validation")
                                message
                            }
                        | None -> ()
                    }

                    fveField {
                        fveLabel {
                            _for (elementId query "template-profile-email")
                            "Email"
                        }

                        fveInput {
                            _id (elementId query "template-profile-email")
                            _name "email"
                            _value request.email
                            _type InputType.Email
                            _required true
                            _maxlength "254"

                            _ariaInvalid (
                                Option.isSome (
                                    if error = Some(Error Ledger.Operations.ValidateProfileError.InvalidEmail) then
                                        Some "Enter a valid email address."
                                    else
                                        None
                                )
                            )

                            _ariaDescribedby (
                                String.concat
                                    " "
                                    [ if
                                          Option.isSome (
                                              (if
                                                   error = Some(
                                                       Error Ledger.Operations.ValidateProfileError.InvalidEmail
                                                   )
                                               then
                                                   Some("Enter a valid email address.")
                                               else
                                                   None)
                                          )
                                      then
                                          ((elementId query "template-profile-email")) + "-validation" ]
                            )
                        }

                        match
                            (if error = Some(Error Ledger.Operations.ValidateProfileError.InvalidEmail) then
                                 Some "Enter a valid email address."
                             else
                                 None)
                        with
                        | Some message ->
                            fveFieldError {
                                _id ((elementId query "template-profile-email") + "-validation")
                                message
                            }
                        | None -> ()
                    }

                    let options =
                        [ "America/Chicago", "America/Chicago"
                          "America/New_York", "America/New_York"
                          "UTC", "UTC" ]

                    let options =
                        if options |> List.exists (fst >> (=) request.timeZone) then
                            options
                        else
                            (request.timeZone, "Unavailable time zone") :: options

                    fveField {
                        fveLabel {
                            _for (elementId query "template-profile-timezone")
                            "Time zone"
                        }

                        fveSelect {
                            _id (elementId query "template-profile-timezone")
                            _name "timezone"
                            _value request.timeZone
                            _nativeFallback true

                            _ariaInvalid (
                                Option.isSome (
                                    if error = Some(Error Ledger.Operations.ValidateProfileError.InvalidTimeZone) then
                                        Some "Choose an available time zone."
                                    else
                                        None
                                )
                            )

                            _ariaDescribedby (
                                String.concat
                                    " "
                                    [ if
                                          Option.isSome (
                                              (if
                                                   error = Some(
                                                       Error Ledger.Operations.ValidateProfileError.InvalidTimeZone
                                                   )
                                               then
                                                   Some("Choose an available time zone.")
                                               else
                                                   None)
                                          )
                                      then
                                          ((elementId query "template-profile-timezone")) + "-validation" ]
                            )

                            fveSelectContent {
                                for value, label, disabled in
                                    (options |> List.map (fun (value, label) -> (value, label, false))) do
                                    fveSelectItem {
                                        _value value
                                        _label label
                                        _disabled disabled
                                    }
                            }
                        }

                        match
                            (if error = Some(Error Ledger.Operations.ValidateProfileError.InvalidTimeZone) then
                                 Some "Choose an available time zone."
                             else
                                 None)
                        with
                        | Some message ->
                            fveFieldError {
                                _id ((elementId query "template-profile-timezone") + "-validation")
                                message
                            }
                        | None -> ()
                    }
                }
            }

        let actions =
            fragment {
                cancelLink query page (elementId query (editorId + "-cancel"))

                formSubmit
                    Color.Primary
                    [ _dataAttr ("disabled", "$" + requestSignal)
                      _dataAttr ("aria-busy", $"${requestSignal} ? 'true' : null")
                      _form formId ]
                    "Update"
            }

        editorDrawer query editorId (title page) focusId body actions
