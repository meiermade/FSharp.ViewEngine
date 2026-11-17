namespace Docs.ComponentPages.ToggleGroup

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
open Docs.Common.ChoicesExamples
open Docs.Common.SharedExamples
open Docs.Common.SamplesExamples
open Docs.Common.SourceListsExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let toggleGroupRegistration =
        registration "components-toggle-group" "/components/toggle-group" "Toggle group" "Toggle group"

    let defaultToggleGroup =
        fveToggleGroup {
            _id "components-alignment-default"
            _name "alignment-default"
            _ariaLabel "Text alignment"

            fveToggleGroupItem {
                _value "left"
                "Left"
            }

            fveToggleGroupItem {
                _value "center"
                "Center"
            }

            fveToggleGroupItem {
                _value "right"
                "Right"
            }
        }

    let singleToggleGroup =
        fveToggleGroup {
            _id "components-alignment-group"
            _name "alignment"
            _ariaLabel "Text alignment"
            _value "left"

            fveToggleGroupItem {
                _value "left"
                "Left"
            }

            fveToggleGroupItem {
                _value "center"
                "Center"
            }

            fveToggleGroupItem {
                _value "right"
                "Right"
            }
        }

    let multipleToggleGroup =
        fveMultiToggleGroup {
            _id "components-formatting-group"
            _name "formatting"
            _ariaLabel "Text formatting"
            _values [ "bold" ]
            _variant ToggleGroupVariant.Spaced
            _size Size.Small

            fveToggleGroupItem {
                _value "bold"
                _label "Bold"
                plusIcon
                "Bold"
            }

            fveToggleGroupItem {
                _value "italic"
                _label "Italic"
                refreshIcon
                "Italic"
            }

            fveToggleGroupItem {
                _value "strike"
                _disabled true
                "Strike"
            }
        }

    let connectedToggleGroups =
        div {
            _class "grid justify-items-start gap-4"

            fveMultiToggleGroup {
                _id "connected-formats"
                _name "connected-format"
                _ariaLabel "Connected formats"
                _values [ "bold"; "italic" ]

                for value, label in [ "bold", "Bold"; "italic", "Italic"; "underline", "Underline" ] do
                    fveToggleGroupItem {
                        _value value
                        label
                    }
            }

            fveToggleGroup {
                _id "single-toggle-item"
                _name "pinned"
                _ariaLabel "Pinned item"
                _allowEmpty true

                fveToggleGroupItem {
                    _value "pinned"
                    "Pinned"
                }
            }
        }

    let toggleGroupFormRegion selected (validation: string option) result =
        div {
            _id "components-toggle-form-region"
            _class "grid gap-3"

            form {
                _class "grid gap-3"
                _dataOn ("submit", "@post('/components/choices/formatting', {contentType: 'form'})")

                fveMultiToggleGroup {
                    _id "components-saved-formats"
                    _name "format"
                    _ariaLabel "Saved formatting"
                    _values selected
                    _variant ToggleGroupVariant.Spaced

                    fveToggleGroupItem {
                        _value "bold"
                        "Bold"
                    }

                    fveToggleGroupItem {
                        _value "italic"
                        "Italic"
                    }

                    fveToggleGroupItem {
                        _value "strike"
                        _disabled true
                        "Strike"
                    }
                }

                match validation with
                | Some message -> fveFieldError { message }
                | None -> ()

                choiceSubmitButton "components-toggle-submit" "Save formatting"
            }

            choiceResult "components-toggle-result" result
        }

    let verticalToggleGroup =
        fveToggleGroup {
            _id "components-view-group"
            _name "view"
            _ariaLabel "Record view"
            _value "summary"
            _orientation Orientation.Vertical

            fveToggleGroupItem {
                _value "summary"
                "Summary"
            }

            fveToggleGroupItem {
                _value "activity"
                "Activity"
            }

            fveToggleGroupItem {
                _value "audit"
                "Audit log"
            }
        }

    let examples () =
            [ sample "toggle-group-default" "Default" [ "defaultToggleGroup" ] (centered defaultToggleGroup)
              sample "toggle-group-single" "Single selection" [ "singleToggleGroup" ] (centered singleToggleGroup)
              sample
                  "toggle-group-connected"
                  "Connected multiple and single-item groups"
                  [ "connectedToggleGroups" ]
                  (centered connectedToggleGroups)
              sample
                  "toggle-group-multiple"
                  "Multiple, spaced and unavailable"
                  [ "plusIcon"; "refreshIcon"; "multipleToggleGroup" ]
                  (centered multipleToggleGroup)
              sample
                  "toggle-group-submission"
                  "Submission"
                  (formSource @ [ "toggleGroupFormRegion" ])
                  (fieldSurface (toggleGroupFormRegion [ "bold" ] None None))
              sample
                  "toggle-group-vertical"
                  "Vertical orientation"
                  [ "verticalToggleGroup" ]
                  (centered verticalToggleGroup) ]


    let private galleryContent : Content =
        { description = "Choose one or more pressed values from an accessible oriented toggle group."
          usage = []
          sections = []
          apiCategory = Some "toggle-group" }

    let toggleGroupPage = gallery toggleGroupRegistration galleryContent (examples ())
