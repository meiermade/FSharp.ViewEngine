namespace Docs.Pages

open Docs.Common

module Home =
    let page =
        { id = "home"
          path = "/"
          aliases = [  ]
          navLabel = "Introduction"
          category = "Getting started"
          title = "FSharp.ViewEngine"
          browserTitle = "FSharp.ViewEngine Documentation"
          nodes = [
            Paragraph [ Text "A minimal, fast view engine for F# that combines the best ideas from several F# view engines. Inspired by "; Link("Giraffe.ViewEngine", "https://github.com/giraffe-fsharp/Giraffe.ViewEngine"); Text ", "; Link("Feliz.ViewEngine", "https://github.com/dbrattli/Feliz.ViewEngine"); Text ", "; Link("Oxpecker.ViewEngine", "https://github.com/Lanayx/Oxpecker"); Text ", and "; Link("Bolero", "https://github.com/fsbolero/Bolero"); Text "." ];
            Heading { id = "design"; title = "Design"; level = 2 };
            Paragraph [ Text "FSharp.ViewEngine uses "; Strong [ Text "computation expressions" ]; Text " (like Oxpecker.ViewEngine and Bolero) to build elements. Each element takes a "; Strong [ Text "Feliz-style single sequence" ]; Text " of attributes and children — there are no separate attribute and children lists. Attributes are "; Strong [ Text "prefixed with underscore" ]; Text " by convention (like Giraffe.ViewEngine, e.g. "; InlineContent.Code "_class"; Text ", "; InlineContent.Code "_id"; Text ", "; InlineContent.Code "_hxGet"; Text "), which produces clean syntax with nice syntax highlighting. The computation expression allows "; Strong [ Text "mixed yielding" ]; Text " of strings, elements, and attributes in any order, so there is no need for a special "; InlineContent.Code "_children"; Text " attribute." ];
            Heading { id = "key-features"; title = "Key Features"; level = 2 };
            UnorderedList [
                [ Strong [ Text "Minimal and fast" ]; Text " — as lean as possible while remaining expressive and type-safe" ];
                [ Strong [ Text "Type-safe HTML generation" ]; Text " with F#" ];
                [ Strong [ Text "Built-in support for HTMX, Alpine.js, Datastar, Tailwind CSS, and SVG" ] ];
                [ Strong [ Text "Composable and functional approach" ] ];
                [ Strong [ Text "No runtime dependencies" ] ]
            ];
            Heading { id = "quick-example"; title = "Quick Example"; level = 2 };
            CodeBlock("fsharp", """open FSharp.ViewEngine
open type Html
open type Htmx
open type TailwindElements

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
    |> Render.toHtmlDocString""");
            Heading { id = "getting-started"; title = "Getting Started"; level = 2 };
            Paragraph [ Text "To get started with FSharp.ViewEngine, check out the "; Link("Installation", "/installation"); Text " guide and then see the "; Link("Usage", "/usage"); Text " example." ];
          ] }
