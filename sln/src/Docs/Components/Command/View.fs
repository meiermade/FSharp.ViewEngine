namespace Docs.ComponentPages.Command

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
    let commandRegistration =
        registration "components-command" "/components/command" "Command" "Command"

    let defaultCommand =
        fveCommand {
            _id "command-default"
            _ariaLabel "Find a component"

            fveCommandGroup {
                _label "Components"

                fveCommandItem {
                    _href "/components/table"
                    "Table"
                }

                fveCommandItem {
                    _href "/components/month-calendar"
                    "Month calendar"
                }
            }
        }

    let commandDocumentIcon = fveIcon { _name IconName.DocumentText; _size Size.Small }

    let commandGroups =
        [ fveCommandGroup {
              _label "Components"

              fveCommandItem {
                  _href "/components/table"
                  _shortcut "⌘T"
                  fveCommandItemLeading { commandDocumentIcon }
                  "Table"
              }

              fveCommandItem {
                  _href "/components/month-calendar"
                  _keywords [ "dates"; "schedule" ]
                  "Month calendar"
              }
          }
          fveCommandGroup {
              _label "Page examples"

              fveCommandItem {
                  _href "/components/page-examples/scheduling"
                  _shortcut "⌘S"
                  "Scheduling"
              }

              fveCommandItem {
                  _href "/components/page-examples/media-management"
                  "Media management"
              }
          } ]

    let groupedCommand =
        fveCommand {
            _id "command-groups"
            _ariaLabel "Search components and examples"
            _placeholder "Find a component or page…"

            for group in commandGroups do
                group
        }

    let contextualCommand =
        fveCommand {
            _id "command-context"
            _ariaLabel "Search page topics"
            _placeholder "Try presentations or PDF…"
            fveCommandGroup {
                _label "Guides"
                fveCommandItem {
                    _href "/guides/presentations"
                    _keywords ["print"; "PDF"]
                    "Presentations"
                }
                fveCommandItem {
                    _href "/guides/presentations#printing"
                    _matchAnchor "Print PDF"
                    "Presentations · Print or save as PDF"
                }
            }
        }

    let commandActionsExample =
        div {
            _attr ("data-signals__root__ifmissing", "{commandFeedback: ''}")
            _class "grid w-full gap-3"

            fveCommand {
                _id "command-actions"
                _ariaLabel "Choose a local action"

                fveCommandGroup {
                    _label "Actions"

                    fveCommandItem {
                        _dataOn ("click", "$commandFeedback = 'Display settings selected.'")
                        "Display settings"
                    }

                    fveCommandItem {
                        _dataOn ("click", "$commandFeedback = 'Profile settings selected.'")
                        "Profile settings"
                    }

                    fveCommandItem {
                        _dataOn ("click", "$commandFeedback = 'Unavailable action.'")
                        "Unavailable action"
                        _disabled true
                    }
                }
            }

            p {
                _role "status"
                _class "text-sm text-[var(--fve-muted-text)]"
                _dataText "$commandFeedback"
            }
        }

    let commandPaletteExample =
        div {
            fveButton {
                for attribute in Command.triggerAttributes "command-palette" do
                    attribute

                "Open command palette"
            }

            fveCommandDialog {
                _id "command-palette"
                _ariaLabel "Navigate the catalog"

                for group in commandGroups do
                    group
            }
        }

    let emptyCommand =
        fveCommand {
            _id "command-empty"
            _ariaLabel "Search an empty collection"
            _emptyLabel "No commands are available."
        }

    let examples () =
            [ sample "command-default" "Default" [ "defaultCommand" ] (fieldSurface defaultCommand)
              sample
                  "command-groups"
                  "Groups, icons and shortcuts"
                  [ "commandDocumentIcon"; "commandGroups"; "groupedCommand" ]
                  (fieldSurface groupedCommand)
              sample "command-context" "Contextual page links" [ "contextualCommand" ] (fieldSurface contextualCommand)
              |> note "Use _matchAnchor for contextual results such as section links. They stay hidden for an empty query or a parent-name-only match, require a query word matching the anchor, and appear after ordinary matches. The full label and keywords still support combined searches."
              sample
                  "command-actions"
                  "Actions and disabled items"
                  [ "commandActionsExample" ]
                  (fieldSurface commandActionsExample)
              sample
                  "command-palette"
                  "Dialog palette"
                  [ "commandDocumentIcon"; "commandGroups"; "commandPaletteExample" ]
                  (centered commandPaletteExample)
              sample "command-empty" "Empty results" [ "emptyCommand" ] (fieldSurface emptyCommand) ]


    let private galleryContent : Content =
        { description = "Search grouped destinations and actions with ranked matching, keyboard navigation, and an optional dialog palette."
          usage = []
          sections = []
          apiCategory = Some "command" }

    let commandPage = gallery commandRegistration galleryContent (examples ())
