namespace Docs.Pages

open Docs.Common

module Changelog =
    let page =
        { id = "changelog"
          path = "/changelog"
          aliases = []
          navLabel = "Changelog"
          category = "Project"
          title = "Changelog"
          browserTitle = "Changelog - FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "Notable changes to FSharp.ViewEngine are recorded here. The structure follows "; Link("Keep a Changelog", "https://keepachangelog.com/en/2.0.0/"); Text ". Releases use the project's existing calendar-oriented version scheme, and incompatible API changes are marked explicitly." ];
            Heading { id = "released"; title = "Released versions"; level = 2 };
            Paragraph [ Text "Published artifacts and complete commit histories are available from "; Link("GitHub Releases", "https://github.com/meiermade/FSharp.ViewEngine/releases"); Text "." ];
            Heading { id = "v2026-8-0"; title = "2026.8.0 · August 7, 2026"; level = 3 };
            UnorderedList [
                [ Text "Improved correctness, documentation, package validation, supported-runtime coverage, source debugging, and benchmark methodology." ];
                [ Text "See "; Link("release v2026.8.0", "https://github.com/meiermade/FSharp.ViewEngine/releases/tag/v2026.8.0"); Text " for the immutable source and artifacts." ]
            ];
            Heading { id = "v2026-2-5"; title = "2026.2.5 · February 28, 2026"; level = 3 };
            UnorderedList [
                [ Text "Updated release deployment triggers and preserved the public API baseline used by package validation." ];
                [ Text "See "; Link("release v2026.2.5", "https://github.com/meiermade/FSharp.ViewEngine/releases/tag/v2026.2.5"); Text "." ]
            ];
            Heading { id = "older-releases"; title = "Older releases"; level = 3 };
            Paragraph [ Text "Earlier calendar releases remain available in the "; Link("complete release history", "https://github.com/meiermade/FSharp.ViewEngine/releases"); Text "." ];
            Heading { id = "unreleased"; title = "Unreleased"; level = 2 };
            Heading { id = "added"; title = "Added"; level = 3 };
            UnorderedList [
                [ Text "Complete WAI-ARIA 1.2 attribute coverage and a generic "; InlineContent.Code "_aria"; Text " escape hatch." ];
                [ Text "Pinned WHATWG inventory checks, missing HTML elements and attributes, "; InlineContent.Code "titleBuilder"; Text ", and attribute-capable void element builders." ];
                [ Text "Complete non-deprecated HTMX 2.0.9 attribute coverage." ];
                [ Text "Complete Datastar 1.0.2 attribute coverage, including "; InlineContent.Code "_dataMatchMedia"; Text ", modifier overloads, and "; InlineContent.Code "_dataPersistFilter"; Text "." ];
                [ Text "Complete Alpine.js 3.15.12 core directive coverage and dedicated helpers for official directive-based plugins." ];
                [ Text "Complete Tailwind Plus Elements 1.0.22 coverage for all 23 custom elements and supported writable attributes." ];
                [ Text "A documented SVG 2 production subset with 21 common elements, geometry and presentation attributes, invariant numeric values, modern linking, and accessibility guidance." ];
                [ Text "Playwright end-to-end coverage for the production Docker Compose Docs image and deployed public site." ];
                [ Text "NuGet package validation that verifies the single compatibility asset, checks the public API against the 2026.2.5 baseline, and executes consumers on each supported runtime." ];
                [ Text "Portable PDB symbol packages with Source Link metadata for GitHub-hosted source debugging." ];
                [ Text "Typed F# documentation pages, registry-driven routes and navigation, direct-render tests, an analysis-first benchmark page with an accessible comparison visual and detailed appendix, and this changelog." ];
                [ Text "Opt-in "; InlineContent.Code "Benchmark"; Text " and "; InlineContent.Code "BenchmarkSmoke"; Text " FAKE targets with forwarded BenchmarkDotNet arguments." ]
            ];
            Heading { id = "changed"; title = "Changed"; level = 3 };
            UnorderedList [
                [ Strong [ Text "Breaking:" ]; Text " Attribute values are always HTML-encoded. Output that previously relied on pre-encoded values must now use the original unencoded value or an explicitly trusted raw boundary." ];
                [ Text "Numeric HTML attribute values now use invariant-culture formatting." ];
                [ Text "Documentation content is rendered directly from typed FSharp.ViewEngine nodes instead of runtime Markdown conversion." ];
                [ Text "The Docs application and primary examples now use pinned, self-hosted Datastar 1.0.2 instead of Alpine.js, while retaining Alpine and HTMX as documented library integrations." ];
                [ Strong [ Text "Breaking:" ]; Text " Datastar modifier-capable presence helpers "; InlineContent.Code "_dataIgnore"; Text " and "; InlineContent.Code "_dataScrollIntoView"; Text " now use unit for their unmodified forms." ];
                [ Strong [ Text "Breaking:" ]; Text " Alpine modifier overloads now accept ordered string lists without leading periods; transition phase arguments now precede their values." ];
                [ Strong [ Text "Breaking:" ]; Text " Renamed the Tailwind Plus Elements API from "; InlineContent.Code "Tailwind"; Text " to "; InlineContent.Code "TailwindElements"; Text " without a compatibility alias, and moved its documentation to "; InlineContent.Code "/extensions/tailwind-elements"; Text "." ];
                [ Text "The NuGet package now ships one "; InlineContent.Code "net8.0"; Text " compatibility asset while testing .NET 8, .NET 9, and .NET 10 runtimes independently." ];
                [ Text "Core and Docs now publish independently with package-specific calendar versions and tags. Docs declares a minimum compatible published Core version, while documentation-site deployment remains a separate workflow." ];
                [ Text "Preview CI now requires full solution builds, unit and Docs tests, package consumer validation, and Docker-based browser tests." ];
                [ Text "Updated BenchmarkDotNet to 0.15.8, removed redundant .NET 10 benchmark dependency references, corrected render-only profiling setup, restored process-isolated execution, expanded representative workload coverage, standardized a practical 100 ms measurement target, refreshed published results, and bounded per-thread renderer buffer retention." ];
                [ Text "Updated Expecto, Giraffe, JetBrains.Annotations, Oxpecker.ViewEngine, Serilog, Prism, cloudflared, and the Pulumi SDK/providers; refreshed compatible infrastructure locks to zero npm audit vulnerabilities." ];
                [ Text "Updated the Docker Tailwind CSS CLI baseline to 4.3.3." ]
            ];
            Heading { id = "removed"; title = "Removed"; level = 3 };
            UnorderedList [
                [ Strong [ Text "Breaking:" ]; Text " Removed the unsupported "; InlineContent.Code "_dataAnimate expression"; Text " overload. Datastar 1.0.2 requires a keyed attribute name." ];
                [ Strong [ Text "Breaking:" ]; Text " Removed key-plus-value overloads for "; InlineContent.Code "_dataBind"; Text ", "; InlineContent.Code "_dataIndicator"; Text ", and "; InlineContent.Code "_dataRef"; Text ". Datastar requires these attributes to use either a key or a value, never both." ];
                [ Strong [ Text "Breaking:" ]; Text " Removed "; InlineContent.Code "_dataRocket"; Text "; "; InlineContent.Code "data-rocket"; Text " is not part of Datastar 1.x." ];
                [ Strong [ Text "Breaking:" ]; Text " Removed Alpine's unrelated "; InlineContent.Code "_by"; Text " helper and the no-expression "; InlineContent.Code "_xOn event"; Text " overload." ];
                [ Text "Removed Markdig and copied Markdown content from the Docs application." ]
            ];
            Heading { id = "deprecated"; title = "Deprecated"; level = 3 };
            UnorderedList [
                [ InlineContent.Code "Html.portal"; Text ", because "; InlineContent.Code "portal"; Text " is not a standard HTML element." ];
                [ InlineContent.Code "_ariaDropeffect"; Text " and "; InlineContent.Code "_ariaGrabbed"; Text ", which are deprecated in WAI-ARIA 1.2." ]
            ];
            Heading { id = "datastar-migration"; title = "Datastar Migration"; level = 2 };
            Paragraph [ Text "Update Datastar call sites as follows." ];
            Heading { id = "key-data-animate"; title = "Key data-animate"; level = 3 };
            CodeBlock("fsharp", """// Before
_dataAnimate "$visible ? 1 : 0"

// After
_dataAnimate ("opacity", "$visible ? 1 : 0")""");
            Heading { id = "initialize-before-binding"; title = "Initialize Before Binding"; level = 3 };
            CodeBlock("fsharp", """// Before: the keyed value was not valid Datastar syntax
input { _dataBind ("name", "'default'") }

// After
_dataSignals ("name", "'default'")
input { _dataBind "name" }""");
            Heading { id = "remove-keyed-values"; title = "Remove Keyed Values"; level = 3 };
            CodeBlock("fsharp", """// Before
_dataIndicator ("loading", "'true'")
_dataRef ("input", "'fallback'")

// After
_dataIndicator "loading"
_dataRef "input"
""");
            Heading { id = "call-modifier-capable-presence-helpers"; title = "Call Modifier-Capable Presence Helpers"; level = 3 };
            CodeBlock("fsharp", """// Before
_dataIgnore
_dataScrollIntoView

// After
_dataIgnore ()
_dataScrollIntoView ()

// With modifiers
_dataIgnore [ "self" ]
_dataScrollIntoView [ "smooth"; "vcenter"; "focus" ]""");
            Heading { id = "replace-data-rocket"; title = "Replace data-rocket"; level = 3 };
            Paragraph [ Text "Remove "; InlineContent.Code "_dataRocket"; Text " when targeting Datastar 1.x. If intentionally rendering markup for an older Datastar release, use the generic trusted attribute escape hatch:" ];
            CodeBlock("fsharp", """_attr ("data-rocket", legacyExpression)""");
            Heading { id = "alpine-migration"; title = "Alpine Migration"; level = 2 };
            Paragraph [ Text "Update Alpine modifier and presence call sites as follows." ];
            Heading { id = "use-alpine-modifier-lists"; title = "Use Modifier Lists"; level = 3 };
            CodeBlock("fsharp", """// Before
_xModel ("name", ".lazy")
_xTrap ("open", ".noscroll")
_xAnchor ("$refs.trigger", ".bottom")

// After
_xModel ([ "lazy" ], "name")
_xTrap ([ "noscroll" ], "open")
_xAnchor ([ "bottom" ], "$refs.trigger")""");
            Heading { id = "name-transition-phases-first"; title = "Name Transition Phases First"; level = 3 };
            CodeBlock("fsharp", """// Before
_xTransition ("opacity-0", ":enter-start")

// After
_xTransition ("enter-start", "opacity-0")""");
            Heading { id = "call-x-ignore"; title = "Call x-ignore"; level = 3 };
            CodeBlock("fsharp", """// New core helper
_xIgnore ()
_xIgnore [ "self" ]""");
            Heading { id = "replace-by"; title = "Replace by"; level = 3 };
            Paragraph [ Text "The removed "; InlineContent.Code "_by"; Text " helper rendered a plain attribute that Alpine does not define. For keyed "; InlineContent.Code "x-for"; Text " templates, bind the key explicitly:" ];
            CodeBlock("fsharp", """_xBind ("key", "item.id")""");
            Heading { id = "tailwind-plus-elements-migration"; title = "Tailwind Plus Elements Migration"; level = 2 };
            Paragraph [ Text "Open the renamed type at Tailwind Plus Elements call sites:" ];
            CodeBlock("fsharp", """// Before
open type Tailwind

// After
open type TailwindElements""");
            Paragraph [ Text "No compatibility type or documentation redirect is provided. The canonical documentation route is now "; InlineContent.Code "/extensions/tailwind-elements"; Text "." ];
          ] }
