namespace Docs.ComponentPages.Card

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

open Docs.Common.BlockExamples

module View =
    let card =
        fveCard {
            p {
                _class "text-sm"
                "Project notes and supporting files."
            }
        }

    let cardWithParts =
        fveCard {
            fveCardHeader {
                div {
                    _class "grid gap-1"

                    h3 {
                        _class "text-base font-semibold"
                        "Project review"
                    }

                    p {
                        _class "text-sm text-[var(--fve-muted-text)]"
                        "Latest updates"
                    }
                }
            }

            fveCardFooter {
                fveButton {
                    _disabled true
                    "Publish"
                }
            }

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Review the selected items before publishing."
            }
        }

    let mediaCard =
        fveCard {
            _size Size.Small

            fveCardMedia {
                img {
                    _src "/social-card.png"
                    _alt "FSharp.ViewEngine — Typed HTML views for F#"
                    _class "aspect-video w-full object-contain"
                }
            }

            fveCardHeader {
                h3 {
                    _class "text-base font-semibold"
                    "Social card"
                }
            }

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Share this artwork with your team."
            }
        }

    let cardRegistration = registration "card" "Card"

    let examples () =
            [ sample "card" "Default" card
              sample "cardWithParts" "Header and footer" cardWithParts
              sample "mediaCard" "With media" mediaCard ]


    let private usage =
        [ p {
              "Use _size for the standard padding scale. For a custom inset, put _style \"--fve-card-inset:1rem\" on fveCard; the header, body and footer share it. The Card supplies its own local default, so set this override on the Card rather than an ancestor."
          } ]

    let page = pageWithExamples cardRegistration usage (examples ())
