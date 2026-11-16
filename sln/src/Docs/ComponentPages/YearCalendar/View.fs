namespace Docs.ComponentPages.YearCalendar

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
    let yearCalendarRegistration =
        registration "components-year-calendar" "/components/year-calendar" "Year calendar" "Year calendar"

    let defaultYearCalendar =
        fveYearCalendar {
            _label "Coastal training year"
            _date calendarDemoDate
            defaultCalendarEvent
        }

    let calendarYearExample =
        fveYearCalendar {
            _label "Team year"
            _date calendarDemoDate
            yield! (calendarDemoEvents: CalendarEvent<string> list)

            fveCalendarToday {
                _date calendarDemoDate
                _href (calendarDateDestination calendarDemoDate)
            }

            _selectedDate calendarDemoDate

            yield!
                (Calendar.dates CalendarView.Year calendarDemoDate
                 |> List.map (fun calendarDate ->
                     fveCalendarDate {
                         _date calendarDate
                         _href (calendarDateDestination calendarDate)
                     }))
        }

    let yearCalendarExamples =
        [ sample
              "year-calendar-default"
              "Default"
              (calendarDefaultSource "defaultYearCalendar")
              (calendarSurface defaultYearCalendar)
          sample
              "year-calendar-events"
              "Event indicators and date navigation"
              (calendarExampleSource "calendarYearExample")
              (calendarSurface calendarYearExample) ]

    let examples () = yearCalendarExamples


    let private galleryContent : Content =
        { description = "Survey a year through twelve responsive compact month calendars."
          usage = []
          sections = []
          apiCategory = Some "year-calendar" }

    let yearCalendarPage = gallery yearCalendarRegistration galleryContent yearCalendarExamples
