namespace Docs.ComponentPages.Skeleton

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
    let skeletonRegistration =
        registration "components-skeleton" "/components/skeleton" "Skeleton" "Skeleton"

    let defaultSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading content"
            fveSkeleton { _class "h-4 w-full" }
        }

    let textSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading description"

            div {
                _class "grid min-w-0 gap-2"
                fveSkeleton { _class "h-4 w-full" }
                fveSkeleton { _class "h-4 w-full" }
                fveSkeleton { _class "h-4 w-3/4" }
            }
        }

    let avatarSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading profile"

            div {
                _class "flex min-w-0 items-center gap-4"
                fveSkeleton { _class "size-12 shrink-0 rounded-full" }

                div {
                    _class "grid min-w-0 flex-1 gap-2"
                    fveSkeleton { _class "h-4 w-full" }
                    fveSkeleton { _class "h-4 w-3/4" }
                }
            }
        }

    let cardSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading card"

            div {
                _class "grid min-w-0 gap-4"
                fveSkeleton { _class "h-36 w-full rounded-xl" }

                div {
                    _class "grid min-w-0 gap-2"
                    fveSkeleton { _class "h-4 w-2/3" }
                    fveSkeleton { _class "h-4 w-1/2" }
                }
            }
        }

    let dimensionSkeleton =
        fveSkeletonRegion {
            _ariaLabel "Loading summary"

            fveSkeleton {
                _class "rounded-full"
                Html._width "75%"
                Html._height "1rem"
            }
        }

    let examples () =
            [ sample "skeleton-default" "Basic" [ "defaultSkeleton" ] (fieldSurface defaultSkeleton)
              sample "skeleton-text" "Text" [ "textSkeleton" ] (fieldSurface textSkeleton)
              sample "skeleton-avatar" "Avatar row" [ "avatarSkeleton" ] (fieldSurface avatarSkeleton)
              sample "skeleton-card" "Card" [ "cardSkeleton" ] (fieldSurface cardSkeleton)
              sample
                  "skeleton-dimensions"
                  "Explicit dimensions"
                  [ "dimensionSkeleton" ]
                  (fieldSurface dimensionSkeleton) ]


    let private galleryContent : Content =
        { description = "Reserve content structure while data is loading without implying completed content."
          usage = []
          sections = []
          apiCategory = Some "skeleton" }

    let skeletonPage = gallery skeletonRegistration galleryContent (examples ())
