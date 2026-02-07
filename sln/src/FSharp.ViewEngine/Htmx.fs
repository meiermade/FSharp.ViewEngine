namespace FSharp.ViewEngine

type Htmx =
    static member inline _hx (key: string, value: string) = { Name = $"hx-{key}"; Value = ValueSome value }
    static member inline _hxGet (v: string) = { Name = "hx-get"; Value = ValueSome v }
    static member inline _hxPost (v: string) = { Name = "hx-post"; Value = ValueSome v }
    static member inline _hxDelete (v: string) = { Name = "hx-delete"; Value = ValueSome v }
    static member inline _hxTrigger (v: string) = { Name = "hx-trigger"; Value = ValueSome v }
    static member inline _hxTarget (v: string) = { Name = "hx-target"; Value = ValueSome v }
    static member inline _hxIndicator (v: string) = { Name = "hx-indicator"; Value = ValueSome v }
    static member inline _hxInclude (v: string) = { Name = "hx-include"; Value = ValueSome v }
    static member inline _hxSwap (v: string) = { Name = "hx-swap"; Value = ValueSome v }
    static member inline _hxSwapOOB (v: string) = { Name = "hx-swap-oob"; Value = ValueSome v }
    static member inline _hxEncoding (v: string) = { Name = "hx-encoding"; Value = ValueSome v }
    static member inline _hxOn (event: string, value: string) = { Name = $"hx-on:{event}"; Value = ValueSome value }
    static member inline _hxHistory (v: string) = { Name = "hx-history"; Value = ValueSome v }
    static member inline _hxVals (v: string) = { Name = "hx-vals"; Value = ValueSome v }
