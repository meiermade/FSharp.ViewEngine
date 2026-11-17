namespace Docs.Common

open System
open Docs.Common
open Giraffe
open Microsoft.Extensions.DependencyInjection
open StarFederation.Datastar
open StarFederation.Datastar.DependencyInjection
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html
open type Svg
open type Datastar
open Docs.Common.SharedExamples

module ChoicesExamples =
    let private statusOptions =
        [ (Active, "Active", false)
          (Pending, "Pending", false)
          (Suspended, "Suspended", false)
          (Scheduled, "Scheduled", false) ]

    let selectStatusOptions =
        [ ("active", "Active", false)
          ("pending", "Pending", false)
          ("suspended", "Suspended", true)
          ("scheduled", "Scheduled", false) ]

    let choiceSubmitButton id (label: string) =
        fveButton {
            _color Color.Primary
            _variant ButtonVariant.Solid
            _type "submit"
            _id id
            label
        }

    let choiceResult id result =
        output {
            _id id
            _role "status"
            _class "block min-h-5 text-sm text-[var(--fve-muted-text)]"
            defaultArg result "Submit the form to see the result."
        }
