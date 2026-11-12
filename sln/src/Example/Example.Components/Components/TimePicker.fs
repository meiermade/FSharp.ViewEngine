namespace Example.Components

open System
open System.Globalization
open FSharp.ViewEngine
open type Html

/// <category>time-picker</category>
[<RequireQualifiedAccess>]
type TimeFormat = TwelveHour | TwentyFourHour

/// <category>time-picker</category>
[<AutoOpen>]
module TimePickerAttributes =
    let private clock (value:TimeOnly) =
        if value.Ticks % TimeSpan.TicksPerMinute <> 0L then invalidArg "value" "TimePicker values must use whole minutes."
        value.ToString("HH:mm", CultureInfo.InvariantCulture)
    type Html with
        static member _value(value:TimeOnly) = Html._attr("value", clock value)
        static member _min(value:TimeOnly) = Html._attr("min", clock value)
        static member _max(value:TimeOnly) = Html._attr("max", clock value)
        /// The number of minutes between choices, relative to the minimum time.
        static member _interval(minutes:int) = Html._attr("minute-interval", string minutes)
        static member _format(value:TimeFormat) = Html._attr("time-format", match value with TimeFormat.TwelveHour -> "12" | TimeFormat.TwentyFourHour -> "24")

module internal TimePicker =
    let private time name fallback attributes =
        match ComponentElement.value name attributes with
        | None -> fallback
        | Some value ->
            match TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None) with
            | true, parsed -> parsed.Hour * 60 + parsed.Minute
            | _ -> invalidArg name "A time must use HH:mm with whole minutes."
    let private clock minutes = TimeOnly(minutes / 60, minutes % 60).ToString("HH:mm", CultureInfo.InvariantCulture)
    let private display format minutes = TimeOnly(minutes / 60, minutes % 60).ToString((if format = "12" then "h:mm tt" else "HH:mm"), CultureInfo.InvariantCulture)

    let render attributes children =
        ComponentContent.noChildren "fveTimePicker" children
        let attributes = ComponentElement.attributes attributes
        let id = ComponentElement.required "id" attributes |> TextField.stableId
        let name = ComponentElement.required "name" attributes
        let minimum = time "min" 0 attributes
        let maximum = time "max" 1439 attributes
        let initial = ComponentElement.value "value" attributes |> Option.filter ((<>) "") |> Option.map (fun _ -> time "value" 0 attributes)
        let interval =
            match ComponentElement.value "minute-interval" attributes with
            | None -> 30
            | Some value -> match Int32.TryParse value with true, value when value > 0 && value <= 1440 -> value | _ -> invalidArg "minute-interval" "The interval must be 1–1440 minutes."
        if minimum > maximum then invalidArg "min" "TimePicker bounds must be ordered within one day."
        if initial |> Option.exists (fun value -> value < minimum || value > maximum || (value - minimum) % interval <> 0) then
            invalidArg "value" "The initial time must fit the bounds and interval."
        let format = ComponentElement.choice "time-format" "12" ["12"; "24"] attributes
        let disabled = TextField.flag "disabled" attributes
        let readOnly = TextField.flag "readonly" attributes
        let required = TextField.flag "required" attributes && not readOnly
        let label = ComponentElement.value "aria-label" attributes |> Option.defaultValue "Time"
        let value = initial |> Option.map clock |> Option.defaultValue ""
        let settings = {| initial = value; format = format; disabled = disabled; readOnly = readOnly |}
        let nativeAttributes =
            ComponentHtml.safeAttributes ["id"; "name"; "type"; "value"; "min"; "max"; "step"; "minute-interval"; "time-format"; "class"; "size"; "required"; "disabled"; "readonly"] attributes
            |> List.filter (fun attribute -> not (attribute.Name.StartsWith("data-", StringComparison.OrdinalIgnoreCase)))
        (Html.el "fve-time-picker") {
            _id (id + "-field")
            _class (ComponentHtml.classes ["block min-w-0"; ComponentHtml.controlSizeClass (ComponentElement.size None attributes); ComponentContent.classes attributes])
            _attr("settings", System.Text.Json.JsonSerializer.Serialize settings)
            for attribute in attributes do
                if attribute.Name.StartsWith("data-", StringComparison.OrdinalIgnoreCase) then attribute
            fveInput {
                _id id; _type "time"; _name name; _value value
                _min (clock minimum); _max (clock maximum); _step (string (interval * 60))
                _required required; _disabled disabled; _readonly readOnly
                _data("time-value", "")
                yield! nativeAttributes
            }
            div {
                _hidden true
                _data("time-enhanced", "")
                fveInputGroup {
                    _class "gap-x-1! px-2! [&>[data-fve-input-control]]:flex-[1_1_0%]!"
                    fveInput {
                        _id (id + "-text"); _type "text"
                        _value (initial |> Option.map (display format) |> Option.defaultValue "")
                        _placeholder (if format = "12" then "h:mm AM" else "HH:mm")
                        _autocomplete "off"; _role "combobox"
                        _attr("aria-autocomplete", "list")
                        _ariaHaspopup "listbox"; _ariaExpanded false; _ariaControls (id + "-options")
                        _required required; _disabled true; _readonly readOnly
                        _data("time-input", "")
                        yield! ComponentHtml.safeAttributes ["aria-label"; "aria-labelledby"] nativeAttributes
                    }
                    fveInputGroupAddon {
                        _position InputGroupPosition.InlineEnd
                        fvePopover {
                            _id (id + "-times")
                            _ariaLabel (label + " options")
                            _alignment PopoverAlignment.End
                            fvePopoverTrigger {
                                fun (forwarded:HtmlAttribute list) -> button {
                                    for attribute in forwarded do attribute
                                    _ariaLabel ("Choose " + label.ToLowerInvariant())
                                    _disabled (disabled || readOnly)
                                    _class "grid size-[calc(var(--fve-control-min-height)-2px)] shrink-0 place-items-center rounded-[var(--fve-radius-control)] text-[var(--fve-muted-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"
                                    _data("time-toggle", "")
                                    raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-[var(--fve-control-icon-size)]" aria-hidden="true"><path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm.75-12.5a.75.75 0 0 0-1.5 0v4.75c0 .199.079.39.22.53l2.5 2.5a.75.75 0 1 0 1.06-1.06l-2.28-2.28V5.5Z" clip-rule="evenodd"/></svg>"""
                                }
                            }
                            fvePopoverContent {
                                _style "width:min(16rem,calc(100vw - 2rem));max-width:100%;max-height:min(18rem,calc(100dvh - 2rem));padding:0.25rem"
                                div {
                                    _id (id + "-options"); _role "listbox"; _ariaLabel label
                                    for minutes in 0 .. 1439 do
                                        if (minutes - minimum) % interval = 0 then
                                            button {
                                                _id (id + "-option-" + string minutes)
                                                _type "button"; _role "option"; _tabindex -1
                                                _hidden (minutes < minimum || minutes > maximum)
                                                _ariaSelected (Option.contains minutes initial)
                                                _data("time-option", clock minutes)
                                                _class "block min-h-8 w-full rounded-[var(--fve-radius-control)] px-2 py-1 text-left text-sm text-[var(--fve-text)] hover:bg-[var(--fve-brand-subtle)] aria-selected:bg-[var(--fve-brand-subtle)] aria-selected:font-semibold data-[active=true]:outline-2 data-[active=true]:-outline-offset-2 data-[active=true]:outline-[var(--fve-brand-ring)]"
                                                display format minutes
                                            }
                                }
                            }
                        }
                    }
                }
            }
        }

/// <summary>A local, minute-resolution time field. Import Assets/time-picker.js once. Native time input is the no-JavaScript fallback.</summary>
/// <category>time-picker</category>
[<AutoOpen>]
module TimePickerDsl =
    let fveTimePicker = ComponentElementBuilder(TimePicker.render)
