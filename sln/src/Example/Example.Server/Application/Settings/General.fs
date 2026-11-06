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

module internal SettingsGeneral =
    let render (query: Query) =
        let table title headings rows =
            RecordTable.render title [ _mobileLayout MobileLayout.Records ] headings rows []

        div {
            _class "grid min-w-0 w-full gap-6"

            Layout.section
                "Details"
                (fveDescriptionList {
                    fveDescriptionListItem {
                        _label "Name"
                        query.workspace.organization.name
                    }

                    fveDescriptionListItem {
                        _label "Reporting commodity"
                        "US dollar (USD)"
                    }

                    fveDescriptionListItem {
                        _label "Your role"
                        query.workspace.organization.role
                    }

                    _columns DescriptionListColumns.Three
                })
        }
