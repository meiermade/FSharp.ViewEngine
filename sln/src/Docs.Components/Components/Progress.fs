namespace FSharp.ViewEngine.Components

open System
open System.Globalization
open FSharp.ViewEngine
open type Html

/// <category>progress</category>
[<RequireQualifiedAccess>]
type ProgressState = Active | Complete | Failed

/// <category>progress</category>
[<AutoOpen>]
module ProgressAttributes =
    type Html with
        static member _state(value:ProgressState) =
            Html._attr("state", match value with ProgressState.Active -> "active" | ProgressState.Complete -> "complete" | ProgressState.Failed -> "failed")

module internal Progress =
    let render attributes children =
        let label = ComponentElement.required "aria-label" attributes
        let number name fallback =
            match ComponentElement.attribute name attributes with
            | None -> fallback
            | Some _ ->
                match Double.TryParse(ComponentElement.required name attributes, NumberStyles.Float, CultureInfo.InvariantCulture) with
                | true, value when Double.IsFinite value -> value
                | _ -> invalidArg "attributes" $"A finite {name} is required."
        let maximum = number "max" 100.
        let value = ComponentElement.attribute "value" attributes |> Option.map (fun _ -> number "value" 0.)
        if maximum <= 0. || value |> Option.exists (fun value -> value < 0. || value > maximum) then
            invalidArg "attributes" "Progress value must be within a positive maximum."
        let state = ComponentElement.choice "state" "active" ["active"; "complete"; "failed"] attributes
        if state = "complete" && value.IsNone then invalidArg "attributes" "Indeterminate progress cannot be complete."
        let readable =
            ComponentElement.value "aria-valuetext" attributes
            |> Option.defaultWith (fun () -> value |> Option.map (fun value -> (value * 100. / maximum).ToString("0", CultureInfo.InvariantCulture) + "%") |> Option.defaultValue "In progress")
        let control =
            progress {
                _ariaLabel label
                _ariaValuetext readable
                _max (maximum.ToString(CultureInfo.InvariantCulture))
                match value with Some value -> _value (value.ToString(CultureInfo.InvariantCulture)) | None -> ()
                _class ("h-2 w-full overflow-hidden rounded-full accent-[var(--fve-brand-solid)]" + if value.IsNone then " animate-pulse motion-reduce:animate-none" else "")
                readable
            }
        ComponentElement.render "section" [_role "group"; _class "grid gap-2"] (ComponentHtml.safeAttributes ["state"; "max"; "value"; "aria-valuetext"] attributes)
            [ div { _class "flex items-baseline justify-between gap-3 text-sm"; strong { label }; span { _class "text-[var(--fve-muted-text)]"; readable } }
              control
              yield! children
              if state = "complete" then p { _role "status"; _class "text-sm text-[var(--fve-positive-text)]"; "Complete" }
              elif state = "failed" then p { _role "alert"; _class "text-sm text-[var(--fve-critical-text)]"; "Failed" } ]

/// <category>progress</category>
[<AutoOpen>]
module ProgressDsl =
    /// Native progress with typed status, ordinary value/max attributes and optional supporting content.
    let fveProgress = ComponentElementBuilder(Progress.render)
