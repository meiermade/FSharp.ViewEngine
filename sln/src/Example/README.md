# Geldos examples

A standalone FSharp.ViewEngine consumer with Marketing, Application, Presentation, Specification, and Documentation sharing one Geldos financial model. See the [Docs gallery](https://fve.meiermade.com/examples) or the source in this directory for implementation details.

## Shared product views

`Example.UI/Views/` owns the product shell, screens, tables and editors used by both Application and Specification. `Example.UI/Components/` contains the CLI-installed controls; updates do not replace `Views/`. The UI owns its view models and has no ledger or host dependency. Application maps its use-case results to those models. Specification supplies fixture view models and its own fixture outcomes, not a second copy of the product views. Workflow pages lead with Fixture, include Sequence when useful, and show Requirements only for non-obvious behavior not already demonstrated or covered by Architecture.

Page links use Datastar GETs where enhanced; native GETs still return complete documents. The server updates history after patching the page. Datastar also handles in-page filters, form feedback, and Specification review controls. Expanding the review Browser keeps the current product iframe and any draft intact.

`Example.Application` hosts Marketing and Application on the same origin. `Example.Specification` runs executable workflows against fixture data without an App server. Keep Spec local in a real product; the FVE gallery publishes this fixture-only demo. `Example.Documentation` is an independently publishable documentation site, with resource overviews and an API reference. `Example.Presentation` hosts the offline-authored deck for the FVE gallery. Each host lists its own dependencies and sources and owns its startup, routes, and browser entry points. Specification references `Example.Domain` to show actual types in Architecture; Documentation uses it for sample financial payloads.

## Run

Requires the .NET 11 SDK and Node.js/npm to build browser assets. From the repository's `sln` directory:

```sh
./fake.sh PublishExample
```

Run whichever host you want. Each publish directory contains everything that host needs at runtime:

| Assembly under `sln/src/Example` | Start at |
| --- | --- |
| `Example.Application/out/Example.Application.dll` | `/examples/marketing` or `/examples/application` |
| `Example.Specification/out/Example.Specification.dll` | `/examples/specification` |
| `Example.Documentation/out/Example.Documentation.dll` | `/examples/documentation` |
| `Example.Presentation/out/Example.Presentation.dll` | `/examples/presentation` |

For example, `dotnet src/Example/Example.Specification/out/Example.Specification.dll` from `sln` launches Spec. Use the origin printed by ASP.NET Core. If serving your own Docs, set `EXAMPLE_DOCS_URL` for the application and Spec to point to it. FAKE builds each host’s CSS and JS; the published hosts do not need Node.js.

For containers, `sln/Dockerfile` has `example-application`, `example-specification`, `example-documentation`, and `example-presentation` targets. The root Compose file builds each as a separate service.

## Standalone presentation script

With a .NET 11 SDK, Node.js 20+/npm and the FSharp.ViewEngine CLI installed:

```sh
cd Example.Presentation
dotnet fve run deck.fsx
```

The CLI generates `deck.html`, with adjacent CSS and a classic JavaScript viewer bundle. Frontend tools are pinned and cached by the CLI; there is no per-deck npm setup. `Example.Presentation/View.fs` defines the five slides for both the hosted page and the standalone deck. The light and dark application captures live in `Example.Presentation/wwwroot/images/presentation/`. The standalone build embeds the light captures and logo for offline use. Only the hosted slides link into the Application demo and API reference. The script has its own CLI-installed editable components and requires neither the server nor Docs. Its generated HTML, stylesheet and viewer bundle are ignored by Git. The viewer supplies expand/restore and slide navigation; without JavaScript, the separated slide stack remains readable.

## Frontend development

Requires Node.js/npm. From the repository's `sln` directory:

```sh
./fake.sh BuildExampleCss
```

For catalog review, run `./fake.sh WatchDocs` from the repository's `sln` directory. The local gateway at `http://127.0.0.1:5054` routes to the catalog and the four example hosts. The watcher rebuilds their source and assets.
