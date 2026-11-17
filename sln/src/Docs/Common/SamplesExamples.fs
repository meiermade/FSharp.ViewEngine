namespace Docs.Common

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
open Docs.Common.SharedExamples

module SamplesExamples =
    [<NoEquality; NoComparison>]
    type ComponentExample =
        { id: string
          title: string
          source: string
          preview: HtmlElement
          note: string option }

    let exampleImports =
        "open System\nopen FSharp.ViewEngine\nopen Acme.Components\nopen type Html\nopen type Svg\nopen type Datastar"

    let sample id title names preview =
        { id = "components-" + id
          title = title
          source = exampleImports + "\n\n" + SourceRegion.declarations names sourceText.Value
          preview = preview
          note = None }

    let serverSample id title names preview =
        let example = sample id title names preview

        { example with
            source =
                "open Giraffe\nopen Microsoft.Extensions.DependencyInjection\nopen StarFederation.Datastar\nopen StarFederation.Datastar.DependencyInjection\n"
                + example.source
            note =
                Some
                    "Includes the running demo's Giraffe handler and routes. Mount the routes in your existing application with services.AddDatastar(); the short delay makes request loading visible." }

    let note message example = { example with note = Some message }

    let gallerySurface (content: HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"

            for attribute in galleryDestinationAttributes do
                attribute

            div {
                for attribute in ComponentsTheme.attributes ComponentsTheme.sky do
                    attribute

                content
            }
        }

    let connectedGallerySurface (content: HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"

            div {
                for attribute in ComponentsTheme.attributes ComponentsTheme.sky do
                    attribute

                content
            }
        }

    let centered (content: HtmlElement) =
        gallerySurface (
            div {
                _class "flex min-h-40 min-w-0 flex-wrap items-center justify-center gap-6 p-[12px]"
                content
            }
        )

    let connectedCentered (content: HtmlElement) =
        connectedGallerySurface (
            div {
                _class "flex min-h-40 min-w-0 flex-wrap items-center justify-center gap-6 p-[12px]"
                content
            }
        )

    let fieldSurface (content: HtmlElement) =
        gallerySurface (
            div {
                _class "mx-auto grid min-h-40 min-w-0 max-w-md content-center gap-4 p-[12px] sm:p-8"
                content
            }
        )
