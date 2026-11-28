namespace Example.UI.Views

open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open type Html
open Model
open ViewData
open TransactionTable

module internal ViewAccount =
    let accountDetail (query:Query) id =
        let account = query.records.accounts |> List.find (fun account -> account.id = id)
        let query = { query with account = string id }
        let page = ApplicationPage.Account id
        let data = homeData query
        let childAccounts = children query.records id
        let transactions = data.transactions |> List.sortByDescending (fun row -> row.date, row.id)
        let pages = max 1 ((transactions.Length + 9) / 10)
        let current = min pages query.transactionPage
        let rows = transactions |> List.skip ((current - 1) * 10) |> List.truncate 10
        let pageHref number =
            applicationHref query page + querySuffix ["range", query.range; "comparison", query.comparison; "startDate", query.startDate; "endDate", query.endDate; "transactionPage", string number] + "#account-transactions"

        fveSectionStack {
            fveSection {
                _label "Details"
                if not (isRoot account) then
                    fveSectionActions { fveLink {
                        _href (collectionHref { query with overlayFrom="" } (ApplicationPage.EditAccount id))
                        _ariaLabel "Edit account"; _variant LinkVariant.Outline
                        Layout.icon IconName.PencilSquare
                        "Edit"
                    } }
                fveDescriptionList {
                    _columns DescriptionListColumns.Three
                    fveDescriptionListItem { _label "Type"; string account.accountType }
                    fveDescriptionListItem { _label "Commodity"; account.currency }
                    match account.parentId with
                    | Some parentId ->
                        let parent = query.records.accounts |> List.find (fun row -> row.id = parentId)
                        fveDescriptionListItem { _label "Parent account"; Layout.inlineLink (applicationHref query (ApplicationPage.Account parentId)) parent.name }
                    | None -> fveDescriptionListItem { _label "Parent account"; "None" }
                    fveDescriptionListItem { _label "Source"; "Manual" }
                    fveDescriptionListItem { _label "Month-end observed balance"; "Not required" }
                    fveDescriptionListItem { _label "Balance"; money (account.balance) }
                    if not childAccounts.IsEmpty then
                        fveDescriptionListItem { _label "Total balance (USD)"; money data.currentBalance }
                }
            }
            fveSection {
                _label "Balance history"
                _description "Compare actual and projected balances. Zoom the chart to focus the transaction history below."
                Home.balanceHistory page false query
            }
            if not childAccounts.IsEmpty then
                fveSection {
                    _label "Child accounts"
                    fveTable {
                        _ariaLabel "Child accounts"
                        _mobileLayout MobileLayout.Records
                        fveTableHeader { fveTableRow { fveTableHead { "Account" }; fveTableHead { _class "text-right"; "Total balance (USD)" } } }
                        fveTableBody {
                            for row in childAccounts do
                                fveTableRow {
                                    fveTableHead { _scope "row"; _mobile Mobile.Primary; Layout.inlineLink (applicationHref query (ApplicationPage.Account row.id)) row.name }
                                    fveTableCell { _label "Total balance (USD)"; _class "text-right"; money (row.totalBalance) }
                                }
                        }
                    }
                }
            fveSection {
                _id "account-transactions"; _label "Transactions"
                fveSectionActions { viewAllLink query id }
                if transactions.IsEmpty then
                    fveEmptyState { _label "No transactions in this date range"; _description "Choose different dates or reset zoom." }
                else
                    transactionTable query (account.name + " transactions") rows (Some data.accountIds)
                    footer {
                        fvePagination {
                            _ariaLabel "Account transactions"; _variant PaginationVariant.PreviousNextOnly
                            fvePaginationSummary { $"Showing {(current - 1) * 10 + 1}–{(current - 1) * 10 + rows.Length} of {transactions.Length} transactions" }
                            fvePaginationPrevious { if current > 1 then _href (pageHref (current - 1)) }
                            fvePaginationNext { if current < pages then _href (pageHref (current + 1)) }
                        }
                    }
            }
        }
