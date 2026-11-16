namespace Docs.ComponentPages.MonthCalendar

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
open Docs.Common.CalendarExamples
open Docs.Common.SamplesExamples
open Docs.Common.CalendarSourceExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let monthCalendarRegistration =
        registration "components-month-calendar" "/components/month-calendar" "Month calendar" "Month calendar"

    let monthSelection =
        fveMonthCalendarSelection {
            _id "month-selection"
            _name "selectedDate"
            _ariaLabel "Date"
        }

    let monthSelectionSingle =
        fveMonthCalendarSelection {
            _id "selected-month-calendar"
            _name "selectedDate"
            _ariaLabel "Selected date"

            _value (
                ((DateOnly(2026, 9, 18)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _displayMonth (DateOnly(2026, 9, 1))
            _clearable true
        }

    let currentDateMonthSelection =
        fveMonthCalendarSelection {
            _id "current-date-month-calendar"
            _name "currentDateSelection"
            _ariaLabel "Date with current-day highlight"
            _value "2026-11-12"
            _currentDate (DateOnly(2026, 11, 5))
            _clearable true
        }

    let todayMonthSelection =
        form {
            _id "today-month-form"
            _class "grid justify-items-start gap-3"
            fveMonthCalendarSelection {
                _id "today-month-calendar"
                _name "shortcutDate"
                _ariaLabel "Date with Today shortcut"
                _value "2026-09-18"
                _displayMonth (DateOnly(2026, 9, 1))
                _today (DateOnly.FromDateTime DateTime.Today)
                _clearable true
            }
            fveButton { _type "reset"; "Reset date" }
        }

    let monthSelectionRange =
        fveMonthCalendarSelection {
            _id "date-range-calendar"
            _name "rangeStart"
            _ariaLabel "Date range"
            _mode CalendarSelectionMode.Range

            _value (
                ((DateOnly(2026, 9, 14)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _endValue (
                ((DateOnly(2026, 9, 18)): System.DateOnly)
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            )

            _rangeEndName "rangeEnd"
            _displayMonth (DateOnly(2026, 9, 1))
            _weekStart MonthCalendarWeekStart.Monday
        }

    let boundedMonthSelection =
        fveMonthCalendarSelection {
            _id "booking-month-calendar"
            _name "bookingDate"
            _today (DateOnly.FromDateTime DateTime.Today)
            _ariaLabel "Booking date"
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

            _lang "en-GB"
            _weekStart MonthCalendarWeekStart.Monday
        }

    let captionMonthSelection =
        fveMonthCalendarSelection {
            _id "birth-month-calendar"
            _name "birthDate"
            _ariaLabel "Choose a birth date"

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

    let pendingMonthSelection =
        fveMonthCalendarSelection {
            _id "pending-month-calendar"
            _name "availableDate"
            _today (DateOnly.FromDateTime DateTime.Today)
            _ariaLabel "Checking available dates"
            _displayMonth (DateOnly(2026, 9, 1))
            _ariaBusy true
        }

    let readOnlyMonthSelection =
        fveMonthCalendarSelection {
            _id "readonly-month-calendar"
            _name "readonlyDate"
            _today (DateOnly.FromDateTime DateTime.Today)
            _ariaLabel "Read-only date"
            _value "2026-09-18"
            _displayMonth (DateOnly(2026, 9, 1))
            _readonly true
        }

    let defaultMonthCalendar =
        fveMonthCalendar {
            _label "Coastal training month"
            _date calendarDemoDate
            defaultCalendarEvent
        }

    let calendarMonthEvents =
        calendarDemoEvents
        @ [ CalendarEvent.create
                "training-day"
                "Team training"
                calendarDemoDate
                (calendarDateDestination calendarDemoDate)
            |> CalendarEvent.withEndDate (calendarDemoDate.AddDays 4)
            CalendarEvent.create
                "equipment-preparation"
                "Equipment preparation"
                (calendarDemoDate.AddDays 1)
                (calendarDateDestination (calendarDemoDate.AddDays 1))
            |> CalendarEvent.withEndDate (calendarDemoDate.AddDays 3)
            CalendarEvent.create
                "site-preparation"
                "Site preparation"
                (DateOnly(2026, 8, 30))
                (calendarDateDestination (DateOnly(2026, 8, 31)))
            |> CalendarEvent.withEndDate (DateOnly(2026, 9, 2))
            CalendarEvent.create
                "course-setup"
                "Course setup"
                (DateOnly(2026, 9, 28))
                (calendarDateDestination (DateOnly(2026, 9, 28)))
            |> CalendarEvent.withEndDate (DateOnly(2026, 10, 2))
            CalendarEvent.create
                "setup-review"
                "Course setup and safety review"
                calendarDemoDate
                (calendarEventDestination "setup-review")
            |> CalendarEvent.withTime (TimeOnly(8, 30)) (TimeOnly(9, 15))
            |> CalendarEvent.withDetail "Review the route and equipment before the first session."
            CalendarEvent.create
                "team-debrief"
                "Afternoon team debrief"
                calendarDemoDate
                (calendarEventDestination "team-debrief")
            |> CalendarEvent.withTime (TimeOnly(15, 30)) (TimeOnly(16, 0)) ]

    let calendarMonthExample =
        fveMonthCalendar {
            _label "Team calendar"
            _date calendarDemoDate
            yield! (calendarMonthEvents: CalendarEvent<string> list)
            _id "team-month-calendar"

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.dates CalendarView.Month calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let compactMonthCalendar =
        fveMonthCalendar {
            _label "Training dates"
            _date calendarDemoDate
            yield! (calendarDemoEvents: CalendarEvent<string> list)
            _layout MonthCalendarLayout.Compact

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.compactMonthDates calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let examples () =
            [ sample
                  "month-calendar-default"
                  "Default"
                  (calendarDefaultSource "defaultMonthCalendar")
                  (calendarSurface defaultMonthCalendar)
              sample
                  "month-calendar-events"
                  "Events and date navigation"
                  [ "calendarDemoDate"
                    "calendarEventDestination"
                    "calendarDemoEvents"
                    "calendarDateDestination"
                    "calendarMonthEvents"
                    "calendarMonthExample" ]
                  (calendarSurface calendarMonthExample)
              sample
                  "month-calendar-compact"
                  "Compact month"
                  (calendarExampleSource "compactMonthCalendar")
                  (calendarSurface compactMonthCalendar)
              sample
                  "month-calendar-selection"
                  "Single-date selection"
                  [ "monthSelection" ]
                  (calendarSurface monthSelection)
              sample
                  "month-calendar-single"
                  "Clearable single date"
                  [ "monthSelectionSingle" ]
                  (calendarSurface monthSelectionSingle)
              sample "month-calendar-current-date" "Current date" [ "currentDateMonthSelection" ] (calendarSurface currentDateMonthSelection)
              |> note "Today has a subtle highlight by default, separate from selection. Use _currentDate for the application's date/timezone without adding a shortcut. This example fixes today to November 5, 2026; November 12 is selected."

              sample "month-calendar-today" "Today shortcut" [ "todayMonthSelection" ] (calendarSurface todayMonthSelection)
              |> note "_today supplies the application's current DateOnly and opts into the shortcut, independently of _clearable. This example uses the host's current date. Clear stays at the start; Today stays at the end."
              sample "month-calendar-range" "Date range" [ "monthSelectionRange" ] (calendarSurface monthSelectionRange)
              sample
                  "month-calendar-bounds"
                  "Bounds, unavailable dates and Monday start"
                  [ "boundedMonthSelection" ]
                  (calendarSurface boundedMonthSelection)
              sample
                  "month-calendar-caption"
                  "Month and year selectors"
                  [ "captionMonthSelection" ]
                  (calendarSurface captionMonthSelection)
              sample
                  "month-calendar-pending"
                  "Busy availability"
                  [ "pendingMonthSelection" ]
                  (calendarSurface pendingMonthSelection)
              sample
                  "month-calendar-readonly"
                  "Read-only selection"
                  [ "readOnlyMonthSelection" ]
                  (calendarSurface readOnlyMonthSelection) ]


    let private galleryContent : Content =
        { description = "Display a month in full or compact form, or select a date or range."
          usage = []
          sections = []
          apiCategory = Some "month-calendar" }

    let monthCalendarPage =
        gallery monthCalendarRegistration galleryContent (examples ())
