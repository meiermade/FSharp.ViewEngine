namespace Docs.Pages

open System
open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Docs
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

    // docs-example:start button
    let importButton =
        Button.create "Import"
        |> Button.withVariant ButtonVariant.Secondary
        |> Button.withSize ControlSize.Small
        |> Button.render

    let buttonPreview =
        themedSurface (
            div {
                _class "flex flex-wrap items-center gap-3"
                [ Button.primary "Create account"; importButton ]
            })
    // docs-example:end button

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

    // docs-example:start select
    let statusSelect =
        Select.create "status" "Status" statusValue statusOptions
        |> Select.withDescription "Controls whether the account can receive entries."
        |> Select.withSelected Active
        |> Select.render

    let selectPreview = themedSurface (div { _class "max-w-sm"; statusSelect })
    // docs-example:end select

    // docs-example:start combobox
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

    let comboboxPreview = themedSurface (div { _class "max-w-sm"; accountCombobox })
    // docs-example:end combobox

    // docs-example:start checkbox
    let includeArchived =
        Checkbox.create "includeArchived" "Include archived accounts"
        |> Checkbox.withDescription "Archived accounts remain read-only."
        |> Checkbox.render

    let checkboxPreview = themedSurface includeArchived
    // docs-example:end checkbox

    // docs-example:start switch
    let postingNotifications =
        Switch.create "postingNotifications" "Posting notifications"
        |> Switch.withDescription "Notify account owners after entries post."
        |> Switch.withChecked
        |> Switch.render

    let switchPreview = themedSurface postingNotifications
    // docs-example:end switch

    // docs-example:start toggle-button
    let compactRows =
        ToggleButton.create "components-compact-rows" "Compact rows"
        |> ToggleButton.pressed
        |> ToggleButton.render

    let toggleButtonPreview = themedSurface compactRows
    // docs-example:end toggle-button

    // docs-example:start radio-group
    let postingMode =
        RadioGroup.create "postingMode" "Posting mode" id [
            RadioGroup.option "automatic" "Automatic"
            RadioGroup.option "manual" "Manual review"
        ]
        |> RadioGroup.withDescription "Choose how approved entries reach the ledger."
        |> RadioGroup.withSelected "automatic"
        |> RadioGroup.render

    let radioGroupPreview = themedSurface postingMode
    // docs-example:end radio-group

    let private accountMenuItems =
        [ MenuItem.link Settings "Account settings"
          MenuItem.separator
          MenuItem.destructiveAction "@delete('/accounts/101')" "Delete account" ]

    // docs-example:start dropdown-menu
    let actionMenu =
        DropdownMenu.create "components-menu-actions" "Actions" accountMenuItems
        |> DropdownMenu.render destinationUrl

    let dropdownMenuPreview = themedSurface actionMenu
    // docs-example:end dropdown-menu

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
            ComponentsTheme.emerald
            |> ComponentsTheme.withRadius Radius.Large
            |> ComponentsTheme.withDensity Density.Compact)
        |> AppShell.render destinationUrl
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
    let statusRegistration = registration "components-status" "/components/status" "Status" "Status"
    let tableRegistration = registration "components-table" "/components/table" "Table" "Table"
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

    let actionRegistrations = [ buttonRegistration; statusRegistration ]
    let dataDisplayRegistrations = [ tableRegistration ]
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
            catalogLink "/components/button" "ACTIONS" "Button" "Primary, secondary, destructive, disabled, pending, and sized actions."
            catalogLink "/components/status" "FEEDBACK" "Status" "Compact semantic state with accessible text and restrained color."
            catalogLink "/components/table" "DATA DISPLAY" "Table" "Typed columns over application-owned row data."
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
                docsBullets [ "Primary identifies the single leading action in a region."; "Secondary and destructive variants express hierarchy or consequence without raw palette names."; "Disabled and pending states prevent interaction while retaining an accessible label." ] ]
            docsSection "accessibility" "Accessibility" [ docsParagraph "Supply action text that describes the result. Button preserves keyboard activation and visible focus, and pending content does not remove the accessible name." ] ]

    let statusPage =
        componentPage statusRegistration "Present compact semantic state with accessible text and restrained color." "status" statusPreview [
            docsSection "usage" "Usage" [ docsParagraph "Use concise helpers for common tones or pipe Status.withTone when the state is application-specific. Status communicates meaning through text as well as semantic color." ]
            docsSection "semantics" "Choosing a tone" [ docsBullets [ "Positive confirms a successful or healthy state."; "Warning identifies a state that needs attention."; "Critical communicates failure or risk."; "Informative provides neutral operational context." ] ] ]

    let tablePage =
        componentPage tableRegistration "Define typed columns over application-owned rows while Table supplies semantic structure and shared presentation." "table" (themedSurface accountTable) [
            docsSection "ownership" "Application-owned data" [ docsParagraph "The application owns querying, sorting, filtering, pagination, formatting, destinations, and authorization. Table owns the caption, header and row structure, alignment, and responsive presentation." ]
            docsSection "accessibility" "Accessibility" [ docsParagraph "The required caption gives the table an accessible name. Column headers remain semantic, and application-provided cell content stays ordinary HtmlElement markup." ] ]

    let selectPage =
        componentPage selectRegistration "Choose one value from a finite, non-editable set with branded select-only combobox behavior." "select" selectPreview [
            docsSection "when-to-use" "When to use Select" [ docsParagraph "Use Select when every available option can be known before interaction. Use Combobox when people need an editable query or remote filtering." ]
            docsSection "keyboard" "Keyboard behavior" [ docsParagraph "DOM focus stays on the trigger while aria-activedescendant identifies the active option. Arrow keys, Home, End, Enter, Escape, bounded multi-character typeahead, and repeated-character cycling follow the select-only combobox model." ]
            docsSection "forms" "Form submission" [ docsParagraph "The selected typed value is encoded into a hidden form field. Applications must validate every submitted value on the server." ] ]

    let comboboxPage =
        componentPage comboboxRegistration "Search local or server-owned options while keeping editable query text separate from submitted selection identity." "combobox" comboboxPreview [
            docsSection "when-to-use" "When to use Combobox" [ docsParagraph "Use Combobox when people need to type before selecting. Static search filters supplied options locally; remote search lets the application return authoritative options from an endpoint." ]
            docsSection "remote-results" "Remote results" [ docsParagraph "Return Combobox.renderOptions from a stable listbox region after filtering application-owned values. The editable input, hidden selection, and active-descendant relationship survive representative Datastar morphs." ]
            docsSection "accessibility" "Keyboard and focus" [ docsParagraph "DOM focus remains on the editable combobox while aria-activedescendant tracks the active option. Arrow keys, Home, End, Enter, Escape, pointer selection, empty results, and repaired active identities remain available after remote updates." ] ]

    let checkboxPage =
        componentPage checkboxRegistration "Capture an independent checked or unchecked choice with a required accessible label." "checkbox" checkboxPreview [
            docsSection "when-to-use" "When to use Checkbox" [ docsParagraph "Use Checkbox for an independent form value that may be checked or unchecked. Use Switch for an immediate setting and Toggle button for a pressed action state." ]
            docsSection "forms" "Form behavior" [ docsParagraph "The branded control retains checkbox semantics, pointer and Space-key interaction, visible focus, and ordinary checked form submission." ] ]

    let switchPage =
        componentPage switchRegistration "Represent an immediate on/off setting with distinct switch semantics." "switch" switchPreview [
            docsSection "when-to-use" "When to use Switch" [ docsParagraph "Use Switch when changing the control immediately turns a setting on or off. Use Checkbox when the value belongs to a form that is submitted later." ]
            docsSection "accessibility" "Accessibility" [ docsParagraph "Switch retains role=switch, aria-checked, pointer and Space-key operation, visible focus, and a required accessible label." ] ]

    let toggleButtonPage =
        componentPage toggleButtonRegistration "Represent whether an action button is currently pressed." "toggle-button" toggleButtonPreview [
            docsSection "when-to-use" "When to use a toggle button" [ docsParagraph "Use ToggleButton for an action state such as compact rows or pinned filters. Do not substitute it for Checkbox, Switch, or a Radio group when form-choice semantics are required." ]
            docsSection "accessibility" "Accessibility" [ docsParagraph "The visible label stays stable while aria-pressed communicates state. Pointer, Enter, and Space activation retain normal button behavior." ] ]

    let radioGroupPage =
        componentPage radioGroupRegistration "Choose exactly one submitted value from a labelled group of typed options." "radio-group" radioGroupPreview [
            docsSection "when-to-use" "When to use a radio group" [ docsParagraph "Use RadioGroup when all mutually exclusive options should remain visible. Use Select when the finite choice needs a more compact presentation." ]
            docsSection "forms" "Form and accessibility behavior" [ docsParagraph "The group requires an accessible label and renders connected radio semantics with one shared form name. Applications encode typed values and validate the submitted choice." ] ]

    let dropdownMenuPage =
        componentPage dropdownMenuRegistration "Present a compact set of application-owned actions and destinations." "dropdown-menu" dropdownMenuPreview [
            docsSection "items" "Menu items" [ docsParagraph "MenuItem.link accepts a typed destination resolved by the application. Action expressions remain explicit trusted application code; separators group related commands." ]
            docsSection "keyboard" "Keyboard and focus" [ docsParagraph "Opening the menu focuses the first item. Arrow keys, Home, End, Enter, Space, Escape, Tab, pointer selection, and outside dismissal preserve a coherent menu focus model and return focus to the trigger when appropriate." ] ]

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
          statusRegistration.path, statusPage
          tableRegistration.path, tablePage
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
