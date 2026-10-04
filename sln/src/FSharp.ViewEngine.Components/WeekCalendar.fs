namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>week-calendar</category>
[<NoEquality; NoComparison>]
type WeekCalendarConfig<'destination> = private WeekCalendarConfig of CalendarDisplayConfig<'destination>

/// <summary>A focused week event calendar. Dates, destinations and time zones remain consumer-owned.</summary>
/// <category>week-calendar</category>
[<RequireQualifiedAccess>]
module WeekCalendar =
    let create label (date:DateOnly) (events:CalendarEvent<'destination> list) =
        CalendarRendering.create label CalendarPeriod.Week date events |> WeekCalendarConfig
    let withPrevious destination (WeekCalendarConfig config) = CalendarRendering.withPrevious destination config |> WeekCalendarConfig
    let withNext destination (WeekCalendarConfig config) = CalendarRendering.withNext destination config |> WeekCalendarConfig
    let withToday date destination (WeekCalendarConfig config) = CalendarRendering.withToday date destination config |> WeekCalendarConfig
    let withSelectedDate date (WeekCalendarConfig config) = CalendarRendering.withSelectedDate date config |> WeekCalendarConfig
    let withDateDestination destination (WeekCalendarConfig config) = CalendarRendering.withDateDestination destination config |> WeekCalendarConfig
    let withEmptyState content (WeekCalendarConfig config) = CalendarRendering.withEmptyState content config |> WeekCalendarConfig
    let loading (WeekCalendarConfig config) = CalendarRendering.loading config |> WeekCalendarConfig
    let withError message (WeekCalendarConfig config) = CalendarRendering.withError message config |> WeekCalendarConfig
    let withUnavailable message (WeekCalendarConfig config) = CalendarRendering.withUnavailable message config |> WeekCalendarConfig
    let withStateAction action (WeekCalendarConfig config) = CalendarRendering.withStateAction action config |> WeekCalendarConfig
    let render resolve (WeekCalendarConfig config) = CalendarRendering.render resolve (fun _ _ -> empty) None config
