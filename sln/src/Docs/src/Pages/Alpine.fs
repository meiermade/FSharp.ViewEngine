namespace Docs.Pages

open Docs.Common

module Alpine =
    let private section id title =
        [ Heading { id = id; title = title; level = 2 } ]

    let private directive id description source =
        [ Heading { id = id; title = id; level = 3 };
          Paragraph [ Text description ];
          CodeBlock("fsharp", source) ]

    let private nodes =
        [ [ Paragraph [ Text "FSharp.ViewEngine covers all 18 core directives in "; Link("Alpine.js 3.15.12", "https://github.com/alpinejs/alpine/releases/tag/v3.15.12"); Text " and provides dedicated helpers for official plugins that expose HTML directives." ] ];
          section "setup" "Setup";
          [ Paragraph [ Text "Open the "; InlineContent.Code "Alpine"; Text " type to access Alpine directives:" ];
            CodeBlock("fsharp", """open FSharp.ViewEngine
open type Html
open type Alpine""");
            Paragraph [ Text "Modifier overloads accept ordered strings without leading periods. Directive arguments, such as event names and transition phases, are separate parameters:" ];
            CodeBlock("fsharp", """button {
    _xOn ("keydown", [ "enter"; "prevent"; "once" ], "save()")
}
div { _xTransition ("enter-start", "opacity-0 scale-90") }""");
            Paragraph [ Text "Directive arguments and modifier strings become part of the HTML attribute name. Treat them as trusted application tokens; FSharp.ViewEngine does not validate or encode attribute names." ] ];

          section "core-directives" "Core Directives";
          directive "x-data" "Initialize a component's reactive state with _xData." """div {
    _xData "{ open: false, count: 0 }"
    button { _xOn ("click", "count++"); "Increment" }
    span { _xText "count" }
}""";
          directive "x-init" "Run an expression when an element is initialized with _xInit." """div {
    _xData "{ users: [] }"
    _xInit "users = await (await fetch('/api/users')).json()"
}""";
          directive "x-show" "Toggle an element's display with _xShow. The important modifier applies display: none !important." """div { _xShow ([ "important" ], "open"); "Content" }""";
          directive "x-bind" "Bind an HTML attribute or property with _xBind. Use it for keyed x-for iterations instead of a plain by attribute." """template {
    _xFor "item in items"
    _xBind ("key", "item.id")
    li { _xText "item.label" }
}""";
          directive "x-on" "Handle browser events with _xOn. Modifiers are emitted in their supplied order." """button {
    _xOn ("click", [ "prevent"; "once" ], "save()")
    "Save"
}""";
          directive "x-text" "Set textContent from an expression with _xText." """span { _xText "message" }""";
          directive "x-html" "Set innerHTML from an expression with _xHtml." """div { _xHtml "trustedHtml" }""";
          [ Paragraph [ Strong [ Text "Security:" ]; Text " Alpine inserts "; InlineContent.Code "x-html"; Text " content as HTML. Only use HTML created by trusted application code; never pass unsanitized user content." ] ];
          directive "x-model" "Create two-way form bindings with _xModel. Modifiers use ordered strings." """input {
    _type "search"
    _xModel ([ "lazy"; "debounce.500ms" ], "query")
}""";
          directive "x-modelable" "Expose a component property to an outer x-model binding with _xModelable." """div {
    _xData "{ value: 0 }"
    _xModelable "value"
}""";
          directive "x-for" "Repeat a template with _xFor. Alpine requires x-for on a template with one root child." """template {
    _xFor "item in items"
    _xBind ("key", "item.id")
    li { _xText "item.label" }
}""";
          directive "x-transition" "Apply Alpine's transition helper or explicit phase classes with _xTransition." """div {
    _xShow "open"
    _xTransition [ "duration.500ms"; "opacity" ]
}
div { _xTransition ("leave-end", "opacity-0 scale-90") }""";
          directive "x-effect" "Re-run an expression whenever its reactive dependencies change with _xEffect." """div { _xEffect "console.log(count)" }""";
          directive "x-ignore" "Prevent Alpine from initializing an element tree with _xIgnore. Use self to ignore only the element." """div { _xIgnore () }
div { _xIgnore [ "self" ] }""";
          directive "x-ref" "Name an element for access through $refs with _xRef." """input { _xRef "searchInput" }
button { _xOn ("click", "$refs.searchInput.focus()"); "Focus" }""";
          directive "x-cloak" "Hide an element until Alpine initializes with the presence-only _xCloak directive." """div { _xCloak; "Hidden until Alpine loads" }""";
          directive "x-teleport" "Move a template to the first element matching a CSS selector with _xTeleport." """template {
    _xTeleport "body"
    div { "Modal" }
}""";
          directive "x-if" "Conditionally add or remove a template's child from the DOM with _xIf." """template {
    _xIf "open"
    div { "Visible while open" }
}""";
          directive "x-id" "Create a scoped set of generated IDs with _xId." """div { _xId "['dropdown']" }""";

          section "plugin-directives" "Plugin Directives";
          [ Paragraph [ Text "Plugin helpers only render attributes; applications must install and register the corresponding Alpine package before Alpine starts. See the "; Link("official plugin documentation", "https://alpinejs.dev/plugins/"); Text " for script and module setup." ] ];
          directive "x-mask" "The Mask plugin formats input as the user types. Use _xMask for a fixed mask or _xMaskDynamic for an expression." """input { _xMask "99/99/9999" }
input { _xMaskDynamic "$money($input)" }""";
          directive "x-intersect" "The Intersect plugin runs an expression when an element enters or leaves the viewport." """div {
    _xIntersect ([ "once"; "threshold.50" ], "visible = true")
}
div { _xIntersect ("leave", [ "full" ], "visible = false") }""";
          directive "x-resize" "The Resize plugin runs an expression when an element or document changes size." """div { _xResize "width = $width" }
div { _xResize ([ "document" ], "viewportWidth = $width") }""";
          directive "x-collapse" "The Collapse plugin animates an x-show element's height." """div {
    _xShow "open"
    _xCollapse [ "duration.500ms"; "min.50px" ]
}""";
          directive "x-trap" "The _xTrap helper requires Alpine's Focus plugin. Focus modifiers include inert, noscroll, noreturn, and noautofocus." """div {
    _xShow "open"
    _xTrap ([ "inert"; "noscroll" ], "open")
}""";
          [ Paragraph [ Strong [ Text "Dependency:" ]; Text " "; InlineContent.Code "x-trap"; Text " is provided by the "; Link("Focus plugin", "https://alpinejs.dev/plugins/focus"); Text ", not Alpine core." ] ];
          directive "x-anchor" "The _xAnchor helper positions an element relative to a reference and requires Alpine's Anchor plugin." """button { _xRef "trigger"; "Open" }
div {
    _xAnchor ([ "bottom-start"; "offset.10"; "fixed" ], "$refs.trigger")
}""";
          [ Paragraph [ Strong [ Text "Dependency:" ]; Text " "; InlineContent.Code "x-anchor"; Text " is provided by the "; Link("Anchor plugin", "https://alpinejs.dev/plugins/anchor"); Text ", not Alpine core." ] ];
          directive "x-sort" "The Sort plugin provides sortable containers, items, groups, configuration, handles, and ignored controls." """ul {
    _xSort ([ "ghost" ], "handleSort($item, $position)")
    _xSortGroup "tasks"
    _xSortConfig "{ animation: 150 }"

    li {
        _xSortItem "task.id"
        button { _xSortHandle; "Drag" }
        button { _xSortIgnore; "Edit" }
    }
}""";

          section "plugins-without-directives" "Plugins Without Directive Helpers";
          [ Paragraph [ Text "The Persist plugin exposes the "; InlineContent.Code "$persist"; Text " magic rather than an HTML directive, so no dedicated attribute helper is provided:" ];
            CodeBlock("fsharp", """div { _xData "{ count: $persist(0) }" }""");
            Paragraph [ Text "The Morph plugin exposes the imperative "; InlineContent.Code "Alpine.morph"; Text " API rather than an HTML directive, so it also has no dedicated attribute helper." ];
            Paragraph [ Text "Use the generic "; InlineContent.Code "_x"; Text " helper for third-party or future directives that are not yet represented:" ];
            CodeBlock("fsharp", """div { _x ("third-party", "expression") }""") ];

          section "trusted-expressions" "Trusted Expressions";
          [ Paragraph [ Text "Alpine directive values execute JavaScript expressions. FSharp.ViewEngine HTML-encodes attribute values, but encoding does not make untrusted expressions safe. Build expressions from trusted application code and do not interpolate user input into them." ] ];

          section "complete-example" "Complete Example";
          [ Paragraph [ Text "A core-only disclosure component with keyboard handling and transitions:" ];
            CodeBlock("fsharp", """div {
    _xData "{ open: false }"

    button {
        _xOn ("click", "open = !open")
        _xOn ("keydown", [ "escape"; "prevent" ], "open = false")
        _xBind ("aria-expanded", "open")
        "Toggle details"
    }

    div {
        _xShow "open"
        _xTransition [ "duration.200ms"; "opacity" ]
        "Details"
    }
}""") ] ]
        |> List.concat

    let page =
        { id = "alpine"
          path = "/extensions/alpine"
          aliases = []
          navLabel = "Alpine"
          category = "Extensions"
          title = "Alpine.js"
          browserTitle = "Alpine.js - FSharp.ViewEngine"
          nodes = nodes }
