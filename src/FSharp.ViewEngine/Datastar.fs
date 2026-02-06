namespace FSharp.ViewEngine

type Datastar =
    // Generic data-* attribute
    static member inline _ds (key: string, value: string) = { Name = $"data-{key}"; Value = ValueSome value }
    static member inline _ds (key: string) = { Name = $"data-{key}"; Value = ValueNone }

    // Core attributes
    static member inline _dsAttr (name: string, v: string) = { Name = $"data-attr:{name}"; Value = ValueSome v }
    static member inline _dsBind (name: string) = { Name = $"data-bind:{name}"; Value = ValueNone }
    static member inline _dsBind (name: string, v: string) = { Name = $"data-bind:{name}"; Value = ValueSome v }
    static member inline _dsClass (name: string, v: string) = { Name = $"data-class:{name}"; Value = ValueSome v }
    static member inline _dsComputed (name: string, v: string) = { Name = $"data-computed:{name}"; Value = ValueSome v }
    static member inline _dsEffect (v: string) = { Name = "data-effect"; Value = ValueSome v }
    static member inline _dsIgnore = { Name = "data-ignore"; Value = ValueNone }
    static member inline _dsIgnoreMorph = { Name = "data-ignore-morph"; Value = ValueNone }
    static member inline _dsIndicator (name: string) = { Name = $"data-indicator:{name}"; Value = ValueNone }
    static member inline _dsIndicator (name: string, v: string) = { Name = $"data-indicator:{name}"; Value = ValueSome v }
    static member inline _dsInit (v: string) = { Name = "data-init"; Value = ValueSome v }
    static member inline _dsJsonSignals (?v: string) = match v with Some v -> { Name = "data-json-signals"; Value = ValueSome v } | None -> { Name = "data-json-signals"; Value = ValueNone }
    static member inline _dsOn (event: string, v: string) = { Name = $"data-on:{event}"; Value = ValueSome v }
    static member inline _dsOnIntersect (v: string) = { Name = "data-on-intersect"; Value = ValueSome v }
    static member inline _dsOnInterval (v: string) = { Name = "data-on-interval"; Value = ValueSome v }
    static member inline _dsOnSignalPatch (v: string) = { Name = "data-on-signal-patch"; Value = ValueSome v }
    static member inline _dsOnSignalPatchFilter (v: string) = { Name = "data-on-signal-patch-filter"; Value = ValueSome v }
    static member inline _dsPreserveAttr (v: string) = { Name = "data-preserve-attr"; Value = ValueSome v }
    static member inline _dsRef (name: string) = { Name = $"data-ref:{name}"; Value = ValueNone }
    static member inline _dsRef (name: string, v: string) = { Name = $"data-ref:{name}"; Value = ValueSome v }
    static member inline _dsShow (v: string) = { Name = "data-show"; Value = ValueSome v }
    static member inline _dsSignals (name: string, v: string) = { Name = $"data-signals:{name}"; Value = ValueSome v }
    static member inline _dsStyle (prop: string, v: string) = { Name = $"data-style:{prop}"; Value = ValueSome v }
    static member inline _dsText (v: string) = { Name = "data-text"; Value = ValueSome v }

    // Pro attributes
    static member inline _dsAnimate (v: string) = { Name = "data-animate"; Value = ValueSome v }
    static member inline _dsCustomValidity (v: string) = { Name = "data-custom-validity"; Value = ValueSome v }
    static member inline _dsOnRaf (v: string) = { Name = "data-on-raf"; Value = ValueSome v }
    static member inline _dsOnResize (v: string) = { Name = "data-on-resize"; Value = ValueSome v }
    static member inline _dsPersist (key: string) = { Name = $"data-persist:{key}"; Value = ValueNone }
    static member inline _dsPersist (key: string, v: string) = { Name = $"data-persist:{key}"; Value = ValueSome v }
    static member inline _dsQueryString (?v: string) = match v with Some v -> { Name = "data-query-string"; Value = ValueSome v } | None -> { Name = "data-query-string"; Value = ValueNone }
    static member inline _dsReplaceUrl (v: string) = { Name = "data-replace-url"; Value = ValueSome v }
    static member inline _dsRocket (v: string) = { Name = "data-rocket"; Value = ValueSome v }
    static member inline _dsScrollIntoView = { Name = "data-scroll-into-view"; Value = ValueNone }
    static member inline _dsViewTransition (v: string) = { Name = "data-view-transition"; Value = ValueSome v }
