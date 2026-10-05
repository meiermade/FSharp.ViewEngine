namespace Acme.DocumentationConsumer

open FSharp.ViewEngine
open Acme.Documentation
open type Html

// Documentation controls install independently; page/site assembly is ordinary consumer HTML.
module Consumer =
    let code = CodeBlock.create "fsharp" "p { \"Hello\" }" |> CodeBlock.render
    let callout = Callout.create "Note" [p { "Consumer-owned supporting information." }] |> Callout.render
    let example = Example.gallery "documentation-example" "Greeting" "fsharp" "p { \"Hello\" }" (p { "Hello" })
    let reference = FSharpApiReference.create [typeof<CodeBlockConfig>] |> FSharpApiReference.render
    let sequenceSource =
        let author = SequenceDiagram.participant "Author" "Author"
        let server = SequenceDiagram.participant "Server" "Server"
        SequenceDiagram.sequence [author; server] [SequenceDiagram.call author server "Render"] |> SequenceDiagram.render
    let diagram = Mermaid.create sequenceSource |> Mermaid.render
    let document =
        html {
            _lang "en"
            head {
                title { "Generated documentation controls" }
                CodeBlock.assets None ["/scripts/prism.js"]
                Mermaid.assets "/scripts/mermaid.js"
            }
            body { main { h1 { "Owned documentation page" }; callout; code; example; diagram; reference } }
        }
