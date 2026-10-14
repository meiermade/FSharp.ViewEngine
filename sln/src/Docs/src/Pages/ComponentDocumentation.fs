namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

/// Dedicated pages for general-purpose blocks formerly hidden in framework bundles.
module ComponentDocumentation =
    let card = Card.create (p { _class "text-sm"; "Project notes and supporting files." }) |> Card.render
    let cardWithParts =
        Card.create (p { _class "text-sm text-[var(--fve-muted-text)]"; "Review the selected items before publishing." })
        |> Card.withHeader (div { _class "grid gap-1"; h3 { _class "text-base font-semibold"; "Project review" }; p { _class "text-sm text-[var(--fve-muted-text)]"; "Latest updates" } })
        |> Card.withFooter (Button.create (ButtonContent.Text "Publish") |> Button.disabled |> Button.render)
        |> Card.render
    let mediaCard =
        Card.create (p { _class "text-sm text-[var(--fve-muted-text)]"; "Share this artwork with your team." })
        |> Card.withMedia (img { _src "/social-card.png"; _alt "FSharp.ViewEngine — Typed HTML views for F#"; _class "aspect-video w-full object-contain" })
        |> Card.withHeader (h3 { _class "text-base font-semibold"; "Social card" })
        |> Card.withSize CardSize.Small |> Card.render
    let sectionHeader = SectionHeader.create "Item details" |> SectionHeader.render
    let sectionHeaderWithActions =
        SectionHeader.create "Recent activity" |> SectionHeader.withDescription "Updates from this month."
        |> SectionHeader.withActions (Button.create (ButtonContent.Text "Export") |> Button.disabled |> Button.render)
        |> SectionHeader.withDivider |> SectionHeader.render
    let fields =
        FieldGroup.create "Delivery preferences" (div { _class "grid gap-3"; Checkbox.create "emailUpdates" "Email updates" |> Checkbox.render; Checkbox.create "printedUpdates" "Printed updates" |> Checkbox.render })
        |> FieldGroup.withDescription "Choose how to receive updates." |> FieldGroup.render
    let mermaidHead pageNonce = Mermaid.assetsWithNonce "/scripts/mermaid.11.16.0.min.js" (Some pageNonce)
    let plainCodeBlock =
        CodeBlock.create "text" "Build succeeded.\n0 warnings\n0 errors"
        |> CodeBlock.render
    let codeBlock =
        CodeBlock.create "fsharp" """let balances = [125.50M; 84.25M]
let total = balances |> List.sum
printfn "Total: %M" total"""
        |> CodeBlock.render
    let pythonCodeBlock =
        CodeBlock.create "python" """from decimal import Decimal

balances = [Decimal("125.50"), Decimal("84.25")]
total = sum(balances)
print(f"Total: {total}")"""
        |> CodeBlock.render
    let typescriptCodeBlock =
        CodeBlock.create "typescript" """type Account = { id: string; balance: number };

const accounts: Account[] = [{ id: "treasury", balance: 125.50 }];
const total = accounts.reduce((sum, account) => sum + account.balance, 0);
console.log(`Total: ${total}`);"""
        |> CodeBlock.render
    let bashCodeBlock =
        CodeBlock.create "bash" """#!/usr/bin/env bash
set -euo pipefail

for project in Domain Application Server; do
    dotnet build "src/$project/$project.fsproj"
done"""
        |> CodeBlock.render
    let sqlCodeBlock =
        CodeBlock.create "sql" """select
    account_id,
    balance
from accounts
where currency = 'USD' and balance > 0;"""
        |> CodeBlock.render
    let callout = Callout.create "Note" [p { "Keep a copy of these instructions for future reference." }] |> Callout.render
    let mermaid = Mermaid.create "flowchart LR\n  Input[Input] --> Validate[Validate]\n  Validate --> Result[Result]" |> Mermaid.render
    let sequence =
        let user = SequenceDiagram.participant "user" "User"
        let server = SequenceDiagram.participant "server" "Server"
        SequenceDiagram.sequence [user; server] [SequenceDiagram.call user server "Validate form"; SequenceDiagram.reply server user "Result"]
        |> SequenceDiagram.render |> Mermaid.create |> Mermaid.render
    let example = Example.gallery "nested-example" "Greeting" "fsharp" "p { \"Hello\" }" (p { "Hello" })
    let fsharpReference = FSharpApiReference.create [typeof<FSharpApiReferenceConfig>; typeof<CodeBlockConfig>] |> FSharpApiReference.render

    let private registration key title : DocPage =
        { id="components-"+key; path="/components/"+key; aliases=(if key="section-header" then ["/components/section"] else []); navLabel=title; category="Components"; title=title; browserTitle=title+" · FSharp.ViewEngine.Components"; nodes=[] }
    let cardRegistration = registration "card" "Card"
    let sectionHeaderRegistration = registration "section-header" "Section header"
    let fieldGroupRegistration = registration "field-group" "Field group"
    let codeBlockRegistration = registration "code-block" "Code block"
    let calloutRegistration = registration "callout" "Callout"
    let mermaidRegistration = registration "mermaid" "Mermaid"
    let exampleRegistration = registration "example" "Example"
    let fsharpReferenceRegistration = registration "fsharp-api-reference" "F# API reference"
    let registrations = [cardRegistration; sectionHeaderRegistration; fieldGroupRegistration; codeBlockRegistration; calloutRegistration; mermaidRegistration; exampleRegistration; fsharpReferenceRegistration]
    let private sourceText = lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.ComponentDocumentation.fs")
    let private sampleNames names name label preview : Components.ComponentExample =
        { id="components-"+name; title=label; source="open FSharp.ViewEngine\nopen Acme.Components\nopen type Html\n\n"+(SourceRegion.declarations names sourceText.Value).Replace("FSharp.ViewEngine.Components", "Acme.Components"); preview=preview; note=None }
    let private sample name label preview = sampleNames [name] name label preview
    let examples key =
        match key with
        | "card" -> [sample "card" "Default" card; sample "cardWithParts" "Header and footer" cardWithParts; sample "mediaCard" "With media" mediaCard]
        | "section-header" -> [sample "sectionHeader" "Default" sectionHeader; sample "sectionHeaderWithActions" "Description, actions and divider" sectionHeaderWithActions]
        | "field-group" -> [sample "fields" "Related fields" fields]
        | "code-block" ->
            [ sample "plainCodeBlock" "Without highlighting" plainCodeBlock
              sample "codeBlock" "F#" codeBlock
              sample "pythonCodeBlock" "Python" pythonCodeBlock
              sample "typescriptCodeBlock" "TypeScript" typescriptCodeBlock
              sample "bashCodeBlock" "Bash" bashCodeBlock
              sample "sqlCodeBlock" "SQL" sqlCodeBlock ]
        | "callout" -> [sample "callout" "Supporting information" callout]
        | "mermaid" -> [sampleNames ["mermaidHead";"mermaid"] "mermaid" "Flowchart" mermaid; sample "sequence" "Sequence constructors" sequence]
        | "example" -> [sample "example" "Preview and code" example]
        | "fsharp-api-reference" -> [sample "fsharpReference" "Compiled declarations" fsharpReference]
        | _ -> []
    let allExamples () = registrations |> List.collect (fun item -> examples (item.path.Substring("/components/".Length)))
    let private pageWithExamples (registration:DocPage) (items:Components.ComponentExample list) =
        let key = registration.path.Substring("/components/".Length)
        let surface (content:HtmlElement) = div { _class "min-w-0 p-6"; content }
        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withLayout Gallery |> DocumentationPage.withRightRail TableOfContents
        |> DocumentationPage.withDescription "An independently installable component. Its content and surrounding layout remain consumer-owned."
        |> DocumentationPage.withLead [Example.lead items.Head.id items.Head.title "fsharp" items.Head.source (surface items.Head.preview)]
        |> DocumentationPage.withSections [
            Components.installationSection key
            DocumentationSection.create "usage" "Usage" [
                CodeBlock.create "fsharp" items.Head.source |> CodeBlock.render
                if key="field-group" then
                    CodeBlock.create "shell" "dotnet fve add checkbox --config src/Acme.Components/fve.json" |> CodeBlock.render
                if key="card" || key="section-header" then
                    p { "The action variants additionally compose Button. Install button when copying those examples; the component itself accepts ordinary consumer-authored HTML." }
                if key="example" then
                    p {
                        "For highlighting and copying, include the runtime once in your document head. Follow the "
                        a { _href "/components/code-block#usage"; _class "font-semibold text-[var(--fve-brand-text)] underline underline-offset-2"; "Code block setup guide" }
                        " for a complete Giraffe example and optional Prism assets."
                    }
                if key="mermaid" then
                    p { "Include Mermaid.assets once in your document head with your consumer-owned script URL, or assetsWithNonce using your server-issued document nonce. Initialization and the lazy runtime share that nonce. Sequence constructors belong to this component. Only render trusted diagrams; CSP does not sanitize source or authorize arbitrary URLs." } ]
            for item in items.Tail do DocumentationSection.create item.id item.title [Example.gallery item.id item.title "fsharp" item.source (surface item.preview)]
            DocumentationSection.create "api-reference" "API reference" [FSharpApiReference.forCategory key typeof<CardConfig> |> FSharpApiReference.render] ]
    let private codeBlockGiraffeSource = """open Microsoft.AspNetCore.Builder
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
            script { _type "module"; _src "/scripts/datastar.js" }
        }
        body {
            for attribute in ComponentsTheme.attributes ComponentsTheme.sky do attribute
            _class "mx-auto max-w-3xl p-6"
            CodeBlock.create "text" "Build succeeded.\n0 warnings\n0 errors"
            |> CodeBlock.render
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

    let private codeBlockPage (registration:DocPage) =
        let items = examples "code-block"
        let linkTo href (label:string) = a { _href href; _class "font-semibold text-[var(--fve-brand-text)] underline underline-offset-2"; label }
        let code language source = CodeBlock.create language source |> CodeBlock.render
        let guideSection id (title:string) content =
            DocumentationSection.create id title (
                h2 { _class "m-0 text-xl font-semibold tracking-tight text-[var(--fve-text)]"; title } :: content)
        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withLayout Gallery
        |> DocumentationPage.withRightRail TableOfContents
        |> DocumentationPage.withDescription "Copyable code blocks with optional Prism syntax highlighting."
        |> DocumentationPage.withLead [Example.lead items.Head.id items.Head.title "fsharp" items.Head.source (div { _class "min-w-0 p-6"; items.Head.preview })]
        |> DocumentationPage.withSections [
            Components.installationSection "code-block"
            guideSection "usage" "Quick start with Giraffe" [
                p { "Save your compiled stylesheet as wwwroot/css/output.css. Download Datastar for the copy button:" }
                code "bash" """mkdir -p wwwroot/scripts wwwroot/css
