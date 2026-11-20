namespace Docs.ComponentPages.Detail

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
open Docs.Common.LedgerExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery

open Docs.Common.Registration

module View =
    let detailRegistration =
        applicationRegistration "components-detail" "/components/detail" "Detail" "Detail"

    let defaultDetail =
        Detail.create "Project notes" [ p { "Document · Updated today" } ]
        |> Detail.render

    let detailExample resolve accountsDestination actions transactions =
        Detail.create
            "Operating checking"
            [ Section.create
                  (fveSectionHeader { _label "Detail" })
                  (fveDescriptionList {
                      fveDescriptionListItem {
                          _label "Status"

                          fveBadge {
                              _color Color.Success
                              "Active"
                          }
                      }

                      fveDescriptionListItem {
                          _label "Type"
                          "Asset"
                      }

                      fveDescriptionListItem {
                          _label "Commodity"
                          "USD"
                      }

                      fveDescriptionListItem {
                          _label "Parent account"
                          "Current assets"
                      }

                      fveDescriptionListItem {
                          _label "Source"
                          "Created in Ledger"
                      }

                      fveDescriptionListItem {
                          _label "Month-end observed balance"
                          "Required"
                      }

                      fveDescriptionListItem {
                          _label "Balance"
                          "$38,442.11"
                      }

                      _columns DescriptionListColumns.Three
                  })
              |> Section.render
              fveSectionHeader {
                  _label "Transactions"
                  _description "Current assets · All accounts"

                  fveSectionHeaderActions {
                      a {
                          _href (resolve accountsDestination)

                          _class "text-sm font-medium text-[var(--fve-brand-text)] underline-offset-4 hover:underline"

                          "View all accounts"
                      }
                  }
              }
              |> fun header -> Section.create header transactions
              |> Section.withLabel "Recent transactions"
              |> Section.render ]
        |> Detail.withActions actions
        |> Detail.render

    let detailPage =
        let actions =
            div {
                _class "flex flex-wrap items-center gap-2"
                Docs.ComponentPages.Compositions.View.link (shellDestinationUrl (LedgerAccount operatingAccount.id)) "View account"

                fveDropdownMenu {
                    _id "components-detail-actions"
                    _ariaLabel "More actions"

                    fveDropdownMenuTrigger {
                        fveButton {
                            _variant ButtonVariant.Ghost

                            moreActionsIcon
                        }
                    }

                    yield!
                        (recordMenuItems
                            "account"
                            operatingAccount.id
                            (LedgerAccount operatingAccount.id)
                            shellDestinationUrl
                            (System.Text.Json.JsonSerializer.Serialize operatingAccount)
                         |> List.tail)
                }
            }

        detailExample shellDestinationUrl LedgerAccounts actions transactionTable
        |> recordActionFeedback "account"

    let detailPreview =
        div {
            _class "p-4 sm:p-6 lg:p-8"
            detailPage
        }
        |> fullBleedThemedSurface

    let examples () = [ sample "detail-default" "Default" [ "defaultDetail" ] (detailsSurface defaultDetail) ]


    let private galleryContent : Content =
        { description = "Compose record identity, metadata, sections, and actions into a detail layout."
          usage = []
          sections = []
          apiCategory = Some "detail" }

    let detailPageDocumentation = gallery detailRegistration galleryContent (examples ())
