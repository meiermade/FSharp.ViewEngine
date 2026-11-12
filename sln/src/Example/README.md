# Geldos examples

A standalone FSharp.ViewEngine consumer with Marketing, Application, Presentation, Specification, and API documentation surfaces sharing one Geldos financial model. See the [Docs gallery](https://fve.meiermade.com/examples) or the source in this directory for implementation details.

## Shared product views

`Example.Components/Views/` owns the product shell, screens, tables and editors used by both Application and Specification. `Example.Components/Components/` contains the CLI-installed controls; updates do not replace `Views/`. Server parses HTTP inputs, runs Application use cases and passes typed data, validation outcomes and navigation behavior to the shared renderer. Specification adds workflow rules, fixture states and review chrome, not a second copy of the product views.

## Run

Requires the .NET 10 SDK. From this directory:

```sh
dotnet run --project Example.Server
```

At the origin printed by ASP.NET Core, open `/examples/marketing`, `/examples/application`, `/examples/presentation`, `/examples/specification`, or `/examples/api-documentation`. Browser assets and generated CSS are checked in; Node.js is not needed to run.

## Standalone presentation script

With a .NET 10 SDK, Node.js 20+/npm and the FSharp.ViewEngine CLI installed:

```sh
cd Presentation
dotnet fve build --open
```

The CLI builds and opens `deck.html`, with adjacent CSS and a classic JavaScript viewer bundle. Frontend tools are pinned and cached by the CLI; there is no per-deck npm setup. `View.fs` supplies the same slide content served by the example host, using the shared Domain and Home use case. The script has its own CLI-installed editable components and requires neither the server nor Docs. Its generated HTML, stylesheet and viewer bundle are ignored by Git. The viewer supplies expand/restore and slide navigation; without JavaScript, the separated slide stack remains readable.

## Frontend development

Requires Node.js/npm:

```sh
npm ci --prefix Example.Server
npm run build:css --prefix Example.Server
```

For catalog review, run `./fake.sh WatchDocs` from the repository's `sln` directory. Its single host at `http://127.0.0.1:5054` serves these same routes and watches their source and assets.
