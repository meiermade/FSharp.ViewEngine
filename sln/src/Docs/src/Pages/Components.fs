namespace Docs.Pages

open System
open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open FSharp.ViewEngine.Components.Application
open FSharp.ViewEngine.Components.Documentation
open type Html
open type Datastar

module Components =
    type AccountStatus =
        | Active
        | Pending
        | Suspended
        | Scheduled

    type Destination =
        | Accounts
        | AccountsPage of int
        | Account of int
        | Settings
        | DropdownMenuGuide

    type ShellDestination =
        | LedgerHome
        | LedgerAccounts
        | LedgerAccount of int
        | LedgerTransaction of int
        | LedgerCreateAccount
        | LedgerReports
        | LedgerSettings
        | TreasuryHome
        | TreasuryPeriod of string
        | TreasuryTransactions
        | TreasuryPayees
        | TreasuryAccounts

    type AccountRow =
        { id:int
          name:string
          accountType:string
          commodity:string
          balance:decimal
          totalBalance:decimal }

    type TransactionStatus =
        | Verified
        | Unverified
        | Committed

    type TransactionRow =
        { id:int
          date:string
          description:string
          status:TransactionStatus
          accounts:string
          amount:decimal }

    let private statusValue = function
        | Active -> "active"
        | Pending -> "pending"
        | Suspended -> "suspended"
        | Scheduled -> "scheduled"

    let private destinationUrl = function
        | Accounts -> "https://ledger.example.test/accounts"
        | AccountsPage page -> $"https://ledger.example.test/accounts?page={page}"
        | Account id -> $"https://ledger.example.test/accounts/{id}"
        | Settings -> "https://ledger.example.test/settings"
        | DropdownMenuGuide -> "/components/dropdown-menu#components-dropdown-menu"

    let private shellDestinationKey = function
        | LedgerHome -> "ledger-home"
        | LedgerAccounts -> "ledger-accounts"
        | LedgerAccount id -> $"ledger-account-{id}"
        | LedgerTransaction id -> $"ledger-transaction-{id}"
        | LedgerCreateAccount -> "ledger-create-account"
        | LedgerReports -> "ledger-reports"
        | LedgerSettings -> "ledger-settings"
        | TreasuryHome -> "treasury-home"
        | TreasuryPeriod region -> $"treasury-period-{region.ToLowerInvariant()}"
        | TreasuryTransactions -> "treasury-transactions"
        | TreasuryPayees -> "treasury-payees"
        | TreasuryAccounts -> "treasury-accounts"

    let tryShellDestination = function
        | "ledger-home" -> Some LedgerHome
        | "ledger-accounts" -> Some LedgerAccounts
        | value when value.StartsWith("ledger-account-", StringComparison.Ordinal) ->
            match Int32.TryParse(value["ledger-account-".Length..]) with
            | true, id -> Some(LedgerAccount id)
            | false, _ -> None
        | value when value.StartsWith("ledger-transaction-", StringComparison.Ordinal) ->
            match Int32.TryParse(value["ledger-transaction-".Length..]) with
            | true, id when List.contains id [ 201; 202; 203; 204 ] -> Some(LedgerTransaction id)
            | _ -> None
        | "ledger-create-account" -> Some LedgerCreateAccount
        | "ledger-reports" -> Some LedgerReports
        | "ledger-settings" -> Some LedgerSettings
        | "treasury-home" -> Some TreasuryHome
        | value when value.StartsWith("treasury-period-", StringComparison.Ordinal) -> Some(TreasuryPeriod value["treasury-period-".Length..])
        | "treasury-transactions" -> Some TreasuryTransactions
        | "treasury-payees" -> Some TreasuryPayees
        | "treasury-accounts" -> Some TreasuryAccounts
        | _ -> None

    let shellDestinationUrl destination =
        $"/components/app-shell?destination={shellDestinationKey destination}"

    let private shellDestinationLink =
        "evt.target.closest('a[href^=\"/components/app-shell?destination=\"]')"

    let private shellDocumentNavigationAttributes =
        let link = shellDestinationLink
        [ _dataOn ("click", $"if ({link}) window.fsharpDocsNavigation.navigate(evt, {link}.getAttribute('href'))") ]

    let private shellFixtureNavigationAttributes =
        let link = shellDestinationLink
        [ _dataOn ("click", $"if ({link} && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) {{ evt.preventDefault(); window.history.pushState(null, '', {link}.getAttribute('href')); @get({link}.getAttribute('href').replace('/components/app-shell?', '/components/app-shell/fixture?')) }}") ]

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.Components.fs")

    let private themedPreview (content:HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"
            content
        }

    let private themedSurface (content:HtmlElement) =
        div {
            for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
            div {
                _class "bg-[var(--fve-page)] p-6 text-[var(--fve-text)]"
                content
            }
        }
        |> themedPreview

    let private fullBleedThemedSurface (content:HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"
            div {
                for attribute in ComponentsTheme.attributes (ComponentsTheme.sky |> ComponentsTheme.withDensity Density.Compact) do attribute
                content
            }
        }

    let pendingSyncButton =
        Button.create "Sync accounts"
        |> Button.withVariant ButtonVariant.Primary
        |> Button.pending
        |> Button.render

    let disabledDeleteButton =
        Button.create "Delete account"
        |> Button.withVariant ButtonVariant.Destructive
        |> Button.disabled
        |> Button.render

    let private plusIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path d="M10 4.25a.75.75 0 0 1 .75.75v4.25H15a.75.75 0 0 1 0 1.5h-4.25V15a.75.75 0 0 1-1.5 0v-4.25H5a.75.75 0 0 1 0-1.5h4.25V5a.75.75 0 0 1 .75-.75Z"/></svg>"""

    let private refreshIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M15.312 4.683A7.25 7.25 0 1 0 17.25 10a.75.75 0 0 0-1.5 0 5.75 5.75 0 1 1-1.604-3.982H12.5a.75.75 0 0 0 0 1.5h3.5a.75.75 0 0 0 .75-.75v-3.5a.75.75 0 0 0-1.5 0v1.415Z" clip-rule="evenodd"/></svg>"""

    let private removeIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M8.75 3.5a1.25 1.25 0 0 1 2.5 0V4h3a.75.75 0 0 1 0 1.5h-.44l-.55 9.08A2.25 2.25 0 0 1 11.02 16.7H8.98a2.25 2.25 0 0 1-2.24-2.12L6.19 5.5h-.44a.75.75 0 0 1 0-1.5h3v-.5Zm-1.06 2 .54 8.99a.75.75 0 0 0 .75.71h2.04a.75.75 0 0 0 .75-.71l.54-8.99H7.69Z" clip-rule="evenodd"/></svg>"""

    let addAccountIconButton =
        IconButton.create "Add account" plusIcon
        |> IconButton.withVariant ButtonVariant.Primary
        |> IconButton.render

    let refreshAccountsIconButton =
        IconButton.create "Refresh accounts" refreshIcon
        |> IconButton.render

    let refreshingIconButton =
        IconButton.create "Refreshing accounts" refreshIcon
        |> IconButton.withVariant ButtonVariant.Ghost
        |> IconButton.pending
        |> IconButton.render

    let disabledRemoveIconButton =
        IconButton.create "Remove account" removeIcon
        |> IconButton.withVariant ButtonVariant.Destructive
        |> IconButton.disabled
        |> IconButton.render

    let badgeExample =
        div {
            _class "flex flex-wrap items-center gap-3"
            [ Badge.create "Internal" |> Badge.render
              Badge.create "New" |> Badge.withTone Tone.Brand |> Badge.render
              Badge.create "Reconciled" |> Badge.withTone Tone.Positive |> Badge.render ]
        }
    let reviewStatus =
        Status.create "Needs review"
        |> Status.withTone Tone.Warning
        |> Status.render

    let statusExample =
        div {
            _class "flex flex-wrap items-center gap-3"
            [ Status.positive "Active"; reviewStatus ]
        }
    let smallLoadingIndicator =
        LoadingIndicator.create "Loading account balances"
        |> LoadingIndicator.withSize ControlSize.Small
        |> LoadingIndicator.render
    let visibleLoadingIndicator =
        LoadingIndicator.create "Refreshing transactions"
        |> LoadingIndicator.withSize ControlSize.Large
        |> LoadingIndicator.withVisibleLabel
        |> LoadingIndicator.render
    let emptyStateExample =
        EmptyState.create "No accounts yet" "Create an account to start tracking balances and entries."
        |> EmptyState.withIcon plusIcon
        |> EmptyState.withActions (
            ActionCluster.create "empty-account-actions" [
                ApplicationAction.link "/components/app-shell?destination=ledger-create-account" "Create account"
                |> ApplicationAction.withVariant ButtonVariant.Primary ]
            |> ActionCluster.render id)
        |> EmptyState.render
    let private rows =
        [ { id = 101; name = "Assets"; accountType = "Asset"; commodity = "USD"; balance = 0M; totalBalance = 184230.19M }
          { id = 102; name = "Liabilities"; accountType = "Liability"; commodity = "USD"; balance = 0M; totalBalance = -42118.44M }
          { id = 103; name = "Equity"; accountType = "Equity"; commodity = "USD"; balance = -93136.75M; totalBalance = -101336.75M }
          { id = 104; name = "Revenue"; accountType = "Revenue"; commodity = "USD"; balance = -138425M; totalBalance = -138425M }
          { id = 105; name = "Expenses"; accountType = "Expense"; commodity = "USD"; balance = 97650M; totalBalance = 97650M }
          { id = 106; name = "Placeholder"; accountType = "Placeholder"; commodity = "USD"; balance = 0M; totalBalance = 0M } ]

    let private operatingAccount : AccountRow =
        { id = 2048; name = "Operating checking"; accountType = "Asset"; commodity = "USD"; balance = 38442.11M; totalBalance = 38442.11M }

    let private money amount =
        if amount < 0M then $"−${-amount:N2}" else $"${amount:N2}"

    let private recordActionFeedback kind (content:HtmlElement) =
        div {
            _dataSignals $"{{_{kind}MenuFeedback: ''}}"
            content
            p {
                _role "status"
                _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                _dataShow $"$_{kind}MenuFeedback != ''"
                _dataText $"$_{kind}MenuFeedback"
                _style "display:none"
            }
        }

    let private recordMenuItems kind id destination resolve payload =
        let json value = System.Text.Json.JsonSerializer.Serialize(value)
        let feedback = $"$_{kind}MenuFeedback"
        let copy label expression =
            let failure = json $"Could not copy {label}. Check clipboard permissions."
            let success = json $"Copied {label}."
            MenuItem.action
                $"{feedback} = ''; if (navigator.clipboard?.writeText) {{ navigator.clipboard.writeText({expression}).then(() => {{ {feedback} = {success} }}).catch(() => {{ {feedback} = {failure} }}) }} else {{ {feedback} = {failure} }}"
                $"Copy {label}"
        let url = resolve destination |> json
        let download =
            $"const url = URL.createObjectURL(new Blob([{json payload}], {{type: 'application/json'}})); const link = document.createElement('a'); link.href = url; link.download = '{kind}-{id}.json'; document.body.append(link); link.click(); link.remove(); URL.revokeObjectURL(url); {feedback} = 'Downloaded {kind}.'"
        [ MenuItem.link destination $"View {kind}"
          copy $"{kind} ID" (json (string id))
          copy $"{kind} link" $"new URL({url}, window.location.href).href"
          MenuItem.separator
          MenuItem.action download $"Download {kind}" ]

    let private accountTableConfig destinationFor resolve rowAttributes =
        Table.create "Accounts" [
            Table.column "Account" (fun (row:AccountRow) ->
                a { _href (resolve (destinationFor row.id)); _class "font-medium text-[var(--fve-brand-text)]"; row.name })
            |> Table.asRowHeader
            |> Table.asMobilePrimary
            Table.column "Type" (fun (row:AccountRow) -> text row.accountType)
            Table.column "Commodity" (fun (row:AccountRow) -> text row.commodity)
            Table.column "Balance" (fun (row:AccountRow) -> text (money row.balance))
            |> Table.alignEnd
            Table.column "Total balance (USD) · Current" (fun (row:AccountRow) -> text (money row.totalBalance))
            |> Table.alignEnd
            Table.rowActionsColumn (fun (row:AccountRow) ->
                RowActions.create $"account-{row.id}-actions" row.name
                    (recordMenuItems "account" row.id (destinationFor row.id) resolve (System.Text.Json.JsonSerializer.Serialize row))
                |> RowActions.render resolve)
        ] rows
        |> Table.withDensity Density.Compact
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.withRowAttributes rowAttributes
        |> Table.withSelection (
            TableSelection.create "accounts-selection" (fun (row:AccountRow) -> string row.id) (fun row -> row.name)
            |> TableSelection.withDisabledRows (fun row -> row.accountType = "Placeholder")
            |> TableSelection.withFormName "accountIds")

    let accountTable = accountTableConfig LedgerAccount shellDestinationUrl (fun _ -> []) |> Table.render |> recordActionFeedback "account"

    let private filteredAccountTable =
        accountTableConfig LedgerAccount shellDestinationUrl (fun row ->
            let searchable = System.Text.Json.JsonSerializer.Serialize($"{row.name} {row.accountType} {row.commodity}".ToLowerInvariant())
            let accountType = System.Text.Json.JsonSerializer.Serialize(row.accountType.ToLowerInvariant())
            [ _dataShow $"(!$collectionquery.trim() || {searchable}.includes($collectionquery.trim().toLowerCase())) && ($collectiontype == 'all' || $collectiontype == {accountType})" ])
        |> Table.render
        |> recordActionFeedback "account"

    let private transactionRows =
        [ { id = 201; date = "Jul 28"; description = "Northwind payment"; status = Verified; accounts = "Accounts receivable → Operating checking"; amount = 4800M }
          { id = 202; date = "Jul 27"; description = "Cloud hosting"; status = Verified; accounts = "Operating checking → Software expense"; amount = 386.42M }
          { id = 203; date = "Jul 26"; description = "ACH withdrawal"; status = Unverified; accounts = "Operating checking → Placeholder"; amount = 1240M }
          { id = 204; date = "Jun 24"; description = "Office supplies"; status = Committed; accounts = "Business card → Office expense"; amount = 128.19M } ]

    let private transactionTableFor destinationFor resolve =
        Table.create "Transactions" [
            Table.column "Date" (fun row -> text row.date)
            Table.column "Description" (fun row ->
                a { _href (resolve (destinationFor row.id)); _class "font-medium text-[var(--fve-brand-text)]"; row.description })
            |> Table.asRowHeader
            |> Table.asMobilePrimary
            Table.column "Status" (fun row ->
                match row.status with
                | Verified -> Status.positive "Verified"
                | Unverified -> Status.warning "Unverified"
                | Committed -> Status.create "Committed" |> Status.withTone Tone.Neutral |> Status.render)
            Table.column "Accounts" (fun row -> text row.accounts)
            Table.column "Amount" (fun row -> text (money row.amount))
            |> Table.alignEnd
            Table.rowActionsColumn (fun row ->
                let payload = System.Text.Json.JsonSerializer.Serialize {| id = row.id; date = row.date; description = row.description; status = string row.status; accounts = row.accounts; amount = row.amount |}
                RowActions.create $"transaction-{row.id}-actions" row.description
                    (recordMenuItems "transaction" row.id (destinationFor row.id) resolve payload)
                |> RowActions.render resolve)
        ] transactionRows
        |> Table.withDensity Density.Compact
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.withSelection (TableSelection.create "transactions-selection" (fun row -> string row.id) (fun row -> row.description))
        |> Table.render
        |> recordActionFeedback "transaction"

    let private transactionTable = transactionTableFor LedgerTransaction shellDestinationUrl

    type TeamMember = { id:int; name:string; email:string; role:string }

    let teamMembers =
        [ { id = 1; name = "Alex Morgan"; email = "alex@example.com"; role = "Owner" }; { id = 2; name = "Jamie Lee"; email = "jamie@example.com"; role = "Member" }; { id = 3; name = "Riley Chen"; email = "riley@example.com"; role = "Member" }; { id = 4; name = "Sam Rivera"; email = "sam@example.com"; role = "Guest" } ]

    let teamColumns =
        [ Table.column "Name" (fun (person:TeamMember) -> text person.name) |> Table.asRowHeader
          Table.column "Email" (fun person -> text person.email)
          Table.column "Role" (fun person -> text person.role) ]

    let simpleTeamTable =
        Table.create "Team members" teamColumns teamMembers
        |> Table.render

    let comfortableTeamTable =
        Table.create "Team members with comfortable rows" teamColumns teamMembers
        |> Table.withDensity Density.Comfortable
        |> Table.render

    let memberStatusTable =
        Table.create "Team member status" [
            Table.column "Name" (fun (name, _, _) -> text name) |> Table.asRowHeader
            Table.column "Email" (fun (_, email, _) -> text email)
            Table.column "Status" (fun (_, _, status) -> status)
        ] [
            "Alex Morgan", "alex@example.com", Status.positive "Active"
            "Jamie Lee", "jamie@example.com", Status.positive "Active"
            "Riley Chen", "riley@example.com", Status.positive "Active"
            "Sam Rivera", "sam@example.com", Status.warning "Invited"
        ]
        |> Table.render

    let selectableTeamTable =
        Table.create "Selectable team members" teamColumns teamMembers
        |> Table.withSelection (
            TableSelection.create "team-member-selection" (fun person -> string person.id) (fun person -> person.name)
            |> TableSelection.withDisabledRows (fun person -> person.role = "Guest")
            |> TableSelection.withFormName "memberIds")
        |> Table.render

    let mobileTeamTable =
        Table.create "Team members as mobile records" [
            Table.column "Name" (fun (person:TeamMember) -> text person.name)
            |> Table.asRowHeader
            |> Table.asMobilePrimary
            Table.column "Email" (fun person -> text person.email)
            Table.column "Role" (fun person -> text person.role)
        ] teamMembers
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.render

    let emptyTeamTable =
        Table.create "Empty team members" teamColumns []
        |> Table.withEmptyState (
            EmptyState.create "No team members" "Team members will appear here when added."
            |> EmptyState.render)
        |> Table.render

    type TeamMemberSort = NameAscending | NameDescending | RoleAscending | RoleDescending

    let teamMemberSortFromQuery sort direction =
        match sort, direction with
        | "name", "desc" -> NameDescending
        | "role", "asc" -> RoleAscending
        | "role", "desc" -> RoleDescending
        | _ -> NameAscending

    let private teamMemberSortUrl sort direction =
        $"/components/table/sort?sort={sort}&direction={direction}"

    let private documentSort sort =
        sort
        |> TableSort.withAttributes [ _dataOn ("click", "evt.preventDefault(); @get(evt.currentTarget.getAttribute('href'))") ]

    let private sortFor column current =
        match column, current with
        | "name", NameAscending -> TableSort.ascending (teamMemberSortUrl "name" "desc") |> documentSort
        | "name", NameDescending -> TableSort.descending (teamMemberSortUrl "name" "asc") |> documentSort
        | "name", _ -> TableSort.by (teamMemberSortUrl "name" "asc") |> documentSort
        | "role", RoleAscending -> TableSort.ascending (teamMemberSortUrl "role" "desc") |> documentSort
        | "role", RoleDescending -> TableSort.descending (teamMemberSortUrl "role" "asc") |> documentSort
        | "role", _ -> TableSort.by (teamMemberSortUrl "role" "asc") |> documentSort
        | _ -> invalidArg (nameof column) "Unsupported team member sort column."

    let sortableTeamTable current =
        let members =
            match current with
            | NameAscending -> teamMembers |> List.sortBy _.name
            | NameDescending -> teamMembers |> List.sortByDescending _.name
            | RoleAscending -> teamMembers |> List.sortBy _.role
            | RoleDescending -> teamMembers |> List.sortByDescending _.role
        Table.create "Sortable team members" [
            Table.column "Name" (fun (person:TeamMember) -> text person.name)
            |> Table.asRowHeader
            |> Table.asMobilePrimary
            |> Table.withSort (sortFor "name" current)
            Table.column "Email" (fun person -> text person.email)
            Table.column "Role" (fun person -> text person.role)
            |> Table.withSort (sortFor "role" current)
        ] members
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.render

    let sortableTeamTablePreview current =
        div {
            _id "components-table-sorting-preview"
            sortableTeamTable current
        }

    let accountDetails =
        DescriptionList.create [
            DetailField.text "Type" "Asset"
            DetailField.text "Commodity" "USD"
            DetailField.text "Parent account" "Current assets"
            DetailField.text "Source" "Imported statement"
            DetailField.status "Status" (Status.positive "Active")
            DetailField.text "Balance" "$42,800"
        ]
        |> DescriptionList.withColumns DescriptionListColumns.Three
        |> DescriptionList.render

    let accountOverview =
        DescriptionList.create [
            DetailField.status "Status" (Status.positive "Active")
            DetailField.status "Reconciliation" (Status.positive "Up to date")
            DetailField.text "Account number" "1040"
            DetailField.text "Currency" "USD"
            DetailField.text "Account name" "Operating checking"
            DetailField.text "Institution" "Example Bank"
            DetailField.text "Statement date" "August 31, 2026"
            DetailField.text "Balance" "$42,800"
        ]
        |> DescriptionList.withColumns DescriptionListColumns.Four
        |> DescriptionList.render

    let accountDetailsWithDescriptions =
        DescriptionList.create [
            DetailField.text "Available balance" "$42,800"
            |> DetailField.withDescription "Includes cleared entries through today."
            DetailField.text "Statement reference" "statement-2026-08-31-operating-checking-1040"
            |> DetailField.withDescription "Imported from the August bank statement."
        ]
        |> DescriptionList.withColumns DescriptionListColumns.Two
        |> DescriptionList.render

    let availableBalanceMetric =
        Metric.text "Available balance" "$42,800"
        |> Metric.withTrend "Up 8% from last month"
        |> Metric.withDescription "Operating and reserve accounts"
        |> Metric.withStatus (Badge.create "Current" |> Badge.withTone Tone.Positive |> Badge.render)
        |> Metric.render

    let pendingEntriesMetric =
        Metric.text "Pending entries" "14"
        |> Metric.withDescription "Require review before posting"
        |> Metric.withStatus (Badge.create "Needs review" |> Badge.withTone Tone.Warning |> Badge.render)
        |> Metric.render

    type PaginationDestination = PaginationPage of int

    let paginationDestinationUrl (PaginationPage page) =
        $"/components/pagination/page?page={page}"

    let paginationPreview requestedPage =
        let currentPage = Math.Clamp(requestedPage, 1, 8)
        let pageItem page =
            if page = currentPage then PaginationItem.current page
            else PaginationItem.link page (PaginationPage page)
        let middlePages = [ max 2 (currentPage - 1) .. min 7 (currentPage + 1) ]
        let items = [
            pageItem 1
            if List.head middlePages > 2 then PaginationItem.gap
            for page in middlePages do pageItem page
            if List.last middlePages < 7 then PaginationItem.gap
            pageItem 8
        ]
        let firstResult = (currentPage - 1) * 25 + 1
        let lastResult = min 184 (currentPage * 25)

        Pagination.create "Accounts pages" items
        |> (if currentPage > 1 then Pagination.withPrevious (PaginationPage(currentPage - 1)) else id)
        |> (if currentPage < 8 then Pagination.withNext (PaginationPage(currentPage + 1)) else id)
        |> Pagination.withSummary (span { $"Showing {firstResult}–{lastResult} of 184 accounts" })
        |> Pagination.render paginationDestinationUrl

    let paginationPreviewRegion requestedPage =
        div {
            _id "components-pagination-region"
            _dataOn ("click", "const link = evt.target.closest('a[href^=\"/components/pagination/page?\"]'); if (link && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) { evt.preventDefault(); @get(link.getAttribute('href')) }")
            paginationPreview requestedPage
        }

    let private statusOptions =
        [ Select.option Active "Active"
          Select.option Pending "Pending"
          Select.option Suspended "Suspended"
          Select.option Scheduled "Scheduled" ]

    let private selectStatusOptions =
        [ Select.option Active "Active"
          Select.option Pending "Pending"
          Select.option Suspended "Suspended" |> Select.disable
          Select.option Scheduled "Scheduled" ]

    let private choiceSubmitButton id (label:string) =
        Button.create label
        |> Button.withVariant ButtonVariant.Primary
        |> Button.asSubmit
        |> Button.withAttributes [ _id id ]
        |> Button.render

    let private choiceResult id result =
        output {
            _id id
            _role "status"
            _class "block min-h-5 text-sm text-[var(--fve-muted-text)]"
            defaultArg result "Submit this ordinary form to see the server-owned result."
        }

    type ContactDetails = { name:string; email:string; notes:string }
    type ContactFormLayout = Stacked | TwoColumns | Sectioned

    let contactFormRegion layout (details:ContactDetails) (errors:Map<string,string>) accepted =
        let key, title =
            match layout with
            | Stacked -> "", "Contact details"
            | TwoColumns -> "grid", "Contact details in two columns"
            | Sectioned -> "sectioned", "Contact details in sections"
        let prefix = if key = "" then "contact-" else "contact-" + key + "-"
        let endpoint = "/components/forms/contact" + (if key = "" then "" else "/" + key)
        let name =
            Input.create "contactName" "Contact name"
            |> Input.withId (prefix + "name")
            |> Input.withValue details.name
            |> Input.required
            |> Input.withAttributes [ _autocomplete "name" ]
            |> (match Map.tryFind "contactName" errors with Some error -> Input.withValidation error | None -> id)
        let email =
            Input.create "email" "Email address"
            |> Input.withId (prefix + "email")
            |> Input.withType InputType.Email
            |> Input.withValue details.email
            |> Input.required
            |> Input.withDescription "Use the address where you receive account correspondence."
            |> Input.withAttributes [ _autocomplete "email" ]
            |> (match Map.tryFind "email" errors with Some error -> Input.withValidation error | None -> id)
        let identityFields =
            div {
                _class (if layout = Stacked then "grid gap-4" else "grid gap-4 sm:grid-cols-2")
                Input.render name
                Input.render email
            }
        let notes =
            Textarea.create "notes" "Notes"
            |> Textarea.withId (prefix + "notes")
            |> Textarea.withValue details.notes
            |> Textarea.withDescription "Any additional information we should know."
            |> Textarea.render
        div {
            _id ("components-" + prefix + "region")
            _class ("mx-auto grid w-full min-w-0 gap-6 p-6 sm:p-8 " + if layout = Stacked then "max-w-xl" else "max-w-4xl")
            if not (Map.isEmpty errors) then
                ErrorSummary.create (prefix + "errors") "Check your contact details" [
                    match Map.tryFind "contactName" errors with
                    | Some message -> FieldError.create (Input.id name) "Contact name" message
                    | None -> ()
                    match Map.tryFind "email" errors with
                    | Some message -> FieldError.create (Input.id email) "Email address" message
                    | None -> () ]
                |> ErrorSummary.focusOnMount
                |> ErrorSummary.render
            if accepted then
                Notice.create (prefix + "result") "Details are valid" (p { "Validation succeeded. This example does not save your data." })
                |> Notice.withTone Tone.Positive
                |> Notice.withAnnouncement NoticeAnnouncement.Polite
                |> Notice.render
            form {
                _ariaLabel title
                _class "grid gap-6"
                _attr ("novalidate", "")
                _dataOn ("submit", $"@post('{endpoint}', {{contentType: 'form'}})")
                if layout = Sectioned then
                    Section.create (SectionHeader.create "Contact information" |> SectionHeader.withDescription "Who should we contact?" |> SectionHeader.withDivider) identityFields
                    |> Section.render (fun (_:unit) -> "")
                    Section.create (SectionHeader.create "Additional details" |> SectionHeader.withDivider) notes
                    |> Section.render (fun (_:unit) -> "")
                else
                    identityFields
                    notes
                div {
                    _class "flex justify-end"
                    choiceSubmitButton (prefix + "submit") "Validate details"
                }
            }
        }

    let private emptyContact = { name = ""; email = ""; notes = "" }
    let contactFormExample = contactFormRegion Stacked emptyContact Map.empty false
    let twoColumnFormExample = contactFormRegion TwoColumns emptyContact Map.empty false
    let sectionedFormExample = contactFormRegion Sectioned emptyContact Map.empty false

    let labelledInput =
        Input.create "exampleEmail" "Email"
        |> Input.withType InputType.Email
        |> Input.withAttributes [ _placeholder "you@example.com"; _autocomplete "email" ]
        |> Input.render
    let inputWithHelp =
        Input.create "helpEmail" "Email"
        |> Input.withType InputType.Email
        |> Input.withDescription "We will use this address for account updates."
        |> Input.withAttributes [ _placeholder "you@example.com"; _autocomplete "email" ]
        |> Input.render
    let requiredInput =
        Input.create "requiredEmail" "Email"
        |> Input.withType InputType.Email
        |> Input.required
        |> Input.render
    let optionalInput =
        Input.create "company" "Company (optional)"
        |> Input.withAttributes [ _autocomplete "organization" ]
        |> Input.render
    let invalidInput =
        Input.create "invalidEmail" "Email"
        |> Input.withType InputType.Email
        |> Input.withValue "not-an-email"
        |> Input.withValidation "Enter a valid email address."
        |> Input.render
    let emailIcon =
        raw """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="size-5"><path stroke-linecap="round" stroke-linejoin="round" d="M21.75 6.75v10.5A2.25 2.25 0 0 1 19.5 19.5h-15a2.25 2.25 0 0 1-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25m19.5 0-8.578 5.498a2.25 2.25 0 0 1-2.344 0L2.25 6.75"/></svg>"""
    let inputWithIcon =
        Input.create "iconEmail" "Email"
        |> Input.withType InputType.Email
        |> Input.withLeadingIcon emailIcon
        |> Input.withAttributes [ _placeholder "you@example.com" ]
        |> Input.render
    let inputWithPrefix =
        Input.create "website" "Website"
        |> Input.withPrefix "https://"
        |> Input.withAttributes [ _placeholder "example.com"; _autocomplete "off" ]
        |> Input.render
    let inputWithSuffix =
        Input.create "price" "Price"
        |> Input.withSuffix "USD"
        |> Input.withAttributes [ _inputmode "decimal"; _placeholder "0.00" ]
        |> Input.render
    let readonlyInput =
        Input.create "memberId" "Member ID"
        |> Input.withValue "MEM-2048"
        |> Input.readOnly
        |> Input.render
    let disabledInput =
        Input.create "disabledEmail" "Email"
        |> Input.withValue "alex@example.com"
        |> Input.disabled
        |> Input.render
    let pendingInput =
        Input.create "pendingEmail" "Email"
        |> Input.withValue "alex@example.com"
        |> Input.pending
        |> Input.render
    let searchInputExample =
        Input.create "query" "Search"
        |> Input.withId "field-search"
        |> Input.withType InputType.Search
        |> Input.withAttributes [ _placeholder "Search…"; _autocomplete "off" ]
        |> Input.render
    let labelledTextarea =
        Textarea.create "message" "Message"
        |> Textarea.withAttributes [ _placeholder "Write your message…" ]
        |> Textarea.render
    let invalidTextarea =
        Textarea.create "invalidMessage" "Message"
        |> Textarea.required
        |> Textarea.withValidation "Enter a message before continuing."
        |> Textarea.render
    let editableInstructions =
        Textarea.create "instructions" "Payment instructions"
        |> Textarea.withId "instructions-editable"
        |> Textarea.withValue "Include the invoice number with your payment."
        |> Textarea.withDescription "Maximum 400 characters."
        |> Textarea.withAttributes [ _maxlength 400 ]
        |> Textarea.render
    let acceptedNotes =
        Textarea.create "acceptedNotes" "Accepted notes"
        |> Textarea.withValue "Approved for the current period."
        |> Textarea.readOnly
        |> Textarea.render
    let pendingNotes =
        Textarea.create "pendingNotes" "Checking notes"
        |> Textarea.withValue "Please quote invoice INV-2048."
        |> Textarea.pending
        |> Textarea.render
    let unavailableNotes =
        Textarea.create "unavailableNotes" "Unavailable notes"
        |> Textarea.disabled
        |> Textarea.render

    let errorSummaryExample =
        // Your application supplies this validation error.
        let emailError = "Enter a valid email address."
        let email =
            Input.create "email" "Email address"
            |> Input.withId "summary-email"
            |> Input.withType InputType.Email
            |> Input.withValue "not-an-email"
            |> Input.withValidation emailError

        div {
            _class "grid min-w-0 max-w-xl gap-4 p-4"
            ErrorSummary.create "example-errors" "Check your details" [
                // The summary link targets the same input rendered below.
                FieldError.create (Input.id email) "Email address" emailError
            ]
            |> ErrorSummary.render

            Input.render email
        }

    let informationNotice =
        Notice.create "notice-information" "Before you submit" (p { "Check the contact name and email address before sending account correspondence." })
        |> Notice.withActions (a { _href "/components/form-layouts"; _class "underline underline-offset-2"; "Review contact details" })
        |> Notice.render
    let successNotice =
        Notice.create "notice-success" "Export ready" (p { "Your export is ready to download." })
        |> Notice.withTone Tone.Positive
        |> Notice.withActions (a { _href "data:text/csv;charset=utf-8,Account%2CBalance%0AOperating%2C42800"; _attr ("download", "accounts.csv"); _class "underline underline-offset-2"; "Download accounts" })
        |> Notice.render
    let warningNotice =
        Notice.create "notice-warning" "Review required" (p { "Two transactions need an account before they can be posted." })
        |> Notice.withTone Tone.Warning
        |> Notice.render
    let criticalNotice =
        Notice.create "notice-error" "Details need attention" (p { "Correct the highlighted fields and validate the form again." })
        |> Notice.withTone Tone.Critical
        |> Notice.withActions (a { _href "/components/error-summary"; _class "underline underline-offset-2"; "Review highlighted fields" })
        |> Notice.render

    let accountSearchExample =
        div {
            _class "grid min-w-0 max-w-xl gap-3 p-4"
            _dataSignals "{fieldsearch: ''}"
            Input.create "query" "Search accounts"
            |> Input.withId "field-search"
            |> Input.withType InputType.Search
            |> Input.withAttributes [ _dataBind "fieldsearch"; _autocomplete "off" ]
            |> Input.render
            ul {
                _ariaLabel "Matching accounts"
                for name in [ "Operating"; "Savings"; "Tax reserve" ] do
                    li { _dataShow ($"'{name.ToLowerInvariant()}'.includes($fieldsearch.toLowerCase())"); _class "text-sm"; name }
            }
            output {
                _role "status"
                _class "text-sm text-[var(--fve-muted-text)]"
                _dataText "['Operating', 'Savings', 'Tax reserve'].filter(name => name.toLowerCase().includes($fieldsearch.toLowerCase())).length + ' matching accounts'"
            }
        }

    let memberOptions =
        [ Select.option "alex" "Alex Morgan"
          Select.option "jamie" "Jamie Lee"
          Select.option "riley" "Riley Chen"
          Select.option "taylor" "Taylor Brooks"
          Select.option "sam" "Sam Rivera" |> Select.disable ]

    let multipleMembersSelect =
        Select.create "memberIds" "Team members" id memberOptions
        |> Select.multiple
        |> Select.withPlaceholder "Choose members"
        |> Select.render

    let selectedMembersSelect =
        Select.create "assignedIds" "Assigned members" id memberOptions
        |> Select.multiple
        |> Select.withSelectedMany [ "alex"; "jamie"; "riley" ]
        |> Select.render

    let disabledMembersSelect =
        Select.create "disabledMemberIds" "Disabled members" id memberOptions
        |> Select.multiple
        |> Select.withSelectedMany [ "alex"; "jamie" ]
        |> Select.disabled
        |> Select.render

    let pendingMembersSelect =
        Select.create "pendingMemberIds" "Updating members" id memberOptions
        |> Select.multiple
        |> Select.withSelectedMany [ "alex"; "jamie" ]
        |> Select.pending
        |> Select.render

    let multipleSelectForm selected validation result =
        div {
            _id "components-multiple-select-form-region"
            _class "grid gap-3"
            form {
                _ariaLabel "Project reviewers"
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/multiple-select', {contentType: 'form'})")
                _class "grid gap-3"
                Select.create "reviewerIds" "Reviewers" id memberOptions
                |> Select.withId "components-reviewers"
                |> Select.multiple
                |> Select.withSelectedMany selected
                |> Select.withDescription "Choose one to three reviewers."
                |> Select.required
                |> (match validation with Some message -> Select.withValidation message | None -> id)
                |> Select.render
                choiceSubmitButton "components-reviewers-submit" "Validate reviewers"
            }
            choiceResult "components-reviewers-result" result
        }

    let multipleMembersCombobox =
        Select.create "searchMemberIds" "Search members" id memberOptions
        |> Select.withSearch SelectSearch.Static
        |> Select.multiple
        |> Select.withSelectedMany [ "alex"; "jamie" ]
        |> Select.render

    let disabledMembersCombobox =
        Select.create "disabledSearchIds" "Disabled search members" id memberOptions
        |> Select.withSearch SelectSearch.Static
        |> Select.multiple
        |> Select.withSelectedMany [ "alex"; "jamie" ]
        |> Select.disabled
        |> Select.render

    let pendingMembersCombobox =
        Select.create "pendingSearchIds" "Updating search members" id memberOptions
        |> Select.withSearch SelectSearch.Static
        |> Select.multiple
        |> Select.withSelectedMany [ "alex"; "jamie" ]
        |> Select.pending
        |> Select.render

    let loadingMembersCombobox =
        Select.create "loadingSearchIds" "Loading members" id memberOptions
        |> Select.withSearch SelectSearch.Static
        |> Select.multiple
        |> Select.withSelectedMany [ "alex" ]
        |> Select.loading
        |> Select.render

    let remoteMembersConfig =
        Select.create "remoteMemberIds" "Remote members" id memberOptions
        |> Select.withId "components-remote-members"
        |> Select.multiple
        |> Select.withSelectedMany [ "alex" ]
        |> Select.withPlaceholder "Search members"
        |> Select.withSearch (SelectSearch.Remote "/components/members/search")

    let remoteMembersCombobox =
        div {
            _class "grid gap-3"
            remoteMembersConfig |> Select.render
            button {
                _type "button"
                _dataOn ("click", "@get('/components/members/field')")
                _class "min-h-8 text-sm text-[var(--fve-brand-text)] underline underline-offset-2"
                "Refresh members"
            }
        }

    let searchMemberOptions (query:string) =
        [ "alex", "Alex Morgan"; "jamie", "Jamie Lee"; "riley", "Riley Chen"; "taylor", "Taylor Brooks" ]
        |> List.filter (fun (_, label) -> label.Contains(query, StringComparison.OrdinalIgnoreCase))
        |> List.map (fun (value, label) -> Select.option value label)

    let remoteMembersField query =
        remoteMembersConfig
        |> Select.withQuery query
        |> Select.withOptions (searchMemberOptions query)
        |> Select.render

    let remoteMembersOptions query retry =
        let config = remoteMembersConfig |> Select.withQuery query
        if String.Equals(query, "error", StringComparison.OrdinalIgnoreCase) then
            if retry then config |> Select.renderOptions
            else
                config
                |> Select.withSearch (SelectSearch.Remote "/components/members/search?retry=true")
                |> Select.withError "Members could not be loaded."
                |> Select.renderOptions
        else
            config |> Select.withOptions (searchMemberOptions query) |> Select.renderOptions

    let multipleComboboxForm selected validation result =
        div {
            _id "components-multiple-combobox-form-region"
            _class "grid gap-3"
            form {
                _ariaLabel "Project assignees"
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/multiple-combobox', {contentType: 'form'})")
                _class "grid gap-3"
                Select.create "assigneeIds" "Assignees" id memberOptions
                |> Select.withId "components-assignees"
                |> Select.withSearch SelectSearch.Static
                |> Select.multiple
                |> Select.withSelectedMany selected
                |> Select.withDescription "Choose one to three assignees."
                |> Select.required
                |> (match validation with Some message -> Select.withValidation message | None -> id)
                |> Select.render
                choiceSubmitButton "components-assignees-submit" "Validate assignees"
            }
            choiceResult "components-assignees-result" result
        }

    let basicSelect =
        Select.create "updateFrequency" "Update frequency" id [
            Select.option "daily" "Daily"
            Select.option "weekly" "Weekly"
            Select.option "monthly" "Monthly" ]
        |> Select.withSelected "weekly"
        |> Select.render
    let selectWithHelp =
        Select.create "digestFrequency" "Digest frequency" id [
            Select.option "daily" "Daily"
            Select.option "weekly" "Weekly"
            Select.option "monthly" "Monthly" ]
        |> Select.withDescription "Choose how often to receive a summary."
        |> Select.withPlaceholder "Choose a frequency"
        |> Select.render
    let basicCheckbox =
        Checkbox.create "emailUpdates" "Email me product updates"
        |> Checkbox.render
    let checkboxWithHelp =
        Checkbox.create "includeSummary" "Include a summary"
        |> Checkbox.withDescription "Add a short overview at the beginning of the report."
        |> Checkbox.render
    let basicSwitch =
        Switch.create "productUpdates" "Product updates"
        |> Switch.render
    let switchWithHelp =
        Switch.create "weeklySummary" "Weekly summary"
        |> Switch.withDescription "Receive one email with the week's activity."
        |> Switch.withChecked
        |> Switch.render
    let basicRadioGroup =
        RadioGroup.create "contactMethod" "Contact method" id [
            RadioGroup.option "email" "Email"
            RadioGroup.option "phone" "Phone" ]
        |> RadioGroup.withSelected "email"
        |> RadioGroup.render
    let radioGroupWithHelp =
        RadioGroup.create "delivery" "Delivery" id [
            RadioGroup.option "standard" "Standard"
            RadioGroup.option "express" "Express" ]
        |> RadioGroup.withDescription "Choose how you would like your order delivered."
        |> RadioGroup.render

    let selectFormRegion selected validation result =
        let config =
            Select.create "status" "Status" statusValue selectStatusOptions
            |> Select.withId "components-status"
            |> Select.withDescription "Controls whether the account can receive entries."
            |> Select.withPlaceholder "Choose a status"
            |> Select.required
            |> (match selected with Some value -> Select.withSelected value | None -> id)
            |> (match validation with Some message -> Select.withValidation message | None -> id)

        div {
            _id "components-select-form-region"
            _class "grid max-w-sm gap-3"
            form {
                _dataOn ("submit", "@post('/components/choices/select')")
                _class "grid gap-3"
                config |> Select.render
                choiceSubmitButton "components-select-submit" "Validate status"
            }
            choiceResult "components-select-result" result
        }

    let disabledStatusSelect =
        Select.create "status" "Disabled status" statusValue selectStatusOptions
        |> Select.withId "components-disabled-status"
        |> Select.withSelected Active
        |> Select.disabled
        |> Select.render

    let pendingStatusSelect =
        Select.create "status" "Updating status" statusValue selectStatusOptions
        |> Select.withId "components-pending-status"
        |> Select.withSelected Pending
        |> Select.pending
        |> Select.render

    let statusSelect = selectFormRegion None None None

    let private accounts = [ 101, "Operating"; 102, "Tax reserve"; 103, "Payroll clearing" ]
    let private accountOptions values =
        values
        |> List.map (fun (value, label) ->
            Select.option value label
            |> (if value = 103 then Select.disable else id))

    let staticAccountCombobox =
        Select.create "staticAccount" "Static account" string (accountOptions accounts)
        |> Select.withId "components-static-account"
        |> Select.withSearch SelectSearch.Static
        |> Select.withSelected 101
        |> Select.withDescription "Filter locally supplied typed options."
        |> Select.render

    let private accountComboboxConfig =
        Select.create "account" "Parent account" string (accountOptions accounts)
        |> Select.withPlaceholder "Search accounts"
        |> Select.withDescription "Results remain authoritative on the server."
        |> Select.withEmptyMessage "No matching accounts"
        |> Select.withLoadingMessage "Loading accounts"
        |> Select.withSearch (SelectSearch.Remote "/components/accounts/search")

    let accountCombobox = accountComboboxConfig |> Select.render

    let accountComboboxOptions query retry =
        if String.Equals(query, "error", StringComparison.OrdinalIgnoreCase) && retry |> not then
            accountComboboxConfig
            |> Select.withSearch (SelectSearch.Remote "/components/accounts/search?retry=true")
            |> Select.withError "Accounts could not be loaded."
            |> Select.renderOptions
        else
            accounts
            |> List.filter (fun (_, label) -> String.IsNullOrWhiteSpace query || label.Contains(query, StringComparison.OrdinalIgnoreCase))
            |> accountOptions
            |> fun options -> accountComboboxConfig |> Select.withOptions options |> Select.renderOptions

    let loadingAccountCombobox =
        Select.create "loadingAccount" "Loading account" string []
        |> Select.withId "components-loading-account"
        |> Select.withSearch (SelectSearch.Remote "/components/accounts/search")
        |> Select.withLoadingMessage "Loading accounts"
        |> Select.loading
        |> Select.render

    let validationAccountCombobox =
        Select.create "validatedAccount" "Account with validation" string (accountOptions accounts)
        |> Select.withId "components-validated-account"
        |> Select.withSearch SelectSearch.Static
        |> Select.withDescription "Choose an account before continuing."
        |> Select.withValidation "Choose an available account."
        |> Select.render

    let disabledAccountCombobox =
        Select.create "disabledAccount" "Disabled account" string (accountOptions accounts)
        |> Select.withId "components-disabled-account"
        |> Select.withSearch SelectSearch.Static
        |> Select.withSelected 101
        |> Select.disabled
        |> Select.render

    let pendingAccountCombobox =
        Select.create "pendingAccount" "Updating account" string (accountOptions accounts)
        |> Select.withId "components-pending-account"
        |> Select.withSearch SelectSearch.Static
        |> Select.withSelected 102
        |> Select.pending
        |> Select.render

    let checkboxFormRegion confirmed validation result =
        let config =
            Checkbox.create "confirmArchivedReview" "Confirm archived-account review"
            |> Checkbox.withId "components-confirm-archived-review"
            |> Checkbox.withDescription "Required before archived accounts can be included."
            |> Checkbox.required
            |> (if confirmed then Checkbox.withChecked else id)
            |> (match validation with Some message -> Checkbox.withValidation message | None -> id)

        div {
            _id "components-checkbox-form-region"
            _class "grid max-w-sm gap-3"
            form {
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/checkbox')")
                _class "grid gap-3"
                config |> Checkbox.render
                choiceSubmitButton "components-checkbox-submit" "Validate confirmation"
            }
            choiceResult "components-checkbox-result" result
        }

    let includeArchived =
        Checkbox.create "includeArchived" "Include archived accounts"
        |> Checkbox.withDescription "Archived accounts remain read-only."
        |> Checkbox.withChecked
        |> Checkbox.render

    let pendingArchivedReview =
        Checkbox.create "confirmArchivedReview" "Saving archived-account review"
        |> Checkbox.withId "components-pending-archived-review"
        |> Checkbox.withChecked
        |> Checkbox.pending
        |> Checkbox.render

    let disabledArchivedReview =
        Checkbox.create "confirmArchivedReview" "Archived review unavailable"
        |> Checkbox.withId "components-disabled-archived-review"
        |> Checkbox.disabled
        |> Checkbox.render

    let switchFormRegion enabled validation result =
        let config =
            Switch.create "postingNotifications" "Posting notifications"
            |> Switch.withId "components-posting-notifications"
            |> Switch.withDescription "Notify account owners after entries post."
            |> (if enabled then Switch.withChecked else id)
            |> (match validation with Some message -> Switch.withValidation message | None -> id)

        div {
            _id "components-switch-form-region"
            _class "grid max-w-sm gap-3"
            form {
                _dataOn ("submit", "@post('/components/choices/switch')")
                _class "grid gap-3"
                config |> Switch.render
                choiceSubmitButton "components-switch-submit" "Save notifications"
            }
            choiceResult "components-switch-result" result
        }

    let postingNotifications = switchFormRegion true None None

    let pendingNotifications =
        Switch.create "postingNotifications" "Saving notifications"
        |> Switch.withId "components-pending-notifications"
        |> Switch.withChecked
        |> Switch.pending
        |> Switch.render

    let invalidNotifications =
        Switch.create "postingNotifications" "Notification service"
        |> Switch.withId "components-invalid-notifications"
        |> Switch.withValidation "Notification preferences could not be saved."
        |> Switch.render

    let compactRows =
        ToggleButton.create "components-compact-rows" "Compact rows"
        |> ToggleButton.pressed
        |> ToggleButton.render

    let pendingCompactRows =
        ToggleButton.create "components-pending-compact-rows" "Applying compact rows"
        |> ToggleButton.pending
        |> ToggleButton.render

    let disabledCompactRows =
        ToggleButton.create "components-disabled-compact-rows" "Compact rows unavailable"
        |> ToggleButton.disabled
        |> ToggleButton.render

    let codePreviewTabs =
        Tabs.create "components-example-format" "Example format" [
            Tab.create "code" "Code" (pre { _class "overflow-x-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-4 text-sm"; "Button.secondary \"Create account\"" })
            Tab.create "preview" "Preview" (div { _class "rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] p-4"; Button.secondary "Create account" }) ]
        |> Tabs.withSelected "preview"
        |> Tabs.render

    let reviewTabsRegion refreshed =
        let refreshActivityButton =
            Button.create "Refresh activity"
            |> Button.withVariant ButtonVariant.Secondary
            |> Button.withAttributes [ _id "components-tabs-refresh"; _dataOn ("click", "evt.currentTarget.focus(); @get('/components/tabs/review')") ]
            |> Button.render
        let activity =
            if refreshed then
                div {
                    p { _role "status"; _class "font-medium"; "Review refreshed" }
                    p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; "Updated just now." }
                    refreshActivityButton
                }
            else
                div {
                    p { _class "text-sm text-[var(--fve-muted-text)]"; "Three account changes need review." }
                    refreshActivityButton
                }

        Tabs.create "components-review-tabs" "Account sections" [
            Tab.create "overview" "Overview" (p { _class "text-sm text-[var(--fve-muted-text)]"; "Operating balance: $42,800" })
            Tab.create "activity" "Activity" activity
            Tab.create "settings" "Settings" (a { _href "/components/theming"; _class "font-medium text-[var(--fve-brand-text)]"; "Review account theme settings" }) ]
        |> Tabs.withVariant TabsVariant.Underlined
        |> Tabs.render

    let private postingModeOptions =
        [ RadioGroup.option "automatic" "Automatic"
          RadioGroup.option "manual" "Manual review"
          RadioGroup.option "scheduled" "Scheduled" |> RadioGroup.disable ]

    let radioGroupFormRegion selected validation result =
        let config =
            RadioGroup.create "postingMode" "Posting mode" id postingModeOptions
            |> RadioGroup.withId "components-posting-mode"
            |> RadioGroup.withDescription "Choose how approved entries reach the ledger."
            |> RadioGroup.required
            |> (match selected with Some value -> RadioGroup.withSelected value | None -> id)
            |> (match validation with Some message -> RadioGroup.withValidation message | None -> id)

        div {
            _id "components-radio-form-region"
            _class "grid max-w-sm gap-3"
            form {
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/radio')")
                _class "grid gap-3"
                config |> RadioGroup.render
                choiceSubmitButton "components-radio-submit" "Validate posting mode"
            }
            choiceResult "components-radio-result" result
        }

    let postingMode = radioGroupFormRegion None None None

    let pendingPostingMode =
        RadioGroup.create "postingMode" "Saving posting mode" id postingModeOptions
        |> RadioGroup.withId "components-pending-posting-mode"
        |> RadioGroup.withSelected "automatic"
        |> RadioGroup.pending
        |> RadioGroup.render

    let disabledPostingMode =
        RadioGroup.create "postingMode" "Posting mode unavailable" id postingModeOptions
        |> RadioGroup.withId "components-disabled-posting-mode"
        |> RadioGroup.withSelected "manual"
        |> RadioGroup.disabled
        |> RadioGroup.render

    let menuLeadingIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 0 1 .143 1.052l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 0 1 1.05-.143Z" clip-rule="evenodd"/></svg>"""

    let dropdownMenuItems refreshed =
        [ MenuItem.group "Account" [
              MenuItem.link DropdownMenuGuide "Dropdown menu guidance"
              MenuItem.action "$menuActivations++" "Record review"
              |> MenuItem.withLeading menuLeadingIcon
              |> MenuItem.withShortcut "R" ]
          MenuItem.separator
          MenuItem.group "Reports" [
              if refreshed then
                  MenuItem.action "$menuActivations++" "Review refreshed actions"
              else
                  MenuItem.action "@get('/components/menus/actions')" "Refresh actions"
              MenuItem.action "$menuActivations++" "Export statement"
              |> MenuItem.disabled
              MenuItem.action "$menuActivations++" "Syncing ledger"
              |> MenuItem.pending
              MenuItem.action "$menuActivations++" "Create report"
              MenuItem.action "$menuActivations++" "Close period" ]
          MenuItem.separator
          MenuItem.destructiveAction "$menuDeletes++" "Delete draft" ]

    let actionMenu refreshed =
        DropdownMenu.create "components-menu-actions" "Actions" (dropdownMenuItems refreshed)
        |> DropdownMenu.withAlignment MenuAlignment.Start
        |> DropdownMenu.render destinationUrl

    let moreActionsMenu =
        DropdownMenu.create "components-menu-more" "More actions" [
            MenuItem.group "View" [
                MenuItem.link DropdownMenuGuide "Read menu guidance"
                MenuItem.action "$menuActivations++" "Record secondary action" ] ]
        |> DropdownMenu.render destinationUrl

    let dropdownMenuRegion refreshed =
        div {
            _id "components-dropdown-menu-region"
            _dataSignals "{menuActivations: 0, menuDeletes: 0}"
            div { _class "flex flex-wrap items-center gap-3"; [ actionMenu refreshed; moreActionsMenu ] }
            p { _class "mt-4 text-sm text-[var(--fve-muted-text)]"; _dataText "'Completed menu actions: ' + $menuActivations"; "Completed menu actions: 0" }
            p { _class "mt-1 text-sm text-[var(--fve-critical-text)]"; _dataText "'Delete activations: ' + $menuDeletes"; "Delete activations: 0" }
            if refreshed then
                p { _role "status"; _class "mt-1 text-sm text-[var(--fve-positive-text)]"; "Actions refreshed from the server." }
        }

    let patchedDropdownMenuRegion = dropdownMenuRegion true

    let dialogConfig =
        Dialog.create "review-account-dialog" "Review account" (
            p { "Confirm the account settings before they are applied." })
        |> Dialog.withDescription "The dialog returns focus to its trigger when it closes."
        |> Dialog.withInitialFocus "review-account-dialog-close"
        |> Dialog.dismissOnBackdrop

    let reviewDialogTrigger =
        dialogConfig
        |> Dialog.trigger "Review account"

    let reviewDialog =
        dialogConfig
        |> Dialog.withFooter (dialogConfig |> Dialog.closeButton "Close")
        |> Dialog.render

    let confirmationDialogConfig =
        ConfirmationDialog.create
            "delete-account-confirmation"
            "Delete account?"
            "Deleting Operating removes its draft entries. Posted entries remain in the audit history."
            "Keep account"
            "Delete account"
            "@post('/components/dialogs/confirm').then(() => document.getElementById('delete-account-confirmation-confirm')?.focus())"

    let confirmationDialogContent validation =
        (match validation with
         | Some message -> confirmationDialogConfig |> ConfirmationDialog.withValidation message
         | None -> confirmationDialogConfig)
        |> ConfirmationDialog.renderContent

    let pendingConfirmationConfig =
        ConfirmationDialog.create
            "pending-account-confirmation"
            "Delete account?"
            "The account deletion is already being checked."
            "Close"
            "Delete account"
            "@post('/components/dialogs/confirm')"
        |> ConfirmationDialog.pending

    let accountDrawerContent refreshed =
        div {
            _id "account-drawer-content"
            nav {
                _ariaLabel "Account settings"
                _class "grid gap-2"
                a { _id "account-drawer-profile"; _href "/components/drawer#profile"; _class "rounded-[var(--fve-radius-control)] px-3 py-2 font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"; "Profile" }
                a { _href "/components/drawer#notifications"; _class "rounded-[var(--fve-radius-control)] px-3 py-2 font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"; "Notifications" }
            }
            Button.create "Refresh panel"
            |> Button.withClass "mt-5"
            |> Button.withAttributes [ _id "account-drawer-refresh"; _dataOn ("click", "@get('/components/drawers/account').then(() => document.getElementById('account-drawer-refresh')?.focus())") ]
            |> Button.render
            if refreshed then
                p { _role "status"; _class "mt-4 text-sm text-[var(--fve-positive-text)]"; "Panel content refreshed from the server." }
        }

    let accountDrawerConfig =
        Drawer.create "account-settings-drawer" "Account settings" (accountDrawerContent false)
        |> Drawer.withDescription "Manage account preferences without leaving the current page."
        |> Drawer.withInitialFocus "account-drawer-profile"

    let filterDrawerConfig =
        Drawer.create "account-filters-drawer" "Account filters" (
            nav {
                _ariaLabel "Account filters"
                _class "grid gap-3"
                a { _href "/components/drawer#active"; "Active accounts" }
                a { _href "/components/drawer#archived"; "Archived accounts" }
            })
        |> Drawer.withSide DrawerSide.Start

    let patchedAccountDrawerContent = accountDrawerContent true

    let private accountTypeFilter =
        Select.create "accountTypeFilter" "Filter by account type" id [
            Select.option "all" "All types"
            Select.option "asset" "Asset"
            Select.option "liability" "Liability"
            Select.option "equity" "Equity"
            Select.option "revenue" "Revenue"
            Select.option "expense" "Expense" ]
        |> Select.withSelected "all"
        |> Select.withVisuallyHiddenLabel
        |> Select.render

    let private toolbar =
        section {
            _role "region"
            _ariaLabel "Collection controls"
            div {
                _class "flex flex-col gap-2 sm:flex-row sm:items-center"
                input {
                    _type "search"
                    _name "query"
                    _ariaLabel "Search accounts"
                    _placeholder "Search accounts"
                    _dataBind "collectionquery"
                    _class "min-h-[var(--fve-control-min-height)] min-w-40 flex-1 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm ring-1 ring-[var(--fve-border)] outline-none focus:ring-2 focus:ring-[var(--fve-brand-ring)]"
                }
                label {
                    _class "flex min-h-[var(--fve-control-min-height)] items-stretch overflow-hidden whitespace-nowrap rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] text-xs ring-1 ring-[var(--fve-border)]"
                    span { _class "flex items-center border-r border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] px-2 font-semibold text-[var(--fve-muted-text)]"; "As of" }
                    input { _type "date"; _name "asOf"; _ariaLabel "As of"; _value "2026-07-31"; _class "min-w-0 w-28 bg-transparent px-2 text-[var(--fve-text)] outline-none" }
                }
                select {
                    _name "accountType"
                    _ariaLabel "Filter by account type"
                    _dataBind "collectiontype"
                    _class "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm ring-1 ring-[var(--fve-border)] outline-none focus:ring-2 focus:ring-[var(--fve-brand-ring)]"
                    option { _value "all"; "All types" }
                    option { _value "asset"; "Asset" }
                    option { _value "liability"; "Liability" }
                    option { _value "equity"; "Equity" }
                    option { _value "revenue"; "Revenue" }
                    option { _value "expense"; "Expense" }
                }
            }
        }

    let private collectionActions =
        ActionCluster.create "components-collection-actions" [
            ApplicationAction.link LedgerCreateAccount "New account"
            |> ApplicationAction.withVariant ButtonVariant.Primary ]

    // Supply your table, filter controls, actions, and URL resolver.
    let collectionExample resolve actions toolbar table =
        Collection.create "Accounts" table
        |> Collection.withDescription
            "Review balances and posting availability."
        |> Collection.withActions actions
        |> Collection.withToolbar toolbar
        |> Collection.render resolve

    let private collectionBulkActions =
        BulkActions.create "account-bulk-actions" "accounts-selection" "Selected account actions"
            [ BulkAction.create "Queue review" "Selected accounts queued for review."
                  (fun keys -> $"document.getElementById('account-bulk-audit').textContent = 'Queued review for account IDs: ' + {keys}.join(', ')")
              |> BulkAction.primary
              BulkAction.create "Archive" "Selected accounts archived in this resettable demo."
                  (fun keys -> $"document.getElementById('account-bulk-audit').textContent = 'Archived account IDs: ' + {keys}.join(', ')")
              |> BulkAction.destructive ]
            filteredAccountTable
        |> BulkActions.render

    let collectionPage =
        div {
            _dataSignals "{collectionquery: '', collectiontype: 'all'}"
            collectionExample shellDestinationUrl collectionActions toolbar collectionBulkActions
            p {
                _role "status"
                _class "text-sm text-[var(--fve-muted-text)]"
                _dataText "Array.from(document.querySelectorAll('#accounts-selection tbody tr')).filter(row => row.getClientRects().length).length + ' matching accounts'"
                "4 matching accounts"
            }
            p {
                _id "account-bulk-audit"
                _role "status"
                _ariaLive "polite"
                _class "text-sm text-[var(--fve-muted-text)]"
            }
        }

    let collectionPreview =
        div { _class "p-4 sm:p-6 lg:p-8"; collectionPage }
        |> fullBleedThemedSurface

    // Supply your actions and rendered transaction content.
    let detailExample resolve accountsDestination actions transactions =
        Detail.create "Operating checking" [
            Section.create (SectionHeader.create "Detail") (
                DescriptionList.create [
                    DetailField.status "Status" (Status.positive "Active")
                    DetailField.text "Type" "Asset"
                    DetailField.text "Commodity" "USD"
                    DetailField.text "Parent account" "Current assets"
                    DetailField.text "Source" "Created in Ledger"
                    DetailField.text "Month-end observed balance" "Required"
                    DetailField.text "Balance" "$38,442.11" ]
                |> DescriptionList.withColumns DescriptionListColumns.Three
                |> DescriptionList.render)
            |> Section.render resolve
            SectionHeader.create "Transactions"
            |> SectionHeader.withDescription "Current assets · All accounts"
            |> SectionHeader.withActions (ActionCluster.create "detail-transaction-actions" [ ApplicationAction.link accountsDestination "View all accounts" ])
            |> fun header -> Section.create header transactions
            |> Section.withLabel "Recent transactions"
            |> Section.render resolve
        ]
        |> Detail.withActions actions
        |> Detail.render resolve

    let detailPage =
        let actions =
            ActionCluster.create "components-detail-actions" [
                ApplicationAction.link (LedgerAccount operatingAccount.id) "View account"
                |> ApplicationAction.withVariant ButtonVariant.Primary ]
            |> ActionCluster.withOverflow (recordMenuItems "account" operatingAccount.id (LedgerAccount operatingAccount.id) shellDestinationUrl (System.Text.Json.JsonSerializer.Serialize operatingAccount) |> List.tail)
        detailExample shellDestinationUrl LedgerAccounts actions transactionTable
        |> recordActionFeedback "account"

    let detailPreview =
        div { _class "p-4 sm:p-6 lg:p-8"; detailPage }
        |> fullBleedThemedSurface

    let private ledgerMark =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M4 3.75A1.75 1.75 0 0 1 5.75 2h8.5A1.75 1.75 0 0 1 16 3.75v12.5A1.75 1.75 0 0 1 14.25 18h-8.5A1.75 1.75 0 0 1 4 16.25V3.75Zm3 1.5A.75.75 0 0 0 7.75 6h4.5a.75.75 0 0 0 0-1.5h-4.5a.75.75 0 0 0-.75.75Zm0 4A.75.75 0 0 0 7.75 10h4.5a.75.75 0 0 0 0-1.5h-4.5a.75.75 0 0 0-.75.75Zm0 4a.75.75 0 0 0 .75.75h2.5a.75.75 0 0 0 0-1.5h-2.5a.75.75 0 0 0-.75.75Z"/></svg>"""

    let private treasuryMark =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M10.75 2.75a.75.75 0 0 0-1.5 0V4H7.5a2.5 2.5 0 0 0 0 5h5a1 1 0 1 1 0 2h-5a1 1 0 0 1-.93-.63.75.75 0 1 0-1.4.54A2.5 2.5 0 0 0 7.5 12.5h1.75v1.75a.75.75 0 0 0 1.5 0V12.5h1.75a2.5 2.5 0 0 0 0-5h-5a1 1 0 1 1 0-2h5a1 1 0 0 1 .93.63.75.75 0 1 0 1.4-.54A2.5 2.5 0 0 0 12.5 4h-1.75V2.75Z"/></svg>"""

    let private navigationGlyph =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M3.5 4.75A1.25 1.25 0 0 1 4.75 3.5h10.5a1.25 1.25 0 0 1 1.25 1.25v10.5a1.25 1.25 0 0 1-1.25 1.25H4.75a1.25 1.25 0 0 1-1.25-1.25V4.75Z"/></svg>"""

    let private shellItem destination label =
        SideNavItem.create destination label
        |> SideNavItem.withLeading navigationGlyph

    let private accountTitle id =
        if id = 2048 then "Operating checking"
        else rows |> List.tryFind (fun row -> row.id = id) |> Option.map _.name |> Option.defaultValue $"Account {id}"

    let private transactionTitle id =
        transactionRows |> List.find (fun row -> row.id = id) |> _.description

    let private ledgerBreadcrumbs current =
        let items =
            match current with
            | LedgerHome -> [ BreadcrumbItem.create LedgerHome "Home" ]
            | LedgerAccounts -> [ BreadcrumbItem.create LedgerHome "Home"; BreadcrumbItem.create LedgerAccounts "Accounts" ]
            | LedgerAccount id -> [ BreadcrumbItem.create LedgerHome "Home"; BreadcrumbItem.create LedgerAccounts "Accounts"; BreadcrumbItem.create (LedgerAccount id) (accountTitle id) ]
            | LedgerTransaction id -> [ BreadcrumbItem.create LedgerHome "Home"; BreadcrumbItem.create (LedgerTransaction id) (transactionTitle id) ]
            | LedgerCreateAccount -> [ BreadcrumbItem.create LedgerHome "Home"; BreadcrumbItem.create LedgerAccounts "Accounts"; BreadcrumbItem.create LedgerCreateAccount "Create account" ]
            | LedgerReports -> [ BreadcrumbItem.create LedgerHome "Home"; BreadcrumbItem.create LedgerReports "Reports" ]
            | LedgerSettings -> [ BreadcrumbItem.create LedgerHome "Home"; BreadcrumbItem.create LedgerSettings "Settings" ]
            | _ -> [ BreadcrumbItem.create LedgerHome "Home" ]
        Breadcrumbs.create "ledger-breadcrumbs" "Breadcrumb" items

    let private treasuryBreadcrumbs current =
        let items =
            match current with
            | TreasuryHome -> [ BreadcrumbItem.create TreasuryHome "Home" ]
            | TreasuryPeriod period -> [ BreadcrumbItem.create TreasuryHome "Home"; BreadcrumbItem.create (TreasuryPeriod period) period ]
            | TreasuryTransactions -> [ BreadcrumbItem.create TreasuryHome "Home"; BreadcrumbItem.create (TreasuryPeriod "July") "July 2026"; BreadcrumbItem.create TreasuryTransactions "Transactions" ]
            | TreasuryPayees -> [ BreadcrumbItem.create TreasuryHome "Home"; BreadcrumbItem.create TreasuryPayees "Payees" ]
            | TreasuryAccounts -> [ BreadcrumbItem.create TreasuryHome "Home"; BreadcrumbItem.create TreasuryAccounts "Accounts" ]
            | _ -> [ BreadcrumbItem.create TreasuryHome "Home" ]
        Breadcrumbs.create "treasury-breadcrumbs" "Breadcrumb" items

    // Resolve your destination values to URLs.
    let breadcrumbsExample resolve home accounts account =
        Breadcrumbs.create "ledger-breadcrumbs" "Breadcrumb" [
            BreadcrumbItem.create home "Home"
            BreadcrumbItem.create accounts "Accounts"
            BreadcrumbItem.create account "Operating checking"
        ]
        |> Breadcrumbs.render resolve

    let breadcrumbsPreview =
        div {
            for attribute in shellDocumentNavigationAttributes do attribute
            breadcrumbsExample shellDestinationUrl LedgerHome LedgerAccounts (LedgerAccount 2048)
        }
        |> themedSurface

    let private ledgerNavigation current =
        let navigationCurrent =
            match current with
            | LedgerAccount _ | LedgerCreateAccount -> LedgerAccounts
            | LedgerHome | LedgerAccounts | LedgerReports | LedgerSettings -> current
            | _ -> LedgerHome
        let workspace =
            div {
                p { _class "text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"; "Workspace" }
                p { _class "mt-1 truncate text-sm font-semibold"; "Meier Made" }
            }
        let navigationHeader =
            SideNavHeader.create "Ledger"
            |> SideNavHeader.withContent (
                a {
                    _href (shellDestinationUrl LedgerHome)
                    _class "flex min-w-0 items-center gap-3 rounded-[var(--fve-radius-control)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                    span { _ariaHidden true; _class "flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white"; ledgerMark }
                    strong { _class "truncate text-base font-semibold"; "Ledger" }
                })
        SideNav.create "ledger-side-navigation" "Ledger primary navigation" navigationHeader [
            SideNavSection.group "Manage" [
                shellItem LedgerHome "Dashboard"
                shellItem LedgerAccounts "Accounts" ]
            SideNavSection.group "Analyze" [ shellItem LedgerReports "Reports" ]
            SideNavSection.group "Configure" [ shellItem LedgerSettings "Settings" ] ]
        |> fun navigation ->
            match current with
            | LedgerTransaction _ -> navigation
            | _ -> SideNav.withCurrent navigationCurrent navigation
        |> SideNav.withWidth SideNavWidth.Standard
        |> SideNav.withContext workspace
        |> SideNav.withMobileContext workspace
        |> SideNav.withFooter (
            a {
                _href (shellDestinationUrl LedgerSettings)
                _class "flex min-h-[var(--fve-control-min-height)] items-center gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                span { _ariaHidden true; _class "flex size-8 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs text-[var(--fve-brand-text)]"; "AM" }
                span { _class "min-w-0"; span { _class "block truncate"; "Andrew Meier" }; span { _class "block truncate text-xs font-normal text-[var(--fve-muted-text)]"; "Account settings" } }
            })

    let groupedLedgerNavigation = ledgerNavigation LedgerAccounts

    let sideNavigationPreview =
        div {
            for attribute in shellDocumentNavigationAttributes do attribute
            _class "h-[36rem] overflow-hidden rounded-xl ring-1 ring-[var(--fve-border)]"
            groupedLedgerNavigation |> SideNav.render shellDestinationUrl
        }
        |> themedSurface

    let private ledgerTitle = function
        | LedgerHome -> "Dashboard"
        | LedgerAccounts -> "Accounts"
        | LedgerAccount id -> accountTitle id
        | LedgerTransaction id -> transactionTitle id
        | LedgerCreateAccount -> "Create account"
        | LedgerReports -> "Reports"
        | LedgerSettings -> "Settings"
        | _ -> "Ledger"

    let private refreshBalancesAction =
        ApplicationAction.command "$ledgerRefreshes++" "Refresh balances"
        |> ApplicationAction.withVariant ButtonVariant.Primary
        |> ApplicationAction.asIconOnly refreshIcon

    let private pageAction destination label =
        ApplicationAction.link destination label

    // Breadcrumbs are optional content, not a built-in heading.
    let pageTopBarExample resolve breadcrumbs =
        PageTopBar.create ()
        |> PageTopBar.withContent (
            div {
                _class "flex min-h-[var(--fve-shell-bar-min-height)] items-center px-4 sm:px-6 lg:px-8"
                breadcrumbs |> Breadcrumbs.render resolve
            })

    let renderPageTopBar resolve breadcrumbs =
        pageTopBarExample resolve breadcrumbs |> PageTopBar.render

    let private pageTopBar breadcrumbs = pageTopBarExample shellDestinationUrl breadcrumbs

    let pageTopBarPreview =
        div {
            for attribute in shellDocumentNavigationAttributes do attribute
            renderPageTopBar shellDestinationUrl (ledgerBreadcrumbs (LedgerAccount 2048))
        }
        |> themedSurface

    let accountPageHeader =
        PageHeader.create "Account 2048"
        |> PageHeader.withSubtitle "Operating checking · Updated moments ago"
        |> PageHeader.withActions (
            ActionCluster.create "account-2048-actions" [
                refreshBalancesAction
                pageAction LedgerReports "View reports" ]
            |> ActionCluster.withOverflow [
                MenuItem.link LedgerSettings "Account settings" ])

    let pageHeaderPreview =
        div {
            for attribute in shellDocumentNavigationAttributes do attribute
            _dataSignals "{ledgerRefreshes: 0}"
            accountPageHeader |> PageHeader.render shellDestinationUrl
            output { _class "sr-only"; _role "status"; _dataText "'Balance refreshes: ' + $ledgerRefreshes"; "Balance refreshes: 0" }
        }
        |> themedSurface

    let private treasuryNavigation current =
        let navigationCurrent =
            match current with
            | TreasuryPeriod _ -> TreasuryTransactions
            | TreasuryHome | TreasuryTransactions | TreasuryPayees | TreasuryAccounts -> current
            | _ -> TreasuryHome
        let navigationHeader =
            SideNavHeader.create "Treasury"
            |> SideNavHeader.withContent (
                div {
                    _class "flex min-w-0 items-center gap-3"
                    span { _ariaHidden true; _class "flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white"; treasuryMark }
                    strong { _class "truncate text-base font-semibold"; "Treasury" }
                })
        SideNav.create "treasury-side-navigation" "Treasury primary navigation" navigationHeader [
            SideNavSection.ungrouped [
                shellItem TreasuryHome "Overview"
                shellItem TreasuryTransactions "Transactions"
                shellItem TreasuryPayees "Payees"
                shellItem TreasuryAccounts "Accounts" ] ]
        |> SideNav.withCurrent navigationCurrent
        |> SideNav.withWidth SideNavWidth.Narrow
        |> SideNav.withFooter (
            a {
                _href (shellDestinationUrl TreasuryHome)
                _class "block rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                "Meier Made"
                span { _class "mt-1 block text-xs font-normal text-[var(--fve-muted-text)]"; "Cash management" }
            })

    let private upcomingPayments =
        let actions =
            ActionCluster.create "upcoming-payment-actions" [
                ApplicationAction.link TreasuryPayees "View payees" ]
            |> ActionCluster.withOverflow [
                MenuItem.link TreasuryAccounts "View accounts" ]
        let header =
            SectionHeader.create "Upcoming transactions"
            |> SectionHeader.withDescription "Payments expected before the current period closes."
            |> SectionHeader.withActions actions
        Section.create header (
            ul {
                _role "list"
                _class "divide-y divide-[var(--fve-border)]"
                li { _class "flex items-center justify-between gap-4 py-3 text-sm"; span { "Aug 15 · Payroll" }; Status.positive "Ready" }
                li { _class "flex items-center justify-between gap-4 py-3 text-sm"; span { "Aug 18 · Cloud hosting" }; Status.create "Scheduled" |> Status.withTone Tone.Informative |> Status.render }
            })
        |> Section.render shellDestinationUrl

    let private completedPayments =
        Section.create (SectionHeader.create "Completed transactions") (
            div {
                _class "flex items-center justify-between gap-4 py-3 text-sm"
                span { "Jul 28 · Northwind payment" }
                strong { _class "font-semibold"; "$4,800.00" }
            })
        |> Section.render shellDestinationUrl

    let private transactionTabs =
        Tabs.create "treasury-transactions-tabs" "Transaction views" [
            Tab.create "upcoming" "Upcoming" upcomingPayments
            Tab.create "completed" "Completed" completedPayments ]
        |> Tabs.withVariant TabsVariant.Underlined
        |> Tabs.render

    // Supply the top bar and tab content; Page owns their layout.
    let transactionsPage topBar tabs =
        PageHeader.create "Transactions"
        |> PageHeader.withSubtitle "Review scheduled and completed cash activity."
        |> fun pageHeader -> Page.create pageHeader empty
        |> Page.withTopBar topBar
        |> Page.withTabs tabs
        |> Page.withWidth PageWidth.Wide
        |> Page.withBodyLayout PageBodyLayout.Padded

    let renderTransactionsPage resolve topBar tabs =
        transactionsPage topBar tabs |> Page.render resolve

    let canvasPage () =
        let graph =
            div {
                _role "region"
                _ariaLabel "Posting flow canvas"
                _tabindex 0
                _class "h-full overflow-auto"
                _dataSignals "{postingZoom: 1}"
                div {
                    _class "flex items-center gap-2 p-3"
                    Button.create "Zoom in" |> Button.withAttributes [ _dataOn ("click", "$postingZoom = Math.min(2, $postingZoom + 0.25)") ] |> Button.render
                    Button.create "Zoom out" |> Button.withAttributes [ _dataOn ("click", "$postingZoom = Math.max(0.5, $postingZoom - 0.25)") ] |> Button.render
                    output { _ariaLabel "Zoom level"; _dataText "Math.round($postingZoom * 100) + '%'"; "100%" }
                }
                div {
                    _style "width:1000px;height:600px;transform-origin:top left"
                    _dataAttr ("style", "'width:' + (1000 * $postingZoom) + 'px;height:' + (600 * $postingZoom) + 'px'")
                    raw """<svg role="img" aria-label="Posting flow from receivables through operating checking to payroll" viewBox="0 0 1000 600" width="100%" height="100%"><path d="M260 230H390M610 230H740" stroke="var(--fve-border)" stroke-width="3"/><g fill="var(--fve-surface)" stroke="var(--fve-border)"><rect x="40" y="190" width="220" height="80" rx="8"/><rect x="390" y="190" width="220" height="80" rx="8"/><rect x="740" y="190" width="220" height="80" rx="8"/></g><g fill="var(--fve-text)" font-size="16" text-anchor="middle"><text x="150" y="236">Receivables</text><text x="500" y="236">Operating checking</text><text x="850" y="236">Payroll</text></g></svg>"""
                }
            }
        Page.create (PageHeader.create "Posting flow") graph
        |> Page.withBodyLayout PageBodyLayout.Canvas

    let treasuryTransactionsPage = transactionsPage (pageTopBar (treasuryBreadcrumbs TreasuryTransactions)) transactionTabs

    let private ledgerPage current =
        let page actions subtitle content =
            PageHeader.create (ledgerTitle current)
            |> PageHeader.withSubtitle subtitle
            |> PageHeader.withActions actions
            |> fun header -> Page.create header content
            |> Page.withTopBar (pageTopBar (ledgerBreadcrumbs current))
            |> Page.withWidth PageWidth.Wide
            |> Page.withBodyLayout PageBodyLayout.Padded

        match current with
        | LedgerAccounts ->
            let actions =
                ActionCluster.create "ledger-accounts-page-actions" [
                    ApplicationAction.link LedgerCreateAccount "Create"
                    |> ApplicationAction.withVariant ButtonVariant.Primary ]
                |> ActionCluster.withOverflow [ MenuItem.link LedgerSettings "Account settings" ]
            let content =
                accountTableConfig LedgerAccount shellDestinationUrl (fun _ -> [])
                |> Table.render
                |> recordActionFeedback "account"
                |> Collection.create "Accounts"
                |> Collection.withVisuallyHiddenTitle
                |> Collection.withToolbar toolbar
                |> Collection.render shellDestinationUrl
            page actions "Chart of accounts · Current valuation" content

        | LedgerAccount accountId ->
            let account = rows |> List.tryFind (fun row -> row.id = accountId) |> Option.defaultValue operatingAccount
            let actions =
                ActionCluster.create $"ledger-account-{accountId}-page-actions" [
                    refreshBalancesAction
                    ApplicationAction.link LedgerReports "View reports" ]
                |> ActionCluster.withOverflow [ MenuItem.link LedgerSettings "Account settings" ]
            let content =
                Detail.create (ledgerTitle current) [
                    Section.create (SectionHeader.create "Detail") (
                        DescriptionList.create [
                            DetailField.status "Status" (Status.positive "Active")
                            DetailField.text "Type" account.accountType
                            DetailField.text "Commodity" account.commodity
                            DetailField.text "Parent account" (if accountId = operatingAccount.id then "Current assets" else "None")
                            DetailField.text "Source" "Created in Ledger"
                            DetailField.text "Month-end observed balance" (if accountId = operatingAccount.id then "Required" else "Not required")
                            DetailField.text "Balance" (money account.balance) ]
                        |> DescriptionList.withColumns DescriptionListColumns.Three
                        |> DescriptionList.render)
                    |> Section.render shellDestinationUrl
                    SectionHeader.create "Transactions"
                    |> SectionHeader.withDescription "Current assets · All accounts"
                    |> fun header -> Section.create header (transactionTableFor LedgerTransaction shellDestinationUrl)
                    |> Section.withLabel "Account transactions"
                    |> Section.render shellDestinationUrl
                ]
                |> Detail.withVisuallyHiddenTitle
                |> Detail.render shellDestinationUrl
            page actions "Asset · USD · Updated moments ago" content

        | LedgerTransaction transactionId ->
            let row = transactionRows |> List.find (fun row -> row.id = transactionId)
            let actions =
                ActionCluster.create $"ledger-transaction-{transactionId}-page-actions" [ ApplicationAction.link LedgerAccounts "View accounts" ]
            let content =
                Section.create (SectionHeader.create "Detail") (
                    DescriptionList.create [
                        DetailField.text "Transaction ID" (string row.id)
                        DetailField.text "Date" row.date
                        DetailField.text "Status" (string row.status)
                        DetailField.text "Accounts" row.accounts
                        DetailField.text "Amount" (money row.amount) ]
                    |> DescriptionList.render)
                |> Section.render shellDestinationUrl
            page actions "Transaction details" content

        | LedgerCreateAccount ->
            let actions =
                ActionCluster.create "ledger-create-account-page-actions" [
                    ApplicationAction.link LedgerAccounts "Cancel"
                    ApplicationAction.command "$ledgerCreates++" "Save"
                    |> ApplicationAction.withVariant ButtonVariant.Primary ]
            let content =
                section {
                    _class "border-y border-[var(--fve-border)] px-4 py-5 sm:px-6 lg:px-8"
                    h2 { _class "text-base font-semibold"; "Account details" }
                    p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; "Choose a name, type, commodity, and optional parent account." }
                }
            page actions "Add an account to the chart of accounts." content

        | LedgerHome ->
            let actions =
                ActionCluster.create "ledger-home-page-actions" [
                    ApplicationAction.link LedgerAccounts "View accounts"
                    |> ApplicationAction.withVariant ButtonVariant.Primary ]
            let content =
                section {
                    _ariaLabel "Ledger summary"
                    _class "grid border-y border-[var(--fve-border)] sm:grid-cols-3 sm:divide-x sm:divide-[var(--fve-border)]"
                    for label, value, description in [ "Assets", "$184,230.19", "Current valuation"; "Liabilities", "−$42,118.44", "Current valuation"; "Unverified transactions", "1", "Requires review" ] do
                        div {
                            _class "px-4 py-5 sm:px-6"
                            Metric.text label value |> Metric.withDescription description |> Metric.render
                        }
                }
            page actions "Meier Made · Production" content

        | LedgerReports | LedgerSettings ->
            let actions =
                ActionCluster.create $"{shellDestinationKey current}-page-actions" [
                    ApplicationAction.link LedgerAccounts "View accounts"
                    |> ApplicationAction.withVariant ButtonVariant.Primary ]
            let content =
                section {
                    _class "border-y border-[var(--fve-border)] px-4 py-6 sm:px-6 lg:px-8"
                    h2 { _class "text-base font-semibold"; ledgerTitle current }
                    p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; "Financial workflows share a constrained page; graphs can opt into a full-width canvas." }
                }
            page actions "Meier Made · Production" content

        | _ -> failwith "Ledger pages require a Ledger destination."

    let private treasuryPage current =
        let pageTitle =
            match current with
            | TreasuryTransactions -> "Transactions"
            | TreasuryPayees -> "Payees"
            | TreasuryAccounts -> "Accounts"
            | _ -> "Overview"
        let pageHeader =
            PageHeader.create pageTitle
            |> PageHeader.withSubtitle "Meier Made cash management"
        if current = TreasuryTransactions then treasuryTransactionsPage
        else
            Page.create pageHeader (
                section {
                    _class "border-y border-[var(--fve-border)] px-4 py-6 sm:px-6 lg:px-8"
                    h2 { _class "text-base font-semibold"; pageTitle }
                    p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; "Choose another financial destination from the product navigation." }
                })
            |> Page.withTopBar (pageTopBar (treasuryBreadcrumbs current))
            |> Page.withWidth PageWidth.Full
            |> Page.withBodyLayout PageBodyLayout.FullBleed

    // navigation: a SideNav configuration.
    // content: rendered Page HTML.
    // resolve: your destination-to-URL function.
    let ledgerShellExample resolve navigation content =
        AppShell.create "ledger-app-shell" navigation content
        |> AppShell.withTheme (
            ComponentsTheme.sky
            |> ComponentsTheme.withRadius Radius.Large
            |> ComponentsTheme.withDensity Density.Compact)
        |> AppShell.withBoundary AppShellBoundary.Container
        |> AppShell.withMobileBottomNavigation "ledger-bottom-navigation" "Ledger quick navigation" [
            BottomNavigationItem.create LedgerHome "Dashboard"
            BottomNavigationItem.create LedgerAccounts "Accounts"
            BottomNavigationItem.create LedgerReports "Reports"
            BottomNavigationItem.create LedgerSettings "Settings" ]
        |> AppShell.asPreview "Ledger application preview"
        |> AppShell.render resolve

    let treasuryShellExample resolve navigation content =
        AppShell.create "treasury-app-shell" navigation content
        |> AppShell.withTheme (
            ComponentsTheme.emerald
            |> ComponentsTheme.withRadius Radius.Medium
            |> ComponentsTheme.withDensity Density.Compact)
        |> AppShell.withBoundary AppShellBoundary.Container
        |> AppShell.withMobileBottomNavigation "treasury-bottom-navigation" "Treasury quick navigation" [
            BottomNavigationItem.create TreasuryHome "Overview"
            BottomNavigationItem.create TreasuryTransactions "Transactions"
            BottomNavigationItem.create TreasuryPayees "Payees"
            BottomNavigationItem.create TreasuryAccounts "Accounts" ]
        |> AppShell.asPreview "Treasury application preview"
        |> AppShell.render resolve

    let ledgerShell current =
        let page =
            div {
                _dataSignals "{ledgerRefreshes: 0, ledgerCreates: 0}"
                ledgerPage current |> Page.render shellDestinationUrl
                output { _class "sr-only"; _role "status"; _dataText "'Balance refreshes: ' + $ledgerRefreshes"; "Balance refreshes: 0" }
            }
        ledgerShellExample shellDestinationUrl (ledgerNavigation current) page

    let treasuryShell current =
        treasuryShellExample shellDestinationUrl (treasuryNavigation current) (treasuryPage current |> Page.render shellDestinationUrl)

    let private shellPreviewFor current =
        match current with
        | LedgerHome | LedgerAccounts | LedgerAccount _ | LedgerTransaction _ | LedgerCreateAccount | LedgerReports | LedgerSettings -> ledgerShell current
        | TreasuryHome | TreasuryPeriod _ | TreasuryTransactions | TreasuryPayees | TreasuryAccounts -> treasuryShell current
        |> fun shell ->
            div {
                _attr ("data-fve-full-bleed-example", "true")
                _class "docs-components-preview h-[36rem]"
                shell
            }

    let private appModePrevious current =
        match current with
        | LedgerAccounts -> None
        | LedgerAccount _ -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | LedgerTransaction _ -> Some(FixtureLink.create "Operating checking" (shellDestinationUrl (LedgerAccount operatingAccount.id)))
        | LedgerCreateAccount -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | LedgerHome | LedgerReports | LedgerSettings -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | _ -> None

    let private appModeNext current =
        match current with
        | LedgerAccounts -> Some(FixtureLink.create "Operating checking" (shellDestinationUrl (LedgerAccount operatingAccount.id)))
        | LedgerAccount _ -> Some(FixtureLink.create "Create account" (shellDestinationUrl LedgerCreateAccount))
        | LedgerCreateAccount -> Some(FixtureLink.create "Dashboard" (shellDestinationUrl LedgerHome))
        | LedgerHome | LedgerReports | LedgerSettings -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | _ -> None

    let shellFixtureFor current =
        let frame = Browser.create (shellPreviewFor current) |> Browser.withAddress "https://ledger.example.test/accounts" |> Browser.withAppMode "ledger-workflow" "Ledger account workflow" |> Browser.render
        let fixture = Fixture.create "ledger-workflow" frame
        let fixture = appModePrevious current |> Option.map (fun previous -> Fixture.withPrevious previous fixture) |> Option.defaultValue fixture
        let fixture = appModeNext current |> Option.map (fun next -> Fixture.withNext next fixture) |> Option.defaultValue fixture
        div {
            _id "components-app-shell-fixture"
            for attribute in shellFixtureNavigationAttributes do attribute
            fixture |> Fixture.render
        }

    let private registration id path navLabel title : DocPage =
        { id = id
          path = path
          aliases = []
          navLabel = navLabel
          category = "Primitives"
          title = title
          browserTitle = $"{title} · FSharp.ViewEngine.Components"
          nodes = [] }

    let private applicationRegistration id path navLabel title =
        { registration id path navLabel title with category = "Application" }

    let private packageRegistration id path navLabel title =
        { registration id path navLabel title with category = "FSharp.ViewEngine.Components" }

    let overviewRegistration =
        packageRegistration "components-overview" "/components" "Overview" "Components"

    let installationRegistration =
        packageRegistration "components-installation" "/components/installation" "Installation" "Installation"

    let buttonRegistration = registration "components-button" "/components/button" "Button" "Button"
    let iconButtonRegistration = registration "components-icon-button" "/components/icon-button" "Icon button" "Icon button"
    let badgeRegistration = registration "components-badge" "/components/badge" "Badge" "Badge"
    let statusRegistration = registration "components-status" "/components/status" "Status" "Status"
    let loadingIndicatorRegistration = registration "components-loading-indicator" "/components/loading-indicator" "Loading indicator" "Loading indicator"
    let emptyStateRegistration = registration "components-empty-state" "/components/empty-state" "Empty state" "Empty state"
    let tableRegistration = registration "components-table" "/components/table" "Table" "Table"
    let descriptionListRegistration = registration "components-description-list" "/components/description-list" "Description list" "Description list"
    let metricRegistration = registration "components-metric" "/components/metric" "Metric" "Metric"
    let paginationRegistration = registration "components-pagination" "/components/pagination" "Pagination" "Pagination"
    let inputRegistration = registration "components-input" "/components/input" "Input" "Input"
    let formLayoutsRegistration = applicationRegistration "components-form-layouts" "/components/form-layouts" "Form layouts" "Form layouts"
    let textareaRegistration = registration "components-textarea" "/components/textarea" "Textarea" "Textarea"
    let errorSummaryRegistration = registration "components-error-summary" "/components/error-summary" "Error summary" "Error summary"
    let noticeRegistration = registration "components-notice" "/components/notice" "Notice" "Notice"
    let selectRegistration = registration "components-select" "/components/select" "Select" "Select"
    let checkboxRegistration = registration "components-checkbox" "/components/checkbox" "Checkbox" "Checkbox"
    let switchRegistration = registration "components-switch" "/components/switch" "Switch" "Switch"
    let toggleButtonRegistration = registration "components-toggle-button" "/components/toggle-button" "Toggle button" "Toggle button"
    let breadcrumbsRegistration = registration "components-breadcrumbs" "/components/breadcrumbs" "Breadcrumbs" "Breadcrumbs"
    let sideNavRegistration = registration "components-side-nav" "/components/side-nav" "Side nav" "Side nav"
    let tabsRegistration = registration "components-tabs" "/components/tabs" "Tabs" "Tabs"
    let radioGroupRegistration = registration "components-radio-group" "/components/radio-group" "Radio group" "Radio group"
    let dropdownMenuRegistration = registration "components-dropdown-menu" "/components/dropdown-menu" "Dropdown menu" "Dropdown menu"
    let dialogRegistration = registration "components-dialog" "/components/dialog" "Dialog" "Dialog"
    let confirmationDialogRegistration = registration "components-confirmation-dialog" "/components/confirmation-dialog" "Confirmation dialog" "Confirmation dialog"
    let drawerRegistration = registration "components-drawer" "/components/drawer" "Drawer" "Drawer"
    let pageTopBarRegistration = applicationRegistration "components-page-top-bar" "/components/page-top-bar" "Page top bar" "Page top bar"
    let pageHeaderRegistration = applicationRegistration "components-page-header" "/components/page-header" "Page header" "Page header"
    let sectionRegistration = registration "components-section" "/components/section" "Section" "Section"
    let browserRegistration = registration "components-browser" "/components/browser" "Browser" "Browser"
    let phoneRegistration = registration "components-phone" "/components/phone" "Phone" "Phone"
    let pageRegistration = applicationRegistration "components-page" "/components/page" "Page" "Page"
    let collectionRegistration = applicationRegistration "components-collection" "/components/collection" "Collection" "Collection"
    let detailRegistration = applicationRegistration "components-detail" "/components/detail" "Detail" "Detail"
    let appShellRegistration = applicationRegistration "components-app-shell" "/components/app-shell" "App shell" "App shell"
    let interactionRegistration = packageRegistration "components-interaction" "/components/interaction-and-server-state" "Interaction and server state" "Interaction and server state"
    let accessibilityRegistration = packageRegistration "components-accessibility" "/components/accessibility" "Accessibility" "Accessibility"
    let themingRegistration = packageRegistration "components-theming" "/components/theming" "Theming and density" "Theming and density"
    let tailwindRegistration = packageRegistration "components-tailwind" "/components/tailwind-css" "Tailwind CSS" "Tailwind CSS setup"
    let customizationRegistration = packageRegistration "components-customization" "/components/customization" "Customization" "Customization"
    let versioningRegistration = packageRegistration "components-versioning" "/components/versioning" "Versioning" "Versioning"

    let actionRegistrations =
        [ buttonRegistration
          iconButtonRegistration
          badgeRegistration
          statusRegistration
          noticeRegistration
          loadingIndicatorRegistration
          emptyStateRegistration ]
    let dataDisplayRegistrations = [ tableRegistration; descriptionListRegistration; metricRegistration; paginationRegistration ]
    let formControlRegistrations =
        [ inputRegistration
          textareaRegistration
          errorSummaryRegistration
          selectRegistration
          checkboxRegistration
          switchRegistration
          toggleButtonRegistration
          radioGroupRegistration ]
    let navigationRegistrations = [ breadcrumbsRegistration; sideNavRegistration; tabsRegistration ]
    let menuOverlayRegistrations = [ dropdownMenuRegistration; dialogRegistration; confirmationDialogRegistration; drawerRegistration ]
    let compositionRegistrations = [ pageTopBarRegistration; pageHeaderRegistration; sectionRegistration; pageRegistration; collectionRegistration; detailRegistration; appShellRegistration; formLayoutsRegistration ]
    let frameRegistrations = [ browserRegistration; phoneRegistration ]
    let guideRegistrations =
        [ interactionRegistration
          accessibilityRegistration
          themingRegistration
          tailwindRegistration
          customizationRegistration
          versioningRegistration ]

    let allRegistrations =
        [ overviewRegistration; installationRegistration ]
        @ actionRegistrations
        @ dataDisplayRegistrations
        @ formControlRegistrations
        @ navigationRegistrations
        @ menuOverlayRegistrations
        @ compositionRegistrations
        @ frameRegistrations
        @ guideRegistrations

    let page = overviewRegistration

    let private themeExample = """let theme =
    ComponentsTheme.emerald
    |> ComponentsTheme.withRadius Radius.Large
    |> ComponentsTheme.withDensity Density.Comfortable

AppShell.create "product-shell" sideNav pageContent
|> AppShell.withTheme theme
|> AppShell.render destinationUrl"""

    let private tailwindExample = """@import "tailwindcss";
@import "./FSharp.ViewEngine.Components.tailwind.css";

.acme-theme {
  --fve-brand-solid: oklch(58% 0.18 264);
  --fve-brand-hover: oklch(51% 0.20 264);
  --fve-brand-ring: oklch(68% 0.16 264);
}"""

    let primaryButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            for size in [ ControlSize.Small; ControlSize.Medium; ControlSize.Large ] do
                Button.create "Create account"
                |> Button.withVariant ButtonVariant.Primary
                |> Button.withSize size
                |> Button.render
        }
    let secondaryButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            for size in [ ControlSize.Small; ControlSize.Medium; ControlSize.Large ] do
                Button.create "View reports"
                |> Button.withVariant ButtonVariant.Secondary
                |> Button.withSize size
                |> Button.render
        }
    let ghostButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            for size in [ ControlSize.Small; ControlSize.Medium; ControlSize.Large ] do
                Button.create "Cancel"
                |> Button.withVariant ButtonVariant.Ghost
                |> Button.withSize size
                |> Button.render
        }
    let destructiveButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            for size in [ ControlSize.Small; ControlSize.Medium; ControlSize.Large ] do
                Button.create "Delete account"
                |> Button.withVariant ButtonVariant.Destructive
                |> Button.withSize size
                |> Button.render
        }

    let confirmationExample =
        div {
            confirmationDialogConfig |> ConfirmationDialog.trigger "Delete account"
            confirmationDialogConfig |> ConfirmationDialog.render
        }
    let pendingConfirmationExample =
        div {
            pendingConfirmationConfig |> ConfirmationDialog.trigger "Open pending confirmation"
            pendingConfirmationConfig |> ConfirmationDialog.render
        }
    let accountDrawerExample =
        div {
            accountDrawerConfig |> Drawer.trigger "Open account panel"
            accountDrawerConfig |> Drawer.render
        }
    let filterDrawerExample =
        div {
            filterDrawerConfig |> Drawer.trigger "Open filters"
            filterDrawerConfig |> Drawer.render
        }
    let sideNavExample = groupedLedgerNavigation |> SideNav.render shellDestinationUrl
    let pageHeaderExample =
        div {
            _dataSignals "{ledgerRefreshes: 0}"
            accountPageHeader |> PageHeader.render shellDestinationUrl
            output { _class "sr-only"; _role "status"; _dataText "'Balance refreshes: ' + $ledgerRefreshes"; "Balance refreshes: 0" }
        }
    let periodNote =
        Section.withoutHeader "Period note" (p { _class "text-sm text-[var(--fve-muted-text)]"; "Amounts reflect the current accounting period." })
        |> Section.render id

    let private workspacePreview (content:HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"
            div {
                for attribute in ComponentsTheme.attributes (ComponentsTheme.emerald |> ComponentsTheme.withDensity Density.Compact) do attribute
                for attribute in shellDocumentNavigationAttributes do attribute
                div { _class "h-[36rem] overflow-hidden"; content }
            }
        }
    let transactionsPagePreview =
        workspacePreview (renderTransactionsPage shellDestinationUrl (pageTopBar (treasuryBreadcrumbs TreasuryTransactions)) transactionTabs)
    let canvasPagePreview = workspacePreview (canvasPage () |> Page.render shellDestinationUrl)

    [<NoEquality; NoComparison>]
    type ComponentExample =
        { id:string
          title:string
          source:string
          preview:HtmlElement
          note:string option }

    let exampleImports = "open System\nopen FSharp.ViewEngine\nopen FSharp.ViewEngine.Components.Primitives\nopen FSharp.ViewEngine.Components.Application\nopen type Html\nopen type Datastar"

    let private sample id title names preview =
        { id = "components-" + id
          title = title
          source = exampleImports + "\n\n" + SourceRegion.declarations names sourceText.Value
          preview = preview
          note = None }

    let private note message example = { example with note = Some message }

    let private gallerySurface (content:HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"
            div {
                for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
                content
            }
        }

    let private centered (content:HtmlElement) =
        gallerySurface (div { _class "flex min-h-40 flex-wrap items-center justify-center gap-6 p-6"; content })

    let private fieldSurface (content:HtmlElement) =
        gallerySurface (div { _class "mx-auto grid min-h-40 min-w-0 max-w-md content-center gap-4 p-[12px] sm:p-8"; content })

    let browserPrimitiveExample =
        Browser.create (
            div {
                _class "grid min-h-64 content-center gap-3 bg-[var(--fve-surface-subtle)] p-8 text-center"
                strong { _class "text-lg"; "Shipping address" }
                p { _class "text-sm text-[var(--fve-muted-text)]"; "Product content stays an ordinary HtmlElement." }
            })
        |> Browser.withAddress "https://shop.example.test/checkout/shipping"
        |> Browser.withAppMode "browser-primitive" "Shipping address"
        |> Browser.render

    let phonePrimitiveExample =
        Phone.create (
            div {
                _class "grid h-full content-center gap-3 p-6 text-center"
                strong { _class "text-lg"; "Saved offers" }
                p { _class "text-sm text-[var(--fve-muted-text)]"; "The phone frame does not prescribe the product interface." }
            })
        |> Phone.withAppMode "phone-primitive" "Saved offers"
        |> Phone.render

    let private detailsSurface (content:HtmlElement) =
        gallerySurface (div { _class "p-4 sm:p-6"; content })

    let private prose content =
        p {
            _class "spec-paragraph"
            text content
        }

    let private code language source =
        CodeBlock.create language source |> CodeBlock.render

    let private bullets items =
        ul {
            _class "spec-bullets list-disc"
            for item in items do li { text item }
        }

    let private gallery (registration:DocPage) examples =
        DocumentationPage.create registration.id registration.title |> DocumentationPage.withLayout Gallery |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withSections [
            for item in examples do
                DocumentationSection.create item.id item.title (
                    [ Example.gallery item.id item.title "fsharp" item.source item.preview ]
                    @ (item.note |> Option.map prose |> Option.toList)) ]

    let private shellSource = [ "ShellDestination"; "shellDestinationKey"; "shellDestinationUrl" ]
    let private formSource = [ "choiceSubmitButton"; "choiceResult" ]
    let private selectSource = [ "AccountStatus"; "statusValue"; "selectStatusOptions" ]
    let private comboboxSource = [ "accounts"; "accountOptions" ]
    let private sideNavSource = shellSource @ [ "ledgerMark"; "navigationGlyph"; "shellItem"; "ledgerNavigation"; "groupedLedgerNavigation"; "sideNavExample" ]

    let paginationExamples requestedPage =
        [ sample "pagination" "Page navigation" [ "PaginationDestination"; "paginationDestinationUrl"; "paginationPreview" ] (centered (paginationPreviewRegion requestedPage)) ]

    let appShellExamples current =
        [ sample "app-shell" "Sidebar application" (shellSource @ [ "ledgerShellExample"; "treasuryShellExample" ]) (shellFixtureFor current) ]

    let private tableExamples current =
        [ sample "table" "Simple" [ "TeamMember"; "teamMembers"; "teamColumns"; "simpleTeamTable" ] (detailsSurface simpleTeamTable)
          sample "table-comfortable" "Comfortable rows" [ "TeamMember"; "teamMembers"; "teamColumns"; "comfortableTeamTable" ] (detailsSurface comfortableTeamTable)
          sample "table-status" "With status values" [ "memberStatusTable" ] (detailsSurface memberStatusTable)
          sample "table-selection" "With checkboxes" [ "TeamMember"; "teamMembers"; "teamColumns"; "selectableTeamTable" ] (detailsSurface selectableTeamTable)
          |> note "Guest rows are not selectable in this example. Selection covers only the rows on this page."
          sample "table-mobile" "Stacked on mobile" [ "TeamMember"; "teamMembers"; "mobileTeamTable" ] (detailsSurface mobileTeamTable)
          sample "table-sorting" "Sortable records" [ "TeamMember"; "teamMembers"; "TeamMemberSort"; "teamMemberSortUrl"; "documentSort"; "sortFor"; "sortableTeamTable" ] (detailsSurface (sortableTeamTablePreview current))
          |> note "The application owns the query, destination, and ordered rows. Table only renders the accessible sort controls."
          sample "table-empty" "Empty state" [ "TeamMember"; "teamColumns"; "emptyTeamTable" ] (detailsSurface emptyTeamTable) ]

    let examplesFor = function
        | "button" -> [
            sample "button" "Primary buttons" [ "primaryButtons" ] (centered primaryButtons)
            sample "button-secondary" "Secondary buttons" [ "secondaryButtons" ] (centered secondaryButtons)
            sample "button-ghost" "Ghost buttons" [ "ghostButtons" ] (centered ghostButtons)
            sample "button-destructive" "Destructive buttons" [ "destructiveButtons" ] (centered destructiveButtons)
            sample "button-pending" "Pending buttons" [ "pendingSyncButton" ] (centered pendingSyncButton)
            sample "button-disabled" "Disabled buttons" [ "disabledDeleteButton" ] (centered disabledDeleteButton) ]
        | "icon-button" -> [
            sample "icon-button" "Primary icon button" [ "plusIcon"; "addAccountIconButton" ] (centered addAccountIconButton)
            sample "icon-button-secondary" "Secondary icon button" [ "refreshIcon"; "refreshAccountsIconButton" ] (centered refreshAccountsIconButton)
            sample "icon-button-pending" "Pending icon button" [ "refreshIcon"; "refreshingIconButton" ] (centered refreshingIconButton)
            sample "icon-button-disabled" "Disabled icon button" [ "removeIcon"; "disabledRemoveIconButton" ] (centered disabledRemoveIconButton) ]
        | "badge" -> [ sample "badge" "Semantic tones" [ "badgeExample" ] (centered badgeExample) ]
        | "status" -> [ sample "status" "Record status" [ "reviewStatus"; "statusExample" ] (centered statusExample) ]
        | "loading-indicator" -> [
            sample "loading-indicator" "Compact indicator" [ "smallLoadingIndicator" ] (centered smallLoadingIndicator)
            sample "loading-indicator-label" "With visible label" [ "visibleLoadingIndicator" ] (centered visibleLoadingIndicator) ]
        | "empty-state" -> [ sample "empty-state" "With recovery action" [ "plusIcon"; "emptyStateExample" ] (fieldSurface emptyStateExample) ]
        | "table" -> tableExamples NameAscending
        | "description-list" -> [
            sample "description-list" "Three-column details" [ "accountDetails" ] (detailsSurface accountDetails)
            sample "description-list-four-columns" "Four-column details" [ "accountOverview" ] (detailsSurface accountOverview)
            sample "description-list-supporting-text" "With supporting text" [ "accountDetailsWithDescriptions" ] (detailsSurface accountDetailsWithDescriptions) ]
        | "metric" -> [
            sample "metric" "With trend and status" [ "availableBalanceMetric" ] (fieldSurface availableBalanceMetric)
            sample "metric-pending" "With review status" [ "pendingEntriesMetric" ] (fieldSurface pendingEntriesMetric) ]
        | "pagination" -> paginationExamples 2
        | "input" -> [
            sample "input" "With label" [ "labelledInput" ] (fieldSurface labelledInput)
            sample "input-help" "With help text" [ "inputWithHelp" ] (fieldSurface inputWithHelp)
            sample "input-required" "Required" [ "requiredInput" ] (fieldSurface requiredInput)
            sample "input-optional" "Optional" [ "optionalInput" ] (fieldSurface optionalInput)
            sample "input-validation" "With validation error" [ "invalidInput" ] (fieldSurface invalidInput)
            sample "input-icon" "With leading icon" [ "emailIcon"; "inputWithIcon" ] (fieldSurface inputWithIcon)
            sample "input-prefix" "With prefix" [ "inputWithPrefix" ] (fieldSurface inputWithPrefix)
            sample "input-suffix" "With suffix" [ "inputWithSuffix" ] (fieldSurface inputWithSuffix)
            sample "search-input" "Search with clear action" [ "searchInputExample" ] (fieldSurface searchInputExample)
            sample "input-readonly" "Read-only" [ "readonlyInput" ] (fieldSurface readonlyInput)
            sample "input-disabled" "Disabled" [ "disabledInput" ] (fieldSurface disabledInput)
            sample "input-pending" "Pending" [ "pendingInput" ] (fieldSurface pendingInput) ]
        | "form-layouts" -> [
            sample "form-layouts" "Stacked with server validation" [ "choiceSubmitButton"; "ContactDetails"; "ContactFormLayout"; "contactFormRegion"; "emptyContact"; "contactFormExample" ] (fullBleedThemedSurface contactFormExample)
            |> note "Submit to see validation errors. This example does not save contact details. The validation form previously shown on Input now lives here."
            sample "form-layouts-grid" "Two-column form" [ "choiceSubmitButton"; "ContactDetails"; "ContactFormLayout"; "contactFormRegion"; "emptyContact"; "twoColumnFormExample" ] (fullBleedThemedSurface twoColumnFormExample)
            sample "form-layouts-sections" "Sectioned form" [ "choiceSubmitButton"; "ContactDetails"; "ContactFormLayout"; "contactFormRegion"; "emptyContact"; "sectionedFormExample" ] (fullBleedThemedSurface sectionedFormExample)
            sample "form-layouts-search" "Search with results" [ "accountSearchExample" ] (fullBleedThemedSurface accountSearchExample) ]
        | "textarea" -> [
            sample "textarea" "With label" [ "labelledTextarea" ] (fieldSurface labelledTextarea)
            sample "textarea-help" "With instructions" [ "editableInstructions" ] (fieldSurface editableInstructions)
            sample "textarea-validation" "With validation error" [ "invalidTextarea" ] (fieldSurface invalidTextarea)
            sample "textarea-readonly" "Read-only" [ "acceptedNotes" ] (fieldSurface acceptedNotes)
            sample "textarea-pending" "Pending" [ "pendingNotes" ] (fieldSurface pendingNotes)
            sample "textarea-disabled" "Disabled" [ "unavailableNotes" ] (fieldSurface unavailableNotes) ]
        | "error-summary" -> [
            sample "error-summary" "Linked field errors" [ "errorSummaryExample" ] (fullBleedThemedSurface errorSummaryExample)
            |> note "Click the error to focus its field. Your application supplies and clears validation messages." ]
        | "notice" -> [
            sample "notice" "Information" [ "informationNotice" ] (fieldSurface informationNotice)
            sample "notice-success" "Success with download" [ "successNotice" ] (fieldSurface successNotice)
            sample "notice-warning" "Warning" [ "warningNotice" ] (fieldSurface warningNotice)
            sample "notice-critical" "Error with recovery link" [ "criticalNotice" ] (fieldSurface criticalNotice) ]
        | "select" -> [
            sample "select" "With label" [ "basicSelect" ] (fieldSurface basicSelect)
            sample "select-help" "With help text" [ "selectWithHelp" ] (fieldSurface selectWithHelp)
            sample "select-validation" "Required selection with validation" (formSource @ selectSource @ [ "selectFormRegion"; "statusSelect" ]) (fieldSurface statusSelect)
            sample "select-disabled" "Disabled" (selectSource @ [ "disabledStatusSelect" ]) (fieldSurface disabledStatusSelect)
            sample "select-pending" "Pending" (selectSource @ [ "pendingStatusSelect" ]) (fieldSurface pendingStatusSelect)
            sample "select-multiple" "Multiple selection" [ "memberOptions"; "multipleMembersSelect" ] (fieldSurface multipleMembersSelect)
            sample "select-multiple-selected" "With several selected" [ "memberOptions"; "selectedMembersSelect" ] (fieldSurface selectedMembersSelect)
            sample "select-multiple-validation" "Multiple selection with validation" (formSource @ [ "memberOptions"; "multipleSelectForm" ]) (fieldSurface (multipleSelectForm [] None None))
            |> note "Submit to check the selection. This example does not save your data."
            sample "select-multiple-disabled" "Multiple selection, disabled" [ "memberOptions"; "disabledMembersSelect" ] (fieldSurface disabledMembersSelect)
            sample "select-multiple-pending" "Multiple selection, pending" [ "memberOptions"; "pendingMembersSelect" ] (fieldSurface pendingMembersSelect)
            sample "select-search" "Searchable" (comboboxSource @ [ "staticAccountCombobox" ]) (fieldSurface staticAccountCombobox)
            sample "select-search-remote" "Searchable with remote results" (comboboxSource @ [ "accountComboboxConfig"; "accountCombobox"; "accountComboboxOptions" ]) (fieldSurface accountCombobox)
            |> note "Search Operating for a result, an unknown name for no matches, or error to try failure and retry."
            sample "select-search-validation" "Searchable with validation" (comboboxSource @ [ "validationAccountCombobox" ]) (fieldSurface validationAccountCombobox)
            sample "select-search-loading" "Searchable, loading" [ "loadingAccountCombobox" ] (fieldSurface loadingAccountCombobox)
            sample "select-search-disabled" "Searchable, disabled" (comboboxSource @ [ "disabledAccountCombobox" ]) (fieldSurface disabledAccountCombobox)
            sample "select-search-pending" "Searchable, pending" (comboboxSource @ [ "pendingAccountCombobox" ]) (fieldSurface pendingAccountCombobox)
            sample "select-search-multiple" "Searchable multiple selection" [ "memberOptions"; "multipleMembersCombobox" ] (fieldSurface multipleMembersCombobox)
            sample "select-search-multiple-remote" "Searchable multiple selection with remote results" [ "memberOptions"; "remoteMembersConfig"; "remoteMembersCombobox"; "searchMemberOptions"; "remoteMembersField"; "remoteMembersOptions" ] (fieldSurface remoteMembersCombobox)
            |> note "Search for Jamie or Riley, an unknown name for no matches, or error to try recovery."
            sample "select-search-multiple-validation" "Searchable multiple selection with validation" (formSource @ [ "memberOptions"; "multipleComboboxForm" ]) (fieldSurface (multipleComboboxForm [] None None))
            |> note "Submit to check the selection. This example does not save your data."
            sample "select-search-multiple-loading" "Searchable multiple selection, loading" [ "memberOptions"; "loadingMembersCombobox" ] (fieldSurface loadingMembersCombobox)
            sample "select-search-multiple-disabled" "Searchable multiple selection, disabled" [ "memberOptions"; "disabledMembersCombobox" ] (fieldSurface disabledMembersCombobox)
            sample "select-search-multiple-pending" "Searchable multiple selection, pending" [ "memberOptions"; "pendingMembersCombobox" ] (fieldSurface pendingMembersCombobox) ]
        | "checkbox" -> [
            sample "checkbox" "With label" [ "basicCheckbox" ] (fieldSurface basicCheckbox)
            sample "checkbox-help" "With help text" [ "checkboxWithHelp" ] (fieldSurface checkboxWithHelp)
            sample "checkbox-validation" "Required confirmation with validation" (formSource @ [ "checkboxFormRegion" ]) (fieldSurface (checkboxFormRegion false None None))
            sample "checkbox-checked" "Checked" [ "includeArchived" ] (fieldSurface includeArchived)
            sample "checkbox-pending" "Pending" [ "pendingArchivedReview" ] (fieldSurface pendingArchivedReview)
            sample "checkbox-disabled" "Disabled" [ "disabledArchivedReview" ] (fieldSurface disabledArchivedReview) ]
        | "switch" -> [
            sample "switch" "With label" [ "basicSwitch" ] (fieldSurface basicSwitch)
            sample "switch-help" "With help text" [ "switchWithHelp" ] (fieldSurface switchWithHelp)
            sample "switch-submission" "Setting with submission" (formSource @ [ "switchFormRegion"; "postingNotifications" ]) (fieldSurface postingNotifications)
            sample "switch-pending" "Pending" [ "pendingNotifications" ] (fieldSurface pendingNotifications)
            sample "switch-validation" "Validation error" [ "invalidNotifications" ] (fieldSurface invalidNotifications) ]
        | "toggle-button" -> [
            sample "toggle-button" "Pressed state" [ "compactRows" ] (centered compactRows)
            sample "toggle-button-pending" "Pending" [ "pendingCompactRows" ] (centered pendingCompactRows)
            sample "toggle-button-disabled" "Disabled" [ "disabledCompactRows" ] (centered disabledCompactRows) ]
        | "tabs" -> [
            sample "tabs" "Segmented tabs" [ "codePreviewTabs" ] (fieldSurface codePreviewTabs)
            sample "tabs-underlined" "Underlined tabs with refresh" [ "reviewTabsRegion" ] (fieldSurface (reviewTabsRegion false)) ]
        | "radio-group" -> [
            sample "radio-group" "With label" [ "basicRadioGroup" ] (fieldSurface basicRadioGroup)
            sample "radio-group-help" "With help text" [ "radioGroupWithHelp" ] (fieldSurface radioGroupWithHelp)
            sample "radio-group-validation" "Required choice with validation" (formSource @ [ "postingModeOptions"; "radioGroupFormRegion"; "postingMode" ]) (fieldSurface postingMode)
            sample "radio-group-pending" "Pending" [ "postingModeOptions"; "pendingPostingMode" ] (fieldSurface pendingPostingMode)
            sample "radio-group-disabled" "Disabled" [ "postingModeOptions"; "disabledPostingMode" ] (fieldSurface disabledPostingMode) ]
        | "dropdown-menu" -> [
            sample "dropdown-menu" "Actions and destinations" [ "Destination"; "destinationUrl"; "menuLeadingIcon"; "dropdownMenuItems"; "actionMenu"; "moreActionsMenu"; "dropdownMenuRegion" ] (fieldSurface (dropdownMenuRegion false))
            |> note "Counters demonstrate activation; no records are deleted. Refresh actions fetches new menu content." ]
        | "dialog" -> [ sample "dialog" "Modal dialog" [ "dialogConfig"; "reviewDialogTrigger"; "reviewDialog" ] (centered (div { reviewDialogTrigger; reviewDialog })) ]
        | "confirmation-dialog" -> [
            sample "confirmation-dialog" "Server-validated confirmation" [ "confirmationDialogConfig"; "confirmationDialogContent"; "confirmationExample" ] (centered confirmationExample)
            |> note "The demo server rejects deletion so you can inspect its validation state."
            sample "confirmation-dialog-pending" "Pending confirmation" [ "pendingConfirmationConfig"; "pendingConfirmationExample" ] (centered pendingConfirmationExample) ]
        | "drawer" -> [
            sample "drawer" "End-side drawer" [ "accountDrawerContent"; "accountDrawerConfig"; "accountDrawerExample" ] (centered accountDrawerExample)
            sample "drawer-start" "Start-side drawer" [ "filterDrawerConfig"; "filterDrawerExample" ] (centered filterDrawerExample) ]
        | "breadcrumbs" -> [ sample "breadcrumbs" "Linked ancestors" [ "breadcrumbsExample" ] breadcrumbsPreview ]
        | "side-nav" -> [ sample "side-nav" "Grouped navigation" sideNavSource sideNavigationPreview ]
        | "page-top-bar" -> [ sample "page-top-bar" "With breadcrumbs" [ "pageTopBarExample"; "renderPageTopBar" ] pageTopBarPreview ]
        | "page-header" -> [ sample "page-header" "Title and page actions" (shellSource @ [ "refreshIcon"; "refreshBalancesAction"; "pageAction"; "accountPageHeader"; "pageHeaderExample" ]) pageHeaderPreview ]
        | "section" -> [
            sample "section" "With heading and actions" (shellSource @ [ "upcomingPayments" ]) (fieldSurface upcomingPayments)
            sample "section-headerless" "Without a visible heading" [ "periodNote" ] (fieldSurface periodNote) ]
        | "browser" -> [
            sample "browser" "Address bar and optional App mode" [ "browserPrimitiveExample" ] (detailsSurface browserPrimitiveExample)
            |> note "Serve app-mode.js at the URL passed to Browser.script once in the host document head." ]
        | "phone" -> [
            sample "phone" "Device frame and optional App mode" [ "phonePrimitiveExample" ] (centered phonePrimitiveExample)
            |> note "Phone owns the device treatment; the screen remains product-owned HTML." ]
        | "page" -> [
            sample "page" "With local navigation" [ "transactionsPage"; "renderTransactionsPage" ] transactionsPagePreview
            sample "page-canvas" "Remaining-height canvas" [ "canvasPage" ] canvasPagePreview ]
        | "collection" -> [ sample "collection" "Collection with record actions" [ "collectionExample" ] collectionPreview ]
        | "detail" -> [ sample "detail" "Detail with related records" [ "detailExample" ] detailPreview ]
        | "app-shell" -> appShellExamples LedgerAccounts
        | id -> invalidArg (nameof id) $"No component examples registered for '{id}'."

    let allExamples () =
        allRegistrations
        |> List.filter (fun page -> page.path.StartsWith("/components/", StringComparison.Ordinal) && page.path <> "/components/installation" && not (guideRegistrations |> List.exists (fun guide -> guide.path = page.path)))
        |> List.collect (fun page -> examplesFor (page.path.Substring("/components/".Length)))

    let installationPage =
        DocumentationPage.create installationRegistration.id installationRegistration.title |> DocumentationPage.withDescription "Install the package, import its Tailwind source manifest, and open the Components namespace." |> DocumentationPage.withSections [
            DocumentationSection.create "package" "Add the package" [
                code "shell" "dotnet add package FSharp.ViewEngine.Components"
                prose "Components versions independently and declares its minimum compatible FSharp.ViewEngine dependency." ]
            DocumentationSection.create "tailwind" "Configure Tailwind CSS" [
                prose "The NuGet package includes FSharp.ViewEngine.Components.tailwind.css under contentFiles/any/any. Copy the manifest into the application CSS source tree and import it after Tailwind CSS."
                code "css" tailwindExample
                p { _class "spec-paragraph"; "See "; a { _href "/components/tailwind-css"; "Tailwind CSS setup" }; " for source detection and semantic theme details." } ]
            DocumentationSection.create "namespace" "Open the namespace" [
                code "fsharp" "open FSharp.ViewEngine\nopen FSharp.ViewEngine.Components.Primitives\nopen FSharp.ViewEngine.Components.Application\nopen type Html"
                prose "Components are ordinary F# values and functions that compose with the existing HtmlElement builders." ] ]

    let buttonPage = gallery buttonRegistration (examplesFor "button")
    let iconButtonPage = gallery iconButtonRegistration (examplesFor "icon-button")
    let badgePage = gallery badgeRegistration (examplesFor "badge")
    let statusPage = gallery statusRegistration (examplesFor "status")
    let loadingIndicatorPage = gallery loadingIndicatorRegistration (examplesFor "loading-indicator")
    let emptyStatePage = gallery emptyStateRegistration (examplesFor "empty-state")
    let tablePageFor sort = gallery tableRegistration (tableExamples sort)
    let tablePage = tablePageFor NameAscending
    let descriptionListPage = gallery descriptionListRegistration (examplesFor "description-list")
    let metricPage = gallery metricRegistration (examplesFor "metric")
    let inputPage = gallery inputRegistration (examplesFor "input")
    let formLayoutsPage = gallery formLayoutsRegistration (examplesFor "form-layouts")
    let textareaPage = gallery textareaRegistration (examplesFor "textarea")
    let errorSummaryPage = gallery errorSummaryRegistration (examplesFor "error-summary")
    let noticePage = gallery noticeRegistration (examplesFor "notice")
    let selectPage = gallery selectRegistration (examplesFor "select")
    let checkboxPage = gallery checkboxRegistration (examplesFor "checkbox")
    let switchPage = gallery switchRegistration (examplesFor "switch")
    let toggleButtonPage = gallery toggleButtonRegistration (examplesFor "toggle-button")
    let breadcrumbsPage = gallery breadcrumbsRegistration (examplesFor "breadcrumbs")
    let sideNavPage = gallery sideNavRegistration (examplesFor "side-nav")
    let tabsPage = gallery tabsRegistration (examplesFor "tabs")
    let radioGroupPage = gallery radioGroupRegistration (examplesFor "radio-group")
    let dropdownMenuPage = gallery dropdownMenuRegistration (examplesFor "dropdown-menu")
    let dialogPage = gallery dialogRegistration (examplesFor "dialog")
    let confirmationDialogPage = gallery confirmationDialogRegistration (examplesFor "confirmation-dialog")
    let drawerPage = gallery drawerRegistration (examplesFor "drawer")
    let pageTopBarPage = gallery pageTopBarRegistration (examplesFor "page-top-bar")
    let pageHeaderPage = gallery pageHeaderRegistration (examplesFor "page-header")
    let sectionPage = gallery sectionRegistration (examplesFor "section")
    let browserPage = gallery browserRegistration (examplesFor "browser")
    let phonePage = gallery phoneRegistration (examplesFor "phone")
    let pagePage = gallery pageRegistration (examplesFor "page")
    let collectionPageDocumentation = gallery collectionRegistration (examplesFor "collection")
    let detailPageDocumentation = gallery detailRegistration (examplesFor "detail")

    let paginationPageFor requestedPage = gallery paginationRegistration (paginationExamples requestedPage)
    let paginationPage = paginationPageFor 2
    let appShellPageFor current = gallery appShellRegistration (appShellExamples current)
    let appShellPage = appShellPageFor LedgerAccounts

    let interactionPage =
        DocumentationPage.create interactionRegistration.id interactionRegistration.title |> DocumentationPage.withDescription "Keep ephemeral interaction local while applications retain authoritative, durable, and security-sensitive state." |> DocumentationPage.withSections [
            DocumentationSection.create "datastar" "Datastar interaction" [
                prose "Datastar is the Components interaction model. Sparse local signals hold ephemeral state such as whether a menu is open or which option is active. Selected form values and editable queries are submitted intentionally."
                prose "Treat Datastar expressions and endpoints as trusted application code and never interpolate untrusted content into executable expressions." ]
            DocumentationSection.create "server" "Server-owned state" [
                prose "Applications continue to own authoritative options, routes, permissions, validation, persistence, actions, and error handling. Remote Combobox results return only the stable options morph region." ]
            DocumentationSection.create "boundaries" "Application boundaries" [ bullets [ "Keep product routes, authorization, domain formatting, query behavior, and durable state in the application."; "Keep table querying, sorting, filtering, pagination, chart data, and drawing application-owned."; "Use Datastar rather than adding a parallel Alpine or client-side component runtime." ] ] ]

    let accessibilityPage =
        DocumentationPage.create accessibilityRegistration.id accessibilityRegistration.title |> DocumentationPage.withDescription "Understand the semantic, keyboard, focus, label, and customization guarantees shared by Components." |> DocumentationPage.withSections [
            DocumentationSection.create "semantics" "Distinct semantics" [ prose "Select, Combobox, Checkbox, Switch, ToggleButton, Tabs, RadioGroup, DropdownMenu, Dialog, and AppShell navigation retain the roles and keyboard models appropriate to each interaction rather than sharing one generic choice control." ]
            DocumentationSection.create "focus" "Focus and active options" [ prose "Select and Combobox keep DOM focus on the combobox while aria-activedescendant identifies the visually active option. Select typeahead buffers rapid characters for prefix matching and cycles options when the same character is repeated." ]
            DocumentationSection.create "labels" "Required labels" [ prose "Accessible labels are required where visible content cannot provide them. Compact layouts use typed visually hidden labels rather than omitting the accessible name." ]
            DocumentationSection.create "protected-attributes" "Protected behavior" [ prose "Package-owned structure, form attributes, ARIA relationships, Datastar bindings, and base classes cannot be replaced through generic customization. Interactive components support pointer and keyboard operation, visible focus, disabled and pending states, multiple instances, and representative morphs." ] ]

    let themingPage =
        DocumentationPage.create themingRegistration.id themingRegistration.title |> DocumentationPage.withDescription "Apply semantic color, radius, density, and shell geometry consistently across a Components subtree or AppShell." |> DocumentationPage.withSections [
            DocumentationSection.create "theme" "Apply a theme" [ prose "Components consume semantic variables for page, surface, text, border, navigation, brand, positive, warning, critical, and informative roles. Variants such as Primary and Positive select roles rather than palette shades."; code "fsharp" themeExample ]
            DocumentationSection.create "modes" "Light and dark modes" [ prose "Built-in sky, emerald, amber, cyan, and neutral themes coordinate default, selected, hover, focus, and navigation colors in light and dark modes. Radius and density settings apply consistently across controls and navigation." ]
            DocumentationSection.create "shell" "Shell policy" [ prose "SideNav width, mobile breakpoint, and viewport or embedded container boundary are typed application choices. PageTopBar and SideNavHeader share a semantic minimum-height token; compact density keeps medium controls approximately 32 pixels high." ]
            DocumentationSection.create "brand" "Product branding" [ prose "Override documented semantic variables in an application theme when product branding requires it. Keep component APIs semantic rather than passing raw palette strings." ] ]

    let tailwindPage =
        DocumentationPage.create tailwindRegistration.id tailwindRegistration.title |> DocumentationPage.withDescription "Generate every package-owned utility from the explicit Tailwind v4 source manifest." |> DocumentationPage.withSections [
            DocumentationSection.create "manifest" "Package source manifest" [ prose "The NuGet package includes FSharp.ViewEngine.Components.tailwind.css under contentFiles/any/any. Copy it into the application CSS source tree and import it after Tailwind CSS."; code "css" tailwindExample ]
            DocumentationSection.create "source-detection" "Source detection" [ prose "Utility classes inside compiled assemblies are not discovered automatically. The explicit Tailwind v4 source manifest lists complete package-owned utility names so Tailwind can emit styles without assembly scanning, consumer call-site scanning, or dynamic class construction." ] ]

    let customizationPage =
        DocumentationPage.create customizationRegistration.id customizationRegistration.title |> DocumentationPage.withDescription "Extend presentation and application-owned slots without replacing component structure or behavior." |> DocumentationPage.withSections [
            DocumentationSection.create "escape-hatches" "Escape hatches" [ prose "Use withAttributes, withClass, and named HtmlElement slots where a component exposes them. Renderers retain structural, form, ARIA, Datastar, and base class attributes so customization cannot duplicate or remove required behavior." ]
            DocumentationSection.create "application-inputs" "Application inputs" [ prose "Applications provide destination resolvers, form-value encoders, trusted Datastar expressions, custom cells, dialog bodies, toolbars, actions, and page content. Submitted values still require server validation." ]
            DocumentationSection.create "native-controls" "Native controls" [ prose "Render browser-native controls directly with the FSharp.ViewEngine DSL when native presentation is intentional. There is no parallel NativeSelect API or separate component markup language." ] ]

    let versioningPage =
        DocumentationPage.create versioningRegistration.id versioningRegistration.title |> DocumentationPage.withDescription "Upgrade Components independently while honoring its declared minimum compatible Core version." |> DocumentationPage.withSections [
            DocumentationSection.create "independent" "Independent releases" [ prose "FSharp.ViewEngine.Components versions independently using Components-specific calendar versions and repository tags. Each release declares its minimum compatible FSharp.ViewEngine version." ]
            DocumentationSection.create "compatibility" "Compatibility" [ prose "Additive modifiers and union cases receive compatibility review. Breaking API changes require a new Components version and migration guidance rather than compatibility wrappers in Core or Docs." ] ]

    let private pages =
        [ installationRegistration.path, installationPage
          buttonRegistration.path, buttonPage
          iconButtonRegistration.path, iconButtonPage
          badgeRegistration.path, badgePage
          statusRegistration.path, statusPage
          loadingIndicatorRegistration.path, loadingIndicatorPage
          emptyStateRegistration.path, emptyStatePage
          tableRegistration.path, tablePage
          descriptionListRegistration.path, descriptionListPage
          metricRegistration.path, metricPage
          paginationRegistration.path, paginationPage
          inputRegistration.path, inputPage
          formLayoutsRegistration.path, formLayoutsPage
          textareaRegistration.path, textareaPage
          errorSummaryRegistration.path, errorSummaryPage
          noticeRegistration.path, noticePage
          selectRegistration.path, selectPage
          checkboxRegistration.path, checkboxPage
          switchRegistration.path, switchPage
          toggleButtonRegistration.path, toggleButtonPage
          breadcrumbsRegistration.path, breadcrumbsPage
          sideNavRegistration.path, sideNavPage
          tabsRegistration.path, tabsPage
          radioGroupRegistration.path, radioGroupPage
          dropdownMenuRegistration.path, dropdownMenuPage
          dialogRegistration.path, dialogPage
          confirmationDialogRegistration.path, confirmationDialogPage
          drawerRegistration.path, drawerPage
          pageTopBarRegistration.path, pageTopBarPage
          pageHeaderRegistration.path, pageHeaderPage
          sectionRegistration.path, sectionPage
          browserRegistration.path, browserPage
          phoneRegistration.path, phonePage
          pageRegistration.path, pagePage
          collectionRegistration.path, collectionPageDocumentation
          detailRegistration.path, detailPageDocumentation
          appShellRegistration.path, appShellPage
          interactionRegistration.path, interactionPage
          accessibilityRegistration.path, accessibilityPage
          themingRegistration.path, themingPage
          tailwindRegistration.path, tailwindPage
          customizationRegistration.path, customizationPage
          versioningRegistration.path, versioningPage ]
        |> Map.ofList

    let tryPage path = Map.tryFind path pages
