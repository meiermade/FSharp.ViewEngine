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
open Overlay

module internal DeleteTransaction =
    let render query id =
        let transaction = findTransaction query.records id

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
