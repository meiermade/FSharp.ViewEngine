namespace Docs.Pages

open Docs.Common

module Svg =
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
          CodeBlock("fsharp", """let checkIcon =
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
    }""");

          heading "chart-example" "Chart and Text Example";
          CodeBlock("fsharp", """svg {
    _viewBox "0 0 400 200"
    g {
        _transform "translate(40 10)"
        _opacity 0.9
        rect { _x 0; _y 0; Svg._width 320; Svg._height 160; _rx 4 }
        line { _x1 0; _y1 160; _x2 320; _y2 160 }
        polyline { _points "0,140 80,100 160,120 240,40 320,20" }
        polygon { _points "0,160 80,120 160,140 160,160" }
        ellipse { _cx 240; _cy 40; _rx 6; _ry 4 }
        textElement {
            _x 160
            _y 190
            _textAnchor "middle"
            _dominantBaseline "middle"
            _fontFamily "sans-serif"
            _fontSize "12px"
            tspan { "Revenue" }
        }
    }
}""");

          heading "resources-example" "Gradients, Clipping, Masking, and Reuse";
          CodeBlock("fsharp", """svg {
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
            _cx "50%"; _cy "50%"; _r "50%"
            _fx "45%"; _fy "45%"; _fr "5%"
        }
        clipPath {
            _id "plot-clip"
            _clipPathUnits "userSpaceOnUse"
            rect { Svg._width 100; Svg._height 50 }
        }
        mask {
            _id "fade-mask"
            _maskUnits "userSpaceOnUse"
            _maskContentUnits "userSpaceOnUse"
        }
        symbol {
            _id "dot"
            _viewBox "0 0 10 10"
            circle { _cx 5; _cy 5; _r 5 }
        }
    }
    g {
        _fill "url(#brand-gradient)"
        _fillOpacity 0.8
        _clipPath "url(#plot-clip)"
        _mask "url(#fade-mask)"
        useElement { _href "#dot"; _x 10; _y 20 }
    }
}""");

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
