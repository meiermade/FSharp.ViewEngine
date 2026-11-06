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
open Overlay

module internal DeleteTransaction =
    let render query id =
        let transaction = findTransaction id

        deletionConfirmation
            query
            (ApplicationPage.DeleteTransaction id)
            transaction.description
            (fveDescriptionList {
                fveDescriptionListItem {
                    _label "Date"
                    date transaction.date
                }

                fveDescriptionListItem {
                    _label "Amount (USD)"
                    money transaction.amount
                }
            })
            true
