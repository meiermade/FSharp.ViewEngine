namespace Example.Server.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.Server
open type Html

open Presentation

module internal Components =
    let contents =
        [ "responsibility", "Responsibility"
          "dependencies", "Dependencies"
          "contract", "Contract"
          "rules", "Rules" ]

    let render () =
        contracts
            "Composition"
            "Example.Components/Example.Components.fsproj contains CLI-installed, editable source for the three surfaces, including Chart and its pinned Rocket/ECharts assets."
            [ "FSharp.ViewEngine and each selected component's declared dependencies." ]
            "fvePageTopBar {\n    fvePageTopBarBrand { brand }\n    breadcrumbs\n    fvePageTopBarActions { actions }\n}\n\nfveSideNav {\n    _id id\n    _ariaLabel label\n    fveSideNavContent { yield! groups }\n}"
            [ "Components SHALL own their presentation, accessibility and local interaction behavior."
              "The consumer SHALL own financial content, routes, theme and workflow state."
              "The full-width PageTopBar SHALL own the shell's shared header height and separator." ]
