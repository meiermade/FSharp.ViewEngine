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

    let downloadableCodeBlock =
        fveCodeBlock {
            _fileName "src/Balance.fsx"
            _language "fsharp"
            _lineNumbers true
            _highlightLines [ 3; 4 ]
            _download true
            """let balances = [125.50M; 84.25M]

let total = balances |> List.sum
printfn "Total: %M" total"""
        }

    let highlightedCodeBlock =
        fveCodeBlock {
            _language "fsharp"
            _highlightLines [ 2 ]
            """let balances = [125.50M; 84.25M]
let total = balances |> List.sum
printfn "Total: %M" total"""
        }

    let codeBlockRegistration = registration "code-block" "Code block"

    let examples () =
            [ sample "plainCodeBlock" "Without highlighting" plainCodeBlock
              sample "codeBlock" "F#" codeBlock
              sample "pythonCodeBlock" "Python" pythonCodeBlock
              sample "typescriptCodeBlock" "TypeScript" typescriptCodeBlock
              sample "bashCodeBlock" "Bash" bashCodeBlock
              sample "sqlCodeBlock" "SQL" sqlCodeBlock
              sample "downloadableCodeBlock" "Named file with line numbers and download" downloadableCodeBlock
              sample "highlightedCodeBlock" "Highlighted lines without numbers" highlightedCodeBlock ]

    let private codeBlockPage (registration: DocPage) =
        let items = examples ()

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
        |> DocumentationPage.withDescription "Copyable code blocks with optional filenames, downloads, line numbers, and highlighting."
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
                  "Usage"
                  [ fveCodeBlock {
                        _language "fsharp"
                        """fveCodeBlock {
    _language "fsharp"
    "let total = 125.50M + 84.25M"
}"""
                    }
                    p { "Set _language for syntax highlighting, or use \"text\" for plain output." } ]
              guideSection
                  "file-and-lines"
                  "Files and line annotations"
                  [ p {
                        "Use _fileName to show a filename or path above the code. Add _download true for complete files; it saves the source using the last part of the path (or snippet.txt without a filename). Copy and Download omit all visual annotations."
                    }
                    p {
                        "Use _lineNumbers true to show a gutter and _highlightLines with 1-based line numbers to emphasize specific lines. Highlighted lines work without the gutter or Prism."
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
                  "api-reference"
                  "API reference"
                  [ fveFSharpApiReference { yield! (FSharpApiReference.forCategory "code-block" (typeof<Size>)) } ] ]

    let page = codeBlockPage codeBlockRegistration
