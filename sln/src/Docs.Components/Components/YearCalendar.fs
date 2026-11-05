namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <summary>A native year event calendar. Event URLs and date navigation are application-owned.</summary>
/// <category>year-calendar</category>
[<AutoOpen>]
module YearCalendarDsl =
    let fveYearCalendar =
        CalendarDisplayBuilder(CalendarView.Year, fun attributes config ->
            CalendarRendering.render id (fun resolve config ->
                div {
                    _class "fve-calendar-year grid min-w-0 items-start gap-8 @min-[34rem]/fve-calendar:grid-cols-2 @min-[34rem]/fve-calendar:rounded-[var(--fve-radius-panel)] @min-[34rem]/fve-calendar:border @min-[34rem]/fve-calendar:border-[var(--fve-border)] @min-[34rem]/fve-calendar:p-4 @min-[64rem]/fve-calendar:grid-cols-3"
                    for month in 1..12 do
                        MonthCalendar.renderCompact resolve true None { config with date = DateOnly(config.date.Year, month, 1); label = config.label + " — " + DateOnly(config.date.Year, month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture); previous = None; next = None; attributes = [] }
                }) (ComponentElement.value "id" attributes) config)
