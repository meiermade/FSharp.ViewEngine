# Financial templates

The three templates share one account/transaction model. The source ZIP includes all source and project files, input.css, and the pinned browser assets and fonts. Extract it while preserving the Domain/, UseCases/, and wwwroot/ subdirectories.

```text
Domain/Domain.fs                Financial fixtures, workspace facts and invariants
Domain/Ledger.Domain.fsproj     Ledger.Domain library
UseCases/Operations.fs          Typed submission validation and selection review
UseCases/Ledger.Application.fsproj  Ledger.Application library
Components/Acme.Components.fsproj  Installed components
Example.fsproj                 Ledger.Server executable
Model.fs, Routing.fs           HTTP destinations and per-render state
Layout.fs, Application.fs      Shared shell and financial pages
AppMode.fs                     Compact Specification review controls
Specification.fs, Architecture.fs  Workflows and project contracts
ApiDocumentation.fs            cURL API reference
Hosting.fs, Program.fs          Native form adapter and executable host
```

The project references follow Ledger.Server → Ledger.Application → Ledger.Domain, with Ledger.Server also referencing Acme.Components.

## Install the building blocks

```sh
dotnet fve init Components/Acme.Components.fsproj --namespace Acme.Components
dotnet fve add card page-top-bar page-header section-header side-nav breadcrumbs table description-list avatar input select radio-group badge button button-group dropdown-menu theme-switcher resizable dialog drawer notice empty-state tabs browser code-block mermaid --config Components/fve.json
```

