namespace Docs.ComponentPages.Mermaid

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

open Docs.Common.BlockExamples

module View =
    let mermaid =
        fveMermaid { "flowchart LR\n  Input[Input] --> Validate[Validate]\n  Validate --> Result[Result]" }

    let sequence =
        let user = SequenceDiagram.participant "user" "User"
        let server = SequenceDiagram.participant "server" "Server"

        let diagram =
            SequenceDiagram.sequence
                [ user; server ]
                [ SequenceDiagram.call user server "Validate form"
                  SequenceDiagram.reply server user "Result" ]

        fveMermaid { SequenceDiagram.render diagram }

    let mermaidRegistration = registration "mermaid" "Mermaid"

    let examples () =
            [ sample "mermaid" "Flowchart" mermaid
              sample "sequence" "Sequence constructors" sequence ]


    let private usage =
        [ p {
              "Sequence constructors belong to this component. Only render trusted diagrams; CSP does not sanitize source or authorize arbitrary URLs."
          } ]

    let page = pageWithExamples mermaidRegistration usage (examples ())
