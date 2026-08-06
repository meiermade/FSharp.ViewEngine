namespace Docs.Pages

open Docs.Common

module Datastar =
    let private section id title =
        [ Heading { id = id; title = title; level = 2 } ]

    let private attribute id description source =
        [ Heading { id = id; title = id; level = 3 };
          Paragraph [ Text description ];
          CodeBlock("fsharp", source) ]

    let private nodes =
        [ [ Paragraph [ Text "FSharp.ViewEngine covers all 31 attributes in the stable "; Link("Datastar 1.0.2 reference", "https://data-star.dev/reference/attributes"); Text " through the "; InlineContent.Code "Datastar"; Text " type." ] ];
          section "setup" "Setup";
          [ Paragraph [ Text "Open the "; InlineContent.Code "Datastar"; Text " type to access Datastar attributes:" ];
            CodeBlock("fsharp", """open FSharp.ViewEngine
open type Html
open type Datastar""");
            Paragraph [ Text "Modifier overloads accept an ordered list of modifier strings without the leading "; InlineContent.Code "__"; Text ". Modifier arguments follow the modifier name after a period:" ];
            CodeBlock("fsharp", """input {
    _dataBind ("query", [ "event.input" ])
    _dataOn ("input", [ "debounce.200ms" ], "@get('/search')")
}""");
            Paragraph [ Text "Keys, event names, and modifier strings become part of the HTML attribute name. Treat them as trusted application tokens; FSharp.ViewEngine does not validate or encode attribute names." ] ];

          section "core-attributes" "Core Attributes";
          attribute "data-signals" "Create or patch signals with _dataSignals. Use object syntax for multiple signals, keyed syntax for one signal, and modifiers for casing or if-missing behavior." """div {
    _dataSignals "{count: 0, name: 'World'}"
    _dataSignals ("page", [ "ifmissing" ], "1")
}""";
          attribute "data-bind" "Create a two-way binding with _dataBind. Keyed bindings are presence-only; modifiers can select a property and synchronization events." """input {
    _type "search"
    _dataBind ("query", [ "prop.value"; "event.input.change" ])
}""";
          attribute "data-computed" "Create a read-only computed signal with _dataComputed." """div {
    _dataComputed ("double", "$count * 2")
    _dataComputed "{greeting: () => 'Hello, ' + $name}"
}""";
          attribute "data-effect" "Run a side-effect expression on initialization and whenever its signal dependencies change with _dataEffect." """div { _dataEffect "console.log($count)" }""";
          attribute "data-show" "Show or hide an element based on an expression with _dataShow." """div {
    _dataShow "$count > 0"
    "Count is positive"
}""";
          attribute "data-text" "Set an element's text content reactively with _dataText." """span { _dataText "$count" }""";
          attribute "data-attr" "Set one or more HTML attributes reactively with _dataAttr." """button {
    _dataAttr ("disabled", "$loading")
    _dataAttr "{'aria-busy': $loading}"
}""";
          attribute "data-class" "Toggle one or more classes with _dataClass. Keyed modifiers can override casing behavior." """div {
    _dataClass ("font-bold", "$strong")
    _dataClass ("my-class", [ "case.camel" ], "$active")
}""";
          attribute "data-style" "Set one or more inline style properties reactively with _dataStyle." """div {
    _dataStyle ("background-color", "$error ? 'red' : 'green'")
    _dataStyle "{display: $visible ? 'flex' : 'none'}"
}""";
          attribute "data-init" "Run an expression when an attribute is initialized with _dataInit. Delay and view-transition modifiers are supported." """div {
    _dataInit ([ "delay.500ms"; "viewtransition" ], "$ready = true")
}""";
          attribute "data-on" "Handle DOM events with _dataOn. Modifier order is preserved in the generated attribute name." """button {
    _dataOn ("click", [ "once"; "prevent" ], "$count++")
    "Increment"
}""";
          attribute "data-on-intersect" "Run an expression when an element enters or exits the viewport with _dataOnIntersect." """div {
    _dataOnIntersect ([ "once"; "threshold.25" ], "$visible = true")
}""";
          attribute "data-on-interval" "Run an expression on an interval with _dataOnInterval." """div {
    _dataOnInterval ([ "duration.500ms.leading" ], "$count++")
}""";
          attribute "data-on-signal-patch" "Run an expression when signals are patched with _dataOnSignalPatch. Timing modifiers can delay, debounce, or throttle the listener." """div {
    _dataOnSignalPatch ([ "debounce.500ms" ], "console.log(patch)")
}""";
          attribute "data-on-signal-patch-filter" "Filter signal patches using include and exclude regular expressions with _dataOnSignalPatchFilter." """div {
    _dataOnSignalPatchFilter "{include: /^count$/, exclude: /temp$/}"
}""";
          attribute "data-indicator" "Create a signal that tracks in-flight fetch requests with the presence-only _dataIndicator helper." """button {
    _dataIndicator "fetching"
    _dataOn ("click", "@get('/endpoint')")
    _dataAttr ("disabled", "$fetching")
}""";
          attribute "data-ref" "Create a signal containing a reference to an element with the presence-only _dataRef helper." """input { _dataRef "searchInput" }""";
          attribute "data-json-signals" "Render signals as JSON for debugging with _dataJsonSignals. Filters and terse output are optional." """pre { _dataJsonSignals () }
pre { _dataJsonSignals ([ "terse" ], "{include: /counter/}") }""";
          attribute "data-ignore" "Prevent Datastar from processing an element tree with _dataIgnore. Use the self modifier to ignore only the element." """div { _dataIgnore () }
div { _dataIgnore [ "self" ] }""";
          attribute "data-ignore-morph" "Prevent an element and its children from being processed during morphing with _dataIgnoreMorph." """div { _dataIgnoreMorph }""";
          attribute "data-preserve-attr" "Preserve one or more existing attribute values during morphing with _dataPreserveAttr." """details {
    _open true
    _dataPreserveAttr "open class"
}""";

          section "pro-attributes" "Pro Attributes";
          [ Paragraph [ Text "Datastar Pro attributes require a "; Link("Datastar Pro license", "https://data-star.dev/pro"); Text ". They are included as convenience helpers but are not part of the free core bundle." ] ];
          attribute "data-animate" "Animate a named element attribute reactively with the keyed _dataAnimate helper." """div {
    _dataAnimate ("opacity", "$visible ? 1 : 0")
}""";
          attribute "data-custom-validity" "Set a form control's custom validation message with _dataCustomValidity." """input {
    _dataBind "email"
    _dataCustomValidity "$email.includes('@') ? '' : 'Enter a valid email'"
}""";
          attribute "data-match-media" "Keep a signal synchronized with a media query using _dataMatchMedia." """div {
    _dataMatchMedia ("is-dark", "'prefers-color-scheme: dark'")
    _dataComputed ("theme", "$isDark ? 'dark' : 'light'")
}""";
          attribute "data-on-raf" "Run an expression on every animation frame with _dataOnRaf. Throttle modifiers can limit updates." """canvas {
    _dataOnRaf ([ "throttle.10ms" ], "draw()")
}""";
          attribute "data-on-resize" "Run an expression when an element's dimensions change with _dataOnResize." """div {
    _dataOnResize ([ "debounce.10ms" ], "$width = el.offsetWidth")
}""";
          attribute "data-persist" "Persist signals to local or session storage with _dataPersist. Use _dataPersistFilter for a default-key filter object." """div { _dataPersist () }
div { _dataPersist "settings" }
div { _dataPersist ("settings", [ "session" ]) }
div { _dataPersistFilter "{include: /theme/}" }""";
          attribute "data-query-string" "Synchronize signals with query-string parameters using _dataQueryString." """div {
    _dataQueryString ([ "filter"; "history" ], "{include: /search|page/}")
}""";
          attribute "data-replace-url" "Replace the browser URL without reloading using an evaluated expression passed to _dataReplaceUrl." """div { _dataReplaceUrl "`/page${$page}`" }""";
          attribute "data-scroll-into-view" "Scroll an element into view with _dataScrollIntoView. Behavior, alignment, and focus are controlled by modifiers." """div { _dataScrollIntoView () }
div { _dataScrollIntoView [ "smooth"; "vcenter"; "focus" ] }""";
          attribute "data-view-transition" "Set an element's view-transition-name reactively with _dataViewTransition." """div { _dataViewTransition "$transitionName" }""";

          section "trusted-expressions" "Trusted Expressions";
          [ Paragraph [ Text "Datastar expressions can execute JavaScript and backend actions. Attribute values are HTML-encoded by FSharp.ViewEngine, but encoding does not make untrusted expressions safe. Build expressions from trusted application code and never interpolate untrusted input into them." ] ];

          section "complete-example" "Complete Example";
          [ Paragraph [ Text "An active search form using signals, binding, modifiers, an indicator, and a backend action:" ];
            CodeBlock("fsharp", """div {
    _dataSignals ("query", [ "ifmissing" ], "''")
    _dataIndicator "searching"

    input {
        _type "search"
        _dataBind "query"
        _dataOn ("input", [ "debounce.200ms" ], "@get('/api/search')")
    }

    span { _dataShow "$searching"; "Searching..." }
    div { _id "search-results" }
}""") ] ]
        |> List.concat

    let page =
        { id = "datastar"
          path = "/extensions/datastar"
          aliases = []
          navLabel = "Datastar"
          category = "Extensions"
          title = "Datastar"
          browserTitle = "Datastar - FSharp.ViewEngine"
          nodes = nodes }
