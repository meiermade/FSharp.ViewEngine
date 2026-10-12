namespace Docs.Examples

open System.Text.Json
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html
open type Datastar

/// Consumer-owned review links. Browser/Phone own frame presentation, not destinations or state.
module AppMode =
    let private icon path =
        raw $"<svg class=\"size-4 shrink-0\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" aria-hidden=\"true\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"{path}\"/></svg>"

    let windowControl id label expanded href =
        let action = (if expanded then "Restore " else "Expand ") + label
        a {
            _id ("fve-fixture-"+id+(if expanded then "-restorer" else "-launcher"))
            _href href; _ariaLabel action; _title action
            _data("review-expand", if expanded then "false" else "true")
            if expanded then _data("fve-app-mode-exit", "true")
            _class "grid size-8 shrink-0 place-items-center rounded-full focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
            span {
                _ariaHidden true
                _class ("grid size-3.5 place-items-center rounded-full " + (if expanded then "bg-[#febc2e] text-[#995700]" else "bg-[#28c840] text-[#0b6523]"))
                if expanded then raw """<svg class="size-2.5" viewBox="0 0 10 10" fill="none" stroke="currentColor" stroke-width="1.2"><path d="M2 5h6"/></svg>"""
                else raw """<svg class="size-2.5" viewBox="0 0 10 10" fill="currentColor"><path d="M1 1h6L1 7zm8 8H3l6-6Z"/></svg>"""
            }
        }

    let controls (id:string) (label:string) (states:(string*string*bool) list) (previous:(string*string) option) (next:(string*string) option) =
        let direction (label:string) path (destination:(string*string) option) =
            match destination with
            | Some(name,href) -> a {
                _href href; _ariaLabel (label+": "+name); _title (label+": "+name)
                _class "grid size-8 shrink-0 place-items-center rounded-md hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)] [&_svg]:size-5"
                icon path
              }
            | None -> span { _ariaDisabled true; _ariaLabel ("No "+label.ToLowerInvariant()+" journey step"); _class "grid size-8 shrink-0 place-items-center text-[var(--fve-muted-text)] opacity-50 [&_svg]:size-5"; icon path }
        nav {
            _ariaLabel "App mode controls"; _data("example-app-controls", "true")
            _class "flex min-w-0 max-w-full items-center gap-1 text-sm"
            direction "Previous" "M19.5 12h-15m0 0 6.75 6.75M4.5 12l6.75-6.75" previous
            DropdownMenu.create (id+"-states") ("Review state: "+label)
            |> DropdownMenu.withTrigger (DropdownMenuTrigger.content (fragment { span { _class "min-w-0 truncate"; label }; icon "m6 9 6 6 6-6" }) |> DropdownMenuTrigger.withVariant ButtonVariant.Ghost |> DropdownMenuTrigger.withAttributes [_style "max-width:min(14rem,50vw);--fve-control-icon-size:1rem"])
            |> DropdownMenu.withContent (states |> List.map (fun (name,href,current) -> let item=DropdownMenuItem.link href name in if current then item |> DropdownMenuItem.withTrailing (icon "m4.5 12.75 6 6 9-13.5") else item))
            |> DropdownMenu.render (fun href -> href)
            direction "Next" "M4.5 12h15m0 0-6.75-6.75M19.5 12l-6.75 6.75" next
            noscript { for name,href,_ in states do a { _href href; _class "px-2 text-sm underline"; name } }
        }

    /// Scroll/focus preferences are local UI state; routes and page states remain server-owned.
    let fixture (id:string) (expanded:bool) (restoreRequested:bool) (content:HtmlElement) =
        let key = JsonSerializer.Serialize ("fve-review-fixture:"+id)
        let focusId = JsonSerializer.Serialize ("fve-fixture-"+id+"-launcher")
        let restoreFocus = if expanded then "" else $"document.getElementById({focusId})?.focus({{preventScroll:true}});"
        let restorePage = if expanded then "" else "document.querySelector('[data-fve-page-scroll]')?.scrollTo(saved.page[0], saved.page[1]);"
        let restore = $"{restoreFocus} try {{ const saved = JSON.parse(sessionStorage.getItem({key}) || 'null'); if (saved) {{ {restorePage} el.querySelectorAll('[data-fve-page-scroll], [data-fve-browser-content], .fve-table-scroll').forEach((node, index) => {{ const position=saved.product[index]; if (position) node.scrollTo(position[0], position[1]) }}); }} }} catch {{}}"
        div {
            _id (if expanded then "spec-app-mode-root" else "fve-fixture-"+id)
            _data("review-fixture", id)
            if expanded then
                _data("fve-app-mode-root", "true")
                _dataOn("keydown__window", Navigation.enhancement.appModeExitAction)
            _class ((Layout.theme |> ComponentsTheme.withDensity Density.Compact |> ComponentsTheme.withControlSize ControlSize.Small |> ComponentsTheme.className)+" min-w-0")
            _dataOn("click", "const link = evt.target.closest?.('[data-review-expand]'); if (link) { try { const key = " + key + "; const product = [...el.querySelectorAll('[data-fve-page-scroll], [data-fve-browser-content], .fve-table-scroll')].map(node => [node.scrollLeft, node.scrollTop]); const saved = JSON.parse(sessionStorage.getItem(key) || 'null'); const page = document.querySelector('[data-fve-page-scroll]'); sessionStorage.setItem(key, JSON.stringify({page: link.dataset.reviewExpand === 'true' ? [page?.scrollLeft ?? 0, page?.scrollTop ?? 0] : saved?.page ?? [0,0], product})); } catch {} }")
            if expanded then
                _dataInit ("if (new URL(location.href).searchParams.get('appMode') === '1') requestAnimationFrame(() => { "+restore+" })")
                _dataOn("fve-navigation-complete__window", "if (new URL(evt.detail.previousHref).searchParams.get('appMode') !== '1') { "+restore+" }")
            elif restoreRequested then
                _dataInit ("if (new URL(location.href).searchParams.has('fveAppReturn')) requestAnimationFrame(() => { "+restore+" })")
                _dataOn("fve-navigation-complete__window", restore)
            content
        }
