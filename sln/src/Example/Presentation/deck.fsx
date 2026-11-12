// fve:components:start
#r "nuget: FSharp.ViewEngine, 2026.8.2"
#load "Components/ComponentBuilder.fs" "Components/Foundation.fs" "Components/Slide.fs" "Components/Button.fs" "Components/IconNames.fs" "Components/IconData.fs" "Components/Icon.fs" "Components/Presentation.fs"
open Example.Components
// fve:components:end
#load "../Example.Domain/Domain.fs" "../Example.Application/Home.fs" "View.fs"

open System.IO
open FSharp.ViewEngine
open type Html

let document =
    html {
        _lang "en"
        head {
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            title { "Geldos · Treasury review" }
            link { _rel "stylesheet"; _href "deck.css" }
        }
        body {
            _class (ComponentsTheme.className ComponentsTheme.emerald + " m-0 bg-[var(--fve-background)] p-4")
            main {
                h1 { _class "sr-only"; "Geldos treasury review" }
                Example.Presentation.View.deck
            }
            script { _src "deck.js" }
        }
    }

let output = Path.Combine(__SOURCE_DIRECTORY__, Path.ChangeExtension(__SOURCE_FILE__, ".html"))
File.WriteAllText(output, Render.toHtmlDocString document)
printfn "%s" output
