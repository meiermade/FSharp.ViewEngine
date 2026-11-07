# FSharp.ViewEngine.Cli

The `fve` .NET tool installs selected components and their dependencies as editable F# and JavaScript source owned by your application.

Install it in a repository-local tool manifest:

```sh
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve --help
```

See the [installation guide](https://fve.meiermade.com/components/installation), [component pages](https://fve.meiermade.com/components), and [CLI source](https://github.com/meiermade/FSharp.ViewEngine/tree/main/sln/src/FSharp.ViewEngine.Cli).

Before updating installed source, use `dotnet fve diff --config path/to/fve.json` to inspect local changes. `fve add --overwrite` explicitly replaces managed source; preserve consumer edits deliberately.
