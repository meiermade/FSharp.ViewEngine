namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

/// Dedicated pages for general-purpose blocks formerly hidden in framework bundles.
module ComponentDocumentation =
    let card = Card.create (p { _class "text-sm"; "Consumer-authored content." }) |> Card.render
    let cardWithParts =
        Card.create (p { _class "text-sm text-[var(--fve-muted-text)]"; "Review the account before publishing the report." })
        |> Card.withHeader (div { _class "grid gap-1"; h3 { _class "text-base font-semibold"; "Account review" }; p { _class "text-sm text-[var(--fve-muted-text)]"; "September close" } })
        |> Card.withFooter (Button.create (ButtonContent.Text "Publish report") |> Button.disabled |> Button.render)
        |> Card.render
    let mediaCard =
        Card.create (p { _class "text-sm text-[var(--fve-muted-text)]"; "Repository-owned social artwork." })
        |> Card.withMedia (img { _src "/social-card.png"; _alt "FSharp.ViewEngine — Typed HTML views for F#"; _class "aspect-video w-full object-contain" })
        |> Card.withHeader (h3 { _class "text-base font-semibold"; "Social card" })
        |> Card.withSize CardSize.Small |> Card.render
    let sectionHeader = SectionHeader.create "Account details" |> SectionHeader.render
    let sectionHeaderWithActions =
        SectionHeader.create "Recent transactions" |> SectionHeader.withDescription "Activity in the current reporting period."
        |> SectionHeader.withActions (Button.create (ButtonContent.Text "Export") |> Button.disabled |> Button.render)
        |> SectionHeader.withDivider |> SectionHeader.render
    let fields =
        FieldGroup.create "Delivery preferences" (div { _class "grid gap-3"; Checkbox.create "emailReceipt" "Email receipt" |> Checkbox.render; Checkbox.create "paperReceipt" "Paper receipt" |> Checkbox.render })
        |> FieldGroup.withDescription "Choose how to receive your receipt." |> FieldGroup.render
    let codeBlockHead pageNonce =
        CodeBlock.assetsWithNonce (Some "/css/prism-tomorrow.1.29.0.min.css") ["/scripts/prism.1.29.0.min.js"; "/scripts/prism-fsharp.1.29.0.min.js"] (Some pageNonce)
    let mermaidHead pageNonce = Mermaid.assetsWithNonce "/scripts/mermaid.11.16.0.min.js" (Some pageNonce)
    let codeBlock = CodeBlock.create "fsharp" "open FSharp.ViewEngine\nopen type Html\n\nlet greeting = p { \"Hello, F#\" }\nlet html = Render.toString greeting" |> CodeBlock.render
    let callout = Callout.create "Note" [p { "Use ordinary HTML for the content. This label introduces supporting information." }] |> Callout.render
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
        | "card" -> [sample "card" "Default" card; sample "cardWithParts" "Header and footer" cardWithParts; sample "mediaCard" "Consumer-authored media" mediaCard]
        | "section-header" -> [sample "sectionHeader" "Default" sectionHeader; sample "sectionHeaderWithActions" "Description, actions and divider" sectionHeaderWithActions]
        | "field-group" -> [sample "fields" "Related fields" fields]
        | "code-block" -> [sampleNames ["codeBlockHead";"codeBlock"] "codeBlock" "Copyable highlighted source" codeBlock]
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
            DocumentationSection.create "installation" "Installation" [CodeBlock.create "shell" $"dotnet fve add {key} --config src/Acme.Components/fve.json" |> CodeBlock.render]
            DocumentationSection.create "usage" "Usage" [
                CodeBlock.create "fsharp" items.Head.source |> CodeBlock.render
                if key="field-group" then
                    CodeBlock.create "shell" "dotnet fve add checkbox --config src/Acme.Components/fve.json" |> CodeBlock.render
                if key="card" || key="section-header" then
                    p { "The action variants additionally compose Button. Install button when copying those examples; the component itself accepts ordinary consumer-authored HTML." }
                if key="code-block" || key="example" then
                    p { "Include CodeBlock.assets once in your document head with consumer-owned Prism URLs, or assetsWithNonce using your server-issued document nonce. Initialization and lazy assets share that nonce. Layout.fs and Hosting.fs in the source ZIP demonstrate enforced Datastar nonce CSP; inline CSS remains a separate style-policy allowance." }
                if key="mermaid" then
                    p { "Include Mermaid.assets once in your document head with your consumer-owned script URL, or assetsWithNonce using your server-issued document nonce. Initialization and the lazy runtime share that nonce. Sequence constructors belong to this component. Only render trusted diagrams; CSP does not sanitize source or authorize arbitrary URLs." } ]
            for item in items.Tail do DocumentationSection.create item.id item.title [Example.gallery item.id item.title "fsharp" item.source (surface item.preview)]
            DocumentationSection.create "api-reference" "API reference" [FSharpApiReference.forCategory key typeof<CardConfig> |> FSharpApiReference.render] ]
    let page registration = pageWithExamples registration (examples (registration.path.Substring("/components/".Length)))
    let tryPage path = registrations |> List.tryFind (fun item -> item.path=path || List.contains path item.aliases) |> Option.map page
