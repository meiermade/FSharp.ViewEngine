namespace Example.Server.ApplicationPages

open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open Model
open Ledger.Domain
open TransactionTable

module internal ViewAccount =
    let accountDetail (query:Query) id =
        let account = query.records.accounts |> List.find (fun account -> account.id = id)
        let query = { query with account = string id }
        let page = ApplicationPage.Account id
        let data = Ledger.Home.readHome { records=query.records; account=query.account; period=query.range; comparison=query.comparison; startDate=query.startDate; endDate=query.endDate }
        let related = query.records.accounts |> List.filter (fun row -> row.id <> id && row.accountType = account.accountType)
        let transactions = data.transactions |> List.sortByDescending (fun row -> row.date, row.id)
        let pages = max 1 ((transactions.Length + 9) / 10)
        let current = min pages query.transactionPage
        let rows = transactions |> List.skip ((current - 1) * 10) |> List.truncate 10
        let viewAll = applicationHref query ApplicationPage.Transactions + querySuffix ["account", string id; "startDate", query.startDate; "endDate", query.endDate]
        let pageHref number =
            applicationHref query page + querySuffix ["range", query.range; "comparison", query.comparison; "startDate", query.startDate; "endDate", query.endDate; "transactionPage", string number] + "#account-transactions"

        fveSectionStack {
            fveSection {
                _label "Details"
                fveSectionActions {
                    fveLink {
                        _href (collectionHref { query with overlayFrom="" } (ApplicationPage.EditAccount id))
                        _ariaLabel "Edit account"; _variant LinkVariant.Outline
                        Layout.icon IconName.PencilSquare
                        "Edit"
                    }
                }
                fveDescriptionList {
                    _columns DescriptionListColumns.Three
                    fveDescriptionListItem { _label "Type"; string account.accountType }
                    fveDescriptionListItem { _label "Commodity"; account.currency }
                    fveDescriptionListItem { _label "Parent account"; Controls.groupLabel account.accountType }
                    fveDescriptionListItem { _label "Source"; "Manual" }
                    fveDescriptionListItem { _label "Month-end observed balance"; "Not required" }
                    fveDescriptionListItem { _label "Balance"; money (balance query.records account) }
                }
            }
            fveSection {
                _label "Balance history"
                _description "Compare actual and projected balances. Zoom the chart to focus the transaction history below."
                Home.balanceHistory page false query
            }
            fveSection {
                _label "Related accounts"
                _description ("Other " + string account.accountType + " accounts in this ledger.")
                fveSectionActions { Layout.secondaryLink (applicationHref query ApplicationPage.Accounts + querySuffix ["accountType", string account.accountType]) "View all" }
                if related.IsEmpty then
                    fveEmptyState { _label "No related accounts"; _description "Other accounts of the same type will appear here." }
                else
                    fveTable {
                        _ariaLabel ("Related " + string account.accountType + " accounts")
                        _mobileLayout MobileLayout.Records
                        fveTableCaption { "Related accounts" }
                        fveTableHeader { fveTableRow { fveTableHead { "Account" }; fveTableHead { "Type" }; fveTableHead { _class "text-right"; "Balance" } } }
                        fveTableBody {
                            for row in related do
                                fveTableRow {
                                    fveTableHead { _scope "row"; _mobile Mobile.Primary; Layout.inlineLink (applicationHref query (ApplicationPage.Account row.id)) row.name }
                                    fveTableCell { _label "Type"; string row.accountType }
                                    fveTableCell { _label "Balance"; _class "text-right"; money (balance query.records row) }
                                }
                        }
                    }
            }
            fveSection {
                _id "account-transactions"; _label "Transactions"
                _description (date data.visibleStart + " – " + date data.visibleEnd)
                fveSectionActions { Layout.secondaryLink viewAll "View all" }
                if transactions.IsEmpty then
                    fveEmptyState { _label "No transactions in this date range"; _description "Choose different dates or reset zoom." }
                else
                    transactionTable query (account.name + " transactions") rows false
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
