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

module internal TransactionTable =
    let transactionTable (query: Query) caption (rows: Transaction list) selectable =
        let url = applicationHref query

        let selection =
            if selectable then
                Some(
                    RecordSelection.view
                        (elementId query "template-transactions-selection")
                        "transactionIds"
                        (selectedKeys query "transactions")
                        (rows
                         |> List.map (fun transaction -> string transaction.id, transaction.description))
                )
            else
                None

        let headings =
            [ match selection with
              | Some selection -> selection.heading
              | None -> ()
              if selectable then
                  collectionSort query ApplicationPage.Transactions "date-asc" "date-desc" "Date" ""
              else
                  fveTableHead { "Date" }
              fveTableHead { "Description" }
              fveTableHead { "Verification" }
              fveTableHead { "Account" }
              fveTableHead {
                  _class "text-right"
                  "Amount (USD)"
              }
              if selectable then
                  fveTableHead {
                      _class "text-right"
                      "Actions"
                  } ]

        let body =
            [ for transaction in rows do
                  fveTableRow {
                      match selection with
                      | Some selection -> yield! selection.rowAttributes (string transaction.id)
                      | None -> ()

                      match selection with
                      | Some selection -> selection.cell (string transaction.id) transaction.description
                      | None -> ()

                      fveTableCell {
                          _label "Date"
                          date transaction.date
                      }

                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary
                          Layout.inlineLink (url (ApplicationPage.Transaction transaction.id)) transaction.description
                      }

                      fveTableCell {
                          _label "Verification"
                          statusBadge transaction.status
                      }

                      fveTableCell {
                          _label "Account"

                          Layout.inlineLink
                              (url (ApplicationPage.Account transaction.accountId))
                              (findAccount transaction.accountId).name
                      }

                      fveTableCell {
                          _label "Amount (USD)"
                          _class "text-right"
                          money transaction.amount
                      }

                      if selectable then
                          fveTableCell {
                              _label "Actions"
                              _mobile Mobile.Actions
                              _class "fve-table-actions text-right"

                              fveDropdownMenu {
                                  _id (elementId query ("template-transaction-actions-" + string transaction.id))
                                  _ariaLabel ("Actions for " + transaction.description)

                                  fveDropdownMenuTrigger {

                                      _class rowActionClass

                                      fun (triggerAttributes: HtmlAttribute list) ->
                                          button {
                                              for attribute in triggerAttributes do
                                                  attribute

                                              Layout.icon
                                                  "M6.75 12a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Z"
                                          }

                                  }

                                  fveDropdownMenuItem {
                                      _href (url (ApplicationPage.Transaction transaction.id))
                                      "Open transaction"
                                  }

                                  fveDropdownMenuSeparator { () }

                                  fveDropdownMenuItem {
                                      _href (
                                          collectionHref
                                              { query with
                                                  overlayFrom = "transactions" }
                                              (ApplicationPage.DeleteTransaction transaction.id)
                                      )

                                      _color Color.Error
                                      "Delete transaction"
                                  }
                              }
                          }
                  } ]

        let attributes =
            [ _mobileLayout MobileLayout.Records
              _scrollRows selectable
              match selection with
              | Some selection -> yield! selection.attributes
              | None -> () ]

        RecordTable.render
            caption
            attributes
            headings
            body
            (selection
             |> Option.map (fun selection -> [ selection.status ])
             |> Option.defaultValue [])
