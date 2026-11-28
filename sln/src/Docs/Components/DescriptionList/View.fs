namespace Docs.ComponentPages.DescriptionList

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
    let descriptionListRegistration =
        registration "components-description-list" "/components/description-list" "Description list" "Description list"

    let defaultDescriptionList =
        fveDescriptionList {
            fveDescriptionListItem {
                _label "Name"
                "Project notes"
            }

            fveDescriptionListItem {
                _label "Format"
                "Document"
            }
        }

    let itemDetails =
        fveDescriptionList {
            _columns DescriptionListColumns.Three

            fveDescriptionListItem {
                _label "Name"
                "Project notes"
            }

            fveDescriptionListItem {
                _label "Format"
                "Document"
            }

            fveDescriptionListItem {
                _label "Folder"
                "Shared files"
            }

            fveDescriptionListItem {
                _label "Owner"

                a {
                    _href "/examples/application/profile"

                    _class
                        "font-medium text-[var(--fve-brand-text)] hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)]"

                    "Andrew Meier"
                }
            }

            fveDescriptionListItem {
                _label "Status"

                fveBadge {
                    _color Color.Success
                    "Active"
                }
            }

            fveDescriptionListItem {
                _label "Size"
                "2.4 MB"
            }
        }

    let itemOverview =
        fveDescriptionList {
            _columns DescriptionListColumns.Four

            fveDescriptionListItem {
                _label "Status"

                fveBadge {
                    _color Color.Success
                    "Active"
                }
            }

            fveDescriptionListItem {
                _label "Review"

                fveBadge {
                    _color Color.Success
                    "Up to date"
                }
            }

            fveDescriptionListItem {
                _label "Reference"
                "1040"
            }

            fveDescriptionListItem {
                _label "Format"
                "Document"
            }

            fveDescriptionListItem {
                _label "Name"
                "Project notes"
            }

            fveDescriptionListItem {
                _label "Folder"
                "Shared files"
            }

            fveDescriptionListItem {
                _label "Updated"
                "August 31, 2026"
            }

            fveDescriptionListItem {
                _label "Size"
                "2.4 MB"
            }
        }

    let itemDetailsWithDescriptions =
        fveDescriptionList {
            _columns DescriptionListColumns.Two

            fveDescriptionListItem {
                _label "Files"
                "24"
                fveDescriptionListDescription { "Includes files added today." }
            }

            fveDescriptionListItem {
                _label "Reference"
                "project-notes-2026-08-31-team-review-1040"
                fveDescriptionListDescription { "Shared during the August project review." }
            }
        }

    let maskedDetails =
        fveDescriptionList {
            fveDescriptionListItem { _label "Bank"; "Example bank" }
            fveDescriptionListItem {
                _label "Routing number"
                _masked true
                _value "001234567"
                fveDescriptionListDescription { "Illustrative payment details." }
            }
        }

    let examples () =
            [ sample
                  "description-list-default"
                  "Default"
                  [ "defaultDescriptionList" ]
                  (detailsSurface defaultDescriptionList)
              sample "description-list" "Three-column details" [ "itemDetails" ] (detailsSurface itemDetails)
              sample
                  "description-list-four-columns"
                  "Four-column details"
                  [ "itemOverview" ]
                  (detailsSurface itemOverview)
              sample
                  "description-list-supporting-text"
                  "With supporting text"
                  [ "itemDetailsWithDescriptions" ]
                  (detailsSurface itemDetailsWithDescriptions)
              sample "description-list-masked" "Masked values" [ "maskedDetails" ] (detailsSurface maskedDetails) ]


    let private galleryContent : Content =
        { description = "Present labelled record details and semantic values as a responsive description list."
          usage = [ prose "Set _masked true and supply the displayed text using _value. Show and Hide change its on-screen visibility; Copy always uses the full value. Supporting content remains visible. Use this only for values the viewer is allowed to receive." ]
          sections = []
          apiCategory = Some "description-list" }

    let descriptionListPage =
        gallery descriptionListRegistration galleryContent (examples ())
