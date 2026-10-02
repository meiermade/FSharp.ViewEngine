namespace FSharp.ViewEngine.Components

open System
open System.Globalization
open System.Text.Json
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
type MonthCalendarWeekStart =
    | Sunday
    | Monday

/// <summary>Controls whether the calendar caption is a label or direct month and year selectors.</summary>
/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
type MonthCalendarCaptionLayout =
    | Label
    | Dropdown

/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
type MonthCalendarSelection =
    | Single of DateOnly option
    | Range of DateOnly option * DateOnly option

/// <category>month-calendar</category>
[<NoEquality; NoComparison>]
type MonthCalendarSelectionConfig =
    private
        { id:string
          label:string
          name:string
          rangeEndName:string
          selection:MonthCalendarSelection
          displayMonth:DateOnly
          minimum:DateOnly option
          maximum:DateOnly option
          unavailable:Set<DateOnly>
          locale:string
          weekStart:MonthCalendarWeekStart
          captionLayout:MonthCalendarCaptionLayout
          clearable:bool
          closeOnSelection:bool
          disabled:bool
          pending:bool
          attributes:HtmlAttribute list }

/// <summary>
/// A DateOnly-based compact month selection configuration.
/// </summary>
/// <exclude/>
[<RequireQualifiedAccess>]
module internal MonthCalendarSelectionRendering =
    let private firstOfMonth (value:DateOnly) = DateOnly(value.Year, value.Month, 1)
    let private dateValue (value:DateOnly) = value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    let private validateSelection = function
        | MonthCalendarSelection.Range(Some startDate, Some endDate) when endDate < startDate -> invalidArg "selection" "Range end cannot precede range start."
        | selection -> selection

    let create id label name selection =
        let selection = validateSelection selection
        let initial =
            match selection with
            | MonthCalendarSelection.Single(Some value)
            | MonthCalendarSelection.Range(Some value, _) -> value
            | _ -> DateOnly.FromDateTime DateTime.Today
        { id = TextField.stableId id
          label = TextField.requiredText (nameof label) label
          name = TextField.requiredText (nameof name) name
          rangeEndName = name + "End"
          selection = selection
          displayMonth = firstOfMonth initial
          minimum = None
          maximum = None
          unavailable = Set.empty
          locale = "en-US"
          weekStart = MonthCalendarWeekStart.Sunday
          captionLayout = MonthCalendarCaptionLayout.Label
          clearable = false
          closeOnSelection = false
          disabled = false
          pending = false
          attributes = [] }

    let withDisplayMonth month (config:MonthCalendarSelectionConfig) = { config with displayMonth = firstOfMonth month }

    let withBounds minimum maximum (config:MonthCalendarSelectionConfig) =
        if minimum > maximum then invalidArg (nameof minimum) "Minimum date cannot follow maximum date."
        { config with minimum = Some minimum; maximum = Some maximum }

    let withUnavailable dates (config:MonthCalendarSelectionConfig) = { config with unavailable = Set.ofList dates }

    let withLocale locale (config:MonthCalendarSelectionConfig) =
        CultureInfo.GetCultureInfo(TextField.requiredText (nameof locale) locale) |> ignore
        { config with locale = locale }

    let withWeekStart weekStart (config:MonthCalendarSelectionConfig) = { config with weekStart = weekStart }
    let withCaptionLayout captionLayout (config:MonthCalendarSelectionConfig) = { config with captionLayout = captionLayout }
    let withRangeEndName name (config:MonthCalendarSelectionConfig) = { config with rangeEndName = TextField.requiredText (nameof name) name }
    let clearable (config:MonthCalendarSelectionConfig) = { config with clearable = true }
    let closeOnSelection (config:MonthCalendarSelectionConfig) = { config with closeOnSelection = true }
    let disabled (config:MonthCalendarSelectionConfig) = { config with disabled = true }
    let pending (config:MonthCalendarSelectionConfig) = { config with pending = true }
    let withAttributes attributes (config:MonthCalendarSelectionConfig) = { config with attributes = attributes }

    let private selectedValues = function
        | MonthCalendarSelection.Single value -> value, None
        | MonthCalendarSelection.Range(startDate, endDate) -> startDate, endDate

    let render (config:MonthCalendarSelectionConfig) =
        let selectedDate, endDate = selectedValues config.selection
        let token = ComponentHtml.signalToken config.id
        let monthSignal = "_month_calendar_" + token + "_month"
        let selectedSignal = "_month_calendar_" + token + "_selected"
        let endSignal = "_month_calendar_" + token + "_end"
        let monthValue = config.displayMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture)
        let selectedValue = selectedDate |> Option.map dateValue |> Option.defaultValue ""
        let endValue = endDate |> Option.map dateValue |> Option.defaultValue ""
        let minimumValue = config.minimum |> Option.map dateValue |> Option.defaultValue ""
        let maximumValue = config.maximum |> Option.map dateValue |> Option.defaultValue ""
        let minimumMonth = config.minimum |> Option.map (firstOfMonth >> _.ToString("yyyy-MM", CultureInfo.InvariantCulture)) |> Option.defaultValue ""
        let maximumMonth = config.maximum |> Option.map (firstOfMonth >> _.ToString("yyyy-MM", CultureInfo.InvariantCulture)) |> Option.defaultValue ""
        let minimumYear = config.minimum |> Option.map _.Year |> Option.defaultValue (max 1 (config.displayMonth.Year - 100))
        let maximumYear = config.maximum |> Option.map _.Year |> Option.defaultValue (min 9999 (config.displayMonth.Year + 100))
        let effectiveMinimumMonth =
            if config.captionLayout = MonthCalendarCaptionLayout.Dropdown && String.IsNullOrEmpty minimumMonth then minimumYear.ToString("0000", CultureInfo.InvariantCulture) + "-01" else minimumMonth
        let effectiveMaximumMonth =
            if config.captionLayout = MonthCalendarCaptionLayout.Dropdown && String.IsNullOrEmpty maximumMonth then maximumYear.ToString("0000", CultureInfo.InvariantCulture) + "-12" else maximumMonth
        let unavailable = config.unavailable |> Seq.map dateValue |> Seq.toArray |> JsonSerializer.Serialize
        let weekStart = if config.weekStart = MonthCalendarWeekStart.Sunday then 0 else 1
        let culture = CultureInfo.GetCultureInfo config.locale
        let monthNames = culture.DateTimeFormat.AbbreviatedMonthNames |> Array.take 12
        let weekdayNames =
            culture.DateTimeFormat.AbbreviatedDayNames
            |> Array.toList
            |> fun names -> if weekStart = 0 then names else names.Tail @ [ names.Head ]
        let isRange = match config.selection with MonthCalendarSelection.Range _ -> true | _ -> false
        let unavailableState = config.disabled || config.pending
        let dateScript index =
            "const first = new Date($" + monthSignal + " + '-01T00:00:00Z'); "
            + $"const offset = (first.getUTCDay() - {weekStart} + 7) %% 7; "
            + $"const date = new Date(Date.UTC(first.getUTCFullYear(), first.getUTCMonth(), {index + 1} - offset)); "
            + "const value = date.toISOString().slice(0, 10); "
            + "el.dataset.value = value; el.textContent = date.getUTCDate(); "
            + "el.dataset.outside = date.getUTCMonth() == first.getUTCMonth() ? 'false' : 'true'; "
            + "el.setAttribute('aria-label', new Intl.DateTimeFormat(" + ComponentHtml.javascriptString config.locale + ", { dateStyle: 'full', timeZone: 'UTC' }).format(date)); "
            + "el.setAttribute('aria-selected', ($" + selectedSignal + " == value || $" + endSignal + " == value) ? 'true' : 'false'); "
            + "el.dataset.inRange = ($" + selectedSignal + " && $" + endSignal + " && value > $" + selectedSignal + " && value < $" + endSignal + ") ? 'true' : 'false'; "
            + "el.disabled = " + (if unavailableState then "true" else "false") + " || (" + ComponentHtml.javascriptString minimumValue + " && value < " + ComponentHtml.javascriptString minimumValue + ") || (" + ComponentHtml.javascriptString maximumValue + " && value > " + ComponentHtml.javascriptString maximumValue + ") || " + unavailable + ".includes(value)"
        let focusScript =
            "const buttons = [...el.closest('[role=grid]').querySelectorAll('[role=gridcell]')]; const index = buttons.indexOf(el); "
            + "const moves = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 }; "
            + "if (moves[evt.key] != null) { evt.preventDefault(); buttons[Math.max(0, Math.min(buttons.length - 1, index + moves[evt.key]))].focus() } "
            + "else if (evt.key == 'Home') { evt.preventDefault(); buttons[index - (index % 7)].focus() } "
            + "else if (evt.key == 'End') { evt.preventDefault(); buttons[Math.min(buttons.length - 1, index + 6 - (index % 7))].focus() } "
            + "else if (evt.key == 'PageUp' || evt.key == 'PageDown') { evt.preventDefault(); const value = new Date($" + monthSignal + " + '-01T00:00:00Z'); value.setUTCMonth(value.getUTCMonth() + (evt.key == 'PageUp' ? -1 : 1)); $" + monthSignal + " = value.toISOString().slice(0, 7) }"
        let close =
            "const popover = el.closest('[popover]'); if (popover) { popover.hidePopover(); const trigger = document.querySelector('[popovertarget=\"' + popover.id + '\"]'); trigger?.focus() }"
        let selectScript =
            if not isRange then
                "$" + selectedSignal + " = el.dataset.value; " + (if config.closeOnSelection then close else "")
            else
                "if (!$" + selectedSignal + " || $" + endSignal + ") { $" + selectedSignal + " = el.dataset.value; $" + endSignal + " = '' } else if (el.dataset.value < $" + selectedSignal + ") { $" + endSignal + " = $" + selectedSignal + "; $" + selectedSignal + " = el.dataset.value; " + (if config.closeOnSelection then close else "") + " } else { $" + endSignal + " = el.dataset.value; " + (if config.closeOnSelection then close else "") + " }"
        let initialOffset = (int config.displayMonth.DayOfWeek - weekStart + 7) % 7
        let changeMonth expression =
            $"const month = {expression}; const minimum = {ComponentHtml.javascriptString effectiveMinimumMonth}; const maximum = {ComponentHtml.javascriptString effectiveMaximumMonth}; ${monthSignal} = minimum && month < minimum ? minimum : maximum && month > maximum ? maximum : month"
        let previousMonth = "(() => { const value = new Date($" + monthSignal + " + '-01T00:00:00Z'); value.setUTCMonth(value.getUTCMonth() - 1); return value.toISOString().slice(0, 7) })()"
        let nextMonth = "(() => { const value = new Date($" + monthSignal + " + '-01T00:00:00Z'); value.setUTCMonth(value.getUTCMonth() + 1); return value.toISOString().slice(0, 7) })()"
        let unavailableLiteral = if unavailableState then "true" else "false"
        let minimumMonthJs = ComponentHtml.javascriptString effectiveMinimumMonth
        let maximumMonthJs = ComponentHtml.javascriptString effectiveMaximumMonth
        let monthAtBound comparison bound =
            let boundJs = ComponentHtml.javascriptString bound
            $"el.disabled = {unavailableLiteral} || ({boundJs} && ${monthSignal} {comparison} {boundJs})"
        section {
            _id config.id
            _ariaLabel config.label
            if config.pending then _ariaBusy true
            _attr ("data-fve-month-calendar", "selection")
            _dataSignals $"{{{monthSignal}: {ComponentHtml.javascriptString monthValue}, {selectedSignal}: {ComponentHtml.javascriptString selectedValue}, {endSignal}: {ComponentHtml.javascriptString endValue}}}"
            _tabindex 0
            _class "grid w-[18rem] max-w-full min-w-0 gap-3 overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-3 text-[var(--fve-text)]"
            for attribute in ComponentHtml.safeAttributes [ "id"; "class"; "aria-label"; "aria-busy" ] config.attributes do attribute
            input { _type "hidden"; _name config.name; _dataBind selectedSignal; _value selectedValue }
            if isRange then input { _type "hidden"; _name config.rangeEndName; _dataBind endSignal; _value endValue }
            header {
                _class "flex min-w-[16.5rem] items-center justify-between gap-3"
                button {
                    _type "button"
                    _ariaLabel "Previous month"
                    _disabled unavailableState
                    _dataEffect (monthAtBound "<=" effectiveMinimumMonth)
                    _dataOn ("click", changeMonth previousMonth)
                    _class "grid size-9 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                    raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M11.78 5.22a.75.75 0 0 1 0 1.06L8.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06l-4.25-4.25a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>"""
                }
                h2 {
                    _ariaLive "polite"
                    _dataText ("new Intl.DateTimeFormat(" + ComponentHtml.javascriptString config.locale + ", { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(new Date($" + monthSignal + " + '-01T00:00:00Z'))")
                    _class (if config.captionLayout = MonthCalendarCaptionLayout.Dropdown then "sr-only" else "text-sm font-semibold")
                    config.displayMonth.ToString("MMMM yyyy", culture)
                }
                if config.captionLayout = MonthCalendarCaptionLayout.Dropdown then
                    div {
                        _class "flex h-9 items-center justify-center gap-1.5 text-sm font-medium"
                        span {
                            _class "relative inline-flex"
                            select {
                                _ariaLabel "Month"
                                _disabled unavailableState
                                _value (config.displayMonth.Month.ToString("00", CultureInfo.InvariantCulture))
                                _dataEffect ("el.value = $" + monthSignal + ".slice(5, 7)")
                                _dataOn ("change", changeMonth ("$" + monthSignal + ".slice(0, 4) + '-' + evt.currentTarget.value"))
                                _class "h-8 w-20 appearance-none rounded-[var(--fve-radius-control)] bg-transparent py-1 pr-6 pl-2 text-sm font-medium hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                                for month in 1..12 do
                                    let value = month.ToString("00", CultureInfo.InvariantCulture)
                                    option {
                                        _value value
                                        if month = config.displayMonth.Month then _selected true
                                        _dataEffect ($"const candidate = ${monthSignal}.slice(0, 4) + '-{value}'; el.disabled = {unavailableLiteral} || ({minimumMonthJs} && candidate < {minimumMonthJs}) || ({maximumMonthJs} && candidate > {maximumMonthJs})")
                                        monthNames[month - 1]
                                    }
                            }
                            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="pointer-events-none absolute top-1/2 right-1.5 size-3.5 -translate-y-1/2 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                        }
                        span {
                            _class "relative inline-flex"
                            select {
                                _ariaLabel "Year"
                                _disabled unavailableState
                                _value (string config.displayMonth.Year)
                                _dataEffect ("el.value = $" + monthSignal + ".slice(0, 4)")
                                _dataOn ("change", changeMonth ("evt.currentTarget.value + '-' + $" + monthSignal + ".slice(5, 7)"))
                                _class "h-8 w-20 appearance-none rounded-[var(--fve-radius-control)] bg-transparent py-1 pr-6 pl-2 text-sm font-medium hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                                for year in minimumYear..maximumYear do
                                    option {
                                        _value (string year)
                                        if year = config.displayMonth.Year then _selected true
                                        string year
                                    }
                            }
                            raw """<svg viewBox="0 0 20 20" fill="currentColor" class="pointer-events-none absolute top-1/2 right-1.5 size-3.5 -translate-y-1/2 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                        }
                    }
                button {
                    _type "button"
                    _ariaLabel "Next month"
                    _disabled unavailableState
                    _dataEffect (monthAtBound ">=" effectiveMaximumMonth)
                    _dataOn ("click", changeMonth nextMonth)
                    _class "grid size-9 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                    raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 1 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                }
            }
            CompactMonth.render config.label weekdayNames 6 "min-w-[16.5rem] gap-1" (fun index ->
                let date = config.displayMonth.AddDays(index - initialOffset)
                let value = dateValue date
                let selected = selectedValue = value || endValue = value
                let inRange = selectedValue <> "" && endValue <> "" && value > selectedValue && value < endValue
                let blocked =
                    unavailableState
                    || (config.minimum |> Option.exists (fun minimum -> date < minimum))
                    || (config.maximum |> Option.exists (fun maximum -> date > maximum))
                    || config.unavailable.Contains date
                button {
                    _type "button"
                    _role "gridcell"
                    _ariaLabel (date.ToString("D", culture))
                    _ariaSelected selected
                    _disabled blocked
                    _tabindex (if index = initialOffset || selected then 0 else -1)
                    _attr ("data-value", value)
                    _attr ("data-outside", if date.Month = config.displayMonth.Month then "false" else "true")
                    _attr ("data-in-range", if inRange then "true" else "false")
                    _dataEffect (dateScript index)
                    _dataOn ("click", selectScript)
                    _dataOn ("keydown", focusScript)
                    _class "grid min-h-9 min-w-0 place-items-center rounded-[var(--fve-radius-control)] text-sm tabular-nums hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-30 data-[outside=true]:text-[var(--fve-muted-text)] data-[in-range=true]:rounded-none data-[in-range=true]:bg-[var(--fve-brand-subtle)] aria-selected:bg-[var(--fve-brand-solid)] aria-selected:text-white"
                    string date.Day
                }
            )
            if config.clearable then
                button {
                    _type "button"
                    _disabled unavailableState
                    _dataOn ("click", "$" + selectedSignal + " = ''; $" + endSignal + " = ''")
                    _class "justify-self-start rounded-[var(--fve-radius-control)] px-2 py-1 text-sm font-medium text-[var(--fve-brand-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                    "Clear date"
                }
        }

/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
type MonthCalendarLayout = Full | Compact

/// <category>month-calendar</category>
[<NoEquality; NoComparison>]
type MonthCalendarConfig<'destination> =
    private { display:CalendarDisplayConfig<'destination>; layout:MonthCalendarLayout }

/// <summary>A month event grid with full and compact layouts. Form selection uses a separate typed configuration.</summary>
/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
module MonthCalendar =
    let create label (date:DateOnly) (events:CalendarEvent<'destination> list) =
        { display = CalendarRendering.create label CalendarPeriod.Month date events; layout = MonthCalendarLayout.Full }
    let withLayout layout (config:MonthCalendarConfig<'destination>) = { config with layout = layout }
    let withPrevious destination (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.withPrevious destination config.display }
    let withNext destination (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.withNext destination config.display }
    let withToday date destination (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.withToday date destination config.display }
    let withSelectedDate date (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.withSelectedDate date config.display }
    let withDateDestination destination (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.withDateDestination destination config.display }
    let withEmptyState content (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.withEmptyState content config.display }
    let loading (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.loading config.display }
    let withError message (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.withError message config.display }
    let withUnavailable message (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.withUnavailable message config.display }
    let withStateAction action (config:MonthCalendarConfig<'destination>) = { config with display = CalendarRendering.withStateAction action config.display }

    let internal renderCompact resolve embedded (config:CalendarDisplayConfig<'destination>) =
        let first = DateOnly(config.date.Year, config.date.Month, 1)
        let offset = (int first.DayOfWeek + 6) % 7
        section {
            _ariaLabel config.label
            _attr ("data-fve-month-calendar", "compact")
            if not embedded then _tabindex 0
            _class (if embedded then "grid min-w-0 gap-3 text-[var(--fve-text)]" else "grid w-[18rem] max-w-full min-w-0 gap-3 overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-3 text-[var(--fve-text)]")
            header {
                _class (if embedded then "flex min-w-0 items-center justify-between gap-2" else "flex min-w-[14rem] items-center justify-between gap-2")
                if not embedded then
                    match config.previous with
                    | Some destination -> a { _href (resolve destination); _ariaLabel "Previous month"; _class "grid size-9 shrink-0 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M11.78 5.22a.75.75 0 0 1 0 1.06L8.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06l-4.25-4.25a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>""" }
                    | None -> ()
                h3 { _class "text-sm font-semibold"; first.ToString((if embedded then "MMMM" else "MMMM yyyy"), CultureInfo.InvariantCulture) }
                if not embedded then
                    match config.next with
                    | Some destination -> a { _href (resolve destination); _ariaLabel "Next month"; _class "grid size-9 shrink-0 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 1 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>""" }
                    | None -> ()
            }
            let weeks = if embedded then (offset + DateTime.DaysInMonth(first.Year, first.Month) + 6) / 7 else 6
            CompactMonth.render (first.ToString("MMMM yyyy", CultureInfo.InvariantCulture) + " dates") ["Mon"; "Tue"; "Wed"; "Thu"; "Fri"; "Sat"; "Sun"] weeks (if embedded then "min-w-0 gap-0" else "min-w-[14rem] gap-1") (fun index ->
                let date = first.AddDays(index - offset)
                let outside = date.Month <> first.Month
                let events = config.events |> List.filter (fun event -> event.date = date)
                let today = config.today |> Option.exists (fun (day, _) -> day = date)
                let selected = config.selectedDate = Some date
                let label = date.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture) + (if List.isEmpty events then "" else ", " + string events.Length + " events: " + (events |> List.map _.title |> String.concat ", "))
                let classes = "relative grid min-h-9 min-w-0 grid-cols-1 place-items-center rounded-[var(--fve-radius-control)] tabular-nums " + (if embedded then "text-xs " else "text-sm ") + (if outside then "text-[var(--fve-muted-text)] " else "text-[var(--fve-text)] ") + (if selected then "bg-[var(--fve-brand-subtle)] " else "")
                let content = fragment {
                    time {
                        _datetime (date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                        if today then _ariaCurrent "date"
                        _class (if today then "grid aspect-square w-7 max-w-full place-items-center rounded-full bg-[var(--fve-brand-solid)] text-white" else "")
                        string date.Day
                    }
                    if not (List.isEmpty events) then span { _ariaHidden "true"; _class "absolute bottom-0 left-1/2 size-1 -translate-x-1/2 rounded-full bg-[var(--fve-brand-solid)]" }
                }
                div {
                    _role "gridcell"
                    _ariaLabel label
                    _attr ("data-date", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                    _attr ("data-outside", if outside then "true" else "false")
                    if embedded && outside then
                        _class "invisible"
                        _ariaHidden true
                    match config.dateDestination with
                    | Some destination when not (embedded && (outside || List.isEmpty events)) ->
                        a { _href (resolve (destination date)); _ariaLabel ((if selected then "Selected: " else "Show ") + label); _class (classes + "no-underline hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"); content }
                    | _ -> span { _class classes; content }
                }
            )
        }

    let render resolve (config:MonthCalendarConfig<'destination>) =
        match config.layout, config.display.state with
        | MonthCalendarLayout.Compact, CalendarState.Ready -> renderCompact resolve false config.display
        | _ -> CalendarRendering.render resolve (fun _ _ -> empty) config.display

    /// Creates a compact form-selection month; native values are separate from event destinations.
    let createSelection id label name selection = MonthCalendarSelectionRendering.create id label name selection
    let withDisplayMonth month config = MonthCalendarSelectionRendering.withDisplayMonth month config
    let withBounds minimum maximum config = MonthCalendarSelectionRendering.withBounds minimum maximum config
    let withUnavailableDates dates config = MonthCalendarSelectionRendering.withUnavailable dates config
    let withLocale locale config = MonthCalendarSelectionRendering.withLocale locale config
    let withWeekStart weekStart config = MonthCalendarSelectionRendering.withWeekStart weekStart config
    let withCaptionLayout captionLayout config = MonthCalendarSelectionRendering.withCaptionLayout captionLayout config
    let withRangeEndName name config = MonthCalendarSelectionRendering.withRangeEndName name config
    let clearable config = MonthCalendarSelectionRendering.clearable config
    let closeOnSelection config = MonthCalendarSelectionRendering.closeOnSelection config
    let disabled config = MonthCalendarSelectionRendering.disabled config
    let pending config = MonthCalendarSelectionRendering.pending config
    let withAttributes attributes config = MonthCalendarSelectionRendering.withAttributes attributes config
    let renderSelection config = MonthCalendarSelectionRendering.render config
