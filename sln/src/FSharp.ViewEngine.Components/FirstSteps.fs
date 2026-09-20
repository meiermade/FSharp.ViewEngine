namespace FSharp.ViewEngine.Components.Application

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open type Html

[<RequireQualifiedAccess>]
type FirstStepState = Complete | Available

[<NoEquality; NoComparison>]
type FirstStep = private { id:string; label:string; description:string option; state:FirstStepState; action:HtmlElement option }

[<RequireQualifiedAccess>]
module FirstStep =
    let create id label =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable step ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A step label is required."
        { id = id; label = label; description = None; state = FirstStepState.Available; action = None }
    let withDescription description (step:FirstStep) = { step with description = Some description }
    let complete (step:FirstStep) = { step with state = FirstStepState.Complete }
    let withAction action (step:FirstStep) = { step with action = Some action }

[<NoEquality; NoComparison>]
type FirstStepsConfig = private { id:string; title:string; steps:FirstStep list; state:FloatingPanelState; boundary:FloatingPanelBoundary }

[<RequireQualifiedAccess>]
module FirstSteps =
    let create id title steps =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable First steps ID is required."
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A First steps title is required."
        if List.isEmpty steps then invalidArg (nameof steps) "At least one setup step is required."
        { id = id; title = title; steps = steps; state = FloatingPanelState.Open; boundary = FloatingPanelBoundary.Viewport }

    let withinContainer (config:FirstStepsConfig) = { config with boundary = FloatingPanelBoundary.Container }
    let minimized (config:FirstStepsConfig) = { config with state = FloatingPanelState.Minimized }
    let dismissed (config:FirstStepsConfig) = { config with state = FloatingPanelState.Dismissed }

    let render config =
        let body =
            ul {
                _class "grid gap-4"
                for index, step in List.indexed config.steps do
                    li {
                        _class "grid gap-2"
                        div {
                            _class "flex min-w-0 items-start gap-2"
                            span {
                                _ariaHidden "true"
                                _class (if step.state = FirstStepState.Complete then "mt-0.5 inline-flex size-5 shrink-0 items-center justify-center rounded-full bg-[var(--fve-positive-subtle)] text-xs font-semibold text-[var(--fve-positive-text)]" else "mt-0.5 inline-flex size-5 shrink-0 items-center justify-center rounded-full bg-[var(--fve-neutral-subtle)] text-xs font-semibold text-[var(--fve-muted-text)]")
                                if step.state = FirstStepState.Complete then
                                    raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-4"><path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 0 1 .143 1.051l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.479-9.816a.75.75 0 0 1 1.051-.143Z" clip-rule="evenodd"/></svg>"""
                                else string (index + 1)
                            }
                            div {
                                _class "min-w-0"
                                p {
                                    _class "font-medium"
                                    if step.state = FirstStepState.Complete then
                                        span {
                                            _class "sr-only"
                                            "Complete: "
                                        }
                                    step.label
                                }
                                match step.description with
                                | Some description -> p { _class "mt-1 text-sm text-[var(--fve-muted-text)]"; description }
                                | None -> ()
                            }
                        }
                        match step.action with
                        | Some action -> div { _class "ml-7"; action }
                        | None -> ()
                    }
            }
        FloatingPanel.create config.id config.title body
        |> FloatingPanel.withDescription "Complete these optional setup steps when ready."
        |> (if config.boundary = FloatingPanelBoundary.Container then FloatingPanel.withinContainer else id)
        |> (match config.state with
            | FloatingPanelState.Open -> id
            | FloatingPanelState.Minimized -> FloatingPanel.minimized
            | FloatingPanelState.Dismissed -> FloatingPanel.dismissed)
        |> FloatingPanel.render
