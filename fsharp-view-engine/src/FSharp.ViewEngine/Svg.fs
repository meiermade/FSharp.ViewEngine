namespace FSharp.ViewEngine

type Svg =
    static member svg (attrs:Attribute seq) = Tag ("svg", attrs)
    static member path (attrs:Attribute seq) = Tag ("path", attrs)
    static member circle (attrs:Attribute seq) = Tag ("circle", attrs)
    
    static member _viewBox (v:string) = KeyValue ("viewBox", v)
    static member _width (v:int) = KeyValue ("width", string v)
    static member _height (v:int) = KeyValue ("height", string v)
    static member _fill (v:string) = KeyValue ("fill", v)
    static member _stroke (v:string) = KeyValue ("stroke", v)
    static member _strokeWidth (v:int) = KeyValue ("stroke-width", string v)
    static member _strokeLinecap (v:string) = KeyValue ("stroke-linecap", v)
    static member _strokeLinejoin (v:string) = KeyValue ("stroke-linejoin", v)
    static member _fillRule (v:string) = KeyValue ("fill-rule", v)
    static member _clipRule (v:string) = KeyValue ("clip-rule", v)
    static member _d (v:string) = KeyValue ("d", v)
    static member _cx (v:int) = KeyValue ("cx", string v)
    static member _cy (v:int) = KeyValue ("cy", string v)
    static member _r (v:int) = KeyValue ("r", string v)
    
