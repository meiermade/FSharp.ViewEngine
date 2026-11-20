// fve:components:start
#r "nuget: FSharp.ViewEngine, 2026.8.2"
#load "Components/ComponentBuilder.fs" "Components/Foundation.fs" "Components/Slide.fs" "Components/Button.fs" "Components/IconNames.fs" "Components/IconData.fs" "Components/Icon.fs" "Components/Presentation.fs"
open Example.Components
// fve:components:end
#load "View.fs"

open System.IO
open FSharp.ViewEngine
open type Html

// Embed the same product captures and logo the hosted deck serves, without network requests.
let image (path: string) =
    let asset = Path.Combine(__SOURCE_DIRECTORY__, "../Example.Server/wwwroot/images", path)
    let kind = if Path.GetExtension(path) = ".svg" then "image/svg+xml" else "image/webp"
    $"data:{kind};base64," + System.Convert.ToBase64String(File.ReadAllBytes asset)

let document =
    html {
        _lang "en"
        head {
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            title { Example.Presentation.View.title }
            link { _rel "stylesheet"; _href "deck.css" }
        }
        body {
            _class (ComponentsTheme.className ComponentsTheme.emerald + " m-0 bg-[var(--fve-background)] p-4")
            main {
                h1 { _class "sr-only"; Example.Presentation.View.title }
                Example.Presentation.View.deck false image
            }
            script { _src "deck.js" }
        }
    }

let output = Path.Combine(__SOURCE_DIRECTORY__, Path.ChangeExtension(__SOURCE_FILE__, ".html"))
File.WriteAllText(output, Render.toHtmlDocString document)
printfn "%s" output
