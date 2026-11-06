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
        { id = id; title = title; date = date; endDate = date; time = None; detail = None; destination = destination }
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
    let internal occursOn date (event:CalendarEvent<'destination>) = event.date <= date && date <= event.endDate
    let internal allDayLabel (event:CalendarEvent<'destination>) =
        if event.endDate = event.date then "All day"
        else "All day, " + event.date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) + "–" + event.endDate.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)

/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
type CalendarState = Ready | Loading | Error of message:string | Unavailable of message:string

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
          state = CalendarState.Ready; stateAction = None; attributes = []; navigation = Map.empty }

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

    let render (resolve:'destination -> string) (renderDates:('destination -> string) -> CalendarDisplayConfig<'destination> -> HtmlElement) id (config:CalendarDisplayConfig<'destination>) =
        let days = Calendar.dates config.view config.date
        let events = config.events |> List.filter (fun event -> event.endDate >= days.Head && event.date <= List.last days) |> List.sortBy (fun event -> event.date, event.time, event.id)
        let allDayRows = days |> List.map (fun date -> events |> List.filter (fun event -> CalendarEvent.occursOn date event && event.time.IsNone) |> List.length) |> List.max
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
                _class ("fve-calendar-date m-0 text-sm font-medium " + (if timed then "sticky top-0 z-20 flex h-16 items-center border-b border-[var(--fve-border)] bg-[var(--fve-background)] p-2" else "p-0.5"))
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
        let eventContent event =
            a {
                _href (resolve event.destination)
                _class ("fve-calendar-event block min-w-0 rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)] no-underline [overflow-wrap:anywhere] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] " + (if timed && event.time.IsSome then "h-full overflow-auto p-1" else "p-2"))
                strong { _class "block text-sm font-medium"; event.title }
                span {
                    _class "block text-xs"
                    match event.time with
                    | Some (start, finish) -> start.ToString("h:mm tt", CultureInfo.InvariantCulture) + "–" + finish.ToString("h:mm tt", CultureInfo.InvariantCulture)
                    | None -> CalendarEvent.allDayLabel event
                }
                match event.detail with
                | Some detail -> span { _class "mt-1 block text-xs"; detail }
                | None -> ()
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
                nav {
                    _ariaLabel (config.label + " date navigation")
                    _class "flex flex-wrap items-center gap-1"
                    for key, label in ["previous", "Previous"; "today", "Today"; "next", "Next"] do
                        navigationLink resolve key [_class control] [text label] config
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
                if config.view = CalendarView.Month || config.view = CalendarView.Year then renderDates resolve config
                else
                    (Html.el "fve-scroll-region") {
                        _class ("fve-calendar-body min-w-0 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] " + (if timed then "relative grid max-h-152 grid-cols-[3.5rem_minmax(0,1fr)] overflow-auto" else "block overflow-x-auto"))
                        _style $"--fve-calendar-hours:{endHour - startHour};--fve-calendar-days:{days.Length};--fve-calendar-all-day-rows:{allDayRows}"
                        _tabindex 0
                        _role "region"
                        _attr("scroll-axis", "both")
                        _ariaLabel (config.label + (if timed then " scrollable times" else " scrollable dates"))
                        if timed then
                            div {
                                _class "fve-calendar-hours sticky left-0 z-30 col-start-1 row-start-1 grid w-14 self-start bg-[var(--fve-background)] pt-[calc(4rem+var(--fve-calendar-all-day-rows)*4rem)] text-xs text-[var(--fve-muted-text)]"
                                _ariaHidden "true"
                                for hour in startHour .. endHour - 1 do
                                    span { _class "h-32 p-1 text-center"; TimeOnly(hour, 0).ToString("h tt", CultureInfo.InvariantCulture) }
                            }
                        ol {
                            _class ("fve-calendar-days m-0 grid list-none p-0 " + (if timed then "col-start-2 row-start-1 grid-cols-[repeat(var(--fve-calendar-days),minmax(0,1fr))]" + (if config.view = CalendarView.Day then " min-w-80" else " min-w-[112rem] @min-[48rem]/fve-calendar:min-w-[56rem]") else "min-w-[56rem] grid-cols-7 gap-px bg-[var(--fve-border)]"))
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

    let part kind attributes children =
        if List.contains kind ["previous"; "next"; "today"; "date"] then ComponentElement.required "href" attributes |> ignore
        if kind = "today" || kind = "date" then parseDate "date" attributes |> ignore
        if kind = "date" then ComponentContent.noChildren "fveCalendarDate" children
        CalendarPartElement(kind, attributes, children) :> HtmlElement

    let configuration view attributes (children:HtmlElement list) events =
        let label = ComponentElement.required "label" attributes
        let date = parseDate "date" attributes
        let mutable config = CalendarRendering.create label view date events
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
            attributes = ComponentHtml.safeAttributes ["date"; "label"; "selected-date"; "calendar-state"; "layout"] attributes }

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
