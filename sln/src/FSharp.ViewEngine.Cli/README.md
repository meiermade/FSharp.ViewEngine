# FSharp.ViewEngine.Cli

The `fve` .NET tool installs selected components and their dependencies as editable F# and JavaScript source owned by your application.

Install it in a repository-local tool manifest:

```sh
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve --help
```

See the [installation guide](https://fve.meiermade.com/components/installation), [component pages](https://fve.meiermade.com/components), and [CLI source](https://github.com/meiermade/FSharp.ViewEngine/tree/main/sln/src/FSharp.ViewEngine.Cli).

New project consumers default to `net11.0`; use `--framework net10.0` for .NET 10. Existing projects retain their target framework. The CLI runs with .NET 10 or 11; older target choices remain available for existing consumers but are no longer actively tested.

For a web app:

```sh
dotnet fve init Acme.Web.fsproj
dotnet fve add button link
npm install
dotnet fve compile
```

Edit `fve.json` to select the frontend paths. `init` adds the build dependencies and a generated imports file without replacing the app's CSS or JavaScript. Use `fve watch` in development alongside `dotnet watch`.

Standalone F# scripts are also supported with SDK 10 or 11. Use SDK 11 for new scripts:

```sh
dotnet fve init deck.fsx
dotnet fve add presentation
dotnet fve run deck.fsx
```

`run` uses the manifest next to the script (or the `--config` you specify) to produce adjacent `.html`, `.css`, and `.js` files. It reads edited installed source as-is. Scripts use pinned Tailwind and esbuild from a local tool cache and don't need their own npm project. See the [script guide](https://fve.meiermade.com/guides/scripts) or the [presentation guide](https://fve.meiermade.com/guides/presentations).

Before updating installed source, use `dotnet fve diff --config path/to/fve.json` to inspect local changes. `fve add --overwrite` explicitly replaces managed source; preserve consumer edits deliberately.
