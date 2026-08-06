module AlpineTests

open System
open FSharp.ViewEngine
open Expecto
open type Html
open type Alpine

let private renderAttribute (attribute: HtmlAttribute) =
    div { yield attribute }
    |> Render.toString

[<Tests>]
let tests =
    testList "Alpine Tests" [
        test "Alpine 3.15.12 core directives render correctly" {
            let valued =
                [ "x-bind:class", _xBind ("class", "open ? 'block' : 'hidden'")
                  "x-data", _xData "{ open: false }"
                  "x-effect", _xEffect "console.log(open)"
                  "x-for", _xFor "item in items"
                  "x-html", _xHtml "content"
                  "x-id", _xId "['dropdown']"
                  "x-if", _xIf "open"
                  "x-init", _xInit "open = true"
                  "x-model", _xModel "name"
                  "x-modelable", _xModelable "value"
                  "x-on:click", _xOn ("click", "open = !open")
                  "x-ref", _xRef "trigger"
                  "x-show", _xShow "open"
                  "x-teleport", _xTeleport "body"
                  "x-text", _xText "message" ]

            for name, attribute in valued do
                let actual = renderAttribute attribute
                Expect.stringStarts actual $"<div {name}=\"" name

            let presence =
                [ "x-cloak", _xCloak
                  "x-ignore", _xIgnore ()
                  "x-transition", _xTransition () ]

            for name, attribute in presence do
                Expect.equal (renderAttribute attribute) $"<div {name}></div>" name
        }

        test "Core directive modifiers and transition phases render in order" {
            let actual =
                div {
                    _xBind ("value", [ "camel" ], "value")
                    _xIgnore [ "self" ]
                    _xModel ([ "lazy"; "debounce.500ms" ], "name")
                    _xOn ("keydown", [ "enter"; "prevent"; "once" ], "save()")
                    _xShow ([ "important" ], "open")
                    _xTransition [ "duration.500ms"; "opacity" ]
                    _xTransition ("enter-start", "opacity-0 scale-90")
                }
                |> Render.toString

            Expect.stringContains actual "x-bind:value.camel=\"value\"" "x-bind modifiers"
            Expect.stringContains actual "x-ignore.self" "x-ignore modifier"
            Expect.stringContains actual "x-model.lazy.debounce.500ms=\"name\"" "x-model modifiers"
            Expect.stringContains actual "x-on:keydown.enter.prevent.once=\"save()\"" "x-on modifiers"
            Expect.stringContains actual "x-show.important=\"open\"" "x-show modifier"
            Expect.stringContains actual "x-transition.duration.500ms.opacity" "x-transition modifiers"
            Expect.stringContains actual "x-transition:enter-start=\"opacity-0 scale-90\"" "x-transition phase"
        }

        test "Official plugin directives render correctly" {
            let actual =
                div {
                    _xMask "99/99/9999"
                    _xMaskDynamic "$money($input)"
                    _xIntersect ([ "once"; "threshold.50" ], "visible = true")
                    _xIntersect ("leave", [ "full" ], "visible = false")
                    _xResize ([ "document" ], "width = $width")
                    _xCollapse ()
                    _xCollapse [ "duration.500ms"; "min.50px" ]
                    _xTrap ([ "inert"; "noscroll" ], "open")
                    _xAnchor ([ "bottom-start"; "offset.10"; "fixed" ], "$refs.trigger")
                    _xSort "handleSort($item, $position)"
                    _xSort ([ "ghost" ], "handleSort($item, $position)")
                    _xSortItem "item.id"
                    _xSortGroup "tasks"
                    _xSortConfig "{ animation: 150 }"
                    _xSortHandle
                    _xSortIgnore
                }
                |> Render.toString

            let expected =
                [ "x-mask=\"99/99/9999\""
                  "x-mask:dynamic=\"$money($input)\""
                  "x-intersect.once.threshold.50=\"visible = true\""
                  "x-intersect:leave.full=\"visible = false\""
                  "x-resize.document=\"width = $width\""
                  "x-collapse"
                  "x-collapse.duration.500ms.min.50px"
                  "x-trap.inert.noscroll=\"open\""
                  "x-anchor.bottom-start.offset.10.fixed=\"$refs.trigger\""
                  "x-sort=\"handleSort($item, $position)\""
                  "x-sort.ghost=\"handleSort($item, $position)\""
                  "x-sort:item=\"item.id\""
                  "x-sort:group=\"tasks\""
                  "x-sort:config=\"{ animation: 150 }\""
                  "x-sort:handle"
                  "x-sort:ignore" ]

            for value in expected do
                Expect.stringContains actual value value
        }

        test "Generic Alpine directives remain available and values are encoded" {
            let actual =
                div {
                    _x ("custom", "'<value>' && ready")
                    _x "custom-presence"
                }
                |> Render.toString

            Expect.stringContains actual "x-custom=\"&#39;&lt;value&gt;&#39; &amp;&amp; ready\"" "generic valued directive"
            Expect.stringContains actual "x-custom-presence" "generic presence directive"
        }

        test "Removed Alpine signatures are absent from the public API" {
            let methods = typeof<Alpine>.GetMethods()
            let hasStringPair name =
                methods
                |> Array.exists (fun methodInfo ->
                    methodInfo.Name = name
                    && (methodInfo.GetParameters() |> Array.map _.ParameterType) = [| typeof<string>; typeof<string> |])

            Expect.isFalse (methods |> Array.exists (fun methodInfo -> methodInfo.Name = "_by")) "plain by attribute is not an Alpine directive"
            Expect.isFalse (methods |> Array.exists (fun methodInfo -> methodInfo.Name = "_xOn" && methodInfo.GetParameters().Length = 1)) "x-on requires an expression"
            Expect.isFalse (hasStringPair "_xModel") "x-model modifiers use an ordered list"
            Expect.isFalse (hasStringPair "_xTrap") "x-trap modifiers use an ordered list"
            Expect.isFalse (hasStringPair "_xAnchor") "x-anchor modifiers use an ordered list"
        }
    ]
