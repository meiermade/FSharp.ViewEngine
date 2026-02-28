module SvgTests

open FSharp.ViewEngine
open Expecto
open type Html
open type Svg

[<Tests>]
let tests =
  testList "SVG Tests" [
    test "SVG elements and attributes render correctly" {
        let actual =
            svg {
                _viewBox "0 0 100 100"
                _fill "none"
                _stroke "black"
                _strokeWidth 2
                _strokeLinecap "round"
                _strokeLinejoin "miter"
                Svg._width 24
                Svg._height 24
                circle {
                    _cx 50
                    _cy 50
                    _r 40
                    _fill "red"
                }
                path {
                    _d "M10 10"
                    _fillRule "evenodd"
                    _clipRule "evenodd"
                }
            } |> Render.toString
        Expect.stringContains actual "<svg" "svg open"
        Expect.stringContains actual "viewBox=\"0 0 100 100\"" "viewBox"
        Expect.stringContains actual "fill=\"none\"" "fill on svg"
        Expect.stringContains actual "stroke=\"black\"" "stroke"
        Expect.stringContains actual "stroke-width=\"2\"" "stroke-width"
        Expect.stringContains actual "stroke-linecap=\"round\"" "stroke-linecap"
        Expect.stringContains actual "stroke-linejoin=\"miter\"" "stroke-linejoin"
        Expect.stringContains actual "width=\"24\"" "svg width"
        Expect.stringContains actual "height=\"24\"" "svg height"
        Expect.stringContains actual "<circle" "circle element"
        Expect.stringContains actual "cx=\"50\"" "cx"
        Expect.stringContains actual "cy=\"50\"" "cy"
        Expect.stringContains actual "r=\"40\"" "r"
        Expect.stringContains actual "fill=\"red\"" "fill on circle"
        Expect.stringContains actual "d=\"M10 10\"" "d"
        Expect.stringContains actual "fill-rule=\"evenodd\"" "fill-rule"
        Expect.stringContains actual "clip-rule=\"evenodd\"" "clip-rule"
    }
  ]
