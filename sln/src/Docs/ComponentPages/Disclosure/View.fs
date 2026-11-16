namespace Docs.ComponentPages.Disclosure

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
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let disclosureRegistration =
        registration "components-disclosure" "/components/disclosure" "Disclosure" "Disclosure"

    let disclosureExample =
        div {
            fveDisclosure { _label "Can I change the content?"; _name "disclosure-example"; p { "Yes. Supply any semantic HTML as the disclosure body." } }
            fveDisclosure { _label "Does this need JavaScript?"; _name "disclosure-example"; _open true; p { "No. Native details and summary own keyboard interaction and expanded state." } }
        }

    let examples () =
            [ sample "disclosure-default" "Independent or grouped questions" [ "disclosureExample" ] (detailsSurface disclosureExample)
              |> note "Use _name to group questions so opening one closes its peers, or omit it for independent disclosures. The native control needs no JavaScript asset." ]


    let private galleryContent : Content =
        { description = "Expand supporting content or FAQ answers with native keyboard-accessible controls."
          usage = []
          sections = []
          apiCategory = Some "disclosure" }

    let disclosurePage = gallery disclosureRegistration galleryContent (examples ())
