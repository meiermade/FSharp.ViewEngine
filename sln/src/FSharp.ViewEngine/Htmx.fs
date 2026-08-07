namespace FSharp.ViewEngine

type Htmx =
    static member inline _hx (key: string, value: string) = { Name = $"hx-{key}"; Value = ValueSome value }
    static member inline _hxGet (v: string) = { Name = "hx-get"; Value = ValueSome v }
    static member inline _hxPost (v: string) = { Name = "hx-post"; Value = ValueSome v }
    static member inline _hxPut (v: string) = { Name = "hx-put"; Value = ValueSome v }
    static member inline _hxPatch (v: string) = { Name = "hx-patch"; Value = ValueSome v }
    static member inline _hxDelete (v: string) = { Name = "hx-delete"; Value = ValueSome v }
    static member inline _hxOn (event: string, value: string) = { Name = $"hx-on:{event}"; Value = ValueSome value }
    static member inline _hxPushUrl (v: string) = { Name = "hx-push-url"; Value = ValueSome v }
    static member inline _hxSelect (v: string) = { Name = "hx-select"; Value = ValueSome v }
    static member inline _hxSelectOOB (v: string) = { Name = "hx-select-oob"; Value = ValueSome v }
    static member inline _hxSwap (v: string) = { Name = "hx-swap"; Value = ValueSome v }
    static member inline _hxSwapOOB (v: string) = { Name = "hx-swap-oob"; Value = ValueSome v }
    static member inline _hxTarget (v: string) = { Name = "hx-target"; Value = ValueSome v }
    static member inline _hxTrigger (v: string) = { Name = "hx-trigger"; Value = ValueSome v }
    static member inline _hxVals (v: string) = { Name = "hx-vals"; Value = ValueSome v }
    static member inline _hxBoost (v: string) = { Name = "hx-boost"; Value = ValueSome v }
    static member inline _hxConfirm (v: string) = { Name = "hx-confirm"; Value = ValueSome v }
    static member inline _hxDisable = { Name = "hx-disable"; Value = ValueNone }
    static member inline _hxDisabledElt (v: string) = { Name = "hx-disabled-elt"; Value = ValueSome v }
    static member inline _hxDisinherit (v: string) = { Name = "hx-disinherit"; Value = ValueSome v }
    static member inline _hxEncoding (v: string) = { Name = "hx-encoding"; Value = ValueSome v }
    static member inline _hxExt (v: string) = { Name = "hx-ext"; Value = ValueSome v }
    static member inline _hxHeaders (v: string) = { Name = "hx-headers"; Value = ValueSome v }
    static member inline _hxHistory (v: string) = { Name = "hx-history"; Value = ValueSome v }
    static member inline _hxHistoryElt = { Name = "hx-history-elt"; Value = ValueNone }
    static member inline _hxInclude (v: string) = { Name = "hx-include"; Value = ValueSome v }
    static member inline _hxIndicator (v: string) = { Name = "hx-indicator"; Value = ValueSome v }
    static member inline _hxInherit (v: string) = { Name = "hx-inherit"; Value = ValueSome v }
    static member inline _hxParams (v: string) = { Name = "hx-params"; Value = ValueSome v }
    static member inline _hxPreserve = { Name = "hx-preserve"; Value = ValueNone }
    static member inline _hxPrompt (v: string) = { Name = "hx-prompt"; Value = ValueSome v }
    static member inline _hxReplaceUrl (v: string) = { Name = "hx-replace-url"; Value = ValueSome v }
    static member inline _hxRequest (v: string) = { Name = "hx-request"; Value = ValueSome v }
    static member inline _hxSync (v: string) = { Name = "hx-sync"; Value = ValueSome v }
    static member inline _hxValidate (v: string) = { Name = "hx-validate"; Value = ValueSome v }
