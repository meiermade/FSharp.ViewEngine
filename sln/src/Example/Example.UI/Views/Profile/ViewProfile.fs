namespace Example.UI.Views

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open type Html
open type Svg
open type Datastar
open Model
open ViewData
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

            fveRadioGroup {
                _orientation Orientation.Horizontal
                _ariaLabelledby (appearanceId + "-label")
                _dataSignals (appearanceSignal, ["root"; "ifmissing"], "'system'")
                _dataInit $"queueMicrotask(() => {{ ${appearanceSignal} = window.fveColorMode.current() }})"
                _dataOn ("fve-color-mode", ["window"], $"${appearanceSignal} = window.fveColorMode.current()")

                for attribute in ColorMode.nativeChoiceAttributes do
                    attribute

                fveFieldLegend {
                    _id (appearanceId + "-label")
                    "Appearance"
                }

                for value, label in [ "system", "System"; "light", "Light"; "dark", "Dark" ] do
                    fveField {
                        _layout FieldLayout.Horizontal
                        fveRadio {
                            _id (appearanceId + "-" + value)
                            _name (elementId query "appearance")
                            _value value
                            _checked (value.Equals "system")
                            _dataBind (appearanceSignal, ["root"])
                        }
                        fveLabel { _for (appearanceId + "-" + value); label }
                    }
            }
        }
// Contextual editors validate only; dismissal returns directly to the originating page.
