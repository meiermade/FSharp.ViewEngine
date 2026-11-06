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

module internal EditOrganization =
    let render (query: Query) =
        let page = ApplicationPage.EditOrganization
        let editorId = "template-organization-editor"
        let focusId = "template-workspace-name"
        let formId = elementId query (editorId + "-form")
        let requestSignal = "_example_organization_pending"

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

                    let request: Ledger.Operations.ValidateOrganizationRequest =
                        match query.editorDraft with
                        | Some(EditorDraft.Organization draft) -> draft
                        | _ ->
                            { name =
                                (if query.state = "invalid" then
                                     ""
                                 else
                                     query.workspace.organization.name)
                              currency = "USD" }

                    let error =
                        if query.state = "invalid" then
                            Some(Ledger.Operations.validateOrganization request)
                        else
                            None

                    fveField {
                        fveLabel {
                            _for (elementId query "template-workspace-name")
                            "Name"
                        }

                        fveInput {
                            _id (elementId query "template-workspace-name")
                            _name "workspace"
                            _value request.name
                            _required true
                            _maxlength "80"

                            _ariaInvalid (
                                Option.isSome (
                                    if error = Some(Error Ledger.Operations.ValidateOrganizationError.InvalidName) then
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
                                                       Error Ledger.Operations.ValidateOrganizationError.InvalidName
                                                   )
                                               then
                                                   Some("Enter a name between 1 and 80 characters.")
                                               else
                                                   None)
                                          )
                                      then
                                          ((elementId query "template-workspace-name")) + "-validation" ]
                            )
                        }

                        match
                            (if error = Some(Error Ledger.Operations.ValidateOrganizationError.InvalidName) then
                                 Some "Enter a name between 1 and 80 characters."
                             else
                                 None)
                        with
                        | Some message ->
                            fveFieldError {
                                _id ((elementId query "template-workspace-name") + "-validation")
                                message
                            }
                        | None -> ()
                    }

                    let options =
                        if request.currency = "USD" then
                            [ "USD", "US dollar (USD)" ]
                        else
                            [ request.currency, "Unavailable commodity"; "USD", "US dollar (USD)" ]

                    fveField {
                        fveLabel {
                            _for (elementId query "template-organization-currency")
                            "Reporting commodity"
                        }

                        fveSelect {
                            _id (elementId query "template-organization-currency")
                            _name "currency"
                            _value request.currency
                            _nativeFallback true

                            _ariaInvalid (
                                Option.isSome (
                                    if
                                        error = Some(Error Ledger.Operations.ValidateOrganizationError.InvalidCurrency)
                                    then
                                        Some "Choose US dollar (USD)."
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
                                                       Error Ledger.Operations.ValidateOrganizationError.InvalidCurrency
                                                   )
                                               then
                                                   Some("Choose US dollar (USD).")
                                               else
                                                   None)
                                          )
                                      then
                                          ((elementId query "template-organization-currency")) + "-validation" ]
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
                            (if error = Some(Error Ledger.Operations.ValidateOrganizationError.InvalidCurrency) then
                                 Some "Choose US dollar (USD)."
                             else
                                 None)
                        with
                        | Some message ->
                            fveFieldError {
                                _id ((elementId query "template-organization-currency") + "-validation")
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
