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

module internal EditEnvironment =
    let render (query: Query) id =
        let page = ApplicationPage.EditEnvironment id
        let environment = findEnvironment id
        let editorId = "template-environment-editor"
        let focusId = "template-environment-name"
        let formId = elementId query (editorId + "-form")
        let requestSignal = "_example_environment_pending"

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

                    let request: Ledger.Operations.ValidateEnvironmentRequest =
                        match query.editorDraft with
                        | Some(EditorDraft.Environment draft) -> draft
                        | _ ->
                            { environmentId = environment.id
                              name = (if query.state = "invalid" then "" else environment.name) }

                    let error =
                        if query.state = "invalid" then
                            Some(Ledger.Operations.validateEnvironment request)
                        else
                            None

                    fveField {
                        fveLabel {
                            _for (elementId query focusId)
                            "Name"
                        }

                        fveInput {
                            _id (elementId query focusId)
                            _name "name"
                            _value request.name
                            _required true
                            _maxlength "80"
                            _autofocus (Option.isSome error)

                            _ariaInvalid (
                                Option.isSome (
                                    if error = Some(Error Ledger.Operations.ValidateEnvironmentError.InvalidName) then
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
                                                       Error Ledger.Operations.ValidateEnvironmentError.InvalidName
                                                   )
                                               then
                                                   Some("Enter a name between 1 and 80 characters.")
                                               else
                                                   None)
                                          )
                                      then
                                          ((elementId query focusId)) + "-validation" ]
                            )
                        }

                        match
                            (if error = Some(Error Ledger.Operations.ValidateEnvironmentError.InvalidName) then
                                 Some "Enter a name between 1 and 80 characters."
                             else
                                 None)
                        with
                        | Some message ->
                            fveFieldError {
                                _id ((elementId query focusId) + "-validation")
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
