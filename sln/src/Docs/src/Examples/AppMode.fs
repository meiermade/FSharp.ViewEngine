namespace Docs.Examples

open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Datastar
open Model
open Ledger.Domain

/// Specification previews and compact App-mode controls.
module AppMode =
    let private icon path =
        raw $"<svg class=\"size-4 shrink-0\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" aria-hidden=\"true\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"{path}\"/></svg>"
    let fixture label href (content:HtmlElement) =
        div {
            _attr("data-example-native-navigation","true")
            _class "relative"
            a {
                _href href; _ariaLabel ("Open "+label+" in App mode"); _title ("Open "+label+" in App mode")
                _class "absolute top-3 right-3 z-2 grid size-8 place-items-center rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)] shadow-sm hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
                icon "M3.75 9V3.75H9m6 0h5.25V9m0 6v5.25H15m-6 0H3.75V15"
            }
            content
        }
    let render (query:Query) (label:string) (states:(string*string*bool) list) (previous:(string*string) option) (next:(string*string) option) (exitHref:string) (product:HtmlElement -> HtmlElement) =
        let iconLinkClass = "grid size-8 shrink-0 place-items-center rounded-[var(--fve-radius-control)] text-[var(--fve-text)] no-underline hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
        let direction (label:string) (graphic:HtmlElement) (destination:(string*string) option) =
            match destination with
            | Some(name,href) -> a { _href href; _ariaLabel (label+": "+name); _title (label+": "+name); _class iconLinkClass; graphic }
            | None -> span { _ariaDisabled true; _ariaLabel ("No "+label.ToLowerInvariant()+" workflow"); _class "grid size-8 shrink-0 place-items-center text-[var(--fve-muted-text)]"; graphic }
        let dockTop = if query.dock="top" then "true" else "false"
        let controls =
            nav {
                _id "spec-app-controls"; _ariaLabel "App mode controls"; _attr("data-example-app-controls","true")
                _attr("data-fve-app-dock",query.dock); _attr("data-fve-color-mode","dark")
                _dataSignals $"{{_example_app_dock_top: {dockTop}, _example_app_dark: false}}"
                _dataInit "$_example_app_dark = document.documentElement.classList.contains('dark'); const reserveDock = () => document.documentElement.style.setProperty('--example-app-dock-space', 'calc(' + el.getBoundingClientRect().height + 'px + max(0.5rem, env(safe-area-inset-top), env(safe-area-inset-bottom)) + 0.5rem)'); new ResizeObserver(reserveDock).observe(el); reserveDock()"
                _dataOn("fve-color-mode__window", "$_example_app_dark = document.documentElement.classList.contains('dark')")
                _dataAttr("data-fve-app-dock", "$_example_app_dock_top ? 'top' : 'bottom'")
                _dataAttr("data-fve-color-mode", "$_example_app_dark ? 'light' : 'dark'")
                _class ((Layout.theme |> ComponentsTheme.withDensity Density.Compact |> ComponentsTheme.withControlSize ControlSize.Small |> ComponentsTheme.className)+" fixed right-[max(0.75rem,env(safe-area-inset-right))] bottom-[max(0.5rem,env(safe-area-inset-bottom))] z-[110] flex min-h-10 max-w-[calc(100vw-1rem)] flex-wrap items-center justify-end gap-0.5 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[color-mix(in_srgb,var(--fve-surface)_92%,transparent)] p-1 text-sm text-[var(--fve-text)] shadow-xl backdrop-blur-sm data-[fve-app-dock=top]:top-[max(0.5rem,env(safe-area-inset-top))] data-[fve-app-dock=top]:bottom-auto")
                direction "Previous" (icon "m15 18-6-6 6-6") previous
                DropdownMenu.create "spec-app-states" ("Review state: "+label)
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.content (fragment { span { _class "min-w-0 truncate"; label }; icon "m8.25 9.75 3.75 3.75 3.75-3.75" }) |> DropdownMenuTrigger.withAttributes [_style "max-width:min(14rem,40vw);padding:0 0.5rem;box-shadow:none"])
                |> DropdownMenu.withContent (states |> List.map (fun (name,href,current) -> let item = DropdownMenuItem.link href name in if current then item |> DropdownMenuItem.withTrailing (icon "m4.5 12.75 6 6 9-13.5") else item))
                |> DropdownMenu.render id
                direction "Next" (icon "m9 6 6 6-6 6") next
                span { _ariaHidden true; _class "mx-1 h-5 w-px shrink-0 bg-[var(--fve-border)]" }
                ThemeSwitcher.create "spec-app-theme" "Choose theme" |> ThemeSwitcher.render
                Button.create (ButtonContent.Icon ("Move App mode controls to top", fragment {
                    span { _dataShow "!$_example_app_dock_top"; icon "M12 19.5v-15m-6 6 6-6 6 6" }
                    span { _dataShow "$_example_app_dock_top"; _style "display:none"; icon "M12 4.5v15m6-6-6 6-6-6" } }))
                |> Button.withSize ControlSize.Small |> Button.withVariant ButtonVariant.Ghost
                |> Button.withAttributes [
                    _title "Move App mode controls to top"
                    _dataAttr("aria-label", "$_example_app_dock_top ? 'Move App mode controls to bottom' : 'Move App mode controls to top'")
                    _dataAttr("title", "$_example_app_dock_top ? 'Move App mode controls to bottom' : 'Move App mode controls to top'")
                    _dataOn("click", "$_example_app_dock_top = !$_example_app_dock_top; const url = new URL(location.href); url.searchParams.set('fveAppDock', $_example_app_dock_top ? 'top' : 'bottom'); history.replaceState(history.state, '', url.pathname + url.search + url.hash)") ] |> Button.render
                a { _href exitHref; _attr("data-example-app-exit","true"); _ariaLabel "Exit App mode"; _title "Exit App mode"; _class iconLinkClass; icon "m6 6 12 12M6 18 18 6" }
                noscript { for name,href,_ in states do a { _href href; _class "px-2 text-sm underline"; name } }
            }
        div {
            _id "spec-app-mode-root"; _attr("data-spec-app-mode","true"); _attr("data-example-native-navigation","true")
            _class "min-h-dvh bg-[var(--fve-background)] text-[var(--fve-text)]"
            // Only the dock preference changes client-side; every page/state destination is ordinary server navigation.
            _dataOn("click__capture__window", "const link = evt.target.closest?.('a[href]'); if (link) { const url = new URL(link.href); if (url.origin == location.origin && url.pathname.startsWith('/examples/specification')) { url.searchParams.set('fveAppDock', new URL(location.href).searchParams.get('fveAppDock') ?? 'bottom'); link.href = url.href } }")
            _dataOn("submit__capture__window", "const dock = evt.target.querySelector('input[name=fveAppDock]'); if (dock) dock.value = new URL(location.href).searchParams.get('fveAppDock') ?? 'bottom'")
            // The product can include the strip in an initially open native editor dialog rather than leaving it inert outside.
            product controls
        }
