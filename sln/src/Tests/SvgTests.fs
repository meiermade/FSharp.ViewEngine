module SvgTests

open System.Globalization
open FSharp.ViewEngine
open Expecto
open type Html
open type Svg

let private expectedElements =
    [
        "circle", "circle"
        "clipPath", "clipPath"
        "defs", "defs"
        "desc", "desc"
        "ellipse", "ellipse"
        "g", "g"
        "line", "line"
        "linearGradient", "linearGradient"
        "mask", "mask"
        "path", "path"
        "polygon", "polygon"
        "polyline", "polyline"
        "radialGradient", "radialGradient"
        "rect", "rect"
        "stop", "stop"
        "svg", "svg"
        "symbol", "symbol"
        "textElement", "text"
        "titleElement", "title"
        "tspan", "tspan"
        "useElement", "use"
    ]

let private expectedAttributes =
    set [
        "_clipPath"; "_clipPathUnits"; "_clipRule"; "_cx"; "_cy"; "_d"
        "_dominantBaseline"; "_dx"; "_dy"; "_fill"; "_fillOpacity"; "_fillRule"
        "_fontFamily"; "_fontSize"; "_fontWeight"; "_fr"; "_fx"; "_fy"
        "_gradientTransform"; "_gradientUnits"; "_height"; "_lengthAdjust"; "_mask"
        "_maskContentUnits"; "_maskUnits"; "_offset"; "_opacity"; "_pathLength"
        "_points"; "_preserveAspectRatio"; "_r"; "_rx"; "_ry"; "_spreadMethod"
        "_stopColor"; "_stopOpacity"; "_stroke"; "_strokeDasharray"
        "_strokeDashoffset"; "_strokeLinecap"; "_strokeLinejoin"; "_strokeMiterlimit"
        "_strokeOpacity"; "_strokeWidth"; "_textAnchor"; "_textLength"; "_transform"
        "_vectorEffect"; "_viewBox"; "_width"; "_x"; "_x1"; "_x2"; "_xmlns"
        "_y"; "_y1"; "_y2"
    ]

