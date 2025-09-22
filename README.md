[![Release](https://github.com/ameier38/FSharp.ViewEngine/actions/workflows/release.yml/badge.svg)](https://github.com/ameier38/FSharp.ViewEngine/actions/workflows/release.yml)

![logo](./etc/logo.svg)

# FSharp.ViewEngine
View engine for F#. Inspired by [Giraffe.ViewEngine](https://github.com/giraffe-fsharp/Giraffe.ViewEngine) and
[Feliz.ViewEngine](https://github.com/dbrattli/Feliz.ViewEngine).

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

html [
    _lang "en"
    _children [
        head [
            title "Test"
            meta [ _charset "utf-8" ]
            link [ _href "/css/compiled.css"; _rel "stylesheet" ]
        ]
        body [
            _xData "{showContent: false}"
            _class "bg-gray-50"
            _children [
                div [
                    _id "page"
                    _class [ "flex"; "flex-col" ]
                    _children [
                        h1 [ _hxGet "/hello"; _hxTarget "#page"; _children "Hello" ]
                        h1 [ _hxGet "/world"; _hxTarget "#page"; _children "World" ]
                    ]
                ]
                br
                div [
                    _xShow "showContent"
                    _children [
                        h2 [ _children "Content" ]
                        p [ _children "Some content" ]
                        ul [
                            _children [
                                li [ _children "One" ]
                                li [ _children "Two" ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
]
|> Element.render
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
