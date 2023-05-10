namespace FSharp.ViewEngine.Alpine

open FSharp.ViewEngine

type Alpine =
    static member _xOn (event:string, v:string) = KeyValue ($"x-on:{event}", v)
    static member _xOn (event:string) = Boolean $"x-on:{event}"
    static member _xInit (v:string) = KeyValue ("x-init", v)
    static member _xData (v:string) = KeyValue ("x-data", v)
    static member _xRef (v:string) = KeyValue ("x-ref", v)
    static member _xText (v:string) = KeyValue ("x-text", v)
    static member _xBind (attr:string, v:string) = KeyValue ($"x-bind:{attr}", v)
    static member _xShow (v:string) = KeyValue ("x-show", v)
    static member _xIf (v:string) = KeyValue ("x-if", v)
    static member _xFor (v:string) = KeyValue ("x-for", v)
    static member _xModel (v:string) = KeyValue ("x-model", v)
    static member _xId (v:string) = KeyValue ("x-id", v)
    static member _xEffect (v:string) = KeyValue ("x-effect", v)
    static member _xTransition (?modifier:string) =
        match modifier with
        | Some m -> Boolean $"x-transition.{m}"
        | None -> Boolean "x-transition"
    static member _xTrap (v:string, ?modifier:string) =
        match modifier with
        | Some m -> KeyValue ($"x-trap.{m}", v)
        | None -> KeyValue ("x-trap", v)
