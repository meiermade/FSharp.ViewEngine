module DatastarTests

open FSharp.ViewEngine
open System.Text.RegularExpressions
open Expecto
open type Html
open type Datastar

[<Tests>]
let tests =
  testList "Datastar Tests" [
    test "Datastar keyed attributes should render with data- prefix" {
        let actual =
            div {
                _dataSignals ("count", "0")
                _dataOn ("click", "$count++")
                _dataShow "$count > 0"
                _dataText "$count"
                _dataBind "name"
                _dataBind ("name", "'default'")
                _dataEffect "console.log($count)"
                _dataClass ("active", "$isActive")
                _dataAttr ("disabled", "$count === 0")
                _dataComputed ("double", "$count * 2")
                _dataInit "console.log('init')"
                _dataIgnore
                _dataIgnoreMorph
                _dataStyle ("color", "red")
                _dataRef "myInput"
                _dataRef ("myInput", "'fallback'")
                _dataIndicator "loading"
                _dataIndicator ("loading", "'true'")
                _dataAnimate "fadeIn"
                _dataPersist ()
                _dataPersist "count"
                _dataPersist ("count", "{include: /count/}")
                _dataScrollIntoView
                "Content"
            } |> Render.toString
        Expect.stringContains actual "data-signals:count=\"0\"" "data-signals keyed"
        Expect.stringContains actual "data-on:click=\"$count++\"" "data-on"
        Expect.stringContains actual "data-show=\"$count > 0\"" "data-show"
        Expect.stringContains actual "data-text=\"$count\"" "data-text"
        Expect.stringContains actual "data-bind:name=\"'default'\"" "data-bind keyed with value"
        Expect.stringContains actual "data-effect=\"console.log($count)\"" "data-effect"
        Expect.stringContains actual "data-class:active=\"$isActive\"" "data-class keyed"
        Expect.stringContains actual "data-attr:disabled=\"$count === 0\"" "data-attr keyed"
        Expect.stringContains actual "data-computed:double=\"$count * 2\"" "data-computed keyed"
        Expect.stringContains actual "data-init=\"console.log('init')\"" "data-init"
        Expect.stringContains actual "data-ignore-morph" "data-ignore-morph"
        Expect.isTrue (Regex.IsMatch(actual, @"data-ignore(?!-)")) "data-ignore (not data-ignore-morph)"
        Expect.stringContains actual "data-style:color=\"red\"" "data-style keyed"
        Expect.stringContains actual "data-ref:myInput=\"'fallback'\"" "data-ref keyed with value"
        Expect.stringContains actual "data-indicator:loading=\"'true'\"" "data-indicator keyed with value"
        Expect.stringContains actual "data-animate=\"fadeIn\"" "data-animate"
        Expect.stringContains actual "data-persist:count=\"{include: /count/}\"" "data-persist keyed with value"
        Expect.isTrue (Regex.IsMatch(actual, @"data-persist(?![:=])")) "data-persist no key"
        Expect.stringContains actual "data-persist:count" "data-persist keyed"
        Expect.stringContains actual "data-scroll-into-view" "data-scroll-into-view"
    }

    test "Datastar object-syntax overloads should render without key suffix" {
        let actual =
            div {
                _dataAttr "{'aria-label': $foo, disabled: $bar}"
                _dataClass "{success: $foo != '', 'font-bold': $foo == 'strong'}"
                _dataComputed "{foo: () => $bar + $baz}"
                _dataSignals "{foo: {bar: 1, baz: 2}}"
                _dataStyle "{display: $hiding ? 'none' : 'flex', 'background-color': $red ? 'red' : 'green'}"
                "Content"
            } |> Render.toString
        Expect.stringContains actual "data-attr=\"{'aria-label': $foo, disabled: $bar}\"" "data-attr object syntax"
        Expect.stringContains actual "data-class=\"{success: $foo != '', 'font-bold': $foo == 'strong'}\"" "data-class object syntax"
        Expect.stringContains actual "data-computed=\"{foo: () => $bar + $baz}\"" "data-computed object syntax"
        Expect.stringContains actual "data-signals=\"{foo: {bar: 1, baz: 2}}\"" "data-signals object syntax"
        Expect.stringContains actual "data-style=\"{display: $hiding ? 'none' : 'flex', 'background-color': $red ? 'red' : 'green'}\"" "data-style object syntax"
    }

    test "Datastar remaining attributes should render correctly" {
        let actual =
            div {
                _dataJsonSignals ()
                _dataJsonSignals "{include: /user/}"
                _dataOnIntersect "$intersected = true"
                _dataOnInterval "$count++"
                _dataOnSignalPatch "console.log('changed')"
                _dataOnSignalPatchFilter "{include: /^counter$/}"
                _dataPreserveAttr "open class"
                _dataCustomValidity "$foo === $bar ? '' : 'Must match'"
                _dataOnRaf "$count++"
                _dataOnResize "$count++"
                _dataQueryString ()
                _dataQueryString "{include: /foo/}"
                _dataReplaceUrl "`/page${page}`"
                _dataRocket "myRocket"
                _dataViewTransition "$foo"
                "Content"
            } |> Render.toString
        Expect.isTrue (Regex.IsMatch(actual, @"data-json-signals(?!=)")) "data-json-signals no value"
        Expect.stringContains actual "data-json-signals=\"{include: /user/}\"" "data-json-signals with value"
        Expect.stringContains actual "data-on-intersect=\"$intersected = true\"" "data-on-intersect"
        Expect.stringContains actual "data-on-interval=\"$count++\"" "data-on-interval"
        Expect.stringContains actual "data-on-signal-patch=\"console.log('changed')\"" "data-on-signal-patch"
        Expect.stringContains actual "data-on-signal-patch-filter=\"{include: /^counter$/}\"" "data-on-signal-patch-filter"
        Expect.stringContains actual "data-preserve-attr=\"open class\"" "data-preserve-attr"
        Expect.stringContains actual "data-custom-validity=\"$foo === $bar ? '' : 'Must match'\"" "data-custom-validity"
        Expect.stringContains actual "data-on-raf=\"$count++\"" "data-on-raf"
        Expect.stringContains actual "data-on-resize=\"$count++\"" "data-on-resize"
        Expect.isTrue (Regex.IsMatch(actual, @"data-query-string(?!=)")) "data-query-string no value"
        Expect.stringContains actual "data-query-string=\"{include: /foo/}\"" "data-query-string with value"
        Expect.stringContains actual "data-replace-url=\"`/page${page}`\"" "data-replace-url"
        Expect.stringContains actual "data-rocket=\"myRocket\"" "data-rocket"
        Expect.stringContains actual "data-view-transition=\"$foo\"" "data-view-transition"
    }
  ]
