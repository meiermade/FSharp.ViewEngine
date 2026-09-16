namespace FSharp.ViewEngine.Components.Application

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open type Html

[<RequireQualifiedAccess>]
type StepState = Complete | Current | Available | Unavailable

[<NoEquality; NoComparison>]
type Step<'destination> = private { label:string; description:string option; state:StepState; destination:'destination option }

[<RequireQualifiedAccess>]
module Step =
    let create label state =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A step label is required."
        { label = label; description = None; state = state; destination = None }
    let withDescription description (step:Step<'destination>) = { step with description = Some description }
    let withDestination destination (step:Step<'destination>) =
        if step.state = StepState.Unavailable then invalidArg (nameof step) "Unavailable steps cannot have destinations."
        { step with destination = Some destination }

[<NoEquality; NoComparison>]
type StepsConfig<'destination> = private { label:string; steps:Step<'destination> list }

[<RequireQualifiedAccess>]
module Steps =
    let create label steps =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A step navigation label is required."
        if List.isEmpty steps then invalidArg (nameof steps) "At least one step is required."
        if steps |> List.filter (fun step -> step.state = StepState.Current) |> List.length <> 1 then invalidArg (nameof steps) "Exactly one step must be current."
        { label = label; steps = steps }
    let render resolve config =
        nav {
            _ariaLabel config.label
            ol {
                _class "grid gap-2 sm:grid-cols-2 lg:grid-cols-4"
                for index, step in List.indexed config.steps do
                    li {
                        _class "min-w-0"
                        let content =
                            fragment {
                                span {
                                    _class "text-xs font-semibold text-[var(--fve-muted-text)]"
                                    "Step " + string (index + 1)
                                }
                                span {
                                    _class "block font-semibold text-[var(--fve-text)]"
                                    step.label
                                }
                                match step.description with
                                | Some description ->
                                    span {
                                        _class "block text-sm text-[var(--fve-muted-text)]"
                                        description
                                    }
                                | None -> ()
                            }
                        match step.destination, step.state with
                        | Some destination, state when state <> StepState.Current ->
                            a {
                                _href (resolve destination)
                                _class "block min-h-full rounded-[var(--fve-radius-panel)] bg-[var(--fve-surface)] p-3 ring-1 ring-[var(--fve-border)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                                content
                            }
                        | _ ->
                            div {
                                if step.state = StepState.Current then _ariaCurrent "step"
                                if step.state = StepState.Unavailable then _ariaDisabled true
                                _class (ComponentHtml.classes [
                                    "min-h-full rounded-[var(--fve-radius-panel)] p-3 ring-1"
                                    if step.state = StepState.Current then "bg-[var(--fve-brand-subtle)] ring-[var(--fve-brand-ring)]"
                                    elif step.state = StepState.Unavailable then "bg-[var(--fve-neutral-subtle)] opacity-50 ring-[var(--fve-border)]"
                                    else "bg-[var(--fve-surface)] ring-[var(--fve-border)]" ])
                                content
                            }
                    }
            }
        }
