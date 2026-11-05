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
type CalendarSelectionMode =
    | Single
    | Range

/// <exclude/>
[<RequireQualifiedAccess>]
type internal MonthCalendarSelection =
    | Single of DateOnly option
    | Range of DateOnly option * DateOnly option

/// <category>month-calendar</category>
[<NoEquality; NoComparison>]
type internal MonthCalendarSelectionConfig =
    private
        { id: string
          label: string
          name: string
          rangeEndName: string
          selection: MonthCalendarSelection
          displayMonth: DateOnly
          minimum: DateOnly option
          maximum: DateOnly option
          unavailable: Set<DateOnly>
          locale: string
          weekStart: MonthCalendarWeekStart
          captionLayout: MonthCalendarCaptionLayout
          clearable: bool
          currentDate: DateOnly
          today: DateOnly option
          closeOnSelection: bool
          disabled: bool
          readOnly: bool
          pending: bool
          attributes: HtmlAttribute list }

/// <summary>
/// A DateOnly-based compact month selection configuration.
/// </summary>
/// <exclude/>
[<RequireQualifiedAccess>]
module internal MonthCalendarSelectionRendering =
    let private firstOfMonth (value: DateOnly) = DateOnly(value.Year, value.Month, 1)

    let private dateValue (value: DateOnly) =
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

    let private validateSelection =
        function
        | MonthCalendarSelection.Range(Some startDate, Some endDate) when endDate < startDate ->
            invalidArg "selection" "Range end cannot precede range start."
        | selection -> selection

    let create id label name selection =
        let selection = validateSelection selection

        let currentDate = DateOnly.FromDateTime DateTime.Today
        let initial =
            match selection with
            | MonthCalendarSelection.Single(Some value)
            | MonthCalendarSelection.Range(Some value, _) -> value
            | _ -> currentDate

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
          currentDate = currentDate
          today = None
          closeOnSelection = false
          disabled = false
          readOnly = false
          pending = false
          attributes = [] }

    let private selectedValues =
        function
        | MonthCalendarSelection.Single value -> value, None
        | MonthCalendarSelection.Range(startDate, endDate) -> startDate, endDate

    let render (config: MonthCalendarSelectionConfig) =
        let selectedDate, endDate = selectedValues config.selection
        let currentDate = config.today |> Option.defaultValue config.currentDate
        let currentDateJs = ComponentHtml.javascriptString (dateValue currentDate)
        let token = ComponentHtml.signalToken config.id
        let monthSignal = "_month_calendar_" + token + "_month"
        let selectedSignal = "_month_calendar_" + token + "_selected"
        let endSignal = "_month_calendar_" + token + "_end"

        let monthValue =
            config.displayMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture)

        let selectedValue = selectedDate |> Option.map dateValue |> Option.defaultValue ""
        let endValue = endDate |> Option.map dateValue |> Option.defaultValue ""
        let minimumValue = config.minimum |> Option.map dateValue |> Option.defaultValue ""
        let maximumValue = config.maximum |> Option.map dateValue |> Option.defaultValue ""

        let minimumMonth =
            config.minimum
            |> Option.map (firstOfMonth >> _.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            |> Option.defaultValue ""

        let maximumMonth =
            config.maximum
            |> Option.map (firstOfMonth >> _.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            |> Option.defaultValue ""

        let todayYear = config.today |> Option.map _.Year |> Option.defaultValue config.displayMonth.Year

        let minimumYear =
            config.minimum
            |> Option.map _.Year
            |> Option.defaultValue (max 1 (min (config.displayMonth.Year - 100) todayYear))

        let maximumYear =
            config.maximum
            |> Option.map _.Year
            |> Option.defaultValue (min 9999 (max (config.displayMonth.Year + 100) todayYear))

        let effectiveMinimumMonth =
            if
                config.captionLayout = MonthCalendarCaptionLayout.Dropdown
                && String.IsNullOrEmpty minimumMonth
            then
                minimumYear.ToString("0000", CultureInfo.InvariantCulture) + "-01"
            else
                minimumMonth

        let effectiveMaximumMonth =
            if
                config.captionLayout = MonthCalendarCaptionLayout.Dropdown
                && String.IsNullOrEmpty maximumMonth
            then
                maximumYear.ToString("0000", CultureInfo.InvariantCulture) + "-12"
            else
                maximumMonth

        let unavailable =
            config.unavailable
            |> Seq.map dateValue
            |> Seq.toArray
            |> JsonSerializer.Serialize

        let weekStart =
            if config.weekStart = MonthCalendarWeekStart.Sunday then
                0
            else
                1

        let culture = CultureInfo.GetCultureInfo config.locale
        let monthNames = culture.DateTimeFormat.AbbreviatedMonthNames |> Array.take 12

        let weekdayNames =
            culture.DateTimeFormat.AbbreviatedDayNames
            |> Array.toList
            |> fun names -> if weekStart = 0 then names else names.Tail @ [ names.Head ]

        let isRange =
            match config.selection with
            | MonthCalendarSelection.Range _ -> true
            | _ -> false

        let unavailableState = config.disabled

        let dateScript index =
            "const first = new Date($"
            + monthSignal
            + " + '-01T00:00:00Z'); "
            + $"const offset = (first.getUTCDay() - {weekStart} + 7) %% 7; "
            + $"const date = new Date(Date.UTC(first.getUTCFullYear(), first.getUTCMonth(), {index + 1} - offset)); "
            + "const value = date.toISOString().slice(0, 10); "
            + "el.dataset.value = value; el.textContent = date.getUTCDate(); "
            + $"if (value == {currentDateJs}) el.setAttribute('aria-current', 'date'); else el.removeAttribute('aria-current'); "
            + "el.dataset.outside = date.getUTCMonth() == first.getUTCMonth() ? 'false' : 'true'; "
            + "el.setAttribute('aria-label', new Intl.DateTimeFormat("
            + ComponentHtml.javascriptString config.locale
            + ", { dateStyle: 'full', timeZone: 'UTC' }).format(date)); "
            + "el.setAttribute('aria-selected', ($"
            + selectedSignal
            + " == value || $"
            + endSignal
            + " == value) ? 'true' : 'false'); "
            + "el.dataset.inRange = ($"
            + selectedSignal
            + " && $"
            + endSignal
            + " && value > $"
            + selectedSignal
            + " && value < $"
            + endSignal
            + ") ? 'true' : 'false'; "
            + "el.disabled = "
            + (if unavailableState then "true" else "false")
            + " || ("
            + ComponentHtml.javascriptString minimumValue
            + " && value < "
            + ComponentHtml.javascriptString minimumValue
            + ") || ("
            + ComponentHtml.javascriptString maximumValue
            + " && value > "
            + ComponentHtml.javascriptString maximumValue
            + ") || "
            + unavailable
            + ".includes(value)"

        let changeMonth expression =
            $"const month = {expression}; const minimum = {ComponentHtml.javascriptString effectiveMinimumMonth}; const maximum = {ComponentHtml.javascriptString effectiveMaximumMonth}; ${monthSignal} = minimum && month < minimum ? minimum : maximum && month > maximum ? maximum : month"

        let focusScript =
            "const buttons = [...el.closest('[role=grid]').querySelectorAll('[role=gridcell]')]; const index = buttons.indexOf(el); "
            + "const moves = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 }; "
            + "const focus = button => { if (button && !button.disabled) { buttons.forEach(b => b.tabIndex = b == button ? 0 : -1); button.focus() } }; "
            + "if (moves[evt.key] != null) { evt.preventDefault(); const step = moves[evt.key]; let target = index + step; while (target >= 0 && target < buttons.length && buttons[target].disabled) target += step; focus(buttons[target]) } "
            + "else if (evt.key == 'Home' || evt.key == 'End') { evt.preventDefault(); const week = buttons.slice(index - index % 7, index - index % 7 + 7).filter(b => !b.disabled); focus(evt.key == 'Home' ? week[0] : week.at(-1)) } "
            + "else if (evt.key == 'PageUp' || evt.key == 'PageDown') { evt.preventDefault(); const day = Number(el.dataset.value.slice(8)); "
            + changeMonth (
                "(() => { const value = new Date($"
                + monthSignal
                + " + '-01T00:00:00Z'); value.setUTCMonth(value.getUTCMonth() + (evt.key == 'PageUp' ? -1 : 1)); return value.toISOString().slice(0, 7) })()"
            )
            + "; requestAnimationFrame(() => { const month = $"
            + monthSignal
            + "; const candidates = buttons.filter(b => !b.disabled && b.dataset.value.startsWith(month)); const target = candidates.reduce((nearest, b) => !nearest || Math.abs(Number(b.dataset.value.slice(8)) - day) < Math.abs(Number(nearest.dataset.value.slice(8)) - day) ? b : nearest, null); focus(target) }) }"

        let close =
            "const popover = el.closest('[popover]'); if (popover) { popover.hidePopover(); const trigger = document.querySelector('[popovertarget=\"' + popover.id + '\"]'); trigger?.focus() }"

        let commitSelection startDate endDate =
            $"${selectedSignal} = {startDate}; ${endSignal} = {endDate}; "
            + (if config.closeOnSelection then close else "")

        let selectScript =
            if config.readOnly then "evt.preventDefault()"
            elif not isRange then commitSelection "el.dataset.value" "''"
            else
                $"if (!${selectedSignal} || ${endSignal}) {{ ${selectedSignal} = el.dataset.value; ${endSignal} = '' }} "
                + $"else if (el.dataset.value < ${selectedSignal}) {{ const previous = ${selectedSignal}; "
                + commitSelection "el.dataset.value" "previous"
                + " } else { "
                + commitSelection ("$" + selectedSignal) "el.dataset.value"
                + " }"

        let initialOffset = (int config.displayMonth.DayOfWeek - weekStart + 7) % 7

        let isBlocked date =
            unavailableState
            || (config.minimum |> Option.exists (fun minimum -> date < minimum))
            || (config.maximum |> Option.exists (fun maximum -> date > maximum))
            || config.unavailable.Contains date

        let initialFocus =
            selectedDate
            |> Option.filter (fun date -> firstOfMonth date = config.displayMonth && not (isBlocked date))
            |> Option.orElseWith (fun () ->
                [ 0 .. DateTime.DaysInMonth(config.displayMonth.Year, config.displayMonth.Month) - 1 ]
                |> List.map config.displayMonth.AddDays
                |> List.tryFind (isBlocked >> not))

        let previousMonth =
            "(() => { const value = new Date($"
            + monthSignal
            + " + '-01T00:00:00Z'); value.setUTCMonth(value.getUTCMonth() - 1); return value.toISOString().slice(0, 7) })()"

        let nextMonth =
            "(() => { const value = new Date($"
            + monthSignal
            + " + '-01T00:00:00Z'); value.setUTCMonth(value.getUTCMonth() + 1); return value.toISOString().slice(0, 7) })()"

        let unavailableLiteral = if unavailableState then "true" else "false"
        let minimumMonthJs = ComponentHtml.javascriptString effectiveMinimumMonth
        let maximumMonthJs = ComponentHtml.javascriptString effectiveMaximumMonth

        let monthAtBound comparison bound =
            let boundJs = ComponentHtml.javascriptString bound
            $"el.disabled = {unavailableLiteral} || ({boundJs} && ${monthSignal} {comparison} {boundJs})"

        let ownerForm =
            ComponentElement.value "form" config.attributes
            |> Option.map (fun id -> "document.getElementById(" + ComponentHtml.javascriptString id + ")")
            |> Option.defaultValue "el.closest('form')"

        let reset =
            "if (evt.target === "
            + ownerForm
            + ") queueMicrotask(() => { if (!evt.defaultPrevented) { $"
            + monthSignal
            + " = "
            + ComponentHtml.javascriptString monthValue
            + "; $"
            + selectedSignal
            + " = "
            + ComponentHtml.javascriptString selectedValue
            + "; $"
            + endSignal
            + " = "
            + ComponentHtml.javascriptString endValue
            + " } })"

        section {
            _id config.id
            _ariaLabel config.label

            if config.pending then
                _ariaBusy true

            _attr ("data-fve-month-calendar", "selection")

            _dataSignals
                $"{{{monthSignal}: {ComponentHtml.javascriptString monthValue}, {selectedSignal}: {ComponentHtml.javascriptString selectedValue}, {endSignal}: {ComponentHtml.javascriptString endValue}}}"

            _dataOn ("reset__window", reset)
            _tabindex 0

            _class (
                "grid w-[18rem] max-w-full min-w-0 gap-3 overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-3 text-[var(--fve-text)] "
                + ComponentContent.classes config.attributes
            )

            for attribute in
                ComponentHtml.safeAttributes [ "id"; "class"; "aria-label"; "aria-busy"; "form" ] config.attributes do
                attribute

            let formAttributes =
                config.attributes
                |> List.filter (fun attribute -> attribute.Name.ToLowerInvariant() = "form")

            input {
                _type "hidden"
                _name config.name
                _dataBind selectedSignal
                _value selectedValue
                _disabled config.disabled

                for attribute in formAttributes do
                    attribute
            }

            if isRange then
                input {
                    _type "hidden"
                    _name config.rangeEndName
                    _dataBind endSignal
                    _value endValue
                    _disabled config.disabled

                    for attribute in formAttributes do
                        attribute
                }

            header {
                _class "flex min-w-[16.5rem] items-center justify-between gap-3"

                button {
                    _type "button"
                    _ariaLabel "Previous month"
                    _disabled unavailableState
                    _dataEffect (monthAtBound "<=" effectiveMinimumMonth)
                    _dataOn ("click", changeMonth previousMonth)

                    _class
                        "grid size-9 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                    raw
                        """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M11.78 5.22a.75.75 0 0 1 0 1.06L8.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06l-4.25-4.25a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>"""
                }

                h2 {
                    _ariaLive "polite"

                    _dataText (
                        "new Intl.DateTimeFormat("
                        + ComponentHtml.javascriptString config.locale
                        + ", { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(new Date($"
                        + monthSignal
                        + " + '-01T00:00:00Z'))"
                    )

                    _class (
                        if config.captionLayout = MonthCalendarCaptionLayout.Dropdown then
                            "sr-only"
                        else
                            "text-sm font-semibold"
                    )

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

                                _dataOn (
                                    "change",
                                    changeMonth ("$" + monthSignal + ".slice(0, 4) + '-' + evt.currentTarget.value")
                                )

                                _class
                                    "h-8 w-20 appearance-none rounded-[var(--fve-radius-control)] bg-transparent py-1 pr-6 pl-2 text-sm font-medium hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                                for month in 1..12 do
                                    let value = month.ToString("00", CultureInfo.InvariantCulture)

                                    option {
                                        _value value

                                        if month = config.displayMonth.Month then
                                            _selected true

                                        _dataEffect (
                                            $"const candidate = ${monthSignal}.slice(0, 4) + '-{value}'; el.disabled = {unavailableLiteral} || ({minimumMonthJs} && candidate < {minimumMonthJs}) || ({maximumMonthJs} && candidate > {maximumMonthJs})"
                                        )

                                        monthNames[month - 1]
                                    }
                            }

                            raw
                                """<svg viewBox="0 0 20 20" fill="currentColor" class="pointer-events-none absolute top-1/2 right-1.5 size-3.5 -translate-y-1/2 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                        }

                        span {
                            _class "relative inline-flex"

                            select {
                                _ariaLabel "Year"
                                _disabled unavailableState
                                _value (string config.displayMonth.Year)
                                _dataEffect ("el.value = $" + monthSignal + ".slice(0, 4)")

                                _dataOn (
                                    "change",
                                    changeMonth ("evt.currentTarget.value + '-' + $" + monthSignal + ".slice(5, 7)")
                                )

                                _class
                                    "h-8 w-20 appearance-none rounded-[var(--fve-radius-control)] bg-transparent py-1 pr-6 pl-2 text-sm font-medium hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                                for year in minimumYear..maximumYear do
                                    option {
                                        _value (string year)

                                        if year = config.displayMonth.Year then
                                            _selected true

                                        string year
                                    }
                            }

                            raw
                                """<svg viewBox="0 0 20 20" fill="currentColor" class="pointer-events-none absolute top-1/2 right-1.5 size-3.5 -translate-y-1/2 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M5.22 7.22a.75.75 0 0 1 1.06 0L10 10.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 8.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                        }
                    }

                button {
                    _type "button"
                    _ariaLabel "Next month"
                    _disabled unavailableState
                    _dataEffect (monthAtBound ">=" effectiveMaximumMonth)
                    _dataOn ("click", changeMonth nextMonth)

                    _class
                        "grid size-9 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                    raw
                        """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 1 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>"""
                }
            }

            CompactMonth.render config.label weekdayNames 6 "min-w-[16.5rem] gap-1" (fun index ->
                let date = config.displayMonth.AddDays(index - initialOffset)
                let value = dateValue date
                let selected = selectedValue = value || endValue = value

                let inRange =
                    selectedValue <> ""
                    && endValue <> ""
                    && value > selectedValue
                    && value < endValue

                let blocked = isBlocked date

                button {
                    _type "button"
                    _role "gridcell"
                    _ariaLabel (date.ToString("D", culture))
                    _ariaSelected selected
                    if date = currentDate then _ariaCurrent "date"

                    if config.readOnly then
                        _ariaDisabled true

                    _disabled blocked
                    _tabindex (if initialFocus = Some date then 0 else -1)
                    _attr ("data-value", value)

                    _attr (
                        "data-outside",
                        if date.Month = config.displayMonth.Month then
                            "false"
                        else
                            "true"
                    )

                    _attr ("data-in-range", if inRange then "true" else "false")
                    _dataEffect (dateScript index)
                    _dataOn ("click", selectScript)
                    _dataOn ("keydown", focusScript)

                    _class
                        "grid min-h-9 min-w-0 place-items-center rounded-[var(--fve-radius-control)] text-sm tabular-nums [&[aria-current=date]:not([aria-selected=true]):not([data-in-range=true])]:bg-[var(--fve-surface-hover)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:cursor-not-allowed disabled:opacity-30 data-[outside=true]:text-[var(--fve-muted-text)] data-[in-range=true]:rounded-none data-[in-range=true]:bg-[var(--fve-brand-subtle)] aria-selected:bg-[var(--fve-brand-solid)] aria-selected:text-white"

                    string date.Day
                })

            if config.clearable || config.today.IsSome then
                let actionClass = "rounded-[var(--fve-radius-control)] px-2 py-1 text-sm font-medium text-[var(--fve-brand-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                div {
                    _class "flex min-w-0 items-center justify-between gap-2"
                    if config.clearable then
                        button {
                            _type "button"
                            _disabled unavailableState
                            if config.readOnly then _ariaDisabled true
                            _dataOn ("click", if config.readOnly then "evt.preventDefault()" else $"${selectedSignal} = ''; ${endSignal} = ''")
                            _class actionClass
                            "Clear"
                        }
                    match config.today with
                    | None -> ()
                    | Some today ->
                        let value = ComponentHtml.javascriptString (dateValue today)
                        let month = ComponentHtml.javascriptString (today.ToString("yyyy-MM", CultureInfo.InvariantCulture))
                        button {
                            _type "button"
                            _ariaLabel ("Select today, " + today.ToString("D", culture))
                            _disabled (config.readOnly || isBlocked today)
                            _data ("value", dateValue today)
                            _dataOn ("click",
                                if config.readOnly then "evt.preventDefault()"
                                else $"${monthSignal} = {month}; " + commitSelection value (if isRange then value else "''"))
                            _class (actionClass + " ms-auto")
                            "Today"
                        }
                }
        }

