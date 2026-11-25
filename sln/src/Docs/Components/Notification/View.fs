namespace Docs.ComponentPages.Notification

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
    let notificationRegistration =
        registration "components-notification" "/components/notification" "Notification" "Notification"

    let notificationLeadItem sequence =
        fveNotification {
            _id $"saved-notification-{sequence}"
            _label "Changes saved"
            p { "Your changes are ready." }
        }

    let notificationLeadRegion sequence =
        let notification =
            if sequence > 0 then
                Some(notificationLeadItem sequence)
            else
                None

        fveNotificationRegion {
            _id "save-notifications"
            _ariaLabel "Workspace notifications"

            match (notification: HtmlElement option) with
            | Some notification -> notification
            | None -> ()

            _withinContainer true
        }

    let simpleNotificationExample =
        div {
            _dataSignals ("components_notification_sequence", ["root"; "ifmissing"], "0")

            div {
                _class "flex min-h-40 items-start justify-center p-6"

                fveButton {
                    _dataOn ("click", "$components_notification_sequence += 1; @post('/components/notifications/show')")
                    "Show notification"
                }
            }

            notificationLeadRegion 0
        }

    let notificationWithActionsExample =
        let action =
            fveButton {
                _variant ButtonVariant.Outline
                _size Size.Small

                _dataOn (
                    "click",
                    "evt.currentTarget.closest('[data-fve-notification]').querySelector('[role=status] div').textContent = 'The invitation remains pending.'"
                )

                "Undo"
            }

        fveNotificationRegion {
            _id "invitation-notifications"
            _ariaLabel "Invitation notifications"
            _withinContainer true

            fveNotification {
                _id "invitation-notification"
                _label "Invitation accepted"
                _color Color.Info
                _persistent true
                p { "You joined Northwind Outdoor." }
                fveNotificationActions { action }
            }
        }

    let timedNotificationItem sequence =
        fveNotification {
            _id $"review-notification-{sequence}"
            _label "Review needed"
            _color Color.Warning
            _ttl 3000
            p { "Two items still need your attention." }
        }

    let timedNotificationRegion sequence =
        fveNotificationRegion {
            _id "timed-notifications"
            _ariaLabel "Review notifications"
            _withinContainer true

            if sequence > 0 then
                timedNotificationItem sequence
        }

    let timedNotificationExample =
        div {
            _dataSignals ("components_timed_notification_sequence", ["root"; "ifmissing"], "0")

            div {
                _class "flex min-h-40 items-start justify-center p-6"

                fveButton {
                    _dataOn (
                        "click",
                        "$components_timed_notification_sequence += 1; @post('/components/notifications/show-timed')"
                    )

                    "Show 3-second notification"
                }
            }

            timedNotificationRegion 0
        }

    let examples () =
            [ sample
                  "notification"
                  "Default"
                  [ "notificationLeadItem"
                    "notificationLeadRegion"
                    "simpleNotificationExample" ]
                  (overlaySurface simpleNotificationExample)
              sample
                  "notification-actions"
                  "Persistent with an action"
                  [ "notificationWithActionsExample" ]
                  (overlaySurface notificationWithActionsExample)
              sample
                  "notification-timing"
                  "Custom lifetime"
                  [ "timedNotificationItem"
                    "timedNotificationRegion"
                    "timedNotificationExample" ]
                  (overlaySurface timedNotificationExample) ]


    let private galleryContent : Content =
        { description = "Present ephemeral, optionally actionable feedback in an accessible bottom-end stack."
          usage = []
          sections = [
              titledSection "accessibility" "Announcements"
                  [ p {
                        "Use _announcement to choose Static, Polite or Assertive announcements. See "
                        a {
                            _href "/components/notice#components-notice-announcement"
                            _class "font-semibold text-[var(--fve-brand-text)] underline"
                            "Notice's announcement example"
                        }
                        " for a live status update."
                    } ]
          ]
          apiCategory = Some "notification" }

    let notificationPage = gallery notificationRegistration galleryContent (examples ())
