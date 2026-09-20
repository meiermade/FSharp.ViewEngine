namespace FSharp.ViewEngine.Components.Primitives

open System
open System.Globalization
open FSharp.ViewEngine
open type Html

[<RequireQualifiedAccess>]
type CalendarView = Day | Week | Month | Year

/// An all-day event, or a minute-resolution interval on one consumer-local date.
/// Consumers split overnight/multi-day events and resolve time zones before rendering.
[<NoEquality; NoComparison>]
type CalendarEvent<'destination> =
    private
        { id:string
          title:string
          date:DateOnly
          time:(TimeOnly * TimeOnly) option
          detail:string option
          destination:'destination }

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

[<RequireQualifiedAccess>]
type CalendarState = Ready | Loading | Error of message:string | Unavailable of message:string

[<NoEquality; NoComparison>]
type CalendarConfig<'destination> =
    private
        { label:string
          view:CalendarView
          date:DateOnly
          events:CalendarEvent<'destination> list
          previous:'destination option
          next:'destination option
          today:(DateOnly * 'destination) option
          selectedDate:DateOnly option
          dateDestination:(DateOnly -> 'destination) option
          viewDestinations:(CalendarView * 'destination) list
          emptyState:HtmlElement
          state:CalendarState
          stateAction:HtmlElement option }

[<RequireQualifiedAccess>]
module Calendar =
    let create label view (date:DateOnly) (events:CalendarEvent<'destination> list) =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A calendar label is required."
        if date.Year < 2 || date.Year > 9998 then invalidArg (nameof date) "The displayed date must permit adjacent calendar weeks (years 2–9998)."
        if events |> List.distinctBy _.id |> List.length <> events.Length then invalidArg (nameof events) "Event IDs must be unique within a calendar."
        { label = label; view = view; date = date; events = events; previous = None; next = None; today = None
          selectedDate = None; dateDestination = None; viewDestinations = []
          emptyState = p {
              _class "p-4 text-sm text-[var(--fve-muted-text)]"
              "No events in this range."
          }
          state = CalendarState.Ready; stateAction = None }
    let withPrevious destination (config:CalendarConfig<'destination>) = { config with previous = Some destination }
    let withNext destination (config:CalendarConfig<'destination>) = { config with next = Some destination }
    /// The consumer supplies today's date in the displayed time zone and its destination.
    let withToday date destination config = { config with today = Some (date, destination) }
    let withSelectedDate date config = { config with selectedDate = Some date }
    let withDateDestination destination config = { config with dateDestination = Some destination }
    let withViewDestinations destinations config = { config with viewDestinations = destinations }
    let withEmptyState emptyState (config:CalendarConfig<'destination>) = { config with emptyState = emptyState }
    let loading (config:CalendarConfig<'destination>) = { config with state = CalendarState.Loading; stateAction = None }
    let withError message (config:CalendarConfig<'destination>) =
        if String.IsNullOrWhiteSpace message then invalidArg (nameof message) "A calendar error message is required."
        { config with state = CalendarState.Error message; stateAction = None }
    let withUnavailable message (config:CalendarConfig<'destination>) =
        if String.IsNullOrWhiteSpace message then invalidArg (nameof message) "A calendar unavailable message is required."
        { config with state = CalendarState.Unavailable message; stateAction = None }
    let withStateAction action (config:CalendarConfig<'destination>) = { config with stateAction = Some action }

    let private format pattern (date:DateOnly) = date.ToString(pattern, CultureInfo.InvariantCulture)
    let private minute (time:TimeOnly) = time.Hour * 60 + time.Minute
    let private monday (date:DateOnly) = date.AddDays(-((int date.DayOfWeek + 6) % 7))
    let private viewName = function CalendarView.Day -> "Day" | CalendarView.Week -> "Week" | CalendarView.Month -> "Month" | CalendarView.Year -> "Year"
    let private dates config =
        let start, count =
            match config.view with
            | CalendarView.Day -> config.date, 1
            | CalendarView.Week -> monday config.date, 7
            | CalendarView.Month ->
                let first = DateOnly(config.date.Year, config.date.Month, 1)
                let start = monday first
                let finish = monday (first.AddMonths(1).AddDays(-1)) |> fun day -> day.AddDays(6)
                start, finish.DayNumber - start.DayNumber + 1
            | CalendarView.Year ->
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

    let render resolve config =
        let days = dates config
        let events = config.events |> List.filter (fun event -> event.date >= days.Head && event.date <= List.last days) |> List.sortBy (fun event -> event.date, event.time, event.id)
        let allDayRows = days |> List.map (fun date -> events |> List.filter (fun event -> event.date = date && event.time.IsNone) |> List.length) |> List.max
        let times = events |> List.choose _.time
        let startHour = times |> List.fold (fun hour (start, _) -> min hour start.Hour) 8
        let endHour = times |> List.fold (fun hour (_, finish) -> max hour ((minute finish + 59) / 60)) 18
        let timed = config.view = CalendarView.Day || config.view = CalendarView.Week
        let rangeLabel =
            match config.view with
            | CalendarView.Month -> format "MMMM yyyy" config.date
            | CalendarView.Day -> format "dddd, MMMM d, yyyy" config.date
            | CalendarView.Week -> format "MMMM d" days.Head + "–" + format "MMMM d, yyyy" (List.last days)
            | CalendarView.Year -> string config.date.Year
        let control = "inline-flex min-h-[var(--fve-control-min-height)] items-center justify-center rounded-[var(--fve-radius-control)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
        let dateHeading date =
            let today = config.today |> Option.exists (fun (value, _) -> value = date)
            let selected = config.selectedDate = Some date
            h3 {
                _class "fve-calendar-date"
                let content =
                    time {
                        _datetime (format "yyyy-MM-dd" date)
                        if today then _ariaCurrent "date"
                        _class (if today then "inline-flex rounded-full bg-[var(--fve-brand-solid)] px-2 py-1 text-white" else "inline-flex px-2 py-1")
                        span { _class "fve-calendar-short-date"; string date.Day }
                        span {
                            _class "fve-calendar-full-date"
                            format "ddd, MMM d" date
                        }
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
                if selected then
                    span {
                        _class "sr-only"
                        "Selected date"
                    }
            }
        let eventContent event =
            a {
                _href (resolve event.destination)
                _class "fve-calendar-event"
                strong { _class "block text-sm font-medium"; event.title }
                span {
                    _class "block text-xs"
                    match event.time with
                    | Some (start, finish) -> start.ToString("h:mm tt", CultureInfo.InvariantCulture) + "–" + finish.ToString("h:mm tt", CultureInfo.InvariantCulture)
                    | None -> "All day"
                }
                match event.detail with
                | Some detail -> span { _class "fve-calendar-detail block text-xs"; detail }
                | None -> ()
            }
        let yearDay month (date:DateOnly) =
            let outside = date.Month <> month
            let dayEvents = events |> List.filter (fun event -> event.date = date)
            li {
                _class "fve-calendar-year-day"
                _attr ("data-date", format "yyyy-MM-dd" date)
                _attr ("data-outside", if outside then "true" else "false")
                _attr ("data-events", string dayEvents.Length)
                _attr ("data-selected", if config.selectedDate = Some date then "true" else "false")
                if outside then
                    _ariaHidden true
                else
                    let today = config.today |> Option.exists (fun (value, _) -> value = date)
                    let selected = config.selectedDate = Some date
                    let eventSummary =
                        match dayEvents with
                        | [] -> ""
                        | values -> ", " + string values.Length + (if values.Length = 1 then " event: " else " events: ") + (values |> List.map _.title |> String.concat ", ")
                    let content =
                        time {
                            _datetime (format "yyyy-MM-dd" date)
                            if today then _ariaCurrent "date"
                            span {
                                _class "sr-only"
                                format "dddd, MMMM d, yyyy" date
                            }
                            span { _ariaHidden "true"; string date.Day }
                        }
                    match config.dateDestination, dayEvents with
                    | Some destination, _ :: _ ->
                        a {
                            _href (resolve (destination date))
                            _ariaLabel ((if selected then "Selected: " else "Show ") + format "dddd, MMMM d, yyyy" date + eventSummary)
                            _class "fve-calendar-year-date"
                            content
                            span {
                                _class "fve-calendar-year-marker"
                                _ariaHidden "true"
                            }
                        }
                    | _ ->
                        div {
                            _class "fve-calendar-year-date"
                            content
                            if not (List.isEmpty dayEvents) then
                                span {
                                    _class "fve-calendar-year-marker"
                                    _ariaHidden "true"
                                }
                        }
            }
        section {
            _ariaLabel config.label
            _class "fve-calendar fve-control-small grid min-w-0 gap-4"
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
                if not (List.isEmpty config.viewDestinations) then
                    nav {
                        _ariaLabel (config.label + " calendar view")
                        _class "flex flex-wrap items-center gap-1"
                        for view, destination in config.viewDestinations do
                            a {
                                _href (resolve destination)
                                if view = config.view then _ariaCurrent "page"
                                _class (control + (if view = config.view then " bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)]" else " text-[var(--fve-muted-text)]"))
                                viewName view
                            }
                    }
            }
            match config.state with
            | CalendarState.Loading ->
                p {
                    _role "status"
                    _ariaLive "polite"
                    _class "p-4 text-sm text-[var(--fve-muted-text)]"
                    "Loading calendar…"
                }
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
                if config.view = CalendarView.Year then
                    div {
                        _class "fve-calendar-year"
                        for month in 1 .. 12 do
                            let first = DateOnly(config.date.Year, month, 1)
                            let start = monday first
                            section {
                                _class "fve-calendar-year-month"
                                h3 {
                                    _class "fve-calendar-year-heading"
                                    format "MMMM" first
                                }
                                div {
                                    _class "fve-calendar-year-weekdays"
                                    _ariaHidden "true"
                                    for day in ["Mon"; "Tue"; "Wed"; "Thu"; "Fri"; "Sat"; "Sun"] do span { day }
                                }
                                ol {
                                    _class "fve-calendar-year-days"
                                    _ariaLabel (format "MMMM yyyy" first + " dates")
                                    for offset in 0 .. 41 do yearDay month (start.AddDays offset)
                                }
                            }
                    }
                else
                    div {
                        _class "fve-calendar-body"
                        _style $"--fve-calendar-hours:{endHour - startHour};--fve-calendar-days:{days.Length};--fve-calendar-all-day-rows:{allDayRows}"
                        if timed then
                            _tabindex 0
                            _role "region"
                            _ariaLabel (config.label + " scrollable times")
                        if config.view = CalendarView.Month then
                            div {
                                _class "fve-calendar-weekdays"
                                _ariaHidden "true"
                                for day in ["Mon"; "Tue"; "Wed"; "Thu"; "Fri"; "Sat"; "Sun"] do span { day }
                            }
                        if timed then
                            div {
                                _class "fve-calendar-hours"
                                _ariaHidden "true"
                                for hour in startHour .. endHour - 1 do
                                    span { TimeOnly(hour, 0).ToString("h tt", CultureInfo.InvariantCulture) }
                            }
                        ol {
                            _class "fve-calendar-days"
                            _ariaLabel (config.label + " dates")
                            for date in days do
                                let dayEvents = events |> List.filter (fun event -> event.date = date)
                                let lanes = placements dayEvents
                                li {
                                    _class "fve-calendar-day"
                                    _attr ("data-date", format "yyyy-MM-dd" date)
                                    _attr ("data-empty", if List.isEmpty dayEvents then "true" else "false")
                                    _attr ("data-selected", if config.selectedDate = Some date then "true" else "false")
                                    _attr ("data-outside", if date.Month <> config.date.Month then "true" else "false")
                                    dateHeading date
                                    ol {
                                        _class "fve-calendar-all-day"
                                        _ariaLabel (format "dddd, MMMM d" date + " all-day events")
                                        for event in dayEvents |> List.filter (fun event -> event.time.IsNone) do
                                            li { _attr ("data-event", event.id); eventContent event }
                                    }
                                    ol {
                                        _class "fve-calendar-events"
                                        _ariaLabel (format "dddd, MMMM d" date + " timed events")
                                        for event in dayEvents |> List.filter (fun event -> event.time.IsSome) do
                                            li {
                                                // IDs remain consumer-owned data, not global DOM IDs duplicated across galleries.
                                                _attr ("data-event", event.id)
                                                _attr ("data-timed", "true")
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
