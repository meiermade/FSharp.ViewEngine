// Retained only for legacy catalog compositions; never distributed by fve.
namespace FSharp.ViewEngine.Components.Templates

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Datastar
open type Html

/// <category>fixture</category>
[<NoEquality; NoComparison>]
type FixtureLink = private { label:string; href:string }

/// <category>fixture</category>
[<NoEquality; NoComparison>]
type FixtureState = private { label:string; href:string; current:bool }

/// <category>fixture</category>
[<NoEquality; NoComparison>]
type FixtureConfig =
    private
        { id:string
          label:string
          launchHref:string
          embeddedContent:HtmlElement
          fullscreenContent:HtmlElement
          previous:FixtureLink option
          next:FixtureLink option
          states:FixtureState list }

/// <category>fixture</category>
[<NoEquality; NoComparison>]
type AppMode = private { frameId:string; exitHref:string }

/// <summary>
/// Selects whether a Fixture is embedded in its page or owns the fullscreen document body.
/// </summary>
/// <category>fixture</category>
type FixtureRenderMode =
    | Embedded
    | Fullscreen of AppMode

/// <category>fixture</category>
[<RequireQualifiedAccess>]
module FixtureLink =
    let create label href =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A Fixture link label is required."
        if String.IsNullOrWhiteSpace href then invalidArg (nameof href) "A Fixture link destination is required."
        { label = label; href = href }

    let internal label (value:FixtureLink) = value.label
    let internal href (value:FixtureLink) = value.href

/// <category>fixture</category>
[<RequireQualifiedAccess>]
module FixtureState =
    let create label href =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A Fixture state label is required."
        if String.IsNullOrWhiteSpace href then invalidArg (nameof href) "A Fixture state destination is required."
        { label = label; href = href; current = false }

    let current (state:FixtureState) = { state with current = true }
    let internal label (value:FixtureState) = value.label
    let internal href (value:FixtureState) = value.href
    let internal isCurrent (value:FixtureState) = value.current

/// <category>fixture</category>
[<RequireQualifiedAccess>]
module AppMode =
    let create frameId exitHref =
        if String.IsNullOrWhiteSpace frameId then invalidArg (nameof frameId) "An App-mode fixture ID is required."
        if String.IsNullOrWhiteSpace exitHref then invalidArg (nameof exitHref) "An App-mode exit destination is required."
        { frameId = frameId; exitHref = exitHref }

    let internal frameId (value:AppMode) = value.frameId
    let internal exitHref (value:AppMode) = value.exitHref

