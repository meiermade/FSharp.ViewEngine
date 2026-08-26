namespace Docs.Pages

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
          Select.option Pending "Pending" |> Select.describe "Requires review before activation"
          Select.option Suspended "Suspended" ]

    // docs-example:start select-combobox
    let statusSelect =
        Select.create "status" "Status" statusValue statusOptions
        |> Select.withDescription "Controls whether the account can receive entries."
        |> Select.withSelected Active
        |> Select.render

    let accountCombobox =
        Combobox.create "account" "Parent account" string [
            Select.option 101 "Operating"
            Select.option 102 "Tax reserve"
        ]
        |> Combobox.withPlaceholder "Search accounts"
        |> Combobox.withSearch (ComboboxSearch.Remote "/accounts/search")
        |> Combobox.render

    let choicePreview =
        themedSurface (
            div {
                _class "grid max-w-xl gap-5 sm:grid-cols-2"
                [ statusSelect; accountCombobox ]
            })
    // docs-example:end select-combobox

    let private accountMenuItems =
        [ MenuItem.link Settings "Account settings"
          MenuItem.separator
          MenuItem.destructiveAction "@delete('/accounts/101')" "Delete account" ]

    // docs-example:start menu-dialog
    let actionMenu =
        DropdownMenu.create "contract-menu-actions" "Actions" accountMenuItems
        |> DropdownMenu.render destinationUrl

    let deleteButton =
        Button.create "Delete"
        |> Button.withVariant ButtonVariant.Destructive
        |> Button.render

    let deleteDialog =
        Dialog.create "delete-account" "Delete account" (
            p { "This permanently removes Operating and its imported entries." })
        |> Dialog.withDescription "This action cannot be undone."
        |> Dialog.withFooter (
            div {
                _class "flex gap-3"
                [ Button.secondary "Cancel"; deleteButton ]
            })
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
        themedSurface (div { _class "flex items-center gap-3"; [ actionMenu; Button.secondary "Review dialog contract"; deleteDialog ] })

    let private collectionDetailPreview =
        themedSurface (div { _class "grid gap-8"; [ collectionPage; detailPage ] })

    let private nodes =
        [ [ Paragraph [
                Strong [ Text "Pre-release contract." ]
                Text " This executable page defines the intended public surface of FSharp.ViewEngine.Components before the package is published. The compiled examples are the contract input for implementation; they are not claims that the production components already exist." ] ];

          section "principles" "API Principles";
          [ Paragraph [ Text "Components is an opinionated FSharp.ViewEngine library for semantic server-rendered HTML, Tailwind v4 styling, and Datastar interaction. It uses ordinary F# values and functions rather than introducing another markup language." ]
            UnorderedList [
                [ Strong [ Text "Required inputs are constructor arguments." ]; Text " A Select cannot exist without its form name, accessible label, value encoder, and options; AppShell cannot exist without product identity, current destination, navigation, and content." ]
                [ Strong [ Text "Optional behavior is piped." ]; Text " Functions such as withLabel, withSelected, withTheme, and withAttributes return updated immutable configuration." ]
                [ Strong [ Text "Common cases are short." ]; Text " Convenience functions such as Button.primary and Status.positive render the ordinary case directly." ]
                [ Strong [ Text "Custom content stays HTML." ]; Text " Leading content, cells, dialog bodies, toolbars, actions, and page content use HtmlElement instead of component-specific child DSLs." ]
                [ Strong [ Text "Closed choices are typed." ]; Text " Variants, tones, sizes, density, radius, selected values, and destinations use discriminated unions or generic values rather than arbitrary visual strings." ] ] ];

          section "call-sites" "Compiled Call Sites";
          example "button-status" "Convenience and configuration" "Common actions and statuses use concise helpers; typed configuration handles variants and size without a custom computation expression." buttonStatusPreview;
          example "table" "Typed table" "Columns render consumer-owned row data and destinations while the component owns semantic table structure and shared presentation." (themedSurface accountTable);
          example "select-combobox" "Select and Combobox" "Select models a finite non-editable choice. Combobox separately models an editable search and an application-owned remote endpoint." choicePreview;
          example "menu-dialog" "Menu and dialog" "Menu destinations and trusted Datastar actions remain consumer inputs. Dialog bodies and footers remain ordinary HtmlElement slots." menuDialogPreview;
          example "collection-detail" "Collection and detail compositions" "Page compositions arrange package primitives without taking ownership of queries, domain formatting, routes, or authorization." collectionDetailPreview;
          example "app-shell" "Typed application shell" "The advanced shell call site retains a destination type and resolver, accepts product-owned navigation and branding, and applies one semantic theme at the root." shellPreview;

          section "state-ownership" "Datastar and State Ownership";
          [ Paragraph [ Text "Datastar is the sole component interaction model. Component-local signals represent ephemeral interaction such as open state, active option, and query text. Bound form values are intentionally submitted. Authoritative options, routes, permissions, queries, validation, persistence, and actions remain server-owned." ]
            Paragraph [ Text "Interactive renderers SHALL generate stable instance IDs and local signal names, preserve focus through representative element patches, and document the morph region expected from remote actions. Applications SHALL treat Datastar expressions and endpoints as trusted application code rather than interpolate untrusted input." ] ];

          section "accessibility" "Accessibility Contract";
          [ Paragraph [ Text "Semantic native elements are the starting point. Select, Combobox, DropdownMenu, Dialog, and AppShell navigation each retain distinct roles and keyboard contracts. Public APIs SHALL require accessible labels where visible content cannot supply them and SHALL prevent escape-hatch attributes from silently removing required semantics." ]
            Paragraph [ Text "Each interactive implementation requires browser evidence for pointer and keyboard behavior, accessible names and relationships, focus entry/restoration, disabled and pending states, multiple instances, and representative post-morph behavior." ] ];

          section "theming" "Semantic Theming";
          [ Paragraph [ Text "A ComponentsTheme is applied once at a component subtree or AppShell. Components consume semantic CSS variables for page, surface, text, border, brand, positive, warning, critical, and informative roles. A component variant such as Primary or Positive selects a role; it does not accept a raw palette shade such as emerald-600." ]
            CodeBlock("fsharp", themeExample)
            Paragraph [ Text "The package supplies coherent light and dark defaults. Consumers may override documented semantic variables in their own theme class while retaining the component state model." ] ];

          section "tailwind" "Tailwind v4 Distribution";
          [ Paragraph [ Text "Compiled assemblies cannot be discovered as Tailwind source. Components therefore ships an importable Tailwind v4 CSS contract containing semantic variables and an explicit @source inline() manifest of complete package-owned class names. It never constructs utility names from caller strings." ]
            CodeBlock("css", tailwindExample)
            Paragraph [ Text "The contract fixture builds this import from an otherwise clean consumer and asserts both package utilities and a consumer brand override are emitted. The package-spine task will determine the final NuGet content path and copy behavior without changing this consumer contract." ] ];

          section "escape-hatches" "Slots and Escape Hatches";
          [ Paragraph [ Text "Components may expose withAttributes, withClass, and named HtmlElement slots where consumers have a demonstrated composition need. Package-owned classes and required ARIA/Datastar attributes remain authoritative. Raw class additions are appended rather than used as a replacement theme API." ]
            Paragraph [ Text "Typed destinations are resolved by an application-supplied function. Form values are encoded by an application-supplied function and validated again on the server. Trusted Datastar expressions are explicit edge inputs; arbitrary user content is never accepted as executable component configuration." ] ];

          section "compatibility" "Versioning and Compatibility";
          [ Paragraph [ Text "FSharp.ViewEngine.Components versions independently using Components-specific calendar versions and repository tags. Each release declares a minimum compatible FSharp.ViewEngine version. Components does not depend on FSharp.ViewEngine.Docs; the documentation application references both packages to host examples." ]
            Paragraph [ Text "The first public release establishes the package-validation baseline. Additive modifiers and union cases require deliberate compatibility review; breaking public-contract changes require a new Components version and migration guidance rather than compatibility wrappers in Core or Docs." ] ];

          section "non-goals" "Non-goals";
          [ UnorderedList [
                [ Text "No Alpine component implementation, adapter, or parallel interaction runtime." ]
                [ Text "No Tailwind Plus Elements dependency and no redistribution of commercial Tailwind Plus source." ]
                [ Text "No generic client-side data-table or chart engine." ]
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
