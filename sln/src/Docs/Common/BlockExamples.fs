namespace Docs.Common

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

module BlockExamples =
    let registration key title : DocPage =
        { id = "components-" + key
          path = "/components/" + key
          aliases = []
          navLabel = title
          category = "Components"
          title = title
          browserTitle = title + " · FSharp.ViewEngine.Components"
          nodes = [] }

    let sourceText =
        lazy (SourceRegion.readEmbeddedModules typeof<DocPage>.Assembly "Docs.BlockSources.")

    let sampleNames names name label preview : SamplesExamples.ComponentExample =
        { id = "components-" + name
          title = label
          source =
            "open FSharp.ViewEngine\nopen Acme.Components\nopen type Html\n\n"
            + (SourceRegion.declarations names sourceText.Value)
                .Replace("FSharp.ViewEngine.Components", "Acme.Components")
          preview = preview
          note = None }

    let sample name label preview = sampleNames [ name ] name label preview

    let pageWithExamples (registration: DocPage) usage (items: SamplesExamples.ComponentExample list) =
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
            [ ComponentInstallation.section key
              DocumentationSection.create
                  "usage"
                  "Usage"
                  [ fveCodeBlock {
                        _language "fsharp"
                        items.Head.source
                    }
                    yield! usage ]
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
