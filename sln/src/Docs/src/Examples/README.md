# FSharp.ViewEngine examples

Three complete compositions share one financial model: Application, executable Specification, and API documentation. This download includes the host, projects, source, Tailwind input, browser assets, and fonts. Extract it with the `Domain/`, `UseCases/`, and `wwwroot/` directories intact.

## Run

Requires the .NET 10 SDK and Node.js/npm. Use the CLI release matching this example download.

```sh
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init Components/Acme.Components.fsproj --namespace Acme.Components
dotnet fve add card page-top-bar page-header section-header side-nav breadcrumbs table description-list avatar input select radio-group badge button button-group dropdown-menu theme-switcher resizable dialog drawer notice empty-state tabs browser code-block mermaid --config Components/fve.json
npm install --save-dev tailwindcss @tailwindcss/cli @tailwindcss/typography
npx @tailwindcss/cli -i input.css -o wwwroot/css/output.css --minify
dotnet run --project Example.fsproj
```

At the origin printed by the host, open:

- `/examples/application` — financial application.
- `/examples/specification` — workflows, product previews, sequence diagrams, rules, and architecture.
- `/examples/api-documentation` — illustrative requests and responses.

The bundled assets require no catalog connection at runtime. Font licenses are included in `wwwroot/fonts/`.

## Adapt

```text
Domain/                  Fixtures and domain invariants
UseCases/                Typed validation and selection review
Components/              Installed, consumer-owned controls
Model.fs, Routing.fs     HTTP destinations and render state
Layout.fs                Shared application and documentation shells
Application.fs           Financial pages
Specification.fs         Workflows using the same application renderer
Architecture.fs          Project contracts and diagrams
AppMode.fs               Specification review controls
ApiDocumentation.fs      API reference
Hosting.fs, Program.fs   Request adaptation and executable host
```

Project references follow Server → Application → Domain, with Server also referencing Components. Replace the identity, navigation, fixtures, and use cases with your own. Component APIs and interactions are documented in the [component catalog](https://fve.meiermade.com/components), not duplicated here. Catalog download controls are host-owned and are not included in the standalone pages.

## Boundaries

Forms validate immutable fixtures; they do not save or delete records. Invalid drafts are encoded in private, no-store responses, never retained in URLs or durable storage. API operations are illustrative contracts, not live endpoints; `$API_ORIGIN` means your backend.

`Hosting.securityHeaders` and `Layout.documentWithNonce` integrate the pinned Datastar 1.0.4 bundle with fresh full-document nonces. Enhanced patches retain the active document nonce. Script policy excludes `unsafe-eval` and unrestricted inline scripts; connections are same-origin. Styling deliberately permits `style-src 'self' 'unsafe-inline'`. See the [nonce CSP guide](https://fve.meiermade.com/extensions/datastar#content-security-policy).

Keep executable expressions and raw markup developer-controlled; serialize user values as data. Browser signals are not secrets or authorization. The fixtures and Origin check are not production authentication or CSRF protection. Your host owns validation, authorization, persistence, and the complete security policy.
