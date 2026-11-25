namespace Docs.ComponentPages.Page

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
open Docs.Common.LedgerExamples
open Docs.ComponentPages.PageTopBar.View
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let pageRegistration =
        applicationRegistration "components-page" "/components/page" "Page" "Page"

    let defaultPage =
        Page.create (fvePageHeader { _label "Project overview" }) (p { "Review items and recent activity." })
        |> Page.render

    let canvasPage () =
        let graph =
            div {
                _role "region"
                _ariaLabel "Workflow canvas"
                _tabindex 0
                _class "h-full overflow-auto"
                _attr ("data-signals__root", "{canvasZoom: 1}")

                div {
                    _class "flex items-center gap-2 p-3"

                    fveButton {
                        _dataOn ("click", "$canvasZoom = Math.min(2, $canvasZoom + 0.25)")
                        "Zoom in"
                    }

                    fveButton {
                        _dataOn ("click", "$canvasZoom = Math.max(0.5, $canvasZoom - 0.25)")
                        "Zoom out"
                    }

                    output {
                        _ariaLabel "Zoom level"
                        _dataText "Math.round($canvasZoom * 100) + '%'"
                        "100%"
                    }
                }

                div {
                    _style "width:1000px;height:600px;transform-origin:top left"
                    _dataAttr ("style", "'width:' + (1000 * $canvasZoom) + 'px;height:' + (600 * $canvasZoom) + 'px'")

                    raw
                        """<svg role="img" aria-label="Workflow from draft through review to publish" viewBox="0 0 1000 600" width="100%" height="100%"><path d="M260 230H390M610 230H740" stroke="var(--fve-border)" stroke-width="3"/><g fill="var(--fve-surface)" stroke="var(--fve-border)"><rect x="40" y="190" width="220" height="80" rx="8"/><rect x="390" y="190" width="220" height="80" rx="8"/><rect x="740" y="190" width="220" height="80" rx="8"/></g><g fill="var(--fve-text)" font-size="16" text-anchor="middle"><text x="150" y="236">Draft</text><text x="500" y="236">Review</text><text x="850" y="236">Publish</text></g></svg>"""
                }
            }

        Page.create (fvePageHeader { _label "Workflow" }) graph
        |> Page.withBodyLayout PageBodyLayout.Canvas

    let private workspacePreview _ (content: HtmlElement) =
        div {
            _attr ("data-fve-full-bleed-example", "true")
            _class "docs-components-preview"

            for attribute in galleryDestinationAttributes do
                attribute

            div {
                for attribute in
                    ComponentsTheme.attributes (ComponentsTheme.sky |> ComponentsTheme.withDensity Density.Compact) do
                    attribute

                div {
                    _class "h-[36rem] overflow-hidden"
                    content
                }
            }
        }

    let transactionsPagePreview =
        workspacePreview
            "transactions-page"
            (renderTransactionsPage (pageTopBar (treasuryBreadcrumbs TreasuryTransactions)) transactionTabs)

    let canvasPagePreview =
        workspacePreview "canvas-page" (canvasPage () |> Page.render)

    let examples () =
            [ sample "page-default" "Default" [ "defaultPage" ] (fullBleedThemedSurface defaultPage)
              sample "page-canvas" "Remaining-height canvas" [ "canvasPage" ] canvasPagePreview ]


    let private galleryContent : Content =
        { description = "Compose application content from page chrome, headings, sections, and bounded layouts."
          usage = []
          sections = []
          apiCategory = Some "page" }

    let pagePage = gallery pageRegistration galleryContent (examples ())
