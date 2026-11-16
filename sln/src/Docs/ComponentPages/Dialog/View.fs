namespace Docs.ComponentPages.Dialog

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
open Docs.Common.SharedExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let dialogRegistration =
        registration "components-dialog" "/components/dialog" "Dialog" "Dialog"

    let defaultDialog =
        fveDialog {
            _id "default-dialog"
            _label "Review changes"
            p { "Check your changes before continuing." }
        }

    let defaultDialogExample =
        div {
            fveDialogTrigger {
                fveButton {
                    _ariaControls "default-dialog"
                    "Open dialog"
                }
            }

            defaultDialog
        }

    let containedDialogConfig =
        fveDialog {
            _id "contained-dialog"
            _label "Review changes"
            _container "contained-dialog-background"
            _initialFocus "contained-dialog-close"
            _dismissOnBackdrop true
            p { "Check your changes before continuing." }

            fveDialogFooter {
                fveDialogClose {
                    _id "contained-dialog-close"
                    _ariaControls "contained-dialog"
                    "Cancel"
                }
            }
        }

    let containedDialogExample =
        div {
            _class "relative h-80 overflow-hidden rounded-lg border border-[var(--fve-border)]"

            div {
                _id "contained-dialog-background"
                _class "h-full p-5"

                p {
                    _class "mb-4 text-sm"
                    "Surrounding documentation stays available."
                }

                fveDialogTrigger {
                    fveButton {
                        _ariaControls "contained-dialog"
                        "Open contained dialog"
                    }
                }
            }

            containedDialogConfig
        }

    let dialogConfig =
        fveDialog {
            _id "review-changes-dialog"
            _label "Review changes"
            _description "You can close this dialog to make further changes."
            _initialFocus "review-changes-dialog-close"
            _dismissOnBackdrop true
            p { "Check your changes before continuing." }

            fveDialogFooter {
                fveDialogClose {
                    _id "review-changes-dialog-close"
                    _ariaControls "review-changes-dialog"
                    "Close"
                }
            }
        }

    let reviewDialogTrigger =
        fveDialogTrigger {
            fveButton {
                _ariaControls "review-changes-dialog"
                "Review changes"
            }
        }

    let reviewDialog = dialogConfig

    let itemDeletionForm (dialogId: string) (validation: string option) =
        let requestSignal = "_" + dialogId.Replace('-', '_') + "_pending"
        let pendingExpression = "$" + requestSignal

        form {
            _id $"{dialogId}-content"
            _method "post"
            _action "/components/dialogs/confirm"
            _dataIndicator requestSignal

            _dataOn (
                "submit",
                $"!({pendingExpression}) && @post('/components/dialogs/confirm').then(() => document.getElementById('{dialogId}-confirm')?.focus())"
            )

            _dataAttr ("aria-busy", $"{pendingExpression} ? 'true' : 'false'")

            match validation with
            | Some message ->
                p {
                    _id $"{dialogId}-validation"
                    _role "alert"

                    _class
                        "rounded-[var(--fve-radius-control)] bg-[var(--fve-critical-subtle)] p-3 text-sm text-[var(--fve-critical-text)] ring-1 ring-inset ring-[var(--fve-critical-ring)]"

                    message
                }
            | None -> ()

            p {
                _id $"{dialogId}-status"
                _tabindex -1
                _role "status"
                _dataShow pendingExpression
                _style "display:none"
                _class "mt-4 text-sm text-[var(--fve-muted-text)]"
                "Confirmation in progress."
            }

            div {
                _class "mt-6 flex flex-wrap justify-end gap-3"

                fveButton {
                    _id $"{dialogId}-cancel"
                    _dataAttr ("disabled", pendingExpression)
                    _dataOn ("click", "el.closest('dialog').close()")
                    "Cancel"
                }

                fveButton {
                    _color Color.Error
                    _variant ButtonVariant.Solid
                    _dataAttr ("disabled", pendingExpression)
                    _dataAttr ("aria-busy", $"{pendingExpression} ? 'true' : null")
                    _type "submit"
                    _id $"{dialogId}-confirm"
                    "Delete"
                }
            }
        }

    let itemDeletionContent validation =
        itemDeletionForm "delete-item-confirmation" validation

    let itemDeletionDialog =
        fveDialog {
            _id "delete-item-confirmation"
            _label "Delete item?"
            itemDeletionContent None
            _description "Delete Project notes? This action cannot be undone."
            _initialFocus "delete-item-confirmation-cancel"

            _role "alertdialog"
            _dataSignals "{_delete_item_confirmation_pending: false}"
            _dataOn ("cancel", "$_delete_item_confirmation_pending && evt.preventDefault()")
        }

    let confirmationExample =
        div {
            fveDialogTrigger {
                fveButton {
                    _ariaControls "delete-item-confirmation"
                    "Delete"
                }
            }

            itemDeletionDialog
        }

    let examples () =
            [ sample
                  "dialog-default"
                  "Default"
                  [ "defaultDialog"; "defaultDialogExample" ]
                  (centered defaultDialogExample)
              sample
                  "dialog"
                  "Modal dialog"
                  [ "dialogConfig"; "reviewDialogTrigger"; "reviewDialog" ]
                  (centered (
                      div {
                          reviewDialogTrigger
                          reviewDialog
                      }
                  ))
              sample
                  "dialog-confirmation"
                  "Server-validated confirmation"
                  [ "itemDeletionForm"
                    "itemDeletionContent"
                    "itemDeletionDialog"
                    "confirmationExample" ]
                  (centered confirmationExample)
              sample
                  "dialog-contained"
                  "App-contained dialog"
                  [ "containedDialogConfig"; "containedDialogExample" ]
                  (themedSurface containedDialogExample)
              |> note
                  "_container keeps the dialog in a positioned app viewport rather than the document top layer. The component owns the backdrop, app-background inertness, Escape and focus return; review chrome remains usable. _open true supports server-rendered open states; mark the background inert in that native state too." ]


    let private galleryContent : Content =
        { description = "Present modal content with labelled structure, focus management, dismissal, and form composition."
          usage = []
          sections = [
              titledSection "accessibility" "Labels and focus"
                  [ prose "Use the title part to name the dialog. Set _initialFocus to a control ID when the default first focus target is not appropriate; Escape dismisses the overlay and restores its opener." ]
          ]
          apiCategory = Some "dialog" }

    let dialogPage = gallery dialogRegistration galleryContent (examples ())
