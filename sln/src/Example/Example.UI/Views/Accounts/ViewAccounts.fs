namespace Example.UI.Views

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open type Html
open type Datastar
open Model
open ViewData
open Controls
open Collection

module internal ViewAccounts =
    let private accountTable (query: Query) (matches: Account list) =
        let url = applicationHref query
        let records = query.records
        let compareSiblings rows =
            match query.sort with
            | "balance" -> List.sortByDescending _.balance rows
            | "balance-asc" -> List.sortBy _.balance rows
            | "name-desc" -> List.sortByDescending _.name rows
            | _ -> List.sortBy _.name rows

        let matchingIds = matches |> List.map _.id |> Set.ofList
        let rec branch depth ancestors (account: Account) =
            let descendants =
                children records account.id
                |> compareSiblings
                |> List.collect (branch (depth + 1) (ancestors @ [ account.id ]))
            if matchingIds.Contains account.id || not descendants.IsEmpty then
                (account, depth, ancestors) :: descendants
            else []

        let values = records.accounts |> List.filter isRoot |> List.collect (branch 0 [])
        let selectable = matches |> List.filter (isRoot >> not)
        let selected = selectedKeys query "accounts"
        let selection =
            RecordSelection.view (elementId query "template-accounts-selection") "accountIds" selected
                (selectable |> List.map (fun account -> string account.id, account.name))
        let hierarchyId = elementId query "template-account-hierarchy"
        let signal = "_table_" + RecordSelection.token hierarchyId + "_open_"
        let filtered = query.search <> "" || not (filterValues query.accountType).IsEmpty || not (filterValues query.account).IsEmpty
        let openByDefault id =
            filtered || (values |> List.exists (fun (row, _, ancestors) -> List.contains id ancestors && List.contains (string row.id) selected))
        let hasChildren id = values |> List.exists (fun (row, _, _) -> row.parentId = Some id)

        let actions (account: Account) =
            let editor = { query with overlayFrom = "accounts" }
            fveDropdownMenu {
                _id (elementId query ("template-account-actions-" + string account.id))
                _ariaLabel ("Actions for " + account.name)
                fveDropdownMenuTrigger {
                    yield! rowActionAttributes
                    fun (attrs: HtmlAttribute list) ->
                        fveButton {
                            yield! attrs
                            fveIcon { _name IconName.EllipsisHorizontal; _variant IconVariant.Micro }
                        }
                }
                fveDropdownMenuItem {
                    _href (collectionHref { editor with parent = string account.id } ApplicationPage.CreateAccount)
                    "Create child"
                }
                if not (isRoot account) then
                    fveDropdownMenuItem {
                        _href (collectionHref editor (ApplicationPage.EditAccount account.id))
                        "Edit account"
                    }
                    fveDropdownMenuSeparator { () }
                    fveDropdownMenuItem {
                        _href (collectionHref editor (ApplicationPage.DeleteAccount account.id))
                        _color Color.Error
                        "Delete account"
                    }
            }

        let headings =
            [ selection.heading
              collectionSort query ApplicationPage.Accounts "name" "name-desc" "Account" ""
              fveTableHead { "Type" }
              fveTableHead { "Commodity" }
              collectionSort query ApplicationPage.Accounts "balance-asc" "balance" "Balance" "text-right"
              fveTableHead { _class "text-right"; "Total balance (USD)" }
              fveTableHead { _class "text-right"; "Actions" } ]

        let body =
            [ for account, depth, ancestors in values do
                  let key = string account.id
                  let expanded = "$" + signal + key
                  fveTableRow {
                      yield! selection.rowAttributes key
                      if not ancestors.IsEmpty then
                          _dataShow (ancestors |> List.map (fun id -> "$" + signal + string id) |> String.concat " && ")
                      if isRoot account then
                          let keys =
                              subtree records account |> List.filter (fun row -> not (isRoot row) && matchingIds.Contains row.id)
                              |> List.map (fun row -> string row.id)
                          if keys.IsEmpty then fveTableCell { () }
                          else selection.groupCell key account.name keys
                      elif matchingIds.Contains account.id then
                          selection.cell key account.name
                      else
                          fveTableCell { () }

                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary
                          span {
                              _class "inline-flex items-center gap-2"
                              _style ($"padding-inline-start:{depth}rem")
                              if hasChildren account.id then
                                  fveButton {
                                      _id (hierarchyId + "-toggle-" + key)
                                      _type "button"
                                      _ariaLabel ((if openByDefault account.id then "Collapse " else "Expand ") + account.name)
                                      _ariaExpanded (openByDefault account.id)
                                      _dataAttr ("aria-expanded", expanded)
                                      _dataAttr ("aria-label", $"({expanded} ? 'Collapse ' : 'Expand ') + {System.Text.Json.JsonSerializer.Serialize account.name}")
                                      _dataOn ("click", $"{expanded} = !{expanded}")
                                      _variant ButtonVariant.Ghost
                                      _size Size.Small
                                      _padding Padding.Uniform
                                      span {
                                          _ariaHidden true
                                          _class (if openByDefault account.id then "inline-flex rotate-90" else "inline-flex")
                                          _dataClass ("rotate-90", expanded)
                                          fveIcon { _name IconName.ChevronRight; _size Size.Small }
                                      }
                                  }
                              else
                                  span { _ariaHidden true; _class "inline-block w-7 shrink-0" }
                              Layout.inlineLink (url (ApplicationPage.Account account.id)) account.name
                          }
                      }
                      fveTableCell { _label "Type"; string account.accountType }
                      fveTableCell { _label "Commodity"; account.currency }
                      fveTableCell { _label "Balance"; _class "text-right tabular-nums"; money (account.balance) }
                      fveTableCell { _label "Total balance (USD)"; _class "text-right tabular-nums"; money (account.totalBalance) }
                      fveTableCell {
                          _label "Actions"
                          _class "fve-table-actions text-right"
                          actions account
                      }
                  } ]

        let attributes =
            [ yield! selection.attributes
              _mobileLayout MobileLayout.Scroll
              _scrollRows true
              _dataSignals (
                  values
                  |> List.choose (fun (account, _, _) ->
                      if hasChildren account.id then Some(signal + string account.id, openByDefault account.id)
                      else None)
                  |> Map.ofList |> System.Text.Json.JsonSerializer.Serialize) ]
        RecordTable.render "Account hierarchy" attributes headings body [ selection.status ]

    let accountsPage (query: Query) =
        let query =
            if List.contains query.sort [ "name"; "name-desc"; "balance"; "balance-asc" ] then query
            else { query with sort = "name" }
        let scopes = filterValues query.account
        let scopeIds =
            query.records.accounts
            |> List.filter (fun account -> List.contains (string account.id) scopes)
            |> List.collect (subtree query.records)
            |> List.map _.id |> Set.ofList
        let rows =
            query.records.accounts
            |> List.filter (fun account ->
                account.name.Contains(query.search, StringComparison.OrdinalIgnoreCase)
                && filterMatches query.accountType (string account.accountType)
                && (scopes.IsEmpty || scopeIds.Contains account.id))
        let filters =
            [ { name = "accountType"; label = "Type"; value = query.accountType
                options = query.catalog.accountTypes |> List.map (fun kind -> string kind, string kind) }
              { name = "account"; label = "Account"; value = query.account
                options = query.records.accounts |> List.map (fun account -> string account.id, account.name) } ]
        let content =
            if rows.IsEmpty then
                fveEmptyState { _label "No matching accounts"; _description "Try a different search or filter." }
            else accountTable query rows
        collection query "accounts" filters content
