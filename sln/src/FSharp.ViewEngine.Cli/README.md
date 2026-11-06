# FSharp.ViewEngine.Cli

`FSharp.ViewEngine.Cli` provides the `fve` .NET tool. It copies selected FSharp.ViewEngine Components and their transitive dependencies into source files that your application owns. A component includes all of its required F# and JavaScript files; there are no separate script selectors. `fve.json` lists selected components and tracks each installed file's owner and checksum separately. Shared helpers, runtimes and licenses resolve internally.

Pin it in a repository-local tool manifest:

```sh
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init src/MyApp.Components/MyApp.Components.fsproj --namespace MyApp.Components
dotnet fve add button input --config src/MyApp.Components/fve.json
```

A global installation is also supported:

```sh
dotnet tool install --global FSharp.ViewEngine.Cli
fve --help
```

Commit the generated project, `fve.json`, and source files. Components provide opinionated [Tailwind CSS](https://tailwindcss.com/) styling and [Datastar](https://data-star.dev/) interactivity; your application owns the CSS and JavaScript builds.

## Frontend build tools

Install Tailwind CSS v4, its CLI and Typography plugin, and esbuild together. Run from your web project:

```sh
npm init -y
npm install --save-dev tailwindcss@4 @tailwindcss/cli@4 @tailwindcss/typography esbuild
```

Commit `package.json` and `package-lock.json`; use `npm ci` on subsequent installs. Create `input.css` beside `package.json`:

```css
@import "tailwindcss" source(none);
@plugin "@tailwindcss/typography";
@source "../MyApp.Components/Components/**/*.fs";
@source "../MyApp.Components/Components/Assets/**/*.js";
@source not "../MyApp.Components/Components/Assets/echarts.6.1.0.esm.min.js";
@source "./**/*.fs";
```

`fve` copies no CSS. Tailwind scans your installed F#, component JavaScript and application views.

## Browser modules

The CLI installs editable ES modules under `Components/Assets/`, including the shared pinned Rocket runtime and its license. Rocket includes Datastar; no separate Datastar installation or script is needed. Native layout controls need no JavaScript. Table, Button, TagInput and Chart install their own editable modules. Chart's module and pinned ECharts renderer can load separately from basic controls; its [component page](https://fve.meiermade.com/components/chart) shows the lazy app entry and settled range event. The ECharts license, notice and provenance are installed with the renderer.

For Button, create `js/app.js` in your web project:

```js
import '../../MyApp.Components/Components/Assets/button.js';
```

Build both assets:

```sh
npx tailwindcss -i input.css -o wwwroot/css/output.css --minify
npx esbuild js/app.js --bundle --format=esm --splitting --outdir=wwwroot/assets --minify
```

Load the stylesheet and module once in your document head:

```fsharp
link { _rel "stylesheet"; _href "/css/output.css" }
script { _type "module"; _src "/assets/app.js" }
```

The [Installation guide](https://fve.meiermade.com/components/installation) includes npm build/watch scripts and a themed F# example.

## Source updates

The configuration records the registry version and checksums. Use `dotnet fve diff --config src/MyApp.Components/fve.json` before changing the pinned tool version. Modified or missing files are reported and never silently replaced; `fve add --overwrite` is the explicit replacement path. See the [component setup guide](https://github.com/meiermade/FSharp.ViewEngine/tree/main/sln/src/FSharp.ViewEngine.Components#readme) for the complete consumer workflow.
