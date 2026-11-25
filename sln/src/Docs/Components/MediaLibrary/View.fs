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
                _attr ("data-signals__root__ifmissing", "{mediastate: 'ready'}")
                _class "grid gap-4"

                div {
                    _class "flex flex-wrap gap-2"

                    for state, label in
                        [ "empty", "Show empty"
                          "pending", "Simulate loading"
                          "error", "Simulate error"
                          "ready", "Restore media" ] do
                        fveButton {
                            _type "button"
                            _dataOn ("click", $"$mediastate = '{state}'")

                            label
                        }
                }

                fveEmptyState {
                    _dataShow "$mediastate == 'empty'"
                    _label "No media assets."
                    _description "Upload an image to begin."
                }

                fveLoadingIndicator {
                    _dataShow "$mediastate == 'pending'"
                    "Loading media…"
                }

                fveNotice {
                    _dataShow "$mediastate == 'error'"
                    _label "Media could not be loaded."
                    _color Color.Error
                    _announcement LiveAnnouncement.Assertive
                    fveNoticeActions {
                        fveButton { _dataOn ("click", "$mediastate = 'ready'"); "Retry media" }
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
