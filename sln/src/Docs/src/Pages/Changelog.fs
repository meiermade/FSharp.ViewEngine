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
            Paragraph [
                Text "Released changes to the ";
                InlineContent.Code "FSharp.ViewEngine";
                Text " and ";
                InlineContent.Code "FSharp.ViewEngine.Docs";
                Text " NuGet packages are recorded here. Each package follows its own calendar-oriented version sequence. Documentation-site and infrastructure deployments are not package releases."
            ];
            Heading { id = "released"; title = "Released packages"; level = 2 };
            Paragraph [ Text "Published artifacts and complete commit histories are available from "; Link("GitHub Releases", "https://github.com/meiermade/FSharp.ViewEngine/releases"); Text "." ];
            Heading { id = "fsharp-viewengine-docs-2026-8-2"; title = "FSharp.ViewEngine.Docs 2026.8.2 · August 19, 2026"; level = 3 };
            UnorderedList [
                [ Text "Added lazy Mermaid loading so diagrams introduced by Docs-managed navigation render even when the initial page did not need Mermaid." ];
                [ Text "Moved diagram initialization to Datastar "; InlineContent.Code "data-init"; Text " and deferred the Mermaid download until the first diagram needs it." ];
                [ Text "See "; Link("release docs/v2026.8.2", "https://github.com/meiermade/FSharp.ViewEngine/releases/tag/docs%2Fv2026.8.2"); Text " for the immutable source and artifacts." ]
            ];
            Heading { id = "fsharp-viewengine-docs-2026-8-1"; title = "FSharp.ViewEngine.Docs 2026.8.1 · August 18, 2026"; level = 3 };
            UnorderedList [
                [ Text "Migrated the reusable documentation components and examples to wrapper-free fragments and standardized title computation expressions." ];
                [ Text "Declared FSharp.ViewEngine 2026.8.2 as the minimum compatible Core package version." ];
                [ Text "See "; Link("release docs/v2026.8.1", "https://github.com/meiermade/FSharp.ViewEngine/releases/tag/docs%2Fv2026.8.1"); Text " for the immutable source and artifacts." ]
            ];
            Heading { id = "fsharp-viewengine-2026-8-2"; title = "FSharp.ViewEngine 2026.8.2 · August 18, 2026"; level = 3 };
            UnorderedList [
                [ Text "Added direct list, array, and sequence composition to regular element builders, including "; InlineContent.Code "yield!"; Text " support." ];
                [ Text "Added wrapper-free fragment computation expressions and standardized title construction on "; InlineContent.Code "title { ... }"; Text "." ];
                [ Text "Expanded behavioral, package-consumer, compiler-contract, documentation, and benchmark coverage." ];
                [ Text "See "; Link("release v2026.8.2", "https://github.com/meiermade/FSharp.ViewEngine/releases/tag/v2026.8.2"); Text " for the immutable source and artifacts." ]
            ];
            Heading { id = "fsharp-viewengine-docs-2026-8-0"; title = "FSharp.ViewEngine.Docs 2026.8.0 · August 13, 2026"; level = 3 };
            UnorderedList [
                [ Text "Introduced reusable article, reference, canvas, API-reference, diagram, navigation, search, theme, story, validation, and executable-specification components." ];
                [ Text "Added accessible responsive navigation, persistent color modes, source-faithful Code and Preview examples, isolated preview routes, and lifecycle-safe Prism and Mermaid rendering." ];
                [ Text "Added package verification across supported runtimes and an independent Docs release train with an explicit minimum compatible Core version." ];
                [ Text "See "; Link("release docs/v2026.8.0", "https://github.com/meiermade/FSharp.ViewEngine/releases/tag/docs%2Fv2026.8.0"); Text " for the immutable source and artifacts." ]
            ];
            Heading { id = "fsharp-viewengine-2026-8-1"; title = "FSharp.ViewEngine 2026.8.1 · August 13, 2026"; level = 3 };
            UnorderedList [
                [ Text "Added fragment composition, validated comments, and rendering targets for existing StringBuilder, TextWriter, and UTF-8 byte consumers." ];
                [ Text "Expanded XML documentation, package verification, supported-runtime tests, and benchmark coverage for the new rendering APIs." ];
                [ Text "See "; Link("release v2026.8.1", "https://github.com/meiermade/FSharp.ViewEngine/releases/tag/v2026.8.1"); Text " for the immutable source and artifacts." ]
            ];
            Heading { id = "fsharp-viewengine-2026-8-0"; title = "FSharp.ViewEngine 2026.8.0 · August 7, 2026"; level = 3 };
            UnorderedList [
                [ Text "Improved correctness, documentation, package validation, supported-runtime coverage, source debugging, and benchmark methodology." ];
                [ Text "See "; Link("release v2026.8.0", "https://github.com/meiermade/FSharp.ViewEngine/releases/tag/v2026.8.0"); Text " for the immutable source and artifacts." ]
            ];
            Heading { id = "fsharp-viewengine-2026-2-5"; title = "FSharp.ViewEngine 2026.2.5 · February 28, 2026"; level = 3 };
            UnorderedList [
                [ Text "Updated release deployment triggers and preserved the public API baseline used by package validation." ];
                [ Text "See "; Link("release v2026.2.5", "https://github.com/meiermade/FSharp.ViewEngine/releases/tag/v2026.2.5"); Text "." ]
            ];
            Heading { id = "older-releases"; title = "Older releases"; level = 3 };
            Paragraph [ Text "Earlier package releases remain available in the "; Link("complete release history", "https://github.com/meiermade/FSharp.ViewEngine/releases"); Text "." ];
          ] }
