namespace Docs.ComponentPages.Slide

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
    let slideRegistration =
        registration "components-slide" "/components/slide" "Slide" "Slide"

    let defaultSlide =
        fveSlide {
            fveSlideHeader {
                fveSlideTitle { "One frame. Your content." }
                fveSlideSubtitle { "A consistent title, content region and footer for every slide." }
            }
            fveSlideContent {
                div {
                    _class "grid grid-cols-2 gap-[2cqw]"
                    div { h3 { _class "m-0 text-[length:2cqw] font-semibold"; "Structure" }; p { "Compose ordinary F# HTML inside the content region." } }
                    div { h3 { _class "m-0 text-[length:2cqw] font-semibold"; "Consistency" }; p { "Keep spacing and typography in the shared frame." } }
                }
            }
            fveSlideFooter { "Prepared for discussion" }
        }

    let coverSlide =
        fveSlide {
            _variant SlideVariant.Cover
            fveSlideHeader {
                fveSlideTitle { "Make the next decision clear." }
                fveSlideSubtitle { "A cover uses the same canvas, header and footer regions." }
            }
            fveSlideContent {
                p { _class "max-w-[50cqw] text-[length:2.5cqw]"; "Use the space for one idea, not a smaller font." }
            }
        }

    let examples () =
            [ sample "slide-default" "Content slide" [ "defaultSlide" ] defaultSlide
              sample "slide-cover" "Cover" [ "coverSlide" ] coverSlide
              |> note "Slides keep a 16:9 canvas, up to 1280px wide at the default root size. Below 768px, the frame scrolls horizontally instead of making text unreadably small. Body content scrolls when it exceeds the reserved region; it is not automatically shrunk. Printing gives each slide a full-bleed 1280×720 page with its backgrounds and metadata. Split overflowing content across slides before printing; paper cannot scroll. Use relative sizing for child layouts."
              |> note "SlideHeader owns the title/subtitle composition. SlideFooter supplies footer copy; SlideBranding supplies the slide’s logo or wordmark. Numbering is automatic inside Presentation. To style the number, place fveSlideNumber as a direct slide child. A standalone slide is numbered 01." ]

    let scriptGuideLink =
        p {
            a {
                _href "/guides/presentations"
                _class "text-[var(--fve-brand-text)] underline underline-offset-4"
                "Build a presentation and print or save it as PDF."
            }
        }


    let private galleryContent : Content =
        { description = "Compose a 16:9 HTML canvas with reusable title, subtitle, content, footer and numbering regions."
          usage = []
          sections = []
          apiCategory = Some "slide" }

    let slidePage =
        let page = gallery slideRegistration galleryContent (examples ())
        { page with lead = page.lead @ [scriptGuideLink] }
