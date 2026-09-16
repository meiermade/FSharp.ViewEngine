namespace FSharp.ViewEngine.Components.Documentation

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open type Html

[<NoEquality; NoComparison>]
type FixtureLink = private { label:string; href:string }

[<NoEquality; NoComparison>]
type FixtureState = private { label:string; href:string; current:bool }

[<NoEquality; NoComparison>]
type FixtureConfig =
    private
        { id:string
          content:HtmlElement
          previous:FixtureLink option
          next:FixtureLink option
          states:FixtureState list }

[<RequireQualifiedAccess>]
module FixtureLink =
    let create label href =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A Fixture link label is required."
        if String.IsNullOrWhiteSpace href then invalidArg (nameof href) "A Fixture link destination is required."
        { label = label; href = href }

[<RequireQualifiedAccess>]
module FixtureState =
    let create label href =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A Fixture state label is required."
        if String.IsNullOrWhiteSpace href then invalidArg (nameof href) "A Fixture state destination is required."
        { label = label; href = href; current = false }
    let current (state:FixtureState) = { state with current = true }

[<RequireQualifiedAccess>]
module Fixture =
    let create id content =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable Fixture ID is required."
        { id = id; content = content; previous = None; next = None; states = [] }
    let withPrevious (previous:FixtureLink) (value:FixtureConfig) = { value with previous = Some previous }
    let withNext (next:FixtureLink) (value:FixtureConfig) = { value with next = Some next }
    let withStates (states:FixtureState list) (value:FixtureConfig) =
        if states |> List.filter _.current |> List.length > 1 then invalidArg (nameof states) "At most one Fixture state can be current."
        { value with states = states }

    let render value =
        let stateSelect =
            let controlId = $"fve-fixture-{value.id}-state"
            let config =
                value.states
                |> List.map (fun state -> Select.option state.href state.label)
                |> Select.create controlId "Review state" id
                |> Select.withId controlId
                |> Select.withVisuallyHiddenLabel
            value.states
            |> List.tryFind _.current
            |> Option.map (fun state -> Select.withSelected state.href config)
            |> Option.defaultValue config
            |> Select.render
        div {
            _attr ("data-fve-fixture", "true")
            _attr ("data-fve-fixture-id", value.id)
            value.content
            nav {
                _attr ("data-fve-app-mode-navigation", "true")
                _ariaLabel "Fixture review navigation"
                match value.previous with
                | Some previous -> a { _href previous.href; _attr ("data-fve-app-mode-previous", "true"); previous.label }
                | None -> ()
                if not value.states.IsEmpty then
                    div { _attr ("data-fve-app-mode-state-select", "true"); stateSelect }
                    div {
                        _attr ("data-fve-app-mode-state-destinations", "true")
                        for state in value.states do
                            a {
                                _href state.href
                                _attr ("data-fve-app-mode-state", "true")
                                if state.current then _ariaCurrent "page"
                                state.label
                            }
                    }
                match value.next with
                | Some next -> a { _href next.href; _attr ("data-fve-app-mode-next", "true"); next.label }
                | None -> ()
            }
        }
