namespace Example.Server.ApplicationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain
open Controls

module internal ViewTransaction =
    let transactionDetail (query: Query) id =
        let transaction = findTransaction id
        let account = findAccount transaction.accountId

        div {
            _class "grid max-w-3xl gap-6"

            fveDescriptionList {
                fveDescriptionListItem {
                    _label "Date"
                    date transaction.date
                }

                fveDescriptionListItem {
                    _label "Account"

                    Layout.inlineLink (applicationHref query (ApplicationPage.Account account.id)) account.name
                }

                fveDescriptionListItem {
                    _label "Amount (USD)"
                    money transaction.amount
                }

                fveDescriptionListItem {
                    _label "Verification"
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
            }
        }
