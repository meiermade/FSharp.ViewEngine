namespace Docs.ComponentPages.Presentation

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
open Docs.ComponentPages.Slide.View
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let presentationRegistration =
        registration "components-presentation" "/components/presentation" "Presentation" "Presentation"

    let presentationExample =
        fvePresentation {
            _label "A two-slide presentation"
            coverSlide
            defaultSlide
        }

    let examples () =
            [ sample "presentation-default" "Separated slides and presentation mode" [ "defaultSlide"; "coverSlide"; "presentationExample" ] presentationExample
              |> note "Presentation accepts Slide children and numbers them in source order. Each slide owns its branding and footer; use an ordinary F# function for repeated configuration. Import the installed Assets/presentation.js module for the viewer. The stacked view shows only Expand; previous/next, position and Restore appear in expanded mode. Expanded mode fits one slide to the viewport, with controls aligned to the canvas’s right edge above a consistent gap; use Left/Right, Home/End and Escape. Stacked slides remain readable without JavaScript. Reused slides do not retain their previous deck position."
              |> note "Use the Presentations guide at /guides/presentations for deck composition, standalone authoring and printing. Choose Print or Save as PDF in your browser for one 16:9 page per slide, without the viewer controls or surrounding host chrome. The active theme is preserved. Printing from the expanded viewer restores its current slide and focus afterward. Print styles also work without JavaScript; no PowerPoint or separate PDF converter is required." ]


    let private galleryContent : Content =
        { description = "Separate and number slides, with an expand/restore viewer and keyboard navigation."
          usage = []
          sections = []
          apiCategory = Some "presentation" }

    let presentationPage =
        let page = gallery presentationRegistration galleryContent (examples ())
        { page with lead = page.lead @ [scriptGuideLink] }
