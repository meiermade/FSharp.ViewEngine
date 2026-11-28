namespace Docs.Pages

open System
open System.IO
open System.Text.RegularExpressions
open Docs.Common

module Presentations =
    let private deckBody =
        """open System.IO
open FSharp.ViewEngine
open type Html

let reviewSlide (attributes:HtmlAttribute list) (children:HtmlElement list) =
    fveSlide {
        yield! attributes
        fveSlideBranding { strong { "Treasury team" } }
        fveSlideFooter { "Internal · Prepared for discussion" }
        yield! children
    }

let deck =
    fvePresentation {
        _label "Quarterly treasury review"
        reviewSlide [_variant SlideVariant.Cover] [
            fveSlideHeader {
                fveSlideTitle { "A clearer cash position." }
                fveSlideSubtitle { "One account model. One set of decisions." }
            }
            fveSlideContent { p { "Author the message, not the slide frame." } }
        ]
        reviewSlide [] [
            fveSlideHeader { fveSlideTitle { "Review the next decision" } }
            fveSlideContent {
                ul {
                    li { "Understand the current position." }
                    li { "Compare the available choices." }
                    li { "Agree the next action." }
                }
            }
        ]
    }

let document =
    html {
        _lang "en"
        head {
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            title { "Quarterly treasury review" }
            link { _rel "stylesheet"; _href "deck.css" }
        }
        body {
            _class (ComponentsTheme.className ComponentsTheme.sky + " m-0 bg-[var(--fve-background)] p-4")
            main {
                h1 { _class "sr-only"; "Quarterly treasury review" }
                deck
            }
            script { _src "deck.js" }
        }
    }

File.WriteAllText(Path.Combine(__SOURCE_DIRECTORY__, "deck.html"), Render.toHtmlDocString document)"""

    let private generatedHeader =
        use stream = typeof<DocPage>.Assembly.GetManifestResourceStream("Docs.PresentationScript.fsx")
        use reader = new StreamReader(stream)
        let source = reader.ReadToEnd()
        let marker = "// fve:components:end"
        let endOfHeader = source.IndexOf(marker, StringComparison.Ordinal) + marker.Length
        Regex.Replace(source.Substring(0, endOfHeader), "(?m)^open .*$", "open deck")

    let private deckScript = generatedHeader + "\n\n" + deckBody + "\n"

    let private pdfCommand =
        [ "\"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome\""
          "  --headless"
          "  --no-pdf-header-footer"
          "  --print-to-pdf=\"$PWD/deck.pdf\""
          "  \"file://$PWD/deck.html\"" ]
        |> String.concat " \\\n"

    let page =
        { id = "presentations-guide"
          path = "/guides/presentations"
          aliases = []
          navLabel = "Presentations"
          category = "Guides"
          title = "Build a presentation"
          browserTitle = "Presentations · FSharp.ViewEngine"
          nodes = [
            Paragraph [Text "Compose a slide deck from ordinary F# HTML, present it in the browser, and print or save it as a PDF."]
            Heading {id="setup"; title="Create a deck"; level=2}
            Paragraph [Text "Check the "; Link("standalone script prerequisites", "/guides/scripts#prerequisites"); Text ", then create a folder for the deck. Run setup and all later commands from inside it:"]
            CodeBlock("shell", """mkdir "My Presentation"
cd "My Presentation"
dotnet new globaljson --sdk-version 11.0.100 --roll-forward latestFeature
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init deck.fsx
dotnet fve add presentation""")
            Heading {id="composition"; title="Compose slides with ordinary F#"; level=2}
            Paragraph [Text "The CLI manages the header between the highlighted lines. Write your code below it."]
            ScriptFile("deck.fsx", deckScript)
            Paragraph [Text "See the "; Link("Slide reference", "/components/slide"); Text " for slide parts and variants."]
            Heading {id="build"; title="Run"; level=2}
            CodeBlock("shell", "dotnet fve run deck.fsx")
            Paragraph [Text "The command creates these files:"]
            HighlightedCodeBlock("text", None, "My Presentation/\n├── deck.html\n├── deck.css\n└── deck.js", [ 2 ])
            Paragraph [Text "Open "; InlineContent.Code "deck.html"; Text " in your browser. To share the deck, zip the whole folder. The recipient can unzip it and open "; InlineContent.Code "deck.html"; Text " in their browser."]
            Heading {id="printing"; title="Export a PDF"; level=2}
            Paragraph [Text "After running the deck, run this from the same folder with Google Chrome installed on macOS:"]
            CodeBlock("shell", pdfCommand)
            Paragraph [Text "The command writes "; InlineContent.Code "deck.pdf"; Text " with one slide per page. It doesn’t open a browser window or require a server."]
            Paragraph [Text "You can also use your browser’s "; Strong [Text "Print → Save as PDF"]; Text ". Turn off browser headers and footers and enable background graphics."]
            Heading {id="example"; title="Explore the complete example"; level=2}
            Paragraph [Text "See the "; Link("Geldos product overview", "/examples/presentation"); Text " for a complete deck."]
          ] }
