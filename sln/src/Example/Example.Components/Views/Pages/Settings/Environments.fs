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

module internal SettingsEnvironments =
    let render (query: Query) =
        let table title headings rows =
            RecordTable.render title [ _mobileLayout MobileLayout.Records ] headings rows []

        fveSectionStack {
            _class "w-full"

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