module internal MonthSelectionElements =
    let flag name attributes =
        ComponentElement.attribute name attributes
        |> Option.exists (fun attribute -> attribute.Value <> ValueSome "false")

    let optionalDate name attributes =
        match ComponentElement.value name attributes with
        | None
        | Some "" -> None
        | Some _ -> Some(CalendarElements.parseDate name attributes)

    let nativeAttributes attributes =
        ComponentHtml.safeAttributes
            [ "name"
              "value"
              "min"
              "max"
              "readonly"
              "disabled"
              "display-month"
              "week-start"
              "caption-layout"
              "selection-mode"
              "end-value"
              "range-end-name"
              "unavailable-dates"
              "clearable"
              "current-date"
              "today"
              "close-on-selection" ]
            attributes

    let configuration attributes =
        let mode =
            ComponentElement.choice "selection-mode" "single" [ "single"; "range" ] attributes

        let value = optionalDate "value" attributes
        let ending = optionalDate "end-value" attributes

        if
            mode = "single"
            && (ending.IsSome
                || ComponentElement.attribute "range-end-name" attributes |> Option.isSome)
        then
            invalidArg "attributes" "Range-end attributes require range selection."

        if ending.IsSome && value.IsNone then
            invalidArg "attributes" "A range end requires a start date."

        let selection =
            if mode = "range" then
                MonthCalendarSelection.Range(value, ending)
            else
                MonthCalendarSelection.Single value

        let config =
            MonthCalendarSelectionRendering.create
                (ComponentElement.required "id" attributes)
                (ComponentElement.required "aria-label" attributes)
                (ComponentElement.required "name" attributes)
                selection

        let today = optionalDate "today" attributes
        let suppliedCurrentDate = optionalDate "current-date" attributes
        let currentDate = today |> Option.orElse suppliedCurrentDate |> Option.defaultValue config.currentDate
        let initialMonth = if value.IsNone then currentDate else config.displayMonth
        let minimum = optionalDate "min" attributes
        let maximum = optionalDate "max" attributes

        match minimum, maximum with
        | Some minimum, Some maximum when minimum > maximum ->
            invalidArg "attributes" "Minimum date cannot follow maximum date."
        | _ -> ()

        let locale = ComponentElement.value "lang" attributes |> Option.defaultValue "en-US"
        CultureInfo.GetCultureInfo(TextField.requiredText "lang" locale) |> ignore

        let unavailable =
            ComponentElement.value "unavailable-dates" attributes
            |> Option.map (fun json ->
                JsonSerializer.Deserialize<string list>(json)
                |> List.map (fun value -> CalendarElements.parseDate "date" [ _attr ("date", value) ])
                |> Set.ofList)
            |> Option.defaultValue Set.empty

        { config with
            displayMonth =
                optionalDate "display-month" attributes
                |> Option.map (fun date -> DateOnly(date.Year, date.Month, 1))
                |> Option.defaultValue (DateOnly(initialMonth.Year, initialMonth.Month, 1))
            rangeEndName =
                ComponentElement.value "range-end-name" attributes
                |> Option.map (TextField.requiredText "range-end-name")
                |> Option.defaultValue config.rangeEndName
            minimum = minimum
            maximum = maximum
            unavailable = unavailable
            locale = locale
            weekStart =
                if ComponentElement.choice "week-start" "sunday" [ "sunday"; "monday" ] attributes = "monday" then
                    MonthCalendarWeekStart.Monday
                else
                    MonthCalendarWeekStart.Sunday
            captionLayout =
                if ComponentElement.choice "caption-layout" "label" [ "label"; "dropdown" ] attributes = "dropdown" then
                    MonthCalendarCaptionLayout.Dropdown
                else
                    MonthCalendarCaptionLayout.Label
            clearable = flag "clearable" attributes
            currentDate = currentDate
            today = today
            closeOnSelection = flag "close-on-selection" attributes
            disabled = flag "disabled" attributes
            readOnly = flag "readonly" attributes
            pending = flag "aria-busy" attributes
            attributes = nativeAttributes attributes }

