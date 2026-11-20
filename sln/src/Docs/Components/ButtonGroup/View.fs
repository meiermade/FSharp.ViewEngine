namespace Docs.ComponentPages.ButtonGroup

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
open Docs.Common.SharedExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let buttonGroupRegistration =
        registration "components-button-group" "/components/button-group" "Button group" "Button group"

    let defaultButtonGroup =
        fveButtonGroup {
            _ariaLabel "Message actions"
            fveButton { "Archive" }
            fveButton { "Report" }
        }

    let buttonGroupExample =
        div {
            _class "flex min-w-0 max-w-full flex-wrap items-center gap-2"

            fveButtonGroup {
                _ariaLabel "Back"

                fveButton {
                    _ariaLabel "Back"

                    span {
                        _ariaHidden true
                        backIcon
                    }
                }
            }

            fveButtonGroup {
                _ariaLabel "Message actions"
                fveButton { "Archive" }
                fveButton { "Report" }
            }

            fveButtonGroup {
                _ariaLabel "Scheduling actions"
                fveButton { "Snooze" }

                fveDropdownMenu {
                    _id "button-group-snooze-menu"
                    _ariaLabel "More scheduling actions"

                    fveDropdownMenuTrigger {
                        fveButton {
                            _variant ButtonVariant.Outline
                            moreActionsIcon
                        }
                    }

                    fveDropdownMenuItem {
                        _dataOn ("click", "$snoozeUntil = 'tomorrow'")
                        "Snooze until tomorrow"
                    }

                    fveDropdownMenuItem {
                        _dataOn ("click", "$snoozeUntil = 'next-week'")
                        "Snooze until next week"
                    }
                }
            }
        }

    let linkedButtonGroup =
        fveButtonGroup {
            _ariaLabel "Component destinations"

            fveLink {
                _variant LinkVariant.Outline
                _color Color.Neutral
                _href "/components/button"
                "Button"
            }

            fveLink {
                _variant LinkVariant.Outline
                _color Color.Neutral
                _href "/components/dropdown-menu"
                "Dropdown menu"
            }
        }

    let selectButtonGroup =
        fveButtonGroup {
            _ariaLabel "Export options"
            "Format"

            fveField {
                fveLabel {
                    _class "sr-only"
                    _for "groupExportFormat"
                    "Export format"
                }

                fveSelect {



                    _value "csv"
                    _nativeFallback true
                    _name "groupExportFormat"

                    fveSelectContent {
                        for choice in ([ ("csv", "CSV", false); ("json", "JSON", false); ("pdf", "PDF", false) ]) do
                            fveSelectItem {
                                let value, label, disabled = choice
                                _value value
                                _label label
                                _disabled disabled
                            }
                    }

                    _id "groupExportFormat"
                }
            }

            fveButton { "Export" }
        }

    let customButtonGroup =
        fveButtonGroup {
            _ariaLabel "Custom control actions"

            div {
                div {
                    button {
                        _type "button"
                        _data ("fve-group-control", "outline")

                        _class
                            "min-h-[var(--fve-control-min-height)] rounded-[var(--fve-radius-control)] border border-[var(--fve-border)] bg-[var(--fve-surface)] px-3 text-[length:var(--fve-control-font-size)] text-[var(--fve-text)] focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                        "Native action"
                    }
                }
            }

            fvePopover {
                _id "button-group-details"
                fvePopoverTrigger { fveButton { "Details" } }

                fvePopoverContent {
                    _ariaLabel "Action details"
                    fveButton { "Independent action" }
                }
            }
        }

    let verticalButtonGroup =
        fveButtonGroup {
            _orientation Orientation.Vertical
            _ariaLabel "Record view"
            fveButton { "Summary" }
            fveButton { "Activity" }
            fveButton { "Files" }
        }

    let examples () =
            [ sample "button-group-default" "Default" [ "defaultButtonGroup" ] (centered defaultButtonGroup)
              sample
                  "button-group"
                  "Grouped actions"
                  [ "backIcon"; "moreActionsIcon"; "buttonGroupExample" ]
                  (centered buttonGroupExample)
              sample
                  "button-group-links"
                  "Native link segments"
                  [ "linkedButtonGroup" ]
                  (connectedCentered linkedButtonGroup)
              sample
                  "button-group-select"
                  "Label and Select segments"
                  [ "selectButtonGroup" ]
                  (centered selectButtonGroup)
              sample
                  "button-group-custom"
                  "Custom controls and popup isolation"
                  [ "customButtonGroup" ]
                  (centered customButtonGroup)
              |> note
                  "Button, Link and Select participate automatically, including when used as overlay triggers. Mark a custom native control with data-fve-group-control=\"outline\" (or \"plain\" for a control without a border). Wrappers do not need special selectors. The group joins logical segments; controls inside a native popover or dialog keep independent geometry. Keep labels and other field presentation caller-owned."
              sample
                  "button-group-vertical"
                  "Vertical orientation"
                  [ "verticalButtonGroup" ]
                  (centered verticalButtonGroup) ]


    let private galleryContent : Content =
        { description = "Join related buttons, native links, Select controls, menu triggers and muted text segments. Each control keeps its own semantics and keyboard behavior; labels are noninteractive, not disabled buttons. Native links retain real destinations."
          usage = []
          sections = []
          apiCategory = Some "button-group" }

    let buttonGroupPage = gallery buttonGroupRegistration galleryContent (examples ())
