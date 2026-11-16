namespace Docs.ComponentPages.Resizable

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
    let resizableRegistration =
        registration "components-resizable" "/components/resizable" "Resizable" "Resizable"

    let defaultResizablePanels =
        fveResizable {
            _id "default-workspace-panels"
            _ariaLabel "Workspace panels"

            fveResizablePanel {
                aside {
                    _class "h-full p-4"
                    "Navigation"
                }
            }

            fveResizablePanel {
                div {
                    _class "h-full p-4"
                    "Content"
                }
            }
        }

    let resizablePanels =
        fveResizable {
            _id "item-workspace-panels"
            _ariaLabel "Workspace panels"
            _initialSize 34
            _minSize 20
            _maxSize 60
            _collapsedSize 0

            fveResizablePanel {
                aside {
                    _class "h-full bg-[var(--fve-surface-subtle)] p-4"
                    strong { "Items" }

                    p {
                        _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                        "Project notes\nShared files\nTeam guide"
                    }
                }
            }

            fveResizablePanel {
                div {
                    _class "h-full p-4"

                    h3 {
                        _class "font-semibold"
                        "Project notes"
                    }

                    p {
                        _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                        "Drag the divider to resize the supporting list."
                    }
                }
            }
        }

    let verticalResizablePanels =
        fveResizable {
            _id "review-workspace-panels"
            _ariaLabel "Review panels"
            _orientation Orientation.Vertical
            _initialSize 40
            _minSize 25
            _maxSize 70

            fveResizablePanel {
                section {
                    _class "h-full p-4"
                    strong { "Review summary" }

                    p {
                        _class "mt-2 text-sm text-[var(--fve-muted-text)]"
                        "Three entries need attention."
                    }
                }
            }

            fveResizablePanel {
                section {
                    _class "h-full bg-[var(--fve-surface-subtle)] p-4 text-sm"
                    "Entry details"
                }
            }
        }

    let examples () =
            [ sample "resizable-default" "Default" [ "defaultResizablePanels" ] (detailsSurface defaultResizablePanels)
              sample "resizable" "Horizontal split panels" [ "resizablePanels" ] (detailsSurface resizablePanels)
              sample
                  "resizable-vertical"
                  "Vertical split panels"
                  [ "verticalResizablePanels" ]
                  (detailsSurface verticalResizablePanels)
              |> note "Use _persistenceKey with a stable, unique key to remember the panel size on this browser, including after navigation or expansion/restoration. Omit it for transient sizing. Stored values outside the current bounds are ignored; resizing still works when browser storage is unavailable." ]


    let private galleryContent : Content =
        { description = "Split content into pointer- and keyboard-resizable horizontal or vertical panels."
          usage = []
          sections = []
          apiCategory = Some "resizable" }

    let resizablePage = gallery resizableRegistration galleryContent (examples ())
