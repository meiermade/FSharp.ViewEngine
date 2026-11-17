namespace Docs.ComponentPages.DayCalendar

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
    let dayCalendarRegistration =
        registration "components-day-calendar" "/components/day-calendar" "Day calendar" "Day calendar"

    let defaultDayCalendar =
        fveDayCalendar {
            _label "Coastal training day"
            _date calendarDemoDate
            defaultCalendarEvent
        }

    let calendarDayExample =
        fveDayCalendar {
            _id "calendar-day-events"
            _label "Thursday sessions"
            _date calendarDemoDate
            yield! (calendarDemoEvents |> List.map (CalendarEvent.withPreview (p { "Arrive ten minutes before the session starts." })))

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.dates CalendarView.Day calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let calendarRangeExample (selection:CalendarDemoSelection) =
        form {
            _method "get"
            _action "/components/day-calendar"
            _ariaLabel "Review a consultation time"
            _data("fve-local-navigation", "true")
            _class "grid min-w-0 gap-3"
            input { _type "hidden"; _name "calendarDate"; _value (selection.date.ToString("yyyy-MM-dd")) }
            fveDayCalendar {
                _id "calendar-time-range"
                _label "Choose a consultation time"
                _date selection.date
                _hours (6, 22)
                _hourHeight 80
                _viewportHeight 320
                _dayHeading false
                _fluidWidth true
                yield! calendarAvailabilityEvents selection.date
                fveCalendarRange {
                    _name "selection"
                    _startTime selection.start
                    _minutes selection.minutes
                    _durations [30; 60; 90]
                    _availableRanges calendarAvailableRanges
                    _minuteStep 30
                }
            }
            div {
                _class "flex flex-wrap items-center gap-2"
                fveButton { _type "submit"; _color Color.Primary; "Review time" }
                fveButton { _type "reset"; _variant ButtonVariant.Outline; "Reset selection" }
            }
            if selection.submitted then
                p {
                    _id "calendar-day-result"
                    _role "status"
                    _class "text-sm text-[var(--fve-muted-text)]"
                    (if List.contains (selection.start, selection.minutes) calendarAvailableRanges then "Provisional selection: " else "Unavailable selection: ")
                    + selection.start.ToString("HH:mm") + " · " + string selection.minutes + " minutes."
                }
            p {
                _class "text-sm text-[var(--fve-muted-text)]"
                "Drag the range or its top/bottom grips. With the range focused, use arrow keys to move and Shift+Arrow to change duration. The native fields also work without JavaScript."
            }
        }

    let dayCalendarExamplesFor selection =
        [ sample
              "day-calendar-default"
              "Default"
              (calendarDefaultSource "defaultDayCalendar")
              (calendarSurface defaultDayCalendar)
          sample
              "day-calendar-events"
              "Overlapping sessions"
              (calendarExampleSource "calendarDayExample")
              (connectedCalendarSurface calendarDayExample)
          |> note "CalendarEvent.withPreview supplies consumer-authored details using Popover. Cards overlap without moving; pointer/keyboard activation raises a card and opens its details. Escape closes the panel and returns focus. Import Assets/popover.js once."
          sample
              "day-calendar-controls"
              "Compact controls"
              ["CalendarDemoSelection"; "calendarDemoDate"; "defaultCalendarSelection"; "calendarReviewHref"; "calendarAvailableRanges"; "calendarAvailabilityEvents"; "calendarReviewControls"; "calendarControlsExample"]
              (connectedCalendarSurface (calendarControlsExample false selection))
          |> note "Valid date changes submit the native GET form automatically; without JavaScript, Show date remains available. Rounded event cards keep their meaning in visible text; optional details reuse Popover."
          sample
              "day-calendar-range"
              "Provisional time selection"
              ["CalendarDemoSelection"; "calendarDemoDate"; "defaultCalendarSelection"; "calendarReviewHref"; "calendarAvailableRanges"; "calendarAvailabilityEvents"; "calendarRangeExample"]
              (connectedCalendarSurface (calendarRangeExample selection))
          |> note "Import Assets/calendar-range.js, Assets/time-picker.js, Assets/select.js and Assets/popover.js. fveCalendarRange stays on the Day calendar page: native selection.start and selection.minutes fields submit a draft, never a booking. Duration uses FVE Select with a native fallback. fve-calendar-range-change reports date, time, end, minutes and valid; the server remains authoritative." ]

    let dayCalendarExamples = dayCalendarExamplesFor defaultCalendarSelection

    let examples () = dayCalendarExamples


    let private galleryContent : Content =
        { description = "Display one day's events in a scrollable time grid with accessible event destinations."
          usage = []
          sections = []
          apiCategory = Some "day-calendar" }

    let dayCalendarPageFor selection = gallery dayCalendarRegistration galleryContent (dayCalendarExamplesFor selection)

    let dayCalendarPage = dayCalendarPageFor defaultCalendarSelection
