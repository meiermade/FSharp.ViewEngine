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
open Collection

module internal ViewAccounts =
    type AccountRow =
        | Group of AccountType
        | Record of Account

    let rowKey =
        function
        | Group kind -> "group-" + string kind
        | Record account -> string account.id

    let accountTable (query: Query) (rows: Account list) =
        let url = applicationHref query

        let values =
            [ for kind in accountTypes do
                  if filterMatches query.accountType (string kind) then
                      yield Group kind

                      yield!
                          rows
                          |> List.filter (fun account -> account.accountType = kind)
                          |> List.map Record ]

        let rowLabel =
            function
            | Group kind -> groupLabel kind
            | Record account -> account.name

        let selection =
            RecordSelection.view
                (elementId query "template-accounts-selection")
                "accountIds"
                (selectedKeys query "accounts")
                (rows |> List.map (fun account -> string account.id, account.name))

        let hierarchyId = elementId query "template-account-hierarchy"
        let hierarchySignal = "_table_" + RecordSelection.token hierarchyId + "_expanded"
        let expanded = "$" + hierarchySignal

        let json value =
            System.Text.Json.JsonSerializer.Serialize value

        let actions (account: Account) =
            fveDropdownMenu {
                _id (elementId query ("template-account-actions-" + string account.id))
                _ariaLabel ("Actions for " + account.name)

                fveDropdownMenuTrigger {

                    _class rowActionClass

                    fun (triggerAttributes: HtmlAttribute list) ->
                        button {
                            for attribute in triggerAttributes do
                                attribute

                            fveIcon { _name IconName.EllipsisHorizontal; _variant IconVariant.Micro }
                        }

                }

                fveDropdownMenuItem {
                    _href (url (ApplicationPage.Account account.id))
                    "Open account"
                }

                fveDropdownMenuItem {
                    _href (
                        collectionHref { query with overlayFrom = "accounts" } (ApplicationPage.EditAccount account.id)
                    )

                    "Edit account"
                }

                fveDropdownMenuSeparator { () }

                fveDropdownMenuItem {
                    _href (
                        collectionHref
                            { query with overlayFrom = "accounts" }
                            (ApplicationPage.DeleteAccount account.id)
                    )

                    "Delete account"
                    _color Color.Error
                }
            }

        let headings =
            [ selection.heading
              collectionSort query ApplicationPage.Accounts "name" "name-desc" "Name" ""
              fveTableHead { "Type" }
              fveTableHead { "Commodity" }
              collectionSort query ApplicationPage.Accounts "balance-asc" "balance" "Balance" "text-right"
              fveTableHead {
                  _class "text-right"
                  "Actions"
              } ]

        let body =
            [ for row in values do
                  let key = rowKey row

                  fveTableRow {
                      yield! selection.rowAttributes key

                      match row with
                      | Record account ->
                          _dataShow (expanded + ".includes(" + json ("group-" + string account.accountType) + ")")
                      | _ -> ()

                      match row with
                      | Record _ -> selection.cell key (rowLabel row)
                      | Group kind ->
                          selection.groupCell key (rowLabel row)
                              (rows |> List.filter (fun account -> account.accountType = kind) |> List.map (fun account -> string account.id))

                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary

                          span {
                              _class "inline-flex items-center gap-2"

                              _style (
                                  match row with
                                  | Group _ -> "padding-inline-start:0rem"
                                  | Record _ -> "padding-inline-start:1rem"
                              )

                              match row with
                              | Group kind when rows |> List.exists (fun account -> account.accountType = kind) ->
                                  button {
                                      _id (hierarchyId + "-toggle-" + RecordSelection.token key)
                                      _type "button"
                                      _ariaLabel ("Collapse " + groupLabel kind)
                                      _ariaExpanded true
                                      _dataAttr ("aria-expanded", expanded + ".includes(" + json key + ")")

                                      _dataAttr (
                                          "aria-label",
                                          "("
                                          + expanded
                                          + ".includes("
                                          + json key
                                          + ") ? 'Collapse ' : 'Expand ') + "
                                          + json (groupLabel kind)
                                      )

                                      _dataOn (
                                          "click",
                                          expanded
                                          + " = "
                                          + expanded
                                          + ".includes("
                                          + json key
                                          + ") ? "
                                          + expanded
                                          + ".filter(key => key !== "
                                          + json key
                                          + ") : [..."
                                          + expanded
                                          + ", "
                                          + json key
                                          + "]"
                                      )

                                      _class
                                          "inline-flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                                      span {
                                          _ariaHidden true
                                          _dataText (expanded + ".includes(" + json key + ") ? '▾' : '▸'")
                                          "▾"
                                      }
                                  }
                              | _ ->
                                  span {
                                      _ariaHidden true
                                      _class "inline-block size-8 shrink-0"
                                  }

                              match row with
                              | Group kind -> strong { groupLabel kind }
                              | Record account ->
                                  Layout.inlineLink (url (ApplicationPage.Account account.id)) account.name
                          }
                      }

                      fveTableCell {
                          _label "Type"

                          match row with
                          | Group kind -> string kind
                          | Record account -> string account.accountType
                      }

                      fveTableCell {
                          _label "Commodity"
                          "USD"
                      }

                      fveTableCell {
                          _label "Balance"
                          _class "text-right"

                          match row with
                          | Record account -> money (balance query.records account)
                          | Group kind ->
                              money (
                                  rows
                                  |> List.filter (fun account -> account.accountType = kind)
                                  |> List.sumBy (balance query.records)
                              )
                      }

                      fveTableCell {
                          _label "Actions"
                          _class "fve-table-actions text-right"

                          match row with
                          | Group _ -> ()
                          | Record account -> actions account
                      }
                  } ]

        let attributes =
            [ yield! selection.attributes
              _mobileLayout MobileLayout.Scroll
              _scrollRows true
              _dataSignals (
                  "{"
                  + hierarchySignal
                  + ":"
                  + json (accountTypes |> List.map (fun kind -> "group-" + string kind))
                  + "}"
              ) ]

        RecordTable.render "Account hierarchy" attributes headings body [ selection.status ]

    let accountsPage (query: Query) =
        let query =
            if List.contains query.sort [ "name"; "name-desc"; "balance"; "balance-asc" ] then
                query
            else
                { query with sort = "name" }

        let rows =
            query.records.accounts
            |> List.filter (fun account ->
                account.name.Contains(query.search, StringComparison.OrdinalIgnoreCase)
                && filterMatches query.accountType (string account.accountType))

        let rows =
            match query.sort with
            | "balance" -> List.sortByDescending (balance query.records) rows
            | "balance-asc" -> List.sortBy (balance query.records) rows
            | "name-desc" -> List.sortByDescending _.name rows
            | _ -> List.sortBy _.name rows

        let filters =
            [ { name = "accountType"
                label = "Type"
                value = query.accountType
                options = accountTypes |> List.map (fun kind -> string kind, string kind) } ]

        let content =
            if rows.IsEmpty then
                fveEmptyState {
                    _label "No matching accounts"
                    _description "Try a different search or account type."
                }
            else
                accountTable query rows

        collection query "accounts" filters content
