namespace Docs.ComponentPages.TimePicker

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
    let timePickerRegistration =
        registration "components-time-picker" "/components/time-picker" "Time picker" "Time picker"

    let defaultTimePicker =
        fveField {
            fveLabel { _for "default-time"; "Time" }
            fveTimePicker { _id "default-time"; _name "time"; _ariaLabel "Time"; _value (TimeOnly(9, 30)) }
        }

    let boundedTimePicker =
        fveField {
            fveLabel { _for "office-time"; "Office visit" }
            fveTimePicker {
                _id "office-time"; _name "office-time"; _ariaLabel "Office visit"
                _value (TimeOnly(13, 15)); _min (TimeOnly(8, 15)); _max (TimeOnly(18, 15))
                _interval 15; _format TimeFormat.TwentyFourHour
            }
            fveFieldDescription { "24-hour display, in 15-minute intervals from 08:15 to 18:15." }
        }

    let timePickerStates =
        div {
            _class "grid w-full min-w-0 gap-4 sm:grid-cols-2"
            for id, label, disabled, readOnly in ["disabled-time", "Disabled", true, false; "readonly-time", "Read-only", false, true] do
                fveField {
                    fveLabel { _for id; label }
                    fveTimePicker { _id id; _name id; _ariaLabel label; _value (TimeOnly(9, 0)); _disabled disabled; _readonly readOnly }
                }
        }

    let timePickerExamples () =
        [ sample "time-picker-default" "Default" ["defaultTimePicker"] (fieldSurface defaultTimePicker)
          sample "time-picker-bounds" "24-hour display and bounds" ["boundedTimePicker"] (fieldSurface boundedTimePicker)
          sample "time-picker-states" "Disabled and read-only" ["timePickerStates"] (fieldSurface timePickerStates)
          |> note "Import Assets/time-picker.js and Assets/popover.js. TimeOnly values submit as HH:mm; the native time input remains usable without JavaScript. Availability and time zones belong to the application." ]

    let examples () = timePickerExamples ()


    let private galleryContent : Content =
        { description = "Type or choose a local time with configurable intervals, bounds and 12/24-hour display."
          usage = []
          sections = []
          apiCategory = Some "time-picker" }

    let timePickerPage = gallery timePickerRegistration galleryContent (timePickerExamples ())
