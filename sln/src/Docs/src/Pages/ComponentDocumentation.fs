namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

/// Dedicated pages for general-purpose blocks formerly hidden in framework bundles.
module ComponentDocumentation =
    let card =
        fveCard {
            p {
                _class "text-sm"
                "Project notes and supporting files."
            }
        }

    let cardWithParts =
        fveCard {
            fveCardHeader {
                div {
                    _class "grid gap-1"

                    h3 {
                        _class "text-base font-semibold"
                        "Project review"
                    }

                    p {
                        _class "text-sm text-[var(--fve-muted-text)]"
                        "Latest updates"
                    }
                }
            }

            fveCardFooter {
                fveButton {
                    _disabled true
                    "Publish"
                }
            }

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Review the selected items before publishing."
            }
        }

    let mediaCard =
        fveCard {
            _size Size.Small

            fveCardMedia {
                img {
                    _src "/social-card.png"
                    _alt "FSharp.ViewEngine — Typed HTML views for F#"
                    _class "aspect-video w-full object-contain"
                }
            }

            fveCardHeader {
                h3 {
                    _class "text-base font-semibold"
                    "Social card"
                }
            }

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Share this artwork with your team."
            }
        }

    let sectionHeader = fveSectionHeader { _label "Item details" }

    let sectionHeaderWithActions =
        fveSectionHeader {
            _label "Recent activity"
            _description "Updates from this month."
            _divider true

            fveSectionHeaderActions {
                fveButton {
                    _disabled true
                    "Export"
                }
            }
        }

    let fields =
        fveFieldGroup {
            _ariaDescribedby "delivery-description"
            fveFieldLegend { "Delivery preferences" }

            fveFieldDescription {
                _id "delivery-description"
                "Choose how to receive updates."
            }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "emailUpdates"
                    _name "emailUpdates"
                }

                fveLabel {
                    _for "emailUpdates"
                    "Email updates"
                }
            }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "printedUpdates"
                    _name "printedUpdates"
                }

                fveLabel {
                    _for "printedUpdates"
                    "Printed updates"
                }
            }
        }

    let fieldsWithoutDescription =
        fveFieldGroup {
            fveFieldLegend { "Optional updates" }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "optionalUpdates"
                    _name "optionalUpdates"
                }

                fveLabel {
                    _for "optionalUpdates"
                    "Send occasional updates"
                }
            }
        }

    let disabledFields =
        fveFieldGroup {
            _disabled true
            fveFieldLegend { "Archived preferences" }

            fveField {
                _layout FieldLayout.Horizontal

                fveCheckbox {
                    _id "archivedUpdates"
                    _name "archivedUpdates"
                    _checked true
                }

                fveLabel {
                    _for "archivedUpdates"
                    "Email updates"
                }
            }
        }

    let horizontalFields =
        fveFieldGroup {
            _layout FieldLayout.Horizontal
            fveFieldLegend { "Notification channels" }

            for value, caption in [ "email", "Email"; "sms", "Text message" ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveCheckbox {
                        _id ("channel-" + value)
                        _name "channels"
                        _value value
                    }

                    fveLabel {
                        _for ("channel-" + value)
                        caption
                    }
                }
        }

    let responsiveFields =
        fveFieldGroup {
            _layout FieldLayout.Responsive
            _ariaDescribedby "responsive-delivery-description"
            fveFieldLegend { "Delivery preferences" }

            fveFieldDescription {
                _id "responsive-delivery-description"
                "Options stack on small screens."
            }

            for value, caption in [ "email", "Email updates"; "printed", "Printed updates" ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveCheckbox {
                        _id ("delivery-" + value)
                        _name "delivery"
                        _value value
                    }

                    fveLabel {
                        _for ("delivery-" + value)
                        caption
                    }
                }
        }

    let mermaidHead pageNonce =
        Mermaid.assetsWithNonce "/scripts/mermaid.11.16.0.min.js" (Some pageNonce)

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

    let callout =
        fveCallout {
            _label "Note"
            p { "Keep a copy of these instructions for future reference." }
        }

    let mermaid =
        fveMermaid { "flowchart LR\n  Input[Input] --> Validate[Validate]\n  Validate --> Result[Result]" }

    let sequence =
        let user = SequenceDiagram.participant "user" "User"
        let server = SequenceDiagram.participant "server" "Server"

        let diagram =
            SequenceDiagram.sequence
                [ user; server ]
                [ SequenceDiagram.call user server "Validate form"
                  SequenceDiagram.reply server user "Result" ]

        fveMermaid { SequenceDiagram.render diagram }

    let example =
        fveExample {
            _name "nested-example"
            _label "Greeting"
            _variant ExampleVariant.Gallery
            _language "fsharp"
            fveExampleCode { "p { \"Hello\" }" }
            p { "Hello" }
        }

    let fsharpReference =
        fveFSharpApiReference {
            typeof<FSharpApiReferenceBuilder>
            typeof<Size>
        }

    let private registration key title : DocPage =
        { id = "components-" + key
          path = "/components/" + key
          aliases = []
          navLabel = title
          category = "Components"
          title = title
          browserTitle = title + " · FSharp.ViewEngine.Components"
          nodes = [] }

    let cardRegistration = registration "card" "Card"
    let sectionHeaderRegistration = registration "section-header" "Section header"
    let fieldGroupRegistration = registration "field-group" "Field group"
    let codeBlockRegistration = registration "code-block" "Code block"
    let calloutRegistration = registration "callout" "Callout"
    let mermaidRegistration = registration "mermaid" "Mermaid"
    let exampleRegistration = registration "example" "Example"

    let fsharpReferenceRegistration =
        registration "fsharp-api-reference" "F# API reference"

    let registrations =
        [ cardRegistration
          sectionHeaderRegistration
          fieldGroupRegistration
          codeBlockRegistration
          calloutRegistration
          mermaidRegistration
          exampleRegistration
          fsharpReferenceRegistration ]

    let private sourceText =
        lazy (SourceRegion.readEmbedded typeof<DocPage>.Assembly "Docs.Pages.ComponentDocumentation.fs")

    let private sampleNames names name label preview : Components.ComponentExample =
        { id = "components-" + name
          title = label
          source =
            "open FSharp.ViewEngine\nopen Acme.Components\nopen type Html\n\n"
            + (SourceRegion.declarations names sourceText.Value)
                .Replace("FSharp.ViewEngine.Components", "Acme.Components")
          preview = preview
          note = None }

    let private sample name label preview = sampleNames [ name ] name label preview

    let examples key =
        match key with
        | "card" ->
            [ sample "card" "Default" card
              sample "cardWithParts" "Header and footer" cardWithParts
              sample "mediaCard" "With media" mediaCard ]
        | "section-header" ->
            [ sample "sectionHeader" "Default" sectionHeader
              sample "sectionHeaderWithActions" "Description, actions and divider" sectionHeaderWithActions ]
        | "field-group" ->
            [ sample "fields" "Related fields" fields
              sample "fieldsWithoutDescription" "Without description" fieldsWithoutDescription
              sample "disabledFields" "Disabled fieldset" disabledFields
              sample "horizontalFields" "Horizontal fields" horizontalFields
              sample "responsiveFields" "Responsive fields" responsiveFields ]
        | "code-block" ->
            [ sample "plainCodeBlock" "Without highlighting" plainCodeBlock
              sample "codeBlock" "F#" codeBlock
              sample "pythonCodeBlock" "Python" pythonCodeBlock
              sample "typescriptCodeBlock" "TypeScript" typescriptCodeBlock
              sample "bashCodeBlock" "Bash" bashCodeBlock
              sample "sqlCodeBlock" "SQL" sqlCodeBlock ]
        | "callout" -> [ sample "callout" "Supporting information" callout ]
        | "mermaid" ->
            [ sampleNames [ "mermaidHead"; "mermaid" ] "mermaid" "Flowchart" mermaid
              sample "sequence" "Sequence constructors" sequence ]
        | "example" -> [ sample "example" "Preview and code" example ]
        | "fsharp-api-reference" -> [ sample "fsharpReference" "Compiled declarations" fsharpReference ]
        | _ -> []

    let allExamples () =
        registrations
        |> List.collect (fun item -> examples (item.path.Substring("/components/".Length)))

    let private pageWithExamples (registration: DocPage) (items: Components.ComponentExample list) =
        let key = registration.path.Substring("/components/".Length)

        let surface (content: HtmlElement) =
            div {
                _class "min-w-0 p-[12px] sm:p-6"
                content
            }

        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withLayout Gallery
        |> DocumentationPage.withRightRail TableOfContents
        |> DocumentationPage.withDescription
            "An independently installable component. Its content and surrounding layout remain consumer-owned."
        |> DocumentationPage.withLead
            [ fveExample {
                  _name items.Head.id
                  _label items.Head.title
                  _variant ExampleVariant.Lead
                  _language "fsharp"
                  fveExampleCode { items.Head.source }
                  surface items.Head.preview
              }
              yield! ComponentInstallation.exampleSetup key items.Head.source ]
        |> DocumentationPage.withSections
            [ Components.installationSection key
              DocumentationSection.create
                  "usage"
                  "Usage"
                  [ fveCodeBlock {
                        _language "fsharp"
                        items.Head.source
                    }
                    if key = "card" then
                        p {
                            "Use _size for the standard padding scale. For a custom inset, put _style \"--fve-card-inset:1rem\" on fveCard; the header, body and footer share it. The Card supplies its own local default, so set this override on the Card rather than an ancestor."
                        }
                    if key = "mermaid" then
                        p {
                            "Sequence constructors belong to this component. Only render trusted diagrams; CSP does not sanitize source or authorize arbitrary URLs."
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
                            surface item.preview
                        }
                        yield! ComponentInstallation.exampleSetup key item.source ]
              DocumentationSection.create
                  "api-reference"
                  "API reference"
                  [ fveFSharpApiReference { yield! (FSharpApiReference.forCategory key (typeof<CardBuilder>)) } ] ]

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
            script { _type "module"; _src "/assets/app.js" }
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
        let items = examples "code-block"

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
            [ Components.installationSection "code-block"
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
                        ", select F#, Python, TypeScript, Bash and SQL, and keep the automatically selected language dependencies. Download the JavaScript bundle and save it as wwwroot/scripts/prism.js."
                    }
                    p { "Replace the CodeBlock.assets line in the quick start with:" }
                    code "fsharp" "CodeBlock.assets None [\"/scripts/prism.js\"]"
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
                    code "fsharp" "CodeBlock.assets (Some \"/css/prism.css\") [\"/scripts/prism.js\"]"
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
        CodeBlock.assetsWithNonce None ["/scripts/prism.js"] (Some nonce)
        script { _type "module"; _src "/assets/app.js"; _attr("nonce", nonce) }
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

    let page (registration: DocPage) =
        if registration.path = codeBlockRegistration.path then
            codeBlockPage registration
        else
            pageWithExamples registration (examples (registration.path.Substring("/components/".Length)))

    let tryPage path =
        registrations
        |> List.tryFind (fun item -> item.path = path || List.contains path item.aliases)
        |> Option.map page
