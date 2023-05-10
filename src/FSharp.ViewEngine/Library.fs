namespace FSharp.ViewEngine

type Attribute =
    | KeyValue of string * string   // e.g., div [ _class "text-xl" ] -> <div class="text-xl"></div>
    | Boolean of string             // e.g., button [ _disabled ] -> <button disabled></button>
    | Children of Element seq       // e.g., div [ p "Hello" ] -> <div><p>Hello</p></div>
    
and Element =
    | Raw of string                         // Raw content
    | Tag of string * Attribute seq         // e.g., <h1>Hello</h1>
    | Void of string * Attribute seq        // e.g., <br>
    | Fragment of Element seq               // Directly render children
    | Noop                                  // No op

module private ViewBuilder =
    open System.Text
    
    let inline (+=) (sb:StringBuilder) (s:string) = sb.Append(s)
    let inline (+!) (sb:StringBuilder) (s:string) = sb.Append(s) |> ignore
    
    let rec buildElement (el:Element) (sb:StringBuilder) =
        match el with
        | Raw text -> sb +! text
        | Tag (tag, attributes) ->
            sb += "<" +! tag
            let children = ResizeArray()
            for attr in attributes do
                match attr with
                | KeyValue (key, value) -> sb += " " += key += "=\"" += value +! "\""
                | Boolean key -> sb += " " +! key
                | Children elements -> children.AddRange(elements)
            sb +! ">"
            for child in children do buildElement child sb
            sb += "</" += tag +! ">"
        | Void (tag, attributes) ->
            sb += "<" +! tag
            for attr in attributes do
                match attr with
                | KeyValue (key, value) -> sb += " " += key += "=\"" += value +! "\""
                | Boolean key -> sb += " " +! key
                | Children _ -> failwith "void elements cannot have children"
            sb +! ">"
        | Fragment children -> for child in children do buildElement child sb
        | Noop -> ()

[<RequireQualifiedAccess>]
module Element =
    open System.Text
    open ViewBuilder
    
    let render (element:Element) =
        let sb = StringBuilder()
        buildElement element sb
        sb.ToString()
        
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
