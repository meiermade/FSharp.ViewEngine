namespace Example.Components.Views.Pages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain
open Controls

module internal SettingsBilling =
    let render (query: Query) =
        let table title headings rows =
            RecordTable.render title [ _mobileLayout MobileLayout.Records ] headings rows []

        fveSectionStack {
            _class "w-full"

            fveDescriptionList {
                fveDescriptionListItem {
                    _label "Subscription"
                    "No subscription"
                }

                fveDescriptionListItem {
                    _label "Payment method"
                    "None"
                }

                fveDescriptionListItem {
                    _label "Invoices"
                    "No invoices"
                }
            }
        }
