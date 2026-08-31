namespace Docs.Pages

open System
open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Docs
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

    type AccountRow =
        { id:int
          name:string
          status:AccountStatus
          balance:decimal }

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
        | DropdownMenuGuide -> "/components/dropdown-menu#keyboard"

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.Components.fs")

    let sourceFor id = SourceRegion.extract id sourceText.Value

    let private themedPreview (content:HtmlElement) =
        div {
            _class "docs-components-preview"
            content
        }

    let private themedSurface (content:HtmlElement) =
        div {
            for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
            div {
                _class "rounded-xl bg-[var(--fve-page)] p-5 text-[var(--fve-text)]"
                content
            }
        }
        |> themedPreview

    // docs-example:start button
    let private trackButtonActivation config =
        config
        |> Button.withAttributes [ _dataOn ("click", "$buttonActivations++") ]
        |> Button.render

    let createAccountButton =
        Button.create "Create account"
        |> Button.withVariant ButtonVariant.Primary
        |> trackButtonActivation

    let importButton =
        Button.create "Import"
        |> Button.withVariant ButtonVariant.Secondary
        |> Button.withSize ControlSize.Small
        |> trackButtonActivation

    let viewActivityButton =
        Button.create "View activity"
        |> Button.withVariant ButtonVariant.Ghost
        |> trackButtonActivation

    let removeDraftButton =
        Button.create "Remove draft"
        |> Button.withVariant ButtonVariant.Destructive
        |> trackButtonActivation

    let pendingSyncButton =
        Button.create "Sync accounts"
        |> Button.withVariant ButtonVariant.Primary
        |> Button.pending
        |> trackButtonActivation

    let disabledDeleteButton =
        Button.create "Delete account"
        |> Button.withVariant ButtonVariant.Destructive
        |> Button.disabled
        |> trackButtonActivation

    let buttonPreview =
        themedSurface (
            div {
                _dataSignals "{buttonActivations: 0}"
                div {
                    _class "flex flex-wrap items-center gap-3"
                    [ createAccountButton
                      importButton
                      viewActivityButton
                      removeDraftButton
                      pendingSyncButton
                      disabledDeleteButton ]
                }
                output {
                    _id "button-activation-count"
                    _class "mt-4 block text-sm text-[var(--fve-muted-text)]"
                    "Activations: "
                    span { _dataText "$buttonActivations"; "0" }
                }
            })
    // docs-example:end button

    let private plusIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path d="M10 4.25a.75.75 0 0 1 .75.75v4.25H15a.75.75 0 0 1 0 1.5h-4.25V15a.75.75 0 0 1-1.5 0v-4.25H5a.75.75 0 0 1 0-1.5h4.25V5a.75.75 0 0 1 .75-.75Z"/></svg>"""

    let private refreshIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M15.312 4.683A7.25 7.25 0 1 0 17.25 10a.75.75 0 0 0-1.5 0 5.75 5.75 0 1 1-1.604-3.982H12.5a.75.75 0 0 0 0 1.5h3.5a.75.75 0 0 0 .75-.75v-3.5a.75.75 0 0 0-1.5 0v1.415Z" clip-rule="evenodd"/></svg>"""

    let private removeIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M8.75 3.5a1.25 1.25 0 0 1 2.5 0V4h3a.75.75 0 0 1 0 1.5h-.44l-.55 9.08A2.25 2.25 0 0 1 11.02 16.7H8.98a2.25 2.25 0 0 1-2.24-2.12L6.19 5.5h-.44a.75.75 0 0 1 0-1.5h3v-.5Zm-1.06 2 .54 8.99a.75.75 0 0 0 .75.71h2.04a.75.75 0 0 0 .75-.71l.54-8.99H7.69Z" clip-rule="evenodd"/></svg>"""

    // docs-example:start icon-button
    let private trackIconButtonActivation config =
        config
        |> IconButton.withAttributes [ _dataOn ("click", "$iconButtonActivations++") ]
        |> IconButton.render

    let addAccountIconButton =
        IconButton.create "Add account" plusIcon
        |> IconButton.withVariant ButtonVariant.Primary
        |> trackIconButtonActivation

    let refreshAccountsIconButton =
        IconButton.create "Refresh accounts" refreshIcon
        |> trackIconButtonActivation

    let refreshingIconButton =
        IconButton.create "Refreshing accounts" refreshIcon
        |> IconButton.withVariant ButtonVariant.Ghost
        |> IconButton.pending
        |> trackIconButtonActivation

    let disabledRemoveIconButton =
        IconButton.create "Remove account" removeIcon
        |> IconButton.withVariant ButtonVariant.Destructive
        |> IconButton.disabled
        |> trackIconButtonActivation

    let iconButtonPreview =
        themedSurface (
            div {
                _dataSignals "{iconButtonActivations: 0}"
                div {
                    _class "flex flex-wrap items-center gap-3"
                    [ addAccountIconButton
                      refreshAccountsIconButton
                      refreshingIconButton
                      disabledRemoveIconButton ]
                }
                output {
                    _id "icon-button-activation-count"
                    _class "mt-4 block text-sm text-[var(--fve-muted-text)]"
                    "Activations: "
                    span { _dataText "$iconButtonActivations"; "0" }
                }
            })
    // docs-example:end icon-button

    // docs-example:start badge
    let badgePreview =
        themedSurface (
            div {
                _class "flex flex-wrap items-center gap-3"
                [ Badge.create "Internal" |> Badge.render
                  Badge.create "New" |> Badge.withTone Tone.Brand |> Badge.render
                  Badge.create "Reconciled" |> Badge.withTone Tone.Positive |> Badge.render ]
            })
    // docs-example:end badge

    // docs-example:start status
    let reviewStatus =
        Status.create "Needs review"
        |> Status.withTone Tone.Warning
        |> Status.render

    let statusPreview =
        themedSurface (
            div {
                _class "flex flex-wrap items-center gap-3"
                [ Status.positive "Active"; reviewStatus ]
            })
    // docs-example:end status

    // docs-example:start loading-indicator
    let loadingIndicatorPreview =
        themedSurface (
            div {
                _class "flex flex-wrap items-center gap-5"
                [ LoadingIndicator.create "Loading account balances"
                  |> LoadingIndicator.withSize ControlSize.Small
                  |> LoadingIndicator.render
                  LoadingIndicator.create "Refreshing transactions"
                  |> LoadingIndicator.withSize ControlSize.Large
                  |> LoadingIndicator.withVisibleLabel
                  |> LoadingIndicator.render ]
            })
    // docs-example:end loading-indicator

    // docs-example:start empty-state
    let emptyStatePreview =
        EmptyState.create "No accounts yet" "Create an account to start tracking balances and entries."
        |> EmptyState.withIcon plusIcon
        |> EmptyState.withActions (Button.primary "Create account")
        |> EmptyState.render
        |> themedSurface
    // docs-example:end empty-state

    let private rows =
        [ { id = 101; name = "Operating"; status = Active; balance = 42800M }
          { id = 102; name = "Tax reserve"; status = Pending; balance = 12750M } ]

    // docs-example:start table
    let private accountTableConfig =
        Table.create "Accounts" [
            Table.column "Account" (fun row ->
                a { _href (destinationUrl (Account row.id)); _class "font-medium text-[var(--fve-brand-text)]"; row.name })
            |> Table.asRowHeader
            Table.column "Status" (fun row ->
                match row.status with
                | Active -> Status.positive "Active"
                | Pending -> Status.warning "Pending"
                | Suspended -> Status.create "Suspended" |> Status.withTone Tone.Critical |> Status.render
                | Scheduled -> Status.create "Scheduled" |> Status.withTone Tone.Informative |> Status.render)
            Table.column "Balance" (fun row -> text $"${row.balance:N0}")
            |> Table.alignEnd
            Table.column "Actions" (fun row ->
                a {
                    _href (destinationUrl (Account row.id))
                    _ariaLabel $"View {row.name}"
                    _class "font-medium text-[var(--fve-brand-text)]"
                    "View"
                })
            |> Table.alignEnd
        ] rows

    let accountTable = accountTableConfig |> Table.render

    let emptyAccountsTable =
        Table.create "Archived accounts" [
            Table.column "Account" (fun (row:AccountRow) -> text row.name)
            |> Table.asRowHeader
        ] []
        |> Table.withEmptyState (
            EmptyState.create "No archived accounts" "Archived accounts appear here without changing active results."
            |> EmptyState.render)
        |> Table.render

    let tablePreview =
        let compactTable =
            accountTableConfig
            |> Table.withVisibleCaption
            |> Table.withDensity Density.Compact
            |> Table.render
        themedSurface (div { _class "grid gap-6"; [ compactTable; emptyAccountsTable ] })
    // docs-example:end table

    // docs-example:start description-list
    let accountDetails =
        DescriptionList.create [
            DetailField.text "Type" "Asset"
            DetailField.status "Status" (Status.positive "Active")
            DetailField.text "Available balance" "$42,800"
            |> DetailField.withDescription "Includes cleared entries through today."
        ]
        |> DescriptionList.withColumns DescriptionListColumns.Three
        |> DescriptionList.render

    let descriptionListPreview = themedSurface accountDetails
    // docs-example:end description-list

    // docs-example:start metric
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

    let metricPreview =
        themedSurface (div { _class "grid gap-6 sm:grid-cols-2"; [ availableBalanceMetric; pendingEntriesMetric ] })
    // docs-example:end metric

    // docs-example:start pagination
    type PaginationDestination = PaginationPage of int

    let paginationDestinationUrl (PaginationPage page) =
        $"/components/pagination?page={page}#components-pagination-panel-preview"

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
        |> themedSurface
    // docs-example:end pagination

    let private balanceChartVisual =
        raw """<svg viewBox="0 0 480 190" class="h-48 min-w-[28rem] w-full" aria-hidden="true"><g fill="var(--fve-brand-subtle)"><rect x="52" y="92" width="58" height="66" rx="4"/><rect x="142" y="72" width="58" height="86" rx="4"/><rect x="232" y="51" width="58" height="107" rx="4"/><rect x="322" y="31" width="58" height="127" rx="4"/></g><path d="M52 86 L171 64 L261 43 L351 23" fill="none" stroke="var(--fve-brand-solid)" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/><g fill="var(--fve-muted-text)" font-size="12" text-anchor="middle"><text x="81" y="180">May</text><text x="171" y="180">Jun</text><text x="261" y="180">Jul</text><text x="351" y="180">Aug</text></g></svg>"""

    let private balanceChartSummary =
        div {
            p { "Balance increased every month, from $31,200 in May to $42,800 in August." }
            table {
                _class "mt-3 text-left text-sm"
                caption { _class "sr-only"; "Monthly operating balance data" }
                tbody {
                    for month, balance in [ "May", "$31,200"; "June", "$34,900"; "July", "$38,600"; "August", "$42,800" ] do
                        tr { th { _scope "row"; _class "pr-6 font-medium"; month }; td { balance } }
                }
            }
        }

    // docs-example:start chart
    let balanceChart =
        Chart.create "operating-balance" "Operating balance" balanceChartSummary balanceChartVisual
        |> Chart.withUnits "USD · month end"
        |> Chart.withLegend (span { "Bars: month-end balance · Line: trend" })
        |> Chart.withAnnotations (span { "August closes at $42,800." })
        |> Chart.withVisibleSummary
        |> Chart.render

    let emptyBalanceChart =
        Chart.empty "new-account-balance" "New account balance" (p { "No historical balance data is available." }) (
            EmptyState.create "No balance history" "Balances appear after the first posted entry."
            |> EmptyState.render)
        |> Chart.render

    let chartPreview =
        themedSurface (div { _class "grid gap-8"; [ balanceChart; emptyBalanceChart ] })
    // docs-example:end chart

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
        button {
            _id id
            _type "submit"
            _class "inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-solid)] px-3 py-[var(--fve-control-padding-block)] text-sm font-semibold text-white outline-none transition-colors hover:bg-[var(--fve-brand-hover)] active:bg-[var(--fve-brand-active)] focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-[var(--fve-brand-ring)]"
            label
        }

    let private choiceResult id result =
        output {
            _id id
            _role "status"
            _class "block min-h-5 text-sm text-[var(--fve-muted-text)]"
            defaultArg result "Submit this ordinary form to see the server-owned result."
        }

    // docs-example:start select
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

    let selectPreview =
        themedSurface (div { _class "grid items-start gap-6 sm:grid-cols-2"; statusSelect; disabledStatusSelect; pendingStatusSelect })
    // docs-example:end select

    // docs-example:start combobox
    let private accounts = [ 101, "Operating"; 102, "Tax reserve"; 103, "Payroll clearing" ]
    let private accountOptions values =
        values
        |> List.map (fun (value, label) ->
            Select.option value label
            |> (if value = 103 then Select.disable else id))

    let staticAccountCombobox =
        Combobox.create "staticAccount" "Static account" string (accountOptions accounts)
        |> Combobox.withId "components-static-account"
        |> Combobox.withSelected 101
        |> Combobox.withDescription "Filter locally supplied typed options."
        |> Combobox.clearable
        |> Combobox.render

    let private accountComboboxConfig =
        Combobox.create "account" "Parent account" string (accountOptions accounts)
        |> Combobox.withPlaceholder "Search accounts"
        |> Combobox.withDescription "Results remain authoritative on the server."
        |> Combobox.withEmptyMessage "No matching accounts"
        |> Combobox.withLoadingMessage "Loading accounts"
        |> Combobox.withSearch (ComboboxSearch.Remote "/components/accounts/search")
        |> Combobox.clearable

    let accountCombobox = accountComboboxConfig |> Combobox.render

    let accountComboboxOptions query retry =
        if String.Equals(query, "error", StringComparison.OrdinalIgnoreCase) && retry |> not then
            accountComboboxConfig
            |> Combobox.withSearch (ComboboxSearch.Remote "/components/accounts/search?retry=true")
            |> Combobox.withError "Accounts could not be loaded."
            |> Combobox.renderOptions
        else
            accounts
            |> List.filter (fun (_, label) -> String.IsNullOrWhiteSpace query || label.Contains(query, StringComparison.OrdinalIgnoreCase))
            |> accountOptions
            |> fun options -> accountComboboxConfig |> Combobox.withOptions options |> Combobox.renderOptions

    let loadingAccountCombobox =
        Combobox.create "loadingAccount" "Loading account" string []
        |> Combobox.withId "components-loading-account"
        |> Combobox.withSearch (ComboboxSearch.Remote "/components/accounts/search")
        |> Combobox.withLoadingMessage "Loading accounts"
        |> Combobox.loading
        |> Combobox.render

    let validationAccountCombobox =
        Combobox.create "validatedAccount" "Account with validation" string (accountOptions accounts)
        |> Combobox.withId "components-validated-account"
        |> Combobox.withDescription "Choose an account before continuing."
        |> Combobox.withValidation "Choose an available account."
        |> Combobox.clearable
        |> Combobox.render

    let disabledAccountCombobox =
        Combobox.create "disabledAccount" "Disabled account" string (accountOptions accounts)
        |> Combobox.withId "components-disabled-account"
        |> Combobox.withSelected 101
        |> Combobox.disabled
        |> Combobox.render

    let pendingAccountCombobox =
        Combobox.create "pendingAccount" "Updating account" string (accountOptions accounts)
        |> Combobox.withId "components-pending-account"
        |> Combobox.withSelected 102
        |> Combobox.pending
        |> Combobox.render

    let comboboxPreview =
        themedSurface (div {
            _class "grid items-start gap-6 sm:grid-cols-2"
            staticAccountCombobox
            accountCombobox
            validationAccountCombobox
            loadingAccountCombobox
            disabledAccountCombobox
            pendingAccountCombobox
        })
    // docs-example:end combobox

    // docs-example:start checkbox
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

    let checkboxPreview =
        themedSurface (div { _class "grid items-start gap-6 sm:grid-cols-2"; checkboxFormRegion false None None; includeArchived; pendingArchivedReview; disabledArchivedReview })
    // docs-example:end checkbox

    // docs-example:start switch
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

    let switchPreview =
        themedSurface (div { _class "grid items-start gap-6 sm:grid-cols-2"; postingNotifications; pendingNotifications; invalidNotifications })
    // docs-example:end switch

    // docs-example:start toggle-button
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

    let toggleButtonPreview =
        themedSurface (div { _class "flex flex-wrap items-center gap-3"; compactRows; pendingCompactRows; disabledCompactRows })
    // docs-example:end toggle-button

    // docs-example:start radio-group
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

    let radioGroupPreview =
        themedSurface (div { _class "grid items-start gap-6 sm:grid-cols-2"; postingMode; pendingPostingMode; disabledPostingMode })
    // docs-example:end radio-group

    let private accountMenuItems =
        [ MenuItem.link Settings "Account settings"
          MenuItem.separator
          MenuItem.destructiveAction "@delete('/accounts/101')" "Delete account" ]

    // docs-example:start dropdown-menu
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
            p { _class "mt-1 text-sm text-[var(--fve-critical-text)]"; _dataText "'Deleted drafts: ' + $menuDeletes"; "Deleted drafts: 0" }
            if refreshed then
                p { _role "status"; _class "mt-1 text-sm text-[var(--fve-positive-text)]"; "Actions refreshed from the server." }
        }

    let dropdownMenuPreview = themedSurface (dropdownMenuRegion false)
    // docs-example:end dropdown-menu

    let patchedDropdownMenuRegion = dropdownMenuRegion true

    // docs-example:start dialog
    let dialogConfig =
        Dialog.create "review-account-dialog" "Review account" (
            p { "Confirm the account settings before they are applied." })
        |> Dialog.withDescription "The dialog returns focus to its trigger when it closes."
        |> Dialog.withInitialFocus "review-account-dialog-close"

    let reviewDialogTrigger =
        dialogConfig
        |> Dialog.trigger "Review account"

    let reviewDialog =
        dialogConfig
        |> Dialog.withFooter (dialogConfig |> Dialog.closeButton "Close")
        |> Dialog.render

    let dialogPreview =
        themedSurface (div { _class "flex items-center gap-3"; [ reviewDialogTrigger; reviewDialog ] })
    // docs-example:end dialog

    let private statusFilter =
        Select.create "statusFilter" "Filter by status" statusValue statusOptions
        |> Select.withVisuallyHiddenLabel
        |> Select.withPlaceholder "All statuses"
        |> Select.render

    let private toolbar =
        div {
            _class "flex flex-wrap gap-3"
            input { _type "search"; _name "query"; _placeholder "Search accounts"; _class "min-h-9 rounded-[var(--fve-radius-control)] bg-[var(--fve-surface)] px-3 ring-1 ring-[var(--fve-border)]" }
            [ statusFilter ]
        }

    let private detailMenu =
        DropdownMenu.create "components-detail-actions" "Actions" accountMenuItems
        |> DropdownMenu.render destinationUrl

    // docs-example:start collection
    let collectionPage =
        Collection.create "Accounts" accountTable
        |> Collection.withDescription "Review balances and posting availability."
        |> Collection.withActions (Button.primary "New account")
        |> Collection.withToolbar toolbar
        |> Collection.render

    let collectionPreview = themedSurface collectionPage
    // docs-example:end collection

    // docs-example:start detail
    let detailPage =
        Detail.create "Operating" [
            div {
                h2 { _class "font-semibold"; "Account details" }
                dl {
                    _class "mt-4 grid gap-3 sm:grid-cols-2"
                    div { dt { _class "text-sm text-[var(--fve-muted-text)]"; "Type" }; dd { _class "font-medium"; "Asset" } }
                    div { dt { _class "text-sm text-[var(--fve-muted-text)]"; "Balance" }; dd { _class "font-medium"; "$42,800" } }
                }
            }
        ]
        |> Detail.withMetadata (Status.positive "Active")
        |> Detail.withActions detailMenu
        |> Detail.render

    let detailPreview = themedSurface detailPage
    // docs-example:end detail

    // docs-example:start app-shell
    let shellContent =
        div {
            h1 { _class "text-2xl font-semibold"; "Accounts" }
            p { _class "mt-2 text-sm text-[var(--fve-muted-text)]"; "Review balances and posting availability." }
        }

    let shellAccountMenu =
        DropdownMenu.create "components-shell-actions" "Account" accountMenuItems
        |> DropdownMenu.render destinationUrl

    let shellPreview =
        AppShell.create "Ledger" Accounts [
            NavigationItem.create Accounts "Accounts"
            NavigationItem.create Settings "Settings"
        ] shellContent
        |> AppShell.withBreadcrumbs [ NavigationItem.create Accounts "Accounts" ]
        |> AppShell.withAccountMenu shellAccountMenu
        |> AppShell.withTheme (
            ComponentsTheme.sky
            |> ComponentsTheme.withRadius Radius.Large
            |> ComponentsTheme.withDensity Density.Compact)
        |> AppShell.render destinationUrl
        |> themedPreview
    // docs-example:end app-shell

    let private registration id path navLabel title : DocPage =
        { id = id
          path = path
          aliases = []
          navLabel = navLabel
          category = "FSharp.ViewEngine.Components"
          title = title
          browserTitle = $"{title} · FSharp.ViewEngine.Components"
          nodes = [] }

    let overviewRegistration =
        registration "components-overview" "/components" "Overview" "Components"

    let installationRegistration =
        registration "components-installation" "/components/installation" "Installation" "Installation"

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
    let chartRegistration = registration "components-chart" "/components/chart" "Chart" "Chart"
    let selectRegistration = registration "components-select" "/components/select" "Select" "Select"
    let comboboxRegistration = registration "components-combobox" "/components/combobox" "Combobox" "Combobox"
    let checkboxRegistration = registration "components-checkbox" "/components/checkbox" "Checkbox" "Checkbox"
    let switchRegistration = registration "components-switch" "/components/switch" "Switch" "Switch"
    let toggleButtonRegistration = registration "components-toggle-button" "/components/toggle-button" "Toggle button" "Toggle button"
    let radioGroupRegistration = registration "components-radio-group" "/components/radio-group" "Radio group" "Radio group"
    let dropdownMenuRegistration = registration "components-dropdown-menu" "/components/dropdown-menu" "Dropdown menu" "Dropdown menu"
    let dialogRegistration = registration "components-dialog" "/components/dialog" "Dialog" "Dialog"
    let collectionRegistration = registration "components-collection" "/components/collection" "Collection" "Collection"
    let detailRegistration = registration "components-detail" "/components/detail" "Detail" "Detail"
    let appShellRegistration = registration "components-app-shell" "/components/app-shell" "App shell" "App shell"
    let interactionRegistration = registration "components-interaction" "/components/interaction-and-server-state" "Interaction and server state" "Interaction and server state"
    let accessibilityRegistration = registration "components-accessibility" "/components/accessibility" "Accessibility" "Accessibility"
    let themingRegistration = registration "components-theming" "/components/theming" "Theming and density" "Theming and density"
    let tailwindRegistration = registration "components-tailwind" "/components/tailwind-css" "Tailwind CSS" "Tailwind CSS setup"
    let customizationRegistration = registration "components-customization" "/components/customization" "Customization" "Customization"
    let versioningRegistration = registration "components-versioning" "/components/versioning" "Versioning" "Versioning"

    let actionRegistrations =
        [ buttonRegistration
          iconButtonRegistration
          badgeRegistration
          statusRegistration
          loadingIndicatorRegistration
          emptyStateRegistration ]
    let dataDisplayRegistrations = [ tableRegistration; descriptionListRegistration; metricRegistration; paginationRegistration; chartRegistration ]
    let formControlRegistrations =
        [ selectRegistration
          comboboxRegistration
          checkboxRegistration
          switchRegistration
          toggleButtonRegistration
          radioGroupRegistration ]
    let menuOverlayRegistrations = [ dropdownMenuRegistration; dialogRegistration ]
    let compositionRegistrations = [ collectionRegistration; detailRegistration; appShellRegistration ]
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
        @ menuOverlayRegistrations
        @ compositionRegistrations
        @ guideRegistrations

    let page = overviewRegistration

    let private themeExample = """let theme =
    ComponentsTheme.emerald
    |> ComponentsTheme.withRadius Radius.Large
    |> ComponentsTheme.withDensity Density.Comfortable

AppShell.create productName current navigation content
|> AppShell.withTheme theme
|> AppShell.render destinationUrl"""

    let private tailwindExample = """@import "tailwindcss";
@import "./FSharp.ViewEngine.Components.tailwind.css";

.acme-theme {
  --fve-brand-solid: oklch(58% 0.18 264);
  --fve-brand-hover: oklch(51% 0.20 264);
  --fve-brand-ring: oklch(68% 0.16 264);
}"""

    let private example (registration:DocPage) sourceId description preview =
        docsSection "example" "Example" [
            docsParagraph description
            docsCustom (docsExample $"components-{sourceId}" registration.title "fsharp" (sourceFor sourceId) preview) ]

    let private previewFirstExample (registration:DocPage) sourceId description preview =
        docsSection "example" "Example" [
            docsParagraph description
            docsCustom (Example.previewFirst $"components-{sourceId}" registration.title "fsharp" (sourceFor sourceId) preview) ]

    let private componentPage
        (registration:DocPage)
        (description:string)
        (sourceId:string)
        (preview:HtmlElement)
        (sections:DocsSection list) =
        docsArticle registration.id registration.title description (example registration sourceId description preview :: sections)

    let private catalogLink (href:string) (group:string) (title:string) (description:string) =
        a {
            _href href
            _class "docs-catalog-card"
            span { _class "docs-catalog-eyebrow"; group }
            strong { title }
            span { _class "docs-catalog-description"; description }
            span { _class "docs-catalog-action"; "View component "; raw "&rarr;" }
        }

    let private catalog =
        div {
            _class "docs-catalog-grid"
            catalogLink "/components/button" "ACTIONS" "Button" "Primary, secondary, ghost, destructive, active, disabled, pending, and sized actions."
            catalogLink "/components/icon-button" "ACTIONS" "Icon button" "Compact icon-only actions with a required accessible name."
            catalogLink "/components/badge" "METADATA" "Badge" "Compact categorical metadata using semantic tones."
            catalogLink "/components/status" "FEEDBACK" "Status" "Compact semantic state with accessible text and restrained color."
            catalogLink "/components/loading-indicator" "FEEDBACK" "Loading indicator" "Accessible indeterminate progress with visible or hidden text."
            catalogLink "/components/empty-state" "FEEDBACK" "Empty state" "Intentional no-content guidance with optional icon and actions."
            catalogLink "/components/table" "DATA DISPLAY" "Table" "Typed columns, captions, row headers, actions, density, and narrow overflow."
            catalogLink "/components/description-list" "DATA DISPLAY" "Description list" "Responsive labelled values and custom detail content."
            catalogLink "/components/metric" "DATA DISPLAY" "Metric" "Labelled values with optional trend, status, and description."
            catalogLink "/components/pagination" "DATA DISPLAY" "Pagination" "Typed destinations and consumer-owned page state."
            catalogLink "/components/chart" "DATA DISPLAY" "Chart" "Consumer-drawn visuals paired with accessible summaries and data."
            catalogLink "/components/select" "FORM CONTROLS" "Select" "A branded finite-choice control with APG keyboard behavior."
            catalogLink "/components/combobox" "FORM CONTROLS" "Combobox" "Editable local or remote search with stable submitted identity."
            catalogLink "/components/checkbox" "FORM CONTROLS" "Checkbox" "Independent checked state with ordinary form submission."
            catalogLink "/components/switch" "FORM CONTROLS" "Switch" "Immediate on/off settings with switch semantics."
            catalogLink "/components/toggle-button" "FORM CONTROLS" "Toggle button" "A pressed or unpressed action state."
            catalogLink "/components/radio-group" "FORM CONTROLS" "Radio group" "One submitted choice from a labelled set."
            catalogLink "/components/dropdown-menu" "MENUS" "Dropdown menu" "Keyboard-navigable actions and destinations."
            catalogLink "/components/dialog" "OVERLAYS" "Dialog" "Connected trigger, initial focus, dismissal, and focus restoration."
            catalogLink "/components/collection" "COMPOSITIONS" "Collection" "Collection heading, actions, toolbar, and application-owned results."
            catalogLink "/components/detail" "COMPOSITIONS" "Detail" "Detail heading, metadata, actions, and custom sections."
            catalogLink "/components/app-shell" "COMPOSITIONS" "App shell" "Typed destinations, navigation, breadcrumbs, account actions, and theme."
        }

    let overviewPage =
        docsArticle overviewRegistration.id overviewRegistration.title "Accessible, server-rendered Tailwind components with Datastar interaction and ordinary F# composition." [
            docsSection "start" "Start using Components" [
                docsParagraph "Install the independently versioned Components package alongside FSharp.ViewEngine, then configure the packaged Tailwind source manifest."
                docsCode "shell" "dotnet add package FSharp.ViewEngine.Components"
                docsCustom (p { _class "spec-paragraph"; a { _href "/components/installation"; "Read the complete installation guide" }; "." }) ]
            docsSection "browse" "Browse components" [ docsCustom catalog ]
            docsSection "principles" "Designed for typed server-rendered applications" [
                docsBullets [
                    "Required inputs stay visible. Accessible labels, names, typed destinations, and content belong in constructors."
                    "Optional behavior is piped. Immutable modifiers add variants, selection, themes, attributes, and application-owned behavior."
                    "Common cases stay concise. Helpers such as Button.primary and Status.positive render the ordinary case directly."
                    "Custom content stays HTML. Cells, dialog bodies, toolbars, actions, and page content remain ordinary HtmlElement values."
                    "Closed choices are typed. Variants, tones, sizes, density, radius, selected values, and destinations avoid visual strings." ] ] ]

    let installationPage =
        docsArticle installationRegistration.id installationRegistration.title "Install the package, import its Tailwind source manifest, and open the Components namespace." [
            docsSection "package" "Add the package" [
                docsCode "shell" "dotnet add package FSharp.ViewEngine.Components"
                docsParagraph "Components versions independently and declares its minimum compatible FSharp.ViewEngine dependency." ]
            docsSection "tailwind" "Configure Tailwind CSS" [
                docsParagraph "The NuGet package includes FSharp.ViewEngine.Components.tailwind.css under contentFiles/any/any. Copy the manifest into the application CSS source tree and import it after Tailwind CSS."
                docsCode "css" tailwindExample
                docsCustom (p { _class "spec-paragraph"; "See "; a { _href "/components/tailwind-css"; "Tailwind CSS setup" }; " for source detection and semantic theme details." }) ]
            docsSection "namespace" "Open the namespace" [
                docsCode "fsharp" "open FSharp.ViewEngine\nopen FSharp.ViewEngine.Components\nopen type Html"
                docsParagraph "Components are ordinary F# values and functions that compose with the existing HtmlElement builders." ] ]

    let buttonPage =
        componentPage buttonRegistration "Render semantic actions with typed variants, sizes, disabled state, and pending state." "button" buttonPreview [
            docsSection "usage" "Usage" [
                docsParagraph "Use Button.primary for the common case or start with Button.create and pipe typed configuration. Buttons render ordinary button semantics and remain application-owned actions."
                docsBullets [ "Primary identifies the single leading action in a region."; "Secondary, ghost, and destructive variants express hierarchy or consequence without raw palette names."; "Every available action provides visible hover, pressed, and focus feedback."; "Disabled and pending states prevent interaction while retaining an accessible label." ] ]
            docsSection "pending" "Pending actions" [ docsParagraph "Button.pending keeps the action label visible, adds an indeterminate loading glyph, exposes aria-busy, and uses native disabled behavior to prevent pointer, Enter, Space, and duplicate form activation." ]
            docsSection "accessibility" "Accessibility" [ docsParagraph "Supply action text that describes the result. Button preserves native keyboard activation and visible focus when available, while pending content never removes the accessible name." ] ]

    let iconButtonPage =
        componentPage iconButtonRegistration "Render a compact icon-only action whose accessible name is a required constructor input." "icon-button" iconButtonPreview [
            docsSection "usage" "Usage" [ docsParagraph "Pass independently authored icon markup as ordinary HtmlElement content. Use IconButton only when the symbol is familiar in context; otherwise prefer Button with visible action text." ]
            docsSection "accessibility" "Accessible name and state" [ docsParagraph "The required label becomes the button's accessible name while supplied icon content is decorative. Disabled and pending modifiers prevent activation; pending retains the same name and exposes busy state." ] ]

    let badgePage =
        componentPage badgeRegistration "Label compact categorical metadata with semantic tone and visible text." "badge" badgePreview [
            docsSection "when-to-use" "Badge or Status" [ docsParagraph "Use Badge for categories, ownership, release labels, or other compact metadata. Use Status when the text describes operational state such as Active, Pending, or Failed." ]
            docsSection "semantics" "Meaning beyond color" [ docsParagraph "Choose a semantic Tone and keep the label concise. The visible text communicates the category without relying on color, and optional leading content remains ordinary HtmlElement markup." ] ]

    let statusPage =
        componentPage statusRegistration "Present compact semantic state with accessible text and restrained color." "status" statusPreview [
            docsSection "usage" "Usage" [ docsParagraph "Use concise helpers for common tones or pipe Status.withTone when the state is application-specific. Status communicates meaning through text as well as semantic color." ]
            docsSection "semantics" "Choosing a tone" [ docsBullets [ "Positive confirms a successful or healthy state."; "Warning identifies a state that needs attention."; "Critical communicates failure or risk."; "Informative provides neutral operational context." ] ] ]

    let loadingIndicatorPage =
        componentPage loadingIndicatorRegistration "Communicate indeterminate progress with an accessible label and theme-compatible motion." "loading-indicator" loadingIndicatorPreview [
            docsSection "labels" "Visible or visually hidden labels" [ docsParagraph "LoadingIndicator.create requires text that describes what is loading. The label is visually hidden by default for compact contexts; use LoadingIndicator.withVisibleLabel when progress needs visible explanation." ]
            docsSection "accessibility" "Progress feedback" [ docsParagraph "The indicator uses polite status semantics and keeps its label in the accessibility tree. Its glyph follows the semantic brand theme and disables animation when reduced motion is requested." ] ]

    let emptyStatePage =
        componentPage emptyStateRegistration "Explain why a region has no content and offer an appropriate next action." "empty-state" emptyStatePreview [
            docsSection "content" "Useful empty guidance" [ docsParagraph "Use a specific title and description that explain the current state. Optional decorative icon and action slots accept ordinary HtmlElement values, so applications retain destinations, authorization, and behavior." ]
            docsSection "composition" "Placement and actions" [ docsParagraph "Render EmptyState where populated content would normally appear. Keep the primary recovery action visible, omit unauthorized actions on the server, and avoid using an empty state as a loading indicator." ] ]

    let tablePage =
        componentPage tableRegistration "Define typed columns over application-owned rows while Table supplies semantic structure and shared presentation." "table" tablePreview [
            docsSection "ownership" "Application-owned data" [ docsParagraph "The application owns querying, sorting, filtering, pagination, formatting, destinations, row actions, and authorization. Table owns the caption, header and row structure, alignment, density, and narrow overflow presentation." ]
            docsSection "accessibility" "Caption and headers" [ docsParagraph "The required caption gives the table an accessible name and may be visible or visually hidden. Use Table.asRowHeader for the identifying column; consumer-rendered cells and actions remain ordinary HtmlElement values." ]
            docsSection "responsive" "Dense and narrow data" [ docsParagraph "Compact density reduces cell padding without removing information. The labelled table region is keyboard reachable and scrolls horizontally when supplied columns need more width than the viewport." ] ]

    let descriptionListPage =
        componentPage descriptionListRegistration "Present labelled values with native description-list relationships and responsive columns." "description-list" descriptionListPreview [
            docsSection "fields" "Detail fields" [ docsParagraph "DetailField requires a meaningful label and accepts ordinary HtmlElement value content. Use text for simple values, status for state content, and withDescription for concise supporting context." ]
            docsSection "semantics" "Description-list semantics" [ docsParagraph "DescriptionList renders valid dl, dt, and dd relationships. Typed column choices change responsive presentation without changing reading order or hiding values." ] ]

    let metricPage =
        componentPage metricRegistration "Highlight a labelled value with optional trend, status, and supporting description." "metric" metricPreview [
            docsSection "ownership" "Consumer-owned meaning" [ docsParagraph "Metric arranges supplied content but does not infer currency, dates, trend direction, success, or domain status. Applications provide formatted values and explicit semantic status content." ]
            docsSection "composition" "Custom content" [ docsParagraph "The required value is ordinary HtmlElement content. Trend text receives a hidden semantic prefix, while status and descriptions remain optional." ] ]

    let paginationPageFor requestedPage =
        let description = "Present consumer-owned pagination state through typed destinations and explicit current-page semantics."
        docsArticle paginationRegistration.id paginationRegistration.title description [
            previewFirstExample paginationRegistration "pagination" description (paginationPreview requestedPage)
            docsSection "ownership" "Application-owned state" [ docsParagraph "The application chooses visible pages, gaps, previous and next destinations, result summary, URLs, query behavior, and durable state. This Docs example uses its own query string to demonstrate real browser navigation; Pagination renders only the navigation presentation." ]
            docsSection "accessibility" "Current and edge states" [ docsParagraph "The constructor requires an accessible navigation label and exactly one current page. Current-page and disabled edge semantics remain explicit, while page links retain ordinary browser navigation and history." ] ]

    let paginationPage = paginationPageFor 2

    let chartPage =
        componentPage chartRegistration "Pair consumer-supplied chart drawing with native figure structure and an accessible summary or data representation." "chart" chartPreview [
            docsSection "drawing" "Consumer-owned drawing" [ docsParagraph "Chart does not draw data or load a charting runtime. Applications supply SVG or HTML visual content, units, legend, annotations, and an explicit empty state." ]
            docsSection "alternative" "Accessible summary and data" [ docsParagraph "Every Chart requires summary content connected to its figure. Supply the essential trend and values as prose, a data table, or both; use withVisibleSummary when the alternative should also be visible." ]
            docsSection "semantics" "Figure relationships" [ docsParagraph "A stable ID connects figure, figcaption title, and summary. Legend and annotation regions remain named, while the consumer decides whether supplied visual markup needs its own graphic semantics." ] ]

    let selectPage =
        componentPage selectRegistration "Choose one value from a finite, non-editable set with branded select-only combobox behavior." "select" selectPreview [
            docsSection "when-to-use" "When to use Select" [ docsParagraph "Use Select when every available option can be known before interaction. Use Combobox when people need an editable query or remote filtering." ]
            docsSection "keyboard" "Keyboard behavior" [ docsParagraph "DOM focus stays on the trigger while aria-activedescendant identifies the active option. Closed and open Enter, Space, Alt+Arrow, Arrow, Home, End, PageUp, PageDown, Tab, Escape, bounded multi-character typeahead, and repeated-character cycling follow the select-only combobox model. Movement clamps at the list boundaries and skips disabled options." ]
            docsSection "states" "Required, validation, and pending" [ docsParagraph "Required state is exposed on the combobox and remains server-validated because the submitted value is a hidden field. Validation joins the description relationship and critical focus treatment. Disabled or pending Selects retain their visible value, close their popup, expose unavailable or busy state, and omit the hidden value from ordinary FormData." ]
            docsSection "forms" "Form submission" [ docsParagraph "The selected typed value is explicitly encoded into a hidden form field. This example posts Datastar signals to a real Docs endpoint so the server can reject a missing or disabled option and patch the stable form region; applications must apply the same validation to every received value." ] ]

    let comboboxPage =
        componentPage comboboxRegistration "Search local or server-owned options while keeping editable query text separate from submitted selection identity." "combobox" comboboxPreview [
            docsSection "when-to-use" "When to use Combobox" [ docsParagraph "Use Combobox when people need to type before selecting. Static search filters supplied options locally; remote search lets the application return authoritative options from an endpoint." ]
            docsSection "query-selection" "Query and selection" [ docsParagraph "Editable query text and the encoded hidden selection are distinct. Typing or clearing removes the submitted identity until a typed option is selected again; disabled or pending controls retain their visible state while omitting the hidden value from ordinary FormData." ]
            docsSection "remote-results" "Remote results and ordering" [ docsParagraph "Return Combobox.renderOptions from the stable popup region after filtering application-owned values. Remote requests explicitly use Datastar requestCancellation: 'auto', so a newer request to the same endpoint cancels an older in-flight request before its response can replace current results." ]
            docsSection "states" "Loading, empty, error, and validation" [ docsParagraph "Remote requests expose a busy loading status without moving focus. Empty results, retryable server-rendered errors, form validation, disabled state, and pending state remain visually and programmatically distinct." ]
            docsSection "accessibility" "Keyboard and focus" [ docsParagraph "DOM focus remains on the editable combobox while aria-activedescendant tracks the active option. Arrow keys, Home, End, Enter, Escape, pointer selection, clearing, disabled-option skipping, and repaired active identities remain available after remote updates." ] ]

    let checkboxPage =
        componentPage checkboxRegistration "Capture an independent checked or unchecked choice with a required accessible label." "checkbox" checkboxPreview [
            docsSection "when-to-use" "When to use Checkbox" [ docsParagraph "Use Checkbox for an independent form value that may be checked or unchecked. Use Switch for an immediate setting and Toggle button for a pressed action state." ]
            docsSection "forms" "Form and validation behavior" [ docsParagraph "The branded control retains native checkbox semantics, pointer and Space-key interaction, visible focus, required constraint behavior, description and validation relationships, and ordinary checked form submission. Unchecked, disabled, and pending checkboxes are omitted from FormData by the platform; the server remains authoritative. The example form uses novalidate deliberately so its real endpoint can demonstrate the server rejection while the control still exposes native required validity." ]
            docsSection "identity" "Stable instances" [ docsParagraph "Use withId when repeated controls intentionally share a form name. The stable ID isolates Datastar signals and accessible relationships while the shared name preserves the application’s submission contract." ] ]

    let switchPage =
        componentPage switchRegistration "Represent an immediate on/off setting with distinct switch semantics." "switch" switchPreview [
            docsSection "when-to-use" "When to use Switch" [ docsParagraph "Use Switch when changing the control immediately turns a setting on or off. Use Checkbox when the value belongs to a form that is submitted later." ]
            docsSection "accessibility" "Accessibility and state" [ docsParagraph "Switch retains a checkbox-backed role=switch, synchronized aria-checked state, pointer and Space-key operation, visible focus, a required accessible label, and description or server-validation relationships. Pending state is busy and unavailable without changing the visible setting label." ]
            docsSection "forms" "Submission" [ docsParagraph "When a Switch participates in a form, its checked true value uses native submission semantics. Unchecked, disabled, and pending switches are omitted; applications own immediate persistence and validation." ] ]

    let toggleButtonPage =
        componentPage toggleButtonRegistration "Represent whether an action button is currently pressed." "toggle-button" toggleButtonPreview [
            docsSection "when-to-use" "When to use a toggle button" [ docsParagraph "Use ToggleButton for an action state such as compact rows or pinned filters. Do not substitute it for Checkbox, Switch, or a Radio group when form-choice semantics are required." ]
            docsSection "accessibility" "Accessibility" [ docsParagraph "The visible label stays stable while aria-pressed communicates state. Pointer, Enter, and Space activation retain normal button behavior. Disabled and pending buttons prevent activation; pending also exposes aria-busy and a reduced-motion-safe loading indicator." ] ]

    let radioGroupPage =
        componentPage radioGroupRegistration "Choose exactly one submitted value from a labelled group of typed options." "radio-group" radioGroupPreview [
            docsSection "when-to-use" "When to use a radio group" [ docsParagraph "Use RadioGroup when all mutually exclusive options should remain visible. Use Select when the finite choice needs a more compact presentation." ]
            docsSection "forms" "Form and accessibility behavior" [ docsParagraph "The labelled radiogroup renders native radio inputs with one shared form name. Required state applies only while enabled; Arrow-key movement and form submission remain browser-native, disabled options are skipped, and applications explicitly encode and validate every submitted choice." ]
            docsSection "state" "Validation, pending, and patches" [ docsParagraph "Description and validation messages name the group state coherently. Disabled or pending groups retain visible selection while native inputs become unavailable and are omitted from FormData. Stable IDs keep repeated names isolated and allow server patches to replace authoritative selection without duplicate relationships. The example form uses novalidate deliberately so its endpoint demonstrates server rejection in addition to native required validity." ] ]

    let dropdownMenuPage =
        componentPage dropdownMenuRegistration "Present a compact set of application-owned actions and destinations." "dropdown-menu" dropdownMenuPreview [
            docsSection "items" "Menu items" [
                docsParagraph "MenuItem.link accepts a typed destination resolved by the application. Action expressions remain explicit trusted application code. Labelled groups and separators organize commands; leading content and shortcut hints remain presentation owned by the consumer. Popup alignment defaults to End and can be set to Start for triggers near the leading edge."
                docsParagraph "Disabled and pending items stay visible for context but are unavailable to pointer and keyboard activation. Pending items preserve their action name, expose busy state, and show a reduced-motion-safe loading indicator. Authorization and whether an action exists remain server-owned." ]
            docsSection "keyboard" "Keyboard and focus" [
                docsParagraph "Enter, Space, and Arrow keys open the menu. Arrow keys wrap among enabled items; Home and End move to the boundaries; bounded character-prefix navigation and repeated-character cycling skip disabled and pending items. Enter and Space activate the focused command."
                docsParagraph "Escape restores the trigger, Tab and outside interaction dismiss without trapping focus, and enabled pointer activation closes coherently. Stable menu IDs keep adjacent instances independent and preserve behavior when a server-rendered Datastar patch replaces the example region." ] ]

    let dialogPage =
        componentPage dialogRegistration "Connect a trigger and modal surface with initial focus, dismissal, and focus restoration." "dialog" dialogPreview [
            docsSection "composition" "Composition" [ docsParagraph "Dialog bodies and footers remain ordinary HtmlElement values. The required identifier and title connect the trigger, labelled dialog, close controls, and focus restoration target." ]
            docsSection "focus" "Focus behavior" [ docsParagraph "Choose an initial focus target deliberately. Escape, close controls, and native dialog close behavior dismiss the overlay and restore focus to its connected trigger." ] ]

    let collectionPageDocumentation =
        componentPage collectionRegistration "Compose a collection heading, description, actions, toolbar, and application-owned result content." "collection" collectionPreview [
            docsSection "ownership" "Application responsibilities" [ docsParagraph "The application retains query parsing, filters, sorting, pagination, authorization, empty/loading/error states, and result rendering. Collection supplies consistent page hierarchy and slots." ]
            docsSection "composition" "Composition" [ docsParagraph "Actions, toolbar controls, and results are ordinary HtmlElement values, so typed routes and branded controls remain application-owned." ] ]

    let detailPageDocumentation =
        componentPage detailRegistration "Compose a detail heading, metadata, actions, and custom content sections." "detail" detailPreview [
            docsSection "ownership" "Application responsibilities" [ docsParagraph "The application retains resource loading, authorization, formatting, validation, mutations, and destinations. Detail supplies consistent page hierarchy without imposing a domain model." ]
            docsSection "composition" "Composition" [ docsParagraph "Metadata, actions, and sections remain ordinary HtmlElement values and can contain other Components primitives." ] ]

    let appShellPage =
        componentPage appShellRegistration "Render a typed application shell with product identity, navigation, breadcrumbs, account actions, content, and one semantic theme." "app-shell" shellPreview [
            docsSection "typed-navigation" "Typed navigation" [ docsParagraph "AppShell keeps destinations generic. The application provides navigation items, the current destination, and one resolver from typed destinations to URLs." ]
            docsSection "theme" "Theme" [ docsParagraph "Apply ComponentsTheme once to coordinate semantic colors, radius, density, light mode, and dark mode across shell navigation and content." ]
            docsSection "responsibilities" "Application responsibilities" [ docsParagraph "Product routes, authorization, current-user behavior, responsive content, and durable state remain in the consuming application." ] ]

    let interactionPage =
        docsArticle interactionRegistration.id interactionRegistration.title "Keep ephemeral interaction local while applications retain authoritative, durable, and security-sensitive state." [
            docsSection "datastar" "Datastar interaction" [
                docsParagraph "Datastar is the Components interaction model. Sparse local signals hold ephemeral state such as whether a menu is open or which option is active. Selected form values and editable queries are submitted intentionally."
                docsParagraph "Treat Datastar expressions and endpoints as trusted application code and never interpolate untrusted content into executable expressions." ]
            docsSection "server" "Server-owned state" [
                docsParagraph "Applications continue to own authoritative options, routes, permissions, validation, persistence, actions, and error handling. Remote Combobox results return only the stable options morph region." ]
            docsSection "boundaries" "Application boundaries" [ docsBullets [ "Keep product routes, authorization, domain formatting, query behavior, and durable state in the application."; "Keep table querying, sorting, filtering, pagination, chart data, and drawing application-owned."; "Use Datastar rather than adding a parallel Alpine or client-side component runtime." ] ] ]

    let accessibilityPage =
        docsArticle accessibilityRegistration.id accessibilityRegistration.title "Understand the semantic, keyboard, focus, label, and customization guarantees shared by Components." [
            docsSection "semantics" "Distinct semantics" [ docsParagraph "Select, Combobox, Checkbox, Switch, ToggleButton, RadioGroup, DropdownMenu, Dialog, and AppShell navigation retain the roles and keyboard models appropriate to each interaction rather than sharing one generic choice control." ]
            docsSection "focus" "Focus and active options" [ docsParagraph "Select and Combobox keep DOM focus on the combobox while aria-activedescendant identifies the visually active option. Select typeahead buffers rapid characters for prefix matching and cycles options when the same character is repeated." ]
            docsSection "labels" "Required labels" [ docsParagraph "Accessible labels are required where visible content cannot provide them. Compact layouts use typed visually hidden labels rather than omitting the accessible name." ]
            docsSection "protected-attributes" "Protected behavior" [ docsParagraph "Package-owned structure, form attributes, ARIA relationships, Datastar bindings, and base classes cannot be replaced through generic customization. Interactive components support pointer and keyboard operation, visible focus, disabled and pending states, multiple instances, and representative morphs." ] ]

    let themingPage =
        docsArticle themingRegistration.id themingRegistration.title "Apply semantic color, radius, and density consistently across a Components subtree or AppShell." [
            docsSection "theme" "Apply a theme" [ docsParagraph "Components consume semantic variables for page, surface, text, border, brand, positive, warning, critical, and informative roles. Variants such as Primary and Positive select roles rather than palette shades."; docsCode "fsharp" themeExample ]
            docsSection "modes" "Light and dark modes" [ docsParagraph "Built-in themes coordinate default, selected, hover, and focus colors in light and dark modes. Radius and density settings apply consistently across controls and navigation." ]
            docsSection "brand" "Product branding" [ docsParagraph "Override documented semantic variables in an application theme when product branding requires it. Keep component APIs semantic rather than passing raw palette strings." ] ]

    let tailwindPage =
        docsArticle tailwindRegistration.id tailwindRegistration.title "Generate every package-owned utility from the explicit Tailwind v4 source manifest." [
            docsSection "manifest" "Package source manifest" [ docsParagraph "The NuGet package includes FSharp.ViewEngine.Components.tailwind.css under contentFiles/any/any. Copy it into the application CSS source tree and import it after Tailwind CSS."; docsCode "css" tailwindExample ]
            docsSection "source-detection" "Source detection" [ docsParagraph "Utility classes inside compiled assemblies are not discovered automatically. The explicit Tailwind v4 source manifest lists complete package-owned utility names so Tailwind can emit styles without assembly scanning, consumer call-site scanning, or dynamic class construction." ] ]

    let customizationPage =
        docsArticle customizationRegistration.id customizationRegistration.title "Extend presentation and application-owned slots without replacing component structure or behavior." [
            docsSection "escape-hatches" "Escape hatches" [ docsParagraph "Use withAttributes, withClass, and named HtmlElement slots where a component exposes them. Renderers retain structural, form, ARIA, Datastar, and base class attributes so customization cannot duplicate or remove required behavior." ]
            docsSection "application-inputs" "Application inputs" [ docsParagraph "Applications provide destination resolvers, form-value encoders, trusted Datastar expressions, custom cells, dialog bodies, toolbars, actions, and page content. Submitted values still require server validation." ]
            docsSection "native-controls" "Native controls" [ docsParagraph "Render browser-native controls directly with the FSharp.ViewEngine DSL when native presentation is intentional. There is no parallel NativeSelect API or separate component markup language." ] ]

    let versioningPage =
        docsArticle versioningRegistration.id versioningRegistration.title "Upgrade Components independently while honoring its declared minimum compatible Core version." [
            docsSection "independent" "Independent releases" [ docsParagraph "FSharp.ViewEngine.Components versions independently using Components-specific calendar versions and repository tags. Each release declares its minimum compatible FSharp.ViewEngine version." ]
            docsSection "compatibility" "Compatibility" [ docsParagraph "Additive modifiers and union cases receive compatibility review. Breaking API changes require a new Components version and migration guidance rather than compatibility wrappers in Core or Docs." ] ]

    let private pages =
        [ overviewRegistration.path, overviewPage
          installationRegistration.path, installationPage
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
          chartRegistration.path, chartPage
          selectRegistration.path, selectPage
          comboboxRegistration.path, comboboxPage
          checkboxRegistration.path, checkboxPage
          switchRegistration.path, switchPage
          toggleButtonRegistration.path, toggleButtonPage
          radioGroupRegistration.path, radioGroupPage
          dropdownMenuRegistration.path, dropdownMenuPage
          dialogRegistration.path, dialogPage
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
