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

module internal Controls =
    let title =
        function
        | ApplicationPage.Home -> "Home"
        | ApplicationPage.Accounts -> "Accounts"
        | ApplicationPage.CreateAccount -> "Create account"
        | ApplicationPage.Account id -> (findAccount id).name
        | ApplicationPage.EditAccount id -> "Edit " + (findAccount id).name
        | ApplicationPage.DeleteAccount id -> "Delete " + (findAccount id).name
        | ApplicationPage.Transactions -> "Transactions"
        | ApplicationPage.Transaction id -> (findTransaction id).description
        | ApplicationPage.DeleteTransaction id -> "Delete " + (findTransaction id).description
        | ApplicationPage.Settings -> "General"
        | ApplicationPage.SettingsSection key -> settingsSections |> List.find (fst >> (=) key) |> snd
        | ApplicationPage.EditOrganization -> "Edit organization"
        | ApplicationPage.Environment id -> (findEnvironment id).name
        | ApplicationPage.EditEnvironment _ -> "Edit environment"
        | ApplicationPage.Profile -> "General"
        | ApplicationPage.EditProfile -> "Edit profile"
        | ApplicationPage.ProfileOrganizations -> "Organizations"

    let pageTitle page query = title (backgroundPage query page)

    let hidden name value =
        input {
            _type "hidden"
            _name name
            _value value
        }

    let contextFields (query: Query) =
        div {
            _class "contents"
            hidden "organization" query.workspace.organization.id
            hidden "environment" query.workspace.environment.id
            hidden "ledger" query.workspace.ledger.id

            for name, value in returnWorkspacePairs query do
                hidden name value

            if query.overlayFrom <> "" then
                hidden "from" query.overlayFrom

            if query.embedded then
                hidden "embedded" "1"

            if query.specification then
                hidden "specState" query.specState

                if query.resource <> "" then
                    hidden "resource" query.resource

            if query.fixture then
                hidden "fveFixture" "product"

            if query.appMode then
                hidden "appMode" "1"
        }

    let editorContextFields query =
        fragment {
            contextFields query

            for name, value in collectionQueryPairs query do
                hidden (if name = "accountType" then "filterAccountType" else name) value
        }
    // Application form submission remains available before enhancement and without JavaScript.

    let formSubmit (color:Color) (attributes: HtmlAttribute list) (label: string) =
        fveButton {
            _type "submit"
            _color color
            _variant ButtonVariant.Solid
            yield! attributes
            label
        }

    let submit label = formSubmit Color.Primary [] label

    let notice query id title (message: string) (color: Color) =
        fveNotice {
            _id (elementId query id)
            _label title
            _announcement (if color = Color.Error then LiveAnnouncement.Assertive else LiveAnnouncement.Polite)
            p { message }
            _color color
        }

    let statusBadge (status: TransactionStatus) =
        fveBadge {
            _color (
                if status = TransactionStatus.Verified then
                    Color.Success
                else
                    Color.Warning
            )

            if status = TransactionStatus.Verified then
                "Verified"
            else
                "Unverified"
        }

    let rowActionClass =
        Size.className Size.Small + " inline-flex size-[var(--fve-control-min-height)] shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-transparent p-0 text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] aria-expanded:bg-[var(--fve-surface-active)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0"

    let selectedKeys (query: Query) kind =
        if not query.selection.IsEmpty then query.selection
        elif query.state <> "selected" then []
        elif kind = "accounts" then [ "101"; "102" ]
        else [ "201"; "203" ]

    let collectionSort query page ascending descending (label: string) (alignment: string) =
        let next = if query.sort = ascending then descending else ascending
        let href = collectionHref { query with sort = next; state = "" } page

        fveTableHead {
            _class alignment

            if query.sort = ascending then
                _ariaSort "ascending"
            elif query.sort = descending then
                _ariaSort "descending"

            a {
                _href href
                _class "fve-table-sort-control"
                label

                span {
                    _ariaHidden true

                    if query.sort = ascending then "↑"
                    elif query.sort = descending then "↓"
                    else "↕"
                }
            }
        }

    let groupLabel =
        function
        | AccountType.Asset -> "Assets"
        | AccountType.Liability -> "Liabilities"
        | AccountType.Equity -> "Equity"
        | AccountType.Revenue -> "Revenue"
        | AccountType.Expense -> "Expenses"
