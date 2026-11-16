namespace Docs.ComponentPages.Separator

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
    let separatorRegistration =
        registration "components-separator" "/components/separator" "Separator" "Separator"

    let defaultSeparator =
        div {
            _class "w-full"
            fveSeparator { () }
        }

    let separatorExamples =
        div {
            _class "grid gap-5"

            div {
                p {
                    _class "mb-3 text-sm font-medium"
                    "Profile details"
                }

                fveSeparator { _role "separator" }

                p {
                    _class "mt-3 text-sm text-[var(--fve-muted-text)]"
                    "Contact information and preferences"
                }
            }

            div {
                _class "flex h-8 items-center gap-4"
                span { "Profile" }
                fveSeparator { _orientation Orientation.Vertical }
                span { "Security" }
            }
        }

    let examples () =
            [ sample "separator-default" "Default" [ "defaultSeparator" ] (centered defaultSeparator)
              sample "separator" "Orientation and semantics" [ "separatorExamples" ] (centered separatorExamples) ]


    let private galleryContent : Content =
        { description = "Separate related content visually while preserving the intended semantic orientation."
          usage = []
          sections = []
          apiCategory = Some "separator" }

    let separatorPage = gallery separatorRegistration galleryContent (examples ())
