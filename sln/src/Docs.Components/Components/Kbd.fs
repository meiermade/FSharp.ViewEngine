namespace FSharp.ViewEngine.Components

open FSharp.ViewEngine
open type Html

module internal Kbd =
    let render attributes children =
        if List.isEmpty children then invalidArg "children" "A key label is required."
        ComponentElement.render "kbd"
            [ _class "inline-flex min-h-5 min-w-5 items-center justify-center rounded-[calc(var(--fve-radius-control)*0.75)] bg-[var(--fve-surface-subtle)] px-1.5 font-mono text-[0.6875rem] font-semibold leading-none text-[var(--fve-muted-text)] ring-1 ring-inset ring-[var(--fve-border)] shadow-[0_1px_0_var(--fve-border)]" ] attributes children

    let shortcut attributes (children:HtmlElement list) =
        if List.isEmpty children then invalidArg "children" "At least one key is required."
        let content =
            children |> List.mapi (fun index child ->
                fragment {
                    if index > 0 then span { _ariaHidden true; _class "text-xs text-[var(--fve-muted-text)]"; "+" }
                    child
                })
        ComponentElement.render "span" [_class "inline-flex items-center gap-1 align-middle"] attributes content

/// <category>kbd</category>
[<AutoOpen>]
module KbdDsl =
    /// A native key label; keyboard behavior remains application-owned.
    let fveKbd = ComponentElementBuilder(Kbd.render)
    /// A group of key labels with presentational separators.
    let fveKbdShortcut = ComponentElementBuilder(Kbd.shortcut)
