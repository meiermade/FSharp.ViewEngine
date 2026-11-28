namespace Docs.ComponentPages.AccountManagement

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
open Docs.Common.SharedExamples
open Docs.Common.LedgerExamples
open Docs.ComponentPages.AccountManagement.Examples
open Docs.ComponentPages.PageTopBar.View
open Docs.ComponentPages.SideNav.View
open Docs.Common.SamplesExamples
open Docs.Common.SourceListsExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let accountManagementRegistration =
        applicationRegistration
            "components-account-management"
            "/components/page-examples/account-management"
            "Account management"
            "Account management"

    let private accountWorkspaceUrl (workspace: AccountWorkspace) =
        let encode (value: string) = Uri.EscapeDataString value

        let columns =
            workspace.visibleColumns |> Set.toList |> List.sort |> String.concat ","

        $"/components/page-examples/account-management?destination=ledger-accounts&query={encode workspace.searchQuery}&accountType={encode workspace.filterType}&sort={workspace.sortColumn}&direction={workspace.sortDirection}&page={workspace.page}&columns={columns}"

    let private accountWorkspacePatch url =
        let target = System.Text.Json.JsonSerializer.Serialize url
        $"@get({target}.replace('/components/page-examples/account-management?', '/components/page-examples/account-management/fixture?'))"

    let private accountDataTable workspace accountRows =
        let sort column label alignment =
            let active = workspace.sortColumn = column

            let nextDirection =
                if active && workspace.sortDirection = "asc" then
                    "desc"
                else
                    "asc"

            let destination =
                accountWorkspaceUrl
                    { workspace with
                        sortColumn = column
                        sortDirection = nextDirection
                        page = 1 }

            let direction =
                if not active then None
                elif workspace.sortDirection = "asc" then Some "ascending"
                else Some "descending"

            RecordTable.sortedHeading label destination direction alignment []

        let selection =
            RecordSelection.view
                "account-data-selection"
                "accountIds"
                []
                (accountRows |> List.map (fun (row: AccountRow) -> string row.id, row.name))

        let headings =
            [ selection.heading
              sort "name" "Account" ""
              if workspace.visibleColumns.Contains "type" then
                  fveTableHead { "Type" }
              if workspace.visibleColumns.Contains "commodity" then
                  fveTableHead { "Commodity" }
              if workspace.visibleColumns.Contains "balance" then
                  sort "balance" "Balance" "text-right"
              if workspace.visibleColumns.Contains "total" then
                  fveTableHead {
                      _class "text-right"
                      "Total balance (USD) · Current"
                  }
              fveTableHead {
                  _class "fve-table-actions text-right"
                  _attr ("data-fve-sticky-cell", "true")

                  span {
                      _class "sr-only"
                      "Actions"
                  }
              } ]

        let body =
            [ for row in accountRows do
                  fveTableRow {
                      yield! selection.rowAttributes (string row.id)
                      selection.cell (string row.id) row.name

                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary

                          a {
                              _href (shellDestinationUrl (LedgerAccount row.id))
                              _class "font-medium text-[var(--fve-brand-text)]"
                              row.name
                          }
                      }

                      if workspace.visibleColumns.Contains "type" then
                          fveTableCell {
                              _label "Type"
                              row.accountType
                          }

                      if workspace.visibleColumns.Contains "commodity" then
                          fveTableCell {
                              _label "Commodity"
                              row.commodity
                          }

                      if workspace.visibleColumns.Contains "balance" then
                          fveTableCell {
                              _label "Balance"
                              _class "text-right"
                              money row.balance
                          }

                      if workspace.visibleColumns.Contains "total" then
                          fveTableCell {
                              _label "Total balance (USD) · Current"
                              _class "text-right"
                              money row.totalBalance
                          }

                      fveTableCell {
                          _class "fve-table-actions text-right"
                          _attr ("data-fve-sticky-cell", "true")
                          _mobile Mobile.Actions

                          fveDropdownMenu {
                              _id $"data-account-{row.id}-actions"
                              _ariaLabel $"More actions for {row.name}"

                              fveDropdownMenuTrigger {
                                  fveButton {
                                      _variant ButtonVariant.Ghost

                                      moreActionsIcon
                                  }
                              }

                              yield!
                                  recordMenuItems
                                      "account"
                                      row.id
                                      (LedgerAccount row.id)
                                      shellDestinationUrl
                                      (System.Text.Json.JsonSerializer.Serialize row)
                          }
                      }
                  } ]

        RecordTable.render
            "Account data"
            ([ _density Density.Compact; _mobileLayout MobileLayout.Records ]
             @ selection.attributes)
            headings
            body
            [ selection.status ]

    let private ledgerPage workspace current =
        let page (actions: HtmlElement) subtitle (content: HtmlElement) =
            fvePageHeader {
                _label (ledgerTitle current)
                _description subtitle
                fvePageHeaderActions { actions }
            }
            |> fun header ->
                Page.create
                    header
                    (div {
                        _class "min-w-0 px-4 pb-4 sm:px-6 sm:pb-6 lg:px-8 lg:pb-8"
                        content
                    })
            |> Page.withWidth PageWidth.Wide
            |> Page.withBodyLayout PageBodyLayout.FullBleed

        match current with
        | LedgerAccounts ->
            let actions =
                div {
                    _class "flex flex-wrap items-center gap-2"

                    a {
                        _href (shellDestinationUrl LedgerCreateAccount)

                        _class
                            "inline-flex min-h-8 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] px-3 py-1.5 text-sm font-medium leading-5 text-white hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                        "Create"
                    }

                    fveDropdownMenu {
                        _id "ledger-accounts-page-actions"
                        _ariaLabel "More actions"

                        fveDropdownMenuTrigger {
                            fveButton {
                                _variant ButtonVariant.Ghost

                                moreActionsIcon
                            }
                        }

                        fveDropdownMenuItem {
                            _href (shellDestinationUrl LedgerSettings)
                            "Account settings"
                        }
                    }
                }

            let filtered =
                rows
                |> List.filter (fun row ->
                    row.name.Contains(workspace.searchQuery, StringComparison.OrdinalIgnoreCase)
                    && (workspace.filterType = "all"
                        || row.accountType.Equals(workspace.filterType, StringComparison.OrdinalIgnoreCase)))

            let sorted =
                match workspace.sortColumn, workspace.sortDirection with
                | "balance", "desc" -> filtered |> List.sortByDescending _.balance
                | "balance", _ -> filtered |> List.sortBy _.balance
                | _, "desc" -> filtered |> List.sortByDescending _.name
                | _ -> filtered |> List.sortBy _.name

            let pageSize = 3
            let pageCount = max 1 ((sorted.Length + pageSize - 1) / pageSize)
            let currentPage = min workspace.page pageCount

            let displayed =
                sorted |> List.skip ((currentPage - 1) * pageSize) |> List.truncate pageSize

            let visibleColumns =
                workspace.visibleColumns |> Set.toList |> List.sort |> String.concat ","

            let columnItems: HtmlElement list =
                [ for key, label in
                      [ "type", "Type"
                        "commodity", "Commodity"
                        "balance", "Balance"
                        "total", "Total balance" ] do
                      let nextColumns =
                          if workspace.visibleColumns.Contains key then
                              workspace.visibleColumns.Remove key
                          else
                              workspace.visibleColumns.Add key

                      let destination =
                          accountWorkspaceUrl
                              { workspace with
                                  visibleColumns = nextColumns
                                  page = 1 }

                      fveDropdownMenuItem {
                          _dataOn ("click", (accountWorkspacePatch destination))
                          _role "menuitemcheckbox"
                          label
                          _ariaChecked (workspace.visibleColumns.Contains key)
                      } ]

            let columnMenu =
                fveDropdownMenu {
                    _id "account-columns"
                    _ariaLabel "Choose columns"
                    fveDropdownMenuTrigger { fveButton { "Columns" } }
                    yield! columnItems
                }

            let controls =
                div {
                    _class "flex flex-wrap items-end gap-2"

                    form {
                        _method "get"
                        _action "/components/page-examples/account-management"
                        _class "flex min-w-0 flex-1 flex-wrap items-end gap-2"

                        input {
                            _type "hidden"
                            _name "destination"
                            _value "ledger-accounts"
                        }

                        input {
                            _type "hidden"
                            _name "sort"
                            _value workspace.sortColumn
                        }

                        input {
                            _type "hidden"
                            _name "direction"
                            _value workspace.sortDirection
                        }

                        input {
                            _type "hidden"
                            _name "columns"
                            _value visibleColumns
                        }

                        div {
                            _class "min-w-0 basis-full sm:min-w-48 sm:flex-1 sm:basis-auto"

                            fveField {
                                fveLabel {
                                    _for "query"
                                    "Search accounts"
                                }

                                fveInput {
                                    _id "query"
                                    _name "query"
                                    _type InputType.Search
                                    _value workspace.searchQuery
                                }
                            }
                        }

                        div {
                            _class "min-w-0 flex-[1_1_10rem] sm:flex-none"

                            fveField {
                                fveLabel {
                                    _for "accountType"
                                    "Filter by account type"
                                }

                                fveSelect {
                                    _id "accountType"
                                    _name "accountType"
                                    _value workspace.filterType

                                    fveSelectContent {
                                        for value, label, disabled in
                                            ([ for value, label in
                                                   [ "all", "All types"
                                                     "asset", "Asset"
                                                     "liability", "Liability"
                                                     "equity", "Equity"
                                                     "revenue", "Revenue"
                                                     "expense", "Expense" ] -> (value, label, false) ]) do
                                            fveSelectItem {
                                                _value value
                                                _label label
                                                _disabled disabled
                                            }
                                    }
                                }
                            }
                        }

                        fveButton {
                            _type "submit"
                            "Apply filters"
                        }
                    }

                    columnMenu

                    if workspace.searchQuery <> "" || workspace.filterType <> "all" then
                        a {
                            _href (
                                accountWorkspaceUrl
                                    { defaultAccountWorkspace with
                                        visibleColumns = workspace.visibleColumns }
                            )

                            _class
                                "inline-flex min-h-10 items-center px-2 text-sm font-medium text-[var(--fve-brand-text)]"

                            "Clear filters"
                        }
                }

            let dataTable =
                if filtered.IsEmpty then
                    fveEmptyState {
                        _label "No matching accounts"
                        _description "Change the search or clear your filters."
                    }
                else
                    accountDataTable workspace displayed

            let pagination =
                if filtered.IsEmpty then
                    div { }
                else
                    let pageUrl number =
                        accountWorkspaceUrl { workspace with page = number }

                    fvePagination {
                        _ariaLabel "Account pages"

                        fvePaginationPrevious {
                            if currentPage > 1 then
                                _href (pageUrl (currentPage - 1))
                        }

                        fvePaginationContent {
                            for number in 1..pageCount do
                                fvePaginationItem {
                                    _page number

                                    if number = currentPage then
                                        _ariaCurrent "page"
                                    else
                                        _href (pageUrl number)
                                }
                        }

                        fvePaginationNext {
                            if currentPage < pageCount then
                                _href (pageUrl (currentPage + 1))
                        }

                        fvePaginationSummary {
                            $"Showing {(currentPage - 1) * pageSize + 1}–{min (currentPage * pageSize) filtered.Length} of {filtered.Length} accounts"
                        }
                    }

            let content =
                div {
                    _class "grid gap-4"
                    dataTable |> recordActionFeedback "account"
                    pagination
                }
                |> Collection.create "Accounts"
                |> Collection.withVisuallyHiddenTitle
                |> Collection.withToolbar controls
                |> Collection.render

            page actions "Chart of accounts · Current valuation" content

        | LedgerAccount accountId ->
            let account =
                rows
                |> List.tryFind (fun row -> row.id = accountId)
                |> Option.defaultValue operatingAccount

            let actions = accountPageActions $"ledger-account-{accountId}-page-actions"

            let content =
                Detail.create
                    (ledgerTitle current)
                    [ Section.create
                          (fveSectionHeader { _label "Detail" })
                          (fveDescriptionList {
                              fveDescriptionListItem {
                                  _label "Status"

                                  fveBadge {
                                      _color Color.Success
                                      "Active"
                                  }
                              }

                              fveDescriptionListItem {
                                  _label "Type"
                                  account.accountType
                              }

                              fveDescriptionListItem {
                                  _label "Commodity"
                                  account.commodity
                              }

                              fveDescriptionListItem {
                                  _label "Parent account"

                                  if accountId = operatingAccount.id then
                                      "Current assets"
                                  else
                                      "None"
                              }

                              fveDescriptionListItem {
                                  _label "Source"
                                  "Created in Ledger"
                              }

                              fveDescriptionListItem {
                                  _label "Month-end observed balance"

                                  if accountId = operatingAccount.id then
                                      "Required"
                                  else
                                      "Not required"
                              }

                              fveDescriptionListItem {
                                  _label "Balance"
                                  money account.balance
                              }

                              _columns DescriptionListColumns.Three
                          })
                      |> Section.render
                      fveSectionHeader {
                          _label "Transactions"
                          _description "Current assets · All accounts"
                      }
                      |> fun header -> Section.create header (transactionTableFor LedgerTransaction shellDestinationUrl)
                      |> Section.withLabel "Account transactions"
                      |> Section.render ]
                |> Detail.withVisuallyHiddenTitle
                |> Detail.render

            page actions "Asset · USD · Updated moments ago" content

        | LedgerTransaction transactionId ->
            let row = transactionRows |> List.find (fun row -> row.id = transactionId)
            let actions = pageAction LedgerAccounts "View accounts"

            let content =
                Section.create
                    (fveSectionHeader { _label "Detail" })
                    (fveDescriptionList {
                        fveDescriptionListItem {
                            _label "Transaction ID"
                            string row.id
                        }

                        fveDescriptionListItem {
                            _label "Date"
                            row.date
                        }

                        fveDescriptionListItem {
                            _label "Status"
                            string row.status
                        }

                        fveDescriptionListItem {
                            _label "Accounts"
                            row.accounts
                        }

                        fveDescriptionListItem {
                            _label "Amount"
                            money row.amount
                        }
                    })
                |> Section.render

            page actions "Transaction details" content

        | LedgerCreateAccount ->
            let actions = empty

            let content =
                form {
                    _method "post"
                    _action "/components/page-examples/account-management/create"
                    _class "grid max-w-xl gap-5"

                    fveField {
                        fveLabel {
                            _for "name"
                            "Account name"
                        }

                        fveInput {
                            _id "name"
                            _name "name"
                            _value workspace.draftName
                            _required true
                            _maxlength 80
                        }
                    }

                    fveField {
                        fveLabel {
                            _for "accountType"
                            "Account type"
                        }

                        fveSelect {
                            _id "accountType"
                            _name "accountType"
                            _value workspace.draftType

                            fveSelectContent {
                                for value, label, disabled in
                                    ([ for kind in [ "Asset"; "Liability"; "Equity"; "Revenue"; "Expense" ] ->
                                           (kind, kind, false) ]) do
                                    fveSelectItem {
                                        _value value
                                        _label label
                                        _disabled disabled
                                    }
                            }
                        }
                    }

                    fveDescriptionList {
                        fveDescriptionListItem {
                            _label "Commodity"
                            "USD"
                        }

                        _columns DescriptionListColumns.One
                    }

                    p {
                        _role "status"
                        _class "text-sm text-[var(--fve-muted-text)]"
                        workspace.feedback
                    }

                    div {
                        _class "flex flex-wrap items-center gap-2"

                        fveButton {
                            _color Color.Primary
                            _variant ButtonVariant.Solid
                            _type "submit"
                            "Create account"
                        }

                        pageAction LedgerAccounts "Cancel"
                    }
                }

            page actions "Add an account to the chart of accounts." content

        | LedgerHome ->
            let actions = pageAction LedgerAccounts "View accounts"

            let content =
                section {
                    _ariaLabel "Ledger summary"

                    _class
                        "grid border-y border-[var(--fve-border)] sm:grid-cols-3 sm:divide-x sm:divide-[var(--fve-border)]"

                    for label, value, description in
                        [ "Assets", "$184,230.19", "Current valuation"
                          "Liabilities", "−$42,118.44", "Current valuation"
                          "Unverified transactions", "1", "Requires review" ] do
                        div {
                            _class "px-4 py-5 sm:px-6"

                            fveMetric {
                                _label label
                                _description description
                                value
                            }
                        }
                }

            page
                actions
                (workspace.workspaceName + " · Production")
                (div {
                    _class "grid gap-8"
                    content
                    Docs.ComponentPages.Compositions.View.balanceChart Docs.ComponentPages.Compositions.View.defaultQuery
                    upcomingPayments
                })

        | LedgerReports ->
            page
                (pageAction LedgerAccounts "View accounts")
                "Operating checking · USD"
                (div {
                    _class "grid gap-6"
                    Docs.ComponentPages.Compositions.View.balanceChart Docs.ComponentPages.Compositions.View.defaultQuery
                    transactionTableFor LedgerTransaction shellDestinationUrl
                })

        | LedgerSettings ->
            let actions = empty

            let content =
                form {
                    _method "post"
                    _action "/components/page-examples/account-management/settings"
                    _class "grid max-w-xl gap-5"

                    fveField {
                        fveLabel {
                            _for "workspaceName"
                            "Workspace name"
                        }

                        fveInput {
                            _id "workspaceName"
                            _name "workspaceName"
                            _value workspace.workspaceName
                            _required true
                            _maxlength 80
                        }
                    }

                    fveDescriptionList {
                        fveDescriptionListItem {
                            _label "Reporting currency"
                            workspace.currency

                            fveDescriptionListDescription { "The example ledger is denominated in USD." }
                        }

                        _columns DescriptionListColumns.One
                    }

                    fveField {
                        _layout FieldLayout.Horizontal

                        fveCheckbox {
                            _id "workspace-email-updates"
                            _name "emailUpdates"
                            _checked workspace.emailUpdates
                        }

                        fveLabel {
                            _for "workspace-email-updates"
                            "Receive weekly summaries"
                        }
                    }

                    p {
                        _role "status"
                        _class "text-sm text-[var(--fve-muted-text)]"
                        workspace.feedback
                    }

                    div {
                        _class "flex flex-wrap items-center gap-2"

                        fveButton {
                            _color Color.Primary
                            _variant ButtonVariant.Solid
                            _type "submit"
                            "Save settings"
                        }

                        pageAction LedgerAccounts "Cancel"
                    }
                }

            page actions (workspace.workspaceName + " · Production") content

        | _ -> failwith "Ledger pages require a Ledger destination."

    let private treasuryPage current =
        let pageTitle =
            match current with
            | TreasuryTransactions -> "Transactions"
            | TreasuryPayees -> "Payees"
            | TreasuryAccounts -> "Accounts"
            | _ -> "Overview"

        let pageHeader =
            fvePageHeader {
                _label pageTitle
                _description "Meier Made cash management"
            }

        if current = TreasuryTransactions then
            treasuryTransactionsPage
        else
            Page.create
                pageHeader
                (section {
                    _class "border-b border-[var(--fve-border)] px-4 py-6 sm:px-6 lg:px-8"

                    match current with
                    | TreasuryAccounts -> accountTable
                    | TreasuryPayees ->
                        fveTable {
                            _ariaLabel "Payees"
                            _mobileLayout MobileLayout.Records

                            fveTableHeader {
                                fveTableRow {
                                    fveTableHead { "Payee" }
                                    fveTableHead { "Latest transaction" }
                                }
                            }

                            fveTableBody {
                                for name, label, id in
                                    [ "Northwind", "Northwind payment", 201
                                      "Cloud hosting", "Cloud hosting", 202
                                      "Office supplier", "Office supplies", 204 ] do
                                    fveTableRow {
                                        fveTableHead {
                                            _scope "row"
                                            _mobile Mobile.Primary
                                            name
                                        }

                                        fveTableCell {
                                            _label "Latest transaction"

                                            a {
                                                _href (shellDestinationUrl (LedgerTransaction id))
                                                _class "text-[var(--fve-brand-text)]"
                                                label
                                            }
                                        }
                                    }
                            }
                        }
                    | _ ->
                        div {
                            _class "grid gap-8"

                            fveMetric {
                                _label "Operating balance"
                                _description "USD · Available cash"
                                "$38,442.11"
                            }

                            upcomingPayments
                            completedPayments
                        }
                })
            |> Page.withTopBar (pageTopBar (treasuryBreadcrumbs current))
            |> Page.withWidth PageWidth.Full
            |> Page.withBodyLayout PageBodyLayout.FullBleed

    // navigation: a SideNav configuration.
    // content: rendered Page HTML.
    // resolve: your destination-to-URL function.

    let ledgerShellExample resolve navigation content =
        (AppShell.create ("ledger-app-shell") (navigation) (content)
         |> AppShell.withTheme (
             (ComponentsTheme.sky
              |> ComponentsTheme.withRadius Radius.Large
              |> ComponentsTheme.withDensity Density.Compact
              |> ComponentsTheme.withControlSize Size.Small)
         )
         |> AppShell.withBoundary (AppShellBoundary.Container)
         |> AppShell.withMobileBottomNavigation (
             fveBottomNavigation {
                 _id "ledger-bottom-navigation"
                 _ariaLabel "Ledger quick navigation"

                 fveBottomNavigationItem {
                     _href (resolve LedgerHome)
                     "Dashboard"
                 }

                 fveBottomNavigationItem {
                     _href (resolve LedgerAccounts)
                     "Accounts"
                 }

                 fveBottomNavigationItem {
                     _href (resolve LedgerReports)
                     "Reports"
                 }

                 fveBottomNavigationItem {
                     _href (resolve LedgerSettings)
                     "Settings"
                 }
             }
         )
         |> AppShell.asPreview ("Ledger application preview")
         |> AppShell.render)

    let treasuryShellExample resolve navigation content =
        (AppShell.create ("treasury-app-shell") (navigation) (content)
         |> AppShell.withTheme (
             (ComponentsTheme.sky
              |> ComponentsTheme.withRadius Radius.Medium
              |> ComponentsTheme.withDensity Density.Compact
              |> ComponentsTheme.withControlSize Size.Small)
         )
         |> AppShell.withBoundary (AppShellBoundary.Container)
         |> AppShell.withMobileBottomNavigation (
             fveBottomNavigation {
                 _id "treasury-bottom-navigation"
                 _ariaLabel "Treasury quick navigation"

                 fveBottomNavigationItem {
                     _href (resolve TreasuryHome)
                     "Overview"
                 }

                 fveBottomNavigationItem {
                     _href (resolve TreasuryTransactions)
                     "Transactions"
                 }

                 fveBottomNavigationItem {
                     _href (resolve TreasuryPayees)
                     "Payees"
                 }

                 fveBottomNavigationItem {
                     _href (resolve TreasuryAccounts)
                     "Accounts"
                 }
             }
         )
         |> AppShell.asPreview ("Treasury application preview")
         |> AppShell.render)

    let ledgerShellWith workspace current =
        let page =
            div {
                _class "flex h-full min-h-0 flex-col"
                _attr ("data-signals__root__ifmissing", "{ledgerRefreshes: 0, ledgerCreates: 0}")

                div {
                    _class "hidden shrink-0 md:block"
                    pageTopBar (ledgerBreadcrumbs current)
                }

                div {
                    _class "min-h-0 flex-1"
                    ledgerPage workspace current |> Page.render
                }

                output {
                    _class "sr-only"
                    _role "status"
                    _dataText "'Balance refreshes: ' + $ledgerRefreshes"
                    "Balance refreshes: 0"
                }
            }

        ledgerShellExample shellDestinationUrl (ledgerNavigation false current) page

    let ledgerShell current =
        ledgerShellWith defaultAccountWorkspace current

    let treasuryShell current =
        treasuryShellExample shellDestinationUrl (treasuryNavigation current) (treasuryPage current |> Page.render)

    let private shellPreviewWith workspace current =
        match current with
        | LedgerHome
        | LedgerAccounts
        | LedgerAccount _
        | LedgerTransaction _
        | LedgerCreateAccount
        | LedgerReports
        | LedgerSettings -> ledgerShellWith workspace current
        | TreasuryHome
        | TreasuryPeriod _
        | TreasuryTransactions
        | TreasuryPayees
        | TreasuryAccounts -> treasuryShell current
        |> fun shell ->
            div {
                _attr ("data-fve-full-bleed-example", "true")
                _class "docs-components-preview h-[36rem]"
                shell
            }

    let private appModePrevious current =
        match current with
        | LedgerAccounts -> None
        | LedgerAccount _ -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | LedgerTransaction _ ->
            Some(FixtureLink.create "Operating checking" (shellDestinationUrl (LedgerAccount operatingAccount.id)))
        | LedgerCreateAccount -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | LedgerHome
        | LedgerReports
        | LedgerSettings -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | _ -> None

    let private appModeNext current =
        match current with
        | LedgerAccounts ->
            Some(FixtureLink.create "Operating checking" (shellDestinationUrl (LedgerAccount operatingAccount.id)))
        | LedgerAccount _ -> Some(FixtureLink.create "Create account" (shellDestinationUrl LedgerCreateAccount))
        | LedgerCreateAccount -> Some(FixtureLink.create "Dashboard" (shellDestinationUrl LedgerHome))
        | LedgerHome
        | LedgerReports
        | LedgerSettings -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | _ -> None

    let private shellFixtureConfigWith workspace current =
        let fixture =
            fveBrowser {
                _address ("https://fve.meiermade.com" + shellDestinationUrl current)
                shellPreviewWith workspace current
            }
            |> Fixture.create "ledger-workflow" "Ledger account workflow" (shellDestinationUrl current)
            |> Fixture.withFullscreenContent (shellPreviewWith workspace current)

        let fixture =
            appModePrevious current
            |> Option.map (fun previous -> Fixture.withPrevious previous fixture)
            |> Option.defaultValue fixture

        appModeNext current
        |> Option.map (fun next -> Fixture.withNext next fixture)
        |> Option.defaultValue fixture

    let shellFixtureWith workspace current =
        div {
            _id "components-app-shell-fixture"

            for attribute in shellFixtureNavigationAttributes do
                attribute

            shellFixtureConfigWith workspace current |> Fixture.render
        }

    let shellFixtureFor current =
        shellFixtureWith defaultAccountWorkspace current

    let accountManagementExamples current =
        [ sample
              "account-management"
              "Connected account workflow"
              (shellSource @ [ "ledgerShellExample"; "treasuryShellExample" ])
              (shellFixtureFor current)
          sample
              "responsive-actions"
              "Responsive page actions"
              (shellSource
               @ [ "refreshIcon"
                   "moreActionsIcon"
                   "refreshBalancesAction"
                   "pageAction"
                   "accountPageActions"
                   "responsiveActionsExample" ])
              (responsiveActionsExample |> fullBleedThemedSurface)
          |> note
              "This is consumer-authored layout, not component policy. The highest-priority action stays visible; below 400px of header width, the reports link moves into a separately identified menu. Both menus use the same real destinations and DropdownMenu keyboard behavior." ]

    let examples () = accountManagementExamples LedgerAccounts


    let private galleryContent : Content =
        { description = $"Use the {accountManagementRegistration.title} component in typed F# interfaces."
          usage = []
          sections = [
              DocumentationSection.create
                  "building-blocks"
                  "Built with"
                  [ buildingBlockLinks
                        "Account management building blocks"
                        [ "/components/app-shell", "App shell"
                          "/components/page", "Page"
                          "/components/collection", "Collection"
                          "/components/detail", "Detail"
                          "/components/page-header", "Page header"
                          "/components/button", "Button"
                          "/components/dropdown-menu", "Dropdown menu"
                          "/components/form-layouts", "Form layouts" ] ]
          ]
          apiCategory = None }

    let accountManagementPageWith workspace current =
        let examples =
            accountManagementExamples current
            |> List.map (fun example ->
                if example.id = "components-account-management" then
                    { example with
                        preview = shellFixtureWith workspace current }
                else
                    example)

        gallery accountManagementRegistration galleryContent examples
        |> DocumentationPage.withFixtures [ shellFixtureConfigWith workspace current ]

    let accountManagementPageFor current =
        accountManagementPageWith defaultAccountWorkspace current

    let accountManagementPage = accountManagementPageFor LedgerAccounts