curl -fsSL \
  https://cdn.jsdelivr.net/gh/starfederation/datastar@v1.0.4/bundles/datastar.js \
  -o wwwroot/scripts/datastar.js"""
                p { "In a Giraffe web project that references your generated Components project, use this as Program.fs. Change Acme.Components to your installed namespace. It serves a plain, copyable block at /." }
                code "fsharp" codeBlockGiraffeSource
                p { "Use the language \"text\" for unhighlighted output. CodeBlock.assets None [] installs copying without downloading any Prism scripts or CSS. Add it once in the document head; Datastar initializes the component and handles the copy action. Copying requires HTTPS or localhost." }
            ]
            guideSection "syntax-highlighting" "Add syntax highlighting" [
                p { "Highlighting is optional and uses "; linkTo "https://prismjs.com/" "Prism"; ". Open the "; linkTo "https://prismjs.com/download.html" "Prism download builder"; ", select F#, Python, TypeScript, Bash and SQL, and keep the automatically selected language dependencies. Download the JavaScript bundle and save it as wwwroot/scripts/prism.js." }
                p { "Replace the CodeBlock.assets line in the quick start with:" }
                code "fsharp" "CodeBlock.assets None [\"/scripts/prism.js\"]"
                p { "Then choose a matching language when creating a block:" }
                code "fsharp" """CodeBlock.create "fsharp" "let total = 125.50M + 84.25M"
