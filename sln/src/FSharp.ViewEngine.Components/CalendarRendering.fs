namespace FSharp.ViewEngine.Components

open System
open System.Globalization
open FSharp.ViewEngine
open type Html

/// <exclude/>
[<RequireQualifiedAccess>]
type internal CalendarPeriod = Day | Week | Month | Year

/// <summary>
/// An all-day event, or a minute-resolution interval on one consumer-local date.
/// Consumers split overnight/multi-day events and resolve time zones before rendering.
/// </summary>
/// <category>month-calendar</category>
[<NoEquality; NoComparison>]
type CalendarEvent<'destination> =
    internal
        { id:string
          title:string
          date:DateOnly
          time:(TimeOnly * TimeOnly) option
          detail:string option
          destination:'destination }

/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
module CalendarEvent =
    let create id title date destination =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable event ID is required."
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "An event title is required."
        { id = id; title = title; date = date; time = None; detail = None; destination = destination }
    let withTime (start:TimeOnly) (finish:TimeOnly) (event:CalendarEvent<'destination>) =
        if finish <= start || start.Ticks % TimeSpan.TicksPerMinute <> 0L || finish.Ticks % TimeSpan.TicksPerMinute <> 0L then
            invalidArg (nameof finish) "An event needs a positive, same-day interval in whole minutes."
        { event with time = Some (start, finish) }
    let withDetail detail (event:CalendarEvent<'destination>) = { event with detail = Some detail }

/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
type CalendarState = Ready | Loading | Error of message:string | Unavailable of message:string

/// <exclude/>
[<NoEquality; NoComparison>]
type internal CalendarDisplayConfig<'destination> =
        { label:string
          view:CalendarPeriod
          date:DateOnly
          events:CalendarEvent<'destination> list
          previous:'destination option
          next:'destination option
          today:(DateOnly * 'destination) option
          selectedDate:DateOnly option
          dateDestination:(DateOnly -> 'destination) option
          emptyState:HtmlElement
          state:CalendarState
          stateAction:HtmlElement option }

/// <exclude/>
[<RequireQualifiedAccess>]
module internal CalendarRendering =
    let create label view (date:DateOnly) (events:CalendarEvent<'destination> list) =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A calendar label is required."
        if date.Year < 2 || date.Year > 9998 then invalidArg (nameof date) "The displayed date must permit adjacent calendar weeks (years 2–9998)."
        if events |> List.distinctBy _.id |> List.length <> events.Length then invalidArg (nameof events) "Event IDs must be unique within a calendar."
        { label = label; view = view; date = date; events = events; previous = None; next = None; today = None
          selectedDate = None; dateDestination = None
          emptyState = p { _class "p-4 text-sm text-[var(--fve-muted-text)]"; "No events in this range." }
          state = CalendarState.Ready; stateAction = None }
    let withPrevious destination (config:CalendarDisplayConfig<'destination>) = { config with previous = Some destination }
    let withNext destination (config:CalendarDisplayConfig<'destination>) = { config with next = Some destination }
    /// The consumer supplies today's date in the displayed time zone and its destination.
    let withToday date destination config = { config with today = Some (date, destination) }
    let withSelectedDate date config = { config with selectedDate = Some date }
    let withDateDestination destination config = { config with dateDestination = Some destination }
    let withEmptyState emptyState (config:CalendarDisplayConfig<'destination>) = { config with emptyState = emptyState }
    let loading (config:CalendarDisplayConfig<'destination>) = { config with state = CalendarState.Loading; stateAction = None }
    let withError message (config:CalendarDisplayConfig<'destination>) =
        if String.IsNullOrWhiteSpace message then invalidArg (nameof message) "A calendar error message is required."
        { config with state = CalendarState.Error message; stateAction = None }
    let withUnavailable message (config:CalendarDisplayConfig<'destination>) =
        if String.IsNullOrWhiteSpace message then invalidArg (nameof message) "A calendar unavailable message is required."
        { config with state = CalendarState.Unavailable message; stateAction = None }
    let withStateAction action (config:CalendarDisplayConfig<'destination>) = { config with stateAction = Some action }

    let private format pattern (date:DateOnly) = date.ToString(pattern, CultureInfo.InvariantCulture)
    let private minute (time:TimeOnly) = time.Hour * 60 + time.Minute
    let private monday (date:DateOnly) = date.AddDays(-((int date.DayOfWeek + 6) % 7))
    let private viewName = function CalendarPeriod.Day -> "Day" | CalendarPeriod.Week -> "Week" | CalendarPeriod.Month -> "Month" | CalendarPeriod.Year -> "Year"
    let private dates config =
        let start, count =
            match config.view with
            | CalendarPeriod.Day -> config.date, 1
            | CalendarPeriod.Week -> monday config.date, 7
            | CalendarPeriod.Month ->
                let first = DateOnly(config.date.Year, config.date.Month, 1)
                let start = monday first
                let finish = monday (first.AddMonths(1).AddDays(-1)) |> fun day -> day.AddDays(6)
                start, finish.DayNumber - start.DayNumber + 1
            | CalendarPeriod.Year ->
                let start = DateOnly(config.date.Year, 1, 1)
                start, start.AddYears(1).DayNumber - start.DayNumber
        [ for offset in 0 .. count - 1 -> start.AddDays offset ]

    // Connected overlap groups share a lane count; disjoint events regain the full day width.
    let private placements events =
        let timed = events |> List.choose (fun event -> event.time |> Option.map (fun (start, finish) -> event, start, finish)) |> List.sortBy (fun (event, start, finish) -> start, finish, event.id)
        let rec takeGroup finish group remaining =
            match remaining with
            | (event, start, ending) :: rest when start < finish -> takeGroup (max finish ending) ((event, start, ending) :: group) rest
            | _ -> List.rev group, remaining
        let rec place remaining =
            match remaining with
            | [] -> []
            | (event, start, finish) :: rest ->
                let group, next = takeGroup finish [event, start, finish] rest
                let lanes = ResizeArray<TimeOnly>()
                let assigned =
                    group |> List.map (fun (event, start, finish) ->
                        let lane = lanes |> Seq.tryFindIndex (fun ending -> ending <= start) |> Option.defaultValue lanes.Count
                        if lane = lanes.Count then lanes.Add finish else lanes[lane] <- finish
                        event.id, lane + 1)
                (assigned |> List.map (fun (id, lane) -> id, (lane, lanes.Count))) @ place next
        place timed |> Map.ofList

    let render (resolve:'destination -> string) (renderYear:('destination -> string) -> CalendarDisplayConfig<'destination> -> HtmlElement) (config:CalendarDisplayConfig<'destination>) =
        let days = dates config
        let events = config.events |> List.filter (fun event -> event.date >= days.Head && event.date <= List.last days) |> List.sortBy (fun event -> event.date, event.time, event.id)
        let allDayRows = days |> List.map (fun date -> events |> List.filter (fun event -> event.date = date && event.time.IsNone) |> List.length) |> List.max
        let times = events |> List.choose _.time
        let startHour = times |> List.fold (fun hour (start, _) -> min hour start.Hour) 8
        let endHour = times |> List.fold (fun hour (_, finish) -> max hour ((minute finish + 59) / 60)) 18
        let timed = config.view = CalendarPeriod.Day || config.view = CalendarPeriod.Week
        let rangeLabel =
            match config.view with
            | CalendarPeriod.Month -> format "MMMM yyyy" config.date
            | CalendarPeriod.Day -> format "dddd, MMMM d, yyyy" config.date
            | CalendarPeriod.Week -> format "MMMM d" days.Head + "–" + format "MMMM d, yyyy" (List.last days)
            | CalendarPeriod.Year -> string config.date.Year
        let control = "inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
        let dateHeading date =
            let today = config.today |> Option.exists (fun (value, _) -> value = date)
            let selected = config.selectedDate = Some date
            h3 {
                _class ("fve-calendar-date m-0 text-sm font-medium " + (if timed then "sticky top-0 z-20 flex h-16 items-center border-b border-[var(--fve-border)] bg-[var(--fve-background)] p-2" else "p-0.5"))
                let content =
                    time {
                        _datetime (format "yyyy-MM-dd" date)
                        if today then _ariaCurrent "date"
                        _class (if today then "inline-flex rounded-full bg-[var(--fve-brand-solid)] px-2 py-1 text-white" else "inline-flex px-2 py-1")
                        span { _class (if timed then "hidden" else "inline"); string date.Day }
                        span { _class (if timed then "" else "sr-only"); format "ddd, MMM d" date }
                    }
                match config.dateDestination with
                | Some destination ->
                    a {
                        _href (resolve (destination date))
                        _ariaLabel ((if selected then "Selected: " else "Show ") + format "dddd, MMMM d, yyyy" date)
                        _class "inline-flex min-h-8 rounded-[var(--fve-radius-control)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                        content
                    }
                | None -> content
                if selected then span { _class "sr-only"; "Selected date" }
            }
        let eventContent event =
            a {
                _href (resolve event.destination)
                _class ("fve-calendar-event block min-w-0 rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)] no-underline [overflow-wrap:anywhere] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] " + (if timed && event.time.IsSome then "h-full overflow-auto p-1" else "p-2"))
                strong { _class "block text-sm font-medium"; event.title }
                span {
                    _class "block text-xs"
                    match event.time with
                    | Some (start, finish) -> start.ToString("h:mm tt", CultureInfo.InvariantCulture) + "–" + finish.ToString("h:mm tt", CultureInfo.InvariantCulture)
                    | None -> "All day"
                }
                match event.detail with
                | Some detail -> span { _class "mt-1 block text-xs"; detail }
                | None -> ()
            }
        section {
            _ariaLabel config.label
            _class "fve-calendar fve-control-small @container/fve-calendar grid min-w-0 grid-cols-1 gap-4 text-[var(--fve-text)]"
            _attr ("data-view", viewName config.view |> fun value -> value.ToLowerInvariant())
            header {
                _class "flex flex-wrap items-center justify-between gap-3"
                div {
                    h2 { _class "text-lg font-semibold text-[var(--fve-text)]"; config.label }
                    p { _class "text-sm text-[var(--fve-muted-text)]"; rangeLabel }
                }
                nav {
                    _ariaLabel (config.label + " date navigation")
                    _class "flex flex-wrap items-center gap-1"
                    for destination, label in [config.previous, "Previous"; config.today |> Option.map snd, "Today"; config.next, "Next"] do
                        match destination with
                        | Some target -> a { _href (resolve target); _class control; label }
                        | None -> ()
                }
            }
            match config.state with
            | CalendarState.Loading ->
                p { _role "status"; _ariaLive "polite"; _class "p-4 text-sm text-[var(--fve-muted-text)]"; "Loading calendar…" }
            | CalendarState.Error message ->
                div {
                    _role "alert"
                    _class "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-critical-subtle)] p-4 text-sm text-[var(--fve-critical-text)]"
                    p { message }
                    config.stateAction |> Option.defaultValue empty
                }
            | CalendarState.Unavailable message ->
                div {
                    _class "flex flex-wrap items-center justify-between gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 text-sm text-[var(--fve-muted-text)]"
                    p { message }
                    config.stateAction |> Option.defaultValue empty
                }
            | CalendarState.Ready ->
                if List.isEmpty events then config.emptyState
                if config.view = CalendarPeriod.Year then renderYear resolve config
                else
                    div {
                        _class ("fve-calendar-body min-w-0 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] " + (if timed then "relative grid max-h-152 grid-cols-[3.5rem_minmax(0,1fr)] overflow-auto" else "overflow-x-auto"))
                        _style $"--fve-calendar-hours:{endHour - startHour};--fve-calendar-days:{days.Length};--fve-calendar-all-day-rows:{allDayRows}"
                        _tabindex 0
                        _role "region"
                        _data ("on:keydown", "if (evt.target === el && !evt.ctrlKey && !evt.metaKey && !evt.altKey && ['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(evt.key)) { const horizontal = evt.key === 'ArrowLeft' || evt.key === 'ArrowRight'; if (horizontal ? el.scrollWidth > el.clientWidth : el.scrollHeight > el.clientHeight) { evt.preventDefault(); const offset = evt.key === 'ArrowLeft' || evt.key === 'ArrowUp' ? -64 : 64; el.scrollBy({left:horizontal ? offset : 0,top:horizontal ? 0 : offset}) } }")
                        _ariaLabel (config.label + (if timed then " scrollable times" else " scrollable dates"))
                        if config.view = CalendarPeriod.Month then
                            div {
                                _class "fve-calendar-weekdays grid min-w-[56rem] grid-cols-7 border-b border-[var(--fve-border)] py-3 text-center text-xs text-[var(--fve-muted-text)]"
                                _ariaHidden "true"
                                for day in ["Mon"; "Tue"; "Wed"; "Thu"; "Fri"; "Sat"; "Sun"] do span { day }
                            }
                        if timed then
                            div {
                                _class "fve-calendar-hours sticky left-0 z-30 col-start-1 row-start-1 grid w-14 self-start bg-[var(--fve-background)] pt-[calc(4rem+var(--fve-calendar-all-day-rows)*4rem)] text-xs text-[var(--fve-muted-text)]"
                                _ariaHidden "true"
                                for hour in startHour .. endHour - 1 do
                                    span { _class "h-32 p-1 text-center"; TimeOnly(hour, 0).ToString("h tt", CultureInfo.InvariantCulture) }
                            }
                        ol {
                            _class ("fve-calendar-days m-0 grid list-none p-0 " + (if timed then "col-start-2 row-start-1 grid-cols-[repeat(var(--fve-calendar-days),minmax(0,1fr))]" + (if config.view = CalendarPeriod.Day then " min-w-80" else " min-w-[112rem] @min-[48rem]/fve-calendar:min-w-[56rem]") else "min-w-[56rem] grid-cols-7 gap-px bg-[var(--fve-border)]"))
                            _ariaLabel (config.label + " dates")
                            for date in days do
                                let dayEvents = events |> List.filter (fun event -> event.date = date)
                                let lanes = placements dayEvents
                                li {
                                    _class ("fve-calendar-day min-w-0 [overflow-wrap:anywhere] " + (if timed then "border-r border-[var(--fve-border)] last:border-r-0" else "min-h-32 bg-[var(--fve-background)] p-1"))
                                    _attr ("data-date", format "yyyy-MM-dd" date)
                                    _attr ("data-empty", if List.isEmpty dayEvents then "true" else "false")
                                    _attr ("data-selected", if config.selectedDate = Some date then "true" else "false")
                                    _attr ("data-outside", if date.Month <> config.date.Month then "true" else "false")
                                    dateHeading date
                                    ol {
                                        _class ("fve-calendar-all-day m-0 grid list-none gap-1.5 p-0 " + (if timed then "h-[calc(var(--fve-calendar-all-day-rows)*4rem)] overflow-auto" else "[&:not(:empty)]:mb-1.5"))
                                        _ariaLabel (format "dddd, MMMM d" date + " all-day events")
                                        for event in dayEvents |> List.filter (fun event -> event.time.IsNone) do
                                            li { _attr ("data-event", event.id); eventContent event }
                                    }
                                    ol {
                                        _class ("fve-calendar-events m-0 grid list-none p-0 " + (if timed then "grid-rows-[repeat(calc(var(--fve-calendar-hours)*60),.133333333rem)] gap-0 bg-[repeating-linear-gradient(to_bottom,var(--fve-border)_0_1px,transparent_1px_8rem)]" else "gap-1.5"))
                                        _ariaLabel (format "dddd, MMMM d" date + " timed events")
                                        for event in dayEvents |> List.filter (fun event -> event.time.IsSome) do
                                            li {
                                                _attr ("data-event", event.id)
                                                _attr ("data-timed", "true")
                                                if timed then _class "col-start-1 row-[var(--fve-calendar-start)/span_var(--fve-calendar-duration)] my-px mr-0.5 ml-[calc((var(--fve-calendar-lane)-1)*100%/var(--fve-calendar-lanes)+2px)] min-h-0 w-[calc(100%/var(--fve-calendar-lanes)-4px)]"
                                                match event.time with
                                                | Some (start, finish) ->
                                                    let lane, count = lanes[event.id]
                                                    _style $"--fve-calendar-start:{minute start - startHour * 60 + 1};--fve-calendar-duration:{minute finish - minute start};--fve-calendar-lane:{lane};--fve-calendar-lanes:{count}"
                                                | None -> ()
                                                eventContent event
                                            }
                                    }
                                }
                        }
                    }
        }

/// <exclude/>
module internal CompactMonth =
    let render label (weekdays:string list) weekCount gridClass (cell:int -> HtmlElement) =
        div {
            _role "grid"
            _ariaLabel label
            _class ("grid " + gridClass)
            div {
                _role "row"
                _class "grid grid-cols-7"
                for name in weekdays do
                    span { _role "columnheader"; _ariaLabel name; _class "grid min-h-9 min-w-0 place-items-center text-xs font-medium text-[var(--fve-muted-text)]"; name.Substring(0, 1) }
            }
            for week in 0..weekCount - 1 do
                div {
                    _role "row"
                    _class "grid grid-cols-7"
                    for day in 0..6 do cell (week * 7 + day)
                }
        }
