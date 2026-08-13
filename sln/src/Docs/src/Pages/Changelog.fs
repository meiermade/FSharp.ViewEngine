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
