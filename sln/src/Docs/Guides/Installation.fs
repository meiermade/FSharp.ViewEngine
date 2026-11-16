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
            CodeBlock("bash", """dotnet add package FSharp.ViewEngine""");
            Heading { id = "using-paket-cli"; title = "Using Paket CLI"; level = 2 };
            CodeBlock("bash", """dotnet paket add FSharp.ViewEngine""");
            Heading { id = "runtime-support"; title = "Runtime Support"; level = 2 };
            Paragraph [ Text "The NuGet package ships a single "; InlineContent.Code "net8.0 compatibility asset"; Text ". NuGet selects that asset for compatible newer runtimes; a target asset is not a separate runtime dependency." ];
            Paragraph [ Text "FSharp.ViewEngine is actively tested on .NET 10 and .NET 11. New CLI project consumers default to net11.0; use --framework net10.0 to select .NET 10. Existing consumer projects keep their target frameworks." ];
            UnorderedList [
                [ Text ".NET 10 LTS and .NET 11 STS support ends November 14, 2028." ];
                [ Text ".NET 8 and .NET 9 reached end of support on November 10, 2026; their runtime test lanes are retired." ]
            ];
            Paragraph [ Text "The "; InlineContent.Code "net8.0"; Text " package asset remains the compatibility baseline. Supporting newer consumers does not require an older runtime to be installed or raise Core’s FSharp.Core minimum. Docs and the shipped application examples run on .NET 11." ];
            Heading { id = "source-debugging"; title = "Source Debugging"; level = 2 };
            Paragraph [ Text "Each release publishes portable symbols separately from the main package. Source Link maps those symbols to the matching GitHub commit so supported debuggers can retrieve the exact source on demand." ];
            Heading { id = "next-steps"; title = "Next Steps"; level = 2 };
            Paragraph [ Text "Once you have FSharp.ViewEngine installed, "; Link("build your first view", "/getting-started/first-view"); Text " and then continue through the core concepts." ];
          ] }
