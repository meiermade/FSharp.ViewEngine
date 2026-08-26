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
                | Suspended -> Status.create "Suspended" |> Status.withTone Tone.Critical |> Status.render)
            Table.column "Balance" (fun row -> text $"${row.balance:N0}")
            |> Table.alignEnd
        ] rows
        |> Table.render
    // docs-example:end table

    let private statusOptions =
        [ Select.option Active "Active"
          Select.option Pending "Pending"
          Select.option Suspended "Suspended" ]

    // docs-example:start select-combobox
    let statusSelect =
        Select.create "status" "Status" statusValue statusOptions
        |> Select.withDescription "Controls whether the account can receive entries."
        |> Select.withSelected Active
        |> Select.render

    let private accounts = [ 101, "Operating"; 102, "Tax reserve" ]
    let private accountOptions values = values |> List.map (fun (value, label) -> Select.option value label)

    let private accountComboboxContract =
        Combobox.create "account" "Parent account" string (accountOptions accounts)
        |> Combobox.withPlaceholder "Search accounts"
        |> Combobox.withSearch (ComboboxSearch.Remote "/components/contract/accounts/search")

    let accountCombobox = accountComboboxContract |> Combobox.render

    let accountComboboxOptions query =
        accounts
        |> List.filter (fun (_, label) -> String.IsNullOrWhiteSpace query || label.Contains(query, StringComparison.OrdinalIgnoreCase))
        |> accountOptions
        |> fun options -> accountComboboxContract |> Combobox.withOptions options |> Combobox.renderOptions

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
        ToggleButton.create "contract-compact-rows" "Compact rows"
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
        DropdownMenu.create "contract-menu-actions" "Actions" accountMenuItems
        |> DropdownMenu.render destinationUrl

    let dialogContract =
        Dialog.create "review-contract-dialog" "Review dialog contract" (
            p { "Native dialog semantics provide modal focus containment and Escape behavior." })
        |> Dialog.withDescription "The component connects opening, initial focus, closing, and focus restoration."
        |> Dialog.withInitialFocus "review-contract-dialog-close"

    let reviewDialogTrigger =
        dialogContract
        |> Dialog.trigger "Review dialog contract"

    let reviewDialog =
        dialogContract
        |> Dialog.withFooter (dialogContract |> Dialog.closeButton "Close")
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
        DropdownMenu.create "contract-detail-actions" "Actions" accountMenuItems
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
        DropdownMenu.create "contract-shell-actions" "Account" accountMenuItems
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
        [ [ Paragraph [
                Strong [ Text "Pre-release contract." ]
                Text " This executable page defines the intended public surface of FSharp.ViewEngine.Components before the package is published. The compiled examples are the contract input for implementation; they are not claims that the production components already exist." ] ];

          section "principles" "API Principles";
          [ Paragraph [ Text "Components is an opinionated FSharp.ViewEngine library for semantic server-rendered HTML, Tailwind v4 styling, and Datastar interaction. It uses ordinary F# values and functions rather than introducing another markup language." ]
            UnorderedList [
                [ Strong [ Text "Required inputs are constructor arguments." ]; Text " A Select, Combobox, Checkbox, Switch, or RadioGroup cannot exist without the name and accessible labeling needed for its semantics; AppShell cannot exist without product identity, current destination, navigation, and content." ]
                [ Strong [ Text "Optional behavior is piped." ]; Text " Functions such as withLabel, withSelected, withTheme, and withAttributes return updated immutable configuration." ]
                [ Strong [ Text "Common cases are short." ]; Text " Convenience functions such as Button.primary and Status.positive render the ordinary case directly." ]
                [ Strong [ Text "Custom content stays HTML." ]; Text " Leading content, cells, dialog bodies, toolbars, actions, and page content use HtmlElement instead of component-specific child DSLs." ]
                [ Strong [ Text "Closed choices are typed." ]; Text " Variants, tones, sizes, density, radius, selected values, and destinations use discriminated unions or generic values rather than arbitrary visual strings." ] ] ];

          section "call-sites" "Compiled Call Sites";
          example "button-status" "Convenience and configuration" "Common actions and statuses use concise helpers; typed configuration handles variants and size without a custom computation expression." buttonStatusPreview;
          example "table" "Typed table" "Columns render consumer-owned row data and destinations while the component owns semantic table structure and shared presentation." (themedSurface accountTable);
          example "select-combobox" "Select and Combobox" "Select is a branded finite-choice trigger and listbox. Combobox separately owns editable query text, a submitted selected value, and an application-owned remote endpoint." choicePreview;
          example "choice-controls" "Checkbox, Switch, ToggleButton, and RadioGroup" "Distinct typed controls preserve their own form and accessibility semantics while the component family owns visible presentation." choiceControlsPreview;
          example "menu-dialog" "Menu and dialog" "Menu destinations and trusted Datastar actions remain consumer inputs. Dialog helpers connect the trigger, initial focus, close action, Escape behavior, and focus restoration while bodies and footers remain ordinary HtmlElement slots." menuDialogPreview;
          example "collection-detail" "Collection and detail compositions" "Page compositions arrange package primitives without taking ownership of queries, domain formatting, routes, or authorization." collectionDetailPreview;
          example "app-shell" "Typed application shell" "The advanced shell call site retains a destination type and resolver, accepts product-owned navigation and branding, and applies one semantic theme at the root." shellPreview;

          section "state-ownership" "Datastar and State Ownership";
          [ Paragraph [ Text "Datastar is the sole component interaction model. Component-local signals represent ephemeral interaction such as open state and temporary pressed state. Select and Combobox keep displayed labels/query text distinct from their intentionally submitted selected values; a remote Combobox query is also intentionally submitted to its application endpoint. Authoritative options, routes, permissions, validation, persistence, and actions remain server-owned." ]
            Paragraph [ Text "Interactive renderers SHALL generate stable instance and option IDs, preserve focus through representative element patches, and document the morph region expected from remote actions. A remote Combobox endpoint filters application-owned values, applies them with withOptions, and returns Combobox.renderOptions so Datastar morphs the stable listbox region without replacing the input or submitted value." ]
            Paragraph [ Text "Applications SHALL treat Datastar expressions and endpoints as trusted application code rather than interpolate untrusted input." ] ];

          section "accessibility" "Accessibility Contract";
          [ Paragraph [ Text "Native semantics are welcome inside a component when they improve forms and accessibility, but public branded controls own their visible presentation and interaction. Select renders a select-only combobox/listbox rather than a native select; Combobox, Checkbox, Switch, ToggleButton, RadioGroup, DropdownMenu, Dialog, and AppShell navigation each retain distinct semantic and keyboard contracts." ]
            Paragraph [ Text "The package exposes no NativeSelect API because consumers can render an ordinary select directly with the existing FSharp.ViewEngine DSL. Public APIs SHALL require accessible labels where visible content cannot supply them and SHALL prevent escape-hatch attributes from silently removing required semantics." ]
            Paragraph [ Text "Each interactive implementation requires browser evidence for pointer and keyboard behavior, accessible names and relationships, focus entry/restoration, disabled and pending states, multiple instances, and representative post-morph behavior." ] ];

          section "theming" "Semantic Theming";
          [ Paragraph [ Text "A ComponentsTheme is applied once at a component subtree or AppShell. Components consume semantic CSS variables for page, surface, text, border, brand, positive, warning, critical, and informative roles. A component variant such as Primary or Positive selects a role; it does not accept a raw palette shade such as emerald-600." ]
            CodeBlock("fsharp", themeExample)
            Paragraph [ Text "The package supplies coherent theme-specific light and dark roles for default, selected, hover, and focus states. Density changes shared control block padding across controls and navigation rather than existing as an unused token. Consumers may override documented semantic variables in their own theme class while retaining the component state model." ] ];

          section "tailwind" "Tailwind v4 Distribution";
          [ Paragraph [ Text "Compiled assemblies cannot be discovered as Tailwind source. Components therefore ships an importable Tailwind v4 CSS contract containing semantic variables and an explicit @source inline() manifest of complete package-owned class names. It never constructs utility names from caller strings." ]
            CodeBlock("css", tailwindExample)
            Paragraph [ Text "The contract fixture builds this import from an otherwise clean consumer and asserts both package utilities and a consumer brand override are emitted. The package-spine task will determine the final NuGet content path and copy behavior without changing this consumer contract." ] ];

          section "escape-hatches" "Slots and Escape Hatches";
          [ Paragraph [ Text "Components may expose withAttributes, withClass, and named HtmlElement slots where consumers have a demonstrated composition need. Each renderer filters its package-owned class, structural, form, ARIA, and Datastar attribute names case-insensitively before rendering escape-hatch attributes, so required semantics cannot be replaced or duplicated. Raw class additions use an explicit withClass modifier where supported rather than replacing the semantic theme API." ]
            Paragraph [ Text "Typed destinations are resolved by an application-supplied function. Form values are encoded by an application-supplied function and validated again on the server. Trusted Datastar expressions are explicit edge inputs; arbitrary user content is never accepted as executable component configuration." ] ];

          section "compatibility" "Versioning and Compatibility";
          [ Paragraph [ Text "FSharp.ViewEngine.Components versions independently using Components-specific calendar versions and repository tags. Each release declares a minimum compatible FSharp.ViewEngine version. Components does not depend on FSharp.ViewEngine.Docs; the documentation application references both packages to host examples." ]
            Paragraph [ Text "The first public release establishes the package-validation baseline. Additive modifiers and union cases require deliberate compatibility review; breaking public-contract changes require a new Components version and migration guidance rather than compatibility wrappers in Core or Docs." ] ];

          section "non-goals" "Non-goals";
          [ UnorderedList [
                [ Text "No Alpine component implementation, adapter, or parallel interaction runtime." ]
                [ Text "No Tailwind Plus Elements dependency and no redistribution of commercial Tailwind Plus source." ]
                [ Text "No generic client-side data-table or chart engine." ]
                [ Text "No NativeSelect API; consumers use the existing FSharp.ViewEngine DSL when browser-native select presentation is intentional." ]
                [ Text "No product routes, authorization rules, domain formatting, query behavior, or durable state in Components." ]
                [ Text "No custom component computation-expression DSL in the initial API." ] ] ] ]
        |> List.concat

    let page =
        { id = "components-contract"
          path = "/components/contract"
          aliases = []
          navLabel = "Contract"
          category = "FSharp.ViewEngine.Components"
          title = "Components contract"
          browserTitle = "Components contract - FSharp.ViewEngine"
          nodes = nodes }
