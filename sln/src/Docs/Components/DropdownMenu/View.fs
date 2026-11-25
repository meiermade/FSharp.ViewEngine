namespace Docs.ComponentPages.DropdownMenu

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
open Docs.ComponentPages.Button.View
open Docs.Common.SamplesExamples
open Docs.Common.SharedExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let dropdownMenuRegistration =
        registration "components-dropdown-menu" "/components/dropdown-menu" "Dropdown menu" "Dropdown menu"

    let menuLeadingIcon = fveIcon { _name IconName.Check; _variant IconVariant.Micro }

    let basicDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-basic"
            fveDropdownMenuTrigger { fveButton { "Actions" } }

            fveDropdownMenuItem {
                _href "/components/button"
                "Button documentation"
            }

            fveDropdownMenuItem {
                _href "/components/button-group"
                "Button group documentation"
            }
        }

    let menuDocumentIcon = fveIcon { _name IconName.DocumentText; _size Size.Small }

    let iconDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-icons"
            fveDropdownMenuTrigger { fveButton { "Documentation" } }
            fveDropdownMenuItem {
                _href "/components/button"
                fveDropdownMenuItemLeading { plusIcon }
                "Button"
            }
            fveDropdownMenuItem {
                _href "/components/popover"
                fveDropdownMenuItemLeading { menuDocumentIcon }
                "Popover"
            }
        }

    let shortcutDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-shortcuts"
            fveDropdownMenuTrigger { fveButton { "Find documentation" } }
            fveDropdownMenuItem {
                _href "/components/button"
                "Button"
                fveDropdownMenuItemShortcut { "B" }
            }
            fveDropdownMenuItem {
                _href "/components/popover"
                "Popover"
                fveDropdownMenuItemShortcut { "P" }
            }
        }

    let checkboxDropdownMenu =
        div {
            _class "grid justify-items-start gap-3"
            _attr ("data-signals__root__ifmissing", "{_menu_email: true, _menu_push: false}")
            fveDropdownMenu {
                _id "components-menu-checkboxes"
                fveDropdownMenuTrigger { fveButton { "Notification preferences" } }
                fveDropdownMenuItem {
                    _role "menuitemcheckbox"; _ariaChecked true
                    _dataAttr ("aria-checked", "$_menu_email ? 'true' : 'false'")
                    _dataOn ("click", "$_menu_email = !$_menu_email")
                    "Email alerts"
                }
                fveDropdownMenuItem {
                    _role "menuitemcheckbox"; _ariaChecked false
                    _dataAttr ("aria-checked", "$_menu_push ? 'true' : 'false'")
                    _dataOn ("click", "$_menu_push = !$_menu_push")
                    "Push alerts"
                }
            }
            output {
                _ariaLive "polite"; _class "text-sm text-[var(--fve-muted-text)]"
                _dataText "'Email: ' + ($_menu_email ? 'on' : 'off') + ' · Push: ' + ($_menu_push ? 'on' : 'off')"
                "Email: on · Push: off"
            }
        }

    let radioDropdownMenu =
        div {
            _class "grid justify-items-start gap-3"
            _attr ("data-signals__root__ifmissing", "{_menu_sort: 'Name'}")
            fveDropdownMenu {
                _id "components-menu-radio"
                fveDropdownMenuTrigger { fveButton { "Sort preview" } }
                fveDropdownMenuGroup {
                    _label "Sort by"
                    for label in ["Name"; "Status"; "Last updated"] do
                        let selected = label = "Name"
                        fveDropdownMenuItem {
                            _role "menuitemradio"; _ariaChecked selected
                            _dataAttr ("aria-checked", "$_menu_sort === '" + label + "' ? 'true' : 'false'")
                            _dataOn ("click", "$_menu_sort = '" + label + "'")
                            label
                        }
                }
            }
            output {
                _ariaLive "polite"; _class "text-sm text-[var(--fve-muted-text)]"
                _dataText "'Sort by: ' + $_menu_sort"
                "Sort by: Name"
            }
        }

    let disabledDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-disabled"
            fveDropdownMenuTrigger { fveButton { "Available actions" } }
            fveDropdownMenuItem { _href "/components/dropdown-menu"; "Read guidance" }
            fveDropdownMenuItem {
                _disabled true
                _description "Choose a saved report first."
                "Export report"
            }
        }

    let destructiveDropdownMenu =
        div {
            _class "grid w-full max-w-sm gap-3"
            _attr ("data-signals__root__ifmissing", "{_menu_draft: 'Review September reports.'}")
            fveField {
                fveLabel { _for "menu-draft-note"; "Draft note" }
                fveTextarea {
                    _id "menu-draft-note"; _name "draftNote"
                    _dataBind ("_menu_draft", ["root"])
                    "Review September reports."
                }
            }
            fveDropdownMenu {
                _id "components-menu-destructive"
                fveDropdownMenuTrigger { fveButton { "Draft actions" } }
                fveDropdownMenuItem {
                    _color Color.Error
                    _dataOn ("click", "$_menu_draft = ''")
                    "Clear draft"
                }
                fveDropdownMenuSeparator { () }
                fveDropdownMenuItem {
                    _dataOn ("click", "$_menu_draft = 'Review September reports.'")
                    "Restore sample"
                }
            }
        }

    let dropdownMenuItems refreshed =
        [ fveDropdownMenuGroup {
              _label "Item"

              fveDropdownMenuItem {
                  _href "/components/dropdown-menu#components-dropdown-menu"
                  "Dropdown menu guidance"
              }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Record review"
                  fveDropdownMenuItemLeading { menuLeadingIcon }
                  fveDropdownMenuItemShortcut { "R" }
              }
          }
          fveDropdownMenuSeparator { () }
          fveDropdownMenuGroup {
              _label "Reports"

              if refreshed then
                  fveDropdownMenuItem {
                      _dataOn ("click", "$menuActivations++")
                      "Review refreshed actions"
                  }
              else
                  fveDropdownMenuItem {
                      _dataOn ("click", "@get('/components/menus/actions')")
                      "Refresh actions"
                  }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Export file"
                  _disabled true
              }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Syncing"
                  _readonly true
                  _ariaBusy true
              }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Create report"
              }

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuActivations++")
                  "Close item"
              }
          }
          fveDropdownMenuSeparator { () }
          fveDropdownMenuGroup {
              _label "Display"

              fveDropdownMenuItem {
                  _dataOn ("click", "$menuCompact = !$menuCompact")
                  _role "menuitemcheckbox"
                  "Compact rows"
                  _dataAttr ("aria-checked", "(" + "$menuCompact" + ") ? 'true' : 'false'")
              }
          }
          fveDropdownMenuSeparator { () }
          fveDropdownMenuItem {
              _dataOn ("click", "$menuDeletes++")
              "Delete draft"
              _color Color.Error
          } ]

    let actionMenu refreshed =
        fveDropdownMenu {
            _id "components-menu-actions"
            _ariaLabel "Actions"
            fveDropdownMenuTrigger { fveButton { "Actions" } }
            yield! (dropdownMenuItems refreshed)
            _alignment DropdownMenuAlignment.Start
        }

    let menuOverflowIcon = fveIcon { _name IconName.EllipsisHorizontal; _variant IconVariant.Micro }

    let menuTrailingChevron = fveIcon { _name IconName.ChevronRight; _variant IconVariant.Micro }

    let fullRowDropdownMenu =
        div {
            _class "w-64 max-w-full border border-[var(--fve-border)]"
            _attr ("data-signals__root__ifmissing", "{menuRegion: 'Side navigation'}")

            fveDropdownMenu {
                _id "components-menu-full-row"
                _class "w-full"
                _ariaLabel "Choose a navigation pattern"

                fveDropdownMenuTrigger {

                    _class
                        "flex min-h-10 w-full items-center rounded-none px-4 py-2 text-left text-sm leading-5 font-semibold text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] "

                    fun (triggerAttributes: HtmlAttribute list) ->
                        button {
                            for attribute in triggerAttributes do
                                attribute

                            span {
                                _class "flex w-full items-center justify-between gap-3"

                                span {
                                    _dataText "$menuRegion"
                                    "Side navigation"
                                }

                                fveIcon { _name IconName.ChevronDown; _variant IconVariant.Micro }
                            }
                        }

                }

                fveDropdownMenuItem {
                    _dataOn ("click", "$menuRegion = 'Side navigation'")
                    _role "menuitemradio"
                    "Side navigation"
                    _description "Persistent desktop navigation"
                    _ariaChecked true

                    _dataAttr ("aria-checked", "(" + "$menuRegion == 'Side navigation'" + ") ? 'true' : 'false'")
                }

                fveDropdownMenuItem {
                    _dataOn ("click", "$menuRegion = 'Breadcrumbs'")
                    _role "menuitemradio"
                    "Breadcrumbs"
                    _description "Location within a page hierarchy"

                    _dataAttr ("aria-checked", "(" + "$menuRegion == 'Breadcrumbs'" + ") ? 'true' : 'false'")
                }

                fveDropdownMenuSeparator { () }

                fveDropdownMenuItem {
                    _href "/components/side-nav"
                    "Read navigation guidance"
                    fveDropdownMenuItemTrailing { menuTrailingChevron }
                }

                _alignment DropdownMenuAlignment.Start
            }
        }

    let customDropdownMenu =
        div {
            _class "grid justify-items-start gap-3"
            _attr ("data-signals__root__ifmissing", "{customMenuClicks: 0}")

            fveDropdownMenu {
                _id "components-menu-custom"

                fveDropdownMenuTrigger {
                    _id "workspace-menu-control"

                    _class
                        "inline-flex items-center justify-center rounded-full border border-[var(--fve-border)] p-1.5 text-sm font-medium text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                    _dataOn ("click", "$customMenuClicks++")

                    fun (triggerAttributes: HtmlAttribute list) ->
                        button {
                            for attribute in triggerAttributes do
                                attribute

                            span {
                                _ariaHidden true

                                _class
                                    "flex size-9 items-center justify-center rounded-full bg-[var(--fve-brand-subtle)] text-xs font-semibold text-[var(--fve-brand-text)]"

                                "PW"
                            }

                            span {
                                _class "sr-only"
                                "Personal workspace"
                            }
                        }

                }

                fveDropdownMenuItem {
                    _href "/components/side-nav"
                    "Side navigation"
                }

                fveDropdownMenuItem {
                    _href "/components/breadcrumbs"
                    "Breadcrumbs"
                }
            }

            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Trigger activations: "

                span {
                    _dataText "$customMenuClicks"
                    "0"
                }
            }
        }

    let moreActionsMenu =
        fveDropdownMenu {
            _id "components-menu-more"
            _ariaLabel "More actions"

            fveDropdownMenuTrigger {
                fveButton {
                    _variant ButtonVariant.Ghost

                    menuOverflowIcon
                }
            }

            fveDropdownMenuGroup {
                _label "View"

                fveDropdownMenuItem {
                    _href "/components/dropdown-menu#components-dropdown-menu"
                    "Read menu guidance"
                }

                fveDropdownMenuItem {
                    _dataOn ("click", "$menuActivations++")
                    "Record secondary action"
                }
            }
        }

    let outlinedDropdownMenu =
        fveDropdownMenu {
            _id "components-menu-outline"

            fveDropdownMenuTrigger {
                fveButton {
                    _ariaLabel "More page actions"
                    menuOverflowIcon
                    _variant ButtonVariant.Outline
                }
            }

            fveDropdownMenuItem {
                _href "/components/dropdown-menu"
                "Read menu guidance"
            }

            fveDropdownMenuItem {
                _href "/components/button"
                "Read button guidance"
            }
        }

    let dropdownMenuRegion refreshed =
        div {
            _id "components-dropdown-menu-region"
            _attr ("data-signals__root__ifmissing", "{menuActivations: 0, menuDeletes: 0, menuCompact: false}")
            _class "grid justify-items-center text-center"

            div {
                _class "flex flex-wrap items-center justify-center gap-3"
                [ actionMenu refreshed; moreActionsMenu ]
            }

            p {
                _class "mt-4 text-sm text-[var(--fve-muted-text)]"
                _dataText "'Completed menu actions: ' + $menuActivations"
                "Completed menu actions: 0"
            }

            p {
                _class "mt-1 text-sm text-[var(--fve-critical-text)]"
                _dataText "'Delete activations: ' + $menuDeletes"
                "Delete activations: 0"
            }

            if refreshed then
                p {
                    _role "status"
                    _class "mt-1 text-sm text-[var(--fve-positive-text)]"
                    "Actions refreshed."
                }
        }

    let patchedDropdownMenuRegion = dropdownMenuRegion true

    let examples () =
            [ sample "dropdown-menu" "Basic menu" [ "basicDropdownMenu" ] (centered basicDropdownMenu)
              sample "dropdown-menu-icons" "Icons" [ "plusIcon"; "menuDocumentIcon"; "iconDropdownMenu" ] (centered iconDropdownMenu)
              sample "dropdown-menu-shortcuts" "Keyboard hints" [ "shortcutDropdownMenu" ] (centered shortcutDropdownMenu)
              |> note "B and P are the menu's built-in typeahead hints: type a letter to focus its item, then Enter to follow the link. They are not global application shortcuts."
              sample "dropdown-menu-checkboxes" "Checkboxes" [ "checkboxDropdownMenu" ] (centered checkboxDropdownMenu)
              sample "dropdown-menu-radio" "Radio choices" [ "radioDropdownMenu" ] (centered radioDropdownMenu)
              sample "dropdown-menu-disabled" "Disabled items" [ "disabledDropdownMenu" ] (centered disabledDropdownMenu)
              sample "dropdown-menu-destructive" "Destructive action" [ "destructiveDropdownMenu" ] (fieldSurface destructiveDropdownMenu)
              sample
                  "dropdown-menu-outline"
                  "Outlined page action"
                  [ "menuOverflowIcon"; "outlinedDropdownMenu" ]
                  (centered outlinedDropdownMenu)
              sample
                  "dropdown-menu-custom"
                  "Custom native control"
                  [ "customDropdownMenu" ]
                  (centered customDropdownMenu)
              sample
                  "dropdown-menu-full-row"
                  "Full context row and supporting descriptions"
                  [ "menuTrailingChevron"; "fullRowDropdownMenu" ]
                  (centered fullRowDropdownMenu)
              sample
                  "dropdown-menu-advanced"
                  "Grouped states and refresh"
                  [ "menuLeadingIcon"
                    "dropdownMenuItems"
                    "actionMenu"
                    "menuOverflowIcon"
                    "moreActionsMenu"
                    "dropdownMenuRegion" ]
                  (fieldSurface (dropdownMenuRegion false))
              sample "dropdown-menu-row-actions" "Row actions"
                  [ "RowActionAccount"; "rowActionAccounts"; "moreActionsIcon"; "rowActionMenu"; "rowActionMenuExample" ]
                  (connectedCentered rowActionMenuExample)
              |> note "The 28px icon-only trigger names its record and stays highlighted while open. Arrow keys navigate; Escape restores trigger focus. Delete opens the Application example's confirmation, rather than deleting immediately." ]


    let private galleryContent : Content =
        { description = "Present contextual actions and typed choices in an accessible dismissible menu."
          usage = [
              p {
                  "Use Dropdown menu for commands and compact choices, with arrow-key navigation and typeahead. For rich content, fields or mixed controls, use "
                  a { _href "/components/popover"; _class "text-[var(--fve-brand-text)] underline"; "Popover" }
                  "."
              }
          ]
          sections = []
          apiCategory = Some "dropdown-menu" }

    let dropdownMenuPage =
        gallery dropdownMenuRegistration galleryContent (examples ())