[<Tests>]
let tests =
    testList "SVG Tests" [
        test "Svg exposes the documented production element inventory" {
            let actual =
                typeof<Svg>.GetProperties()
                |> Array.map _.Name
                |> Set.ofArray

            let expected = expectedElements |> List.map fst |> Set.ofList
            Expect.equal actual expected "21 supported SVG elements"
        }

        test "Svg exposes the documented production attribute inventory" {
            let actual =
                typeof<Svg>.GetMethods()
                |> Array.map _.Name
                |> Array.filter _.StartsWith("_")
                |> Set.ofArray

            Expect.equal actual expectedAttributes "supported SVG-specific attributes"
        }

        test "Every supported SVG element renders with the correct case-sensitive name" {
            let actual =
                svg {
                    circle { }
                    clipPath { }
                    defs { }
                    desc { "Description" }
                    ellipse { }
                    g { }
                    line { }
                    linearGradient { }
                    mask { }
                    path { }
                    polygon { }
                    polyline { }
                    radialGradient { }
                    rect { }
                    stop { }
                    symbol { }
                    textElement { "Text" }
                    titleElement { "Title" }
                    tspan { "Span" }
                    useElement { }
                }
                |> Render.toString

            for helper, tag in expectedElements do
                Expect.stringContains actual $"<{tag}" helper
                Expect.stringContains actual $"</{tag}>" helper
        }

        test "Icon attributes render correctly" {
            let actual =
                svg {
                    _viewBox "0 0 24 24"
                    Svg._width 24
                    Svg._height 24
                    _preserveAspectRatio "xMidYMid meet"
                    _fill "none"
                    _stroke "currentColor"
                    _strokeWidth 1.5
                    _strokeLinecap "round"
                    _strokeLinejoin "round"
                    _vectorEffect "non-scaling-stroke"
                    path {
                        _d "M5 13l4 4L19 7"
                        _fillRule "evenodd"
                        _clipRule "evenodd"
                        _pathLength 1.0
                    }
                }
                |> Render.toString

            Expect.stringContains actual "viewBox=\"0 0 24 24\"" "viewBox casing"
            Expect.stringContains actual "preserveAspectRatio=\"xMidYMid meet\"" "aspect ratio"
            Expect.stringContains actual "stroke-width=\"1.5\"" "fractional stroke"
            Expect.stringContains actual "vector-effect=\"non-scaling-stroke\"" "vector effect"
            Expect.stringContains actual "pathLength=\"1\"" "normalized path length"
        }

        test "Chart geometry and text render correctly" {
            let actual =
                svg {
                    _viewBox "0 0 400 200"
                    g {
                        _transform "translate(40 10)"
                        _opacity 0.9
                        rect { _x 0; _y 0; Svg._width 320; Svg._height 160; _rx 4; _ry 4 }
                        line { _x1 0; _y1 160; _x2 320; _y2 160 }
                        polyline { _points "0,140 80,100 160,120 240,40 320,20" }
                        polygon { _points "0,160 80,120 160,140 160,160" }
                        ellipse { _cx 240; _cy 40; _rx 6; _ry 4 }
                        textElement {
                            _x 160
                            _y 190
                            _dx "0.5em"
                            _dy 2
                            _textAnchor "middle"
                            _dominantBaseline "middle"
                            _fontFamily "sans-serif"
                            _fontSize "12px"
                            _fontWeight "600"
                            _textLength "80"
                            _lengthAdjust "spacingAndGlyphs"
                            tspan { "Revenue" }
                        }
                    }
                }
                |> Render.toString

            Expect.stringContains actual "transform=\"translate(40 10)\"" "group transform"
            Expect.stringContains actual "points=\"0,140 80,100 160,120 240,40 320,20\"" "polyline points"
            Expect.stringContains actual "text-anchor=\"middle\"" "text anchor"
            Expect.stringContains actual "lengthAdjust=\"spacingAndGlyphs\"" "text length adjustment"
        }

        test "Gradients clipping masking and reuse render correctly" {
            let actual =
                svg {
                    _xmlns "http://www.w3.org/2000/svg"
                    defs {
                        linearGradient {
                            _id "brand-gradient"
                            _x1 "0%"
                            _y1 "0%"
                            _x2 "100%"
                            _y2 "0%"
                            _gradientUnits "objectBoundingBox"
                            _gradientTransform "rotate(10)"
                            _spreadMethod "pad"
                            stop { _offset "0%"; _stopColor "#0ea5e9"; _stopOpacity 1.0 }
                            stop { _offset "100%"; _stopColor "#6366f1"; _stopOpacity 0.75 }
                        }
                        radialGradient {
                            _id "spotlight"
                            _cx "50%"
                            _cy "50%"
                            _r "50%"
                            _fx "45%"
                            _fy "45%"
                            _fr "5%"
                        }
                        clipPath { _id "plot-clip"; _clipPathUnits "userSpaceOnUse"; rect { Svg._width 100; Svg._height 50 } }
                        mask { _id "fade-mask"; _maskUnits "userSpaceOnUse"; _maskContentUnits "userSpaceOnUse" }
                        symbol { _id "dot"; _viewBox "0 0 10 10"; circle { _cx 5; _cy 5; _r 5 } }
                    }
                    g {
                        _fill "url(#brand-gradient)"
                        _fillOpacity 0.8
                        _strokeOpacity "60%"
                        _strokeMiterlimit 4.0
                        _strokeDasharray "4 2"
                        _strokeDashoffset 1.5
                        _clipPath "url(#plot-clip)"
                        _mask "url(#fade-mask)"
                        useElement { _href "#dot"; _x 10; _y 20 }
                    }
                }
                |> Render.toString

            Expect.stringContains actual "xmlns=\"http://www.w3.org/2000/svg\"" "SVG namespace"
            Expect.stringContains actual "gradientUnits=\"objectBoundingBox\"" "gradient units casing"
            Expect.stringContains actual "stop-opacity=\"0.75\"" "string stop opacity"
            Expect.stringContains actual "clip-path=\"url(#plot-clip)\"" "clip path reference"
            Expect.stringContains actual "href=\"#dot\"" "modern unnamespaced href"
            Expect.isFalse (actual.Contains("xlink:href")) "deprecated XLink is not emitted"
        }

        test "Informative and decorative SVG accessibility patterns render correctly" {
            let informative =
                svg {
                    _role "img"
                    _ariaLabelledby "sales-title sales-description"
                    titleElement { _id "sales-title"; "Quarterly sales" }
                    desc { _id "sales-description"; "Sales increased each quarter." }
                }
                |> Render.toString

            let decorative = svg { _ariaHidden true; path { _d "M0 0" } } |> Render.toString

            Expect.stringContains informative "role=\"img\"" "image role"
            Expect.stringContains informative "aria-labelledby=\"sales-title sales-description\"" "accessible references"
            Expect.stringContains informative "<title id=\"sales-title\">Quarterly sales</title>" "accessible title"
            Expect.stringContains informative "<desc id=\"sales-description\">Sales increased each quarter.</desc>" "accessible description"
            Expect.stringContains decorative "aria-hidden=\"true\"" "decorative SVG"
        }

        test "SVG numeric values use invariant formatting" {
            let previousCulture = CultureInfo.CurrentCulture
            try
                CultureInfo.CurrentCulture <- CultureInfo.GetCultureInfo("fr-FR")
                let actual =
                    circle {
                        _cx 1.5
                        _cy 2.25
                        _r 3.75
                    }
                    |> Render.toString

                Expect.equal actual "<circle cx=\"1.5\" cy=\"2.25\" r=\"3.75\"></circle>" "invariant decimal separator"
            finally
                CultureInfo.CurrentCulture <- previousCulture
        }
    ]
