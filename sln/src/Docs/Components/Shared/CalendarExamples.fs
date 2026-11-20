namespace Docs.Common

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

module CalendarExamples =
    let calendarDemoDate = DateOnly(2026, 9, 17)

    let calendarEventDestination id =
        "/components/page-examples/scheduling?item=" + id

    let calendarDemoEvents =
        [ CalendarEvent.create
              "lesson-201"
              "Coastal trail lesson"
              calendarDemoDate
              (calendarEventDestination "lesson-201")
          |> CalendarEvent.withTime (TimeOnly(10, 0)) (TimeOnly(11, 30))
          |> CalendarEvent.withDetail "Maya and Sam · Andy Meier"
          CalendarEvent.create "camp-017" "Beginner riding camp" calendarDemoDate (calendarEventDestination "camp-017")
          |> CalendarEvent.withTime (TimeOnly(10, 30)) (TimeOnly(12, 0))
          |> CalendarEvent.withDetail "Overlaps the trail lesson by one hour"
          CalendarEvent.create
              "lesson-202"
              "Cornering fundamentals"
              (calendarDemoDate.AddDays 1)
              (calendarEventDestination "lesson-202")
          |> CalendarEvent.withTime (TimeOnly(9, 0)) (TimeOnly(10, 0))
          CalendarEvent.create
              "ride-019"
              "Open track practice"
              (calendarDemoDate.AddDays 2)
              (calendarEventDestination "ride-019")
          |> CalendarEvent.withTime (TimeOnly(13, 0)) (TimeOnly(15, 0))
          CalendarEvent.create
              "return-020"
              "Equipment return"
              (calendarDemoDate.AddDays 7)
              (calendarEventDestination "return-020")
          |> CalendarEvent.withTime (TimeOnly(16, 0)) (TimeOnly(16, 30)) ]

    let defaultCalendarEvent =
        CalendarEvent.create
            "lesson-201-default"
            "Coastal trail lesson"
            calendarDemoDate
            (calendarEventDestination "lesson-201-default")

    let calendarDateDestination (date: DateOnly) =
        "/components/page-examples/scheduling?view=day&range="
        + string (date.DayNumber - calendarDemoDate.DayNumber)

    type CalendarDemoSelection =
        { date:DateOnly; start:TimeOnly; minutes:int; submitted:bool }

    let defaultCalendarSelection =
        { date = calendarDemoDate; start = TimeOnly(9, 0); minutes = 60; submitted = false }

    let calendarReviewHref weekly (date:DateOnly) (start:TimeOnly) minutes =
        (if weekly then "/components/week-calendar" else "/components/day-calendar")
        + "?calendarDate=" + date.ToString("yyyy-MM-dd")
        + "&selection.start=" + Uri.EscapeDataString(start.ToString("HH:mm"))
        + "&selection.minutes=" + string minutes

    // The demo consumer owns its opening hours, occupied periods and valid duration choices.

    let calendarAvailableRanges =
        [ for start in 9 * 60 .. 30 .. 16 * 60 + 30 do
              for minutes in [30; 60; 90] do
                  if start + minutes <= 17 * 60 && (start + minutes <= 11 * 60 || start >= 12 * 60) then
                      yield TimeOnly(start / 60, start % 60), minutes ]

    let calendarAvailabilityEvents (day:DateOnly) =
        [ CalendarEvent.create ("closed-" + string day.DayNumber) "Unavailable · Team briefing" day (calendarReviewHref false day (TimeOnly(8, 30)) 30)
          |> CalendarEvent.withTime (TimeOnly(8, 30)) (TimeOnly(9, 0))
          |> CalendarEvent.withTone CalendarEventTone.Muted
          |> CalendarEvent.withPreview (p { "The team is not available during this briefing." })
          CalendarEvent.create ("occupied-" + string day.DayNumber) "Your consultation" day (calendarReviewHref false day (TimeOnly(11, 0)) 60)
          |> CalendarEvent.withTime (TimeOnly(11, 0)) (TimeOnly(12, 0))
          |> CalendarEvent.withDetail "Confirmed · Unavailable for another booking"
          |> CalendarEvent.withTone CalendarEventTone.Neutral
          |> CalendarEvent.withPreview (p { "Choose a different time for a new consultation." }) ]

    let calendarReviewControls (submit: HtmlAttribute list) weekly (selection:CalendarDemoSelection) =
        fveCalendarControls {
            form {
                _method "get"
                _action (if weekly then "/components/week-calendar" else "/components/day-calendar")
                _ariaLabel "Calendar date"
                for attribute in submit do attribute
                _class "flex min-w-0 flex-wrap items-center gap-2"
                fveLabel {
                    _class "min-w-0"
                    span { _class "sr-only"; "Calendar date" }
                    fveInput {
                        _type InputType.Date
                        _size Size.Medium
                        _name "calendarDate"
                        _value (selection.date.ToString("yyyy-MM-dd"))
                        _min "2026-09-14"
                        _max "2026-09-20"
                        _required true
                        _dataOn ("change", "if (evt.target.value && evt.target.validity.valid) evt.target.form.requestSubmit()")
                        _class "min-w-0 w-auto"
                    }
                }
                input { _type "hidden"; _name "selection.start"; _value (selection.start.ToString("HH:mm")) }
                input { _type "hidden"; _name "selection.minutes"; _value (string selection.minutes) }
                noscript { fveButton { _type "submit"; "Show date" } }
            }
            nav {
                _ariaLabel "Calendar view"
                fveButtonGroup {
                    _ariaLabel "Choose calendar view"
                    for isWeek, label in [false, "Day"; true, "Week"] do
                        fveLink {
                            _href (calendarReviewHref isWeek selection.date selection.start selection.minutes)
                            _variant LinkVariant.Outline
                            if isWeek = weekly then _ariaCurrent "page"
                            _class "aria-[current=page]:bg-[var(--fve-brand-subtle)]"
                            label
                        }
                }
            }
        }

    let calendarControlsExample submit weekly (selection:CalendarDemoSelection) =
        let view = if weekly then CalendarView.Week else CalendarView.Day
        let calendar = if weekly then fveWeekCalendar else fveDayCalendar
        div {
            _id (if weekly then "week-calendar-preview" else "day-calendar-preview")
            _class "grid min-w-0 gap-3"
            calendar {
                _id (if weekly then "calendar-week-controls" else "calendar-day-controls")
                _label "Consultations"
                _date selection.date
                _selectedDate selection.date
                _hours (6, 22)
                _hourHeight 80
                _viewportHeight 400
                _fluidWidth true
                if not weekly then _dayHeading false
                calendarReviewControls submit weekly selection
                for day in Calendar.dates view selection.date do
                    yield! calendarAvailabilityEvents day
            }
            p {
                _role "status"
                _id (if weekly then "calendar-week-result" else "calendar-day-controls-result")
                _class "text-sm text-[var(--fve-muted-text)]"
                if selection.submitted then
                    let available = List.contains (selection.start, selection.minutes) calendarAvailableRanges
                    (if available then "Provisional selection: " else "Unavailable selection: ")
                    + selection.date.ToString("MMM d") + ", " + selection.start.ToString("HH:mm") + " · " + string selection.minutes + " minutes. No booking has been made."
                else "Open an event to see its details. Use the date and view controls to navigate."
            }
        }
