namespace Docs.ComponentPages.MediaLibrary

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
open Docs.Common.SamplesExamples
open Docs.Common.SourceListsExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let mediaLibraryRegistration =
        applicationRegistration "components-media-library" "/components/media-library" "Media library" "Media library"

    let mediaLibraryExample =
        let library =
            MediaLibrary.create
                "product-media"
                "Site artwork"
                "assetIds"
                [ MediaAsset.create
                      "social-card"
                      "Social card"
                      "/social-card.png"
                      "FSharp.ViewEngine — Typed HTML views for F#"
                      LedgerAccounts
                  |> MediaAsset.withDetail "1200 × 630 · PNG"
                  |> MediaAsset.primary
                  MediaAsset.create
                      "app-icon"
                      "App icon"
                      "/android-chrome-512x512.png"
                      "FSharp.ViewEngine blue code-mark icon"
                      LedgerAccounts
                  |> MediaAsset.withDetail "512 × 512 · PNG"
                  MediaAsset.create
                      "touch-icon"
                      "Touch icon"
                      "/apple-touch-icon.png"
                      "FSharp.ViewEngine blue code-mark touch icon"
                      LedgerAccounts
                  |> MediaAsset.withDetail "180 × 180 · PNG" ]
            |> MediaLibrary.withSelected [ "social-card" ]
            |> MediaLibrary.render shellDestinationUrl

        library
        |> fun libraryWithActions ->
            div {
                _dataSignals "{mediastate: 'ready'}"
                _class "grid gap-4"

                div {
                    _class "flex flex-wrap gap-2"

                    for state, label in
                        [ "empty", "Show empty"
                          "pending", "Simulate loading"
                          "error", "Simulate error"
                          "ready", "Restore media" ] do
                        button {
                            _type "button"
                            _dataOn ("click", $"$mediastate = '{state}'")

                            _class
                                "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)]"

                            label
                        }
                }

                p {
                    _dataShow "$mediastate == 'empty'"

                    _class
                        "rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"

                    "No media assets. Upload an image to begin."
                }

                p {
                    _dataShow "$mediastate == 'pending'"
                    _role "status"
                    _ariaLive "polite"

                    _class
                        "rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"

                    "Loading media…"
                }

                div {
                    _dataShow "$mediastate == 'error'"
                    _role "alert"

                    _class
                        "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-4 text-sm text-[var(--fve-critical-text)]"

                    span { "Media could not be loaded." }

                    button {
                        _type "button"
                        _dataOn ("click", "$mediastate = 'ready'")

                        _class
                            "rounded-[var(--fve-radius-control)] px-3 py-2 font-semibold ring-1 ring-[var(--fve-critical-ring)]"

                        "Retry media"
                    }
                }

                div {
                    _dataShow "$mediastate == 'ready'"
                    _class "grid gap-4"
                    libraryWithActions

                    div {
                        _class "flex flex-wrap gap-2"

                        fveButton {
                            _dataOn (
                                "click",
                                "document.getElementById('media-edit-feedback').textContent = 'Selected asset IDs: ' + Array.from(document.querySelectorAll('#product-media input:checked')).map(input => input.value).join(', ')"
                            )

                            "Inspect selected assets"
                        }

                        fveButton {
                            _dataOn (
                                "click",
                                "document.getElementById('product-media')?.dispatchEvent(new CustomEvent('fve-selection-clear'))"
                            )

                            "Clear selection"
                        }
                    }

                    fveField {
                        fveLabel {
                            _for "media-alt-text"
                            "Alt text"
                        }

                        fveTextarea {
                            _id "media-alt-text"
                            _name "altText"
                            _value "FSharp.ViewEngine — Typed HTML views for F#"
                            _ariaDescribedby (String.concat " " [ ("media-alt-text") + "-description" ])
                        }

                        fveFieldDescription {
                            _id ("media-alt-text" + "-description")
                            "Describe the selected asset for people who cannot see it."
                        }
                    }

                    fveButton {
                        _dataOn (
                            "click",
                            "(() => { const value = document.getElementById('media-alt-text').value; document.querySelectorAll('#product-media input:checked').forEach(input => input.closest('li').querySelector('img').alt = value); document.getElementById('media-edit-feedback').textContent = 'Alt text updated for selected media.' })()"
                        )

                        "Save alt text"
                    }

                    fveField {
                        fveLabel {
                            _for "media-replacement"
                            "Replacement image"
                        }

                        fveFileSelection {
                            _id "media-replacement"
                            _name "replacementImage"
                            _accept "image/*"
                            _ariaDescribedby "media-replacement-help"
                        }

                        fveFieldDescription {
                            _id "media-replacement-help"
                            "Choose a local image; this provider-free example does not upload it."
                        }
                    }

                    fveButton {
                        _dataOn (
                            "click",
                            "document.querySelectorAll('#product-media input:checked').forEach(input => input.closest('li').querySelector('img').src = '/favicon-32x32.png'); document.getElementById('media-edit-feedback').textContent = 'Demo replacement applied to selected media.'"
                        )

                        "Apply demo replacement"
                    }

                    p {
                        _id "media-edit-feedback"
                        _role "status"
                        _ariaLive "polite"
                        _class "text-sm text-[var(--fve-muted-text)]"
                    }
                }
            }

    let examples () =
            [ sample
                  "media-library"
                  "Library and editor"
                  (shellSource @ [ "mediaLibraryExample" ])
                  (detailsSurface mediaLibraryExample) ]


    let private galleryContent : Content =
        { description = "Browse and select media with filters, metadata, and responsive collection states."
          usage = []
          sections = []
          apiCategory = Some "media-library" }

    let mediaLibraryPage =
        gallery mediaLibraryRegistration galleryContent (examples ())
