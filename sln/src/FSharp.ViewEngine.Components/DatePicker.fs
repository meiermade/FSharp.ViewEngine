namespace FSharp.ViewEngine.Components

open System
open System.Globalization
open FSharp.ViewEngine
open type Html
open type Datastar

/// <category>date-picker</category>
[<NoEquality; NoComparison>]
type DatePickerConfig =
    private
        { id:string
          name:string
          label:string
          selection:MonthCalendarSelection
          displayMonth:DateOnly option
          minimum:DateOnly option
          maximum:DateOnly option
          unavailable:DateOnly list
          locale:string
          weekStart:MonthCalendarWeekStart
          captionLayout:MonthCalendarCaptionLayout
          rangeEndName:string option
          description:string option
          validation:string option
          clearable:bool
          disabled:bool
          pending:bool
          attributes:HtmlAttribute list }

/// <summary>
/// A labelled form field that composes Popover and compact MonthCalendar selection.
/// </summary>
/// <category>date-picker</category>
[<RequireQualifiedAccess>]
module DatePicker =
    let private validateSelection = function
        | MonthCalendarSelection.Range(Some startDate, Some endDate) when endDate < startDate -> invalidArg "selection" "Range end cannot precede range start."
        | selection -> selection

    let create id name label selection =
        { id = TextField.stableId id
          name = TextField.requiredText (nameof name) name
          label = TextField.requiredText (nameof label) label
          selection = validateSelection selection
          displayMonth = None
          minimum = None
          maximum = None
          unavailable = []
          locale = "en-US"
          weekStart = MonthCalendarWeekStart.Sunday
          captionLayout = MonthCalendarCaptionLayout.Label
          rangeEndName = None
          description = None
          validation = None
          clearable = false
          disabled = false
          pending = false
          attributes = [] }

    let withDisplayMonth month (config:DatePickerConfig) = { config with displayMonth = Some month }

    let withBounds minimum maximum (config:DatePickerConfig) =
        if minimum > maximum then invalidArg (nameof minimum) "Minimum date cannot follow maximum date."
        { config with minimum = Some minimum; maximum = Some maximum }

    let withUnavailable dates (config:DatePickerConfig) = { config with unavailable = dates }

    let withLocale locale (config:DatePickerConfig) =
        CultureInfo.GetCultureInfo(TextField.requiredText (nameof locale) locale) |> ignore
        { config with locale = locale }

    let withWeekStart weekStart (config:DatePickerConfig) = { config with weekStart = weekStart }
    let withCaptionLayout captionLayout (config:DatePickerConfig) = { config with captionLayout = captionLayout }
    let withRangeEndName name (config:DatePickerConfig) = { config with rangeEndName = Some(TextField.requiredText (nameof name) name) }
    let withDescription description (config:DatePickerConfig) = { config with description = Some(TextField.requiredText (nameof description) description) }
    let withValidation message (config:DatePickerConfig) = { config with validation = Some(TextField.requiredText (nameof message) message) }
    let clearable (config:DatePickerConfig) = { config with clearable = true }
    let disabled (config:DatePickerConfig) = { config with disabled = true }
    let pending (config:DatePickerConfig) = { config with pending = true }
    let withAttributes attributes (config:DatePickerConfig) = { config with attributes = attributes }

    let render (config:DatePickerConfig) =
        let culture = CultureInfo.GetCultureInfo config.locale
        let format (value:DateOnly) = value.ToString("d", culture)
        let visibleValue =
            match config.selection with
            | MonthCalendarSelection.Single(Some value) -> format value
            | MonthCalendarSelection.Single None -> "Choose date"
            | MonthCalendarSelection.Range(Some startDate, Some endDate) -> format startDate + " – " + format endDate
            | MonthCalendarSelection.Range(Some startDate, None) -> format startDate + " – Choose end date"
            | MonthCalendarSelection.Range(None, _) -> "Choose date range"
        let calendarToken = ComponentHtml.signalToken (config.id + "-calendar")
        let selectedSignal = "_month_calendar_" + calendarToken + "_selected"
        let endSignal = "_month_calendar_" + calendarToken + "_end"
        let formatExpression signal = "new Intl.DateTimeFormat(" + ComponentHtml.javascriptString config.locale + ", { dateStyle: 'short', timeZone: 'UTC' }).format(new Date($" + signal + " + 'T00:00:00Z'))"
        let visibleValueExpression =
            match config.selection with
            | MonthCalendarSelection.Single _ -> "$" + selectedSignal + " ? " + formatExpression selectedSignal + " : 'Choose date'"
            | MonthCalendarSelection.Range _ -> "$" + selectedSignal + " ? (" + formatExpression selectedSignal + " + ($" + endSignal + " ? ' – ' + " + formatExpression endSignal + " : ' – Choose end date')) : 'Choose date range'"
        let labelId = config.id + "-label"
        let descriptionId = config.id + "-description"
        let validationId = config.id + "-validation"
        let triggerId = config.id + "-popover-trigger"
        let describedBy =
            [ if config.description.IsSome then descriptionId
              if config.validation.IsSome then validationId ]
            |> String.concat " "
        let mutable calendar =
            MonthCalendar.createSelection (config.id + "-calendar") (config.label + " calendar") config.name config.selection
            |> MonthCalendar.withLocale config.locale
            |> MonthCalendar.withWeekStart config.weekStart
            |> MonthCalendar.withCaptionLayout config.captionLayout
            |> MonthCalendar.closeOnSelection
        calendar <- config.displayMonth |> Option.map (fun month -> MonthCalendar.withDisplayMonth month calendar) |> Option.defaultValue calendar
        calendar <-
            match config.minimum, config.maximum with
            | Some minimum, Some maximum -> MonthCalendar.withBounds minimum maximum calendar
            | _ -> calendar
        calendar <- if List.isEmpty config.unavailable then calendar else MonthCalendar.withUnavailableDates config.unavailable calendar
        calendar <- config.rangeEndName |> Option.map (fun name -> MonthCalendar.withRangeEndName name calendar) |> Option.defaultValue calendar
        calendar <- if config.clearable then MonthCalendar.clearable calendar else calendar
        calendar <- if config.disabled then MonthCalendar.disabled calendar else calendar
        calendar <- if config.pending then MonthCalendar.pending calendar else calendar
        let trigger =
            span {
                _class "flex min-w-48 items-center justify-between gap-3"
                span { _dataText visibleValueExpression; visibleValue }
                raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4 shrink-0 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M5.75 2a.75.75 0 0 1 .75.75V4h7V2.75a.75.75 0 0 1 1.5 0V4h.25A2.75 2.75 0 0 1 18 6.75v8.5A2.75 2.75 0 0 1 15.25 18H4.75A2.75 2.75 0 0 1 2 15.25v-8.5A2.75 2.75 0 0 1 4.75 4H5V2.75A.75.75 0 0 1 5.75 2ZM3.5 8.5v6.75c0 .69.56 1.25 1.25 1.25h10.5c.69 0 1.25-.56 1.25-1.25V8.5h-13Z" clip-rule="evenodd"/></svg>"""
            }
        div {
            _class "grid min-w-0 content-start gap-1.5"
            for attribute in ComponentHtml.safeAttributes [ "class" ] config.attributes do attribute
            label {
                _id labelId
                _for triggerId
                _class "flex items-center gap-2 text-sm font-medium text-[var(--fve-text)]"
                config.label
                if config.pending then ComponentHtml.loadingGlyph ControlSize.Small
            }
            Popover.create (config.id + "-popover") config.label trigger (config.label + " calendar") (MonthCalendar.renderSelection calendar)
            |> Popover.withAlignment PopoverAlignment.Start
            |> Popover.withContentClass "w-auto max-w-[calc(100vw-2rem)] overflow-hidden p-0"
            |> Popover.withTriggerAttributes [
                _ariaLabelledby labelId
                if describedBy <> "" then _ariaDescribedby describedBy
                _ariaInvalid config.validation.IsSome
                if config.pending then _ariaBusy true
                _disabled (config.disabled || config.pending) ]
            |> Popover.render
            match config.description with
            | Some description -> p { _id descriptionId; _class "text-sm text-[var(--fve-muted-text)]"; description }
            | None -> ()
            match config.validation with
            | Some message -> p { _id validationId; _role "alert"; _class "text-sm text-[var(--fve-critical-text)]"; message }
            | None -> ()
        }
