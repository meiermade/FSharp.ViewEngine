namespace FSharp.ViewEngine.Components.Application

open System
open FSharp.ViewEngine
open type Html

[<RequireQualifiedAccess>]
type CalendarView = List | Day | Week | Month

[<NoEquality; NoComparison>]
type CalendarEvent<'destination> =
    private
        { id:string
          title:string
          dateLabel:string
          timeLabel:string option
          detail:string option
          destination:'destination }

[<RequireQualifiedAccess>]
module CalendarEvent =
    let create id title dateLabel destination =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable event ID is required."
        if String.IsNullOrWhiteSpace title || String.IsNullOrWhiteSpace dateLabel then invalidArg (nameof title) "An event title and date label are required."
        { id = id; title = title; dateLabel = dateLabel; timeLabel = None; detail = None; destination = destination }
    let withTime timeLabel (event:CalendarEvent<'destination>) = { event with timeLabel = Some timeLabel }
    let withDetail detail (event:CalendarEvent<'destination>) = { event with detail = Some detail }

[<NoEquality; NoComparison>]
type CalendarConfig<'destination> =
    private
        { label:string
          view:CalendarView
          rangeLabel:string
          events:CalendarEvent<'destination> list
          previous:'destination option
          next:'destination option
          viewDestinations:(CalendarView * 'destination) list
          emptyState:HtmlElement }

[<RequireQualifiedAccess>]
module Calendar =
    let create label view rangeLabel events =
        if String.IsNullOrWhiteSpace label || String.IsNullOrWhiteSpace rangeLabel then invalidArg (nameof label) "Calendar label and current range are required."
        { label = label; view = view; rangeLabel = rangeLabel; events = events; previous = None; next = None; viewDestinations = []
          emptyState =
            p {
                _class "p-4 text-sm text-[var(--fve-muted-text)]"
                "No events in this range."
            } }
    let withPrevious destination config = { config with previous = Some destination }
    let withNext destination config = { config with next = Some destination }
    let withViewDestinations destinations config = { config with viewDestinations = destinations }
    let withEmptyState emptyState config = { config with emptyState = emptyState }
    let render resolve config =
        let viewLabel = function CalendarView.List -> "List" | CalendarView.Day -> "Day" | CalendarView.Week -> "Week" | CalendarView.Month -> "Month"
        section {
            _ariaLabel config.label
            _class "grid gap-4"
            header {
                _class "flex flex-wrap items-center justify-between gap-3"
                div {
                    h2 {
                        _class "text-lg font-semibold text-[var(--fve-text)]"
                        config.label
                    }
                    p {
                        _class "text-sm text-[var(--fve-muted-text)]"
                        config.rangeLabel
                    }
                }
                nav {
                    _ariaLabel "Calendar date navigation"
                    _class "flex items-center gap-2"
                    match config.previous with
                    | Some destination ->
                        a {
                            _href (resolve destination)
                            _class "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                            "Previous"
                        }
                    | None -> ()
                    match config.next with
                    | Some destination ->
                        a {
                            _href (resolve destination)
                            _class "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold ring-1 ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                            "Next"
                        }
                    | None -> ()
                }
            }
            if not (List.isEmpty config.viewDestinations) then
                nav {
                    _ariaLabel "Calendar view"
                    _class "flex flex-wrap gap-2"
                    for view, destination in config.viewDestinations do
                        a {
                            _href (resolve destination)
                            if view = config.view then _ariaCurrent "page"
                            _class (if view = config.view then "rounded-[var(--fve-radius-control)] bg-[var(--fve-brand-subtle)] px-3 py-2 text-sm font-semibold text-[var(--fve-brand-text)]" else "rounded-[var(--fve-radius-control)] px-3 py-2 text-sm font-semibold text-[var(--fve-muted-text)] hover:bg-[var(--fve-surface-hover)]")
                            viewLabel view
                        }
                }
            if List.isEmpty config.events then config.emptyState
            else
                ol {
                    _class (match config.view with CalendarView.List -> "grid gap-2" | CalendarView.Day -> "grid gap-2 md:grid-cols-2" | CalendarView.Week -> "grid gap-2 md:grid-cols-3" | CalendarView.Month -> "grid gap-2 sm:grid-cols-2 lg:grid-cols-4")
                    for event in config.events do
                        li {
                            _id event.id
                            a {
                                _href (resolve event.destination)
                                _class "block min-h-full rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-3 ring-1 ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                                p {
                                    _class "text-xs font-semibold text-[var(--fve-muted-text)]"
                                    event.dateLabel
                                }
                                strong {
                                    _class "mt-1 block text-sm text-[var(--fve-text)]"
                                    event.title
                                }
                                match event.timeLabel with
                                | Some time ->
                                    span {
                                        _class "mt-1 block text-sm text-[var(--fve-muted-text)]"
                                        time
                                    }
                                | None -> ()
                                match event.detail with
                                | Some detail ->
                                    span {
                                        _class "mt-1 block text-sm text-[var(--fve-muted-text)]"
                                        detail
                                    }
                                | None -> ()
                            }
                        }
                }
        }
