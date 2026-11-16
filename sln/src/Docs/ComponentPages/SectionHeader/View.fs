namespace Docs.ComponentPages.SectionHeader

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

open Docs.Common.BlockExamples

module View =
    let sectionHeader = fveSectionHeader { _label "Item details" }

    let sectionHeaderWithActions =
        fveSectionHeader {
            _label "Recent activity"
            _description "Updates from this month."
            _divider true

            fveSectionHeaderActions {
                fveButton {
                    _disabled true
                    "Export"
                }
            }
        }

    let sectionHeaderRegistration = registration "section-header" "Section header"

    let examples () =
            [ sample "sectionHeader" "Default" sectionHeader
              sample "sectionHeaderWithActions" "Description, actions and divider" sectionHeaderWithActions ]


    let page = pageWithExamples sectionHeaderRegistration [] (examples ())
