# Financial application example

One runnable F# application demonstrates Application, executable Specification, and API documentation over the same immutable financial fixtures. It is a real FSharp.ViewEngine consumer, independent of Docs—not source transformed into a download.

## Run

Requires the .NET 10 SDK. From this directory:

```sh
dotnet run --project Example.Server
```

At the origin printed by ASP.NET Core, open `/examples/application`, `/examples/specification`, or `/examples/api-documentation`. Browser assets and generated CSS are checked in; no catalog connection or Node installation is needed to run.

## Projects

```text
Example.Domain/        Financial fixtures and invariants (Ledger.Domain)
Example.Application/   Validation and Home read model (Ledger.Operations, Ledger.Home)
Example.Components/    CLI-installed editable controls and pinned browser assets
Example.Server/        Giraffe host, routes, views, executable Specification and API reference
```

References flow Server → Application → Domain, with Server also referencing Components. Components uses the published FSharp.ViewEngine core package, not the catalog's component assembly. Its `Components/` files and `fve.json` are installed by the CLI and committed; builds never regenerate them. The installed Chart owns its Rocket/ECharts presentation; financial calculations remain in Application.

To refresh the installed controls with this checkout's CLI, from the repository's `sln` directory:

```sh
dotnet run --project src/FSharp.ViewEngine.Cli -- add button --config src/Example/Example.Components/fve.json --overwrite
```

This updates the complete installed set and replaces its managed source. Consumer-owned files outside `Components/` are retained.

## Page ownership

`Example.Server/Application/` contains the product page views and genuinely shared form, table and shell helpers. `Example.Server/Specification/` contains the owning workflow pages, including their states, sequences and rules, plus individual architecture pages. States stay with their page; the small registries connect routes and navigation.

Specification Browser fixtures load complete same-origin product documents using those same application views. Review controls stay outside the iframe; native dialogs and drawers stay inside it. Enhanced Expand/Restore preserves the product document, draft and scroll. The Application example itself does not require an iframe.

## Rocket chart

Home uses a thin [Datastar Rocket](https://data-star.dev/reference/rocket) component around Apache ECharts. The Rocket 1.0.4 bundle replaces ordinary Datastar; never load both in one document. Rocket is beta and pinned here. ECharts 6.1.0 renders SVG, follows the existing Light/Dark/System appearance, resizes, updates JSON props without replacing the plot, and disposes when removed.

The Home page builds ECharts options for its dated balances and opts into `brushend`, received as a bubbling `chart-brushend` event only when a drag completes. It normalizes UTC-axis bounds to inclusive calendar dates, submits the native range form, and receives server-rendered HTML. The product-owned `balance-chart.js` uses the generic `chart-render` hook for currency formatters and brush activation; native double-click resets the range. `Ledger.Home.readHome` calculates balances and filters the full matching fixture set. The URL owns the selected dates; refresh, Back/Forward, and reset use the same read model. Date controls provide the equivalent keyboard workflow. The Specification uses this exact application renderer and chart.

The chart owns no routes, financial calculations, authorization, or transaction queries. Pinned assets, upstream URLs/checksums, and licenses are in `Example.Components/Components/Assets`.

## Styling and catalog development

To edit Tailwind styles, install Node.js/npm and run:

```sh
npm ci --prefix Example.Server
npm run build:css --prefix Example.Server
```

For repository review, use `./fake.sh WatchDocs` from `sln`. Its single host at `http://127.0.0.1:5054` mounts these same routes/assets and watches both stylesheets. The example owns its documents and Rocket runtime; the catalog mounts the same consumer without wrapping its documents. The three gallery cards open in new tabs, and View source links to this directory at the deployed `RELEASE_COMMIT` on GitHub (the repository root for unpublished local work).

## Boundaries

Forms validate immutable fixtures. Invalid editor values live only in their private, no-store response—not URLs or cookies. The example-only one-second editor delay makes real request indicators visible. Native links and forms remain the fallback when JavaScript is unavailable. Set `$API_ORIGIN` to your backend when using the JSON API examples.

Fresh document nonces authorize developer-owned Datastar expressions without `unsafe-eval`. Font licenses are included with the assets.
