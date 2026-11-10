namespace FSharp.ViewEngine.Components

open System
open System.Globalization
open FSharp.ViewEngine
open type Html

/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
type CalendarView = Day | Week | Month | Year

/// <summary>The visible date values for each calendar view, before application-owned URL mapping.</summary>
/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
module Calendar =
    let dates view (date:DateOnly) =
        if date.Year < 2 || date.Year > 9998 then invalidArg (nameof date) "The displayed date must permit adjacent calendar weeks (years 2–9998)."
        let monday (day:DateOnly) = day.AddDays(-((int day.DayOfWeek + 6) % 7))
        let start, count =
            match view with
            | CalendarView.Day -> date, 1
            | CalendarView.Week -> monday date, 7
            | CalendarView.Month ->
                let first = DateOnly(date.Year, date.Month, 1)
                let start = monday first
                let finish = monday (first.AddMonths(1).AddDays(-1)) |> fun day -> day.AddDays(6)
                start, finish.DayNumber - start.DayNumber + 1
            | CalendarView.Year ->
                let start = DateOnly(date.Year, 1, 1)
                start, start.AddYears(1).DayNumber - start.DayNumber
        [ for offset in 0 .. count - 1 -> start.AddDays offset ]

    /// A standalone compact month retains six complete weeks.
    let compactMonthDates date =
        let start = dates CalendarView.Month date |> List.head
        [ for offset in 0 .. 41 -> start.AddDays offset ]

/// <category>day-calendar</category>
[<RequireQualifiedAccess>]
type CalendarEventTone = Brand | Neutral | Muted

