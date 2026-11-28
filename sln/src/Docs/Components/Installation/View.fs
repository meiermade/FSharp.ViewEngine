namespace Docs.ComponentPages.Installation

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.Gallery
open Docs.Common.SharedExamples

open Docs.Common.Registration

module View =
    let installationRegistration =
        packageRegistration "components-installation" "/components/installation" "Installation" "Installation"

    let installationPage =
        DocumentationPage.create installationRegistration.id installationRegistration.title
        |> DocumentationPage.withDescription
            "Install editable components with the CLI, then build their Tailwind stylesheet and browser modules."
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "tool"
                  "Install the CLI"
                  [ p {
                        "For NuGet setup and supported .NET versions, see "
                        fveLink { _href "/installation"; _underline LinkUnderline.Always; "Core installation" }
                        "."
                    }
                    prose
                        "Start with a .NET web project and Node.js LTS with npm. Run these commands from your repository root:"
                    code
                        "shell"
                        "dotnet new tool-manifest\ndotnet tool install FSharp.ViewEngine.Cli\ndotnet tool restore" ]
              DocumentationSection.create
                  "project"
                  "Initialize and install"
                  [ code "shell" "dotnet fve init src/Acme.Components/Acme.Components.fsproj --frontend-root ../Acme.Web\ndotnet fve add button field input --config src/Acme.Components/fve.json\ndotnet add src/Acme.Web/Acme.Web.fsproj reference src/Acme.Components/Acme.Components.fsproj"
                    prose "The CLI installs editable source. Init records your web root and paths to package.json, app.js, input.css, and the compiled outputs in fve.json. It creates missing inputs and adds the required build dependencies. Edit those paths if your project uses a different layout." ]
              DocumentationSection.create
                  "runtime"
                  "Customize your frontend"
                  [ prose "Your app.js imports the FVE-generated imports file once. Installing a component updates that file. Add your own browser code to app.js and put Tailwind sources, themes, and plugins in input.css. The installed runtime includes Datastar; don't load another copy."
                    p {
                        "FVE compiles styles with "
                        fveLink { _href "https://tailwindcss.com/"; _underline LinkUnderline.Always; "Tailwind CSS" }
                        " and uses "
                        fveLink { _href "https://data-star.dev/"; _underline LinkUnderline.Always; "Datastar" }
                        " for browser interactions."
                    }
                    prose "Run npm install in the directory containing package.json, then commit the manifest and lock file. Use npm ci for subsequent installs." ]
              DocumentationSection.create
                  "build"
                  "Compile and watch"
                  [ code "shell" "dotnet fve compile --config src/Acme.Components/fve.json\ndotnet fve watch --config src/Acme.Components/fve.json"
                    prose "Compile writes CSS and JavaScript to the configured paths. Watch rebuilds those assets; run dotnet watch separately for the server." ]
              DocumentationSection.create
                  "namespace"
                  "Serve and use the components"
                  [ prose "Enable static files in your host (for example, app.UseStaticFiles() with ASP.NET Core). Load the stylesheet and, for interactive components, the module once. Apply a component theme to the owning region:"
                    fveCodeBlock {
                        _language "fsharp"
                        _highlightLines [ 11; 12 ]
                        """open FSharp.ViewEngine
open Acme.Components
open type Html

let page =
    html {
        head {
            title { "My application" }
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            link { _rel "stylesheet"; _href "/css/output.css" }
            script { _type "module"; _src "/js/app.js" }
        }
        body {
            for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
            fveButton { _color Color.Primary; _variant ButtonVariant.Solid; "Save" }
        }
    }"""
                    }
                    p {
                        "See the "
                        fveLink { _href "/examples/application"; _native true; _underline LinkUnderline.Always; "full Application example" }
                        " to try the components in a running app and browse its source."
                    } ]
              DocumentationSection.create
                  "updating"
                  "Update installed components"
                  [ code
                        "shell"
                        "dotnet tool update FSharp.ViewEngine.Cli\ndotnet fve diff --config src/Acme.Components/fve.json"
                    prose
                        "The tool manifest pins the registry version. Review the diff before updating installed source: fve add preserves local edits; --overwrite replaces them only when you explicitly choose it. Commit the tool manifest and installed source together." ] ]
