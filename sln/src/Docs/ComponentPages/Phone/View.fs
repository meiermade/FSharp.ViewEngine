namespace Docs.ComponentPages.Phone

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
open Docs.Common.SharedExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let phoneRegistration =
        registration "components-phone" "/components/phone" "Phone" "Phone"

    let phoneWithContent =
        fvePhone {
            div {
                _class "grid h-full content-center gap-3 p-[12px] text-center"

                strong {
                    _class "text-lg"
                    "Notes"
                }

                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    "Confirm arrival time."
                }
            }
        }

    // Host-owned review links; installed frames supply slots and viewport presentation only.

    let phoneReviewFrame expanded =
        fvePhone {
            _expanded expanded

            div {
                _class "flex h-full min-h-0 flex-col"

                div {
                    _class "shrink-0 border-b border-[var(--fve-border)] p-4 text-center font-semibold"
                    "Messages"
                }

                div {
                    _class "min-h-0 flex-1 overflow-auto p-4"

                    p {
                        _class "rounded-lg bg-[var(--fve-surface-hover)] p-3 text-base"
                        "Confirm arrival time."
                    }
                }

                div {
                    _class "shrink-0 border-t border-[var(--fve-border)] p-4 text-sm text-[var(--fve-muted-text)]"
                    "Message preview"
                }
            }

            fvePhoneToolbar {
                div {
                    _class "flex min-w-0 flex-wrap items-center gap-3"
                    fixtureWindowControl "/components/phone" "phone-review" expanded

                    span {
                        _class "min-w-0 flex-1 text-sm font-medium"
                        "Phone preview"
                    }

                    fixtureAppearance "phone-review-theme"
                }
            }
        }

    let phoneWithControls = phoneReviewFrame false

    let defaultPhone =
        fvePhone {
            div {
                _class "grid h-full place-items-center p-[12px] text-center"
                "Screen preview"
            }
        }

    let examples () =
            [ sample "phone-default" "Default" [ "defaultPhone" ] (centered defaultPhone)
              sample "phone" "With screen content" [ "phoneWithContent" ] (centered phoneWithContent)
              sample
                  "phone-review"
                  "Review controls and expansion"
                  [ "fixtureAppearance"
                    "fixtureWindowControl"
                    "phoneReviewFrame"
                    "phoneWithControls" ]
                  (themedSurface phoneWithControls)
              |> note
                  "Install color-mode and dropdown-menu, and initialize ColorMode.assets once in the host head as shown in Theming. withToolbar places consumer-owned review controls above the device. withExpanded retains desktop proportions and presents edge-to-edge content on narrow or short screens, without a fabricated browser address. Controls and destinations remain host-owned." ]


    let private galleryContent : Content =
        { description = "Frame rendered content in a neutral responsive phone viewport."
          usage = []
          sections = []
          apiCategory = Some "phone" }

    let phonePage = gallery phoneRegistration galleryContent (examples ())
