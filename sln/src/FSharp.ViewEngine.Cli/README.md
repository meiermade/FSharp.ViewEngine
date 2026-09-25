# FSharp.ViewEngine.Cli

`FSharp.ViewEngine.Cli` provides the `fve` .NET tool. It copies selected FSharp.ViewEngine Components and their transitive dependencies into source files that your application owns.

Pin it in a repository-local tool manifest:

```sh
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init src/MyApp.Components/MyApp.Components.fsproj --namespace MyApp.Components
dotnet fve add button text-field --config src/MyApp.Components/fve.json
```

A global installation is also supported:

```sh
dotnet tool install --global FSharp.ViewEngine.Cli
fve --help
```

Commit the generated project, `fve.json`, source, and CSS. The configuration records the registry version and checksums. Use `dotnet fve diff --config src/MyApp.Components/fve.json` before changing the pinned tool version. Modified or missing files are reported and never silently replaced; `fve add --overwrite` is the explicit replacement path.
