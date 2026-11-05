namespace Example.Server.ApplicationPages

open System
open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html
open type Svg
open type Datastar
open Model
open Ledger.Domain
open Controls

module internal Overlay =
    let cancelLink query page id =
        fveLink {
            _variant LinkVariant.Outline
            _color Color.Neutral

            _href (
                collectionHref
                    { query with
                        overlayFrom = ""
                        state = ""
                        editorDraft = None }
                    (backgroundPage query page)
            )

            _id id
            "Cancel"
        }

    let editorDrawer
        (query: Query)
        (baseId: string)
        (label: string)
        (baseFocusId: string)
        (body: HtmlElement)
        (actions: HtmlElement)
        =
        let id = elementId query baseId
        let focusId = elementId query baseFocusId

        if query.previewId = "native" then
            section {
                _ariaLabel label

                _class
                    "fixed inset-y-0 right-0 z-50 flex w-full max-w-lg flex-col border-l border-[var(--fve-border)] bg-[var(--fve-background)]"

                h2 {
                    _class "shrink-0 border-b border-[var(--fve-border)] p-5 text-lg font-semibold"
                    label
                }

                div {
                    _class "min-h-0 flex-1 overflow-y-auto p-5"
                    body
                }

                div {
                    _class "flex shrink-0 justify-end gap-3 border-t border-[var(--fve-border)] p-5"
                    actions
                }
            }
        else
            fveDrawer {
                _id id
                _label label
                _initialFocus focusId

                _dataPreserveAttr "open"

                _style "width:min(32rem,100%);max-width:100%"

                _dataInit
                    $"queueMicrotask(() => {{ if (el.isConnected) {{ if (!el.open) el.showModal(); document.getElementById('{focusId}')?.focus() }} }})"

                _dataOn (
                    "click",
                    [ "capture" ],
                    $"if (evt.target === el || evt.target.closest('#{id}-close')) {{ evt.preventDefault(); evt.stopImmediatePropagation(); document.getElementById('{id}-cancel')?.click() }}"
                )

                _dataOn ("cancel", $"evt.preventDefault(); document.getElementById('{id}-cancel')?.click()")
                body
                fveDrawerFooter { actions }
            }

    let deletionConfirmation query page label (details: HtmlElement) eligible =
        let overlayId = elementId query "template-delete-record"
        let formId = overlayId + "-form"
        let cancelId = overlayId + "-cancel"

        let body =
            fragment {
                if not eligible || query.state = "delete-blocked" then
                    notice
                        query
                        "template-delete-blocked"
                        "Account cannot be deleted"
                        "It has a balance or recorded transactions."
                        Color.Error

                details

                form {
                    _id formId
                    _method "post"
                    _action (collectionHref query page)
                    editorContextFields query
                    hidden "action" "delete"
                }
            }

        let actions =
            fragment {
                cancelLink query page cancelId

                if eligible then
                    formSubmit Color.Error [ _form formId ] "Delete"
            }

        if query.previewId = "native" then
            Layout.section
                ("Delete " + label + "?")
                (fragment {
                    body

                    div {
                        _class "mt-5 flex justify-end gap-3"
                        actions
                    }
                })
        else
            fveDialog {
                _id overlayId
                _label ("Delete " + label + "?")
                _initialFocus cancelId
                _dismissOnBackdrop true

                _dataOn ("close", [ "capture" ], $"document.getElementById('{cancelId}').click()")

                _dataInit
                    $"queueMicrotask(() => {{ if (el.isConnected) {{ if (!el.open) el.showModal(); document.getElementById('{cancelId}')?.focus() }} }})"

                body
                fveDialogFooter { actions }
            }
