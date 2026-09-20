namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<RequireQualifiedAccess>]
type ProgressState = Active | Complete | Failed

[<NoEquality; NoComparison>]
type ProgressConfig = private { label:string; value:int; maximum:int; valueText:string option; detail:string option; state:ProgressState }

[<RequireQualifiedAccess>]
module Progress =
    let create label value maximum =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A progress label is required."
        if maximum < 1 then invalidArg (nameof maximum) "Progress maximum must be positive."
        if value < 0 || value > maximum then invalidArg (nameof value) "Progress value must be within its range."
        { label = label; value = value; maximum = maximum; valueText = None; detail = None; state = ProgressState.Active }
    let withValueText valueText config = { config with valueText = Some valueText }
    let withDetail detail config = { config with detail = Some detail }
    let complete config = { config with state = ProgressState.Complete }
    let failed config = { config with state = ProgressState.Failed }
    let render config =
        let percent = config.value * 100 / config.maximum
        let readable = config.valueText |> Option.defaultValue (string percent + "%")
        section {
            _role "group"
            _ariaLabel config.label
            _class "grid gap-2"
            div {
                _class "flex items-baseline justify-between gap-3 text-sm"
                strong { config.label }
                span {
                    _class "text-[var(--fve-muted-text)]"
                    readable
                }
            }
            progress {
                _ariaLabel config.label
                _value (string config.value)
                _max (string config.maximum)
                _class "h-2 w-full overflow-hidden rounded-full accent-[var(--fve-brand-solid)]"
                readable
            }
            match config.detail with
            | Some detail ->
                p {
                    _class "text-sm text-[var(--fve-muted-text)]"
                    detail
                }
            | None -> ()
            match config.state with
            | ProgressState.Complete ->
                p {
                    _role "status"
                    _class "text-sm text-[var(--fve-positive-text)]"
                    "Complete"
                }
            | ProgressState.Failed ->
                p {
                    _role "alert"
                    _class "text-sm text-[var(--fve-critical-text)]"
                    "Failed"
                }
            | ProgressState.Active -> ()
        }
