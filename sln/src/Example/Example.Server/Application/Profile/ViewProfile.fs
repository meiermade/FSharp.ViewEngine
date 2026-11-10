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

module internal ViewProfile =
    let profile (query: Query) =
        let appearanceId = elementId query "template-appearance"
        let appearanceSignal = appearanceId.Replace('-', '_') + "_value"

        fveSectionStack {
            _class "w-full"

            Layout.sectionWithEdit
                "Details"
                (applicationHref query ApplicationPage.EditProfile)
                (div {
                    _class "grid grid-cols-[auto_minmax(0,1fr)] items-start gap-5"

                    fveAvatar {
                        _ariaLabel "Andrew Meier"
                        "AM"
                    }

                    fveDescriptionList {
                        fveDescriptionListItem {
                            _label "Name"
                            "Andrew Meier"
                        }

                        fveDescriptionListItem {
                            _label "Email"
                            "andrew@meiermade.com"
                        }

                        fveDescriptionListItem {
                            _label "Time zone"
                            "America/Chicago"
                        }

                        _columns DescriptionListColumns.Three
                    }
                })

            fieldset {
                _role "radiogroup"
                _ariaLabelledby (appearanceId + "-label")
                _attr ("data-signals__ifmissing", $"{{{appearanceSignal}: 'system'}}")
                _dataInit $"queueMicrotask(() => {{ ${appearanceSignal} = window.fveColorMode.current() }})"
                _dataOn ("fve-color-mode__window", $"${appearanceSignal} = window.fveColorMode.current()")

                for attribute in ColorMode.nativeChoiceAttributes do
                    attribute

                legend {
                    _id (appearanceId + "-label")
                    _class "mb-3 text-sm font-semibold"
                    "Appearance"
                }

                div {
                    _class "inline-flex max-w-full gap-1 rounded-lg bg-[var(--fve-surface-hover)] p-1"

                    for value, label in [ "system", "System"; "light", "Light"; "dark", "Dark" ] do
                        Html.label {
                            _class
                                "flex min-h-8 cursor-pointer items-center justify-center rounded-md px-3 text-sm font-medium text-[var(--fve-muted-text)] has-[:checked]:bg-[var(--fve-surface)] has-[:checked]:text-[var(--fve-text)] has-[:checked]:shadow-sm has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-[var(--fve-brand-ring)]"

                            input {
                                _type "radio"
                                _name (elementId query "appearance")
                                _value value
                                _checked (value.Equals "system")
                                _class "sr-only"
                                _dataBind appearanceSignal
                            }

                            label
                        }
                }
            }
        }
// Contextual editors validate only; dismissal returns directly to the originating page.
