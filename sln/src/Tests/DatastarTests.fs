module DatastarTests

open System
open FSharp.ViewEngine
open Expecto
open type Html
open type Datastar

let private renderAttribute (attribute: HtmlAttribute) =
    div { yield attribute }
    |> Render.toString

[<Tests>]
let tests =
    testList "Datastar Tests" [
        test "Datastar 1.0.2 stable attributes render correctly" {
            let valuedAttributes =
                [ "data-animate:opacity", _dataAnimate ("opacity", "$visible ? 1 : 0")
                  "data-attr", _dataAttr "{disabled: $loading}"
                  "data-class", _dataClass "{active: $active}"
                  "data-computed", _dataComputed "{double: () => $count * 2}"
                  "data-custom-validity", _dataCustomValidity "$valid ? '' : 'Invalid'"
                  "data-effect", _dataEffect "console.log($count)"
                  "data-init", _dataInit "$ready = true"
                  "data-match-media:is-dark", _dataMatchMedia ("is-dark", "'prefers-color-scheme: dark'")
                  "data-on:click", _dataOn ("click", "$count++")
                  "data-on-intersect", _dataOnIntersect "$visible = true"
                  "data-on-interval", _dataOnInterval "$count++"
                  "data-on-raf", _dataOnRaf "$frames++"
                  "data-on-resize", _dataOnResize "$width = el.offsetWidth"
                  "data-on-signal-patch", _dataOnSignalPatch "console.log(patch)"
                  "data-on-signal-patch-filter", _dataOnSignalPatchFilter "{include: /^count$/}"
                  "data-preserve-attr", _dataPreserveAttr "open class"
                  "data-query-string", _dataQueryString "{include: /search/}"
                  "data-replace-url", _dataReplaceUrl "`/page${$page}`"
                  "data-show", _dataShow "$visible"
                  "data-signals", _dataSignals "{count: 0}"
                  "data-style", _dataStyle "{display: $visible ? 'block' : 'none'}"
                  "data-text", _dataText "$count"
                  "data-view-transition", _dataViewTransition "$transitionName" ]

            for name, attribute in valuedAttributes do
                let actual = renderAttribute attribute
                Expect.stringStarts actual $"<div {name}=\"" name
        }

        test "Datastar 1.0.2 presence-only attributes render correctly" {
            let attributes =
                [ "data-bind:name", _dataBind "name"
                  "data-ignore", _dataIgnore ()
                  "data-ignore-morph", _dataIgnoreMorph
                  "data-indicator:loading", _dataIndicator "loading"
                  "data-json-signals", _dataJsonSignals ()
                  "data-persist", _dataPersist ()
                  "data-ref:element", _dataRef "element"
                  "data-scroll-into-view", _dataScrollIntoView () ]

            for name, attribute in attributes do
                let actual = renderAttribute attribute
                Expect.equal actual $"<div {name}></div>" name
        }

        test "Keyed and object syntax remain available" {
            let actual =
                div {
                    _dataAttr ("aria-label", "$label")
                    _dataClass ("font-bold", "$strong")
                    _dataComputed ("double", "$count * 2")
                    _dataSignals ("count", "0")
                    _dataStyle ("background-color", "$color")
                }
                |> Render.toString

            Expect.stringContains actual "data-attr:aria-label=\"$label\"" "keyed attribute"
            Expect.stringContains actual "data-class:font-bold=\"$strong\"" "keyed class"
            Expect.stringContains actual "data-computed:double=\"$count * 2\"" "keyed computed"
            Expect.stringContains actual "data-signals:count=\"0\"" "keyed signal"
            Expect.stringContains actual "data-style:background-color=\"$color\"" "keyed style"
        }

        test "Modifiers compose onto keyed, unkeyed, and no-value attributes in order" {
            let actual =
                div {
                    _dataBind ("query", [ "prop.value"; "event.input.change" ])
                    _dataClass ("my-class", [ "case.camel" ], "$active")
                    _dataComputed ("my-signal", [ "case.kebab" ], "$count * 2")
                    _dataIgnore [ "self" ]
                    _dataIndicator ("loading-state", [ "case.kebab" ])
                    _dataInit ([ "delay.500ms"; "viewtransition" ], "$ready = true")
                    _dataJsonSignals ([ "terse" ], "{include: /count/}")
                    _dataOn ("input", [ "window"; "debounce.200ms.leading"; "prevent" ], "@get('/search')")
                    _dataOnIntersect ([ "once"; "threshold.25" ], "$visible = true")
                    _dataOnInterval ([ "duration.500ms.leading" ], "$count++")
                    _dataOnSignalPatch ([ "debounce.500ms" ], "console.log(patch)")
                    _dataRef ("my-element", [ "case.kebab" ])
                    _dataSignals ("count", [ "ifmissing" ], "0")
                    _dataMatchMedia ("is-dark", [ "case.kebab" ], "'prefers-color-scheme: dark'")
                    _dataOnRaf ([ "throttle.10ms" ], "$frames++")
                    _dataOnResize ([ "debounce.10ms" ], "$width = el.offsetWidth")
                    _dataPersist [ "session" ]
                    _dataPersist ("settings", [ "session" ])
                    _dataPersistFilter ([ "session" ], "{include: /theme/}")
                    _dataQueryString ([ "filter"; "history" ], "{include: /search/}")
                    _dataScrollIntoView [ "smooth"; "vcenter"; "focus" ]
                }
                |> Render.toString

            let expectedNames =
                [ "data-bind:query__prop.value__event.input.change"
                  "data-class:my-class__case.camel"
                  "data-computed:my-signal__case.kebab"
                  "data-ignore__self"
                  "data-indicator:loading-state__case.kebab"
                  "data-init__delay.500ms__viewtransition"
                  "data-json-signals__terse"
                  "data-on:input__window__debounce.200ms.leading__prevent"
                  "data-on-intersect__once__threshold.25"
                  "data-on-interval__duration.500ms.leading"
                  "data-on-signal-patch__debounce.500ms"
                  "data-ref:my-element__case.kebab"
                  "data-signals:count__ifmissing"
                  "data-match-media:is-dark__case.kebab"
                  "data-on-raf__throttle.10ms"
                  "data-on-resize__debounce.10ms"
                  "data-persist__session"
                  "data-persist:settings__session"
                  "data-query-string__filter__history"
                  "data-scroll-into-view__smooth__vcenter__focus" ]

            for name in expectedNames do
                Expect.stringContains actual name name

            Expect.stringContains actual "data-persist__session=\"{include: /theme/}\"" "default-key persist filter"
        }

        test "Removed Datastar overloads are absent from the public API" {
            let methods = typeof<Datastar>.GetMethods()
            let hasStringPair name =
                methods
                |> Array.exists (fun methodInfo ->
                    methodInfo.Name = name
                    && (methodInfo.GetParameters() |> Array.map _.ParameterType) = [| typeof<string>; typeof<string> |])

            Expect.isFalse (hasStringPair "_dataBind") "keyed data-bind values are invalid"
            Expect.isFalse (hasStringPair "_dataIndicator") "keyed data-indicator values are invalid"
            Expect.isFalse (hasStringPair "_dataRef") "keyed data-ref values are invalid"
            Expect.isFalse (methods |> Array.exists (fun methodInfo -> methodInfo.Name = "_dataRocket")) "data-rocket was removed"

            let animateParameterCounts =
                methods
                |> Array.filter (fun methodInfo -> methodInfo.Name = "_dataAnimate")
                |> Array.map (fun methodInfo -> methodInfo.GetParameters().Length)
                |> Array.sort

            Expect.sequenceEqual animateParameterCounts [| 2 |] "data-animate requires a key and expression"
        }

        test "Datastar values are HTML encoded" {
            let actual = div { _dataOn ("click", "'<value>' && $ready") } |> Render.toString
            Expect.equal actual "<div data-on:click=\"&#39;&lt;value&gt;&#39; &amp;&amp; $ready\"></div>" "encoded expression"
        }
    ]
