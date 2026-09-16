namespace FSharp.ViewEngine.Components.Application

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open type Html
open type Datastar

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
type FirstStepsConfig = private { id:string; title:string; steps:FirstStep list }

[<RequireQualifiedAccess>]
module FirstSteps =
    let create id title steps =
        if String.IsNullOrWhiteSpace id || id |> Seq.exists Char.IsWhiteSpace then invalidArg (nameof id) "A stable First steps ID is required."
        if String.IsNullOrWhiteSpace title then invalidArg (nameof title) "A First steps title is required."
        if List.isEmpty steps then invalidArg (nameof steps) "At least one setup step is required."
        { id = id; title = title; steps = steps }

    let render config =
        let signalName = ComponentHtml.signalToken (config.id + "-hidden")
        let hidden = "$" + signalName
        div {
            _dataSignals ("{" + signalName + ": false}")
            button {
                _type "button"
                _class "text-sm font-semibold text-[var(--fve-brand-text)] underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                _dataShow hidden
                _style "display:none"
                _dataOn ("click", hidden + " = false")
                "Restore first steps"
            }
            section {
                _id config.id
                _ariaLabel config.title
                _class "grid gap-3 rounded-[var(--fve-radius-panel)] bg-[var(--fve-neutral-subtle)] p-4 ring-1 ring-[var(--fve-border)]"
                _dataShow ("!" + hidden)
                header {
                    _class "flex items-start justify-between gap-3"
                    div {
                        h2 { _class "text-base font-semibold"; config.title }
                        p {
                            _class "text-sm text-[var(--fve-muted-text)]"
                            "Complete these optional setup steps when ready."
                        }
                    }
                    button {
                        _type "button"
                        _ariaLabel "Minimize first steps"
                        _class "rounded-[var(--fve-radius-control)] px-2 py-1 text-sm hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"
                        _dataOn ("click", hidden + " = true")
                        "Minimize"
                    }
                }
                ul {
                    _class "grid gap-3"
                    for step in config.steps do
                        li {
                            _class "flex flex-wrap items-start justify-between gap-3"
                            div {
                                _class "min-w-0"
                                p {
                                    _class "font-medium"
                                    if step.state = FirstStepState.Complete then "Complete: "
                                    step.label
                                }
                                match step.description with
                                | Some description ->
                                    p {
                                        _class "text-sm text-[var(--fve-muted-text)]"
                                        description
                                    }
                                | None -> ()
                            }
                            match step.action with
                            | Some action -> action
                            | None -> ()
                        }
                }
            }
        }