/// <summary>
/// An inclusive all-day date range, or a minute-resolution interval on one consumer-local date.
/// Consumers split overnight timed events and resolve time zones before rendering.
/// </summary>
/// <category>month-calendar</category>
[<NoEquality; NoComparison>]
type CalendarEvent<'destination> =
    internal
        { id:string
          title:string
          date:DateOnly
          endDate:DateOnly
          time:(TimeOnly * TimeOnly) option
          detail:string option
          tone:CalendarEventTone
          preview:HtmlElement option
          destination:'destination }

/// <remarks>
/// create supplies a single-day all-day event. withEndDate extends it through an inclusive end date.
/// Timed events remain same-day; split overnight intervals and resolve time zones before rendering.
/// </remarks>
/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
module CalendarEvent =
    let create id title date destination =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable event ID is required."
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "An event title is required."
        { id = id; title = title; date = date; endDate = date; time = None; detail = None; tone = CalendarEventTone.Brand; preview = None; destination = destination }
    /// Extends an all-day event through this inclusive date; timed events cannot span dates.
    let withEndDate (endDate:DateOnly) (event:CalendarEvent<'destination>) =
        if endDate < event.date then invalidArg (nameof endDate) "An all-day range cannot end before it starts."
        if event.time.IsSome then invalidArg (nameof event) "Only all-day events support an end date."
        { event with endDate = endDate }
    let withTime (start:TimeOnly) (finish:TimeOnly) (event:CalendarEvent<'destination>) =
        if event.endDate <> event.date then invalidArg (nameof event) "Timed events must occupy one date."
        if finish <= start || start.Ticks % TimeSpan.TicksPerMinute <> 0L || finish.Ticks % TimeSpan.TicksPerMinute <> 0L then
            invalidArg (nameof finish) "An event needs a positive, same-day interval in whole minutes."
        { event with time = Some (start, finish) }
    let withDetail detail (event:CalendarEvent<'destination>) = { event with detail = Some detail }
    /// Visual emphasis only. Supply the event's meaning in its visible title/detail.
    let withTone tone (event:CalendarEvent<'destination>) = { event with tone = tone }
    /// Optional consumer-authored Day/Week event details shown using Popover. Requires a calendar ID.
    /// The title, time, detail and original destination are retained in the panel.
    let withPreview content (event:CalendarEvent<'destination>) = { event with preview = Some content }
    let internal occursOn date (event:CalendarEvent<'destination>) = event.date <= date && date <= event.endDate
    let internal allDayLabel (event:CalendarEvent<'destination>) =
        if event.endDate = event.date then "All day"
        else "All day, " + event.date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) + "–" + event.endDate.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)

/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
type CalendarState = Ready | Loading | Error of message:string | Unavailable of message:string

type internal CalendarRangeConfig =
    { start:int; minutes:int; durations:int list; available:(int * int) list; step:int; name:string }

/// <exclude/>
[<NoEquality; NoComparison>]
type internal CalendarDisplayConfig<'destination> =
        { label:string
          view:CalendarView
          date:DateOnly
          events:CalendarEvent<'destination> list
          previous:'destination option
          next:'destination option
          today:(DateOnly * 'destination) option
          selectedDate:DateOnly option
          dateDestination:(DateOnly -> 'destination option) option
          attributes:HtmlAttribute list
          navigation:Map<string, HtmlAttribute list * HtmlElement list>
          emptyState:HtmlElement
          state:CalendarState
          stateAction:HtmlElement option
          controls:HtmlElement option
          showDayHeading:bool
          hours:(int * int) option
          hourHeight:int
          viewportHeight:int option
          fluidWidth:bool
          range:CalendarRangeConfig option }

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
          state = CalendarState.Ready; stateAction = None; attributes = []; navigation = Map.empty
          controls = None; showDayHeading = true; hours = None; hourHeight = 128; viewportHeight = None
          fluidWidth = false; range = None }

    let private format pattern (date:DateOnly) = date.ToString(pattern, CultureInfo.InvariantCulture)
    let private minute (time:TimeOnly) = time.Hour * 60 + time.Minute
    let private viewName = function CalendarView.Day -> "Day" | CalendarView.Week -> "Week" | CalendarView.Month -> "Month" | CalendarView.Year -> "Year"

    let dateAttributes (date:DateOnly) (config:CalendarDisplayConfig<'destination>) =
        config.navigation |> Map.tryFind ("date-" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) |> Option.map fst |> Option.defaultValue []

    let navigationLink resolve key defaults fallback (config:CalendarDisplayConfig<'destination>) =
        let destination =
            match key with "previous" -> config.previous | "next" -> config.next | _ -> config.today |> Option.map snd
        match destination with
        | None -> empty
        | Some destination ->
            let attributes, children = config.navigation |> Map.tryFind key |> Option.defaultValue ([], [])
            ComponentElement.render "a" (_href (resolve destination) :: defaults) attributes (if List.isEmpty children then fallback else children)

    // Connected overlap groups share staggered lanes; disjoint events regain the full day width.
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

    let private clock value = if value = 1440 then "24:00" else TimeOnly(value / 60, value % 60).ToString("HH:mm", CultureInfo.InvariantCulture)

    // Calendar starts use a midnight-based lattice, including steps that do not divide an hour.
    let private firstRangeStart minimum step = ((minimum + step - 1) / step) * step
    let private lastRangeStart maximum length step = ((maximum - length) / step) * step

    let private rangeOverlay (range:CalendarRangeConfig) =
        li {
            _class "pointer-events-none relative z-20 col-start-1 row-[var(--fve-selection-start)/span_var(--fve-selection-duration)] mx-0.5 my-px min-h-8 rounded-[var(--fve-radius-control)] border border-[var(--fve-brand-solid)] bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)] data-[valid=false]:border-dashed data-[valid=false]:border-[var(--fve-critical-text)] data-[valid=false]:bg-[var(--fve-critical-subtle)] data-[valid=false]:text-[var(--fve-critical-text)]"
            _data("range-selection", "true")
            _data("valid", string (List.contains (range.start, range.minutes) range.available) |> _.ToLowerInvariant())
            span { _data("range-part", "start"); _ariaHidden true; _class "pointer-events-auto absolute inset-x-0 -top-1 z-10 flex h-3 cursor-ns-resize touch-none justify-center"; span { _class "mt-1 h-1 w-7 rounded-full bg-current" } }
            button {
                _type "button"
                _data("range-part", "move")
                _class "pointer-events-auto h-full min-h-8 w-full cursor-grab touch-none rounded-[var(--fve-radius-control)] p-2 text-left text-sm font-medium focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
                _ariaLabel "Move selected time range"
                span { _data("range-label", ""); clock range.start + "–" + clock (range.start + range.minutes) }
            }
            span { _data("range-part", "end"); _ariaHidden true; _class "pointer-events-auto absolute inset-x-0 -bottom-1 z-10 flex h-3 cursor-ns-resize touch-none justify-center"; span { _class "mt-1 h-1 w-7 rounded-full bg-current" } }
        }

    let private rangeSurface id date startHour endHour (range:CalendarRangeConfig) (grid:HtmlElement) =
        let first = firstRangeStart (startHour * 60) range.step
        let last = lastRangeStart (endHour * 60) range.minutes range.step
        let settings =
            {| date = format "yyyy-MM-dd" date; start = range.start; minutes = range.minutes
               durations = List.toArray range.durations; available = range.available |> List.map (fun (start, minutes) -> [|start; minutes|]) |> List.toArray
               step = range.step; minimum = startHour * 60; maximum = endHour * 60 |}
        (Html.el "fve-calendar-range") {
            _id (id + "-range")
            _class "block min-w-0"
            _attr("settings", System.Text.Json.JsonSerializer.Serialize settings)
            _style $"--fve-selection-start:{range.start - startHour * 60 + 1};--fve-selection-duration:{range.minutes}"
            grid
            div {
                _class "grid min-w-0 grid-cols-1 items-end gap-3 pt-3 @min-[28rem]/fve-calendar:grid-cols-2"
                div {
                    _class "grid min-w-0 gap-1 text-sm"
                    label { _for (id + "-range-start"); "Start time" }
                    fveTimePicker {
                        _id (id + "-range-start")
                        _name (range.name + ".start")
                        _ariaLabel "Start time"
                        _value (TimeOnly(range.start / 60, range.start % 60))
                        _min (TimeOnly(first / 60, first % 60))
                        _max (TimeOnly(last / 60, last % 60))
                        _interval range.step
                        _format TimeFormat.TwentyFourHour
                        _size Size.Small
                        _required true
                        _data("range-field", "start")
                    }
                }
                div {
                    _class "grid min-w-0 gap-1 text-sm"
                    label { _for (id + "-range-duration"); "Duration" }
                    fveSelect {
                        _id (id + "-range-duration")
                        _name (range.name + ".minutes")
                        _ariaLabel "Duration"
                        _class "[&_[data-fve-select-label]]:whitespace-normal"
                        _value (string range.minutes)
                        _size Size.Small
                        _nativeFallback true
                        _data("range-field", "duration")
                        fveSelectContent {
                            for minutes in range.durations do
                                fveSelectItem {
                                    _value (string minutes)
                                    string minutes + " minutes"
                                }
                        }
                    }
                }
            }
            p { _id (id + "-range-help"); _class "sr-only"; "Arrow keys move the selected time. Shift and an arrow changes its duration. Escape cancels a pointer adjustment." }
            p {
                _id (id + "-range-status"); _role "status"; _ariaLive "polite"; _class "mt-2 text-sm text-[var(--fve-muted-text)]"; _data("range-status", "")
                if List.contains (range.start, range.minutes) range.available then "Selected time is available."
                else "This time is unavailable. Choose another start time or duration."
            }
        }

    let render (resolve:'destination -> string) (renderDates:('destination -> string) -> CalendarDisplayConfig<'destination> -> HtmlElement) id (config:CalendarDisplayConfig<'destination>) =
        let days = Calendar.dates config.view config.date
        let events = config.events |> List.filter (fun event -> event.endDate >= days.Head && event.date <= List.last days) |> List.sortBy (fun event -> event.date, event.time, event.id)
        let allDayRows = days |> List.map (fun date -> events |> List.filter (fun event -> CalendarEvent.occursOn date event && event.time.IsNone) |> List.length) |> List.max
        let timed = config.view = CalendarView.Day || config.view = CalendarView.Week
        let intervals =
            [ for event in events do match event.time with Some (start, finish) -> yield minute start, minute finish | None -> ()
              match config.range with
              | Some range ->
                  yield range.start, range.start + range.minutes
                  for start, length in range.available do yield start, start + length
              | None -> () ]
        let startHour, endHour =
            config.hours |> Option.defaultWith (fun () ->
                (intervals |> List.fold (fun hour (start, _) -> min hour (start / 60)) 8),
                (intervals |> List.fold (fun hour (_, ending) -> max hour ((ending + 59) / 60)) 18))
        if intervals |> List.exists (fun (start, ending) -> start < startHour * 60 || ending > endHour * 60) then
            invalidArg "hours" "Visible hours must include every event and selection range."
        if config.range |> Option.exists (fun range -> range.durations |> List.exists (fun length -> firstRangeStart (startHour * 60) range.step + length > endHour * 60)) then
            invalidArg "durations" "Every duration must fit a step-aligned start within the visible hours."
        let showDateHeading = config.view <> CalendarView.Day || config.showDayHeading
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
                _class ("fve-calendar-date m-0 text-sm font-medium " + (if timed then "sticky top-0 z-40 flex h-16 items-center border-b border-[var(--fve-border)] bg-[var(--fve-background)] p-2" else "p-0.5"))
                let content =
                    time {
                        _datetime (format "yyyy-MM-dd" date)
                        if today then _ariaCurrent "date"
                        _class (if today then "inline-flex rounded-full bg-[var(--fve-brand-solid)] px-2 py-1 text-white" else "inline-flex px-2 py-1")
                        span { _class (if timed then "hidden" else "inline"); string date.Day }
                        span { _class (if timed then "" else "sr-only"); format "ddd, MMM d" date }
                    }
                match config.dateDestination |> Option.bind (fun destination -> destination date) with
                | Some destination ->
                    a {
                        for attribute in ComponentElement.attributes ([_href (resolve destination); _ariaLabel ((if selected then "Selected: " else "Show ") + format "dddd, MMMM d, yyyy" date); _class "inline-flex min-h-8 rounded-[var(--fve-radius-control)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"] @ dateAttributes date config) do attribute
                        content
                    }
                | None -> content
                if selected then span { _class "sr-only"; "Selected date" }
            }
        let eventContent date (event:CalendarEvent<'destination>) =
            let timeLabel =
                match event.time with
                | Some (start, finish) -> start.ToString("h:mm tt", CultureInfo.InvariantCulture) + "–" + finish.ToString("h:mm tt", CultureInfo.InvariantCulture)
                | None -> CalendarEvent.allDayLabel event
            let tone =
                match event.tone with
                | CalendarEventTone.Brand -> "[--fve-calendar-event-fill:var(--fve-brand-subtle)] [--fve-calendar-event-ink:var(--fve-brand-text)]"
                | CalendarEventTone.Neutral -> "[--fve-calendar-event-fill:var(--fve-neutral-subtle)] [--fve-calendar-event-ink:var(--fve-text)]"
                | CalendarEventTone.Muted -> "[--fve-calendar-event-fill:var(--fve-surface-subtle)] [--fve-calendar-event-ink:var(--fve-muted-text)]"
            let cardAttributes =
                [ _data("tone", string event.tone |> _.ToLowerInvariant())
                  _class ("fve-calendar-event flex h-full w-full min-w-0 flex-col items-start justify-start overflow-x-hidden overflow-y-auto rounded-[var(--fve-radius-control)] bg-[var(--fve-calendar-event-fill)] p-2 text-left text-[var(--fve-calendar-event-ink)] ring-1 ring-inset ring-[var(--fve-border)] aria-[expanded=true]:ring-2 aria-[expanded=true]:ring-[var(--fve-brand-ring)] " + tone + " no-underline hover:bg-[color-mix(in_srgb,var(--fve-calendar-event-fill)_88%,var(--fve-calendar-event-ink))] aria-[expanded=true]:bg-[color-mix(in_srgb,var(--fve-calendar-event-fill)_80%,var(--fve-calendar-event-ink))] focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]") ]
            let cardContent =
                [ strong { _class "block w-full shrink-0 whitespace-normal text-sm font-medium"; event.title }
                  span { _class "block w-full shrink-0 text-xs"; timeLabel }
                  if event.preview.IsNone then
                      match event.detail with
                      | Some detail -> span { _class "mt-1 block w-full shrink-0 text-xs"; detail }
                      | None -> () ]
            match event.preview with
            | None -> ComponentElement.render "a" [_href (resolve event.destination)] cardAttributes cardContent
            | Some content ->
                let calendarId = id |> Option.defaultWith (fun () -> invalidArg "id" "A calendar with event previews requires a stable ID.")
                let previewId = calendarId + "-" + format "yyyy-MM-dd" date + "-event-" + event.id
                fvePopover {
                    _id previewId
                    _ariaLabel event.title
                    _focusOnOpen true
                    _class "h-full w-full min-w-0"
                    fvePopoverTrigger {
                        fun attributes -> ComponentElement.render "button" attributes cardAttributes cardContent
                    }
                    fvePopoverContent {
                        _class "[overflow-wrap:normal]"
                        div {
                            _class "flex items-start justify-between gap-3 @max-[24rem]/fve-calendar:flex-col-reverse"
                            h3 { _class "text-base font-semibold"; event.title }
                            button {
                                _type "button"
                                _attr("popovertarget", previewId + "-content")
                                _attr("popovertargetaction", "hide")
                                _ariaLabel "Close event details"
                                _class "grid size-8 shrink-0 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] @max-[24rem]/fve-calendar:self-end"
                                raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5" aria-hidden="true"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 0 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 0 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""
                            }
                        }
                        p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; format "dddd, MMMM d" event.date + " · " + timeLabel }
                        match event.detail with Some detail -> p { _class "mt-3 text-sm"; detail } | None -> ()
                        div { _class "mt-3 text-sm"; content }
                        a { _href (resolve event.destination); _class "mt-4 inline-flex min-h-8 items-center font-medium text-[var(--fve-brand-text)] underline underline-offset-4"; "View event" }
                    }
                }
        section {
            for attribute in ComponentElement.attributes ([
                match id with Some id -> _id id | None -> ()
                _ariaLabel config.label
                _class "fve-calendar fve-control-small @container/fve-calendar grid min-w-0 grid-cols-1 gap-4 text-[var(--fve-text)]"
                _attr ("data-view", viewName config.view |> fun value -> value.ToLowerInvariant()) ] @ config.attributes) do attribute
            header {
                _class "flex flex-wrap items-center justify-between gap-3"
                div {
                    h2 { _class "text-lg font-semibold text-[var(--fve-text)]"; config.label }
                    p { _class "text-sm text-[var(--fve-muted-text)]"; rangeLabel }
                }
                div {
                    _class "flex min-w-0 flex-wrap items-center gap-3"
                    config.controls |> Option.defaultValue empty
                    nav {
                        _ariaLabel (config.label + " date navigation")
                        _class "flex flex-wrap items-center gap-1"
                        for key, label in ["previous", "Previous"; "today", "Today"; "next", "Next"] do
                            navigationLink resolve key [_class control] [text label] config
                    }
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
                if List.isEmpty events && config.range.IsNone then config.emptyState
                if config.view = CalendarView.Month || config.view = CalendarView.Year then renderDates resolve config
                else
                    let body = (Html.el "fve-scroll-region") {
                        _class ("fve-calendar-body min-w-0 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] " + (if timed then "relative grid max-h-[var(--fve-calendar-viewport-height,38rem)] grid-cols-[3.5rem_minmax(0,1fr)] overflow-auto" else "block overflow-x-auto"))
                        let hourHeight = (float config.hourHeight / 16.).ToString("0.########", CultureInfo.InvariantCulture)
                        let minuteHeight = (float config.hourHeight / 960.).ToString("0.########", CultureInfo.InvariantCulture)
                        let headingHeight = if showDateHeading then "4rem" else "0rem"
                        _style ($"--fve-calendar-hours:{endHour - startHour};--fve-calendar-days:{days.Length};--fve-calendar-all-day-rows:{allDayRows};--fve-calendar-hour-height:{hourHeight}rem;--fve-calendar-minute-height:{minuteHeight}rem;--fve-calendar-heading-height:{headingHeight}" + (config.viewportHeight |> Option.map (fun value -> $";--fve-calendar-viewport-height:{value}px") |> Option.defaultValue ""))
                        _tabindex 0
                        _role "region"
                        _attr("scroll-axis", "both")
                        _ariaLabel (config.label + (if timed then " scrollable times" else " scrollable dates"))
                        if timed && showDateHeading then
                            div { _class "fve-calendar-corner sticky top-0 left-0 z-50 col-start-1 row-start-1 h-16 self-start border-b border-[var(--fve-border)] bg-[var(--fve-background)]"; _ariaHidden true }
                        if timed then
                            div {
                                _class "fve-calendar-hours sticky left-0 z-40 col-start-1 row-start-1 grid w-14 self-start bg-[var(--fve-background)] pt-[calc(var(--fve-calendar-heading-height)+var(--fve-calendar-all-day-rows)*4rem)] text-xs text-[var(--fve-muted-text)]"
                                _ariaHidden "true"
                                for hour in startHour .. endHour - 1 do
                                    span { _class "h-[var(--fve-calendar-hour-height)] p-1 text-center"; TimeOnly(hour, 0).ToString("h tt", CultureInfo.InvariantCulture) }
                            }
                        ol {
                            _class ("fve-calendar-days m-0 grid list-none p-0 " + (if timed then "col-start-2 row-start-1 grid-cols-[repeat(var(--fve-calendar-days),minmax(0,1fr))]" + (if config.view = CalendarView.Day then (if config.fluidWidth then "" else " min-w-80") else " min-w-[112rem] @min-[48rem]/fve-calendar:min-w-[56rem]") else "min-w-[56rem] grid-cols-7 gap-px bg-[var(--fve-border)]"))
                            _ariaLabel (config.label + " dates")
                            for date in days do
                                let dayEvents = events |> List.filter (CalendarEvent.occursOn date)
                                let lanes = placements dayEvents
                                li {
                                    _class ("fve-calendar-day min-w-0 [overflow-wrap:anywhere] " + (if timed then "border-r border-[var(--fve-border)] last:border-r-0" else "min-h-32 bg-[var(--fve-background)] p-1"))
                                    _attr ("data-date", format "yyyy-MM-dd" date)
                                    _attr ("data-empty", if List.isEmpty dayEvents then "true" else "false")
                                    _attr ("data-selected", if config.selectedDate = Some date then "true" else "false")
                                    _attr ("data-outside", if date.Month <> config.date.Month then "true" else "false")
                                    if showDateHeading then dateHeading date
                                    ol {
                                        _class ("fve-calendar-all-day m-0 grid grid-cols-1 list-none gap-1.5 p-0 " + (if timed then "h-[calc(var(--fve-calendar-all-day-rows)*4rem)] overflow-auto" else "[&:not(:empty)]:mb-1.5"))
                                        _ariaLabel (format "dddd, MMMM d" date + " all-day events")
                                        for event in dayEvents |> List.filter (fun event -> event.time.IsNone) do
                                            li { _attr ("data-event", event.id); eventContent date event }
                                    }
                                    let eventsGrid = ol {
                                        _class ("fve-calendar-events relative m-0 grid grid-cols-1 list-none p-0 " + (if timed then "grid-rows-[repeat(calc(var(--fve-calendar-hours)*60),var(--fve-calendar-minute-height))] gap-0 bg-[repeating-linear-gradient(to_bottom,color-mix(in_srgb,var(--fve-border)_60%,transparent)_0_1px,transparent_1px_var(--fve-calendar-hour-height))]" else "gap-1.5"))
                                        _ariaLabel (format "dddd, MMMM d" date + " timed events")
                                        match config.range with
                                        | Some range ->
                                            li {
                                                _class "col-start-1 row-[1/-1] min-h-0"
                                                _data("range-background", "")
                                                let first = firstRangeStart (startHour * 60) range.step
                                                _style $"padding-top:calc(var(--fve-calendar-minute-height)*{first - startHour * 60})"
                                                for start in first .. range.step .. endHour * 60 - 1 do
                                                    button { _type "button"; _tabindex -1; _ariaLabel ("Choose " + clock start); _data("range-start", string start); _class "relative block w-full after:pointer-events-none after:absolute after:inset-x-0.5 after:inset-y-px after:rounded-[var(--fve-radius-control)] hover:after:bg-[color-mix(in_srgb,var(--fve-brand-subtle)_60%,var(--fve-background))]"; _style $"height:calc(var(--fve-calendar-minute-height)*{min range.step (endHour * 60 - start)})" }
                                            }
                                            rangeOverlay range
                                        | None -> ()
                                        for event in dayEvents |> List.filter (fun event -> event.time.IsSome) do
                                            li {
                                                _attr ("data-event", event.id)
                                                _attr ("data-timed", "true")
                                                if timed then _class "relative z-0 col-start-1 row-[var(--fve-calendar-start)/span_var(--fve-calendar-duration)] my-px ml-[calc((var(--fve-calendar-lane)-1)*100%/(var(--fve-calendar-lanes)+1)+2px)] min-h-0 w-[calc(200%/(var(--fve-calendar-lanes)+1)-4px)] focus-within:z-30 has-[:popover-open]:z-30"
                                                match event.time with
                                                | Some (start, finish) ->
                                                    let lane, count = lanes[event.id]
                                                    _style $"--fve-calendar-start:{minute start - startHour * 60 + 1};--fve-calendar-duration:{minute finish - minute start};--fve-calendar-lane:{lane};--fve-calendar-lanes:{count}"
                                                | None -> ()
                                                eventContent date event
                                            }
                                    }
                                    eventsGrid
                                }
                        }
                    }
                    match config.range with
                    | Some range -> rangeSurface (Option.get id) days.Head startHour endHour range body
                    | None -> body
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

type internal CalendarPartElement(kind:string, attributes:HtmlAttribute list, children:HtmlElement list) =
    inherit HtmlElement()
    member _.Kind = kind
    member _.Attributes = attributes
    member _.Children = children
    override _.Render(_) = invalidOp "Calendar parts must be placed inside a calendar builder."

module internal CalendarElements =
    let parseDate name attributes =
        let value = ComponentElement.required name attributes
        match DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) with
        | true, date -> date
        | _ -> invalidArg "attributes" $"{name} must be an ISO date (yyyy-MM-dd)."

    let private integer name fallback minimum maximum attributes =
        let value =
            match ComponentElement.value name attributes with
            | None -> fallback
            | Some value -> match Int32.TryParse value with true, parsed -> parsed | _ -> invalidArg name "A whole number is required."
        if value < minimum || value > maximum then invalidArg name $"{name} must be between {minimum} and {maximum}."
        value

    let private parseTime attributes =
        let value = ComponentElement.required "start-time" attributes
        match TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None) with
        | true, time -> time.Hour * 60 + time.Minute
        | _ -> invalidArg "start-time" "Start time must use HH:mm."

    let part kind attributes children =
        if List.contains kind ["previous"; "next"; "today"; "date"] then ComponentElement.required "href" attributes |> ignore
        if kind = "today" || kind = "date" then parseDate "date" attributes |> ignore
        if kind = "date" then ComponentContent.noChildren "fveCalendarDate" children
        if kind = "range" then ComponentContent.noChildren "fveCalendarRange" children
        CalendarPartElement(kind, attributes, children) :> HtmlElement

    let configuration view attributes (children:HtmlElement list) events =
        let label = ComponentElement.required "label" attributes
        let date = parseDate "date" attributes
        let timed = view = CalendarView.Day || view = CalendarView.Week
        let hours =
            ComponentElement.value "hours" attributes |> Option.map (fun value ->
                match System.Text.Json.JsonSerializer.Deserialize<int array>(value) with
                | [|start; ending|] when start >= 0 && ending <= 24 && start < ending -> start, ending
                | _ -> invalidArg "hours" "Visible hours must be a positive range within 0–24.")
        if not timed && (["hours"; "hour-height"; "viewport-height"; "day-heading"; "fluid-width"] |> List.exists (fun key -> ComponentElement.value key attributes |> Option.isSome)) then
            invalidArg "attributes" "Time-grid presentation belongs to Day/Week calendars."
        let flag name fallback = ComponentElement.value name attributes |> Option.map ((=) "true") |> Option.defaultValue fallback
        let mutable config =
            { CalendarRendering.create label view date events with
                hours = hours; hourHeight = integer "hour-height" 128 48 160 attributes
                viewportHeight = ComponentElement.value "viewport-height" attributes |> Option.map (fun _ -> integer "viewport-height" 608 200 960 attributes)
                showDayHeading = flag "day-heading" true; fluidWidth = flag "fluid-width" false }
        let mutable seen = Set.empty
        let mutable links = Map.empty
        for child in children do
            match child with
            | :? NoopElement -> ()
            | :? CalendarPartElement as part ->
                let native = ComponentHtml.safeAttributes ["date"] part.Attributes
                if part.Kind = "date" then
                    let date = parseDate "date" part.Attributes
                    if Map.containsKey date links then invalidArg "children" "Each calendar date can have only one destination."
                    links <- Map.add date (ComponentElement.required "href" part.Attributes) links
                    config <- { config with navigation = Map.add ("date-" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) (native, []) config.navigation }
                else
                    if Set.contains part.Kind seen then invalidArg "children" $"Only one calendar {part.Kind} part is supported."
                    seen <- Set.add part.Kind seen
                    match part.Kind with
                    | "previous" -> config <- { config with previous = Some (ComponentElement.required "href" part.Attributes) }
                    | "next" -> config <- { config with next = Some (ComponentElement.required "href" part.Attributes) }
                    | "today" -> config <- { config with today = Some (parseDate "date" part.Attributes, ComponentElement.required "href" part.Attributes) }
                    | "empty" -> config <- { config with emptyState = ComponentElement.render "div" [] native part.Children }
                    | "action" -> config <- { config with stateAction = Some (ComponentElement.render "div" [] native part.Children) }
                    | "controls" -> config <- { config with controls = Some (ComponentElement.render "div" [_class "flex min-w-0 flex-wrap items-center gap-2"] native part.Children) }
                    | "range" ->
                        if view <> CalendarView.Day then invalidArg "children" "Timed range selection belongs to a Day calendar."
                        ComponentElement.required "id" attributes |> ignore
                        let start = parseTime part.Attributes
                        let minutes = integer "minutes" 60 1 1440 part.Attributes
                        let step = integer "minute-step" 30 1 60 part.Attributes
                        let durations = System.Text.Json.JsonSerializer.Deserialize<int array>(ComponentElement.required "durations" part.Attributes) |> Array.toList |> List.distinct |> List.sort
                        if durations.IsEmpty || durations |> List.exists (fun value -> value <= 0 || value > 1440) || not (List.contains minutes durations) || start + minutes > 1440 || start % step <> 0 then
                            invalidArg "durations" "The selection must use an allowed positive same-day duration and a step-aligned start."
                        let available =
                            System.Text.Json.JsonSerializer.Deserialize<string array array>(ComponentElement.required "available-ranges" part.Attributes)
                            |> Array.map (function
                                | [|time; duration|] ->
                                    let startAttributes = [Html._attr("start-time", time)]
                                    let at = parseTime startAttributes
                                    match Int32.TryParse duration with
                                    | true, length when List.contains length durations && at + length <= 1440 && at % step = 0 -> at, length
                                    | _ -> invalidArg "available-ranges" "Available ranges must use step-aligned starts and allowed same-day durations."
                                | _ -> invalidArg "available-ranges" "Each available range requires a start and duration.") |> Array.toList
                        let name = ComponentElement.required "name" part.Attributes
                        config <- { config with range = Some {start = start; minutes = minutes; durations = durations; available = available; step = step; name = name} }
                    | _ -> invalidArg "children" "Unsupported calendar part."
                    if List.contains part.Kind ["previous"; "next"; "today"] then
                        config <- { config with navigation = Map.add part.Kind (native, part.Children) config.navigation }
            | _ -> invalidArg "children" "Calendars accept events and calendar parts, not arbitrary HTML children."
        let state =
            match ComponentElement.value "calendar-state" attributes with
            | None -> CalendarState.Ready
            | Some value ->
                match System.Text.Json.JsonSerializer.Deserialize<string array>(value) with
                | [|"ready"|] -> CalendarState.Ready
                | [|"loading"|] -> CalendarState.Loading
                | [|"error"; message|] when not (String.IsNullOrWhiteSpace message) -> CalendarState.Error message
                | [|"unavailable"; message|] when not (String.IsNullOrWhiteSpace message) -> CalendarState.Unavailable message
                | _ -> invalidArg "attributes" "Unsupported calendar state."
        match ComponentElement.value "id" attributes with
        | Some id when String.IsNullOrWhiteSpace id || Seq.exists Char.IsWhiteSpace id -> invalidArg "attributes" "A calendar ID cannot contain whitespace."
        | _ -> ()
        { config with
            selectedDate = ComponentElement.value "selected-date" attributes |> Option.map (fun _ -> parseDate "selected-date" attributes)
            dateDestination = if Map.isEmpty links then None else Some (fun day -> Map.tryFind day links)
            state = state
            attributes = ComponentHtml.safeAttributes ["date"; "label"; "selected-date"; "calendar-state"; "layout"; "hours"; "hour-height"; "viewport-height"; "day-heading"; "fluid-width"] attributes }

/// <exclude/>
type CalendarDisplayBuilder internal (view:CalendarView, render:HtmlAttribute list -> CalendarDisplayConfig<string> -> HtmlElement) =
    inherit ComponentExpressionBuilder()
    member _.Yield(event:CalendarEvent<string>) : ComponentCode<CalendarEvent<string> list> =
        fun state -> state.Options.Add(fun events -> events @ [event])
    member _.YieldFrom(events:CalendarEvent<string> seq) : ComponentCode<CalendarEvent<string> list> =
        fun state -> state.Options.Add(fun previous -> previous @ List.ofSeq events)
    member _.Run(code:ComponentCode<CalendarEvent<string> list>) =
        let state = ComponentState<CalendarEvent<string> list>()
        code state
        let events = ([], state.Options) ||> Seq.fold (fun events update -> update events)
        let attributes = List.ofSeq state.Attributes
        let config = CalendarElements.configuration view attributes (List.ofSeq state.Children) events
        render attributes config

/// <category>month-calendar</category>
[<AutoOpen>]
module CalendarAttributes =
    type Html with
        static member _date(value:DateOnly) = Html._attr("date", value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
        static member _selectedDate(value:DateOnly) = Html._attr("selected-date", value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
        static member _hours(startHour:int, endHour:int) = Html._attr("hours", System.Text.Json.JsonSerializer.Serialize [|startHour; endHour|])
        static member _hourHeight(value:int) = Html._attr("hour-height", string value)
        static member _viewportHeight(value:int) = Html._attr("viewport-height", string value)
        static member _dayHeading(value:bool) = Html._attr("day-heading", if value then "true" else "false")
        static member _fluidWidth(value:bool) = Html._attr("fluid-width", if value then "true" else "false")
        static member _startTime(value:TimeOnly) =
            if value.Ticks % TimeSpan.TicksPerMinute <> 0L then invalidArg "value" "Calendar times must use whole minutes."
            Html._attr("start-time", value.ToString("HH:mm", CultureInfo.InvariantCulture))
        static member _minutes(value:int) = Html._attr("minutes", string value)
        static member _minuteStep(value:int) = Html._attr("minute-step", string value)
        static member _durations(values:int list) = Html._attr("durations", System.Text.Json.JsonSerializer.Serialize values)
        static member _availableRanges(values:(TimeOnly * int) list) =
            for time, _ in values do
                if time.Ticks % TimeSpan.TicksPerMinute <> 0L then invalidArg "values" "Calendar times must use whole minutes."
            Html._attr("available-ranges", System.Text.Json.JsonSerializer.Serialize (values |> List.map (fun (time, minutes) -> [|time.ToString("HH:mm", CultureInfo.InvariantCulture); string minutes|])))
        static member _state(value:CalendarState) =
            let state =
                match value with
                | CalendarState.Ready -> [|"ready"|]
                | CalendarState.Loading -> [|"loading"|]
                | CalendarState.Error message -> [|"error"; message|]
                | CalendarState.Unavailable message -> [|"unavailable"; message|]
            Html._attr("calendar-state", System.Text.Json.JsonSerializer.Serialize state)

/// <category>month-calendar</category>
[<AutoOpen>]
module CalendarPartsDsl =
    let fveCalendarPrevious = ComponentElementBuilder(CalendarElements.part "previous")
    let fveCalendarNext = ComponentElementBuilder(CalendarElements.part "next")
    let fveCalendarToday = ComponentElementBuilder(CalendarElements.part "today")
    let fveCalendarDate = ComponentElementBuilder(CalendarElements.part "date")
    let fveCalendarEmpty = ComponentElementBuilder(CalendarElements.part "empty")
    let fveCalendarStateAction = ComponentElementBuilder(CalendarElements.part "action")
    let fveCalendarControls = ComponentElementBuilder(CalendarElements.part "controls")
    let fveCalendarRange = ComponentElementBuilder(CalendarElements.part "range")
