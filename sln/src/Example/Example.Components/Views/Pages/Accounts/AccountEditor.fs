namespace Example.Components.Views.Pages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain
open Controls
open Overlay

module internal AccountEditor =
    let accountForm (query: Query) existing suffix =
        let account = existing |> Option.map findAccount

        let page =
            existing
            |> Option.map ApplicationPage.EditAccount
            |> Option.defaultValue ApplicationPage.CreateAccount

        let invalid = query.state = "invalid"
        let duplicate = query.state = "duplicate-name"

        let seededName =
            if duplicate then
                accounts |> List.find (fun item -> Some item.id <> existing) |> _.name
            elif invalid then
                ""
            else
                account |> Option.map _.name |> Option.defaultValue ""

        let kind =
            account
            |> Option.map (fun item -> string item.accountType)
            |> Option.defaultValue "Asset"

        let draft =
            match query.editorDraft with
            | Some(EditorDraft.Account draft) -> Some draft
            | _ -> None

        let enteredName = draft |> Option.map _.name |> Option.defaultValue seededName
        let nameId = elementId query ("template-account-name" + suffix)

        let nameError =
            if duplicate then
                Some($"An account named “{enteredName.Trim()}” already exists.")
            elif invalid then
                Some "Enter a name between 1 and 80 characters."
            else
                None

        let name =
            fveField {
                fveLabel {
                    _for nameId
                    "Name"
                }

                fveInput {
                    _id (elementId query ("template-account-name" + suffix))
                    _name "name"
                    _value enteredName
                    _required true
                    _maxlength "80"
                    _autofocus (invalid || duplicate)
                    _ariaInvalid nameError.IsSome

                    if nameError.IsSome then
                        _ariaDescribedby (nameId + "-validation")
                }

                match nameError with
                | Some message ->
                    fveFieldError {
                        _id (nameId + "-validation")
                        message
                    }
                | None -> ()
            }

        let invalidFixedDetails =
            draft
            |> Option.exists (fun value ->
                value.currency <> "USD"
                || value.subtype <> "Generic"
                || value.observedBalance <> "not-required")

        let parentType =
            draft
            |> Option.map _.parentType
            |> Option.defaultWith (fun () ->
                if query.state = "invalid-details" then
                    accountTypes |> List.find (fun value -> string value <> kind) |> string
                else
                    kind)

        let editorSelect name (label: string) selected options (validation: string option) =
            let options =
                if options |> List.exists (fst >> (=) selected) then
                    options
                else
                    (selected, "Unavailable value") :: options

            fveField {
                fveLabel {
                    _for (elementId query ("template-editor-" + name + suffix))
                    label
                }

                fveSelect {
                    _id (elementId query ("template-editor-" + name + suffix))
                    _name name
                    _value selected
                    _nativeFallback true
                    _ariaInvalid (Option.isSome validation)

                    _ariaDescribedby (
                        String.concat
                            " "
                            [ if Option.isSome (validation) then
                                  ((elementId query ("template-editor-" + name + suffix))) + "-validation" ]
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

                match validation with
                | Some message ->
                    fveFieldError {
                        _id ((elementId query ("template-editor-" + name + suffix)) + "-validation")
                        message
                    }
                | None -> ()
            }

        div {
            _class "grid gap-5"

            if query.state = "invalid-details" && invalidFixedDetails then
                notice
                    query
                    ("template-account-details-invalid" + suffix)
                    "Check account details"
                    "The submitted account details are unavailable. Reload the form and try again."
                    Color.Error

            form {
                _id (elementId query ("template-account-form" + suffix))
                _dataIndicator "_example_account_pending"
                _dataOn ("submit", query.submitAction "_example_account_pending")
                _method "post"
                _action (collectionHref query page)
                _ariaLabel (if existing.IsSome then "Edit account" else "Create account")
                _class "grid gap-5 [--fve-control-min-height:2.5rem] [--fve-control-font-size:1rem]"
                editorContextFields query
                name

                editorSelect
                    "parentType"
                    "Parent account"
                    parentType
                    (accountTypes |> List.map (fun kind -> string kind, groupLabel kind))
                    (if query.state = "invalid-details" && not invalidFixedDetails then
                         Some "Choose the root matching the account type."
                     else
                         None)

                editorSelect
                    "accountType"
                    "Account type"
                    (draft |> Option.map _.accountType |> Option.defaultValue kind)
                    (accountTypes |> List.map (fun kind -> string kind, string kind))
                    (if query.state = "invalid-type" then
                         Some "Choose an available account type."
                     else
                         None)

                hidden "subtype" "Generic"
                hidden "currency" "USD"
                hidden "observedBalance" "not-required"

                fveDescriptionList {
                    fveDescriptionListItem {
                        _label "Subtype"
                        "Generic"
                    }

                    fveDescriptionListItem {
                        _label "Commodity"
                        "USD"
                    }

                    fveDescriptionListItem {
                        _label "Month-end observed balance"
                        "Not required"
                    }
                }
            }
        }

    let accountFormActions query existing suffix =
        let page =
            existing
            |> Option.map ApplicationPage.EditAccount
            |> Option.defaultValue ApplicationPage.CreateAccount

        fragment {
            cancelLink query page (elementId query ("template-account-editor-cancel" + suffix))

            formSubmit
                Color.Primary
                [ _dataAttr ("disabled", "$_example_account_pending")
                  _dataAttr ("aria-busy", "$_example_account_pending ? 'true' : null")
                  _form (elementId query ("template-account-form" + suffix)) ]
                (if existing.IsSome then "Update" else "Create")
        }

    let accountEditor (query: Query) (existing: int option) =
        editorDrawer
            query
            "template-account-editor"
            (if existing.IsSome then "Edit account" else "Create account")
            "template-account-name"
            (accountForm query existing "")
            (accountFormActions query existing "")
