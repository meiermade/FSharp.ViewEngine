namespace FSharp.ViewEngine

type Alpine =
    static member _by(value:string) = KeyValue("by", value)
    static member _x (key:string, ?value:string) = match value with Some v -> KeyValue ($"x-{key}", v) | None -> Boolean $"x-{key}"
    static member _xOn (event:string, v:string) = KeyValue ($"x-on:{event}", v)
    static member _xOn (event:string) = Boolean $"x-on:{event}"
    static member _xInit (value:string) = KeyValue ("x-init", value)
    static member _xData (value:string) = KeyValue ("x-data", value)
    static member _xRef (value:string) = KeyValue ("x-ref", value)
    static member _xText (value:string) = KeyValue ("x-text", value)
    static member _xBind (attr:string, value:string) = KeyValue ($"x-bind:{attr}", value)
    static member _xShow (value:string) = KeyValue ("x-show", value)
    static member _xIf (value:string) = KeyValue ("x-if", value)
    static member _xFor (value:string) = KeyValue ("x-for", value)
    static member _xModel (value:string, ?modifier:string) =
        match modifier with
        | Some modifier -> KeyValue ($"x-model{modifier}", value)
        | None -> KeyValue("x-model", value)
    static member _xModelable (value:string) = KeyValue ("x-modelable", value)
    static member _xId (value:string) = KeyValue ("x-id", value)
    static member _xEffect (value:string) = KeyValue ("x-effect", value)
    static member _xTransition (?value:string, ?modifier:string) =
        match value, modifier with
        | Some value, Some modifier -> KeyValue ($"x-transition{modifier}", value)
        | Some value, None -> KeyValue ("x-transition", value)
        | None, Some modifier -> Boolean $"x-transition{modifier}"
        | None, None -> Boolean "x-transition"
    static member _xTrap (value:string, ?modifier:string) =
        match modifier with
        | Some modifier -> KeyValue ($"x-trap{modifier}", value)
        | None -> KeyValue ("x-trap", value)
    static member _xCloak = Boolean "x-cloak"
    static member _xAnchor (value:string, ?modifier:string) =
        match modifier with
        | Some m -> KeyValue ($"x-anchor{m}", value)
        | None -> KeyValue ("x-anchor", value)
    static member _xTeleport(value:string) = KeyValue("x-teleport", value)
