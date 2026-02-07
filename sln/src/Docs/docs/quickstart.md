# Quickstart

Get started with FSharp.ViewEngine in just a few steps.

## Basic Usage

Elements are built using computation expressions. You can yield attributes (prefixed with `_`), child elements, and strings in any order — they all go into a single sequence, so there is no need for a separate children attribute:


```fsharp
open FSharp.ViewEngine
open type Html

let myView = 
    div {
        _class "container"
        h1 { "Hello, World!" }
        p { "Welcome to FSharp.ViewEngine" }
    }

// Render to string
let htmlString = Render.toHtmlDocString myView
```

This will produce the following HTML:

```html
<div class="container">
    <h1>Hello, World!</h1>
    <p>Welcome to FSharp.ViewEngine</p>
</div>
```

## With HTMX and Tailwind CSS

FSharp.ViewEngine includes built-in support for HTMX and Tailwind CSS:

```fsharp
open FSharp.ViewEngine
open type Html
open type Htmx
open type Tailwind

let interactiveView =
    div {
        _class [ "bg-blue-500"; "text-white"; "p-4"; "rounded" ]
        button {
            _class [ "bg-green-500"; "hover:bg-green-700"; "px-4"; "py-2"; "rounded" ]
            _hxGet "/api/data"
            _hxTarget "#result"
            "Load Data"
        }
        div {
            _id "result"
            _class [ "mt-4" ]
        }
    }
```

## Complete HTML Document

Here's how to create a complete HTML document:

```fsharp
let completePage =
    html {
        _lang "en"
        head {
            title "My Page"
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            link { _rel "stylesheet"; _href "https://cdn.tailwindcss.com" }
        }
        body {
            _class [ "bg-gray-100"; "font-sans" ]
            div {
                _class [ "container"; "mx-auto"; "px-4"; "py-8" ]
                h1 {
                    _class [ "text-3xl"; "font-bold"; "text-gray-900"; "mb-4" ]
                    "Welcome to my site!"
                }
                p {
                    _class [ "text-lg"; "text-gray-600" ]
                    "This is built with FSharp.ViewEngine."
                }
            }
        }
    }
```
