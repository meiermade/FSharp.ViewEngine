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

    let private recordActions query deletePage =
        fveDropdownMenu {
            _id (elementId query "record-actions")
            _ariaLabel "More actions"
            fveDropdownMenuTrigger {
                _class (Controls.rowActionClass + " ring-1 ring-inset ring-[var(--fve-border)]")
                fun (attributes:HtmlAttribute list) -> button {
                    for attribute in attributes do attribute
                    fveIcon { _name IconName.EllipsisHorizontal; _variant IconVariant.Micro }
                }
            }
            fveDropdownMenuItem {
                _href (collectionHref query deletePage)
                _color Color.Error
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
            | ApplicationPage.Environment id -> ViewEnvironment.environmentDetail query id
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
                    (ApplicationPage.DeleteAccount id)
            | ApplicationPage.Transaction id ->
                recordActions { query with overlayFrom = "" } (ApplicationPage.DeleteTransaction id)
            | ApplicationPage.SettingsSection "api-clients" ->
                fveLink {
                    _variant LinkVariant.Outline
                    _color Color.Neutral
                    _href "/examples/api-documentation"
                    _target "_blank"
                    _rel "noopener"
                    _title "Opens in a new tab"
                    "API Docs"

                    Layout.icon IconName.ArrowTopRightOnSquare
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
