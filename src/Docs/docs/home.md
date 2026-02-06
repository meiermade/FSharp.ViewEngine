# FSharp.ViewEngine Documentation

A minimal, fast view engine for F# that combines the best ideas from several F# view engines. Inspired by [Giraffe.ViewEngine](https://github.com/giraffe-fsharp/Giraffe.ViewEngine), [Feliz.ViewEngine](https://github.com/dbrattli/Feliz.ViewEngine), [Oxpecker.ViewEngine](https://github.com/Lanayx/Oxpecker), and [Bolero](https://github.com/fsbolero/Bolero).

## Design

FSharp.ViewEngine uses **computation expressions** (like Oxpecker.ViewEngine and Bolero) to build elements. Each element takes a **Feliz-style single sequence** of attributes and children — there are no separate attribute and children lists. Attributes are **prefixed with underscore** by convention (like Giraffe.ViewEngine, e.g. `_class`, `_id`, `_hxGet`), which produces clean syntax with nice syntax highlighting. The computation expression allows **mixed yielding** of strings, elements, and attributes in any order, so there is no need for a special `_children` attribute.

## Key Features

- **Minimal and fast** — as lean as possible while remaining expressive and type-safe
- **Type-safe HTML generation** with F#
- **Built-in support for HTMX, Alpine.js, Datastar, Tailwind CSS, and SVG**
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
