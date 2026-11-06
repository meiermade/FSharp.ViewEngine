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

module internal SettingsUsers =
    let render (query: Query) =
        let table title headings rows =
            RecordTable.render title [ _mobileLayout MobileLayout.Records ] headings rows []

        div {
            _class "grid min-w-0 w-full gap-6"

            table
                "Organization users"
                [ fveTableHead { "Name" }; fveTableHead { "Email" }; fveTableHead { "Role" } ]
                [ fveTableRow {
                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary
                          Layout.inlineLink (applicationHref query ApplicationPage.Profile) "Andrew Meier"
                      }

                      fveTableCell {
                          _label "Email"
                          "andrew@meiermade.com"
                      }

                      fveTableCell {
                          _label "Role"
                          query.workspace.organization.role
                      }
                  } ]
        }
