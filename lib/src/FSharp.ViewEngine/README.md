[![Release](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/release.yml/badge.svg)](https://github.com/meiermade/FSharp.ViewEngine/actions/workflows/release.yml)

# FSharp.ViewEngine
View engine for F#. Inspired by [Giraffe.ViewEngine](https://github.com/giraffe-fsharp/Giraffe.ViewEngine) and
[Feliz.ViewEngine](https://github.com/dbrattli/Feliz.ViewEngine).
Documentation site built using FSharp.ViewEngine available at [https://fsharpviewengine.meiermade.com](https://fsharpviewengine.meiermade.com).
> See [App](./src/App) for the source code.

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
