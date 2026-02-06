# FSharp.ViewEngine Documentation

A powerful F# view engine for building HTML with type safety and composability. Inspired by Giraffe.ViewEngine and Feliz.ViewEngine.

## Key Features

- **Type-safe HTML generation** with F#
- **Built-in support for HTMX attributes**
- **Tailwind CSS integration**
- **Alpine.js directives support** 
- **Composable and functional approach**
- **No runtime dependencies**

## Quick Example

```fsharp
open FSharp.ViewEngine
open type Html
open type Htmx
open type Tailwind

let myPage = 
    html {
        _lang "en"
        head {
            title "My App"
            meta { _charset "utf-8" }
            link { _href "/css/tailwind.css"; _rel "stylesheet" }
        }
        body {
            _class "bg-gray-100"
            div {
                _class [ "container"; "mx-auto"; "p-4" ]
                h1 {
                    _class [ "text-3xl"; "font-bold"; "text-blue-600"; "mb-4" ]
                    "Welcome!"
                }
                button {
                    _class [ "bg-blue-500"; "text-white"; "px-4"; "py-2"; "rounded" ]
                    _hxGet "/api/data"
                    _hxTarget "#content"
                    "Load Content"
                }
                div {
                    _id "content"
                    _class [ "mt-4" ]
                }
            }
        }
    }
    |> Render.toHtmlDocString
```

## Getting Started

To get started with FSharp.ViewEngine, check out the [Installation](installation) guide and then follow the [Quickstart](quickstart) tutorial.
