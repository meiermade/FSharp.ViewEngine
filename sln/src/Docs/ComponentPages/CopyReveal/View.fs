namespace Docs.ComponentPages.CopyReveal

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
    let copyRevealRegistration =
        registration "components-copy-reveal" "/components/copy-reveal" "Copy and reveal" "Copy and reveal"

    let copyRevealExample =
        fveField {
            fveLabel {
                _for "demo-token"
                "Demo API token"
            }

            fveCopyReveal {
                _id "demo-token"
                _value "fve_demo_84fK2s"
            }
        }

    let revealedCopyRevealExample =
        fveField {
            fveLabel {
                _for "revealed-demo-token"
                "Revealed demo token"
            }

            fveCopyReveal {
                _id "revealed-demo-token"
                _value "fve_demo_safe"
                _revealed true
            }
        }

    let iconCopyRevealExample =
        fveField {
            fveLabel {
                _for "icon-demo-token"
                "Icon action demo token"
            }

            fveCopyReveal {
                _id "icon-demo-token"
                _value "fve_demo_icon"
                _iconButtons true
            }
        }

    let examples () =
            [ sample "copy-reveal" "Default" [ "copyRevealExample" ] (fieldSurface copyRevealExample)
              sample "copy-reveal-icons" "Icon actions" [ "iconCopyRevealExample" ] (fieldSurface iconCopyRevealExample)
              sample
                  "copy-reveal-revealed"
                  "Initially revealed"
                  [ "revealedCopyRevealExample" ]
                  (fieldSurface revealedCopyRevealExample) ]


    let private galleryContent : Content =
        { description = "Copy or deliberately reveal sensitive text with accessible text or icon actions."
          usage = []
          sections = []
          apiCategory = Some "copy-reveal" }

    let copyRevealPage = gallery copyRevealRegistration galleryContent (examples ())
