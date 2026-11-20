# Geldos examples

A standalone FSharp.ViewEngine consumer with Marketing, Application, Presentation, Specification, and API documentation surfaces sharing one Geldos financial model. See the [Docs gallery](https://fve.meiermade.com/examples) or the source in this directory for implementation details.

## Shared product views

`Example.Components/Views/` owns the product shell, screens, tables and editors used by both Application and Specification. `Example.Components/Components/` contains the CLI-installed controls; updates do not replace `Views/`. Server parses HTTP inputs, runs Application use cases and passes typed data, validation outcomes and navigation behavior to the shared renderer. Specification adds reviewable fixture states and review chrome, not a second copy of the product views. Workflow pages lead with Fixture, include Sequence when useful, and show Requirements only for non-obvious behavior not already demonstrated or covered by Architecture.

Links between pages load complete documents. Supporting browsers crossfade with native View Transitions unless reduced motion is requested. Datastar handles in-page filters, form feedback, and Specification review controls. Expanding the review Browser keeps the current product iframe and any draft intact.

`Example.Server/` is organized by HTTP feature. `Common/` owns document/SSE responses and form boundaries; Product handlers share execution between Application and Specification routes. `Config.fs` and `Services.fs` construct host settings and ephemeral demo-session protection, `Host.fs` configures middleware/assets, and `Program.fs` composes routes. Specification, Marketing, API documentation and Presentation keep their own route and presentation code.

## Run

Requires the .NET 11 SDK and Node.js/npm to build browser assets. From the repository's `sln` directory:

```sh
./fake.sh PublishExample
dotnet src/Example/Example.Server/out/Example.Server.dll --urls http://127.0.0.1:5000
```

At the origin printed by ASP.NET Core, open `/examples/marketing`, `/examples/application`, `/examples/presentation`, `/examples/specification`, or `/examples/api-documentation`. FAKE builds `input.css` and `js/app.js` into `wwwroot/css` and `wwwroot/js`. The published server runs without Docs or Node.js.

## Standalone presentation script

With a .NET 11 SDK, Node.js 20+/npm and the FSharp.ViewEngine CLI installed:

```sh
cd Presentation
dotnet fve build --open
```

The CLI builds and opens `deck.html`, with adjacent CSS and a classic JavaScript viewer bundle. Frontend tools are pinned and cached by the CLI; there is no per-deck npm setup. `View.fs` supplies the five-slide Geldos product overview for both the example host and the standalone deck. It uses light and dark captures of the actual Home, Transactions and Accounts screens, kept in `Example.Server/wwwroot/images/presentation/`. The standalone build embeds the light captures and logo for offline use. Only the hosted slides link into the Application demo and API reference. The script has its own CLI-installed editable components and requires neither the server nor Docs. Its generated HTML, stylesheet and viewer bundle are ignored by Git. The viewer supplies expand/restore and slide navigation; without JavaScript, the separated slide stack remains readable.

## Frontend development

Requires Node.js/npm. From the repository's `sln` directory:

```sh
./fake.sh BuildExampleCss
```

For catalog review, run `./fake.sh WatchDocs` from the repository's `sln` directory. The local gateway at `http://127.0.0.1:5054` routes to the separate Docs and Example.Server hosts. The watcher rebuilds their source and assets.
