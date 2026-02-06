namespace FSharp.ViewEngine

type Svg =
    static member val svg = TagBuilder("svg") with get
    static member val path = TagBuilder("path") with get
    static member val circle = TagBuilder("circle") with get
    static member inline _viewBox (v: string) = { Name = "viewBox"; Value = ValueSome v }
    static member inline _width (v: int) = { Name = "width"; Value = ValueSome(string v) }
    static member inline _height (v: int) = { Name = "height"; Value = ValueSome(string v) }
    static member inline _fill (v: string) = { Name = "fill"; Value = ValueSome v }
    static member inline _stroke (v: string) = { Name = "stroke"; Value = ValueSome v }
    static member inline _strokeWidth (v: int) = { Name = "stroke-width"; Value = ValueSome(string v) }
    static member inline _strokeLinecap (v: string) = { Name = "stroke-linecap"; Value = ValueSome v }
    static member inline _strokeLinejoin (v: string) = { Name = "stroke-linejoin"; Value = ValueSome v }
    static member inline _fillRule (v: string) = { Name = "fill-rule"; Value = ValueSome v }
    static member inline _clipRule (v: string) = { Name = "clip-rule"; Value = ValueSome v }
    static member inline _d (v: string) = { Name = "d"; Value = ValueSome v }
    static member inline _cx (v: int) = { Name = "cx"; Value = ValueSome(string v) }
    static member inline _cy (v: int) = { Name = "cy"; Value = ValueSome(string v) }
    static member inline _r (v: int) = { Name = "r"; Value = ValueSome(string v) }

