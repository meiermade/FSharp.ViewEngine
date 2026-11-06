namespace Docs.Pages

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar

module Components =
    type ChoiceStatus =
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
        { id: int
          name: string
          accountType: string
          commodity: string
          balance: decimal
          totalBalance: decimal }

    type AccountWorkspace =
        { workspaceName: string
          currency: string
          emailUpdates: bool
          draftName: string
          draftType: string
          feedback: string
          searchQuery: string
          filterType: string
          sortColumn: string
          sortDirection: string
          page: int
          visibleColumns: Set<string> }

    let defaultAccountWorkspace =
        { workspaceName = "Meier Made"
          currency = "USD"
          emailUpdates = true
          draftName = ""
          draftType = "Asset"
          feedback = ""
          searchQuery = ""
          filterType = "all"
          sortColumn = "name"
          sortDirection = "asc"
          page = 1
          visibleColumns = set [ "type"; "commodity"; "balance"; "total" ] }

    type HierarchyItem =
        { key: string
          ancestors: string list
          level: int
          name: string
          kind: string
          count: int
          hasChildren: bool }

    type TransactionStatus =
        | Verified
        | Unverified
        | Committed

    type TransactionRow =
        { id: int
          date: string
          description: string
          status: TransactionStatus
          accounts: string
          amount: decimal }

    let private statusValue =
        function
        | Active -> "active"
        | Pending -> "pending"
        | Suspended -> "suspended"
        | Scheduled -> "scheduled"

    let private destinationUrl =
        function
        | Accounts -> "/components/page-examples/account-management?destination=ledger-accounts"
        | AccountsPage page -> $"/components/page-examples/account-management?destination=ledger-accounts&page={page}"
        | Account id -> $"/components/page-examples/account-management?destination=ledger-account-{id}"
        | Settings -> "/components/page-examples/account-management?destination=ledger-settings"
        | DropdownMenuGuide -> "/components/dropdown-menu#components-dropdown-menu"

    let private shellDestinationKey =
        function
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

    let tryShellDestination =
        function
        | "ledger-home" -> Some LedgerHome
        | "ledger-accounts" -> Some LedgerAccounts
        | value when value.StartsWith("ledger-account-", StringComparison.Ordinal) ->
            match Int32.TryParse(value["ledger-account-".Length ..]) with
            | true, id -> Some(LedgerAccount id)
            | false, _ -> None
        | value when value.StartsWith("ledger-transaction-", StringComparison.Ordinal) ->
            match Int32.TryParse(value["ledger-transaction-".Length ..]) with
            | true, id when List.contains id [ 201; 202; 203; 204 ] -> Some(LedgerTransaction id)
            | _ -> None
        | "ledger-create-account" -> Some LedgerCreateAccount
        | "ledger-reports" -> Some LedgerReports
        | "ledger-settings" -> Some LedgerSettings
        | "treasury-home" -> Some TreasuryHome
        | value when value.StartsWith("treasury-period-", StringComparison.Ordinal) ->
            Some(TreasuryPeriod value["treasury-period-".Length ..])
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
          _dataOn (
              "click",
              "const link = evt.target.closest?.('a[href]'); if (link && !link.hash && !link.hasAttribute('download') && !link.hasAttribute('data-fve-app-mode-launch') && !link.hasAttribute('data-review-expand') && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) evt.preventDefault()"
          ) ]

    let private moreActionsIcon =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5" aria-hidden="true"><path d="M3.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM8.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0ZM13.75 10a1.25 1.25 0 1 1 2.5 0 1.25 1.25 0 0 1-2.5 0Z"/></svg>"""

    type RowActionAccount = { id: int; name: string; canDelete: bool }

    let rowActionAccounts =
        [ { id = 101; name = "Operating checking"; canDelete = false }
          { id = 104; name = "Savings"; canDelete = false }
          { id = 105; name = "Unassigned expense"; canDelete = true } ]

    let rowActionMenu prefix (account: RowActionAccount) =
        let href = $"/examples/application/accounts/{account.id}"
        fveDropdownMenu {
            _id ($"{prefix}-{account.id}")
            _class "ms-auto shrink-0"
            _ariaLabel ("Actions for " + account.name)
            fveDropdownMenuTrigger {
                fveButton {
                    _variant ButtonVariant.Ghost
                    _size Size.Small
                    _iconOnly true
                    _class "aria-expanded:bg-[var(--fve-surface-active)]"
                    moreActionsIcon
                }
            }
            fveDropdownMenuItem { _href href; "Open account" }
            fveDropdownMenuItem { _href (href + "/edit"); "Edit account" }
            fveDropdownMenuSeparator { () }
            fveDropdownMenuItem {
                _href (href + "/delete")
                _color Color.Error
                _disabled (not account.canDelete)
                if not account.canDelete then _description "Has a balance or recorded transactions"
                "Delete account"
            }
        }

    let rowActionButtons =
        div {
            _class "mx-auto grid w-full max-w-lg gap-1"
            for index, account in rowActionAccounts |> List.indexed do
                div {
                    _class ("flex min-w-0 flex-wrap items-center justify-between gap-3 rounded px-3 py-2 " +
                        (if index = 1 then "bg-[var(--fve-brand-subtle)]" else "hover:bg-[var(--fve-surface-hover)]"))
                    span { _class "min-w-0 flex-[1_0_min(10rem,100%)] break-words text-sm"; account.name }
                    rowActionMenu "button-row-actions" account
                }
            div {
                _class "flex min-w-0 flex-wrap items-center justify-between gap-3 px-3 py-2 text-sm text-[var(--fve-muted-text)]"
                span { _class "min-w-0 flex-[1_0_min(10rem,100%)] break-words"; "Actions unavailable" }
                fveButton {
                    _variant ButtonVariant.Ghost; _size Size.Small; _iconOnly true; _class "ms-auto"
                    _ariaLabel "Actions unavailable"; _disabled true; moreActionsIcon
                }
            }
        }

    let rowActionMenuExample =
        div {
            _class "flex w-full min-w-0 max-w-sm flex-wrap items-center justify-between gap-3 rounded bg-[var(--fve-surface-hover)] px-2 py-2 text-sm"
            span { _class "min-w-0 flex-[1_0_min(10rem,100%)] break-words"; rowActionAccounts[2].name }
            rowActionMenu "menu-row-actions" rowActionAccounts[2]
        }

    let rowActionsTable =
        fveTable {
            _ariaLabel "Accounts with row actions"
            fveTableHeader {
                fveTableRow { fveTableHead { "Account" }; fveTableHead { _class "text-right"; "Actions" } }
            }
            fveTableBody {
                for account in rowActionAccounts do
                    fveTableRow {
                        fveTableHead { _scope "row"; _mobile Mobile.Primary; account.name }
                        fveTableCell {
                            _label "Actions"; _class "fve-table-actions text-right"
                            rowActionMenu "table-row-actions" account
                        }
                    }
            }
        }

    let private shellFixtureNavigationAttributes =
        let link = shellDestinationLink

        [ _dataOn (
              "click",
              $"if ({link} && !{link}.hasAttribute('data-fve-app-mode-launch') && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) {{ evt.preventDefault(); window.history.pushState(null, '', {link}.getAttribute('href')); @get({link}.getAttribute('href').replace('/components/page-examples/account-management?', '/components/page-examples/account-management/fixture?')) }}"
          ) ]

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.Components.fs")

    let private themedPreview (content: HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"

            for attribute in galleryDestinationAttributes do
                attribute

            content
        }

    let private themedSurface (content: HtmlElement) =
        div {
            for attribute in ComponentsTheme.attributes ComponentsTheme.sky do
                attribute

            div {
                _class "bg-[var(--fve-background)] p-[12px] sm:p-6 text-[var(--fve-text)]"
                content
            }
        }
        |> themedPreview

    let private fullBleedThemedSurface (content: HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"

            for attribute in galleryDestinationAttributes do
                attribute

            div {
                for attribute in
                    ComponentsTheme.attributes (ComponentsTheme.sky |> ComponentsTheme.withDensity Density.Compact) do
                    attribute

                content
            }
        }

    let pendingSyncButton =
        fveButton {
            _color Color.Primary
            _variant ButtonVariant.Solid
            _dataIndicator "_sync_pending"
            _dataOn ("click", "@get('/components/button/settled', {filterSignals: {exclude: /.*/}, retry: 'never'})")
            "Sync"
        }

    let disabledDeleteButton =
        fveButton {
            _color Color.Error
            _variant ButtonVariant.Solid
            _disabled true
            "Delete"
        }

    let private plusIcon =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path d="M10 4.25a.75.75 0 0 1 .75.75v4.25H15a.75.75 0 0 1 0 1.5h-4.25V15a.75.75 0 0 1-1.5 0v-4.25H5a.75.75 0 0 1 0-1.5h4.25V5a.75.75 0 0 1 .75-.75Z"/></svg>"""

    let private refreshIcon =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M15.312 4.683A7.25 7.25 0 1 0 17.25 10a.75.75 0 0 0-1.5 0 5.75 5.75 0 1 1-1.604-3.982H12.5a.75.75 0 0 0 0 1.5h3.5a.75.75 0 0 0 .75-.75v-3.5a.75.75 0 0 0-1.5 0v1.415Z" clip-rule="evenodd"/></svg>"""

    let private removeIcon =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M8.75 3.5a1.25 1.25 0 0 1 2.5 0V4h3a.75.75 0 0 1 0 1.5h-.44l-.55 9.08A2.25 2.25 0 0 1 11.02 16.7H8.98a2.25 2.25 0 0 1-2.24-2.12L6.19 5.5h-.44a.75.75 0 0 1 0-1.5h3v-.5Zm-1.06 2 .54 8.99a.75.75 0 0 0 .75.71h2.04a.75.75 0 0 0 .75-.71l.54-8.99H7.69Z" clip-rule="evenodd"/></svg>"""

    let createIconOnlyButton =
        fveButton {
            _color Color.Primary
            _variant ButtonVariant.Solid
            _ariaLabel "Create"
            _iconOnly true
            plusIcon
        }

    let refreshingIconOnlyButton =
        fveButton {
            _variant ButtonVariant.Ghost
            _ariaLabel "Refresh"
            _iconOnly true
            _dataIndicator "_refresh_pending"
            _dataOn ("click", "@get('/components/button/settled', {filterSignals: {exclude: /.*/}, retry: 'never'})")
            refreshIcon
        }

    let disabledRemoveIconOnlyButton =
        fveButton {
            _color Color.Error
            _variant ButtonVariant.Solid
            _ariaLabel "Remove"
            _iconOnly true
            _disabled true
            removeIcon
        }

    let private backIcon =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M11.53 4.47a.75.75 0 0 1 0 1.06L7.06 10l4.47 4.47a.75.75 0 1 1-1.06 1.06l-5-5a.75.75 0 0 1 0-1.06l5-5a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>"""

    let defaultButtonGroup =
        fveButtonGroup {
            _ariaLabel "Message actions"
            fveButton { "Archive" }
            fveButton { "Report" }
        }

    let buttonGroupExample =
        div {
            _class "flex min-w-0 max-w-full flex-wrap items-center gap-2"

            fveButtonGroup {
                _ariaLabel "Back"

                fveButton {
                    _ariaLabel "Back"
                    _iconOnly true

                    span {
                        _ariaHidden true
                        backIcon
                    }
                }
            }

            fveButtonGroup {
                _ariaLabel "Message actions"
                fveButton { "Archive" }
                fveButton { "Report" }
            }

            fveButtonGroup {
                _ariaLabel "Scheduling actions"
                fveButton { "Snooze" }

                fveDropdownMenu {
                    _id "button-group-snooze-menu"
                    _ariaLabel "More scheduling actions"

                    fveDropdownMenuTrigger {
                        fveButton {
                            _iconOnly true

                            _variant ButtonVariant.Outline
                            moreActionsIcon
                        }
                    }

                    fveDropdownMenuItem {
                        _dataOn ("click", "$snoozeUntil = 'tomorrow'")
                        "Snooze until tomorrow"
                    }

                    fveDropdownMenuItem {
                        _dataOn ("click", "$snoozeUntil = 'next-week'")
                        "Snooze until next week"
                    }
                }
            }
        }

    let linkedButtonGroup =
        fveButtonGroup {
            _ariaLabel "Component destinations"

            fveLink {
                _variant LinkVariant.Outline
                _color Color.Neutral
                _href "/components/button"
                "Button"
            }

            fveLink {
                _variant LinkVariant.Outline
                _color Color.Neutral
                _href "/components/dropdown-menu"
                "Dropdown menu"
            }
        }

    let selectButtonGroup =
        fveButtonGroup {
            _ariaLabel "Export options"
            "Format"

            fveField {
                fveLabel {
                    _class "sr-only"
                    _for "groupExportFormat"
                    "Export format"
                }

                fveSelect {



                    _value "csv"
                    _nativeFallback true
                    _name "groupExportFormat"

                    fveSelectContent {
                        for choice in ([ ("csv", "CSV", false); ("json", "JSON", false); ("pdf", "PDF", false) ]) do
                            fveSelectItem {
                                let value, label, disabled = choice
                                _value value
                                _label label
                                _disabled disabled
                            }
                    }

                    _id "groupExportFormat"
                }
            }

            fveButton { "Export" }
        }

    let customButtonGroup =
        fveButtonGroup {
            _ariaLabel "Custom control actions"

            div {
                div {
                    button {
                        _type "button"
                        _data ("fve-group-control", "outline")

                        _class
                            "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 text-[length:var(--fve-control-font-size)] text-[var(--fve-text)] focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                        "Native action"
                    }
                }
            }

            fvePopover {
                _id "button-group-details"
                fvePopoverTrigger { fveButton { "Details" } }

                fvePopoverContent {
                    _ariaLabel "Action details"
                    fveButton { "Independent action" }
                }
            }
        }

    let verticalButtonGroup =
        fveButtonGroup {
            _orientation Orientation.Vertical
            _ariaLabel "Record view"
            fveButton { "Summary" }
            fveButton { "Activity" }
            fveButton { "Files" }
        }

    let badgeExample = fveBadge { "Internal" }

    let badgeColorExamples =
        div {
            _class "flex flex-wrap items-center gap-3"

            for label, color in
                [ "Primary", Color.Primary
                  "Secondary", Color.Secondary
                  "Success", Color.Success
                  "Warning", Color.Warning
                  "Error", Color.Error
                  "Info", Color.Info
                  "Neutral", Color.Neutral ] do
                fveBadge {
                    _color color
                    label
                }
        }

    let badgeVariantExamples =
        div {
            _class "flex flex-wrap items-center gap-3"

            for label, variant in
                [ "Solid", BadgeVariant.Solid
                  "Soft", BadgeVariant.Soft
                  "Outline", BadgeVariant.Outline
                  "Ghost", BadgeVariant.Ghost ] do
                fveBadge {
                    _color Color.Success
                    _variant variant
                    label
                }
        }

    let reviewStatus =
        fveBadge {
            _color Color.Warning
            "Needs review"
        }

    let keyboardKey = fveKbd { "Esc" }

    let keyboardShortcut =
        fveKbdShortcut {
            _ariaLabel "Open search"

            for key in [ "⌘"; "K" ] do
                fveKbd { key }
        }

    let defaultSeparator =
        div {
            _class "w-full"
            fveSeparator { () }
        }

    let separatorExamples =
        div {
            _class "grid gap-5"

            div {
                p {
                    _class "mb-3 text-sm font-medium"
                    "Profile details"
                }

                fveSeparator { _role "separator" }

                p {
                    _class "mt-3 text-sm text-[var(--fve-muted-text)]"
                    "Contact information and preferences"
                }
            }

            div {
                _class "flex h-8 items-center gap-4"
                span { "Profile" }
                fveSeparator { _orientation Orientation.Vertical }
                span { "Security" }
            }
        }

    let defaultSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading content"
            fveSkeleton { _class "h-4 w-full" }
        }

    let textSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading description"

            div {
                _class "grid min-w-0 gap-2"
                fveSkeleton { _class "h-4 w-full" }
                fveSkeleton { _class "h-4 w-full" }
                fveSkeleton { _class "h-4 w-3/4" }
            }
        }

    let avatarSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading profile"

            div {
                _class "flex min-w-0 items-center gap-4"
                fveSkeleton { _class "size-12 shrink-0 rounded-full" }

                div {
                    _class "grid min-w-0 flex-1 gap-2"
                    fveSkeleton { _class "h-4 w-full" }
                    fveSkeleton { _class "h-4 w-3/4" }
                }
            }
        }

    let cardSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading card"

            div {
                _class "grid min-w-0 gap-4"
                fveSkeleton { _class "h-36 w-full rounded-xl" }

                div {
                    _class "grid min-w-0 gap-2"
                    fveSkeleton { _class "h-4 w-2/3" }
                    fveSkeleton { _class "h-4 w-1/2" }
                }
            }
        }

    let dimensionSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading summary"

            fveSkeleton {
                _class "rounded-full"
                Html._width "75%"
                Html._height "1rem"
            }
        }

    let defaultLoadingIndicator = fveLoadingIndicator { _ariaLabel "Loading" }

    let visibleLoadingIndicator =
        fveLoadingIndicator {
            _ariaLabel "Refreshing items"
            _size Size.Large
            "Refreshing items"
        }

    let defaultEmptyState =
        fveEmptyState {
            _label "No items yet"
            _description "Add your first item to get started."
        }

    let emptyStateExample =
        fveEmptyState {
            _label "No matching items"
            _description "Clear your filters to see all items."
            fveEmptyStateIcon { plusIcon }

            fveEmptyStateActions {
                fveLink {
                    _variant LinkVariant.Outline
                    _color Color.Neutral
                    _href "/components/empty-state"
                    "Clear filters"
                }
            }
        }

    let private rows =
        [ { id = 101
            name = "Assets"
            accountType = "Asset"
            commodity = "USD"
            balance = 0M
            totalBalance = 184230.19M }
          { id = 102
            name = "Liabilities"
            accountType = "Liability"
            commodity = "USD"
            balance = 0M
            totalBalance = -42118.44M }
          { id = 103
            name = "Equity"
            accountType = "Equity"
            commodity = "USD"
            balance = -93136.75M
            totalBalance = -101336.75M }
          { id = 104
            name = "Revenue"
            accountType = "Revenue"
            commodity = "USD"
            balance = -138425M
            totalBalance = -138425M }
          { id = 105
            name = "Expenses"
            accountType = "Expense"
            commodity = "USD"
            balance = 97650M
            totalBalance = 97650M }
          { id = 106
            name = "Placeholder"
            accountType = "Placeholder"
            commodity = "USD"
            balance = 0M
            totalBalance = 0M } ]

    let private operatingAccount: AccountRow =
        { id = 2048
          name = "Operating checking"
          accountType = "Asset"
          commodity = "USD"
          balance = 38442.11M
          totalBalance = 38442.11M }

    let accountNameExists name =
        operatingAccount :: rows
        |> List.exists (fun row -> String.Equals(row.name, name, StringComparison.OrdinalIgnoreCase))

    let private money amount =
        if amount < 0M then $"−${-amount:N2}" else $"${amount:N2}"

    let private recordActionFeedback kind (content: HtmlElement) =
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
        let json value =
            System.Text.Json.JsonSerializer.Serialize(value)

        let feedback = $"$_{kind}MenuFeedback"

        let copy label expression =
            let failure = json $"Could not copy {label}. Check clipboard permissions."
            let success = json $"Copied {label}."

            fveDropdownMenuItem {
                _dataOn (
                    "click",
                    $"{feedback} = ''; if (navigator.clipboard?.writeText) {{ navigator.clipboard.writeText({expression}).then(() => {{ {feedback} = {success} }}).catch(() => {{ {feedback} = {failure} }}) }} else {{ {feedback} = {failure} }}"
                )

                $"Copy {label}"
            }

        let url = resolve destination |> json

        let download =
            $"const url = URL.createObjectURL(new Blob([{json payload}], {{type: 'application/json'}})); const link = document.createElement('a'); link.href = url; link.download = '{kind}-{id}.json'; document.body.append(link); link.click(); link.remove(); URL.revokeObjectURL(url); {feedback} = 'Downloaded {kind}.'"

        [ fveDropdownMenuItem {
              _href (resolve destination)
              $"View {kind}"
          }
          copy $"{kind} ID" (json (string id))
          copy $"{kind} link" $"new URL({url}, window.location.href).href"
          fveDropdownMenuSeparator { () }
          fveDropdownMenuItem {
              _dataOn ("click", download)
              $"Download {kind}"
          } ]

    let private accountTableViewFor
        (accountRows: AccountRow list)
        destinationFor
        resolve
        (rowAttributes: AccountRow -> HtmlAttribute list)
        =
        let selection =
            RecordSelection.view
                "accounts-selection"
                "accountIds"
                []
                (accountRows |> List.map (fun row -> string row.id, row.name))

        let headings =
            [ selection.heading
              fveTableHead { "Account" }
              fveTableHead { "Type" }
              fveTableHead { "Commodity" }
              fveTableHead {
                  _class "text-right"
                  "Balance"
              }
              fveTableHead {
                  _class "text-right"
                  "Total balance (USD) · Current"
              }
              fveTableHead {
                  _class "fve-table-actions text-right"
                  _attr ("data-fve-sticky-cell", "true")

                  span {
                      _class "sr-only"
                      "Actions"
                  }
              } ]

        let body =
            [ for row in accountRows do
                  fveTableRow {
                      yield! selection.rowAttributes (string row.id)
                      yield! rowAttributes row
                      selection.cell (string row.id) row.name

                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary

                          a {
                              _href (resolve (destinationFor row.id))
                              _class "font-medium text-[var(--fve-brand-text)]"
                              row.name
                          }
                      }

                      fveTableCell {
                          _label "Type"
                          row.accountType
                      }

                      fveTableCell {
                          _label "Commodity"
                          row.commodity
                      }

                      fveTableCell {
                          _label "Balance"
                          _class "text-right"
                          money row.balance
                      }

                      fveTableCell {
                          _label "Total balance (USD) · Current"
                          _class "text-right"
                          money row.totalBalance
                      }

                      fveTableCell {
                          _mobile Mobile.Actions
                          _class "fve-table-actions text-right"
                          _attr ("data-fve-sticky-cell", "true")

                          fveDropdownMenu {
                              _id $"account-{row.id}-actions"
                              _ariaLabel $"More actions for {row.name}"

                              fveDropdownMenuTrigger {
                                  fveButton {
                                      _iconOnly true
                                      _variant ButtonVariant.Ghost

                                      moreActionsIcon
                                  }
                              }

                              yield!
                                  recordMenuItems
                                      "account"
                                      row.id
                                      (destinationFor row.id)
                                      resolve
                                      (System.Text.Json.JsonSerializer.Serialize row)
                          }
                      }
                  } ]

        RecordTable.render
            "Accounts"
            ([ _density Density.Compact; _mobileLayout MobileLayout.Records ]
             @ selection.attributes)
            headings
            body
            [ selection.status ]

    let private accountTableView destinationFor resolve rowAttributes =
        accountTableViewFor rows destinationFor resolve rowAttributes

    let accountTable =
        accountTableView LedgerAccount shellDestinationUrl (fun _ -> [])
        |> recordActionFeedback "account"

    let hierarchyItems =
        [ { key = "projects"
            ancestors = []
            level = 0
            name = "Projects"
            kind = "Folder"
            count = 24
            hasChildren = true }
          { key = "website"
            ancestors = [ "projects" ]
            level = 1
            name = "Website"
            kind = "Folder"
            count = 12
            hasChildren = true }
          { key = "notes"
            ancestors = [ "projects"; "website" ]
            level = 2
            name = "Project notes"
            kind = "Document"
            count = 1
            hasChildren = false }
          { key = "design"
            ancestors = [ "projects"; "website" ]
            level = 2
            name = "Design files"
            kind = "Folder"
            count = 11
            hasChildren = false }
          { key = "research"
            ancestors = [ "projects" ]
            level = 1
            name = "Research"
            kind = "Folder"
            count = 12
            hasChildren = false }
          { key = "shared"
            ancestors = []
            level = 0
            name = "Shared files"
            kind = "Folder"
            count = 8
            hasChildren = true }
          { key = "guide"
            ancestors = [ "shared" ]
            level = 1
            name = "Team guide"
            kind = "Document"
            count = 1
            hasChildren = false } ]

    let hierarchicalItemTable =
        fveTable {
            _ariaLabel "Item hierarchy"
            _mobileLayout MobileLayout.Records
            _dataSignals "{itemHierarchyExpanded:['projects','website','shared']}"

            fveTableHeader {
                fveTableRow {
                    fveTableHead { "Name" }
                    fveTableHead { "Type" }

                    fveTableHead {
                        _class "text-right"
                        "Items"
                    }
                }
            }

            fveTableBody {
                for row in hierarchyItems do
                    let key = System.Text.Json.JsonSerializer.Serialize row.key
                    let ancestors = System.Text.Json.JsonSerializer.Serialize row.ancestors
                    let expanded = $"$itemHierarchyExpanded.includes({key})"

                    fveTableRow {
                        _id ("hierarchy-" + row.key)
                        _dataShow $"{ancestors}.every(key => $itemHierarchyExpanded.includes(key))"

                        fveTableHead {
                            _scope "row"
                            _mobile Mobile.Primary

                            span {
                                _class "inline-flex items-center gap-2"
                                _style $"padding-inline-start:{row.level}rem"

                                if row.hasChildren then
                                    button {
                                        _type "button"

                                        _class
                                            "inline-flex size-8 items-center justify-center rounded focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                                        _dataAttr ("aria-expanded", expanded)

                                        _dataAttr (
                                            "aria-label",
                                            $"({expanded} ? 'Collapse ' : 'Expand ') + {System.Text.Json.JsonSerializer.Serialize row.name}"
                                        )

                                        _dataOn (
                                            "click",
                                            $"$itemHierarchyExpanded = {expanded} ? $itemHierarchyExpanded.filter(key => key !== {key}) : [...$itemHierarchyExpanded, {key}]"
                                        )

                                        span {
                                            _ariaHidden true
                                            _dataText $"{expanded} ? '▾' : '▸'"
                                        }
                                    }
                                else
                                    span {
                                        _ariaHidden true
                                        _class "inline-block size-8"
                                    }

                                a {
                                    _href "/components/table"
                                    _class "font-medium text-[var(--fve-brand-text)]"
                                    row.name
                                }
                            }
                        }

                        fveTableCell {
                            _label "Type"
                            row.kind
                        }

                        fveTableCell {
                            _label "Items"
                            _class "text-right"
                            string row.count
                        }
                    }
            }
        }

    let private accountFilterExpression (row: AccountRow) =
        let searchable =
            System.Text.Json.JsonSerializer.Serialize(
                $"{row.name} {row.accountType} {row.commodity}".ToLowerInvariant()
            )

        let accountType =
            System.Text.Json.JsonSerializer.Serialize(row.accountType.ToLowerInvariant())

        $"(!$collectionquery.trim() || {searchable}.includes($collectionquery.trim().toLowerCase())) && ($collectiontype == 'all' || $collectiontype == {accountType})"

    let private collectionHasMatches =
        rows
        |> List.map (fun row -> $"({accountFilterExpression row})")
        |> String.concat " || "

    let private filteredAccountTable =
        accountTableView LedgerAccount shellDestinationUrl (fun row -> [ _dataShow (accountFilterExpression row) ])
        |> recordActionFeedback "account"

    let private transactionRows =
        [ { id = 201
            date = "Jul 28"
            description = "Northwind payment"
            status = Verified
            accounts = "Accounts receivable → Operating checking"
            amount = 4800M }
          { id = 202
            date = "Jul 27"
            description = "Cloud hosting"
            status = Verified
            accounts = "Operating checking → Software expense"
            amount = 386.42M }
          { id = 203
            date = "Jul 26"
            description = "ACH withdrawal"
            status = Unverified
            accounts = "Operating checking → Placeholder"
            amount = 1240M }
          { id = 204
            date = "Jun 24"
            description = "Office supplies"
            status = Committed
            accounts = "Business card → Office expense"
            amount = 128.19M } ]

    let private transactionTableFor destinationFor resolve =
        let selection =
            RecordSelection.view
                "transactions-selection"
                "transactions-selection"
                []
                (transactionRows |> List.map (fun row -> string row.id, row.description))

        let headings =
            [ selection.heading
              fveTableHead { "Date" }
              fveTableHead { "Description" }
              fveTableHead { "Status" }
              fveTableHead { "Accounts" }
              fveTableHead {
                  _class "text-right"
                  "Amount"
              }
              fveTableHead {
                  _class "fve-table-actions text-right"
                  _attr ("data-fve-sticky-cell", "true")

                  span {
                      _class "sr-only"
                      "Actions"
                  }
              } ]

        let body =
            [ for row in transactionRows do
                  fveTableRow {
                      yield! selection.rowAttributes (string row.id)
                      selection.cell (string row.id) row.description

                      fveTableCell {
                          _label "Date"
                          row.date
                      }

                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary

                          a {
                              _href (resolve (destinationFor row.id))
                              _class "font-medium text-[var(--fve-brand-text)]"
                              row.description
                          }
                      }

                      fveTableCell {
                          _label "Status"

                          match row.status with
                          | Verified ->
                              fveBadge {
                                  _color Color.Success
                                  "Verified"
                              }
                          | Unverified ->
                              fveBadge {
                                  _color Color.Warning
                                  "Unverified"
                              }
                          | Committed ->
                              fveBadge {
                                  _color Color.Neutral
                                  "Committed"
                              }
                      }

                      fveTableCell {
                          _label "Accounts"
                          row.accounts
                      }

                      fveTableCell {
                          _label "Amount"
                          _class "text-right"
                          money row.amount
                      }

                      fveTableCell {
                          _mobile Mobile.Actions
                          _class "fve-table-actions text-right"
                          _attr ("data-fve-sticky-cell", "true")

                          let payload =
                              System.Text.Json.JsonSerializer.Serialize
                                  {| id = row.id
                                     date = row.date
                                     description = row.description
                                     status = string row.status
                                     accounts = row.accounts
                                     amount = row.amount |}

                          fveDropdownMenu {
                              _id $"transaction-{row.id}-actions"
                              _ariaLabel $"More actions for {row.description}"

                              fveDropdownMenuTrigger {
                                  fveButton {
                                      _iconOnly true
                                      _variant ButtonVariant.Ghost

                                      moreActionsIcon
                                  }
                              }

                              yield! recordMenuItems "transaction" row.id (destinationFor row.id) resolve payload
                          }
                      }
                  } ]

        RecordTable.render
            "Transactions"
            ([ _density Density.Compact; _mobileLayout MobileLayout.Records ]
             @ selection.attributes)
            headings
            body
            [ selection.status ]
        |> recordActionFeedback "transaction"

    let private transactionTable =
        transactionTableFor LedgerTransaction shellDestinationUrl

    type TeamMember =
        { id: int
          name: string
          email: string
          role: string }

    let teamMembers =
        [ { id = 1
            name = "Alex Morgan"
            email = "alex@fve.meiermade.com"
            role = "Owner" }
          { id = 2
            name = "Jamie Lee"
            email = "jamie@fve.meiermade.com"
            role = "Member" }
          { id = 3
            name = "Riley Chen"
            email = "riley@fve.meiermade.com"
            role = "Member" }
          { id = 4
            name = "Sam Rivera"
            email = "sam@fve.meiermade.com"
            role = "Guest" } ]

    let teamTableHeader =
        fveTableHeader {
            fveTableRow {
                fveTableHead { "Name" }
                fveTableHead { "Email" }
                fveTableHead { "Role" }
            }
        }

    let teamTableRows (members: TeamMember list) =
        fragment {
            for person in members do
                fveTableRow {
                    fveTableHead {
                        _scope "row"
                        _mobile Mobile.Primary
                        _class "font-medium"
                        person.name
                    }

                    fveTableCell {
                        _label "Email"
                        person.email
                    }

                    fveTableCell {
                        _label "Role"
                        person.role
                    }
                }
        }

    let simpleTeamTable =
        fveTable {
            _ariaLabel "Team members"
            teamTableHeader
            fveTableBody { teamTableRows teamMembers }
        }

    let scrollableTeamTable =
        fveTable {
            _ariaLabel "Team members in a bounded viewport"
            _class "h-28"
            _scrollRows true
            teamTableHeader
            fveTableBody { teamTableRows teamMembers }
        }

    let comfortableTeamTable =
        fveTable {
            _ariaLabel "Team members with comfortable rows"
            _density Density.Comfortable
            teamTableHeader
            fveTableBody { teamTableRows teamMembers }
        }

    let memberStatusTable =
        fveTable {
            _ariaLabel "Team member status"

            fveTableHeader {
                fveTableRow {
                    fveTableHead { "Name" }
                    fveTableHead { "Email" }
                    fveTableHead { "Status" }
                }
            }

            fveTableBody {
                for person in teamMembers do
                    fveTableRow {
                        fveTableHead {
                            _scope "row"
                            person.name
                        }

                        fveTableCell { person.email }

                        fveTableCell {
                            if person.role = "Guest" then
                                fveBadge {
                                    _color Color.Warning
                                    "Invited"
                                }
                            else
                                fveBadge {
                                    _color Color.Success
                                    "Active"
                                }
                        }
                    }
            }
        }

    let selectableTeamTable =
        let keys =
            teamMembers
            |> List.map (fun person -> string person.id)
            |> System.Text.Json.JsonSerializer.Serialize

        fveTable {
            _id "team-member-selection"
            _ariaLabel "Selectable team members"
            _dataSignals "{tableMemberSelection:[]}"
            _dataOn ("fve-selection-clear", "$tableMemberSelection = []")

            _dataEffect
                "el.dispatchEvent(new CustomEvent('fve-table-selection-change', {bubbles:true, detail:{keys:Array.from($tableMemberSelection)}}))"

            output {
                _role "status"
                _ariaLive "polite"
                _class "sr-only"
                _dataText "$tableMemberSelection.length + ' selected'"
                "0 selected"
            }

            fveTableHeader {
                fveTableRow {
                    fveTableHead {
                        input {
                            _type "checkbox"
                            _class "fve-table-checkbox"
                            _ariaLabel "Select all rows on this page"

                            _dataEffect
                                $"el.checked = $tableMemberSelection.length === {teamMembers.Length}; el.indeterminate = $tableMemberSelection.length > 0 && !el.checked"

                            _dataOn ("change", $"$tableMemberSelection = el.checked ? {keys} : []")
                        }
                    }

                    fveTableHead { "Name" }
                    fveTableHead { "Email" }
                    fveTableHead { "Role" }
                }
            }

            fveTableBody {
                for person in teamMembers do
                    let key = string person.id

                    fveTableRow {
                        _dataAttr ("data-selected", $"$tableMemberSelection.includes('{key}') ? 'true' : 'false'")

                        fveTableCell {
                            input {
                                _type "checkbox"
                                _name "memberIds"
                                _value key
                                _class "fve-table-checkbox"
                                _ariaLabel ("Select " + person.name)
                                _dataEffect $"el.checked = $tableMemberSelection.includes('{key}')"

                                _dataOn (
                                    "change",
                                    $"$tableMemberSelection = el.checked ? [...$tableMemberSelection.filter(key => key !== '{key}'), '{key}'] : $tableMemberSelection.filter(key => key !== '{key}')"
                                )
                            }
                        }

                        fveTableHead {
                            _scope "row"
                            person.name
                        }

                        fveTableCell { person.email }
                        fveTableCell { person.role }
                    }
            }
        }

    let mobileTeamTable =
        fveTable {
            _ariaLabel "Team members as mobile records"
            _mobileLayout MobileLayout.Records
            teamTableHeader
            fveTableBody { teamTableRows teamMembers }
        }

    let emptyTeamTable =
        fveTable {
            _ariaLabel "Empty team members"
            teamTableHeader
            fveTableBody { () }

            fveTableEmpty {
                fveEmptyState {
                    _label "No team members"
                    _description "Team members will appear here when added."
                }
            }
        }

    let infiniteTeamMembers =
        [ for number in 1..24 ->
              { id = number
                name = $"Team member {number}"
                email = $"member{number}@fve.meiermade.com"
                role = "Member" } ]

    let infiniteTeamRows offset =
        fragment {
            for person in infiniteTeamMembers |> List.skip offset |> List.truncate 8 do
                fveTableRow {
                    _id $"infinite-person-{person.id}"

                    fveTableHead {
                        _scope "row"
                        person.name
                    }

                    fveTableCell { person.email }
                    fveTableCell { person.role }
                }

            let next = min (offset + 8) infiniteTeamMembers.Length

            if next < infiniteTeamMembers.Length then
                fveTableRow {
                    _id $"people-more-{next}"

                    _dataOnIntersect (
                        [ "once" ],
                        $"@get('/components/table/more?offset={next}', {{filterSignals: {{exclude: /.*/}}, retry: 'never'}})"
                    )

                    fveTableCell {
                        _colspan 3

                        span {
                            _role "status"
                            "Loading more members…"
                        }
                    }
                }
        }

    let infiniteTeamTable =
        fveTable {
            _id "infinite-team-table"
            _ariaLabel "More team members"
            _class "h-64"
            _scrollRows true
            teamTableHeader
            fveTableBody { infiniteTeamRows 0 }
        }

    let tableMoreHandler: HttpHandler =
        fun _ context ->
            task {
                match Int32.TryParse(context.Request.Query["offset"].ToString()) with
                | true, offset when offset > 0 && offset < infiniteTeamMembers.Length && offset % 8 = 0 ->
                    do! System.Threading.Tasks.Task.Delay(300, context.RequestAborted)
                    let datastar = context.RequestServices.GetRequiredService<IDatastarService>()

                    do!
                        datastar.PatchElementsAsync(
                            infiniteTeamRows offset |> Render.toString,
                            PatchElementsOptions(
                                Selector = $"#people-more-{offset}",
                                PatchMode = ElementPatchMode.Replace
                            ),
                            context.RequestAborted
                        )

                    return Some context
                | _ ->
                    context.Response.StatusCode <- 400
                    return Some context
            }

    let tableMoreRoutes: HttpHandler =
        Giraffe.Core.GET >=> route "/components/table/more" >=> tableMoreHandler

    type TeamMemberSort =
        | NameAscending
        | NameDescending
        | RoleAscending
        | RoleDescending

    let teamMemberSortFromQuery sort direction =
        match sort, direction with
        | "name", "desc" -> NameDescending
        | "role", "asc" -> RoleAscending
        | "role", "desc" -> RoleDescending
        | _ -> NameAscending

    let private teamMemberSortUrl sort direction =
        $"/components/table?sort={sort}&direction={direction}"

    let private sortFor column current =
        match column, current with
        | "name", NameAscending
        | "role", RoleAscending -> "ascending", "desc"
        | "name", NameDescending
        | "role", RoleDescending -> "descending", "asc"
        | _ -> "unsorted", "asc"

    let private teamSortHeading (column: string) (heading: string) current =
        let state, next = sortFor column current

        fveTableHead {
            if state <> "unsorted" then
                _ariaSort state

            a {
                _href (teamMemberSortUrl column next)
                _class "fve-table-sort-control"
                _ariaLabel $"Sort by {heading}; currently {state}"

                _dataOn (
                    "click",
                    "if (evt.button === 0 && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey) { evt.preventDefault(); @get(el.getAttribute('href').replace('/components/table?', '/components/table/sort?'), { filterSignals: { exclude: /.*/ }, retry: 'never' }) }"
                )

                heading

                span {
                    _ariaHidden true

                    if state = "ascending" then "↑"
                    elif state = "descending" then "↓"
                    else "↕"
                }
            }
        }

    let sortableTeamTable current =
        let members =
            match current with
            | NameAscending -> teamMembers |> List.sortBy _.name
            | NameDescending -> teamMembers |> List.sortByDescending _.name
            | RoleAscending -> teamMembers |> List.sortBy _.role
            | RoleDescending -> teamMembers |> List.sortByDescending _.role

        fveTable {
            _ariaLabel "Sortable team members"
            _mobileLayout MobileLayout.Records

            fveTableHeader {
                fveTableRow {
                    teamSortHeading "name" "Name" current
                    fveTableHead { "Email" }
                    teamSortHeading "role" "Role" current
                }
            }

            fveTableBody { teamTableRows members }
        }

    let sortableTeamTablePreview current =
        div {
            _id "components-table-sorting-preview"
            sortableTeamTable current
        }

    let defaultDescriptionList =
        fveDescriptionList {
            fveDescriptionListItem {
                _label "Name"
                "Project notes"
            }

            fveDescriptionListItem {
                _label "Format"
                "Document"
            }
        }

    let itemDetails =
        fveDescriptionList {
            _columns DescriptionListColumns.Three

            fveDescriptionListItem {
                _label "Name"
                "Project notes"
            }

            fveDescriptionListItem {
                _label "Format"
                "Document"
            }

            fveDescriptionListItem {
                _label "Folder"
                "Shared files"
            }

            fveDescriptionListItem {
                _label "Owner"

                a {
                    _href "/examples/application/profile"

                    _class
                        "font-medium text-[var(--fve-brand-text)] hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                    "Andrew Meier"
                }
            }

            fveDescriptionListItem {
                _label "Status"

                fveBadge {
                    _color Color.Success
                    "Active"
                }
            }

            fveDescriptionListItem {
                _label "Size"
                "2.4 MB"
            }
        }

    let itemOverview =
        fveDescriptionList {
            _columns DescriptionListColumns.Four

            fveDescriptionListItem {
                _label "Status"

                fveBadge {
                    _color Color.Success
                    "Active"
                }
            }

            fveDescriptionListItem {
                _label "Review"

                fveBadge {
                    _color Color.Success
                    "Up to date"
                }
            }

            fveDescriptionListItem {
                _label "Reference"
                "1040"
            }

            fveDescriptionListItem {
                _label "Format"
                "Document"
            }

            fveDescriptionListItem {
                _label "Name"
                "Project notes"
            }

            fveDescriptionListItem {
                _label "Folder"
                "Shared files"
            }

            fveDescriptionListItem {
                _label "Updated"
                "August 31, 2026"
            }

            fveDescriptionListItem {
                _label "Size"
                "2.4 MB"
            }
        }

    let itemDetailsWithDescriptions =
        fveDescriptionList {
            _columns DescriptionListColumns.Two

            fveDescriptionListItem {
                _label "Files"
                "24"
                fveDescriptionListDescription { "Includes files added today." }
            }

            fveDescriptionListItem {
                _label "Reference"
                "project-notes-2026-08-31-team-review-1040"
                fveDescriptionListDescription { "Shared during the August project review." }
            }
        }

    let defaultMetric =
        fveMetric {
            _label "Total items"
            "1,280"
        }

    let totalItemsMetric =
        fveMetric {
            _label "Total items"
            _trend "Up 8% from last month"
            _description "Across all projects"
            "1,280"

            fveMetricStatus {
                fveBadge {
                    _color Color.Success
                    "Current"
                }
            }
        }

    let pendingItemsMetric =
        fveMetric {
            _label "Pending items"
            _description "Require review before publishing"
            "14"

            fveMetricStatus {
                fveBadge {
                    _color Color.Warning
                    "Needs review"
                }
            }
        }

    let multipleMetrics =
        div {
            _role "group"
            _ariaLabel "Monthly activity metrics"

            _class
                "grid w-full min-w-0 max-w-4xl grid-cols-[repeat(auto-fit,minmax(min(100%,12rem),1fr))] justify-items-center gap-8"

            for label, value in [ "Views", "48,000"; "Downloads", "3,120"; "Items", "1,280" ] do
                fveMetric {
                    _label label
                    _description "September 2026"
                    value
                }
        }

    let defaultPagination =
        fvePagination {
            _ariaLabel "Results pages"

            fvePaginationContent {
                fvePaginationItem {
                    _page 1
                    _ariaCurrent "page"
                }

                fvePaginationItem {
                    _page 2
                    _href "/components/pagination/page?page=2"
                }
            }
        }

    type PaginationDestination = PaginationPage of int

    let paginationDestinationUrl (PaginationPage page) =
        $"/components/pagination/page?page={page}"

    let paginationConfig (variant: PaginationVariant) requestedPage =
        let currentPage = Math.Clamp(requestedPage, 1, 8)

        let pageItem page =
            fvePaginationItem {
                _page page

                if page = currentPage then
                    _ariaCurrent "page"
                else
                    _href (paginationDestinationUrl (PaginationPage page))
            }

        let middlePages = [ max 2 (currentPage - 1) .. min 7 (currentPage + 1) ]
        let firstResult = (currentPage - 1) * 25 + 1
        let lastResult = min 184 (currentPage * 25)

        fvePagination {
            _ariaLabel "Results pages"
            _variant variant

            fvePaginationPrevious {
                if currentPage > 1 then
                    _href (paginationDestinationUrl (PaginationPage(currentPage - 1)))
            }

            fvePaginationContent {
                pageItem 1

                if List.head middlePages > 2 then
                    fvePaginationEllipsis { () }

                for page in middlePages do
                    pageItem page

                if List.last middlePages < 7 then
                    fvePaginationEllipsis { () }

                pageItem 8
            }

            fvePaginationNext {
                if currentPage < 8 then
                    _href (paginationDestinationUrl (PaginationPage(currentPage + 1)))
            }

            fvePaginationSummary { $"Showing {firstResult}–{lastResult} of 184 items" }
        }

    let paginationPreview requestedPage =
        paginationConfig PaginationVariant.Full requestedPage

    let paginationVariantPreview variant requestedPage = paginationConfig variant requestedPage

    let paginationPreviewRegion requestedPage =
        div {
            _id "components-pagination-region"
            paginationPreview requestedPage
        }

    let private statusOptions =
        [ (Active, "Active", false)
          (Pending, "Pending", false)
          (Suspended, "Suspended", false)
          (Scheduled, "Scheduled", false) ]

    let private selectStatusOptions =
        [ ("active", "Active", false)
          ("pending", "Pending", false)
          ("suspended", "Suspended", true)
          ("scheduled", "Scheduled", false) ]

    let private choiceSubmitButton id (label: string) =
        fveButton {
            _color Color.Primary
            _variant ButtonVariant.Solid
            _type "submit"
            _id id
            label
        }

    let private choiceResult id result =
        output {
            _id id
            _role "status"
            _class "block min-h-5 text-sm text-[var(--fve-muted-text)]"
            defaultArg result "Submit the form to see the result."
        }

    type ContactDetails =
        { name: string
          email: string
          notes: string }

    type ContactFormLayout =
        | Stacked
        | TwoColumns
        | Sectioned

    let contactFormRegion layout (details: ContactDetails) (errors: Map<string, string>) accepted =
        let key, title =
            match layout with
            | Stacked -> "", "Contact details"
            | TwoColumns -> "grid", "Contact details in two columns"
            | Sectioned -> "sectioned", "Contact details in sections"

        let prefix = if key = "" then "contact-" else "contact-" + key + "-"
        let endpoint = "/components/forms/contact" + (if key = "" then "" else "/" + key)

        let name =
            fveField {
                fveLabel {
                    _for (prefix + "name")
                    "Contact name"
                }

                fveInput {
                    _id (prefix + "name")
                    _name "contactName"
                    _value details.name
                    _required true
                    _autocomplete "name"
                    _ariaInvalid (Option.isSome (Map.tryFind "contactName" errors))

                    _ariaDescribedby (
                        String.concat
                            " "
                            [ if Option.isSome (Map.tryFind "contactName" errors) then
                                  ((prefix + "name")) + "-validation" ]
                    )
                }

                match Map.tryFind "contactName" errors with
                | Some message ->
                    fveFieldError {
                        _id ((prefix + "name") + "-validation")
                        message
                    }
                | None -> ()
            }

        let email =
            fveField {
                fveLabel {
                    _for (prefix + "email")
                    "Email address"
                }

                fveInput {
                    _id (prefix + "email")
                    _name "email"
                    _type InputType.Email
                    _value details.email
                    _required true
                    _autocomplete "email"
                    _ariaInvalid (Option.isSome (Map.tryFind "email" errors))

                    _ariaDescribedby (
                        String.concat
                            " "
                            [ ((prefix + "email")) + "-description"
                              if Option.isSome (Map.tryFind "email" errors) then
                                  ((prefix + "email")) + "-validation" ]
                    )
                }

                fveFieldDescription {
                    _id ((prefix + "email") + "-description")
                    "Use the address where you receive updates."
                }

                match Map.tryFind "email" errors with
                | Some message ->
                    fveFieldError {
                        _id ((prefix + "email") + "-validation")
                        message
                    }
                | None -> ()
            }

        let identityFields =
            div {
                _class (
                    if layout = Stacked then
                        "grid gap-4"
                    else
                        "grid gap-4 sm:grid-cols-2"
                )

                name
                email
            }

        let notes =
            fveField {
                fveLabel {
                    _for (prefix + "notes")
                    "Notes"
                }

                fveTextarea {
                    _id (prefix + "notes")
                    _name "notes"
                    _value details.notes
                    _ariaDescribedby (String.concat " " [ ((prefix + "notes")) + "-description" ])
                }

                fveFieldDescription {
                    _id ((prefix + "notes") + "-description")
                    "Any additional information we should know."
                }
            }

        div {
            _id ("components-" + prefix + "region")

            _class (
                "mx-auto grid w-full min-w-0 gap-6 p-6 sm:p-8 "
                + if layout = Stacked then "max-w-xl" else "max-w-4xl"
            )

            if not (Map.isEmpty errors) then
                fveErrorSummary {
                    _id (prefix + "errors")
                    _label "Check your contact details"
                    _autofocus true

                    match Map.tryFind "contactName" errors with
                    | Some message ->
                        fveErrorSummaryItem {
                            _href ("#" + prefix + "name")
                            "Contact name: " + message
                        }
                    | None -> ()

                    match Map.tryFind "email" errors with
                    | Some message ->
                        fveErrorSummaryItem {
                            _href ("#" + prefix + "email")
                            "Email address: " + message
                        }
                    | None -> ()
                }

            if accepted then
                fveNotice {
                    _id (prefix + "result")
                    _label "Details are valid"
                    p { "Validation succeeded." }
                    _color Color.Success
                    _announcement LiveAnnouncement.Polite
                }

            form {
                _ariaLabel title
                _class "grid gap-6"
                _attr ("novalidate", "")
                _dataOn ("submit", $"@post('{endpoint}', {{contentType: 'form'}})")

                if layout = Sectioned then
                    Section.create
                        (fveSectionHeader {
                            _label "Contact information"
                            _description "Who should we contact?"
                            _divider true
                        })
                        identityFields
                    |> Section.render

                    Section.create
                        (fveSectionHeader {
                            _label "Additional details"
                            _divider true
                        })
                        notes
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
        fveField {
            fveLabel {
                _for "exampleEmail"
                "Email"
            }

            fveInput {
                _id "exampleEmail"
                _name "exampleEmail"
            }
        }

    let inputWithHelp =
        fveField {
            fveLabel {
                _for "helpEmail"
                "Email"
            }

            fveInput {
                _id "helpEmail"
                _name "helpEmail"
                _type InputType.Email
                _placeholder "you@fve.meiermade.com"
                _autocomplete "email"
                _ariaDescribedby "helpEmail-description"
            }

            fveFieldDescription {
                _id "helpEmail-description"
                "We will use this address for updates."
            }
        }

    let nativeValidationAttributes key initiallyShown (message: string) =
        let checkedState = "$" + key + "_checked"
        let error = "$" + key + "_error"
        let refresh = error + " = el.validity.valid ? '' : " + System.Text.Json.JsonSerializer.Serialize(message)
        let validate = checkedState + " = true; " + refresh
        [ _dataSignals ($"{{{key}_checked: false, {key}_error: ''}}")
          _dataOn ("blur", validate)
          _dataOn ("input", $"if ({checkedState}) {{ {refresh} }}")
          _dataOn ("invalid", validate)
          _dataOn ("reset__window", $"if (evt.target === el.form) queueMicrotask(() => {{ if (!evt.defaultPrevented) {{ {checkedState} = false; {error} = ''; }} }})")
          _dataAttr ("aria-invalid", error + " ? 'true' : null")
          if initiallyShown then _dataInit validate ]

    let requiredInput =
        fveField {
            fveLabel { _for "requiredEmail"; "Email (required)" }
            fveInput {
                _id "requiredEmail"
                _name "requiredEmail"
                _type InputType.Email
                _required true
                _ariaDescribedby "required-email-help"
                yield! nativeValidationAttributes "_required_email" false "Enter a valid email address."
                _dataAttr ("aria-describedby", "$_required_email_error ? 'required-email-help required-email-error' : 'required-email-help'")
            }
            fveFieldDescription {
                _id "required-email-help"
                "Leave the field to check it. Correcting the email clears the error as you type."
            }
            fveFieldError {
                _id "required-email-error"
                _style "display:none"
                _dataShow "$_required_email_error"
                _dataText "$_required_email_error"
            }
        }

    let optionalInput =
        fveField {
            fveLabel {
                _for "company"
                "Company (optional)"
            }

            fveInput {
                _id "company"
                _name "company"
                _autocomplete "organization"
            }
        }

    let invalidInput =
        fveField {
            fveLabel { _for "invalidEmail"; "Email" }
            fveInput {
                _id "invalidEmail"
                _name "invalidEmail"
                _type InputType.Email
                _value "not-an-email"
                yield! nativeValidationAttributes "_invalid_email" true "Enter a valid email address."
                _dataAttr ("aria-describedby", "$_invalid_email_error ? 'invalidEmail-validation' : null")
            }
            fveFieldError {
                _id "invalidEmail-validation"
                _style "display:none"
                _dataShow "$_invalid_email_error"
                _dataText "$_invalid_email_error"
            }
        }

    let emailIcon =
        raw
            """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="size-5"><path stroke-linecap="round" stroke-linejoin="round" d="M21.75 6.75v10.5A2.25 2.25 0 0 1 19.5 19.5h-15a2.25 2.25 0 0 1-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25m19.5 0-8.578 5.498a2.25 2.25 0 0 1-2.344 0L2.25 6.75"/></svg>"""

    let inputWithIcon =
        fveField {
            fveLabel {
                _for "iconEmail"
                "Email"
            }

            fveInputGroup {
                fveInput {
                    _id "iconEmail"
                    _name "iconEmail"
                    _type InputType.Email
                    _placeholder "you@fve.meiermade.com"
                }
                fveInputGroupAddon { _ariaHidden true; emailIcon }
            }
        }

    let inputWithPrefix =
        fveField {
            fveLabel {
                _for "website"
                "Website"
            }

            fveInputGroup {
                fveInput {
                    _id "website"
                    _name "website"
                    _placeholder "fve.meiermade.com"
                    _autocomplete "off"
                    _ariaDescribedby "website-scheme"
                }
                fveInputGroupAddon { _id "website-scheme"; "https://" }
            }
        }

    let inputWithSuffix =
        fveField {
            fveLabel {
                _for "distance"
                "Distance"
            }

            fveInputGroup {
                fveInput {
                    _id "distance"
                    _name "distance"
                    _inputmode "decimal"
                    _placeholder "0.00"
                    _ariaDescribedby "distance-unit"
                }
                fveInputGroupAddon {
                    _position InputGroupPosition.InlineEnd
                    _id "distance-unit"
                    "km"
                }
            }
        }

    let disabledInput =
        fveField {
            fveLabel {
                _for "disabledEmail"
                "Email"
            }

            fveInput {

                _name "disabledEmail"
                _value "alex@fve.meiermade.com"
                _disabled true

                _id "disabledEmail"
            }
        }

    let readOnlyInput =
        fveField {
            fveLabel { _for "readOnlyEmail"; "Read-only email" }
            fveInput {
                _id "readOnlyEmail"
                _name "readOnlyEmail"
                _value "alex@fve.meiermade.com"
                _readonly true
            }
        }

    let searchInputExample =
        fveField {
            fveLabel {
                _for "field-search"
                "Search"
            }

            fveInput {

                _name "query"
                _id "field-search"
                _type "search"
                _placeholder "Search…"
                _autocomplete "off"
            }
        }

    let inputSizeExamples =
        div {
            _class "grid min-w-0 gap-6"

            for size, name in [ Size.ExtraSmall, "Extra small"; Size.Small, "Compact"; Size.Medium, "Standard"; Size.Large, "Large"; Size.ExtraLarge, "Extra large" ] do
                let key = name.ToLowerInvariant().Replace(" ", "-")
                div {
                    _class (Size.className size + " flex flex-wrap items-end gap-3")

                    fveField {
                        fveLabel {
                            _for ("sizing-" + key)
                            Html.text (name + " search")
                        }

                        fveInput {
                            _id ("sizing-" + key)
                            _name ("sizing-" + key)
                            _type InputType.Search
                        }
                    }

                    fveField {
                        fveLabel {
                            _for ("sizing-type-" + key)
                            Html.text (name + " type")
                        }

                        fveSelect {
                            _id ("sizing-type-" + key)
                            _name ("sizing-type-" + key)
                            _value "all"

                            fveSelectContent {
                                for value, label, disabled in ([ ("all", "All types", false) ]) do
                                    fveSelectItem {
                                        _value value
                                        _label label
                                        _disabled disabled
                                    }
                            }
                        }
                    }

                    div {
                        _class "flex flex-wrap items-center gap-3"
                        fveButton { Html.text (name + " action") }

                        fveButton {
                            _ariaLabel (name + " refresh")
                            _iconOnly true

                            span {
                                _ariaHidden true
                                _class "inline-flex shrink-0"
                                refreshIcon
                            }
                        }
                    }
                }

            div {
                _class (Size.className Size.Small + " flex flex-wrap items-end gap-3")

                fveField {
                    fveLabel {
                        _for "override-search"
                        "Larger search in a compact region"
                    }

                    fveInput {
                        _id "override-search"
                        _name "override-search"
                        _type InputType.Search
                        _size Size.Large
                    }
                }

                fveField {
                    fveLabel {
                        _for "override-type"
                        "Larger type"
                    }

                    fveSelect {
                        _id "override-type"
                        _name "override-type"
                        _value "all"
                        _size Size.Large

                        fveSelectContent {
                            for value, label, disabled in ([ ("all", "All types", false) ]) do
                                fveSelectItem {
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }
                    }
                }

                div {
                    _class "flex flex-wrap items-center gap-3"

                    fveButton {
                        _size Size.Large
                        "Larger action"
                    }

                    fveButton {
                        _size Size.Large
                        _ariaLabel "Larger refresh"
                        _iconOnly true

                        span {
                            _ariaHidden true
                            _class "inline-flex shrink-0"
                            refreshIcon
                        }
                    }
                }
            }
        }

    let tagInputExample =
        fveField {
            fveLabel {
                _for "item-tags"
                "Tags"
            }

            fveTagInput {
                _id "item-tags"
                _name "tags"
                _values []
            }
        }

    let invalidTagInputExample =
        fveField {
            fveLabel {
                _for "required-tags"
                "Required tags"
            }

            fveTagInput {
                _id "required-tags"
                _name "requiredTags"
                _ariaRequired true
                _ariaInvalid true
                _ariaDescribedby "required-tags-help required-tags-error"
            }

            fveFieldDescription {
                _id "required-tags-help"
                "Add at least one classification."
            }

            fveFieldError {
                _id "required-tags-error"
                "Add a tag before continuing."
            }
        }

    let pendingTagInputExample =
        fveField {
            fveLabel {
                _for "pending-tags"
                "Updating tags"
            }

            fveTagInput {
                _id "pending-tags"
                _name "pendingTags"
                _values [ "reviewed" ]
                _readonly true
                _ariaBusy true
            }
        }

    let disabledTagInputExample =
        fveField {
            fveLabel {
                _for "disabled-tags"
                "Unavailable tags"
            }

            fveTagInput {
                _id "disabled-tags"
                _name "disabledTags"
                _values [ "archived" ]
                _disabled true
            }
        }

    let tagInputFormRegion values (validation: string option) (result: string option) =
        div {
            _id "components-tag-form-region"
            _class "grid gap-3"

            form {
                _class "grid gap-3"
                _dataOn ("submit", "@post('/components/tags', {contentType: 'form'})")

                fveField {
                    fveLabel {
                        _for "submitted-tags"
                        "Submitted tags"
                    }

                    fveTagInput {
                        _id "submitted-tags"
                        _name "submittedTags"
                        _values values
                        _ariaInvalid validation.IsSome

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ "submitted-tags-help"
                                  if validation.IsSome then
                                      "submitted-tags-error" ]
                        )
                    }

                    fveFieldDescription {
                        _id "submitted-tags-help"
                        "Choose one to five tags."
                    }

                    match validation with
                    | Some message ->
                        fveFieldError {
                            _id "submitted-tags-error"
                            message
                        }
                    | None -> ()
                }

                fveButton {
                    _type "submit"
                    "Save tags"
                }

                fveButton {
                    _type "button"
                    _dataOn ("click", "@get('/components/tags/reset')")
                    "Reset tags from server"
                }
            }

            match result with
            | Some message ->
                output {
                    _role "status"
                    message
                }
            | None -> ()
        }

    let labelledTextarea =
        fveField {
            fveLabel {
                _for "message"
                "Message"
            }

            fveTextarea {
                _id "message"
                _name "message"
            }
        }

    let composerTextarea =
        fveField {
            fveLabel {
                _for "reply"
                _class "sr-only"
                "Message"
            }

            fveTextarea {
                _id "reply"
                _name "reply"
                _rows 1
                _required true
                _placeholder "Write a message…"
            }
        }

    let invalidTextarea =
        fveField {
            fveLabel { _for "invalidMessage"; "Message" }
            fveTextarea {
                _id "invalidMessage"
                _name "invalidMessage"
                _required true
                yield! nativeValidationAttributes "_invalid_message" true "Enter a message before continuing."
                _dataAttr ("aria-describedby", "$_invalid_message_error ? 'invalidMessage-validation' : null")
            }
            fveFieldError {
                _id "invalidMessage-validation"
                _style "display:none"
                _dataShow "$_invalid_message_error"
                _dataText "$_invalid_message_error"
            }
        }

    let editableInstructions =
        fveField {
            fveLabel {
                _for "instructions-editable"
                "Instructions"
            }

            fveTextarea {
                _id "instructions-editable"
                _name "instructions"
                _value "Include the reference number when you reply."
                _maxlength 400
                _ariaDescribedby (String.concat " " [ ("instructions-editable") + "-description" ])
            }

            fveFieldDescription {
                _id ("instructions-editable" + "-description")
                "Maximum 400 characters."
            }
        }

    let readOnlyNotes =
        fveField {
            fveLabel { _for "readOnlyNotes"; "Read-only notes" }
            fveTextarea {
                _id "readOnlyNotes"
                _name "readOnlyNotes"
                _value "Please include reference ITEM-2048."
                _readonly true
            }
        }

    let unavailableNotes =
        fveField {
            fveLabel {
                _for "unavailableNotes"
                "Unavailable notes"
            }

            fveTextarea {
                _id "unavailableNotes"
                _name "unavailableNotes"
                _disabled true
            }
        }

    let errorSummaryExample =
        div {
            _class "grid min-w-0 max-w-xl gap-4 p-4"
            fveErrorSummary {
                _id "example-errors"
                _label "Check your details"
                _style "display:none"
                _dataShow "$_summary_email_error"
                fveErrorSummaryItem {
                    _href "#summary-email"
                    _dataText "'Email address: ' + $_summary_email_error"
                }
            }
            fveField {
                fveLabel { _for "summary-email"; "Email address" }
                fveInput {
                    _id "summary-email"
                    _name "email"
                    _type InputType.Email
                    _value "not-an-email"
                    yield! nativeValidationAttributes "_summary_email" true "Enter a valid email address."
                    _dataAttr ("aria-describedby", "$_summary_email_error ? 'summary-email-validation' : null")
                }
                fveFieldError {
                    _id "summary-email-validation"
                    _style "display:none"
                    _dataShow "$_summary_email_error"
                    _dataText "$_summary_email_error"
                }
            }
        }

    let informationNotice =
        fveNotice {
            _id "notice-information"
            _label "Before you submit"
            p { "Check the contact name and email address before sending." }
        }

    let noticeAnnouncementExample =
        div {
            _class "grid gap-4"
            _dataSignals "{_noticeStatus: ''}"

            fveButton {
                _class "justify-self-start"
                _dataOn ("click", "$_noticeStatus = 'Ready to continue.'")
                "Update status"
            }

            fveNotice {
                _id "notice-announcement"
                _label "Status"
                _announcement LiveAnnouncement.Polite
                span { _dataText "$_noticeStatus" }
            }
        }

    let noticeColorExamples =
        div {
            _class "grid w-full min-w-0 grid-cols-[repeat(auto-fit,minmax(min(100%,20rem),1fr))] gap-4"

            for label, color in
                [ "Primary", Color.Primary
                  "Secondary", Color.Secondary
                  "Success", Color.Success
                  "Warning", Color.Warning
                  "Error", Color.Error
                  "Info", Color.Info
                  "Neutral", Color.Neutral ] do
                fveNotice {
                    _id ("notice-color-" + label.ToLowerInvariant())
                    _label label
                    p { "Review your details before continuing." }
                    _color color
                }
        }

    let noticeVariantExamples =
        div {
            _class "grid w-full min-w-0 grid-cols-[repeat(auto-fit,minmax(min(100%,20rem),1fr))] gap-4"

            for label, variant in
                [ "Solid", NoticeVariant.Solid
                  "Soft", NoticeVariant.Soft
                  "Outline", NoticeVariant.Outline
                  "Ghost", NoticeVariant.Ghost ] do
                fveNotice {
                    _id ("notice-variant-" + label.ToLowerInvariant())
                    _label label
                    p { "Review your details before continuing." }
                    _variant variant

                    fveNoticeActions {
                        a {
                            _href "/components/notice"
                            _class "underline underline-offset-2"
                            "Read guidance"
                        }
                    }
                }
        }

    let successNotice =
        fveNotice {
            _id "notice-success"
            _label "Export ready"
            p { "Your export is ready to download." }
            _color Color.Success

            fveNoticeActions {
                a {
                    _href "data:text/csv;charset=utf-8,Name%2CStatus%0AProject%20notes%2CActive"
                    _attr ("download", "items.csv")
                    _class "underline underline-offset-2"
                    "Download"
                }
            }
        }

    let warningNotice =
        fveNotice {
            _id "notice-warning"
            _label "Review required"
            p { "Two items need review before they can be published." }
            _color Color.Warning
        }

    let criticalNotice =
        fveNotice {
            _id "notice-error"
            _label "Details need attention"
            p { "Correct the highlighted fields and validate the form again." }
            _color Color.Error

            fveNoticeActions {
                a {
                    _href "/components/error-summary"
                    _class "underline underline-offset-2"
                    "Review highlighted fields"
                }
            }
        }

    let memberOptions =
        [ ("alex", "Alex Morgan", false)
          ("jamie", "Jamie Lee", false)
          ("riley", "Riley Chen", false)
          ("taylor", "Taylor Brooks", false)
          ("sam", "Sam Rivera", true) ]

    let multipleMembersSelect =
        fveField {
            fveLabel {
                _for "memberIds"
                "Team members"
            }

            fveMultiSelect {



                _placeholder "Choose members"
                _name "memberIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "memberIds"
            }
        }

    let selectedMembersSelect =
        fveField {
            fveLabel {
                _for "assignedIds"
                "Assigned members"
            }

            fveMultiSelect {



                _values ([ "alex"; "jamie"; "riley" ])
                _name "assignedIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "assignedIds"
            }
        }

    let disabledMembersSelect =
        fveField {
            fveLabel {
                _for "disabledMemberIds"
                "Disabled members"
            }

            fveMultiSelect {



                _values ([ "alex"; "jamie" ])
                _name "disabledMemberIds"
                _disabled true

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "disabledMemberIds"
            }
        }

    let pendingMembersSelect =
        fveField {
            fveLabel {
                _for "pendingMemberIds"
                "Updating members"
            }

            fveMultiSelect {



                _values ([ "alex"; "jamie" ])
                _readonly true
                _ariaBusy true
                _name "pendingMemberIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "pendingMemberIds"
            }
        }

    let multipleSelectForm selection (validationMessage: string option) result =
        div {
            _id "components-multiple-select-form-region"
            _class "grid gap-3"

            form {
                _ariaLabel "Project reviewers"
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/multiple-select', {contentType: 'form'})")
                _class "grid gap-3"

                fveField {
                    fveLabel {
                        _for "components-reviewers"
                        "Reviewers"
                    }

                    fveMultiSelect {



                        _values selection


                        _name "reviewerIds"
                        _id "components-reviewers"
                        _required true

                        fveSelectContent {
                            for choice in memberOptions do
                                fveSelectItem {
                                    let value, label, disabled = choice
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }

                        _ariaInvalid (Option.isSome validationMessage)

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ ("components-reviewers") + "-description"
                                  if Option.isSome (validationMessage) then
                                      ("components-reviewers") + "-validation" ]
                        )
                    }

                    fveFieldDescription {
                        _id ("components-reviewers" + "-description")
                        "Choose one to three reviewers."
                    }

                    match validationMessage with
                    | Some message ->
                        fveFieldError {
                            _id ("components-reviewers" + "-validation")
                            message
                        }
                    | None -> ()
                }

                choiceSubmitButton "components-reviewers-submit" "Validate reviewers"
            }

            choiceResult "components-reviewers-result" result
        }

    let multipleMembersCombobox =
        fveField {
            fveLabel {
                _for "searchMemberIds"
                "Search members"
            }

            fveMultiSelect {



                _search SelectSearch.Static
                _values ([ "alex"; "jamie" ])
                _name "searchMemberIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "searchMemberIds"
            }
        }

    let disabledMembersCombobox =
        fveField {
            fveLabel {
                _for "disabledSearchIds"
                "Disabled search members"
            }

            fveMultiSelect {



                _search SelectSearch.Static
                _values ([ "alex"; "jamie" ])
                _name "disabledSearchIds"
                _disabled true

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "disabledSearchIds"
            }
        }

    let pendingMembersCombobox =
        fveField {
            fveLabel {
                _for "pendingSearchIds"
                "Updating search members"
            }

            fveMultiSelect {



                _search SelectSearch.Static
                _values ([ "alex"; "jamie" ])
                _readonly true
                _ariaBusy true
                _name "pendingSearchIds"

                fveSelectContent {
                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "pendingSearchIds"
            }
        }

    let loadingMembersCombobox =
        fveField {
            fveLabel {
                _for "loadingSearchIds"
                "Loading members"
            }

            fveMultiSelect {



                _search SelectSearch.Static
                _values ([ "alex" ])
                _name "loadingSearchIds"

                fveSelectContent {
                    _loading true

                    for choice in memberOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "loadingSearchIds"
            }
        }

    let remoteMembersContent searchText choices (failure: string option) =
        fveSelectContent {
            _id "fve-select-components_remote_members-popup"
            _query searchText
            _error failure
            _retry "/components/members/search?retry=true"

            for choice in choices do
                fveSelectItem {
                    let value, label, disabled = choice
                    _value value
                    _label label
                    _disabled disabled
                }
        }

    let remoteMembersView searchText choices failure =
        fveField {
            fveLabel {
                _for "components-remote-members"
                "Remote members"
            }

            fveMultiSelect {


                _values (([ ("alex", "Alex Morgan", false) ]) |> List.map (fun (value, _, _) -> value))

                for value, label, _ in
                    (([ ("alex", "Alex Morgan", false) ])
                     |> List.distinctBy (fun (value, _, _) -> value)) do
                    fveSelectSelectedItem {
                        _value value
                        label
                    }

                _placeholder "Search members"

                _search (
                    SelectSearch.Remote(
                        if Option.isSome failure then
                            "/components/members/search?retry=true"
                        else
                            "/components/members/search"
                    )
                )

                _query searchText
                remoteMembersContent searchText choices failure
                _name "remoteMemberIds"
                _id "components-remote-members"

                _ariaDescribedby (String.concat " " [ ("components-remote-members") + "-description" ])
            }

            fveFieldDescription {
                _id ("components-remote-members" + "-description")
                "Try Jamie or Riley for matches, or error to test retry."
            }
        }

    let remoteMembersCombobox =
        div {
            _class "grid gap-3"
            remoteMembersView "" memberOptions None

            button {
                _type "button"
                _dataOn ("click", "document.getElementById('fve-select-components_remote_members-field').dispatchEvent(new CustomEvent('fve-select-refresh', {detail:{endpoint:'/components/members/field'}}))")
                _class "min-h-8 text-sm text-[var(--fve-brand-text)] underline underline-offset-2"
                "Refresh members"
            }
        }

    let searchMemberOptions (query: string) =
        [ "alex", "Alex Morgan"
          "jamie", "Jamie Lee"
          "riley", "Riley Chen"
          "taylor", "Taylor Brooks" ]
        |> List.filter (fun (_, label) -> label.Contains(query, StringComparison.OrdinalIgnoreCase))
        |> List.map (fun (value, label) -> (value, label, false))

    let remoteMembersField searchText =
        remoteMembersView searchText (searchMemberOptions searchText) None

    let remoteMembersOptions searchText retry =
        let isError = String.Equals(searchText, "error", StringComparison.OrdinalIgnoreCase)

        let choices =
            if isError then
                memberOptions
            else
                searchMemberOptions searchText

        let failure =
            if isError && not retry then
                Some "Members could not be loaded."
            else
                None

        remoteMembersContent searchText choices failure

    let selectSearchQuery signalName (context: Microsoft.AspNetCore.Http.HttpContext) =
        try
            use signals =
                System.Text.Json.JsonDocument.Parse(context.Request.Query["datastar"].ToString())

            match signals.RootElement.TryGetProperty(signalName: string) with
            | true, value when value.ValueKind = System.Text.Json.JsonValueKind.String -> Some(value.GetString())
            | _ -> None
        with
        | :? System.Text.Json.JsonException
        | :? InvalidOperationException -> None

    let remoteMemberSearch wholeField : HttpHandler =
        fun next context ->
            task {
                let searchText =
                    selectSearchQuery "components_remote_members_query" context
                    |> Option.defaultValue ""

                let retry = context.Request.Query["retry"].ToString() = "true"
                do! System.Threading.Tasks.Task.Delay(300, context.RequestAborted)

                let view =
                    if wholeField then
                        remoteMembersField searchText
                    else
                        remoteMembersOptions searchText retry

                return! htmlString (Render.toString view) next context
            }

    let remoteMembersRoutes: HttpHandler =
        choose
            [ Giraffe.Core.GET
              >=> route "/components/members/search"
              >=> remoteMemberSearch false
              Giraffe.Core.GET
              >=> route "/components/members/field"
              >=> remoteMemberSearch true ]

    let multipleComboboxForm selection (validationMessage: string option) result =
        div {
            _id "components-multiple-combobox-form-region"
            _class "grid gap-3"

            form {
                _ariaLabel "Project assignees"
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/multiple-combobox', {contentType: 'form'})")
                _class "grid gap-3"

                fveField {
                    fveLabel {
                        _for "components-assignees"
                        "Assignees"
                    }

                    fveMultiSelect {



                        _search SelectSearch.Static
                        _values selection


                        _name "assigneeIds"
                        _id "components-assignees"
                        _required true

                        fveSelectContent {
                            for choice in memberOptions do
                                fveSelectItem {
                                    let value, label, disabled = choice
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }

                        _ariaInvalid (Option.isSome validationMessage)

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ ("components-assignees") + "-description"
                                  if Option.isSome (validationMessage) then
                                      ("components-assignees") + "-validation" ]
                        )
                    }

                    fveFieldDescription {
                        _id ("components-assignees" + "-description")
                        "Choose one to three assignees."
                    }

                    match validationMessage with
                    | Some message ->
                        fveFieldError {
                            _id ("components-assignees" + "-validation")
                            message
                        }
                    | None -> ()
                }

                choiceSubmitButton "components-assignees-submit" "Validate assignees"
            }

            choiceResult "components-assignees-result" result
        }

    let frequencyOptions =
        [ "daily", "Daily"; "weekly", "Weekly"; "monthly", "Monthly" ]

    let defaultSelect =
        fveField {
            fveLabel {
                _for "updateFrequencyDefault"
                "Update frequency"
            }

            fveSelect {
                _id "updateFrequencyDefault"
                _name "updateFrequencyDefault"
                _value "daily"

                fveSelectContent {
                    for value, label in frequencyOptions do
                        fveSelectItem {
                            _value value

                            if value = "monthly" then
                                _disabled true

                            label
                        }
                }
            }
        }

    let nativeFallbackSelect =
        fveField {
            fveLabel {
                _for "nativeFrequency"
                "Update frequency"
            }

            fveSelect {



                _value "weekly"
                _nativeFallback true
                _name "nativeFrequency"

                fveSelectContent {
                    for choice in
                        ([ ("daily", "Daily", false)
                           ("weekly", "Weekly", false)
                           ("monthly", "Monthly", false) ]) do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "nativeFrequency"
            }
        }

    let basicSelect =
        fveField {
            fveLabel {
                _for "updateFrequency"
                "Update frequency"
            }

            fveSelect {



                _value "weekly"
                _position SelectPosition.SelectedItem
                _name "updateFrequency"

                fveSelectContent {
                    for choice in
                        ([ ("daily", "Daily", false)
                           ("weekly", "Weekly", false)
                           ("monthly", "Monthly", false) ]) do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "updateFrequency"
            }
        }

    let selectWithHelp =
        fveField {
            fveLabel {
                _for "digestFrequency"
                "Digest frequency"
            }

            fveSelect {




                _placeholder "Choose a frequency"
                _name "digestFrequency"

                fveSelectContent {
                    for choice in
                        ([ ("daily", "Daily", false)
                           ("weekly", "Weekly", false)
                           ("monthly", "Monthly", false) ]) do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "digestFrequency"
                _ariaDescribedby (String.concat " " [ ("digestFrequency") + "-description" ])
            }

            fveFieldDescription {
                _id ("digestFrequency" + "-description")
                "Choose how often to receive a summary."
            }
        }

    let edgeAlignedSelect =
        fveField {
            fveLabel {
                _for "exportFormat"
                "Export format"
            }

            fveSelect {



                _value "json"
                _position SelectPosition.TriggerEnd
                _name "exportFormat"

                fveSelectContent {
                    for choice in ([ ("csv", "CSV", false); ("json", "JSON", false); ("pdf", "PDF", false) ]) do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _id "exportFormat"
            }
        }

    let basicCheckbox =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "emailUpdates"
                _name "emailUpdates"
            }

            fveLabel {
                _for "emailUpdates"
                "Email me product updates"
            }
        }

    let checkboxWithHelp =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "includeSummary"
                _name "includeSummary"
                _ariaDescribedby "includeSummary-description"
            }

            fveFieldContent {
                fveLabel {
                    _for "includeSummary"
                    "Include a summary"
                }

                fveFieldDescription {
                    _id "includeSummary-description"
                    "Add a short overview at the beginning of the report."
                }
            }
        }

    let indeterminateCheckbox =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "mixedSelection"
                _name "mixedSelection"
                _indeterminate true
            }

            fveLabel {
                _for "mixedSelection"
                "Select all items"
            }
        }

    let basicSwitch =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "productUpdates"
                _name "productUpdates"
            }

            fveLabel {
                _for "productUpdates"
                "Product updates"
            }
        }

    let switchWithHelp =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "weeklySummary"
                _name "weeklySummary"
                _checked true
                _ariaDescribedby "weeklySummary-help"
            }

            fveFieldContent {
                fveLabel {
                    _for "weeklySummary"
                    "Weekly summary"
                }

                fveFieldDescription {
                    _id "weeklySummary-help"
                    "Receive one email with the week's activity."
                }
            }
        }

    let basicRadioGroup =
        fveRadioGroup {
            fveFieldLegend { "Contact method" }

            for value, caption in [ "email", "Email"; "phone", "Phone" ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("contact-" + value)
                        _name "contactMethod"
                        _value value
                    }

                    fveLabel {
                        _for ("contact-" + value)
                        caption
                    }
                }
        }

    let radioGroupWithHelp =
        fveRadioGroup {
            _ariaDescribedby "delivery-help"
            fveFieldLegend { "Delivery" }

            fveFieldDescription {
                _id "delivery-help"
                "Choose how you would like your order delivered."
            }

            for value, caption, description in
                [ "standard", "Standard", "Arrives in three to five business days."
                  "express", "Express", "Arrives the next business day." ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("shipping-" + value)
                        _name "delivery"
                        _value value
                        _ariaDescribedby ("shipping-" + value + "-help")
                    }

                    fveFieldContent {
                        fveLabel {
                            _for ("shipping-" + value)
                            caption
                        }

                        fveFieldDescription {
                            _id ("shipping-" + value + "-help")
                            description
                        }
                    }
                }
        }

    let horizontalRadioGroup =
        fveRadioGroup {
            _orientation Orientation.Horizontal
            fveFieldLegend { "Table density" }

            for value, caption in [ "compact", "Compact"; "comfortable", "Comfortable"; "spacious", "Spacious" ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("density-" + value)
                        _name "density"
                        _value value

                        if value = "comfortable" then
                            _checked true
                    }

                    fveLabel {
                        _for ("density-" + value)
                        caption
                    }
                }
        }

    let defaultChoiceCards =
        fveChoiceCards {
            _id "default-assistance-choice"
            fveFieldLegend { "Booking assistance" }

            fveChoiceCardsContent {
                for value, caption in [ "self", "Self-service"; "staff", "Staff assisted" ] do
                    fveChoiceCard {
                        fveRadio {
                            _name "defaultAssistance"
                            _value value
                        }

                        fveChoiceCardTitle { caption }
                    }
            }
        }

    let richChoiceCards =
        fveChoiceCards {
            _id "assistance-choice"
            fveFieldLegend { "Booking assistance" }

            fveChoiceCardsContent {
                for value, caption, description, metadata in
                    [ "self",
                      "Self-service",
                      "The participant completes details and waivers before arrival.",
                      "No staff coordination required"
                      "staff",
                      "Staff assisted",
                      "A coordinator confirms equipment and participant details.",
                      "Recommended for group bookings"
                      "unavailable", "Managed service", "Available only to contracted organizations.", "" ] do
                    fveChoiceCard {
                        fveRadio {
                            _name "assistance"
                            _value value
                            _required true

                            if value = "staff" then
                                _checked true

                            if value = "unavailable" then
                                _disabled true
                        }

                        fveChoiceCardContent {
                            fveChoiceCardTitle { caption }
                            fveChoiceCardDescription { description }

                            if metadata <> "" then
                                fveChoiceCardMetadata { metadata }
                        }
                    }
            }
        }

    let multipleChoiceCards =
        fveChoiceCards {
            _id "equipment-choice"
            fveFieldLegend { "Equipment" }

            fveChoiceCardsContent {
                for value, caption, description in
                    [ "helmet", "Helmet", "Required protective equipment."
                      "pads", "Protective pads", "Knee and elbow protection."
                      "radio", "Trail radio", "Unavailable for this booking." ] do
                    fveChoiceCard {
                        fveCheckbox {
                            _name "equipmentIds"
                            _value value
                            if value = "radio" then _disabled true else _checked true
                        }

                        fveChoiceCardContent {
                            fveChoiceCardTitle { caption }
                            fveChoiceCardDescription { description }

                            if value = "helmet" then
                                fveChoiceCardMetadata { "Available" }
                        }
                    }
            }
        }

    let invalidChoiceCards =
        fveChoiceCards {
            _id "invalid-assistance-choice"
            _ariaInvalid true
            _ariaDescribedby "invalid-assistance-error"
            fveFieldLegend { "Booking assistance" }

            fveChoiceCardsContent {
                for value, caption in [ "self", "Self-service"; "staff", "Staff assisted" ] do
                    fveChoiceCard {
                        fveRadio {
                            _name "invalidAssistance"
                            _value value
                            _required true
                            _ariaInvalid true
                            _ariaDescribedby "invalid-assistance-error"
                        }

                        fveChoiceCardTitle { caption }
                    }
            }

            fveFieldError {
                _id "invalid-assistance-error"
                "Choose how this booking will be supported."
            }
        }

    let selectFormRegion (selection: string option) (validationMessage: string option) result =
        div {
            _id "components-select-form-region"
            _class "grid max-w-sm gap-3"

            form {
                _dataOn ("submit", "@post('/components/choices/select', {contentType:'form'})")
                _class "grid gap-3"

                fveField {
                    fveLabel {
                        _for "components-status"
                        "Status"
                    }

                    fveSelect {



                        match selection with
                        | Some value -> _value value
                        | None -> ()

                        _placeholder "Choose a status"

                        _name "status"
                        _id "components-status"
                        _required true

                        fveSelectContent {
                            for choice in selectStatusOptions do
                                fveSelectItem {
                                    let value, label, disabled = choice
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }

                        _ariaInvalid (Option.isSome validationMessage)

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ ("components-status") + "-description"
                                  if Option.isSome (validationMessage) then
                                      ("components-status") + "-validation" ]
                        )
                    }

                    fveFieldDescription {
                        _id ("components-status" + "-description")
                        "Choose the availability of this item."
                    }

                    match validationMessage with
                    | Some message ->
                        fveFieldError {
                            _id ("components-status" + "-validation")
                            message
                        }
                    | None -> ()
                }

                choiceSubmitButton "components-select-submit" "Validate status"
            }

            choiceResult "components-select-result" result
        }

    let disabledStatusSelect =
        fveField {
            fveLabel {
                _for "components-disabled-status"
                "Disabled status"
            }

            fveSelect {



                _value "active"
                _name "status"
                _id "components-disabled-status"
                _disabled true

                fveSelectContent {
                    for choice in selectStatusOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let pendingStatusSelect =
        fveField {
            fveLabel {
                _for "components-pending-status"
                "Updating status"
            }

            fveSelect {



                _value "pending"
                _readonly true
                _ariaBusy true
                _name "status"
                _id "components-pending-status"

                fveSelectContent {
                    for choice in selectStatusOptions do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let statusSelect = selectFormRegion None None None

    let private searchItems =
        [ "101", "Project notes"; "102", "Shared files"; "103", "Archived files" ]

    let private itemOptions values =
        values |> List.map (fun (value, label) -> (value, label, (value = "103")))

    let staticItemCombobox =
        fveField {
            fveLabel {
                _for "components-static-item"
                "Search items"
            }

            fveSelect {



                _search SelectSearch.Static
                _value "101"

                _name "staticItem"
                _id "components-static-item"

                fveSelectContent {
                    for choice in itemOptions searchItems do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _ariaDescribedby (String.concat " " [ ("components-static-item") + "-description" ])
            }

            fveFieldDescription {
                _id ("components-static-item" + "-description")
                "Type to filter the items."
            }
        }

    let itemContent contentId searchText (failure: string option) (choices: (string * string * bool) list) =
        fveSelectContent {
            _id contentId
            _query searchText
            _error failure
            _retry "/components/items/search?retry=true"
            _emptyMessage "No matching items"
            _loadingMessage "Loading items"

            for choice in choices do
                fveSelectItem {
                    let value, label, disabled = choice
                    _value value
                    _label label
                    _disabled disabled
                }
        }

    let itemComboboxView fieldName (caption: string) (choices: (string * string * bool) list) =
        fveField {
            fveLabel {
                _for fieldName
                caption
            }

            fveSelect {


                _placeholder "Search items"

                _search (SelectSearch.Remote "/components/items/search")
                itemContent ("fve-select-" + fieldName + "-popup") "" None choices
                _name fieldName

                _id fieldName
                _ariaDescribedby (String.concat " " [ (fieldName) + "-description" ])
            }

            fveFieldDescription {
                _id (fieldName + "-description")
                "Search for Project notes, or type error to try the retry action."
            }
        }

    let itemCombobox =
        let choices = itemOptions searchItems

        fveField {
            fveLabel {
                _for "item"
                "Choose an item"
            }

            fveSelect {


                _placeholder "Search items"

                _search (SelectSearch.Remote "/components/items/search")
                _name "item"
                itemContent "fve-select-item-popup" "" None choices

                _id "item"
                _ariaDescribedby (String.concat " " [ ("item") + "-description" ])
            }

            fveFieldDescription {
                _id ("item" + "-description")
                "Search for Project notes, or type error to try the retry action."
            }
        }

    let isolatedItemComboboxes =
        div {
            _class "grid gap-4 sm:grid-cols-2"
            itemComboboxView "firstitem" "First item" (itemOptions searchItems)
            itemComboboxView "seconditem" "Second item" (itemOptions searchItems)
        }

    let itemComboboxOptions contentId searchText retry =
        let failed =
            String.Equals(searchText, "error", StringComparison.OrdinalIgnoreCase)
            && not retry

        let choices =
            if failed then
                []
            else
                searchItems
                |> List.filter (fun (_, label) ->
                    String.IsNullOrWhiteSpace searchText
                    || label.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                |> itemOptions

        itemContent contentId searchText (if failed then Some "Items could not be loaded." else None) choices

    let itemSearchTargets =
        [ "item_query", "fve-select-item-popup"
          "firstitem_query", "fve-select-firstitem-popup"
          "seconditem_query", "fve-select-seconditem-popup"
          "components_loading_item_query", "fve-select-components_loading_item-popup" ]

    let itemSearchHandler signalName contentId : HttpHandler =
        fun next context ->
            task {
                match selectSearchQuery signalName context with
                | None ->
                    return! (setStatusCode 400 >=> Giraffe.Core.text "A known search field is required.") next context
                | Some searchText ->
                    let delay =
                        if searchText.StartsWith("proj", StringComparison.OrdinalIgnoreCase) then
                            750
                        else
                            300

                    do! System.Threading.Tasks.Task.Delay(delay, context.RequestAborted)
                    let retry = context.Request.Query["retry"].ToString() = "true"
                    return! htmlString (itemComboboxOptions contentId searchText retry |> Render.toString) next context
            }

    let itemSearchRoutes: HttpHandler =
        Giraffe.Core.GET
        >=> route "/components/items/search"
        >=> itemSearchHandler "item_query" "fve-select-item-popup"

    let itemFixtureSearchRoutes: HttpHandler =
        Giraffe.Core.GET
        >=> route "/components/items/search"
        >=> fun next context ->
            // Additional field identities belong to regression fixtures, not the public example.
            match
                itemSearchTargets
                |> List.tryFind (fun (signal, _) -> signal <> "item_query" && (selectSearchQuery signal context).IsSome)
            with
            | Some(signal, contentId) -> itemSearchHandler signal contentId next context
            | None -> System.Threading.Tasks.Task.FromResult None

    let composedStatusSelect =
        let icon data =
            svg {
                _ariaHidden true
                _class "size-4 shrink-0"
                _viewBox "0 0 24 24"
                _fill "none"
                _stroke "currentColor"
                _strokeWidth "1.5"

                path {
                    _strokeLinecap "round"
                    _strokeLinejoin "round"
                    _d data
                }
            }

        let active = ("active", "Active", false)
        let pendingChoice = ("pending", "Pending", false)
        let scheduled = ("scheduled", "Scheduled", false)

        fveField {
            fveLabel {
                _for "components-composed-status"
                "Composed status"
            }

            fveSelect {


                _value "active"

                fveSelectContent {
                    _id "composed-status-content"

                    fveSelectGroup {
                        _label "Current"

                        fveSelectItem {
                            let value, label, disabled = active
                            _value value
                            _label label
                            _disabled disabled

                            span {
                                _class "flex flex-wrap items-center gap-x-2 gap-y-1 font-normal"
                                icon "m3.75 13.5 10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75Z"

                                span {
                                    _class "whitespace-nowrap"
                                    "Active"
                                }
                            }
                        }

                    }

                    fveSelectGroup {
                        _label "Upcoming"

                        fveSelectItem {
                            let value, label, disabled = pendingChoice
                            _value value
                            _label label
                            _disabled disabled

                            span {
                                _class "flex flex-wrap items-center gap-x-2 gap-y-1 font-normal"
                                icon "M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"

                                span {
                                    _class "whitespace-nowrap"
                                    "Pending"
                                }
                            }
                        }

                        fveSelectItem {
                            let value, label, disabled = scheduled
                            _value value
                            _label label
                            _disabled disabled

                            span {
                                _class "flex flex-wrap items-center gap-x-2 gap-y-1 font-normal"

                                icon
                                    "M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5m-9-6h.008v.008H12v-.008ZM12 15h.008v.008H12V15Zm0 2.25h.008v.008H12v-.008ZM9.75 15h.008v.008H9.75V15Zm0 2.25h.008v.008H9.75v-.008ZM7.5 15h.008v.008H7.5V15Zm0 2.25h.008v.008H7.5v-.008Zm6.75-4.5h.008v.008h-.008v-.008Zm0 2.25h.008v.008h-.008V15Zm0 2.25h.008v.008h-.008v-.008Zm2.25-4.5h.008v.008H16.5v-.008Zm0 2.25h.008v.008H16.5V15Z"

                                span {
                                    _class "whitespace-nowrap"
                                    "Scheduled"
                                }
                            }
                        }

                    }
                }

                _name "composedStatus"
                _id "components-composed-status"
            }
        }

    let loadingItemCombobox =
        fveField {
            fveLabel {
                _for "components-loading-item"
                "Loading items"
            }

            fveSelect {



                _search (SelectSearch.Remote "/components/items/search")
                _name "loadingItem"
                _id "components-loading-item"

                fveSelectContent {
                    _loading true
                    _loadingMessage "Loading items"
                }
            }
        }

    let validationItemCombobox =
        fveField {
            fveLabel {
                _for "components-validated-item"
                "Item with validation"
            }

            fveSelect {



                _search SelectSearch.Static


                _name "validatedItem"
                _id "components-validated-item"

                fveSelectContent {
                    for choice in itemOptions searchItems do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }

                _ariaInvalid (Option.isSome (Some "Choose an available item."))

                _ariaDescribedby (
                    String.concat
                        " "
                        [ ("components-validated-item") + "-description"
                          if Option.isSome (Some("Choose an available item.")) then
                              ("components-validated-item") + "-validation" ]
                )
            }

            fveFieldDescription {
                _id ("components-validated-item" + "-description")
                "Choose an item before continuing."
            }

            match Some "Choose an available item." with
            | Some message ->
                fveFieldError {
                    _id ("components-validated-item" + "-validation")
                    message
                }
            | None -> ()
        }

    let disabledItemCombobox =
        fveField {
            fveLabel {
                _for "components-disabled-item"
                "Unavailable item"
            }

            fveSelect {



                _search SelectSearch.Static
                _value "101"
                _name "disabledItem"
                _id "components-disabled-item"
                _disabled true

                fveSelectContent {
                    for choice in itemOptions searchItems do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let pendingItemCombobox =
        fveField {
            fveLabel {
                _for "components-pending-item"
                "Updating item"
            }

            fveSelect {



                _search SelectSearch.Static
                _value "102"
                _readonly true
                _ariaBusy true
                _name "pendingItem"
                _id "components-pending-item"

                fveSelectContent {
                    for choice in itemOptions searchItems do
                        fveSelectItem {
                            let value, label, disabled = choice
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let checkboxFormRegion confirmed (validation: string option) result =
        div {
            _id "components-checkbox-form-region"
            _class "grid max-w-sm gap-3"

            form {
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/checkbox', {contentType: 'form'})")
                _class "grid gap-3"

                fveField {
                    _layout FieldLayout.Horizontal

                    fveCheckbox {
                        _id "components-confirm-archived-review"
                        _name "confirmArchivedReview"
                        _required true
                        _checked confirmed
                        _ariaInvalid validation.IsSome

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ "confirmed-description"
                                  if validation.IsSome then
                                      "confirmed-validation" ]
                        )
                    }

                    fveFieldContent {
                        fveLabel {
                            _for "components-confirm-archived-review"
                            "Confirm review"
                        }

                        fveFieldDescription {
                            _id "confirmed-description"
                            "Required before continuing."
                        }

                        match validation with
                        | Some message ->
                            fveFieldError {
                                _id "confirmed-validation"
                                message
                            }
                        | None -> ()
                    }
                }

                choiceSubmitButton "components-checkbox-submit" "Validate confirmation"
            }

            choiceResult "components-checkbox-result" result
        }

    let includeArchived =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "includeArchived"
                _name "includeArchived"
                _checked true
                _ariaDescribedby "includeArchived-description"
            }

            fveFieldContent {
                fveLabel {
                    _for "includeArchived"
                    "Include archived items"
                }

                fveFieldDescription {
                    _id "includeArchived-description"
                    "Archived items remain read-only."
                }
            }
        }

    let pendingArchivedReview =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "components-pending-archived-review"
                _name "confirmArchivedReview"
                _checked true
                _readonly true
                _ariaBusy true
            }

            fveLabel {
                _for "components-pending-archived-review"
                "Saving review"
            }
        }

    let disabledArchivedReview =
        fveField {
            _layout FieldLayout.Horizontal

            fveCheckbox {
                _id "components-disabled-archived-review"
                _name "confirmArchivedReview"
                _disabled true
            }

            fveLabel {
                _for "components-disabled-archived-review"
                "Archived review unavailable"
            }
        }

    let switchFormRegion enabled (validation: string option) result =
        div {
            _id "components-switch-form-region"
            _class "grid max-w-sm gap-3"

            form {
                _dataOn ("submit", "@post('/components/choices/switch', {contentType: 'form'})")
                _class "grid gap-3"

                fveField {
                    _layout FieldLayout.Horizontal

                    fveSwitch {
                        _id "components-notifications"
                        _name "notifications"
                        _checked enabled
                        _ariaInvalid validation.IsSome

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ "notifications-help"
                                  if validation.IsSome then
                                      "notifications-error" ]
                        )
                    }

                    fveFieldContent {
                        fveLabel {
                            _for "components-notifications"
                            "Notifications"
                        }

                        fveFieldDescription {
                            _id "notifications-help"
                            "Receive an update when new items are published."
                        }

                        match validation with
                        | Some message ->
                            fveFieldError {
                                _id "notifications-error"
                                message
                            }
                        | None -> ()
                    }
                }

                choiceSubmitButton "components-switch-submit" "Save notifications"
            }

            choiceResult "components-switch-result" result
        }

    let notificationSetting = switchFormRegion true None None

    let pendingNotifications =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "components-pending-notifications"
                _name "notifications"
                _checked true
                _readonly true
                _ariaBusy true
            }

            fveLabel {
                _for "components-pending-notifications"
                "Saving notifications"
            }
        }

    let disabledNotifications =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "components-disabled-notifications"
                _name "notifications"
                _disabled true
            }

            fveLabel {
                _for "components-disabled-notifications"
                "Notifications unavailable"
            }
        }

    let invalidNotifications =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "components-invalid-notifications"
                _name "notifications"
                _ariaInvalid true
                _ariaDescribedby "notifications-invalid-error"
            }

            fveFieldContent {
                fveLabel {
                    _for "components-invalid-notifications"
                    "Notification service"
                }

                fveFieldError {
                    _id "notifications-invalid-error"
                    "Notification preferences could not be saved."
                }
            }
        }

    let switchSizes =
        div {
            _class "grid w-full max-w-sm gap-4"

            for value, caption, size in
                [ "compact", "Compact switch", Size.Small
                  "standard", "Standard switch", Size.Medium
                  "prominent", "Prominent switch", Size.Large ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveSwitch {
                        _id (value + "Alerts")
                        _name (value + "Alerts")
                        _size size
                        _checked true
                    }

                    fveLabel {
                        _for (value + "Alerts")
                        caption
                    }
                }
        }

    let stackedSwitch =
        fveField {
            fveLabel {
                _for "automaticExports"
                "Automatic exports"
            }

            fveSwitch {
                _id "automaticExports"
                _name "automaticExports"
                _ariaDescribedby "automaticExports-help"
            }

            fveFieldDescription {
                _id "automaticExports-help"
                "Generate an export when new files are published."
            }
        }

    let compactRows =
        fveToggleButton {
            _id "components-compact-rows"
            "Compact rows"
        }

    let pendingCompactRows =
        fveToggleButton {
            _id "components-pending-compact-rows"
            _readonly true
            _ariaBusy true
            "Applying compact rows"
        }

    let disabledCompactRows =
        fveToggleButton {
            _id "components-disabled-compact-rows"
            _disabled true
            "Compact rows unavailable"
        }

    let toggleButtonPresentation =
        div {
            _class "flex flex-wrap items-center gap-3"

            fveToggleButton {
                _id "toggle-soft"
                _variant ToggleButtonVariant.Soft
                _size Size.Small
                plusIcon
                "Add item"
            }

            fveToggleButton {
                _id "toggle-outline"
                _variant ToggleButtonVariant.Outline
                _ariaPressed true
                plusIcon
                "Add item"
            }

            fveToggleButton {
                _id "toggle-ghost"
                _variant ToggleButtonVariant.Ghost
                _size Size.Large
                plusIcon
                "Add item"
            }
        }

    let defaultToggleGroup =
        fveToggleGroup {
            _id "components-alignment-default"
            _name "alignment-default"
            _ariaLabel "Text alignment"

            fveToggleGroupItem {
                _value "left"
                "Left"
            }

            fveToggleGroupItem {
                _value "center"
                "Center"
            }

            fveToggleGroupItem {
                _value "right"
                "Right"
            }
        }

    let singleToggleGroup =
        fveToggleGroup {
            _id "components-alignment-group"
            _name "alignment"
            _ariaLabel "Text alignment"
            _value "left"

            fveToggleGroupItem {
                _value "left"
                "Left"
            }

            fveToggleGroupItem {
                _value "center"
                "Center"
            }

            fveToggleGroupItem {
                _value "right"
                "Right"
            }
        }

    let multipleToggleGroup =
        fveMultiToggleGroup {
            _id "components-formatting-group"
            _name "formatting"
            _ariaLabel "Text formatting"
            _values [ "bold" ]
            _variant ToggleGroupVariant.Spaced
            _size Size.Small

            fveToggleGroupItem {
                _value "bold"
                _label "Bold"
                plusIcon
                "Bold"
            }

            fveToggleGroupItem {
                _value "italic"
                _label "Italic"
                refreshIcon
                "Italic"
            }

            fveToggleGroupItem {
                _value "strike"
                _disabled true
                "Strike"
            }
        }

    let connectedToggleGroups =
        div {
            _class "grid justify-items-start gap-4"

            fveMultiToggleGroup {
                _id "connected-formats"
                _name "connected-format"
                _ariaLabel "Connected formats"
                _values [ "bold"; "italic" ]

                for value, label in [ "bold", "Bold"; "italic", "Italic"; "underline", "Underline" ] do
                    fveToggleGroupItem {
                        _value value
                        label
                    }
            }

            fveToggleGroup {
                _id "single-toggle-item"
                _name "pinned"
                _ariaLabel "Pinned item"
                _allowEmpty true

                fveToggleGroupItem {
                    _value "pinned"
                    "Pinned"
                }
            }
        }

    let toggleGroupFormRegion selected (validation: string option) result =
        div {
            _id "components-toggle-form-region"
            _class "grid gap-3"

            form {
                _class "grid gap-3"
                _dataOn ("submit", "@post('/components/choices/formatting', {contentType: 'form'})")

                fveMultiToggleGroup {
                    _id "components-saved-formats"
                    _name "format"
                    _ariaLabel "Saved formatting"
                    _values selected
                    _variant ToggleGroupVariant.Spaced

                    fveToggleGroupItem {
                        _value "bold"
                        "Bold"
                    }

                    fveToggleGroupItem {
                        _value "italic"
                        "Italic"
                    }

                    fveToggleGroupItem {
                        _value "strike"
                        _disabled true
                        "Strike"
                    }
                }

                match validation with
                | Some message -> fveFieldError { message }
                | None -> ()

                choiceSubmitButton "components-toggle-submit" "Save formatting"
            }

            choiceResult "components-toggle-result" result
        }

    let verticalToggleGroup =
        fveToggleGroup {
            _id "components-view-group"
            _name "view"
            _ariaLabel "Record view"
            _value "summary"
            _orientation Orientation.Vertical

            fveToggleGroupItem {
                _value "summary"
                "Summary"
            }

            fveToggleGroupItem {
                _value "activity"
                "Activity"
            }

            fveToggleGroupItem {
                _value "audit"
                "Audit log"
            }
        }

    let codePreviewTabs =
        fveTabs {
            _id "components-example-format"
            _ariaLabel "Example format"

            fveTabsItem {
                _value "code"
                fveTabsTrigger { "Code" }

                pre {
                    _role "region"
                    _ariaLabel "Button F# code"
                    _tabindex 0

                    _dataOn (
                        "keydown",
                        """
                        if (evt.target === el && !evt.altKey && !evt.ctrlKey && !evt.metaKey && !evt.shiftKey
                            && (evt.key === 'ArrowLeft' || evt.key === 'ArrowRight')) {
                            evt.preventDefault();
                            el.scrollBy({left: evt.key === 'ArrowLeft' ? -80 : 80});
                        }
                    """
                    )

                    _class
                        "overflow-x-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-4 text-sm outline-none focus-visible:outline-2 focus-visible:outline-solid focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                    "fveButton { \"Create\" }"
                }
            }

            fveTabsItem {
                _value "preview"
                fveTabsTrigger { "Preview" }

                div {
                    _class "rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] p-4"
                    fveButton { "Create" }
                }
            }
        }

    let verticalTabs =
        fveTabs {
            _id "components-vertical-tabs"
            _ariaLabel "Workspace settings"

            fveTabsItem {
                _value "profile"

                fveTabsTrigger {
                    span {
                        _ariaHidden true
                        _class "mr-2 inline-flex size-4 items-center justify-center align-text-bottom"
                        plusIcon
                    }

                    "Profile"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Update your name and profile image."
                }
            }

            fveTabsItem {
                _value "notifications"

                fveTabsTrigger {
                    span {
                        _ariaHidden true
                        _class "mr-2 inline-flex size-4 items-center justify-center align-text-bottom"
                        refreshIcon
                    }

                    "Notifications"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Choose which updates send alerts."
                }
            }

            fveTabsItem {
                _value "administration"

                fveTabsTrigger {
                    span {
                        _ariaHidden true
                        _class "mr-2 inline-flex size-4 items-center justify-center align-text-bottom"
                        removeIcon
                    }

                    _disabled true
                    "Administration"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "These settings require administrator access."
                }
            }

            _orientation Orientation.Vertical
        }

    let reviewTabsRegion refreshed =
        let refreshActivityButton =
            fveButton {
                _id "components-tabs-refresh"
                _dataOn ("click", "evt.currentTarget.focus(); @get('/components/tabs/review')")
                "Refresh activity"
            }

        let activity =
            if refreshed then
                div {
                    p {
                        _role "status"
                        _class "font-medium"
                        "Review refreshed"
                    }

                    p {
                        _class "mt-1 text-sm text-[var(--fve-muted-text)]"
                        "Updated just now."
                    }

                    refreshActivityButton
                }
            else
                div {
                    p {
                        _class "text-sm text-[var(--fve-muted-text)]"
                        "Three items need review."
                    }

                    refreshActivityButton
                }

        fveTabs {
            _id "components-review-tabs"
            _ariaLabel "Item sections"

            fveTabsItem {
                _value "overview"
                fveTabsTrigger { "Overview" }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "24 items updated this month"
                }
            }

            fveTabsItem {
                _value "activity"
                fveTabsTrigger { "Activity" }
                activity
            }

            fveTabsItem {
                _value "settings"
                fveTabsTrigger { "Settings" }

                a {
                    _href "/components/theming"
                    _class "font-medium text-[var(--fve-brand-text)]"
                    "Review appearance settings"
                }
            }

            _variant TabsVariant.Underlined
        }

    let private updateModeOptions =
        [ "automatic", "Automatic", false
          "manual", "Manual review", false
          "scheduled", "Scheduled", true ]

    let radioGroupFormRegion selected (validation: string option) result =
        div {
            _id "components-radio-form-region"
            _class "grid max-w-sm gap-3"

            form {
                _novalidate true
                _dataOn ("submit", "@post('/components/choices/radio', {contentType: 'form'})")
                _class "grid gap-3"

                fveRadioGroup {
                    _id "components-update-mode"
                    _ariaRequired true
                    _ariaInvalid validation.IsSome

                    _ariaDescribedby (
                        String.concat
                            " "
                            [ "update-mode-help"
                              if validation.IsSome then
                                  "update-mode-error" ]
                    )

                    fveFieldLegend { "Update mode" }

                    fveFieldDescription {
                        _id "update-mode-help"
                        "Choose how updates are applied."
                    }

                    for value, caption, disabled in updateModeOptions do
                        fveField {
                            _layout FieldLayout.Horizontal

                            fveRadio {
                                _id ("update-mode-" + value)
                                _name "updateMode"
                                _value value

                                if selected = Some value then
                                    _checked true

                                _disabled disabled
                                _required true
                                _ariaInvalid validation.IsSome

                                _ariaDescribedby (
                                    String.concat
                                        " "
                                        [ "update-mode-help"
                                          if validation.IsSome then
                                              "update-mode-error" ]
                                )
                            }

                            fveLabel {
                                _for ("update-mode-" + value)
                                caption
                            }
                        }

                    match validation with
                    | Some message ->
                        fveFieldError {
                            _id "update-mode-error"
                            message
                        }
                    | None -> ()
                }

                choiceSubmitButton "components-radio-submit" "Validate choice"
            }

            choiceResult "components-radio-result" result
        }

    let updateMode = radioGroupFormRegion None None None

    let pendingUpdateMode =
        fveRadioGroup {
            _ariaBusy true
            fveFieldLegend { "Saving update mode" }

            for value, caption, disabled in updateModeOptions do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("pending-mode-" + value)
                        _name "pendingUpdateMode"
                        _value value

                        if value = "automatic" then
                            _checked true

                        _disabled disabled
                        _readonly true
                    }

                    fveLabel {
                        _for ("pending-mode-" + value)
                        caption
                    }
                }
        }

    let disabledUpdateMode =
        fveRadioGroup {
            _disabled true
            fveFieldLegend { "Update mode unavailable" }

            for value, caption, _ in updateModeOptions do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveRadio {
                        _id ("disabled-mode-" + value)
                        _name "disabledUpdateMode"
                        _value value

                        if value = "manual" then
                            _checked true
                    }

                    fveLabel {
                        _for ("disabled-mode-" + value)
                        caption
                    }
                }
        }

    let menuLeadingIcon =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 0 1 .143 1.052l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 0 1 1.05-.143Z" clip-rule="evenodd"/></svg>"""

    let basicDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-basic"
            fveDropdownMenuTrigger { fveButton { "Actions" } }

            fveDropdownMenuItem {
                _href "/components/button"
                "Button documentation"
            }

            fveDropdownMenuItem {
                _href "/components/button-group"
                "Button group documentation"
            }
        }

    let menuDocumentIcon =
        raw """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="size-4" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6M6 3h8l4 4v14H6V3Zm8 0v4h4"/></svg>"""

    let iconDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-icons"
            fveDropdownMenuTrigger { fveButton { "Documentation" } }
            fveDropdownMenuItem {
                _href "/components/button"
                fveDropdownMenuItemLeading { plusIcon }
                "Button"
            }
            fveDropdownMenuItem {
                _href "/components/popover"
                fveDropdownMenuItemLeading { menuDocumentIcon }
                "Popover"
            }
        }

    let shortcutDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-shortcuts"
            fveDropdownMenuTrigger { fveButton { "Find documentation" } }
            fveDropdownMenuItem {
                _href "/components/button"
                "Button"
                fveDropdownMenuItemShortcut { "B" }
            }
            fveDropdownMenuItem {
                _href "/components/popover"
                "Popover"
                fveDropdownMenuItemShortcut { "P" }
            }
        }

    let checkboxDropdownMenu =
        div {
            _class "grid justify-items-start gap-3"
            _dataSignals "{_menu_email: true, _menu_push: false}"
            fveDropdownMenu {
                _id "components-menu-checkboxes"
                fveDropdownMenuTrigger { fveButton { "Notification preferences" } }
                fveDropdownMenuItem {
                    _role "menuitemcheckbox"; _ariaChecked true
                    _dataAttr ("aria-checked", "$_menu_email ? 'true' : 'false'")
                    _dataOn ("click", "$_menu_email = !$_menu_email")
                    "Email alerts"
                }
                fveDropdownMenuItem {
                    _role "menuitemcheckbox"; _ariaChecked false
                    _dataAttr ("aria-checked", "$_menu_push ? 'true' : 'false'")
                    _dataOn ("click", "$_menu_push = !$_menu_push")
                    "Push alerts"
                }
            }
            output {
                _ariaLive "polite"; _class "text-sm text-[var(--fve-muted-text)]"
                _dataText "'Email: ' + ($_menu_email ? 'on' : 'off') + ' · Push: ' + ($_menu_push ? 'on' : 'off')"
                "Email: on · Push: off"
            }
        }

    let radioDropdownMenu =
        div {
            _class "grid justify-items-start gap-3"
            _dataSignals "{_menu_sort: 'Name'}"
            fveDropdownMenu {
                _id "components-menu-radio"
                fveDropdownMenuTrigger { fveButton { "Sort preview" } }
                fveDropdownMenuGroup {
                    _label "Sort by"
                    for label in ["Name"; "Status"; "Last updated"] do
                        let selected = label = "Name"
                        fveDropdownMenuItem {
                            _role "menuitemradio"; _ariaChecked selected
                            _dataAttr ("aria-checked", "$_menu_sort === '" + label + "' ? 'true' : 'false'")
                            _dataOn ("click", "$_menu_sort = '" + label + "'")
                            label
                        }
                }
            }
            output {
                _ariaLive "polite"; _class "text-sm text-[var(--fve-muted-text)]"
                _dataText "'Sort by: ' + $_menu_sort"
                "Sort by: Name"
            }
        }

    let disabledDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-disabled"
            fveDropdownMenuTrigger { fveButton { "Available actions" } }
            fveDropdownMenuItem { _href "/components/dropdown-menu"; "Read guidance" }
            fveDropdownMenuItem {
                _disabled true
                _description "Choose a saved report first."
                "Export report"
            }
        }

    let destructiveDropdownMenu =
        div {
            _class "grid w-full max-w-sm gap-3"
            _dataSignals "{_menu_draft: 'Review September reports.'}"
            fveField {
                fveLabel { _for "menu-draft-note"; "Draft note" }
                fveTextarea {
                    _id "menu-draft-note"; _name "draftNote"
                    _dataBind "_menu_draft"
                    "Review September reports."
                }
            }
            fveDropdownMenu {
                _id "components-menu-destructive"
                fveDropdownMenuTrigger { fveButton { "Draft actions" } }
                fveDropdownMenuItem {
                    _color Color.Error
                    _dataOn ("click", "$_menu_draft = ''")
                    "Clear draft"
                }
                fveDropdownMenuSeparator { () }
                fveDropdownMenuItem {
                    _dataOn ("click", "$_menu_draft = 'Review September reports.'")
                    "Restore sample"
                }
            }
        }

    let dropdownMenuItems refreshed =
        [ fveDropdownMenuGroup {
              _label "Item"

              fveDropdownMenuItem {
                  _href "/components/dropdown-menu#components-dropdown-menu"
                  "Dropdown menu guidance"
              }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Record review"
                  fveDropdownMenuItemLeading { menuLeadingIcon }
                  fveDropdownMenuItemShortcut { "R" }
              }
          }
          fveDropdownMenuSeparator { () }
          fveDropdownMenuGroup {
              _label "Reports"

              if refreshed then
                  fveDropdownMenuItem {
                      _dataOn ("click", "$menuActivations++")
                      "Review refreshed actions"
                  }
              else
                  fveDropdownMenuItem {
                      _dataOn ("click", "@get('/components/menus/actions')")
                      "Refresh actions"
                  }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Export file"
                  _disabled true
              }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Syncing"
                  _readonly true
                  _ariaBusy true
              }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Create report"
              }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Close item"
              }
          }
          fveDropdownMenuSeparator { () }
          fveDropdownMenuGroup {
              _label "Display"

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuCompact = !$menuCompact")
                  _role "menuitemcheckbox"
                  "Compact rows"
                  _dataAttr ("aria-checked", "(" + "$menuCompact" + ") ? 'true' : 'false'")
              }
          }
          fveDropdownMenuSeparator { () }
          fveDropdownMenuItem {
              _dataOn ("click", "$menuDeletes++")
              "Delete draft"
              _color Color.Error
          } ]

    let actionMenu refreshed =
        fveDropdownMenu {
            _id "components-menu-actions"
            _ariaLabel "Actions"
            fveDropdownMenuTrigger { fveButton { "Actions" } }
            yield! (dropdownMenuItems refreshed)
            _alignment DropdownMenuAlignment.Start
        }

    let menuOverflowIcon =
        svg {
            _viewBox "0 0 24 24"
            _fill "none"
            _stroke "currentColor"
            _strokeWidth "1.5"
            _ariaHidden "true"
            _class "size-5"

            path {
                _strokeLinecap "round"
                _strokeLinejoin "round"

                _d
                    "M6.75 12a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm6 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Z"
            }
        }

    let menuTrailingChevron =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M7.21 4.23a.75.75 0 0 1 1.06-.02l5.5 5.25a.75.75 0 0 1 0 1.08l-5.5 5.25a.75.75 0 0 1-1.04-1.08L12.16 10 7.23 5.29a.75.75 0 0 1-.02-1.06Z" clip-rule="evenodd"/></svg>"""

    let fullRowDropdownMenu =
        div {
            _class "w-64 max-w-full border border-[var(--fve-border)]"
            _dataSignals "{menuRegion: 'Side navigation'}"

            fveDropdownMenu {
                _id "components-menu-full-row"
                _class "w-full"
                _ariaLabel "Choose a navigation pattern"

                fveDropdownMenuTrigger {

                    _class
                        "flex min-h-10 w-full items-center rounded-none px-4 py-2 text-left text-sm leading-5 font-semibold text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] active:bg-[var(--fve-surface-active)]"

                    fun (triggerAttributes: HtmlAttribute list) ->
                        button {
                            for attribute in triggerAttributes do
                                attribute

                            span {
                                _class "flex w-full items-center justify-between gap-3"

                                span {
                                    _dataText "$menuRegion"
                                    "Side navigation"
                                }

                                raw
                                    """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 0 1 1.06.02L10 11.17l3.71-3.94a.75.75 0 0 1 1.08 1.04l-4.25 4.5a.75.75 0 0 1-1.08 0l-4.25-4.5a.75.75 0 0 1 .02-1.06Z" clip-rule="evenodd"/></svg>"""
                            }
                        }

                }

                fveDropdownMenuItem {
                    _dataOn ("click", "$menuRegion = 'Side navigation'")
                    _role "menuitemradio"
                    "Side navigation"
                    _description "Persistent desktop navigation"
                    _ariaChecked true

                    _dataAttr ("aria-checked", "(" + "$menuRegion == 'Side navigation'" + ") ? 'true' : 'false'")
                }

                fveDropdownMenuItem {
                    _dataOn ("click", "$menuRegion = 'Breadcrumbs'")
                    _role "menuitemradio"
                    "Breadcrumbs"
                    _description "Location within a page hierarchy"

                    _dataAttr ("aria-checked", "(" + "$menuRegion == 'Breadcrumbs'" + ") ? 'true' : 'false'")
                }

                fveDropdownMenuSeparator { () }

                fveDropdownMenuItem {
                    _href "/components/side-nav"
                    "Read navigation guidance"
                    fveDropdownMenuItemTrailing { menuTrailingChevron }
                }

                _alignment DropdownMenuAlignment.Start
            }
        }

    let customDropdownMenu =
        div {
            _class "grid justify-items-start gap-3"
            _dataSignals "{customMenuClicks: 0}"

            fveDropdownMenu {
                _id "components-menu-custom"

                fveDropdownMenuTrigger {
                    _id "workspace-menu-control"

                    _class
                        "inline-flex items-center justify-center rounded-full border border-[var(--fve-border)] p-1.5 text-sm font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                    _dataOn ("click", "$customMenuClicks++")

                    fun (triggerAttributes: HtmlAttribute list) ->
                        button {
                            for attribute in triggerAttributes do
                                attribute

                            span {
                                _ariaHidden true

                                _class
                                    "flex size-9 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs font-semibold text-[var(--fve-brand-text)]"

                                "PW"
                            }

                            span {
                                _class "sr-only"
                                "Personal workspace"
                            }
                        }

                }

                fveDropdownMenuItem {
                    _href "/components/side-nav"
                    "Side navigation"
                }

                fveDropdownMenuItem {
                    _href "/components/breadcrumbs"
                    "Breadcrumbs"
                }
            }

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Trigger activations: "

                span {
                    _dataText "$customMenuClicks"
                    "0"
                }
            }
        }

    let moreActionsMenu =
        fveDropdownMenu {
            _id "components-menu-more"
            _ariaLabel "More actions"

            fveDropdownMenuTrigger {
                fveButton {
                    _iconOnly true
                    _variant ButtonVariant.Ghost

                    menuOverflowIcon
                }
            }

            fveDropdownMenuGroup {
                _label "View"

                fveDropdownMenuItem {
                    _href "/components/dropdown-menu#components-dropdown-menu"
                    "Read menu guidance"
                }

                fveDropdownMenuItem {
                    _dataOn ("click", "$menuActivations++")
                    "Record secondary action"
                }
            }
        }

    let outlinedDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-outline"

            fveDropdownMenuTrigger {
                fveButton {
                    _iconOnly true

                    _ariaLabel "More page actions"
                    menuOverflowIcon
                    _variant ButtonVariant.Outline
                }
            }

            fveDropdownMenuItem {
                _href "/components/dropdown-menu"
                "Read menu guidance"
            }

            fveDropdownMenuItem {
                _href "/components/button"
                "Read button guidance"
            }
        }

    let dropdownMenuRegion refreshed =
        div {
            _id "components-dropdown-menu-region"
            _dataSignals "{menuActivations: 0, menuDeletes: 0, menuCompact: false}"
            _class "grid justify-items-center text-center"

            div {
                _class "flex flex-wrap items-center justify-center gap-3"
                [ actionMenu refreshed; moreActionsMenu ]
            }

            p {
                _class "mt-4 text-sm text-[var(--fve-muted-text)]"
                _dataText "'Completed menu actions: ' + $menuActivations"
                "Completed menu actions: 0"
            }

            p {
                _class "mt-1 text-sm text-[var(--fve-critical-text)]"
                _dataText "'Delete activations: ' + $menuDeletes"
                "Delete activations: 0"
            }

            if refreshed then
                p {
                    _role "status"
                    _class "mt-1 text-sm text-[var(--fve-positive-text)]"
                    "Actions refreshed."
                }
        }

    let patchedDropdownMenuRegion = dropdownMenuRegion true

    let defaultDrawer =
        fveDrawer {
            _id "default-drawer"
            _label "Item details"
            p { "Project notes · Document" }
        }

    let defaultDrawerExample =
        div {
            fveDrawerTrigger {
                fveButton {
                    _ariaControls "default-drawer"
                    "Open drawer"
                }
            }

            defaultDrawer
        }

    let defaultDialog =
        fveDialog {
            _id "default-dialog"
            _label "Review changes"
            p { "Check your changes before continuing." }
        }

    let defaultDialogExample =
        div {
            fveDialogTrigger {
                fveButton {
                    _ariaControls "default-dialog"
                    "Open dialog"
                }
            }

            defaultDialog
        }

    let containedDrawerConfig =
        fveDrawer {
            _id "contained-drawer"
            _label "Item details"
            p { "Project notes · Document" }
            _container "contained-drawer-background"
            _initialFocus "contained-drawer-close"

            fveDrawerFooter {
                fveButton {
                    _dataOn ("click", "document.getElementById('contained-drawer').close()")
                    "Done"
                }
            }
        }

    let containedDrawerExample =
        div {
            _class "relative h-80 overflow-hidden rounded-lg border border-[var(--fve-border)]"

            div {
                _id "contained-drawer-background"
                _class "h-full p-5"

                p {
                    _class "mb-4 text-sm"
                    "Open the drawer to review the item."
                }

                fveDrawerTrigger {
                    fveButton {
                        _ariaControls "contained-drawer"
                        "Open contained drawer"
                    }
                }
            }

            containedDrawerConfig
        }

    let containedDialogConfig =
        fveDialog {
            _id "contained-dialog"
            _label "Review changes"
            _container "contained-dialog-background"
            _initialFocus "contained-dialog-close"
            _dismissOnBackdrop true
            p { "Check your changes before continuing." }

            fveDialogFooter {
                fveDialogClose {
                    _id "contained-dialog-close"
                    _ariaControls "contained-dialog"
                    "Cancel"
                }
            }
        }

    let containedDialogExample =
        div {
            _class "relative h-80 overflow-hidden rounded-lg border border-[var(--fve-border)]"

            div {
                _id "contained-dialog-background"
                _class "h-full p-5"

                p {
                    _class "mb-4 text-sm"
                    "Surrounding documentation stays available."
                }

                fveDialogTrigger {
                    fveButton {
                        _ariaControls "contained-dialog"
                        "Open contained dialog"
                    }
                }
            }

            containedDialogConfig
        }

    let dialogConfig =
        fveDialog {
            _id "review-changes-dialog"
            _label "Review changes"
            _description "You can close this dialog to make further changes."
            _initialFocus "review-changes-dialog-close"
            _dismissOnBackdrop true
            p { "Check your changes before continuing." }

            fveDialogFooter {
                fveDialogClose {
                    _id "review-changes-dialog-close"
                    _ariaControls "review-changes-dialog"
                    "Close"
                }
            }
        }

    let reviewDialogTrigger =
        fveDialogTrigger {
            fveButton {
                _ariaControls "review-changes-dialog"
                "Review changes"
            }
        }

    let reviewDialog = dialogConfig

    let itemDeletionForm (dialogId: string) (validation: string option) =
        let requestSignal = "_" + dialogId.Replace('-', '_') + "_pending"
        let pendingExpression = "$" + requestSignal

        form {
            _id $"{dialogId}-content"
            _method "post"
            _action "/components/dialogs/confirm"
            _dataIndicator requestSignal

            _dataOn (
                "submit",
                $"!({pendingExpression}) && @post('/components/dialogs/confirm').then(() => document.getElementById('{dialogId}-confirm')?.focus())"
            )

            _dataAttr ("aria-busy", $"{pendingExpression} ? 'true' : 'false'")

            match validation with
            | Some message ->
                p {
                    _id $"{dialogId}-validation"
                    _role "alert"

                    _class
                        "rounded-[var(--fve-radius-control)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)] ring-1 ring-inset ring-[var(--fve-critical-ring)]"

                    message
                }
            | None -> ()

            p {
                _id $"{dialogId}-status"
                _tabindex -1
                _role "status"
                _dataShow pendingExpression
                _style "display:none"
                _class "mt-4 text-sm text-[var(--fve-muted-text)]"
                "Confirmation in progress."
            }

            div {
                _class "mt-6 flex flex-wrap justify-end gap-3"

                fveButton {
                    _id $"{dialogId}-cancel"
                    _dataAttr ("disabled", pendingExpression)
                    _dataOn ("click", "el.closest('dialog').close()")
                    "Cancel"
                }

                fveButton {
                    _color Color.Error
                    _variant ButtonVariant.Solid
                    _dataAttr ("disabled", pendingExpression)
                    _dataAttr ("aria-busy", $"{pendingExpression} ? 'true' : null")
                    _type "submit"
                    _id $"{dialogId}-confirm"
                    "Delete"
                }
            }
        }

    let itemDeletionContent validation =
        itemDeletionForm "delete-item-confirmation" validation

    let itemDeletionDialog =
        fveDialog {
            _id "delete-item-confirmation"
            _label "Delete item?"
            itemDeletionContent None
            _description "Delete Project notes? This action cannot be undone."
            _initialFocus "delete-item-confirmation-cancel"

            _role "alertdialog"
            _dataSignals "{_delete_item_confirmation_pending: false}"
            _dataOn ("cancel", "$_delete_item_confirmation_pending && evt.preventDefault()")
        }

    let itemDrawerContent refreshed =
        div {
            _id "item-drawer-content"
            _class "grid gap-5"

            fveDescriptionList {
                fveDescriptionListItem {
                    _label "Name"
                    "Project notes"
                }

                fveDescriptionListItem {
                    _label "Type"
                    "Document"
                }

                fveDescriptionListItem {
                    _label "Size"
                    "2.4 MB"
                }

                fveDescriptionListItem {
                    _label "Status"

                    fveBadge {
                        _color Color.Success
                        "Active"
                    }
                }
            }

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Last updated September 15, 2026 by Alex Morgan."
            }

            fveButton {
                _id "item-drawer-refresh"

                _dataOn (
                    "click",
                    "@get('/components/drawers/item').then(() => document.getElementById('item-drawer-refresh')?.focus())"
                )

                "Refresh details"
            }

            if refreshed then
                p {
                    _role "status"
                    _class "text-sm text-[var(--fve-positive-text)]"
                    "Details refreshed."
                }
        }

    let itemDrawerConfig =
        fveDrawer {
            _id "item-details-drawer"
            _label "Item details"
            itemDrawerContent false
            _description "Review the selected item."
            _initialFocus "item-drawer-refresh"
        }

    let itemEditorBody =
        form {
            _id "item-editor-form"
            _class "grid gap-6"

            section {
                _ariaLabelledby "item-editor-basics"
                _class "grid gap-4"

                h3 {
                    _id "item-editor-basics"
                    _class "text-base font-semibold"
                    "Details"
                }

                fveField {
                    fveLabel {
                        _for "item-editor-name"
                        "Name"
                    }

                    fveInput {
                        _id "item-editor-name"
                        _name "itemName"
                        _value "Project notes"
                        _ariaDescribedby (String.concat " " [ ("item-editor-name") + "-description" ])
                    }

                    fveFieldDescription {
                        _id ("item-editor-name" + "-description")
                        "Choose a name that is easy to find."
                    }
                }

                fveField {
                    fveLabel {
                        _for "item-editor-type"
                        "Type"
                    }

                    fveSelect {
                        _id "item-editor-type"
                        _name "itemType"
                        _value "document"

                        fveSelectContent {
                            for value, label, disabled in
                                ([ ("document", "Document", false)
                                   ("image", "Image", false)
                                   ("folder", "Folder", false) ]) do
                                fveSelectItem {
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }
                    }
                }

                fveField {
                    fveLabel {
                        _for "reference"
                        "Reference"
                    }

                    fveInput {
                        _id "reference"
                        _name "reference"
                        _value "1040"
                    }
                }
            }

            section {
                _ariaLabelledby "item-editor-organization"
                _class "grid gap-4 border-t border-[var(--fve-border)] pt-6"

                h3 {
                    _id "item-editor-organization"
                    _class "text-base font-semibold"
                    "Organization"
                }

                fveField {
                    fveLabel {
                        _for "folder"
                        "Folder"
                    }

                    fveInput {
                        _id "folder"
                        _name "folder"
                        _value "Shared files"
                    }
                }

                fveField {
                    fveLabel {
                        _for "notes"
                        "Internal notes"
                    }

                    fveTextarea {
                        _id "notes"
                        _name "notes"
                        _rows 5
                        _value "Notes from the latest project review."
                        _ariaDescribedby (String.concat " " [ ("notes") + "-description" ])
                    }

                    fveFieldDescription {
                        _id ("notes" + "-description")
                        "Only workspace members can read these notes."
                    }
                }
            }
        }

    let itemEditorFooter =
        div {
            _class "flex flex-wrap justify-end gap-3"

            fveButton {
                _dataOn ("click", "document.getElementById('item-editor-drawer')?.close()")
                "Cancel"
            }

            fveButton {
                _type "submit"
                _attr ("form", "item-editor-form")
                "Save changes"
            }
        }

    let itemEditorDrawerConfig =
        fveDrawer {
            _id "item-editor-drawer"
            _label "Edit item"
            itemEditorBody
            _description "Update the item details."
            fveDrawerFooter { itemEditorFooter }
            _initialFocus "item-editor-name"
            _size DrawerSize.Large
            _style "width:min(32rem,100vw)"
        }

    let filterDrawerConfig =
        fveDrawer {
            _id "item-filters-drawer"
            _label "Filters"

            nav {
                _ariaLabel "Item filters"
                _class "grid gap-3"

                a {
                    _href "/components/drawer#components-drawer"
                    "Active items"
                }

                a {
                    _href "/components/drawer#components-drawer-wide"
                    "Archived items"
                }
            }

            _side DrawerSide.Start
        }

    let topDrawerConfig =
        fveDrawer {
            _id "item-summary-drawer"
            _label "Summary"

            div {
                _class "grid gap-2 text-sm"
                p { "24 items in Shared files" }

                p {
                    _class "text-[var(--fve-muted-text)]"
                    "Updated September 28."
                }
            }

            _description "A horizontal drawer for a compact review task."
            _side DrawerSide.Top
        }

    let bottomDrawerConfig =
        fveDrawer {
            _id "item-history-drawer"
            _label "Activity history"

            ol {
                _class "grid gap-4 text-sm"

                for index in 1..12 do
                    li { $"Activity entry {index}" }
            }

            _description "Long content scrolls while the heading and actions remain reachable."
            _side DrawerSide.Bottom
            _size DrawerSize.Large

            fveDrawerFooter {
                fveButton {
                    _dataOn ("click", "document.getElementById('item-history-drawer')?.close()")
                    "Done"
                }
            }
        }

    let patchedItemDrawerContent = itemDrawerContent true

    let private accountTypeFilter =
        fveField {
            fveLabel {
                _for "accountTypeFilter"
                _class "sr-only"
                "Filter by account type"
            }

            fveSelect {
                _id "accountTypeFilter"
                _name "accountTypeFilter"
                _value "all"

                fveSelectContent {
                    for value, label, disabled in
                        ([ ("all", "All types", false)
                           ("asset", "Asset", false)
                           ("liability", "Liability", false)
                           ("equity", "Equity", false)
                           ("revenue", "Revenue", false)
                           ("expense", "Expense", false) ]) do
                        fveSelectItem {
                            _value value
                            _label label
                            _disabled disabled
                        }
                }
            }
        }

    let private toolbar =
        section {
            _role "region"
            _ariaLabel "Collection controls"

            div {
                _class "flex flex-col gap-2 sm:flex-row sm:items-center"

                div {
                    _class "min-w-40 flex-1"

                    fveField {
                        fveLabel {
                            _for "query"
                            _class "sr-only"
                            "Search accounts"
                        }

                        fveInput {
                            _id "query"
                            _name "query"
                            _type InputType.Search

                            _placeholder "Search accounts"
                            _dataBind "collectionquery"
                            _dataAttr ("disabled", "$collectionstate != 'ready'")
                        }
                    }
                }

                label {
                    _class
                        "flex min-h-[var(--fve-control-min-height)] items-stretch overflow-hidden whitespace-nowrap rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] text-xs ring-1 ring-[var(--fve-border)]"

                    span {
                        _class
                            "flex items-center border-r border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] px-2 font-semibold text-[var(--fve-muted-text)]"

                        "As of"
                    }

                    input {
                        _type "date"
                        _name "asOf"
                        _ariaLabel "As of"
                        _value "2026-07-31"
                        _class "min-w-0 w-28 bg-transparent px-2 text-[var(--fve-text)] outline-none"
                    }
                }

                select {
                    _name "accountType"
                    _ariaLabel "Filter by account type"
                    _dataBind "collectiontype"
                    _dataAttr ("disabled", "$collectionstate != 'ready'")

                    _class
                        "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 text-sm ring-1 ring-[var(--fve-border)] outline-none focus:ring-2 focus:ring-[var(--fve-brand-ring)]"

                    option {
                        _value "all"
                        "All types"
                    }

                    option {
                        _value "asset"
                        "Asset"
                    }

                    option {
                        _value "liability"
                        "Liability"
                    }

                    option {
                        _value "equity"
                        "Equity"
                    }

                    option {
                        _value "revenue"
                        "Revenue"
                    }

                    option {
                        _value "expense"
                        "Expense"
                    }
                }

                button {
                    _type "button"
                    _dataOn ("click", "$collectionquery = ''; $collectiontype = 'all'")

                    _dataAttr (
                        "disabled",
                        "(!$collectionquery && $collectiontype == 'all') || $collectionstate != 'ready'"
                    )

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] disabled:opacity-50"

                    "Clear filters"
                }

                button {
                    _type "button"
                    _dataOn ("click", "$collectionstate = 'pending'; setTimeout(() => $collectionstate = 'ready', 350)")
                    _dataAttr ("disabled", "$collectionstate != 'ready'")

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] disabled:opacity-50"

                    "Refresh filters"
                }

                button {
                    _type "button"
                    _dataOn ("click", "$collectionstate = 'error'")
                    _dataAttr ("disabled", "$collectionstate != 'ready'")

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] disabled:opacity-50"

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

                _class
                    "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)]"

                span { "Filter options are temporarily unavailable." }

                button {
                    _type "button"
                    _dataOn ("click", "$collectionstate = 'ready'")

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 font-semibold ring-1 ring-[var(--fve-critical-ring)]"

                    "Retry filters"
                }
            }
        }

    let private collectionActions =
        a {
            _href (shellDestinationUrl LedgerCreateAccount)

            _class
                "inline-flex items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] px-3 py-2 text-sm font-medium text-white hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

            "New account"
        }

    let defaultPageHeader = fvePageHeader { _label "Project overview" }

    let defaultSection =
        Section.create (fveSectionHeader { _label "Item details" }) (p { "Project notes · Document" })
        |> Section.render

    let defaultPage =
        Page.create (fvePageHeader { _label "Project overview" }) (p { "Review items and recent activity." })
        |> Page.render

    let defaultCollection =
        Collection.create
            "Items"
            (ul {
                li { "Project notes" }
                li { "Team guide" }
            })
        |> Collection.render

    let defaultDetail =
        Detail.create "Project notes" [ p { "Document · Updated today" } ]
        |> Detail.render

    let pageHeaderLayoutExample =
        div {
            _dataSignals "{itemRefreshes: 0}"

            fvePageHeader {
                _label "Project notes"
                _description "Updated moments ago"

                fvePageHeaderActions {
                    fveButton {
                        _ariaLabel "Refresh"
                        _dataOn ("click", "$itemRefreshes++")

                        span {
                            _ariaHidden true
                            _class "inline-flex shrink-0"
                            refreshIcon
                        }
                    }
                }
            }

            output {
                _role "status"
                _class "sr-only"
                _dataText "'Refreshes: ' + $itemRefreshes"
                "Refreshes: 0"
            }
        }

    // Supply ordinary HTML for your table, filter controls, and actions.
    let collectionExample actions toolbar table =
        Collection.create "Accounts" table
        |> Collection.withDescription "Review balances and posting availability."
        |> Collection.withActions actions
        |> Collection.withToolbar toolbar
        |> Collection.render

    let private collectionSelectionForm =
        form {
            _class "grid gap-3"
            filteredAccountTable

            fveButton {
                _dataOn (
                    "click",
                    "document.getElementById('account-bulk-audit').textContent = 'Selected account IDs: ' + new FormData(evt.currentTarget.form).getAll('accountIds').join(', ')"
                )

                "Inspect selected accounts"
            }
        }

    let collectionPage =
        div {
            _dataSignals "{collectionquery: '', collectiontype: 'all', collectionstate: 'ready'}"

            _dataInit
                "(() => { const params = new URL(window.location.href).searchParams; $collectionquery = params.get('query') || ''; $collectiontype = params.get('accountType') || 'all' })()"

            _dataEffect
                "(() => { const url = new URL(window.location.href); $collectionquery ? url.searchParams.set('query', $collectionquery) : url.searchParams.delete('query'); $collectiontype != 'all' ? url.searchParams.set('accountType', $collectiontype) : url.searchParams.delete('accountType'); window.history.replaceState(window.history.state, '', url) })()"

            collectionExample collectionActions toolbar collectionSelectionForm

            p {
                _dataShow $"!({collectionHasMatches})"
                _role "status"

                _class
                    "rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"

                "No accounts match these filters. Clear filters to restore all accounts."
            }

            p {
                _role "status"
                _class "text-sm text-[var(--fve-muted-text)]"

                _dataText
                    "Array.from(document.querySelectorAll('#accounts-selection tbody tr')).filter(row => row.getClientRects().length).length + ' matching accounts'"

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
        div {
            _class "p-4 sm:p-6 lg:p-8"
            collectionPage
        }
        |> fullBleedThemedSurface

    // Supply your actions and rendered transaction content.
    let detailExample resolve accountsDestination actions transactions =
        Detail.create
            "Operating checking"
            [ Section.create
                  (fveSectionHeader { _label "Detail" })
                  (fveDescriptionList {
                      fveDescriptionListItem {
                          _label "Status"

                          fveBadge {
                              _color Color.Success
                              "Active"
                          }
                      }

                      fveDescriptionListItem {
                          _label "Type"
                          "Asset"
                      }

                      fveDescriptionListItem {
                          _label "Commodity"
                          "USD"
                      }

                      fveDescriptionListItem {
                          _label "Parent account"
                          "Current assets"
                      }

                      fveDescriptionListItem {
                          _label "Source"
                          "Created in Ledger"
                      }

                      fveDescriptionListItem {
                          _label "Month-end observed balance"
                          "Required"
                      }

                      fveDescriptionListItem {
                          _label "Balance"
                          "$38,442.11"
                      }

                      _columns DescriptionListColumns.Three
                  })
              |> Section.render
              fveSectionHeader {
                  _label "Transactions"
                  _description "Current assets · All accounts"

                  fveSectionHeaderActions {
                      a {
                          _href (resolve accountsDestination)

                          _class "text-sm font-medium text-[var(--fve-brand-text)] underline-offset-4 hover:underline"

                          "View all accounts"
                      }
                  }
              }
              |> fun header -> Section.create header transactions
              |> Section.withLabel "Recent transactions"
              |> Section.render ]
        |> Detail.withActions actions
        |> Detail.render

    let detailPage =
        let actions =
            div {
                _class "flex flex-wrap items-center gap-2"
                PageExamples.link (shellDestinationUrl (LedgerAccount operatingAccount.id)) "View account"

                fveDropdownMenu {
                    _id "components-detail-actions"
                    _ariaLabel "More actions"

                    fveDropdownMenuTrigger {
                        fveButton {
                            _iconOnly true
                            _variant ButtonVariant.Ghost

                            moreActionsIcon
                        }
                    }

                    yield!
                        (recordMenuItems
                            "account"
                            operatingAccount.id
                            (LedgerAccount operatingAccount.id)
                            shellDestinationUrl
                            (System.Text.Json.JsonSerializer.Serialize operatingAccount)
                         |> List.tail)
                }
            }

        detailExample shellDestinationUrl LedgerAccounts actions transactionTable
        |> recordActionFeedback "account"

    let detailPreview =
        div {
            _class "p-4 sm:p-6 lg:p-8"
            detailPage
        }
        |> fullBleedThemedSurface

    let private ledgerMark =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M4 3.75A1.75 1.75 0 0 1 5.75 2h8.5A1.75 1.75 0 0 1 16 3.75v12.5A1.75 1.75 0 0 1 14.25 18h-8.5A1.75 1.75 0 0 1 4 16.25V3.75Zm3 1.5A.75.75 0 0 0 7.75 6h4.5a.75.75 0 0 0 0-1.5h-4.5a.75.75 0 0 0-.75.75Zm0 4A.75.75 0 0 0 7.75 10h4.5a.75.75 0 0 0 0-1.5h-4.5a.75.75 0 0 0-.75.75Zm0 4a.75.75 0 0 0 .75.75h2.5a.75.75 0 0 0 0-1.5h-2.5a.75.75 0 0 0-.75.75Z"/></svg>"""

    let private treasuryMark =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M10.75 2.75a.75.75 0 0 0-1.5 0V4H7.5a2.5 2.5 0 0 0 0 5h5a1 1 0 1 1 0 2h-5a1 1 0 0 1-.93-.63.75.75 0 1 0-1.4.54A2.5 2.5 0 0 0 7.5 12.5h1.75v1.75a.75.75 0 0 0 1.5 0V12.5h1.75a2.5 2.5 0 0 0 0-5h-5a1 1 0 1 1 0-2h5a1 1 0 0 1 .93.63.75.75 0 1 0 1.4-.54A2.5 2.5 0 0 0 12.5 4h-1.75V2.75Z"/></svg>"""

    let private navigationGlyph =
        raw
            """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M3.5 4.75A1.25 1.25 0 0 1 4.75 3.5h10.5a1.25 1.25 0 0 1 1.25 1.25v10.5a1.25 1.25 0 0 1-1.25 1.25H4.75a1.25 1.25 0 0 1-1.25-1.25V4.75Z"/></svg>"""

    let private shellItem destination (label: string) =
        fveSideNavItem {
            _href (shellDestinationUrl destination)
            label
            fveSideNavLeading { navigationGlyph }
        }

    let private accountTitle id =
        if id = 2048 then
            "Operating checking"
        else
            rows
            |> List.tryFind (fun row -> row.id = id)
            |> Option.map _.name
            |> Option.defaultValue $"Account {id}"

    let private transactionTitle id =
        transactionRows |> List.find (fun row -> row.id = id) |> _.description

    let private ledgerBreadcrumbs current =
        let items =
            match current with
            | LedgerHome ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  } ]
            | LedgerAccounts ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerAccounts)
                      "Accounts"
                  } ]
            | LedgerAccount id ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerAccounts)
                      "Accounts"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl (LedgerAccount id))
                      accountTitle id
                  } ]
            | LedgerTransaction id ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl (LedgerTransaction id))
                      transactionTitle id
                  } ]
            | LedgerCreateAccount ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerAccounts)
                      "Accounts"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerCreateAccount)
                      "Create account"
                  } ]
            | LedgerReports ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerReports)
                      "Reports"
                  } ]
            | LedgerSettings ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerSettings)
                      "Settings"
                  } ]
            | _ ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl LedgerHome)
                      "Home"
                  } ]

        fveBreadcrumbs {
            _id "ledger-breadcrumbs"
            _ariaLabel "Breadcrumb"
            yield! items
        }

    let private treasuryBreadcrumbs current =
        let items =
            match current with
            | TreasuryHome ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  } ]
            | TreasuryPeriod period ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl (TreasuryPeriod period))
                      period
                  } ]
            | TreasuryTransactions ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl (TreasuryPeriod "July"))
                      "July 2026"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryTransactions)
                      "Transactions"
                  } ]
            | TreasuryPayees ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryPayees)
                      "Payees"
                  } ]
            | TreasuryAccounts ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  }
                  fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryAccounts)
                      "Accounts"
                  } ]
            | _ ->
                [ fveBreadcrumbItem {
                      _href (shellDestinationUrl TreasuryHome)
                      "Home"
                  } ]

        fveBreadcrumbs {
            _id "treasury-breadcrumbs"
            _ariaLabel "Breadcrumb"
            yield! items
        }

    type DefaultSideNavDestination =
        | Overview
        | Settings

    let defaultSideNavUrl =
        function
        | Overview -> "/components/side-nav"
        | Settings -> "/components/theming"

    let defaultSideNav =
        fveSideNav {
            _id "default-side-navigation"
            _ariaLabel "Primary navigation"

            fveSideNavContent {
                fveSideNavItem {
                    _href (defaultSideNavUrl Overview)
                    "Overview"
                }

                fveSideNavItem {
                    _href (defaultSideNavUrl Settings)
                    "Settings"
                }
            }
        }

    // Resolve your destination values to URLs.
    let breadcrumbsExample resolve home collection detail =
        fveBreadcrumbs {
            _id "item-breadcrumbs"
            _ariaLabel "Breadcrumb"

            fveBreadcrumbItem {
                _href (resolve home)
                "Home"
            }

            fveBreadcrumbItem {
                _href (resolve collection)
                "Items"
            }

            fveBreadcrumbItem {
                _href (resolve detail)
                "Project notes"
            }
        }

    let defaultSideNavPreview = div { defaultSideNav } |> themedSurface

    let sideNavStructureExample =
        fveSideNav {
            _id "structure-navigation"
            _ariaLabel "Navigation structure"
            _persistenceKey "structure-navigation-preferences"

            fveSideNavContent {
                fveSideNavItem {
                    _href "/components"
                    "Home"
                }

                fveSideNavSection {
                    _label "Workspace"

                    fveSideNavItem {
                        _href "/components/table#components-table-hierarchy"
                        "Items"
                    }

                    fveSideNavItem {
                        _href "/components/month-calendar"
                        "Calendar"
                    }
                }

                fveSideNavGroup {
                    _label "Documentation"

                    fveSideNavItem {
                        _href "/examples/specification"
                        "Specification"
                    }

                    fveSideNavGroup {
                        _label "Components"

                        fveSideNavItem {
                            _href "/components/button"
                            "Button"
                        }

                        fveSideNavItem {
                            _href "/components/table"
                            "Table"
                        }

                        _id "structure-components"
                        _expanded true
                    }

                    _id "structure-documentation"
                    _expanded true
                }
            }

            _value "/components/table#components-table-hierarchy"
        }

    let sideNavStructurePreview = div { sideNavStructureExample } |> themedSurface

    let sideNavRowIcon data =
        svg {
            _viewBox "0 0 24 24"
            _fill "none"
            _stroke "currentColor"
            _strokeWidth "1.5"

            path {
                _strokeLinecap "round"
                _strokeLinejoin "round"
                _d data
            }
        }

    let sideNavSlotsExample =
        let workspaceMenu =
            fveDropdownMenu {
                _id "slot-workspace"
                _ariaLabel "Choose workspace"
                fveDropdownMenuTrigger { fveButton { "Choose workspace" } }
                _alignment DropdownMenuAlignment.Start

                fveDropdownMenuItem {
                    _href "/components/side-nav?workspace=alex"
                    "Alex"
                }

                fveDropdownMenuItem {
                    _href "/components/side-nav?workspace=jordan"
                    "Jordan"
                }

                fveDropdownMenuSeparator { () }

                fveDropdownMenuItem {
                    _href "/examples/application/profile/organizations"
                    "Organizations"
                }
            }

        fveSideNav {
            _id "slot-navigation"
            _ariaLabel "Application navigation"

            fveSideNavHeader {
                _label "Workspace"

                a {
                    _href "/components"
                    _class "flex items-center gap-3 text-sm font-semibold"
                    "Workspace"
                }
            }

            fveSideNavContext {
                fveSideNavRow {
                    _label "Alex"
                    workspaceMenu

                    fveSideNavLeading {
                        sideNavRowIcon "M3.75 3.75h6v6h-6zm10.5 0h6v6h-6zm-10.5 10.5h6v6h-6zm10.5 0h6v6h-6z"
                    }

                    fveSideNavTrailing {
                        fveBadge {
                            _ariaLabel "2 workspaces"
                            "2"
                        }
                    }
                }
            }

            fveSideNavContent {
                fveSideNavItem {
                    _href "/components"
                    "Home"
                }

                fveSideNavSection {
                    _label "Workspace"

                    fveSideNavItem {
                        _href "/components/table#components-table-hierarchy"
                        "Items"
                    }

                    fveSideNavItem {
                        _href "/components/month-calendar"
                        "Calendar"
                    }
                }
            }

            _value "/components/table#components-table-hierarchy"

            fveSideNavFooter {
                fveSideNavRow {
                    _href "/examples/application/settings"
                    "Settings"

                    fveSideNavLeading {
                        sideNavRowIcon
                            "M9.594 3.94c.09-.542.56-.94 1.11-.94h2.592c.55 0 1.02.398 1.11.94l.213 1.281a6.52 6.52 0 0 1 1.94 1.12l1.217-.456a1.125 1.125 0 0 1 1.37.49l1.296 2.247a1.125 1.125 0 0 1-.26 1.431l-1.003.827a6.759 6.759 0 0 1 0 2.24l1.003.827c.424.35.534.954.26 1.43l-1.296 2.247a1.125 1.125 0 0 1-1.37.491l-1.217-.456a6.57 6.57 0 0 1-1.94 1.12l-.213 1.281c-.09.543-.56.94-1.11.94h-2.592c-.55 0-1.02-.397-1.11-.94l-.213-1.281a6.52 6.52 0 0 1-1.94-1.12l-1.217.456a1.125 1.125 0 0 1-1.369-.49l-1.296-2.247a1.125 1.125 0 0 1 .26-1.431l1.003-.827a6.932 6.932 0 0 1 0-2.24l-1.003-.827a1.125 1.125 0 0 1-.26-1.43l1.296-2.247a1.125 1.125 0 0 1 1.37-.491l1.216.456a6.52 6.52 0 0 1 1.94-1.12l.213-1.281ZM15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z"
                    }
                }

                fveSeparator { () }

                fveSideNavRow {
                    _href "/examples/application/profile"
                    "Andrew Meier"

                    fveSideNavLeading {
                        fveAvatar {
                            _ariaLabel "Andrew Meier"
                            _ariaHidden true
                            "AM"
                        }
                    }

                    _description "andrew@meiermade.com"
                }
            }
        }

    let sideNavSlotsPreview =
        div {
            _class (
                ComponentsTheme.sky
                |> ComponentsTheme.withDensity Density.Compact
                |> ComponentsTheme.className
            )

            div {
                _class "h-96 overflow-hidden rounded-xl ring-1 ring-[var(--fve-border)]"
                sideNavSlotsExample
            }
        }

    let breadcrumbsPreview =
        div { breadcrumbsExample id "/components" "/components/table" "/components/card" }
        |> themedSurface

    let breadcrumbTrail =
        [ fveBreadcrumbItem {
              _href "/"
              "Home"
          }
          fveBreadcrumbItem {
              _href "/components"
              "Components"
          }
          fveBreadcrumbItem {
              _href "/components/side-nav"
              "Navigation"
          }
          fveBreadcrumbItem {
              _href "/components/breadcrumbs"
              "Breadcrumbs"
          } ]

    let middleBreadcrumbsExample =
        fveBreadcrumbs {
            _id "middle-breadcrumbs"
            _ariaLabel "Middle overflow breadcrumb"
            _class "max-w-xs"
            yield! breadcrumbTrail
        }

    let visibleBreadcrumbsExample =
        fveBreadcrumbs {
            _id "visible-breadcrumbs"
            _ariaLabel "Available space breadcrumb"
            yield! breadcrumbTrail
        }

    let private ledgerNavigation showMobileWorkspace current =
        let navigationCurrent =
            match current with
            | LedgerAccount _
            | LedgerCreateAccount -> LedgerAccounts
            | LedgerHome
            | LedgerAccounts
            | LedgerReports
            | LedgerSettings -> current
            | _ -> LedgerHome

        let workspace =
            div {
                p {
                    _class "text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"
                    "Workspace"
                }

                p {
                    _class "mt-1 truncate text-sm font-semibold"
                    "Meier Made"
                }
            }

        let compactBrand =
            a {
                _href (shellDestinationUrl LedgerHome)
                _ariaLabel "Ledger home"

                _class
                    "flex size-8 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"

                ledgerMark
            }

        let navigationHeader =
            fveSideNavHeader {
                _label "Ledger"

                a {
                    _href (shellDestinationUrl LedgerHome)

                    _class
                        "flex min-w-0 items-center gap-3 rounded-[var(--fve-radius-control)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"

                    span {
                        _ariaHidden true

                        _class
                            "flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white"

                        ledgerMark
                    }

                    strong {
                        _class "truncate text-base font-semibold"
                        "Ledger"
                    }
                }

                fveSideNavHeaderCompact { compactBrand }
            }

        let accountAction =
            fveButton {
                _variant ButtonVariant.Ghost
                _size Size.Small
                _ariaLabel "Pin Accounts"

                _dataOn (
                    "click",
                    "el.setAttribute('aria-pressed', el.getAttribute('aria-pressed') != 'true' ? 'true' : 'false')"
                )

                _ariaPressed false

                span {
                    _ariaHidden true
                    _class "inline-flex shrink-0"

                    raw
                        """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path d="M7 2.75A.75.75 0 0 1 7.75 2h4.5a.75.75 0 0 1 .75.75v4.69l1.78 1.78a.75.75 0 0 1-.53 1.28h-3.5v6.75a.75.75 0 0 1-1.5 0V10.5h-3.5a.75.75 0 0 1-.53-1.28L7 7.44V2.75Z"/></svg>"""
                }
            }

        fveSideNav {
            _id "ledger-side-navigation"
            _ariaLabel "Ledger primary navigation"
            Html._width SideNavWidth.Standard

            match current with
            | LedgerTransaction _ -> ()
            | _ -> _value (shellDestinationUrl navigationCurrent)

            navigationHeader

            fveSideNavContent {
                fveSideNavSection {
                    _label "Manage"
                    shellItem LedgerHome "Dashboard"

                    fveSideNavItem {
                        _href (shellDestinationUrl LedgerAccounts)
                        fveSideNavLeading { navigationGlyph }

                        fveSideNavBadge {
                            fveBadge {
                                _color Color.Neutral
                                "6"
                            }
                        }

                        fveSideNavAction { accountAction }
                        "Accounts"
                    }
                }

                fveSideNavSection {
                    _label "Analyze"

                    fveSideNavGroup {
                        _label "Reporting"
                        _expanded true
                        fveSideNavLeading { navigationGlyph }
                        shellItem LedgerReports "Reports"
                    }
                }

                fveSideNavSection {
                    _label "Configure"
                    shellItem LedgerSettings "Settings"
                }
            }

            fveSideNavContext {
                div {
                    _class "px-4 py-3"
                    workspace
                }
            }

            if showMobileWorkspace then
                fveSideNavMobileContext { workspace }

            fveSideNavFooter {
                fveSideNavRow {
                    _href (shellDestinationUrl LedgerSettings)

                    fveSideNavLeading {
                        span {
                            _class
                                "flex size-5 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs text-[var(--fve-brand-text)]"

                            "AM"
                        }
                    }

                    "Andy Meier"
                }
            }

            fveSideNavCompactFooter {
                a {
                    _href (shellDestinationUrl LedgerSettings)
                    _ariaLabel "Andy Meier profile"

                    _class
                        "flex size-8 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs font-semibold text-[var(--fve-brand-text)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--fve-brand-ring)]"

                    "AM"
                }
            }
        }

    let groupedLedgerNavigation = ledgerNavigation true LedgerAccounts

    let nestedSideNav =
        let action =
            fveButton {
                _variant ButtonVariant.Ghost
                _ariaLabel "Pin items"
                _ariaPressed false

                _dataOn (
                    "click",
                    "el.setAttribute('aria-pressed', el.getAttribute('aria-pressed') !== 'true' ? 'true' : 'false')"
                )

                span {
                    _ariaHidden true
                    _class "inline-flex shrink-0"

                    raw
                        """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path d="M7 2.75A.75.75 0 0 1 7.75 2h4.5a.75.75 0 0 1 .75.75v4.69l1.78 1.78a.75.75 0 0 1-.53 1.28h-3.5v6.75a.75.75 0 0 1-1.5 0V10.5h-3.5a.75.75 0 0 1-.53-1.28L7 7.44V2.75Z"/></svg>"""
                }
            }

        fveSideNav {
            _id "nested-item-navigation"
            _ariaLabel "Workspace navigation"
            fveSideNavHeader { _label "Workspace" }

            fveSideNavContent {
                fveSideNavItem {
                    _href "/components"
                    "Home"
                }

                fveSideNavItem {
                    _href "/components/table#components-table-hierarchy"
                    "Items"
                    fveSideNavBadge { fveBadge { "24" } }
                    fveSideNavAction { action }
                }

                fveSideNavGroup {
                    _label "Documentation"

                    fveSideNavItem {
                        _href "/components/button"
                        "Button"
                    }

                    fveSideNavGroup {
                        _label "Content"

                        fveSideNavItem {
                            _href "/components/table"
                            "Table"
                        }

                        fveSideNavItem {
                            _href "/components/card"
                            "Card"
                        }

                        _id "nested-content"
                    }

                    _id "nested-documentation"
                    _expanded true
                }
            }

            _value "/components/table#components-table-hierarchy"
        }

    let sideNavigationPreview =
        div {
            _class "h-[36rem] overflow-hidden rounded-xl ring-1 ring-[var(--fve-border)]"
            nestedSideNav
        }
        |> themedSurface

    let private ledgerTitle =
        function
        | LedgerHome -> "Dashboard"
        | LedgerAccounts -> "Accounts"
        | LedgerAccount id -> accountTitle id
        | LedgerTransaction id -> transactionTitle id
        | LedgerCreateAccount -> "Create account"
        | LedgerReports -> "Reports"
        | LedgerSettings -> "Settings"
        | _ -> "Ledger"

    let private refreshBalancesAction =
        fveButton {
            _color Color.Primary
            _variant ButtonVariant.Solid
            _ariaLabel "Refresh balances"
            _dataOn ("click", "$ledgerRefreshes++")

            span {
                _ariaHidden true
                _class "inline-flex shrink-0"
                refreshIcon
            }
        }

    let private pageAction destination (label: string) =
        a {
            _href (shellDestinationUrl destination)

            _class
                "inline-flex min-h-8 items-center px-3 text-sm font-medium text-[var(--fve-brand-text)] underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

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

                fveDropdownMenu {
                    _id (id + "-wide")
                    _ariaLabel "More actions"

                    fveDropdownMenuTrigger {
                        fveButton {
                            _iconOnly true
                            _variant ButtonVariant.Ghost

                            moreActionsIcon
                        }
                    }

                    fveDropdownMenuItem {
                        _href (shellDestinationUrl LedgerSettings)
                        "Account settings"
                    }
                }
            }

            div {
                _class "@min-[400px]:hidden"

                fveDropdownMenu {
                    _id (id + "-compact")
                    _ariaLabel "More actions"

                    fveDropdownMenuTrigger {
                        fveButton {
                            _iconOnly true
                            _variant ButtonVariant.Ghost

                            moreActionsIcon
                        }
                    }

                    fveDropdownMenuItem {
                        _href (shellDestinationUrl LedgerReports)
                        "View reports"
                    }

                    fveDropdownMenuItem {
                        _href (shellDestinationUrl LedgerSettings)
                        "Account settings"
                    }
                }
            }
        }

    // Breadcrumbs are optional content, not a built-in heading.
    let pageTopBarExample (breadcrumbs: HtmlElement) =
        fvePageTopBar {
            div {
                _class "flex min-w-0 flex-1 items-center"
                breadcrumbs
            }
        }

    let renderPageTopBar breadcrumbs = pageTopBarExample breadcrumbs

    let private pageTopBar breadcrumbs = pageTopBarExample breadcrumbs

    let pageTopBarPreview =
        div {
            fveBreadcrumbs {
                _id "page-bar-breadcrumbs"
                _ariaLabel "Breadcrumb"

                fveBreadcrumbItem {
                    _href "/components"
                    "Home"
                }

                fveBreadcrumbItem {
                    _href "/components/table"
                    "Items"
                }

                fveBreadcrumbItem {
                    _href "/components/card"
                    "Project notes"
                }
            }
            |> renderPageTopBar
        }
        |> themedSurface

    let shellTopBarExample =
        div {
            _class "@container/fve-shell"

            fvePageTopBar {
                fvePageTopBarBrand {
                    strong {
                        _class "text-base font-semibold"
                        "Workspace"
                    }
                }

                fveBreadcrumbs {
                    _id "shell-bar-breadcrumbs"
                    _ariaLabel "Shell breadcrumb"

                    fveBreadcrumbItem {
                        _href "/components"
                        "Home"
                    }

                    fveBreadcrumbItem {
                        _href "/components/table"
                        "Items"
                    }
                }

                fvePageTopBarActions {
                    a {
                        _href "/examples/application/profile"
                        _class "whitespace-nowrap text-sm font-medium"
                        "View profile"
                    }
                }
            }

            div {
                _class "flex min-h-32"

                div {
                    _class "hidden w-60 shrink-0 @3xl/fve-shell:block"

                    fveSideNav {
                        _id "shell-top-bar-navigation"
                        _ariaLabel "Workspace"

                        fveSideNavContent {
                            fveSideNavItem {
                                _href "/components"
                                "Home"
                            }

                            fveSideNavItem {
                                _href "/components/table"
                                "Items"
                            }
                        }

                        Html._width SideNavWidth.Standard
                    }
                }

                div {
                    _class "min-w-0 flex-1 px-4 py-6 sm:px-6 lg:px-8"

                    p {
                        _class "text-sm text-[var(--fve-muted-text)]"
                        "Page region"
                    }
                }
            }
        }

    let accountPageHeader =
        fvePageHeader {
            _label "Account 2048"
            _description "Operating checking · Updated moments ago"

            fvePageHeaderActions {
                div {
                    _class "flex flex-wrap items-center gap-2"
                    refreshBalancesAction
                    pageAction LedgerReports "View reports"
                }
            }
        }

    let pageHeaderPreview =
        div {
            _dataSignals "{ledgerRefreshes: 0}"
            accountPageHeader

            output {
                _class "sr-only"
                _role "status"
                _dataText "'Balance refreshes: ' + $ledgerRefreshes"
                "Balance refreshes: 0"
            }
        }
        |> themedSurface

    let private treasuryNavigation current =
        let navigationCurrent =
            match current with
            | TreasuryPeriod _ -> TreasuryTransactions
            | TreasuryHome
            | TreasuryTransactions
            | TreasuryPayees
            | TreasuryAccounts -> current
            | _ -> TreasuryHome

        let workspace =
            div {
                p {
                    _class "text-xs font-semibold uppercase tracking-wide text-[var(--fve-muted-text)]"
                    "Workspace"
                }

                p {
                    _class "mt-1 truncate text-sm font-semibold"
                    "Meier Made"
                }
            }

        let navigationHeader =
            fveSideNavHeader {
                _label "Treasury"

                div {
                    _class "flex min-w-0 items-center gap-3"

                    span {
                        _ariaHidden true

                        _class
                            "flex size-8 shrink-0 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] text-white"

                        treasuryMark
                    }

                    strong {
                        _class "truncate text-base font-semibold"
                        "Treasury"
                    }
                }
            }

        fveSideNav {
            _id "treasury-side-navigation"
            _ariaLabel "Treasury primary navigation"
            navigationHeader

            fveSideNavContent {
                shellItem TreasuryHome "Overview"
                shellItem TreasuryTransactions "Transactions"
                shellItem TreasuryPayees "Payees"
                shellItem TreasuryAccounts "Accounts"
            }

            _value (shellDestinationUrl navigationCurrent)
            Html._width SideNavWidth.Narrow

            fveSideNavContext {
                div {
                    _class "px-4 py-3"
                    workspace
                }
            }

            fveSideNavMobileContext { workspace }

            fveSideNavFooter {
                div {
                    _class "flex min-w-0 items-center gap-3 px-3 py-2"

                    span {
                        _ariaHidden true

                        _class
                            "flex size-8 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs text-[var(--fve-brand-text)]"

                        "AM"
                    }

                    span {
                        _class "min-w-0 truncate text-sm font-semibold"
                        "Andy Meier"
                    }
                }
            }
        }

    let private upcomingPayments =
        let actions =
            div {
                _class "flex flex-wrap items-center gap-2"
                pageAction TreasuryPayees "View payees"

                fveDropdownMenu {
                    _id "upcoming-payment-actions"
                    _ariaLabel "More actions"

                    fveDropdownMenuTrigger {
                        fveButton {
                            _iconOnly true
                            _variant ButtonVariant.Ghost

                            moreActionsIcon
                        }
                    }

                    fveDropdownMenuItem {
                        _href (shellDestinationUrl TreasuryAccounts)
                        "View accounts"
                    }
                }
            }

        let header =
            fveSectionHeader {
                _label "Upcoming transactions"
                _description "Payments expected before the current period closes."
                fveSectionHeaderActions { actions }
            }

        Section.create
            header
            (ul {
                _role "list"
                _class "divide-y divide-[var(--fve-border)]"

                li {
                    _class "flex items-center justify-between gap-4 py-3 text-sm"
                    span { "Aug 15 · Payroll" }

                    fveBadge {
                        _color Color.Success
                        "Ready"
                    }
                }

                li {
                    _class "flex items-center justify-between gap-4 py-3 text-sm"
                    span { "Aug 18 · Cloud hosting" }

                    fveBadge {
                        _color Color.Info
                        "Scheduled"
                    }
                }
            })
        |> Section.render

    let private completedPayments =
        Section.create
            (fveSectionHeader { _label "Completed transactions" })
            (div {
                _class "flex items-center justify-between gap-4 py-3 text-sm"
                span { "Jul 28 · Northwind payment" }

                strong {
                    _class "font-semibold"
                    "$4,800.00"
                }
            })
        |> Section.render

    let private transactionTabs =
        fveTabs {
            _id "treasury-transactions-tabs"
            _ariaLabel "Transaction views"

            fveTabsItem {
                _value "upcoming"
                fveTabsTrigger { "Upcoming" }
                upcomingPayments
            }

            fveTabsItem {
                _value "completed"
                fveTabsTrigger { "Completed" }
                completedPayments
            }

            _variant TabsVariant.Underlined
        }

    // Supply the top bar and tab content; Page owns their layout.
    let transactionsPage topBar tabs =
        fvePageHeader {
            _label "Transactions"
            _description "Review scheduled and completed cash activity."
        }
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
                _ariaLabel "Workflow canvas"
                _tabindex 0
                _class "h-full overflow-auto"
                _dataSignals "{canvasZoom: 1}"

                div {
                    _class "flex items-center gap-2 p-3"

                    fveButton {
                        _dataOn ("click", "$canvasZoom = Math.min(2, $canvasZoom + 0.25)")
                        "Zoom in"
                    }

                    fveButton {
                        _dataOn ("click", "$canvasZoom = Math.max(0.5, $canvasZoom - 0.25)")
                        "Zoom out"
                    }

                    output {
                        _ariaLabel "Zoom level"
                        _dataText "Math.round($canvasZoom * 100) + '%'"
                        "100%"
                    }
                }

                div {
                    _style "width:1000px;height:600px;transform-origin:top left"
                    _dataAttr ("style", "'width:' + (1000 * $canvasZoom) + 'px;height:' + (600 * $canvasZoom) + 'px'")

                    raw
                        """<svg role="img" aria-label="Workflow from draft through review to publish" viewBox="0 0 1000 600" width="100%" height="100%"><path d="M260 230H390M610 230H740" stroke="var(--fve-border)" stroke-width="3"/><g fill="var(--fve-surface)" stroke="var(--fve-border)"><rect x="40" y="190" width="220" height="80" rx="8"/><rect x="390" y="190" width="220" height="80" rx="8"/><rect x="740" y="190" width="220" height="80" rx="8"/></g><g fill="var(--fve-text)" font-size="16" text-anchor="middle"><text x="150" y="236">Draft</text><text x="500" y="236">Review</text><text x="850" y="236">Publish</text></g></svg>"""
                }
            }

        Page.create (fvePageHeader { _label "Workflow" }) graph
        |> Page.withBodyLayout PageBodyLayout.Canvas

    let treasuryTransactionsPage =
        transactionsPage (pageTopBar (treasuryBreadcrumbs TreasuryTransactions)) transactionTabs

    let private accountWorkspaceUrl (workspace: AccountWorkspace) =
        let encode (value: string) = Uri.EscapeDataString value

        let columns =
            workspace.visibleColumns |> Set.toList |> List.sort |> String.concat ","

        $"/components/page-examples/account-management?destination=ledger-accounts&query={encode workspace.searchQuery}&accountType={encode workspace.filterType}&sort={workspace.sortColumn}&direction={workspace.sortDirection}&page={workspace.page}&columns={columns}"

    let private accountWorkspacePatch url =
        let target = System.Text.Json.JsonSerializer.Serialize url
        $"window.history.pushState(null, '', {target}); @get({target}.replace('/components/page-examples/account-management?', '/components/page-examples/account-management/fixture?'))"

    let private accountDataTable workspace accountRows =
        let sort column label alignment =
            let active = workspace.sortColumn = column

            let nextDirection =
                if active && workspace.sortDirection = "asc" then
                    "desc"
                else
                    "asc"

            let destination =
                accountWorkspaceUrl
                    { workspace with
                        sortColumn = column
                        sortDirection = nextDirection
                        page = 1 }

            let direction =
                if not active then None
                elif workspace.sortDirection = "asc" then Some "ascending"
                else Some "descending"

            RecordTable.sortedHeading label destination direction alignment []

        let selection =
            RecordSelection.view
                "account-data-selection"
                "accountIds"
                []
                (accountRows |> List.map (fun (row: AccountRow) -> string row.id, row.name))

        let headings =
            [ selection.heading
              sort "name" "Account" ""
              if workspace.visibleColumns.Contains "type" then
                  fveTableHead { "Type" }
              if workspace.visibleColumns.Contains "commodity" then
                  fveTableHead { "Commodity" }
              if workspace.visibleColumns.Contains "balance" then
                  sort "balance" "Balance" "text-right"
              if workspace.visibleColumns.Contains "total" then
                  fveTableHead {
                      _class "text-right"
                      "Total balance (USD) · Current"
                  }
              fveTableHead {
                  _class "fve-table-actions text-right"
                  _attr ("data-fve-sticky-cell", "true")

                  span {
                      _class "sr-only"
                      "Actions"
                  }
              } ]

        let body =
            [ for row in accountRows do
                  fveTableRow {
                      yield! selection.rowAttributes (string row.id)
                      selection.cell (string row.id) row.name

                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary

                          a {
                              _href (shellDestinationUrl (LedgerAccount row.id))
                              _class "font-medium text-[var(--fve-brand-text)]"
                              row.name
                          }
                      }

                      if workspace.visibleColumns.Contains "type" then
                          fveTableCell {
                              _label "Type"
                              row.accountType
                          }

                      if workspace.visibleColumns.Contains "commodity" then
                          fveTableCell {
                              _label "Commodity"
                              row.commodity
                          }

                      if workspace.visibleColumns.Contains "balance" then
                          fveTableCell {
                              _label "Balance"
                              _class "text-right"
                              money row.balance
                          }

                      if workspace.visibleColumns.Contains "total" then
                          fveTableCell {
                              _label "Total balance (USD) · Current"
                              _class "text-right"
                              money row.totalBalance
                          }

                      fveTableCell {
                          _class "fve-table-actions text-right"
                          _attr ("data-fve-sticky-cell", "true")
                          _mobile Mobile.Actions

                          fveDropdownMenu {
                              _id $"data-account-{row.id}-actions"
                              _ariaLabel $"More actions for {row.name}"

                              fveDropdownMenuTrigger {
                                  fveButton {
                                      _iconOnly true
                                      _variant ButtonVariant.Ghost

                                      moreActionsIcon
                                  }
                              }

                              yield!
                                  recordMenuItems
                                      "account"
                                      row.id
                                      (LedgerAccount row.id)
                                      shellDestinationUrl
                                      (System.Text.Json.JsonSerializer.Serialize row)
                          }
                      }
                  } ]

        RecordTable.render
            "Account data"
            ([ _density Density.Compact; _mobileLayout MobileLayout.Records ]
             @ selection.attributes)
            headings
            body
            [ selection.status ]

    let private ledgerPage workspace current =
        let page (actions: HtmlElement) subtitle (content: HtmlElement) =
            fvePageHeader {
                _label (ledgerTitle current)
                _description subtitle
                fvePageHeaderActions { actions }
            }
            |> fun header ->
                Page.create
                    header
                    (div {
                        _class "min-w-0 px-4 pb-4 sm:px-6 sm:pb-6 lg:px-8 lg:pb-8"
                        content
                    })
            |> Page.withWidth PageWidth.Wide
            |> Page.withBodyLayout PageBodyLayout.FullBleed

        match current with
        | LedgerAccounts ->
            let actions =
                div {
                    _class "flex flex-wrap items-center gap-2"

                    a {
                        _href (shellDestinationUrl LedgerCreateAccount)

                        _class
                            "inline-flex min-h-8 items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] px-3 py-1.5 text-sm font-medium leading-5 text-white hover:bg-[var(--fve-brand-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                        "Create"
                    }

                    fveDropdownMenu {
                        _id "ledger-accounts-page-actions"
                        _ariaLabel "More actions"

                        fveDropdownMenuTrigger {
                            fveButton {
                                _iconOnly true
                                _variant ButtonVariant.Ghost

                                moreActionsIcon
                            }
                        }

                        fveDropdownMenuItem {
                            _href (shellDestinationUrl LedgerSettings)
                            "Account settings"
                        }
                    }
                }

            let filtered =
                rows
                |> List.filter (fun row ->
                    row.name.Contains(workspace.searchQuery, StringComparison.OrdinalIgnoreCase)
                    && (workspace.filterType = "all"
                        || row.accountType.Equals(workspace.filterType, StringComparison.OrdinalIgnoreCase)))

            let sorted =
                match workspace.sortColumn, workspace.sortDirection with
                | "balance", "desc" -> filtered |> List.sortByDescending _.balance
                | "balance", _ -> filtered |> List.sortBy _.balance
                | _, "desc" -> filtered |> List.sortByDescending _.name
                | _ -> filtered |> List.sortBy _.name

            let pageSize = 3
            let pageCount = max 1 ((sorted.Length + pageSize - 1) / pageSize)
            let currentPage = min workspace.page pageCount

            let displayed =
                sorted |> List.skip ((currentPage - 1) * pageSize) |> List.truncate pageSize

            let visibleColumns =
                workspace.visibleColumns |> Set.toList |> List.sort |> String.concat ","

            let columnItems: HtmlElement list =
                [ for key, label in
                      [ "type", "Type"
                        "commodity", "Commodity"
                        "balance", "Balance"
                        "total", "Total balance" ] do
                      let nextColumns =
                          if workspace.visibleColumns.Contains key then
                              workspace.visibleColumns.Remove key
                          else
                              workspace.visibleColumns.Add key

                      let destination =
                          accountWorkspaceUrl
                              { workspace with
                                  visibleColumns = nextColumns
                                  page = 1 }

                      fveDropdownMenuItem {
                          _dataOn ("click", (accountWorkspacePatch destination))
                          _role "menuitemcheckbox"
                          label
                          _ariaChecked (workspace.visibleColumns.Contains key)
                      } ]

            let columnMenu =
                fveDropdownMenu {
                    _id "account-columns"
                    _ariaLabel "Choose columns"
                    fveDropdownMenuTrigger { fveButton { "Columns" } }
                    yield! columnItems
                }

            let controls =
                div {
                    _class "flex flex-wrap items-end gap-2"

                    form {
                        _method "get"
                        _action "/components/page-examples/account-management"
                        _class "flex min-w-0 flex-1 flex-wrap items-end gap-2"

                        input {
                            _type "hidden"
                            _name "destination"
                            _value "ledger-accounts"
                        }

                        input {
                            _type "hidden"
                            _name "sort"
                            _value workspace.sortColumn
                        }

                        input {
                            _type "hidden"
                            _name "direction"
                            _value workspace.sortDirection
                        }

                        input {
                            _type "hidden"
                            _name "columns"
                            _value visibleColumns
                        }

                        div {
                            _class "min-w-0 basis-full sm:min-w-48 sm:flex-1 sm:basis-auto"

                            fveField {
                                fveLabel {
                                    _for "query"
                                    "Search accounts"
                                }

                                fveInput {
                                    _id "query"
                                    _name "query"
                                    _type InputType.Search
                                    _value workspace.searchQuery
                                }
                            }
                        }

                        div {
                            _class "min-w-0 flex-[1_1_10rem] sm:flex-none"

                            fveField {
                                fveLabel {
                                    _for "accountType"
                                    "Filter by account type"
                                }

                                fveSelect {
                                    _id "accountType"
                                    _name "accountType"
                                    _value workspace.filterType

                                    fveSelectContent {
                                        for value, label, disabled in
                                            ([ for value, label in
                                                   [ "all", "All types"
                                                     "asset", "Asset"
                                                     "liability", "Liability"
                                                     "equity", "Equity"
                                                     "revenue", "Revenue"
                                                     "expense", "Expense" ] -> (value, label, false) ]) do
                                            fveSelectItem {
                                                _value value
                                                _label label
                                                _disabled disabled
                                            }
                                    }
                                }
                            }
                        }

                        fveButton {
                            _type "submit"
                            "Apply filters"
                        }
                    }

                    columnMenu

                    if workspace.searchQuery <> "" || workspace.filterType <> "all" then
                        a {
                            _href (
                                accountWorkspaceUrl
                                    { defaultAccountWorkspace with
                                        visibleColumns = workspace.visibleColumns }
                            )

                            _class
                                "inline-flex min-h-10 items-center px-2 text-sm font-medium text-[var(--fve-brand-text)]"

                            "Clear filters"
                        }
                }

            let dataTable =
                if filtered.IsEmpty then
                    fveEmptyState {
                        _label "No matching accounts"
                        _description "Change the search or clear your filters."
                    }
                else
                    accountDataTable workspace displayed

            let pagination =
                if filtered.IsEmpty then
                    div { }
                else
                    let pageUrl number =
                        accountWorkspaceUrl { workspace with page = number }

                    fvePagination {
                        _ariaLabel "Account pages"

                        fvePaginationPrevious {
                            if currentPage > 1 then
                                _href (pageUrl (currentPage - 1))
                        }

                        fvePaginationContent {
                            for number in 1..pageCount do
                                fvePaginationItem {
                                    _page number

                                    if number = currentPage then
                                        _ariaCurrent "page"
                                    else
                                        _href (pageUrl number)
                                }
                        }

                        fvePaginationNext {
                            if currentPage < pageCount then
                                _href (pageUrl (currentPage + 1))
                        }

                        fvePaginationSummary {
                            $"Showing {(currentPage - 1) * pageSize + 1}–{min (currentPage * pageSize) filtered.Length} of {filtered.Length} accounts"
                        }
                    }

            let content =
                div {
                    _class "grid gap-4"
                    dataTable |> recordActionFeedback "account"
                    pagination
                }
                |> Collection.create "Accounts"
                |> Collection.withVisuallyHiddenTitle
                |> Collection.withToolbar controls
                |> Collection.render

            page actions "Chart of accounts · Current valuation" content

        | LedgerAccount accountId ->
            let account =
                rows
                |> List.tryFind (fun row -> row.id = accountId)
                |> Option.defaultValue operatingAccount

            let actions = accountPageActions $"ledger-account-{accountId}-page-actions"

            let content =
                Detail.create
                    (ledgerTitle current)
                    [ Section.create
                          (fveSectionHeader { _label "Detail" })
                          (fveDescriptionList {
                              fveDescriptionListItem {
                                  _label "Status"

                                  fveBadge {
                                      _color Color.Success
                                      "Active"
                                  }
                              }

                              fveDescriptionListItem {
                                  _label "Type"
                                  account.accountType
                              }

                              fveDescriptionListItem {
                                  _label "Commodity"
                                  account.commodity
                              }

                              fveDescriptionListItem {
                                  _label "Parent account"

                                  if accountId = operatingAccount.id then
                                      "Current assets"
                                  else
                                      "None"
                              }

                              fveDescriptionListItem {
                                  _label "Source"
                                  "Created in Ledger"
                              }

                              fveDescriptionListItem {
                                  _label "Month-end observed balance"

                                  if accountId = operatingAccount.id then
                                      "Required"
                                  else
                                      "Not required"
                              }

                              fveDescriptionListItem {
                                  _label "Balance"
                                  money account.balance
                              }

                              _columns DescriptionListColumns.Three
                          })
                      |> Section.render
                      fveSectionHeader {
                          _label "Transactions"
                          _description "Current assets · All accounts"
                      }
                      |> fun header -> Section.create header (transactionTableFor LedgerTransaction shellDestinationUrl)
                      |> Section.withLabel "Account transactions"
                      |> Section.render ]
                |> Detail.withVisuallyHiddenTitle
                |> Detail.render

            page actions "Asset · USD · Updated moments ago" content

        | LedgerTransaction transactionId ->
            let row = transactionRows |> List.find (fun row -> row.id = transactionId)
            let actions = pageAction LedgerAccounts "View accounts"

            let content =
                Section.create
                    (fveSectionHeader { _label "Detail" })
                    (fveDescriptionList {
                        fveDescriptionListItem {
                            _label "Transaction ID"
                            string row.id
                        }

                        fveDescriptionListItem {
                            _label "Date"
                            row.date
                        }

                        fveDescriptionListItem {
                            _label "Status"
                            string row.status
                        }

                        fveDescriptionListItem {
                            _label "Accounts"
                            row.accounts
                        }

                        fveDescriptionListItem {
                            _label "Amount"
                            money row.amount
                        }
                    })
                |> Section.render

            page actions "Transaction details" content

        | LedgerCreateAccount ->
            let actions = empty

            let content =
                form {
                    _method "post"
                    _action "/components/page-examples/account-management/create"
                    _class "grid max-w-xl gap-5"

                    fveField {
                        fveLabel {
                            _for "name"
                            "Account name"
                        }

                        fveInput {
                            _id "name"
                            _name "name"
                            _value workspace.draftName
                            _required true
                            _maxlength 80
                        }
                    }

                    fveField {
                        fveLabel {
                            _for "accountType"
                            "Account type"
                        }

                        fveSelect {
                            _id "accountType"
                            _name "accountType"
                            _value workspace.draftType

                            fveSelectContent {
                                for value, label, disabled in
                                    ([ for kind in [ "Asset"; "Liability"; "Equity"; "Revenue"; "Expense" ] ->
                                           (kind, kind, false) ]) do
                                    fveSelectItem {
                                        _value value
                                        _label label
                                        _disabled disabled
                                    }
                            }
                        }
                    }

                    fveDescriptionList {
                        fveDescriptionListItem {
                            _label "Commodity"
                            "USD"
                        }

                        _columns DescriptionListColumns.One
                    }

                    p {
                        _role "status"
                        _class "text-sm text-[var(--fve-muted-text)]"
                        workspace.feedback
                    }

                    div {
                        _class "flex flex-wrap items-center gap-2"

                        fveButton {
                            _color Color.Primary
                            _variant ButtonVariant.Solid
                            _type "submit"
                            "Create account"
                        }

                        pageAction LedgerAccounts "Cancel"
                    }
                }

            page actions "Add an account to the chart of accounts." content

        | LedgerHome ->
            let actions = pageAction LedgerAccounts "View accounts"

            let content =
                section {
                    _ariaLabel "Ledger summary"

                    _class
                        "grid border-y border-[var(--fve-border)] sm:grid-cols-3 sm:divide-x sm:divide-[var(--fve-border)]"

                    for label, value, description in
                        [ "Assets", "$184,230.19", "Current valuation"
                          "Liabilities", "−$42,118.44", "Current valuation"
                          "Unverified transactions", "1", "Requires review" ] do
                        div {
                            _class "px-4 py-5 sm:px-6"

                            fveMetric {
                                _label label
                                _description description
                                value
                            }
                        }
                }

            page
                actions
                (workspace.workspaceName + " · Production")
                (div {
                    _class "grid gap-8"
                    content
                    PageExamples.balanceChart PageExamples.defaultQuery
                    upcomingPayments
                })

        | LedgerReports ->
            page
                (pageAction LedgerAccounts "View accounts")
                "Operating checking · USD"
                (div {
                    _class "grid gap-6"
                    PageExamples.balanceChart PageExamples.defaultQuery
                    transactionTableFor LedgerTransaction shellDestinationUrl
                })

        | LedgerSettings ->
            let actions = empty

            let content =
                form {
                    _method "post"
                    _action "/components/page-examples/account-management/settings"
                    _class "grid max-w-xl gap-5"

                    fveField {
                        fveLabel {
                            _for "workspaceName"
                            "Workspace name"
                        }

                        fveInput {
                            _id "workspaceName"
                            _name "workspaceName"
                            _value workspace.workspaceName
                            _required true
                            _maxlength 80
                        }
                    }

                    fveDescriptionList {
                        fveDescriptionListItem {
                            _label "Reporting currency"
                            workspace.currency

                            fveDescriptionListDescription { "The example ledger is denominated in USD." }
                        }

                        _columns DescriptionListColumns.One
                    }

                    fveField {
                        _layout FieldLayout.Horizontal

                        fveCheckbox {
                            _id "workspace-email-updates"
                            _name "emailUpdates"
                            _checked workspace.emailUpdates
                        }

                        fveLabel {
                            _for "workspace-email-updates"
                            "Receive weekly summaries"
                        }
                    }

                    p {
                        _role "status"
                        _class "text-sm text-[var(--fve-muted-text)]"
                        workspace.feedback
                    }

                    div {
                        _class "flex flex-wrap items-center gap-2"

                        fveButton {
                            _color Color.Primary
                            _variant ButtonVariant.Solid
                            _type "submit"
                            "Save settings"
                        }

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
            fvePageHeader {
                _label pageTitle
                _description "Meier Made cash management"
            }

        if current = TreasuryTransactions then
            treasuryTransactionsPage
        else
            Page.create
                pageHeader
                (section {
                    _class "border-b border-[var(--fve-border)] px-4 py-6 sm:px-6 lg:px-8"

                    match current with
                    | TreasuryAccounts -> accountTable
                    | TreasuryPayees ->
                        fveTable {
                            _ariaLabel "Payees"
                            _mobileLayout MobileLayout.Records

                            fveTableHeader {
                                fveTableRow {
                                    fveTableHead { "Payee" }
                                    fveTableHead { "Latest transaction" }
                                }
                            }

                            fveTableBody {
                                for name, label, id in
                                    [ "Northwind", "Northwind payment", 201
                                      "Cloud hosting", "Cloud hosting", 202
                                      "Office supplier", "Office supplies", 204 ] do
                                    fveTableRow {
                                        fveTableHead {
                                            _scope "row"
                                            _mobile Mobile.Primary
                                            name
                                        }

                                        fveTableCell {
                                            _label "Latest transaction"

                                            a {
                                                _href (shellDestinationUrl (LedgerTransaction id))
                                                _class "text-[var(--fve-brand-text)]"
                                                label
                                            }
                                        }
                                    }
                            }
                        }
                    | _ ->
                        div {
                            _class "grid gap-8"

                            fveMetric {
                                _label "Operating balance"
                                _description "USD · Available cash"
                                "$38,442.11"
                            }

                            upcomingPayments
                            completedPayments
                        }
                })
            |> Page.withTopBar (pageTopBar (treasuryBreadcrumbs current))
            |> Page.withWidth PageWidth.Full
            |> Page.withBodyLayout PageBodyLayout.FullBleed

    // navigation: a SideNav configuration.
    // content: rendered Page HTML.
    // resolve: your destination-to-URL function.
    let ledgerShellExample resolve navigation content =
        (AppShell.create ("ledger-app-shell") (navigation) (content)
         |> AppShell.withTheme (
             (ComponentsTheme.sky
              |> ComponentsTheme.withRadius Radius.Large
              |> ComponentsTheme.withDensity Density.Compact
              |> ComponentsTheme.withControlSize Size.Small)
         )
         |> AppShell.withBoundary (AppShellBoundary.Container)
         |> AppShell.withMobileBottomNavigation (
             fveBottomNavigation {
                 _id "ledger-bottom-navigation"
                 _ariaLabel "Ledger quick navigation"

                 fveBottomNavigationItem {
                     _href (resolve LedgerHome)
                     "Dashboard"
                 }

                 fveBottomNavigationItem {
                     _href (resolve LedgerAccounts)
                     "Accounts"
                 }

                 fveBottomNavigationItem {
                     _href (resolve LedgerReports)
                     "Reports"
                 }

                 fveBottomNavigationItem {
                     _href (resolve LedgerSettings)
                     "Settings"
                 }
             }
         )
         |> AppShell.asPreview ("Ledger application preview")
         |> AppShell.render)

    let treasuryShellExample resolve navigation content =
        (AppShell.create ("treasury-app-shell") (navigation) (content)
         |> AppShell.withTheme (
             (ComponentsTheme.sky
              |> ComponentsTheme.withRadius Radius.Medium
              |> ComponentsTheme.withDensity Density.Compact
              |> ComponentsTheme.withControlSize Size.Small)
         )
         |> AppShell.withBoundary (AppShellBoundary.Container)
         |> AppShell.withMobileBottomNavigation (
             fveBottomNavigation {
                 _id "treasury-bottom-navigation"
                 _ariaLabel "Treasury quick navigation"

                 fveBottomNavigationItem {
                     _href (resolve TreasuryHome)
                     "Overview"
                 }

                 fveBottomNavigationItem {
                     _href (resolve TreasuryTransactions)
                     "Transactions"
                 }

                 fveBottomNavigationItem {
                     _href (resolve TreasuryPayees)
                     "Payees"
                 }

                 fveBottomNavigationItem {
                     _href (resolve TreasuryAccounts)
                     "Accounts"
                 }
             }
         )
         |> AppShell.asPreview ("Treasury application preview")
         |> AppShell.render)

    let ledgerShellWith workspace current =
        let page =
            div {
                _class "flex h-full min-h-0 flex-col"
                _dataSignals "{ledgerRefreshes: 0, ledgerCreates: 0}"

                div {
                    _class "hidden shrink-0 md:block"
                    pageTopBar (ledgerBreadcrumbs current)
                }

                div {
                    _class "min-h-0 flex-1"
                    ledgerPage workspace current |> Page.render
                }

                output {
                    _class "sr-only"
                    _role "status"
                    _dataText "'Balance refreshes: ' + $ledgerRefreshes"
                    "Balance refreshes: 0"
                }
            }

        ledgerShellExample shellDestinationUrl (ledgerNavigation false current) page

    let ledgerShell current =
        ledgerShellWith defaultAccountWorkspace current

    let treasuryShell current =
        treasuryShellExample shellDestinationUrl (treasuryNavigation current) (treasuryPage current |> Page.render)

    let private shellPreviewWith workspace current =
        match current with
        | LedgerHome
        | LedgerAccounts
        | LedgerAccount _
        | LedgerTransaction _
        | LedgerCreateAccount
        | LedgerReports
        | LedgerSettings -> ledgerShellWith workspace current
        | TreasuryHome
        | TreasuryPeriod _
        | TreasuryTransactions
        | TreasuryPayees
        | TreasuryAccounts -> treasuryShell current
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
        | LedgerTransaction _ ->
            Some(FixtureLink.create "Operating checking" (shellDestinationUrl (LedgerAccount operatingAccount.id)))
        | LedgerCreateAccount -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | LedgerHome
        | LedgerReports
        | LedgerSettings -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | _ -> None

    let private appModeNext current =
        match current with
        | LedgerAccounts ->
            Some(FixtureLink.create "Operating checking" (shellDestinationUrl (LedgerAccount operatingAccount.id)))
        | LedgerAccount _ -> Some(FixtureLink.create "Create account" (shellDestinationUrl LedgerCreateAccount))
        | LedgerCreateAccount -> Some(FixtureLink.create "Dashboard" (shellDestinationUrl LedgerHome))
        | LedgerHome
        | LedgerReports
        | LedgerSettings -> Some(FixtureLink.create "Accounts" (shellDestinationUrl LedgerAccounts))
        | _ -> None

    let private shellFixtureConfigWith workspace current =
        let fixture =
            fveBrowser {
                _address ("https://fve.meiermade.com" + shellDestinationUrl current)
                shellPreviewWith workspace current
            }
            |> Fixture.create "ledger-workflow" "Ledger account workflow" (shellDestinationUrl current)
            |> Fixture.withFullscreenContent (shellPreviewWith workspace current)

        let fixture =
            appModePrevious current
            |> Option.map (fun previous -> Fixture.withPrevious previous fixture)
            |> Option.defaultValue fixture

        appModeNext current
        |> Option.map (fun next -> Fixture.withNext next fixture)
        |> Option.defaultValue fixture

    let shellFixtureWith workspace current =
        div {
            _id "components-app-shell-fixture"

            for attribute in shellFixtureNavigationAttributes do
                attribute

            shellFixtureConfigWith workspace current |> Fixture.render
        }

    let shellFixtureFor current =
        shellFixtureWith defaultAccountWorkspace current

    let private registration id path navLabel title : DocPage =
        { id = id
          path = path
          aliases = []
          navLabel = navLabel
          category = "Components"
          title = title
          browserTitle = $"{title} · FSharp.ViewEngine.Components"
          nodes = [] }

    let private applicationRegistration id path navLabel title = registration id path navLabel title

    let private packageRegistration id path navLabel title =
        { registration id path navLabel title with
            category = "Source distribution" }

    let overviewRegistration =
        packageRegistration "components-overview" "/components" "Overview" "Components"

    let installationRegistration =
        packageRegistration "components-installation" "/components/installation" "Installation" "Installation"

    let buttonRegistration =
        registration "components-button" "/components/button" "Button" "Button"

    let linkRegistration =
        registration "components-link" "/components/link" "Link" "Link"

    let buttonGroupRegistration =
        registration "components-button-group" "/components/button-group" "Button group" "Button group"

    let kbdRegistration = registration "components-kbd" "/components/kbd" "Kbd" "Kbd"

    let badgeRegistration =
        registration "components-badge" "/components/badge" "Badge" "Badge"

    let loadingIndicatorRegistration =
        registration
            "components-loading-indicator"
            "/components/loading-indicator"
            "Loading indicator"
            "Loading indicator"

    let progressRegistration =
        registration "components-progress" "/components/progress" "Progress" "Progress"

    let emptyStateRegistration =
        registration "components-empty-state" "/components/empty-state" "Empty state" "Empty state"

    let skeletonRegistration =
        registration "components-skeleton" "/components/skeleton" "Skeleton" "Skeleton"

    let separatorRegistration =
        registration "components-separator" "/components/separator" "Separator" "Separator"

    let tableRegistration =
        registration "components-table" "/components/table" "Table" "Table"

    let chartRegistration =
        registration "components-chart" "/components/chart" "Chart" "Chart"

    let descriptionListRegistration =
        registration "components-description-list" "/components/description-list" "Description list" "Description list"

    let metricRegistration =
        registration "components-metric" "/components/metric" "Metric" "Metric"

    let paginationRegistration =
        registration "components-pagination" "/components/pagination" "Pagination" "Pagination"

    let avatarRegistration =
        registration "components-avatar" "/components/avatar" "Avatar" "Avatar"

    let copyRevealRegistration =
        registration "components-copy-reveal" "/components/copy-reveal" "Copy and reveal" "Copy and reveal"

    let inputRegistration =
        registration "components-input" "/components/input" "Input" "Input"

    let fieldRegistration =
        registration "components-field" "/components/field" "Field" "Field"

    let inputGroupRegistration =
        registration "components-input-group" "/components/input-group" "Input group" "Input group"

    let fileSelectionRegistration =
        registration "components-file-selection" "/components/file-selection" "File selection" "File selection"

    let tagInputRegistration =
        registration "components-tag-input" "/components/tag-input" "Tag input" "Tag input"

    let formLayoutsRegistration =
        applicationRegistration "components-form-layouts" "/components/form-layouts" "Form layouts" "Form layouts"

    let textareaRegistration =
        registration "components-textarea" "/components/textarea" "Textarea" "Textarea"

    let errorSummaryRegistration =
        registration "components-error-summary" "/components/error-summary" "Error summary" "Error summary"

    let noticeRegistration =
        registration "components-notice" "/components/notice" "Notice" "Notice"

    let notificationRegistration =
        registration "components-notification" "/components/notification" "Notification" "Notification"

    let selectRegistration =
        registration "components-select" "/components/select" "Select" "Select"

    let datePickerRegistration =
        registration "components-date-picker" "/components/date-picker" "Date picker" "Date picker"

    let checkboxRegistration =
        registration "components-checkbox" "/components/checkbox" "Checkbox" "Checkbox"

    let switchRegistration =
        registration "components-switch" "/components/switch" "Switch" "Switch"

    let toggleButtonRegistration =
        registration "components-toggle-button" "/components/toggle-button" "Toggle button" "Toggle button"

    let toggleGroupRegistration =
        registration "components-toggle-group" "/components/toggle-group" "Toggle group" "Toggle group"

    let breadcrumbsRegistration =
        registration "components-breadcrumbs" "/components/breadcrumbs" "Breadcrumbs" "Breadcrumbs"

    let sideNavRegistration =
        registration "components-side-nav" "/components/side-nav" "Side nav" "Side nav"

    let tabsRegistration =
        registration "components-tabs" "/components/tabs" "Tabs" "Tabs"

    let radioGroupRegistration =
        registration "components-radio-group" "/components/radio-group" "Radio group" "Radio group"

    let choiceCardsRegistration =
        registration "components-choice-cards" "/components/choice-cards" "Choice cards" "Choice cards"

    let dropdownMenuRegistration =
        registration "components-dropdown-menu" "/components/dropdown-menu" "Dropdown menu" "Dropdown menu"

    let commandRegistration =
        registration "components-command" "/components/command" "Command" "Command"

    let dialogRegistration =
        registration "components-dialog" "/components/dialog" "Dialog" "Dialog"

    let drawerRegistration =
        registration "components-drawer" "/components/drawer" "Drawer" "Drawer"

    let floatingPanelRegistration =
        registration "components-floating-panel" "/components/floating-panel" "Floating panel" "Floating panel"

    let tooltipRegistration =
        registration "components-tooltip" "/components/tooltip" "Tooltip" "Tooltip"

    let popoverRegistration =
        registration "components-popover" "/components/popover" "Popover" "Popover"

    let pageTopBarRegistration =
        applicationRegistration "components-page-top-bar" "/components/page-top-bar" "Page top bar" "Page top bar"

    let pageHeaderRegistration =
        applicationRegistration "components-page-header" "/components/page-header" "Page header" "Page header"

    let sectionRegistration =
        applicationRegistration "components-section" "/components/section" "Section heading" "Section heading"

    let browserRegistration =
        registration "components-browser" "/components/browser" "Browser" "Browser"

    let resizableRegistration =
        registration "components-resizable" "/components/resizable" "Resizable" "Resizable"

    let phoneRegistration =
        registration "components-phone" "/components/phone" "Phone" "Phone"

    let pageRegistration =
        applicationRegistration "components-page" "/components/page" "Page" "Page"

    let collectionRegistration =
        applicationRegistration "components-collection" "/components/collection" "Collection" "Collection"

    let detailRegistration =
        applicationRegistration "components-detail" "/components/detail" "Detail" "Detail"

    let appShellRegistration =
        applicationRegistration "components-app-shell" "/components/app-shell" "App shell" "App shell"

    let bottomNavigationRegistration =
        applicationRegistration
            "components-bottom-navigation"
            "/components/bottom-navigation"
            "Bottom navigation"
            "Bottom navigation"

    let messageRegistration =
        applicationRegistration "components-message" "/components/message" "Message" "Message"

    let dayCalendarRegistration =
        registration "components-day-calendar" "/components/day-calendar" "Day calendar" "Day calendar"

    let weekCalendarRegistration =
        registration "components-week-calendar" "/components/week-calendar" "Week calendar" "Week calendar"

    let monthCalendarRegistration =
        registration "components-month-calendar" "/components/month-calendar" "Month calendar" "Month calendar"

    let yearCalendarRegistration =
        registration "components-year-calendar" "/components/year-calendar" "Year calendar" "Year calendar"

    let mediaLibraryRegistration =
        applicationRegistration "components-media-library" "/components/media-library" "Media library" "Media library"

    let accountManagementRegistration =
        applicationRegistration
            "components-account-management"
            "/components/page-examples/account-management"
            "Account management"
            "Account management"

    let interactionRegistration =
        packageRegistration "components-interaction" "/components/using-signals" "Using signals" "Using signals"

    let themingRegistration =
        packageRegistration "components-theming" "/components/theming" "Theming" "Theming"

    let actionRegistrations =
        [ buttonRegistration
          linkRegistration
          buttonGroupRegistration
          dropdownMenuRegistration
          toggleButtonRegistration
          toggleGroupRegistration ]

    let feedbackRegistrations =
        [ badgeRegistration
          noticeRegistration
          notificationRegistration
          loadingIndicatorRegistration
          progressRegistration
          emptyStateRegistration
          skeletonRegistration ]

    let dataDisplayRegistrations =
        [ tableRegistration
          chartRegistration
          descriptionListRegistration
          metricRegistration
          avatarRegistration
          copyRevealRegistration
          dayCalendarRegistration
          weekCalendarRegistration
          monthCalendarRegistration
          yearCalendarRegistration
          kbdRegistration
          separatorRegistration ]

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

    let navigationRegistrations =
        [ breadcrumbsRegistration
          sideNavRegistration
          tabsRegistration
          paginationRegistration
          commandRegistration ]

    let overlayRegistrations =
        [ dialogRegistration
          drawerRegistration
          floatingPanelRegistration
          popoverRegistration
          tooltipRegistration ]

    let compositionRegistrations = [ pageTopBarRegistration; pageHeaderRegistration ]

    let frameRegistrations =
        [ browserRegistration; phoneRegistration; resizableRegistration ]

    let applicationNavigationRegistrations = [ bottomNavigationRegistration ]
    let applicationWorkflowRegistrations = [ messageRegistration ]
    let applicationResourceRegistrations: DocPage list = []
    let pageExampleRegistrations: DocPage list = []

    let guideRegistrations = [ interactionRegistration; themingRegistration ]

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

    let private tailwindExample =
        """@import "tailwindcss" source(none);
@plugin "@tailwindcss/typography";
@custom-variant dark (&:where(.dark, .dark *));
@source "../Acme.Components/Components/**/*.fs";
@source "../Acme.Components/Components/Assets/**/*.js";
@source not "../Acme.Components/Components/Assets/echarts.6.1.0.esm.min.js";
@source "./**/*.fs";"""

    let basicButton =
        fveButton {
            _dataIndicator "_basic_sync_pending"
            _dataOn ("click", "@get('/components/button/settled', {filterSignals: {exclude: /.*/}, retry: 'never'})")
            "Sync"
        }

    let basicLink =
        fveLink {
            _href "/components"
            "Read documentation"
        }

    let linkVariantExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, treatment in
                [ "Text", LinkVariant.Text
                  "Solid", LinkVariant.Solid
                  "Soft", LinkVariant.Soft
                  "Outline", LinkVariant.Outline
                  "Ghost", LinkVariant.Ghost ] do
                fveLink {
                    _variant treatment
                    _href "/components"
                    label
                }
        }

    let linkColorExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, palette in
                [ "Primary", Color.Primary
                  "Secondary", Color.Secondary
                  "Success", Color.Success
                  "Warning", Color.Warning
                  "Error", Color.Error
                  "Info", Color.Info
                  "Neutral", Color.Neutral ] do
                fveLink {
                    _color palette
                    _href "/components"
                    label
                }
        }

    let linkSizeExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, size in [ "Extra small", Size.ExtraSmall; "Small", Size.Small; "Medium", Size.Medium; "Large", Size.Large; "Extra large", Size.ExtraLarge ] do
                fveLink {
                    _variant LinkVariant.Solid
                    _size size
                    _href "/examples"
                    label
                }
        }

    let newTabLink =
        fveLink {
            _variant LinkVariant.Outline
            _href "/examples"
            _target "_blank"
            _rel "noopener"
            "Open examples in a new tab"
        }

    let buttonSizeExamples =
        div {
            _class "flex min-w-0 flex-wrap items-end justify-center gap-6"
            for size, label in
                [ Size.ExtraSmall, "Extra small"
                  Size.Small, "Small"
                  Size.Medium, "Medium"
                  Size.Large, "Large"
                  Size.ExtraLarge, "Extra large" ] do
                div {
                    _class "flex flex-col items-center gap-3"
                    fveButton {
                        _size size; _color Color.Primary; _variant ButtonVariant.Solid
                        label
                    }
                    fveButton {
                        _size size; _variant ButtonVariant.Outline; _iconOnly true
                        _ariaLabel (label + " refresh")
                        refreshIcon
                    }
                }
        }

    let buttonColorExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, palette in
                [ "Primary", Color.Primary
                  "Secondary", Color.Secondary
                  "Success", Color.Success
                  "Warning", Color.Warning
                  "Error", Color.Error
                  "Info", Color.Info
                  "Neutral", Color.Neutral ] do
                fveButton {
                    _color palette
                    _variant ButtonVariant.Solid
                    label
                }
        }

    let buttonVariantExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, treatment in
                [ "Solid", ButtonVariant.Solid
                  "Soft", ButtonVariant.Soft
                  "Outline", ButtonVariant.Outline
                  "Ghost", ButtonVariant.Ghost ] do
                fveButton {
                    _color Color.Primary
                    _variant treatment
                    label
                }
        }

    let customButtonPalette: ColorPalette =
        { light =
            { solid = "#9a3412"
              solidHover = "#7c2d12"
              solidActive = "#431407"
              onSolid = "#ffffff"
              soft = "#fff7ed"
              softHover = "#ffedd5"
              softActive = "#fed7aa"
              text = "#9a3412"
              border = "#9a3412"
              focus = "#9a3412" }
          dark =
            { solid = "#fdba74"
              solidHover = "#fed7aa"
              solidActive = "#ffedd5"
              onSolid = "#431407"
              soft = "#431407"
              softHover = "#5c2209"
              softActive = "#7c2d12"
              text = "#ffedd5"
              border = "#fdba74"
              focus = "#fdba74" } }

    let customPaletteButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, treatment in
                [ "Solid", ButtonVariant.Solid
                  "Soft", ButtonVariant.Soft
                  "Outline", ButtonVariant.Outline
                  "Ghost", ButtonVariant.Ghost ] do
                fveButton {
                    _color Color.Custom
                    _palette customButtonPalette
                    _variant treatment
                    label
                }
        }

    let pendingErrorButton =
        fveButton {
            _color Color.Error
            _variant ButtonVariant.Solid
            _dataIndicator "_delete_pending"
            _dataOn ("click", "@get('/components/button/settled', {filterSignals: {exclude: /.*/}, retry: 'never'})")
            "Delete"
        }

    let iconOnlyButtonVariantExamples =
        div {
            _class "flex flex-wrap items-center justify-center gap-3"

            for label, treatment in
                [ "Solid", ButtonVariant.Solid
                  "Soft", ButtonVariant.Soft
                  "Outline", ButtonVariant.Outline
                  "Ghost", ButtonVariant.Ghost ] do
                fveButton {
                    _color Color.Secondary
                    _variant treatment
                    _ariaLabel (label + " refresh")
                    _iconOnly true
                    refreshIcon
                }
        }

    let buttonContentExamples =
        let continueIcon =
            raw
                """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M7.22 14.78a.75.75 0 0 1 0-1.06L10.94 10 7.22 6.28a.75.75 0 0 1 1.06-1.06l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0Z" clip-rule="evenodd"/></svg>"""

        div {
            _class "flex flex-wrap items-center justify-center gap-3"
            fveButton { "Text" }

            fveButton {
                _ariaLabel "Refresh"
                _iconOnly true
                refreshIcon
            }

            fveButton {
                plusIcon
                "New branch"
            }

            fveButton {
                "Continue"
                continueIcon
            }

            fveButton {
                strong { "Custom" }
                span { " content" }
            }
        }

    let primaryButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"

            for size in [ Size.Small; Size.Medium; Size.Large ] do
                fveButton {
                    _color Color.Primary
                    _variant ButtonVariant.Solid
                    _size size
                    "Create"
                }
        }

    let secondaryButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"

            for size in [ Size.Small; Size.Medium; Size.Large ] do
                fveButton {
                    _color Color.Secondary
                    _variant ButtonVariant.Soft
                    _size size
                    "View reports"
                }
        }

    let ghostButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"

            for size in [ Size.Small; Size.Medium; Size.Large ] do
                fveButton {
                    _variant ButtonVariant.Ghost
                    _size size
                    "Cancel"
                }
        }

    let destructiveButtons =
        div {
            _class "flex flex-wrap items-center justify-center gap-4"

            for size in [ Size.Small; Size.Medium; Size.Large ] do
                fveButton {
                    _color Color.Error
                    _variant ButtonVariant.Solid
                    _size size
                    "Delete"
                }
        }

    let confirmationExample =
        div {
            fveDialogTrigger {
                fveButton {
                    _ariaControls "delete-item-confirmation"
                    "Delete"
                }
            }

            itemDeletionDialog
        }

    let itemDrawerExample =
        div {
            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-details-drawer"
                    "Open item details"
                }
            }

            itemDrawerConfig
        }

    let itemEditorDrawerExample =
        div {
            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-editor-drawer"
                    "Edit item"
                }
            }

            itemEditorDrawerConfig
        }

    let filterDrawerExample =
        div {
            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-filters-drawer"
                    "Open filters"
                }
            }

            filterDrawerConfig
        }

    let horizontalDrawerExamples =
        div {
            _class "flex flex-wrap gap-3"

            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-summary-drawer"
                    "Open top drawer"
                }
            }

            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-history-drawer"
                    "Open bottom drawer"
                }
            }

            topDrawerConfig
            bottomDrawerConfig
        }

    let sideNavExample = nestedSideNav

    let pageHeaderExample =
        div {
            _dataSignals "{ledgerRefreshes: 0}"
            accountPageHeader

            output {
                _class "sr-only"
                _role "status"
                _dataText "'Balance refreshes: ' + $ledgerRefreshes"
                "Balance refreshes: 0"
            }
        }

    let periodNote =
        Section.withoutHeader
            "Supporting note"
            (p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Items are shown in the order they were added."
            })
        |> Section.render

    let responsiveActionsExample =
        div {
            _dataSignals "{ledgerRefreshes: 0}"

            fvePageHeader {
                _label "Operating checking"
                fvePageHeaderActions { accountPageActions "responsive-account-actions" }
            }

            output {
                _class "sr-only"
                _role "status"
                _dataText "'Balance refreshes: ' + $ledgerRefreshes"
                "Balance refreshes: 0"
            }
        }

    let bottomNavigationExample =
        fveBottomNavigation {
            _id "example-bottom-navigation"
            _ariaLabel "Primary navigation"

            fveBottomNavigationItem {
                _href "/components"
                "Home"
            }

            fveBottomNavigationItem {
                _href "/components/table"
                "Items"
            }

            fveBottomNavigationItem {
                _href "/components/metric"
                "Activity"
            }

            fveBottomNavigationItem {
                _href "/components/theming"
                "Settings"
            }

            _value "/components/table"
        }

    let compactBottomNavigationExample =
        fveBottomNavigation {
            _id "compact-bottom-navigation"
            _ariaLabel "Workspace navigation"

            fveBottomNavigationItem {
                _href "/components"
                "Home"
            }

            fveBottomNavigationItem {
                _href "/components/month-calendar"
                "Schedule"
            }

            fveBottomNavigationItem {
                _href "/components/page-examples/media-management"
                "Media"
            }

            _value "/components/month-calendar"
        }

    let private workspacePreview _ (content: HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"

            for attribute in galleryDestinationAttributes do
                attribute

            div {
                for attribute in
                    ComponentsTheme.attributes (ComponentsTheme.sky |> ComponentsTheme.withDensity Density.Compact) do
                    attribute

                div {
                    _class "h-[36rem] overflow-hidden"
                    content
                }
            }
        }

    let transactionsPagePreview =
        workspacePreview
            "transactions-page"
            (renderTransactionsPage (pageTopBar (treasuryBreadcrumbs TreasuryTransactions)) transactionTabs)

    let canvasPagePreview =
        workspacePreview "canvas-page" (canvasPage () |> Page.render)

    [<NoEquality; NoComparison>]
    type ComponentExample =
        { id: string
          title: string
          source: string
          preview: HtmlElement
          note: string option }

    let exampleImports =
        "open System\nopen FSharp.ViewEngine\nopen Acme.Components\nopen type Html\nopen type Svg\nopen type Datastar"

    let private sample id title names preview =
        { id = "components-" + id
          title = title
          source = exampleImports + "\n\n" + SourceRegion.declarations names sourceText.Value
          preview = preview
          note = None }

    let private serverSample id title names preview =
        let example = sample id title names preview

        { example with
            source =
                "open Giraffe\nopen Microsoft.Extensions.DependencyInjection\nopen StarFederation.Datastar\nopen StarFederation.Datastar.DependencyInjection\n"
                + example.source
            note =
                Some
                    "Includes the running demo's Giraffe handler and routes. Mount the routes in your existing application with services.AddDatastar(); the short delay makes request loading visible." }

    let private note message example = { example with note = Some message }

    let private gallerySurface (content: HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"

            for attribute in galleryDestinationAttributes do
                attribute

            div {
                for attribute in ComponentsTheme.attributes ComponentsTheme.sky do
                    attribute

                content
            }
        }

    let private connectedGallerySurface (content: HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"

            div {
                for attribute in ComponentsTheme.attributes ComponentsTheme.sky do
                    attribute

                content
            }
        }

    let private centered (content: HtmlElement) =
        gallerySurface (
            div {
                _class "flex min-h-40 min-w-0 flex-wrap items-center justify-center gap-6 p-[12px]"
                content
            }
        )

    let private connectedCentered (content: HtmlElement) =
        connectedGallerySurface (
            div {
                _class "flex min-h-40 min-w-0 flex-wrap items-center justify-center gap-6 p-[12px]"
                content
            }
        )

    let private fieldSurface (content: HtmlElement) =
        gallerySurface (
            div {
                _class "mx-auto grid min-h-40 min-w-0 max-w-md content-center gap-4 p-[12px] sm:p-8"
                content
            }
        )

    let defaultResizablePanels =
        fveResizable {
            _id "default-workspace-panels"
            _ariaLabel "Workspace panels"

            fveResizablePanel {
                aside {
                    _class "h-full p-4"
                    "Navigation"
                }
            }

            fveResizablePanel {
                div {
                    _class "h-full p-4"
                    "Content"
                }
            }
        }

    let resizablePanels =
        fveResizable {
            _id "item-workspace-panels"
            _ariaLabel "Workspace panels"
            _initialSize 34
            _minSize 20
            _maxSize 60
            _collapsedSize 0

            fveResizablePanel {
                aside {
                    _class "h-full bg-[var(--fve-surface-subtle)] p-4"
                    strong { "Items" }

                    p {
                        _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                        "Project notes\nShared files\nTeam guide"
                    }
                }
            }

            fveResizablePanel {
                div {
                    _class "h-full p-4"

                    h3 {
                        _class "font-semibold"
                        "Project notes"
                    }

                    p {
                        _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                        "Drag the divider to resize the supporting list."
                    }
                }
            }
        }

    let verticalResizablePanels =
        fveResizable {
            _id "review-workspace-panels"
            _ariaLabel "Review panels"
            _orientation Orientation.Vertical
            _initialSize 40
            _minSize 25
            _maxSize 70

            fveResizablePanel {
                section {
                    _class "h-full p-4"
                    strong { "Review summary" }

                    p {
                        _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                        "Three entries need attention."
                    }
                }
            }

            fveResizablePanel {
                section {
                    _class "h-full bg-[var(--fve-surface-subtle)] p-4 text-sm"
                    "Entry details"
                }
            }
        }

    let basicTooltip =
        fveTooltip {
            _id "archive-tooltip"
            _description "Moves the item out of active lists."

            fveButton {
                _ariaDescribedby ("archive-tooltip" + "-content")
                "Archive"
            }
        }

    let bottomTooltip =
        fveTooltip {
            _id "bottom-placement-tooltip"
            _description "Moves the item out of active lists."
            _delay 150
            _side TooltipSide.Bottom

            fveButton {
                _variant ButtonVariant.Outline
                _ariaDescribedby ("bottom-placement-tooltip" + "-content")
                "Archive"
            }
        }

    let describedTooltip =
        div {
            _class "grid gap-3"

            p {
                _id "archive-description"
                _class "text-sm text-[var(--fve-muted-text)]"
                "Archived items remain searchable."
            }

            fveTooltip {
                _id "described-archive-tooltip"
                _description "Moves the item out of active lists."

                fveButton {
                    _ariaDescribedby ("archive-description " + ("described-archive-tooltip" + "-content"))
                    "Archive"
                }
            }
        }

    let profilePopover =
        fvePopover {
            _id "profile-popover"
            fvePopoverTrigger { fveButton { "Profile details" } }
            fvePopoverContent {
                _tabindex 0
                _autofocus true
                _class "grid gap-3"
                h3 { _class "font-semibold"; "Personal workspace" }
                p { _class "text-sm text-[var(--fve-muted-text)]"; "Profile information and workspace access." }
                dl {
                    _class "grid grid-cols-1 gap-x-4 gap-y-2 break-words text-sm sm:grid-cols-2"
                    dt { _class "text-[var(--fve-muted-text)]"; "Access" }
                    dd { "Owner" }
                    dt { _class "text-[var(--fve-muted-text)]"; "Environment" }
                    dd { "Production" }
                }
            }
        }

    let previewSizePopover =
        div {
            _class "grid w-full justify-items-start gap-4"
            _dataSignals "{_popover_preview_width: 240, _popover_preview_height: 120}"
            fvePopover {
                _id "preview-size-popover"
                _focusOnOpen true
                fvePopoverTrigger { fveButton { "Preview size" } }
                fvePopoverContent {
                    form {
                        _ariaLabel "Preview dimensions"
                        _class "grid gap-3"
                        _dataOn ("submit", "evt.preventDefault()")
                        _dataOn ("reset", "$_popover_preview_width = 240; $_popover_preview_height = 120")
                        h3 { _class "font-semibold"; "Preview dimensions" }
                        p { _class "text-sm text-[var(--fve-muted-text)]"; "Changes resize the sample immediately." }
                        for key, label, value, minimum, maximum in
                            [ "width", "Width", "240", "120", "400"
                              "height", "Height", "120", "64", "240" ] do
                            fveField {
                                fveLabel { _for ("popover-preview-" + key); label }
                                fveInput {
                                    _id ("popover-preview-" + key); _name key; _type InputType.Number
                                    _value value; _min minimum; _max maximum; _required true
                                    _dataBind ("_popover_preview_" + key)
                                }
                            }
                        fveButton { _type "reset"; "Reset size" }
                    }
                }
            }
            div {
                _ariaLabel "Size preview"
                _class "flex items-center justify-center rounded border border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] text-sm text-[var(--fve-muted-text)]"
                _style "width:240px;height:120px;min-width:120px;max-width:min(400px,100%);min-height:64px;max-height:240px"
                _dataStyle ("width", "$_popover_preview_width + 'px'")
                _dataStyle ("height", "$_popover_preview_height + 'px'")
                "Preview"
            }
        }

    let alignedPopover =
        fvePopover {
            _id "filter-popover"
            _alignment PopoverAlignment.End
            _side PopoverSide.Top
            fvePopoverTrigger { fveButton { "Filter details" } }
            fvePopoverContent {
                _ariaLabel "Filter details"
                _tabindex 0
                _autofocus true
                h3 { _class "font-semibold"; "Active items" }
                p { _class "mt-2 text-sm text-[var(--fve-muted-text)]"; "Showing items updated this month." }
            }
        }

    let nativeTriggerPopover =
        fvePopover {
            _id "native-workspace-popover"
            fvePopoverTrigger {
                _ariaLabel "Workspace information"
                fun (attributes: HtmlAttribute list) ->
                    button {
                        for attribute in attributes do attribute
                        _class "inline-flex size-9 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-sm font-semibold text-[var(--fve-brand-text)]"
                        span { _ariaHidden true; "AM" }
                    }
            }
            fvePopoverContent {
                _tabindex 0
                _autofocus true
                h3 { _class "font-semibold"; "Personal workspace" }
                p { _class "mt-2 text-sm text-[var(--fve-muted-text)]"; "You have owner access in this workspace." }
            }
        }

    let monthSelection =
        fveMonthCalendarSelection {
            _id "month-selection"
            _name "selectedDate"
            _ariaLabel "Date"
        }

    let monthSelectionSingle =
        fveMonthCalendarSelection {
            _id "selected-month-calendar"
            _name "selectedDate"
            _ariaLabel "Selected date"

            _value (
                ((DateOnly(2026, 9, 18)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _displayMonth (DateOnly(2026, 9, 1))
            _clearable true
        }

    let currentDateMonthSelection =
        fveMonthCalendarSelection {
            _id "current-date-month-calendar"
            _name "currentDateSelection"
            _ariaLabel "Date with current-day highlight"
            _value "2026-11-12"
            _currentDate (DateOnly(2026, 11, 5))
            _clearable true
        }

    let todayMonthSelection =
        form {
            _id "today-month-form"
            _class "grid justify-items-start gap-3"
            fveMonthCalendarSelection {
                _id "today-month-calendar"
                _name "shortcutDate"
                _ariaLabel "Date with Today shortcut"
                _value "2026-09-18"
                _displayMonth (DateOnly(2026, 9, 1))
                _today (DateOnly.FromDateTime DateTime.Today)
                _clearable true
            }
            fveButton { _type "reset"; "Reset date" }
        }

    let monthSelectionRange =
        fveMonthCalendarSelection {
            _id "date-range-calendar"
            _name "rangeStart"
            _ariaLabel "Date range"
            _mode CalendarSelectionMode.Range

            _value (
                ((DateOnly(2026, 9, 14)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _endValue (
                ((DateOnly(2026, 9, 18)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _rangeEndName "rangeEnd"
            _displayMonth (DateOnly(2026, 9, 1))
            _weekStart MonthCalendarWeekStart.Monday
        }

    let boundedMonthSelection =
        fveMonthCalendarSelection {
            _id "booking-month-calendar"
            _name "bookingDate"
            _today (DateOnly.FromDateTime DateTime.Today)
            _ariaLabel "Booking date"
            _displayMonth (DateOnly(2026, 9, 1))

            _min (
                ((DateOnly(2026, 9, 10)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _max (
                ((DateOnly(2026, 9, 25)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _unavailableDates (
                [ DateOnly(2026, 9, 16); DateOnly(2026, 9, 17) ]
                |> List.map (fun (date: System.DateOnly) ->
                    date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
            )

            _lang "en-GB"
            _weekStart MonthCalendarWeekStart.Monday
        }

    let captionMonthSelection =
        fveMonthCalendarSelection {
            _id "birth-month-calendar"
            _name "birthDate"
            _ariaLabel "Choose a birth date"

            _value (
                ((DateOnly(2000, 6, 15)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _displayMonth (DateOnly(2000, 6, 1))

            _min (
                ((DateOnly(1920, 1, 1)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _max (
                ((DateOnly(2026, 12, 31)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _captionLayout MonthCalendarCaptionLayout.Dropdown
        }

    let pendingMonthSelection =
        fveMonthCalendarSelection {
            _id "pending-month-calendar"
            _name "availableDate"
            _today (DateOnly.FromDateTime DateTime.Today)
            _ariaLabel "Checking available dates"
            _displayMonth (DateOnly(2026, 9, 1))
            _ariaBusy true
        }

    let readOnlyMonthSelection =
        fveMonthCalendarSelection {
            _id "readonly-month-calendar"
            _name "readonlyDate"
            _today (DateOnly.FromDateTime DateTime.Today)
            _ariaLabel "Read-only date"
            _value "2026-09-18"
            _displayMonth (DateOnly(2026, 9, 1))
            _readonly true
        }

    let defaultDatePicker =
        fveField {
            fveLabel {
                _for "default-selected-date"
                "Date"
            }

            fveDatePicker {
                _id "default-selected-date"
                _name "defaultSelectedDate"
            }
        }

    let readOnlyDatePicker =
        fveField {
            fveLabel {
                _for "readonly-date"
                "Read-only date"
            }

            fveDatePicker {
                _id "readonly-date"
                _name "readonlyDate"
                _today (DateOnly.FromDateTime DateTime.Today)
                _value "2026-09-18"
                _readonly true
            }
        }

    let basicDatePicker =
        fveField {
            fveLabel {
                _id ("selected-date" + "-label")
                _for "selected-date"
                "Date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "selected-date"
                    _name "selectedDate"

                    _value (
                        ((DateOnly(2026, 9, 18)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _clearable true
                    _ariaDescribedby (String.concat " " [ ("selected-date" + "-description") ])
                }
            }

            fveFieldDescription {
                _id ("selected-date" + "-description")
                "Uses a canonical date-only form value."
            }
        }

    let currentDateDatePicker =
        fveField {
            fveLabel { _for "current-date-picker"; "Date with current-day highlight" }
            fveDatePicker {
                _id "current-date-picker"
                _name "currentDateSelection"
                _value "2026-11-12"
                _currentDate (DateOnly(2026, 11, 5))
                _clearable true
            }
        }

    let todayDatePicker =
        form {
            _id "today-picker-form"
            _class "grid justify-items-start gap-3"
            fveField {
                fveLabel { _for "today-selected-date"; "Date with Today shortcut" }
                fveDatePicker {
                    _id "today-selected-date"
                    _name "shortcutDate"
                    _value "2026-09-18"
                    _today (DateOnly.FromDateTime DateTime.Today)
                    _clearable true
                }
            }
            fveButton { _type "reset"; "Reset date" }
        }

    let rangeDatePicker =
        fveField {
            fveLabel {
                _id ("date-range" + "-label")
                _for "date-range"
                "Date range"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "date-range"
                    _name "periodStart"
                    _today (DateOnly.FromDateTime DateTime.Today)
                    _mode CalendarSelectionMode.Range

                    _value (
                        ((DateOnly(2026, 9, 14)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _endValue (
                        ((DateOnly(2026, 9, 18)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _rangeEndName "periodEnd"
                    _weekStart MonthCalendarWeekStart.Monday
                }
            }
        }

    let captionDatePicker =
        fveField {
            fveLabel {
                _id ("date-of-birth" + "-label")
                _for "date-of-birth"
                "Date of birth"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "date-of-birth"
                    _name "dateOfBirth"

                    _value (
                        ((DateOnly(2000, 6, 15)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _displayMonth (DateOnly(2000, 6, 1))

                    _min (
                        ((DateOnly(1920, 1, 1)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _max (
                        ((DateOnly(2026, 12, 31)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _captionLayout MonthCalendarCaptionLayout.Dropdown
                }
            }
        }

    let boundedDatePicker =
        fveField {
            fveLabel {
                _id ("travel-date" + "-label")
                _for "travel-date"
                "Travel date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "travel-date"
                    _name "travelDate"
                    _today (DateOnly.FromDateTime DateTime.Today)
                    _displayMonth (DateOnly(2026, 9, 1))

                    _min (
                        ((DateOnly(2026, 9, 10)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _max (
                        ((DateOnly(2026, 9, 25)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _unavailableDates (
                        [ DateOnly(2026, 9, 16); DateOnly(2026, 9, 17) ]
                        |> List.map (fun (date: System.DateOnly) ->
                            date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
                    )
                }
            }
        }

    let invalidDatePicker =
        fveField {
            fveLabel {
                _id ("required-date" + "-label")
                _for "required-date"
                "Date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "required-date"
                    _name "requiredDate"
                    _displayMonth (DateOnly(2026, 9, 1))
                    _ariaDescribedby (String.concat " " [ ("required-date" + "-validation") ])
                    _ariaInvalid true
                }
            }

            fveFieldError {
                _id ("required-date" + "-validation")
                _role "alert"
                "Choose an available date."
            }
        }

    let pendingDatePicker =
        fveField {
            fveLabel {
                _id ("availability-date" + "-label")
                _for "availability-date"
                "Availability date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "availability-date"
                    _name "availabilityDate"
                    _today (DateOnly.FromDateTime DateTime.Today)

                    _value (
                        ((DateOnly(2026, 9, 21)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _ariaBusy true
                }
            }
        }

    let disabledDatePicker =
        fveField {
            fveLabel {
                _id ("archived-date" + "-label")
                _for "archived-date"
                "Archived date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "archived-date"
                    _name "archivedDate"
                    _today (DateOnly.FromDateTime DateTime.Today)

                    _value (
                        ((DateOnly(2026, 9, 8)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _disabled true
                }
            }
        }

    let defaultField =
        fveField {
            fveLabel {
                _for "reference"
                "Reference"
            }

            fveInput {
                _id "reference"
                _name "reference"
            }
        }

    let customQuantityField =
        fveField {
            fveLabel {
                _for "item-quantity"
                "Quantity"
            }

            input {
                _id "item-quantity"
                _name "quantity"
                _type "number"
                _required true
                _step "0.01"
                _ariaDescribedby "item-quantity-description"

                _class
                    "min-h-[var(--fve-control-min-height)] w-full rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
            }

            fveFieldDescription {
                _id "item-quantity-description"
                "Enter the number of items."
            }
        }

    let responsiveFieldRows =
        div {
            _class "grid gap-4"

            fveField {
                _layout FieldLayout.Responsive

                fveLabel {
                    _for "legal-name"
                    "Legal name"
                }

                fveInput {
                    _id "legal-name"
                    _name "legalName"
                    _ariaDescribedby "legal-name-description"
                }

                fveFieldDescription {
                    _id "legal-name-description"
                    "Use the name on your profile."
                }
            }

            fveField {
                _layout FieldLayout.Responsive

                fveLabel {
                    _for "item-reference"
                    "Reference"
                }

                fveInput {
                    _id "item-reference"
                    _name "itemReference"
                    _ariaInvalid true
                    _ariaDescribedby "item-reference-validation"
                }

                fveFieldError {
                    _id "item-reference-validation"
                    "Enter the nine-character reference."
                }
            }
        }

    let groupedFields =
        fveFieldGroup {
            _ariaDescribedby "delivery-preferences-description"
            fveFieldLegend { "Delivery preferences" }

            fveFieldDescription {
                _id "delivery-preferences-description"
                "Choose one or both delivery methods."
            }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "emailDelivery"
                    _name "emailDelivery"
                    _checked true
                }

                fveLabel {
                    _for "emailDelivery"
                    "Email updates"
                }
            }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "paperDelivery"
                    _name "paperDelivery"
                }

                fveLabel {
                    _for "paperDelivery"
                    "Printed updates"
                }
            }
        }

    let weightInputGroup =
        fveField {
            fveLabel {
                _for "package-weight"
                "Weight"
            }

            fveInputGroup {
                fveInputGroupAddon {
                    _ariaHidden true
                    "Approx."
                }

                fveInput {
                    _id "package-weight"
                    _name "packageWeight"
                    _value "12.5"
                    _ariaDescribedby "package-weight-description"
                }

                fveInputGroupAddon {
                    _position InputGroupPosition.InlineEnd
                    _ariaHidden true
                    "kg"
                }
            }

            fveFieldDescription {
                _id "package-weight-description"
                "Whole numbers or decimals are accepted."
            }
        }

    let searchIcon =
        raw """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.197 5.197a7.5 7.5 0 0 0 10.606 10.606Z"/></svg>"""

    let searchInputGroup =
        fveField {
            fveLabel {
                _for "item-search"
                "Search items"
            }

            fveInputGroup {
                fveInput {
                    _id "item-search"
                    _name "query"
                    _type InputType.Search
                    _placeholder "Search items…"
                }
                fveInputGroupAddon { _ariaHidden true; searchIcon }
                fveInputGroupAddon {
                    _position InputGroupPosition.InlineEnd
                    _ariaHidden true

                    fveKbdShortcut {
                        fveKbd { "⌘" }
                        fveKbd { "K" }
                    }
                }
            }
        }

    let inputGroupWithHelp =
        fveField {
            fveLabel {
                _for "help-reference"
                "Reference with help"
            }

            fveInputGroup {
                fveInput {
                    _id "help-reference"
                    _name "reference"
                }

                fveInputGroupAddon {
                    _position InputGroupPosition.InlineStart

                    fvePopover {
                        _id "input-group-help"

                        fvePopoverTrigger {
                            fveButton {
                                _size Size.Small
                                _variant ButtonVariant.Ghost
                                _ariaLabel "Reference help"
                                "Help"
                            }
                        }

                        fvePopoverContent {
                            fveField {
                                fveLabel {
                                    _for "help-limit"
                                    "Maximum length"
                                }

                                fveInput {
                                    _id "help-limit"
                                    _name "limit"
                                    _value "10"
                                }
                            }
                        }
                    }
                }
            }
        }

    let textareaInputGroup =
        fveField {
            fveLabel {
                _for "message-draft"
                "Message"
            }

            fveInputGroup {
                fveTextarea {
                    _id "message-draft"
                    _dataRef "_message_draft"
                    _name "message"
                    _rows 4
                    _maxlength 2000
                    "The latest project notes are ready for review."
                }
                fveInputGroupAddon {
                    _position InputGroupPosition.BlockStart
                    _class "border-b border-[var(--fve-border)]"
                    "Message draft"
                }
                fveInputGroupAddon {
                    _position InputGroupPosition.BlockEnd
                    _class "justify-between border-t border-[var(--fve-border)]"
                    span { "Maximum 2,000 characters." }
                    fveButton {
                        _variant ButtonVariant.Ghost
                        _size Size.Small
                        _dataOn ("click", "$_message_draft.value = ''; $_message_draft.dispatchEvent(new Event('input', {bubbles: true})); $_message_draft.focus()")
                        "Clear message"
                    }
                }
            }
        }

    let invalidInputGroup =
        fveField {
            fveLabel { _for "item-reference-code"; "Reference" }
            fveInputGroup {
                fveInput {
                    _id "item-reference-code"
                    _name "referenceCode"
                    _required true
                    _pattern "[0-9]{9}"
                    _inputmode "numeric"
                    yield! nativeValidationAttributes "_reference_code" true "Enter a nine-digit reference."
                    _dataAttr ("aria-describedby", "$_reference_code_error ? 'item-reference-code-validation' : null")
                }
                fveInputGroupAddon { _ariaHidden true; "ID" }
            }
            fveFieldError {
                _id "item-reference-code-validation"
                _style "display:none"
                _dataShow "$_reference_code_error"
                _dataText "$_reference_code_error"
            }
        }

    let emailCheckResult valid =
        p {
            _id "input-group-check-result"
            _dataRef "_email_check_result"
            _role "status"
            _class "text-sm text-[var(--fve-muted-text)]"
            if valid then "Email format is valid." else "Enter a valid email address."
        }

    let checkEmailInputGroup =
        fveField {
            _dataSignals "{_email_check_pending: false}"
            fveLabel { _for "email-to-check"; "Email to check" }
            fveInputGroup {
                fveInput {
                    _id "email-to-check"
                    _dataRef "_email_check_input"
                    _dataOn ("input", "$_email_check_result.textContent = ''")
                    _name "email"
                    _type InputType.Email
                    _value "alex@fve.meiermade.com"
                    _required true
                    _maxlength 254
                    _dataAttr ("readonly", "$_email_check_pending")
                    _dataAttr ("aria-busy", "$_email_check_pending ? 'true' : null")
                }
                fveInputGroupAddon {
                    _position InputGroupPosition.BlockEnd
                    _class "justify-between border-t border-[var(--fve-border)]"
                    fveButton {
                        _size Size.Small
                        _variant ButtonVariant.Ghost
                        _dataIndicator "_email_check_pending"
                        _dataAttr ("disabled", "$_email_check_pending")
                        _dataOn ("click", "$_email_check_input.reportValidity() && @get('/components/input-group/check-email?email=' + encodeURIComponent($_email_check_input.value), {requestCancellation: 'cleanup', retry: 'never'})")
                        "Check email"
                    }
                    span {
                        _style "display:none"
                        _dataShow "$_email_check_pending"
                        _role "status"
                        _ariaLabel "Checking email format"
                        "Checking…"
                    }
                }
            }
            p { _id "input-group-check-result"; _dataRef "_email_check_result"; _role "status" }
        }

    let checkEmailHandler: HttpHandler =
        fun next context -> task {
            let email = context.Request.Query["email"].ToString()
            let valid = email.Length <= 254 && System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)
            do! System.Threading.Tasks.Task.Delay(1000, context.RequestAborted)
            let html = emailCheckResult valid |> Render.toString
            if context.Request.Headers["datastar-request"].ToString() = "true" then
                let service = context.RequestServices.GetRequiredService<IDatastarService>()
                do! service.PatchElementsAsync(html, cancellationToken = context.RequestAborted)
                return Some context
            else
                return! htmlString html next context
        }

    let checkEmailRoutes: HttpHandler =
        Giraffe.Core.GET >=> route "/components/input-group/check-email" >=> checkEmailHandler

    let defaultMessage =
        fveMessage {
            _label "Jordan Lee"
            p { "Can you review the latest project notes?" }
        }

    let messageRows =
        div {
            _class "grid gap-5"

            fveMessage {
                _label "Jordan Lee"
                p { "Can you review the project notes before lunch?" }

                fveMessageAvatar {
                    fveAvatar {
                        _ariaLabel "Jordan Lee"
                        _size Size.Small
                        "JL"
                    }
                }

                fveMessageMetadata { "09:12" }
            }

            fveMessage {
                _label "Andy Meier"
                p { "Yes. I’ll add comments to the two items that need attention." }
                _side MessageSide.Sender
                fveMessageMetadata { "09:14" }
                fveMessageStatus { "Delivered" }
            }
        }

    let messageWithoutAvatar =
        fveMessage {
            _label "Support bot"

            p {
                _class "whitespace-pre-wrap"
                "The export is ready.\nIt contains 184 items."
            }

            fveMessageMetadata { "Just now" }

            fveMessageActions {
                fveButton {
                    _variant ButtonVariant.Ghost
                    _size Size.Small
                    "Download"
                }
            }
        }

    let browserWithAddress =
        fveBrowser {
            _address "https://fve.meiermade.com/components/browser"

            div {
                _class "grid min-h-64 content-center gap-3 bg-[var(--fve-surface-subtle)] p-[12px] text-center"

                strong {
                    _class "text-lg"
                    "Content preview"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Rendered HTML"
                }
            }
        }

    let phoneWithContent =
        fvePhone {
            div {
                _class "grid h-full content-center gap-3 p-[12px] text-center"

                strong {
                    _class "text-lg"
                    "Notes"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Confirm arrival time."
                }
            }
        }

    // Host-owned review links; installed frames supply slots and viewport presentation only.
    let fixtureWindowControl (path: string) (id: string) (expanded: bool) =
        let label =
            (if expanded then "Restore " else "Expand ")
            + (if id = "browser-review" then
                   "Browser fixture"
               else
                   "Phone fixture")

        a {
            _id ("fve-fixture-" + id + (if expanded then "-restorer" else "-launcher"))

            _href (
                if expanded then
                    path + "?fveAppReturn=" + id
                else
                    path + "?expanded=1"
            )

            _ariaLabel label
            _title label

            if expanded then
                _data ("fve-app-mode-exit", "true")
            else
                _data ("review-expand", "true")

                _dataInit (
                    "if (new URL(location.href).searchParams.get('fveAppReturn') === "
                    + System.Text.Json.JsonSerializer.Serialize id
                    + ") { el.focus({preventScroll:true}); el.scrollIntoView({block:'center'}) }"
                )

            _class
                "grid size-8 shrink-0 place-items-center rounded-full focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

            span {
                _ariaHidden true

                _class (
                    "grid size-3.5 place-items-center rounded-full "
                    + (if expanded then
                           "bg-[#febc2e] text-[#995700]"
                       else
                           "bg-[#28c840] text-[#0b6523]")
                )

                if expanded then
                    raw
                        """<svg class="size-2.5" viewBox="0 0 10 10" fill="none" stroke="currentColor" stroke-width="1.2"><path d="M2 5h6"/></svg>"""
                else
                    raw
                        """<svg class="size-2.5" viewBox="0 0 10 10" fill="currentColor"><path d="M1 1h6L1 7zm8 8H3l6-6Z"/></svg>"""
            }
        }

    let fixtureAppearance (id: string) =
        let signal = "_" + id.Replace('-', '_') + "_mode"

        div {
            _attr ("data-signals__ifmissing", $"{{{signal}: window.fveColorMode.current()}}")
            _dataInit $"${signal} = window.fveColorMode.current()"
            _dataOn ("fve-color-mode__window", $"${signal} = window.fveColorMode.current()")

            fveDropdownMenu {
                _id id

                fveDropdownMenuTrigger {
                    _ariaLabel "Choose fixture theme"

                    fun (attributes: HtmlAttribute list) ->
                        button {
                            for attribute in attributes do
                                attribute

                            _class
                                "inline-flex min-h-8 items-center rounded-[var(--fve-radius-control)] px-2 text-sm font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                            "Theme"
                        }
                }

                for value, caption in [ "system", "System"; "light", "Light"; "dark", "Dark" ] do
                    let isChecked = value = "system"

                    fveDropdownMenuItem {
                        _role "menuitemradio"
                        _ariaChecked isChecked
                        _dataAttr ("aria-checked", $"${signal} == '{value}' ? 'true' : 'false'")
                        _dataOn ("click", $"window.fveColorMode.set('{value}')")
                        caption
                    }
            }
        }

    let browserReviewFrame expanded =
        fveBrowser {
            _expanded expanded
            _address "/components/browser"

            div {
                _class "grid min-h-64 content-center gap-3 p-6 text-center"

                strong {
                    _class "text-lg"
                    "Content preview"
                }

                p { "One frame at either size." }
            }

            fveBrowserWindowControls { fixtureWindowControl "/components/browser" "browser-review" expanded }

            fveBrowserToolbarActions { fixtureAppearance "browser-review-theme" }
        }

    let browserWithControls = browserReviewFrame false

    let phoneReviewFrame expanded =
        fvePhone {
            _expanded expanded

            div {
                _class "flex h-full min-h-0 flex-col"

                div {
                    _class "shrink-0 border-b border-[var(--fve-border)] p-4 text-center font-semibold"
                    "Messages"
                }

                div {
                    _class "min-h-0 flex-1 overflow-auto p-4"

                    p {
                        _class "rounded-lg bg-[var(--fve-surface-hover)] p-3 text-base"
                        "Confirm arrival time."
                    }
                }

                div {
                    _class "shrink-0 border-t border-[var(--fve-border)] p-4 text-sm text-[var(--fve-muted-text)]"
                    "Message preview"
                }
            }

            fvePhoneToolbar {
                div {
                    _class "flex min-w-0 flex-wrap items-center gap-3"
                    fixtureWindowControl "/components/phone" "phone-review" expanded

                    span {
                        _class "min-w-0 flex-1 text-sm font-medium"
                        "Phone preview"
                    }

                    fixtureAppearance "phone-review-theme"
                }
            }
        }

    let phoneWithControls = phoneReviewFrame false

    let expandedReviewFixture phone =
        div {
            _id "docs-navigation-root"
            _data ("fve-navigation-root", "true")
            _data ("fve-app-mode-root", "true")
            _dataOn ("keydown__window", Example.Server.Navigation.enhancement.appModeExitAction)

            main {
                _id "main-content"
                _tabindex -1
                _class "outline-none"

                if phone then
                    phoneReviewFrame true
                else
                    browserReviewFrame true
            }
        }

    let private detailsSurface (content: HtmlElement) =
        gallerySurface (
            div {
                _class "p-[12px] sm:p-6"
                content
            }
        )

    let private calendarSurface (content: HtmlElement) =
        gallerySurface (
            div {
                _class "grid w-full min-w-0 grid-cols-1 gap-4 p-[12px] sm:p-6"
                content
            }
        )

    let private overlaySurface (content: HtmlElement) =
        gallerySurface (
            div {
                _class "relative min-h-[32rem] overflow-hidden bg-[var(--fve-surface-subtle)] p-4"
                content
            }
        )

    let private prose content =
        p {
            _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)] [overflow-wrap:anywhere]"
            text content
        }

    let private code language (source: string) =
        fveCodeBlock {
            _language language
            source
        }

    let private bullets items =
        ul {
            _class
                "m-0 list-disc pl-5 text-base leading-relaxed text-[var(--fve-muted-text)] marker:text-[var(--fve-brand-ring)]"

            for item in items do
                li {
                    _class "mt-2 first:mt-0"
                    text item
                }
        }

    let private buildingBlockLinks label (items: (string * string) list) =
        div {
            _class "flex flex-wrap items-baseline gap-x-4 gap-y-2"

            h2 {
                _class "text-base font-normal"
                "Built with"
            }

            nav {
                _ariaLabel label
                _class "flex flex-wrap gap-x-4 gap-y-2"

                for path, itemLabel in items do
                    a {
                        _href path

                        _class
                            "font-semibold text-[var(--fve-brand-text)] underline decoration-[var(--fve-brand-ring)] underline-offset-[0.18em]"

                        text itemLabel
                    }
            }
        }

    let private apiOwner (registration: DocPage) =
        if
            registration.path.StartsWith("/components/page-examples/", StringComparison.Ordinal)
            || registration.id = formLayoutsRegistration.id
        then
            None
        else
            Some(registration.path.Substring("/components/".Length))

    let private componentKey (registration: DocPage) =
        registration.path.Substring("/components/".Length)

    let private componentDescription (registration: DocPage) =
        match componentKey registration with
        | "button" ->
            "Trigger actions and submit forms with explicit text, icon, color, variant, size, and pending content."
        | "button-group" ->
            "Join related buttons, native links, Select controls, menu triggers and muted text segments. Each control keeps its own semantics and keyboard behavior; labels are noninteractive, not disabled buttons. Native links retain real destinations."
        | "dropdown-menu" -> "Present contextual actions and typed choices in an accessible dismissible menu."
        | "badge" -> "Label compact metadata and semantic states with typed colors and presentation variants."
        | "notice" ->
            "Present persistent contextual guidance with independent color, visual treatment, and accessible announcement policy."
        | "notification" -> "Present ephemeral, optionally actionable feedback in an accessible bottom-end stack."
        | "loading-indicator" -> "Communicate indeterminate work with a labelled, reusable loading indicator."
        | "progress" -> "Communicate determinate, indeterminate, complete, and failed progress."
        | "empty-state" -> "Explain an empty collection or unavailable result and provide its next useful action."
        | "skeleton" -> "Reserve content structure while data is loading without implying completed content."
        | "table" ->
            "Render typed tabular data with responsive layouts, row headers, selection, hierarchy, and actions."
        | "description-list" -> "Present labelled record details and semantic values as a responsive description list."
        | "metric" -> "Present a labelled value, trend, and supporting context as a compact metric."
        | "avatar" -> "Represent a person or entity with an image, initials fallback, size, and accessible name."
        | "copy-reveal" -> "Copy or deliberately reveal sensitive text with accessible text or icon actions."
        | "day-calendar" -> "Display one day's events in a scrollable time grid with accessible event destinations."
        | "week-calendar" -> "Display a week in seven shared time columns with horizontal and vertical scrolling."
        | "month-calendar" -> "Display a month in full or compact form, or select a date or range."
        | "year-calendar" -> "Survey a year through twelve responsive compact month calendars."
        | "kbd" -> "Render keyboard keys and shortcut groups as compact instructional content."
        | "separator" -> "Separate related content visually while preserving the intended semantic orientation."
        | "input" -> "One native input. Use Field for labels and feedback, and Input Group for icons, text and actions inside a shared frame."
        | "input-group" -> "One shared frame around an Input or Textarea, with icons, text, actions or content above and below. Field owns the external label and feedback."
        | "field" -> "Own labels, descriptions, validation, orientation, and custom form-control composition."
        | "textarea" -> "Collect multiline text with a native textarea. Compose labels and feedback with Field."
        | "file-selection" -> "Collect one or more files with accessible native selection and validation."
        | "tag-input" -> "Add and remove submitted text tags with native labels, validation, and read-only states."
        | "error-summary" -> "Summarize validation errors and link users to the affected fields."
        | "select" ->
            "Choose single or multiple string values with search, keyboard, form and positioning behavior. Single selections update their named value input and emit a bubbling change event. Ordinary single selects can opt into a styled native fallback before Datastar initializes; only the active control submits a value."
        | "date-picker" -> "Compose a native labelled date control with popover selection and caller-owned feedback."
        | "chart" ->
            "Create line, column, bar, Sankey and other ECharts charts with ordinary F# options. The host handles lazy loading, theme, resizing and cleanup; your application owns the data and interactions."
        | "checkbox" ->
            "Submit a checked native value with explicit Field labels and feedback. Unchecked controls follow native form semantics."
        | "switch" -> "Toggle an immediate binary setting with accessible checked and unavailable states."
        | "toggle-button" -> "Toggle a pressed action with typed presentation, size, and availability states."
        | "toggle-group" -> "Choose one or more pressed values from an accessible oriented toggle group."
        | "radio-group" -> "Choose one submitted value from an accessible typed radio group."
        | "choice-cards" -> "Choose one rich option from labelled cards with descriptions and supporting content."
        | "breadcrumbs" -> "Show the current hierarchy with semantic links and responsive overflow."
        | "side-nav" ->
            "Compose optional header, context, scrolling content and footer slots. Use SideNavItem for links, SideNavSection for static headings, and SideNavGroup for collapsible groups. SideNavRow links have no default trailing indicator; menu rows retain a downward chevron. Add an avatar with withLeading, secondary text or email with withDescription, and optional trailing content with withTrailing. Sections use small, muted labels without indentation, and groups guide nested children with vertical lines."
        | "tabs" -> "Switch between labelled local panels with orientation-aware keyboard navigation."
        | "pagination" -> "Navigate bounded result pages with current, previous, next, and compact range items."
        | "command" ->
            "Search grouped destinations and actions with ranked matching, keyboard navigation, and an optional dialog palette."
        | "dialog" ->
            "Present modal content with labelled structure, focus management, dismissal, and form composition."
        | "drawer" -> "Present modal detail or controls from a selected viewport edge."
        | "floating-panel" -> "Keep dismissible supporting content available beside the primary workflow."
        | "popover" -> "Present labelled interactive content adjacent to a trigger with focus and dismissal ownership."
        | "tooltip" ->
            "Provide delayed supplementary text for a labelled control without replacing its accessible name."
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
        | "media-library" -> "Browse and select media with filters, metadata, and responsive collection states."
        | _ -> $"Use the {registration.title} component in typed F# interfaces."

    let private titledSection id title content =
        DocumentationSection.create
            id
            title
            (h2 {
                _class "text-xl font-semibold text-[var(--fve-text)]"
                title
             }
             :: content)

    let installationSection selector = ComponentInstallation.section selector

    let private componentGallery (registration: DocPage) examples =
        match examples with
        | [] -> invalidOp $"Component page '{registration.path}' requires a canonical lead example."
        | lead :: variants ->
            let leadContent =
                [ fveExample {
                      _name lead.id
                      _label lead.title
                      _variant ExampleVariant.Lead
                      _language "fsharp"
                      fveExampleCode { lead.source }
                      lead.preview
                  }
                  yield! ComponentInstallation.exampleSetup (componentKey registration) lead.source ]

            let sections =
                [ installationSection (componentKey registration)
                  titledSection
                      "usage"
                      "Usage"
                      [ code "fsharp" lead.source
                        if registration.id = dropdownMenuRegistration.id then
                            p {
                                "Use Dropdown menu for commands and compact choices, with arrow-key navigation and typeahead. For rich content, fields or mixed controls, use "
                                a { _href "/components/popover"; _class "text-[var(--fve-brand-text)] underline"; "Popover" }
                                "."
                            }
                        if registration.id = popoverRegistration.id then
                            p {
                                "Use Popover for contextual content and forms with normal Tab navigation. It does not block the rest of the page. For a list of commands or compact choices, use "
                                a { _href "/components/dropdown-menu"; _class "text-[var(--fve-brand-text)] underline"; "Dropdown menu" }
                                "."
                            }
                        if registration.id = chartRegistration.id then
                            prose
                                "_options accepts JSON-compatible F# records or anonymous records using ECharts option names. Column and horizontal-bar charts both use type = \"bar\"; the category axis chooses their direction. Sankey uses nodes and links instead of axes. --fve-chart-height controls height. The default palette and text follow the owning theme; explicit ECharts colors remain caller-owned."

                            p {
                                "The value passed to "
                                Html.code { "_options" }
                                " follows the "
                                a {
                                    _href "https://echarts.apache.org/en/option.html"
                                    _class "text-[var(--fve-brand-text)] underline"
                                    "ECharts options API"
                                }
                                "."
                            }

                            prose
                                "Use _events to opt into ECharts events: click becomes chart-click, brushend becomes chart-brushend. evt.detail contains the original payload. After each options/theme update, chart-render exposes evt.detail.chart for application-owned JavaScript formatters or actions. _empty true displays the empty state without interpreting your datasets."

                            p {
                                "See the "

                                a {
                                    _href "/examples/application"
                                    _class "text-[var(--fve-brand-text)] underline"
                                    "Application template"
                                }

                                " for currency formatting, date-range brushing and server-owned filtering."
                            }
                        if registration.id = inputRegistration.id || registration.id = textareaRegistration.id then
                            prose "required, type, pattern and length constraints use the browser's validity rules. :invalid matches any currently invalid value; :user-invalid delays native error styling until interaction/submission. These examples use Datastar to show inline errors after blur or invalid submission, then recheck on input so the border, aria-invalid, associated message and summary clear together as soon as the value is valid. Form reset clears this temporary feedback."
                            prose "The nativeValidationAttributes helper is example-owned presentation, not an Input API or a second value model. Always validate submissions on the server. For server-only rules, recheck on the server and render _ariaInvalid with an associated Field error; a syntactically valid email does not prove that a server-only error is resolved. Read-only values remain submitted; disabled values do not."
                            p {
                                "Use "
                                a { _href "/components/input-group"; _class "text-[var(--fve-brand-text)] underline"; "Input Group" }
                                " for icons, prefixes, suffixes and actions inside the control frame."
                            }
                        if registration.id = fileSelectionRegistration.id then
                            prose "FileSelection keeps a native chooser and always provides a quiet Clear action when files are selected. Clear is hidden while empty or unavailable for editing; required fields can still be cleared. Clearing returns focus to the chooser and emits native input/change events. Its accessible name identifies the field and, for a single selection, the filename. The native control owns the filename or multiple-file count—there is no automatic duplicate list. Field owns the label, help and error."
                            prose "Validate a selection on change, or a missing required file when submitting—not when the file picker takes focus or is cancelled. accept only guides the picker. The validation example checks the filename extension for local feedback and uses setCustomValidity; your server validates the actual upload, including content, type and size. Selection alone does not upload anything."
                            prose "A richer multi-file list can offer per-file Remove actions and, when useful, Clear all. Keep that upload/queue composition in your application rather than adding it to every file input."
                        if registration.id = inputGroupRegistration.id then
                            prose "Compose ordinary fveInput or fveTextarea with fveInputGroupAddon. _position selects InlineStart (default), InlineEnd, BlockStart or BlockEnd. The control renders before addons in the DOM so Tab reaches the input before addon actions; logical positions follow the writing direction. Give meaningful text an ID and connect it with _ariaDescribedby; mark decorative icons _ariaHidden. Reuse ordinary buttons and menus—there are no group-specific control wrappers. Use Button Group instead to join separately bordered controls."
                        if registration.id = selectRegistration.id then
                            prose
                                "Compose fveSelectContent and fveSelectItem inside fveSelect or fveMultiSelect. The Select supplies its trigger and manages selection/form values; customize items with ordinary HTML."

                            prose
                                "Give remote content a stable ID and render it with the same helper for initial results and responses. The remote example includes the running handlers and routes. Items supply their own string values and labels; serialize domain values explicitly at the HTML boundary." ]
                  for item in variants do
                      DocumentationSection.create
                          item.id
                          item.title
                          ([ fveExample {
                                 _name item.id
                                 _label item.title
                                 _variant ExampleVariant.Gallery
                                 _language "fsharp"
                                 fveExampleCode { item.source }
                                 item.preview
                             } ]
                           @ ComponentInstallation.exampleSetup (componentKey registration) item.source
                           @ (item.note |> Option.map prose |> Option.toList))
                  if registration.id = sideNavRegistration.id then
                      titledSection
                          "expansion-and-scroll"
                          "Expansion and scroll"
                          [ prose
                                "Groups stay independently open or closed during navigation, history and refresh. Their initial state opens the active branch; remembered choices take precedence, including an explicit collapse of that branch. Content scroll is also remembered for the current browser tab."
                            prose
                                "Preferences use sessionStorage and default to the stable SideNav ID. Use _persistenceKey to separate unrelated navigation trees that reuse an ID, or to share preferences between responsive presentations of one tree. Give groups stable _id values when their order changes; otherwise their structural position and label identify them."
                            prose
                                "_expandedSignal keeps the host responsible for initializing and persisting that group's state. When storage is unavailable, toggles still work in the current document. Without Datastar, native links and details/summary remain usable, but cross-document preferences are not restored." ]
                  if registration.id = appShellRegistration.id then
                      titledSection
                          "complete-pages"
                          "Complete page examples"
                          [ p {
                                "For populated pages and a connected workflow, explore "

                                a {
                                    _href "/examples/application"
                                    "Application"
                                }

                                ". These shell examples intentionally show only the layout."
                            } ]
                  if
                      List.contains
                          registration.id
                          [ buttonRegistration.id
                            fieldRegistration.id
                            dialogRegistration.id
                            drawerRegistration.id
                            notificationRegistration.id ]
                  then
                      titledSection
                          "accessibility"
                          (if registration.id = notificationRegistration.id then
                               "Announcements"
                           else
                               "Labels and focus")
                          [ if registration.id = buttonRegistration.id then
                                prose
                                    "Text content names the button. For icon-only actions, provide _ariaLabel; see the icon-only examples above."
                            if registration.id = fieldRegistration.id then
                                prose
                                    "Connect fveLabel's _for to the control's _id. Connect descriptions and errors with _ariaDescribedby, and set _ariaInvalid when the field is invalid; the examples show those relationships explicitly."
                            if
                                registration.id = dialogRegistration.id
                                || registration.id = drawerRegistration.id
                            then
                                prose
                                    "Use the title part to name the dialog. Set _initialFocus to a control ID when the default first focus target is not appropriate; Escape dismisses the overlay and restores its opener."
                            if registration.id = notificationRegistration.id then
                                p {
                                    "Use _announcement to choose Static, Polite or Assertive announcements. See "

                                    a {
                                        _href "/components/notice#components-notice-announcement"
                                        _class "font-semibold text-[var(--fve-brand-text)] underline"
                                        "Notice's announcement example"
                                    }

                                    " for a live status update."
                                } ]
                  match apiOwner registration with
                  | Some owner ->
                      titledSection
                          "api-reference"
                          "API reference"
                          [ fveFSharpApiReference {
                                yield! (FSharpApiReference.forCategory owner (typeof<ButtonBuilder>))
                            } ]
                  | None -> () ]

            DocumentationPage.create registration.id registration.title
            |> DocumentationPage.withDescription (componentDescription registration)
            |> DocumentationPage.withLayout Gallery
            |> DocumentationPage.withRightRail TableOfContents
            |> DocumentationPage.withLead leadContent
            |> DocumentationPage.withSections sections

    let private pageExampleGallery (registration: DocPage) examples =
        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withLayout Gallery
        |> DocumentationPage.withRightRail NoRail
        |> DocumentationPage.withSections
            [ for item in examples do
                  DocumentationSection.create
                      item.id
                      item.title
                      ([ fveExample {
                             _name item.id
                             _label item.title
                             _variant ExampleVariant.Gallery
                             _language "fsharp"
                             fveExampleCode { item.source }
                             item.preview
                         } ]
                       @ (item.note |> Option.map prose |> Option.toList))
              if registration.id = accountManagementRegistration.id then
                  DocumentationSection.create
                      "building-blocks"
                      "Built with"
                      [ buildingBlockLinks
                            "Account management building blocks"
                            [ "/components/app-shell", "App shell"
                              "/components/page", "Page"
                              "/components/collection", "Collection"
                              "/components/detail", "Detail"
                              "/components/page-header", "Page header"
                              "/components/button", "Button"
                              "/components/dropdown-menu", "Dropdown menu"
                              "/components/form-layouts", "Form layouts" ] ] ]

    let private gallery (registration: DocPage) examples =
        if registration.path.StartsWith("/components/page-examples/", StringComparison.Ordinal) then
            pageExampleGallery registration examples
        else
            componentGallery registration examples

    let floatingPanelContent =
        div {
            _class "grid gap-3 text-sm"
            p { "Use this space for contextual guidance that should not block the primary task." }

            a {
                _href "/components/page-examples/operations-dashboard"
                _class "font-semibold text-[var(--fve-brand-text)] underline-offset-2 hover:underline"
                "Open operations"
            }
        }

    let floatingPanelExample =
        fveFloatingPanel {
            _id "workspace-guide"
            _label "Workspace guide"
            _description "Helpful context remains available beside the application."
            _boundary FloatingPanelBoundary.Container
            floatingPanelContent
        }

    let minimizedFloatingPanelExample =
        fveFloatingPanel {
            _id "minimized-workspace-guide"
            _label "Workspace guide"
            _boundary FloatingPanelBoundary.Container
            _state FloatingPanelState.Minimized
            floatingPanelContent
        }

    let floatingPanelChecklistContent =
        ol {
            _class "m-0 grid list-none gap-4 p-0 text-sm"

            li {
                p {
                    _class "font-medium"
                    "1. Review the examples"
                }

                a {
                    _href "/examples"
                    _class "font-semibold text-[var(--fve-brand-text)] underline underline-offset-2"
                    "Open examples"
                }
            }

            li {
                p {
                    _class "font-medium"
                    "2. Choose your components"
                }

                p {
                    _class "text-[var(--fve-muted-text)]"
                    "Complete"
                }
            }
        }

    let floatingPanelChecklistExample =
        fveFloatingPanel {
            _id "component-checklist"
            _label "Setup checklist"
            _description "Two steps to get started."
            _boundary FloatingPanelBoundary.Container
            floatingPanelChecklistContent
        }

    let notificationLeadItem sequence =
        fveNotification {
            _id $"saved-notification-{sequence}"
            _label "Changes saved"
            p { "Your changes are ready." }
        }

    let notificationLeadRegion sequence =
        let notification =
            if sequence > 0 then
                Some(notificationLeadItem sequence)
            else
                None

        fveNotificationRegion {
            _id "save-notifications"
            _ariaLabel "Workspace notifications"

            match (notification: HtmlElement option) with
            | Some notification -> notification
            | None -> ()

            _withinContainer true
        }

    let simpleNotificationExample =
        div {
            _dataSignals ("components_notification_sequence", [ "ifmissing" ], "0")

            div {
                _class "flex min-h-40 items-start justify-center p-6"

                fveButton {
                    _dataOn ("click", "$components_notification_sequence += 1; @post('/components/notifications/show')")
                    "Show notification"
                }
            }

            notificationLeadRegion 0
        }

    let notificationWithActionsExample =
        let action =
            fveButton {
                _variant ButtonVariant.Outline
                _size Size.Small

                _dataOn (
                    "click",
                    "evt.currentTarget.closest('[data-fve-notification]').querySelector('[role=status] div').textContent = 'The invitation remains pending.'"
                )

                "Undo"
            }

        fveNotificationRegion {
            _id "invitation-notifications"
            _ariaLabel "Invitation notifications"
            _withinContainer true

            fveNotification {
                _id "invitation-notification"
                _label "Invitation accepted"
                _color Color.Info
                _persistent true
                p { "You joined Northwind Outdoor." }
                fveNotificationActions { action }
            }
        }

    let timedNotificationItem sequence =
        fveNotification {
            _id $"review-notification-{sequence}"
            _label "Review needed"
            _color Color.Warning
            _ttl 3000
            p { "Two items still need your attention." }
        }

    let timedNotificationRegion sequence =
        fveNotificationRegion {
            _id "timed-notifications"
            _ariaLabel "Review notifications"
            _withinContainer true

            if sequence > 0 then
                timedNotificationItem sequence
        }

    let timedNotificationExample =
        div {
            _dataSignals ("components_timed_notification_sequence", [ "ifmissing" ], "0")

            div {
                _class "flex min-h-40 items-start justify-center p-6"

                fveButton {
                    _dataOn (
                        "click",
                        "$components_timed_notification_sequence += 1; @post('/components/notifications/show-timed')"
                    )

                    "Show 3-second notification"
                }
            }

            timedNotificationRegion 0
        }

    let progressExampleRegion percentage =
        fveProgress {
            _id "progress-example-value"
            _ariaLabel "Example task"
            _value (string percentage)
            _max "100"

            if percentage = 100 then
                _state ProgressState.Complete
        }

    let operationalProgressExample =
        div {
            _class "grid gap-4"
            _dataSignals "{components_progress_pending: false, components_progress_done: false}"
            progressExampleRegion 0

            // The trigger stays mounted while the server patches the progress region.
            fveButton {
                _id "progress-example-trigger"
                _class "justify-self-start"
                _color Color.Primary
                _variant ButtonVariant.Solid
                _dataIndicator "components_progress_pending"
                _dataAttr ("disabled", "$components_progress_pending")
                _dataAttr ("aria-busy", "$components_progress_pending ? 'true' : null")

                _dataOn (
                    "click",
                    "@post('/components/progress/run', {filterSignals: {exclude: /.*/}, requestCancellation: 'cleanup', retry: 'never'})"
                )

                span {
                    _dataText
                        "$components_progress_pending ? 'Running…' : $components_progress_done ? 'Run again' : 'Run example'"

                    "Run example"
                }
            }
        }

    let progressExampleHandler: HttpHandler =
        fun _ context ->
            task {
                let datastar = context.RequestServices.GetRequiredService<IDatastarService>()

                try
                    do! datastar.PatchSignalsAsync({| components_progress_done = false |}, context.RequestAborted)

                    for percentage in 0..10..100 do
                        // This short delay simulates work; real work supplies the completed amount.
                        if percentage > 0 then
                            do! System.Threading.Tasks.Task.Delay(300, context.RequestAborted)

                        do!
                            datastar.PatchElementsAsync(
                                progressExampleRegion percentage |> Render.toString,
                                cancellationToken = context.RequestAborted
                            )

                    do! datastar.PatchSignalsAsync({| components_progress_done = true |}, context.RequestAborted)
                with :? OperationCanceledException when context.RequestAborted.IsCancellationRequested ->
                    ()

                return Some context
            }

    let progressExampleRoutes: HttpHandler =
        Giraffe.Core.POST
        >=> route "/components/progress/run"
        >=> progressExampleHandler

    let indeterminateProgressExample =
        fveProgress {
            _ariaLabel "Preparing files"
            _ariaValuetext "Checking source files"

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "No completion percentage is available yet."
            }
        }

    let completedProgressExample =
        fveProgress {
            _ariaLabel "File upload"
            _value "100"
            _max "100"
            _ariaValuetext "100%"
            _state ProgressState.Complete
        }

    let failedProgressExample =
        fveProgress {
            _ariaLabel "File upload"
            _value "68"
            _max "100"
            _ariaValuetext "Stopped at 68%"
            _state ProgressState.Failed

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "The upload stopped. Try again."
            }
        }

    let defaultFileSelection =
        fveField {
            fveLabel {
                _for "selected-file"
                "File"
            }

            fveFileSelection {
                _id "selected-file"
                _name "selectedFile"
            }
        }

    let fileSelectionExample =
        fveField {
            fveLabel {
                _for "selected-files"
                "Files"
            }

            fveFileSelection {
                _id "selected-files"
                _name "selectedFiles"
                _accept ".csv,text/csv"
                _multiple true
                _ariaDescribedby "selected-files-help"
            }

            fveFieldDescription {
                _id "selected-files-help"
                "Choose one or more CSV files."
            }
        }

    let pdfSelectionValidation =
        "const file = el.files[0]; const message = !file ? 'Select a PDF document.' : !file.name.toLowerCase().endsWith('.pdf') ? 'The selected file must be a PDF.' : ''; el.setCustomValidity(message); $_pdf_selection_error = message;"

    let invalidFileSelectionExample =
        fveField {
            fveLabel { _for "required-file"; "Document (required)" }
            fveFileSelection {
                _id "required-file"
                _name "requiredFile"
                _accept ".pdf,application/pdf"
                _required true
                _ariaDescribedby "required-file-help"
                _dataSignals "{_pdf_selection_error: ''}"
                _dataOn ("change", pdfSelectionValidation)
                _dataOn ("invalid", pdfSelectionValidation)
                _dataOn ("reset__window", "if (evt.target === el.form) queueMicrotask(() => { if (!evt.defaultPrevented) { el.setCustomValidity(''); $_pdf_selection_error = ''; } })")
                _dataAttr ("aria-invalid", "$_pdf_selection_error ? 'true' : null")
                _dataAttr ("aria-describedby", "$_pdf_selection_error ? 'required-file-help required-file-error' : 'required-file-help'")
            }
            fveFieldDescription { _id "required-file-help"; "PDF only." }
            fveFieldError {
                _id "required-file-error"
                _style "display:none"
                _dataShow "$_pdf_selection_error"
                _dataText "$_pdf_selection_error"
            }
        }

    let disabledFileSelectionExample =
        fveField {
            fveLabel {
                _for "disabled-files"
                "Files unavailable"
            }

            fveFileSelection {
                _id "disabled-files"
                _name "disabledFiles"
                _disabled true
            }
        }

    let defaultAvatar =
        fveAvatar {
            _ariaLabel "Alex Morgan"
            "AM"
        }

    let avatarFallbackExample =
        div {
            _class "flex flex-wrap items-center gap-4"

            fveAvatar {
                _ariaLabel "Alex Morgan"
                _size Size.Small
                "AM"
            }

            fveAvatar {
                _ariaLabel "Jamie Lee"
                "JL"
            }

            fveAvatar {
                _ariaLabel "Riley Chen"
                _size Size.Large
                "RC"
            }
        }

    let avatarImageExample =
        fveAvatar {
            _ariaLabel "FSharp.ViewEngine"
            _src "/android-chrome-512x512.png"
            _size Size.Large
            "FV"
        }

    let decorativeAvatarExample =
        fveAvatar {
            _ariaLabel "Decorative profile mark"
            _ariaHidden true
            "AM"
        }

    let copyRevealExample =
        fveField {
            fveLabel {
                _for "demo-token"
                "Demo API token"
            }

            fveCopyReveal {
                _id "demo-token"
                _value "fve_demo_84fK2s"
            }
        }

    let revealedCopyRevealExample =
        fveField {
            fveLabel {
                _for "revealed-demo-token"
                "Revealed demo token"
            }

            fveCopyReveal {
                _id "revealed-demo-token"
                _value "fve_demo_safe"
                _revealed true
            }
        }

    let iconCopyRevealExample =
        fveField {
            fveLabel {
                _for "icon-demo-token"
                "Icon action demo token"
            }

            fveCopyReveal {
                _id "icon-demo-token"
                _value "fve_demo_icon"
                _iconButtons true
            }
        }

    let defaultCommand =
        fveCommand {
            _id "command-default"
            _ariaLabel "Find a component"

            fveCommandGroup {
                _label "Components"

                fveCommandItem {
                    _href "/components/table"
                    "Table"
                }

                fveCommandItem {
                    _href "/components/month-calendar"
                    "Month calendar"
                }
            }
        }

    let commandDocumentIcon =
        Svg.svg {
            _viewBox "0 0 24 24"
            _fill "none"
            _attr ("stroke", "currentColor")
            _attr ("stroke-width", "1.5")

            path {
                _attr ("stroke-linecap", "round")
                _attr ("stroke-linejoin", "round")
                _d "M9 12h6m-6 4h6M6 3h9l3 3v15H6V3Z"
            }
        }

    let commandGroups =
        [ fveCommandGroup {
              _label "Components"

              fveCommandItem {
                  _href "/components/table"
                  _shortcut "⌘T"
                  fveCommandItemLeading { commandDocumentIcon }
                  "Table"
              }

              fveCommandItem {
                  _href "/components/month-calendar"
                  _keywords [ "dates"; "schedule" ]
                  "Month calendar"
              }
          }
          fveCommandGroup {
              _label "Page examples"

              fveCommandItem {
                  _href "/components/page-examples/scheduling"
                  _shortcut "⌘S"
                  "Scheduling"
              }

              fveCommandItem {
                  _href "/components/page-examples/media-management"
                  "Media management"
              }
          } ]

    let groupedCommand =
        fveCommand {
            _id "command-groups"
            _ariaLabel "Search components and examples"
            _placeholder "Find a component or page…"

            for group in commandGroups do
                group
        }

    let commandActionsExample =
        div {
            _dataSignals "{commandFeedback: ''}"
            _class "grid w-full gap-3"

            fveCommand {
                _id "command-actions"
                _ariaLabel "Choose a local action"

                fveCommandGroup {
                    _label "Actions"

                    fveCommandItem {
                        _dataOn ("click", "$commandFeedback = 'Display settings selected.'")
                        "Display settings"
                    }

                    fveCommandItem {
                        _dataOn ("click", "$commandFeedback = 'Profile settings selected.'")
                        "Profile settings"
                    }

                    fveCommandItem {
                        _dataOn ("click", "$commandFeedback = 'Unavailable action.'")
                        "Unavailable action"
                        _disabled true
                    }
                }
            }

            p {
                _role "status"
                _class "text-sm text-[var(--fve-muted-text)]"
                _dataText "$commandFeedback"
            }
        }

    let commandPaletteExample =
        div {
            fveButton {
                for attribute in Command.triggerAttributes "command-palette" do
                    attribute

                "Open command palette"
            }

            fveCommandDialog {
                _id "command-palette"
                _ariaLabel "Navigate the catalog"

                for group in commandGroups do
                    group
            }
        }

    let emptyCommand =
        fveCommand {
            _id "command-empty"
            _ariaLabel "Search an empty collection"
            _emptyLabel "No commands are available."
        }

    let private calendarDemoDate = DateOnly(2026, 9, 17)

    let private calendarEventDestination id =
        "/components/page-examples/scheduling?item=" + id

    let calendarDemoEvents =
        [ CalendarEvent.create
              "lesson-201"
              "Coastal trail lesson"
              calendarDemoDate
              (calendarEventDestination "lesson-201")
          |> CalendarEvent.withTime (TimeOnly(10, 0)) (TimeOnly(11, 30))
          |> CalendarEvent.withDetail "Maya and Sam · Andy Meier"
          CalendarEvent.create "camp-017" "Beginner riding camp" calendarDemoDate (calendarEventDestination "camp-017")
          |> CalendarEvent.withTime (TimeOnly(10, 30)) (TimeOnly(12, 0))
          |> CalendarEvent.withDetail "Overlaps the trail lesson by one hour"
          CalendarEvent.create
              "lesson-202"
              "Cornering fundamentals"
              (calendarDemoDate.AddDays 1)
              (calendarEventDestination "lesson-202")
          |> CalendarEvent.withTime (TimeOnly(9, 0)) (TimeOnly(10, 0))
          CalendarEvent.create
              "ride-019"
              "Open track practice"
              (calendarDemoDate.AddDays 2)
              (calendarEventDestination "ride-019")
          |> CalendarEvent.withTime (TimeOnly(13, 0)) (TimeOnly(15, 0))
          CalendarEvent.create
              "return-020"
              "Equipment return"
              (calendarDemoDate.AddDays 7)
              (calendarEventDestination "return-020")
          |> CalendarEvent.withTime (TimeOnly(16, 0)) (TimeOnly(16, 30)) ]

    let defaultCalendarEvent =
        CalendarEvent.create
            "lesson-201-default"
            "Coastal trail lesson"
            calendarDemoDate
            (calendarEventDestination "lesson-201-default")

    let lineChartOptions =
        {| tooltip = {| trigger = "axis" |}
           grid =
            {| left = 8
               right = 16
               top = 16
               bottom = 8
               containLabel = true |}
           xAxis =
            {| ``type`` = "category"
               data = [| "Jan"; "Feb"; "Mar" |] |}
           yAxis = {| ``type`` = "value" |}
           series =
            [| {| name = "Revenue"
                  ``type`` = "line"
                  data = [| 24; 36; 42 |] |} |] |}

    let defaultChart =
        fveChart {
            _id "line-chart"
            _ariaLabel "Revenue: January 24, February 36, March 42"
            _options lineChartOptions
        }

    let columnChartOptions =
        {| tooltip = {| trigger = "axis" |}
           grid =
            {| left = 8
               right = 16
               top = 16
               bottom = 8
               containLabel = true |}
           xAxis =
            {| ``type`` = "category"
               data = [| "Jan"; "Feb"; "Mar" |] |}
           yAxis = {| ``type`` = "value" |}
           series =
            [| {| name = "Revenue"
                  ``type`` = "bar"
                  data = [| 24; 36; 42 |] |} |] |}

    let columnChart =
        fveChart {
            _id "column-chart"
            _ariaLabel "Revenue by month, as columns"
            _options columnChartOptions
        }

    let barChartOptions =
        {| tooltip = {| trigger = "axis" |}
           grid =
            {| left = 8
               right = 16
               top = 16
               bottom = 8
               containLabel = true |}
           xAxis = {| ``type`` = "value" |}
           yAxis =
            {| ``type`` = "category"
               data = [| "Jan"; "Feb"; "Mar" |] |}
           series =
            [| {| name = "Revenue"
                  ``type`` = "bar"
                  data = [| 24; 36; 42 |] |} |] |}

    let barChart =
        fveChart {
            _id "bar-chart"
            _ariaLabel "Revenue by month, as horizontal bars"
            _options barChartOptions
        }

    let sankeyChartOptions =
        {| tooltip = {| trigger = "item" |}
           series =
            [| {| ``type`` = "sankey"
                  left = 8
                  right = 8
                  top = 16
                  bottom = 16
                  data =
                   [| {| name = "Income"
                         label = {| position = "right" |} |}
                      {| name = "Costs"
                         label = {| position = "left" |} |}
                      {| name = "Savings"
                         label = {| position = "left" |} |} |]
                  links =
                   [| {| source = "Income"
                         target = "Costs"
                         value = 70 |}
                      {| source = "Income"
                         target = "Savings"
                         value = 30 |} |]
                  lineStyle = {| color = "gradient"; opacity = 0.35 |} |} |] |}

    let sankeyChart =
        fveChart {
            _id "sankey-chart"
            _ariaLabel "Income 100 flows into costs 70 and savings 30"
            _options sankeyChartOptions
        }

    let chartTypeControl (label: string) options =
        fveButton {
            _dataOn (
                "click",
                $"$_chart_element.setAttribute('options', JSON.stringify({System.Text.Json.JsonSerializer.Serialize(options)}))"
            )

            label
        }

    let interactiveChart =
        div {
            _class "grid min-w-0 gap-3"
            _dataSignals "{ _chart_selection: 'Click a point or column.' }"

            fveChart {
                _id "interactive-values-chart"
                _dataRef "_chart_element"
                _ariaLabel "Revenue with application-owned chart type and selection"
                _options lineChartOptions
                _events [ "click" ]
                _dataOn ("chart-click", "$_chart_selection = evt.detail.name + ': ' + evt.detail.value")
            }

            div {
                _class "flex flex-wrap gap-2"
                chartTypeControl "Show line" lineChartOptions
                chartTypeControl "Show columns" columnChartOptions
            }

            p {
                _role "status"
                _dataText "$_chart_selection"
                "Click a point or column."
            }
        }

    let emptyChart =
        fveChart {
            _id "empty-values-chart"
            _ariaLabel "Revenue with no available data"
            _options {| series = Array.empty<obj> |}
            _empty true
        }

    let defaultDayCalendar =
        fveDayCalendar {
            _label "Coastal training day"
            _date calendarDemoDate
            defaultCalendarEvent
        }

    let defaultWeekCalendar =
        fveWeekCalendar {
            _label "Coastal training week"
            _date calendarDemoDate
            defaultCalendarEvent
        }

    let defaultMonthCalendar =
        fveMonthCalendar {
            _label "Coastal training month"
            _date calendarDemoDate
            defaultCalendarEvent
        }

    let defaultYearCalendar =
        fveYearCalendar {
            _label "Coastal training year"
            _date calendarDemoDate
            defaultCalendarEvent
        }

    let calendarDateDestination (date: DateOnly) =
        "/components/page-examples/scheduling?view=day&range="
        + string (date.DayNumber - calendarDemoDate.DayNumber)

    let calendarDayExample =
        fveDayCalendar {
            _label "Thursday sessions"
            _date calendarDemoDate
            yield! (calendarDemoEvents: CalendarEvent<string> list)

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.dates CalendarView.Day calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let calendarWeekExample =
        fveWeekCalendar {
            _label "Training week"
            _date calendarDemoDate
            yield! (calendarDemoEvents: CalendarEvent<string> list)

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.dates CalendarView.Week calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let calendarMonthEvents =
        calendarDemoEvents
        @ [ CalendarEvent.create
                "training-day"
                "Team training"
                calendarDemoDate
                (calendarDateDestination calendarDemoDate)
            |> CalendarEvent.withEndDate (calendarDemoDate.AddDays 4)
            CalendarEvent.create
                "equipment-preparation"
                "Equipment preparation"
                (calendarDemoDate.AddDays 1)
                (calendarDateDestination (calendarDemoDate.AddDays 1))
            |> CalendarEvent.withEndDate (calendarDemoDate.AddDays 3)
            CalendarEvent.create
                "site-preparation"
                "Site preparation"
                (DateOnly(2026, 8, 30))
                (calendarDateDestination (DateOnly(2026, 8, 31)))
            |> CalendarEvent.withEndDate (DateOnly(2026, 9, 2))
            CalendarEvent.create
                "course-setup"
                "Course setup"
                (DateOnly(2026, 9, 28))
                (calendarDateDestination (DateOnly(2026, 9, 28)))
            |> CalendarEvent.withEndDate (DateOnly(2026, 10, 2))
            CalendarEvent.create
                "setup-review"
                "Course setup and safety review"
                calendarDemoDate
                (calendarEventDestination "setup-review")
            |> CalendarEvent.withTime (TimeOnly(8, 30)) (TimeOnly(9, 15))
            |> CalendarEvent.withDetail "Review the route and equipment before the first session."
            CalendarEvent.create
                "team-debrief"
                "Afternoon team debrief"
                calendarDemoDate
                (calendarEventDestination "team-debrief")
            |> CalendarEvent.withTime (TimeOnly(15, 30)) (TimeOnly(16, 0)) ]

    let calendarMonthExample =
        fveMonthCalendar {
            _label "Team calendar"
            _date calendarDemoDate
            yield! (calendarMonthEvents: CalendarEvent<string> list)
            _id "team-month-calendar"

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.dates CalendarView.Month calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let compactMonthCalendar =
        fveMonthCalendar {
            _label "Training dates"
            _date calendarDemoDate
            yield! (calendarDemoEvents: CalendarEvent<string> list)
            _layout MonthCalendarLayout.Compact

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.compactMonthDates calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let calendarYearExample =
        fveYearCalendar {
            _label "Team year"
            _date calendarDemoDate
            yield! (calendarDemoEvents: CalendarEvent<string> list)

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.dates CalendarView.Year calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let defaultBrowser =
        fveBrowser {
            div {
                _class "p-[12px] text-center"
                "Content preview"
            }
        }

    let defaultPhone =
        fvePhone {
            div {
                _class "grid h-full place-items-center p-[12px] text-center"
                "Screen preview"
            }
        }

    let defaultBottomNavigation =
        fveBottomNavigation {
            _id "default-bottom-navigation"
            _ariaLabel "Default primary navigation"

            fveBottomNavigationItem {
                _href "/components"
                "Home"
            }

            fveBottomNavigationItem {
                _href "/components/table"
                "Items"
            }
        }

    let mediaLibraryExample =
        let library =
            MediaLibrary.create
                "product-media"
                "Site artwork"
                "assetIds"
                [ MediaAsset.create
                      "social-card"
                      "Social card"
                      "/social-card.png"
                      "FSharp.ViewEngine — Typed HTML views for F#"
                      LedgerAccounts
                  |> MediaAsset.withDetail "1200 × 630 · PNG"
                  |> MediaAsset.primary
                  MediaAsset.create
                      "app-icon"
                      "App icon"
                      "/android-chrome-512x512.png"
                      "FSharp.ViewEngine blue code-mark icon"
                      LedgerAccounts
                  |> MediaAsset.withDetail "512 × 512 · PNG"
                  MediaAsset.create
                      "touch-icon"
                      "Touch icon"
                      "/apple-touch-icon.png"
                      "FSharp.ViewEngine blue code-mark touch icon"
                      LedgerAccounts
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

                    for state, label in
                        [ "empty", "Show empty"
                          "pending", "Simulate loading"
                          "error", "Simulate error"
                          "ready", "Restore media" ] do
                        button {
                            _type "button"
                            _dataOn ("click", $"$mediastate = '{state}'")

                            _class
                                "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)]"

                            label
                        }
                }

                p {
                    _dataShow "$mediastate == 'empty'"

                    _class
                        "rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"

                    "No media assets. Upload an image to begin."
                }

                p {
                    _dataShow "$mediastate == 'pending'"
                    _role "status"
                    _ariaLive "polite"

                    _class
                        "rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"

                    "Loading media…"
                }

                div {
                    _dataShow "$mediastate == 'error'"
                    _role "alert"

                    _class
                        "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-4 text-sm text-[var(--fve-critical-text)]"

                    span { "Media could not be loaded." }

                    button {
                        _type "button"
                        _dataOn ("click", "$mediastate = 'ready'")

                        _class
                            "rounded-[var(--fve-radius-control)] px-3 py-2 font-semibold ring-1 ring-[var(--fve-critical-ring)]"

                        "Retry media"
                    }
                }

                div {
                    _dataShow "$mediastate == 'ready'"
                    _class "grid gap-4"
                    libraryWithActions

                    div {
                        _class "flex flex-wrap gap-2"

                        fveButton {
                            _dataOn (
                                "click",
                                "document.getElementById('media-edit-feedback').textContent = 'Selected asset IDs: ' + Array.from(document.querySelectorAll('#product-media input:checked')).map(input => input.value).join(', ')"
                            )

                            "Inspect selected assets"
                        }

                        fveButton {
                            _dataOn (
                                "click",
                                "document.getElementById('product-media')?.dispatchEvent(new CustomEvent('fve-selection-clear'))"
                            )

                            "Clear selection"
                        }
                    }

                    fveField {
                        fveLabel {
                            _for "media-alt-text"
                            "Alt text"
                        }

                        fveTextarea {
                            _id "media-alt-text"
                            _name "altText"
                            _value "FSharp.ViewEngine — Typed HTML views for F#"
                            _ariaDescribedby (String.concat " " [ ("media-alt-text") + "-description" ])
                        }

                        fveFieldDescription {
                            _id ("media-alt-text" + "-description")
                            "Describe the selected asset for people who cannot see it."
                        }
                    }

                    fveButton {
                        _dataOn (
                            "click",
                            "(() => { const value = document.getElementById('media-alt-text').value; document.querySelectorAll('#product-media input:checked').forEach(input => input.closest('li').querySelector('img').alt = value); document.getElementById('media-edit-feedback').textContent = 'Alt text updated for selected media.' })()"
                        )

                        "Save alt text"
                    }

                    fveField {
                        fveLabel {
                            _for "media-replacement"
                            "Replacement image"
                        }

                        fveFileSelection {
                            _id "media-replacement"
                            _name "replacementImage"
                            _accept "image/*"
                            _ariaDescribedby "media-replacement-help"
                        }

                        fveFieldDescription {
                            _id "media-replacement-help"
                            "Choose a local image; this provider-free example does not upload it."
                        }
                    }

                    fveButton {
                        _dataOn (
                            "click",
                            "document.querySelectorAll('#product-media input:checked').forEach(input => input.closest('li').querySelector('img').src = '/favicon-32x32.png'); document.getElementById('media-edit-feedback').textContent = 'Demo replacement applied to selected media.'"
                        )

                        "Apply demo replacement"
                    }

                    p {
                        _id "media-edit-feedback"
                        _role "status"
                        _ariaLive "polite"
                        _class "text-sm text-[var(--fve-muted-text)]"
                    }
                }
            }

    let traceViewerIntegrationExample =
        div {
            _dataSignals "{tracestate: 'ready'}"
            _class "grid gap-3"

            div {
                _class "flex flex-wrap gap-2"

                for state, label in
                    [ "ready", "Show trace"
                      "pending", "Load trace"
                      "empty", "Show empty trace"
                      "error", "Simulate trace error" ] do
                    button {
                        _type "button"
                        _dataOn ("click", $"$tracestate = '{state}'")

                        _class
                            "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)]"

                        label
                    }
            }

            p {
                _dataShow "$tracestate == 'pending'"
                _role "status"
                _class "text-sm text-[var(--fve-muted-text)]"
                "Loading trace…"
            }

            p {
                _dataShow "$tracestate == 'empty'"
                _class "text-sm text-[var(--fve-muted-text)]"
                "No spans matched this trace query."
            }

            div {
                _dataShow "$tracestate == 'error'"
                _role "alert"

                _class
                    "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)]"

                span { "Trace data could not be loaded." }

                button {
                    _type "button"
                    _dataOn ("click", "$tracestate = 'ready'")

                    _class
                        "rounded-[var(--fve-radius-control)] px-3 py-2 font-semibold ring-1 ring-[var(--fve-critical-ring)]"

                    "Retry trace"
                }
            }

            figure {
                _dataShow "$tracestate == 'ready'"
                _class "grid gap-3"

                div {
                    _tabindex 0
                    _ariaLabel "Scrollable trace diagram"

                    _class
                        "overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-4 ring-1 ring-[var(--fve-border)]"

                    svg {
                        _viewBox "0 0 720 180"
                        _role "img"
                        _ariaLabel "Request trace from browser through API and database"
                        _class "min-w-[40rem] w-full"

                        line {
                            _x1 120
                            _y1 90
                            _x2 600
                            _y2 90
                            _stroke "currentColor"
                            _strokeWidth 2
                        }

                        for x, label, duration in
                            [ 120, "Browser", "0 ms"; 360, "API", "42 ms"; 600, "Database", "18 ms" ] do
                            circle {
                                _cx x
                                _cy 90
                                _r 34
                                _fill "var(--fve-brand-subtle)"
                                _stroke "var(--fve-brand-solid)"
                                _strokeWidth 2
                            }

                            textElement {
                                _x x
                                _y 86
                                _textAnchor "middle"
                                _fill "currentColor"
                                label
                            }

                            textElement {
                                _x x
                                _y 108
                                _textAnchor "middle"
                                _fill "currentColor"
                                duration
                            }
                    }
                }

                figcaption {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Trace request-84f2 · 60 ms total. The ordered list remains the accessible source of truth."
                }

                ol {
                    _class "grid gap-2"

                    for label, detail in
                        [ "Browser", "GET /accounts · starts at 0 ms"
                          "API", "Authorization and query · 42 ms"
                          "Database", "SELECT accounts · 18 ms" ] do
                        li {
                            _class
                                "flex items-start justify-between gap-3 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface-subtle)] p-3 text-sm"

                            strong { label }

                            span {
                                _class "text-right text-[var(--fve-muted-text)]"
                                detail
                            }
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

                        _class
                            "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] aria-pressed:bg-[var(--fve-brand-subtle)] aria-pressed:text-[var(--fve-brand-text)]"

                        label
                    }
            }

            div {
                _tabindex 0
                _ariaLabel "Scrollable balance chart"

                _class
                    "overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-4 ring-1 ring-[var(--fve-border)]"

                for value, label, points in
                    [ "30d", "Thirty-day balance: $24,820", "0,130 120,112 240,122 360,76 480,58 600,32"
                      "90d", "Ninety-day balance: $24,820", "0,150 120,138 240,96 360,118 480,70 600,32" ] do
                    svg {
                        _viewBox "0 0 600 180"
                        _role "img"
                        _ariaLabel label
                        _dataShow $"$chartperiod == '{value}'"
                        _class "min-w-[36rem] w-full"

                        line {
                            _x1 0
                            _y1 160
                            _x2 600
                            _y2 160
                            _stroke "var(--fve-border)"
                            _strokeWidth 1
                        }

                        polyline {
                            _points points
                            _fill "none"
                            _stroke "var(--fve-brand-solid)"
                            _strokeWidth 4
                            _vectorEffect "non-scaling-stroke"
                        }
                    }
            }

            table {
                _class "w-full text-sm"

                caption {
                    _class "text-left font-semibold text-[var(--fve-text)]"
                    "Balance data"
                }

                thead {
                    tr {
                        th {
                            _scope "col"
                            _class "py-2 text-left"
                            "Period"
                        }

                        th {
                            _scope "col"
                            _class "py-2 text-right"
                            "Closing balance"
                        }
                    }
                }

                tbody {
                    tr {
                        _dataShow "$chartperiod == '30d'"

                        td {
                            _class "py-2"
                            "30 days"
                        }

                        td {
                            _class "py-2 text-right"
                            "$24,820"
                        }
                    }

                    tr {
                        _dataShow "$chartperiod == '90d'"

                        td {
                            _class "py-2"
                            "90 days"
                        }

                        td {
                            _class "py-2 text-right"
                            "$24,820"
                        }
                    }
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

                    for value, label, preview in
                        [ "northwind", "Northwind", "Can we move pickup?"
                          "contoso", "Contoso", "Thanks for the update." ] do
                        li {
                            button {
                                _type "button"
                                _dataOn ("click", $"$thread = '{value}'")
                                _dataAttr ("aria-pressed", $"$thread == '{value}' ? 'true' : 'false'")

                                _class
                                    "w-full rounded-[var(--fve-radius-control)] p-3 text-left ring-1 ring-[var(--fve-border)] aria-pressed:bg-[var(--fve-brand-subtle)]"

                                strong {
                                    _class "block"
                                    label
                                }

                                span {
                                    _class "block truncate text-sm text-[var(--fve-muted-text)]"
                                    preview
                                }
                            }
                        }
                }
            }

            section {
                _ariaLabel "Current conversation"
                _class "grid min-w-0 gap-3"

                div {
                    _dataShow "$thread == 'northwind'"

                    h3 {
                        _class "font-semibold"
                        "Northwind"
                    }

                    p {
                        _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface-subtle)] p-3"
                        "Can we move pickup to 10:30 AM?"
                    }
                }

                div {
                    _dataShow "$thread == 'contoso'"

                    h3 {
                        _class "font-semibold"
                        "Contoso"
                    }

                    p {
                        _class "rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface-subtle)] p-3"
                        "Thanks for the update."
                    }
                }

                fveField {
                    fveLabel {
                        _for "reply"
                        "Reply"
                    }

                    fveTextarea {
                        _id "reply"
                        _name "reply"
                        _ariaDescribedby (String.concat " " [ ("reply") + "-description" ])
                    }

                    fveFieldDescription {
                        _id ("reply" + "-description")
                        "Messages in this bounded example stay local to the rendered page."
                    }
                }

                fveButton {
                    _dataOn (
                        "click",
                        "document.getElementById('message-feedback').textContent = 'Message queued in this resettable demo.'"
                    )

                    "Send message"
                }

                p {
                    _id "message-feedback"
                    _role "status"
                    _ariaLive "polite"
                    _class "text-sm text-[var(--fve-muted-text)]"
                }
            }
        }

    let private shellSource =
        [ "ShellDestination"; "shellDestinationKey"; "shellDestinationUrl" ]

    let private formSource = [ "choiceSubmitButton"; "choiceResult" ]
    let private selectSource = [ "selectStatusOptions" ]
    let private comboboxSource = [ "searchItems"; "itemOptions" ]
    let private sideNavSource = [ "nestedSideNav"; "sideNavExample" ]

    let paginationExamples requestedPage =
        [ sample
              "pagination"
              "Page navigation"
              [ "PaginationDestination"
                "paginationDestinationUrl"
                "paginationConfig"
                "paginationPreview" ]
              (centered (paginationPreviewRegion requestedPage))
          sample
              "pagination-numbers"
              "Numbers only"
              [ "PaginationDestination"
                "paginationDestinationUrl"
                "paginationConfig"
                "paginationVariantPreview" ]
              (centered (paginationVariantPreview PaginationVariant.NumbersOnly requestedPage))
          sample
              "pagination-edges"
              "Previous and next only"
              [ "PaginationDestination"
                "paginationDestinationUrl"
                "paginationConfig"
                "paginationVariantPreview" ]
              (centered (paginationVariantPreview PaginationVariant.PreviousNextOnly requestedPage)) ]

    let paginationGalleryExamples requestedPage =
        sample "pagination-default" "Default" [ "defaultPagination" ] (fieldSurface defaultPagination)
        :: paginationExamples requestedPage

    type ShellLayoutDestination =
        | Dashboard
        | Projects
        | Preferences

    let shellLayoutLabel =
        function
        | Dashboard -> "Dashboard"
        | Projects -> "Projects"
        | Preferences -> "Settings"

    let shellLayoutUrl =
        function
        | Dashboard -> "/components/app-shell"
        | Projects -> "/components/app-shell?section=projects"
        | Preferences -> "/components/app-shell?section=settings"

    let shellLayoutExample id width bottomNavigation collapsible current =
        let layoutLabel =
            if bottomNavigation then
                "Full-width workspace"
            else
                "Constrained workspace"

        let header =
            fveSideNavHeader {
                _label "Workspace"

                fveSideNavHeaderCompact {
                    span {
                        _ariaHidden true
                        _class "font-semibold"
                        "W"
                    }
                }
            }

        let navigation =
            fveSideNav {
                _id (id + "-navigation")
                _ariaLabel (layoutLabel + " navigation")
                header

                fveSideNavContent {
                    fveSideNavItem {
                        _href (shellLayoutUrl Dashboard)
                        "Dashboard"
                        fveSideNavLeading { navigationGlyph }
                    }

                    fveSideNavItem {
                        _href (shellLayoutUrl Projects)
                        "Projects"
                        fveSideNavLeading { navigationGlyph }
                    }

                    fveSideNavItem {
                        _href (shellLayoutUrl Preferences)
                        "Settings"
                        fveSideNavLeading { navigationGlyph }
                    }
                }

                _value (shellLayoutUrl current)

                fveSideNavFooter {
                    fveSideNavRow {
                        _href (shellLayoutUrl Preferences)
                        "Andy Meier"
                    }
                }

                fveSideNavCompactFooter {
                    a {
                        _href (shellLayoutUrl Preferences)
                        _ariaLabel "Andy Meier profile"

                        _class
                            "flex size-8 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs font-semibold"

                        "AM"
                    }
                }
            }

        let content =
            Page.create
                (fvePageHeader { _label (shellLayoutLabel current) })
                (div {
                    _class
                        "grid min-h-80 place-items-center rounded-lg border border-dashed border-[var(--fve-border)] bg-[var(--fve-surface-subtle)] p-6 text-center text-sm text-[var(--fve-muted-text)]"

                    "Page content"
                })
            |> Page.withWidth width
            |> Page.withTopBar (
                fvePageTopBar {
                    div {
                        _class "flex min-w-0 flex-1 items-center"

                        fveBreadcrumbs {
                            _id (id + "-breadcrumbs")
                            _ariaLabel (layoutLabel + " breadcrumb")

                            if current <> Dashboard then
                                fveBreadcrumbItem {
                                    _href (shellLayoutUrl Dashboard)
                                    "Dashboard"
                                }

                            fveBreadcrumbItem {
                                _href (shellLayoutUrl current)
                                shellLayoutLabel current
                            }
                        }
                    }
                }
            )
            |> Page.render

        let shell =
            (AppShell.create (id) (navigation) (content)
             |> AppShell.withBoundary (AppShellBoundary.Container)
             |> AppShell.asPreview ((layoutLabel + " layout preview")))

        let shell =
            if collapsible then
                shell |> AppShell.withCollapsibleNavigation
            else
                shell

        let shell =
            if bottomNavigation then
                shell
                |> AppShell.withMobileBottomNavigation (
                    fveBottomNavigation {
                        _id (id + "-bottom")
                        _ariaLabel "Workspace quick navigation"

                        fveBottomNavigationItem {
                            _href (shellLayoutUrl Dashboard)
                            "Dashboard"
                        }

                        fveBottomNavigationItem {
                            _href (shellLayoutUrl Projects)
                            "Projects"
                        }

                        fveBottomNavigationItem {
                            _href (shellLayoutUrl Preferences)
                            "Settings"
                        }
                    }
                )
            else
                shell

        div {
            _class "h-[36rem]"
            shell |> AppShell.render
        }

    let appShellExamples current =
        let source =
            [ "navigationGlyph"
              "ShellLayoutDestination"
              "shellLayoutLabel"
              "shellLayoutUrl"
              "shellLayoutExample" ]

        let preview _ (content: HtmlElement) = themedPreview content

        let withUsage usage (example: ComponentExample) =
            { example with
                source = example.source + "\n\n" + usage }

        [ sample
              "app-shell"
              "Collapsible sidebar with constrained content"
              source
              (preview "app-shell-sidebar" (shellLayoutExample "layout-sidebar" PageWidth.Reading false true current))
          |> withUsage "shellLayoutExample \"layout-sidebar\" PageWidth.Reading false true Dashboard"
          sample
              "app-shell-bottom"
              "Full-width content with mobile bottom navigation"
              source
              (preview "app-shell-bottom" (shellLayoutExample "layout-bottom" PageWidth.Full true false current))
          |> withUsage "shellLayoutExample \"layout-bottom\" PageWidth.Full true false Dashboard" ]

    let accountManagementExamples current =
        [ sample
              "account-management"
              "Connected account workflow"
              (shellSource @ [ "ledgerShellExample"; "treasuryShellExample" ])
              (shellFixtureFor current)
          sample
              "responsive-actions"
              "Responsive page actions"
              (shellSource
               @ [ "refreshIcon"
                   "moreActionsIcon"
                   "refreshBalancesAction"
                   "pageAction"
                   "accountPageActions"
                   "responsiveActionsExample" ])
              (responsiveActionsExample |> fullBleedThemedSurface)
          |> note
              "This is consumer-authored layout, not component policy. The highest-priority action stays visible; below 400px of header width, the reports link moves into a separately identified menu. Both menus use the same real destinations and DropdownMenu keyboard behavior." ]

    let private calendarExampleSource example =
        [ "calendarDemoDate"
          "calendarEventDestination"
          "calendarDemoEvents"
          "calendarDateDestination"
          example ]

    let private calendarDefaultSource example =
        [ "calendarDemoDate"
          "calendarEventDestination"
          "defaultCalendarEvent"
          example ]

    let dayCalendarExamples =
        [ sample
              "day-calendar-default"
              "Default"
              (calendarDefaultSource "defaultDayCalendar")
              (calendarSurface defaultDayCalendar)
          sample
              "day-calendar-events"
              "Overlapping sessions"
              (calendarExampleSource "calendarDayExample")
              (calendarSurface calendarDayExample) ]

    let weekCalendarExamples =
        [ sample
              "week-calendar-default"
              "Default"
              (calendarDefaultSource "defaultWeekCalendar")
              (calendarSurface defaultWeekCalendar)
          sample
              "week-calendar-events"
              "Events and date navigation"
              (calendarExampleSource "calendarWeekExample")
              (calendarSurface calendarWeekExample) ]

    let yearCalendarExamples =
        [ sample
              "year-calendar-default"
              "Default"
              (calendarDefaultSource "defaultYearCalendar")
              (calendarSurface defaultYearCalendar)
          sample
              "year-calendar-events"
              "Event indicators and date navigation"
              (calendarExampleSource "calendarYearExample")
              (calendarSurface calendarYearExample) ]

    let private tableExamples current =
        [ sample
              "table"
              "Default"
              [ "TeamMember"
                "teamMembers"
                "teamTableHeader"
                "teamTableRows"
                "simpleTeamTable" ]
              (detailsSurface simpleTeamTable)
          |> note
              "Import Assets/table.js from your application entry. Table creates a custom-element host; rows and cells remain native HTML."
          sample
              "table-comfortable"
              "Comfortable rows"
              [ "TeamMember"
                "teamMembers"
                "teamTableHeader"
                "teamTableRows"
                "comfortableTeamTable" ]
              (detailsSurface comfortableTeamTable)
          sample
              "table-status"
              "With status values"
              [ "TeamMember"; "teamMembers"; "memberStatusTable" ]
              (detailsSurface memberStatusTable)
          sample
              "table-selection"
              "With checkboxes"
              [ "TeamMember"; "teamMembers"; "selectableTeamTable" ]
              (detailsSurface selectableTeamTable)
          sample
              "table-scrollable"
              "Scrollable rows and sticky headings"
              [ "TeamMember"
                "teamMembers"
                "teamTableHeader"
                "teamTableRows"
                "scrollableTeamTable" ]
              (detailsSurface scrollableTeamTable)
          sample
              "table-mobile"
              "Stacked on mobile"
              [ "TeamMember"
                "teamMembers"
                "teamTableHeader"
                "teamTableRows"
                "mobileTeamTable" ]
              (detailsSurface mobileTeamTable)
          sample
              "table-sorting"
              "Sortable records"
              [ "TeamMember"
                "teamMembers"
                "teamTableRows"
                "TeamMemberSort"
                "teamMemberSortUrl"
                "sortFor"
                "teamSortHeading"
                "sortableTeamTable" ]
              (detailsSurface (sortableTeamTablePreview current))
          sample
              "table-hierarchy"
              "Hierarchical items"
              [ "HierarchyItem"; "hierarchyItems"; "hierarchicalItemTable" ]
              (detailsSurface hierarchicalItemTable)
          sample "table-row-actions" "Row actions"
              [ "RowActionAccount"; "rowActionAccounts"; "moreActionsIcon"; "rowActionMenu"; "rowActionsTable" ]
              (connectedGallerySurface (div { _class "p-3 sm:p-6"; rowActionsTable }))
          |> note "Compose Button and Dropdown menu in an actions column; Table does not own record operations. The optional installation links below lead to their focused component recipes."
          sample "table-empty" "Empty state" [ "teamTableHeader"; "emptyTeamTable" ] (detailsSurface emptyTeamTable)
          serverSample
              "table-infinite"
              "Infinite scroll"
              [ "TeamMember"
                "teamTableHeader"
                "infiniteTeamMembers"
                "infiniteTeamRows"
                "infiniteTeamTable"
                "tableMoreHandler"
                "tableMoreRoutes" ]
              (detailsSurface infiniteTeamTable)
          |> note
              "Scroll the rows. The loading row replaces itself with the next batch; the final batch has no loading row." ]

    let examplesFor =
        function
        | "button" ->
            [ sample "button-basic" "Default" [ "basicButton" ] (centered basicButton)
              sample "button-sizes" "Sizes" [ "refreshIcon"; "buttonSizeExamples" ] (centered buttonSizeExamples)
              |> note "Extra small, Small, Medium, Large and Extra large are 24, 28, 32, 36 and 40px at the default root size. Use _iconOnly true for a square icon button, with an accessible name; its padding is independent of text buttons. Medium is the 32px application default."
              sample "button-colors" "Colors" [ "buttonColorExamples" ] (centered buttonColorExamples)
              sample "button-variants" "Variants" [ "buttonVariantExamples" ] (centered buttonVariantExamples)
              sample "button-pending" "Pending" [ "pendingSyncButton" ] (centered pendingSyncButton)
              |> note "Click Sync to see its loading state."
              sample
                  "button-content"
                  "Children"
                  [ "plusIcon"; "refreshIcon"; "buttonContentExamples" ]
                  (centered buttonContentExamples)
              |> note
                  "SVG icons follow the control size: 12px for Extra small, 16px for Small and Medium, and 20px for Large and Extra large at the default root size."
              sample
                  "button-icon-variants"
                  "Icon-only variants"
                  [ "refreshIcon"; "iconOnlyButtonVariantExamples" ]
                  (centered iconOnlyButtonVariantExamples)
              sample
                  "button-custom"
                  "Custom palette"
                  [ "customButtonPalette"; "customPaletteButtons" ]
                  (centered customPaletteButtons)
              sample "button" "Primary buttons" [ "primaryButtons" ] (centered primaryButtons)
              sample "button-secondary" "Secondary buttons" [ "secondaryButtons" ] (centered secondaryButtons)
              sample "button-ghost" "Ghost buttons" [ "ghostButtons" ] (centered ghostButtons)
              sample "button-destructive" "Destructive buttons" [ "destructiveButtons" ] (centered destructiveButtons)
              sample "button-error-pending" "Destructive pending" [ "pendingErrorButton" ] (centered pendingErrorButton)
              |> note "Click Delete to see its loading state."
              sample "button-disabled" "Disabled buttons" [ "disabledDeleteButton" ] (centered disabledDeleteButton)
              sample
                  "button-icon-primary"
                  "Primary icon-only"
                  [ "plusIcon"; "createIconOnlyButton" ]
                  (centered createIconOnlyButton)
              sample
                  "button-icon-pending"
                  "Pending icon-only"
                  [ "refreshIcon"; "refreshingIconOnlyButton" ]
                  (centered refreshingIconOnlyButton)
              |> note "Click the button to see its loading state."
              sample
                  "button-icon-disabled"
                  "Disabled icon-only"
                  [ "removeIcon"; "disabledRemoveIconOnlyButton" ]
                  (centered disabledRemoveIconOnlyButton)
              sample "button-row-actions" "Row actions"
                  [ "RowActionAccount"; "rowActionAccounts"; "moreActionsIcon"; "rowActionMenu"; "rowActionButtons" ]
                  (connectedGallerySurface (div { _class "p-3 sm:p-6"; rowActionButtons }))
              |> note "Use a 28px Small ghost button with _iconOnly true and a horizontal ellipsis. Name it for its record; preserve hover contrast on hovered and selected backgrounds. Open, Edit and Delete navigate to the working Application example." ]
        | "link" ->
            [ sample "link-basic" "Default" [ "basicLink" ] (connectedCentered basicLink)
              sample "link-sizes" "Sizes" [ "linkSizeExamples" ] (connectedCentered linkSizeExamples)
              sample "link-colors" "Colors" [ "linkColorExamples" ] (connectedCentered linkColorExamples)
              sample "link-variants" "Variants" [ "linkVariantExamples" ] (connectedCentered linkVariantExamples)
              sample "link-new-tab" "Native new-tab navigation" [ "newTabLink" ] (connectedCentered newTabLink) ]
        | "button-group" ->
            [ sample "button-group-default" "Default" [ "defaultButtonGroup" ] (centered defaultButtonGroup)
              sample
                  "button-group"
                  "Grouped actions"
                  [ "backIcon"; "moreActionsIcon"; "buttonGroupExample" ]
                  (centered buttonGroupExample)
              sample
                  "button-group-links"
                  "Native link segments"
                  [ "linkedButtonGroup" ]
                  (connectedCentered linkedButtonGroup)
              sample
                  "button-group-select"
                  "Label and Select segments"
                  [ "selectButtonGroup" ]
                  (centered selectButtonGroup)
              sample
                  "button-group-custom"
                  "Custom controls and popup isolation"
                  [ "customButtonGroup" ]
                  (centered customButtonGroup)
              |> note
                  "Button, Link and Select participate automatically, including when used as overlay triggers. Mark a custom native control with data-fve-group-control=\"outline\" (or \"plain\" for a control without a border). Wrappers do not need special selectors. The group joins logical segments; controls inside a native popover or dialog keep independent geometry. Keep labels and other field presentation caller-owned."
              sample
                  "button-group-vertical"
                  "Vertical orientation"
                  [ "verticalButtonGroup" ]
                  (centered verticalButtonGroup) ]
        | "badge" ->
            [ sample "badge" "Default" [ "badgeExample" ] (centered badgeExample)
              sample "badge-colors" "Colors" [ "badgeColorExamples" ] (centered badgeColorExamples)
              |> note
                  "Soft semantic badges blend their fill and text with the surrounding theme for quieter status cues. Neutral and Custom palettes retain their supplied colors; Solid, Outline and Ghost are unchanged."
              sample "badge-variants" "Variants" [ "badgeVariantExamples" ] (centered badgeVariantExamples) ]
        | "kbd" ->
            [ sample "kbd" "Default" [ "keyboardKey" ] (centered keyboardKey)
              sample "kbd-shortcut" "Shortcut group" [ "keyboardShortcut" ] (centered keyboardShortcut) ]
        | "separator" ->
            [ sample "separator-default" "Default" [ "defaultSeparator" ] (centered defaultSeparator)
              sample "separator" "Orientation and semantics" [ "separatorExamples" ] (centered separatorExamples) ]
        | "skeleton" ->
            [ sample "skeleton-default" "Basic" [ "defaultSkeleton" ] (fieldSurface defaultSkeleton)
              sample "skeleton-text" "Text" [ "textSkeleton" ] (fieldSurface textSkeleton)
              sample "skeleton-avatar" "Avatar row" [ "avatarSkeleton" ] (fieldSurface avatarSkeleton)
              sample "skeleton-card" "Card" [ "cardSkeleton" ] (fieldSurface cardSkeleton)
              sample
                  "skeleton-dimensions"
                  "Explicit dimensions"
                  [ "dimensionSkeleton" ]
                  (fieldSurface dimensionSkeleton) ]
        | "resizable" ->
            [ sample "resizable-default" "Default" [ "defaultResizablePanels" ] (detailsSurface defaultResizablePanels)
              sample "resizable" "Horizontal split panels" [ "resizablePanels" ] (detailsSurface resizablePanels)
              sample
                  "resizable-vertical"
                  "Vertical split panels"
                  [ "verticalResizablePanels" ]
                  (detailsSurface verticalResizablePanels) ]
        | "tooltip" ->
            [ sample "tooltip" "Default" [ "basicTooltip" ] (centered basicTooltip)
              sample "tooltip-described" "Existing description" [ "describedTooltip" ] (centered describedTooltip)
              sample "tooltip-bottom" "Bottom placement" [ "bottomTooltip" ] (centered bottomTooltip) ]
        | "popover" ->
            [ sample "popover" "Content panel" [ "profilePopover" ] (centered profilePopover)
              sample "popover-focus" "With form" [ "previewSizePopover" ] (detailsSurface previewSizePopover)
              |> note "Tab moves through ordinary fields and buttons; Escape returns to the trigger. The preview size is local UI state, not a saved profile."
              sample "popover-alignment" "Side and alignment" [ "alignedPopover" ] (centered alignedPopover)
              sample "popover-native-trigger" "Custom native trigger" [ "nativeTriggerPopover" ] (centered nativeTriggerPopover)
              |> note "Use this factory only when authoring a custom native control. Apply the supplied attributes; Popover retains its relationships, positioning and dismissal. The native trigger also works without JavaScript." ]
        | "message" ->
            [ sample "message-default" "Default" [ "defaultMessage" ] (detailsSurface defaultMessage)
              sample "message" "Sender and receiver" [ "messageRows" ] (detailsSurface messageRows)
              sample
                  "message-no-avatar"
                  "Metadata and actions without an avatar"
                  [ "messageWithoutAvatar" ]
                  (detailsSurface messageWithoutAvatar) ]
        | "loading-indicator" ->
            [ sample "loading-indicator" "Default" [ "defaultLoadingIndicator" ] (centered defaultLoadingIndicator)
              sample
                  "loading-indicator-label"
                  "With visible label"
                  [ "visibleLoadingIndicator" ]
                  (centered visibleLoadingIndicator) ]
        | "progress" ->
            [ serverSample
                  "progress-active"
                  "Default"
                  [ "progressExampleRegion"
                    "operationalProgressExample"
                    "progressExampleHandler"
                    "progressExampleRoutes" ]
                  (fieldSurface operationalProgressExample)
              |> note
                  "Run a short example task. One SSE response updates the native progress bar through completion. The copied view, handler and routes use services.AddDatastar(); the delay simulates work."
              sample
                  "progress-indeterminate"
                  "Indeterminate"
                  [ "indeterminateProgressExample" ]
                  (fieldSurface indeterminateProgressExample)
              sample
                  "progress-complete"
                  "Complete"
                  [ "completedProgressExample" ]
                  (fieldSurface completedProgressExample)
              sample "progress-failed" "Failed" [ "failedProgressExample" ] (fieldSurface failedProgressExample) ]
        | "empty-state" ->
            [ sample "empty-state-default" "Default" [ "defaultEmptyState" ] (fieldSurface defaultEmptyState)
              sample
                  "empty-state"
                  "With recovery action"
                  [ "plusIcon"; "emptyStateExample" ]
                  (fieldSurface emptyStateExample) ]
        | "table" -> tableExamples NameAscending
        | "description-list" ->
            [ sample
                  "description-list-default"
                  "Default"
                  [ "defaultDescriptionList" ]
                  (detailsSurface defaultDescriptionList)
              sample "description-list" "Three-column details" [ "itemDetails" ] (detailsSurface itemDetails)
              sample
                  "description-list-four-columns"
                  "Four-column details"
                  [ "itemOverview" ]
                  (detailsSurface itemOverview)
              sample
                  "description-list-supporting-text"
                  "With supporting text"
                  [ "itemDetailsWithDescriptions" ]
                  (detailsSurface itemDetailsWithDescriptions) ]
        | "metric" ->
            [ sample "metric-default" "Default" [ "defaultMetric" ] (centered defaultMetric)
              sample "metric" "With trend and status" [ "totalItemsMetric" ] (centered totalItemsMetric)
              sample "metric-pending" "With review status" [ "pendingItemsMetric" ] (centered pendingItemsMetric)
              sample "metric-multiple" "Multiple metrics" [ "multipleMetrics" ] (centered multipleMetrics) ]
        | "pagination" -> paginationGalleryExamples 2
        | "avatar" ->
            [ sample "avatar-default" "Default" [ "defaultAvatar" ] (centered defaultAvatar)
              sample
                  "avatar-fallback"
                  "Fallbacks and sizes"
                  [ "avatarFallbackExample" ]
                  (centered avatarFallbackExample)
              sample "avatar-image" "Image" [ "avatarImageExample" ] (centered avatarImageExample)
              sample "avatar-decorative" "Decorative" [ "decorativeAvatarExample" ] (centered decorativeAvatarExample) ]
        | "copy-reveal" ->
            [ sample "copy-reveal" "Default" [ "copyRevealExample" ] (fieldSurface copyRevealExample)
              sample "copy-reveal-icons" "Icon actions" [ "iconCopyRevealExample" ] (fieldSurface iconCopyRevealExample)
              sample
                  "copy-reveal-revealed"
                  "Initially revealed"
                  [ "revealedCopyRevealExample" ]
                  (fieldSurface revealedCopyRevealExample) ]
        | "input" ->
            [ sample "input" "Default" [ "labelledInput" ] (fieldSurface labelledInput)
              sample
                  "input-sizes"
                  "Consistent control sizes"
                  [ "refreshIcon"; "inputSizeExamples" ]
                  (detailsSurface inputSizeExamples)
              sample "input-help" "With help text" [ "inputWithHelp" ] (fieldSurface inputWithHelp)
              sample "input-required" "Required" [ "nativeValidationAttributes"; "requiredInput" ] (fieldSurface requiredInput)
              sample "input-optional" "Optional" [ "optionalInput" ] (fieldSurface optionalInput)
              sample "input-validation" "Validation error" [ "nativeValidationAttributes"; "invalidInput" ] (fieldSurface invalidInput)
              sample
                  "search-input"
                  "Native search"
                  [ "searchInputExample" ]
                  (fieldSurface searchInputExample)
              sample "input-disabled" "Disabled" [ "disabledInput" ] (fieldSurface disabledInput)
              sample "input-readonly" "Read-only" [ "readOnlyInput" ] (fieldSurface readOnlyInput) ]
        | "field" ->
            [ sample "field-default" "Default" [ "defaultField" ] (fieldSurface defaultField)
              sample "field" "Custom control" [ "customQuantityField" ] (fieldSurface customQuantityField)
              sample
                  "field-responsive"
                  "Responsive label and control rows"
                  [ "responsiveFieldRows" ]
                  (fieldSurface responsiveFieldRows)
              sample "field-group" "Semantic fieldset" [ "groupedFields" ] (fieldSurface groupedFields) ]
        | "input-group" ->
            [ sample "input-group-default" "Prefix" [ "inputWithPrefix" ] (fieldSurface inputWithPrefix)
              sample "input-group-icon" "Icon" [ "emailIcon"; "inputWithIcon" ] (fieldSurface inputWithIcon)
              sample "input-group-suffix" "Suffix" [ "inputWithSuffix" ] (fieldSurface inputWithSuffix)
              sample "input-group" "Text addons" [ "weightInputGroup" ] (fieldSurface weightInputGroup)
              sample
                  "input-group-search"
                  "Icon and keyboard hint"
                  [ "searchIcon"; "searchInputGroup" ]
                  (fieldSurface searchInputGroup)
              sample
                  "input-group-help"
                  "Addon popup keeps its own controls"
                  [ "inputGroupWithHelp" ]
                  (fieldSurface inputGroupWithHelp)
              sample "input-group-textarea" "Textarea header and footer" [ "textareaInputGroup" ] (fieldSurface textareaInputGroup)
              sample "input-group-validation" "Validation error" [ "nativeValidationAttributes"; "invalidInputGroup" ] (fieldSurface invalidInputGroup)
              serverSample "input-group-check" "Checking with visible feedback"
                  [ "emailCheckResult"; "checkEmailInputGroup"; "checkEmailHandler"; "checkEmailRoutes" ]
                  (fieldSurface checkEmailInputGroup)
              |> note "The server checks email format after a one-second demonstration delay. The request indicator owns temporary busy/readonly feedback, then restores editing. Register checkEmailRoutes and services.AddDatastar() in your application." ]
        | "form-layouts" ->
            [ sample
                  "form-layouts"
                  "Stacked with server validation"
                  [ "choiceSubmitButton"
                    "ContactDetails"
                    "ContactFormLayout"
                    "contactFormRegion"
                    "emptyContact"
                    "contactFormExample" ]
                  (fullBleedThemedSurface contactFormExample)
              sample
                  "form-layouts-grid"
                  "Two-column form"
                  [ "choiceSubmitButton"
                    "ContactDetails"
                    "ContactFormLayout"
                    "contactFormRegion"
                    "emptyContact"
                    "twoColumnFormExample" ]
                  (fullBleedThemedSurface twoColumnFormExample)
              sample
                  "form-layouts-sections"
                  "Sectioned form"
                  [ "choiceSubmitButton"
                    "ContactDetails"
                    "ContactFormLayout"
                    "contactFormRegion"
                    "emptyContact"
                    "sectionedFormExample" ]
                  (fullBleedThemedSurface sectionedFormExample) ]
        | "file-selection" ->
            [ sample "file-selection-default" "Default" [ "defaultFileSelection" ] (fieldSurface defaultFileSelection)
              sample "file-selection" "Multiple files" [ "fileSelectionExample" ] (fieldSurface fileSelectionExample)
              sample
                  "file-selection-validation"
                  "Validation"
                  [ "pdfSelectionValidation"; "invalidFileSelectionExample" ]
                  (fieldSurface invalidFileSelectionExample)
              sample
                  "file-selection-disabled"
                  "Disabled"
                  [ "disabledFileSelectionExample" ]
                  (fieldSurface disabledFileSelectionExample) ]
        | "tag-input" ->
            [ sample "tag-input" "Default" [ "tagInputExample" ] (fieldSurface tagInputExample)
              |> note
                  "Import Assets/tag-input.js from your application entry. The light-DOM host owns editing and form reset; initial tags are server-rendered native form values."
              sample
                  "tag-input-submission"
                  "Submission"
                  [ "tagInputFormRegion" ]
                  (fieldSurface (tagInputFormRegion [ "reviewed" ] None None))
              sample
                  "tag-input-validation"
                  "Validation"
                  [ "invalidTagInputExample" ]
                  (fieldSurface invalidTagInputExample)
              sample "tag-input-pending" "Pending" [ "pendingTagInputExample" ] (fieldSurface pendingTagInputExample)
              sample
                  "tag-input-disabled"
                  "Disabled"
                  [ "disabledTagInputExample" ]
                  (fieldSurface disabledTagInputExample) ]
        | "textarea" ->
            [ sample "textarea" "Default" [ "labelledTextarea" ] (fieldSurface labelledTextarea)
              sample "textarea-composer" "Visually hidden label" [ "composerTextarea" ] (fieldSurface composerTextarea)
              sample "textarea-help" "With instructions" [ "editableInstructions" ] (fieldSurface editableInstructions)
              sample "textarea-validation" "Validation error" [ "nativeValidationAttributes"; "invalidTextarea" ] (fieldSurface invalidTextarea)
              sample "textarea-readonly" "Read-only" [ "readOnlyNotes" ] (fieldSurface readOnlyNotes)
              sample "textarea-disabled" "Disabled" [ "unavailableNotes" ] (fieldSurface unavailableNotes) ]
        | "error-summary" ->
            [ sample
                  "error-summary"
                  "Linked field errors"
                  [ "nativeValidationAttributes"; "errorSummaryExample" ]
                  (fullBleedThemedSurface errorSummaryExample) ]
        | "notice" ->
            [ sample "notice" "Default" [ "informationNotice" ] (fieldSurface informationNotice)
              sample "notice-colors" "Colors" [ "noticeColorExamples" ] (detailsSurface noticeColorExamples)
              sample "notice-variants" "Variants" [ "noticeVariantExamples" ] (detailsSurface noticeVariantExamples)
              sample "notice-success" "Success with download" [ "successNotice" ] (fieldSurface successNotice)
              sample "notice-warning" "Warning" [ "warningNotice" ] (fieldSurface warningNotice)
              sample "notice-critical" "Error with recovery link" [ "criticalNotice" ] (fieldSurface criticalNotice)
              sample
                  "notice-announcement"
                  "Announce an update"
                  [ "noticeAnnouncementExample" ]
                  (fieldSurface noticeAnnouncementExample)
              |> note
                  "Ordinary persistent notices use LiveAnnouncement.Static. Use Polite for status updates, as shown here; reserve Assertive for urgent changes that need immediate attention." ]
        | "notification" ->
            [ sample
                  "notification"
                  "Default"
                  [ "notificationLeadItem"
                    "notificationLeadRegion"
                    "simpleNotificationExample" ]
                  (overlaySurface simpleNotificationExample)
              sample
                  "notification-actions"
                  "Persistent with an action"
                  [ "notificationWithActionsExample" ]
                  (overlaySurface notificationWithActionsExample)
              sample
                  "notification-timing"
                  "Custom lifetime"
                  [ "timedNotificationItem"
                    "timedNotificationRegion"
                    "timedNotificationExample" ]
                  (overlaySurface timedNotificationExample) ]
        | "select" ->
            [ sample "select-default" "Basic" [ "frequencyOptions"; "defaultSelect" ] (fieldSurface defaultSelect)
              sample
                  "select-composed"
                  "Custom items and groups"
                  [ "composedStatusSelect" ]
                  (fieldSurface composedStatusSelect)
              sample
                  "select-multiple"
                  "Multiple selection"
                  [ "memberOptions"; "selectedMembersSelect" ]
                  (fieldSurface selectedMembersSelect)
              sample
                  "select-search"
                  "Local search"
                  (comboboxSource @ [ "staticItemCombobox" ])
                  (fieldSurface staticItemCombobox)
              serverSample
                  "select-search-remote"
                  "Remote search"
                  (comboboxSource
                   @ [ "itemContent"
                       "itemCombobox"
                       "itemComboboxOptions"
                       "selectSearchQuery"
                       "itemSearchHandler"
                       "itemSearchRoutes" ])
                  (fieldSurface itemCombobox)
              sample
                  "select-validation"
                  "Form validation"
                  (formSource @ selectSource @ [ "selectFormRegion"; "statusSelect" ])
                  (fieldSurface statusSelect)
              sample
                  "select-disabled"
                  "Disabled"
                  (selectSource @ [ "disabledStatusSelect" ])
                  (fieldSurface disabledStatusSelect)
              sample
                  "select-pending"
                  "Pending"
                  (selectSource @ [ "pendingStatusSelect" ])
                  (fieldSurface pendingStatusSelect) ]
        | "select-fixtures" ->
            [ sample "select-default" "Default" [ "frequencyOptions"; "defaultSelect" ] (fieldSurface defaultSelect)
              sample
                  "select-native-fallback"
                  "Native fallback"
                  [ "nativeFallbackSelect" ]
                  (fieldSurface nativeFallbackSelect)
              sample "select" "Selected-item alignment (default)" [ "basicSelect" ] (fieldSurface basicSelect)
              sample "select-help" "With help text" [ "selectWithHelp" ] (fieldSurface selectWithHelp)
              sample
                  "select-edge"
                  "Explicit trigger-edge position"
                  [ "edgeAlignedSelect" ]
                  (fieldSurface edgeAlignedSelect)
              sample
                  "select-validation"
                  "Required selection with validation"
                  (formSource @ selectSource @ [ "selectFormRegion"; "statusSelect" ])
                  (fieldSurface statusSelect)
              sample
                  "select-disabled"
                  "Disabled"
                  (selectSource @ [ "disabledStatusSelect" ])
                  (fieldSurface disabledStatusSelect)
              sample
                  "select-pending"
                  "Pending"
                  (selectSource @ [ "pendingStatusSelect" ])
                  (fieldSurface pendingStatusSelect)
              sample
                  "select-multiple"
                  "Multiple selection"
                  [ "memberOptions"; "multipleMembersSelect" ]
                  (fieldSurface multipleMembersSelect)
              sample
                  "select-multiple-selected"
                  "With several selected"
                  [ "memberOptions"; "selectedMembersSelect" ]
                  (fieldSurface selectedMembersSelect)
              sample
                  "select-multiple-validation"
                  "Multiple selection with validation"
                  (formSource @ [ "memberOptions"; "multipleSelectForm" ])
                  (fieldSurface (multipleSelectForm [] None None))
              sample
                  "select-multiple-disabled"
                  "Multiple selection, disabled"
                  [ "memberOptions"; "disabledMembersSelect" ]
                  (fieldSurface disabledMembersSelect)
              sample
                  "select-multiple-pending"
                  "Multiple selection, pending"
                  [ "memberOptions"; "pendingMembersSelect" ]
                  (fieldSurface pendingMembersSelect)
              sample
                  "select-search"
                  "Searchable"
                  (comboboxSource @ [ "staticItemCombobox" ])
                  (fieldSurface staticItemCombobox)
              sample
                  "select-composed"
                  "Composed groups and icon items"
                  [ "composedStatusSelect" ]
                  (fieldSurface composedStatusSelect)
              serverSample
                  "select-search-remote"
                  "Searchable with remote results"
                  (comboboxSource
                   @ [ "itemContent"
                       "itemComboboxView"
                       "itemCombobox"
                       "itemComboboxOptions"
                       "selectSearchQuery"
                       "itemSearchTargets"
                       "itemSearchHandler"
                       "itemSearchRoutes" ])
                  (fieldSurface itemCombobox)
              serverSample
                  "select-search-isolation"
                  "Independent searches sharing one endpoint"
                  (comboboxSource
                   @ [ "itemContent"
                       "itemComboboxView"
                       "isolatedItemComboboxes"
                       "itemComboboxOptions"
                       "selectSearchQuery"
                       "itemSearchTargets"
                       "itemSearchHandler"
                       "itemSearchRoutes"
                       "itemFixtureSearchRoutes" ])
                  (fieldSurface isolatedItemComboboxes)
              sample
                  "select-search-validation"
                  "Searchable with validation"
                  (comboboxSource @ [ "validationItemCombobox" ])
                  (fieldSurface validationItemCombobox)
              sample
                  "select-search-loading"
                  "Searchable, loading"
                  [ "loadingItemCombobox" ]
                  (fieldSurface loadingItemCombobox)
              sample
                  "select-search-disabled"
                  "Searchable, disabled"
                  (comboboxSource @ [ "disabledItemCombobox" ])
                  (fieldSurface disabledItemCombobox)
              sample
                  "select-search-pending"
                  "Searchable, pending"
                  (comboboxSource @ [ "pendingItemCombobox" ])
                  (fieldSurface pendingItemCombobox)
              sample
                  "select-search-multiple"
                  "Searchable multiple selection"
                  [ "memberOptions"; "multipleMembersCombobox" ]
                  (fieldSurface multipleMembersCombobox)
              serverSample
                  "select-search-multiple-remote"
                  "Searchable multiple selection with remote results"
                  [ "memberOptions"
                    "remoteMembersContent"
                    "remoteMembersView"
                    "remoteMembersCombobox"
                    "searchMemberOptions"
                    "remoteMembersField"
                    "remoteMembersOptions"
                    "selectSearchQuery"
                    "remoteMemberSearch"
                    "remoteMembersRoutes" ]
                  (fieldSurface remoteMembersCombobox)
              sample
                  "select-search-multiple-validation"
                  "Searchable multiple selection with validation"
                  (formSource @ [ "memberOptions"; "multipleComboboxForm" ])
                  (fieldSurface (multipleComboboxForm [] None None))
              sample
                  "select-search-multiple-loading"
                  "Searchable multiple selection, loading"
                  [ "memberOptions"; "loadingMembersCombobox" ]
                  (fieldSurface loadingMembersCombobox)
              sample
                  "select-search-multiple-disabled"
                  "Searchable multiple selection, disabled"
                  [ "memberOptions"; "disabledMembersCombobox" ]
                  (fieldSurface disabledMembersCombobox)
              sample
                  "select-search-multiple-pending"
                  "Searchable multiple selection, pending"
                  [ "memberOptions"; "pendingMembersCombobox" ]
                  (fieldSurface pendingMembersCombobox) ]
        | "month-calendar" ->
            [ sample
                  "month-calendar-default"
                  "Default"
                  (calendarDefaultSource "defaultMonthCalendar")
                  (calendarSurface defaultMonthCalendar)
              sample
                  "month-calendar-events"
                  "Events and date navigation"
                  [ "calendarDemoDate"
                    "calendarEventDestination"
                    "calendarDemoEvents"
                    "calendarDateDestination"
                    "calendarMonthEvents"
                    "calendarMonthExample" ]
                  (calendarSurface calendarMonthExample)
              sample
                  "month-calendar-compact"
                  "Compact month"
                  (calendarExampleSource "compactMonthCalendar")
                  (calendarSurface compactMonthCalendar)
              sample
                  "month-calendar-selection"
                  "Single-date selection"
                  [ "monthSelection" ]
                  (calendarSurface monthSelection)
              sample
                  "month-calendar-single"
                  "Clearable single date"
                  [ "monthSelectionSingle" ]
                  (calendarSurface monthSelectionSingle)
              sample "month-calendar-current-date" "Current date" [ "currentDateMonthSelection" ] (calendarSurface currentDateMonthSelection)
              |> note "Today has a subtle highlight by default, separate from selection. Use _currentDate for the application's date/timezone without adding a shortcut. This example fixes today to November 5, 2026; November 12 is selected."

              sample "month-calendar-today" "Today shortcut" [ "todayMonthSelection" ] (calendarSurface todayMonthSelection)
              |> note "_today supplies the application's current DateOnly and opts into the shortcut, independently of _clearable. This example uses the host's current date. Clear stays at the start; Today stays at the end."
              sample "month-calendar-range" "Date range" [ "monthSelectionRange" ] (calendarSurface monthSelectionRange)
              sample
                  "month-calendar-bounds"
                  "Bounds, unavailable dates and Monday start"
                  [ "boundedMonthSelection" ]
                  (calendarSurface boundedMonthSelection)
              sample
                  "month-calendar-caption"
                  "Month and year selectors"
                  [ "captionMonthSelection" ]
                  (calendarSurface captionMonthSelection)
              sample
                  "month-calendar-pending"
                  "Busy availability"
                  [ "pendingMonthSelection" ]
                  (calendarSurface pendingMonthSelection)
              sample
                  "month-calendar-readonly"
                  "Read-only selection"
                  [ "readOnlyMonthSelection" ]
                  (calendarSurface readOnlyMonthSelection) ]
        | "chart" ->
            [ sample "chart-default" "Line" [ "lineChartOptions"; "defaultChart" ] defaultChart
              sample "chart-column" "Column" [ "columnChartOptions"; "columnChart" ] columnChart
              sample "chart-bar" "Horizontal bar" [ "barChartOptions"; "barChart" ] barChart
              sample "chart-sankey" "Sankey" [ "sankeyChartOptions"; "sankeyChart" ] sankeyChart
              sample
                  "chart-interaction"
                  "Updating options and handling events"
                  [ "lineChartOptions"
                    "columnChartOptions"
                    "chartTypeControl"
                    "interactiveChart" ]
                  interactiveChart
              sample "chart-empty" "Empty data" [ "emptyChart" ] emptyChart ]
        | "date-picker" ->
            [ sample "date-picker-default" "Default" [ "defaultDatePicker" ] (fieldSurface defaultDatePicker)
              sample "date-picker" "Clearable single date" [ "basicDatePicker" ] (fieldSurface basicDatePicker)
              sample "date-picker-current-date" "Current date" [ "currentDateDatePicker" ] (fieldSurface currentDateDatePicker)
              |> note "Today is subtly highlighted without being selected or adding a footer shortcut. _currentDate overrides the local rendering date for application timezones. This example fixes today to November 5, 2026; November 12 is selected."

              sample "date-picker-today" "Today shortcut" [ "todayDatePicker" ] (fieldSurface todayDatePicker)
              |> note "Pass _today using the application's date and timezone. Today selects the date and closes the picker; Reset restores the original value. Clear is configured separately."
              sample "date-picker-range" "Date range" [ "rangeDatePicker" ] (fieldSurface rangeDatePicker)
              |> note "In range mode, Today selects a complete one-day range (today–today). Bounds, unavailable dates, readonly and disabled still apply."
              sample
                  "date-picker-caption"
                  "Month and year selectors"
                  [ "captionDatePicker" ]
                  (fieldSurface captionDatePicker)
              sample
                  "date-picker-bounds"
                  "Bounds and unavailable dates"
                  [ "boundedDatePicker" ]
                  (fieldSurface boundedDatePicker)
              sample "date-picker-validation" "Validation" [ "invalidDatePicker" ] (fieldSurface invalidDatePicker)
              sample "date-picker-pending" "Pending" [ "pendingDatePicker" ] (fieldSurface pendingDatePicker)
              sample "date-picker-disabled" "Disabled" [ "disabledDatePicker" ] (fieldSurface disabledDatePicker)
              sample "date-picker-readonly" "Read-only" [ "readOnlyDatePicker" ] (fieldSurface readOnlyDatePicker) ]
        | "checkbox" ->
            [ sample "checkbox" "Default" [ "basicCheckbox" ] (fieldSurface basicCheckbox)
              sample "checkbox-help" "With help text" [ "checkboxWithHelp" ] (fieldSurface checkboxWithHelp)
              sample
                  "checkbox-indeterminate"
                  "Indeterminate"
                  [ "indeterminateCheckbox" ]
                  (fieldSurface indeterminateCheckbox)
              sample
                  "checkbox-validation"
                  "Required confirmation with validation"
                  (formSource @ [ "checkboxFormRegion" ])
                  (fieldSurface (checkboxFormRegion false None None))
              sample "checkbox-checked" "Checked" [ "includeArchived" ] (fieldSurface includeArchived)
              sample "checkbox-pending" "Pending" [ "pendingArchivedReview" ] (fieldSurface pendingArchivedReview)
              sample "checkbox-disabled" "Disabled" [ "disabledArchivedReview" ] (fieldSurface disabledArchivedReview) ]
        | "switch" ->
            [ sample "switch" "Default" [ "basicSwitch" ] (fieldSurface basicSwitch)
              sample "switch-help" "With help text" [ "switchWithHelp" ] (fieldSurface switchWithHelp)
              sample
                  "switch-submission"
                  "Setting with submission"
                  (formSource @ [ "switchFormRegion"; "notificationSetting" ])
                  (fieldSurface notificationSetting)
              sample "switch-pending" "Pending" [ "pendingNotifications" ] (fieldSurface pendingNotifications)
              sample "switch-disabled" "Disabled" [ "disabledNotifications" ] (fieldSurface disabledNotifications)
              sample
                  "switch-validation"
                  "Validation error"
                  [ "invalidNotifications" ]
                  (fieldSurface invalidNotifications)
              sample "switch-sizes" "Sizes" [ "switchSizes" ] (fieldSurface switchSizes)
              sample "switch-stacked" "Stacked layout" [ "stackedSwitch" ] (fieldSurface stackedSwitch) ]
        | "toggle-button" ->
            [ sample "toggle-button" "Default" [ "compactRows" ] (centered compactRows)
              sample "toggle-button-pending" "Pending" [ "pendingCompactRows" ] (centered pendingCompactRows)
              sample "toggle-button-disabled" "Disabled" [ "disabledCompactRows" ] (centered disabledCompactRows)
              sample
                  "toggle-button-presentation"
                  "Content, sizes and variants"
                  [ "plusIcon"; "toggleButtonPresentation" ]
                  (centered toggleButtonPresentation) ]
        | "toggle-group" ->
            [ sample "toggle-group-default" "Default" [ "defaultToggleGroup" ] (centered defaultToggleGroup)
              sample "toggle-group-single" "Single selection" [ "singleToggleGroup" ] (centered singleToggleGroup)
              sample
                  "toggle-group-connected"
                  "Connected multiple and single-item groups"
                  [ "connectedToggleGroups" ]
                  (centered connectedToggleGroups)
              sample
                  "toggle-group-multiple"
                  "Multiple, spaced and unavailable"
                  [ "plusIcon"; "refreshIcon"; "multipleToggleGroup" ]
                  (centered multipleToggleGroup)
              sample
                  "toggle-group-submission"
                  "Submission"
                  (formSource @ [ "toggleGroupFormRegion" ])
                  (fieldSurface (toggleGroupFormRegion [ "bold" ] None None))
              sample
                  "toggle-group-vertical"
                  "Vertical orientation"
                  [ "verticalToggleGroup" ]
                  (centered verticalToggleGroup) ]
        | "tabs" ->
            [ sample "tabs" "Default" [ "codePreviewTabs" ] (fieldSurface codePreviewTabs)
              sample
                  "tabs-underlined"
                  "Underlined tabs with refresh"
                  [ "reviewTabsRegion" ]
                  (fieldSurface (reviewTabsRegion false))
              sample
                  "tabs-vertical"
                  "Vertical, icons and disabled items"
                  [ "plusIcon"; "refreshIcon"; "removeIcon"; "verticalTabs" ]
                  (fieldSurface verticalTabs) ]
        | "radio-group" ->
            [ sample "radio-group" "Default" [ "basicRadioGroup" ] (fieldSurface basicRadioGroup)
              sample "radio-group-help" "Option descriptions" [ "radioGroupWithHelp" ] (fieldSurface radioGroupWithHelp)
              sample
                  "radio-group-horizontal"
                  "Horizontal orientation"
                  [ "horizontalRadioGroup" ]
                  (fieldSurface horizontalRadioGroup)
              sample
                  "radio-group-validation"
                  "Required choice with validation"
                  (formSource @ [ "updateModeOptions"; "radioGroupFormRegion"; "updateMode" ])
                  (fieldSurface updateMode)
              sample
                  "radio-group-pending"
                  "Pending"
                  [ "updateModeOptions"; "pendingUpdateMode" ]
                  (fieldSurface pendingUpdateMode)
              sample
                  "radio-group-disabled"
                  "Disabled"
                  [ "updateModeOptions"; "disabledUpdateMode" ]
                  (fieldSurface disabledUpdateMode) ]
        | "choice-cards" ->
            [ sample "choice-cards-default" "Default" [ "defaultChoiceCards" ] (detailsSurface defaultChoiceCards)
              sample "choice-cards" "Single choice" [ "richChoiceCards" ] (detailsSurface richChoiceCards)
              sample
                  "choice-cards-multiple"
                  "Multiple choices"
                  [ "multipleChoiceCards" ]
                  (detailsSurface multipleChoiceCards)
              sample "choice-cards-validation" "Validation" [ "invalidChoiceCards" ] (detailsSurface invalidChoiceCards) ]
        | "dropdown-menu" ->
            [ sample "dropdown-menu" "Basic menu" [ "basicDropdownMenu" ] (centered basicDropdownMenu)
              sample "dropdown-menu-icons" "Icons" [ "plusIcon"; "menuDocumentIcon"; "iconDropdownMenu" ] (centered iconDropdownMenu)
              sample "dropdown-menu-shortcuts" "Keyboard hints" [ "shortcutDropdownMenu" ] (centered shortcutDropdownMenu)
              |> note "B and P are the menu's built-in typeahead hints: type a letter to focus its item, then Enter to follow the link. They are not global application shortcuts."
              sample "dropdown-menu-checkboxes" "Checkboxes" [ "checkboxDropdownMenu" ] (centered checkboxDropdownMenu)
              sample "dropdown-menu-radio" "Radio choices" [ "radioDropdownMenu" ] (centered radioDropdownMenu)
              sample "dropdown-menu-disabled" "Disabled items" [ "disabledDropdownMenu" ] (centered disabledDropdownMenu)
              sample "dropdown-menu-destructive" "Destructive action" [ "destructiveDropdownMenu" ] (fieldSurface destructiveDropdownMenu)
              sample
                  "dropdown-menu-outline"
                  "Outlined page action"
                  [ "menuOverflowIcon"; "outlinedDropdownMenu" ]
                  (centered outlinedDropdownMenu)
              sample
                  "dropdown-menu-custom"
                  "Custom native control"
                  [ "customDropdownMenu" ]
                  (centered customDropdownMenu)
              sample
                  "dropdown-menu-full-row"
                  "Full context row and supporting descriptions"
                  [ "menuTrailingChevron"; "fullRowDropdownMenu" ]
                  (centered fullRowDropdownMenu)
              sample
                  "dropdown-menu-advanced"
                  "Grouped states and refresh"
                  [ "menuLeadingIcon"
                    "dropdownMenuItems"
                    "actionMenu"
                    "menuOverflowIcon"
                    "moreActionsMenu"
                    "dropdownMenuRegion" ]
                  (fieldSurface (dropdownMenuRegion false))
              sample "dropdown-menu-row-actions" "Row actions"
                  [ "RowActionAccount"; "rowActionAccounts"; "moreActionsIcon"; "rowActionMenu"; "rowActionMenuExample" ]
                  (connectedCentered rowActionMenuExample)
              |> note "The 28px icon-only trigger names its record and stays highlighted while open. Arrow keys navigate; Escape restores trigger focus. Delete opens the Application example's confirmation, rather than deleting immediately." ]
        | "command" ->
            [ sample "command-default" "Default" [ "defaultCommand" ] (fieldSurface defaultCommand)
              sample
                  "command-groups"
                  "Groups, icons and shortcuts"
                  [ "commandDocumentIcon"; "commandGroups"; "groupedCommand" ]
                  (fieldSurface groupedCommand)
              sample
                  "command-actions"
                  "Actions and disabled items"
                  [ "commandActionsExample" ]
                  (fieldSurface commandActionsExample)
              sample
                  "command-palette"
                  "Dialog palette"
                  [ "commandDocumentIcon"; "commandGroups"; "commandPaletteExample" ]
                  (centered commandPaletteExample)
              sample "command-empty" "Empty results" [ "emptyCommand" ] (fieldSurface emptyCommand) ]
        | "dialog" ->
            [ sample
                  "dialog-default"
                  "Default"
                  [ "defaultDialog"; "defaultDialogExample" ]
                  (centered defaultDialogExample)
              sample
                  "dialog"
                  "Modal dialog"
                  [ "dialogConfig"; "reviewDialogTrigger"; "reviewDialog" ]
                  (centered (
                      div {
                          reviewDialogTrigger
                          reviewDialog
                      }
                  ))
              sample
                  "dialog-confirmation"
                  "Server-validated confirmation"
                  [ "itemDeletionForm"
                    "itemDeletionContent"
                    "itemDeletionDialog"
                    "confirmationExample" ]
                  (centered confirmationExample)
              sample
                  "dialog-contained"
                  "App-contained dialog"
                  [ "containedDialogConfig"; "containedDialogExample" ]
                  (themedSurface containedDialogExample)
              |> note
                  "_container keeps the dialog in a positioned app viewport rather than the document top layer. The component owns the backdrop, app-background inertness, Escape and focus return; review chrome remains usable. _open true supports server-rendered open states; mark the background inert in that native state too." ]
        | "drawer" ->
            [ sample
                  "drawer-default"
                  "Default"
                  [ "defaultDrawer"; "defaultDrawerExample" ]
                  (centered defaultDrawerExample)
              sample
                  "drawer"
                  "Standard detail drawer"
                  [ "itemDrawerContent"; "itemDrawerConfig"; "itemDrawerExample" ]
                  (centered itemDrawerExample)
              sample
                  "drawer-wide"
                  "Wide editing form"
                  [ "itemEditorBody"
                    "itemEditorFooter"
                    "itemEditorDrawerConfig"
                    "itemEditorDrawerExample" ]
                  (centered itemEditorDrawerExample)
              sample
                  "drawer-start"
                  "Start-side filters"
                  [ "filterDrawerConfig"; "filterDrawerExample" ]
                  (centered filterDrawerExample)
              sample
                  "drawer-horizontal"
                  "Top and bottom edges"
                  [ "topDrawerConfig"; "bottomDrawerConfig"; "horizontalDrawerExamples" ]
                  (centered horizontalDrawerExamples)
              sample
                  "drawer-contained"
                  "App-contained drawer"
                  [ "containedDrawerConfig"; "containedDrawerExample" ]
                  (themedSurface containedDrawerExample)
              |> note
                  "Render beside the named app background in a relative, bounded viewport. _container uses show(), not showModal(): only that app background becomes inert. _open true supports route-owned native open states; mark the background inert in that native state too. The default standalone Drawer remains document-modal." ]
        | "floating-panel" ->
            [ sample
                  "floating-panel"
                  "Open non-modal panel"
                  [ "floatingPanelContent"; "floatingPanelExample" ]
                  (overlaySurface floatingPanelExample)
              sample
                  "floating-panel-minimized"
                  "Minimized with restore"
                  [ "floatingPanelContent"; "minimizedFloatingPanelExample" ]
                  (overlaySurface minimizedFloatingPanelExample)
              sample
                  "floating-panel-checklist"
                  "Checklist"
                  [ "floatingPanelChecklistContent"; "floatingPanelChecklistExample" ]
                  (overlaySurface floatingPanelChecklistExample) ]
        | "breadcrumbs" ->
            [ sample "breadcrumbs" "Default" [ "breadcrumbsExample" ] breadcrumbsPreview
              sample
                  "breadcrumbs-middle"
                  "Middle overflow"
                  [ "breadcrumbTrail"; "middleBreadcrumbsExample" ]
                  (themedSurface middleBreadcrumbsExample)
              sample
                  "breadcrumbs-visible"
                  "Available space"
                  [ "breadcrumbTrail"; "visibleBreadcrumbsExample" ]
                  (themedSurface visibleBreadcrumbsExample)
              |> note "The full trail stays visible while it fits. Resize the preview to collapse older ancestors into the ellipsis; they return when space is available. Width, not item count or viewport breakpoints, owns this behavior." ]
        | "side-nav" ->
            [ sample
                  "side-nav-default"
                  "Default"
                  [ "DefaultSideNavDestination"; "defaultSideNavUrl"; "defaultSideNav" ]
                  defaultSideNavPreview
              sample
                  "side-nav-structure"
                  "Sections and collapsible groups"
                  [ "sideNavStructureExample" ]
                  sideNavStructurePreview
              sample
                  "side-nav-slots"
                  "Header, context, content and footer"
                  [ "sideNavRowIcon"; "sideNavSlotsExample" ]
                  sideNavSlotsPreview
              sample "side-nav" "Nested navigation with badges and actions" sideNavSource sideNavigationPreview ]
        | "page-top-bar" ->
            [ sample "page-top-bar" "With breadcrumbs" [ "pageTopBarExample"; "renderPageTopBar" ] pageTopBarPreview
              sample
                  "page-top-bar-shell"
                  "Full-width shell bar"
                  [ "shellTopBarExample" ]
                  (shellTopBarExample |> themedSurface) ]
        | "page-header" ->
            [ sample "page-header-default" "Default" [ "defaultPageHeader" ] (detailsSurface defaultPageHeader)
              sample
                  "page-header"
                  "Title and page actions"
                  [ "refreshIcon"; "pageHeaderLayoutExample" ]
                  (themedSurface pageHeaderLayoutExample) ]
        | "section" ->
            [ sample "section-default" "Default" [ "defaultSection" ] (detailsSurface defaultSection)
              sample "section-headerless" "Without a visible heading" [ "periodNote" ] (detailsSurface periodNote) ]
        | "browser" ->
            [ sample "browser-default" "Default" [ "defaultBrowser" ] (centered defaultBrowser)
              sample "browser" "With an address bar" [ "browserWithAddress" ] (centered browserWithAddress)
              sample
                  "browser-review"
                  "Review controls and expansion"
                  [ "fixtureAppearance"
                    "fixtureWindowControl"
                    "browserReviewFrame"
                    "browserWithControls" ]
                  (themedSurface browserWithControls)
              |> note
                  "Install color-mode and dropdown-menu, and initialize ColorMode.assets once in the host head as shown in Theming. withWindowControls and withToolbarActions accept host-owned links/controls; withExpanded fills the viewport without dropping the address bar. Your host owns the expanded URL, review state, journey and documentation visibility. See the Specification page example for the complete review composition." ]
        | "phone" ->
            [ sample "phone-default" "Default" [ "defaultPhone" ] (centered defaultPhone)
              sample "phone" "With screen content" [ "phoneWithContent" ] (centered phoneWithContent)
              sample
                  "phone-review"
                  "Review controls and expansion"
                  [ "fixtureAppearance"
                    "fixtureWindowControl"
                    "phoneReviewFrame"
                    "phoneWithControls" ]
                  (themedSurface phoneWithControls)
              |> note
                  "Install color-mode and dropdown-menu, and initialize ColorMode.assets once in the host head as shown in Theming. withToolbar places consumer-owned review controls above the device. withExpanded retains desktop proportions and presents edge-to-edge content on narrow or short screens, without a fabricated browser address. Controls and destinations remain host-owned." ]
        | "page" ->
            [ sample "page-default" "Default" [ "defaultPage" ] (fullBleedThemedSurface defaultPage)
              sample "page-canvas" "Remaining-height canvas" [ "canvasPage" ] canvasPagePreview ]
        | "collection" ->
            [ sample "collection-default" "Default" [ "defaultCollection" ] (detailsSurface defaultCollection) ]
        | "detail" -> [ sample "detail-default" "Default" [ "defaultDetail" ] (detailsSurface defaultDetail) ]
        | "app-shell" -> appShellExamples Dashboard
        | "page-examples/account-management" -> accountManagementExamples LedgerAccounts
        | "bottom-navigation" ->
            [ sample
                  "bottom-navigation-default"
                  "Default"
                  [ "defaultBottomNavigation" ]
                  (detailsSurface defaultBottomNavigation)
              sample
                  "bottom-navigation"
                  "Four destinations"
                  [ "bottomNavigationExample" ]
                  (detailsSurface bottomNavigationExample)
              sample
                  "bottom-navigation-compact"
                  "Three destinations"
                  [ "compactBottomNavigationExample" ]
                  (detailsSurface compactBottomNavigationExample) ]
        | "day-calendar" -> dayCalendarExamples
        | "week-calendar" -> weekCalendarExamples
        | "year-calendar" -> yearCalendarExamples
        | "media-library" ->
            [ sample
                  "media-library"
                  "Library and editor"
                  (shellSource @ [ "mediaLibraryExample" ])
                  (detailsSurface mediaLibraryExample) ]
        | value when value.StartsWith("page-examples/", StringComparison.Ordinal) ->
            let page =
                PageExamples.pages
                |> List.find (fun page -> value = "page-examples/" + PageExamples.slug page)

            [ { id = "components-" + PageExamples.slug page
                title = PageExamples.title page + " workspace"
                source = PageExamples.source page
                preview = connectedGallerySurface (PageExamples.preview page PageExamples.defaultQuery)
                note = None } ]
        | id -> invalidArg (nameof id) $"No component examples registered for '{id}'."

    let private pageExampleBuildingBlocks =
        function
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
              "floating-panel", "Floating panel" ]
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
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  example.id
                  example.title
                  [ fveExample {
                        _name example.id
                        _label example.title
                        _variant ExampleVariant.Gallery
                        _language "fsharp"
                        fveExampleCode { example.source }
                        example.preview
                    } ]
              DocumentationSection.create
                  "building-blocks"
                  "Built with"
                  [ pageExampleBuildingBlocks page
                    |> List.map (fun (path, label) -> "/components/" + path, label)
                    |> buildingBlockLinks ((PageExamples.title page) + " building blocks") ] ]

    let allExamples () =
        allRegistrations
        |> List.filter (fun page ->
            page.path.StartsWith("/components/", StringComparison.Ordinal)
            && page.path <> "/components/installation"
            && not (guideRegistrations |> List.exists (fun guide -> guide.path = page.path)))
        |> List.collect (fun page -> examplesFor (page.path.Substring("/components/".Length)))

    let installationPage =
        DocumentationPage.create installationRegistration.id installationRegistration.title
        |> DocumentationPage.withDescription
            "Install editable components, then build their Tailwind styles and Datastar-powered interactivity with npm."
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "tool"
                  "Install the CLI"
                  [ prose
                        "Start with a .NET web project and Node.js LTS with npm. Run these commands from your repository root:"
                    code
                        "shell"
                        "dotnet new tool-manifest\ndotnet tool install FSharp.ViewEngine.Cli\ndotnet tool restore" ]
              DocumentationSection.create
                  "project"
                  "Add components"
                  [ code
                        "shell"
                        "dotnet fve init src/Acme.Components/Acme.Components.fsproj --namespace Acme.Components\ndotnet fve add button field input --config src/Acme.Components/fve.json\ndotnet add src/Acme.Web/Acme.Web.fsproj reference src/Acme.Components/Acme.Components.fsproj"
                    prose
                        "The CLI copies F# source, component JavaScript and required runtime assets. Commit the installed source, fve.json and tool manifest." ]
              DocumentationSection.create
                  "npm"
                  "Install the build tools"
                  [ p {
                        "Install "

                        a {
                            _href "https://tailwindcss.com/docs/installation/tailwind-cli"
                            _class "text-[var(--fve-brand-text)] underline underline-offset-2"
                            "Tailwind CSS"
                        }

                        ", its CLI and Typography plugin, and "

                        a {
                            _href "https://esbuild.github.io/"
                            _class "text-[var(--fve-brand-text)] underline underline-offset-2"
                            "esbuild"
                        }

                        " together in your web project:"
                    }
                    code
                        "shell"
                        "cd src/Acme.Web\nnpm init -y\nnpm install --save-dev tailwindcss@4 @tailwindcss/cli@4 @tailwindcss/typography esbuild"
                    prose
                        "Run the remaining npm commands from this directory. Commit package.json and package-lock.json; use npm ci on subsequent installs." ]
              DocumentationSection.create
                  "tailwind"
                  "Configure Tailwind"
                  [ prose
                        "Create src/Acme.Web/input.css. Scan your views and the installed F# and JavaScript so Tailwind includes the components' styles:"
                    code "css" tailwindExample ]
              DocumentationSection.create
                  "runtime"
                  "Import component JavaScript"
                  [ prose
                        "Create src/Acme.Web/js/app.js. Import the shared runtime once, then add the imports shown in each component's Installation section. For Button:"
                    code
                        "javascript"
                        "import '../../Acme.Components/Components/Assets/datastar-rocket.1.0.4.js';\nimport '../../Acme.Components/Components/Assets/button.js';"
                    p {
                        "The installed Rocket runtime includes "

                        a {
                            _href "https://data-star.dev/"
                            _class "text-[var(--fve-brand-text)] underline underline-offset-2"
                            "Datastar"
                        }

                        ". No separate Datastar installation or script is needed."
                    } ]
              DocumentationSection.create
                  "build"
                  "Build and watch"
                  [ prose "Add these scripts to your web project's package.json:"
                    code
                        "json"
                        """{
  "scripts": {
    "build:css": "tailwindcss -i input.css -o wwwroot/css/output.css --minify",
    "build:js": "esbuild js/app.js --bundle --format=esm --splitting --outdir=wwwroot/assets --minify",
    "watch:css": "npm run build:css -- --watch",
    "watch:js": "npm run build:js -- --watch"
  }
}"""
                    code "shell" "npm run build:css\nnpm run build:js"
                    prose
                        "During development, run npm run watch:css and npm run watch:js in separate terminals alongside your .NET host." ]
              DocumentationSection.create
                  "namespace"
                  "Use the components"
                  [ prose "Load the generated stylesheet and module once. Apply a component theme to the owning region:"
                    code
                        "fsharp"
                        """open FSharp.ViewEngine
open Acme.Components
open type Html

let page =
    html {
        head {
            title { "My application" }
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            link { _rel "stylesheet"; _href "/css/output.css" }
            script { _type "module"; _src "/assets/app.js" }
        }
        body {
            for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
            fveButton { _color Color.Primary; _variant ButtonVariant.Solid; "Save" }
        }
    }""" ];
              DocumentationSection.create
                  "updating"
                  "Update installed components"
                  [ code
                        "shell"
                        "dotnet tool update FSharp.ViewEngine.Cli\ndotnet fve diff --config src/Acme.Components/fve.json"
                    prose
                        "The tool manifest pins the registry version. Review the diff before updating installed source: fve add preserves local edits; --overwrite replaces them only when you explicitly choose it. Commit the tool manifest and installed source together." ] ]

    let buttonPage = gallery buttonRegistration (examplesFor "button")
    let linkPage = gallery linkRegistration (examplesFor "link")
    let buttonGroupPage = gallery buttonGroupRegistration (examplesFor "button-group")
    let badgePage = gallery badgeRegistration (examplesFor "badge")
    let kbdPage = gallery kbdRegistration (examplesFor "kbd")
    let separatorPage = gallery separatorRegistration (examplesFor "separator")
    let skeletonPage = gallery skeletonRegistration (examplesFor "skeleton")

    let loadingIndicatorPage =
        gallery loadingIndicatorRegistration (examplesFor "loading-indicator")

    let progressPage = gallery progressRegistration (examplesFor "progress")
    let emptyStatePage = gallery emptyStateRegistration (examplesFor "empty-state")

    let tablePageFor sort =
        gallery tableRegistration (tableExamples sort)

    let tablePage = tablePageFor NameAscending
    let chartPage = gallery chartRegistration (examplesFor "chart")

    let descriptionListPage =
        gallery descriptionListRegistration (examplesFor "description-list")

    let metricPage = gallery metricRegistration (examplesFor "metric")
    let avatarPage = gallery avatarRegistration (examplesFor "avatar")
    let copyRevealPage = gallery copyRevealRegistration (examplesFor "copy-reveal")
    let inputPage = gallery inputRegistration (examplesFor "input")
    let fieldPage = gallery fieldRegistration (examplesFor "field")
    let inputGroupPage = gallery inputGroupRegistration (examplesFor "input-group")

    let fileSelectionPage =
        gallery fileSelectionRegistration (examplesFor "file-selection")

    let tagInputPage = gallery tagInputRegistration (examplesFor "tag-input")
    let formLayoutsPage = gallery formLayoutsRegistration (examplesFor "form-layouts")
    let textareaPage = gallery textareaRegistration (examplesFor "textarea")

    let errorSummaryPage =
        gallery errorSummaryRegistration (examplesFor "error-summary")

    let noticePage = gallery noticeRegistration (examplesFor "notice")
    let notificationPage = gallery notificationRegistration (examplesFor "notification")
    let selectPage = gallery selectRegistration (examplesFor "select")
    let selectFixturePage = gallery selectRegistration (examplesFor "select-fixtures")
    let datePickerPage = gallery datePickerRegistration (examplesFor "date-picker")
    let checkboxPage = gallery checkboxRegistration (examplesFor "checkbox")
    let switchPage = gallery switchRegistration (examplesFor "switch")

    let toggleButtonPage =
        gallery toggleButtonRegistration (examplesFor "toggle-button")

    let toggleGroupPage = gallery toggleGroupRegistration (examplesFor "toggle-group")
    let breadcrumbsPage = gallery breadcrumbsRegistration (examplesFor "breadcrumbs")
    let sideNavPage = gallery sideNavRegistration (examplesFor "side-nav")
    let tabsPage = gallery tabsRegistration (examplesFor "tabs")
    let radioGroupPage = gallery radioGroupRegistration (examplesFor "radio-group")
    let choiceCardsPage = gallery choiceCardsRegistration (examplesFor "choice-cards")

    let dropdownMenuPage =
        gallery dropdownMenuRegistration (examplesFor "dropdown-menu")

    let commandPage = gallery commandRegistration (examplesFor "command")
    let dialogPage = gallery dialogRegistration (examplesFor "dialog")
    let drawerPage = gallery drawerRegistration (examplesFor "drawer")

    let floatingPanelPage =
        gallery floatingPanelRegistration (examplesFor "floating-panel")

    let tooltipPage = gallery tooltipRegistration (examplesFor "tooltip")
    let popoverPage = gallery popoverRegistration (examplesFor "popover")
    let pageTopBarPage = gallery pageTopBarRegistration (examplesFor "page-top-bar")
    let pageHeaderPage = gallery pageHeaderRegistration (examplesFor "page-header")
    let sectionPage = gallery sectionRegistration (examplesFor "section")
    let browserPage = gallery browserRegistration (examplesFor "browser")
    let resizablePage = gallery resizableRegistration (examplesFor "resizable")
    let phonePage = gallery phoneRegistration (examplesFor "phone")
    let pagePage = gallery pageRegistration (examplesFor "page")

    let collectionPageDocumentation =
        gallery collectionRegistration (examplesFor "collection")

    let detailPageDocumentation = gallery detailRegistration (examplesFor "detail")

    let paginationPageFor requestedPage =
        gallery paginationRegistration (paginationGalleryExamples requestedPage)

    let paginationPage = paginationPageFor 2

    let appShellPageFor current =
        gallery appShellRegistration (appShellExamples current)

    let appShellPage = appShellPageFor Dashboard

    let accountManagementPageWith workspace current =
        let examples =
            accountManagementExamples current
            |> List.map (fun example ->
                if example.id = "components-account-management" then
                    { example with
                        preview = shellFixtureWith workspace current }
                else
                    example)

        gallery accountManagementRegistration examples
        |> DocumentationPage.withFixtures [ shellFixtureConfigWith workspace current ]

    let accountManagementPageFor current =
        accountManagementPageWith defaultAccountWorkspace current

    let accountManagementPage = accountManagementPageFor LedgerAccounts

    let bottomNavigationPage =
        gallery bottomNavigationRegistration (examplesFor "bottom-navigation")

    let messagePage = gallery messageRegistration (examplesFor "message")
    let dayCalendarPage = gallery dayCalendarRegistration dayCalendarExamples
    let weekCalendarPage = gallery weekCalendarRegistration weekCalendarExamples

    let monthCalendarPage =
        gallery monthCalendarRegistration (examplesFor "month-calendar")

    let yearCalendarPage = gallery yearCalendarRegistration yearCalendarExamples

    let mediaLibraryPage =
        gallery mediaLibraryRegistration (examplesFor "media-library")

    let interactionPage = ComponentGuideExamples.signalsPage interactionRegistration
    let themingPage = ComponentGuideExamples.themingPage themingRegistration

    let private pages =
        [ installationRegistration.path, installationPage
          buttonRegistration.path, buttonPage
          linkRegistration.path, linkPage
          buttonGroupRegistration.path, buttonGroupPage
          badgeRegistration.path, badgePage
          kbdRegistration.path, kbdPage
          separatorRegistration.path, separatorPage
          skeletonRegistration.path, skeletonPage
          loadingIndicatorRegistration.path, loadingIndicatorPage
          progressRegistration.path, progressPage
          emptyStateRegistration.path, emptyStatePage
          tableRegistration.path, tablePage
          chartRegistration.path, chartPage
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
          dayCalendarRegistration.path, dayCalendarPage
          weekCalendarRegistration.path, weekCalendarPage
          monthCalendarRegistration.path, monthCalendarPage
          yearCalendarRegistration.path, yearCalendarPage
          mediaLibraryRegistration.path, mediaLibraryPage
          interactionRegistration.path, interactionPage
          themingRegistration.path, themingPage ]
        @ [ for page in PageExamples.pages -> PageExamples.url page, pageExamplePageFor page PageExamples.defaultQuery ]
        |> Map.ofList

    let tryPage path = Map.tryFind path pages
