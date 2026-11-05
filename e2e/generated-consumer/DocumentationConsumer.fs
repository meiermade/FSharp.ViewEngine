namespace Acme.DocumentationConsumer

open FSharp.ViewEngine
open Acme.Documentation
open type Html

// Documentation controls install independently; page/site assembly is ordinary consumer HTML.
module Consumer =
    let code =
        fveCodeBlock {
            _language "fsharp"
            "p { \"Hello\" }"
        }

    let callout =
        fveCallout {
            _label "Note"
            p { "Consumer-owned supporting information." }
        }

    let example =
        fveExample {
            _name "documentation-example"
            _label "Greeting"
            _variant ExampleVariant.Gallery
            _language "fsharp"
            fveExampleCode { "p { \"Hello\" }" }
            p { "Hello" }
        }

    let reference =
        fveFSharpApiReference { yield! FSharpApiReference.forCategory "code-block" typeof<Size> }

    let sequenceSource =
        let author = SequenceDiagram.participant "Author" "Author"
        let server = SequenceDiagram.participant "Server" "Server"

        SequenceDiagram.sequence [ author; server ] [ SequenceDiagram.call author server "Render" ]
        |> SequenceDiagram.render

    let diagram = fveMermaid { sequenceSource }

    let document =
        html {
            _lang "en"

            head {
                title { "Generated documentation controls" }
                CodeBlock.assets None [ "/scripts/prism.js" ]
                Mermaid.assets "/scripts/mermaid.js"
            }

            body {
                main {
                    h1 { "Owned documentation page" }
                    callout
                    code
                    example
                    diagram
                    reference
                }
            }
        }
