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

module internal TransactionTable =
    let viewAllLink (query: Query) accountId =
        let href =
            applicationHref query ApplicationPage.Transactions
            + querySuffix [ "account", string accountId; "startDate", query.startDate; "endDate", query.endDate ]
        Layout.inlineLink href "View all"

    let transactionTable (query: Query) caption (rows: Transaction list) (scope: Set<int> option) =
        let selectable = scope.IsNone
        let url = applicationHref query
        let amountLabel = if selectable then "Amount (USD)" else "Change (USD)"
        let accountLink id =
            let account = query.records.accounts |> List.find (fun row -> row.id = id)
            Layout.inlineLink (url (ApplicationPage.Account id)) account.name

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
              fveTableHead { "Status" }
              fveTableHead { "Accounts" }
              fveTableHead {
                  _class "text-right"
                  amountLabel
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
                          _label "Status"
                          statusBadge transaction.status
                      }

                      fveTableCell {
                          _label "Accounts"
                          accountLink transaction.fromAccountId
                          " → "
                          accountLink transaction.toAccountId
                      }

                      fveTableCell {
                          _label amountLabel
                          _class "text-right tabular-nums"
                          money (scope |> Option.map (fun ids -> signedAmount ids transaction) |> Option.defaultValue transaction.amount)
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

                                      yield! rowActionAttributes

                                      fun (triggerAttributes: HtmlAttribute list) ->
                                          fveButton {
                                              for attribute in triggerAttributes do
                                                  attribute

                                              fveIcon { _name IconName.EllipsisHorizontal; _variant IconVariant.Micro }
                                          }

                                  }

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
