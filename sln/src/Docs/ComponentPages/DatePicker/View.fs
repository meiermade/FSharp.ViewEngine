namespace Docs.ComponentPages.DatePicker

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
    let datePickerRegistration =
        registration "components-date-picker" "/components/date-picker" "Date picker" "Date picker"

    let defaultDatePicker =
        fveField {
            fveLabel {
                _for "default-selected-date"
                "Date"
            }

            fveDatePicker {
                _id "default-selected-date"
                _name "defaultSelectedDate"
            }
        }

    let readOnlyDatePicker =
        fveField {
            fveLabel {
                _for "readonly-date"
                "Read-only date"
            }

            fveDatePicker {
                _id "readonly-date"
                _name "readonlyDate"
                _today (DateOnly.FromDateTime DateTime.Today)
                _value "2026-09-18"
                _readonly true
            }
        }

    let basicDatePicker =
        fveField {
            fveLabel {
                _id ("selected-date" + "-label")
                _for "selected-date"
                "Date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "selected-date"
                    _name "selectedDate"

                    _value (
                        ((DateOnly(2026, 9, 18)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _clearable true
                    _ariaDescribedby (String.concat " " [ ("selected-date" + "-description") ])
                }
            }

            fveFieldDescription {
                _id ("selected-date" + "-description")
                "Uses a canonical date-only form value."
            }
        }

    let currentDateDatePicker =
        fveField {
            fveLabel { _for "current-date-picker"; "Date with current-day highlight" }
            fveDatePicker {
                _id "current-date-picker"
                _name "currentDateSelection"
                _value "2026-11-12"
                _currentDate (DateOnly(2026, 11, 5))
                _clearable true
            }
        }

    let todayDatePicker =
        form {
            _id "today-picker-form"
            _class "grid justify-items-start gap-3"
            fveField {
                fveLabel { _for "today-selected-date"; "Date with Today shortcut" }
                fveDatePicker {
                    _id "today-selected-date"
                    _name "shortcutDate"
                    _value "2026-09-18"
                    _today (DateOnly.FromDateTime DateTime.Today)
                    _clearable true
                }
            }
            fveButton { _type "reset"; "Reset date" }
        }

    let rangeDatePicker =
        fveField {
            fveLabel {
                _id ("date-range" + "-label")
                _for "date-range"
                "Date range"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "date-range"
                    _name "periodStart"
                    _today (DateOnly.FromDateTime DateTime.Today)
                    _mode CalendarSelectionMode.Range

                    _value (
                        ((DateOnly(2026, 9, 14)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _endValue (
                        ((DateOnly(2026, 9, 18)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _rangeEndName "periodEnd"
                    _weekStart MonthCalendarWeekStart.Monday
                }
            }
        }

    let captionDatePicker =
        fveField {
            fveLabel {
                _id ("date-of-birth" + "-label")
                _for "date-of-birth"
                "Date of birth"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "date-of-birth"
                    _name "dateOfBirth"

                    _value (
                        ((DateOnly(2000, 6, 15)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _displayMonth (DateOnly(2000, 6, 1))

                    _min (
                        ((DateOnly(1920, 1, 1)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _max (
                        ((DateOnly(2026, 12, 31)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _captionLayout MonthCalendarCaptionLayout.Dropdown
                }
            }
        }

    let boundedDatePicker =
        fveField {
            fveLabel {
                _id ("travel-date" + "-label")
                _for "travel-date"
                "Travel date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "travel-date"
                    _name "travelDate"
                    _today (DateOnly.FromDateTime DateTime.Today)
                    _displayMonth (DateOnly(2026, 9, 1))

                    _min (
                        ((DateOnly(2026, 9, 10)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _max (
                        ((DateOnly(2026, 9, 25)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _unavailableDates (
                        [ DateOnly(2026, 9, 16); DateOnly(2026, 9, 17) ]
                        |> List.map (fun (date: System.DateOnly) ->
                            date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
                    )
                }
            }
        }

    let invalidDatePicker =
        fveField {
            fveLabel {
                _id ("required-date" + "-label")
                _for "required-date"
                "Date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "required-date"
                    _name "requiredDate"
                    _displayMonth (DateOnly(2026, 9, 1))
                    _ariaDescribedby (String.concat " " [ ("required-date" + "-validation") ])
                    _ariaInvalid true
                }
            }

            fveFieldError {
                _id ("required-date" + "-validation")
                _role "alert"
                "Choose an available date."
            }
        }

    let pendingDatePicker =
        fveField {
            fveLabel {
                _id ("availability-date" + "-label")
                _for "availability-date"
                "Availability date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "availability-date"
                    _name "availabilityDate"
                    _today (DateOnly.FromDateTime DateTime.Today)

                    _value (
                        ((DateOnly(2026, 9, 21)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _ariaBusy true
                }
            }
        }

    let disabledDatePicker =
        fveField {
            fveLabel {
                _id ("archived-date" + "-label")
                _for "archived-date"
                "Archived date"
            }

            fveFieldContent {
                fveDatePicker {
                    _id "archived-date"
                    _name "archivedDate"
                    _today (DateOnly.FromDateTime DateTime.Today)

                    _value (
                        ((DateOnly(2026, 9, 8)): System.DateOnly)
                            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    )

                    _disabled true
                }
            }
        }

    let examples () =
            [ sample "date-picker-default" "Default" [ "defaultDatePicker" ] (fieldSurface defaultDatePicker)
              sample "date-picker" "Clearable single date" [ "basicDatePicker" ] (fieldSurface basicDatePicker)
              sample "date-picker-current-date" "Current date" [ "currentDateDatePicker" ] (fieldSurface currentDateDatePicker)
              |> note "Today is subtly highlighted without being selected or adding a footer shortcut. _currentDate overrides the local rendering date for application timezones. This example fixes today to November 5, 2026; November 12 is selected."

              sample "date-picker-today" "Today shortcut" [ "todayDatePicker" ] (fieldSurface todayDatePicker)
              |> note "Pass _today using the application's date and timezone. Today selects the date and closes the picker; Reset restores the original value. Clear is configured separately."
              sample "date-picker-range" "Date range" [ "rangeDatePicker" ] (fieldSurface rangeDatePicker)
              |> note "In range mode, Today selects a complete one-day range (today–today). Bounds, unavailable dates, readonly and disabled still apply."
              sample
                  "date-picker-caption"
                  "Month and year selectors"
                  [ "captionDatePicker" ]
                  (fieldSurface captionDatePicker)
              sample
                  "date-picker-bounds"
                  "Bounds and unavailable dates"
                  [ "boundedDatePicker" ]
                  (fieldSurface boundedDatePicker)
              sample "date-picker-validation" "Validation" [ "invalidDatePicker" ] (fieldSurface invalidDatePicker)
              sample "date-picker-pending" "Pending" [ "pendingDatePicker" ] (fieldSurface pendingDatePicker)
              sample "date-picker-disabled" "Disabled" [ "disabledDatePicker" ] (fieldSurface disabledDatePicker)
              sample "date-picker-readonly" "Read-only" [ "readOnlyDatePicker" ] (fieldSurface readOnlyDatePicker) ]


    let private galleryContent : Content =
        { description = "Compose a native labelled date control with popover selection and caller-owned feedback."
          usage = []
          sections = []
          apiCategory = Some "date-picker" }

    let datePickerPage = gallery datePickerRegistration galleryContent (examples ())