See the [Browser](https://fve.meiermade.com/components/browser), [Tabs](https://fve.meiermade.com/components/tabs), [Button](https://fve.meiermade.com/components/button), [Button group](https://fve.meiermade.com/components/button-group), [Dropdown menu](https://fve.meiermade.com/components/dropdown-menu), and [Dialog](https://fve.meiermade.com/components/dialog) documentation for those building blocks.

## Assets

The ZIP includes `input.css`, configured for Tailwind CSS v4 with the Typography plugin and Noto Sans/Noto Sans Mono. Compile your own stylesheet after installing the components, then run the host:

```sh
npm install --save-dev tailwindcss @tailwindcss/cli @tailwindcss/typography
npx @tailwindcss/cli -i input.css -o wwwroot/css/output.css --minify
dotnet run --project Example.fsproj
```

The included `wwwroot/fonts` contains the Noto font files and their license. `wwwroot/css` and `wwwroot/scripts` contain the pinned Prism stylesheet and Prism/F#/SQL/Bash/JSON scripts, Mermaid script, and Datastar script configured in Layout.fs. No catalog assets or Templates assembly are required at runtime. You may replace these consumer-owned fonts and assets intentionally while retaining the shared type roles and geometry. CodeBlock.assetsWithNonce and Mermaid.assetsWithNonce own their small browser lifecycles and authorize lazy-loaded assets with the active document nonce.

The application opens at `/examples/application`, its spec at `/examples/specification`, and the API reference at `/examples/api-documentation`. The catalog opens each example in a new tab and adds a source-ZIP download link to its own top bar. These download links are host-owned actions, not part of the standalone host. Specification preview/product navigation deliberately uses complete-document requests rather than the catalog's general Datastar page morphing.

The financial application includes Home, Accounts, Transactions, workspace selection, Settings, and Profile. Create and edit use a contextual Drawer with one editable column and fixed header/footer; one-choice defaults are read-only. The consumer handles originating-page context, transient error values and unsaved-change dismissal through Drawer.withAttributes. Account and transaction deletion use the shared Dialog, focus Cancel and submit Delete; account activity blocks eligibility, while transaction deletion only requires an existing record. These operations validate immutable fixtures rather than saving or deleting them. Failed editor values are returned only in a no-store response, never in URLs, cookies or storage. Shells fill the viewport; reading and financial content are constrained inside them. Secondary page actions and Cancel use shared Neutral + Outline Button presentation, with native destination links rendered by Button.renderLink. Page-header overflow opts into DropdownMenuTrigger.withVariant ButtonVariant.Outline; row menus and navigation keep their lighter default presentation. Applied filters use ButtonGroup to join a muted label, shared Select and icon-only remove Button at the shared compact control size. Select.withNativeFallback provides a styled native select before enhancement; the enhanced value input emits a bubbling change event after a committed selection. The consumer-owned GET form applies the filter automatically; without JavaScript it exposes an Apply button. Removal submits a separate native GET form, preserving search, sort and the other filters. Clear all remains a separate link.

SideNav exposes four optional slots: Header, Context, Content and Footer. Layout.applicationShell puts the brand in SideNavHeader alongside the equally high PageTopBar, the workspace SideNavRow menu in Context, SideNavContent navigation in Content: plain SideNavItem links, non-collapsible SideNavSection headings, and collapsible SideNavGroup labels. Sections use small, muted labels with normal casing and no child indentation or guide. Links never reserve empty chevron spacers: plain links use the same inset at every depth, aligned with the visible edge of sibling group chevrons, with a 4px gutter between guides and row highlights. Only collapsible groups retain nested indentation and guides aligned under their chevrons. Settings/Profile SideNavRow links occupy Footer. Context and Footer take ordered lists of rendered elements, so rows can be added, removed or replaced without changing the container. Only Content scrolls; the owning layout supplies the bounded height. The documentation composition omits Context and Footer. All of these SideNav helpers are installed by `side-nav` and documented on the [Side nav page](https://fve.meiermade.com/components/side-nav).

## Security and host integration

The host keeps Datastar rather than replacing its expression runtime. It pins the official Datastar 1.0.4 standard bundle and uses [Datastar's nonce CSP mode](https://data-star.dev/reference/security). `Hosting.securityHeaders` creates a fresh cryptographically random nonce for each full-page response. `Layout.documentWithNonce` supplies that value through `<html data-nonce="…">` and authorizes ThemeSwitcher, CodeBlock and Mermaid initialization. Datastar consumes the bootstrap attribute, then uses the document nonce for expressions and scripts in element patches. Do not replace the active nonce on a partial update. A new full document, including native validation errors, receives a new nonce.

The script policy contains `script-src 'self' 'nonce-{value}'` and `script-src-attr 'none'`, not `unsafe-eval` or unrestricted script `unsafe-inline`. `connect-src 'self'` permits same-origin backend actions. External runtime asset origins must be explicitly authorized by the host; this standalone host requires none. The current components use inline CSS variables/positioning, so `style-src 'self' 'unsafe-inline'` is an intentional, separate styling allowance—not a claim of inline-style-free compatibility. Tailor image, frame, form and other directives to your application. These examples allow same-origin framing for their Browser previews.

Keep executable expressions and raw markup developer-controlled. HTML encoding does not make JavaScript interpolation safe. Serialize user values as data; signals are visible and editable in the browser, never credentials or authorization. Validate values and authorization server-side, and sanitize any intentionally allowed user HTML. The immutable fixtures and Origin check are not a production authentication/CSRF system. Private invalid drafts remain encoded in no-store responses; there are no durable identity or financial mutations.

Profile appearance marks its native radios with `ThemeSwitcher.nativeChoiceAttributes`. The already-authorized theme bootstrap handles native click/change/Space before delayed Datastar initialization, while normal enhanced state synchronization remains Datastar-owned. No inline event-handler attributes or application-specific theme runtime are needed.

## Executable Specification

Layout.documentationShell is an explicit consumer-owned composition of SideNav, PageTopBar, Breadcrumbs, ThemeSwitcher and Resizable—not a second installable application/documentation framework. Adapt its identity, navigation, breadcrumb hierarchy and content. It follows the actual FVE Docs shell: 48px branding/top bar, compact left-chevron navigation, independently scrolling left/main/right regions, and keyboard/pointer-resizable rails. Article pages constrain reading width and provide On this page; workflows use the wide main region. Layout.documentationSection gives article sections truthful fragment targets. The narrow navigation uses a native modal with Escape/focus return, with an ordinary disclosure fallback when JavaScript is unavailable.

Resource navigation groups separate jobs: Accounts contains View accounts, View account, Create account, Update account and Delete account; Transactions contains View transactions and View transaction. Each workflow owns **Wireframe → Sequence → Rules** and named states. Specification.fs builds each state's HTML with the same Application renderer used by the standalone application. Ordinary underlined Tabs show these elements inside Browser previews; Sequence and bullet Rules stay outside the tabs.

Each preview's compact **expand icon** is a real App-mode link. The URL selects the workflow and `specState`; `appMode=1` (also accepting the previous `fveAppMode=app`) selects a complete product document rather than the documentation shell. AppMode.fs composes the compact floating state dropdown, previous/next workflow icons, System/Light/Dark menu, top/bottom docking and X using installed controls. X links to the current workflow/state without App mode.

Click product links to continue through the actual financial journey: Home → account/transaction detail, Accounts → create/edit → server validation → cancel/back, Settings sections, workspace changes and Profile. Model.fs maps those destinations to real Specification routes with exact resource IDs. GET forms carry workspace and review context; Hosting.fs validates the same bounded POST actions: account errors return entered values only in a private, no-store response, while successful and non-editor outcomes redirect to finite states without private submitted values. `fveAppDock=top/bottom` survives product/state/workflow navigation and form outcomes. The server resolves both the displayed product and the review controls from the same URL.

Use a distinct preview ID for each inline state to scope its controls. Editor and deletion previews show the shared form or confirmation; App mode opens the native Dialog with the review controls available.

Sequence diagrams name the actual GET/POST form endpoints, projects, validation branches and responses. The Architecture navigation drills from System context to Solution overview and the four project contracts. Clickable diagram nodes open those same pages.

Appearance is one document-wide preference provided by the installed ThemeSwitcher. Layout.documentWithNonce includes ThemeSwitcher.assetsWithNonce before the stylesheet; the application bar, documentation bar, Profile and App-mode menu share `window.fveColorMode` and the `fve-color-mode` event. Only the local appearance preference is persisted. Include the assets once, choose your own storage key, and keep the same key across your product and Spec. The examples fill the viewport without an extra catalog viewer bar. Inline previews and App-mode documents use their own layout bounds. Query.topBarActions supplies optional host-owned controls; inline state previews omit these controls.

## API reference

Resource-grouped navigation and a responsive two-column layout place descriptions, parameters and returns beside copyable **cURL** requests and JSON response states. Columns stack when space is constrained. Examples do not imply a financial SDK or a working Run-request service.

Workspace context uses `organization`, `environment`, and `ledger` query values. It is carried by ordinary links and native forms. The example ledgers/environments deliberately share seeded financial records; sandbox contexts are labelled. Replace the authored fixtures with your own scoped use cases in a real application.

Account forms use USD, generic subtypes, type-root parents and no required month-end observations. They open as authored dialogs, with a native no-JavaScript form fallback. Settings exposes real category destinations but does not administer organizations, issue API secrets, or connect billing. Profile appearance is a local browser preference; identity submissions are not stored.

Forms validate finite seeded demo states without durable submission retention or financial mutations; account errors preserve entered values only in the current response. Replace that demo boundary with your own application use cases before production. API operations are illustrative contracts: `$API_ORIGIN` refers to your own backend, not a live API hosted by this catalog.
