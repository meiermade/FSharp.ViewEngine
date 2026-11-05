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

module internal SettingsEnvironments =
    let render (query: Query) =
        let table title headings rows =
            RecordTable.render title [ _mobileLayout MobileLayout.Records ] headings rows []

        div {
            _class "grid min-w-0 w-full gap-6"

            table
                "Organization environments"
                [ fveTableHead { "Name" }; fveTableHead { "Type" }; fveTableHead { "Status" } ]
                [ for environment in environments do
                      fveTableRow {
                          fveTableHead {
                              _scope "row"
                              _mobile Mobile.Primary

                              Layout.inlineLink
                                  (applicationHref query (ApplicationPage.Environment environment.id))
                                  environment.name
                          }

                          fveTableCell {
                              _label "Type"
                              if environment.sandbox then "Sandbox" else "Production"
                          }

                          fveTableCell {
                              _label "Status"

                              fveBadge {
                                  _color Color.Success
                                  "Ready"
                              }
                          }
                      } ]
        }
