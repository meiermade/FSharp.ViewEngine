namespace FSharp.ViewEngine.Components.Documentation

open System
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Primitives
open type Datastar
open type Html

[<NoEquality; NoComparison>]
type FixtureLink = private { label:string; href:string }

[<NoEquality; NoComparison>]
type FixtureState = private { label:string; href:string; current:bool }

[<RequireQualifiedAccess>]
type private FixtureSurface =
    | Viewport
    | Phone

[<NoEquality; NoComparison>]
type FixtureConfig =
    private
        { id:string
          label:string
          launchHref:string
          embeddedContent:HtmlElement
          fullscreenContent:HtmlElement
          surface:FixtureSurface
          previous:FixtureLink option
          next:FixtureLink option
          states:FixtureState list }

[<NoEquality; NoComparison>]
type AppMode = private { frameId:string; exitHref:string }

/// Selects whether a Documentation Fixture is embedded in its page or owns the fullscreen document body.
type FixtureRenderMode =
    | Embedded
    | Fullscreen of AppMode

[<RequireQualifiedAccess>]
module FixtureLink =
    let create label href =
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A Fixture link label is required."
        if String.IsNullOrWhiteSpace href then invalidArg (nameof href) "A Fixture link destination is required."
        { label = label; href = href }

    let internal label (value:FixtureLink) = value.label
    let internal href (value:FixtureLink) = value.href

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

[<RequireQualifiedAccess>]
module AppMode =
    let create frameId exitHref =
        if String.IsNullOrWhiteSpace frameId then invalidArg (nameof frameId) "An App-mode fixture ID is required."
        if String.IsNullOrWhiteSpace exitHref then invalidArg (nameof exitHref) "An App-mode exit destination is required."
        { frameId = frameId; exitHref = exitHref }

    let internal frameId (value:AppMode) = value.frameId
    let internal exitHref (value:AppMode) = value.exitHref

[<RequireQualifiedAccess>]
module Fixture =
    let private validate id label launchHref =
        if String.IsNullOrWhiteSpace id then invalidArg (nameof id) "A stable Fixture ID is required."
        if String.IsNullOrWhiteSpace label then invalidArg (nameof label) "A Fixture label is required."
        if String.IsNullOrWhiteSpace launchHref then invalidArg (nameof launchHref) "A Fixture launch destination is required."

    let private configured id label launchHref embeddedContent fullscreenContent surface =
        validate id label launchHref
        { id = id
          label = label
          launchHref = launchHref
          embeddedContent = embeddedContent
          fullscreenContent = fullscreenContent
          surface = surface
          previous = None
          next = None
          states = [] }

    /// Creates a generic viewport fixture whose embedded and fullscreen content are identical.
    let create id label launchHref content =
        configured id label launchHref content content FixtureSurface.Viewport

    /// Creates a Browser fixture. The embedded view includes browser chrome; fullscreen mode renders only product content.
    let browser id label launchHref (value:BrowserConfig) =
        configured id label launchHref (Browser.render value) (Browser.fullscreenContent value) FixtureSurface.Viewport

    /// Creates a Phone fixture. Fullscreen mode retains the device treatment on wider viewports and becomes edge-to-edge on small screens.
    let phone id label launchHref (value:PhoneConfig) =
        configured id label launchHref (Phone.render value) (Phone.fullscreenContent value) FixtureSurface.Phone

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
        div {
            _attr ("data-fve-fixture", "true")
            _attr ("data-fve-fixture-id", value.id)
            _class "relative"
            a {
                _id launcherElementId
                _href (value.launchHref |> appModeHref value.id |> withQueryParameter "fveAppTransition" "enter")
                _attr ("data-fve-app-mode-launch", "true")
                _dataInit $"setTimeout(() => {{ const url = new URL(location.href); if (url.searchParams.get('fveAppReturn') === '{value.id}') {{ el.focus(); url.searchParams.delete('fveAppReturn'); url.searchParams.delete('fveAppTransition'); history.replaceState(null, '', url.pathname + url.search + url.hash) }} }}, 0)"
                _ariaLabel $"Open {value.label} in App mode"
                _title $"Open {value.label} in App mode"
                _class "absolute top-3 right-3 z-2 grid size-8 cursor-pointer place-items-center rounded-md border border-[var(--fve-border)] bg-[var(--fve-surface)] text-[var(--fve-text)] shadow-sm hover:bg-[var(--fve-surface-hover)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] [&>svg]:size-4"
                raw """<svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M7.25 3.75h-3.5v3.5M12.75 3.75h3.5v3.5M7.25 16.25h-3.5v-3.5M12.75 16.25h3.5v-3.5"/></svg>"""
            }
            value.embeddedContent
        }

    let internal id (value:FixtureConfig) = value.id
    let internal label (value:FixtureConfig) = value.label
    let internal fullscreenContent (value:FixtureConfig) = value.fullscreenContent
    let internal surface (value:FixtureConfig) = match value.surface with FixtureSurface.Viewport -> "browser" | FixtureSurface.Phone -> "phone"
    let internal previous (value:FixtureConfig) = value.previous
    let internal next (value:FixtureConfig) = value.next
    let internal states (value:FixtureConfig) = value.states
