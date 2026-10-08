namespace Docs.Examples

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain

/// Financial application pages shared with the Specification.
module Application =
    let navigation =
        [ "/examples/application", "Home"; "/examples/application/accounts", "Accounts"; "/examples/application/transactions", "Transactions"
          "/examples/application/settings", "Settings"; "/examples/application/profile", "Profile"
          yield! settingsSections |> List.map (fun (key,label) -> "/examples/application/settings/"+key,label) ]
    let title = function
        | ApplicationPage.Home -> "Home" | ApplicationPage.Accounts -> "Accounts" | ApplicationPage.CreateAccount -> "Create account"
        | ApplicationPage.Account id -> (findAccount id).name | ApplicationPage.EditAccount id -> "Edit "+(findAccount id).name
        | ApplicationPage.DeleteAccount id -> "Delete "+(findAccount id).name
        | ApplicationPage.Transactions -> "Transactions" | ApplicationPage.Transaction id -> (findTransaction id).description
        | ApplicationPage.DeleteTransaction id -> "Delete "+(findTransaction id).description
        | ApplicationPage.Settings -> "Organizations" | ApplicationPage.SettingsSection key -> settingsSections |> List.find (fst >> (=) key) |> snd
        | ApplicationPage.Profile -> "Profile"
    let pageTitle page query = title (backgroundPage query page)
    let private hidden name value = input { _type "hidden"; _name name; _value value }
    let private contextFields (query:Query) =
        div {
            _class "contents"
            hidden "organization" query.workspace.organization.id
            hidden "environment" query.workspace.environment.id
            hidden "ledger" query.workspace.ledger.id
            if query.overlayFrom<>"" then hidden "from" query.overlayFrom
            if query.embedded then hidden "embedded" "1"
            if query.specification then
                hidden "specState" query.specState
                if query.resource<>"" then hidden "resource" query.resource
                hidden "fveAppDock" query.dock
            if query.appMode then hidden "appMode" "1"
        }
    let private editorContextFields query = fragment {
        contextFields query
        for name,value in collectionQueryPairs query do hidden (if name="accountType" then "filterAccountType" else name) value
    }
    let private submit (label:string) = Button.create (ButtonContent.Text label) |> Button.asSubmit |> Button.withColor ButtonColor.Primary |> Button.withVariant ButtonVariant.Solid |> Button.render
    let private notice query id title (message:string) color = Notice.create (elementId query id) title (p { message }) |> Notice.withColor color |> Notice.render
    let private statusBadge (status:TransactionStatus) =
        Badge.create (if status=TransactionStatus.Verified then "Verified" else "Unverified")
        |> Badge.withColor (if status=TransactionStatus.Verified then BadgeColor.Success else BadgeColor.Warning) |> Badge.render
    let private selectedKeys (query:Query) kind =
        if query.state<>"selected" then []
        elif kind="accounts" then ["101";"102"] else ["201";"203"]
    let private collectionSort query page ascending descending =
        let next = if query.sort=ascending then descending else ascending
        let href = collectionHref {query with sort=next;state=""} page
        if query.sort=ascending then TableSort.ascending href
        elif query.sort=descending then TableSort.descending href
        else TableSort.by href
    let private transactionTable (query:Query) caption (rows:Transaction list) selectable =
        let url = applicationHref query
        let columns : TableColumn<Transaction> list =
            [ TableColumn.create "Date" (fun (transaction:Transaction) -> text (date transaction.date)) |> (if selectable then TableColumn.withSort (collectionSort query ApplicationPage.Transactions "date-asc" "date-desc") else id)
              TableColumn.create "Description" (fun (transaction:Transaction) -> Layout.link (url (ApplicationPage.Transaction transaction.id)) transaction.description) |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary
              TableColumn.create "Verification" (fun transaction -> statusBadge transaction.status)
              TableColumn.create "Account" (fun transaction -> Layout.link (url (ApplicationPage.Account transaction.accountId)) (findAccount transaction.accountId).name)
              TableColumn.create "Amount (USD)" (fun transaction -> text (money transaction.amount)) |> TableColumn.alignEnd
              if selectable then
                  TableColumn.create "Actions" (fun (transaction:Transaction) ->
                      DropdownMenu.create (elementId query ("template-transaction-actions-"+string transaction.id)) ("Actions for "+transaction.description)
                      |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon (Layout.icon "M6.75 12a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Z"))
                      |> DropdownMenu.withContent [
                          DropdownMenuItem.link (url (ApplicationPage.Transaction transaction.id)) "Open transaction"
                          DropdownMenuItem.separator
                          DropdownMenuItem.link (collectionHref {query with overlayFrom="transactions"} (ApplicationPage.DeleteTransaction transaction.id)) "Delete transaction" |> DropdownMenuItem.withColor DropdownMenuItemColor.Error ]
                      |> DropdownMenu.render id) |> TableColumn.alignEnd ]
        let config = Table.create caption columns rows |> Table.withMobileLayout TableMobileLayout.Records
        let config = if selectable then config |> Table.withSelection (TableSelection.create (elementId query "template-transactions-selection") (fun (transaction:Transaction) -> string transaction.id) (fun transaction -> transaction.description) |> TableSelection.withFormName "transactionIds" |> TableSelection.withSelectedKeys (selectedKeys query "transactions")) |> Table.withScrollableRows else config
        config |> Table.render
    let private select query name label selected options =
        Select.create name label id (options |> List.map (fun (value,label) -> SelectOption.create value label)) |> Select.withId (elementId query ("template-"+name)) |> Select.withSelected selected |> Select.render
    let private selectionFeedback (query:Query) =
        div {
            _class "contents"
            if query.state="selection-valid" then notice query "template-selection-valid" "Selected records checked" "The selected rows are available in the example. No financial changes were made." NoticeColor.Success
            if query.state="selection-invalid" then notice query "template-selection-invalid" "Choose records" "Select at least one available record before reviewing the selection." NoticeColor.Error
        }
    // This collection composition belongs to Ledger, not the installed Components library.
    type private CollectionFilter = { name:string; label:string; value:string; options:(string*string) list }
    let private filterQuery name value (query:Query) =
        match name with
        | "accountType" -> {query with accountType=value}
        | "status" -> {query with status=value}
        | "account" -> {query with account=value}
        | _ -> query
    let private collection (query:Query) kind (filters:CollectionFilter list) (rows:HtmlElement) =
        let page = if kind="accounts" then ApplicationPage.Accounts else ApplicationPage.Transactions
        let active = filters |> List.filter (fun filter -> List.contains filter.name query.filters || filter.value<>"all")
        let query = {query with filters=active |> List.map _.name}
        let available = filters |> List.filter (fun filter -> not (List.contains filter.name query.filters))
        let selectionId = elementId query ("template-"+kind+"-selection")
        let formId = elementId query ("template-"+kind+"-batch-form")
        let countSignal = (elementId query ("template-"+kind+"-selection-count")).Replace('-','_')
        let review () = Button.create (ButtonContent.Text "Review selected") |> Button.asSubmit |> Button.withColor ButtonColor.Primary |> Button.withVariant ButtonVariant.Solid |> Button.withAttributes [_attr("form",formId);_name "action";_value "review-selected"] |> Button.render
        let clearFilters = active |> List.fold (fun query filter -> filterQuery filter.name "all" query) {query with filters=[];state=""}
        div {
            _attr("data-ledger-collection",kind)
            _class "group/collection flex min-h-0 flex-1 flex-col"
            _attr("data-signals__ifmissing",$"{{{countSignal}:0}}")
            _dataOn("fve-table-selection-change",$"${countSignal} = evt.detail.keys.length")
            _dataOn("click",$"const link = evt.target.closest('a[href]'); if (link?.closest('[data-ledger-applied-filters]') && evt.button === 0 && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey) document.getElementById('{selectionId}')?.dispatchEvent(new CustomEvent('fve-selection-clear'))")
            div {
                _attr("data-ledger-collection-toolbar","true")
                _class "shrink-0 border-b border-[var(--fve-border)] bg-[var(--fve-background)] pb-4"
                selectionFeedback query
                form {
                    _method "get"; _action (applicationHref query page); _ariaLabel (if kind="accounts" then "Account filters" else "Transaction filters")
                    _class "flex min-w-0 flex-wrap items-center gap-3 group-has-[[data-fve-table]_input[name]:checked]/collection:hidden"
                    _dataShow $"${countSignal} == 0"
                    contextFields {query with specState="default";resource=""}
                    for name,value in collectionQueryPairs query do if name<>"search" then hidden name value
                    div {
                        _class "min-w-0 flex-[1_1_14rem]"
                        Input.create "search" ("Search "+kind) |> Input.withId (elementId query ("template-"+(if kind="accounts" then "account" else "transaction")+"-search")) |> Input.withType InputType.Search |> Input.withValue query.search |> Input.withVisuallyHiddenLabel |> Input.withAttributes [_placeholder ("Search "+kind)] |> Input.render
                    }
                    if available.IsEmpty then Button.create (ButtonContent.Text "Add filter") |> Button.disabled |> Button.render
                    else
                        DropdownMenu.create (elementId query ("template-"+kind+"-add-filter")) "Add filter"
                        |> DropdownMenu.withTrigger (DropdownMenuTrigger.content (span { _class "inline-flex items-center gap-2"; Layout.icon "M12 4.5v15m7.5-7.5h-15"; "Add filter" }))
                        |> DropdownMenu.withContent [for filter in available do DropdownMenuItem.link (collectionHref {query with filters=query.filters@[filter.name];state=""} page) filter.label]
                        |> DropdownMenu.render id
                }
                div {
                    _attr("data-ledger-selection-toolbar","true")
                    _class "flex min-w-0 flex-wrap items-center gap-3"
                    _dataShow $"${countSignal} > 0"
                    _style "display:none"
                    span { _class "text-sm font-medium"; _dataText $"${countSignal} + ' selected'" }
                    review ()
                    Button.create (ButtonContent.Text "Clear selection") |> Button.withVariant ButtonVariant.Outline |> Button.withAttributes [_dataOn("click", $"document.getElementById('{selectionId}').dispatchEvent(new CustomEvent('fve-selection-clear')); requestAnimationFrame(() => el.closest('[data-ledger-collection]').querySelector('input[type=search]').focus())")] |> Button.render
                }
                noscript {
                    div {
                        _class "hidden flex-wrap items-center gap-3 group-has-[[data-fve-table]_input[name]:checked]/collection:flex"
                        span { _class "text-sm font-medium"; "Selected records" }
                        review ()
                        Layout.secondaryLink (collectionHref {query with state=""} page) "Clear selection"
                    }
                }
                if not active.IsEmpty then
                    div {
                        _attr("data-ledger-applied-filters","true")
                        _class "mt-3 flex min-w-0 flex-wrap items-center gap-2"
                        for filter in active do
                            let removeId = elementId query ("template-"+kind+"-remove-"+filter.name)
                            let removed = filterQuery filter.name "all" {query with filters=query.filters |> List.filter ((<>) filter.name);state=""}
                            form {
                                _id removeId; _method "get"; _action (applicationHref query page); _class "hidden"
                                contextFields {removed with specState="default";resource=""}
                                for name,value in collectionQueryPairs removed do hidden name value
                            }
                            form {
                                _method "get"; _action (applicationHref query page); _ariaLabel (filter.label+" filter values")
                                _class "flex min-w-0 max-w-full flex-wrap items-center gap-2"
                                _dataOn("change", "el.requestSubmit()")
                                contextFields {query with state="";specState="default";resource=""}
                                for name,value in collectionQueryPairs query do if name<>filter.name then hidden name value
                                let choice =
                                    Select.create filter.name (filter.label+" filter") id (filter.options |> List.map (fun (value,label) -> SelectOption.create value label))
                                    |> Select.withId (elementId query ("template-"+kind+"-filter-"+filter.name))
                                    |> Select.withSelected filter.value
                                    |> Select.withNativeFallback
                                ButtonGroup.create (filter.label+" filter") [
                                    ButtonGroupItem.label filter.label
                                    ButtonGroupItem.select choice
                                    Button.create (ButtonContent.Icon ("Remove "+filter.label+" filter", Layout.icon "m6 6 12 12M6 18 18 6"))
                                    |> Button.asSubmit
                                    |> Button.withAttributes [_attr("form",removeId)]
                                    |> ButtonGroupItem.button ]
                                |> ButtonGroup.render id
                                noscript { Button.create (ButtonContent.Text ("Apply "+filter.label+" filter")) |> Button.asSubmit |> Button.render }
                            }
                        Layout.link (collectionHref clearFilters page) "Clear all"
                    }
            }
            form {
                _id formId; _method "post"; _action (applicationHref query page); _ariaLabel ("Selected "+kind+" actions"); _class "flex min-h-0 flex-1 flex-col"
                contextFields query
                for name,value in collectionQueryPairs query do hidden name value
                rows
            }
        }

    let private chart (query:Query) =
        let selected = accounts |> List.tryFind (fun account -> string account.id=query.account) |> Option.defaultValue accounts.Head
        let range = match query.range with "30" -> 30 | "7" -> 7 | _ -> 90
        let endDate = DateOnly(2026,9,18)
        let startDate = endDate.AddDays(-range)
        let balanceAt at = selected.openingBalance + (transactions |> List.filter (fun transaction -> transaction.accountId=selected.id && transaction.date<=at) |> List.sumBy _.amount)
        let actual = [ for index in 0..8 -> startDate.AddDays(range*index/8),balanceAt (startDate.AddDays(range*index/8)) ]
        let factor = match query.comparison with "optimistic" -> 1.5M | "pessimistic" -> -0.5M | _ -> 1M
        let scenario = [ for index in 0..8 -> startDate.AddDays(range*index/8),selected.openingBalance + decimal index*120M*factor ]
        let values = (actual@scenario) |> List.map snd
        let lower = (List.min values)-500M
        let upper = (List.max values)+500M
        let point index value = $"{40+index*90},{210-int ((value-lower)/(upper-lower)*170M)}"
        let points values = values |> List.mapi (fun index (_,value) -> point index value) |> String.concat " "
        Card.create (div {
            _class "grid gap-4"
            div { _class "flex flex-wrap items-start justify-between gap-3"; div { p { _class "text-xs text-[var(--fve-muted-text)]"; "Balance · "+selected.name }; p { _class "mt-1 text-2xl font-semibold tabular-nums"; money (balance selected) } }; div { _class "flex gap-4 text-xs text-[var(--fve-muted-text)]"; span { _class "text-[var(--fve-brand-text)]"; "━ Actual" }; span { "┄ "+(match query.comparison with "optimistic" -> "Optimistic" | "pessimistic" -> "Pessimistic" | _ -> "Expected")+" scenario" } } }
            svg {
                _viewBox "0 0 800 240"; _role "img"; _ariaLabel ("Actual and illustrative scenario balances for "+selected.name); _class "h-56 w-full"
                Svg.titleElement { "Actual balances from seeded transactions; dashed values are an authored scenario, not a financial forecast." }
                for y in [40;125;210] do line { _x1 "40"; _x2 "760"; _y1 (string y); _y2 (string y); _stroke "var(--fve-border)" }
                polyline { _points (points scenario); _fill "none"; _stroke "var(--fve-muted-text)"; _strokeWidth "2"; _strokeDasharray "6 5" }
                polyline { _points (points actual); _fill "none"; _stroke "var(--fve-brand-solid)"; _strokeWidth "3" }
            }
            div { _class "flex justify-between text-xs text-[var(--fve-muted-text)]"; span { date startDate }; span { date endDate } }
            p { _class "text-xs text-[var(--fve-muted-text)]"; "Actual balances use seeded opening balances and transactions. The dashed scenario is illustrative, not a prediction." }
        }) |> Card.render
    let private home (query:Query) =
        let selected = accounts |> List.tryFind (fun account -> string account.id=query.account) |> Option.defaultValue accounts.Head
        div {
            _class "grid gap-6"
            form {
                _method "get"; _action (applicationHref query ApplicationPage.Home); _ariaLabel "Home balance controls"; _class "flex flex-wrap items-end gap-3"
                contextFields query
                select query "account" "Account" (string selected.id) (accounts |> List.map (fun account -> string account.id,account.name))
                select query "range" "Period" query.range ["7","Last 7 days";"30","Last 30 days";"90","Last 90 days"]
                select query "comparison" "Scenario" query.comparison ["expected","Expected";"optimistic","Optimistic";"pessimistic","Pessimistic"]
                submit "Apply"
            }
            chart query
            Layout.section "Transactions" (transactionTable query "Transactions for the selected account" (transactions |> List.filter (fun transaction -> transaction.accountId=selected.id) |> List.sortByDescending _.date) false)
        }
    type private AccountRow = Group of AccountType | Record of Account
    let private groupLabel = function AccountType.Asset -> "Assets" | AccountType.Liability -> "Liabilities" | AccountType.Equity -> "Equity" | AccountType.Revenue -> "Revenue" | AccountType.Expense -> "Expenses"
    let private rowKey = function Group kind -> "group-"+string kind | Record account -> string account.id
    let private accountTable (query:Query) (rows:Account list) =
        let url = applicationHref query
        let values = [ for kind in accountTypes do if query.accountType="all" || query.accountType=string kind then yield Group kind; yield! rows |> List.filter (fun account -> account.accountType=kind) |> List.map Record ]
        let hierarchy = TableHierarchy.create (elementId query "template-account-hierarchy") rowKey (function Group kind -> groupLabel kind | Record account -> account.name) (function Group _ -> [] | Record account -> ["group-"+string account.accountType]) (function Group _ -> 0 | Record _ -> 1) (function Group kind -> rows |> List.exists (fun account -> account.accountType=kind) | Record _ -> false) |> TableHierarchy.withExpandedKeys (accountTypes |> List.map (fun kind -> "group-"+string kind))
        let actions (account:Account) =
            DropdownMenu.create (elementId query ("template-account-actions-"+string account.id)) ("Actions for "+account.name)
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon (Layout.icon "M6.75 12a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Z"))
            |> DropdownMenu.withContent [
                DropdownMenuItem.link (url (ApplicationPage.Account account.id)) "Open account"
                DropdownMenuItem.link (collectionHref {query with overlayFrom="accounts"} (ApplicationPage.EditAccount account.id)) "Edit account"
                DropdownMenuItem.separator
                DropdownMenuItem.link (collectionHref {query with overlayFrom="accounts"} (ApplicationPage.DeleteAccount account.id)) "Delete account" |> DropdownMenuItem.withColor DropdownMenuItemColor.Error ]
            |> DropdownMenu.render id
        Table.create "Account hierarchy" [
            TableColumn.create "Name" (function Group kind -> strong { groupLabel kind } | Record account -> a { _href (url (ApplicationPage.Account account.id)); _class "font-medium text-[var(--fve-brand-text)] hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"; account.name }) |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary |> TableColumn.withSort (collectionSort query ApplicationPage.Accounts "name" "name-desc")
            TableColumn.create "Type" (function Group kind -> text (string kind) | Record account -> text (string account.accountType))
            TableColumn.create "Commodity" (fun _ -> text "USD")
            TableColumn.create "Balance" (function Record account -> text (money (balance account)) | Group kind -> text (money (rows |> List.filter (fun account -> account.accountType=kind) |> List.sumBy balance))) |> TableColumn.alignEnd |> TableColumn.withSort (collectionSort query ApplicationPage.Accounts "balance-asc" "balance")
            TableColumn.create "Actions" (function Group _ -> text "" | Record account -> actions account) |> TableColumn.alignEnd ] values
        |> Table.withHierarchy hierarchy
        |> Table.withSelection (TableSelection.create (elementId query "template-accounts-selection") rowKey (function Group kind -> groupLabel kind | Record account -> account.name) |> TableSelection.withFormName "accountIds" |> TableSelection.withSelectedKeys (selectedKeys query "accounts"))
        |> Table.withMobileLayout TableMobileLayout.Scroll |> Table.withScrollableRows |> Table.render
    let private accountsPage (query:Query) =
        let query = if List.contains query.sort ["name";"name-desc";"balance";"balance-asc"] then query else {query with sort="name"}
        let rows = accounts |> List.filter (fun account -> account.name.Contains(query.search,StringComparison.OrdinalIgnoreCase) && (query.accountType="all" || string account.accountType=query.accountType))
        let rows = match query.sort with "balance" -> List.sortByDescending balance rows | "balance-asc" -> List.sortBy balance rows | "name-desc" -> List.sortByDescending _.name rows | _ -> List.sortBy _.name rows
        let filters = [{name="accountType";label="Type";value=query.accountType;options=("all","All types")::(accountTypes |> List.map (fun kind -> string kind,string kind))}]
        let content = if rows.IsEmpty then EmptyState.create "No matching accounts" "Try a different search or account type." |> EmptyState.render else accountTable query rows
        collection query "accounts" filters content
    let private accountDetail (query:Query) id =
        let account = findAccount id
        div {
            _class "grid gap-6"
            DescriptionList.create [DescriptionListItem.text "Type" (string account.accountType);DescriptionListItem.text "Commodity" "USD";DescriptionListItem.text "Parent account" (groupLabel account.accountType);DescriptionListItem.text "Source" "Created in the example";DescriptionListItem.text "Month-end observed balance" "Not required";DescriptionListItem.text "Balance" (money (balance account))] |> DescriptionList.withColumns DescriptionListColumns.Three |> DescriptionList.render
            chart {query with account=string id}
            Layout.section "Transactions" (transactionTable query (account.name+" transactions") (transactions |> List.filter (fun transaction -> transaction.accountId=id)) false)
        }
    let private cancelLink query page id =
        Button.create (ButtonContent.Text "Cancel") |> Button.withAttributes [_id id]
        |> Button.renderLink (collectionHref {query with overlayFrom="";state="";accountDraft=None} (backgroundPage query page))
    let private accountForm (query:Query) existing suffix =
        let account = existing |> Option.map findAccount
        let page = existing |> Option.map ApplicationPage.EditAccount |> Option.defaultValue ApplicationPage.CreateAccount
        let invalid = query.state="invalid"
        let seededName = account |> Option.map _.name |> Option.defaultValue ""
        let kind = account |> Option.map (fun item -> string item.accountType) |> Option.defaultValue "Asset"
        let draft = query.accountDraft
        let name = Input.create "name" "Name" |> Input.withId (elementId query ("template-account-name"+suffix)) |> Input.withValue (draft |> Option.map _.name |> Option.defaultValue seededName) |> Input.required |> Input.withAttributes [_maxlength "80"]
        let name = if invalid then name |> Input.withValidation "Use a unique name between 1 and 80 characters." else name
        let editorSelect name label selected options validation =
            let options = if options |> List.exists (fst >> (=) selected) then options else (selected,"Unavailable value")::options
            Select.create name label id (options |> List.map (fun (value,label) -> SelectOption.create value label))
            |> Select.withId (elementId query ("template-editor-"+name+suffix)) |> Select.withSelected selected |> Select.withNativeFallback
            |> (match validation with Some message -> Select.withValidation message | None -> id) |> Select.render
        div {
            _class "grid gap-5"
            if query.state="validated" then notice query ("template-account-valid"+suffix) "Account validated" "Example data unchanged." NoticeColor.Success
            if query.state="invalid-type" then notice query ("template-account-invalid"+suffix) "Check account type" "Choose an available account type." NoticeColor.Error
            if query.state="invalid-details" then notice query ("template-account-details-invalid"+suffix) "Check account details" "The parent account must match the account type." NoticeColor.Error
            form {
                _id (elementId query ("template-account-form"+suffix))
                _method "post"; _action (collectionHref query page); _ariaLabel (if existing.IsSome then "Edit account" else "Create account")
                _class "grid gap-5 [--fve-control-min-height:2.5rem] [--fve-control-font-size:1rem]"
                editorContextFields query
                Input.render name
                editorSelect "parentType" "Parent account" (draft |> Option.map _.parentType |> Option.defaultValue kind) (accountTypes |> List.map (fun kind -> string kind,groupLabel kind)) (if query.state="invalid-details" then Some "Choose the root matching the account type." else None)
                editorSelect "accountType" "Account type" (draft |> Option.map _.accountType |> Option.defaultValue kind) (accountTypes |> List.map (fun kind -> string kind,string kind)) (if query.state="invalid-type" then Some "Choose an available account type." else None)
                hidden "subtype" "Generic"; hidden "currency" "USD"; hidden "observedBalance" "not-required"
                DescriptionList.create [DescriptionListItem.text "Subtype" "Generic";DescriptionListItem.text "Commodity" "USD";DescriptionListItem.text "Month-end observed balance" "Not required"] |> DescriptionList.render
            }
        }
    let private accountFormActions query existing suffix =
        let page = existing |> Option.map ApplicationPage.EditAccount |> Option.defaultValue ApplicationPage.CreateAccount
        fragment {
            cancelLink query page (elementId query ("template-account-editor-cancel"+suffix))
            Button.create (ButtonContent.Text (if existing.IsSome then "Update" else "Save")) |> Button.asSubmit
            |> Button.withAttributes [_attr("form",elementId query ("template-account-form"+suffix))]
            |> Button.withColor ButtonColor.Primary |> Button.withVariant ButtonVariant.Solid |> Button.render
        }
    let private transactionsPage (query:Query) =
        let query = if List.contains query.sort ["date-asc";"date-desc"] then query else {query with sort="date-desc"}
        let rows = transactions |> List.filter (fun transaction -> transaction.description.Contains(query.search,StringComparison.OrdinalIgnoreCase) && (query.status="all" || (query.status="verified" && transaction.status=TransactionStatus.Verified) || (query.status="unverified" && transaction.status=TransactionStatus.NeedsReview)) && (query.account="all" || string transaction.accountId=query.account)) |> (if query.sort="date-asc" then List.sortBy _.date else List.sortByDescending _.date)
        let filters = [
            {name="status";label="Verification";value=query.status;options=["all","All statuses";"verified","Verified";"unverified","Unverified"]}
            {name="account";label="Account";value=query.account;options=("all","All accounts")::(accounts |> List.map (fun account -> string account.id,account.name))} ]
        let content = if rows.IsEmpty then EmptyState.create "No matching transactions" "Adjust the search, account, or verification filter." |> EmptyState.render else transactionTable query "Transactions" rows true
        collection query "transactions" filters content
    let private transactionDetail (query:Query) id =
        let transaction = findTransaction id
        let account = findAccount transaction.accountId
        div {
            _class "grid max-w-3xl gap-6"
            DescriptionList.create [DescriptionListItem.text "Date" (date transaction.date);DescriptionListItem.create "Account" (Layout.link (applicationHref query (ApplicationPage.Account account.id)) account.name);DescriptionListItem.text "Amount (USD)" (money transaction.amount);DescriptionListItem.create "Verification" (statusBadge transaction.status);DescriptionListItem.text "Source" "Seeded example";DescriptionListItem.text "Comment" "No supporting comment in this record"] |> DescriptionList.withColumns DescriptionListColumns.Three |> DescriptionList.render
            Layout.link (applicationHref query ApplicationPage.Transactions) "Back to transactions"
        }
    let private settings (query:Query) key =
        let url = applicationHref query
        let table caption columns rows = Table.create caption columns rows |> Table.withMobileLayout TableMobileLayout.Records |> Table.render
        div {
            _class "grid max-w-4xl gap-6"
            match key with
            | "environments" ->
                table "Organization environments" [TableColumn.create "Name" (fun (environment:Environment) -> Layout.link (applicationHref {query with workspace={query.workspace with environment=environment}} ApplicationPage.Home) environment.name) |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary;TableColumn.create "Type" (fun environment -> text (if environment.sandbox then "Sandbox" else "Production"));TableColumn.create "Status" (fun _ -> Badge.create "Ready" |> Badge.withColor BadgeColor.Success |> Badge.render)] environments
                p { _class "text-sm text-[var(--fve-muted-text)]"; "Choose an environment to open its server-rendered application context. All environments use the same resettable fixtures." }
            | "users" ->
                table "Organization users" [TableColumn.create "Name" (fun (name,_,_) -> Layout.link (url ApplicationPage.Profile) name) |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary;TableColumn.create "Email" (fun (_,email,_) -> text email);TableColumn.create "Role" (fun (_,_,role) -> text role)] ["Andrew Meier","andrew@meiermade.com",query.workspace.organization.role]
            | "api-clients" ->
                EmptyState.create "No API clients" "This resettable example does not issue secrets or configure integrations." |> EmptyState.render
                Layout.link "/examples/api-documentation" "Read the API documentation"
            | "ledgers" ->
                let available = ledgers |> List.filter (fun ledger -> ledger.organizationId=query.workspace.organization.id)
                table "Organization ledgers" [TableColumn.create "Name" (fun (ledger:Ledger) -> Layout.link (applicationHref {query with workspace={query.workspace with ledger=ledger}} ApplicationPage.Home) ledger.name) |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary;TableColumn.create "Reporting commodity" (fun _ -> text "USD");TableColumn.create "Selected" (fun ledger -> text (if ledger.id=query.workspace.ledger.id then "Current ledger" else "Available"))] available
            | "billing" ->
                DescriptionList.create [DescriptionListItem.text "Plan" "Example workspace";DescriptionListItem.text "Billing" "Not connected";DescriptionListItem.text "Payment method" "None";DescriptionListItem.text "Invoices" "No invoices"] |> DescriptionList.render
                notice query "template-billing" "No live billing" "No subscription, checkout, payment method, or billing provider is connected to this template." NoticeColor.Info
            | _ ->
                if query.state="validated" then notice query "template-settings-valid" "Organization values validated" "The form passed validation. No organization settings were changed or retained." NoticeColor.Success
                if query.state="invalid" then notice query "template-settings-invalid" "Check organization values" "Enter a name between 1 and 80 characters and USD as the reporting commodity." NoticeColor.Error
                table "Organizations" [TableColumn.create "Name" (fun (organization:Organization) -> Layout.link (applicationHref {query with workspace=workspaceFromStrings organization.id "production" ""} ApplicationPage.Settings) organization.name) |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary;TableColumn.create "Role" (fun organization -> text organization.role);TableColumn.create "Status" (fun organization -> text (if organization.id=query.workspace.organization.id then "Current organization" else "Available"))] organizations
                Layout.section "Organization details" (form {
                    _method "post"; _action (url ApplicationPage.Settings); _ariaLabel "Organization settings"; _class "grid max-w-xl gap-4"
                    contextFields query
                    Input.create "workspace" "Name" |> Input.withId (elementId query "template-workspace-name") |> Input.withValue query.workspace.organization.name |> Input.withAttributes [_required true;_maxlength "80"] |> Input.render
                    DescriptionList.create [DescriptionListItem.text "Environment" query.workspace.environment.name;DescriptionListItem.text "Current ledger" query.workspace.ledger.name;DescriptionListItem.text "Access" query.workspace.organization.role] |> DescriptionList.render
                    select query "currency" "Reporting commodity" "USD" ["USD","US dollar (USD)"]
                    submit "Validate settings"
                })
        }
    let private profile (query:Query) =
        let appearanceId = elementId query "template-appearance"
        let appearanceSignal = appearanceId.Replace('-', '_')+"_value"
        let applyAppearance = "window.fveColorMode.set(mode)"
        let activateAppearance = $"const input = event.target; if (input instanceof HTMLInputElement && input.type == 'radio' && input.checked) {{ const mode = input.value; {applyAppearance} }}"
        div {
            _class "grid max-w-4xl gap-8"
            if query.state="validated" then notice query "template-profile-valid" "Profile values validated" "No identity values were saved or retained." NoticeColor.Success
            if query.state="invalid" then notice query "template-profile-invalid" "Check profile values" "Enter a name between 1 and 80 characters, a valid email and an available time zone." NoticeColor.Error
            Layout.section "Identity" (div {
                _class "grid gap-5 sm:grid-cols-[auto_1fr]"
                Avatar.create "Andrew Meier" "AM" |> Avatar.render
                form {
                    _method "post"; _action (applicationHref query ApplicationPage.Profile); _ariaLabel "Profile values"; _class "grid max-w-xl gap-4"
                    contextFields query
                    Input.create "name" "Name" |> Input.withId (elementId query "template-profile-name") |> Input.withValue "Andrew Meier" |> Input.withAttributes [_required true;_maxlength "80"] |> Input.render
                    Input.create "email" "Email" |> Input.withId (elementId query "template-profile-email") |> Input.withType InputType.Email |> Input.withValue "andrew@meiermade.com" |> Input.withAttributes [_required true;_maxlength "254"] |> Input.render
                    select query "timezone" "Time zone" "America/Chicago" ["America/Chicago","America/Chicago";"America/New_York","America/New_York";"UTC","UTC"]
                    submit "Validate profile"
                }
            })
            Layout.section "Authentication" (DescriptionList.create [DescriptionListItem.text "Session" "Authored example identity";DescriptionListItem.text "Provider" "Not connected";DescriptionListItem.text "Account management" "No live authentication or password changes"] |> DescriptionList.render)
            Layout.section "Organization access" (Table.create "Profile organization access" [TableColumn.create "Organization" (fun (organization:Organization) -> Layout.link (applicationHref {query with workspace=workspaceFromStrings organization.id "production" ""} ApplicationPage.Settings) organization.name) |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary;TableColumn.create "Role" (fun organization -> text organization.role)] organizations |> Table.withMobileLayout TableMobileLayout.Records |> Table.render)
            Layout.section "Preferences" (div {
                _class "grid max-w-xl gap-4"
                div {
                    _dataInit $"queueMicrotask(() => {{ ${appearanceSignal} = window.fveColorMode.current() }})"
                    _dataOn("fve-color-mode__window", $"${appearanceSignal} = window.fveColorMode.current()")
                    _onclick activateAppearance
                    // Chromium does not click an already-checked radio when Space is pressed.
                    _onkeydown $"if (event.key == ' ') {{ {activateAppearance} }}"
                    RadioGroup.create (elementId query "appearance") "Appearance" id [RadioGroupOption.create "system" "System";RadioGroupOption.create "light" "Light";RadioGroupOption.create "dark" "Dark"]
                    |> RadioGroup.withId appearanceId |> RadioGroup.withSelected "system" |> RadioGroup.render
                }
                p { _class "text-xs text-[var(--fve-muted-text)]"; "Appearance changes this local browser view. Profile submission only validates the authored form." }
            })
        }
    let private accountEditor (query:Query) existing (reviewControls:HtmlElement) =
        let page = existing |> Option.map ApplicationPage.EditAccount |> Option.defaultValue ApplicationPage.CreateAccount
        // Hidden specification tabs use a bounded inline form, never a document-wide modal.
        if query.previewId<>"" then
            section {
                _ariaLabel (title page); _class "absolute inset-y-0 right-0 z-30 flex w-full max-w-lg flex-col border-l border-[var(--fve-border)] bg-[var(--fve-surface)]"
                h2 { _class "shrink-0 border-b border-[var(--fve-border)] p-5 text-lg font-semibold"; title page }
                div { _class "min-h-0 flex-1 overflow-y-auto p-5"; accountForm query existing "" }
                div { _class "flex shrink-0 justify-end gap-3 border-t border-[var(--fve-border)] p-5"; accountFormActions query existing "" }
            }
        else
            let seeded = existing |> Option.map findAccount
            let kind = seeded |> Option.map (fun account -> string account.accountType) |> Option.defaultValue "Asset"
            let initial = System.Text.Json.JsonSerializer.Serialize [|[|"name";seeded |> Option.map _.name |> Option.defaultValue ""|];[|"parentType";kind|];[|"accountType";kind|]|]
            let dirty = "const form = el.querySelector('form'); const dirty = form && JSON.stringify([...new FormData(form)].filter(([name]) => ['name','parentType','accountType'].includes(name))) !== el.dataset.initial;"
            let dismiss = dirty+" if (!dirty || window.confirm('Discard unsaved changes?')) { el.dataset.discarding = 'true'; el.close() }"
            Drawer.create "template-account-editor" (if existing.IsSome then "Edit account" else "Create account") (accountForm query existing "")
            |> Drawer.withInitialFocus "template-account-name"
            |> Drawer.withFooter (fragment { accountFormActions query existing ""; reviewControls })
            |> Drawer.withAttributes [
                _style ("width:min(32rem,100vw);max-width:100vw" + (if query.specification && query.appMode then (if query.dock="top" then ";padding-top:var(--example-app-dock-space,3.5rem)" else ";padding-bottom:var(--example-app-dock-space,3.5rem)") else ""))
                if query.specification && query.appMode then _dataAttr ("style", "'width:min(32rem,100vw);max-width:100vw;' + ($_example_app_dock_top ? 'padding-top:var(--example-app-dock-space,3.5rem)' : 'padding-bottom:var(--example-app-dock-space,3.5rem)')")
                _dataInit ($"el.dataset.initial = JSON.stringify({initial}); queueMicrotask(() => {{ if (el.isConnected && !el.open) {{ el.showModal(); document.getElementById('template-account-name')?.focus() }} }})")
                _dataOn ("click", ["capture"], "if (evt.target === el || evt.target.closest('#template-account-editor-close, #template-account-editor-cancel')) { evt.preventDefault(); evt.stopImmediatePropagation(); "+dismiss+" }")
                _dataOn ("submit", "el.dataset.submitting = 'true'")
                _dataOn ("cancel", "evt.preventDefault(); "+dismiss)
                _dataOn ("close", ["capture"], "window.location.assign(document.getElementById('template-account-editor-cancel').href)")
                _dataOn ("beforeunload__window", dirty+" if (dirty && !el.dataset.submitting && !el.dataset.discarding) { evt.preventDefault(); evt.returnValue = '' }") ]
            |> Drawer.render
    let private deletionConfirmation query page label (details:HtmlElement) eligible (reviewControls:HtmlElement) =
        let overlayId = elementId query "template-delete-record"
        let formId = overlayId+"-form"
        let cancelId = overlayId+"-cancel"
        let body = fragment {
            if query.state="deleted" then yield notice query "template-delete-valid" "Deletion validated" "Example data unchanged." NoticeColor.Success
            if not eligible || query.state="delete-blocked" then yield notice query "template-delete-blocked" "Account cannot be deleted" "It has a balance or recorded transactions." NoticeColor.Error
            yield details
            yield form { _id formId; _method "post"; _action (collectionHref query page); editorContextFields query; hidden "action" "delete" }
        }
        let actions = fragment {
            cancelLink query page cancelId
            if eligible then
                Button.create (ButtonContent.Text "Delete") |> Button.asSubmit |> Button.withAttributes [_attr("form",formId)]
                |> Button.withColor ButtonColor.Error |> Button.withVariant ButtonVariant.Solid |> Button.render
        }
        if query.previewId<>"" then Layout.section ("Delete "+label+"?") (fragment { body; div { _class "mt-5 flex justify-end gap-3"; actions } })
        else
            Dialog.create overlayId ("Delete "+label+"?") body
            |> Dialog.withFooter (fragment { actions; reviewControls }) |> Dialog.withInitialFocus cancelId |> Dialog.dismissOnBackdrop
            |> Dialog.withAttributes [
                _dataOn ("close", ["capture"], $"document.getElementById('{cancelId}').click()")
                _dataInit ($"queueMicrotask(() => {{ if (el.isConnected && !el.open) {{ el.showModal(); document.getElementById('{cancelId}')?.focus() }} }})") ]
            |> Dialog.render
    let private recordActions query editPage deletePage =
        div {
            _class "flex min-w-0 max-w-full items-center gap-2"
            match editPage with Some page -> Layout.secondaryLink (collectionHref query page) "Edit account" | None -> ()
            DropdownMenu.create (elementId query "template-record-actions") "More actions"
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon (Layout.icon "M6.75 12a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Z") |> DropdownMenuTrigger.withVariant ButtonVariant.Outline)
            |> DropdownMenu.withContent [DropdownMenuItem.link (collectionHref query deletePage) "Delete" |> DropdownMenuItem.withColor DropdownMenuItemColor.Error]
            |> DropdownMenu.render id
        }
    let renderWithReviewControls (reviewControls:HtmlElement) page (query:Query) =
        let query = if page=ApplicationPage.Transactions && query.sort="name" then {query with sort="date-desc"} else query
        let url = applicationHref query
        let background = backgroundPage query page
        let content =
            match background with
            | ApplicationPage.Home -> home query | ApplicationPage.Accounts -> accountsPage query
            | ApplicationPage.Account id -> accountDetail query id
            | ApplicationPage.Transactions -> transactionsPage query | ApplicationPage.Transaction id -> transactionDetail query id
            | ApplicationPage.Settings -> settings query "organizations" | ApplicationPage.SettingsSection key -> settings query key | ApplicationPage.Profile -> profile query
            | _ -> text ""
        let actions =
            match background with
            | ApplicationPage.Accounts -> Layout.primaryLink (collectionHref {query with overlayFrom="accounts"} ApplicationPage.CreateAccount) "Create account"
            | ApplicationPage.Account id -> recordActions {query with overlayFrom=""} (Some (ApplicationPage.EditAccount id)) (ApplicationPage.DeleteAccount id)
            | ApplicationPage.Transaction id -> recordActions {query with overlayFrom=""} None (ApplicationPage.DeleteTransaction id)
            | _ -> text ""
        let overlay =
            match page with
            | ApplicationPage.CreateAccount -> accountEditor query None reviewControls
            | ApplicationPage.EditAccount id -> accountEditor query (Some id) reviewControls
            | ApplicationPage.DeleteAccount id -> deletionConfirmation query page (findAccount id).name (text "") (canDeleteAccount id) reviewControls
            | ApplicationPage.DeleteTransaction id ->
                let transaction = findTransaction id
                deletionConfirmation query page transaction.description (DescriptionList.create [DescriptionListItem.text "Date" (date transaction.date);DescriptionListItem.text "Amount (USD)" (money transaction.amount)] |> DescriptionList.render) true reviewControls
            | _ -> text ""
        fragment {
            Layout.applicationShell background (url background) query (title background) actions (fragment {
                if query.previewId<>"" && background<>page then
                    div { _class "relative flex min-h-[28rem] min-w-0 flex-1 flex-col"; content; overlay }
                else content
                if query.previewId="" && background<>page then noscript {
                    match page with
                    | ApplicationPage.CreateAccount | ApplicationPage.EditAccount _ ->
                        let existing = match page with ApplicationPage.EditAccount id -> Some id | _ -> None
                        section {
                            _ariaLabel (title page); _class "fixed inset-y-0 right-0 z-50 flex w-full max-w-lg flex-col border-l border-[var(--fve-border)] bg-[var(--fve-surface)]"
                            h2 { _class "shrink-0 border-b border-[var(--fve-border)] p-5 text-lg font-semibold"; title page }
                            div { _class "min-h-0 flex-1 overflow-y-auto p-5"; accountForm query existing "-native" }
                            div { _class "flex shrink-0 justify-end gap-3 border-t border-[var(--fve-border)] p-5"; accountFormActions query existing "-native" }
                        }
                    | _ -> deletionConfirmation {query with previewId="native"} page (match page with ApplicationPage.DeleteAccount id -> (findAccount id).name | ApplicationPage.DeleteTransaction id -> (findTransaction id).description | _ -> "record") (text "") (match page with ApplicationPage.DeleteAccount id -> canDeleteAccount id | _ -> true) (text "")
                }
            })
            if query.previewId="" then div { _class ((Layout.theme |> ComponentsTheme.withDensity Density.Compact |> ComponentsTheme.withControlSize ControlSize.Small |> ComponentsTheme.className)+" contents"); overlay }
            if background=page || query.previewId<>"" then reviewControls
        }
    let render page query = renderWithReviewControls (text "") page query
