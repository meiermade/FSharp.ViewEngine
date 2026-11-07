# Financial application example

A standalone FSharp.ViewEngine consumer with Application, Specification, and API documentation surfaces. See the [Docs gallery](https://fve.meiermade.com/examples) or the source in this directory for implementation details.

## Run

Requires the .NET 10 SDK. From this directory:

```sh
dotnet run --project Example.Server
```

At the origin printed by ASP.NET Core, open `/examples/application`, `/examples/specification`, or `/examples/api-documentation`. Browser assets and generated CSS are checked in; Node.js is not needed to run.

## Frontend development

Requires Node.js/npm:

```sh
npm ci --prefix Example.Server
npm run build:css --prefix Example.Server
```

For catalog review, run `./fake.sh WatchDocs` from the repository's `sln` directory. Its single host at `http://127.0.0.1:5054` serves these same routes and watches their source and assets.
