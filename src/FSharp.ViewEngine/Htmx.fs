namespace FSharp.ViewEngine

type Htmx =
    static member _hx (key:string, value:string) = KeyValue ($"hx-{key}", value)
    static member _hxGet (v:string) = KeyValue ("hx-get", v)
    static member _hxPost (v:string) = KeyValue("hx-post", v)
    static member _hxDelete (v:string) = KeyValue("hx-delete", v)
    static member _hxTrigger (v:string) = KeyValue ("hx-trigger", v)
    static member _hxTarget (v:string) = KeyValue ("hx-target", v)
    static member _hxIndicator (v:string) = KeyValue ("hx-indicator", v)
    static member _hxInclude (v:string) = KeyValue ("hx-include", v)
    static member _hxSwap (v:string) = KeyValue ("hx-swap", v)
    static member _hxSwapOOB (v:string) = KeyValue ("hx-swap-oob", v)
    static member _hxEncoding (value:string) = KeyValue("hx-encoding", value)
    static member _hxOn (event:string, value:string) = KeyValue($"hx-on:{event}", value)
    static member _hxHistory (value:string) = KeyValue("hx-history", value)
    static member _hxVals(value:string) = KeyValue ("hx-vals", value)
