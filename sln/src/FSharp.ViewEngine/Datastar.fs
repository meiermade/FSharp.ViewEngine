namespace FSharp.ViewEngine

type Datastar =
    static member private ModifiedName(name: string, modifiers: string list) =
        match modifiers with
        | [] -> name
        | _ ->
            let suffix = String.concat "__" modifiers
            $"{name}__{suffix}"

    static member private Modified(name: string, modifiers: string list, value: string) : HtmlAttribute =
        { Name = Datastar.ModifiedName(name, modifiers); Value = ValueSome value }

    static member private Modified(name: string, modifiers: string list) : HtmlAttribute =
        { Name = Datastar.ModifiedName(name, modifiers); Value = ValueNone }

    // Core attributes
    static member inline _dataAttr (v: string) = { Name = "data-attr"; Value = ValueSome v }
    static member inline _dataAttr (name: string, v: string) = { Name = $"data-attr:{name}"; Value = ValueSome v }
    static member inline _dataBind (name: string) = { Name = $"data-bind:{name}"; Value = ValueNone }
    static member _dataBind (name: string, modifiers: string list) = Datastar.Modified($"data-bind:{name}", modifiers)
    static member inline _dataClass (v: string) = { Name = "data-class"; Value = ValueSome v }
    static member inline _dataClass (name: string, v: string) = { Name = $"data-class:{name}"; Value = ValueSome v }
    static member _dataClass (name: string, modifiers: string list, v: string) = Datastar.Modified($"data-class:{name}", modifiers, v)
    static member inline _dataComputed (v: string) = { Name = "data-computed"; Value = ValueSome v }
    static member inline _dataComputed (name: string, v: string) = { Name = $"data-computed:{name}"; Value = ValueSome v }
    static member _dataComputed (name: string, modifiers: string list, v: string) = Datastar.Modified($"data-computed:{name}", modifiers, v)
    static member inline _dataEffect (v: string) = { Name = "data-effect"; Value = ValueSome v }
    static member inline _dataIgnore () = { Name = "data-ignore"; Value = ValueNone }
    static member _dataIgnore (modifiers: string list) = Datastar.Modified("data-ignore", modifiers)
    static member inline _dataIgnoreMorph = { Name = "data-ignore-morph"; Value = ValueNone }
    static member inline _dataIndicator (name: string) = { Name = $"data-indicator:{name}"; Value = ValueNone }
    static member _dataIndicator (name: string, modifiers: string list) = Datastar.Modified($"data-indicator:{name}", modifiers)
    static member inline _dataInit (v: string) = { Name = "data-init"; Value = ValueSome v }
    static member _dataInit (modifiers: string list, v: string) = Datastar.Modified("data-init", modifiers, v)
    static member inline _dataJsonSignals (?v: string) = match v with Some v -> { Name = "data-json-signals"; Value = ValueSome v } | None -> { Name = "data-json-signals"; Value = ValueNone }
    static member _dataJsonSignals (modifiers: string list) = Datastar.Modified("data-json-signals", modifiers)
    static member _dataJsonSignals (modifiers: string list, v: string) = Datastar.Modified("data-json-signals", modifiers, v)
    static member inline _dataOn (event: string, v: string) = { Name = $"data-on:{event}"; Value = ValueSome v }
    static member _dataOn (event: string, modifiers: string list, v: string) = Datastar.Modified($"data-on:{event}", modifiers, v)
    static member inline _dataOnIntersect (v: string) = { Name = "data-on-intersect"; Value = ValueSome v }
    static member _dataOnIntersect (modifiers: string list, v: string) = Datastar.Modified("data-on-intersect", modifiers, v)
    static member inline _dataOnInterval (v: string) = { Name = "data-on-interval"; Value = ValueSome v }
    static member _dataOnInterval (modifiers: string list, v: string) = Datastar.Modified("data-on-interval", modifiers, v)
    static member inline _dataOnSignalPatch (v: string) = { Name = "data-on-signal-patch"; Value = ValueSome v }
    static member _dataOnSignalPatch (modifiers: string list, v: string) = Datastar.Modified("data-on-signal-patch", modifiers, v)
    static member inline _dataOnSignalPatchFilter (v: string) = { Name = "data-on-signal-patch-filter"; Value = ValueSome v }
    static member inline _dataPreserveAttr (v: string) = { Name = "data-preserve-attr"; Value = ValueSome v }
    static member inline _dataRef (name: string) = { Name = $"data-ref:{name}"; Value = ValueNone }
    static member _dataRef (name: string, modifiers: string list) = Datastar.Modified($"data-ref:{name}", modifiers)
    static member inline _dataShow (v: string) = { Name = "data-show"; Value = ValueSome v }
    static member inline _dataSignals (v: string) = { Name = "data-signals"; Value = ValueSome v }
    static member inline _dataSignals (name: string, v: string) = { Name = $"data-signals:{name}"; Value = ValueSome v }
    static member _dataSignals (name: string, modifiers: string list, v: string) = Datastar.Modified($"data-signals:{name}", modifiers, v)
    static member inline _dataStyle (v: string) = { Name = "data-style"; Value = ValueSome v }
    static member inline _dataStyle (prop: string, v: string) = { Name = $"data-style:{prop}"; Value = ValueSome v }
    static member inline _dataText (v: string) = { Name = "data-text"; Value = ValueSome v }

    // Pro attributes
    static member inline _dataAnimate (name: string, v: string) = { Name = $"data-animate:{name}"; Value = ValueSome v }
    static member inline _dataCustomValidity (v: string) = { Name = "data-custom-validity"; Value = ValueSome v }
    static member inline _dataMatchMedia (name: string, v: string) = { Name = $"data-match-media:{name}"; Value = ValueSome v }
    static member _dataMatchMedia (name: string, modifiers: string list, v: string) = Datastar.Modified($"data-match-media:{name}", modifiers, v)
    static member inline _dataOnRaf (v: string) = { Name = "data-on-raf"; Value = ValueSome v }
    static member _dataOnRaf (modifiers: string list, v: string) = Datastar.Modified("data-on-raf", modifiers, v)
    static member inline _dataOnResize (v: string) = { Name = "data-on-resize"; Value = ValueSome v }
    static member _dataOnResize (modifiers: string list, v: string) = Datastar.Modified("data-on-resize", modifiers, v)
    static member inline _dataPersist () = { Name = "data-persist"; Value = ValueNone }
    static member _dataPersist (modifiers: string list) = Datastar.Modified("data-persist", modifiers)
    static member inline _dataPersist (key: string) = { Name = $"data-persist:{key}"; Value = ValueNone }
    static member _dataPersist (key: string, modifiers: string list) = Datastar.Modified($"data-persist:{key}", modifiers)
    static member inline _dataPersist (key: string, v: string) = { Name = $"data-persist:{key}"; Value = ValueSome v }
    static member _dataPersist (key: string, modifiers: string list, v: string) = Datastar.Modified($"data-persist:{key}", modifiers, v)
    static member inline _dataPersistFilter (v: string) = { Name = "data-persist"; Value = ValueSome v }
    static member _dataPersistFilter (modifiers: string list, v: string) = Datastar.Modified("data-persist", modifiers, v)
    static member inline _dataQueryString (?v: string) = match v with Some v -> { Name = "data-query-string"; Value = ValueSome v } | None -> { Name = "data-query-string"; Value = ValueNone }
    static member _dataQueryString (modifiers: string list) = Datastar.Modified("data-query-string", modifiers)
    static member _dataQueryString (modifiers: string list, v: string) = Datastar.Modified("data-query-string", modifiers, v)
    static member inline _dataReplaceUrl (v: string) = { Name = "data-replace-url"; Value = ValueSome v }
    static member inline _dataScrollIntoView () = { Name = "data-scroll-into-view"; Value = ValueNone }
    static member _dataScrollIntoView (modifiers: string list) = Datastar.Modified("data-scroll-into-view", modifiers)
    static member inline _dataViewTransition (v: string) = { Name = "data-view-transition"; Value = ValueSome v }
