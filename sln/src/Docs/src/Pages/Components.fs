namespace Docs.Pages

open System
open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html

module Components =
    type AccountStatus =
        | Active
        | Pending
        | Suspended
        | Scheduled

    type Destination =
        | Accounts
        | Account of int
        | Settings

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
        | Account id -> $"https://ledger.example.test/accounts/{id}"
        | Settings -> "https://ledger.example.test/settings"

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.Components.fs")

    let sourceFor id = SourceRegion.extract id sourceText.Value

    let private themedSurface (content:HtmlElement) =
        div {
            for attribute in ComponentsTheme.attributes ComponentsTheme.emerald do attribute
            div {
                _class "rounded-xl bg-[var(--fve-page)] p-5 text-[var(--fve-text)]"
                content
            }
        }

    // docs-example:start button-status
    let importButton =
        Button.create "Import"
        |> Button.withVariant ButtonVariant.Secondary
        |> Button.withSize ControlSize.Small
        |> Button.render

    let reviewStatus =
        Status.create "Needs review"
        |> Status.withTone Tone.Warning
        |> Status.render

    let buttonStatusPreview =
        themedSurface (
            div {
                _class "flex flex-wrap items-center gap-3"
                [ Button.primary "Create account"
                  importButton
                  Status.positive "Active"
                  reviewStatus ]
            })
    // docs-example:end button-status

    let private rows =
        [ { id = 101; name = "Operating"; status = Active; balance = 42800M }
          { id = 102; name = "Tax reserve"; status = Pending; balance = 12750M } ]

    // docs-example:start table
    let accountTable =
        Table.create "Accounts" [
            Table.column "Account" (fun row ->
                a { _href (destinationUrl (Account row.id)); _class "font-medium text-[var(--fve-brand-text)]"; row.name })
            Table.column "Status" (fun row ->
                match row.status with
                | Active -> Status.positive "Active"
                | Pending -> Status.warning "Pending"
                | Suspended -> Status.create "Suspended" |> Status.withTone Tone.Critical |> Status.render
                | Scheduled -> Status.create "Scheduled" |> Status.withTone Tone.Informative |> Status.render)
            Table.column "Balance" (fun row -> text $"${row.balance:N0}")
            |> Table.alignEnd
        ] rows
        |> Table.render
    // docs-example:end table

    let private statusOptions =
        [ Select.option Active "Active"
          Select.option Pending "Pending"
          Select.option Suspended "Suspended"
          Select.option Scheduled "Scheduled" ]

    // docs-example:start select-combobox
    let statusSelect =
        Select.create "status" "Status" statusValue statusOptions
        |> Select.withDescription "Controls whether the account can receive entries."
        |> Select.withSelected Active
        |> Select.render

    let private accounts = [ 101, "Operating"; 102, "Tax reserve" ]
    let private accountOptions values = values |> List.map (fun (value, label) -> Select.option value label)

    let private accountComboboxConfig =
        Combobox.create "account" "Parent account" string (accountOptions accounts)
        |> Combobox.withPlaceholder "Search accounts"
        |> Combobox.withSearch (ComboboxSearch.Remote "/components/accounts/search")

    let accountCombobox = accountComboboxConfig |> Combobox.render

    let accountComboboxOptions query =
        accounts
        |> List.filter (fun (_, label) -> String.IsNullOrWhiteSpace query || label.Contains(query, StringComparison.OrdinalIgnoreCase))
        |> accountOptions
        |> fun options -> accountComboboxConfig |> Combobox.withOptions options |> Combobox.renderOptions

    let choicePreview =
        themedSurface (
            div {
                _class "grid max-w-xl gap-5 sm:grid-cols-2"
                [ statusSelect; accountCombobox ]
            })
    // docs-example:end select-combobox

    // docs-example:start choice-controls
    let includeArchived =
        Checkbox.create "includeArchived" "Include archived accounts"
        |> Checkbox.withDescription "Archived accounts remain read-only."
        |> Checkbox.render

    let postingNotifications =
        Switch.create "postingNotifications" "Posting notifications"
        |> Switch.withDescription "Notify account owners after entries post."
        |> Switch.withChecked
        |> Switch.render

    let compactRows =
        ToggleButton.create "components-compact-rows" "Compact rows"
        |> ToggleButton.pressed
        |> ToggleButton.render

    let postingMode =
        RadioGroup.create "postingMode" "Posting mode" id [
            RadioGroup.option "automatic" "Automatic"
            RadioGroup.option "manual" "Manual review"
        ]
        |> RadioGroup.withDescription "Choose how approved entries reach the ledger."
        |> RadioGroup.withSelected "automatic"
        |> RadioGroup.render

    let choiceControlsPreview =
        themedSurface (
            div {
                _class "grid max-w-xl gap-6"
                div { _class "grid gap-4 sm:grid-cols-2"; [ includeArchived; postingNotifications ] }
                div { _class "flex flex-wrap items-start gap-6"; [ compactRows; postingMode ] }
            })
    // docs-example:end choice-controls

    let private accountMenuItems =
        [ MenuItem.link Settings "Account settings"
          MenuItem.separator
          MenuItem.destructiveAction "@delete('/accounts/101')" "Delete account" ]

    // docs-example:start menu-dialog
    let actionMenu =
        DropdownMenu.create "components-menu-actions" "Actions" accountMenuItems
        |> DropdownMenu.render destinationUrl

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
    // docs-example:end menu-dialog

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

    // docs-example:start collection-detail
    let collectionPage =
        Collection.create "Accounts" accountTable
        |> Collection.withDescription "Review balances and posting availability."
        |> Collection.withActions (Button.primary "New account")
        |> Collection.withToolbar toolbar
        |> Collection.render

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
    // docs-example:end collection-detail

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
            ComponentsTheme.emerald
            |> ComponentsTheme.withRadius Radius.Large
            |> ComponentsTheme.withDensity Density.Compact)
        |> AppShell.render destinationUrl
    // docs-example:end app-shell

    let private section id title =
        [ Heading { id = id; title = title; level = 2 } ]

    let private example id title description preview =
        [ Heading { id = id; title = title; level = 3 }
          Paragraph [ Text description ]
          Example($"components-{id}", title, "fsharp", sourceFor id, preview) ]

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

    let private menuDialogPreview =
        themedSurface (div { _class "flex items-center gap-3"; [ actionMenu; reviewDialogTrigger; reviewDialog ] })

    let private collectionDetailPreview =
        themedSurface (div { _class "grid gap-8"; [ collectionPage; detailPage ] })

    let private nodes =
        [ [ Paragraph [ Text "FSharp.ViewEngine.Components provides accessible, server-rendered components with semantic Tailwind styling and Datastar interaction. Components are ordinary F# values and functions that compose with the existing HtmlElement builders." ] ];

          section "using-components" "Using Components";
          [ Paragraph [ Text "Required semantic inputs are constructor arguments, while optional presentation and behavior are added through immutable pipeline functions." ]
            UnorderedList [
                [ Strong [ Text "Required inputs stay visible." ]; Text " Form controls require their name and accessible label; AppShell requires product identity, navigation, current destination, and content." ]
                [ Strong [ Text "Optional behavior is piped." ]; Text " Functions such as withSelected, withTheme, and withAttributes return updated configuration." ]
                [ Strong [ Text "Common cases stay concise." ]; Text " Helpers such as Button.primary and Status.positive render the ordinary case directly." ]
                [ Strong [ Text "Custom content stays HTML." ]; Text " Cells, dialog bodies, toolbars, actions, and page content remain ordinary HtmlElement values." ]
                [ Strong [ Text "Closed choices are typed." ]; Text " Variants, tones, sizes, density, radius, selected values, and destinations use discriminated unions or generic values rather than visual strings." ] ] ];

          section "actions-feedback" "Actions and feedback";
          example "button-status" "Button and status" "Use concise helpers for common actions and statuses, or pipe typed configuration for variants and sizes." buttonStatusPreview;

          section "data-display" "Data display";
          example "table" "Table" "Define typed columns over application-owned row data while Table supplies semantic structure and shared presentation." (themedSurface accountTable);

          section "form-controls" "Form controls";
          example "select-combobox" "Select and Combobox" "Use Select for a finite non-editable choice and Combobox when people need to search or enter a query before selecting a submitted value." choicePreview;
          example "choice-controls" "Checkbox, Switch, ToggleButton, and RadioGroup" "Choose the control whose checked, on/off, pressed, or grouped-choice semantics match the interaction." choiceControlsPreview;

          section "menus-overlays" "Menus and overlays";
          example "menu-dialog" "Dropdown menu and dialog" "DropdownMenu presents actions or destinations. Dialog connects its trigger, initial focus, close actions, Escape behavior, and focus restoration." menuDialogPreview;

          section "compositions" "Compositions";
          example "collection-detail" "Collection and detail" "Arrange shared primitives into collection and detail pages while the application retains queries, formatting, routes, and authorization." collectionDetailPreview;
          example "app-shell" "Application shell" "Keep destinations typed, resolve their URLs in the application, and apply one semantic theme to navigation and content." shellPreview;

          section "state-ownership" "Interaction and server state";
          [ Paragraph [ Text "Datastar is the component interaction model. Local signals hold ephemeral state such as whether a menu is open, while selected form values and editable queries are submitted intentionally. Applications continue to own authoritative options, routes, permissions, validation, persistence, and actions." ]
            Paragraph [ Text "Remote Combobox results use a stable listbox region: filter application-owned values on the server, apply them with withOptions, and return Combobox.renderOptions. The input, submitted selection, and active-descendant relationship remain stable while Datastar morphs the options." ]
            Paragraph [ Text "Treat Datastar expressions and endpoints as trusted application code and never interpolate untrusted content into executable expressions." ] ];

          section "accessibility" "Accessibility";
          [ Paragraph [ Text "Branded controls own their visible presentation while preserving the semantics appropriate to each interaction. Select uses a select-only combobox and listbox; Checkbox, Switch, ToggleButton, RadioGroup, DropdownMenu, Dialog, and AppShell navigation retain their distinct roles and keyboard behavior." ]
            Paragraph [ Text "Select and Combobox keep DOM focus on the combobox while aria-activedescendant identifies the visually active option. Select typeahead buffers rapid characters for prefix matching and cycles options when the same character is repeated." ]
            Paragraph [ Text "Accessible labels are required where visible content cannot provide them. Package-owned structure, form attributes, ARIA relationships, and Datastar bindings cannot be replaced through generic attribute customization." ]
            Paragraph [ Text "Interactive components support pointer and keyboard operation, visible focus, disabled and pending states, multiple instances, and stable behavior after representative Datastar morphs." ] ];

          section "theming" "Theming and density";
          [ Paragraph [ Text "Apply ComponentsTheme once to a component subtree or AppShell. Components consume semantic CSS variables for page, surface, text, border, brand, positive, warning, critical, and informative roles. Variants such as Primary and Positive select semantic roles rather than raw palette shades." ]
            CodeBlock("fsharp", themeExample)
            Paragraph [ Text "Built-in themes provide coordinated light and dark colors for default, selected, hover, and focus states. Radius and density settings apply consistently across controls and navigation. Override documented semantic variables in an application theme when product branding requires it." ] ];

          section "tailwind" "Tailwind CSS setup";
          [ Paragraph [ Text "Import the Components stylesheet after Tailwind CSS. The stylesheet includes semantic variables and an explicit Tailwind v4 source manifest because utility classes inside compiled assemblies are not discovered automatically." ]
            CodeBlock("css", tailwindExample)
            Paragraph [ Text "The manifest lists complete package-owned utility names, so Tailwind can emit component styles without scanning application call sites or constructing classes from consumer strings." ] ];

          section "customization" "Customization";
          [ Paragraph [ Text "Use withAttributes, withClass, and named HtmlElement slots where a component exposes them. Renderers retain ownership of structural, form, ARIA, Datastar, and base class attributes so customization cannot duplicate or remove required behavior." ]
            Paragraph [ Text "Applications provide destination resolvers and form-value encoders. Submitted values still require server validation, and trusted Datastar expressions remain explicit edge inputs." ] ];

          section "versioning" "Versioning";
          [ Paragraph [ Text "FSharp.ViewEngine.Components versions independently using Components-specific calendar versions and repository tags. Each release declares its minimum compatible FSharp.ViewEngine version." ]
            Paragraph [ Text "Additive modifiers and union cases receive compatibility review. Breaking API changes require a new Components version and migration guidance rather than compatibility wrappers in Core or Docs." ] ];

          section "application-responsibilities" "Application responsibilities";
          [ UnorderedList [
                [ Text "Render browser-native controls directly with the FSharp.ViewEngine DSL when native presentation is intentional." ]
                [ Text "Keep product routes, authorization, domain formatting, query behavior, and durable state in the application." ]
                [ Text "Keep table querying, sorting, filtering, pagination, chart data, and drawing behavior application-owned." ]
                [ Text "Use Datastar for component interaction rather than adding a parallel Alpine or client-side component runtime." ]
                [ Text "Compose custom content with HtmlElement values instead of introducing a separate component markup language." ] ] ] ]
        |> List.concat

    let page =
        { id = "components-overview"
          path = "/components"
          aliases = []
          navLabel = "Overview"
          category = "FSharp.ViewEngine.Components"
          title = "Components"
          browserTitle = "Components - FSharp.ViewEngine"
          nodes = nodes }
