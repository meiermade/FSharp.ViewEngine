namespace Docs.ComponentPages.Example

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

open Docs.Common.BlockExamples

module View =
    let example =
        fveExample {
            _name "nested-example"
            _label "Greeting"
            _variant ExampleVariant.Gallery
            _language "fsharp"
            fveExampleCode { "p { \"Hello\" }" }
            p { "Hello" }
        }

    let exampleRegistration = registration "example" "Example"

    let examples () = [ sample "example" "Preview and code" example ]


    let page = pageWithExamples exampleRegistration [] (examples ())
