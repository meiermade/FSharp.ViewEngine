namespace Example.Server

open System
open FSharp.ViewEngine
open Example.Components
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain

/// Financial application pages shared with the Specification.
open Example.Server.ApplicationPages

module Application =
    let navigation =
        [ "/examples/application", "Home"
          "/examples/application/accounts", "Accounts"
          "/examples/application/transactions", "Transactions"
          "/examples/application/settings", "Settings"
          "/examples/application/profile", "General"
          "/examples/application/profile/organizations", "Organizations"
          yield!
              settingsSections
              |> List.map (fun (key, label) -> "/examples/application/settings/" + key, label) ]

    let title = Controls.title
    let pageTitle = Controls.pageTitle

    let private settings query =
        function
        | "environments" -> SettingsEnvironments.render query
        | "users" -> SettingsUsers.render query
        | "api-clients" -> SettingsApiClients.render query
        | "ledgers" -> SettingsLedgers.render query
        | "billing" -> SettingsBilling.render query
        | _ -> SettingsGeneral.render query

    let private recordActions query editPage deletePage =
        div {
            _class "flex min-w-0 max-w-full items-center gap-2"

            match editPage with
            | Some page -> Layout.secondaryLink (collectionHref query page) "Edit account"
            | None -> ()

            fveLink {
                _variant LinkVariant.Outline
                _color Color.Error
                _href (collectionHref query deletePage)
                "Delete"
            }
        }

    let render page (query: Query) =
        let query =
            if page = ApplicationPage.Transactions && query.sort = "name" then
                { query with sort = "date-desc" }
            else
                query

        let url = applicationHref query
        let background = backgroundPage query page

        let content =
            match background with
            | ApplicationPage.Home -> Home.home query
            | ApplicationPage.Accounts -> ViewAccounts.accountsPage query
            | ApplicationPage.Account id -> ViewAccount.accountDetail query id
            | ApplicationPage.Transactions -> ViewTransactions.transactionsPage query
            | ApplicationPage.Transaction id -> ViewTransaction.transactionDetail query id
            | ApplicationPage.Settings -> SettingsGeneral.render query
            | ApplicationPage.SettingsSection key -> settings query key
            | ApplicationPage.Environment id -> ViewEnvironment.environmentDetail id
            | ApplicationPage.Profile -> ViewProfile.profile query
            | ApplicationPage.ProfileOrganizations -> ProfileOrganizations.profileOrganizations query
            | _ -> text ""

        let actions =
            match background with
            | ApplicationPage.Accounts ->
                div {
                    _class "flex max-w-full flex-wrap items-center justify-end gap-2"
                    Collection.restoreAction query background
                    Layout.primaryLink
                        (collectionHref { query with overlayFrom = "accounts" } ApplicationPage.CreateAccount)
                        "Create account"
                }
            | ApplicationPage.Transactions -> Collection.restoreAction query background
            | ApplicationPage.Account id ->
                recordActions
                    { query with overlayFrom = "" }
                    (Some(ApplicationPage.EditAccount id))
                    (ApplicationPage.DeleteAccount id)
            | ApplicationPage.Transaction id ->
                recordActions { query with overlayFrom = "" } None (ApplicationPage.DeleteTransaction id)
            | ApplicationPage.Environment id ->
                Layout.secondaryLink (url (ApplicationPage.EditEnvironment id)) "Edit environment"
            | ApplicationPage.Profile
            | ApplicationPage.Settings
            | ApplicationPage.SettingsSection "general" ->
                fveLink {
                    _variant LinkVariant.Outline
                    _color Color.Neutral
                    _href (url (if background = ApplicationPage.Profile then ApplicationPage.EditProfile else ApplicationPage.EditOrganization))
                    Layout.icon "m16.862 4.487 1.687-1.688a1.875 1.875 0 0 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897L16.863 4.487Zm0 0L19.5 7.125M18 14.25v4.875A1.875 1.875 0 0 1 16.125 21H4.875A1.875 1.875 0 0 1 3 19.125V7.875A1.875 1.875 0 0 1 4.875 6H9.75"
                    "Edit"
                }
            | ApplicationPage.SettingsSection "api-clients" ->
                fveLink {
                    _variant LinkVariant.Outline
                    _color Color.Neutral
                    _href "/examples/api-documentation"
                    _target "_blank"
                    _rel "noopener"
                    _title "Opens in a new tab"
                    "API Docs"

                    Layout.icon
                        "M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-7.5M21 3v7.5"
                }
            | _ -> text ""

        let overlay =
            match page with
            | ApplicationPage.CreateAccount -> CreateAccount.render query
            | ApplicationPage.EditAccount id -> UpdateAccount.render query id
            | ApplicationPage.EditProfile -> EditProfile.render query
            | ApplicationPage.EditOrganization -> EditOrganization.render query
            | ApplicationPage.EditEnvironment id -> EditEnvironment.render query id
            | ApplicationPage.DeleteAccount id -> DeleteAccount.render query id
            | ApplicationPage.DeleteTransaction id -> DeleteTransaction.render query id
            | _ -> text ""

        let application =
            Layout.applicationShell
                background
                (url background)
                query
                (title background)
                actions
                (fragment {
                    content

                    if query.previewId = "" && background <> page then
                        noscript {
                            match page with
                            | ApplicationPage.CreateAccount
                            | ApplicationPage.EditAccount _ ->
                                let existing =
                                    match page with
                                    | ApplicationPage.EditAccount id -> Some id
                                    | _ -> None

                                section {
                                    _ariaLabel (title page)

                                    _class
                                        "fixed inset-y-0 right-0 z-50 flex w-full max-w-lg flex-col border-l border-[var(--fve-border)] bg-[var(--fve-background)]"

                                    h2 {
                                        _class "shrink-0 border-b border-[var(--fve-border)] p-5 text-lg font-semibold"
                                        title page
                                    }

                                    div {
                                        _class "min-h-0 flex-1 overflow-y-auto p-5"
                                        AccountEditor.accountForm query existing "-native"
                                    }

                                    div {
                                        _class "flex shrink-0 justify-end gap-3 border-t border-[var(--fve-border)] p-5"
                                        AccountEditor.accountFormActions query existing "-native"
                                    }
                                }
                            | ApplicationPage.EditProfile -> EditProfile.render { query with previewId = "native" }
                            | ApplicationPage.EditOrganization ->
                                EditOrganization.render { query with previewId = "native" }
                            | ApplicationPage.EditEnvironment id ->
                                EditEnvironment.render { query with previewId = "native" } id
                            | _ ->
                                Overlay.deletionConfirmation
                                    { query with previewId = "native" }
                                    page
                                    (match page with
                                     | ApplicationPage.DeleteAccount id -> (findAccount id).name
                                     | ApplicationPage.DeleteTransaction id -> (findTransaction id).description
                                     | _ -> "record")
                                    (text "")
                                    (match page with
                                     | ApplicationPage.DeleteAccount id -> canDeleteAccount query.records id
                                     | _ -> true)
                        }
                })

        fragment {
            application

            if query.previewId = "" then
                div {
                    _class (
                        (Layout.theme
                         |> ComponentsTheme.withDensity Density.Compact
                         |> ComponentsTheme.withControlSize Size.Medium
                         |> ComponentsTheme.className)
                        + " contents"
                    )

                    overlay
                    Collection.notifications query
                }
        }
