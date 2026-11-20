namespace Docs.ComponentPages.CodeBlock

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

open Docs.Common.BlockExamples

module View =
    let plainCodeBlock =
        fveCodeBlock {
            _language "text"
            "Build succeeded.\n0 warnings\n0 errors"
        }

    let codeBlock =
        fveCodeBlock {
            _language "fsharp"

            """let balances = [125.50M; 84.25M]
let total = balances |> List.sum
printfn "Total: %M" total"""
        }

    let pythonCodeBlock =
        fveCodeBlock {
            _language "python"

            """from decimal import Decimal

balances = [Decimal("125.50"), Decimal("84.25")]
total = sum(balances)
print(f"Total: {total}")"""
        }

    let typescriptCodeBlock =
        fveCodeBlock {
            _language "typescript"

            """type Account = { id: string; balance: number };

const accounts: Account[] = [{ id: "treasury", balance: 125.50 }];
const total = accounts.reduce((sum, account) => sum + account.balance, 0);
console.log(`Total: ${total}`);"""
        }

    let bashCodeBlock =
        fveCodeBlock {
            _language "bash"

            """#!/usr/bin/env bash
set -euo pipefail

for project in Domain Application Server; do
    dotnet build "src/$project/$project.fsproj"
done"""
        }

    let sqlCodeBlock =
        fveCodeBlock {
            _language "sql"

            """select
    account_id,
    balance
from accounts
where currency = 'USD' and balance > 0;"""
        }

    let codeBlockRegistration = registration "code-block" "Code block"

    let examples () =
            [ sample "plainCodeBlock" "Without highlighting" plainCodeBlock
              sample "codeBlock" "F#" codeBlock
              sample "pythonCodeBlock" "Python" pythonCodeBlock
              sample "typescriptCodeBlock" "TypeScript" typescriptCodeBlock
              sample "bashCodeBlock" "Bash" bashCodeBlock
              sample "sqlCodeBlock" "SQL" sqlCodeBlock ]

    let private codeBlockGiraffeSource =
        """open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe
open FSharp.ViewEngine
open Acme.Components
open type Html

let indexView =
    html {
        head {
            title { "Build output" }
            meta { _charset "utf-8" }
            meta { _name "viewport"; _content "width=device-width, initial-scale=1" }
            link { _rel "stylesheet"; _href "/css/output.css" }
            CodeBlock.assets None [] // Copying only; no Prism.
            script { _type "module"; _src "/js/app.js" }
        }
        body {
            for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
            _class "mx-auto max-w-3xl p-6"
            fveCodeBlock { _language "text"; "Build succeeded.\n0 warnings\n0 errors" }
        }
    }

let indexHandler : HttpHandler =
    htmlString (Render.toHtmlDocString indexView)

let builder = WebApplication.CreateBuilder()
builder.Services.AddGiraffe() |> ignore
let app = builder.Build()
app.UseStaticFiles() |> ignore
app.UseGiraffe(GET >=> route "/" >=> indexHandler)
app.Run()"""

    let private codeBlockPage (registration: DocPage) =
        let items = examples ()

        let linkTo href (label: string) =
            a {
                _href href
                _class "font-semibold text-[var(--fve-brand-text)] underline underline-offset-2"
                label
            }

        let code language (source: string) =
            fveCodeBlock {
                _language language
                source
            }

        let guideSection id (title: string) content =
            DocumentationSection.create
                id
                title
                (h2 {
                    _class "m-0 text-xl font-semibold tracking-tight text-[var(--fve-text)]"
                    title
                 }
                 :: content)

        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withLayout Gallery
        |> DocumentationPage.withRightRail TableOfContents
        |> DocumentationPage.withDescription "Copyable code blocks with optional Prism syntax highlighting."
        |> DocumentationPage.withLead
            [ fveExample {
                  _name items.Head.id
                  _label items.Head.title
                  _variant ExampleVariant.Lead
                  _language "fsharp"
                  fveExampleCode { items.Head.source }

                  div {
                      _class "min-w-0 p-6"
                      items.Head.preview
                  }
              } ]
        |> DocumentationPage.withSections
            [ Docs.Common.Gallery.installationSection "code-block"
              guideSection
                  "usage"
                  "Quick start with Giraffe"
                  [ p {
                        "Follow "
                        linkTo "/components/installation" "Installation"
                        " for the shared CSS and JavaScript build."
                    }
                    p {
                        "In a Giraffe web project that references your generated Components project, use this as Program.fs. Change Acme.Components to your installed namespace. It serves a plain, copyable block at /."
                    }
                    code "fsharp" codeBlockGiraffeSource
                    p {
                        "Use the language \"text\" for unhighlighted output. CodeBlock.assets None [] installs copying without downloading any Prism scripts or CSS. Add it once in the document head; Datastar initializes the component and handles the copy action. Copying requires HTTPS or localhost."
                    } ]
              guideSection
                  "syntax-highlighting"
                  "Add syntax highlighting"
                  [ p {
                        "Highlighting is optional and uses "
                        linkTo "https://prismjs.com/" "Prism"
                        ". Open the "
                        linkTo "https://prismjs.com/download.html" "Prism download builder"
                        ", select F#, Python, TypeScript, Bash and SQL, and keep the automatically selected language dependencies. Download the JavaScript bundle and save it as wwwroot/js/prism.js."
                    }
                    p { "Replace the CodeBlock.assets line in the quick start with:" }
                    code "fsharp" "CodeBlock.assets None [\"/js/prism.js\"]"
                    p { "Then choose a matching language when creating a block:" }
                    code
                        "fsharp"
                        """fveCodeBlock {
    _language "fsharp"
    "let total = 125.50M + 84.25M"
}"""
                    p {
                        "The runtime loads the bundle when it initializes code and reapplies highlighting after Datastar patches. No Prism stylesheet is needed: the component's Tailwind styles supply light/dark token colors. Keep using \"text\" for blocks that should remain unhighlighted."
                    } ]
              guideSection
                  "prism-theme"
                  "Optional Prism stylesheet"
                  [ p {
                        "To use a Prism theme instead of the built-in colors, select a theme in the "
                        linkTo "https://prismjs.com/download.html" "download builder"
                        ", download the CSS, and save it as wwwroot/css/prism.css. Replace the assets call with:"
                    }
                    code "fsharp" "CodeBlock.assets (Some \"/css/prism.css\") [\"/js/prism.js\"]"
                    p {
                        "A Prism theme replaces token colors; it does not change the component's code surface or automatically switch between light and dark palettes."
                    } ]
              for item in items.Tail do
                  DocumentationSection.create
                      item.id
                      item.title
                      [ fveExample {
                            _name item.id
                            _label item.title
                            _variant ExampleVariant.Gallery
                            _language "fsharp"
                            fveExampleCode { item.source }

                            div {
                                _class "min-w-0 p-6"
                                item.preview
                            }
                        } ]
              guideSection
                  "nonce-csp"
                  "Optional nonce CSP"
                  [ p {
                        "If your host enforces nonce CSP, expose its fresh document nonce on the html element's data-nonce attribute, and pass it to assetsWithNonce and the Datastar script. That same nonce must be authorized by the response's script-src policy:"
                    }
                    code
                        "fsharp"
                        """let codeAssets nonce =
    fragment {
        CodeBlock.assetsWithNonce None ["/js/prism.1.30.0.min.js"] (Some nonce)
        script { _type "module"; _src "/js/app.js"; _attr("nonce", nonce) }
    }"""
                    p {
                        "For nonce generation, response headers and enhanced-navigation behavior, see the "
                        linkTo "/extensions/datastar#content-security-policy" "Datastar CSP guide"
                        "."
                    } ]
              guideSection
                  "api-reference"
                  "API reference"
                  [ fveFSharpApiReference { yield! (FSharpApiReference.forCategory "code-block" (typeof<Size>)) } ] ]

    let page = codeBlockPage codeBlockRegistration