/// <category>fixture</category>
[<RequireQualifiedAccess>]
module Fixture =
    let private validate id label launchHref =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable Fixture ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A Fixture label is required."
        if String.IsNullOrWhiteSpace launchHref then invalidArg (nameof launchHref) "A Fixture launch destination is required."

    /// Accepts arbitrary HTML. Embedded and App-mode content are identical unless explicitly overridden.
    let create id label launchHref (content:HtmlElement) =
        validate id label launchHref
        { id = id
          label = label
          launchHref = launchHref
          embeddedContent = content
          fullscreenContent = content
          previous = None
          next = None
          states = [] }

    /// Retained legacy catalog content override.
    let withFullscreenContent content (value:FixtureConfig) = { value with fullscreenContent = content }

    let withPrevious (previous:FixtureLink) (value:FixtureConfig) = { value with previous = Some previous }
    let withNext (next:FixtureLink) (value:FixtureConfig) = { value with next = Some next }

    let withStates (states:FixtureState list) (value:FixtureConfig) =
        if states |> List.filter _.current |> List.length > 1 then invalidArg (nameof states) "At most one Fixture state can be current."
        { value with states = states }

    let private withQueryParameter key (parameterValue:string) (href:string) =
        let hashIndex = href.IndexOf('#')
        let pathAndQuery, fragment =
            if hashIndex < 0 then href, ""
            else href.Substring(0, hashIndex), href.Substring(hashIndex)
        let separator = if pathAndQuery.Contains('?') then "&" else "?"
        $"{pathAndQuery}{separator}{key}={Uri.EscapeDataString parameterValue}{fragment}"

    let internal appModeHref frameId href =
        match Uri.TryCreate(href, UriKind.Absolute) with
        | true, absolute when absolute.Scheme = Uri.UriSchemeHttp || absolute.Scheme = Uri.UriSchemeHttps -> href
        | _ -> href |> withQueryParameter "fveAppMode" "app" |> withQueryParameter "fveAppFrame" frameId

    let internal returnFocusHref frameId href =
        href
        |> withQueryParameter "fveAppReturn" frameId
        |> withQueryParameter "fveAppTransition" "exit"

    let internal launcherId value = $"fve-fixture-{value.id}-launcher"

    let render value =
        let launcherElementId = launcherId value
        let frameIdExpression = ComponentHtml.javascriptString value.id
        div {
            _attr ("data-fve-fixture", "true")
            _attr ("data-fve-fixture-id", value.id)
            _class "relative"
            a {
                _id launcherElementId
                _href (value.launchHref |> appModeHref value.id |> withQueryParameter "fveAppTransition" "enter")
                _attr ("data-fve-app-mode-launch", "true")
                _dataInit $"setTimeout(() => {{ const url = new URL(location.href); if (url.searchParams.get('fveAppReturn') === {frameIdExpression}) {{ el.focus(); url.searchParams.delete('fveAppReturn'); url.searchParams.delete('fveAppTransition'); history.replaceState(null, '', url.pathname + url.search + url.hash) }} }}, 0)"
                _ariaLabel $"Open {value.label} in App mode"
                _title $"Open {value.label} in App mode"
                _class "absolute top-3 right-3 z-2 grid size-8 cursor-pointer place-items-center rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)] shadow-sm hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] [&>svg]:size-4"
                raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M7.25 3.75h-3.5v3.5M12.75 3.75h3.5v3.5M7.25 16.25h-3.5v-3.5M12.75 16.25h3.5v-3.5"/></svg>"""
            }
            value.embeddedContent
        }

    /// Underlined, keyboard-accessible fixture states; unrelated diagrams/rules remain outside these tabs.
    let tabs id label selectedId (fixtures:FixtureConfig list) =
        div {
            Tabs.create id label (fixtures |> List.map (fun fixture -> TabItem.create fixture.id fixture.label (render fixture)))
            |> Tabs.withVariant TabsVariant.Underlined |> Tabs.withSelected selectedId |> Tabs.render
            noscript {
                nav {
                    _ariaLabel (label+" without JavaScript")
                    _class "mt-4 flex flex-wrap gap-3"
                    for fixture in fixtures do
                        a { _href (appModeHref fixture.id fixture.launchHref); _class "text-sm text-[var(--fve-brand-text)] underline"; "Open "+fixture.label }
                }
            }
        }

    let private reviewIcon path =
        raw $"<svg class=\"size-4 shrink-0\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" aria-hidden=\"true\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"{path}\"/></svg>"

    /// Full-viewport product content with a compact floating review strip. The consumer owns routing and preference persistence.
    let renderAppMode (exitHref:string) (value:FixtureConfig) =
        if String.IsNullOrWhiteSpace exitHref then invalidArg (nameof exitHref) "An App-mode exit destination is required."
        let token = ComponentHtml.signalToken value.id
        let dock = $"_fixture_{token}_dock_top"
        let appearance = $"_fixture_{token}_appearance"
        let dark = $"_fixture_{token}_dark"
        let controlsId = $"fve-fixture-{value.id}-review"
        let iconLinkClass = "grid size-8 shrink-0 place-items-center rounded-[var(--fve-radius-control)] text-[var(--fve-text)] no-underline hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"
        let direction (label:string) (icon:HtmlElement) (link:FixtureLink option) =
            match link with
            | Some (destination:FixtureLink) ->
                a { _href destination.href; _ariaLabel (label+": "+destination.label); _title (label+": "+destination.label); _class iconLinkClass; icon }
            | None ->
                span { _ariaDisabled true; _ariaLabel ("No "+label.ToLowerInvariant()+" workflow"); _class "grid size-8 shrink-0 place-items-center text-[var(--fve-muted-text)]"; icon }
        let initialize = String.concat "; " [
            $"${dock} = new URL(location.href).searchParams.get('fveAppDock') == 'top'"
            "el._fveFixtureAppearance?.disconnect()"
            $"const synchronize = () => {{ const root = document.documentElement; const mode = root.dataset.colorMode; ${appearance} = ['system', 'light', 'dark'].includes(mode) ? mode : (root.classList.contains('dark') ? 'dark' : 'light'); ${dark} = root.classList.contains('dark') }}"
            "const observer = new MutationObserver(records => { if (!el.isConnected) { observer.disconnect(); return }; if (records.some(record => record.target === document.documentElement && record.type === 'attributes')) synchronize() })"
            // Watch removal too so this component-owned observer disconnects after a page morph.
            "observer.observe(document.documentElement, {attributes: true, attributeFilter: ['class', 'data-color-mode'], childList: true, subtree: true})"
            "el._fveFixtureAppearance = observer"
            "synchronize()" ]
        let hasWorkflow = value.previous.IsSome || value.next.IsSome
        let currentLabel = value.states |> List.tryFind _.current |> Option.map _.label |> Option.defaultValue "Choose state"
        let themeIcon = fragment {
            span { _dataShow $"${appearance} == 'system'"; reviewIcon "M9 17.25v3m6-3v3M6 20.25h12M4.5 3.75h15A1.5 1.5 0 0 1 21 5.25v10.5a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 15.75V5.25a1.5 1.5 0 0 1 1.5-1.5Z" }
            span { _dataShow $"${appearance} == 'light'"; _style "display:none"; reviewIcon "M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z" }
            span { _dataShow $"${appearance} == 'dark'"; _style "display:none"; reviewIcon "M21.752 15.002A9.718 9.718 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 2.25 12c0 5.385 4.365 9.75 9.75 9.75a9.753 9.753 0 0 0 9.752-6.748Z" }
        }
        let themes : DropdownMenuItem<unit> list = [
            for mode,label in ["system","System";"light","Light";"dark","Dark"] do
                let choose = $"${appearance} = '{mode}'; const dark = '{mode}' == 'dark' || ('{mode}' == 'system' && matchMedia('(prefers-color-scheme: dark)').matches); const root = document.documentElement; root.classList.toggle('dark', dark); root.style.colorScheme = dark ? 'dark' : 'light'; root.dataset.colorMode = '{mode}'; window.dispatchEvent(new CustomEvent('fve-fixture-appearance', {{detail: '{mode}'}}))"
                DropdownMenuItem.radio choose label |> DropdownMenuItem.withCheckedExpression $"${appearance} == '{mode}'" ]
        div {
            _id "fve-app-mode-root"; _tabindex -1; _attr ("data-fve-app-mode-root", "true")
            _attr ("data-fve-fixture-id", value.id)
            _class "min-h-dvh w-full bg-[var(--fve-background)] text-[var(--fve-text)] outline-none"
            value.fullscreenContent
            nav {
                _id controlsId; _ariaLabel "App mode controls"; _attr ("data-fve-app-mode-controls", "true")
                _attr ("data-fve-app-dock", "bottom"); _attr ("data-fve-color-mode", "dark")
                _dataSignals $"{{{dock}: false, {appearance}: 'system', {dark}: false}}"
                _dataInit initialize
                _dataOn ("pagehide__window", "el._fveFixtureAppearance?.disconnect()")
                _dataAttr ("data-fve-app-dock", $"${dock} ? 'top' : 'bottom'")
                _dataAttr ("data-fve-color-mode", $"${dark} ? 'light' : 'dark'")
                // Preserve the review envelope before either native or host-enhanced link activation.
                _dataOn ("click__capture", $"const link = evt.target.closest?.('a[href]'); if (link && el.contains(link) && !link.hasAttribute('data-fve-app-mode-exit')) {{ const target = new URL(link.href); if (target.origin == location.origin) {{ target.searchParams.set('fveAppDock', ${dock} ? 'top' : 'bottom'); link.href = target.href }} }}")
                _class ((ComponentsTheme.sky |> ComponentsTheme.withDensity Density.Compact |> ComponentsTheme.withControlSize ControlSize.Small |> ComponentsTheme.className)+" fixed right-[max(0.75rem,env(safe-area-inset-right))] bottom-[max(0.5rem,env(safe-area-inset-bottom))] z-[110] flex min-h-10 max-w-[calc(100vw-1rem)] flex-wrap items-center justify-end gap-0.5 rounded-[var(--fve-radius-panel)] border border-[var(--fve-border)] bg-[color-mix(in_srgb,var(--fve-surface)_92%,transparent)] p-1 text-sm text-[var(--fve-text)] shadow-xl backdrop-blur-sm data-[fve-app-dock=top]:top-[max(0.5rem,env(safe-area-inset-top))] data-[fve-app-dock=top]:bottom-auto")
                if hasWorkflow then direction "Previous" (reviewIcon "m15 18-6-6 6-6") value.previous
                if not value.states.IsEmpty then
                    DropdownMenu.create (controlsId+"-state") ("Review state: "+currentLabel)
                    |> DropdownMenu.withTrigger (DropdownMenuTrigger.content (fragment {
                        span { _class "min-w-0 truncate"; currentLabel }
                        reviewIcon "m8.25 9.75 3.75 3.75 3.75-3.75" }))
                    |> DropdownMenu.withContent (value.states |> List.map (fun state ->
                        let item = DropdownMenuItem.link state.href state.label
                        if state.current then item |> DropdownMenuItem.withTrailing (reviewIcon "m4.5 12.75 6 6 9-13.5") else item))
                    |> DropdownMenu.withinGroup "relative inline-flex min-w-0 max-w-[min(14rem,40vw)]" "max-w-full gap-2 px-2! py-0! ring-0! shadow-none!"
                    |> DropdownMenu.render id
                else span { _class "max-w-40 truncate px-2 text-sm"; value.label }
                if hasWorkflow then direction "Next" (reviewIcon "m9 6 6 6-6 6") value.next
                span { _ariaHidden true; _class "mx-1 h-5 w-px shrink-0 bg-[var(--fve-border)]" }
                DropdownMenu.create (controlsId+"-theme") "Choose theme"
                |> DropdownMenu.withTrigger (DropdownMenuTrigger.icon themeIcon)
                |> DropdownMenu.withContent themes |> DropdownMenu.render (fun () -> "")
                Button.create (ButtonContent.Icon ("Move App mode controls to top", fragment {
                    span { _dataShow $"!${dock}"; reviewIcon "M12 19.5v-15m-6 6 6-6 6 6" }
                    span { _dataShow $"${dock}"; _style "display:none"; reviewIcon "M12 4.5v15m6-6-6 6-6-6" } }))
                |> Button.withSize ControlSize.Small |> Button.withVariant ButtonVariant.Ghost
                |> Button.withAttributes [
                    _title "Move App mode controls to top"
                    _dataAttr ("aria-label", $"${dock} ? 'Move App mode controls to bottom' : 'Move App mode controls to top'")
                    _dataAttr ("title", $"${dock} ? 'Move App mode controls to bottom' : 'Move App mode controls to top'")
                    _dataOn ("click", $"${dock} = !${dock}; const url = new URL(location.href); url.searchParams.set('fveAppDock', ${dock} ? 'top' : 'bottom'); history.replaceState(history.state, '', url.pathname + url.search + url.hash)") ]
                |> Button.render
                a {
                    _href (returnFocusHref value.id exitHref); _attr ("data-fve-app-mode-exit", "true")
                    _ariaLabel "Exit App mode"; _title "Exit App mode"; _class iconLinkClass
                    reviewIcon "m6 6 12 12M6 18 18 6"
                }
                noscript {
                    for state in value.states do a { _href state.href; _class "px-2 text-sm underline"; state.label }
                }
            }
        }

    let internal id (value:FixtureConfig) = value.id
    let internal label (value:FixtureConfig) = value.label
    let internal fullscreenContent (value:FixtureConfig) = value.fullscreenContent
    let internal previous (value:FixtureConfig) = value.previous
    let internal next (value:FixtureConfig) = value.next
    let internal states (value:FixtureConfig) = value.states
