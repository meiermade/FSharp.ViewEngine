namespace FSharp.ViewEngine

open System.Globalization

type Svg =
    static member val svg = TagBuilder("svg") with get
    static member val g = TagBuilder("g") with get
    static member val defs = TagBuilder("defs") with get
    static member val symbol = TagBuilder("symbol") with get
    static member val useElement = TagBuilder("use") with get
    static member val titleElement = TagBuilder("title") with get
    static member val desc = TagBuilder("desc") with get

    static member val path = TagBuilder("path") with get
    static member val circle = TagBuilder("circle") with get
    static member val rect = TagBuilder("rect") with get
    static member val line = TagBuilder("line") with get
    static member val polyline = TagBuilder("polyline") with get
    static member val polygon = TagBuilder("polygon") with get
    static member val ellipse = TagBuilder("ellipse") with get

    static member val clipPath = TagBuilder("clipPath") with get
    static member val mask = TagBuilder("mask") with get

    static member val linearGradient = TagBuilder("linearGradient") with get
    static member val radialGradient = TagBuilder("radialGradient") with get
    static member val stop = TagBuilder("stop") with get

    static member val textElement = TagBuilder("text") with get
    static member val tspan = TagBuilder("tspan") with get

    static member private Attribute(name: string, value: string) =
        { Name = name; Value = ValueSome value }

    static member private Attribute(name: string, value: int) =
        { Name = name
          Value = ValueSome(value.ToString(CultureInfo.InvariantCulture)) }

    static member private Attribute(name: string, value: float) =
        { Name = name
          Value = ValueSome(value.ToString("R", CultureInfo.InvariantCulture)) }

    static member _viewBox(value: string) = Svg.Attribute("viewBox", value)
    static member _preserveAspectRatio(value: string) = Svg.Attribute("preserveAspectRatio", value)
    static member _xmlns(value: string) = Svg.Attribute("xmlns", value)
    static member _transform(value: string) = Svg.Attribute("transform", value)
    static member _vectorEffect(value: string) = Svg.Attribute("vector-effect", value)

    static member _opacity(value: int) = Svg.Attribute("opacity", value)
    static member _opacity(value: float) = Svg.Attribute("opacity", value)
    static member _opacity(value: string) = Svg.Attribute("opacity", value)

    static member _fill(value: string) = Svg.Attribute("fill", value)
    static member _fillRule(value: string) = Svg.Attribute("fill-rule", value)

    static member _fillOpacity(value: int) = Svg.Attribute("fill-opacity", value)
    static member _fillOpacity(value: float) = Svg.Attribute("fill-opacity", value)
    static member _fillOpacity(value: string) = Svg.Attribute("fill-opacity", value)

    static member _stroke(value: string) = Svg.Attribute("stroke", value)
    static member _strokeLinecap(value: string) = Svg.Attribute("stroke-linecap", value)
    static member _strokeLinejoin(value: string) = Svg.Attribute("stroke-linejoin", value)
    static member _strokeDasharray(value: string) = Svg.Attribute("stroke-dasharray", value)

    static member _strokeWidth(value: int) = Svg.Attribute("stroke-width", value)
    static member _strokeWidth(value: float) = Svg.Attribute("stroke-width", value)
    static member _strokeWidth(value: string) = Svg.Attribute("stroke-width", value)

    static member _strokeOpacity(value: int) = Svg.Attribute("stroke-opacity", value)
    static member _strokeOpacity(value: float) = Svg.Attribute("stroke-opacity", value)
    static member _strokeOpacity(value: string) = Svg.Attribute("stroke-opacity", value)

    static member _strokeMiterlimit(value: int) = Svg.Attribute("stroke-miterlimit", value)
    static member _strokeMiterlimit(value: float) = Svg.Attribute("stroke-miterlimit", value)
    static member _strokeMiterlimit(value: string) = Svg.Attribute("stroke-miterlimit", value)

    static member _strokeDashoffset(value: int) = Svg.Attribute("stroke-dashoffset", value)
    static member _strokeDashoffset(value: float) = Svg.Attribute("stroke-dashoffset", value)
    static member _strokeDashoffset(value: string) = Svg.Attribute("stroke-dashoffset", value)

    static member _clipRule(value: string) = Svg.Attribute("clip-rule", value)
    static member _clipPath(value: string) = Svg.Attribute("clip-path", value)
    static member _clipPathUnits(value: string) = Svg.Attribute("clipPathUnits", value)
    static member _mask(value: string) = Svg.Attribute("mask", value)
    static member _maskUnits(value: string) = Svg.Attribute("maskUnits", value)
    static member _maskContentUnits(value: string) = Svg.Attribute("maskContentUnits", value)

    static member _d(value: string) = Svg.Attribute("d", value)
    static member _points(value: string) = Svg.Attribute("points", value)

    static member _pathLength(value: int) = Svg.Attribute("pathLength", value)
    static member _pathLength(value: float) = Svg.Attribute("pathLength", value)
    static member _pathLength(value: string) = Svg.Attribute("pathLength", value)

    static member _width(value: int) = Svg.Attribute("width", value)
    static member _width(value: float) = Svg.Attribute("width", value)
    static member _height(value: int) = Svg.Attribute("height", value)
    static member _height(value: float) = Svg.Attribute("height", value)

    static member _x(value: int) = Svg.Attribute("x", value)
    static member _x(value: float) = Svg.Attribute("x", value)
    static member _x(value: string) = Svg.Attribute("x", value)
    static member _y(value: int) = Svg.Attribute("y", value)
    static member _y(value: float) = Svg.Attribute("y", value)
    static member _y(value: string) = Svg.Attribute("y", value)

    static member _x1(value: int) = Svg.Attribute("x1", value)
    static member _x1(value: float) = Svg.Attribute("x1", value)
    static member _x1(value: string) = Svg.Attribute("x1", value)
    static member _y1(value: int) = Svg.Attribute("y1", value)
    static member _y1(value: float) = Svg.Attribute("y1", value)
    static member _y1(value: string) = Svg.Attribute("y1", value)
    static member _x2(value: int) = Svg.Attribute("x2", value)
    static member _x2(value: float) = Svg.Attribute("x2", value)
    static member _x2(value: string) = Svg.Attribute("x2", value)
    static member _y2(value: int) = Svg.Attribute("y2", value)
    static member _y2(value: float) = Svg.Attribute("y2", value)
    static member _y2(value: string) = Svg.Attribute("y2", value)

    static member _cx(value: int) = Svg.Attribute("cx", value)
    static member _cx(value: float) = Svg.Attribute("cx", value)
    static member _cx(value: string) = Svg.Attribute("cx", value)
    static member _cy(value: int) = Svg.Attribute("cy", value)
    static member _cy(value: float) = Svg.Attribute("cy", value)
    static member _cy(value: string) = Svg.Attribute("cy", value)
    static member _r(value: int) = Svg.Attribute("r", value)
    static member _r(value: float) = Svg.Attribute("r", value)
    static member _r(value: string) = Svg.Attribute("r", value)
    static member _rx(value: int) = Svg.Attribute("rx", value)
    static member _rx(value: float) = Svg.Attribute("rx", value)
    static member _rx(value: string) = Svg.Attribute("rx", value)
    static member _ry(value: int) = Svg.Attribute("ry", value)
    static member _ry(value: float) = Svg.Attribute("ry", value)
    static member _ry(value: string) = Svg.Attribute("ry", value)

    static member _fx(value: int) = Svg.Attribute("fx", value)
    static member _fx(value: float) = Svg.Attribute("fx", value)
    static member _fx(value: string) = Svg.Attribute("fx", value)
    static member _fy(value: int) = Svg.Attribute("fy", value)
    static member _fy(value: float) = Svg.Attribute("fy", value)
    static member _fy(value: string) = Svg.Attribute("fy", value)
    static member _fr(value: int) = Svg.Attribute("fr", value)
    static member _fr(value: float) = Svg.Attribute("fr", value)
    static member _fr(value: string) = Svg.Attribute("fr", value)

    static member _gradientUnits(value: string) = Svg.Attribute("gradientUnits", value)
    static member _gradientTransform(value: string) = Svg.Attribute("gradientTransform", value)
    static member _spreadMethod(value: string) = Svg.Attribute("spreadMethod", value)

    static member _offset(value: int) = Svg.Attribute("offset", value)
    static member _offset(value: float) = Svg.Attribute("offset", value)
    static member _offset(value: string) = Svg.Attribute("offset", value)
    static member _stopColor(value: string) = Svg.Attribute("stop-color", value)

    static member _stopOpacity(value: int) = Svg.Attribute("stop-opacity", value)
    static member _stopOpacity(value: float) = Svg.Attribute("stop-opacity", value)
    static member _stopOpacity(value: string) = Svg.Attribute("stop-opacity", value)

    static member _dx(value: int) = Svg.Attribute("dx", value)
    static member _dx(value: float) = Svg.Attribute("dx", value)
    static member _dx(value: string) = Svg.Attribute("dx", value)
    static member _dy(value: int) = Svg.Attribute("dy", value)
    static member _dy(value: float) = Svg.Attribute("dy", value)
    static member _dy(value: string) = Svg.Attribute("dy", value)

    static member _textAnchor(value: string) = Svg.Attribute("text-anchor", value)
    static member _dominantBaseline(value: string) = Svg.Attribute("dominant-baseline", value)
    static member _fontFamily(value: string) = Svg.Attribute("font-family", value)
    static member _fontWeight(value: string) = Svg.Attribute("font-weight", value)

    static member _fontSize(value: int) = Svg.Attribute("font-size", value)
    static member _fontSize(value: float) = Svg.Attribute("font-size", value)
    static member _fontSize(value: string) = Svg.Attribute("font-size", value)

    static member _textLength(value: int) = Svg.Attribute("textLength", value)
    static member _textLength(value: float) = Svg.Attribute("textLength", value)
    static member _textLength(value: string) = Svg.Attribute("textLength", value)
    static member _lengthAdjust(value: string) = Svg.Attribute("lengthAdjust", value)
