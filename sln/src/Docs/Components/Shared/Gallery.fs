namespace Docs.Common

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open Docs.Common.SamplesExamples

module Gallery =
    let detailsSurface (content: HtmlElement) =
        gallerySurface (
            div {
                _class "p-[12px] sm:p-6"
                content
            }
        )

    let calendarSurface (content: HtmlElement) =
        gallerySurface (
            div {
                _class "grid w-full min-w-0 grid-cols-1 gap-4 p-[12px] sm:p-6"
                content
            }
        )

    let connectedCalendarSurface (content: HtmlElement) =
        connectedGallerySurface (div { _class "grid w-full min-w-0 grid-cols-1 gap-4 p-[12px] sm:p-6"; content })

    let overlaySurface (content: HtmlElement) =
        gallerySurface (
            div {
                _class "relative min-h-[32rem] overflow-hidden bg-[var(--fve-surface-subtle)] p-4"
                content
            }
        )

    let prose content =
        p {
            _class "m-0 text-base leading-relaxed text-[var(--fve-muted-text)] [overflow-wrap:anywhere]"
            text content
        }

    let code language (source: string) =
        fveCodeBlock {
            _language language
            source
        }

    let private bullets items =
        ul {
            _class
                "m-0 list-disc pl-5 text-base leading-relaxed text-[var(--fve-muted-text)] marker:text-[var(--fve-brand-ring)]"

            for item in items do
                li {
                    _class "mt-2 first:mt-0"
                    text item
                }
        }

    let buildingBlockLinks label (items: (string * string) list) =
        div {
            _class "flex flex-wrap items-baseline gap-x-4 gap-y-2"

            h2 {
                _class "text-base font-normal"
                "Built with"
            }

            nav {
                _ariaLabel label
                _class "flex flex-wrap gap-x-4 gap-y-2"

                for path, itemLabel in items do
                    a {
                        _href path

                        _class
                            "font-semibold text-[var(--fve-brand-text)] underline decoration-[var(--fve-brand-ring)] underline-offset-[0.18em]"

                        text itemLabel
                    }
            }
        }

    let titledSection id title content =
        DocumentationSection.create
            id
            title
            (h2 {
                _class "text-xl font-semibold text-[var(--fve-text)]"
                title
             }
             :: content)

    let installationSection selector = ComponentInstallation.section selector

    type Content =
        { description: string
          usage: HtmlElement list
          sections: DocumentationSectionConfig list
          apiCategory: string option }

    let private componentKey (registration: DocPage) = registration.path.Substring("/components/".Length)

    let private componentGallery (registration: DocPage) (content: Content) examples =
        match examples with
        | [] -> invalidOp $"Component page '{registration.path}' requires a canonical lead example."
        | lead :: variants ->
            let leadContent =
                [ fveExample {
                      _name lead.id
                      _label lead.title
                      _variant ExampleVariant.Lead
                      _language "fsharp"
                      fveExampleCode { lead.source }
                      lead.preview
                  }
                  yield! ComponentInstallation.exampleSetup (componentKey registration) lead.source ]

            let sections =
                [ installationSection (componentKey registration)
                  titledSection
                      "usage"
                      "Usage"
                      [ code "fsharp" lead.source
                        yield! content.usage ]
                  for item in variants do
                      DocumentationSection.create
                          item.id
                          item.title
                          ([ fveExample {
                                 _name item.id
                                 _label item.title
                                 _variant ExampleVariant.Gallery
                                 _language "fsharp"
                                 fveExampleCode { item.source }
                                 item.preview
                             } ]
                           @ ComponentInstallation.exampleSetup (componentKey registration) item.source
                           @ (item.note |> Option.map prose |> Option.toList))
                  yield! content.sections
                  match content.apiCategory with
                  | Some owner ->
                      titledSection
                          "api-reference"
                          "API reference"
                          [ fveFSharpApiReference {
                                yield! (FSharpApiReference.forCategory owner (typeof<ButtonBuilder>))
                            } ]
                  | None -> () ]

            DocumentationPage.create registration.id registration.title
            |> DocumentationPage.withDescription content.description
            |> DocumentationPage.withLayout Gallery
            |> DocumentationPage.withRightRail TableOfContents
            |> DocumentationPage.withLead leadContent
            |> DocumentationPage.withSections sections

    let private pageExampleGallery (registration: DocPage) (content: Content) examples =
        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withLayout Gallery
        |> DocumentationPage.withRightRail NoRail
        |> DocumentationPage.withSections
            [ for item in examples do
                  DocumentationSection.create
                      item.id
                      item.title
                      ([ fveExample {
                             _name item.id
                             _label item.title
                             _variant ExampleVariant.Gallery
                             _language "fsharp"
                             fveExampleCode { item.source }
                             item.preview
                         } ]
                       @ (item.note |> Option.map prose |> Option.toList))
              yield! content.sections ]

    let gallery (registration: DocPage) content examples =
        if registration.path.StartsWith("/components/page-examples/", StringComparison.Ordinal) then
            pageExampleGallery registration content examples
        else
            componentGallery registration content examples
