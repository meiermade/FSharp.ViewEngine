namespace Docs.Pages

open System
open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
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

    type AccountWorkspace =
        { workspaceName:string
          currency:string
          emailUpdates:bool
          draftName:string
          draftType:string
          feedback:string
          searchQuery:string
          filterType:string
          sortColumn:string
          sortDirection:string
          page:int
          visibleColumns:Set<string> }

    let defaultAccountWorkspace =
        { workspaceName="Meier Made"; currency="USD"; emailUpdates=true; draftName=""; draftType="Asset"; feedback=""; searchQuery=""; filterType="all"; sortColumn="name"; sortDirection="asc"; page=1; visibleColumns=set [ "type"; "commodity"; "balance"; "total" ] }

    type HierarchyAccount =
        { key:string
          ancestors:string list
          level:int
          name:string
          accountType:string
          total:decimal
          hasChildren:bool }

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
        | Accounts -> "/components/page-examples/account-management?destination=ledger-accounts"
        | AccountsPage page -> $"/components/page-examples/account-management?destination=ledger-accounts&page={page}"
        | Account id -> $"/components/page-examples/account-management?destination=ledger-account-{id}"
        | Settings -> "/components/page-examples/account-management?destination=ledger-settings"
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
        $"/components/page-examples/account-management?destination={shellDestinationKey destination}"

    let private shellDestinationLink =
        "evt.target.closest('a[href^=\"/components/page-examples/account-management?destination=\"]')"

    let private galleryDestinationAttributes =
        [ _attr ("data-fve-gallery-contained", "true")
          _dataOn ("click", "const link = evt.target.closest?.('a[href]'); if (link && !link.hash && !link.hasAttribute('download') && !link.hasAttribute('data-fve-app-mode-launch') && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) evt.preventDefault()") ]

    let private moreActionsIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5" aria-hidden="true"><path d="M3.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM8.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM13.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0Z"/></svg>"""

    let private shellFixtureNavigationAttributes =
        let link = shellDestinationLink
        [ _dataOn ("click", $"if ({link} && !{link}.hasAttribute('data-fve-app-mode-launch') && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) {{ evt.preventDefault(); window.history.pushState(null, '', {link}.getAttribute('href')); @get({link}.getAttribute('href').replace('/components/page-examples/account-management?', '/components/page-examples/account-management/fixture?')) }}") ]

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.Components.fs")

    let private themedPreview (content:HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"
            for attribute in galleryDestinationAttributes do attribute
            content
        }

    let private themedSurface (content:HtmlElement) =
        div {
            for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
            div {
                _class "bg-[var(--fve-background)] p-6 text-[var(--fve-text)]"
                content
            }
        }
        |> themedPreview

    let private fullBleedThemedSurface (content:HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"
            for attribute in galleryDestinationAttributes do attribute
            div {
                for attribute in ComponentsTheme.attributes (ComponentsTheme.sky |> ComponentsTheme.withDensity Density.Compact) do attribute
                content
            }
        }

    let pendingSyncButton =
        Button.create (ButtonContent.Text "Sync accounts")
        |> Button.withColor ButtonColor.Primary
        |> Button.withVariant ButtonVariant.Solid
        |> Button.pending
        |> Button.render

    let disabledDeleteButton =
        Button.create (ButtonContent.Text "Delete account")
        |> Button.withColor ButtonColor.Error
        |> Button.withVariant ButtonVariant.Solid
        |> Button.disabled
        |> Button.render

    let private plusIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path d="M10 4.25a.75.75 0 0 1 .75.75v4.25H15a.75.75 0 0 1 0 1.5h-4.25V15a.75.75 0 0 1-1.5 0v-4.25H5a.75.75 0 0 1 0-1.5h4.25V5a.75.75 0 0 1 .75-.75Z"/></svg>"""

    let private refreshIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M15.312 4.683A7.25 7.25 0 1 0 17.25 10a.75.75 0 0 0-1.5 0 5.75 5.75 0 1 1-1.604-3.982H12.5a.75.75 0 0 0 0 1.5h3.5a.75.75 0 0 0 .75-.75v-3.5a.75.75 0 0 0-1.5 0v1.415Z" clip-rule="evenodd"/></svg>"""

    let private removeIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M8.75 3.5a1.25 1.25 0 0 1 2.5 0V4h3a.75.75 0 0 1 0 1.5h-.44l-.55 9.08A2.25 2.25 0 0 1 11.02 16.7H8.98a2.25 2.25 0 0 1-2.24-2.12L6.19 5.5h-.44a.75.75 0 0 1 0-1.5h3v-.5Zm-1.06 2 .54 8.99a.75.75 0 0 0 .75.71h2.04a.75.75 0 0 0 .75-.71l.54-8.99H7.69Z" clip-rule="evenodd"/></svg>"""

    let addAccountIconOnlyButton =
        Button.create (ButtonContent.Icon ("Add account", plusIcon))
        |> Button.withColor ButtonColor.Primary
        |> Button.withVariant ButtonVariant.Solid
        |> Button.render

    let refreshingIconOnlyButton =
        Button.create (ButtonContent.Icon ("Refreshing accounts", refreshIcon))
        |> Button.withVariant ButtonVariant.Ghost
        |> Button.pending
        |> Button.render

    let disabledRemoveIconOnlyButton =
        Button.create (ButtonContent.Icon ("Remove account", removeIcon))
        |> Button.withColor ButtonColor.Error
        |> Button.withVariant ButtonVariant.Solid
        |> Button.disabled
        |> Button.render

    let private backIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M11.53 4.47a.75.75 0 0 1 0 1.06L7.06 10l4.47 4.47a.75.75 0 1 1-1.06 1.06l-5-5a.75.75 0 0 1 0-1.06l5-5a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>"""

    let defaultButtonGroup =
        ButtonGroup.create "Message actions" [
            Button.create (ButtonContent.Text "Archive") |> ButtonGroupItem.button
            Button.create (ButtonContent.Text "Report") |> ButtonGroupItem.button ]
        |> ButtonGroup.render id

    let buttonGroupExample =
        let menu =
            DropdownMenu.create "button-group-snooze-menu" "More scheduling actions"
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon moreActionsIcon |> DropdownMenuTrigger.withAttributes [ _ariaLabel "More scheduling actions" ])
            |> DropdownMenu.withContent [
                DropdownMenuItem.action "$snoozeUntil = 'tomorrow'" "Snooze until tomorrow"
                DropdownMenuItem.action "$snoozeUntil = 'next-week'" "Snooze until next week" ]
        div {
            _class "flex min-w-0 max-w-full flex-wrap items-center gap-2"
            ButtonGroup.create "Back" [
                Button.create (ButtonContent.Icon ("Back", backIcon)) |> ButtonGroupItem.button ]
            |> ButtonGroup.render id
            ButtonGroup.create "Message actions" [
                Button.create (ButtonContent.Text "Archive") |> ButtonGroupItem.button
                Button.create (ButtonContent.Text "Report") |> ButtonGroupItem.button ]
            |> ButtonGroup.render id
            ButtonGroup.create "Scheduling actions" [
                Button.create (ButtonContent.Text "Snooze") |> ButtonGroupItem.button
                menu |> ButtonGroupItem.menu ]
            |> ButtonGroup.render id
        }

    let verticalButtonGroup =
        ButtonGroup.create "Record view" [
            Button.create (ButtonContent.Text "Summary") |> ButtonGroupItem.button
            Button.create (ButtonContent.Text "Activity") |> ButtonGroupItem.button
            Button.create (ButtonContent.Text "Files") |> ButtonGroupItem.button ]
        |> ButtonGroup.withOrientation ButtonGroupOrientation.Vertical
        |> ButtonGroup.render id

    let badgeExample = Badge.create "Internal" |> Badge.render
    let badgeColorExamples =
        div {
            _class "flex flex-wrap items-center gap-3"
            for label, color in [
                "Primary", BadgeColor.Primary; "Secondary", BadgeColor.Secondary
                "Success", BadgeColor.Success; "Warning", BadgeColor.Warning
                "Error", BadgeColor.Error; "Info", BadgeColor.Info; "Neutral", BadgeColor.Neutral
            ] do Badge.create label |> Badge.withColor color |> Badge.render
        }

    let badgeVariantExamples =
        div {
            _class "flex flex-wrap items-center gap-3"
            for label, variant in [ "Solid", BadgeVariant.Solid; "Soft", BadgeVariant.Soft; "Outline", BadgeVariant.Outline; "Ghost", BadgeVariant.Ghost ] do
                Badge.create label |> Badge.withColor BadgeColor.Success |> Badge.withVariant variant |> Badge.render
        }

    let reviewStatus = Badge.create "Needs review" |> Badge.withColor BadgeColor.Warning |> Badge.render

    let keyboardKey = Kbd.create "Esc" |> Kbd.render
    let keyboardShortcut = Kbd.shortcut [ "⌘"; "K" ] |> Kbd.withLabel "Open search" |> Kbd.render

    let defaultSeparator =
        div { _class "w-full"; Separator.create () |> Separator.render }

    let separatorExamples =
        div {
            _class "grid gap-5"
            div { p { _class "mb-3 text-sm font-medium"; "Account details" }; Separator.create () |> Separator.semantic |> Separator.render; p { _class "mt-3 text-sm text-[var(--fve-muted-text)]"; "Contact and billing information" } }
            div { _class "flex h-8 items-center gap-4"; span { "Profile" }; Separator.create () |> Separator.withOrientation SeparatorOrientation.Vertical |> Separator.render; span { "Security" } }
        }

    let defaultSkeleton =
        SkeletonRegion.create "Loading content" (Skeleton.create () |> Skeleton.render)
        |> SkeletonRegion.render

    let textSkeleton =
        SkeletonRegion.create "Loading description" (
            div {
                _class "grid min-w-0 gap-2"
                Skeleton.create () |> Skeleton.render
                Skeleton.create () |> Skeleton.render
                Skeleton.create () |> Skeleton.withSize "75%" "1rem" |> Skeleton.render
            })
        |> SkeletonRegion.render

    let avatarSkeleton =
        SkeletonRegion.create "Loading profile" (
            div {
                _class "flex min-w-0 items-center gap-4"
                Skeleton.create () |> Skeleton.withShape SkeletonShape.Circle |> Skeleton.render
                div {
                    _class "grid min-w-0 flex-1 gap-2"
                    Skeleton.create () |> Skeleton.render
                    Skeleton.create () |> Skeleton.withSize "75%" "1rem" |> Skeleton.render
                }
            })
        |> SkeletonRegion.render

    let cardSkeleton =
        SkeletonRegion.create "Loading card" (
            div {
                _class "grid min-w-0 gap-4"
                Skeleton.create () |> Skeleton.withShape SkeletonShape.Rectangle |> Skeleton.withSize "100%" "9rem" |> Skeleton.render
                div {
                    _class "grid min-w-0 gap-2"
                    Skeleton.create () |> Skeleton.withSize "70%" "1rem" |> Skeleton.render
                    Skeleton.create () |> Skeleton.withSize "45%" "1rem" |> Skeleton.render
                }
            })
        |> SkeletonRegion.render

    let defaultItem =
        ul { Item.create "Quarterly close" |> Item.render }

    let itemExample =
        ul {
            _class "grid max-w-xl gap-2"
            Item.create "Quarterly close"
            |> Item.withDescription (p { "Review reconciliation notes and unresolved balances." })
            |> Item.withMetadata (time { _datetime "2026-09-30"; "Sep 30" })
            |> Item.withVariant ItemVariant.Outlined
            |> Item.withLink "/components/page-examples/account-management"
            |> Item.render
            Item.create "Export package"
            |> Item.withDescription (p { "Prepared for the finance review." })
            |> Item.withMetadata (Badge.create "Ready" |> Badge.withColor BadgeColor.Success |> Badge.render)
            |> Item.withActions (Button.create (ButtonContent.Text "Download") |> Button.withVariant ButtonVariant.Ghost |> Button.render)
            |> Item.withVariant ItemVariant.Muted
            |> Item.render
        }

    let defaultLoadingIndicator =
        LoadingIndicator.create "Loading account balances"
        |> LoadingIndicator.render
    let visibleLoadingIndicator =
        LoadingIndicator.create "Refreshing transactions"
        |> LoadingIndicator.withSize ControlSize.Large
        |> LoadingIndicator.withVisibleLabel
        |> LoadingIndicator.render
    let defaultEmptyState =
        EmptyState.create "No accounts yet" "Create an account to start tracking balances and entries."
        |> EmptyState.render

    let emptyStateExample =
        EmptyState.create "No accounts yet" "Create an account to start tracking balances and entries."
        |> EmptyState.withIcon plusIcon
        |> EmptyState.withActions (
            a {
                _href "/components/page-examples/account-management?destination=ledger-create-account"
                _class "inline-flex items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] px-3 py-2 text-sm font-medium text-white hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
                "Create account"
            })
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

    let accountNameExists name =
        operatingAccount :: rows
        |> List.exists (fun row -> String.Equals(row.name, name, StringComparison.OrdinalIgnoreCase))

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
            DropdownMenuItem.action
                $"{feedback} = ''; if (navigator.clipboard?.writeText) {{ navigator.clipboard.writeText({expression}).then(() => {{ {feedback} = {success} }}).catch(() => {{ {feedback} = {failure} }}) }} else {{ {feedback} = {failure} }}"
                $"Copy {label}"
        let url = resolve destination |> json
        let download =
            $"const url = URL.createObjectURL(new Blob([{json payload}], {{type: 'application/json'}})); const link = document.createElement('a'); link.href = url; link.download = '{kind}-{id}.json'; document.body.append(link); link.click(); link.remove(); URL.revokeObjectURL(url); {feedback} = 'Downloaded {kind}.'"
        [ DropdownMenuItem.link destination $"View {kind}"
          copy $"{kind} ID" (json (string id))
          copy $"{kind} link" $"new URL({url}, window.location.href).href"
          DropdownMenuItem.separator
          DropdownMenuItem.action download $"Download {kind}" ]

    let private accountTableConfigFor accountRows destinationFor resolve rowAttributes =
        Table.create "Accounts" [
            TableColumn.create "Account" (fun (row:AccountRow) ->
                a { _href (resolve (destinationFor row.id)); _class "font-medium text-[var(--fve-brand-text)]"; row.name })
            |> TableColumn.asRowHeader
            |> TableColumn.asMobilePrimary
            TableColumn.create "Type" (fun (row:AccountRow) -> text row.accountType)
            TableColumn.create "Commodity" (fun (row:AccountRow) -> text row.commodity)
            TableColumn.create "Balance" (fun (row:AccountRow) -> text (money row.balance))
            |> TableColumn.alignEnd
            TableColumn.create "Total balance (USD) · Current" (fun (row:AccountRow) -> text (money row.totalBalance))
            |> TableColumn.alignEnd
            TableColumn.rowActions (fun (row:AccountRow) ->
                DropdownMenu.create $"account-{row.id}-actions" $"More actions for {row.name}"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon moreActionsIcon)
                |> DropdownMenu.withContent (recordMenuItems "account" row.id (destinationFor row.id) resolve (System.Text.Json.JsonSerializer.Serialize row))
                |> DropdownMenu.render resolve)
        ] accountRows
        |> Table.withDensity Density.Compact
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.withRowAttributes rowAttributes
        |> Table.withSelection (
            TableSelection.create "accounts-selection" (fun (row:AccountRow) -> string row.id) (fun row -> row.name)
            |> TableSelection.withFormName "accountIds")

    let private accountTableConfig destinationFor resolve rowAttributes = accountTableConfigFor rows destinationFor resolve rowAttributes

    let accountTable = accountTableConfig LedgerAccount shellDestinationUrl (fun _ -> []) |> Table.render |> recordActionFeedback "account"

    let hierarchyAccounts =
        [ { key = "assets"; ancestors = []; level = 0; name = "Assets"; accountType = "Aggregate"; total = 184230.19M; hasChildren = true }
          { key = "cash"; ancestors = [ "assets" ]; level = 1; name = "Cash"; accountType = "Aggregate"; total = 96110.27M; hasChildren = true }
          { key = "checking"; ancestors = [ "assets"; "cash" ]; level = 2; name = "Operating checking"; accountType = "Account"; total = 72104.18M; hasChildren = false }
          { key = "savings"; ancestors = [ "assets"; "cash" ]; level = 2; name = "Tax savings"; accountType = "Account"; total = 24006.09M; hasChildren = false }
          { key = "receivables"; ancestors = [ "assets" ]; level = 1; name = "Accounts receivable"; accountType = "Account"; total = 88119.92M; hasChildren = false }
          { key = "liabilities"; ancestors = []; level = 0; name = "Liabilities"; accountType = "Aggregate"; total = -42118.44M; hasChildren = true }
          { key = "card"; ancestors = [ "liabilities" ]; level = 1; name = "Business card"; accountType = "Account"; total = -42118.44M; hasChildren = false } ]

    let hierarchicalAccountTable =
        Table.create "Account hierarchy"
            [ TableColumn.create "Account" (fun row -> a { _href "/components/collection"; _class "font-medium text-[var(--fve-brand-text)]"; row.name })
              |> TableColumn.asRowHeader
              |> TableColumn.asMobilePrimary
              TableColumn.create "Type" (fun row -> text row.accountType)
              TableColumn.create "Total balance (USD) · Current" (fun row -> text (money row.total))
              |> TableColumn.alignEnd ]
            hierarchyAccounts
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.withHierarchy (
            TableHierarchy.create "account-hierarchy" _.key (fun row -> row.name) _.ancestors _.level _.hasChildren
            |> TableHierarchy.withExpandedKeys [ "assets"; "cash"; "liabilities" ])
        |> Table.render

    let private accountFilterExpression (row:AccountRow) =
        let searchable = System.Text.Json.JsonSerializer.Serialize($"{row.name} {row.accountType} {row.commodity}".ToLowerInvariant())
        let accountType = System.Text.Json.JsonSerializer.Serialize(row.accountType.ToLowerInvariant())
        $"(!$collectionquery.trim() || {searchable}.includes($collectionquery.trim().toLowerCase())) && ($collectiontype == 'all' || $collectiontype == {accountType})"

    let private collectionHasMatches =
        rows
        |> List.map (fun row -> $"({accountFilterExpression row})")
        |> String.concat " || "

    let private filteredAccountTable =
        accountTableConfig LedgerAccount shellDestinationUrl (fun row -> [ _dataShow (accountFilterExpression row) ])
        |> Table.render
        |> recordActionFeedback "account"

    let private transactionRows =
        [ { id = 201; date = "Jul 28"; description = "Northwind payment"; status = Verified; accounts = "Accounts receivable → Operating checking"; amount = 4800M }
          { id = 202; date = "Jul 27"; description = "Cloud hosting"; status = Verified; accounts = "Operating checking → Software expense"; amount = 386.42M }
          { id = 203; date = "Jul 26"; description = "ACH withdrawal"; status = Unverified; accounts = "Operating checking → Placeholder"; amount = 1240M }
          { id = 204; date = "Jun 24"; description = "Office supplies"; status = Committed; accounts = "Business card → Office expense"; amount = 128.19M } ]

    let private transactionTableFor destinationFor resolve =
        Table.create "Transactions" [
            TableColumn.create "Date" (fun row -> text row.date)
            TableColumn.create "Description" (fun row ->
                a { _href (resolve (destinationFor row.id)); _class "font-medium text-[var(--fve-brand-text)]"; row.description })
            |> TableColumn.asRowHeader
            |> TableColumn.asMobilePrimary
            TableColumn.create "Status" (fun row ->
                match row.status with
                | Verified -> Badge.create "Verified" |> Badge.withColor BadgeColor.Success |> Badge.render
                | Unverified -> Badge.create "Unverified" |> Badge.withColor BadgeColor.Warning |> Badge.render
                | Committed -> Badge.create "Committed" |> Badge.withColor BadgeColor.Neutral |> Badge.render)
            TableColumn.create "Accounts" (fun row -> text row.accounts)
            TableColumn.create "Amount" (fun row -> text (money row.amount))
            |> TableColumn.alignEnd
            TableColumn.rowActions (fun row ->
                let payload = System.Text.Json.JsonSerializer.Serialize {| id = row.id; date = row.date; description = row.description; status = string row.status; accounts = row.accounts; amount = row.amount |}
                DropdownMenu.create $"transaction-{row.id}-actions" $"More actions for {row.description}"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon moreActionsIcon)
                |> DropdownMenu.withContent (recordMenuItems "transaction" row.id (destinationFor row.id) resolve payload)
                |> DropdownMenu.render resolve)
        ] transactionRows
        |> Table.withDensity Density.Compact
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.withSelection (TableSelection.create "transactions-selection" (fun row -> string row.id) (fun row -> row.description))
        |> Table.render
        |> recordActionFeedback "transaction"

    let private transactionTable = transactionTableFor LedgerTransaction shellDestinationUrl

    type TeamMember = { id:int; name:string; email:string; role:string }

    let teamMembers =
        [ { id = 1; name = "Alex Morgan"; email = "alex@fve.meiermade.com"; role = "Owner" }; { id = 2; name = "Jamie Lee"; email = "jamie@fve.meiermade.com"; role = "Member" }; { id = 3; name = "Riley Chen"; email = "riley@fve.meiermade.com"; role = "Member" }; { id = 4; name = "Sam Rivera"; email = "sam@fve.meiermade.com"; role = "Guest" } ]

    let teamColumns =
        [ TableColumn.create "Name" (fun (person:TeamMember) -> text person.name) |> TableColumn.asRowHeader
          TableColumn.create "Email" (fun person -> text person.email)
          TableColumn.create "Role" (fun person -> text person.role) ]

    let simpleTeamTable =
        Table.create "Team members" teamColumns teamMembers
        |> Table.render

    let comfortableTeamTable =
        Table.create "Team members with comfortable rows" teamColumns teamMembers
        |> Table.withDensity Density.Comfortable
        |> Table.render

    let memberStatusTable =
        Table.create "Team member status" [
            TableColumn.create "Name" (fun (name, _, _) -> text name) |> TableColumn.asRowHeader
            TableColumn.create "Email" (fun (_, email, _) -> text email)
            TableColumn.create "Status" (fun (_, _, status) -> status)
        ] [
            "Alex Morgan", "alex@fve.meiermade.com", Badge.create "Active" |> Badge.withColor BadgeColor.Success |> Badge.render
            "Jamie Lee", "jamie@fve.meiermade.com", Badge.create "Active" |> Badge.withColor BadgeColor.Success |> Badge.render
            "Riley Chen", "riley@fve.meiermade.com", Badge.create "Active" |> Badge.withColor BadgeColor.Success |> Badge.render
            "Sam Rivera", "sam@fve.meiermade.com", Badge.create "Invited" |> Badge.withColor BadgeColor.Warning |> Badge.render
        ]
        |> Table.render

    let selectableTeamTable =
        Table.create "Selectable team members" teamColumns teamMembers
        |> Table.withSelection (
            TableSelection.create "team-member-selection" (fun person -> string person.id) (fun person -> person.name)
            |> TableSelection.withFormName "memberIds")
        |> Table.render

    let mobileTeamTable =
        Table.create "Team members as mobile records" [
            TableColumn.create "Name" (fun (person:TeamMember) -> text person.name)
            |> TableColumn.asRowHeader
            |> TableColumn.asMobilePrimary
            TableColumn.create "Email" (fun person -> text person.email)
            TableColumn.create "Role" (fun person -> text person.role)
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
            TableColumn.create "Name" (fun (person:TeamMember) -> text person.name)
            |> TableColumn.asRowHeader
            |> TableColumn.asMobilePrimary
            |> TableColumn.withSort (sortFor "name" current)
            TableColumn.create "Email" (fun person -> text person.email)
            TableColumn.create "Role" (fun person -> text person.role)
            |> TableColumn.withSort (sortFor "role" current)
        ] members
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.render

    let sortableTeamTablePreview current =
        div {
            _id "components-table-sorting-preview"
            sortableTeamTable current
        }

    let defaultDescriptionList =
        DescriptionList.create [
            DescriptionListItem.text "Account type" "Asset"
            DescriptionListItem.text "Currency" "USD" ]
        |> DescriptionList.render

    let accountDetails =
        DescriptionList.create [
            DescriptionListItem.text "Type" "Asset"
            DescriptionListItem.text "Commodity" "USD"
            DescriptionListItem.text "Parent account" "Current assets"
            DescriptionListItem.text "Source" "Imported statement"
            DescriptionListItem.status "Status" (Badge.create "Active" |> Badge.withColor BadgeColor.Success |> Badge.render)
            DescriptionListItem.text "Balance" "$42,800"
        ]
        |> DescriptionList.withColumns DescriptionListColumns.Three
        |> DescriptionList.render

    let accountOverview =
        DescriptionList.create [
            DescriptionListItem.status "Status" (Badge.create "Active" |> Badge.withColor BadgeColor.Success |> Badge.render)
            DescriptionListItem.status "Reconciliation" (Badge.create "Up to date" |> Badge.withColor BadgeColor.Success |> Badge.render)
            DescriptionListItem.text "Account number" "1040"
            DescriptionListItem.text "Currency" "USD"
            DescriptionListItem.text "Account name" "Operating checking"
            DescriptionListItem.text "Institution" "Example Bank"
            DescriptionListItem.text "Statement date" "August 31, 2026"
            DescriptionListItem.text "Balance" "$42,800"
        ]
        |> DescriptionList.withColumns DescriptionListColumns.Four
        |> DescriptionList.render

    let accountDetailsWithDescriptions =
        DescriptionList.create [
            DescriptionListItem.text "Available balance" "$42,800"
            |> DescriptionListItem.withDescription "Includes cleared entries through today."
            DescriptionListItem.text "Statement reference" "statement-2026-08-31-operating-checking-1040"
            |> DescriptionListItem.withDescription "Imported from the August bank statement."
        ]
        |> DescriptionList.withColumns DescriptionListColumns.Two
        |> DescriptionList.render

    let defaultMetric = Metric.text "Available balance" "$42,800" |> Metric.render

    let availableBalanceMetric =
        Metric.text "Available balance" "$42,800"
        |> Metric.withTrend "Up 8% from last month"
        |> Metric.withDescription "Operating and reserve accounts"
        |> Metric.withStatus (Badge.create "Current" |> Badge.withColor BadgeColor.Success |> Badge.render)
        |> Metric.render

    let pendingEntriesMetric =
        Metric.text "Pending entries" "14"
        |> Metric.withDescription "Require review before posting"
        |> Metric.withStatus (Badge.create "Needs review" |> Badge.withColor BadgeColor.Warning |> Badge.render)
        |> Metric.render

    let multipleMetrics =
        div {
            _role "group"
            _ariaLabel "Monthly financial metrics"
            _class "grid w-full min-w-0 max-w-4xl grid-cols-[repeat(auto-fit,minmax(min(100%,12rem),1fr))] justify-items-center gap-8"
            for label, value in [ "Revenue", "$48,000"; "Expenses", "$31,200"; "Net income", "$16,800" ] do
                Metric.text label value
                |> Metric.withDescription "September 2026"
                |> Metric.render
        }

    let defaultPagination =
        Pagination.create "Results pages" [
            PaginationItem.current 1
            PaginationItem.link 2 "/components/pagination/page?page=2" ]
        |> Pagination.render id

    type PaginationDestination = PaginationPage of int

    let paginationDestinationUrl (PaginationPage page) =
        $"/components/pagination/page?page={page}"

    let paginationConfig requestedPage =
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

    let paginationPreview requestedPage =
        paginationConfig requestedPage |> Pagination.render paginationDestinationUrl

    let paginationVariantPreview variant requestedPage =
        paginationConfig requestedPage
        |> Pagination.withVariant variant
        |> Pagination.render paginationDestinationUrl

    let paginationPreviewRegion requestedPage =
        div {
            _id "components-pagination-region"
            paginationPreview requestedPage
        }

    let private statusOptions =
        [ SelectOption.create Active "Active"
          SelectOption.create Pending "Pending"
          SelectOption.create Suspended "Suspended"
          SelectOption.create Scheduled "Scheduled" ]

    let private selectStatusOptions =
        [ SelectOption.create Active "Active"
          SelectOption.create Pending "Pending"
          SelectOption.create Suspended "Suspended" |> SelectOption.disabled
          SelectOption.create Scheduled "Scheduled" ]

    let private choiceSubmitButton id (label:string) =
        Button.create (ButtonContent.Text label)
        |> Button.withColor ButtonColor.Primary
        |> Button.withVariant ButtonVariant.Solid
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
                |> Notice.withColor NoticeColor.Success
                |> Notice.withAnnouncement LiveAnnouncement.Polite
                |> Notice.render
            form {
                _ariaLabel title
                _class "grid gap-6"
                _attr ("novalidate", "")
                _dataOn ("submit", $"@post('{endpoint}', {{contentType: 'form'}})")
                if layout = Sectioned then
                    Section.create (SectionHeader.create "Contact information" |> SectionHeader.withDescription "Who should we contact?" |> SectionHeader.withDivider) identityFields
                    |> Section.render
                    Section.create (SectionHeader.create "Additional details" |> SectionHeader.withDivider) notes
                    |> Section.render
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
        |> Input.render
    let inputWithHelp =
        Input.create "helpEmail" "Email"
        |> Input.withType InputType.Email
        |> Input.withDescription "We will use this address for account updates."
        |> Input.withAttributes [ _placeholder "you@fve.meiermade.com"; _autocomplete "email" ]
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
        |> Input.withAttributes [ _placeholder "you@fve.meiermade.com" ]
        |> Input.render
    let inputWithPrefix =
        Input.create "website" "Website"
        |> Input.withPrefix "https://"
        |> Input.withAttributes [ _placeholder "fve.meiermade.com"; _autocomplete "off" ]
        |> Input.render
    let inputWithSuffix =
        Input.create "price" "Price"
        |> Input.withSuffix "USD"
        |> Input.withAttributes [ _inputmode "decimal"; _placeholder "0.00" ]
        |> Input.render
    let disabledInput =
        Input.create "disabledEmail" "Email"
        |> Input.withValue "alex@fve.meiermade.com"
        |> Input.disabled
        |> Input.render
    let pendingInput =
        Input.create "pendingEmail" "Email"
        |> Input.withValue "alex@fve.meiermade.com"
        |> Input.pending
        |> Input.render
    let searchInputExample =
        Input.create "query" "Search"
        |> Input.withId "field-search"
        |> Input.withType InputType.Search
        |> Input.withAttributes [ _placeholder "Search…"; _autocomplete "off" ]
        |> Input.render
    let inputSizeExamples =
        div {
            _class "grid min-w-0 gap-6"
            for size, name in [ ControlSize.Small, "Compact"; ControlSize.Medium, "Standard"; ControlSize.Large, "Large" ] do
                div {
                    _class (ControlSize.className size + " flex flex-wrap items-end gap-3")
                    Input.create ("sizing-" + name) (name + " search")
                    |> Input.withType InputType.Search
                    |> Input.render
                    Select.create ("sizing-type-" + name) (name + " type") id [ SelectOption.create "all" "All types" ]
                    |> Select.withSelected "all"
                    |> Select.render
                    div {
                        _class "flex flex-wrap items-center gap-3"
                        Button.create (ButtonContent.Text (name + " action")) |> Button.render
                        Button.create (ButtonContent.Icon (name + " refresh", refreshIcon)) |> Button.render
                    }
                }
            div {
                _class (ControlSize.className ControlSize.Small + " flex flex-wrap items-end gap-3")
                Input.create "override-search" "Larger search in a compact region"
                |> Input.withType InputType.Search
                |> Input.withSize ControlSize.Large
                |> Input.render
                Select.create "override-type" "Larger type" id [ SelectOption.create "all" "All types" ]
                |> Select.withSelected "all" |> Select.withSize ControlSize.Large |> Select.render
                div {
                    _class "flex flex-wrap items-center gap-3"
                    Button.create (ButtonContent.Text "Larger action") |> Button.withSize ControlSize.Large |> Button.render
                    Button.create (ButtonContent.Icon ("Larger refresh", refreshIcon)) |> Button.withSize ControlSize.Large |> Button.render
                }
            }
        }

    let tagInputExample =
        TagInput.create "ledger-tags" "tags" "Tags" []
        |> TagInput.render
    let invalidTagInputExample =
        TagInput.create "required-tags" "requiredTags" "Required tags" []
        |> TagInput.withDescription "Add at least one classification."
        |> TagInput.withValidation "Add a tag before continuing."
        |> TagInput.render
    let pendingTagInputExample =
        TagInput.create "pending-tags" "pendingTags" "Updating tags" [ "reviewed" ]
        |> TagInput.pending
        |> TagInput.render
    let disabledTagInputExample =
        TagInput.create "disabled-tags" "disabledTags" "Unavailable tags" [ "archived" ]
        |> TagInput.disabled
        |> TagInput.render
    let labelledTextarea =
        Textarea.create "message" "Message"
        |> Textarea.render
    let composerTextarea =
        Textarea.create "reply" "Message"
        |> Textarea.withRows 1
        |> Textarea.withVisuallyHiddenLabel
        |> Textarea.required
        |> Textarea.withAttributes [ _placeholder "Write a message…" ]
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
        |> Notice.render
    let noticeColorExamples =
        div {
            _class "grid w-full min-w-0 grid-cols-[repeat(auto-fit,minmax(min(100%,20rem),1fr))] gap-4"
            for label, color in [ "Primary", NoticeColor.Primary; "Secondary", NoticeColor.Secondary; "Success", NoticeColor.Success; "Warning", NoticeColor.Warning; "Error", NoticeColor.Error; "Info", NoticeColor.Info; "Neutral", NoticeColor.Neutral ] do
                Notice.create ("notice-color-" + label.ToLowerInvariant()) label (p { "Review account details." })
                |> Notice.withColor color
                |> Notice.render
        }
    let noticeVariantExamples =
        div {
            _class "grid w-full min-w-0 grid-cols-[repeat(auto-fit,minmax(min(100%,20rem),1fr))] gap-4"
            for label, variant in [ "Solid", NoticeVariant.Solid; "Soft", NoticeVariant.Soft; "Outline", NoticeVariant.Outline; "Ghost", NoticeVariant.Ghost ] do
                Notice.create ("notice-variant-" + label.ToLowerInvariant()) label (p { "Review account details." })
                |> Notice.withVariant variant
                |> Notice.withActions (a { _href "/components/notice"; _class "underline underline-offset-2"; "Read guidance" })
                |> Notice.render
        }
    let successNotice =
        Notice.create "notice-success" "Export ready" (p { "Your export is ready to download." })
        |> Notice.withColor NoticeColor.Success
        |> Notice.withActions (a { _href "data:text/csv;charset=utf-8,Account%2CBalance%0AOperating%2C42800"; _attr ("download", "accounts.csv"); _class "underline underline-offset-2"; "Download accounts" })
        |> Notice.render
    let warningNotice =
        Notice.create "notice-warning" "Review required" (p { "Two transactions need an account before they can be posted." })
        |> Notice.withColor NoticeColor.Warning
        |> Notice.render
    let criticalNotice =
        Notice.create "notice-error" "Details need attention" (p { "Correct the highlighted fields and validate the form again." })
        |> Notice.withColor NoticeColor.Error
        |> Notice.withActions (a { _href "/components/error-summary"; _class "underline underline-offset-2"; "Review highlighted fields" })
        |> Notice.render

    let memberOptions =
        [ SelectOption.create "alex" "Alex Morgan"
          SelectOption.create "jamie" "Jamie Lee"
          SelectOption.create "riley" "Riley Chen"
          SelectOption.create "taylor" "Taylor Brooks"
          SelectOption.create "sam" "Sam Rivera" |> SelectOption.disabled ]

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
        |> List.map (fun (value, label) -> SelectOption.create value label)

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

    let defaultSelect =
        Select.create "updateFrequencyDefault" "Update frequency" id [
            SelectOption.create "daily" "Daily"
            SelectOption.create "weekly" "Weekly"
            SelectOption.create "monthly" "Monthly" ]
        |> Select.render

    let basicSelect =
        Select.create "updateFrequency" "Update frequency" id [
            SelectOption.create "daily" "Daily"
            SelectOption.create "weekly" "Weekly"
            SelectOption.create "monthly" "Monthly" ]
        |> Select.withSelected "weekly"
        |> Select.render
    let selectWithHelp =
        Select.create "digestFrequency" "Digest frequency" id [
            SelectOption.create "daily" "Daily"
            SelectOption.create "weekly" "Weekly"
            SelectOption.create "monthly" "Monthly" ]
        |> Select.withDescription "Choose how often to receive a summary."
        |> Select.withPlaceholder "Choose a frequency"
        |> Select.render
    let edgeAlignedSelect =
        Select.create "exportFormat" "Export format" id [
            SelectOption.create "csv" "CSV"
            SelectOption.create "json" "JSON"
            SelectOption.create "pdf" "PDF" ]
        |> Select.withSelected "json"
        |> Select.withPosition SelectPosition.TriggerEnd
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
        |> Switch.withChecked true
        |> Switch.render
    let basicRadioGroup =
        RadioGroup.create "contactMethod" "Contact method" id [
            RadioGroupOption.create "email" "Email"
            RadioGroupOption.create "phone" "Phone" ]
        |> RadioGroup.render
    let radioGroupWithHelp =
        RadioGroup.create "delivery" "Delivery" id [
            RadioGroupOption.create "standard" "Standard" |> RadioGroupOption.withDescription "Arrives in three to five business days."
            RadioGroupOption.create "express" "Express" |> RadioGroupOption.withDescription "Arrives the next business day." ]
        |> RadioGroup.withDescription "Choose how you would like your order delivered."
        |> RadioGroup.render

    let horizontalRadioGroup =
        RadioGroup.create "density" "Table density" id [
            RadioGroupOption.create "compact" "Compact"
            RadioGroupOption.create "comfortable" "Comfortable"
            RadioGroupOption.create "spacious" "Spacious" ]
        |> RadioGroup.withSelected "comfortable"
        |> RadioGroup.withOrientation RadioGroupOrientation.Horizontal
        |> RadioGroup.render

    let defaultChoiceCards =
        ChoiceCards.single "default-assistance-choice" "defaultAssistance" "Booking assistance" id [
            ChoiceCardOption.create "self" "Self-service"
            ChoiceCardOption.create "staff" "Staff assisted" ]
        |> ChoiceCards.render

    let richChoiceCards =
        ChoiceCards.single "assistance-choice" "assistance" "Booking assistance" id
            [ ChoiceCardOption.create "self" "Self-service"
              |> ChoiceCardOption.withDescription "The participant completes details and waivers before arrival."
              |> ChoiceCardOption.withMetadata "No staff coordination required"
              ChoiceCardOption.create "staff" "Staff assisted"
              |> ChoiceCardOption.withDescription "A coordinator confirms equipment and participant details."
              |> ChoiceCardOption.withMetadata "Recommended for group bookings"
              ChoiceCardOption.create "unavailable" "Managed service"
              |> ChoiceCardOption.withDescription "Available only to contracted organizations."
              |> ChoiceCardOption.disabled ]
        |> ChoiceCards.withSelected [ "staff" ]
        |> ChoiceCards.required
        |> ChoiceCards.render

    let multipleChoiceCards =
        ChoiceCards.multiple "equipment-choice" "equipmentIds" "Equipment" id
            [ ChoiceCardOption.create "helmet" "Helmet"
              |> ChoiceCardOption.withDescription "Required protective equipment."
              |> ChoiceCardOption.withMetadata "Available"
              ChoiceCardOption.create "pads" "Protective pads"
              |> ChoiceCardOption.withDescription "Knee and elbow protection."
              ChoiceCardOption.create "radio" "Trail radio"
              |> ChoiceCardOption.withDescription "Unavailable for this booking."
              |> ChoiceCardOption.disabled ]
        |> ChoiceCards.withSelected [ "helmet"; "pads" ]
        |> ChoiceCards.render

    let invalidChoiceCards =
        ChoiceCards.single "invalid-assistance-choice" "invalidAssistance" "Booking assistance" id
            [ ChoiceCardOption.create "self" "Self-service"
              ChoiceCardOption.create "staff" "Staff assisted" ]
        |> ChoiceCards.required
        |> ChoiceCards.withValidation "Choose how this booking will be supported."
        |> ChoiceCards.render

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
            SelectOption.create value label
            |> (if value = 103 then SelectOption.disabled else id))

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
            |> Checkbox.withChecked confirmed
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
        |> Checkbox.withChecked true
        |> Checkbox.render

    let pendingArchivedReview =
        Checkbox.create "confirmArchivedReview" "Saving archived-account review"
        |> Checkbox.withId "components-pending-archived-review"
        |> Checkbox.withChecked true
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
            |> Switch.withChecked enabled
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
        |> Switch.withChecked true
        |> Switch.pending
        |> Switch.render

    let invalidNotifications =
        Switch.create "postingNotifications" "Notification service"
        |> Switch.withId "components-invalid-notifications"
        |> Switch.withValidation "Notification preferences could not be saved."
        |> Switch.render

    let switchSizes =
        div {
            _class "grid w-full max-w-sm gap-4"
            Switch.create "compactAlerts" "Compact switch" |> Switch.withSize ControlSize.Small |> Switch.withChecked true |> Switch.render
            Switch.create "standardAlerts" "Standard switch" |> Switch.withSize ControlSize.Medium |> Switch.withChecked true |> Switch.render
            Switch.create "prominentAlerts" "Prominent switch" |> Switch.withSize ControlSize.Large |> Switch.withChecked true |> Switch.render
        }

    let stackedSwitch =
        Switch.create "automaticExports" "Automatic exports"
        |> Switch.withDescription "Generate an export after each reconciliation closes."
        |> Switch.withLayout SwitchLayout.Stacked
        |> Switch.render

    let compactRows =
        ToggleButton.create "components-compact-rows" "Compact rows"
        |> ToggleButton.render

    let pendingCompactRows =
        ToggleButton.create "components-pending-compact-rows" "Applying compact rows"
        |> ToggleButton.pending
        |> ToggleButton.render

    let disabledCompactRows =
        ToggleButton.create "components-disabled-compact-rows" "Compact rows unavailable"
        |> ToggleButton.disabled
        |> ToggleButton.render

    let toggleButtonPresentation =
        div {
            _class "flex flex-wrap items-center gap-3"
            ToggleButton.create "toggle-soft" "Add account"
            |> ToggleButton.withLeading plusIcon
            |> ToggleButton.withVariant ToggleButtonVariant.Soft
            |> ToggleButton.withSize ControlSize.Small
            |> ToggleButton.render
            ToggleButton.create "toggle-outline" "Add account"
            |> ToggleButton.withLeading plusIcon
            |> ToggleButton.withVariant ToggleButtonVariant.Outline
            |> ToggleButton.pressed
            |> ToggleButton.render
            ToggleButton.create "toggle-ghost" "Add account"
            |> ToggleButton.withLeading plusIcon
            |> ToggleButton.withVariant ToggleButtonVariant.Ghost
            |> ToggleButton.withSize ControlSize.Large
            |> ToggleButton.render
        }

    let defaultToggleGroup =
        ToggleGroup.single "alignment-default" "components-alignment-default" "Text alignment" id [
            ToggleGroupOption.create "left" "Left"
            ToggleGroupOption.create "center" "Center"
            ToggleGroupOption.create "right" "Right" ]
        |> ToggleGroup.render

    let singleToggleGroup =
        ToggleGroup.single "alignment" "components-alignment-group" "Text alignment" id [
            ToggleGroupOption.create "left" "Left"
            ToggleGroupOption.create "center" "Center"
            ToggleGroupOption.create "right" "Right" ]
        |> ToggleGroup.withSelected "left"
        |> ToggleGroup.render

    let multipleToggleGroup =
        ToggleGroup.multiple "formatting" "components-formatting-group" "Text formatting" id [
            ToggleGroupOption.create "bold" "Bold" |> ToggleGroupOption.withLeading plusIcon
            ToggleGroupOption.create "italic" "Italic" |> ToggleGroupOption.withLeading refreshIcon
            ToggleGroupOption.create "strike" "Strike" |> ToggleGroupOption.disabled ]
        |> ToggleGroup.withSelectedMany [ "bold" ]
        |> ToggleGroup.withVariant ToggleGroupVariant.Spaced
        |> ToggleGroup.withSize ControlSize.Small
        |> ToggleGroup.render

    let verticalToggleGroup =
        ToggleGroup.single "view" "components-view-group" "Record view" id [
            ToggleGroupOption.create "summary" "Summary"
            ToggleGroupOption.create "activity" "Activity"
            ToggleGroupOption.create "audit" "Audit log" ]
        |> ToggleGroup.withSelected "summary"
        |> ToggleGroup.withOrientation ToggleGroupOrientation.Vertical
        |> ToggleGroup.render

    let codePreviewTabs =
        Tabs.create "components-example-format" "Example format" [
            TabItem.create "code" "Code" (
                pre {
                    _role "region"
                    _ariaLabel "Button F# code"
                    _tabindex 0
                    _dataOn ("keydown", """
                        if (evt.target === el && !evt.altKey && !evt.ctrlKey && !evt.metaKey && !evt.shiftKey
                            && (evt.key === 'ArrowLeft' || evt.key === 'ArrowRight')) {
                            evt.preventDefault();
                            el.scrollBy({left: evt.key === 'ArrowLeft' ? -80 : 80});
                        }
                    """)
                    _class "overflow-x-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-4 text-sm outline-none focus-visible:outline-2 focus-visible:outline-solid focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
                    "Button.create (ButtonContent.Text \"Create account\") |> Button.render"
                })
            TabItem.create "preview" "Preview" (div { _class "rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] p-4"; Button.create (ButtonContent.Text "Create account") |> Button.render }) ]
        |> Tabs.render

    let verticalTabs =
        Tabs.create "components-vertical-tabs" "Workspace settings" [
            TabItem.create "profile" "Profile" (p { _class "text-sm text-[var(--fve-muted-text)]"; "Update your name and profile image." })
            |> TabItem.withLeading plusIcon
            TabItem.create "notifications" "Notifications" (p { _class "text-sm text-[var(--fve-muted-text)]"; "Choose which account changes send alerts." })
            |> TabItem.withLeading refreshIcon
            TabItem.create "billing" "Billing" (p { _class "text-sm text-[var(--fve-muted-text)]"; "Billing changes require administrator access." })
            |> TabItem.withLeading removeIcon
            |> TabItem.disabled ]
        |> Tabs.withOrientation TabsOrientation.Vertical
        |> Tabs.render

    let reviewTabsRegion refreshed =
        let refreshActivityButton =
            Button.create (ButtonContent.Text "Refresh activity")
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
            TabItem.create "overview" "Overview" (p { _class "text-sm text-[var(--fve-muted-text)]"; "Operating balance: $42,800" })
            TabItem.create "activity" "Activity" activity
            TabItem.create "settings" "Settings" (a { _href "/components/theming"; _class "font-medium text-[var(--fve-brand-text)]"; "Review account theme settings" }) ]
        |> Tabs.withVariant TabsVariant.Underlined
        |> Tabs.render

    let private postingModeOptions =
        [ RadioGroupOption.create "automatic" "Automatic"
          RadioGroupOption.create "manual" "Manual review"
          RadioGroupOption.create "scheduled" "Scheduled" |> RadioGroupOption.disabled ]

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

    let basicDropdownMenu =
        DropdownMenu.create "components-menu-basic" "Actions"
        |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "Actions")
        |> DropdownMenu.withContent [
            DropdownMenuItem.link "/components/button" "Button documentation"
            DropdownMenuItem.link "/components/button-group" "Button group documentation" ]
        |> DropdownMenu.render id

    let dropdownMenuItems refreshed =
        [ DropdownMenuItem.group "Account" [
              DropdownMenuItem.link DropdownMenuGuide "Dropdown menu guidance"
              DropdownMenuItem.action "$menuActivations++" "Record review"
              |> DropdownMenuItem.withLeading menuLeadingIcon
              |> DropdownMenuItem.withShortcut "R" ]
          DropdownMenuItem.separator
          DropdownMenuItem.group "Reports" [
              if refreshed then
                  DropdownMenuItem.action "$menuActivations++" "Review refreshed actions"
              else
                  DropdownMenuItem.action "@get('/components/menus/actions')" "Refresh actions"
              DropdownMenuItem.action "$menuActivations++" "Export statement"
              |> DropdownMenuItem.disabled
              DropdownMenuItem.action "$menuActivations++" "Syncing ledger"
              |> DropdownMenuItem.pending
              DropdownMenuItem.action "$menuActivations++" "Create report"
              DropdownMenuItem.action "$menuActivations++" "Close period" ]
          DropdownMenuItem.separator
          DropdownMenuItem.group "Display" [
              DropdownMenuItem.checkbox "$menuCompact = !$menuCompact" "Compact rows"
              |> DropdownMenuItem.withCheckedExpression "$menuCompact" ]
          DropdownMenuItem.separator
          DropdownMenuItem.action "$menuDeletes++" "Delete draft" |> DropdownMenuItem.withColor DropdownMenuItemColor.Error ]

    let actionMenu refreshed =
        DropdownMenu.create "components-menu-actions" "Actions"
        |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "Actions")
        |> DropdownMenu.withContent (dropdownMenuItems refreshed)
        |> DropdownMenu.withAlignment DropdownMenuAlignment.Start
        |> DropdownMenu.render destinationUrl

    let menuOverflowIcon =
        svg {
            _viewBox "0 0 24 24"
            _fill "none"
            _stroke "currentColor"
            _strokeWidth "1.5"
            _ariaHidden "true"
            _class "size-5"
            path { _strokeLinecap "round"; _strokeLinejoin "round"; _d "M6.75 12a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Z" }
        }

    let menuTrailingChevron =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M7.21 4.23a.75.75 0 0 1 1.06-.02l5.5 5.25a.75.75 0 0 1 0 1.08l-5.5 5.25a.75.75 0 0 1-1.04-1.08L12.16 10 7.23 5.29a.75.75 0 0 1-.02-1.06Z" clip-rule="evenodd"/></svg>"""

    let fullRowDropdownMenu =
        div {
            _class "w-64 max-w-full border border-[var(--fve-border)]"
            _dataSignals "{menuRegion: 'Side navigation'}"
            DropdownMenu.create "components-menu-full-row" "Choose a navigation pattern"
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.content (span {
                _class "flex w-full items-center justify-between gap-3"
                span { _dataText "$menuRegion"; "Side navigation" }
                raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 0 1 1.06.02L10 11.17l3.71-3.94a.75.75 0 0 1 1.08 1.04l-4.25 4.5a.75.75 0 0 1-1.08 0l-4.25-4.5a.75.75 0 0 1 .02-1.06Z" clip-rule="evenodd"/></svg>"""
            }) |> DropdownMenuTrigger.asFullRow)
            |> DropdownMenu.withContent [
                DropdownMenuItem.radio "$menuRegion = 'Side navigation'" "Side navigation"
                |> DropdownMenuItem.withDescription "Persistent desktop navigation"
                |> DropdownMenuItem.withChecked true |> DropdownMenuItem.withCheckedExpression "$menuRegion == 'Side navigation'"
                DropdownMenuItem.radio "$menuRegion = 'Breadcrumbs'" "Breadcrumbs"
                |> DropdownMenuItem.withDescription "Location within a page hierarchy"
                |> DropdownMenuItem.withCheckedExpression "$menuRegion == 'Breadcrumbs'"
                DropdownMenuItem.separator
                DropdownMenuItem.link "/components/side-nav" "Read navigation guidance"
                |> DropdownMenuItem.withTrailing menuTrailingChevron ]
            |> DropdownMenu.withAlignment DropdownMenuAlignment.Start
            |> DropdownMenu.render id
        }

    let moreActionsMenu =
        DropdownMenu.create "components-menu-more" "More actions"
        |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon menuOverflowIcon)
        |> DropdownMenu.withContent [
            DropdownMenuItem.group "View" [
                DropdownMenuItem.link DropdownMenuGuide "Read menu guidance"
                DropdownMenuItem.action "$menuActivations++" "Record secondary action" ] ]
        |> DropdownMenu.render destinationUrl

    let dropdownMenuRegion refreshed =
        div {
            _id "components-dropdown-menu-region"
            _dataSignals "{menuActivations: 0, menuDeletes: 0, menuCompact: false}"
            _class "grid justify-items-center text-center"
            div { _class "flex flex-wrap items-center justify-center gap-3"; [ actionMenu refreshed; moreActionsMenu ] }
            p { _class "mt-4 text-sm text-[var(--fve-muted-text)]"; _dataText "'Completed menu actions: ' + $menuActivations"; "Completed menu actions: 0" }
            p { _class "mt-1 text-sm text-[var(--fve-critical-text)]"; _dataText "'Delete activations: ' + $menuDeletes"; "Delete activations: 0" }
            if refreshed then
                p { _role "status"; _class "mt-1 text-sm text-[var(--fve-positive-text)]"; "Actions refreshed from the server." }
        }

    let patchedDropdownMenuRegion = dropdownMenuRegion true

    let defaultDrawer =
        Drawer.create "default-drawer" "Account details" (p { "Operating checking · USD" })

    let defaultDrawerExample =
        div {
            defaultDrawer |> Drawer.trigger "Open drawer"
            defaultDrawer |> Drawer.render
        }

    let defaultDialog =
        Dialog.create "default-dialog" "Review account" (p { "Confirm the account settings before they are applied." })

    let defaultDialogExample =
        div {
            defaultDialog |> Dialog.trigger "Open dialog"
            defaultDialog |> Dialog.render
        }

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

    let accountDeletionForm (dialogId:string) isPending (validation:string option) =
        let pendingSignal = "_" + dialogId.Replace('-', '_') + "_pending"
        let pendingExpression = if isPending then "true" else "$" + pendingSignal
        form {
            _id $"{dialogId}-content"
            _method "post"
            _action "/components/dialogs/confirm"
            _dataIndicator pendingSignal
            _dataOn ("submit", $"!({pendingExpression}) && @post('/components/dialogs/confirm').then(() => document.getElementById('{dialogId}-confirm')?.focus())")
            _ariaBusy isPending
            _dataAttr ("aria-busy", $"{pendingExpression} ? 'true' : 'false'")
            match validation with
            | Some message ->
                p {
                    _id $"{dialogId}-validation"
                    _role "alert"
                    _class "rounded-[var(--fve-radius-control)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)] ring-1 ring-inset ring-[var(--fve-critical-ring)]"
                    message
                }
            | None -> ()
            p {
                _id $"{dialogId}-status"
                _tabindex -1
                _role "status"
                _dataShow pendingExpression
                if not isPending then _style "display:none"
                _class "mt-4 text-sm text-[var(--fve-muted-text)]"
                "Confirmation in progress."
            }
            div {
                _class "mt-6 flex flex-wrap justify-end gap-3"
                Button.create (ButtonContent.Text "Keep account")
                |> (if isPending then Button.disabled else id)
                |> Button.withAttributes [
                    _id $"{dialogId}-cancel"
                    _dataAttr ("disabled", pendingExpression)
                    _dataOn ("click", "el.closest('dialog').close()") ]
                |> Button.render
                Button.create (ButtonContent.Text "Delete account")
                |> Button.withColor ButtonColor.Error
                |> Button.withVariant ButtonVariant.Solid
                |> Button.asSubmit
                |> (if isPending then Button.pending else id)
                |> Button.withAttributes [
                    _id $"{dialogId}-confirm"
                    _dataAttr ("disabled", pendingExpression)
                    _dataAttr ("aria-busy", $"{pendingExpression} ? 'true' : null") ]
                |> Button.render
            }
        }

    let accountDeletionContent validation =
        accountDeletionForm "delete-account-confirmation" false validation

    let accountDeletionDialog =
        Dialog.create "delete-account-confirmation" "Delete account?" (accountDeletionContent None)
        |> Dialog.withDescription "Deleting Operating removes its draft entries. Posted entries remain in the audit history."
        |> Dialog.withInitialFocus "delete-account-confirmation-cancel"
        |> Dialog.withAttributes [
            _role "alertdialog"
            _dataSignals "{_delete_account_confirmation_pending: false}"
            _dataOn ("cancel", "$_delete_account_confirmation_pending && evt.preventDefault()") ]

    let pendingDeletionDialog =
        Dialog.create "pending-account-confirmation" "Delete account?" (accountDeletionForm "pending-account-confirmation" true None)
        |> Dialog.withDescription "The account deletion is already being checked."
        |> Dialog.withInitialFocus "pending-account-confirmation-status"
        |> Dialog.withAttributes [ _role "alertdialog"; _dataOn ("cancel", "evt.preventDefault()") ]

    let accountDrawerContent refreshed =
        div {
            _id "account-drawer-content"
            _class "grid gap-5"
            DescriptionList.create [
                DescriptionListItem.text "Account name" "Operating checking"
                DescriptionListItem.text "Type" "Asset"
                DescriptionListItem.text "Currency" "USD"
                DescriptionListItem.status "Status" (Badge.create "Active" |> Badge.withColor BadgeColor.Success |> Badge.render)
            ]
            |> DescriptionList.render
            p { _class "text-sm text-[var(--fve-muted-text)]"; "Last reconciled September 15, 2026 by Andy Meier." }
            Button.create (ButtonContent.Text "Refresh details")
            |> Button.withAttributes [ _id "account-drawer-refresh"; _dataOn ("click", "@get('/components/drawers/account').then(() => document.getElementById('account-drawer-refresh')?.focus())") ]
            |> Button.render
            if refreshed then
                p { _role "status"; _class "text-sm text-[var(--fve-positive-text)]"; "Account details refreshed from the server." }
        }

    let accountDrawerConfig =
        Drawer.create "account-settings-drawer" "Account details" (accountDrawerContent false)
        |> Drawer.withDescription "Review the selected account without leaving the current page."
        |> Drawer.withInitialFocus "account-drawer-refresh"

    let accountEditorBody =
        form {
            _id "account-editor-form"
            _class "grid gap-6"
            section {
                _ariaLabelledby "account-editor-basics"
                _class "grid gap-4"
                h3 { _id "account-editor-basics"; _class "text-base font-semibold"; "Account basics" }
                Input.create "accountName" "Account name"
                |> Input.withId "account-editor-name"
                |> Input.withValue "Operating checking"
                |> Input.withDescription "Use the name shown throughout reporting and reconciliation."
                |> Input.render
                Select.create "accountType" "Account type" id [
                    SelectOption.create "asset" "Asset"
                    SelectOption.create "liability" "Liability"
                    SelectOption.create "equity" "Equity"
                    SelectOption.create "revenue" "Revenue"
                    SelectOption.create "expense" "Expense"
                ]
                |> Select.withId "account-editor-type"
                |> Select.withSelected "asset"
                |> Select.render
                Input.create "accountNumber" "Account number"
                |> Input.withValue "1040"
                |> Input.render
            }
            section {
                _ariaLabelledby "account-editor-reporting"
                _class "grid gap-4 border-t border-[var(--fve-border)] pt-6"
                h3 { _id "account-editor-reporting"; _class "text-base font-semibold"; "Reporting" }
                Input.create "reportingGroup" "Reporting group"
                |> Input.withValue "Cash and cash equivalents"
                |> Input.render
                Textarea.create "accountNotes" "Internal notes"
                |> Textarea.withRows 5
                |> Textarea.withValue "Primary operating account for Northwind Outdoor."
                |> Textarea.withDescription "Only workspace members can read these notes."
                |> Textarea.render
            }
        }

    let accountEditorFooter =
        div {
            _class "flex flex-wrap justify-end gap-3"
            Button.create (ButtonContent.Text "Cancel")
            |> Button.withAttributes [ _dataOn ("click", "document.getElementById('account-editor-drawer')?.close()") ]
            |> Button.render
            Button.create (ButtonContent.Text "Save changes")
            |> Button.asSubmit
            |> Button.withAttributes [ _attr ("form", "account-editor-form") ]
            |> Button.render
        }

    let accountEditorDrawerConfig =
        Drawer.create "account-editor-drawer" "Edit account" accountEditorBody
        |> Drawer.withDescription "Update the account fields used by reporting and reconciliation."
        |> Drawer.withFooter accountEditorFooter
        |> Drawer.withInitialFocus "account-editor-name"
        |> Drawer.withSize DrawerSize.Large

    let filterDrawerConfig =
        Drawer.create "account-filters-drawer" "Account filters" (
            nav {
                _ariaLabel "Account filters"
                _class "grid gap-3"
                a { _href "/components/drawer#active"; "Active accounts" }
                a { _href "/components/drawer#archived"; "Archived accounts" }
            })
        |> Drawer.withSide DrawerSide.Start

    let topDrawerConfig =
        Drawer.create "account-summary-drawer" "Account summary" (div { _class "grid gap-2 text-sm"; p { "Operating balance: $42,800" }; p { _class "text-[var(--fve-muted-text)]"; "Reconciled through September 28." } })
        |> Drawer.withDescription "A horizontal drawer for a compact review task."
        |> Drawer.withSide DrawerSide.Top

    let bottomDrawerConfig =
        Drawer.create "account-history-drawer" "Account history" (ol { _class "grid gap-4 text-sm"; for index in 1..12 do li { $"Activity entry {index}" } })
        |> Drawer.withDescription "Long content scrolls while the heading and actions remain reachable."
        |> Drawer.withSide DrawerSide.Bottom
        |> Drawer.withSize DrawerSize.Large
        |> Drawer.withFooter (Button.create (ButtonContent.Text "Done") |> Button.withAttributes [ _dataOn ("click", "document.getElementById('account-history-drawer')?.close()") ] |> Button.render)

    let patchedAccountDrawerContent = accountDrawerContent true

    let private accountTypeFilter =
        Select.create "accountTypeFilter" "Filter by account type" id [
            SelectOption.create "all" "All types"
            SelectOption.create "asset" "Asset"
            SelectOption.create "liability" "Liability"
            SelectOption.create "equity" "Equity"
            SelectOption.create "revenue" "Revenue"
            SelectOption.create "expense" "Expense" ]
        |> Select.withSelected "all"
        |> Select.withVisuallyHiddenLabel
        |> Select.render

    let private toolbar =
        section {
            _role "region"
            _ariaLabel "Collection controls"
            div {
                _class "flex flex-col gap-2 sm:flex-row sm:items-center"
                div {
                    _class "min-w-40 flex-1"
                    Input.create "query" "Search accounts"
                    |> Input.withType InputType.Search
                    |> Input.withVisuallyHiddenLabel
                    |> Input.withAttributes [ _placeholder "Search accounts"; _dataBind "collectionquery"; _dataAttr ("disabled", "$collectionstate != 'ready'") ]
                    |> Input.render
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
                    _dataAttr ("disabled", "$collectionstate != 'ready'")
                    _class "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm ring-1 ring-[var(--fve-border)] outline-none focus:ring-2 focus:ring-[var(--fve-brand-ring)]"
                    option { _value "all"; "All types" }
                    option { _value "asset"; "Asset" }
                    option { _value "liability"; "Liability" }
                    option { _value "equity"; "Equity" }
                    option { _value "revenue"; "Revenue" }
                    option { _value "expense"; "Expense" }
                }
                button {
                    _type "button"
                    _dataOn ("click", "$collectionquery = ''; $collectiontype = 'all'")
                    _dataAttr ("disabled", "(!$collectionquery && $collectiontype == 'all') || $collectionstate != 'ready'")
                    _class "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] disabled:opacity-50"
                    "Clear filters"
                }
                button {
                    _type "button"
                    _dataOn ("click", "$collectionstate = 'pending'; setTimeout(() => $collectionstate = 'ready', 350)")
                    _dataAttr ("disabled", "$collectionstate != 'ready'")
                    _class "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] disabled:opacity-50"
                    "Refresh filters"
                }
                button {
                    _type "button"
                    _dataOn ("click", "$collectionstate = 'error'")
                    _dataAttr ("disabled", "$collectionstate != 'ready'")
                    _class "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] disabled:opacity-50"
                    "Simulate filter error"
                }
            }
            p {
                _dataShow "$collectionstate == 'pending'"
                _role "status"
                _ariaLive "polite"
                _class "text-sm text-[var(--fve-muted-text)]"
                "Refreshing filter options…"
            }
            div {
                _dataShow "$collectionstate == 'error'"
                _role "alert"
                _class "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)]"
                span { "Filter options are temporarily unavailable." }
                button {
                    _type "button"
                    _dataOn ("click", "$collectionstate = 'ready'")
                    _class "rounded-[var(--fve-radius-control)] px-3 py-2 font-semibold ring-1 ring-[var(--fve-critical-ring)]"
                    "Retry filters"
                }
            }
        }

    let private collectionActions =
        a {
            _href (shellDestinationUrl LedgerCreateAccount)
            _class "inline-flex items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] px-3 py-2 text-sm font-medium text-white hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
            "New account"
        }

    let defaultPageHeader = PageHeader.create "Account overview" |> PageHeader.render

    let defaultSection =
        Section.create (SectionHeader.create "Account details") (p { "Operating checking · USD" })
        |> Section.render

    let defaultPage =
        Page.create (PageHeader.create "Account overview") (p { "Review balances and recent activity." })
        |> Page.render

    let defaultCollection =
        Collection.create "Accounts" (ul { li { "Operating checking" }; li { "Reserve savings" } })
        |> Collection.render

    let defaultDetail =
        Detail.create "Operating checking" [ p { "Asset · USD" } ]
        |> Detail.render

    // Supply ordinary HTML for your table, filter controls, and actions.
    let collectionExample actions toolbar table =
        Collection.create "Accounts" table
        |> Collection.withDescription
            "Review balances and posting availability."
        |> Collection.withActions actions
        |> Collection.withToolbar toolbar
        |> Collection.render

    let private collectionSelectionForm =
        form {
            _class "grid gap-3"
            filteredAccountTable
            Button.create (ButtonContent.Text "Inspect selected accounts")
            |> Button.withAttributes [ _dataOn ("click", "document.getElementById('account-bulk-audit').textContent = 'Selected account IDs: ' + new FormData(evt.currentTarget.form).getAll('accountIds').join(', ')") ]
            |> Button.render
        }

    let collectionPage =
        div {
            _dataSignals "{collectionquery: '', collectiontype: 'all', collectionstate: 'ready'}"
            _dataInit "(() => { const params = new URL(window.location.href).searchParams; $collectionquery = params.get('query') || ''; $collectiontype = params.get('accountType') || 'all' })()"
            _dataEffect "(() => { const url = new URL(window.location.href); $collectionquery ? url.searchParams.set('query', $collectionquery) : url.searchParams.delete('query'); $collectiontype != 'all' ? url.searchParams.set('accountType', $collectiontype) : url.searchParams.delete('accountType'); window.history.replaceState(window.history.state, '', url) })()"
            collectionExample collectionActions toolbar collectionSelectionForm
            p {
                _dataShow $"!({collectionHasMatches})"
                _role "status"
                _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"
                "No accounts match these filters. Clear filters to restore all accounts."
            }
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
                    DescriptionListItem.status "Status" (Badge.create "Active" |> Badge.withColor BadgeColor.Success |> Badge.render)
                    DescriptionListItem.text "Type" "Asset"
                    DescriptionListItem.text "Commodity" "USD"
                    DescriptionListItem.text "Parent account" "Current assets"
                    DescriptionListItem.text "Source" "Created in Ledger"
                    DescriptionListItem.text "Month-end observed balance" "Required"
                    DescriptionListItem.text "Balance" "$38,442.11" ]
                |> DescriptionList.withColumns DescriptionListColumns.Three
                |> DescriptionList.render)
            |> Section.render
            SectionHeader.create "Transactions"
            |> SectionHeader.withDescription "Current assets · All accounts"
            |> SectionHeader.withActions (a { _href (resolve accountsDestination); _class "text-sm font-medium text-[var(--fve-brand-text)] underline-offset-4 hover:underline"; "View all accounts" })
            |> fun header -> Section.create header transactions
            |> Section.withLabel "Recent transactions"
            |> Section.render
        ]
        |> Detail.withActions actions
        |> Detail.render

    let detailPage =
        let actions =
            div {
                _class "flex flex-wrap items-center gap-2"
                PageExamples.link (shellDestinationUrl (LedgerAccount operatingAccount.id)) "View account"
                DropdownMenu.create "components-detail-actions" "More actions"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon moreActionsIcon)
                |> DropdownMenu.withContent (recordMenuItems "account" operatingAccount.id (LedgerAccount operatingAccount.id) shellDestinationUrl (System.Text.Json.JsonSerializer.Serialize operatingAccount) |> List.tail)
                |> DropdownMenu.render shellDestinationUrl
            }
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

    type DefaultSideNavDestination = Overview | Settings

    let defaultSideNavUrl = function
        | Overview -> "/components/side-nav"
        | Settings -> "/components/theming"

    let defaultSideNav =
        SideNav.create "default-side-navigation" "Primary navigation" (SideNavHeader.create "Workspace") [
            SideNavSection.ungrouped [
                SideNavItem.create Overview "Overview"
                SideNavItem.create Settings "Settings" ] ]
        |> SideNav.render defaultSideNavUrl

    // Resolve your destination values to URLs.
    let breadcrumbsExample resolve home accounts account =
        Breadcrumbs.create "ledger-breadcrumbs" "Breadcrumb" [
            BreadcrumbItem.create home "Home"
            BreadcrumbItem.create accounts "Accounts"
            BreadcrumbItem.create account "Operating checking"
        ]
        |> Breadcrumbs.render resolve

    let defaultSideNavPreview = div { defaultSideNav } |> themedSurface

    let breadcrumbsPreview =
        div {
            breadcrumbsExample shellDestinationUrl LedgerHome LedgerAccounts (LedgerAccount 2048)
        }
        |> themedSurface

    let breadcrumbTrail =
        [ BreadcrumbItem.create "/" "Home"
          BreadcrumbItem.create "/components" "Components"
          BreadcrumbItem.create "/components" "Components"
          BreadcrumbItem.create "/components/primitives#navigation" "Navigation"
          BreadcrumbItem.create "/components/breadcrumbs" "Breadcrumbs" ]

    let middleBreadcrumbsExample =
        Breadcrumbs.create "middle-breadcrumbs" "Middle overflow breadcrumb" breadcrumbTrail
        |> Breadcrumbs.render id

    let visibleBreadcrumbsExample =
        Breadcrumbs.create "visible-breadcrumbs" "Visible item count breadcrumb" breadcrumbTrail
        |> Breadcrumbs.withMaxVisibleItems 4
        |> Breadcrumbs.render id

    let private ledgerNavigation showMobileWorkspace current =
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
        let compactBrand = a { _href (shellDestinationUrl LedgerHome); _ariaLabel "Ledger home"; _class "flex size-8 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"; ledgerMark }
        let navigationHeader =
            SideNavHeader.create "Ledger"
            |> SideNavHeader.withContent (
                a {
                    _href (shellDestinationUrl LedgerHome)
                    _class "flex min-w-0 items-center gap-3 rounded-[var(--fve-radius-control)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                    span { _ariaHidden true; _class "flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white"; ledgerMark }
                    strong { _class "truncate text-base font-semibold"; "Ledger" }
                })
            |> SideNavHeader.withCompactContent compactBrand
        let accountAction = Button.create (ButtonContent.Icon ("Pin Accounts", raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path d="M7 2.75A.75.75 0 0 1 7.75 2h4.5a.75.75 0 0 1 .75.75v4.69l1.78 1.78a.75.75 0 0 1-.53 1.28h-3.5v6.75a.75.75 0 0 1-1.5 0V10.5h-3.5a.75.75 0 0 1-.53-1.28L7 7.44V2.75Z"/></svg>""")) |> Button.withVariant ButtonVariant.Ghost |> Button.withSize ControlSize.Small |> Button.withAttributes [ _dataOn ("click", "el.setAttribute('aria-pressed', el.getAttribute('aria-pressed') != 'true' ? 'true' : 'false')"); _ariaPressed false ] |> Button.render
        SideNav.create "ledger-side-navigation" "Ledger primary navigation" navigationHeader [
            SideNavSection.group "Manage" [
                shellItem LedgerHome "Dashboard"
                shellItem LedgerAccounts "Accounts"
                |> SideNavItem.withBadge (Badge.create "6" |> Badge.withColor BadgeColor.Neutral |> Badge.render)
                |> SideNavItem.withAction accountAction ]
            SideNavSection.group "Analyze" [
                SideNavItem.nested "Reporting" [ shellItem LedgerReports "Reports" ]
                |> SideNavItem.withLeading navigationGlyph
                |> SideNavItem.expanded ]
            SideNavSection.group "Configure" [ shellItem LedgerSettings "Settings" ] ]
        |> fun navigation ->
            match current with
            | LedgerTransaction _ -> navigation
            | _ -> SideNav.withCurrent navigationCurrent navigation
        |> SideNav.withWidth SideNavWidth.Standard
        |> SideNav.withContext workspace
        |> (if showMobileWorkspace then SideNav.withMobileContext workspace else id)
        |> SideNav.withFooter (
            a {
                _href (shellDestinationUrl LedgerSettings)
                _class "flex min-h-[var(--fve-control-min-height)] items-center gap-3 rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-navigation-padding-block)] text-sm font-semibold text-[var(--fve-text)] outline-none hover:bg-[var(--fve-surface-hover)] focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"
                span { _ariaHidden true; _class "flex size-8 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs text-[var(--fve-brand-text)]"; "AM" }
                span { _class "min-w-0 truncate"; "Andy Meier" }
            })
        |> SideNav.withCompactFooter (
            a { _href (shellDestinationUrl LedgerSettings); _ariaLabel "Andy Meier profile"; _class "flex size-8 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs font-semibold text-[var(--fve-brand-text)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"; "AM" })

    let groupedLedgerNavigation = ledgerNavigation true LedgerAccounts

    let sideNavigationPreview =
        div {
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
        Button.create (ButtonContent.Icon ("Refresh balances", refreshIcon))
        |> Button.withColor ButtonColor.Primary
        |> Button.withVariant ButtonVariant.Solid
        |> Button.withAttributes [ _dataOn ("click", "$ledgerRefreshes++") ]
        |> Button.render

    let private pageAction destination (label:string) =
        a {
            _href (shellDestinationUrl destination)
            _class "inline-flex min-h-8 items-center px-3 text-sm font-medium text-[var(--fve-brand-text)] underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
            label
        }

    // Consumer-authored responsive policy, used by the Account management page example.
    let accountPageActions id =
        div {
            _id id
            _class "flex shrink-0 items-center gap-2"
            refreshBalancesAction
            div {
                _class "hidden items-center gap-2 @min-[400px]:flex"
                pageAction LedgerReports "View reports"
                DropdownMenu.create (id + "-wide") "More actions"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon moreActionsIcon)
                |> DropdownMenu.withContent [ DropdownMenuItem.link LedgerSettings "Account settings" ]
                |> DropdownMenu.render shellDestinationUrl
            }
            div {
                _class "@min-[400px]:hidden"
                DropdownMenu.create (id + "-compact") "More actions"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon moreActionsIcon)
                |> DropdownMenu.withContent [
                    DropdownMenuItem.link LedgerReports "View reports"
                    DropdownMenuItem.link LedgerSettings "Account settings" ]
                |> DropdownMenu.render shellDestinationUrl
            }
        }

    // Breadcrumbs are optional content, not a built-in heading.
    let pageTopBarExample resolve breadcrumbs =
        PageTopBar.create ()
        |> PageTopBar.withContent (
            div {
                _class "flex min-w-0 flex-1 items-center"
                breadcrumbs |> Breadcrumbs.render resolve
            })

    let renderPageTopBar resolve breadcrumbs =
        pageTopBarExample resolve breadcrumbs |> PageTopBar.render

    let private pageTopBar breadcrumbs = pageTopBarExample shellDestinationUrl breadcrumbs

    let pageTopBarPreview =
        div {
            renderPageTopBar shellDestinationUrl (ledgerBreadcrumbs (LedgerAccount 2048))
        }
        |> themedSurface

    let shellTopBarExample =
        div {
            _class "@container/fve-shell"
            PageTopBar.create ()
            |> PageTopBar.withBrand (strong { _class "text-base font-semibold"; "Ledger" })
            |> PageTopBar.withContent (Breadcrumbs.create "shell-bar-breadcrumbs" "Breadcrumb" [
                BreadcrumbItem.create "/examples/application" "Home"
                BreadcrumbItem.create "/examples/application/accounts" "Accounts" ] |> Breadcrumbs.render id)
            |> PageTopBar.withActions (a { _href "/examples/application/profile"; _class "whitespace-nowrap text-sm font-medium"; "View profile" })
            |> PageTopBar.render
            div {
                _class "flex min-h-32"
                div {
                    _class "hidden w-60 shrink-0 @3xl/fve-shell:block"
                    SideNav.create "shell-top-bar-navigation" "Ledger" (SideNavHeader.create "Ledger") [
                        SideNavSection.ungrouped [ SideNavItem.create "/examples/application" "Home"; SideNavItem.create "/examples/application/accounts" "Accounts" ] ]
                    |> SideNav.withWidth SideNavWidth.Standard
                    |> SideNav.withoutHeader
                    |> SideNav.render id
                }
                div { _class "min-w-0 flex-1 px-4 py-6 sm:px-6 lg:px-8"; p { _class "text-sm text-[var(--fve-muted-text)]"; "Page region" } }
            }
        }

    let accountPageHeader =
        PageHeader.create "Account 2048"
        |> PageHeader.withSubtitle "Operating checking · Updated moments ago"
        |> PageHeader.withActions (
            div {
                _class "flex flex-wrap items-center gap-2"
                refreshBalancesAction
                pageAction LedgerReports "View reports"
            })

    let pageHeaderPreview =
        div {
            _dataSignals "{ledgerRefreshes: 0}"
            accountPageHeader |> PageHeader.render
            output { _class "sr-only"; _role "status"; _dataText "'Balance refreshes: ' + $ledgerRefreshes"; "Balance refreshes: 0" }
        }
        |> themedSurface

    let private treasuryNavigation current =
        let navigationCurrent =
            match current with
            | TreasuryPeriod _ -> TreasuryTransactions
            | TreasuryHome | TreasuryTransactions | TreasuryPayees | TreasuryAccounts -> current
            | _ -> TreasuryHome
        let workspace =
            div {
                p { _class "text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"; "Workspace" }
                p { _class "mt-1 truncate text-sm font-semibold"; "Meier Made" }
            }
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
        |> SideNav.withContext workspace
        |> SideNav.withMobileContext workspace
        |> SideNav.withFooter (
            div {
                _class "flex min-w-0 items-center gap-3 px-3 py-2"
                span { _ariaHidden true; _class "flex size-8 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs text-[var(--fve-brand-text)]"; "AM" }
                span { _class "min-w-0 truncate text-sm font-semibold"; "Andy Meier" }
            })

    let private upcomingPayments =
        let actions =
            div {
                _class "flex flex-wrap items-center gap-2"
                pageAction TreasuryPayees "View payees"
                DropdownMenu.create "upcoming-payment-actions" "More actions"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon moreActionsIcon)
                |> DropdownMenu.withContent [ DropdownMenuItem.link TreasuryAccounts "View accounts" ]
                |> DropdownMenu.render shellDestinationUrl
            }
        let header =
            SectionHeader.create "Upcoming transactions"
            |> SectionHeader.withDescription "Payments expected before the current period closes."
            |> SectionHeader.withActions actions
        Section.create header (
            ul {
                _role "list"
                _class "divide-y divide-[var(--fve-border)]"
                li { _class "flex items-center justify-between gap-4 py-3 text-sm"; span { "Aug 15 · Payroll" }; Badge.create "Ready" |> Badge.withColor BadgeColor.Success |> Badge.render }
                li { _class "flex items-center justify-between gap-4 py-3 text-sm"; span { "Aug 18 · Cloud hosting" }; Badge.create "Scheduled" |> Badge.withColor BadgeColor.Info |> Badge.render }
            })
        |> Section.render

    let private completedPayments =
        Section.create (SectionHeader.create "Completed transactions") (
            div {
                _class "flex items-center justify-between gap-4 py-3 text-sm"
                span { "Jul 28 · Northwind payment" }
                strong { _class "font-semibold"; "$4,800.00" }
            })
        |> Section.render

    let private transactionTabs =
        Tabs.create "treasury-transactions-tabs" "Transaction views" [
            TabItem.create "upcoming" "Upcoming" upcomingPayments
            TabItem.create "completed" "Completed" completedPayments ]
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

    let renderTransactionsPage topBar tabs =
        transactionsPage topBar tabs |> Page.render

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
                    Button.create (ButtonContent.Text "Zoom in") |> Button.withAttributes [ _dataOn ("click", "$postingZoom = Math.min(2, $postingZoom + 0.25)") ] |> Button.render
                    Button.create (ButtonContent.Text "Zoom out") |> Button.withAttributes [ _dataOn ("click", "$postingZoom = Math.max(0.5, $postingZoom - 0.25)") ] |> Button.render
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

    let private accountWorkspaceUrl (workspace:AccountWorkspace) =
        let encode (value:string) = Uri.EscapeDataString value
        let columns = workspace.visibleColumns |> Set.toList |> List.sort |> String.concat ","
        $"/components/page-examples/account-management?destination=ledger-accounts&query={encode workspace.searchQuery}&accountType={encode workspace.filterType}&sort={workspace.sortColumn}&direction={workspace.sortDirection}&page={workspace.page}&columns={columns}"

    let private accountWorkspacePatch url =
        let target = System.Text.Json.JsonSerializer.Serialize url
        $"window.history.pushState(null, '', {target}); @get({target}.replace('/components/page-examples/account-management?', '/components/page-examples/account-management/fixture?'))"

    let private accountDataTable workspace accountRows =
        let sort column =
            let active = workspace.sortColumn = column
            let direction = if active && workspace.sortDirection = "asc" then "desc" else "asc"
            let destination = accountWorkspaceUrl { workspace with sortColumn=column; sortDirection=direction; page=1 }
            if active && workspace.sortDirection = "asc" then TableSort.ascending destination
            elif active then TableSort.descending destination
            else TableSort.by destination
        let columns = [
            TableColumn.create "Account" (fun (row:AccountRow) -> a { _href (shellDestinationUrl (LedgerAccount row.id)); _class "font-medium text-[var(--fve-brand-text)]"; row.name })
            |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary |> TableColumn.withSort (sort "name")
            if workspace.visibleColumns.Contains "type" then TableColumn.create "Type" (fun row -> text row.accountType)
            if workspace.visibleColumns.Contains "commodity" then TableColumn.create "Commodity" (fun row -> text row.commodity)
            if workspace.visibleColumns.Contains "balance" then TableColumn.create "Balance" (fun row -> text (money row.balance)) |> TableColumn.alignEnd |> TableColumn.withSort (sort "balance")
            if workspace.visibleColumns.Contains "total" then TableColumn.create "Total balance (USD) · Current" (fun row -> text (money row.totalBalance)) |> TableColumn.alignEnd
            TableColumn.rowActions (fun row ->
                DropdownMenu.create $"data-account-{row.id}-actions" $"More actions for {row.name}"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon moreActionsIcon)
                |> DropdownMenu.withContent (recordMenuItems "account" row.id (LedgerAccount row.id) shellDestinationUrl (System.Text.Json.JsonSerializer.Serialize row))
                |> DropdownMenu.render shellDestinationUrl) ]
        Table.create "Account data" columns accountRows
        |> Table.withDensity Density.Compact
        |> Table.withMobileLayout TableMobileLayout.Records
        |> Table.withSelection (TableSelection.create "account-data-selection" (fun (row:AccountRow) -> string row.id) (fun row -> row.name) |> TableSelection.withFormName "accountIds")
        |> Table.render

    let private ledgerPage workspace current =
        let page actions subtitle (content:HtmlElement) =
            PageHeader.create (ledgerTitle current)
            |> PageHeader.withSubtitle subtitle
            |> PageHeader.withActions actions
            |> fun header -> Page.create header (div { _class "min-w-0 px-4 pb-4 sm:px-6 sm:pb-6 lg:px-8 lg:pb-8"; content })
            |> Page.withWidth PageWidth.Wide
            |> Page.withBodyLayout PageBodyLayout.FullBleed

        match current with
        | LedgerAccounts ->
            let actions =
                div {
                    _class "flex flex-wrap items-center gap-2"
                    a {
                        _href (shellDestinationUrl LedgerCreateAccount)
                        _class "inline-flex min-h-8 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] px-3 py-1.5 text-sm font-medium leading-5 text-white hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
                        "Create"
                    }
                    DropdownMenu.create "ledger-accounts-page-actions" "More actions"
                    |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon moreActionsIcon)
                    |> DropdownMenu.withContent [ DropdownMenuItem.link LedgerSettings "Account settings" ]
                    |> DropdownMenu.render shellDestinationUrl
                }
            let filtered = rows |> List.filter (fun row -> row.name.Contains(workspace.searchQuery,StringComparison.OrdinalIgnoreCase) && (workspace.filterType="all" || row.accountType.Equals(workspace.filterType,StringComparison.OrdinalIgnoreCase)))
            let sorted =
                match workspace.sortColumn, workspace.sortDirection with
                | "balance", "desc" -> filtered |> List.sortByDescending _.balance
                | "balance", _ -> filtered |> List.sortBy _.balance
                | _, "desc" -> filtered |> List.sortByDescending _.name
                | _ -> filtered |> List.sortBy _.name
            let pageSize = 3
            let pageCount = max 1 ((sorted.Length + pageSize - 1) / pageSize)
            let currentPage = min workspace.page pageCount
            let displayed = sorted |> List.skip ((currentPage - 1) * pageSize) |> List.truncate pageSize
            let visibleColumns = workspace.visibleColumns |> Set.toList |> List.sort |> String.concat ","
            let columnItems : DropdownMenuItem<string> list =
                [ for key, label in [ "type", "Type"; "commodity", "Commodity"; "balance", "Balance"; "total", "Total balance" ] do
                    let nextColumns = if workspace.visibleColumns.Contains key then workspace.visibleColumns.Remove key else workspace.visibleColumns.Add key
                    let destination = accountWorkspaceUrl { workspace with visibleColumns=nextColumns; page=1 }
                    DropdownMenuItem.checkbox (accountWorkspacePatch destination) label
                    |> DropdownMenuItem.withChecked (workspace.visibleColumns.Contains key) ]
            let columnMenu =
                DropdownMenu.create "account-columns" "Choose columns"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "Columns")
                |> DropdownMenu.withContent columnItems
                |> DropdownMenu.render id
            let controls =
                div {
                    _class "flex flex-wrap items-end gap-2"
                    form {
                        _method "get"; _action "/components/page-examples/account-management"
                        _class "flex min-w-0 flex-1 flex-wrap items-end gap-2"
                        input { _type "hidden"; _name "destination"; _value "ledger-accounts" }
                        input { _type "hidden"; _name "sort"; _value workspace.sortColumn }
                        input { _type "hidden"; _name "direction"; _value workspace.sortDirection }
                        input { _type "hidden"; _name "columns"; _value visibleColumns }
                        div { _class "min-w-0 basis-full sm:min-w-48 sm:flex-1 sm:basis-auto"; Input.create "query" "Search accounts" |> Input.withType InputType.Search |> Input.withValue workspace.searchQuery |> Input.render }
                        div { _class "min-w-0 flex-[1_1_10rem] sm:flex-none"; Select.create "accountType" "Filter by account type" id [ for value,label in ["all","All types";"asset","Asset";"liability","Liability";"equity","Equity";"revenue","Revenue";"expense","Expense"] -> SelectOption.create value label ] |> Select.withSelected workspace.filterType |> Select.render }
                        Button.create (ButtonContent.Text "Apply filters") |> Button.asSubmit |> Button.render
                    }
                    columnMenu
                    if workspace.searchQuery <> "" || workspace.filterType <> "all" then
                        a { _href (accountWorkspaceUrl { defaultAccountWorkspace with visibleColumns=workspace.visibleColumns }); _class "inline-flex min-h-10 items-center px-2 text-sm font-medium text-[var(--fve-brand-text)]"; "Clear filters" }
                }
            let dataTable =
                if filtered.IsEmpty then EmptyState.create "No matching accounts" "Change the search or clear your filters." |> EmptyState.render
                else accountDataTable workspace displayed
            let pagination =
                if filtered.IsEmpty then div { }
                else
                    let pageUrl number = accountWorkspaceUrl { workspace with page=number }
                    let items = [ for number in 1..pageCount -> if number = currentPage then PaginationItem.current number else PaginationItem.link number (pageUrl number) ]
                    Pagination.create "Account pages" items
                    |> (if currentPage > 1 then Pagination.withPrevious (pageUrl (currentPage - 1)) else id)
                    |> (if currentPage < pageCount then Pagination.withNext (pageUrl (currentPage + 1)) else id)
                    |> Pagination.withSummary (span { $"Showing {(currentPage - 1) * pageSize + 1}–{min (currentPage * pageSize) filtered.Length} of {filtered.Length} accounts" })
                    |> Pagination.render id
            let content =
                div { _class "grid gap-4"; dataTable |> recordActionFeedback "account"; pagination }
                |> Collection.create "Accounts"
                |> Collection.withVisuallyHiddenTitle
                |> Collection.withToolbar controls
                |> Collection.render
            page actions "Chart of accounts · Current valuation" content

        | LedgerAccount accountId ->
            let account = rows |> List.tryFind (fun row -> row.id = accountId) |> Option.defaultValue operatingAccount
            let actions = accountPageActions $"ledger-account-{accountId}-page-actions"
            let content =
                Detail.create (ledgerTitle current) [
                    Section.create (SectionHeader.create "Detail") (
                        DescriptionList.create [
                            DescriptionListItem.status "Status" (Badge.create "Active" |> Badge.withColor BadgeColor.Success |> Badge.render)
                            DescriptionListItem.text "Type" account.accountType
                            DescriptionListItem.text "Commodity" account.commodity
                            DescriptionListItem.text "Parent account" (if accountId = operatingAccount.id then "Current assets" else "None")
                            DescriptionListItem.text "Source" "Created in Ledger"
                            DescriptionListItem.text "Month-end observed balance" (if accountId = operatingAccount.id then "Required" else "Not required")
                            DescriptionListItem.text "Balance" (money account.balance) ]
                        |> DescriptionList.withColumns DescriptionListColumns.Three
                        |> DescriptionList.render)
                    |> Section.render
                    SectionHeader.create "Transactions"
                    |> SectionHeader.withDescription "Current assets · All accounts"
                    |> fun header -> Section.create header (transactionTableFor LedgerTransaction shellDestinationUrl)
                    |> Section.withLabel "Account transactions"
                    |> Section.render
                ]
                |> Detail.withVisuallyHiddenTitle
                |> Detail.render
            page actions "Asset · USD · Updated moments ago" content

        | LedgerTransaction transactionId ->
            let row = transactionRows |> List.find (fun row -> row.id = transactionId)
            let actions = pageAction LedgerAccounts "View accounts"
            let content =
                Section.create (SectionHeader.create "Detail") (
                    DescriptionList.create [
                        DescriptionListItem.text "Transaction ID" (string row.id)
                        DescriptionListItem.text "Date" row.date
                        DescriptionListItem.text "Status" (string row.status)
                        DescriptionListItem.text "Accounts" row.accounts
                        DescriptionListItem.text "Amount" (money row.amount) ]
                    |> DescriptionList.render)
                |> Section.render
            page actions "Transaction details" content

        | LedgerCreateAccount ->
            let actions = empty
            let content =
                form {
                    _method "post"
                    _action "/components/page-examples/account-management/create"
                    _class "grid max-w-xl gap-5"
                    Input.create "name" "Account name" |> Input.withValue workspace.draftName |> Input.withAttributes [ _required true; _maxlength 80 ] |> Input.render
                    Select.create "accountType" "Account type" id [ for kind in ["Asset";"Liability";"Equity";"Revenue";"Expense"] -> SelectOption.create kind kind ] |> Select.withSelected workspace.draftType |> Select.render
                    DescriptionList.create [ DescriptionListItem.text "Commodity" "USD" ]
                    |> DescriptionList.withColumns DescriptionListColumns.One
                    |> DescriptionList.render
                    p { _role "status"; _class "text-sm text-[var(--fve-muted-text)]"; workspace.feedback }
                    div {
                        _class "flex flex-wrap items-center gap-2"
                        Button.create (ButtonContent.Text "Create account") |> Button.asSubmit |> Button.withColor ButtonColor.Primary |> Button.withVariant ButtonVariant.Solid |> Button.render
                        pageAction LedgerAccounts "Cancel"
                    }
                }
            page actions "Add an account to the chart of accounts." content

        | LedgerHome ->
            let actions = pageAction LedgerAccounts "View accounts"
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
            page actions (workspace.workspaceName + " · Production") (div { _class "grid gap-8"; content; PageExamples.balanceChart PageExamples.defaultQuery; upcomingPayments })

        | LedgerReports ->
            page (pageAction LedgerAccounts "View accounts") "Operating checking · USD" (div { _class "grid gap-6"; PageExamples.balanceChart PageExamples.defaultQuery; transactionTableFor LedgerTransaction shellDestinationUrl })

        | LedgerSettings ->
            let actions = empty
            let content =
                form {
                    _method "post"; _action "/components/page-examples/account-management/settings"
                    _class "grid max-w-xl gap-5"
                    Input.create "workspaceName" "Workspace name" |> Input.withValue workspace.workspaceName |> Input.withAttributes [_required true; _maxlength 80] |> Input.render
                    DescriptionList.create [
                        DescriptionListItem.text "Reporting currency" workspace.currency
                        |> DescriptionListItem.withDescription "The example ledger is denominated in USD." ]
                    |> DescriptionList.withColumns DescriptionListColumns.One
                    |> DescriptionList.render
                    Checkbox.create "emailUpdates" "Receive weekly summaries" |> Checkbox.withChecked workspace.emailUpdates |> Checkbox.render
                    p { _role "status"; _class "text-sm text-[var(--fve-muted-text)]"; workspace.feedback }
                    div {
                        _class "flex flex-wrap items-center gap-2"
                        Button.create (ButtonContent.Text "Save settings") |> Button.asSubmit |> Button.withColor ButtonColor.Primary |> Button.withVariant ButtonVariant.Solid |> Button.render
                        pageAction LedgerAccounts "Cancel"
                    }
                }
            page actions (workspace.workspaceName + " · Production") content

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
                    match current with
                    | TreasuryAccounts -> accountTable
                    | TreasuryPayees ->
                        Table.create "Payees" [ TableColumn.create "Payee" (fun (name,_,_) -> text name) |> TableColumn.asRowHeader |> TableColumn.asMobilePrimary; TableColumn.create "Latest transaction" (fun (_,label,id) -> a { _href (shellDestinationUrl (LedgerTransaction id)); _class "text-[var(--fve-brand-text)]"; text label }) ] ["Northwind","Northwind payment",201;"Cloud hosting","Cloud hosting",202;"Office supplier","Office supplies",204] |> Table.withMobileLayout TableMobileLayout.Records |> Table.render
                    | _ -> div { _class "grid gap-8"; Metric.text "Operating balance" "$38,442.11" |> Metric.withDescription "USD · Available cash" |> Metric.render; upcomingPayments; completedPayments }
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
            |> ComponentsTheme.withDensity Density.Compact
            |> ComponentsTheme.withControlSize ControlSize.Small)
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
            ComponentsTheme.sky
            |> ComponentsTheme.withRadius Radius.Medium
            |> ComponentsTheme.withDensity Density.Compact
            |> ComponentsTheme.withControlSize ControlSize.Small)
        |> AppShell.withBoundary AppShellBoundary.Container
        |> AppShell.withMobileBottomNavigation "treasury-bottom-navigation" "Treasury quick navigation" [
            BottomNavigationItem.create TreasuryHome "Overview"
            BottomNavigationItem.create TreasuryTransactions "Transactions"
            BottomNavigationItem.create TreasuryPayees "Payees"
            BottomNavigationItem.create TreasuryAccounts "Accounts" ]
        |> AppShell.asPreview "Treasury application preview"
        |> AppShell.render resolve

    let ledgerShellWith workspace current =
        let page =
            div {
                _class "flex h-full min-h-0 flex-col"
                _dataSignals "{ledgerRefreshes: 0, ledgerCreates: 0}"
                div { _class "hidden shrink-0 md:block"; pageTopBar (ledgerBreadcrumbs current) |> PageTopBar.render }
                div { _class "min-h-0 flex-1"; ledgerPage workspace current |> Page.render }
                output { _class "sr-only"; _role "status"; _dataText "'Balance refreshes: ' + $ledgerRefreshes"; "Balance refreshes: 0" }
            }
        ledgerShellExample shellDestinationUrl (ledgerNavigation false current) page

    let ledgerShell current = ledgerShellWith defaultAccountWorkspace current

    let treasuryShell current =
        treasuryShellExample shellDestinationUrl (treasuryNavigation current) (treasuryPage current |> Page.render)

    let private shellPreviewWith workspace current =
        match current with
        | LedgerHome | LedgerAccounts | LedgerAccount _ | LedgerTransaction _ | LedgerCreateAccount | LedgerReports | LedgerSettings -> ledgerShellWith workspace current
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

    let private shellFixtureConfigWith workspace current =
        let fixture =
            Browser.create (shellPreviewWith workspace current)
            |> Browser.withAddress ("https://fve.meiermade.com" + shellDestinationUrl current)
            |> Browser.render
            |> Fixture.create "ledger-workflow" "Ledger account workflow" (shellDestinationUrl current)
            |> Fixture.withFullscreenContent (shellPreviewWith workspace current)
        let fixture = appModePrevious current |> Option.map (fun previous -> Fixture.withPrevious previous fixture) |> Option.defaultValue fixture
        appModeNext current |> Option.map (fun next -> Fixture.withNext next fixture) |> Option.defaultValue fixture

    let shellFixtureWith workspace current =
        div {
            _id "components-app-shell-fixture"
            for attribute in shellFixtureNavigationAttributes do attribute
            shellFixtureConfigWith workspace current |> Fixture.render
        }

    let shellFixtureFor current = shellFixtureWith defaultAccountWorkspace current

    let private registration id path navLabel title : DocPage =
        { id = id
          path = path
          aliases = []
          navLabel = navLabel
          category = "Components"
          title = title
          browserTitle = $"{title} · FSharp.ViewEngine.Components"
          nodes = [] }

    let private applicationRegistration id path navLabel title =
        registration id path navLabel title

    let private packageRegistration id path navLabel title =
        { registration id path navLabel title with category = "Source distribution" }

    let overviewRegistration =
        packageRegistration "components-overview" "/components" "Overview" "Components"

    let installationRegistration =
        packageRegistration "components-installation" "/components/installation" "Installation" "Installation"

    let buttonRegistration = registration "components-button" "/components/button" "Button" "Button"
    let buttonGroupRegistration = registration "components-button-group" "/components/button-group" "Button group" "Button group"
    let kbdRegistration = registration "components-kbd" "/components/kbd" "Kbd" "Kbd"
    let badgeRegistration = registration "components-badge" "/components/badge" "Badge" "Badge"
    let loadingIndicatorRegistration = registration "components-loading-indicator" "/components/loading-indicator" "Loading indicator" "Loading indicator"
    let progressRegistration = registration "components-progress" "/components/progress" "Progress" "Progress"
    let emptyStateRegistration = registration "components-empty-state" "/components/empty-state" "Empty state" "Empty state"
    let skeletonRegistration = registration "components-skeleton" "/components/skeleton" "Skeleton" "Skeleton"
    let separatorRegistration = registration "components-separator" "/components/separator" "Separator" "Separator"
    let itemRegistration = registration "components-item" "/components/item" "Item" "Item"
    let tableRegistration = registration "components-table" "/components/table" "Table" "Table"
    let descriptionListRegistration = registration "components-description-list" "/components/description-list" "Description list" "Description list"
    let metricRegistration = registration "components-metric" "/components/metric" "Metric" "Metric"
    let paginationRegistration = registration "components-pagination" "/components/pagination" "Pagination" "Pagination"
    let avatarRegistration = registration "components-avatar" "/components/avatar" "Avatar" "Avatar"
    let copyRevealRegistration = registration "components-copy-reveal" "/components/copy-reveal" "Copy and reveal" "Copy and reveal"
    let inputRegistration = registration "components-input" "/components/input" "Input" "Input"
    let fieldRegistration = registration "components-field" "/components/field" "Field" "Field"
    let inputGroupRegistration = registration "components-input-group" "/components/input-group" "Input group" "Input group"
    let fileSelectionRegistration = registration "components-file-selection" "/components/file-selection" "File selection" "File selection"
    let tagInputRegistration = registration "components-tag-input" "/components/tag-input" "Tag input" "Tag input"
    let formLayoutsRegistration = applicationRegistration "components-form-layouts" "/components/form-layouts" "Form layouts" "Form layouts"
    let textareaRegistration = registration "components-textarea" "/components/textarea" "Textarea" "Textarea"
    let errorSummaryRegistration = registration "components-error-summary" "/components/error-summary" "Error summary" "Error summary"
    let noticeRegistration = registration "components-notice" "/components/notice" "Notice" "Notice"
    let notificationRegistration = registration "components-notification" "/components/notification" "Notification" "Notification"
    let selectRegistration = registration "components-select" "/components/select" "Select" "Select"
    let datePickerRegistration = registration "components-date-picker" "/components/date-picker" "Date picker" "Date picker"
    let checkboxRegistration = registration "components-checkbox" "/components/checkbox" "Checkbox" "Checkbox"
    let switchRegistration = registration "components-switch" "/components/switch" "Switch" "Switch"
    let toggleButtonRegistration = registration "components-toggle-button" "/components/toggle-button" "Toggle button" "Toggle button"
    let toggleGroupRegistration = registration "components-toggle-group" "/components/toggle-group" "Toggle group" "Toggle group"
    let breadcrumbsRegistration = registration "components-breadcrumbs" "/components/breadcrumbs" "Breadcrumbs" "Breadcrumbs"
    let sideNavRegistration = registration "components-side-nav" "/components/side-nav" "Side nav" "Side nav"
    let tabsRegistration = registration "components-tabs" "/components/tabs" "Tabs" "Tabs"
    let radioGroupRegistration = registration "components-radio-group" "/components/radio-group" "Radio group" "Radio group"
    let choiceCardsRegistration = registration "components-choice-cards" "/components/choice-cards" "Choice cards" "Choice cards"
    let dropdownMenuRegistration = registration "components-dropdown-menu" "/components/dropdown-menu" "Dropdown menu" "Dropdown menu"
    let commandRegistration = registration "components-command" "/components/command" "Command" "Command"
    let dialogRegistration = registration "components-dialog" "/components/dialog" "Dialog" "Dialog"
    let drawerRegistration = registration "components-drawer" "/components/drawer" "Drawer" "Drawer"
    let floatingPanelRegistration = registration "components-floating-panel" "/components/floating-panel" "Floating panel" "Floating panel"
    let tooltipRegistration = registration "components-tooltip" "/components/tooltip" "Tooltip" "Tooltip"
    let popoverRegistration = registration "components-popover" "/components/popover" "Popover" "Popover"
    let pageTopBarRegistration = applicationRegistration "components-page-top-bar" "/components/page-top-bar" "Page top bar" "Page top bar"
    let pageHeaderRegistration = applicationRegistration "components-page-header" "/components/page-header" "Page header" "Page header"
    let sectionRegistration = applicationRegistration "components-section" "/components/section" "Section heading" "Section heading"
    let browserRegistration = registration "components-browser" "/components/browser" "Browser" "Browser"
    let resizableRegistration = registration "components-resizable" "/components/resizable" "Resizable" "Resizable"
    let phoneRegistration = registration "components-phone" "/components/phone" "Phone" "Phone"
    let pageRegistration = applicationRegistration "components-page" "/components/page" "Page" "Page"
    let collectionRegistration = applicationRegistration "components-collection" "/components/collection" "Collection" "Collection"
    let detailRegistration = applicationRegistration "components-detail" "/components/detail" "Detail" "Detail"
    let appShellRegistration = applicationRegistration "components-app-shell" "/components/app-shell" "App shell" "App shell"
    let bottomNavigationRegistration = applicationRegistration "components-bottom-navigation" "/components/bottom-navigation" "Bottom navigation" "Bottom navigation"
    let messageRegistration = applicationRegistration "components-message" "/components/message" "Message" "Message"
    let uploadRegistration = { registration "components-upload" "/components/upload-list" "Upload list" "Upload list" with aliases=["/components/upload"] }
    let stepsRegistration = applicationRegistration "components-steps" "/components/steps" "Steps" "Steps"
    let firstStepsRegistration = applicationRegistration "components-first-steps" "/components/first-steps" "First steps" "First steps"
    let dayCalendarRegistration = registration "components-day-calendar" "/components/day-calendar" "Day calendar" "Day calendar"
    let weekCalendarRegistration = registration "components-week-calendar" "/components/week-calendar" "Week calendar" "Week calendar"
    let monthCalendarRegistration = registration "components-month-calendar" "/components/month-calendar" "Month calendar" "Month calendar"
    let yearCalendarRegistration = registration "components-year-calendar" "/components/year-calendar" "Year calendar" "Year calendar"
    let mediaLibraryRegistration = applicationRegistration "components-media-library" "/components/media-library" "Media library" "Media library"
    let accountManagementRegistration = applicationRegistration "components-account-management" "/components/page-examples/account-management" "Account management" "Account management"
    let interactionRegistration = packageRegistration "components-interaction" "/components/interaction-and-server-state" "Interaction and server state" "Interaction and server state"
    let accessibilityRegistration = packageRegistration "components-accessibility" "/components/accessibility" "Accessibility" "Accessibility"
    let themingRegistration = packageRegistration "components-theming" "/components/theming" "Theming and density" "Theming and density"
    let tailwindRegistration = packageRegistration "components-tailwind" "/components/tailwind-css" "Tailwind CSS" "Tailwind CSS setup"
    let customizationRegistration = packageRegistration "components-customization" "/components/customization" "Customization" "Customization"
    let versioningRegistration = packageRegistration "components-versioning" "/components/versioning" "Versioning" "Versioning"

    let actionRegistrations = [ buttonRegistration; buttonGroupRegistration; dropdownMenuRegistration; toggleButtonRegistration; toggleGroupRegistration ]
    let feedbackRegistrations = [ badgeRegistration; noticeRegistration; notificationRegistration; loadingIndicatorRegistration; progressRegistration; emptyStateRegistration; skeletonRegistration ]
    let dataDisplayRegistrations = [ tableRegistration; descriptionListRegistration; metricRegistration; avatarRegistration; copyRevealRegistration; dayCalendarRegistration; weekCalendarRegistration; monthCalendarRegistration; yearCalendarRegistration; itemRegistration; kbdRegistration; separatorRegistration ]
    let formControlRegistrations =
        [ inputRegistration
          inputGroupRegistration
          fieldRegistration
          textareaRegistration
          fileSelectionRegistration
          tagInputRegistration
          errorSummaryRegistration
          selectRegistration
          datePickerRegistration
          checkboxRegistration
          switchRegistration
          radioGroupRegistration
          choiceCardsRegistration ]
    let navigationRegistrations = [ breadcrumbsRegistration; sideNavRegistration; tabsRegistration; paginationRegistration; commandRegistration ]
    let overlayRegistrations = [ dialogRegistration; drawerRegistration; floatingPanelRegistration; popoverRegistration; tooltipRegistration ]
    let compositionRegistrations = [ pageTopBarRegistration; pageHeaderRegistration ]
    let frameRegistrations = [ browserRegistration; phoneRegistration; resizableRegistration ]
    let applicationNavigationRegistrations = [ bottomNavigationRegistration ]
    let applicationWorkflowRegistrations = [ messageRegistration; uploadRegistration; stepsRegistration; firstStepsRegistration ]
    let applicationResourceRegistrations : DocPage list = []
    let pageExampleRegistrations : DocPage list = []
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
        @ feedbackRegistrations
        @ dataDisplayRegistrations
        @ formControlRegistrations
        @ navigationRegistrations
        @ overlayRegistrations
        @ compositionRegistrations
        @ frameRegistrations
        @ applicationNavigationRegistrations
        @ applicationWorkflowRegistrations
        @ applicationResourceRegistrations
        @ pageExampleRegistrations
        @ guideRegistrations

    let page = overviewRegistration

    let private themeExample = """let theme =
    ComponentsTheme.emerald
    |> ComponentsTheme.withRadius Radius.Large
    |> ComponentsTheme.withDensity Density.Compact
    |> ComponentsTheme.withControlSize ControlSize.Medium

div {
    ComponentsTheme.attributes theme
    p { "Consumer-owned workspace" }
}"""

    let private tailwindExample = """@import "tailwindcss" source(none);
@source "./src/Acme.Components/Components/**/*.fs";
@source "./src/Acme.Web/**/*.fs";"""

    let private supportedVariableDefaults = """/* Supported customization tokens and defaults. */
.fve-components {
  --fve-background: oklch(98.5% 0.002 247.839);
  --fve-surface: oklch(100% 0 0);
  --fve-surface-subtle: oklch(96.7% 0.003 264.542);
  --fve-surface-hover: oklch(96.7% 0.003 264.542);
  --fve-surface-active: oklch(92.8% 0.006 264.531);
  --fve-text: oklch(21% 0.034 264.665);
  --fve-muted-text: oklch(44.6% 0.03 256.802);
  --fve-border: oklch(87.2% 0.01 258.338);
  --fve-overlay-backdrop: oklch(13% 0.028 261.692 / 55%);
  --fve-neutral-subtle: oklch(96.7% 0.003 264.542);
  --fve-neutral-text: oklch(37.3% 0.034 259.733);
  --fve-neutral-ring: oklch(70.7% 0.022 261.325);
  --fve-brand-subtle: oklch(97.7% 0.013 236.62);
  --fve-brand-solid: oklch(50% 0.134 242.749);
  --fve-brand-hover: oklch(44.3% 0.11 240.79);
  --fve-brand-active: oklch(39.1% 0.09 240.876);
  --fve-brand-text: oklch(44.3% 0.11 240.79);
  --fve-brand-ring: oklch(68.5% 0.169 237.323);
  --fve-positive-subtle: oklch(96.2% 0.044 156.743);
  --fve-positive-text: oklch(44.8% 0.119 151.328);
  --fve-positive-ring: oklch(72.3% 0.219 149.579);
  --fve-warning-subtle: oklch(97.3% 0.071 103.193);
  --fve-warning-text: oklch(47.6% 0.114 61.907);
  --fve-warning-ring: oklch(76.9% 0.188 70.08);
  --fve-critical-subtle: oklch(97.1% 0.013 17.38);
  --fve-critical-solid: oklch(57.7% 0.245 27.325);
  --fve-critical-hover: oklch(50.5% 0.213 27.518);
  --fve-critical-active: oklch(44.4% 0.177 26.899);
  --fve-critical-text: oklch(44.4% 0.177 26.899);
  --fve-critical-ring: oklch(63.7% 0.237 25.331);
  --fve-info-subtle: oklch(97% 0.014 254.604);
  --fve-info-text: oklch(48.8% 0.243 264.376);
  --fve-info-ring: oklch(62.3% 0.214 259.815);
  --fve-radius-control: 0.5rem;
  --fve-radius-panel: 0.75rem;
  --fve-control-min-height: 2.5rem;
  --fve-control-padding-block: 0.5rem;
  --fve-control-font-size: 1rem;
  --fve-control-line-height: 1.5rem;
  --fve-navigation-min-height: 2.25rem;
  --fve-navigation-padding-block: 0.5rem;
  --fve-shell-bar-min-height: 4rem;
  --fve-shell-sidebar-width: 15rem;
  --fve-popup-active-background: var(--fve-brand-solid);
  --fve-popup-active-text: white;
  --fve-table-control-size: 1.75rem;
  --fve-table-padding-block-compact: 0.25rem;
  --fve-table-padding-block-comfortable: 0.75rem;
  --fve-table-padding-inline: 0.75rem;
}"""

    let paletteCustomizationVariables =
        [ for color in [ "primary"; "secondary"; "success"; "warning"; "error"; "info"; "neutral" ] do
            for role in [ "solid"; "hover"; "active"; "on-solid"; "soft"; "soft-hover"; "soft-active"; "text"; "border"; "focus" ] do
                $"--fve-{color}-{role}" ]
        |> String.concat "\n"

    let private rendererOwnedVariables = """/* Renderer-owned implementation variables; do not customize. */
--fve-calendar-all-day-rows
--fve-calendar-days
--fve-calendar-duration
--fve-calendar-hours
--fve-calendar-lane
--fve-calendar-lanes
--fve-calendar-start
--fve-row-background
--fve-table-background
--fve-side-nav-width
--fve-docs-code-surface
--fve-color-solid
--fve-color-hover
--fve-color-active
--fve-color-on-solid
--fve-color-soft
--fve-color-soft-hover
--fve-color-soft-active
--fve-color-text
--fve-color-border
--fve-color-focus"""

    let basicButton = Button.create (ButtonContent.Text "Continue") |> Button.render

    let buttonColorExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"
            for label, color in [
                "Primary", ButtonColor.Primary; "Secondary", ButtonColor.Secondary
                "Success", ButtonColor.Success; "Warning", ButtonColor.Warning
                "Error", ButtonColor.Error; "Info", ButtonColor.Info; "Neutral", ButtonColor.Neutral
            ] do
                Button.create (ButtonContent.Text label)
                |> Button.withColor color
                |> Button.withVariant ButtonVariant.Solid
                |> Button.render
        }

    let buttonVariantExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"
            for label, variant in [ "Solid", ButtonVariant.Solid; "Soft", ButtonVariant.Soft; "Outline", ButtonVariant.Outline; "Ghost", ButtonVariant.Ghost ] do
                Button.create (ButtonContent.Text label)
                |> Button.withColor ButtonColor.Primary
                |> Button.withVariant variant
                |> Button.render
        }

    let customButtonPalette : ColorPalette =
        { light =
            { solid = "#9a3412"; solidHover = "#7c2d12"; solidActive = "#431407"; onSolid = "#ffffff"
              soft = "#fff7ed"; softHover = "#ffedd5"; softActive = "#fed7aa"
              text = "#9a3412"; border = "#9a3412"; focus = "#9a3412" }
          dark =
            { solid = "#fdba74"; solidHover = "#fed7aa"; solidActive = "#ffedd5"; onSolid = "#431407"
              soft = "#431407"; softHover = "#5c2209"; softActive = "#7c2d12"
              text = "#ffedd5"; border = "#fdba74"; focus = "#fdba74" } }

    let customPaletteButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"
            for label, variant in [ "Solid", ButtonVariant.Solid; "Soft", ButtonVariant.Soft; "Outline", ButtonVariant.Outline; "Ghost", ButtonVariant.Ghost ] do
                Button.create (ButtonContent.Text label)
                |> Button.withColor (ButtonColor.Custom customButtonPalette)
                |> Button.withVariant variant
                |> Button.render
        }

    let pendingErrorButton =
        Button.create (ButtonContent.Text "Deleting account")
        |> Button.withColor ButtonColor.Error
        |> Button.withVariant ButtonVariant.Solid
        |> Button.pending
        |> Button.render

    let iconOnlyButtonVariantExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"
            for label, variant in [ "Solid", ButtonVariant.Solid; "Soft", ButtonVariant.Soft; "Outline", ButtonVariant.Outline; "Ghost", ButtonVariant.Ghost ] do
                Button.create (ButtonContent.Icon (label + " refresh", refreshIcon))
                |> Button.withColor ButtonColor.Secondary
                |> Button.withVariant variant
                |> Button.render
        }

    let buttonContentExamples =
        let continueIcon = raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M7.22 14.78a.75.75 0 0 1 0-1.06L10.94 10 7.22 6.28a.75.75 0 0 1 1.06-1.06l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0Z" clip-rule="evenodd"/></svg>"""
        div {
            _class "flex flex-wrap items-center justify-center gap-3"
            Button.create (ButtonContent.Text "Text") |> Button.render
            Button.create (ButtonContent.Icon ("Refresh", refreshIcon)) |> Button.render
            Button.create (ButtonContent.IconText (plusIcon, "New branch")) |> Button.render
            Button.create (ButtonContent.TextIcon ("Continue", continueIcon)) |> Button.render
            Button.create (ButtonContent.Custom (fragment { strong { "Custom" }; span { " content" } })) |> Button.render
        }

    let primaryButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            for size in [ ControlSize.Small; ControlSize.Medium; ControlSize.Large ] do
                Button.create (ButtonContent.Text "Create account")
                |> Button.withColor ButtonColor.Primary
                |> Button.withVariant ButtonVariant.Solid
                |> Button.withSize size
                |> Button.render
        }
    let secondaryButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            for size in [ ControlSize.Small; ControlSize.Medium; ControlSize.Large ] do
                Button.create (ButtonContent.Text "View reports")
                |> Button.withColor ButtonColor.Secondary
                |> Button.withVariant ButtonVariant.Soft
                |> Button.withSize size
                |> Button.render
        }
    let ghostButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            for size in [ ControlSize.Small; ControlSize.Medium; ControlSize.Large ] do
                Button.create (ButtonContent.Text "Cancel")
                |> Button.withVariant ButtonVariant.Ghost
                |> Button.withSize size
                |> Button.render
        }
    let destructiveButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"
            for size in [ ControlSize.Small; ControlSize.Medium; ControlSize.Large ] do
                Button.create (ButtonContent.Text "Delete account")
                |> Button.withColor ButtonColor.Error
                |> Button.withVariant ButtonVariant.Solid
                |> Button.withSize size
                |> Button.render
        }

    let confirmationExample =
        div {
            accountDeletionDialog |> Dialog.trigger "Delete account"
            accountDeletionDialog |> Dialog.render
        }
    let pendingConfirmationExample =
        div {
            pendingDeletionDialog |> Dialog.trigger "Open pending confirmation"
            pendingDeletionDialog |> Dialog.render
        }
    let accountDrawerExample =
        div {
            accountDrawerConfig |> Drawer.trigger "Open account details"
            accountDrawerConfig |> Drawer.render
        }
    let accountEditorDrawerExample =
        div {
            accountEditorDrawerConfig |> Drawer.trigger "Edit account"
            accountEditorDrawerConfig |> Drawer.render
        }
    let filterDrawerExample =
        div {
            filterDrawerConfig |> Drawer.trigger "Open filters"
            filterDrawerConfig |> Drawer.render
        }
    let horizontalDrawerExamples =
        div {
            _class "flex flex-wrap gap-3"
            topDrawerConfig |> Drawer.trigger "Open top drawer"
            bottomDrawerConfig |> Drawer.trigger "Open bottom drawer"
            topDrawerConfig |> Drawer.render
            bottomDrawerConfig |> Drawer.render
        }
    let sideNavExample = groupedLedgerNavigation |> SideNav.render shellDestinationUrl
    let pageHeaderExample =
        div {
            _dataSignals "{ledgerRefreshes: 0}"
            accountPageHeader |> PageHeader.render
            output { _class "sr-only"; _role "status"; _dataText "'Balance refreshes: ' + $ledgerRefreshes"; "Balance refreshes: 0" }
        }
    let periodNote =
        Section.withoutHeader "Period note" (p { _class "text-sm text-[var(--fve-muted-text)]"; "Amounts reflect the current accounting period." })
        |> Section.render

    let responsiveActionsExample =
        div {
            _dataSignals "{ledgerRefreshes: 0}"
            PageHeader.create "Operating checking"
            |> PageHeader.withActions (accountPageActions "responsive-account-actions")
            |> PageHeader.render
            output { _class "sr-only"; _role "status"; _dataText "'Balance refreshes: ' + $ledgerRefreshes"; "Balance refreshes: 0" }
        }

    let bottomNavigationExample =
        BottomNavigation.create "example-bottom-navigation" "Primary navigation" [
            BottomNavigationItem.create "/components/application" "Home"
            BottomNavigationItem.create "/components/collection" "Accounts"
            BottomNavigationItem.create "/components/metric" "Reports"
            BottomNavigationItem.create "/components/theming" "Settings" ]
        |> BottomNavigation.withCurrent "/components/collection"
        |> BottomNavigation.render id

    let compactBottomNavigationExample =
        BottomNavigation.create "compact-bottom-navigation" "Workspace navigation" [
            BottomNavigationItem.create "/components/application" "Home"
            BottomNavigationItem.create "/components/month-calendar" "Schedule"
            BottomNavigationItem.create "/components/media-library" "Media" ]
        |> BottomNavigation.withCurrent "/components/month-calendar"
        |> BottomNavigation.render id

    let private workspacePreview _ (content:HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"
            for attribute in galleryDestinationAttributes do attribute
            div {
                for attribute in ComponentsTheme.attributes (ComponentsTheme.sky |> ComponentsTheme.withDensity Density.Compact) do attribute
                div { _class "h-[36rem] overflow-hidden"; content }
            }
        }
    let transactionsPagePreview =
        workspacePreview "transactions-page" (renderTransactionsPage (pageTopBar (treasuryBreadcrumbs TreasuryTransactions)) transactionTabs)
    let canvasPagePreview = workspacePreview "canvas-page" (canvasPage () |> Page.render)

    [<NoEquality; NoComparison>]
    type ComponentExample =
        { id:string
          title:string
          source:string
          preview:HtmlElement
          note:string option }

    let exampleImports = "open System\nopen FSharp.ViewEngine\nopen Acme.Components\nopen type Html\nopen type Svg\nopen type Datastar"

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
            for attribute in galleryDestinationAttributes do attribute
            div {
                for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
                content
            }
        }

    let private connectedGallerySurface (content:HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"
            div {
                for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
                content
            }
        }

    let private centered (content:HtmlElement) =
        gallerySurface (div { _class "flex min-h-40 min-w-0 flex-wrap items-center justify-center gap-6 p-[12px]"; content })

    let private fieldSurface (content:HtmlElement) =
        gallerySurface (div { _class "mx-auto grid min-h-40 min-w-0 max-w-md content-center gap-4 p-[12px] sm:p-8"; content })

    let defaultResizablePanels =
        let leading = ResizablePanel.create (aside { _class "h-full p-4"; "Navigation" })
        Resizable.create "default-workspace-panels" "Workspace panels" leading (div { _class "h-full p-4"; "Content" })
        |> Resizable.render

    let resizablePanels =
        let navigationPanel =
            ResizablePanel.create (aside { _class "h-full bg-[var(--fve-surface-subtle)] p-4"; strong { "Accounts" }; p { _class "mt-2 text-sm text-[var(--fve-muted-text)]"; "Operating\nSavings\nTax reserve" } })
            |> ResizablePanel.withInitialSize 34
            |> ResizablePanel.withBounds 20 60
            |> ResizablePanel.collapsible 0
        Resizable.create "account-workspace-panels" "Account workspace panels" navigationPanel (div { _class "h-full p-4"; h3 { _class "font-semibold"; "Operating account" }; p { _class "mt-2 text-sm text-[var(--fve-muted-text)]"; "Resize the supporting list without changing the route or persisting a layout preference." } })
        |> Resizable.render

    let verticalResizablePanels =
        let summaryPanel =
            ResizablePanel.create (section { _class "h-full p-4"; strong { "Review summary" }; p { _class "mt-2 text-sm text-[var(--fve-muted-text)]"; "Three entries need attention." } })
            |> ResizablePanel.withInitialSize 40
            |> ResizablePanel.withBounds 25 70
        Resizable.create "review-workspace-panels" "Review panels" summaryPanel (section { _class "h-full bg-[var(--fve-surface-subtle)] p-4 text-sm"; "Entry details" })
        |> Resizable.withOrientation ResizableOrientation.Vertical
        |> Resizable.render

    let basicTooltip =
        Tooltip.create "archive-tooltip" "Moves the account out of active lists." (Button.create (ButtonContent.Text "Archive") |> Button.render)
        |> Tooltip.render

    let bottomTooltip =
        Tooltip.create "bottom-placement-tooltip" "Moves the account out of active lists." (Button.create (ButtonContent.Text "Archive") |> Button.withVariant ButtonVariant.Outline |> Button.render)
        |> Tooltip.withSide TooltipSide.Bottom
        |> Tooltip.withDelay 150
        |> Tooltip.render

    let profilePopover =
        Popover.create "profile-popover" "Account details" (span { "Account details" }) "Account details" (p { "Signed in as Alex Morgan." })
        |> Popover.render

    let focusedProfilePopover =
        let content =
            div {
                _class "grid gap-4"
                Input.create "displayName" "Display name" |> Input.withValue "Alex Morgan" |> Input.render
                p { _class "text-sm text-[var(--fve-muted-text)]"; "This local example does not save changes." }
            }
        Popover.create "focused-profile-popover" "Edit profile" (span { "Edit profile" }) "Edit profile" content
        |> Popover.focusContentOnOpen
        |> Popover.render

    let alignedPopover =
        Popover.create "filter-popover" "Open filter details" (span { "Filter details" }) "Filter details" (p { _class "text-sm text-[var(--fve-muted-text)]"; "Showing active accounts with balances above zero." })
        |> Popover.withAlignment PopoverAlignment.End
        |> Popover.withSide PopoverSide.Top
        |> Popover.render

    let monthSelection =
        MonthCalendar.createSelection "month-selection" "Date" "selectedDate" (MonthCalendarSelection.Single None)
        |> MonthCalendar.renderSelection

    let monthSelectionSingle =
        MonthCalendar.createSelection "settlement-month-calendar" "Settlement date" "settlementDate" (MonthCalendarSelection.Single(Some(DateOnly(2026, 9, 18))))
        |> MonthCalendar.withDisplayMonth (DateOnly(2026, 9, 1))
        |> MonthCalendar.clearable
        |> MonthCalendar.renderSelection

    let monthSelectionRange =
        MonthCalendar.createSelection "reporting-range-calendar" "Reporting range" "reportingStart" (MonthCalendarSelection.Range(Some(DateOnly(2026, 9, 14)), Some(DateOnly(2026, 9, 18))))
        |> MonthCalendar.withRangeEndName "reportingEnd"
        |> MonthCalendar.withDisplayMonth (DateOnly(2026, 9, 1))
        |> MonthCalendar.withWeekStart MonthCalendarWeekStart.Monday
        |> MonthCalendar.renderSelection

    let boundedMonthSelection =
        MonthCalendar.createSelection "booking-month-calendar" "Booking date" "bookingDate" (MonthCalendarSelection.Single None)
        |> MonthCalendar.withDisplayMonth (DateOnly(2026, 9, 1))
        |> MonthCalendar.withBounds (DateOnly(2026, 9, 10)) (DateOnly(2026, 9, 25))
        |> MonthCalendar.withUnavailableDates [ DateOnly(2026, 9, 16); DateOnly(2026, 9, 17) ]
        |> MonthCalendar.withLocale "en-GB"
        |> MonthCalendar.withWeekStart MonthCalendarWeekStart.Monday
        |> MonthCalendar.renderSelection

    let captionMonthSelection =
        MonthCalendar.createSelection "birth-month-calendar" "Choose a birth date" "birthDate" (MonthCalendarSelection.Single(Some(DateOnly(2000, 6, 15))))
        |> MonthCalendar.withDisplayMonth (DateOnly(2000, 6, 1))
        |> MonthCalendar.withBounds (DateOnly(1920, 1, 1)) (DateOnly(2026, 12, 31))
        |> MonthCalendar.withCaptionLayout MonthCalendarCaptionLayout.Dropdown
        |> MonthCalendar.renderSelection

    let pendingMonthSelection =
        MonthCalendar.createSelection "pending-month-calendar" "Checking available dates" "availableDate" (MonthCalendarSelection.Single None)
        |> MonthCalendar.withDisplayMonth (DateOnly(2026, 9, 1))
        |> MonthCalendar.pending
        |> MonthCalendar.renderSelection

    let defaultDatePicker =
        DatePicker.create "default-invoice-date" "defaultInvoiceDate" "Invoice date" (MonthCalendarSelection.Single None)
        |> DatePicker.render

    let basicDatePicker =
        DatePicker.create "invoice-date" "invoiceDate" "Invoice date" (MonthCalendarSelection.Single(Some(DateOnly(2026, 9, 18))))
        |> DatePicker.withDescription "Uses a canonical date-only form value."
        |> DatePicker.clearable
        |> DatePicker.render

    let rangeDatePicker =
        DatePicker.create "reporting-period" "periodStart" "Reporting period" (MonthCalendarSelection.Range(Some(DateOnly(2026, 9, 14)), Some(DateOnly(2026, 9, 18))))
        |> DatePicker.withRangeEndName "periodEnd"
        |> DatePicker.withWeekStart MonthCalendarWeekStart.Monday
        |> DatePicker.render

    let captionDatePicker =
        DatePicker.create "date-of-birth" "dateOfBirth" "Date of birth" (MonthCalendarSelection.Single(Some(DateOnly(2000, 6, 15))))
        |> DatePicker.withDisplayMonth (DateOnly(2000, 6, 1))
        |> DatePicker.withBounds (DateOnly(1920, 1, 1)) (DateOnly(2026, 12, 31))
        |> DatePicker.withCaptionLayout MonthCalendarCaptionLayout.Dropdown
        |> DatePicker.render

    let boundedDatePicker =
        DatePicker.create "travel-date" "travelDate" "Travel date" (MonthCalendarSelection.Single None)
        |> DatePicker.withDisplayMonth (DateOnly(2026, 9, 1))
        |> DatePicker.withBounds (DateOnly(2026, 9, 10)) (DateOnly(2026, 9, 25))
        |> DatePicker.withUnavailable [ DateOnly(2026, 9, 16); DateOnly(2026, 9, 17) ]
        |> DatePicker.render

    let invalidDatePicker =
        DatePicker.create "closing-date" "closingDate" "Closing date" (MonthCalendarSelection.Single None)
        |> DatePicker.withDisplayMonth (DateOnly(2026, 9, 1))
        |> DatePicker.withValidation "Choose a date in the open accounting period."
        |> DatePicker.render

    let pendingDatePicker =
        DatePicker.create "availability-date" "availabilityDate" "Availability date" (MonthCalendarSelection.Single(Some(DateOnly(2026, 9, 21))))
        |> DatePicker.pending
        |> DatePicker.render

    let disabledDatePicker =
        DatePicker.create "archived-date" "archivedDate" "Archived date" (MonthCalendarSelection.Single(Some(DateOnly(2026, 9, 8))))
        |> DatePicker.disabled
        |> DatePicker.render

    let defaultField =
        Field.create "reference" "Reference"
        |> Field.render (fun attributes ->
            input {
                for attribute in attributes do attribute
                _name "reference"
                _class "min-h-[var(--fve-control-min-height)] w-full rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
            })

    let customCurrencyField =
        Field.create "adjustment-amount" "Adjustment amount"
        |> Field.withDescription "Enter the amount before fees."
        |> Field.required
        |> Field.render (fun attributes ->
            input {
                for attribute in attributes do attribute
                _name "adjustmentAmount"
                _type "number"
                _step "0.01"
                _class "min-h-[var(--fve-control-min-height)] w-full rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
            })

    let responsiveFieldRows =
        div {
            _class "grid gap-4"
            Field.create "legal-name" "Legal name"
            |> Field.withLayout FieldLayout.Responsive
            |> Field.withDescription "Matches the account owner record."
            |> Field.render (fun attributes -> input { for attribute in attributes do attribute; _name "legalName"; _class "min-h-10 w-full rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3" })
            Field.create "tax-reference" "Tax reference"
            |> Field.withLayout FieldLayout.Responsive
            |> Field.withValidation "Enter the nine-character reference."
            |> Field.render (fun attributes -> input { for attribute in attributes do attribute; _name "taxReference"; _class "min-h-10 w-full rounded-md border border-[var(--fve-critical-ring)] bg-[var(--fve-surface)] px-3" })
        }

    let groupedFields =
        FieldGroup.create "Delivery preferences" (div { _class "grid gap-3"; Checkbox.create "emailDelivery" "Email receipt" |> Checkbox.withChecked true |> Checkbox.render; Checkbox.create "paperDelivery" "Paper receipt" |> Checkbox.render })
        |> FieldGroup.withDescription "Choose one or both delivery methods."
        |> FieldGroup.render

    let defaultInputGroup =
        InputGroup.create "account-reference" "accountReference" "Account reference"
        |> InputGroup.render

    let currencyInputGroup =
        InputGroup.create "budget-amount" "budgetAmount" "Budget"
        |> InputGroup.withValue "1250"
        |> InputGroup.withAddon (InputGroupAddon.text InputGroupPosition.Leading "$")
        |> InputGroup.withAddon (InputGroupAddon.text InputGroupPosition.Trailing "USD")
        |> InputGroup.withDescription "Whole dollars or decimals are accepted."
        |> InputGroup.render

    let searchInputGroup =
        InputGroup.create "account-search" "query" "Search accounts"
        |> InputGroup.withInputType InputType.Search
        |> InputGroup.withAddon (InputGroupAddon.icon InputGroupPosition.Leading (raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11ZM2 9a7 7 0 1 1 12.95 3.69l3.18 3.18a.75.75 0 1 1-1.06 1.06l-3.18-3.18A7 7 0 0 1 2 9Z" clip-rule="evenodd"/></svg>"""))
        |> InputGroup.withAddon (InputGroupAddon.keyboard InputGroupPosition.Trailing "⌘ K")
        |> InputGroup.render

    let actionableInputGroup =
        InputGroup.create "workspace-url" "workspaceUrl" "Workspace URL"
        |> InputGroup.withValue "components"
        |> InputGroup.withAddon (InputGroupAddon.text InputGroupPosition.Leading "fve.meiermade.com/")
        |> InputGroup.withAddon (InputGroupAddon.action InputGroupPosition.Trailing (Button.create (ButtonContent.Text "Copy") |> Button.withVariant ButtonVariant.Ghost |> Button.withSize ControlSize.Small |> Button.render))
        |> InputGroup.render

    let dropdownInputGroup =
        let menu =
            DropdownMenu.create "amount-options" "Amount options"
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.text "Options")
            |> DropdownMenu.withContent [
                DropdownMenuItem.action "document.getElementById('invoice-amount').value = ''" "Clear amount"
                DropdownMenuItem.action "document.getElementById('invoice-amount').value = '125.00'" "Use standard amount" ]
            |> DropdownMenu.render id
        InputGroup.create "invoice-amount" "invoiceAmount" "Invoice amount"
        |> InputGroup.withValue "89.50"
        |> InputGroup.withAddon (InputGroupAddon.text InputGroupPosition.Leading "$")
        |> InputGroup.withAddon (InputGroupAddon.action InputGroupPosition.Trailing menu)
        |> InputGroup.render

    let textareaInputGroup =
        InputGroup.create "message-draft" "message" "Message"
        |> InputGroup.asTextarea 4
        |> InputGroup.withValue "The September close is ready for review."
        |> InputGroup.withFooter (div { _class "contents"; span { _class "text-xs text-[var(--fve-muted-text)]"; "48 / 2,000" }; Button.create (ButtonContent.Text "Send") |> Button.withColor ButtonColor.Primary |> Button.withVariant ButtonVariant.Solid |> Button.withSize ControlSize.Small |> Button.render })
        |> InputGroup.render

    let invalidInputGroup =
        InputGroup.create "routing-number" "routingNumber" "Routing number"
        |> InputGroup.withValidation "Enter a nine-digit routing number."
        |> InputGroup.withAddon (InputGroupAddon.text InputGroupPosition.Leading "US")
        |> InputGroup.render

    let pendingInputGroup =
        InputGroup.create "verified-domain" "domain" "Domain"
        |> InputGroup.withValue "fve.meiermade.com"
        |> InputGroup.pending
        |> InputGroup.withAddon (InputGroupAddon.text InputGroupPosition.Trailing "Checking…")
        |> InputGroup.render

    let defaultMessage =
        Message.create "Jordan Lee" (p { "Can you review the September close?" })
        |> Message.render

    let messageRows =
        div {
            _class "grid gap-5"
            Message.create "Jordan Lee" (p { "Can you review the September close before lunch?" })
            |> Message.withAvatar (Avatar.create "Jordan Lee" "JL" |> Avatar.withSize AvatarSize.Small |> Avatar.render)
            |> Message.withMetadata "09:12"
            |> Message.render
            Message.create "Andy Meier" (p { "Yes. I’ll add notes to the two entries that need attention." })
            |> Message.withSide MessageSide.Sender
            |> Message.withMetadata "09:14"
            |> Message.withStatus "Delivered"
            |> Message.render
        }

    let messageWithoutAvatar =
        Message.create "Support bot" (p { _class "whitespace-pre-wrap"; "The export is ready.\nIt contains 184 account records." })
        |> Message.withMetadata "Just now"
        |> Message.withActions (Button.create (ButtonContent.Text "Download") |> Button.withVariant ButtonVariant.Ghost |> Button.withSize ControlSize.Small |> Button.render)
        |> Message.render

    let browserWithAddress =
        Browser.create (
            div {
                _class "grid min-h-64 content-center gap-3 bg-[var(--fve-surface-subtle)] p-[12px] text-center"
                strong { _class "text-lg"; "Content preview" }
                p { _class "text-sm text-[var(--fve-muted-text)]"; "Rendered HTML" }
            })
        |> Browser.withAddress "https://fve.meiermade.com/components/browser"
        |> Browser.render

    let phoneWithContent =
        Phone.create (
            div {
                _class "grid h-full content-center gap-3 p-[12px] text-center"
                strong { _class "text-lg"; "Notes" }
                p { _class "text-sm text-[var(--fve-muted-text)]"; "Confirm arrival time." }
            })
        |> Phone.render

    let private detailsSurface (content:HtmlElement) =
        gallerySurface (div { _class "p-4 sm:p-6"; content })

    let private calendarSurface (content:HtmlElement) =
        gallerySurface (div { _class "grid w-full min-w-0 grid-cols-1 gap-4 p-[12px] sm:p-6"; content })

    let private overlaySurface (content:HtmlElement) =
        gallerySurface (div { _class "relative min-h-[32rem] overflow-hidden bg-[var(--fve-surface-subtle)] p-4"; content })

    let private prose content =
        p { _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)] [overflow-wrap:anywhere]"; text content }

    let private code language source =
        CodeBlock.create language source |> CodeBlock.render

    let private bullets items =
        ul {
            _class "m-0 list-disc pl-5 text-base leading-relaxed text-[var(--fve-muted-text)] marker:text-[var(--fve-brand-ring)]"
            for item in items do li { _class "mt-2 first:mt-0"; text item }
        }

    let private buildingBlockLinks label (items:(string * string) list) =
        div {
            _class "flex flex-wrap items-baseline gap-x-4 gap-y-2"
            h2 { _class "text-base font-normal"; "Built with" }
            nav {
                _ariaLabel label
                _class "flex flex-wrap gap-x-4 gap-y-2"
                for path, itemLabel in items do
                    a { _href path; _class "font-semibold text-[var(--fve-brand-text)] underline decoration-[var(--fve-brand-ring)] underline-offset-[0.18em]"; text itemLabel }
            }
        }

    let private apiOwner (registration:DocPage) =
        if registration.path.StartsWith("/components/page-examples/", StringComparison.Ordinal)
           || registration.id = formLayoutsRegistration.id then None
        else Some(registration.path.Substring("/components/".Length))

    let private componentKey (registration:DocPage) = registration.path.Substring("/components/".Length)

    let private componentDescription (registration:DocPage) =
        match componentKey registration with
        | "button" -> "Trigger actions and submit forms with explicit text, icon, color, variant, size, and pending content."
        | "button-group" -> "Join related buttons and menu triggers into one compact horizontal or vertical control group."
        | "dropdown-menu" -> "Present contextual actions and typed choices in an accessible dismissible menu."
        | "badge" -> "Label compact metadata and semantic states with typed colors and presentation variants."
        | "notice" -> "Present persistent contextual guidance with independent color, visual treatment, and accessible announcement policy."
        | "notification" -> "Present ephemeral, optionally actionable feedback in an accessible bottom-end stack."
        | "loading-indicator" -> "Communicate indeterminate work with a labelled, reusable loading indicator."
        | "progress" -> "Communicate determinate, indeterminate, complete, and failed progress."
        | "empty-state" -> "Explain an empty collection or unavailable result and provide its next useful action."
        | "skeleton" -> "Reserve content structure while data is loading without implying completed content."
        | "table" -> "Render typed tabular data with responsive layouts, row headers, selection, hierarchy, and actions."
        | "description-list" -> "Present labelled record details and semantic values as a responsive description list."
        | "metric" -> "Present a labelled value, trend, and supporting context as a compact metric."
        | "avatar" -> "Represent a person or entity with an image, initials fallback, size, and accessible name."
        | "copy-reveal" -> "Copy or deliberately reveal sensitive text with accessible text or icon actions."
        | "day-calendar" -> "Display one day's events in a scrollable time grid with accessible event destinations."
        | "week-calendar" -> "Display a week in seven shared time columns with horizontal and vertical scrolling."
        | "month-calendar" -> "Display a month in full or compact form, or select a date or range."
        | "year-calendar" -> "Survey a year through twelve responsive compact month calendars."
        | "item" -> "Compose structured list rows with media, content, metadata, links, and actions."
        | "kbd" -> "Render keyboard keys and shortcut groups as compact instructional content."
        | "separator" -> "Separate related content visually while preserving the intended semantic orientation."
        | "input" -> "Collect a single text-like value with complete label, hint, validation, and pending behavior."
        | "input-group" -> "Compose inputs or textareas with addons, actions, hints, menus, and validation."
        | "field" -> "Own labels, descriptions, validation, orientation, and custom form-control composition."
        | "textarea" -> "Collect multiline text with complete field semantics and validation."
        | "file-selection" -> "Collect one or more files with accessible native selection and validation."
        | "tag-input" -> "Add and remove a bounded set of submitted text tags with validation."
        | "error-summary" -> "Summarize validation errors and link users to the affected fields."
        | "select" -> "Choose typed single or multiple values with search, keyboard, form, and positioning behavior."
        | "date-picker" -> "Combine a labelled date field, popover, and compact Month calendar into one form control."
        | "checkbox" -> "Submit an explicit checked or unchecked value with complete field semantics."
        | "switch" -> "Toggle an immediate binary setting with accessible checked and unavailable states."
        | "toggle-button" -> "Toggle a pressed action with typed presentation, size, and availability states."
        | "toggle-group" -> "Choose one or more pressed values from an accessible oriented toggle group."
        | "radio-group" -> "Choose one submitted value from an accessible typed radio group."
        | "choice-cards" -> "Choose one rich option from labelled cards with descriptions and supporting content."
        | "breadcrumbs" -> "Show the current hierarchy with semantic links and responsive overflow."
        | "side-nav" -> "Navigate nested application destinations with groups, badges, actions, and current state."
        | "tabs" -> "Switch between labelled local panels with orientation-aware keyboard navigation."
        | "pagination" -> "Navigate bounded result pages with current, previous, next, and compact range items."
        | "command" -> "Search grouped destinations and actions with ranked matching, keyboard navigation, and an optional dialog palette."
        | "dialog" -> "Present modal content with labelled structure, focus management, dismissal, and form composition."
        | "drawer" -> "Present modal detail or controls from a selected viewport edge."
        | "floating-panel" -> "Keep dismissible supporting content available beside the primary workflow."
        | "popover" -> "Present labelled interactive content adjacent to a trigger with focus and dismissal ownership."
        | "tooltip" -> "Provide delayed supplementary text for a labelled control without replacing its accessible name."
        | "page-top-bar" -> "Compose compact page context and actions above the primary page heading."
        | "page-header" -> "Compose a page title, description, metadata, and responsive actions."
        | "section" -> "Introduce a page section with a heading, supporting copy, and optional actions."
        | "page" -> "Compose application content from page chrome, headings, sections, and bounded layouts."
        | "collection" -> "Compose collection headings, controls, results, empty states, and pagination."
        | "detail" -> "Compose record identity, metadata, sections, and actions into a detail layout."
        | "app-shell" -> "Frame application navigation, responsive chrome, content, and optional mobile destinations."
        | "form-layouts" -> "Arrange complete fields into consistent stacked, inline, and responsive form layouts."
        | "browser" -> "Frame rendered content with a truthful browser title and address."
        | "phone" -> "Frame rendered content in a neutral responsive phone viewport."
        | "resizable" -> "Split content into pointer- and keyboard-resizable horizontal or vertical panels."
        | "bottom-navigation" -> "Navigate primary mobile destinations from a compact bottom bar."
        | "message" -> "Compose sender identity, message content, metadata, and contextual actions."
        | "upload-list" -> "Present consumer-owned upload progress, completion, and failure states."
        | "steps" -> "Communicate ordered workflow progress with current, complete, and upcoming steps."
        | "first-steps" -> "Guide initial setup work with progress, actions, minimization, and dismissal."
        | "media-library" -> "Browse and select media with filters, metadata, and responsive collection states."
        | _ -> $"Use the {registration.title} component in typed F# interfaces."

    let private installationSelector (registration:DocPage) =
        match componentKey registration with
        | "upload" -> "upload-list"
        | key -> key

    let private titledSection id title content =
        DocumentationSection.create id title (h2 { _class "text-xl font-semibold text-[var(--fve-text)]"; title } :: content)

    let private componentGallery (registration:DocPage) examples =
        match examples with
        | [] -> invalidOp $"Component page '{registration.path}' requires a canonical lead example."
        | lead :: variants ->
            let installation = installationSelector registration
            let leadContent =
                [ Example.lead lead.id lead.title "fsharp" lead.source lead.preview ]
                @ (lead.note |> Option.map prose |> Option.toList)
            let sections = [
                titledSection "installation" "Installation" [
                    prose "Copy this component and its required dependencies into the consumer-owned Components project."
                    code "shell" $"dotnet fve add {installation} --config src/Acme.Components/fve.json"
                    p {
                        _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)]"
                        "The CLI resolves dependencies and preserves consumer-authored files. See "
                        a { _href installationRegistration.path; _class "font-semibold text-[var(--fve-brand-text)] underline underline-offset-2"; "Installation" }
                        " for project initialization and Tailwind source detection."
                    } ]
                titledSection "usage" "Usage" [ code "fsharp" lead.source ]
                for item in variants do
                    DocumentationSection.create item.id item.title (
                        [ Example.gallery item.id item.title "fsharp" item.source item.preview ]
                        @ (item.note |> Option.map prose |> Option.toList))
                if registration.id = appShellRegistration.id then
                    titledSection "complete-pages" "Complete page examples" [
                        p {
                            "For populated pages and a connected workflow, explore "
                            a { _href accountManagementRegistration.path; "Account management" }
                            ". These shell examples intentionally show only the layout."
                        } ]
                match apiOwner registration with
                | Some owner ->
                    titledSection "api-reference" "API reference" [
                        FSharpApiReference.forCategory owner typeof<ButtonConfig> |> FSharpApiReference.render ]
                | None -> () ]
            DocumentationPage.create registration.id registration.title
            |> DocumentationPage.withDescription (componentDescription registration)
            |> DocumentationPage.withLayout Gallery
            |> DocumentationPage.withRightRail TableOfContents
            |> DocumentationPage.withLead leadContent
            |> DocumentationPage.withSections sections

    let private pageExampleGallery (registration:DocPage) examples =
        DocumentationPage.create registration.id registration.title |> DocumentationPage.withLayout Gallery |> DocumentationPage.withRightRail NoRail |> DocumentationPage.withSections [
            for item in examples do
                DocumentationSection.create item.id item.title (
                    [ Example.gallery item.id item.title "fsharp" item.source item.preview ]
                    @ (item.note |> Option.map prose |> Option.toList))
            if registration.id = accountManagementRegistration.id then
                DocumentationSection.create "building-blocks" "Built with" [
                    buildingBlockLinks "Account management building blocks" [
                        "/components/app-shell", "App shell"
                        "/components/page", "Page"
                        "/components/collection", "Collection"
                        "/components/detail", "Detail"
                        "/components/page-header", "Page header"
                        "/components/button", "Button"
                        "/components/dropdown-menu", "Dropdown menu"
                        "/components/form-layouts", "Form layouts" ] ] ]

    let private gallery (registration:DocPage) examples =
        if registration.path.StartsWith("/components/page-examples/", StringComparison.Ordinal) then pageExampleGallery registration examples
        else componentGallery registration examples

    let setupSteps =
        [ FirstStep.create "connect-bank" "Connect a bank account"
          |> FirstStep.withDescription "Import accounts and reconcile current balances."
          |> FirstStep.withAction (a { _href "/components/collection"; _class "text-sm font-semibold text-[var(--fve-brand-text)] underline-offset-2 hover:underline"; "Connect account" })
          FirstStep.create "review-accounts" "Review imported accounts"
          |> FirstStep.complete ]

    let firstStepsExample =
        FirstSteps.create "ledger-first-steps" "First steps" setupSteps
        |> FirstSteps.withinContainer
        |> FirstSteps.render

    let minimizedFirstStepsExample =
        FirstSteps.create "minimized-first-steps" "First steps" setupSteps
        |> FirstSteps.withinContainer
        |> FirstSteps.minimized
        |> FirstSteps.render

    let dismissedFirstStepsExample =
        FirstSteps.create "dismissed-first-steps" "First steps" setupSteps
        |> FirstSteps.withinContainer
        |> FirstSteps.dismissed
        |> FirstSteps.render

    let floatingPanelContent =
        div {
            _class "grid gap-3 text-sm"
            p { "Use this space for contextual guidance that should not block the primary task." }
            a { _href "/components/page-examples/operations-dashboard"; _class "font-semibold text-[var(--fve-brand-text)] underline-offset-2 hover:underline"; "Open operations" }
        }

    let floatingPanelExample =
        FloatingPanel.create "workspace-guide" "Workspace guide" floatingPanelContent
        |> FloatingPanel.withDescription "Helpful context remains available beside the application."
        |> FloatingPanel.withinContainer
        |> FloatingPanel.render

    let minimizedFloatingPanelExample =
        FloatingPanel.create "minimized-workspace-guide" "Workspace guide" floatingPanelContent
        |> FloatingPanel.withinContainer
        |> FloatingPanel.minimized
        |> FloatingPanel.render

    let notificationLeadItem sequence =
        Notification.create $"saved-notification-{sequence}" "Changes saved" (p { $"Server-confirmed notification {sequence} is ready." })

    let notificationLeadRegion sequence =
        let notification = if sequence > 0 then Some(notificationLeadItem sequence) else None
        NotificationRegion.create "save-notifications" "Workspace notifications" notification
        |> NotificationRegion.withinContainer
        |> NotificationRegion.render

    let simpleNotificationExample =
        div {
            _dataSignals ("components_notification_sequence", [ "ifmissing" ], "0")
            div {
                _class "flex min-h-40 items-start justify-center p-6"
                Button.create (ButtonContent.Text "Show notification")
                |> Button.withAttributes [ _dataOn ("click", "$components_notification_sequence += 1; @post('/components/notifications/show')") ]
                |> Button.render
            }
            notificationLeadRegion 0
        }

    let notificationWithActionsExample =
        let action =
            Button.create (ButtonContent.Text "Undo")
            |> Button.withVariant ButtonVariant.Outline
            |> Button.withSize ControlSize.Small
            |> Button.withAttributes [ _dataOn ("click", "evt.currentTarget.closest('[data-fve-notification]').querySelector('[role=status] div').textContent = 'The invitation remains pending.'") ]
            |> Button.render
        Notification.create "invitation-notification" "Invitation accepted" (p { "You joined Northwind Outdoor." })
        |> Notification.withColor NotificationColor.Info
        |> Notification.withActions action
        |> Notification.persistent
        |> Some
        |> NotificationRegion.create "invitation-notifications" "Invitation notifications"
        |> NotificationRegion.withinContainer
        |> NotificationRegion.render

    let timedNotificationExample =
        Notification.create "review-notification" "Review needed" (p { "Two unmatched transactions still need an account." })
        |> Notification.withColor NotificationColor.Warning
        |> Notification.withTtl 8000
        |> Some
        |> NotificationRegion.create "timed-notifications" "Review notifications"
        |> NotificationRegion.withinContainer
        |> NotificationRegion.render

    let operationalProgressExample =
        Progress.create "Statement import" 68 100
        |> Progress.render

    let indeterminateProgressExample =
        Progress.indeterminate "Preparing statement import"
        |> Progress.withValueText "Checking source files"
        |> Progress.withDetail "No completion percentage is available yet."
        |> Progress.render

    let completedProgressExample =
        Progress.create "Statement import" 100 100
        |> Progress.withValueText "100%"
        |> Progress.complete
        |> Progress.render

    let failedProgressExample =
        Progress.create "Statement import" 68 100
        |> Progress.withValueText "Stopped at 68%"
        |> Progress.withDetail "The application can provide a retry action beside the progress component."
        |> Progress.failed
        |> Progress.render

    let defaultFileSelection =
        FileSelection.create "statement-file" "statementFile" "Statement"
        |> FileSelection.render

    let fileSelectionExample =
        FileSelection.create "statement-files" "statements" "Statements"
        |> FileSelection.withDescription "Choose one or more CSV or OFX statements. Each file remains a native form value."
        |> FileSelection.withAccept ".csv,.ofx,text/csv"
        |> FileSelection.multiple
        |> FileSelection.render

    let invalidFileSelectionExample =
        FileSelection.create "receipt-file" "receipt" "Receipt"
        |> FileSelection.withDescription "Choose one PDF receipt."
        |> FileSelection.withAccept ".pdf,application/pdf"
        |> FileSelection.required
        |> FileSelection.withValidation "Choose a PDF receipt before continuing."
        |> FileSelection.render

    let pendingFileSelectionExample =
        FileSelection.create "pending-files" "pendingFiles" "Evidence files"
        |> FileSelection.withDescription "The current files are being validated."
        |> FileSelection.multiple
        |> FileSelection.pending
        |> FileSelection.render

    let uploadQueueExample =
        div {
            _dataSignals "{uploadFeedback: ''}"
            UploadList.create "Statement uploads"
                [ UploadItem.create "upload-complete" "checking-july.ofx" UploadState.Complete
                  |> UploadItem.withDetail "84 KB"
                  UploadItem.create "upload-active" "card-july.csv" (UploadState.Uploading (68, 100))
                  |> UploadItem.withDetail "142 KB"
                  |> UploadItem.withActions (
                      Button.create (ButtonContent.Text "Cancel")
                      |> Button.withAttributes [ _dataOn ("click", "$uploadFeedback = 'Upload cancelled. The selected local file remains available to retry.'") ]
                      |> Button.render)
                  UploadItem.create "upload-failed" "savings-july.csv" (UploadState.Failed "The demo transport rejected this file.")
                  |> UploadItem.withActions (
                      Button.create (ButtonContent.Text "Retry")
                      |> Button.withColor ButtonColor.Primary
                      |> Button.withVariant ButtonVariant.Solid
                      |> Button.withAttributes [ _dataOn ("click", "$uploadFeedback = 'Retry queued for savings-july.csv.'") ]
                      |> Button.render) ]
            |> UploadList.render
            output {
                _role "status"
                _ariaLive "polite"
                _dataShow "$uploadFeedback != ''"
                _dataText "$uploadFeedback"
                _style "display:none"
                _class "mt-2 text-sm text-[var(--fve-muted-text)]"
            }
        }

    let emptyUploadQueueExample =
        UploadList.create "Statement uploads" []
        |> UploadList.render

    let periodCloseStepsExample =
        div {
            _dataSignals "{stepnote: '', stepattempted: false}"
            _class "grid gap-4"
            Steps.create "Period close progress"
                [ Step.create "Review balances" StepState.Complete
                  |> Step.withDestination "/components/collection"
                  Step.create "Reconcile statements" StepState.Current
                  |> Step.withDescription "Resolve the remaining statement differences."
                  Step.create "Post adjustments" StepState.Available
                  |> Step.withDestination "/components/detail"
                  Step.create "Close period" StepState.Unavailable ]
            |> Steps.render id
            Input.create "reconciliationNote" "Reconciliation note"
            |> Input.withAttributes [ _dataBind "stepnote" ]
            |> Input.required
            |> Input.render
            Button.create (ButtonContent.Text "Continue")
            |> Button.withColor ButtonColor.Primary
            |> Button.withVariant ButtonVariant.Solid
            |> Button.withAttributes [ _dataOn ("click", "$stepattempted = true") ]
            |> Button.render
            p {
                _dataShow "$stepattempted && !$stepnote.trim()"
                _role "alert"
                _class "text-sm text-[var(--fve-critical-text)]"
                "Enter a reconciliation note before continuing. The current step has not changed."
            }
            p {
                _dataShow "$stepattempted && $stepnote.trim()"
                _role "status"
                _class "text-sm text-[var(--fve-positive-text)]"
                "Reconciliation is ready for server validation."
            }
        }

    let defaultAvatar = Avatar.create "Alex Morgan" "AM" |> Avatar.render

    let avatarFallbackExample =
        div {
            _class "flex flex-wrap items-center gap-4"
            Avatar.create "Alex Morgan" "AM" |> Avatar.withSize AvatarSize.Small |> Avatar.render
            Avatar.create "Jamie Lee" "JL" |> Avatar.render
            Avatar.create "Riley Chen" "RC" |> Avatar.withSize AvatarSize.Large |> Avatar.render
        }

    let avatarImageExample =
        Avatar.create "FSharp.ViewEngine" "FV"
        |> Avatar.withImage "/android-chrome-512x512.png"
        |> Avatar.withSize AvatarSize.Large
        |> Avatar.render

    let decorativeAvatarExample =
        Avatar.create "Decorative account mark" "AM"
        |> Avatar.decorative
        |> Avatar.render

    let copyRevealExample =
        CopyReveal.create "demo-token" "Demo API token" "fve_demo_84fK2s"
        |> CopyReveal.render

    let revealedCopyRevealExample =
        CopyReveal.create "revealed-demo-token" "Revealed demo token" "fve_demo_safe"
        |> CopyReveal.revealed
        |> CopyReveal.render

    let iconCopyRevealExample =
        CopyReveal.create "icon-demo-token" "Icon action demo token" "fve_demo_icon"
        |> CopyReveal.withIconButtons
        |> CopyReveal.render

    let defaultCommand =
        Command.create "command-default" "Find a component" [
            CommandGroup.create "Components" [
                CommandItem.link "/components/table" "Table"
                CommandItem.link "/components/month-calendar" "Month calendar" ] ]
        |> Command.render id

    let commandDocumentIcon =
        Svg.svg {
            _viewBox "0 0 24 24"
            _fill "none"
            _attr ("stroke", "currentColor")
            _attr ("stroke-width", "1.5")
            path { _attr ("stroke-linecap", "round"); _attr ("stroke-linejoin", "round"); _d "M9 12h6m-6 4h6M6 3h9l3 3v15H6V3Z" }
        }

    let commandGroups = [
        CommandGroup.create "Components" [
            CommandItem.link "/components/table" "Table" |> CommandItem.withLeading commandDocumentIcon |> CommandItem.withShortcut "⌘T"
            CommandItem.link "/components/month-calendar" "Month calendar" |> CommandItem.withKeywords [ "dates"; "schedule" ] ]
        CommandGroup.create "Page examples" [
            CommandItem.link "/components/page-examples/scheduling" "Scheduling" |> CommandItem.withShortcut "⌘S"
            CommandItem.link "/components/page-examples/account-management" "Account management" ] ]

    let groupedCommand =
        Command.create "command-groups" "Search components and examples" commandGroups
        |> Command.withPlaceholder "Find a component or page…"
        |> Command.render id

    let commandActionsExample =
        div {
            _dataSignals "{commandFeedback: ''}"
            _class "grid w-full gap-3"
            Command.create "command-actions" "Choose a local action" [
                CommandGroup.create "Actions" [
                    CommandItem.action "$commandFeedback = 'Display settings selected.'" "Display settings"
                    |> CommandItem.withDescription "A local selection; no settings are saved."
                    CommandItem.action "$commandFeedback = 'Account settings selected.'" "Account settings"
                    CommandItem.action "$commandFeedback = 'Unavailable action.'" "Unavailable action" |> CommandItem.disabled ] ]
            |> Command.render id
            p { _role "status"; _class "text-sm text-[var(--fve-muted-text)]"; _dataText "$commandFeedback" }
        }

    let commandPaletteExample =
        let command = Command.create "command-palette" "Navigate the catalog" commandGroups
        div {
            Command.trigger "Open command palette" command
            Command.renderDialog id command
        }

    let emptyCommand =
        Command.create "command-empty" "Search an empty collection" []
        |> Command.withEmptyState "No commands are available."
        |> Command.render id

    let private calendarDemoDate = DateOnly(2026, 9, 17)

    let private calendarEventDestination id =
        "/components/page-examples/scheduling?item=" + id

    let calendarDemoEvents =
        [ CalendarEvent.create "lesson-201" "Coastal trail lesson" calendarDemoDate (calendarEventDestination "lesson-201")
          |> CalendarEvent.withTime (TimeOnly(10, 0)) (TimeOnly(11, 30))
          |> CalendarEvent.withDetail "Maya and Sam · Andy Meier"
          CalendarEvent.create "camp-017" "Beginner riding camp" calendarDemoDate (calendarEventDestination "camp-017")
          |> CalendarEvent.withTime (TimeOnly(10, 30)) (TimeOnly(12, 0))
          |> CalendarEvent.withDetail "Overlaps the trail lesson by one hour"
          CalendarEvent.create "lesson-202" "Cornering fundamentals" (calendarDemoDate.AddDays 1) (calendarEventDestination "lesson-202")
          |> CalendarEvent.withTime (TimeOnly(9, 0)) (TimeOnly(10, 0))
          CalendarEvent.create "ride-019" "Open track practice" (calendarDemoDate.AddDays 2) (calendarEventDestination "ride-019")
          |> CalendarEvent.withTime (TimeOnly(13, 0)) (TimeOnly(15, 0))
          CalendarEvent.create "return-020" "Equipment return" (calendarDemoDate.AddDays 7) (calendarEventDestination "return-020")
          |> CalendarEvent.withTime (TimeOnly(16, 0)) (TimeOnly(16, 30)) ]

    let defaultCalendarEvent =
        CalendarEvent.create "lesson-201-default" "Coastal trail lesson" calendarDemoDate (calendarEventDestination "lesson-201-default")

    let defaultDayCalendar =
        DayCalendar.create "Coastal training day" calendarDemoDate [ defaultCalendarEvent ]
        |> DayCalendar.render id

    let defaultWeekCalendar =
        WeekCalendar.create "Coastal training week" calendarDemoDate [ defaultCalendarEvent ]
        |> WeekCalendar.render id

    let defaultMonthCalendar =
        MonthCalendar.create "Coastal training month" calendarDemoDate [ defaultCalendarEvent ]
        |> MonthCalendar.render id

    let defaultYearCalendar =
        YearCalendar.create "Coastal training year" calendarDemoDate [ defaultCalendarEvent ]
        |> YearCalendar.render id

    let calendarDateDestination (date:DateOnly) =
        "/components/page-examples/scheduling?view=day&range=" + string (date.DayNumber - calendarDemoDate.DayNumber)

    let calendarDayExample =
        DayCalendar.create "Thursday sessions" calendarDemoDate calendarDemoEvents
        |> DayCalendar.withToday calendarDemoDate (calendarDateDestination calendarDemoDate)
        |> DayCalendar.withSelectedDate calendarDemoDate
        |> DayCalendar.withDateDestination calendarDateDestination
        |> DayCalendar.render id

    let calendarWeekExample =
        WeekCalendar.create "Training week" calendarDemoDate calendarDemoEvents
        |> WeekCalendar.withToday calendarDemoDate (calendarDateDestination calendarDemoDate)
        |> WeekCalendar.withSelectedDate calendarDemoDate
        |> WeekCalendar.withDateDestination calendarDateDestination
        |> WeekCalendar.render id

    let calendarMonthExample =
        MonthCalendar.create "Team calendar" calendarDemoDate calendarDemoEvents
        |> MonthCalendar.withToday calendarDemoDate (calendarDateDestination calendarDemoDate)
        |> MonthCalendar.withSelectedDate calendarDemoDate
        |> MonthCalendar.withDateDestination calendarDateDestination
        |> MonthCalendar.render id

    let compactMonthCalendar =
        MonthCalendar.create "Training dates" calendarDemoDate calendarDemoEvents
        |> MonthCalendar.withLayout MonthCalendarLayout.Compact
        |> MonthCalendar.withToday calendarDemoDate (calendarDateDestination calendarDemoDate)
        |> MonthCalendar.withSelectedDate calendarDemoDate
        |> MonthCalendar.withDateDestination calendarDateDestination
        |> MonthCalendar.render id

    let calendarYearExample =
        YearCalendar.create "Team year" calendarDemoDate calendarDemoEvents
        |> YearCalendar.withToday calendarDemoDate (calendarDateDestination calendarDemoDate)
        |> YearCalendar.withSelectedDate calendarDemoDate
        |> YearCalendar.withDateDestination calendarDateDestination
        |> YearCalendar.render id

    let defaultBrowser =
        Browser.create (div { _class "p-[12px] text-center"; "Content preview" })
        |> Browser.render

    let defaultPhone =
        Phone.create (div { _class "grid h-full place-items-center p-[12px] text-center"; "Screen preview" })
        |> Phone.render

    let defaultBottomNavigation =
        BottomNavigation.create "default-bottom-navigation" "Default primary navigation" [
            BottomNavigationItem.create "/components" "Home"
            BottomNavigationItem.create "/components/collection" "Accounts" ]
        |> BottomNavigation.render id

    let mediaLibraryExample =
        let library =
            MediaLibrary.create "product-media" "Site artwork" "assetIds"
                [ MediaAsset.create "social-card" "Social card" "/social-card.png" "FSharp.ViewEngine — Typed HTML views for F#" LedgerAccounts
                  |> MediaAsset.withDetail "1200 × 630 · PNG"
                  |> MediaAsset.primary
                  MediaAsset.create "app-icon" "App icon" "/android-chrome-512x512.png" "FSharp.ViewEngine blue code-mark icon" LedgerAccounts
                  |> MediaAsset.withDetail "512 × 512 · PNG"
                  MediaAsset.create "touch-icon" "Touch icon" "/apple-touch-icon.png" "FSharp.ViewEngine blue code-mark touch icon" LedgerAccounts
                  |> MediaAsset.withDetail "180 × 180 · PNG" ]
            |> MediaLibrary.withSelected [ "social-card" ]
            |> MediaLibrary.render shellDestinationUrl
        library
        |> fun libraryWithActions ->
            div {
                _dataSignals "{mediastate: 'ready'}"
                _class "grid gap-4"
                div {
                    _class "flex flex-wrap gap-2"
                    for state, label in [ "empty", "Show empty"; "pending", "Simulate loading"; "error", "Simulate error"; "ready", "Restore media" ] do
                        button {
                            _type "button"
                            _dataOn ("click", $"$mediastate = '{state}'")
                            _class "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)]"
                            label
                        }
                }
                p {
                    _dataShow "$mediastate == 'empty'"
                    _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"
                    "No media assets. Upload an image to begin."
                }
                p {
                    _dataShow "$mediastate == 'pending'"
                    _role "status"
                    _ariaLive "polite"
                    _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"
                    "Loading media…"
                }
                div {
                    _dataShow "$mediastate == 'error'"
                    _role "alert"
                    _class "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-4 text-sm text-[var(--fve-critical-text)]"
                    span { "Media could not be loaded." }
                    button { _type "button"; _dataOn ("click", "$mediastate = 'ready'"); _class "rounded-[var(--fve-radius-control)] px-3 py-2 font-semibold ring-1 ring-[var(--fve-critical-ring)]"; "Retry media" }
                }
                div {
                    _dataShow "$mediastate == 'ready'"
                    _class "grid gap-4"
                    libraryWithActions
                    div {
                        _class "flex flex-wrap gap-2"
                        Button.create (ButtonContent.Text "Inspect selected assets")
                        |> Button.withAttributes [ _dataOn ("click", "document.getElementById('media-edit-feedback').textContent = 'Selected asset IDs: ' + Array.from(document.querySelectorAll('#product-media input:checked')).map(input => input.value).join(', ')") ]
                        |> Button.render
                        Button.create (ButtonContent.Text "Clear selection")
                        |> Button.withAttributes [ _dataOn ("click", "document.getElementById('product-media')?.dispatchEvent(new CustomEvent('fve-selection-clear'))") ]
                        |> Button.render
                    }
                    Textarea.create "altText" "Alt text"
                    |> Textarea.withId "media-alt-text"
                    |> Textarea.withValue "FSharp.ViewEngine — Typed HTML views for F#"
                    |> Textarea.withDescription "Describe the selected asset for people who cannot see it."
                    |> Textarea.render
                    Button.create (ButtonContent.Text "Save alt text")
                    |> Button.withAttributes [ _dataOn ("click", "(() => { const value = document.getElementById('media-alt-text').value; document.querySelectorAll('#product-media input:checked').forEach(input => input.closest('li').querySelector('img').alt = value); document.getElementById('media-edit-feedback').textContent = 'Alt text updated for selected media.' })()") ]
                    |> Button.render
                    FileSelection.create "media-replacement" "replacementImage" "Replacement image"
                    |> FileSelection.withDescription "Choose a local image; this provider-free example does not upload it."
                    |> FileSelection.withAccept "image/*"
                    |> FileSelection.render
                    Button.create (ButtonContent.Text "Apply demo replacement")
                    |> Button.withAttributes [ _dataOn ("click", "document.querySelectorAll('#product-media input:checked').forEach(input => input.closest('li').querySelector('img').src = '/favicon-32x32.png'); document.getElementById('media-edit-feedback').textContent = 'Demo replacement applied to selected media.'") ]
                    |> Button.render
                    p { _id "media-edit-feedback"; _role "status"; _ariaLive "polite"; _class "text-sm text-[var(--fve-muted-text)]" }
                }
            }

    let traceViewerIntegrationExample =
        div {
            _dataSignals "{tracestate: 'ready'}"
            _class "grid gap-3"
            div {
                _class "flex flex-wrap gap-2"
                for state, label in [ "ready", "Show trace"; "pending", "Load trace"; "empty", "Show empty trace"; "error", "Simulate trace error" ] do
                    button {
                        _type "button"
                        _dataOn ("click", $"$tracestate = '{state}'")
                        _class "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)]"
                        label
                    }
            }
            p { _dataShow "$tracestate == 'pending'"; _role "status"; _class "text-sm text-[var(--fve-muted-text)]"; "Loading trace…" }
            p { _dataShow "$tracestate == 'empty'"; _class "text-sm text-[var(--fve-muted-text)]"; "No spans matched this trace query." }
            div {
                _dataShow "$tracestate == 'error'"
                _role "alert"
                _class "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)]"
                span { "Trace data could not be loaded." }
                button { _type "button"; _dataOn ("click", "$tracestate = 'ready'"); _class "rounded-[var(--fve-radius-control)] px-3 py-2 font-semibold ring-1 ring-[var(--fve-critical-ring)]"; "Retry trace" }
            }
            figure {
                _dataShow "$tracestate == 'ready'"
                _class "grid gap-3"
                div {
                    _tabindex 0
                    _ariaLabel "Scrollable trace diagram"
                    _class "overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-4 ring-1 ring-[var(--fve-border)]"
                    svg {
                        _viewBox "0 0 720 180"
                        _role "img"
                        _ariaLabel "Request trace from browser through API and database"
                        _class "min-w-[40rem] w-full"
                        line { _x1 120; _y1 90; _x2 600; _y2 90; _stroke "currentColor"; _strokeWidth 2 }
                        for x, label, duration in [ 120, "Browser", "0 ms"; 360, "API", "42 ms"; 600, "Database", "18 ms" ] do
                            circle { _cx x; _cy 90; _r 34; _fill "var(--fve-brand-subtle)"; _stroke "var(--fve-brand-solid)"; _strokeWidth 2 }
                            textElement { _x x; _y 86; _textAnchor "middle"; _fill "currentColor"; label }
                            textElement { _x x; _y 108; _textAnchor "middle"; _fill "currentColor"; duration }
                    }
                }
                figcaption {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Trace request-84f2 · 60 ms total. The ordered list remains the accessible source of truth."
                }
                ol {
                    _class "grid gap-2"
                    for label, detail in [ "Browser", "GET /accounts · starts at 0 ms"; "API", "Authorization and query · 42 ms"; "Database", "SELECT accounts · 18 ms" ] do
                        li {
                            _class "flex items-start justify-between gap-3 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-3 text-sm"
                            strong { label }
                            span { _class "text-right text-[var(--fve-muted-text)]"; detail }
                        }
                }
            }
        }

    let financialChartIntegrationExample =
        div {
            _dataSignals "{chartperiod: '30d'}"
            _class "grid gap-4"
            div {
                _class "flex flex-wrap gap-2"
                for value, label in [ "30d", "30 days"; "90d", "90 days" ] do
                    button {
                        _type "button"
                        _dataOn ("click", $"$chartperiod = '{value}'")
                        _dataAttr ("aria-pressed", $"$chartperiod == '{value}' ? 'true' : 'false'")
                        _class "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] aria-pressed:bg-[var(--fve-brand-subtle)] aria-pressed:text-[var(--fve-brand-text)]"
                        label
                    }
            }
            div {
                _tabindex 0
                _ariaLabel "Scrollable balance chart"
                _class "overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-4 ring-1 ring-[var(--fve-border)]"
                for value, label, points in
                    [ "30d", "Thirty-day balance: $24,820", "0,130 120,112 240,122 360,76 480,58 600,32"
                      "90d", "Ninety-day balance: $24,820", "0,150 120,138 240,96 360,118 480,70 600,32" ] do
                    svg {
                        _viewBox "0 0 600 180"
                        _role "img"
                        _ariaLabel label
                        _dataShow ($"$chartperiod == '{value}'")
                        _class "min-w-[36rem] w-full"
                        line { _x1 0; _y1 160; _x2 600; _y2 160; _stroke "var(--fve-border)"; _strokeWidth 1 }
                        polyline { _points points; _fill "none"; _stroke "var(--fve-brand-solid)"; _strokeWidth 4; _vectorEffect "non-scaling-stroke" }
                    }
            }
            table {
                _class "w-full text-sm"
                caption { _class "text-left font-semibold text-[var(--fve-text)]"; "Balance data" }
                thead { tr { th { _scope "col"; _class "py-2 text-left"; "Period" }; th { _scope "col"; _class "py-2 text-right"; "Closing balance" } } }
                tbody {
                    tr { _dataShow "$chartperiod == '30d'"; td { _class "py-2"; "30 days" }; td { _class "py-2 text-right"; "$24,820" } }
                    tr { _dataShow "$chartperiod == '90d'"; td { _class "py-2"; "90 days" }; td { _class "py-2 text-right"; "$24,820" } }
                }
            }
        }

    let messagingIntegrationExample =
        div {
            _dataSignals "{thread: 'northwind'}"
            _class "grid min-w-0 gap-4 md:grid-cols-[16rem_minmax(0,1fr)]"
            nav {
                _ariaLabel "Conversations"
                ul {
                    _class "grid gap-2"
                    for value, label, preview in [ "northwind", "Northwind", "Can we move pickup?"; "contoso", "Contoso", "Thanks for the update." ] do
                        li {
                            button {
                                _type "button"
                                _dataOn ("click", $"$thread = '{value}'")
                                _dataAttr ("aria-pressed", $"$thread == '{value}' ? 'true' : 'false'")
                                _class "w-full rounded-[var(--fve-radius-control)] p-3 text-left ring-1 ring-[var(--fve-border)] aria-pressed:bg-[var(--fve-brand-subtle)]"
                                strong { _class "block"; label }
                                span { _class "block truncate text-sm text-[var(--fve-muted-text)]"; preview }
                            }
                        }
                }
            }
            section {
                _ariaLabel "Current conversation"
                _class "grid min-w-0 gap-3"
                div {
                    _dataShow "$thread == 'northwind'"
                    h3 { _class "font-semibold"; "Northwind" }
                    p { _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface-subtle)] p-3"; "Can we move pickup to 10:30 AM?" }
                }
                div {
                    _dataShow "$thread == 'contoso'"
                    h3 { _class "font-semibold"; "Contoso" }
                    p { _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface-subtle)] p-3"; "Thanks for the update." }
                }
                Textarea.create "reply" "Reply"
                |> Textarea.withDescription "Messages in this bounded example stay local to the rendered page."
                |> Textarea.render
                Button.create (ButtonContent.Text "Send message")
                |> Button.withAttributes [ _dataOn ("click", "document.getElementById('message-feedback').textContent = 'Message queued in this resettable demo.'") ]
                |> Button.render
                p {
                    _id "message-feedback"
                    _role "status"
                    _ariaLive "polite"
                    _class "text-sm text-[var(--fve-muted-text)]"
                }
            }
        }

    let private shellSource = [ "ShellDestination"; "shellDestinationKey"; "shellDestinationUrl" ]
    let private formSource = [ "choiceSubmitButton"; "choiceResult" ]
    let private selectSource = [ "AccountStatus"; "statusValue"; "selectStatusOptions" ]
    let private comboboxSource = [ "accounts"; "accountOptions" ]
    let private sideNavSource = shellSource @ [ "ledgerMark"; "navigationGlyph"; "shellItem"; "ledgerNavigation"; "groupedLedgerNavigation"; "sideNavExample" ]

    let paginationExamples requestedPage =
        [ sample "pagination" "Page navigation" [ "PaginationDestination"; "paginationDestinationUrl"; "paginationConfig"; "paginationPreview" ] (centered (paginationPreviewRegion requestedPage))
          sample "pagination-numbers" "Numbers only" [ "PaginationDestination"; "paginationDestinationUrl"; "paginationConfig"; "paginationVariantPreview" ] (centered (paginationVariantPreview PaginationVariant.NumbersOnly requestedPage))
          sample "pagination-edges" "Previous and next only" [ "PaginationDestination"; "paginationDestinationUrl"; "paginationConfig"; "paginationVariantPreview" ] (centered (paginationVariantPreview PaginationVariant.PreviousNextOnly requestedPage)) ]

    let paginationGalleryExamples requestedPage =
        sample "pagination-default" "Default" [ "defaultPagination" ] (fieldSurface defaultPagination)
        :: paginationExamples requestedPage

    type ShellLayoutDestination = Dashboard | Projects | Preferences

    let shellLayoutLabel = function
        | Dashboard -> "Dashboard"
        | Projects -> "Projects"
        | Preferences -> "Settings"

    let shellLayoutUrl = function
        | Dashboard -> "/components/app-shell"
        | Projects -> "/components/app-shell?section=projects"
        | Preferences -> "/components/app-shell?section=settings"

    let shellLayoutExample id width bottomNavigation collapsible current =
        let layoutLabel = if bottomNavigation then "Full-width workspace" else "Constrained workspace"
        let header = SideNavHeader.create "Workspace" |> SideNavHeader.withCompactContent (span { _ariaHidden true; _class "font-semibold"; "W" })
        let navigation =
            SideNav.create (id + "-navigation") (layoutLabel + " navigation") header [
                SideNavSection.ungrouped [
                    SideNavItem.create Dashboard "Dashboard" |> SideNavItem.withLeading navigationGlyph
                    SideNavItem.create Projects "Projects" |> SideNavItem.withLeading navigationGlyph
                    SideNavItem.create Preferences "Settings" |> SideNavItem.withLeading navigationGlyph ] ]
            |> SideNav.withCurrent current
            |> SideNav.withFooter (a { _href (shellLayoutUrl Preferences); _class "block px-3 py-2 text-sm font-semibold"; "Andy Meier" })
            |> SideNav.withCompactFooter (a { _href (shellLayoutUrl Preferences); _ariaLabel "Andy Meier profile"; _class "flex size-8 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs font-semibold"; "AM" })
        let content =
            Page.create (PageHeader.create (shellLayoutLabel current)) (
                div {
                    _class "grid min-h-80 place-items-center rounded-lg border border-dashed border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] p-6 text-center text-sm text-[var(--fve-muted-text)]"
                    "Page content"
                })
            |> Page.withWidth width
            |> Page.withTopBar (
                PageTopBar.create ()
                |> PageTopBar.withContent (div {
                    _class "flex min-w-0 flex-1 items-center"
                    Breadcrumbs.create (id + "-breadcrumbs") (layoutLabel + " breadcrumb") [
                        if current <> Dashboard then BreadcrumbItem.create Dashboard "Dashboard"
                        BreadcrumbItem.create current (shellLayoutLabel current) ]
                    |> Breadcrumbs.render shellLayoutUrl
                }))
            |> Page.render
        let shell =
            AppShell.create id navigation content
            |> AppShell.withBoundary AppShellBoundary.Container
            |> AppShell.asPreview (layoutLabel + " layout preview")
        let shell = if collapsible then shell |> AppShell.withCollapsibleNavigation else shell
        let shell =
            if bottomNavigation then
                shell |> AppShell.withMobileBottomNavigation (id + "-bottom") "Workspace quick navigation" [
                    BottomNavigationItem.create Dashboard "Dashboard"
                    BottomNavigationItem.create Projects "Projects"
                    BottomNavigationItem.create Preferences "Settings" ]
            else shell
        div {
            _class "h-[36rem]"
            shell |> AppShell.render shellLayoutUrl
        }

    let appShellExamples current =
        let source = [ "navigationGlyph"; "ShellLayoutDestination"; "shellLayoutLabel"; "shellLayoutUrl"; "shellLayoutExample" ]
        let preview _ (content:HtmlElement) =
            themedPreview content
        let withUsage usage (example:ComponentExample) = { example with source = example.source + "\n\n" + usage }
        [ sample "app-shell" "Collapsible sidebar with constrained content" source (preview "app-shell-sidebar" (shellLayoutExample "layout-sidebar" PageWidth.Reading false true current))
          |> withUsage "shellLayoutExample \"layout-sidebar\" PageWidth.Reading false true Dashboard"
          sample "app-shell-bottom" "Full-width content with mobile bottom navigation" source (preview "app-shell-bottom" (shellLayoutExample "layout-bottom" PageWidth.Full true false current))
          |> withUsage "shellLayoutExample \"layout-bottom\" PageWidth.Full true false Dashboard" ]

    let accountManagementExamples current =
        [ sample "account-management" "Connected account workflow" (shellSource @ [ "ledgerShellExample"; "treasuryShellExample" ]) (shellFixtureFor current)
          sample "responsive-actions" "Responsive page actions" (shellSource @ [ "refreshIcon"; "moreActionsIcon"; "refreshBalancesAction"; "pageAction"; "accountPageActions"; "responsiveActionsExample" ])
              (responsiveActionsExample |> fullBleedThemedSurface)
          |> note "This is consumer-authored layout, not component policy. The highest-priority action stays visible; below 400px of header width, the reports link moves into a separately identified menu. Both menus use the same real destinations and DropdownMenu keyboard behavior." ]

    let private calendarExampleSource example = [ "calendarDemoDate"; "calendarEventDestination"; "calendarDemoEvents"; "calendarDateDestination"; example ]
    let private calendarDefaultSource example = [ "calendarDemoDate"; "calendarEventDestination"; "defaultCalendarEvent"; example ]

    let dayCalendarExamples =
        [ sample "day-calendar-default" "Default" (calendarDefaultSource "defaultDayCalendar") (calendarSurface defaultDayCalendar)
          sample "day-calendar-events" "Overlapping sessions" (calendarExampleSource "calendarDayExample") (calendarSurface calendarDayExample) ]

    let weekCalendarExamples =
        [ sample "week-calendar-default" "Default" (calendarDefaultSource "defaultWeekCalendar") (calendarSurface defaultWeekCalendar)
          sample "week-calendar-events" "Events and date navigation" (calendarExampleSource "calendarWeekExample") (calendarSurface calendarWeekExample) ]

    let yearCalendarExamples =
        [ sample "year-calendar-default" "Default" (calendarDefaultSource "defaultYearCalendar") (calendarSurface defaultYearCalendar)
          sample "year-calendar-events" "Event indicators and date navigation" (calendarExampleSource "calendarYearExample") (calendarSurface calendarYearExample) ]

    let private tableExamples current =
        [ sample "table" "Default" [ "TeamMember"; "teamMembers"; "teamColumns"; "simpleTeamTable" ] (detailsSurface simpleTeamTable)
          sample "table-comfortable" "Comfortable rows" [ "TeamMember"; "teamMembers"; "teamColumns"; "comfortableTeamTable" ] (detailsSurface comfortableTeamTable)
          sample "table-status" "With status values" [ "memberStatusTable" ] (detailsSurface memberStatusTable)
          sample "table-selection" "With checkboxes" [ "TeamMember"; "teamMembers"; "teamColumns"; "selectableTeamTable" ] (detailsSurface selectableTeamTable)
          sample "table-mobile" "Stacked on mobile" [ "TeamMember"; "teamMembers"; "mobileTeamTable" ] (detailsSurface mobileTeamTable)
          sample "table-sorting" "Sortable records" [ "TeamMember"; "teamMembers"; "TeamMemberSort"; "teamMemberSortUrl"; "documentSort"; "sortFor"; "sortableTeamTable" ] (detailsSurface (sortableTeamTablePreview current))
          |> note "The application owns the query, destination, and ordered rows. Table only renders the accessible sort controls."
          sample "table-hierarchy" "Hierarchical accounts and aggregates" [ "HierarchyAccount"; "money"; "hierarchyAccounts"; "hierarchicalAccountTable" ] (detailsSurface hierarchicalAccountTable)
          |> note "The consumer supplies every ancestor, level, aggregate value, and destination. Table owns disclosure presentation only."
          sample "table-empty" "Empty state" [ "TeamMember"; "teamColumns"; "emptyTeamTable" ] (detailsSurface emptyTeamTable) ]

    let examplesFor = function
        | "button" -> [
            sample "button-basic" "Default" [ "basicButton" ] (centered basicButton)
            sample "button-colors" "Colors" [ "buttonColorExamples" ] (centered buttonColorExamples)
            sample "button-content" "Content modes" [ "plusIcon"; "refreshIcon"; "buttonContentExamples" ] (centered buttonContentExamples)
            |> note "ButtonContent makes text, icon-only, icon-before-text, text-before-icon, and custom presentational HTML explicit. Icon-only buttons require an accessible name; Custom content must not contain nested interactive controls."
            sample "button-variants" "Variants" [ "buttonVariantExamples" ] (centered buttonVariantExamples)
            sample "button-icon-variants" "Icon-only variants" [ "refreshIcon"; "iconOnlyButtonVariantExamples" ] (centered iconOnlyButtonVariantExamples)
            sample "button-custom" "Custom palette" [ "customButtonPalette"; "customPaletteButtons" ] (centered customPaletteButtons)
            |> note "Supply explicit light/dark foreground, background, border, focus and interaction colors. You own contrast for custom palettes; one arbitrary color cannot guarantee readable text."
            sample "button" "Primary buttons" [ "primaryButtons" ] (centered primaryButtons)
            |> note "Enabled actions move down one pixel while pressed without shifting the layout. Reduced motion, unavailable controls, and popup triggers do not move."
            sample "button-secondary" "Secondary buttons" [ "secondaryButtons" ] (centered secondaryButtons)
            sample "button-ghost" "Ghost buttons" [ "ghostButtons" ] (centered ghostButtons)
            sample "button-destructive" "Destructive buttons" [ "destructiveButtons" ] (centered destructiveButtons)
            sample "button-pending" "Pending buttons" [ "pendingSyncButton" ] (centered pendingSyncButton)
            sample "button-error-pending" "Pending error-colored action" [ "pendingErrorButton" ] (centered pendingErrorButton)
            |> note "Pending is temporary work: the button is busy, displays progress, and prevents another activation."
            sample "button-disabled" "Disabled buttons" [ "disabledDeleteButton" ] (centered disabledDeleteButton)
            |> note "Disabled is unavailable and does not display progress or claim that work is running."
            sample "button-icon-primary" "Primary icon-only" [ "plusIcon"; "addAccountIconOnlyButton" ] (centered addAccountIconOnlyButton)
            sample "button-icon-pending" "Pending icon-only" [ "refreshIcon"; "refreshingIconOnlyButton" ] (centered refreshingIconOnlyButton)
            sample "button-icon-disabled" "Disabled icon-only" [ "removeIcon"; "disabledRemoveIconOnlyButton" ] (centered disabledRemoveIconOnlyButton) ]
        | "button-group" -> [
            sample "button-group-default" "Default" [ "defaultButtonGroup" ] (centered defaultButtonGroup)
            sample "button-group" "Grouped actions" [ "backIcon"; "moreActionsIcon"; "buttonGroupExample" ] (centered buttonGroupExample)
            sample "button-group-vertical" "Vertical orientation" [ "verticalButtonGroup" ] (centered verticalButtonGroup) ]
        | "badge" -> [
            sample "badge" "Default" [ "badgeExample" ] (centered badgeExample)
            sample "badge-colors" "Colors" [ "badgeColorExamples" ] (centered badgeColorExamples)
            sample "badge-variants" "Variants" [ "badgeVariantExamples" ] (centered badgeVariantExamples) ]
        | "kbd" -> [
            sample "kbd" "Default" [ "keyboardKey" ] (centered keyboardKey)
            sample "kbd-shortcut" "Shortcut group" [ "keyboardShortcut" ] (centered keyboardShortcut)
            |> note "Kbd renders semantic key labels only. The host owns any corresponding keyboard behavior." ]
        | "separator" -> [
            sample "separator-default" "Default" [ "defaultSeparator" ] (centered defaultSeparator)
            sample "separator" "Orientation and semantics" [ "separatorExamples" ] (centered separatorExamples) ]
        | "skeleton" -> [
            sample "skeleton-default" "Default" [ "defaultSkeleton" ] (fieldSurface defaultSkeleton)
            sample "skeleton-text" "Text" [ "textSkeleton" ] (fieldSurface textSkeleton)
            sample "skeleton-avatar" "Avatar row" [ "avatarSkeleton" ] (fieldSurface avatarSkeleton)
            |> note "Consumer-authored HTML owns the layout; one busy status owns the announcement while individual placeholders remain silent."
            sample "skeleton-card" "Card" [ "cardSkeleton" ] (fieldSurface cardSkeleton)
            |> note "Use Skeleton for initial loading, not to replace already-visible content. Reduced-motion users receive static placeholders." ]
        | "item" -> [
            sample "item-default" "Default" [ "defaultItem" ] (centered defaultItem)
            sample "item" "Links and action rows" [ "itemExample" ] (centered itemExample)
            |> note "Whole-row links never contain nested controls. Rows with actions keep their links and buttons individually reachable." ]
        | "resizable" -> [
            sample "resizable-default" "Default" [ "defaultResizablePanels" ] (detailsSurface defaultResizablePanels)
            sample "resizable" "Horizontal split panels" [ "resizablePanels" ] (detailsSurface resizablePanels)
            sample "resizable-vertical" "Vertical split panels" [ "verticalResizablePanels" ] (detailsSurface verticalResizablePanels)
            |> note "Pointer, touch and keyboard resizing are local. Consumers decide whether to persist a resulting size." ]
        | "tooltip" -> [
            sample "tooltip" "Default" [ "basicTooltip" ] (centered basicTooltip)
            sample "tooltip-bottom" "Bottom placement" [ "bottomTooltip" ] (centered bottomTooltip)
            |> note "The trigger retains its own accessible name. Tooltip text is supplementary and never contains interactive content." ]
        | "popover" -> [
            sample "popover" "Default" [ "profilePopover" ] (centered profilePopover)
            sample "popover-focus" "Initial content focus" [ "focusedProfilePopover" ] (centered focusedProfilePopover)
            sample "popover-alignment" "Side and alignment" [ "alignedPopover" ] (centered alignedPopover)
            |> note "Popover owns trigger relationships, top-layer dismissal and optional initial focus. Use Dropdown menu for commands and Floating panel for persistent surfaces." ]
        | "message" -> [
            sample "message-default" "Default" [ "defaultMessage" ] (detailsSurface defaultMessage)
            sample "message" "Sender and receiver" [ "messageRows" ] (detailsSurface messageRows)
            sample "message-no-avatar" "Metadata and actions without an avatar" [ "messageWithoutAvatar" ] (detailsSurface messageWithoutAvatar)
            |> note "Message owns layout only. The host owns conversation transport, persistence, streaming, retry and delivery truth." ]
        | "loading-indicator" -> [
            sample "loading-indicator" "Default" [ "defaultLoadingIndicator" ] (centered defaultLoadingIndicator)
            sample "loading-indicator-label" "With visible label" [ "visibleLoadingIndicator" ] (centered visibleLoadingIndicator) ]
        | "progress" -> [
            sample "progress-active" "Default" [ "operationalProgressExample" ] (fieldSurface operationalProgressExample)
            sample "progress-indeterminate" "Indeterminate" [ "indeterminateProgressExample" ] (fieldSurface indeterminateProgressExample)
            |> note "Indeterminate progress reports work without aria-valuenow or an invented percentage."
            sample "progress-complete" "Complete" [ "completedProgressExample" ] (fieldSurface completedProgressExample)
            sample "progress-failed" "Failed" [ "failedProgressExample" ] (fieldSurface failedProgressExample) ]
        | "empty-state" -> [
            sample "empty-state-default" "Default" [ "defaultEmptyState" ] (fieldSurface defaultEmptyState)
            sample "empty-state" "With recovery action" [ "plusIcon"; "emptyStateExample" ] (fieldSurface emptyStateExample) ]
        | "table" -> tableExamples NameAscending
        | "description-list" -> [
            sample "description-list-default" "Default" [ "defaultDescriptionList" ] (detailsSurface defaultDescriptionList)
            sample "description-list" "Three-column details" [ "accountDetails" ] (detailsSurface accountDetails)
            sample "description-list-four-columns" "Four-column details" [ "accountOverview" ] (detailsSurface accountOverview)
            sample "description-list-supporting-text" "With supporting text" [ "accountDetailsWithDescriptions" ] (detailsSurface accountDetailsWithDescriptions) ]
        | "metric" -> [
            sample "metric-default" "Default" [ "defaultMetric" ] (centered defaultMetric)
            sample "metric" "With trend and status" [ "availableBalanceMetric" ] (centered availableBalanceMetric)
            sample "metric-pending" "With review status" [ "pendingEntriesMetric" ] (centered pendingEntriesMetric)
            sample "metric-multiple" "Multiple metrics" [ "multipleMetrics" ] (centered multipleMetrics) ]
        | "pagination" -> paginationGalleryExamples 2
        | "avatar" -> [
            sample "avatar-default" "Default" [ "defaultAvatar" ] (centered defaultAvatar)
            sample "avatar-fallback" "Fallbacks and sizes" [ "avatarFallbackExample" ] (centered avatarFallbackExample)
            sample "avatar-image" "Image" [ "avatarImageExample" ] (centered avatarImageExample)
            sample "avatar-decorative" "Decorative" [ "decorativeAvatarExample" ] (centered decorativeAvatarExample) ]
        | "copy-reveal" -> [
            sample "copy-reveal" "Default" [ "copyRevealExample" ] (fieldSurface copyRevealExample)
            sample "copy-reveal-icons" "Icon actions" [ "iconCopyRevealExample" ] (fieldSurface iconCopyRevealExample)
            sample "copy-reveal-revealed" "Initially revealed" [ "revealedCopyRevealExample" ] (fieldSurface revealedCopyRevealExample) ]
        | "input" -> [
            sample "input" "Default" [ "labelledInput" ] (fieldSurface labelledInput)
            sample "input-sizes" "Consistent control sizes" [ "refreshIcon"; "inputSizeExamples" ] (detailsSurface inputSizeExamples)
            |> note "Small, Medium and Large use 32, 40 and 48px baselines. Small uses compact 14px/20px application text; Medium and Large use 16px/24px text. Values use regular weight, actions medium, and field labels remain 14px. Set a region with ControlSize.className, or set an application theme with ComponentsTheme.withControlSize. Explicit withSize overrides inherit neither the region size nor layout density."
            sample "input-help" "With help text" [ "inputWithHelp" ] (fieldSurface inputWithHelp)
            sample "input-required" "Required" [ "requiredInput" ] (fieldSurface requiredInput)
            sample "input-optional" "Optional" [ "optionalInput" ] (fieldSurface optionalInput)
            sample "input-validation" "With validation error" [ "invalidInput" ] (fieldSurface invalidInput)
            sample "input-icon" "With leading icon" [ "emailIcon"; "inputWithIcon" ] (fieldSurface inputWithIcon)
            sample "input-prefix" "With prefix" [ "inputWithPrefix" ] (fieldSurface inputWithPrefix)
            sample "input-suffix" "With suffix" [ "inputWithSuffix" ] (fieldSurface inputWithSuffix)
            sample "search-input" "Search with clear action" [ "searchInputExample" ] (fieldSurface searchInputExample)
            sample "input-disabled" "Disabled" [ "disabledInput" ] (fieldSurface disabledInput)
            sample "input-pending" "Pending" [ "pendingInput" ] (fieldSurface pendingInput) ]
        | "field" -> [
            sample "field-default" "Default" [ "defaultField" ] (fieldSurface defaultField)
            sample "field" "Custom control" [ "customCurrencyField" ] (fieldSurface customCurrencyField)
            sample "field-responsive" "Responsive label and control rows" [ "responsiveFieldRows" ] (fieldSurface responsiveFieldRows)
            sample "field-group" "Semantic fieldset" [ "groupedFields" ] (fieldSurface groupedFields)
            |> note "Field owns one label/help/error relationship around a custom control. Complete controls such as Input, Textarea and Checkbox remain convenient standalone APIs and should not be nested inside another Field wrapper." ]
        | "input-group" -> [
            sample "input-group-default" "Default" [ "defaultInputGroup" ] (fieldSurface defaultInputGroup)
            sample "input-group" "Text addons" [ "currencyInputGroup" ] (fieldSurface currencyInputGroup)
            sample "input-group-search" "Icon and keyboard hint" [ "searchInputGroup" ] (fieldSurface searchInputGroup)
            sample "input-group-action" "Interactive addon" [ "actionableInputGroup" ] (fieldSurface actionableInputGroup)
            sample "input-group-dropdown" "Dropdown addon" [ "dropdownInputGroup" ] (fieldSurface dropdownInputGroup)
            sample "input-group-textarea" "Textarea footer" [ "textareaInputGroup" ] (fieldSurface textareaInputGroup)
            sample "input-group-validation" "Validation" [ "invalidInputGroup" ] (fieldSurface invalidInputGroup)
            sample "input-group-pending" "Pending" [ "pendingInputGroup" ] (fieldSurface pendingInputGroup)
            |> note "Decorative addons are not form values or focus targets. Interactive addons keep their own accessible names and follow the native Tab order." ]
        | "form-layouts" -> [
            sample "form-layouts" "Stacked with server validation" [ "choiceSubmitButton"; "ContactDetails"; "ContactFormLayout"; "contactFormRegion"; "emptyContact"; "contactFormExample" ] (fullBleedThemedSurface contactFormExample)
            |> note "Submit to see validation errors. This example does not save contact details. The validation form previously shown on Input now lives here."
            sample "form-layouts-grid" "Two-column form" [ "choiceSubmitButton"; "ContactDetails"; "ContactFormLayout"; "contactFormRegion"; "emptyContact"; "twoColumnFormExample" ] (fullBleedThemedSurface twoColumnFormExample)
            sample "form-layouts-sections" "Sectioned form" [ "choiceSubmitButton"; "ContactDetails"; "ContactFormLayout"; "contactFormRegion"; "emptyContact"; "sectionedFormExample" ] (fullBleedThemedSurface sectionedFormExample) ]
        | "file-selection" -> [
            sample "file-selection-default" "Default" [ "defaultFileSelection" ] (fieldSurface defaultFileSelection)
            sample "file-selection" "Multiple files" [ "fileSelectionExample" ] (fieldSurface fileSelectionExample)
            sample "file-selection-validation" "Validation" [ "invalidFileSelectionExample" ] (fieldSurface invalidFileSelectionExample)
            sample "file-selection-pending" "Pending" [ "pendingFileSelectionExample" ] (fieldSurface pendingFileSelectionExample)
            |> note "Pending communicates temporary validation with busy status; disabled would communicate only unavailability." ]
        | "tag-input" -> [
            sample "tag-input" "Default" [ "tagInputExample" ] (fieldSurface tagInputExample)
            sample "tag-input-validation" "Validation" [ "invalidTagInputExample" ] (fieldSurface invalidTagInputExample)
            sample "tag-input-pending" "Pending" [ "pendingTagInputExample" ] (fieldSurface pendingTagInputExample)
            |> note "Pending communicates that an update is temporarily in progress."
            sample "tag-input-disabled" "Disabled" [ "disabledTagInputExample" ] (fieldSurface disabledTagInputExample)
            |> note "Disabled communicates unavailability without busy or progress feedback." ]
        | "textarea" -> [
            sample "textarea" "Default" [ "labelledTextarea" ] (fieldSurface labelledTextarea)
            sample "textarea-composer" "Visually hidden label" [ "composerTextarea" ] (fieldSurface composerTextarea)
            sample "textarea-help" "With instructions" [ "editableInstructions" ] (fieldSurface editableInstructions)
            sample "textarea-validation" "With validation error" [ "invalidTextarea" ] (fieldSurface invalidTextarea)
            sample "textarea-pending" "Pending" [ "pendingNotes" ] (fieldSurface pendingNotes)
            sample "textarea-disabled" "Disabled" [ "unavailableNotes" ] (fieldSurface unavailableNotes) ]
        | "error-summary" -> [
            sample "error-summary" "Linked field errors" [ "errorSummaryExample" ] (fullBleedThemedSurface errorSummaryExample)
            |> note "Click the error to focus its field. Your application supplies and clears validation messages." ]
        | "notice" -> [
            sample "notice" "Default" [ "informationNotice" ] (fieldSurface informationNotice)
            sample "notice-colors" "Colors" [ "noticeColorExamples" ] (detailsSurface noticeColorExamples)
            sample "notice-variants" "Variants" [ "noticeVariantExamples" ] (detailsSurface noticeVariantExamples)
            sample "notice-success" "Success with download" [ "successNotice" ] (fieldSurface successNotice)
            sample "notice-warning" "Warning" [ "warningNotice" ] (fieldSurface warningNotice)
            sample "notice-critical" "Error with recovery link" [ "criticalNotice" ] (fieldSurface criticalNotice) ]
        | "notification" -> [
            sample "notification" "Default" [ "notificationLeadItem"; "notificationLeadRegion"; "simpleNotificationExample" ] (overlaySurface simpleNotificationExample)
            sample "notification-actions" "Persistent with an action" [ "notificationWithActionsExample" ] (overlaySurface notificationWithActionsExample)
            sample "notification-timing" "Custom lifetime" [ "timedNotificationExample" ] (overlaySurface timedNotificationExample)
            |> note "Notification owns dismissal and its local lifetime. NotificationRegion renders zero or one notification; each newly confirmed notification replaces the current one. Persistent notifications do not expire automatically but can still be replaced." ]
        | "select" -> [
            sample "select-default" "Default" [ "defaultSelect" ] (fieldSurface defaultSelect)
            sample "select" "With selected value" [ "basicSelect" ] (fieldSurface basicSelect)
            sample "select-help" "With help text" [ "selectWithHelp" ] (fieldSurface selectWithHelp)
            sample "select-edge" "Trigger-edge position" [ "edgeAlignedSelect" ] (fieldSurface edgeAlignedSelect)
            |> note "Ordinary single selects align the selected option over the closed value by default. Choose a trigger edge explicitly for dense layouts; searchable and multiple selects remain edge-positioned. Touch and viewport-edge cases fall back to an edge position."
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
        | "month-calendar" -> [
            sample "month-calendar-default" "Default" (calendarDefaultSource "defaultMonthCalendar") (calendarSurface defaultMonthCalendar)
            sample "month-calendar-events" "Events and date navigation" (calendarExampleSource "calendarMonthExample") (calendarSurface calendarMonthExample)
            sample "month-calendar-compact" "Compact month" (calendarExampleSource "compactMonthCalendar") (calendarSurface compactMonthCalendar)
            sample "month-calendar-selection" "Single-date selection" [ "monthSelection" ] (calendarSurface monthSelection)
            sample "month-calendar-single" "Clearable single date" [ "monthSelectionSingle" ] (calendarSurface monthSelectionSingle)
            sample "month-calendar-range" "Date range" [ "monthSelectionRange" ] (calendarSurface monthSelectionRange)
            sample "month-calendar-bounds" "Bounds, unavailable dates and Monday start" [ "boundedMonthSelection" ] (calendarSurface boundedMonthSelection)
            sample "month-calendar-caption" "Month and year selectors" [ "captionMonthSelection" ] (calendarSurface captionMonthSelection)
            |> note "Use the dropdown caption for dates far from the current month, such as birth dates. Bounds constrain the selectors and arrow navigation."
            sample "month-calendar-pending" "Pending availability" [ "pendingMonthSelection" ] (calendarSurface pendingMonthSelection)
            |> note "Form selection submits canonical date-only values. Display calendars use date destinations instead; Date picker composes the compact selection configuration." ]
        | "date-picker" -> [
            sample "date-picker-default" "Default" [ "defaultDatePicker" ] (fieldSurface defaultDatePicker)
            sample "date-picker" "Clearable single date" [ "basicDatePicker" ] (fieldSurface basicDatePicker)
            sample "date-picker-range" "Date range" [ "rangeDatePicker" ] (fieldSurface rangeDatePicker)
            sample "date-picker-caption" "Month and year selectors" [ "captionDatePicker" ] (fieldSurface captionDatePicker)
            |> note "Month and year selectors make long-distance dates practical while the calendar retains arrow and grid-keyboard navigation."
            sample "date-picker-bounds" "Bounds and unavailable dates" [ "boundedDatePicker" ] (fieldSurface boundedDatePicker)
            sample "date-picker-validation" "Validation" [ "invalidDatePicker" ] (fieldSurface invalidDatePicker)
            sample "date-picker-pending" "Pending" [ "pendingDatePicker" ] (fieldSurface pendingDatePicker)
            sample "date-picker-disabled" "Disabled" [ "disabledDatePicker" ] (fieldSurface disabledDatePicker)
            |> note "DatePicker composes Popover and compact MonthCalendar selection. The server remains responsible for validating submitted date-only values." ]
        | "checkbox" -> [
            sample "checkbox" "Default" [ "basicCheckbox" ] (fieldSurface basicCheckbox)
            sample "checkbox-help" "With help text" [ "checkboxWithHelp" ] (fieldSurface checkboxWithHelp)
            sample "checkbox-validation" "Required confirmation with validation" (formSource @ [ "checkboxFormRegion" ]) (fieldSurface (checkboxFormRegion false None None))
            sample "checkbox-checked" "Checked" [ "includeArchived" ] (fieldSurface includeArchived)
            sample "checkbox-pending" "Pending" [ "pendingArchivedReview" ] (fieldSurface pendingArchivedReview)
            sample "checkbox-disabled" "Disabled" [ "disabledArchivedReview" ] (fieldSurface disabledArchivedReview) ]
        | "switch" -> [
            sample "switch" "Default" [ "basicSwitch" ] (fieldSurface basicSwitch)
            sample "switch-help" "With help text" [ "switchWithHelp" ] (fieldSurface switchWithHelp)
            sample "switch-submission" "Setting with submission" (formSource @ [ "switchFormRegion"; "postingNotifications" ]) (fieldSurface postingNotifications)
            sample "switch-pending" "Pending" [ "pendingNotifications" ] (fieldSurface pendingNotifications)
            sample "switch-validation" "Validation error" [ "invalidNotifications" ] (fieldSurface invalidNotifications)
            sample "switch-sizes" "Sizes" [ "switchSizes" ] (fieldSurface switchSizes)
            sample "switch-stacked" "Stacked layout" [ "stackedSwitch" ] (fieldSurface stackedSwitch) ]
        | "toggle-button" -> [
            sample "toggle-button" "Default" [ "compactRows" ] (centered compactRows)
            sample "toggle-button-pending" "Pending" [ "pendingCompactRows" ] (centered pendingCompactRows)
            sample "toggle-button-disabled" "Disabled" [ "disabledCompactRows" ] (centered disabledCompactRows)
            sample "toggle-button-presentation" "Content, sizes and variants" [ "plusIcon"; "toggleButtonPresentation" ] (centered toggleButtonPresentation) ]
        | "toggle-group" -> [
            sample "toggle-group-default" "Default" [ "defaultToggleGroup" ] (centered defaultToggleGroup)
            sample "toggle-group-single" "Single selection" [ "singleToggleGroup" ] (centered singleToggleGroup)
            sample "toggle-group-multiple" "Multiple, spaced and unavailable" [ "plusIcon"; "refreshIcon"; "multipleToggleGroup" ] (centered multipleToggleGroup)
            sample "toggle-group-vertical" "Vertical orientation" [ "verticalToggleGroup" ] (centered verticalToggleGroup) ]
        | "tabs" -> [
            sample "tabs" "Default" [ "codePreviewTabs" ] (fieldSurface codePreviewTabs)
            sample "tabs-underlined" "Underlined tabs with refresh" [ "reviewTabsRegion" ] (fieldSurface (reviewTabsRegion false))
            sample "tabs-vertical" "Vertical, icons and disabled items" [ "plusIcon"; "refreshIcon"; "removeIcon"; "verticalTabs" ] (fieldSurface verticalTabs) ]
        | "radio-group" -> [
            sample "radio-group" "Default" [ "basicRadioGroup" ] (fieldSurface basicRadioGroup)
            sample "radio-group-help" "Option descriptions" [ "radioGroupWithHelp" ] (fieldSurface radioGroupWithHelp)
            sample "radio-group-horizontal" "Horizontal orientation" [ "horizontalRadioGroup" ] (fieldSurface horizontalRadioGroup)
            sample "radio-group-validation" "Required choice with validation" (formSource @ [ "postingModeOptions"; "radioGroupFormRegion"; "postingMode" ]) (fieldSurface postingMode)
            sample "radio-group-pending" "Pending" [ "postingModeOptions"; "pendingPostingMode" ] (fieldSurface pendingPostingMode)
            sample "radio-group-disabled" "Disabled" [ "postingModeOptions"; "disabledPostingMode" ] (fieldSurface disabledPostingMode) ]
        | "choice-cards" -> [
            sample "choice-cards-default" "Default" [ "defaultChoiceCards" ] (detailsSurface defaultChoiceCards)
            sample "choice-cards" "Single choice" [ "richChoiceCards" ] (detailsSurface richChoiceCards)
            sample "choice-cards-multiple" "Multiple choices" [ "multipleChoiceCards" ] (detailsSurface multipleChoiceCards)
            sample "choice-cards-validation" "Validation" [ "invalidChoiceCards" ] (detailsSurface invalidChoiceCards) ]
        | "dropdown-menu" -> [
            sample "dropdown-menu" "Basic menu" [ "basicDropdownMenu" ] (centered basicDropdownMenu)
            sample "dropdown-menu-full-row" "Full context row and supporting descriptions" [ "menuTrailingChevron"; "fullRowDropdownMenu" ] (centered fullRowDropdownMenu)
            |> note "The trigger fills its owning region without an inset border or rounded corners. Menu descriptions and trailing content preserve the same keyboard, dismissal and focus behavior."
            sample "dropdown-menu-advanced" "Grouped states and server refresh" [ "Destination"; "destinationUrl"; "menuLeadingIcon"; "dropdownMenuItems"; "actionMenu"; "menuOverflowIcon"; "moreActionsMenu"; "dropdownMenuRegion" ] (fieldSurface (dropdownMenuRegion false))
            |> note "This advanced example adds text and icon triggers, groups, independent checkbox choices, disabled, pending, destructive, shortcut, counters, and server-patched content." ]
        | "command" -> [
            sample "command-default" "Default" [ "defaultCommand" ] (fieldSurface defaultCommand)
            sample "command-groups" "Groups, icons and shortcuts" [ "commandDocumentIcon"; "commandGroups"; "groupedCommand" ] (fieldSurface groupedCommand)
            |> note "Shortcut labels do not register hotkeys. Matching ranks titles before keywords and descriptions; group order remains consumer-owned."
            sample "command-actions" "Actions and disabled items" [ "commandActionsExample" ] (fieldSurface commandActionsExample)
            sample "command-palette" "Dialog palette" [ "commandDocumentIcon"; "commandGroups"; "commandPaletteExample" ] (centered commandPaletteExample)
            sample "command-empty" "Empty results" [ "emptyCommand" ] (fieldSurface emptyCommand) ]
        | "dialog" -> [
            sample "dialog-default" "Default" [ "defaultDialog"; "defaultDialogExample" ] (centered defaultDialogExample)
            sample "dialog" "Modal dialog" [ "dialogConfig"; "reviewDialogTrigger"; "reviewDialog" ] (centered (div { reviewDialogTrigger; reviewDialog }))
            sample "dialog-confirmation" "Server-validated confirmation" [ "accountDeletionForm"; "accountDeletionContent"; "accountDeletionDialog"; "confirmationExample" ] (centered confirmationExample)
            |> note "Compose a native form and Buttons inside Dialog. Focus Cancel first and prevent dismissal or repeated submission while pending. The demo server rejects deletion so you can inspect its validation state."
            sample "dialog-pending" "Pending confirmation" [ "accountDeletionForm"; "pendingDeletionDialog"; "pendingConfirmationExample" ] (centered pendingConfirmationExample) ]
        | "drawer" -> [
            sample "drawer-default" "Default" [ "defaultDrawer"; "defaultDrawerExample" ] (centered defaultDrawerExample)
            sample "drawer" "Standard detail drawer" [ "accountDrawerContent"; "accountDrawerConfig"; "accountDrawerExample" ] (centered accountDrawerExample)
            sample "drawer-wide" "Wide editing form" [ "accountEditorBody"; "accountEditorFooter"; "accountEditorDrawerConfig"; "accountEditorDrawerExample" ] (centered accountEditorDrawerExample)
            sample "drawer-start" "Start-side filters" [ "filterDrawerConfig"; "filterDrawerExample" ] (centered filterDrawerExample)
            sample "drawer-horizontal" "Top and bottom edges" [ "topDrawerConfig"; "bottomDrawerConfig"; "horizontalDrawerExamples" ] (centered horizontalDrawerExamples)
            |> note "All four edges use the native modal dialog lifecycle. Gesture and snap-point behavior is intentionally outside this component." ]
        | "floating-panel" -> [
            sample "floating-panel" "Open non-modal panel" [ "floatingPanelContent"; "floatingPanelExample" ] (overlaySurface floatingPanelExample)
            sample "floating-panel-minimized" "Minimized with restore" [ "floatingPanelContent"; "minimizedFloatingPanelExample" ] (overlaySurface minimizedFloatingPanelExample) ]
        | "breadcrumbs" -> [
            sample "breadcrumbs" "Default" [ "breadcrumbsExample" ] breadcrumbsPreview
            sample "breadcrumbs-middle" "Middle overflow" [ "breadcrumbTrail"; "middleBreadcrumbsExample" ] (themedSurface middleBreadcrumbsExample)
            |> note "Long trails keep the root, parent, and current page visible. Open the middle menu for earlier ancestors. Narrow screens collect all ancestors into one menu."
            sample "breadcrumbs-visible" "Visible item count" [ "breadcrumbTrail"; "visibleBreadcrumbsExample" ] (themedSurface visibleBreadcrumbsExample)
            |> note "withMaxVisibleItems counts path items, not the overflow trigger. The default is three; choose at least two to retain the root and current page." ]
        | "side-nav" -> [
            sample "side-nav-default" "Default" [ "DefaultSideNavDestination"; "defaultSideNavUrl"; "defaultSideNav" ] defaultSideNavPreview
            sample "side-nav" "Nested navigation with badges and actions" sideNavSource sideNavigationPreview
            |> note "SideNav owns navigation structure, current destinations and optional workspace/profile regions. AppShell adds the opt-in desktop icon rail and mobile drawer behavior." ]
        | "page-top-bar" -> [ sample "page-top-bar" "With breadcrumbs" [ "pageTopBarExample"; "renderPageTopBar" ] pageTopBarPreview
                              sample "page-top-bar-shell" "Full-width shell bar" [ "shellTopBarExample" ] (shellTopBarExample |> themedSurface) ]
        | "page-header" -> [
            sample "page-header-default" "Default" [ "defaultPageHeader" ] (detailsSurface defaultPageHeader)
            sample "page-header" "Title and page actions" (shellSource @ [ "refreshIcon"; "refreshBalancesAction"; "pageAction"; "accountPageHeader"; "pageHeaderExample" ]) pageHeaderPreview ]
        | "section" -> [
            sample "section-default" "Default" [ "defaultSection" ] (detailsSurface defaultSection)
            sample "section" "With heading and actions" (shellSource @ [ "pageAction"; "moreActionsIcon"; "upcomingPayments" ]) (detailsSurface upcomingPayments)
            sample "section-headerless" "Without a visible heading" [ "periodNote" ] (detailsSurface periodNote) ]
        | "browser" -> [
            sample "browser-default" "Default" [ "defaultBrowser" ] (centered defaultBrowser)
            sample "browser" "With an address bar" [ "browserWithAddress" ] (centered browserWithAddress)
            |> note "The address is presentational; Browser renders the content without navigation or viewer controls." ]
        | "phone" -> [
            sample "phone-default" "Default" [ "defaultPhone" ] (centered defaultPhone)
            sample "phone" "With screen content" [ "phoneWithContent" ] (centered phoneWithContent)
            |> note "Phone owns the device treatment; the screen remains product-owned HTML." ]
        | "page" -> [
            sample "page-default" "Default" [ "defaultPage" ] (fullBleedThemedSurface defaultPage)
            sample "page" "With local navigation" [ "transactionsPage"; "renderTransactionsPage" ] transactionsPagePreview
            sample "page-canvas" "Remaining-height canvas" [ "canvasPage" ] canvasPagePreview ]
        | "collection" -> [
            sample "collection-default" "Default" [ "defaultCollection" ] (detailsSurface defaultCollection)
            sample "collection" "Collection with record actions" [ "collectionExample" ] collectionPreview ]
        | "detail" -> [
            sample "detail-default" "Default" [ "defaultDetail" ] (detailsSurface defaultDetail)
            sample "detail" "Detail with related records" [ "detailExample" ] detailPreview ]
        | "app-shell" -> appShellExamples Dashboard
        | "page-examples/account-management" -> accountManagementExamples LedgerAccounts
        | "bottom-navigation" -> [
            sample "bottom-navigation-default" "Default" [ "defaultBottomNavigation" ] (detailsSurface defaultBottomNavigation)
            sample "bottom-navigation" "Four destinations" [ "bottomNavigationExample" ] (detailsSurface bottomNavigationExample)
            sample "bottom-navigation-compact" "Three destinations" [ "compactBottomNavigationExample" ] (detailsSurface compactBottomNavigationExample) ]
        | "upload-list" -> [
            sample "upload" "Queue and recovery" [ "uploadQueueExample" ] (detailsSurface uploadQueueExample)
            sample "upload-empty" "Empty queue" [ "emptyUploadQueueExample" ] (detailsSurface emptyUploadQueueExample) ]
        | "steps" -> [ sample "steps" "Period-close steps" [ "periodCloseStepsExample" ] (detailsSurface periodCloseStepsExample) ]
        | "first-steps" -> [
            sample "first-steps" "Open setup guidance" [ "setupSteps"; "firstStepsExample" ] (overlaySurface firstStepsExample)
            sample "first-steps-minimized" "Minimized guidance" [ "setupSteps"; "minimizedFirstStepsExample" ] (overlaySurface minimizedFirstStepsExample)
            sample "first-steps-dismissed" "Dismissed with restore" [ "setupSteps"; "dismissedFirstStepsExample" ] (overlaySurface dismissedFirstStepsExample) ]
        | "day-calendar" -> dayCalendarExamples
        | "week-calendar" -> weekCalendarExamples
        | "year-calendar" -> yearCalendarExamples
        | "media-library" -> [ sample "media-library" "Library and editor" (shellSource @ [ "mediaLibraryExample" ]) (detailsSurface mediaLibraryExample) ]
        | value when value.StartsWith("page-examples/", StringComparison.Ordinal) ->
            let page = PageExamples.pages |> List.find (fun page -> value = "page-examples/" + PageExamples.slug page)
            [ { id = "components-" + PageExamples.slug page; title = PageExamples.title page + " workspace"; source = PageExamples.source page
                preview = connectedGallerySurface (PageExamples.preview page PageExamples.defaultQuery); note = None } ]
        | id -> invalidArg (nameof id) $"No component examples registered for '{id}'."

    let private pageExampleBuildingBlocks = function
        | PageExamples.DependencyGraph ->
            [ "app-shell", "App shell"
              "page", "Page"
              "input", "Input"
              "description-list", "Description list"
              "badge", "Badge" ]
        | PageExamples.ExecutionDetail ->
            [ "app-shell", "App shell"
              "page", "Page"
              "table", "Table"
              "description-list", "Description list"
              "notice", "Notice" ]
        | PageExamples.FinancialReporting ->
            [ "app-shell", "App shell"
              "page", "Page"
              "metric", "Metric"
              "table", "Table" ]
        | PageExamples.Messaging ->
            [ "app-shell", "App shell"
              "page", "Page"
              "side-nav", "Side nav"
              "textarea", "Textarea" ]
        | PageExamples.Operations ->
            [ "app-shell", "App shell"
              "page", "Page"
              "metric", "Metric"
              "table", "Table"
              "first-steps", "First steps" ]
        | PageExamples.Scheduling ->
            [ "app-shell", "App shell"
              "page", "Page"
              "day-calendar", "Day calendar"
              "week-calendar", "Week calendar"
              "month-calendar", "Month calendar"
              "year-calendar", "Year calendar"
              "description-list", "Description list" ]
        | PageExamples.MediaManagement ->
            [ "app-shell", "App shell"
              "page", "Page"
              "media-library", "Media library"
              "input", "Input"
              "textarea", "Textarea"
              "file-selection", "File selection" ]

    let pageExamplePageFor page query =
        let example =
            { id = "components-" + PageExamples.slug page
              title = PageExamples.title page + " workspace"
              source = PageExamples.source page
              preview = connectedGallerySurface (PageExamples.preview page query)
              note = None }
        gallery (PageExamples.registration page) [ example ]
        |> DocumentationPage.withFixtures [ PageExamples.fixture page query ]
        |> DocumentationPage.withSections [
            DocumentationSection.create example.id example.title [
                Example.gallery example.id example.title "fsharp" example.source example.preview ]
            DocumentationSection.create "building-blocks" "Built with" [
                pageExampleBuildingBlocks page
                |> List.map (fun (path, label) -> "/components/" + path, label)
                |> buildingBlockLinks ((PageExamples.title page) + " building blocks") ] ]

    let allExamples () =
        allRegistrations
        |> List.filter (fun page -> page.path.StartsWith("/components/", StringComparison.Ordinal) && page.path <> "/components/installation" && not (guideRegistrations |> List.exists (fun guide -> guide.path = page.path)))
        |> List.collect (fun page -> examplesFor (page.path.Substring("/components/".Length)))

    let installationPage =
        DocumentationPage.create installationRegistration.id installationRegistration.title |> DocumentationPage.withDescription "Pin fve, create a consumer-owned Components project, and add only the source your application uses." |> DocumentationPage.withSections [
            DocumentationSection.create "tool" "Pin the repository-local tool" [
                code "shell" "dotnet new tool-manifest\ndotnet tool install FSharp.ViewEngine.Cli\ndotnet tool restore"
                prose "A local tool manifest is the reproducible default. Global installation remains available with dotnet tool install --global FSharp.ViewEngine.Cli." ]
            DocumentationSection.create "project" "Initialize and add components" [
                code "shell" "dotnet fve init src/Acme.Components/Acme.Components.fsproj --namespace Acme.Components\ndotnet fve add button input --config src/Acme.Components/fve.json\ndotnet add src/Acme.Web/Acme.Web.fsproj reference src/Acme.Components/Acme.Components.fsproj"
                prose "The generated project references FSharp.ViewEngine normally. fve copies the selected canonical source plus required transitive dependencies in deterministic F# compile order; your repository owns and commits the result." ]
            DocumentationSection.create "tailwind" "Scan the owned F# source" [
                code "css" tailwindExample
                prose "Import Tailwind, then point source detection directly at the copied F# and application source. No NuGet-cache path or copied package source manifest is required."
                p { "See "; a { _href "/components/tailwind-css"; "Tailwind CSS setup" }; " for the exact source-detection contract." } ]
            DocumentationSection.create "namespace" "Use the selected namespace" [
                code "fsharp" "open FSharp.ViewEngine\nopen Acme.Components\nopen type Html"
                prose "Components remain ordinary typed F# values and functions. Edit the copied source when product needs differ from the canonical implementation." ] ]

    let buttonPage = gallery buttonRegistration (examplesFor "button")
    let buttonGroupPage = gallery buttonGroupRegistration (examplesFor "button-group")
    let badgePage = gallery badgeRegistration (examplesFor "badge")
    let kbdPage = gallery kbdRegistration (examplesFor "kbd")
    let separatorPage = gallery separatorRegistration (examplesFor "separator")
    let skeletonPage = gallery skeletonRegistration (examplesFor "skeleton")
    let itemPage = gallery itemRegistration (examplesFor "item")
    let loadingIndicatorPage = gallery loadingIndicatorRegistration (examplesFor "loading-indicator")
    let progressPage = gallery progressRegistration (examplesFor "progress")
    let emptyStatePage = gallery emptyStateRegistration (examplesFor "empty-state")
    let tablePageFor sort = gallery tableRegistration (tableExamples sort)
    let tablePage = tablePageFor NameAscending
    let descriptionListPage = gallery descriptionListRegistration (examplesFor "description-list")
    let metricPage = gallery metricRegistration (examplesFor "metric")
    let avatarPage = gallery avatarRegistration (examplesFor "avatar")
    let copyRevealPage = gallery copyRevealRegistration (examplesFor "copy-reveal")
    let inputPage = gallery inputRegistration (examplesFor "input")
    let fieldPage = gallery fieldRegistration (examplesFor "field")
    let inputGroupPage = gallery inputGroupRegistration (examplesFor "input-group")
    let fileSelectionPage = gallery fileSelectionRegistration (examplesFor "file-selection")
    let tagInputPage = gallery tagInputRegistration (examplesFor "tag-input")
    let formLayoutsPage = gallery formLayoutsRegistration (examplesFor "form-layouts")
    let textareaPage = gallery textareaRegistration (examplesFor "textarea")
    let errorSummaryPage = gallery errorSummaryRegistration (examplesFor "error-summary")
    let noticePage = gallery noticeRegistration (examplesFor "notice")
    let notificationPage = gallery notificationRegistration (examplesFor "notification")
    let selectPage = gallery selectRegistration (examplesFor "select")
    let datePickerPage = gallery datePickerRegistration (examplesFor "date-picker")
    let checkboxPage = gallery checkboxRegistration (examplesFor "checkbox")
    let switchPage = gallery switchRegistration (examplesFor "switch")
    let toggleButtonPage = gallery toggleButtonRegistration (examplesFor "toggle-button")
    let toggleGroupPage = gallery toggleGroupRegistration (examplesFor "toggle-group")
    let breadcrumbsPage = gallery breadcrumbsRegistration (examplesFor "breadcrumbs")
    let sideNavPage = gallery sideNavRegistration (examplesFor "side-nav")
    let tabsPage = gallery tabsRegistration (examplesFor "tabs")
    let radioGroupPage = gallery radioGroupRegistration (examplesFor "radio-group")
    let choiceCardsPage = gallery choiceCardsRegistration (examplesFor "choice-cards")
    let dropdownMenuPage = gallery dropdownMenuRegistration (examplesFor "dropdown-menu")
    let commandPage = gallery commandRegistration (examplesFor "command")
    let dialogPage = gallery dialogRegistration (examplesFor "dialog")
    let drawerPage = gallery drawerRegistration (examplesFor "drawer")
    let floatingPanelPage = gallery floatingPanelRegistration (examplesFor "floating-panel")
    let tooltipPage = gallery tooltipRegistration (examplesFor "tooltip")
    let popoverPage = gallery popoverRegistration (examplesFor "popover")
    let pageTopBarPage = gallery pageTopBarRegistration (examplesFor "page-top-bar")
    let pageHeaderPage = gallery pageHeaderRegistration (examplesFor "page-header")
    let sectionPage = gallery sectionRegistration (examplesFor "section")
    let browserPage = gallery browserRegistration (examplesFor "browser")
    let resizablePage = gallery resizableRegistration (examplesFor "resizable")
    let phonePage = gallery phoneRegistration (examplesFor "phone")
    let pagePage = gallery pageRegistration (examplesFor "page")
    let collectionPageDocumentation = gallery collectionRegistration (examplesFor "collection")
    let detailPageDocumentation = gallery detailRegistration (examplesFor "detail")

    let paginationPageFor requestedPage = gallery paginationRegistration (paginationGalleryExamples requestedPage)
    let paginationPage = paginationPageFor 2
    let appShellPageFor current = gallery appShellRegistration (appShellExamples current)
    let appShellPage = appShellPageFor Dashboard
    let accountManagementPageWith workspace current =
        let examples =
            accountManagementExamples current
            |> List.map (fun example ->
                if example.id = "components-account-management" then { example with preview = shellFixtureWith workspace current }
                else example)
        gallery accountManagementRegistration examples
        |> DocumentationPage.withFixtures [ shellFixtureConfigWith workspace current ]
    let accountManagementPageFor current = accountManagementPageWith defaultAccountWorkspace current
    let accountManagementPage = accountManagementPageFor LedgerAccounts
    let bottomNavigationPage = gallery bottomNavigationRegistration (examplesFor "bottom-navigation")
    let messagePage = gallery messageRegistration (examplesFor "message")
    let uploadPage = gallery uploadRegistration (examplesFor "upload-list")
    let stepsPage = gallery stepsRegistration (examplesFor "steps")
    let firstStepsPage = gallery firstStepsRegistration (examplesFor "first-steps")
    let dayCalendarPage = gallery dayCalendarRegistration dayCalendarExamples
    let weekCalendarPage = gallery weekCalendarRegistration weekCalendarExamples
    let monthCalendarPage = gallery monthCalendarRegistration (examplesFor "month-calendar")
    let yearCalendarPage = gallery yearCalendarRegistration yearCalendarExamples
    let mediaLibraryPage = gallery mediaLibraryRegistration (examplesFor "media-library")
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
            DocumentationSection.create "api-reference" "API reference" [
                h2 { _class "text-xl font-semibold text-[var(--fve-text)]"; "API reference" }
                FSharpApiReference.forCategory "accessibility" typeof<ButtonConfig> |> FSharpApiReference.render ]
            DocumentationSection.create "semantics" "Distinct semantics" [ prose "Select, Combobox, Checkbox, Switch, ToggleButton, Tabs, RadioGroup, DropdownMenu, Dialog, and AppShell navigation retain the roles and keyboard models appropriate to each interaction rather than sharing one generic choice control." ]
            DocumentationSection.create "focus" "Focus and active options" [ prose "Select and Combobox keep DOM focus on the combobox while aria-activedescendant identifies the visually active option. Select typeahead buffers rapid characters for prefix matching and cycles options when the same character is repeated." ]
            DocumentationSection.create "labels" "Required labels" [ prose "Accessible labels are required where visible content cannot provide them. Compact layouts use typed visually hidden labels rather than omitting the accessible name." ]
            DocumentationSection.create "protected-attributes" "Protected behavior" [ prose "Component-owned structure, form attributes, ARIA relationships, Datastar bindings, and base classes cannot be replaced through generic customization APIs. Because the source is local, deliberate structural changes remain visible and reviewable in the consumer repository. Interactive components support pointer and keyboard operation, visible focus, disabled and pending states, multiple instances, and representative morphs." ] ]

    let themingPage =
        DocumentationPage.create themingRegistration.id themingRegistration.title |> DocumentationPage.withDescription "Apply semantic color, radius, density, and shell geometry consistently across a Components subtree or AppShell." |> DocumentationPage.withSections [
            DocumentationSection.create "api-reference" "API reference" [
                h2 { _class "text-xl font-semibold text-[var(--fve-text)]"; "API reference" }
                FSharpApiReference.forCategory "theming" typeof<ButtonConfig> |> FSharpApiReference.render ]
            DocumentationSection.create "theme" "Apply a theme" [ prose "Components consume semantic variables for page, surface, text, border and navigation. Components such as Button select their own Color and Variant unions: Primary or Secondary chooses a palette, while Solid, Soft, Outline or Ghost chooses its appearance. Color does not change validation, announcements or action behavior."; code "fsharp" themeExample ]
            DocumentationSection.create "modes" "Light and dark modes" [ prose "Built-in sky, emerald, amber, cyan, and neutral themes coordinate default, selected, hover, focus, and navigation colors in light and dark modes. Radius applies across controls and navigation. Control size is independent of navigation and shell density: use ComponentsTheme.withControlSize for an application or ControlSize.className for a region. Small, Medium and Large have 32, 40 and 48px baselines; Small uses compact 14px/20px text, and Medium is the 16px/24px default. Button, Input and Select also support explicit withSize overrides." ]
            DocumentationSection.create "shell" "Shell policy" [ prose "SideNav width, mobile breakpoint, and viewport or embedded container boundary are typed application choices. PageTopBar and SideNavHeader share a semantic minimum-height token. Compact density retains the 48px shell bar and compact navigation independently of control size. Select Small explicitly for 32px controls, or retain Medium for aligned 40px actions and fields." ]
            DocumentationSection.create "brand" "Product branding" [ prose "Override documented semantic variables in an application theme when product branding requires it. Primary follows the current brand’s readable text/soft colors; Secondary has its own violet palette and independent tokens. Button also accepts Custom of ColorPalette. Supply explicit light and dark colors, including on-solid text and interactive states; verify contrast for every supported appearance. The theme establishes color-scheme so light-dark() responds to the selected mode rather than only the operating-system preference." ] ]

    let tailwindPage =
        DocumentationPage.create tailwindRegistration.id tailwindRegistration.title |> DocumentationPage.withDescription "Compile presentation, semantic tokens, and structural behavior directly from consumer-owned F# source." |> DocumentationPage.withSections [
            DocumentationSection.create "source" "Direct source detection" [ code "css" tailwindExample; prose "Tailwind reads the copied source directly. Component renderers use complete static utility strings, including theme-token and container-query utilities, so there is no generated safelist, copied CSS asset, or global NuGet-cache path." ]
            DocumentationSection.create "stylesheet" "One host-owned stylesheet" [ prose "fve copies no CSS. Import Tailwind, scan the copied F# source, and compile one application stylesheet. Commit the owned source and use fve diff to compare it with the selected registry." ] ]

    let customizationPage =
        DocumentationPage.create customizationRegistration.id customizationRegistration.title |> DocumentationPage.withDescription "Customize local source deliberately and override the supported semantic tokens without replacing hidden package behavior." |> DocumentationPage.withSections [
            DocumentationSection.create "source" "Edit owned source" [ prose "A copied component is ordinary project source. Change its complete utility strings, API, or markup in the consumer repository and review that change like application code. Re-running fve add is idempotent and does not silently replace local edits."; code "shell" "dotnet fve diff --config src/Acme.Components/fve.json" ]
            DocumentationSection.create "tokens" "Supported customization variables" [ prose "These are the complete supported --fve-* customization variables. Background is the base application or document canvas; Surface is for bounded or floating containers; Surface subtle, hover, and active are low-emphasis and interaction roles rather than lighter/darker palette steps. Replace the retired --fve-page token with --fve-background. The values shown are the default light surface, sky theme, comfortable density, medium controls, and table fallbacks."; code "css" supportedVariableDefaults ]
            DocumentationSection.create "palettes" "Independent color palettes" [ prose "These optional tokens independently override Button palettes. Omitted values use built-in accessible pairs: brand Primary, violet Secondary, green Success, amber Warning, red Error, blue Info and gray Neutral. Use light-dark(light, dark) for mode-dependent overrides; solid, hover and active share on-solid, while soft treatments use text. Check foreground/background and focus contrast when overriding a palette. Keep legacy brand and field-state variables above for other controls."; code "text" paletteCustomizationVariables ]
            DocumentationSection.create "renderer-owned" "Renderer-owned variables" [ prose "The following variables carry per-render values or internal surface state. Their names are observable CSS implementation details, not customization tokens."; code "text" rendererOwnedVariables ]
            DocumentationSection.create "application-inputs" "Application inputs" [ prose "Applications provide destination resolvers, form-value encoders, trusted Datastar expressions, custom cells, dialog bodies, toolbars, actions, and page content. Submitted values still require server validation." ]
            DocumentationSection.create "native-controls" "Native controls" [ prose "Render browser-native controls directly with the FSharp.ViewEngine DSL when native presentation is intentional. There is no parallel NativeSelect API or separate component markup language." ] ]

    let versioningPage =
        DocumentationPage.create versioningRegistration.id versioningRegistration.title |> DocumentationPage.withDescription "Pin a registry version, compare it with owned source, and choose every replacement explicitly." |> DocumentationPage.withSections [
            DocumentationSection.create "pin" "Pin the registry" [ prose "The repository-local tool manifest pins FSharp.ViewEngine.Cli. Each installed file records its registry version and checksum in fve.json."; code "shell" "dotnet tool update FSharp.ViewEngine.Cli\ndotnet fve diff --config src/Acme.Components/fve.json" ]
            DocumentationSection.create "safety" "No silent source upgrades" [ prose "fve diff reports unchanged, modified, missing, and unavailable files against the selected tool registry. fve add preserves installed edits; --overwrite is an explicit replacement decision. The first release does not attempt a three-way merge." ]
            DocumentationSection.create "migration" "Migrate from the compiled package" [ prose "Create the owned Components project, add the components your application uses, replace the FSharp.ViewEngine.Components package reference with a project reference, update namespace opens if you selected a custom namespace, and point Tailwind at the copied F# source. Keep FSharp.ViewEngine as the conventional Core package dependency." ] ]

    let private pages =
        [ installationRegistration.path, installationPage
          buttonRegistration.path, buttonPage
          buttonGroupRegistration.path, buttonGroupPage
          badgeRegistration.path, badgePage
          kbdRegistration.path, kbdPage
          separatorRegistration.path, separatorPage
          skeletonRegistration.path, skeletonPage
          itemRegistration.path, itemPage
          loadingIndicatorRegistration.path, loadingIndicatorPage
          progressRegistration.path, progressPage
          emptyStateRegistration.path, emptyStatePage
          tableRegistration.path, tablePage
          descriptionListRegistration.path, descriptionListPage
          metricRegistration.path, metricPage
          paginationRegistration.path, paginationPage
          avatarRegistration.path, avatarPage
          copyRevealRegistration.path, copyRevealPage
          inputRegistration.path, inputPage
          fieldRegistration.path, fieldPage
          inputGroupRegistration.path, inputGroupPage
          fileSelectionRegistration.path, fileSelectionPage
          tagInputRegistration.path, tagInputPage
          formLayoutsRegistration.path, formLayoutsPage
          textareaRegistration.path, textareaPage
          errorSummaryRegistration.path, errorSummaryPage
          noticeRegistration.path, noticePage
          notificationRegistration.path, notificationPage
          selectRegistration.path, selectPage
          datePickerRegistration.path, datePickerPage
          checkboxRegistration.path, checkboxPage
          switchRegistration.path, switchPage
          toggleButtonRegistration.path, toggleButtonPage
          toggleGroupRegistration.path, toggleGroupPage
          breadcrumbsRegistration.path, breadcrumbsPage
          sideNavRegistration.path, sideNavPage
          tabsRegistration.path, tabsPage
          radioGroupRegistration.path, radioGroupPage
          choiceCardsRegistration.path, choiceCardsPage
          dropdownMenuRegistration.path, dropdownMenuPage
          commandRegistration.path, commandPage
          dialogRegistration.path, dialogPage
          drawerRegistration.path, drawerPage
          floatingPanelRegistration.path, floatingPanelPage
          tooltipRegistration.path, tooltipPage
          popoverRegistration.path, popoverPage
          pageTopBarRegistration.path, pageTopBarPage
          pageHeaderRegistration.path, pageHeaderPage
          sectionRegistration.path, sectionPage
          browserRegistration.path, browserPage
          resizableRegistration.path, resizablePage
          phoneRegistration.path, phonePage
          pageRegistration.path, pagePage
          collectionRegistration.path, collectionPageDocumentation
          detailRegistration.path, detailPageDocumentation
          appShellRegistration.path, appShellPage
          accountManagementRegistration.path, accountManagementPage
          bottomNavigationRegistration.path, bottomNavigationPage
          messageRegistration.path, messagePage
          uploadRegistration.path, uploadPage
          stepsRegistration.path, stepsPage
          firstStepsRegistration.path, firstStepsPage
          dayCalendarRegistration.path, dayCalendarPage
          weekCalendarRegistration.path, weekCalendarPage
          monthCalendarRegistration.path, monthCalendarPage
          yearCalendarRegistration.path, yearCalendarPage
          mediaLibraryRegistration.path, mediaLibraryPage
          interactionRegistration.path, interactionPage
          accessibilityRegistration.path, accessibilityPage
          themingRegistration.path, themingPage
          tailwindRegistration.path, tailwindPage
          customizationRegistration.path, customizationPage
          versioningRegistration.path, versioningPage ]
        @ [ for page in PageExamples.pages -> PageExamples.url page, pageExamplePageFor page PageExamples.defaultQuery ]
        |> Map.ofList

    let tryPage path = Map.tryFind path pages
