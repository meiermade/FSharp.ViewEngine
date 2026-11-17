namespace Docs.ComponentPages.Switch

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
open Docs.Common.ChoicesExamples
open Docs.Common.SamplesExamples
open Docs.Common.SourceListsExamples
open Docs.ComponentPages.ToggleButton.View
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let switchRegistration =
        registration "components-switch" "/components/switch" "Switch" "Switch"

    let basicSwitch =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "productUpdates"
                _name "productUpdates"
            }

            fveLabel {
                _for "productUpdates"
                "Product updates"
            }
        }

    let switchWithHelp =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "weeklySummary"
                _name "weeklySummary"
                _checked true
                _ariaDescribedby "weeklySummary-help"
            }

            fveFieldContent {
                fveLabel {
                    _for "weeklySummary"
                    "Weekly summary"
                }

                fveFieldDescription {
                    _id "weeklySummary-help"
                    "Receive one email with the week's activity."
                }
            }
        }

    let switchFormRegion enabled (validation: string option) result =
        div {
            _id "components-switch-form-region"
            _class "grid max-w-sm gap-3"

            form {
                _dataOn ("submit", "@post('/components/choices/switch', {contentType: 'form'})")
                _class "grid gap-3"

                fveField {
                    _layout FieldLayout.Horizontal

                    fveSwitch {
                        _id "components-notifications"
                        _name "notifications"
                        _checked enabled
                        _ariaInvalid validation.IsSome

                        _ariaDescribedby (
                            String.concat
                                " "
                                [ "notifications-help"
                                  if validation.IsSome then
                                      "notifications-error" ]
                        )
                    }

                    fveFieldContent {
                        fveLabel {
                            _for "components-notifications"
                            "Notifications"
                        }

                        fveFieldDescription {
                            _id "notifications-help"
                            "Receive an update when new items are published."
                        }

                        match validation with
                        | Some message ->
                            fveFieldError {
                                _id "notifications-error"
                                message
                            }
                        | None -> ()
                    }
                }

                choiceSubmitButton "components-switch-submit" "Save notifications"
            }

            choiceResult "components-switch-result" result
        }

    let notificationSetting = switchFormRegion true None None

    let pendingNotifications =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "components-pending-notifications"
                _name "notifications"
                _checked true
                _readonly true
                _ariaBusy true
            }

            fveLabel {
                _for "components-pending-notifications"
                "Saving notifications"
            }
        }

    let disabledNotifications =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "components-disabled-notifications"
                _name "notifications"
                _disabled true
            }

            fveLabel {
                _for "components-disabled-notifications"
                "Notifications unavailable"
            }
        }

    let invalidNotifications =
        fveField {
            _layout FieldLayout.Horizontal

            fveSwitch {
                _id "components-invalid-notifications"
                _name "notifications"
                _ariaInvalid true
                _ariaDescribedby "notifications-invalid-error"
            }

            fveFieldContent {
                fveLabel {
                    _for "components-invalid-notifications"
                    "Notification service"
                }

                fveFieldError {
                    _id "notifications-invalid-error"
                    "Notification preferences could not be saved."
                }
            }
        }

    let switchSizes =
        div {
            _class "grid w-full max-w-sm gap-4"

            for value, caption, size in
                [ "compact", "Compact switch", Size.Small
                  "standard", "Standard switch", Size.Medium
                  "prominent", "Prominent switch", Size.Large ] do
                fveField {
                    _layout FieldLayout.Horizontal

                    fveSwitch {
                        _id (value + "Alerts")
                        _name (value + "Alerts")
                        _size size
                        _checked true
                    }

                    fveLabel {
                        _for (value + "Alerts")
                        caption
                    }
                }
        }

    let examples () =
            [ sample "switch" "Default" [ "basicSwitch" ] (fieldSurface basicSwitch)
              sample "switch-help" "With help text" [ "switchWithHelp" ] (fieldSurface switchWithHelp)
              sample
                  "switch-submission"
                  "Setting with submission"
                  (formSource @ [ "switchFormRegion"; "notificationSetting" ])
                  (fieldSurface notificationSetting)
              sample "switch-pending" "Pending" [ "pendingNotifications" ] (fieldSurface pendingNotifications)
              sample "switch-disabled" "Disabled" [ "disabledNotifications" ] (fieldSurface disabledNotifications)
              sample
                  "switch-validation"
                  "Validation error"
                  [ "invalidNotifications" ]
                  (fieldSurface invalidNotifications)
              sample "switch-sizes" "Sizes" [ "switchSizes" ] (fieldSurface switchSizes)
              sample "switch-stacked" "Stacked layout" [ "stackedSwitch" ] (fieldSurface stackedSwitch) ]


    let private galleryContent : Content =
        { description = "Toggle an immediate binary setting with accessible checked and unavailable states."
          usage = []
          sections = []
          apiCategory = Some "switch" }

    let switchPage = gallery switchRegistration galleryContent (examples ())
