namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html

/// <category>progress</category>
[<RequireQualifiedAccess>]
type ProgressState = Active | Complete | Failed

/// <category>progress</category>
[<RequireQualifiedAccess>]
type ProgressValue =
    | Determinate of value:int * maximum:int
    | Indeterminate

/// <category>progress</category>
[<NoEquality; NoComparison>]
type ProgressConfig = private { label:string; progress:ProgressValue; valueText:string option; detail:string option; state:ProgressState }

/// <category>progress</category>
[<RequireQualifiedAccess>]
module Progress =
    let create label value maximum =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A progress label is required."
        if maximum < 1 then invalidArg (nameof maximum) "Progress maximum must be positive."
        if value < 0 || value > maximum then invalidArg (nameof value) "Progress value must be within its range."
        { label = label; progress = ProgressValue.Determinate(value, maximum); valueText = None; detail = None; state = ProgressState.Active }
    let indeterminate label =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A progress label is required."
        { label = label; progress = ProgressValue.Indeterminate; valueText = None; detail = None; state = ProgressState.Active }
    let withValueText valueText config = { config with valueText = Some valueText }
    let withDetail detail config = { config with detail = Some detail }
    let complete config =
        match config.progress with
        | ProgressValue.Indeterminate -> invalidArg (nameof config) "Indeterminate progress cannot be marked complete without a determinate value."
        | ProgressValue.Determinate _ -> { config with state = ProgressState.Complete }
    let failed config = { config with state = ProgressState.Failed }
    let render config =
        let percent =
            match config.progress with
            | ProgressValue.Determinate(value, maximum) -> Some(value * 100 / maximum)
            | ProgressValue.Indeterminate -> None
        let readable = config.valueText |> Option.defaultValue (percent |> Option.map (fun value -> string value + "%") |> Option.defaultValue "In progress")
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
                _ariaValuetext readable
                match config.progress with
                | ProgressValue.Determinate(value, maximum) ->
                    _value (string value)
                    _max (string maximum)
                | ProgressValue.Indeterminate -> ()
                _class ("h-2 w-full overflow-hidden rounded-full accent-[var(--fve-brand-solid)]" + if percent.IsNone then " animate-pulse motion-reduce:animate-none" else "")
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
