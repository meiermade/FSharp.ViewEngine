namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>day-calendar</category>
[<NoEquality; NoComparison>]
type DayCalendarConfig<'destination> = private DayCalendarConfig of CalendarDisplayConfig<'destination>

/// <summary>A focused day event calendar. Dates, destinations and time zones remain consumer-owned.</summary>
/// <category>day-calendar</category>
[<RequireQualifiedAccess>]
module DayCalendar =
    let create label (date:DateOnly) (events:CalendarEvent<'destination> list) =
        CalendarRendering.create label CalendarPeriod.Day date events |> DayCalendarConfig
    let withPrevious destination (DayCalendarConfig config) = CalendarRendering.withPrevious destination config |> DayCalendarConfig
    let withNext destination (DayCalendarConfig config) = CalendarRendering.withNext destination config |> DayCalendarConfig
    let withToday date destination (DayCalendarConfig config) = CalendarRendering.withToday date destination config |> DayCalendarConfig
    let withSelectedDate date (DayCalendarConfig config) = CalendarRendering.withSelectedDate date config |> DayCalendarConfig
    let withDateDestination destination (DayCalendarConfig config) = CalendarRendering.withDateDestination destination config |> DayCalendarConfig
    let withEmptyState content (DayCalendarConfig config) = CalendarRendering.withEmptyState content config |> DayCalendarConfig
    let loading (DayCalendarConfig config) = CalendarRendering.loading config |> DayCalendarConfig
    let withError message (DayCalendarConfig config) = CalendarRendering.withError message config |> DayCalendarConfig
    let withUnavailable message (DayCalendarConfig config) = CalendarRendering.withUnavailable message config |> DayCalendarConfig
    let withStateAction action (DayCalendarConfig config) = CalendarRendering.withStateAction action config |> DayCalendarConfig
    let render resolve (DayCalendarConfig config) = CalendarRendering.render resolve (fun _ _ -> empty) None config
