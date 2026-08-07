namespace Docs.Pages

open Docs.Common

module Installation =
    let page =
        { id = "installation"
          path = "/installation"
          aliases = [  ]
          navLabel = "Installation"
          category = "Getting started"
          title = "Installation"
          browserTitle = "Installation - FSharp.ViewEngine"
          nodes = [
            Paragraph [ Text "FSharp.ViewEngine is distributed as a NuGet package. You can install it using your preferred package manager." ];
            Heading { id = "using-net-cli"; title = "Using .NET CLI"; level = 2 };
            CodeBlock("bash", """dotnet package add FSharp.ViewEngine""");
            Heading { id = "using-paket-cli"; title = "Using Paket CLI"; level = 2 };
            CodeBlock("bash", """dotnet paket add FSharp.ViewEngine""");
            Heading { id = "runtime-support"; title = "Runtime Support"; level = 2 };
            Paragraph [ Text "The NuGet package ships a single "; InlineContent.Code "net8.0 compatibility asset"; Text ". NuGet selects that asset for compatible newer runtimes; a target asset is not a separate runtime dependency." ];
            Paragraph [ Text "FSharp.ViewEngine is actively tested on .NET 8, .NET 9, and .NET 10 while those runtimes remain supported by Microsoft." ];
            UnorderedList [
                [ Text ".NET 8 and .NET 9 support ends November 10, 2026." ];
                [ Text ".NET 10 LTS support ends November 14, 2028." ]
            ];
            Paragraph [ Text "After .NET 8 and .NET 9 reach end of support, their runtime tests will be removed. The "; InlineContent.Code "net8.0"; Text " package asset may remain as the compatibility baseline until the implementation needs APIs from a newer target framework." ];
            Heading { id = "source-debugging"; title = "Source Debugging"; level = 2 };
            Paragraph [ Text "Each release publishes portable symbols separately from the main package. Source Link maps those symbols to the matching GitHub commit so supported debuggers can retrieve the exact source on demand." ];
            Heading { id = "next-steps"; title = "Next Steps"; level = 2 };
            Paragraph [ Text "Once you have FSharp.ViewEngine installed, head over to the "; Link("Usage", "/usage"); Text " guide to start building your first HTML views." ];
          ] }
