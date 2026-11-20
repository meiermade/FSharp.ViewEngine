namespace Docs.ComponentPages.Callout

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

open Docs.Common.BlockExamples

module View =
    let callout =
        fveCallout {
            _label "Note"
            p { "Keep a copy of these instructions for future reference." }
        }

    let calloutRegistration = registration "callout" "Callout"

    let examples () = [ sample "callout" "Supporting information" callout ]


    let page = pageWithExamples calloutRegistration [] (examples ())
