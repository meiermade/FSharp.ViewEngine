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

module internal SettingsLedgers =
    let render (query: Query) =
        let table title headings rows =
            RecordTable.render title [ _mobileLayout MobileLayout.Records ] headings rows []

        fveSectionStack {
            _class "w-full"

            let available =
                query.catalog.ledgers
                |> List.filter (fun ledger -> ledger.organizationId = query.workspace.organization.id)

            table
                "Organization ledgers"
                [ fveTableHead { "Name" }; fveTableHead { "Reporting commodity" } ]
                [ for ledger in available do
                      fveTableRow {
                          fveTableHead {
                              _scope "row"
                              _mobile Mobile.Primary

                              Layout.inlineLink
                                  (applicationHref
                                      { query with
                                          workspace = { query.workspace with ledger = ledger }
                                          returnWorkspace = None }
                                      ApplicationPage.Home)
                                  ledger.name
                          }

                          fveTableCell {
                              _label "Reporting commodity"
                              "USD"
                          }
                      } ]
        }
