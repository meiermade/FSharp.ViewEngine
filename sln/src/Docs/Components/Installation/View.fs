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
                        fveLink { _href "/installation"; "Core installation" }
                        "."
                    }
                    prose
                        "Start with a .NET web project and Node.js LTS with npm. Run these commands from your repository root:"
                    code
                        "shell"
                        "dotnet new tool-manifest\ndotnet tool install FSharp.ViewEngine.Cli\ndotnet tool restore" ]
              DocumentationSection.create
                  "project"
                  "Add components"
                  [ code
                        "shell"
                        "dotnet fve init src/Acme.Components/Acme.Components.fsproj --namespace Acme.Components\ndotnet fve add button field input --config src/Acme.Components/fve.json\ndotnet add src/Acme.Web/Acme.Web.fsproj reference src/Acme.Components/Acme.Components.fsproj"
                    prose
                        "The CLI copies F# source, component JavaScript and required runtime assets. Commit the installed source, fve.json and tool manifest." ]
              DocumentationSection.create
                  "npm"
                  "Install the build tools"
                  [ p {
                        "Install "

                        fveLink {
                            _href "https://tailwindcss.com/docs/installation/tailwind-cli"
                            "Tailwind CSS"
                        }

                        ", its CLI and Typography plugin, and "

                        fveLink {
                            _href "https://esbuild.github.io/"
                            "esbuild"
                        }

                        " together in your web project:"
                    }
                    code
                        "shell"
                        "cd src/Acme.Web\nnpm init -y\nnpm install --save-dev tailwindcss@4 @tailwindcss/cli@4 @tailwindcss/typography esbuild"
                    prose
                        "Run the remaining npm commands from this directory. Commit package.json and package-lock.json; use npm ci on subsequent installs." ]
              DocumentationSection.create
                  "tailwind"
                  "Configure Tailwind"
                  [ prose
                        "Create src/Acme.Web/input.css. Scan your views and the installed F# and JavaScript so Tailwind includes the components' styles:"
                    code "css" tailwindExample ]
              DocumentationSection.create
                  "runtime"
                  "Import component JavaScript"
                  [ prose
                        "Create src/Acme.Web/js/app.js. Import the shared runtime once, then add the imports shown in each component's Installation section. For Button:"
                    code
                        "javascript"
                        "import '../../Acme.Components/Components/Assets/datastar-rocket.1.0.4.js';\nimport '../../Acme.Components/Components/Assets/button.js';"
                    p {
                        "The installed Rocket runtime includes "

                        fveLink {
                            _href "https://data-star.dev/"
                            "Datastar"
                        }

                        ". No separate Datastar installation or script is needed."
                    }
                    prose "If none of your installed components need JavaScript, skip the module entry, JavaScript build, and script tag." ]
              DocumentationSection.create
                  "build"
                  "Build and watch"
                  [ prose "Keep build inputs outside wwwroot. Serve only browser-ready files from wwwroot/js, wwwroot/css, wwwroot/fonts and wwwroot/images. This recipe uses esbuild; browser-ready modules can also be served directly. Add these scripts to your web project's package.json:"
                    code
                        "json"
                        """{
  "scripts": {
    "build:css": "tailwindcss -i input.css -o wwwroot/css/output.css --minify",
    "build:js": "esbuild js/app.js --bundle --format=esm --splitting --outdir=wwwroot/js --minify",
    "watch:css": "npm run build:css -- --watch",
    "watch:js": "npm run build:js -- --watch"
  }
}"""
                    code "shell" "npm run build:css\nnpm run build:js"
                    prose
                        "During development, run npm run watch:css and npm run watch:js in separate terminals alongside your .NET host." ]
              DocumentationSection.create
                  "namespace"
                  "Serve and use the components"
                  [ prose "Enable static files in your host (for example, app.UseStaticFiles() with ASP.NET Core). Load the stylesheet and, for interactive components, the module once. Apply a component theme to the owning region:"
                    code
                        "fsharp"
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
                    p {
                        "See the "
                        fveLink { _href "/examples/application"; "full Application example" }
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
