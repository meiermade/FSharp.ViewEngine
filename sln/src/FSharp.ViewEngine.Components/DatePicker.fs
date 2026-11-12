namespace FSharp.ViewEngine.Components

open System.Globalization
open FSharp.ViewEngine
open type Html
open type Datastar

module internal DatePicker =
    let render attributes children =
        ComponentContent.noChildren "fveDatePicker" children
        let id = ComponentElement.required "id" attributes
        let size = ComponentElement.size None attributes

        let calendarLabel =
            ComponentElement.value "aria-label" attributes |> Option.defaultValue "Calendar"

        let calendar =
            MonthSelectionElements.configuration (attributes @ [ _id (id + "-calendar"); _ariaLabel calendarLabel ])
            |> fun config ->
                { config with
                    closeOnSelection = true
                    attributes =
                        attributes
                        |> List.filter (fun attribute -> attribute.Name.ToLowerInvariant() = "form") }

        let format (value: System.DateOnly option) =
            match value with
            | Some value -> value.ToString("d", CultureInfo.GetCultureInfo calendar.locale)
            | None -> ""

        let visibleValue =
            match calendar.selection with
            | MonthCalendarSelection.Single(Some value) -> format (Some value)
            | MonthCalendarSelection.Single None -> "Choose date"
            | MonthCalendarSelection.Range(Some startDate, Some endDate) ->
                format (Some startDate) + " – " + format (Some endDate)
            | MonthCalendarSelection.Range(Some startDate, None) -> format (Some startDate) + " – Choose end date"
            | MonthCalendarSelection.Range(None, _) -> "Choose date range"

        let content = fvePopover {
            _id (id + "-popover")
            _alignment PopoverAlignment.Start

            fvePopoverTrigger {

                _class (
                    "inline-flex items-center justify-center gap-2 rounded-[var(--fve-radius-control)] [--fve-internal-control-border-width:1px] border border-[var(--fve-border)] bg-[var(--fve-surface)] font-medium text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50 "
                    + ComponentHtml.sizeClasses size)

                for attribute in MonthSelectionElements.nativeAttributes attributes |> ComponentHtml.safeAttributes ["size"] do
                    attribute

                _disabled calendar.disabled

                if calendar.readOnly then
                    _readonly true

                fun (triggerAttributes: HtmlAttribute list) ->
                    button {
                        for attribute in triggerAttributes do
                            attribute

                        span {
                            _class "flex min-w-48 items-center justify-between gap-3"

                            span {
                                _attr ("data-fve-date-picker-value", "")
                                visibleValue
                            }

                            if calendar.pending then
                                ComponentHtml.loadingGlyph Size.Small

                            raw
                                """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4 shrink-0 text-[var(--fve-muted-text)]" aria-hidden="true"><path fill-rule="evenodd" d="M5.75 2a.75.75 0 0 1 .75.75V4h7V2.75a.75.75 0 0 1 1.5 0V4h.25A2.75 2.75 0 0 1 18 6.75v8.5A2.75 2.75 0 0 1 15.25 18H4.75A2.75 2.75 0 0 1 2 15.25v-8.5A2.75 2.75 0 0 1 4.75 4H5V2.75A.75.75 0 0 1 5.75 2ZM3.5 8.5v6.75c0 .69.56 1.25 1.25 1.25h10.5c.69 0 1.25-.56 1.25-1.25V8.5h-13Z" clip-rule="evenodd"/></svg>"""
                        }
                    }

            }

            fvePopoverContent {
                _class "w-auto max-w-[calc(100vw-32px)]"
                // Fit the available anchor area; retain a percentage fallback where stretch is unsupported.
                _style "padding:0;max-height:min(32rem,calc(100dvh - 2rem),100%);max-height:stretch;position-try-order:most-height"
                MonthCalendarSelectionRendering.render calendar
            }
        }
        ComponentElement.render "fve-date-picker"
            [ _id (id + "-host"); _class "contents"; _lang calendar.locale
              match calendar.selection with MonthCalendarSelection.Range _ -> _attr("range", "") | _ -> () ] [] [content]

/// <summary>A date-only form control; native labels and feedback belong to the caller.</summary>
/// <category>date-picker</category>
[<AutoOpen>]
module DatePickerDsl =
    let fveDatePicker = ComponentElementBuilder(DatePicker.render)
