namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine

/// <summary>A native daily event calendar. Event URLs and date navigation are application-owned.</summary>
/// <category>day-calendar</category>
[<AutoOpen>]
module DayCalendarDsl =
    let fveDayCalendar =
        CalendarDisplayBuilder(CalendarView.Day, fun attributes config ->
            CalendarRendering.render id (fun _ _ -> Html.empty) (ComponentElement.value "id" attributes) config)
