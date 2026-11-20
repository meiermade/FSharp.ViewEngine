namespace Docs.ComponentPages.Browser

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
open Docs.Common.FramesExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let browserRegistration =
        registration "components-browser" "/components/browser" "Browser" "Browser"

    let browserWithAddress =
        fveBrowser {
            _address "https://fve.meiermade.com/components/browser"

            div {
                _class "grid min-h-64 content-center gap-3 bg-[var(--fve-surface-subtle)] p-[12px] text-center"

                strong {
                    _class "text-lg"
                    "Content preview"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Rendered HTML"
                }
            }
        }

    let browserReviewStates =
        [ "summary", "Summary", "An overview of the content under review."
          "details", "Details", "Supporting details for the same content."
          "empty", "Empty", "No items to display in this state." ]

    let browserReviewHref expanded state =
        "/components/browser?state=" + state + (if expanded then "&expanded=1" else "")

    let browserReviewDirection (filterNavigation: HtmlAttribute list) expanded state offset =
        let index = browserReviewStates |> List.findIndex (fun (key, _, _) -> key = state)
        let label, icon = if offset < 0 then "Back", IconName.ChevronLeft else "Forward", IconName.ChevronRight
        let content = fveIcon { _name icon; _variant IconVariant.Mini }
        match List.tryItem (index + offset) browserReviewStates with
        | Some (key, _, _) ->
            fveLink {
                _href (browserReviewHref expanded key)
                yield! filterNavigation
                _variant LinkVariant.Outline
                _color Color.Neutral
                _size Size.Small
                _ariaLabel label
                _title label
                content
            }
        | None ->
            fveButton {
                _variant ButtonVariant.Outline
                _size Size.Small
                _disabled true
                _ariaLabel label
                _title label
                content
            }

    let browserReviewFrame navigation (filterNavigation: HtmlAttribute list) expanded requested =
        let state, title, description =
            browserReviewStates
            |> List.tryFind (fun (key, _, _) -> key = requested)
            |> Option.defaultValue browserReviewStates.Head
        fveBrowser {
            _id "browser-review-frame"
            _expanded expanded
            _address (browserReviewHref expanded state)
            _data ("review-state", state)

            fveBrowserWindowControls {
                _class "w-48 max-w-full gap-1"
                browserReviewDirection filterNavigation expanded state -1
                fveDropdownMenu {
                    _id "browser-review-state"
                    _class "min-w-0 flex-1"
                    fveDropdownMenuTrigger {
                        _ariaLabel ("Review state: " + title)
                        fveButton {
                            _variant ButtonVariant.Outline
                            _size Size.Small
                            _class "w-full justify-between"
                            span { _class "min-w-0 truncate"; title }
                            fveIcon { _name IconName.ChevronDown; _variant IconVariant.Mini }
                        }
                    }
                    for key, label, _ in browserReviewStates do
                        fveDropdownMenuItem {
                            _href (browserReviewHref expanded key)
                            yield! filterNavigation
                            _ariaCurrent (if key = state then "true" else "false")
                            label
                            if key = state then
                                fveDropdownMenuItemTrailing { fveIcon { _name IconName.Check; _variant IconVariant.Mini } }
                        }
                }
                browserReviewDirection filterNavigation expanded state 1
            }

            div {
                _class "grid min-h-64 content-center gap-3 p-6 text-center"
                strong { _class "text-lg"; title }
                p { description }
            }

            fveBrowserToolbarActions {
                fixtureWindowControl navigation (browserReviewHref false state) "browser-review" expanded
            }
        }

    let browserWithControls = browserReviewFrame [] [] false "summary"

    let browserReviewPreview = browserReviewFrame [] [Docs.Web.PreviewUpdate.link "browser-review-frame" ""]

    let defaultBrowser =
        fveBrowser {
            div {
                _class "p-[12px] text-center"
                "Content preview"
            }
        }

    let examples () =
            [ sample "browser-default" "Default" [ "defaultBrowser" ] (centered defaultBrowser)
              sample "browser" "With an address bar" [ "browserWithAddress" ] (centered browserWithAddress)
              sample
                  "browser-review"
                  "Review controls and expansion"
                  [ "fixtureWindowControl"
                    "browserReviewStates"
                    "browserReviewHref"
                    "browserReviewDirection"
                    "browserReviewFrame"
                    "browserWithControls" ]
                  (connectedGallerySurface (div { _class "p-3 sm:p-6"; browserReviewPreview false "summary" }))
              |> note
                  "Back/Forward links and the state menu navigate between real server-rendered states. The address and selected state remain accurate when expanded or restored. The fixed-width navigation group keeps the state selector between Back and Forward without shifting the arrows. Browser inherits its host’s theme. Install button, link, icon and dropdown-menu; import Assets/button.js and Assets/dropdown-menu.js. fveBrowserWindowControls and fveBrowserToolbarActions accept host-owned controls; _expanded fills the viewport. The host reads the state and expanded query parameters and renders browserReviewFrame; Browser itself does not own routes or history." ]


    let private galleryContent : Content =
        { description = "Frame rendered content with a truthful browser title and address."
          usage = []
          sections = []
          apiCategory = Some "browser" }

    let browserPageFor state =
        examples ()
        |> List.map (fun example ->
            if example.id = "components-browser-review" then
                { example with preview = connectedGallerySurface (div { _class "p-3 sm:p-6"; browserReviewPreview false state }) }
            else example)
        |> gallery browserRegistration galleryContent

    let browserPage = browserPageFor "summary"
