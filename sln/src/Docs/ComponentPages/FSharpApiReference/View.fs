namespace Docs.ComponentPages.FSharpApiReference

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

open Docs.Common.BlockExamples

module View =
    let fsharpReference =
        fveFSharpApiReference {
            typeof<FSharpApiReferenceBuilder>
            typeof<Size>
        }

    let fsharpReferenceRegistration =
        registration "fsharp-api-reference" "F# API reference"

    let examples () = [ sample "fsharpReference" "Compiled declarations" fsharpReference ]


    let page = pageWithExamples fsharpReferenceRegistration [] (examples ())
