namespace Docs.Pages

open Docs.Common

module Scripting =
    let page =
        { id = "standalone-scripts"
          path = "/guides/scripts"
          aliases = ["/getting-started/scripts"]
          navLabel = "Standalone F# scripts"
          category = "Guides"
          title = "Standalone F# scripts"
          browserTitle = "Standalone F# scripts · FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "Author HTML in F# and use "; InlineContent.Code "dotnet fve run page.fsx"; Text " to render the page, compile its styles and JavaScript, and print the generated path. No web server is required." ]
            Heading {id="prerequisites"; title="Prerequisites"; level=2}
            Paragraph [ Text "Use a .NET 11 SDK and Node.js 20 or newer with npm for CLI builds. FVE installs and caches its pinned Tailwind and esbuild tools automatically; no per-page npm project or manual frontend-tool installation is needed." ]
            Paragraph [ Text "SDK 10 and 11 are actively tested for script execution; use SDK 11 for new scripts. SDK selection is separate from Core’s net8.0 compatibility asset. If a parent directory selects an older SDK, select SDK 11 in a script-local global.json before running the script." ]
            Heading {id="folder"; title="Create a folder for your script"; level=2}
            Paragraph [Text "From the parent directory where you want to keep the page, create a folder and enter it. Run the remaining commands inside that folder so your script, setup files, and generated output stay together:"]
            CodeBlock("shell", "mkdir \"My Standalone Page\"\ncd \"My Standalone Page\"")
            Paragraph [Text "Choose the Core-only workflow for a plain HTML page, or skip to "; Link("the CLI workflow", "#components"); Text " for editable components, CSS, and JavaScript."]
            Heading {id="core"; title="Use the Core package"; level=2}
            Paragraph [Text "Save this script as "; InlineContent.Code "page.fsx"; Text " in your new folder. It references Core directly and renders ordinary typed HTML:"]
            CodeBlock("fsharp", """#r "nuget: FSharp.ViewEngine, 2026.8.2"
open System.IO
open FSharp.ViewEngine
open type Html

let document =
    html {
        _lang "en"
        head { title { "A standalone page" } }
        body { h1 { "Built with F#" } }
    }

File.WriteAllText(Path.Combine(__SOURCE_DIRECTORY__, "page.html"), Render.toHtmlDocString document)""")
            CodeBlock("shell", "dotnet fsi page.fsx")
            Heading {id="components"; title="Install editable components into a script"; level=2}
            Paragraph [Text "In your script folder, pin the SDK, install the CLI as a local tool, and initialize the script with the Card component:"]
            CodeBlock("shell", """dotnet new globaljson --sdk-version 11.0.100 --roll-forward latestFeature
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init page.fsx
dotnet fve add card""")
            Paragraph [Text "Setup creates "; InlineContent.Code "page.fsx"; Text ", "; InlineContent.Code "input.css"; Text ", and "; InlineContent.Code "fve.json"; Text ", and installs editable Card source into "; InlineContent.Code "Components/"; Text "."]
            Heading {id="page"; title="Compose the HTML page"; level=2}
            Paragraph [Text "The CLI manages the header between the highlighted lines. Write your code below it."]
            ScriptFile("page.fsx", """// fve:components:start
#r "nuget: FSharp.ViewEngine, 2026.8.2"
#load "Components/ComponentBuilder.fs" "Components/Foundation.fs" "Components/Card.fs"
open page
// fve:components:end

open System.IO
open FSharp.ViewEngine
open type Html

let document =
    html {
        _lang "en"
        head {
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            title { "A standalone page" }
            link { _rel "stylesheet"; _href "page.css" }
        }
        body {
            _class (ComponentsTheme.className ComponentsTheme.sky + " m-0 bg-[var(--fve-background)] p-6 text-[var(--fve-text)]")
            main {
                _class "mx-auto max-w-xl"
                fveCard {
                    fveCardHeader {
                        h1 { _class "m-0 text-2xl font-semibold"; "Built with F#" }
                    }
                    p { "This page uses editable component source and opens directly from disk." }
                }
            }
            script { _src "page.js" }
        }
    }

File.WriteAllText(Path.Combine(__SOURCE_DIRECTORY__, "page.html"), Render.toHtmlDocString document)""")
            Heading {id="styles"; title="Build and view"; level=2}
            Paragraph [Text "Run this from your script folder to render the page, compile input.css, and bundle the installed components’ browser modules:"]
            CodeBlock("shell", "dotnet fve run page.fsx")
            Paragraph [Text "The CLI prints the generated paths. Open "; InlineContent.Code "page.html"; Text " in your browser."]
            Paragraph [Text "Init creates input.css with these source scans. Keep it editable; add @source entries for shared files outside the script directory when needed:"]
            CodeBlock("css", """@import "tailwindcss" source(none);
@source "./**/*.fs";
@source "./**/*.fsx";
@source "./Components/Assets/*.js";""")
            Paragraph [Text "Treat page.html, page.css and page.js as generated outputs. The command uses your installed component source without reinstalling or upgrading it. Keep the generated assets, local images and fonts beside the HTML and use relative paths. Application projects continue to use dotnet build."]
            Heading {id="ownership"; title="Keep source ownership explicit"; level=2}
            Paragraph [Text "The component namespace defaults to the script name ("; InlineContent.Code "page"; Text " here). Use "; InlineContent.Code "--namespace MyApp.Components"; Text " at init to override it. When initializing an existing script, remove its manual FSharp.ViewEngine "; InlineContent.Code "#r"; Text " first so the CLI can manage the reference."]
            CodeBlock("shell", """dotnet fve diff
dotnet fve add card --overwrite""")
            Paragraph [Text "Use diff before updating. Reinstalling without overwrite preserves edited component source; changing or removing a managed script block fails before any source writes. Use overwrite only when replacing that managed content is intended. Rendered script values are encoded as normal FVE text; raw remains an explicit trusted-markup escape hatch."]
            Heading {id="next"; title="Next steps"; level=2}
            Paragraph [Link("Build a presentation and print it to PDF", "/guides/presentations"); Text " · "; Link("Use a shared theme", "/guides/theming"); Text " · "; Link("Serve HTML with Giraffe", "/guides/giraffe")]
          ] }