/// <category>month-calendar</category>
[<AutoOpen>]
module CalendarSelectionAttributes =
    type Html with
        static member _mode(value: CalendarSelectionMode) =
            Html._attr (
                "selection-mode",
                match value with
                | CalendarSelectionMode.Single -> "single"
                | CalendarSelectionMode.Range -> "range"
            )

        /// Override the default local rendering date without adding a Today shortcut.
        static member _currentDate(value: DateOnly) =
            Html._attr ("current-date", value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))

        /// Opt into a Today shortcut and use this date for the current-day highlight.
        static member _today(value: DateOnly) =
            Html._attr ("today", value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))

        static member _endValue(value: string) = Html._attr ("end-value", value)
        static member _rangeEndName(value: string) = Html._attr ("range-end-name", value)

        static member _displayMonth(value: DateOnly) =
            Html._attr ("display-month", value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))

        static member _unavailableDates(values: string list) =
            Html._attr ("unavailable-dates", JsonSerializer.Serialize values)

        static member _weekStart(value: MonthCalendarWeekStart) =
            Html._attr (
                "week-start",
                match value with
                | MonthCalendarWeekStart.Sunday -> "sunday"
                | MonthCalendarWeekStart.Monday -> "monday"
            )

        static member _captionLayout(value: MonthCalendarCaptionLayout) =
            Html._attr (
                "caption-layout",
                match value with
                | MonthCalendarCaptionLayout.Label -> "label"
                | MonthCalendarCaptionLayout.Dropdown -> "dropdown"
            )

        static member _closeOnSelection(value: bool) =
            Html._attr ("close-on-selection", if value then "true" else "false")

