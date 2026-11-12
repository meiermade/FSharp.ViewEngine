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
            Paragraph [ Text "Author HTML in F# and use "; InlineContent.Code "dotnet fve build"; Text " to render the page, compile its styles and JavaScript, and open it in your browser. No web server is required." ]
            Heading {id="prerequisites"; title="Prerequisites"; level=2}
            Paragraph [ Text "Use a .NET 10 SDK and Node.js 20 or newer with npm for CLI builds. FVE installs and caches its pinned Tailwind and esbuild tools automatically; no per-page npm project or manual frontend-tool installation is needed." ]
            Paragraph [ Text "The current Core package and installed source use newer FSharp.Core APIs, so .NET 8 FSI is not supported. This does not change .NET 8/9/10 project-consumer support. If a parent directory selects an older SDK, select .NET 10 in a script-local global.json before running the script." ]
            Heading {id="core"; title="Use the Core package"; level=2}
            Paragraph [Text "A script can reference the pinned Core package directly and render ordinary typed HTML:"]
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
            Paragraph [Text "Use a local tool manifest to pin the CLI. Run these commands in a new directory. Script consumers use the same registry, dependency resolution and editable-source protections as project consumers:"]
            CodeBlock("shell", """dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
dotnet new tool-manifest
dotnet tool install FSharp.ViewEngine.Cli
dotnet fve init page.fsx
dotnet fve add card""")
            Paragraph [Text "The CLI creates page.fsx and fve.json, pins the Core reference, and writes one dependency-ordered #load directive in a managed block. Keep that block; it opens the configured component namespace. The namespace defaults to the script filename without its extension (page here), with hyphens and spaces replaced by underscores. Use the optional --namespace MyApp.Components override only when you want a different source namespace. Installation does not compile a DLL or require an .fsproj. A new script also gets an editable input.css and matching CSS/JavaScript references in its HTML shell. Build does not rewrite those authored inputs. Existing content outside the block is preserved. When initializing an existing script, remove its old manual FSharp.ViewEngine #r first so the CLI can own that single pinned reference."]
            Heading {id="page"; title="Compose the HTML page"; level=2}
            Paragraph [Text "Replace the starter document below the managed block with ordinary HTML and an editable Card component:"]
            CodeBlock("fsharp", """open System.IO
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
            Paragraph [Text "Build renders the script, compiles input.css, and bundles the installed components’ browser modules into matching HTML, CSS and JavaScript files. Use --open to view the finished HTML in your default browser:"]
            CodeBlock("shell", "dotnet fve build --open")
            Paragraph [Text "Init creates input.css with these source scans. Keep it editable; add @source entries for shared files outside the script directory when needed:"]
            CodeBlock("css", """@import "tailwindcss" source(none);
@source "./**/*.fs";
@source "./**/*.fsx";
@source "./Components/Assets/*.js";""")
            Paragraph [Text "The command reads the entry script from fve.json; use --config path/to/fve.json when building from another directory. Treat page.html, page.css and page.js as generated outputs. Build uses edited installed components without reinstalling or upgrading them. Keep the generated assets, local images and fonts beside the HTML and use relative paths. Application projects continue to use dotnet build."]
            Heading {id="ownership"; title="Keep source ownership explicit"; level=2}
            CodeBlock("shell", """dotnet fve diff
dotnet fve add card --overwrite""")
            Paragraph [Text "Use diff before updating. Reinstalling without overwrite preserves edited component source; changing or removing a managed script block fails before any source writes. Use overwrite only when replacing that managed content is intended. Rendered script values are encoded as normal FVE text; raw remains an explicit trusted-markup escape hatch."]
            Heading {id="next"; title="Next steps"; level=2}
            Paragraph [Link("Build a presentation and print it to PDF", "/guides/presentations"); Text " · "; Link("Use a shared theme", "/guides/theming"); Text " · "; Link("Serve HTML with Giraffe", "/guides/giraffe")]
          ] }
