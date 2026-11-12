namespace Example.Server

open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open type Html
open type Datastar

/// Consumer-owned review links. Browser/Phone own frame presentation, not destinations or state.
module AppMode =
    let private icon (name: IconName) = fveIcon { _name name; _size Size.Small }

    let windowControl id label expanded href =
        let action = (if expanded then "Restore " else "Expand ") + label

        fveLink {
            _variant LinkVariant.Outline
            _color Color.Neutral
            _size Size.Small
            _id ("fve-fixture-" + id + (if expanded then "-restorer" else "-launcher"))
            _href href
            _ariaLabel action
            _title action
            _data ("review-expand", if expanded then "false" else "true")

            if expanded then
                _data ("fve-app-mode-exit", "true")

            fveIcon {
                _name (if expanded then IconName.ArrowsPointingIn else IconName.ArrowsPointingOut)
                _variant IconVariant.Mini
            }
        }

    let controls
        (id: string)
        (label: string)
        (states: (string * string * bool) list)
        (previous: (string * string) option)
        (next: (string * string) option)
        =
        let direction (label: string) (name: IconName) (destination: (string * string) option) =
            match destination with
            | Some(destinationName, href) ->
                a {
                    _href href
                    _ariaLabel (label + ": " + destinationName)
                    _title (label + ": " + destinationName)

                    _class
                        "grid size-8 shrink-0 place-items-center rounded-md [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] [&_svg]:size-5"

                    fveIcon { _name name; _size Size.Medium }
                }
            | None ->
                span {
                    _role "link"
                    _ariaDisabled true
                    _ariaLabel ("No " + label.ToLowerInvariant() + " journey step")

                    _class
                        "grid size-8 shrink-0 place-items-center text-[var(--fve-muted-text)] opacity-50 [&_svg]:size-5"

                    fveIcon { _name name; _size Size.Medium }
                }

        nav {
            _ariaLabel "App mode controls"
            _data ("example-app-controls", "true")
            _class "flex min-w-0 max-w-full items-center gap-1 text-sm"
            direction "Previous" IconName.ArrowLeft previous

            fveDropdownMenu {
                _id (id + "-states")
                _class "min-w-0"
                _ariaLabel ("Review state: " + label)

                fveDropdownMenuTrigger {

                    _class
                        "[&_svg]:size-[var(--fve-control-icon-size,1rem)] [&_svg]:shrink-0 inline-flex min-w-0 min-h-[var(--fve-control-min-height)] items-center justify-center gap-2 rounded-[var(--fve-radius-control)] ring-1 ring-inset ring-[var(--fve-border)] bg-transparent px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)] font-medium text-[var(--fve-text)] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:hover:bg-[linear-gradient(var(--fve-hover-wash),var(--fve-hover-wash))] [&:not([readonly])]:not-disabled:not-aria-disabled:not-aria-busy:active:bg-[linear-gradient(var(--fve-pressed-wash),var(--fve-pressed-wash))] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] disabled:opacity-50"

                    _style "max-width:min(14rem,50vw);--fve-control-icon-size:1rem"

                    fun (triggerAttributes: HtmlAttribute list) ->
                        button {
                            for attribute in triggerAttributes do
                                attribute

                            span {
                                _class "min-w-0 truncate"
                                label
                            }

                            icon IconName.ChevronDown
                        }

                }

                for name, href, current in states do
                    fveDropdownMenuItem {
                        _href href
                        name

                        if current then
                            fveDropdownMenuItemTrailing { icon IconName.Check }
                    }
            }

            direction "Next" IconName.ArrowRight next

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