|> CodeBlock.render"""
                p { "The runtime loads the bundle when it initializes code and reapplies highlighting after Datastar patches. No Prism stylesheet is needed: the component's Tailwind styles supply light/dark token colors. Keep using \"text\" for blocks that should remain unhighlighted." }
            ]
            guideSection "prism-theme" "Optional Prism stylesheet" [
                p { "To use a Prism theme instead of the built-in colors, select a theme in the "; linkTo "https://prismjs.com/download.html" "download builder"; ", download the CSS, and save it as wwwroot/css/prism.css. Replace the assets call with:" }
                code "fsharp" "CodeBlock.assets (Some \"/css/prism.css\") [\"/scripts/prism.js\"]"
                p { "A Prism theme replaces token colors; it does not change the component's code surface or automatically switch between light and dark palettes." }
            ]
            for item in items.Tail do
                DocumentationSection.create item.id item.title [Example.gallery item.id item.title "fsharp" item.source (div { _class "min-w-0 p-6"; item.preview })]
            guideSection "nonce-csp" "Optional nonce CSP" [
                p { "If your host enforces nonce CSP, expose its fresh document nonce on the html element's data-nonce attribute, and pass it to assetsWithNonce and the Datastar script. That same nonce must be authorized by the response's script-src policy:" }
                code "fsharp" """let codeAssets nonce =
    fragment {
        CodeBlock.assetsWithNonce None ["/scripts/prism.js"] (Some nonce)
        script { _type "module"; _src "/scripts/datastar.js"; _attr("nonce", nonce) }
    }"""
                p { "For nonce generation, response headers and enhanced-navigation behavior, see the "; linkTo "/extensions/datastar#content-security-policy" "Datastar CSP guide"; "." }
            ]
            guideSection "api-reference" "API reference" [FSharpApiReference.forCategory "code-block" typeof<CodeBlockConfig> |> FSharpApiReference.render]
        ]

    let page (registration:DocPage) =
        if registration.path = codeBlockRegistration.path then codeBlockPage registration
        else pageWithExamples registration (examples (registration.path.Substring("/components/".Length)))
    let tryPage path = registrations |> List.tryFind (fun item -> item.path=path || List.contains path item.aliases) |> Option.map page
