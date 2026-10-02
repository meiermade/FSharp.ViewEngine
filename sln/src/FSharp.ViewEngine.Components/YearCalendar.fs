namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>year-calendar</category>
[<NoEquality; NoComparison>]
type YearCalendarConfig<'destination> = private YearCalendarConfig of CalendarDisplayConfig<'destination>

/// <summary>A focused year event calendar. Dates, destinations and time zones remain consumer-owned.</summary>
/// <category>year-calendar</category>
[<RequireQualifiedAccess>]
module YearCalendar =
    let create label (date:DateOnly) (events:CalendarEvent<'destination> list) =
        CalendarRendering.create label CalendarPeriod.Year date events |> YearCalendarConfig
    let withPrevious destination (YearCalendarConfig config) = CalendarRendering.withPrevious destination config |> YearCalendarConfig
    let withNext destination (YearCalendarConfig config) = CalendarRendering.withNext destination config |> YearCalendarConfig
    let withToday date destination (YearCalendarConfig config) = CalendarRendering.withToday date destination config |> YearCalendarConfig
    let withSelectedDate date (YearCalendarConfig config) = CalendarRendering.withSelectedDate date config |> YearCalendarConfig
    let withDateDestination destination (YearCalendarConfig config) = CalendarRendering.withDateDestination destination config |> YearCalendarConfig
    let withEmptyState content (YearCalendarConfig config) = CalendarRendering.withEmptyState content config |> YearCalendarConfig
    let loading (YearCalendarConfig config) = CalendarRendering.loading config |> YearCalendarConfig
    let withError message (YearCalendarConfig config) = CalendarRendering.withError message config |> YearCalendarConfig
    let withUnavailable message (YearCalendarConfig config) = CalendarRendering.withUnavailable message config |> YearCalendarConfig
    let withStateAction action (YearCalendarConfig config) = CalendarRendering.withStateAction action config |> YearCalendarConfig
    let render resolve (YearCalendarConfig config) =
        CalendarRendering.render resolve (fun resolve config ->
            div {
                _class "fve-calendar-year grid min-w-0 items-start gap-8 @min-[34rem]/fve-calendar:grid-cols-2 @min-[34rem]/fve-calendar:rounded-[var(--fve-radius-panel)] @min-[34rem]/fve-calendar:border @min-[34rem]/fve-calendar:border-[var(--fve-border)] @min-[34rem]/fve-calendar:p-4 @min-[64rem]/fve-calendar:grid-cols-3"
                for month in 1..12 do
                    MonthCalendar.renderCompact resolve true { config with date = DateOnly(config.date.Year, month, 1); label = config.label + " — " + DateOnly(config.date.Year, month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture); previous = None; next = None }
            }) config
