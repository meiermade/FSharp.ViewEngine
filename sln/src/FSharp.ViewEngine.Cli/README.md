# FSharp.ViewEngine.Cli

The `fve` .NET tool installs selected components and their dependencies as editable F# and JavaScript source owned by your application.

Install it in a repository-local tool manifest:

```sh
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve --help
```

See the [installation guide](https://fve.meiermade.com/components/installation), [component pages](https://fve.meiermade.com/components), and [CLI source](https://github.com/meiermade/FSharp.ViewEngine/tree/main/sln/src/FSharp.ViewEngine.Cli).

Standalone F# scripts are also supported. Use a .NET 10 SDK for script execution (project consumers still support .NET 8/9/10):

```sh
dotnet fve init deck.fsx
dotnet fve add presentation
dotnet fve build --open
```

The CLI maintains a pinned Core `#r`, one dependency-ordered `#load` directive and the configured namespace in the script's managed block. No `.fsproj` or component DLL is required. Authored content outside the block is preserved; edited or missing managed blocks require explicit `--overwrite`. For script consumers, `init` creates an editable `input.css` and matching asset references in the new HTML shell. `build` reads `fve.json`, runs FSI, compiles Tailwind and bundles installed browser modules into adjacent `.html`, `.css` and `.js` outputs. It preserves authored inputs and edited installed components; it does not run `add` or upgrade source. `--config` selects another manifest and `--open` launches the completed HTML. Node.js 20+ and npm are required; the CLI manages pinned frontend tools in a per-user cache (`FVE_BUILD_CACHE` can select another cache root). Application projects continue using `dotnet build`. See the [script guide](https://fve.meiermade.com/guides/scripts) or the [presentation guide](https://fve.meiermade.com/guides/presentations).

Before updating installed source, use `dotnet fve diff --config path/to/fve.json` to inspect local changes. `fve add --overwrite` explicitly replaces managed source; preserve consumer edits deliberately.
