namespace FSharp.ViewEngine

type Alpine =
    static member inline _by (value: string) = { Name = "by"; Value = ValueSome value }
    static member inline _x (key: string, ?value: string) =
        match value with
        | Some v -> { Name = $"x-{key}"; Value = ValueSome v }
        | None -> { Name = $"x-{key}"; Value = ValueNone }
    static member inline _xOn (event: string, v: string) = { Name = $"x-on:{event}"; Value = ValueSome v }
    static member inline _xOn (event: string) = { Name = $"x-on:{event}"; Value = ValueNone }
    static member inline _xInit (value: string) = { Name = "x-init"; Value = ValueSome value }
    static member inline _xData (value: string) = { Name = "x-data"; Value = ValueSome value }
    static member inline _xRef (value: string) = { Name = "x-ref"; Value = ValueSome value }
    static member inline _xText (value: string) = { Name = "x-text"; Value = ValueSome value }
    static member inline _xBind (attr: string, value: string) = { Name = $"x-bind:{attr}"; Value = ValueSome value }
    static member inline _xShow (value: string) = { Name = "x-show"; Value = ValueSome value }
    static member inline _xIf (value: string) = { Name = "x-if"; Value = ValueSome value }
    static member inline _xFor (value: string) = { Name = "x-for"; Value = ValueSome value }
    static member inline _xModel (value: string, ?modifier: string) =
        match modifier with
        | Some m -> { Name = $"x-model{m}"; Value = ValueSome value }
        | None -> { Name = "x-model"; Value = ValueSome value }
    static member inline _xModelable (value: string) = { Name = "x-modelable"; Value = ValueSome value }
    static member inline _xId (value: string) = { Name = "x-id"; Value = ValueSome value }
    static member inline _xEffect (value: string) = { Name = "x-effect"; Value = ValueSome value }
    static member inline _xTransition (?value: string, ?modifier: string) =
        match value, modifier with
        | Some v, Some m -> { Name = $"x-transition{m}"; Value = ValueSome v }
        | Some v, None -> { Name = "x-transition"; Value = ValueSome v }
        | None, Some m -> { Name = $"x-transition{m}"; Value = ValueNone }
        | None, None -> { Name = "x-transition"; Value = ValueNone }
    static member inline _xTrap (value: string, ?modifier: string) =
        match modifier with
        | Some m -> { Name = $"x-trap{m}"; Value = ValueSome value }
        | None -> { Name = "x-trap"; Value = ValueSome value }
    static member inline _xCloak = { Name = "x-cloak"; Value = ValueNone }
    static member inline _xAnchor (value: string, ?modifier: string) =
        match modifier with
        | Some m -> { Name = $"x-anchor{m}"; Value = ValueSome value }
        | None -> { Name = "x-anchor"; Value = ValueSome value }
    static member inline _xTeleport (value: string) = { Name = "x-teleport"; Value = ValueSome value }
