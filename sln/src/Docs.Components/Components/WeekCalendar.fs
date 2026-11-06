namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine

/// <summary>A native weekly event calendar. Event URLs and date navigation are application-owned.</summary>
/// <category>week-calendar</category>
[<AutoOpen>]
module WeekCalendarDsl =
    let fveWeekCalendar =
        CalendarDisplayBuilder(CalendarView.Week, fun attributes config ->
            CalendarRendering.render id (fun _ _ -> Html.empty) (ComponentElement.value "id" attributes) config)
