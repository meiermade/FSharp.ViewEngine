namespace Docs.Common

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.SharedExamples

module LedgerExamples =
    let rows =
        [ { id = 101
            name = "Assets"
            accountType = "Asset"
            commodity = "USD"
            balance = 0M
            totalBalance = 184230.19M }
          { id = 102
            name = "Liabilities"
            accountType = "Liability"
            commodity = "USD"
            balance = 0M
            totalBalance = -42118.44M }
          { id = 103
            name = "Equity"
            accountType = "Equity"
            commodity = "USD"
            balance = -93136.75M
            totalBalance = -101336.75M }
          { id = 104
            name = "Revenue"
            accountType = "Revenue"
            commodity = "USD"
            balance = -138425M
            totalBalance = -138425M }
          { id = 105
            name = "Expenses"
            accountType = "Expense"
            commodity = "USD"
            balance = 97650M
            totalBalance = 97650M }
          { id = 106
            name = "Placeholder"
            accountType = "Placeholder"
            commodity = "USD"
            balance = 0M
            totalBalance = 0M } ]

    let operatingAccount: AccountRow =
        { id = 2048
          name = "Operating checking"
          accountType = "Asset"
          commodity = "USD"
          balance = 38442.11M
          totalBalance = 38442.11M }

    let money amount =
        if amount < 0M then $"−${-amount:N2}" else $"${amount:N2}"

    let recordActionFeedback kind (content: HtmlElement) =
        div {
            _attr ("data-signals__root__ifmissing", $"{{_{kind}MenuFeedback: ''}}")
            content

            p {
                _role "status"
                _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                _dataShow $"$_{kind}MenuFeedback != ''"
                _dataText $"$_{kind}MenuFeedback"
                _style "display:none"
            }
        }

    let recordMenuItems kind id destination resolve payload =
        let json value =
            System.Text.Json.JsonSerializer.Serialize(value)

        let feedback = $"$_{kind}MenuFeedback"

        let copy label expression =
            let failure = json $"Could not copy {label}. Check clipboard permissions."
            let success = json $"Copied {label}."

            fveDropdownMenuItem {
                _dataOn (
                    "click",
                    $"{feedback} = ''; if (navigator.clipboard?.writeText) {{ navigator.clipboard.writeText({expression}).then(() => {{ {feedback} = {success} }}).catch(() => {{ {feedback} = {failure} }}) }} else {{ {feedback} = {failure} }}"
                )

                $"Copy {label}"
            }

        let url = resolve destination |> json

        let download =
            $"const url = URL.createObjectURL(new Blob([{json payload}], {{type: 'application/json'}})); const link = document.createElement('a'); link.href = url; link.download = '{kind}-{id}.json'; document.body.append(link); link.click(); link.remove(); URL.revokeObjectURL(url); {feedback} = 'Downloaded {kind}.'"

        [ fveDropdownMenuItem {
              _href (resolve destination)
              $"View {kind}"
          }
          copy $"{kind} ID" (json (string id))
          copy $"{kind} link" $"new URL({url}, window.location.href).href"
          fveDropdownMenuSeparator { () }
          fveDropdownMenuItem {
              _dataOn ("click", download)
              $"Download {kind}"
          } ]

    let private accountTableViewFor
        (accountRows: AccountRow list)
        destinationFor
        resolve
        (rowAttributes: AccountRow -> HtmlAttribute list)
        =
        let selection =
            RecordSelection.view
                "accounts-selection"
                "accountIds"
                []
                (accountRows |> List.map (fun row -> string row.id, row.name))

        let headings =
            [ selection.heading
              fveTableHead { "Account" }
              fveTableHead { "Type" }
              fveTableHead { "Commodity" }
              fveTableHead {
                  _class "text-right"
                  "Balance"
              }
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
                      yield! rowAttributes row
                      selection.cell (string row.id) row.name

                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary

                          a {
                              _href (resolve (destinationFor row.id))
                              _class "font-medium text-[var(--fve-brand-text)]"
                              row.name
                          }
                      }

                      fveTableCell {
                          _label "Type"
                          row.accountType
                      }

                      fveTableCell {
                          _label "Commodity"
                          row.commodity
                      }

                      fveTableCell {
                          _label "Balance"
                          _class "text-right"
                          money row.balance
                      }

                      fveTableCell {
                          _label "Total balance (USD) · Current"
                          _class "text-right"
                          money row.totalBalance
                      }

                      fveTableCell {
                          _mobile Mobile.Actions
                          _class "fve-table-actions text-right"
                          _attr ("data-fve-sticky-cell", "true")

                          fveDropdownMenu {
                              _id $"account-{row.id}-actions"
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
                                      (destinationFor row.id)
                                      resolve
                                      (System.Text.Json.JsonSerializer.Serialize row)
                          }
                      }
                  } ]

        RecordTable.render
            "Accounts"
            ([ _density Density.Compact; _mobileLayout MobileLayout.Records ]
             @ selection.attributes)
            headings
            body
            [ selection.status ]

    let accountTableView destinationFor resolve rowAttributes =
        accountTableViewFor rows destinationFor resolve rowAttributes

    let hierarchyItems =
        [ { key = "projects"
            ancestors = []
            level = 0
            name = "Projects"
            kind = "Folder"
            count = 24
            hasChildren = true }
          { key = "website"
            ancestors = [ "projects" ]
            level = 1
            name = "Website"
            kind = "Folder"
            count = 12
            hasChildren = true }
          { key = "notes"
            ancestors = [ "projects"; "website" ]
            level = 2
            name = "Project notes"
            kind = "Document"
            count = 1
            hasChildren = false }
          { key = "design"
            ancestors = [ "projects"; "website" ]
            level = 2
            name = "Design files"
            kind = "Folder"
            count = 11
            hasChildren = false }
          { key = "research"
            ancestors = [ "projects" ]
            level = 1
            name = "Research"
            kind = "Folder"
            count = 12
            hasChildren = false }
          { key = "shared"
            ancestors = []
            level = 0
            name = "Shared files"
            kind = "Folder"
            count = 8
            hasChildren = true }
          { key = "guide"
            ancestors = [ "shared" ]
            level = 1
            name = "Team guide"
            kind = "Document"
            count = 1
            hasChildren = false } ]

    let hierarchicalItemTable =
        fveTable {
            _ariaLabel "Item hierarchy"
            _mobileLayout MobileLayout.Records
            _attr ("data-signals__root__ifmissing", "{itemHierarchyExpanded:['projects','website','shared']}")

            fveTableHeader {
                fveTableRow {
                    fveTableHead { "Name" }
                    fveTableHead { "Type" }

                    fveTableHead {
                        _class "text-right"
                        "Items"
                    }
                }
            }

            fveTableBody {
                for row in hierarchyItems do
                    let key = System.Text.Json.JsonSerializer.Serialize row.key
                    let ancestors = System.Text.Json.JsonSerializer.Serialize row.ancestors
                    let expanded = $"$itemHierarchyExpanded.includes({key})"

                    fveTableRow {
                        _id ("hierarchy-" + row.key)
                        _dataShow $"{ancestors}.every(key => $itemHierarchyExpanded.includes(key))"

                        fveTableHead {
                            _scope "row"
                            _mobile Mobile.Primary

                            span {
                                _class "inline-flex items-center gap-2"
                                _style $"padding-inline-start:{row.level}rem"

                                if row.hasChildren then
                                    button {
                                        _type "button"

                                        _class
                                            "inline-flex size-8 items-center justify-center rounded focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                                        _dataAttr ("aria-expanded", expanded)

                                        _dataAttr (
                                            "aria-label",
                                            $"({expanded} ? 'Collapse ' : 'Expand ') + {System.Text.Json.JsonSerializer.Serialize row.name}"
                                        )

                                        _dataOn (
                                            "click",
                                            $"$itemHierarchyExpanded = {expanded} ? $itemHierarchyExpanded.filter(key => key !== {key}) : [...$itemHierarchyExpanded, {key}]"
                                        )

                                        span {
                                            _ariaHidden true
                                            _dataText $"{expanded} ? '▾' : '▸'"
                                        }
                                    }
                                else
                                    span {
                                        _ariaHidden true
                                        _class "inline-block size-8"
                                    }

                                a {
                                    _href "/components/table"
                                    _class "font-medium text-[var(--fve-brand-text)]"
                                    row.name
                                }
                            }
                        }

                        fveTableCell {
                            _label "Type"
                            row.kind
                        }

                        fveTableCell {
                            _label "Items"
                            _class "text-right"
                            string row.count
                        }
                    }
            }
        }

    let private accountFilterExpression (row: AccountRow) =
        let searchable =
            System.Text.Json.JsonSerializer.Serialize(
                $"{row.name} {row.accountType} {row.commodity}".ToLowerInvariant()
            )

        let accountType =
            System.Text.Json.JsonSerializer.Serialize(row.accountType.ToLowerInvariant())

        $"(!$collectionquery.trim() || {searchable}.includes($collectionquery.trim().toLowerCase())) && ($collectiontype == 'all' || $collectiontype == {accountType})"

    let collectionHasMatches =
        rows
        |> List.map (fun row -> $"({accountFilterExpression row})")
        |> String.concat " || "

    let filteredAccountTable =
        accountTableView LedgerAccount shellDestinationUrl (fun row -> [ _dataShow (accountFilterExpression row) ])
        |> recordActionFeedback "account"

    let transactionRows =
        [ { id = 201
            date = "Jul 28"
            description = "Northwind payment"
            status = Verified
            accounts = "Accounts receivable → Operating checking"
            amount = 4800M }
          { id = 202
            date = "Jul 27"
            description = "Cloud hosting"
            status = Verified
            accounts = "Operating checking → Software expense"
            amount = 386.42M }
          { id = 203
            date = "Jul 26"
            description = "ACH withdrawal"
            status = Unverified
            accounts = "Operating checking → Placeholder"
            amount = 1240M }
          { id = 204
            date = "Jun 24"
            description = "Office supplies"
            status = Committed
            accounts = "Business card → Office expense"
            amount = 128.19M } ]

    let transactionTableFor destinationFor resolve =
        let selection =
            RecordSelection.view
                "transactions-selection"
                "transactions-selection"
                []
                (transactionRows |> List.map (fun row -> string row.id, row.description))

        let headings =
            [ selection.heading
              fveTableHead { "Date" }
              fveTableHead { "Description" }
              fveTableHead { "Status" }
              fveTableHead { "Accounts" }
              fveTableHead {
                  _class "text-right"
                  "Amount"
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
            [ for row in transactionRows do
                  fveTableRow {
                      yield! selection.rowAttributes (string row.id)
                      selection.cell (string row.id) row.description

                      fveTableCell {
                          _label "Date"
                          row.date
                      }

                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary

                          a {
                              _href (resolve (destinationFor row.id))
                              _class "font-medium text-[var(--fve-brand-text)]"
                              row.description
                          }
                      }

                      fveTableCell {
                          _label "Status"

                          match row.status with
                          | Verified ->
                              fveBadge {
                                  _color Color.Success
                                  "Verified"
                              }
                          | Unverified ->
                              fveBadge {
                                  _color Color.Warning
                                  "Unverified"
                              }
                          | Committed ->
                              fveBadge {
                                  _color Color.Neutral
                                  "Committed"
                              }
                      }

                      fveTableCell {
                          _label "Accounts"
                          row.accounts
                      }

                      fveTableCell {
                          _label "Amount"
                          _class "text-right"
                          money row.amount
                      }

                      fveTableCell {
                          _mobile Mobile.Actions
                          _class "fve-table-actions text-right"
                          _attr ("data-fve-sticky-cell", "true")

                          let payload =
                              System.Text.Json.JsonSerializer.Serialize
                                  {| id = row.id
                                     date = row.date
                                     description = row.description
                                     status = string row.status
                                     accounts = row.accounts
                                     amount = row.amount |}

                          fveDropdownMenu {
                              _id $"transaction-{row.id}-actions"
                              _ariaLabel $"More actions for {row.description}"

                              fveDropdownMenuTrigger {
                                  fveButton {
                                      _variant ButtonVariant.Ghost

                                      moreActionsIcon
                                  }
                              }

                              yield! recordMenuItems "transaction" row.id (destinationFor row.id) resolve payload
                          }
                      }
                  } ]

        RecordTable.render
            "Transactions"
            ([ _density Density.Compact; _mobileLayout MobileLayout.Records ]
             @ selection.attributes)
            headings
            body
            [ selection.status ]
        |> recordActionFeedback "transaction"

    let transactionTable =
        transactionTableFor LedgerTransaction shellDestinationUrl

    let toolbar =
        section {
            _role "region"
            _ariaLabel "Collection controls"

            div {
                _class "flex flex-col gap-2 sm:flex-row sm:items-center"

                div {
                    _class "min-w-40 flex-1"

                    fveField {
                        fveLabel {
                            _for "query"
                            _class "sr-only"
                            "Search accounts"
                        }

                        fveInput {
                            _id "query"
                            _name "query"
                            _type InputType.Search

                            _placeholder "Search accounts"
                            _dataBind ("collectionquery", ["root"])
                            _dataAttr ("disabled", "$collectionstate != 'ready'")
                        }
                    }
                }

                label {
                    _class
                        "flex min-h-[var(--fve-control-min-height)] items-stretch overflow-hidden whitespace-nowrap rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] text-xs ring-1 ring-[var(--fve-border)]"

                    span {
                        _class
                            "flex items-center border-r border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] px-2 font-semibold text-[var(--fve-muted-text)]"

                        "As of"
                    }

                    input {
                        _type "date"
                        _name "asOf"
                        _ariaLabel "As of"
                        _value "2026-07-31"
                        _class "min-w-0 w-28 bg-transparent px-2 text-[var(--fve-text)] outline-none"
                    }
                }

                select {
                    _name "accountType"
                    _ariaLabel "Filter by account type"
                    _dataBind ("collectiontype", ["root"])
                    _dataAttr ("disabled", "$collectionstate != 'ready'")

                    _class
                        "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm ring-1 ring-[var(--fve-border)] outline-none focus:ring-2 focus:ring-[var(--fve-brand-ring)]"

                    option {
                        _value "all"
                        "All types"
                    }

                    option {
                        _value "asset"
                        "Asset"
                    }

                    option {
                        _value "liability"
                        "Liability"
                    }

                    option {
                        _value "equity"
                        "Equity"
                    }

                    option {
                        _value "revenue"
                        "Revenue"
                    }

                    option {
                        _value "expense"
                        "Expense"
                    }
                }

                button {
                    _type "button"
                    _dataOn ("click", "$collectionquery = ''; $collectiontype = 'all'")

                    _dataAttr (
                        "disabled",
                        "(!$collectionquery && $collectiontype == 'all') || $collectionstate != 'ready'"
                    )

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] disabled:opacity-50"

                    "Clear filters"
                }

                button {
                    _type "button"
                    _dataOn ("click", "$collectionstate = 'pending'; setTimeout(() => $collectionstate = 'ready', 350)")
                    _dataAttr ("disabled", "$collectionstate != 'ready'")

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] disabled:opacity-50"

                    "Refresh filters"
                }

                button {
                    _type "button"
                    _dataOn ("click", "$collectionstate = 'error'")
                    _dataAttr ("disabled", "$collectionstate != 'ready'")

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] disabled:opacity-50"

                    "Simulate filter error"
                }
            }

            p {
                _dataShow "$collectionstate == 'pending'"
                _role "status"
                _ariaLive "polite"
                _class "text-sm text-[var(--fve-muted-text)]"
                "Refreshing filter options…"
            }

            div {
                _dataShow "$collectionstate == 'error'"
                _role "alert"

                _class
                    "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)]"

                span { "Filter options are temporarily unavailable." }

                button {
                    _type "button"
                    _dataOn ("click", "$collectionstate = 'ready'")

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 font-semibold ring-1 ring-[var(--fve-critical-ring)]"

                    "Retry filters"
                }
            }
        }

    let collectionActions =
        a {
            _href (shellDestinationUrl LedgerCreateAccount)

            _class
                "inline-flex items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] px-3 py-2 text-sm font-medium text-white hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

            "New account"
        }

    let treasuryBreadcrumbs current =
        let items =
            match current with
            | TreasuryHome ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  } ]
            | TreasuryPeriod period ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl (TreasuryPeriod period))
                      period
                  } ]
            | TreasuryTransactions ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl (TreasuryPeriod "July"))
                      "July 2026"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryTransactions)
                      "Transactions"
                  } ]
            | TreasuryPayees ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryPayees)
                      "Payees"
                  } ]
            | TreasuryAccounts ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryAccounts)
                      "Accounts"
                  } ]
            | _ ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  } ]

        fveBreadcrumbs {
            _id "treasury-breadcrumbs"
            _ariaLabel "Breadcrumb"
            yield! items
        }

    let refreshBalancesAction =
        fveButton {
            _color Color.Primary
            _variant ButtonVariant.Solid
            _ariaLabel "Refresh balances"
            _dataOn ("click", "$ledgerRefreshes++")

            span {
                _ariaHidden true
                _class "inline-flex shrink-0"
                refreshIcon
            }
        }

    let pageAction destination (label: string) =
        a {
            _href (shellDestinationUrl destination)

            _class
                "inline-flex min-h-8 items-center px-3 text-sm font-medium text-[var(--fve-brand-text)] underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

            label
        }

    // Consumer-authored responsive policy, used by the Account management page example.

    let accountPageHeader =
        fvePageHeader {
            _label "Account 2048"
            _description "Operating checking · Updated moments ago"

            fvePageHeaderActions {
                div {
                    _class "flex flex-wrap items-center gap-2"
                    refreshBalancesAction
                    pageAction LedgerReports "View reports"
                }
            }
        }

    let upcomingPayments =
        let actions =
            div {
                _class "flex flex-wrap items-center gap-2"
                pageAction TreasuryPayees "View payees"

                fveDropdownMenu {
                    _id "upcoming-payment-actions"
                    _ariaLabel "More actions"

                    fveDropdownMenuTrigger {
                        fveButton {
                            _variant ButtonVariant.Ghost

                            moreActionsIcon
                        }
                    }

                    fveDropdownMenuItem {
                        _href (shellDestinationUrl TreasuryAccounts)
                        "View accounts"
                    }
                }
            }

        let header =
            fveSectionHeader {
                _label "Upcoming transactions"
                _description "Payments expected before the current period closes."
                fveSectionHeaderActions { actions }
            }

        Section.create
            header
            (ul {
                _role "list"
                _class "divide-y divide-[var(--fve-border)]"

                li {
                    _class "flex items-center justify-between gap-4 py-3 text-sm"
                    span { "Aug 15 · Payroll" }

                    fveBadge {
                        _color Color.Success
                        "Ready"
                    }
                }

                li {
                    _class "flex items-center justify-between gap-4 py-3 text-sm"
                    span { "Aug 18 · Cloud hosting" }

                    fveBadge {
                        _color Color.Info
                        "Scheduled"
                    }
                }
            })
        |> Section.render

    let completedPayments =
        Section.create
            (fveSectionHeader { _label "Completed transactions" })
            (div {
                _class "flex items-center justify-between gap-4 py-3 text-sm"
                span { "Jul 28 · Northwind payment" }

                strong {
                    _class "font-semibold"
                    "$4,800.00"
                }
            })
        |> Section.render

    let transactionTabs =
        fveTabs {
            _id "treasury-transactions-tabs"
            _ariaLabel "Transaction views"

            fveTabsItem {
                _value "upcoming"
                fveTabsTrigger { "Upcoming" }
                upcomingPayments
            }

            fveTabsItem {
                _value "completed"
                fveTabsTrigger { "Completed" }
                completedPayments
            }

            _variant TabsVariant.Underlined
        }

    // Supply the top bar and tab content; Page owns their layout.

    let transactionsPage topBar tabs =
        fvePageHeader {
            _label "Transactions"
            _description "Review scheduled and completed cash activity."
        }
        |> fun pageHeader -> Page.create pageHeader empty
        |> Page.withTopBar topBar
        |> Page.withTabs tabs
        |> Page.withWidth PageWidth.Wide
        |> Page.withBodyLayout PageBodyLayout.Padded

    let renderTransactionsPage topBar tabs =
        transactionsPage topBar tabs |> Page.render
