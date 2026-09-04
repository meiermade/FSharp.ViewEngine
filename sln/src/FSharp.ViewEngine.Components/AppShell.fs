namespace FSharp.ViewEngine.Components

open System
open FSharp.ViewEngine
open type Html
open type Datastar

[<NoEquality; NoComparison>]
type AppShellConfig<'destination when 'destination:equality> =
    private
        { id:string
          navigation:SideNavigationConfig<'destination>
          content:HtmlElement
          theme:ComponentsTheme }

[<RequireQualifiedAccess>]
module AppShell =
    let create id navigation content =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable application-shell ID is required."
        if String.Equals(id, SideNavigationView.id navigation, StringComparison.Ordinal) then
            invalidArg (nameof id) "Application-shell and side-navigation IDs must be distinct."
        { id = id
          navigation = navigation
          content = content
          theme = ComponentsTheme.sky }

    let withTheme theme config = { config with theme = theme }

    let private menuIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path fill-rule="evenodd" d="M2.75 5A.75.75 0 0 1 3.5 4.25h13a.75.75 0 0 1 0 1.5h-13A.75.75 0 0 1 2.75 5Zm0 5a.75.75 0 0 1 .75-.75h13a.75.75 0 0 1 0 1.5h-13a.75.75 0 0 1-.75-.75Zm0 5a.75.75 0 0 1 .75-.75h13a.75.75 0 0 1 0 1.5h-13a.75.75 0 0 1-.75-.75Z" clip-rule="evenodd"/></svg>"""

    let private closeIcon =
        raw """<svg viewBox="0 0 20 20" fill="currentColor" class="size-5"><path d="M5.22 5.22a.75.75 0 0 1 1.06 0L10 8.94l3.72-3.72a.75.75 0 1 1 1.06 1.06L11.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06L10 11.06l-3.72 3.72a.75.75 0 0 1-1.06-1.06L8.94 10 5.22 6.28a.75.75 0 0 1 0-1.06Z"/></svg>"""

    let render resolve (config:AppShellConfig<'destination>) =
        let openSignal = $"_app_shell_{ComponentHtml.optionToken config.id}_navigation_open"
        let navigationId = SideNavigationView.id config.navigation
        let triggerId = $"{config.id}-navigation-trigger"
        let navigationExpression = $"document.getElementById({ComponentHtml.javascriptString navigationId})"
        let triggerExpression = $"document.getElementById({ComponentHtml.javascriptString triggerId})"
        let closeAndRestore = $"${openSignal} = false, queueMicrotask(() => {triggerExpression}?.focus())"
        let openAndFocus =
            $"${openSignal} = true; queueMicrotask(() => ({navigationExpression}?.querySelector('[aria-current=page]') ?? {navigationExpression}?.querySelector('button, a[href]'))?.focus())"
        let mobileViewport = "window.matchMedia('(max-width: 1023px)').matches"
        let mobileNavigationLabel = ComponentHtml.javascriptString $"{SideNavigationView.productName config.navigation} navigation"
        let trapFocus =
            $"{mobileViewport} && ${openSignal} && evt.key == 'Tab' && (() => {{ const items = Array.from({navigationExpression}.querySelectorAll('a[href], button:not([disabled]), [tabindex]:not([tabindex=\"-1\"])')).filter(item => item.getClientRects().length); const first = items[0]; const last = items.at(-1); if (evt.shiftKey && document.activeElement == first) {{ evt.preventDefault(); last?.focus(); }} else if (!evt.shiftKey && document.activeElement == last) {{ evt.preventDefault(); first?.focus(); }} }})()"
        let closeControl =
            IconButton.create "Close navigation" closeIcon
            |> IconButton.withVariant ButtonVariant.Ghost
            |> IconButton.withClass "lg:hidden"
            |> IconButton.withAttributes [ _id $"{config.id}-navigation-close"; _dataOn ("click", closeAndRestore) ]
            |> IconButton.render
        let navigationAttributes = [
            _dataClass ("visible", $"${openSignal}")
            _dataClass ("invisible", $"!${openSignal}")
            _dataClass ("translate-x-0", $"${openSignal}")
            _dataClass ("-translate-x-full", $"!${openSignal}")
            _dataAttr ("role", $"{mobileViewport} && ${openSignal} ? 'dialog' : null")
            _dataAttr ("aria-label", $"{mobileViewport} && ${openSignal} ? {mobileNavigationLabel} : null")
            _dataAttr ("aria-modal", $"{mobileViewport} && ${openSignal} ? 'true' : null")
            _dataOn ("keydown", $"evt.key == 'Escape' && (evt.preventDefault(), {closeAndRestore}); {trapFocus}")
            _dataOn ("click", $"evt.target.closest('a[href]') && (${openSignal} = false)") ]

        div {
            _id config.id
            _class (ComponentHtml.classes [ ComponentsTheme.className config.theme; "relative flex h-dvh min-h-[36rem] overflow-hidden bg-[var(--fve-page)] text-[var(--fve-text)]" ])
            _dataSignals $"{{{openSignal}: false}}"
            _dataOn ("resize", [ "window" ], $"!{mobileViewport} && (${openSignal} = false)")
            div {
                _attr ("data-fve-app-shell-backdrop", "true")
                _ariaHidden true
                _dataClass ("visible", $"${openSignal}")
                _dataClass ("invisible", $"!${openSignal}")
                _dataClass ("opacity-100", $"${openSignal}")
                _dataClass ("opacity-0", $"!${openSignal}")
                _dataOn ("click", closeAndRestore)
                _class "fixed inset-0 z-30 invisible bg-[var(--fve-overlay-backdrop)] opacity-0 transition-opacity motion-reduce:transition-none lg:hidden"
            }
            SideNavigationView.render
                "fixed inset-y-0 left-0 z-40 flex w-[min(20rem,calc(100%-3rem))] -translate-x-full invisible flex-col border-r border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)] shadow-xl transition-transform motion-reduce:transition-none lg:static lg:visible lg:w-64 lg:translate-x-0 lg:shadow-none"
                navigationAttributes
                (Some closeControl)
                resolve
                config.navigation
            div {
                _attr ("data-fve-app-shell-content", "true")
                _dataAttr ("inert", $"${openSignal} ? true : null")
                _dataAttr ("aria-hidden", $"${openSignal} ? 'true' : null")
                _class "flex min-w-0 flex-1 flex-col"
                header {
                    _class "flex min-h-16 shrink-0 items-center gap-3 border-b border-[var(--fve-border)] bg-[var(--fve-surface)] px-4 lg:hidden"
                    IconButton.create "Open navigation" menuIcon
                    |> IconButton.withVariant ButtonVariant.Ghost
                    |> IconButton.withAttributes [
                        _id triggerId
                        _ariaControls navigationId
                        _ariaExpanded false
                        _dataAttr ("aria-expanded", $"${openSignal} ? 'true' : 'false'")
                        _dataOn ("click", openAndFocus) ]
                    |> IconButton.render
                    strong { _class "min-w-0 truncate text-sm font-semibold"; SideNavigationView.productName config.navigation }
                }
                main {
                    _class "min-h-0 min-w-0 flex-1"
                    config.content
                }
            }
        }
