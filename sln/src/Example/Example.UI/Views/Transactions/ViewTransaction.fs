namespace Example.UI.Views

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open type Html
open type Svg
open type Datastar
open Model
open ViewData
open Controls

module internal ViewTransaction =
    let transactionDetail (query: Query) id =
        let transaction = findTransaction query.records id
        let accountLink accountId =
            let account = findAccount query.records accountId
            Layout.inlineLink (applicationHref query (ApplicationPage.Account account.id)) account.name

        fveSectionStack {
            Layout.section "Details" (fveDescriptionList {
                fveDescriptionListItem {
                    _label "Date"
                    date transaction.date
                }

                fveDescriptionListItem {
                    _label "From account"
                    accountLink transaction.fromAccountId
                }

                fveDescriptionListItem {
                    _label "To account"
                    accountLink transaction.toAccountId
                }

                fveDescriptionListItem {
                    _label "Amount (USD)"
                    money transaction.amount
                }

                fveDescriptionListItem {
                    _label "Status"
                    statusBadge transaction.status
                }

                fveDescriptionListItem {
                    _label "Source"
                    "Manual"
                }

                fveDescriptionListItem {
                    _label "Comment"
                    "No supporting comment in this record"
                }

                _columns DescriptionListColumns.Three
            })
        }
