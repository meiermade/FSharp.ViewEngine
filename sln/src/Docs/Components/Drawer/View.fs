namespace Docs.ComponentPages.Drawer

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
    let drawerRegistration =
        registration "components-drawer" "/components/drawer" "Drawer" "Drawer"

    let defaultDrawer =
        fveDrawer {
            _id "default-drawer"
            _label "Item details"
            p { "Project notes · Document" }
        }

    let defaultDrawerExample =
        div {
            fveDrawerTrigger {
                fveButton {
                    _ariaControls "default-drawer"
                    "Open drawer"
                }
            }

            defaultDrawer
        }

    let navigationDrawerExample =
        div {
            fveDrawerTrigger {
                _ariaControls "navigation-drawer"
                fveButton { "Open navigation drawer" }
            }
            fveDrawer {
                _id "navigation-drawer"
                _label "Project navigation"
                _variant DrawerVariant.Navigation
                _side DrawerSide.Start
                _initialFocus "navigation-drawer-dismiss"
                div {
                    _class "flex items-center justify-between gap-3 border-b border-[var(--fve-border)] p-3"
                    strong { "Projects" }
                    fveDrawerClose {
                        _ariaControls "navigation-drawer"
                        _id "navigation-drawer-dismiss"
                        _ariaLabel "Close navigation"
                        _variant ButtonVariant.Ghost
                        _padding Padding.Uniform
                        fveIcon { _name IconName.XMark; _size Size.Small }
                    }
                }
                nav {
                    _ariaLabel "Projects"
                    _class "grid gap-1 p-3"
                    fveLink { _href "/components/drawer"; "Drawer documentation" }
                    fveLink { _href "/components/side-nav"; "Side navigation" }
                }
            }
        }

    let containedDrawerConfig =
        fveDrawer {
            _id "contained-drawer"
            _label "Item details"
            p { "Project notes · Document" }
            _container "contained-drawer-background"
            _initialFocus "contained-drawer-close"

            fveDrawerFooter {
                fveButton {
                    _dataOn ("click", "document.getElementById('contained-drawer').close()")
                    "Done"
                }
            }
        }

    let containedDrawerExample =
        div {
            _class "relative h-80 overflow-hidden rounded-lg border border-[var(--fve-border)]"

            div {
                _id "contained-drawer-background"
                _class "h-full p-5"

                p {
                    _class "mb-4 text-sm"
                    "Open the drawer to review the item."
                }

                fveDrawerTrigger {
                    fveButton {
                        _ariaControls "contained-drawer"
                        "Open contained drawer"
                    }
                }
            }

            containedDrawerConfig
        }

    let itemDrawerContent refreshed =
        div {
            _id "item-drawer-content"
            _class "grid gap-5"

            fveDescriptionList {
                fveDescriptionListItem {
                    _label "Name"
                    "Project notes"
                }

                fveDescriptionListItem {
                    _label "Type"
                    "Document"
                }

                fveDescriptionListItem {
                    _label "Size"
                    "2.4 MB"
                }

                fveDescriptionListItem {
                    _label "Status"

                    fveBadge {
                        _color Color.Success
                        "Active"
                    }
                }
            }

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Last updated September 15, 2026 by Alex Morgan."
            }

            fveButton {
                _id "item-drawer-refresh"

                _dataOn (
                    "click",
                    "@get('/components/drawers/item').then(() => document.getElementById('item-drawer-refresh')?.focus())"
                )

                "Refresh details"
            }

            if refreshed then
                p {
                    _role "status"
                    _class "text-sm text-[var(--fve-positive-text)]"
                    "Details refreshed."
                }
        }

    let itemDrawerConfig =
        fveDrawer {
            _id "item-details-drawer"
            _label "Item details"
            itemDrawerContent false
            _description "Review the selected item."
            _initialFocus "item-drawer-refresh"
        }

    let itemEditorBody =
        form {
            _id "item-editor-form"
            _class "grid gap-6"

            section {
                _ariaLabelledby "item-editor-basics"
                _class "grid gap-4"

                h3 {
                    _id "item-editor-basics"
                    _class "text-base font-semibold"
                    "Details"
                }

                fveField {
                    fveLabel {
                        _for "item-editor-name"
                        "Name"
                    }

                    fveInput {
                        _id "item-editor-name"
                        _name "itemName"
                        _value "Project notes"
                        _ariaDescribedby (String.concat " " [ ("item-editor-name") + "-description" ])
                    }

                    fveFieldDescription {
                        _id ("item-editor-name" + "-description")
                        "Choose a name that is easy to find."
                    }
                }

                fveField {
                    fveLabel {
                        _for "item-editor-type"
                        "Type"
                    }

                    fveSelect {
                        _id "item-editor-type"
                        _name "itemType"
                        _value "document"

                        fveSelectContent {
                            for value, label, disabled in
                                ([ ("document", "Document", false)
                                   ("image", "Image", false)
                                   ("folder", "Folder", false) ]) do
                                fveSelectItem {
                                    _value value
                                    _label label
                                    _disabled disabled
                                }
                        }
                    }
                }

                fveField {
                    fveLabel {
                        _for "reference"
                        "Reference"
                    }

                    fveInput {
                        _id "reference"
                        _name "reference"
                        _value "1040"
                    }
                }
            }

            section {
                _ariaLabelledby "item-editor-organization"
                _class "grid gap-4 border-t border-[var(--fve-border)] pt-6"

                h3 {
                    _id "item-editor-organization"
                    _class "text-base font-semibold"
                    "Organization"
                }

                fveField {
                    fveLabel {
                        _for "folder"
                        "Folder"
                    }

                    fveInput {
                        _id "folder"
                        _name "folder"
                        _value "Shared files"
                    }
                }

                fveField {
                    fveLabel {
                        _for "notes"
                        "Internal notes"
                    }

                    fveTextarea {
                        _id "notes"
                        _name "notes"
                        _rows 5
                        _value "Notes from the latest project review."
                        _ariaDescribedby (String.concat " " [ ("notes") + "-description" ])
                    }

                    fveFieldDescription {
                        _id ("notes" + "-description")
                        "Only workspace members can read these notes."
                    }
                }
            }
        }

    let itemEditorFooter =
        div {
            _class "flex flex-wrap justify-end gap-3"

            fveButton {
                _dataOn ("click", "document.getElementById('item-editor-drawer')?.close()")
                "Cancel"
            }

            fveButton {
                _type "submit"
                _attr ("form", "item-editor-form")
                "Save changes"
            }
        }

    let itemEditorDrawerConfig =
        fveDrawer {
            _id "item-editor-drawer"
            _label "Edit item"
            itemEditorBody
            _description "Update the item details."
            _footerPlacement DrawerFooterPlacement.AfterContent
            fveDrawerFooter { itemEditorFooter }
            _initialFocus "item-editor-name"
            _size DrawerSize.Large
            _style "width:min(32rem,100vw)"
        }

    let filterDrawerConfig =
        fveDrawer {
            _id "item-filters-drawer"
            _label "Filters"

            nav {
                _ariaLabel "Item filters"
                _class "grid gap-3"

                fveLink {
                    _href "/components/drawer#components-drawer"
                    "Active items"
                }

                fveLink {
                    _href "/components/drawer#components-drawer-wide"
                    "Archived items"
                }
            }

            _side DrawerSide.Start
        }

    let topDrawerConfig =
        fveDrawer {
            _id "item-summary-drawer"
            _label "Summary"

            div {
                _class "grid gap-2 text-sm"
                p { "24 items in Shared files" }

                p {
                    _class "text-[var(--fve-muted-text)]"
                    "Updated September 28."
                }
            }

            _description "A horizontal drawer for a compact review task."
            _side DrawerSide.Top
        }

    let bottomDrawerConfig =
        fveDrawer {
            _id "item-history-drawer"
            _label "Activity history"

            ol {
                _class "grid gap-4 text-sm"

                for index in 1..12 do
                    li { $"Activity entry {index}" }
            }

            _description "Long content scrolls while the heading and actions remain reachable."
            _side DrawerSide.Bottom
            _size DrawerSize.Large

            fveDrawerFooter {
                fveButton {
                    _dataOn ("click", "document.getElementById('item-history-drawer')?.close()")
                    "Done"
                }
            }
        }

    let patchedItemDrawerContent = itemDrawerContent true

    let itemDrawerExample =
        div {
            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-details-drawer"
                    "Open item details"
                }
            }

            itemDrawerConfig
        }

    let itemEditorDrawerExample =
        div {
            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-editor-drawer"
                    "Edit item"
                }
            }

            itemEditorDrawerConfig
        }

    let filterDrawerExample =
        div {
            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-filters-drawer"
                    "Open filters"
                }
            }

            filterDrawerConfig
        }

    let horizontalDrawerExamples =
        div {
            _class "flex flex-wrap gap-3"

            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-summary-drawer"
                    "Open top drawer"
                }
            }

            fveDrawerTrigger {
                fveButton {
                    _ariaControls "item-history-drawer"
                    "Open bottom drawer"
                }
            }

            topDrawerConfig
            bottomDrawerConfig
        }

    let examples () =
            [ sample
                  "drawer-default"
                  "Default"
                  [ "defaultDrawer"; "defaultDrawerExample" ]
                  (centered defaultDrawerExample)
              sample "drawer-navigation" "Navigation" [ "navigationDrawerExample" ] (centered navigationDrawerExample)
              |> note "DrawerVariant.Navigation provides a flush body without the standard visible header and a compact navigation width. Supply a close control and initial focus in the navigation content; Drawer retains native modality, dismissal and focus restoration."
              sample
                  "drawer"
                  "Standard detail drawer"
                  [ "itemDrawerContent"; "itemDrawerConfig"; "itemDrawerExample" ]
                  (centered itemDrawerExample)
              sample
                  "drawer-wide"
                  "Wide editing form"
                  [ "itemEditorBody"
                    "itemEditorFooter"
                    "itemEditorDrawerConfig"
                    "itemEditorDrawerExample" ]
                  (centered itemEditorDrawerExample)
              |> note "DrawerFooterPlacement.AfterContent keeps actions below the fields when they fit. If the body needs to scroll, the footer remains visible. Omit _footerPlacement to keep the footer at the bottom of the drawer."
              sample
                  "drawer-start"
                  "Start-side filters"
                  [ "filterDrawerConfig"; "filterDrawerExample" ]
                  (centered filterDrawerExample)
              sample
                  "drawer-horizontal"
                  "Top and bottom edges"
                  [ "topDrawerConfig"; "bottomDrawerConfig"; "horizontalDrawerExamples" ]
                  (centered horizontalDrawerExamples)
              sample
                  "drawer-contained"
                  "App-contained drawer"
                  [ "containedDrawerConfig"; "containedDrawerExample" ]
                  (themedSurface containedDrawerExample)
              |> note
                  "Render beside the named app background in a relative, bounded viewport. _container uses show(), not showModal(): only that app background becomes inert. _open true supports route-owned native open states; mark the background inert in that native state too. The default standalone Drawer remains document-modal." ]


    let private galleryContent : Content =
        { description = "Present modal detail or controls from a selected viewport edge."
          usage = []
          sections = [
              titledSection "accessibility" "Labels and focus"
                  [ prose "Use the title part to name the dialog. Set _initialFocus to a control ID when the default first focus target is not appropriate; Escape dismisses the overlay and restores its opener." ]
          ]
          apiCategory = Some "drawer" }

    let drawerPage = gallery drawerRegistration galleryContent (examples ())
