namespace Docs.ComponentPages.Notice

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
    let noticeRegistration =
        registration "components-notice" "/components/notice" "Notice" "Notice"

    let informationNotice =
        fveNotice {
            _id "notice-information"
            _label "Before you submit"
            p { "Check the contact name and email address before sending." }
        }

    let noticeAnnouncementExample =
        div {
            _class "grid gap-4"
            _attr ("data-signals__root", "{_noticeStatus: ''}")

            fveButton {
                _class "justify-self-start"
                _dataOn ("click", "$_noticeStatus = 'Ready to continue.'")
                "Update status"
            }

            fveNotice {
                _id "notice-announcement"
                _label "Status"
                _announcement LiveAnnouncement.Polite
                span { _dataText "$_noticeStatus" }
            }
        }

    let noticeColorExamples =
        div {
            _class "grid w-full min-w-0 grid-cols-[repeat(auto-fit,minmax(min(100%,20rem),1fr))] gap-4"

            for label, color in
                [ "Primary", Color.Primary
                  "Secondary", Color.Secondary
                  "Success", Color.Success
                  "Warning", Color.Warning
                  "Error", Color.Error
                  "Info", Color.Info
                  "Neutral", Color.Neutral ] do
                fveNotice {
                    _id ("notice-color-" + label.ToLowerInvariant())
                    _label label
                    p { "Review your details before continuing." }
                    _color color
                }
        }

    let noticeVariantExamples =
        div {
            _class "grid w-full min-w-0 grid-cols-[repeat(auto-fit,minmax(min(100%,20rem),1fr))] gap-4"

            for label, variant in
                [ "Solid", NoticeVariant.Solid
                  "Soft", NoticeVariant.Soft
                  "Outline", NoticeVariant.Outline
                  "Ghost", NoticeVariant.Ghost ] do
                fveNotice {
                    _id ("notice-variant-" + label.ToLowerInvariant())
                    _label label
                    p { "Review your details before continuing." }
                    _variant variant

                    fveNoticeActions {
                        fveLink {
                            _variant LinkVariant.Plain
                            _href "/components/notice"
                            _class "underline underline-offset-2"
                            "Read guidance"
                        }
                    }
                }
        }

    let successNotice =
        fveNotice {
            _id "notice-success"
            _label "Export ready"
            p { "Your export is ready to download." }
            _color Color.Success

            fveNoticeActions {
                fveLink {
                    _variant LinkVariant.Plain
                    _href "data:text/csv;charset=utf-8,Name%2CStatus%0AProject%20notes%2CActive"
                    _attr ("download", "items.csv")
                    _class "underline underline-offset-2"
                    "Download"
                }
            }
        }

    let warningNotice =
        fveNotice {
            _id "notice-warning"
            _label "Review required"
            p { "Two items need review before they can be published." }
            _color Color.Warning
        }

    let criticalNotice =
        fveNotice {
            _id "notice-error"
            _label "Details need attention"
            p { "Correct the highlighted fields and validate the form again." }
            _color Color.Error

            fveNoticeActions {
                fveLink {
                    _variant LinkVariant.Plain
                    _href "/components/error-summary"
                    _class "underline underline-offset-2"
                    "Review highlighted fields"
                }
            }
        }

    let examples () =
            [ sample "notice" "Default" [ "informationNotice" ] (fieldSurface informationNotice)
              sample "notice-colors" "Colors" [ "noticeColorExamples" ] (detailsSurface noticeColorExamples)
              sample "notice-variants" "Variants" [ "noticeVariantExamples" ] (detailsSurface noticeVariantExamples)
              sample "notice-success" "Success with download" [ "successNotice" ] (fieldSurface successNotice)
              sample "notice-warning" "Warning" [ "warningNotice" ] (fieldSurface warningNotice)
              sample "notice-critical" "Error with recovery link" [ "criticalNotice" ] (fieldSurface criticalNotice)
              sample
                  "notice-announcement"
                  "Announce an update"
                  [ "noticeAnnouncementExample" ]
                  (fieldSurface noticeAnnouncementExample)
              |> note
                  "Ordinary persistent notices use LiveAnnouncement.Static. Use Polite for status updates, as shown here; reserve Assertive for urgent changes that need immediate attention." ]


    let private galleryContent : Content =
        { description = "Present persistent contextual guidance with independent color, visual treatment, and accessible announcement policy."
          usage = []
          sections = []
          apiCategory = Some "notice" }

    let noticePage = gallery noticeRegistration galleryContent (examples ())
