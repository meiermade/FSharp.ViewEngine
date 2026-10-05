# Financial templates

The three templates share one account/transaction model. Copy all listed source and project files, preserving the Domain/ and UseCases/ subdirectories.

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
dotnet fve add card page-top-bar page-header section-header side-nav breadcrumbs table description-list avatar input select radio-group badge button dropdown-menu dialog notice empty-state tabs browser code-block mermaid --config Components/fve.json
dotnet run --project Example.fsproj
```

See the [Browser](https://fve.meiermade.com/components/browser), [Tabs](https://fve.meiermade.com/components/tabs), [Button](https://fve.meiermade.com/components/button), [Dropdown menu](https://fve.meiermade.com/components/dropdown-menu), and [Dialog](https://fve.meiermade.com/components/dialog) documentation for those building blocks.

## Assets

Use Tailwind CSS v4 with the Typography plugin. Put this in `input.css` beside the template sources; compile to `wwwroot/css/output.css`:

```css
@import "tailwindcss";
@source "./Components/**/*.fs";
@source "./*.fs";
@plugin "@tailwindcss/typography";
@custom-variant dark (&:where(.dark, .dark *));
```

```sh
npm install --save-dev tailwindcss @tailwindcss/cli @tailwindcss/typography
npx @tailwindcss/cli -i input.css -o wwwroot/css/output.css --minify
```

Copy the pinned Prism stylesheet and Prism/F#/SQL/Bash/JSON scripts, Mermaid script, and Datastar script from this repository's `sln/src/Docs/wwwroot/css` and `scripts` into your own `wwwroot` at the paths configured in Layout.fs. CodeBlock.assets and Mermaid.assets own their small browser lifecycles.

The application opens at `/examples/application`, its spec at `/examples/specification`, and the API reference at `/examples/api-documentation`. The catalog's Preview/Code bar is outside these copied source files. Specification preview/product navigation deliberately uses complete-document requests rather than the catalog's general Datastar page morphing.

The financial application includes Home, Accounts, Transactions, workspace selection, Settings, and Profile. Shells fill the viewport; reading and financial content are constrained inside them.

## Executable Specification

Resource navigation groups separate jobs: Accounts contains View accounts, View account, Create account, Update account and Delete account; Transactions contains View transactions and View transaction. Each workflow owns **Wireframe → Sequence → Rules** and named states. Specification.fs builds each state's HTML with the same Application renderer used by the standalone application. Ordinary underlined Tabs show these elements inside Browser previews; Sequence and bullet Rules stay outside the tabs.

Each preview's compact **expand icon** is a real App-mode link. The URL selects the workflow and `specState`; `appMode=1` (also accepting the previous `fveAppMode=app`) selects a complete product document rather than the documentation shell. AppMode.fs composes the compact floating state dropdown, previous/next workflow icons, System/Light/Dark menu, top/bottom docking and X using installed controls. X links to the current workflow/state without App mode.

Click product links to continue through the actual financial journey: Home → account/transaction detail, Accounts → create/edit → server validation → cancel/back, Settings sections, workspace changes and Profile. Model.fs maps those destinations to real Specification routes with exact resource IDs. GET forms carry workspace and review context; Hosting.fs validates the same bounded POST actions and redirects to finite outcomes without retaining submissions. `fveAppDock=top/bottom` survives product/state/workflow navigation and form outcomes. The server resolves both the displayed product and the review controls from the same URL.

Use a distinct preview ID for each inline state to scope its controls. Editor and deletion previews show the shared form or confirmation; App mode opens the native Dialog with the review controls available.

Sequence diagrams name the actual GET/POST form endpoints, projects, validation branches and responses. The Architecture navigation drills from System context to Solution overview and the four project contracts. Clickable diagram nodes open those same pages.

Appearance is one document-wide preference owned by Layout.fs and AppMode.fs. Profile and the compact theme menu share `financial-example-color-mode`; only the local appearance preference is persisted. The catalog alone sets `--example-chrome-height` around its viewer. Inline previews and App-mode documents use their own layout bounds with no catalog sticky offset.

## API reference

Resource-grouped navigation and a responsive two-column layout place descriptions, parameters and returns beside copyable **cURL** requests and JSON response states. Columns stack when space is constrained. Examples do not imply a financial SDK or a working Run-request service.

Workspace context uses `organization`, `environment`, and `ledger` query values. It is carried by ordinary links and native forms. The example ledgers/environments deliberately share seeded financial records; sandbox contexts are labelled. Replace the authored fixtures with your own scoped use cases in a real application.

Account forms use USD, generic subtypes, type-root parents and no required month-end observations. They open as authored dialogs, with a native no-JavaScript form fallback. Settings exposes real category destinations but does not administer organizations, issue API secrets, or connect billing. Profile appearance is a local browser preference; identity submissions are not stored.

Forms validate finite seeded demo states without retaining submissions or running financial operations. Replace that demo boundary with your own application use cases before production. API operations are illustrative contracts: `$API_ORIGIN` refers to your own backend, not a live API hosted by this catalog.
