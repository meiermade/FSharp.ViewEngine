namespace Docs.Pages

open Docs.Common

module Presentations =
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
            Paragraph [Text "For a standalone deck, complete the SDK, Node.js and local CLI setup in "; Link("Standalone F# scripts", "/guides/scripts"); Text " first. In a new directory, initialize a script and install Presentation with its dependencies:"]
            CodeBlock("shell", """dotnet fve init deck.fsx
dotnet fve add presentation""")
            Paragraph [Text "Keep the generated managed block at the top of deck.fsx. It pins Core, loads the editable component source, and opens the filename-derived namespace (deck here). The optional --namespace override is only needed for a different source namespace. For an application project, install the same components into its components project and return the deck through your normal "; Link("Giraffe handler", "/guides/giraffe"); Text ". Neither Slide nor Presentation requires a particular web framework."]
            Heading {id="composition"; title="Compose slides with ordinary F#"; level=2}
            Paragraph [Text "Replace the starter document below the managed block with the following. Each slide owns its branding and footer; an ordinary function shares those choices without a second configuration API:"]
            CodeBlock("fsharp", """open System.IO
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

File.WriteAllText(Path.Combine(__SOURCE_DIRECTORY__, "deck.html"), Render.toHtmlDocString document)""")
            Paragraph [Text "SlideHeader groups the title and subtitle, SlideContent owns the body, and SlideFooter and SlideBranding provide slide-local metadata. Presentation numbers slides in source order. Use any ordinary F# composition, encoded HTML, images or components inside those regions. The "; Link("Slide reference", "/components/slide"); Text " documents the parts and variants."]
            Heading {id="build"; title="Build and open"; level=2}
            CodeBlock("shell", "dotnet fve build --open")
            Paragraph [Text "The CLI renders deck.fsx, compiles input.css and bundles the installed browser modules into deck.html, deck.css and deck.js. The generated script stylesheet already scans the local F# and component source; see "; Link("script build setup", "/guides/scripts#styles"); Text " for shared source directories and build configuration. No server, separate npm project or per-deck viewer JavaScript is needed. Use relative paths for local fonts and images, and keep the generated assets beside the HTML when sharing it."]
            Heading {id="viewing"; title="Present in the browser"; level=2}
            Paragraph [Text "The normal view is a separated stack with only an Expand action. Each canvas keeps its 16:9 composition between 48rem and 80rem wide; narrow screens pan horizontally rather than shrinking the text. Expand fits one existing slide to the viewport without rebuilding its content. The expanded toolbar provides Previous/Next, position and Restore."]
            UnorderedList [
                [Text "Use Left/Right to move between slides, or Home/End for the first/last slide."]
                [Text "Use Escape or Restore to return to the stack at the current slide."]
                [Text "Inputs and nested components keep their state while expanding or restoring."]
                [Text "Without JavaScript, the separated stack and source-order numbering remain readable."] ]
            Paragraph [Text "Set --fve-slide-cover-background, --fve-slide-cover-text and --fve-slide-cover-muted on the owning region to customize the cover palette. Standard slides inherit the component theme. See "; Link("Theming", "/guides/theming"); Text " for shared palettes, size and appearance choices. The "; Link("Presentation reference", "/components/presentation"); Text " covers the focused viewer contract."]
            Heading {id="printing"; title="Print or save as PDF"; level=2}
            OrderedList [
                [Text "Open the finished HTML in your browser and check every slide in the intended theme."]
                [Text "Choose Print (Cmd+P on macOS or Ctrl+P on Windows/Linux)."]
                [Text "Choose Save as PDF or your printer. Keep browser headers and footers off, and enable background graphics if your browser offers that setting."]
                [Text "Check the preview: one slide per page, with complete titles, content, branding, footers and numbering. Then save or print."] ]
            Paragraph [Text "The components supply full-bleed 16:9 pages at 1280×720 CSS pixels (960×540 PDF points). Viewer controls, the stack backdrop and surrounding host chrome are omitted; slide colors and the active theme are preserved. A physical printer can fit the pages to its printable paper area. No PowerPoint or separate PDF converter is required."]
            Paragraph [Text "Printing from the expanded viewer temporarily returns the content to normal document flow and restores the same slide and focus afterward. Print layout also works without JavaScript. Scrolling is only a screen behavior: split overflowing content across slides before printing rather than relying on a scrollable body."]
            Heading {id="example"; title="Explore the complete example"; level=2}
            Paragraph [Text "The "; Link("Geldos treasury review", "/examples/presentation"); Text " is a complete financial deck using the shared example model. Its editable source in "; InlineContent.Code "sln/src/Example/Presentation"; Text " shows how a web-hosted page and a standalone script share the same F# deck."]
          ] }
