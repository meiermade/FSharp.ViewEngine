namespace Docs.ComponentPages.FloatingPanel

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
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let floatingPanelRegistration =
        registration "components-floating-panel" "/components/floating-panel" "Floating panel" "Floating panel"

    let floatingPanelContent =
        div {
            _class "grid gap-3 text-sm"
            p { "Use this space for contextual guidance that should not block the primary task." }

            a {
                _href "/components/page-examples/operations-dashboard"
                _class "font-semibold text-[var(--fve-brand-text)] underline-offset-2 hover:underline"
                "Open operations"
            }
        }

    let floatingPanelExample =
        fveFloatingPanel {
            _id "workspace-guide"
            _label "Workspace guide"
            _description "Helpful context remains available beside the application."
            _boundary FloatingPanelBoundary.Container
            floatingPanelContent
        }

    let minimizedFloatingPanelExample =
        fveFloatingPanel {
            _id "minimized-workspace-guide"
            _label "Workspace guide"
            _boundary FloatingPanelBoundary.Container
            _state FloatingPanelState.Minimized
            floatingPanelContent
        }

    let floatingPanelChecklistContent =
        ol {
            _class "m-0 grid list-none gap-4 p-0 text-sm"

            li {
                p {
                    _class "font-medium"
                    "1. Review the examples"
                }

                a {
                    _href "/examples"
                    _class "font-semibold text-[var(--fve-brand-text)] underline underline-offset-2"
                    "Open examples"
                }
            }

            li {
                p {
                    _class "font-medium"
                    "2. Choose your components"
                }

                p {
                    _class "text-[var(--fve-muted-text)]"
                    "Complete"
                }
            }
        }

    let floatingPanelChecklistExample =
        fveFloatingPanel {
            _id "component-checklist"
            _label "Setup checklist"
            _description "Two steps to get started."
            _boundary FloatingPanelBoundary.Container
            floatingPanelChecklistContent
        }

    let examples () =
            [ sample
                  "floating-panel"
                  "Open non-modal panel"
                  [ "floatingPanelContent"; "floatingPanelExample" ]
                  (overlaySurface floatingPanelExample)
              sample
                  "floating-panel-minimized"
                  "Minimized with restore"
                  [ "floatingPanelContent"; "minimizedFloatingPanelExample" ]
                  (overlaySurface minimizedFloatingPanelExample)
              sample
                  "floating-panel-checklist"
                  "Checklist"
                  [ "floatingPanelChecklistContent"; "floatingPanelChecklistExample" ]
                  (overlaySurface floatingPanelChecklistExample) ]


    let private galleryContent : Content =
        { description = "Keep dismissible supporting content available beside the primary workflow."
          usage = []
          sections = []
          apiCategory = Some "floating-panel" }

    let floatingPanelPage =
        gallery floatingPanelRegistration galleryContent (examples ())
