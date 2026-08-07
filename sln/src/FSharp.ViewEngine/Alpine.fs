namespace FSharp.ViewEngine

type Alpine =
    static member private ModifiedName(name: string, modifiers: string list) =
        match modifiers with
        | [] -> name
        | _ ->
            let suffix = String.concat "." modifiers
            $"{name}.{suffix}"

    static member private Modified(name: string, modifiers: string list, value: string) : HtmlAttribute =
        { Name = Alpine.ModifiedName(name, modifiers); Value = ValueSome value }

    static member private Modified(name: string, modifiers: string list) : HtmlAttribute =
        { Name = Alpine.ModifiedName(name, modifiers); Value = ValueNone }

    static member inline _x (key: string, ?value: string) =
        match value with
        | Some v -> { Name = $"x-{key}"; Value = ValueSome v }
        | None -> { Name = $"x-{key}"; Value = ValueNone }

    // Core directives
    static member inline _xBind (attr: string, value: string) = { Name = $"x-bind:{attr}"; Value = ValueSome value }
    static member _xBind (attr: string, modifiers: string list, value: string) = Alpine.Modified($"x-bind:{attr}", modifiers, value)
    static member inline _xCloak = { Name = "x-cloak"; Value = ValueNone }
    static member inline _xData (value: string) = { Name = "x-data"; Value = ValueSome value }
    static member inline _xEffect (value: string) = { Name = "x-effect"; Value = ValueSome value }
    static member inline _xFor (value: string) = { Name = "x-for"; Value = ValueSome value }
    static member inline _xHtml (value: string) = { Name = "x-html"; Value = ValueSome value }
    static member inline _xId (value: string) = { Name = "x-id"; Value = ValueSome value }
    static member inline _xIf (value: string) = { Name = "x-if"; Value = ValueSome value }
    static member inline _xIgnore () = { Name = "x-ignore"; Value = ValueNone }
    static member _xIgnore (modifiers: string list) = Alpine.Modified("x-ignore", modifiers)
    static member inline _xInit (value: string) = { Name = "x-init"; Value = ValueSome value }
    static member inline _xModel (value: string) = { Name = "x-model"; Value = ValueSome value }
    static member _xModel (modifiers: string list, value: string) = Alpine.Modified("x-model", modifiers, value)
    static member inline _xModelable (value: string) = { Name = "x-modelable"; Value = ValueSome value }
    static member inline _xOn (event: string, value: string) = { Name = $"x-on:{event}"; Value = ValueSome value }
    static member _xOn (event: string, modifiers: string list, value: string) = Alpine.Modified($"x-on:{event}", modifiers, value)
    static member inline _xRef (value: string) = { Name = "x-ref"; Value = ValueSome value }
    static member inline _xShow (value: string) = { Name = "x-show"; Value = ValueSome value }
    static member _xShow (modifiers: string list, value: string) = Alpine.Modified("x-show", modifiers, value)
    static member inline _xTeleport (value: string) = { Name = "x-teleport"; Value = ValueSome value }
    static member inline _xText (value: string) = { Name = "x-text"; Value = ValueSome value }
    static member inline _xTransition () = { Name = "x-transition"; Value = ValueNone }
    static member _xTransition (modifiers: string list) = Alpine.Modified("x-transition", modifiers)
    static member inline _xTransition (phase: string, value: string) = { Name = $"x-transition:{phase}"; Value = ValueSome value }

    // Mask plugin
    static member inline _xMask (value: string) = { Name = "x-mask"; Value = ValueSome value }
    static member inline _xMaskDynamic (value: string) = { Name = "x-mask:dynamic"; Value = ValueSome value }

    // Intersect plugin
    static member inline _xIntersect (value: string) = { Name = "x-intersect"; Value = ValueSome value }
    static member _xIntersect (modifiers: string list, value: string) = Alpine.Modified("x-intersect", modifiers, value)
    static member inline _xIntersect (event: string, value: string) = { Name = $"x-intersect:{event}"; Value = ValueSome value }
    static member _xIntersect (event: string, modifiers: string list, value: string) = Alpine.Modified($"x-intersect:{event}", modifiers, value)

    // Resize plugin
    static member inline _xResize (value: string) = { Name = "x-resize"; Value = ValueSome value }
    static member _xResize (modifiers: string list, value: string) = Alpine.Modified("x-resize", modifiers, value)

    // Collapse plugin
    static member inline _xCollapse () = { Name = "x-collapse"; Value = ValueNone }
    static member _xCollapse (modifiers: string list) = Alpine.Modified("x-collapse", modifiers)

    // Focus plugin
    static member inline _xTrap (value: string) = { Name = "x-trap"; Value = ValueSome value }
    static member _xTrap (modifiers: string list, value: string) = Alpine.Modified("x-trap", modifiers, value)

    // Anchor plugin
    static member inline _xAnchor (value: string) = { Name = "x-anchor"; Value = ValueSome value }
    static member _xAnchor (modifiers: string list, value: string) = Alpine.Modified("x-anchor", modifiers, value)

    // Sort plugin
    static member inline _xSort () = { Name = "x-sort"; Value = ValueNone }
    static member inline _xSort (value: string) = { Name = "x-sort"; Value = ValueSome value }
    static member _xSort (modifiers: string list) = Alpine.Modified("x-sort", modifiers)
    static member _xSort (modifiers: string list, value: string) = Alpine.Modified("x-sort", modifiers, value)
    static member inline _xSortItem (value: string) = { Name = "x-sort:item"; Value = ValueSome value }
    static member inline _xSortGroup (value: string) = { Name = "x-sort:group"; Value = ValueSome value }
    static member inline _xSortConfig (value: string) = { Name = "x-sort:config"; Value = ValueSome value }
    static member inline _xSortHandle = { Name = "x-sort:handle"; Value = ValueNone }
    static member inline _xSortIgnore = { Name = "x-sort:ignore"; Value = ValueNone }
