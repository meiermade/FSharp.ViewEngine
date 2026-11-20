namespace Docs.ComponentPages.Message

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
    let messageRegistration =
        applicationRegistration "components-message" "/components/message" "Message" "Message"

    let defaultMessage =
        fveMessage {
            _label "Jordan Lee"
            p { "Can you review the latest project notes?" }
        }

    let messageRows =
        div {
            _class "grid gap-5"

            fveMessage {
                _label "Jordan Lee"
                p { "Can you review the project notes before lunch?" }

                fveMessageAvatar {
                    fveAvatar {
                        _ariaLabel "Jordan Lee"
                        _size Size.Small
                        "JL"
                    }
                }

                fveMessageMetadata { "09:12" }
            }

            fveMessage {
                _label "Andy Meier"
                p { "Yes. I’ll add comments to the two items that need attention." }
                _side MessageSide.Sender
                fveMessageMetadata { "09:14" }
                fveMessageStatus { "Delivered" }
            }
        }

    let messageWithoutAvatar =
        fveMessage {
            _label "Support bot"

            p {
                _class "whitespace-pre-wrap"
                "The export is ready.\nIt contains 184 items."
            }

            fveMessageMetadata { "Just now" }

            fveMessageActions {
                fveButton {
                    _variant ButtonVariant.Ghost
                    _size Size.Small
                    "Download"
                }
            }
        }

    let examples () =
            [ sample "message-default" "Default" [ "defaultMessage" ] (detailsSurface defaultMessage)
              sample "message" "Sender and receiver" [ "messageRows" ] (detailsSurface messageRows)
              sample
                  "message-no-avatar"
                  "Metadata and actions without an avatar"
                  [ "messageWithoutAvatar" ]
                  (detailsSurface messageWithoutAvatar) ]


    let private galleryContent : Content =
        { description = "Compose sender identity, message content, metadata, and contextual actions."
          usage = []
          sections = []
          apiCategory = Some "message" }

    let messagePage = gallery messageRegistration galleryContent (examples ())
