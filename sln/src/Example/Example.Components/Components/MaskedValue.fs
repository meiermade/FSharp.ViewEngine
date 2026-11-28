namespace Example.Components

open FSharp.ViewEngine
open type Html

/// <category>input</category>
[<AutoOpen>]
module MaskedValueAttributes =
    type Html with
        static member _masked(value:bool) = Html._attr("masked", if value then "true" else "false")

module internal MaskedValue =
    let enabled attributes = ComponentElement.value "masked" attributes = Some "true"
    let mask = "••••••••"

    let action name label target (classes: string) =
        button {
            _type "button"
            _hidden true
            _ariaLabel label
            _title label
            for id in Option.toList target do _ariaControls id
            _data ("fve-masked-action", name)
            _class ("grid size-7 shrink-0 place-items-center rounded-sm text-[var(--fve-muted-text)] hover:bg-[var(--fve-hover-wash)] hover:text-[var(--fve-text)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50 " + classes)
            if name = "copy" then
                raw """<svg aria-hidden="true" class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h2"/></svg>"""
            else
                raw """<svg aria-hidden="true" class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z"/><circle cx="12" cy="12" r="3"/><path data-fve-mask-slash hidden d="M3 3l18 18"/></svg>"""
        }
