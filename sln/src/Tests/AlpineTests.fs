module AlpineTests

open FSharp.ViewEngine
open System.Text.RegularExpressions
open Expecto
open type Html
open type Alpine

[<Tests>]
let tests =
  testList "Alpine Tests" [
    test "Alpine attributes render correctly" {
        let actual =
            div {
                _xData "{ open: false }"
                _xInit "console.log('init')"
                _xShow "open"
                _xBind ("class", "open ? 'active' : ''")
                _xOn ("click", "$dispatch('close')")
                _xText "$message"
                _xRef "container"
                _xIf "show"
                _xFor "item in items"
                _xModel "name"
                _xModel ("name", ".lazy")
                _xModelable "value"
                _xId "['dropdown']"
                _xEffect "$watch('open', val => console.log(val))"
                _xTransition ()
                _xTransition ("fade", ":enter")
                _xTrap "open"
                _xTrap ("open", ".noscroll")
                _xCloak
                _xAnchor "#trigger"
                _xAnchor ("#trigger", ".bottom")
                _xTeleport "#modals"
                _by "x.id"
                _x ("mask", "99/99/9999")
                _x "collapse"
                "Content"
            } |> Render.toString
        Expect.stringContains actual "x-data=\"{ open: false }\"" "x-data"
        Expect.stringContains actual "x-init=\"console.log('init')\"" "x-init"
        Expect.stringContains actual "x-show=\"open\"" "x-show"
        Expect.stringContains actual "x-bind:class=\"open ? 'active' : ''\"" "x-bind"
        Expect.stringContains actual "x-on:click=\"$dispatch('close')\"" "x-on with handler"
        Expect.stringContains actual "x-text=\"$message\"" "x-text"
        Expect.stringContains actual "x-ref=\"container\"" "x-ref"
        Expect.stringContains actual "x-if=\"show\"" "x-if"
        Expect.stringContains actual "x-for=\"item in items\"" "x-for"
        Expect.stringContains actual "x-model=\"name\"" "x-model"
        Expect.stringContains actual "x-model.lazy=\"name\"" "x-model with modifier"
        Expect.stringContains actual "x-modelable=\"value\"" "x-modelable"
        Expect.stringContains actual "x-id=\"['dropdown']\"" "x-id"
        Expect.stringContains actual "x-effect=\"$watch('open', val => console.log(val))\"" "x-effect"
        Expect.isTrue (Regex.IsMatch(actual, @"x-transition(?!=)(?!:)")) "x-transition bare"
        Expect.stringContains actual "x-transition:enter=\"fade\"" "x-transition with modifier and value"
        Expect.stringContains actual "x-trap=\"open\"" "x-trap"
        Expect.stringContains actual "x-trap.noscroll=\"open\"" "x-trap with modifier"
        Expect.stringContains actual "x-cloak" "x-cloak"
        Expect.stringContains actual "x-anchor=\"#trigger\"" "x-anchor"
        Expect.stringContains actual "x-anchor.bottom=\"#trigger\"" "x-anchor with modifier"
        Expect.stringContains actual "x-teleport=\"#modals\"" "x-teleport"
        Expect.stringContains actual "by=\"x.id\"" "by"
        Expect.stringContains actual "x-mask=\"99/99/9999\"" "x generic with value"
        Expect.isTrue (Regex.IsMatch(actual, @"x-collapse(?!=)")) "x generic no value"
    }
  ]
