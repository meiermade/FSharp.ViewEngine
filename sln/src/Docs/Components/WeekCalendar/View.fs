namespace Docs.ComponentPages.WeekCalendar

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
    let weekCalendarRegistration =
        registration "components-week-calendar" "/components/week-calendar" "Week calendar" "Week calendar"

    let defaultWeekCalendar =
        fveWeekCalendar {
            _label "Coastal training week"
            _date calendarDemoDate
            defaultCalendarEvent
        }

    let calendarWeekExample =
        fveWeekCalendar {
            _id "calendar-week-events"
            _label "Training week"
            _date calendarDemoDate
            yield! (calendarDemoEvents |> List.map (CalendarEvent.withPreview (p { "Arrive ten minutes before the session starts." })))

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.dates CalendarView.Week calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let weekControlsPreview = calendarControlsExample [Docs.Web.PreviewUpdate.form "week-calendar-preview"] true

    let weekCalendarExamplesFor selection =
        [ sample
              "week-calendar-default"
              "Default"
              (calendarDefaultSource "defaultWeekCalendar")
              (calendarSurface defaultWeekCalendar)
          sample
              "week-calendar-events"
              "Events and date navigation"
              (calendarExampleSource "calendarWeekExample")
              (connectedCalendarSurface calendarWeekExample)
          sample
              "week-calendar-controls"
              "Shared controls and event details"
              ["CalendarDemoSelection"; "calendarDemoDate"; "defaultCalendarSelection"; "calendarReviewHref"; "calendarAvailableRanges"; "calendarAvailabilityEvents"; "calendarReviewControls"; "calendarControlsExample"]
              (connectedCalendarSurface (weekControlsPreview selection))
          |> note "The same native controls, bounds, density and rounded event previews work in a week. The consumer owns Day/Week navigation and panel content. Import Assets/popover.js once." ]

    let weekCalendarExamples = weekCalendarExamplesFor defaultCalendarSelection

    let examples () = weekCalendarExamples


    let private galleryContent : Content =
        { description = "Display a week in seven shared time columns with horizontal and vertical scrolling."
          usage = []
          sections = []
          apiCategory = Some "week-calendar" }

    let weekCalendarPageFor selection = gallery weekCalendarRegistration galleryContent (weekCalendarExamplesFor selection)

    let weekCalendarPage = weekCalendarPageFor defaultCalendarSelection
