namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open type Html
open type FSharp.ViewEngine.Svg

module Svg =
    let private iconExampleSource = """let checkIcon =
    svg {
        _viewBox "0 0 24 24"
        _preserveAspectRatio "xMidYMid meet"
        Svg._width 24
        Svg._height 24
        _fill "none"
        _stroke "currentColor"
        _strokeWidth 1.5
        _strokeLinecap "round"
        _strokeLinejoin "round"
        _vectorEffect "non-scaling-stroke"
        path {
            _d "M5 13l4 4L19 7"
            _pathLength 1.0
        }
    }"""

    let private previewSurface (content:HtmlElement) =
        div {
            _data("example-surface", "true")
            _style "display:grid;min-height:16rem;place-items:center;border-radius:.65rem;background:#f8fafc;padding:1.5rem;color:#0f172a"
            content
        }

    let private iconExamplePreview =
        previewSurface (
            svg {
                _viewBox "0 0 24 24"
                Svg._width 72
                Svg._height 72
                _fill "none"
                _stroke "currentColor"
                _strokeWidth 1.5
                _strokeLinecap "round"
                _strokeLinejoin "round"
                path { _d "M5 13l4 4L19 7" }
            })

    let private chartExampleSource = """svg {
    _viewBox "0 0 400 200"
    _role "img"
    _ariaLabel "Revenue trend rising across five periods"
    g {
        _transform "translate(40 10)"
        rect {
            _x 0; _y 0; Svg._width 320; Svg._height 160; _rx 8
            _fill "#ffffff"; _stroke "#cbd5e1"
        }
        polygon {
            _points "0,140 80,100 160,120 240,40 320,20 320,160 0,160"
            _fill "#10b981"; _fillOpacity 0.14
        }
        line { _x1 0; _y1 160; _x2 320; _y2 160; _stroke "#94a3b8" }
        polyline {
            _points "0,140 80,100 160,120 240,40 320,20"
            _fill "none"; _stroke "#059669"; _strokeWidth 4
            _strokeLinecap "round"; _strokeLinejoin "round"
        }
        ellipse { _cx 240; _cy 40; _rx 6; _ry 6; _fill "#047857" }
        textElement {
            _x 160; _y 184; _textAnchor "middle"
            _fontFamily "sans-serif"; _fontSize "12px"; _fill "#475569"
            tspan { "Revenue" }
        }
    }
}"""

    let private chartExamplePreview =
        previewSurface (
            svg {
                _viewBox "0 0 400 200"
                _role "img"
                _ariaLabel "Revenue trend rising across five periods"
                _style "width:100%;max-width:40rem;height:auto"
                g {
                    _transform "translate(40 10)"
                    rect { _x 0; _y 0; Svg._width 320; Svg._height 160; _rx 8; _fill "#ffffff"; _stroke "#cbd5e1" }
                    polygon { _points "0,140 80,100 160,120 240,40 320,20 320,160 0,160"; _fill "#10b981"; _fillOpacity 0.14 }
                    line { _x1 0; _y1 160; _x2 320; _y2 160; _stroke "#94a3b8" }
                    polyline { _points "0,140 80,100 160,120 240,40 320,20"; _fill "none"; _stroke "#059669"; _strokeWidth 4; _strokeLinecap "round"; _strokeLinejoin "round" }
                    ellipse { _cx 240; _cy 40; _rx 6; _ry 6; _fill "#047857" }
                    textElement { _x 160; _y 184; _textAnchor "middle"; _fontFamily "sans-serif"; _fontSize "12px"; _fill "#475569"; tspan { "Revenue" } }
                }
            })

    let private resourcesExampleSource = """svg {
    _viewBox "0 0 400 180"
    defs {
        linearGradient {
            _id "svg-brand-gradient"
            _x1 "0%"; _y1 "0%"; _x2 "100%"; _y2 "100%"
            stop { _offset "0%"; _stopColor "#0ea5e9" }
            stop { _offset "100%"; _stopColor "#6366f1" }
        }
        radialGradient {
            _id "svg-spotlight"
            stop { _offset "35%"; _stopColor "white" }
            stop { _offset "100%"; _stopColor "black" }
        }
        clipPath {
            _id "svg-card-clip"
            rect { _x 20; _y 20; Svg._width 360; Svg._height 140; _rx 28 }
        }
        mask {
            _id "svg-fade-mask"
            rect {
                _x 20; _y 20; Svg._width 360; Svg._height 140
                _fill "url(#svg-spotlight)"
            }
        }
        symbol {
            _id "svg-dot"
            _viewBox "0 0 10 10"
            circle { _cx 5; _cy 5; _r 5 }
        }
    }
    g {
        _clipPath "url(#svg-card-clip)"
        rect { _x 20; _y 20; Svg._width 360; Svg._height 140; _fill "url(#svg-brand-gradient)" }
        g {
            _fill "white"; _mask "url(#svg-fade-mask)"
            useElement { _href "#svg-dot"; _x 80; _y 70; Svg._width 40; Svg._height 40 }
            useElement { _href "#svg-dot"; _x 180; _y 45; Svg._width 70; Svg._height 70 }
            useElement { _href "#svg-dot"; _x 300; _y 75; Svg._width 30; Svg._height 30 }
        }
    }
}"""

    let private resourcesExamplePreview =
        previewSurface (
            svg {
                _viewBox "0 0 400 180"
                _role "img"
                _ariaLabel "Gradient card with clipped and masked reusable circles"
                _style "width:100%;max-width:40rem;height:auto"
                defs {
                    linearGradient { _id "svg-brand-gradient-preview"; _x1 "0%"; _y1 "0%"; _x2 "100%"; _y2 "100%"; stop { _offset "0%"; _stopColor "#0ea5e9" }; stop { _offset "100%"; _stopColor "#6366f1" } }
                    radialGradient { _id "svg-spotlight-preview"; stop { _offset "35%"; _stopColor "white" }; stop { _offset "100%"; _stopColor "black" } }
                    clipPath { _id "svg-card-clip-preview"; rect { _x 20; _y 20; Svg._width 360; Svg._height 140; _rx 28 } }
                    mask { _id "svg-fade-mask-preview"; rect { _x 20; _y 20; Svg._width 360; Svg._height 140; _fill "url(#svg-spotlight-preview)" } }
                    symbol { _id "svg-dot-preview"; _viewBox "0 0 10 10"; circle { _cx 5; _cy 5; _r 5 } }
                }
                g {
                    _clipPath "url(#svg-card-clip-preview)"
                    rect { _x 20; _y 20; Svg._width 360; Svg._height 140; _fill "url(#svg-brand-gradient-preview)" }
                    g {
                        _fill "white"; _mask "url(#svg-fade-mask-preview)"
                        useElement { _href "#svg-dot-preview"; _x 80; _y 70; Svg._width 40; Svg._height 40 }
                        useElement { _href "#svg-dot-preview"; _x 180; _y 45; Svg._width 70; Svg._height 70 }
                        useElement { _href "#svg-dot-preview"; _x 300; _y 75; Svg._width 30; Svg._height 30 }
                    }
                }
            })

    let private heading id title =
        Heading { id = id; title = title; level = 2 }

    let private nodes =
        [ Paragraph [ Text "FSharp.ViewEngine provides a maintained SVG 2 production subset for icons, charts, gradients, clipping, reusable symbols, text, and accessible inline graphics." ];
          heading "support-policy" "Support Policy";
          Paragraph [ Text "The "; InlineContent.Code "Svg"; Text " type intentionally covers 21 common elements rather than every SVG 2 feature. Unsupported filters, animation, metadata, and specialized elements remain available through the generic trusted-name escape hatches." ];
          Paragraph [ Text "SVG-specific helpers complement the global attributes on "; InlineContent.Code "Html"; Text ". Continue using "; InlineContent.Code "_id"; Text ", "; InlineContent.Code "_class"; Text ", "; InlineContent.Code "_style"; Text ", "; InlineContent.Code "_href"; Text ", "; InlineContent.Code "_role"; Text ", and the WAI-ARIA helpers from "; InlineContent.Code "Html"; Text "." ];

          heading "setup" "Setup";
          Paragraph [ Text "Open both static types when building inline SVG:" ];
          CodeBlock("fsharp", """open FSharp.ViewEngine
open type Html
open type Svg""");
          Paragraph [ Text "The SVG text and title builders are named "; InlineContent.Code "textElement"; Text " and "; InlineContent.Code "titleElement"; Text " so opening "; InlineContent.Code "Svg"; Text " does not shadow "; InlineContent.Code "Html.text"; Text " or "; InlineContent.Code "Html.title"; Text ". "; InlineContent.Code "useElement"; Text " avoids the F# "; InlineContent.Code "use"; Text " keyword." ];

          heading "element-reference" "Element Reference";
          Paragraph [ Strong [ Text "Structure and descriptions: " ]; InlineContent.Code "svg"; Text ", "; InlineContent.Code "g"; Text ", "; InlineContent.Code "defs"; Text ", "; InlineContent.Code "symbol"; Text ", "; InlineContent.Code "useElement"; Text ", "; InlineContent.Code "titleElement"; Text ", and "; InlineContent.Code "desc"; Text "." ];
          Paragraph [ Strong [ Text "Shapes: " ]; InlineContent.Code "path"; Text ", "; InlineContent.Code "circle"; Text ", "; InlineContent.Code "rect"; Text ", "; InlineContent.Code "line"; Text ", "; InlineContent.Code "polyline"; Text ", "; InlineContent.Code "polygon"; Text ", and "; InlineContent.Code "ellipse"; Text "." ];
          Paragraph [ Strong [ Text "Resources and paint: " ]; InlineContent.Code "clipPath"; Text ", "; InlineContent.Code "mask"; Text ", "; InlineContent.Code "linearGradient"; Text ", "; InlineContent.Code "radialGradient"; Text ", and "; InlineContent.Code "stop"; Text "." ];
          Paragraph [ Strong [ Text "Text: " ]; InlineContent.Code "textElement"; Text " and "; InlineContent.Code "tspan"; Text "." ];

          heading "numeric-and-length-values" "Numeric and Length Values";
          Paragraph [ Text "Geometry and numeric presentation helpers accept integers, invariant-culture floating-point values, and—where SVG permits lengths, percentages, or lists—strings. Root width and height retain numeric "; InlineContent.Code "Svg._width"; Text " and "; InlineContent.Code "Svg._height"; Text " helpers; use the existing HTML string helpers for CSS lengths." ];
          CodeBlock("fsharp", """svg {
    Svg._width 24
    Svg._height 24.5
    Html._width "100%"
    circle { _cx "50%"; _cy "50%"; _r 7.5 }
}""");

          heading "icon-example" "Icon Example";
          Example("svg-icon-example", "Check icon", "fsharp", iconExampleSource, iconExamplePreview);

          heading "chart-example" "Chart and Text Example";
          Example("svg-chart-example", "Revenue chart", "fsharp", chartExampleSource, chartExamplePreview);

          heading "resources-example" "Gradients, Clipping, Masking, and Reuse";
          Example("svg-resources-example", "Gradient resources", "fsharp", resourcesExampleSource, resourcesExamplePreview);

          heading "accessibility" "Accessibility";
          Paragraph [ Text "For an informative graphic, give the root an image role and connect direct child "; InlineContent.Code "titleElement"; Text " and "; InlineContent.Code "desc"; Text " elements with "; InlineContent.Code "_ariaLabelledby"; Text ". SVG Accessibility API Mappings expose these children as the accessible name and description." ];
          CodeBlock("fsharp", """svg {
    _role "img"
    _ariaLabelledby "sales-title sales-description"
    titleElement { _id "sales-title"; "Quarterly sales" }
    desc {
        _id "sales-description"
        "Sales increased in each quarter."
    }
    // chart geometry
}""");
          Paragraph [ Text "Hide a purely decorative SVG from assistive technology:" ];
          CodeBlock("fsharp", """svg {
    _ariaHidden true
    path { _d "M5 13l4 4L19 7" }
}""");

          heading "linking" "Linking and Namespaces";
          Paragraph [ Text "SVG 2 uses the unnamespaced "; InlineContent.Code "href"; Text " attribute. Use "; InlineContent.Code "Html._href"; Text " with "; InlineContent.Code "useElement"; Text " and do not emit deprecated "; InlineContent.Code "xlink:href"; Text ". Add "; InlineContent.Code "_xmlns"; Text " when producing standalone SVG markup; it is optional for inline SVG parsed as HTML." ];
          CodeBlock("fsharp", """symbol { _id "check"; path { _d "M5 13l4 4L19 7" } }
useElement { _href "#check" }""");

          heading "attribute-reference" "Attribute Reference";
          Paragraph [ Strong [ Text "Viewport and global presentation: " ]; InlineContent.Code "_viewBox"; Text ", "; InlineContent.Code "_preserveAspectRatio"; Text ", "; InlineContent.Code "_xmlns"; Text ", "; InlineContent.Code "_transform"; Text ", "; InlineContent.Code "_opacity"; Text ", and "; InlineContent.Code "_vectorEffect"; Text "." ];
          Paragraph [ Strong [ Text "Fill and stroke: " ]; InlineContent.Code "_fill"; Text ", "; InlineContent.Code "_fillOpacity"; Text ", "; InlineContent.Code "_fillRule"; Text ", "; InlineContent.Code "_stroke"; Text ", "; InlineContent.Code "_strokeWidth"; Text ", "; InlineContent.Code "_strokeOpacity"; Text ", "; InlineContent.Code "_strokeLinecap"; Text ", "; InlineContent.Code "_strokeLinejoin"; Text ", "; InlineContent.Code "_strokeMiterlimit"; Text ", "; InlineContent.Code "_strokeDasharray"; Text ", and "; InlineContent.Code "_strokeDashoffset"; Text "." ];
          Paragraph [ Strong [ Text "Geometry: " ]; InlineContent.Code "_x"; Text ", "; InlineContent.Code "_y"; Text ", "; InlineContent.Code "_x1"; Text ", "; InlineContent.Code "_y1"; Text ", "; InlineContent.Code "_x2"; Text ", "; InlineContent.Code "_y2"; Text ", "; InlineContent.Code "_cx"; Text ", "; InlineContent.Code "_cy"; Text ", "; InlineContent.Code "_r"; Text ", "; InlineContent.Code "_rx"; Text ", "; InlineContent.Code "_ry"; Text ", "; InlineContent.Code "_width"; Text ", "; InlineContent.Code "_height"; Text ", "; InlineContent.Code "_d"; Text ", "; InlineContent.Code "_points"; Text ", and "; InlineContent.Code "_pathLength"; Text "." ];
          Paragraph [ Strong [ Text "Resources: " ]; InlineContent.Code "_clipRule"; Text ", "; InlineContent.Code "_clipPath"; Text ", "; InlineContent.Code "_clipPathUnits"; Text ", "; InlineContent.Code "_mask"; Text ", "; InlineContent.Code "_maskUnits"; Text ", "; InlineContent.Code "_maskContentUnits"; Text ", "; InlineContent.Code "_gradientUnits"; Text ", "; InlineContent.Code "_gradientTransform"; Text ", "; InlineContent.Code "_spreadMethod"; Text ", "; InlineContent.Code "_fx"; Text ", "; InlineContent.Code "_fy"; Text ", "; InlineContent.Code "_fr"; Text ", "; InlineContent.Code "_offset"; Text ", "; InlineContent.Code "_stopColor"; Text ", and "; InlineContent.Code "_stopOpacity"; Text "." ];
          Paragraph [ Strong [ Text "Text: " ]; InlineContent.Code "_dx"; Text ", "; InlineContent.Code "_dy"; Text ", "; InlineContent.Code "_textAnchor"; Text ", "; InlineContent.Code "_dominantBaseline"; Text ", "; InlineContent.Code "_fontFamily"; Text ", "; InlineContent.Code "_fontSize"; Text ", "; InlineContent.Code "_fontWeight"; Text ", "; InlineContent.Code "_textLength"; Text ", and "; InlineContent.Code "_lengthAdjust"; Text "." ];

          heading "unsupported-svg" "Unsupported SVG";
          Paragraph [ Text "Use "; InlineContent.Code "Html.el"; Text " for an SVG element outside the maintained production subset and "; InlineContent.Code "Html._attr"; Text " for an unsupported attribute. Names passed to both helpers are trusted markup tokens and are not validated." ];
          CodeBlock("fsharp", """svg {
    Html.el "filter" {
        _id "blur"
        Html.el "feGaussianBlur" {
            _attr ("stdDeviation", "2")
        }
    }
}""") ]

    let page =
        { id = "svg"
          path = "/extensions/svg"
          aliases = []
          navLabel = "SVG"
          category = "Extensions"
          title = "SVG"
          browserTitle = "SVG - FSharp.ViewEngine"
          nodes = nodes }
