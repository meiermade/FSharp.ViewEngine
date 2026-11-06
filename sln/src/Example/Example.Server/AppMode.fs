namespace Example.Server

open FSharp.ViewEngine
open Example.Components
open type Html
open type Datastar

/// Consumer-owned review links. Browser/Phone own frame presentation, not destinations or state.
module AppMode =
    let private icon path =
        raw
            $"<svg class=\"size-4 shrink-0\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" aria-hidden=\"true\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"{path}\"/></svg>"

    let windowControl id label expanded href =
        let action = (if expanded then "Restore " else "Expand ") + label

        a {
            _id ("fve-fixture-" + id + (if expanded then "-restorer" else "-launcher"))
            _href href
            _ariaLabel action
            _title action
            _data ("review-expand", if expanded then "false" else "true")

            if expanded then
                _data ("fve-app-mode-exit", "true")

            _class
                "grid size-8 shrink-0 place-items-center rounded-full focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

            span {
                _ariaHidden true

                _class (
                    "grid size-3.5 place-items-center rounded-full "
                    + (if expanded then
                           "bg-[#febc2e] text-[#995700]"
                       else
                           "bg-[#28c840] text-[#0b6523]")
                )

                if expanded then
                    raw
                        """<svg class="size-2.5" viewBox="0 0 10 10" fill="none" stroke="currentColor" stroke-width="1.2"><path d="M2 5h6"/></svg>"""
                else
                    raw
                        """<svg class="size-2.5" viewBox="0 0 10 10" fill="currentColor"><path d="M1 1h6L1 7zm8 8H3l6-6Z"/></svg>"""
            }
        }

    let controls
        (id: string)
        (label: string)
        (states: (string * string * bool) list)
        (previous: (string * string) option)
        (next: (string * string) option)
        =
        let direction (label: string) path (destination: (string * string) option) =
            match destination with
            | Some(name, href) ->
                a {
                    _href href
                    _ariaLabel (label + ": " + name)
                    _title (label + ": " + name)

                    _class
                        "grid size-8 shrink-0 place-items-center rounded-md hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] [&_svg]:size-5"

                    icon path
                }
            | None ->
                span {
                    _role "link"
                    _ariaDisabled true
                    _ariaLabel ("No " + label.ToLowerInvariant() + " journey step")

                    _class
                        "grid size-8 shrink-0 place-items-center text-[var(--fve-muted-text)] opacity-50 [&_svg]:size-5"

                    icon path
                }

        nav {
            _ariaLabel "App mode controls"
            _data ("example-app-controls", "true")
            _class "flex min-w-0 max-w-full items-center gap-1 text-sm"
            direction "Previous" "M19.5 12h-15m0 0 6.75 6.75M4.5 12l6.75-6.75" previous

            fveDropdownMenu {
                _id (id + "-states")
                _class "min-w-0"
                _ariaLabel ("Review state: " + label)

                fveDropdownMenuTrigger {

                    _class
                        "[&_svg]:size-[var(--fve-control-icon-size,1rem)] [&_svg]:shrink-0 inline-flex min-w-0 min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] ring-1 ring-inset ring-[var(--fve-border)] bg-transparent px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                    _style "max-width:min(14rem,50vw);--fve-control-icon-size:1rem"

                    fun (triggerAttributes: HtmlAttribute list) ->
                        button {
                            for attribute in triggerAttributes do
                                attribute

                            span {
                                _class "min-w-0 truncate"
                                label
                            }

                            icon "m6 9 6 6 6-6"
                        }

                }

                for name, href, current in states do
                    fveDropdownMenuItem {
                        _href href
                        name

                        if current then
                            fveDropdownMenuItemTrailing { icon "m4.5 12.75 6 6 9-13.5" }
                    }
            }

            direction "Next" "M4.5 12h15m0 0-6.75-6.75M19.5 12l-6.75 6.75" next

            noscript {
                for name, href, _ in states do
                    a {
                        _href href
                        _class "px-2 text-sm underline"
                        name
                    }
            }
        }

    /// The frame stays at the same DOM position when its review chrome expands.
    let fixture (id: string) (expanded: bool) (content: HtmlElement) =
        div {
            _id "spec-review-fixture"
            _data ("review-fixture", id)
            _data ("review-expanded", if expanded then "true" else "false")

            if expanded then
                _data ("fve-app-mode-root", "true")

            _class (
                (Layout.theme
                 |> ComponentsTheme.withDensity Density.Compact
                 |> ComponentsTheme.withControlSize Size.Small
                 |> ComponentsTheme.className)
                + (if expanded then
                       " fixed inset-0 z-[100] h-dvh min-w-0"
                   else
                       " min-w-0")
            )

            content
        }