/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
type MonthCalendarLayout =
    | Full
    | Compact

/// <category>month-calendar</category>
[<NoEquality; NoComparison>]
type internal MonthCalendarConfig<'destination> =
    private
        { id: string
          display: CalendarDisplayConfig<'destination>
          layout: MonthCalendarLayout }

/// <summary>A month event grid with full and compact layouts. Form selection has its own native builder.</summary>
/// <remarks>
/// Full layout reserves three event rows per day; all-day ranges keep their lane across week boundaries.
/// All-day end dates are inclusive. Overflow opens the complete day list, including continuing events.
/// Use _id for distinct stable DOM scopes when calendars share a label.
/// Form selection submits date-only values; display calendars use destination navigation.
/// </remarks>
/// <category>month-calendar</category>
[<RequireQualifiedAccess>]
module internal MonthCalendar =
    let internal renderCompact resolve embedded id (config: CalendarDisplayConfig<'destination>) =
        let first = DateOnly(config.date.Year, config.date.Month, 1)
        let offset = (int first.DayOfWeek + 6) % 7

        section {
            for attribute in
                ComponentElement.attributes (
                    [ match id with
                      | Some id -> _id id
                      | None -> ()
                      _ariaLabel config.label
                      _attr ("data-fve-month-calendar", "compact")
                      if not embedded then
                          _tabindex 0
                      _class (
                          if embedded then
                              "grid min-w-0 gap-3 text-[var(--fve-text)]"
                          else
                              "grid w-[18rem] max-w-full min-w-0 gap-3 overflow-x-auto rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-3 text-[var(--fve-text)]"
                      ) ]
                    @ config.attributes
                ) do
                attribute

            header {
                _class (
                    if embedded then
                        "flex min-w-0 items-center justify-between gap-2"
                    else
                        "flex min-w-[14rem] items-center justify-between gap-2"
                )

                if not embedded then
                    CalendarRendering.navigationLink
                        resolve
                        "previous"
                        [ _ariaLabel "Previous month"
                          _class
                              "grid size-9 shrink-0 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]" ]
                        [ raw
                              """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M11.78 5.22a.75.75 0 0 1 0 1.06L8.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06l-4.25-4.25a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/></svg>""" ]
                        config

                h3 {
                    _class "text-sm font-semibold"
                    first.ToString((if embedded then "MMMM" else "MMMM yyyy"), CultureInfo.InvariantCulture)
                }

                if not embedded then
                    CalendarRendering.navigationLink
                        resolve
                        "next"
                        [ _ariaLabel "Next month"
                          _class
                              "grid size-9 shrink-0 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]" ]
                        [ raw
                              """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 1 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/></svg>""" ]
                        config
            }

            let weeks =
                if embedded then
                    (offset + DateTime.DaysInMonth(first.Year, first.Month) + 6) / 7
                else
                    6

            CompactMonth.render
                (first.ToString("MMMM yyyy", CultureInfo.InvariantCulture) + " dates")
                [ "Mon"; "Tue"; "Wed"; "Thu"; "Fri"; "Sat"; "Sun" ]
                weeks
                (if embedded then "min-w-0 gap-0" else "min-w-[14rem] gap-1")
                (fun index ->
                    let date = first.AddDays(index - offset)
                    let outside = date.Month <> first.Month
                    let events = config.events |> List.filter (CalendarEvent.occursOn date)
                    let today = config.today |> Option.exists (fun (day, _) -> day = date)
                    let selected = config.selectedDate = Some date

                    let label =
                        date.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture)
                        + (if List.isEmpty events then
                               ""
                           else
                               ", "
                               + string events.Length
                               + " events: "
                               + (events |> List.map _.title |> String.concat ", "))

                    let classes =
                        "relative grid min-h-10 min-w-0 grid-cols-1 place-items-center rounded-[var(--fve-radius-control)] tabular-nums "
                        + (if embedded then "text-xs " else "text-sm ")
                        + (if outside then
                               "text-[var(--fve-muted-text)] "
                           else
                               "text-[var(--fve-text)] ")
                        + (if selected then "bg-[var(--fve-brand-subtle)] " else "")

                    let content =
                        fragment {
                            time {
                                _datetime (date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))

                                if today then
                                    _ariaCurrent "date"

                                _class (
                                    if today then
                                        "grid aspect-square w-7 max-w-full place-items-center rounded-full bg-[var(--fve-brand-solid)] text-white"
                                    else
                                        ""
                                )

                                string date.Day
                            }

                            if not (List.isEmpty events) then
                                span {
                                    _ariaHidden "true"

                                    _class
                                        "absolute bottom-0 left-1/2 size-1 -translate-x-1/2 rounded-full bg-[var(--fve-brand-solid)]"
                                }
                        }

                    div {
                        _role "gridcell"
                        _ariaLabel label
                        _attr ("data-date", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                        _attr ("data-outside", if outside then "true" else "false")

                        if embedded && outside then
                            _class "invisible"
                            _ariaHidden true

                        match config.dateDestination |> Option.bind (fun destination -> destination date) with
                        | Some destination when not (embedded && (outside || List.isEmpty events)) ->
                            ComponentElement.render
                                "a"
                                [ _href (resolve destination)
                                  _ariaLabel ((if selected then "Selected: " else "Show ") + label)
                                  _class (
                                      classes
                                      + "no-underline hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                                  ) ]
                                (CalendarRendering.dateAttributes date config)
                                [ content ]
                        | _ ->
                            span {
                                _class classes
                                content
                            }
                    })
        }

    let private renderFull resolve (config: MonthCalendarConfig<'destination>) =
        let display = config.display
        let first = DateOnly(display.date.Year, display.date.Month, 1)
        let start = first.AddDays(-((int first.DayOfWeek + 6) % 7))

        let count =
            ((first.DayNumber - start.DayNumber
              + DateTime.DaysInMonth(first.Year, first.Month)
              + 6)
             / 7)
            * 7

        let format pattern (date: DateOnly) =
            date.ToString(pattern, CultureInfo.InvariantCulture)
        // Assign one lane for the visible range, so a continuing event does not jump at a week boundary.
        let laneEnds = ResizeArray<DateOnly>()

        let allDayPlacements =
            display.events
            |> List.filter (fun event ->
                event.time.IsNone
                && event.endDate >= start
                && event.date <= start.AddDays(count - 1))
            |> List.sortBy (fun event -> event.date, -(event.endDate.DayNumber), event.id)
            |> List.map (fun event ->
                let lane =
                    laneEnds
                    |> Seq.tryFindIndex (fun ending -> ending < event.date)
                    |> Option.defaultValue laneEnds.Count

                if lane = laneEnds.Count then
                    laneEnds.Add event.endDate
                else
                    laneEnds[lane] <- event.endDate

                event, lane)

        let eventTime (event: CalendarEvent<'destination>) =
            match event.time with
            | Some(start, finish) ->
                start.ToString("h:mm tt", CultureInfo.InvariantCulture)
                + "–"
                + finish.ToString("h:mm tt", CultureInfo.InvariantCulture)
            | None -> CalendarEvent.allDayLabel event

        let eventLink compact segment (event: CalendarEvent<'destination>) =
            let continuationClasses =
                match segment with
                | Some(first: DateOnly, last: DateOnly) ->
                    (if first > event.date then " rounded-l-none" else "")
                    + (if last < event.endDate then " rounded-r-none" else "")
                | None -> ""

            a {
                _href (resolve event.destination)
                _ariaLabel (event.title + ", " + eventTime event)

                _class (
                    "fve-calendar-event min-w-0 rounded-[var(--fve-radius-control)] no-underline focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] "
                    + (if not compact then
                           "block p-2 hover:bg-[var(--fve-surface-hover)]"
                       elif event.time.IsNone then
                           "absolute inset-y-0 left-0 flex h-7 items-center gap-1 overflow-hidden bg-[var(--fve-brand-text)] px-1.5 text-[light-dark(white,#101828)] hover:opacity-90"
                       else
                           "flex h-7 w-full items-center gap-1 overflow-hidden px-1.5 hover:bg-[var(--fve-surface-hover)]")
                    + continuationClasses
                )

                match segment with
                | Some(first, last) ->
                    let columns = last.DayNumber - first.DayNumber + 1
                    // Each crossed column contributes its padding and the one-pixel calendar divider.
                    _style $"width:calc({columns} * 100%% + {columns - 1} * (0.5rem + 1px))"
                | None -> ()

                if compact then
                    match event.time with
                    | Some(start, _) ->
                        span {
                            _ariaHidden true
                            _class "size-1.5 shrink-0 rounded-full bg-[var(--fve-brand-solid)]"
                        }

                        time {
                            _datetime (
                                format "yyyy-MM-dd" event.date
                                + "T"
                                + start.ToString("HH:mm", CultureInfo.InvariantCulture)
                            )

                            _class "shrink-0 text-xs tabular-nums"

                            start
                                .ToString((if start.Minute = 0 then "htt" else "h:mmtt"), CultureInfo.InvariantCulture)
                                .ToLowerInvariant()
                        }
                    | None -> ()

                    span {
                        _class "min-w-0 truncate text-xs font-medium"
                        event.title
                    }
                else
                    p {
                        _class "text-sm font-medium [overflow-wrap:anywhere]"
                        event.title
                    }

                    p {
                        _class "text-xs text-[var(--fve-muted-text)]"
                        eventTime event
                    }

                    match event.detail with
                    | Some detail ->
                        p {
                            _class "mt-1 text-xs text-[var(--fve-muted-text)] [overflow-wrap:anywhere]"
                            detail
                        }
                    | None -> ()
            }

        div {
            _class
                "fve-calendar-body min-w-0 overflow-x-auto rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)]"

            _tabindex 0
            _role "region"
            _ariaLabel (display.label + " scrollable dates")

            _dataOn (
                "keydown",
                "if (evt.target === el && !evt.ctrlKey && !evt.metaKey && !evt.altKey && ['ArrowLeft','ArrowRight'].includes(evt.key) && el.scrollWidth > el.clientWidth) { evt.preventDefault(); el.scrollBy({left:evt.key === 'ArrowLeft' ? -64 : 64}) }"
            )

            div {
                _class
                    "fve-calendar-weekdays grid min-w-[56rem] grid-cols-7 border-b border-[var(--fve-border)] py-3 text-center text-xs text-[var(--fve-muted-text)]"

                _ariaHidden true

                for day in [ "Mon"; "Tue"; "Wed"; "Thu"; "Fri"; "Sat"; "Sun" ] do
                    span { day }
            }

            ol {
                _ariaLabel (display.label + " dates")

                _class
                    "fve-calendar-days m-0 grid min-w-[56rem] auto-rows-[10rem] list-none grid-cols-7 gap-px bg-[var(--fve-border)] p-0"

                for offset in 0 .. count - 1 do
                    let date = start.AddDays offset
                    let dateId = format "yyyy-MM-dd" date
                    let label = format "dddd, MMMM d, yyyy" date
                    let selected = display.selectedDate = Some date
                    let today = display.today |> Option.exists (fun (day, _) -> day = date)

                    let events =
                        display.events
                        |> List.filter (CalendarEvent.occursOn date)
                        |> List.sortBy (fun event ->
                            event.time
                            |> Option.map (fun (start, _) -> start.Ticks)
                            |> Option.defaultValue -1L)

                    let weekStart = start.AddDays(offset / 7 * 7)

                    let visibleAllDay =
                        allDayPlacements
                        |> List.filter (fun (event, lane) -> lane < 3 && CalendarEvent.occursOn date event)

                    let reservedRows =
                        visibleAllDay |> List.fold (fun count (_, lane) -> max count (lane + 1)) 0

                    let visibleTimed =
                        events
                        |> List.filter (fun event -> event.time.IsSome)
                        |> List.truncate (3 - reservedRows)

                    let hiddenCount = events.Length - visibleAllDay.Length - visibleTimed.Length

                    li {
                        _class "fve-calendar-day min-w-0 bg-[var(--fve-background)] p-1"
                        _attr ("data-date", dateId)
                        _attr ("data-empty", if List.isEmpty events then "true" else "false")
                        _attr ("data-selected", if selected then "true" else "false")
                        _attr ("data-outside", if date.Month <> first.Month then "true" else "false")

                        h3 {
                            _class "fve-calendar-date m-0 flex h-9 items-center justify-center text-sm font-medium"

                            let content =
                                time {
                                    _datetime dateId

                                    if today then
                                        _ariaCurrent "date"

                                    _class (
                                        "grid size-7 place-items-center rounded-full "
                                        + (if today then
                                               "bg-[var(--fve-brand-text)] text-[light-dark(white,#101828)]"
                                           elif date.Month <> first.Month then
                                               "text-[var(--fve-muted-text)]"
                                           else
                                               "text-[var(--fve-text)]")
                                    )

                                    string date.Day
                                }

                            match display.dateDestination |> Option.bind (fun destination -> destination date) with
                            | Some destination ->
                                ComponentElement.render
                                    "a"
                                    [ _href (resolve destination)
                                      _ariaLabel ((if selected then "Selected: " else "Show ") + label)
                                      _class
                                          "inline-flex rounded-full focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]" ]
                                    (CalendarRendering.dateAttributes date display)
                                    [ content ]
                            | None ->
                                span {
                                    _ariaLabel label
                                    content
                                }

                            if selected then
                                span {
                                    _class "sr-only"
                                    "Selected date"
                                }
                        }

                        ol {
                            _ariaLabel (label + " events")
                            _class "fve-calendar-events m-0 grid grid-rows-[repeat(3,1.75rem)] list-none gap-0.5 p-0"

                            for event, lane in visibleAllDay do
                                let segmentStart = max weekStart event.date

                                li {
                                    _attr ("data-event", event.id)
                                    _style $"grid-row:{lane + 1}"

                                    if date = segmentStart then
                                        _class "relative z-10 min-w-0"

                                        eventLink
                                            true
                                            (Some(segmentStart, min (weekStart.AddDays 6) event.endDate))
                                            event
                                    else
                                        // The visible bar starts earlier in this week; retain this day's accessible event list.
                                        span {
                                            _class "sr-only"
                                            event.title + ", " + eventTime event
                                        }
                                }

                            for index, event in visibleTimed |> List.indexed do
                                li {
                                    _class "min-w-0"
                                    _attr ("data-event", event.id)
                                    _style $"grid-row:{reservedRows + index + 1}"
                                    eventLink true None event
                                }
                        }

                        if hiddenCount > 0 then
                            let popupId = config.id + "-" + dateId + "-events"

                            let content =
                                fragment {
                                    header {
                                        _class "mb-3 flex items-start justify-between gap-3"

                                        h4 {
                                            _class "text-sm font-semibold"
                                            label
                                        }

                                        button {
                                            _type "button"
                                            _ariaLabel "Close day events"

                                            _class
                                                "grid size-8 shrink-0 place-items-center rounded-[var(--fve-radius-control)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"

                                            _dataOn (
                                                "click",
                                                "const popup = el.closest('[popover]'); popup.hidePopover(); popup.previousElementSibling?.focus()"
                                            )

                                            raw
                                                """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 0 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 0 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""
                                        }
                                    }

                                    ol {
                                        _class "m-0 grid list-none gap-1 p-0"

                                        for event in events do
                                            li {
                                                _attr ("data-event", event.id)
                                                eventLink false None event
                                            }
                                    }
                                }

                            fvePopover {
                                yield _id (popupId)
                                yield _focusOnOpen true

                                yield
                                    fvePopoverTrigger {
                                        _ariaLabel (($"Show all {events.Length} events for {label}"))

                                        _class (
                                            "inline-flex min-h-7 items-center rounded-[var(--fve-radius-control)] px-1.5 text-xs font-medium text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                                        )

                                        fun (triggerAttributes: HtmlAttribute list) ->
                                            button {
                                                for attribute in triggerAttributes do
                                                    attribute

                                                text $"{hiddenCount} more"
                                            }

                                    }

                                yield
                                    fvePopoverContent {
                                        yield _ariaLabel ((label + " events"))
                                        yield (content: HtmlElement)
                                    }
                            }
                    }
            }
        }

    let internal render resolve (config: MonthCalendarConfig<'destination>) =
        match config.layout, config.display.state with
        | MonthCalendarLayout.Compact, CalendarState.Ready ->
            renderCompact resolve false (Some config.id) config.display
        | _ ->
            CalendarRendering.render
                resolve
                (fun resolve _ -> renderFull resolve config)
                (Some config.id)
                config.display

/// <category>month-calendar</category>
[<AutoOpen>]
module MonthCalendarAttributes =
    type Html with
        static member _layout(value: MonthCalendarLayout) =
            Html._attr (
                "layout",
                match value with
                | MonthCalendarLayout.Full -> "full"
                | MonthCalendarLayout.Compact -> "compact"
            )

/// <category>month-calendar</category>
[<AutoOpen>]
module MonthCalendarDsl =
    let fveMonthCalendarSelection =
        ComponentElementBuilder(fun attributes children ->
            ComponentContent.noChildren "fveMonthCalendarSelection" children

            MonthSelectionElements.configuration attributes
            |> MonthCalendarSelectionRendering.render)

    let fveMonthCalendar =
        CalendarDisplayBuilder(
            CalendarView.Month,
            fun attributes display ->
                let layout =
                    match ComponentElement.choice "layout" "full" [ "full"; "compact" ] attributes with
                    | "compact" -> MonthCalendarLayout.Compact
                    | _ -> MonthCalendarLayout.Full

                let calendarId =
                    ComponentElement.value "id" attributes
                    |> Option.defaultValue ("month-calendar-" + ComponentHtml.optionToken display.label)

                MonthCalendar.render
                    id
                    { id = calendarId
                      display = display
                      layout = layout }
        )
