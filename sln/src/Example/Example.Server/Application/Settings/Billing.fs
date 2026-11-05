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

module internal SettingsBilling =
    let render (query: Query) =
        let table title headings rows =
            RecordTable.render title [ _mobileLayout MobileLayout.Records ] headings rows []

        div {
            _class "grid min-w-0 w-full gap-6"

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
