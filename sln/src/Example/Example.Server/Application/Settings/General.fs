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

        fveSectionStack {
            _class "w-full"

            Layout.sectionWithEdit
                "Details"
                (applicationHref query ApplicationPage.EditOrganization)
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
