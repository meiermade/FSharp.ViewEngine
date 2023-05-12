namespace FSharp.ViewEngine

type Attribute =
    | KeyValue of string * string   // e.g., div [ _class "text-xl" ] -> <div class="text-xl"></div>
    | Boolean of string             // e.g., button [ _disabled ] -> <button disabled></button>
    | Children of Element seq       // e.g., div [ p "Hello" ] -> <div><p>Hello</p></div>
    
and Element =
    | Raw of string                         // Raw content
    | Text of string                        // Text content (html encoded)
    | Tag of string * Attribute seq         // e.g., <h1>Hello</h1>
    | Void of string * Attribute seq        // e.g., <br>
    | Fragment of Element seq               // Directly render children
    | Noop                                  // No op

module private ViewBuilder =
    open System.Text
    open System.Web
    
    let inline (+=) (sb:StringBuilder) (s:string) = sb.Append(s)
    let inline (+!) (sb:StringBuilder) (s:string) = sb.Append(s) |> ignore
    
    let encode (s:string) = HttpUtility.HtmlEncode(s)
    
    let rec buildElement (el:Element) (sb:StringBuilder) =
        match el with
        | Raw text -> sb +! text
        | Text text -> sb +! encode text
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
