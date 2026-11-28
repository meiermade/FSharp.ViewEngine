namespace Docs.Common

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar

module SharedExamples =
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

    let galleryDestinationAttributes =
        let contain selector =
            $"const link = evt.target.closest?.('{selector}'); if (link && !link.hash && !link.hasAttribute('download') && !link.hasAttribute('data-fve-app-mode-launch') && !link.hasAttribute('data-review-expand') && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) evt.preventDefault()"
        [ _attr ("data-fve-gallery-contained", "true")
          _dataOn ("click", [ "capture" ], contain "a[data-fve-link-navigation]")
          _dataOn ("click", contain "a[href]") ]

    let moreActionsIcon = fveIcon { _name IconName.EllipsisHorizontal; _variant IconVariant.Micro }

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
                        (if index = 1 then "bg-[var(--fve-brand-subtle)]" else "[&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))]"))
                    span { _class "min-w-0 flex-[1_0_min(10rem,100%)] break-words text-sm"; account.name }
                    rowActionMenu "button-row-actions" account
                }
            div {
                _class "flex min-w-0 flex-wrap items-center justify-between gap-3 px-3 py-2 text-sm text-[var(--fve-muted-text)]"
                span { _class "min-w-0 flex-[1_0_min(10rem,100%)] break-words"; "Actions unavailable" }
                fveButton {
                    _variant ButtonVariant.Ghost; _size Size.Small; _class "ms-auto"
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

    let shellFixtureNavigationAttributes =
        let link = shellDestinationLink

        [ _dataOn (
              "click", [ "capture" ],
              $"if ({link} && !{link}.hasAttribute('data-fve-app-mode-launch') && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.button === 0) {{ evt.preventDefault(); @get({link}.getAttribute('href').replace('/components/page-examples/account-management?', '/components/page-examples/account-management/fixture?')) }}"
          ) ]

    let sourceText =
        lazy (SourceRegion.readEmbeddedModules typeof<DocPage>.Assembly "Docs.ComponentSources.")

    let themedPreview (content: HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"

            for attribute in galleryDestinationAttributes do
                attribute

            content
        }

    let themedSurface (content: HtmlElement) =
        div {
            for attribute in ComponentsTheme.attributes ComponentsTheme.sky do
                attribute

            div {
                _class "bg-[var(--fve-background)] p-[12px] sm:p-6 text-[var(--fve-text)]"
                content
            }
        }
        |> themedPreview

    let fullBleedThemedSurface (content: HtmlElement) =
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

    let tailwindExample =
        """@import "tailwindcss" source(none);
@plugin "@tailwindcss/typography";
@custom-variant dark (&:where(.dark, .dark *));
@source "../Acme.Components/Components/**/*.fs";
@source "../Acme.Components/Components/Assets/**/*.js";
@source "./**/*.fs";"""

    let refreshIcon =
        fveIcon { _name IconName.ArrowPath; _variant IconVariant.Micro }
