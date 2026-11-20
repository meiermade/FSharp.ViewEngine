namespace Docs.ComponentPages.Avatar

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
    let avatarRegistration =
        registration "components-avatar" "/components/avatar" "Avatar" "Avatar"

    let defaultAvatar =
        fveAvatar {
            _ariaLabel "Alex Morgan"
            "AM"
        }

    let avatarFallbackExample =
        div {
            _class "flex flex-wrap items-center gap-4"

            fveAvatar {
                _ariaLabel "Alex Morgan"
                _size Size.Small
                "AM"
            }

            fveAvatar {
                _ariaLabel "Jamie Lee"
                "JL"
            }

            fveAvatar {
                _ariaLabel "Riley Chen"
                _size Size.Large
                "RC"
            }
        }

    let avatarImageExample =
        fveAvatar {
            _ariaLabel "FSharp.ViewEngine"
            _src "/android-chrome-512x512.png"
            _referrerpolicy "no-referrer"
            _size Size.Large
            "FV"
        }

    let failedAvatarImageExample =
        fveAvatar {
            _ariaLabel "Unavailable profile image"
            _src "data:image/png;base64,broken"
            "AM"
        }

    let decorativeAvatarExample =
        fveAvatar {
            _ariaLabel "Decorative profile mark"
            _ariaHidden true
            "AM"
        }

    let examples () =
            [ sample "avatar-default" "Default" [ "defaultAvatar" ] (centered defaultAvatar)
              sample
                  "avatar-fallback"
                  "Fallbacks and sizes"
                  [ "avatarFallbackExample" ]
                  (centered avatarFallbackExample)
              sample "avatar-image" "Image" [ "avatarImageExample" ] (centered avatarImageExample)
              |> note "Import Assets/avatar.js to display successfully loaded images. The supplied fallback remains visible while loading, after failure, or when JavaScript is unavailable. _referrerpolicy is forwarded to the image; the caller chooses the policy. The wrapper supplies one accessible name."
              sample "avatar-image-failed" "Image unavailable" [ "failedAvatarImageExample" ] (centered failedAvatarImageExample)
              sample "avatar-decorative" "Decorative" [ "decorativeAvatarExample" ] (centered decorativeAvatarExample) ]


    let private galleryContent : Content =
        { description = "Represent a person or entity with an image, initials fallback, size, and accessible name."
          usage = []
          sections = []
          apiCategory = Some "avatar" }

    let avatarPage = gallery avatarRegistration galleryContent (examples ())
