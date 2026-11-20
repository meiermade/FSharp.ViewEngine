namespace Docs.ComponentPages.EmptyState

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
open Docs.ComponentPages.Button.View
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let emptyStateRegistration =
        registration "components-empty-state" "/components/empty-state" "Empty state" "Empty state"

    let defaultEmptyState =
        fveEmptyState {
            _label "No items yet"
            _description "Add your first item to get started."
        }

    let titleOnlyEmptyState =
        fveEmptyState { _label "No matching items" }

    let emptyStateExample =
        fveEmptyState {
            _label "No matching items"
            _description "Clear your filters to see all items."
            fveEmptyStateIcon { plusIcon }

            fveEmptyStateActions {
                fveLink {
                    _variant LinkVariant.Outline
                    _color Color.Neutral
                    _href "/components/empty-state"
                    "Clear filters"
                }
            }
        }

    let examples () =
            [ sample "empty-state-default" "Default" [ "defaultEmptyState" ] (fieldSurface defaultEmptyState)
              sample "empty-state-title-only" "Title only" [ "titleOnlyEmptyState" ] (fieldSurface titleOnlyEmptyState)
              |> note "Description is optional. Add guidance or recovery actions only when they help the user."
              sample
                  "empty-state"
                  "With recovery action"
                  [ "plusIcon"; "emptyStateExample" ]
                  (fieldSurface emptyStateExample) ]


    let private galleryContent : Content =
        { description = "Explain an empty collection or unavailable result and provide its next useful action."
          usage = []
          sections = []
          apiCategory = Some "empty-state" }

    let emptyStatePage = gallery emptyStateRegistration galleryContent (examples ())
