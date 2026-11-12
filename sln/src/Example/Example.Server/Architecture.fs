namespace Example.Server

open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open type Html

module Architecture =
    let navigation =
        [ "architecture", "System context"
          "architecture/solution", "Overview"
          "architecture/solution/domain", "Example.Domain"
          "architecture/solution/application", "Example.Application"
          "architecture/solution/components", "Example.Components"
          "architecture/solution/server", "Example.Server" ]

    let href page = "/examples/specification/" + page

    let contents page =
        match page with
        | "architecture" -> ArchitecturePages.Context.contents
        | "architecture/solution" -> ArchitecturePages.Solution.contents
        | "architecture/solution/domain" -> ArchitecturePages.Domain.contents
        | "architecture/solution/application" -> ArchitecturePages.Application.contents
        | "architecture/solution/components" -> ArchitecturePages.Components.contents
        | "architecture/solution/server" -> ArchitecturePages.Server.contents
        | _ -> invalidArg (nameof page) "Choose a registered architecture page."

    let render page =
        match page with
        | "architecture" -> ArchitecturePages.Context.render ()
        | "architecture/solution" -> ArchitecturePages.Solution.render ()
        | "architecture/solution/domain" -> ArchitecturePages.Domain.render ()
        | "architecture/solution/application" -> ArchitecturePages.Application.render ()
        | "architecture/solution/components" -> ArchitecturePages.Components.render ()
        | "architecture/solution/server" -> ArchitecturePages.Server.render ()
        | _ -> invalidArg (nameof page) "Choose a registered architecture page."
