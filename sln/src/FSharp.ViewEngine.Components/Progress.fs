namespace FSharp.ViewEngine.Components.Primitives

open System
open FSharp.ViewEngine
open type Html

[<NoEquality; NoComparison>]
type ProgressConfig = private { label:string; value:int; maximum:int; detail:string option; status:string option }

[<RequireQualifiedAccess>]
module Progress =
    let create label value maximum =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A progress label is required."
        if maximum < 1 then invalidArg (nameof maximum) "Progress maximum must be positive."
        if value < 0 || value > maximum then invalidArg (nameof value) "Progress value must be within its range."
        { label = label; value = value; maximum = maximum; detail = None; status = None }
    let withDetail detail config = { config with detail = Some detail }
    let withStatus status config = { config with status = Some status }
    let render config =
        let percent = config.value * 100 / config.maximum
        section {
            _role "group"
            _ariaLabel config.label
            _class "grid gap-2"
            div { _class "flex items-baseline justify-between gap-3 text-sm"; strong { config.label }; span { _class "text-[var(--fve-muted-text)]"; $"{config.value} of {config.maximum} (" + string percent + "%)" } }
            progress { _value (string config.value); _max (string config.maximum); _class "h-2 w-full overflow-hidden rounded-full accent-[var(--fve-brand-solid)]"; string percent + "%" }
            match config.detail with Some detail -> p { _class "text-sm text-[var(--fve-muted-text)]"; detail } | None -> ()
            match config.status with Some status -> p { _role "status"; _ariaLive "polite"; _class "text-sm text-[var(--fve-muted-text)]"; status } | None -> ()
        }
