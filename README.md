[![Release](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/release.yml/badge.svg)](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/release.yml)

<p align="center">
  <img src="etc/logo.png" alt="FSharp.ViewEngine" width="128">
</p>

# FSharp.ViewEngine
View engine for F#. Inspired by [Giraffe.ViewEngine](https://github.com/giraffe-fsharp/Giraffe.ViewEngine),
[Feliz.ViewEngine](https://github.com/dbrattli/Feliz.ViewEngine), and
[Oxpecker.ViewEngine](https://github.com/Lanayx/Oxpecker).
Documentation site built using FSharp.ViewEngine available at [https://fsharpviewengine.meiermade.com](https://fsharpviewengine.meiermade.com).
> See [docs/src/Docs](./docs/src/Docs) for the source code.

## Installation
Add the core view engine package.
```shell
dotnet add package FSharp.ViewEngine
```

## Usage
```fsharp
open FSharp.ViewEngine
open type Html
open type Htmx
open type Alpine
open type Datastar
open type Tailwind

html {
    _lang "en"
    head {
        title "Test"
        meta { _charset "utf-8" }
        link { _href "/css/compiled.css"; _rel "stylesheet" }
    }
    body {
        _xData "{showContent: false}"
        _class "bg-gray-50"
        div {
            _id "page"
            _class [ "flex"; "flex-col" ]
            h1 { _hxGet "/hello"; _hxTarget "#page"; "Hello" }
            h1 { _hxGet "/world"; _hxTarget "#page"; "World" }
        }
        br
        div {
            _xShow "showContent"
            h2 { "Content" }
            p { "Some content" }
            ul {
                li { "One" }
                li { "Two" }
            }
        }
    }
}
|> Render.toHtmlDocString
```
```html
<!DOCTYPE html>
<html lang="en">
    <head>
        <title>Test</title>
        <meta charset="utf-8">
        <link href="/css/compiled.css" rel="stylesheet">
    </head>
    <body x-data="{showContent: false}" class="bg-gray-50">
        <div id="page" class="flex flex-col">
            <h1 hx-get="/hello" hx-target="#page">Hello</h1>
            <h1 hx-get="/world" hx-target="#page">World</h1>
        </div>
        <br>
        <div x-show="showContent">
            <h2>Content</h2>
            <p>Some content</p>
            <ul>
                <li>One</li>
                <li>Two</li>
            </ul>
        </div>
    </body>
</html>
```

## Benchmarks
Ran on February 6, 2026 with BenchmarkDotNet MediumRun only.

Environment:
- Windows 11 (10.0.26200.7623)
- 12th Gen Intel Core i9-12900HK
- .NET SDK 10.0.102, .NET Runtime 10.0.2 (X64 RyuJIT AVX2)

Command:
```
cd lib && dotnet run -c Release --project src/Benchmarks/Benchmarks.fsproj
```

BuildAndRender (mean, lower is better):
| Method        | Mean      | Allocated |
|-------------- |----------:|----------:|
| ViewEngineApi |  5.763 μs |  11.4 KB  |
| OxpeckerApi   |  7.562 μs | 12.88 KB  |
| GiraffeApi    |  7.925 μs | 23.95 KB  |
| FelizApi      | 11.053 μs | 25.87 KB  |

RenderOnly:
| Method        | Mean     | Allocated |
|-------------- |---------:|----------:|
| ViewEngineApi | 2.464 μs |  2.94 KB  |
| OxpeckerApi   | 2.796 μs |  2.94 KB  |
| GiraffeApi    | 3.176 μs | 12.77 KB  |
| FelizApi      | 6.151 μs |  14.2 KB  |

BuildOnly:
| Method        | Mean     | Allocated |
|-------------- |---------:|----------:|
| ViewEngineApi | 2.153 μs |  8.46 KB  |
| OxpeckerApi   | 5.275 μs |  9.95 KB  |
| GiraffeApi    | 7.323 μs | 11.17 KB  |
| FelizApi      | 7.707 μs | 11.66 KB  |
