namespace Example.Server.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
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
            "Example.Components/Components contains CLI-installed, editable controls. Example.Components/Views owns the product shell, screens, tables and editors used by both Application and Specification. It accepts typed view inputs and consumer-owned destinations; there is no separate Spec rendering of product screens."
            [ "FSharp.ViewEngine and selected control dependencies; Example.Application for typed read models, editor inputs and validation outcomes." ]
            "fvePageTopBar {\n    fvePageTopBarBrand { brand }\n    breadcrumbs\n    fvePageTopBarActions { actions }\n}\n\nfveSideNav {\n    _id id\n    _ariaLabel label\n    fveSideNavContent { yield! groups }\n}"
            [ "Components SHALL own shared product presentation, accessibility and local interaction behavior. CLI updates SHALL only replace Components/, never the consumer-owned Views/."
              "The host SHALL parse HTTP inputs, invoke use cases and provide data, validation outcomes and navigation behavior. Views SHALL NOT reference Server or invoke Application operations."
              "The full-width PageTopBar SHALL own the shell's shared header height and separator." ]
