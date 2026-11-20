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
open Docs.ComponentPages.Button.View
open Docs.ComponentPages.SideNav.View
open Docs.ComponentPages.PageTopBar.View

open Docs.Common.LedgerExamples

module Examples =
    let accountNameExists name =
        operatingAccount :: rows
        |> List.exists (fun row -> String.Equals(row.name, name, StringComparison.OrdinalIgnoreCase))

    let accountTable =
        accountTableView LedgerAccount shellDestinationUrl (fun _ -> [])
        |> recordActionFeedback "account"

    let private accountTypeFilter =
        fveField {
            fveLabel {
                _for "accountTypeFilter"
                _class "sr-only"
                "Filter by account type"
            }

            fveSelect {
                _id "accountTypeFilter"
                _name "accountTypeFilter"
                _value "all"

                fveSelectContent {
                    for value, label, disabled in
                        ([ ("all", "All types", false)
                           ("asset", "Asset", false)
                           ("liability", "Liability", false)
                           ("equity", "Equity", false)
                           ("revenue", "Revenue", false)
                           ("expense", "Expense", false) ]) do
                        fveSelectItem {
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let private treasuryMark =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M10.75 2.75a.75.75 0 0 0-1.5 0V4H7.5a2.5 2.5 0 0 0 0 5h5a1 1 0 1 1 0 2h-5a1 1 0 0 1-.93-.63.75.75 0 1 0-1.4.54A2.5 2.5 0 0 0 7.5 12.5h1.75v1.75a.75.75 0 0 0 1.5 0V12.5h1.75a2.5 2.5 0 0 0 0-5h-5a1 1 0 1 1 0-2h5a1 1 0 0 1 .93.63.75.75 0 1 0 1.4-.54A2.5 2.5 0 0 0 12.5 4h-1.75V2.75Z"/></svg>"""

    let private accountTitle id =
        if id = 2048 then
            "Operating checking"
        else
            rows
            |> List.tryFind (fun row -> row.id = id)
            |> Option.map _.name
            |> Option.defaultValue $"Account {id}"

    let private transactionTitle id =
        transactionRows |> List.find (fun row -> row.id = id) |> _.description

    let ledgerBreadcrumbs current =
        let items =
            match current with
            | LedgerHome ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  } ]
            | LedgerAccounts ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerAccounts)
                      "Accounts"
                  } ]
            | LedgerAccount id ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerAccounts)
                      "Accounts"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl (LedgerAccount id))
                      accountTitle id
                  } ]
            | LedgerTransaction id ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl (LedgerTransaction id))
                      transactionTitle id
                  } ]
            | LedgerCreateAccount ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerAccounts)
                      "Accounts"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerCreateAccount)
                      "Create account"
                  } ]
            | LedgerReports ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerReports)
                      "Reports"
                  } ]
            | LedgerSettings ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerSettings)
                      "Settings"
                  } ]
            | _ ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  } ]

        fveBreadcrumbs {
            _id "ledger-breadcrumbs"
            _ariaLabel "Breadcrumb"
            yield! items
        }

    let ledgerTitle =
        function
        | LedgerHome -> "Dashboard"
        | LedgerAccounts -> "Accounts"
        | LedgerAccount id -> accountTitle id
        | LedgerTransaction id -> transactionTitle id
        | LedgerCreateAccount -> "Create account"
        | LedgerReports -> "Reports"
        | LedgerSettings -> "Settings"
        | _ -> "Ledger"

    let accountPageActions id =
        div {
            _id id
            _class "flex shrink-0 items-center gap-2"
            refreshBalancesAction

            div {
                _class "hidden items-center gap-2 @min-[400px]:flex"
                pageAction LedgerReports "View reports"

                fveDropdownMenu {
                    _id (id + "-wide")
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

            div {
                _class "@min-[400px]:hidden"

                fveDropdownMenu {
                    _id (id + "-compact")
                    _ariaLabel "More actions"

                    fveDropdownMenuTrigger {
                        fveButton {
                            _variant ButtonVariant.Ghost

                            moreActionsIcon
                        }
                    }

                    fveDropdownMenuItem {
                        _href (shellDestinationUrl LedgerReports)
                        "View reports"
                    }

                    fveDropdownMenuItem {
                        _href (shellDestinationUrl LedgerSettings)
                        "Account settings"
                    }
                }
            }
        }

    // Breadcrumbs are optional content, not a built-in heading.

    let pageHeaderPreview =
        div {
            _dataSignals "{ledgerRefreshes: 0}"
            accountPageHeader

            output {
                _class "sr-only"
                _role "status"
                _dataText "'Balance refreshes: ' + $ledgerRefreshes"
                "Balance refreshes: 0"
            }
        }
        |> themedSurface

    let treasuryNavigation current =
        let navigationCurrent =
            match current with
            | TreasuryPeriod _ -> TreasuryTransactions
            | TreasuryHome
            | TreasuryTransactions
            | TreasuryPayees
            | TreasuryAccounts -> current
            | _ -> TreasuryHome

        let workspace =
            div {
                p {
                    _class "text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"
                    "Workspace"
                }

                p {
                    _class "mt-1 truncate text-sm font-semibold"
                    "Meier Made"
                }
            }

        let navigationHeader =
            fveSideNavHeader {
                _label "Treasury"

                div {
                    _class "flex min-w-0 items-center gap-3"

                    span {
                        _ariaHidden true

                        _class
                            "flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white"

                        treasuryMark
                    }

                    strong {
                        _class "truncate text-base font-semibold"
                        "Treasury"
                    }
                }
            }

        fveSideNav {
            _id "treasury-side-navigation"
            _ariaLabel "Treasury primary navigation"
            navigationHeader

            fveSideNavContent {
                shellItem TreasuryHome "Overview"
                shellItem TreasuryTransactions "Transactions"
                shellItem TreasuryPayees "Payees"
                shellItem TreasuryAccounts "Accounts"
            }

            _value (shellDestinationUrl navigationCurrent)
            Html._width SideNavWidth.Narrow

            fveSideNavContext {
                div {
                    _class "px-4 py-3"
                    workspace
                }
            }

            fveSideNavMobileContext { workspace }

            fveSideNavFooter {
                div {
                    _class "flex min-w-0 items-center gap-3 px-3 py-2"

                    fveAvatar { _ariaHidden true; _size Size.Small; "AM" }

                    span {
                        _class "min-w-0 truncate text-sm font-semibold"
                        "Andy Meier"
                    }
                }
            }
        }

    let treasuryTransactionsPage =
        transactionsPage (pageTopBar (treasuryBreadcrumbs TreasuryTransactions)) transactionTabs

    let responsiveActionsExample =
        div {
            _dataSignals "{ledgerRefreshes: 0}"

            fvePageHeader {
                _label "Operating checking"
                fvePageHeaderActions { accountPageActions "responsive-account-actions" }
            }

            output {
                _class "sr-only"
                _role "status"
                _dataText "'Balance refreshes: ' + $ledgerRefreshes"
                "Balance refreshes: 0"
            }
        }
