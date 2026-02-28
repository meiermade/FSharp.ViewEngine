namespace FSharp.ViewEngine

type Datastar =
    // Core attributes
    static member inline _dataAttr (name: string, v: string) = { Name = $"data-attr:{name}"; Value = ValueSome v }
    static member inline _dataBind (name: string) = { Name = $"data-bind:{name}"; Value = ValueNone }
    static member inline _dataBind (name: string, v: string) = { Name = $"data-bind:{name}"; Value = ValueSome v }
    static member inline _dataClass (name: string, v: string) = { Name = $"data-class:{name}"; Value = ValueSome v }
    static member inline _dataComputed (name: string, v: string) = { Name = $"data-computed:{name}"; Value = ValueSome v }
    static member inline _dataEffect (v: string) = { Name = "data-effect"; Value = ValueSome v }
    static member inline _dataIgnore = { Name = "data-ignore"; Value = ValueNone }
    static member inline _dataIgnoreMorph = { Name = "data-ignore-morph"; Value = ValueNone }
    static member inline _dataIndicator (name: string) = { Name = $"data-indicator:{name}"; Value = ValueNone }
    static member inline _dataIndicator (name: string, v: string) = { Name = $"data-indicator:{name}"; Value = ValueSome v }
    static member inline _dataInit (v: string) = { Name = "data-init"; Value = ValueSome v }
    static member inline _dataJsonSignals (?v: string) = match v with Some v -> { Name = "data-json-signals"; Value = ValueSome v } | None -> { Name = "data-json-signals"; Value = ValueNone }
    static member inline _dataOn (event: string, v: string) = { Name = $"data-on:{event}"; Value = ValueSome v }
    static member inline _dataOnIntersect (v: string) = { Name = "data-on-intersect"; Value = ValueSome v }
    static member inline _dataOnInterval (v: string) = { Name = "data-on-interval"; Value = ValueSome v }
    static member inline _dataOnSignalPatch (v: string) = { Name = "data-on-signal-patch"; Value = ValueSome v }
    static member inline _dataOnSignalPatchFilter (v: string) = { Name = "data-on-signal-patch-filter"; Value = ValueSome v }
    static member inline _dataPreserveAttr (v: string) = { Name = "data-preserve-attr"; Value = ValueSome v }
    static member inline _dataRef (name: string) = { Name = $"data-ref:{name}"; Value = ValueNone }
    static member inline _dataRef (name: string, v: string) = { Name = $"data-ref:{name}"; Value = ValueSome v }
    static member inline _dataShow (v: string) = { Name = "data-show"; Value = ValueSome v }
    static member inline _dataSignals (name: string, v: string) = { Name = $"data-signals:{name}"; Value = ValueSome v }
    static member inline _dataStyle (prop: string, v: string) = { Name = $"data-style:{prop}"; Value = ValueSome v }
    static member inline _dataText (v: string) = { Name = "data-text"; Value = ValueSome v }

    // Pro attributes
    static member inline _dataAnimate (v: string) = { Name = "data-animate"; Value = ValueSome v }
    static member inline _dataCustomValidity (v: string) = { Name = "data-custom-validity"; Value = ValueSome v }
    static member inline _dataOnRaf (v: string) = { Name = "data-on-raf"; Value = ValueSome v }
    static member inline _dataOnResize (v: string) = { Name = "data-on-resize"; Value = ValueSome v }
    static member inline _dataPersist () = { Name = "data-persist"; Value = ValueNone }
    static member inline _dataPersist (key: string) = { Name = $"data-persist:{key}"; Value = ValueNone }
    static member inline _dataPersist (key: string, v: string) = { Name = $"data-persist:{key}"; Value = ValueSome v }
    static member inline _dataQueryString (?v: string) = match v with Some v -> { Name = "data-query-string"; Value = ValueSome v } | None -> { Name = "data-query-string"; Value = ValueNone }
    static member inline _dataReplaceUrl (v: string) = { Name = "data-replace-url"; Value = ValueSome v }
    static member inline _dataRocket (v: string) = { Name = "data-rocket"; Value = ValueSome v }
    static member inline _dataScrollIntoView = { Name = "data-scroll-into-view"; Value = ValueNone }
    static member inline _dataViewTransition (v: string) = { Name = "data-view-transition"; Value = ValueSome v }
