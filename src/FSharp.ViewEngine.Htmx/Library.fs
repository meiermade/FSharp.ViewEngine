namespace FSharp.ViewEngine

type Htmx =
    static member _hxMethod (method:string, url:string) = KeyValue ("hx-" + method, url)
    static member _hxGet (v:string) = KeyValue ("hx-get", v)
    static member _hxPost (v:string) = KeyValue("hx-post", v)
    static member _hxDelete (v:string) = KeyValue("hx-delete", v)
    static member _hxTrigger (v:string) = KeyValue ("hx-trigger", v)
    static member _hxTarget (v:string) = KeyValue ("hx-target", v)
    static member _hxIndicator (v:string) = KeyValue ("hx-indicator", v)
    static member _hxSwap (v:string) = KeyValue ("hx-swap", v)
    static member _hxSwapOOB (v:string) = KeyValue ("hx-swap-oob", v)
