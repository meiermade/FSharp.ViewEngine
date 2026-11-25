namespace Docs.ComponentPages.PageHeader

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
open Docs.Common.LedgerExamples
open Docs.Common.SamplesExamples
open Docs.Common.Gallery
open Docs.Common.SharedExamples

open Docs.Common.Registration

module View =
    let pageHeaderRegistration =
        applicationRegistration "components-page-header" "/components/page-header" "Page header" "Page header"

    let defaultPageHeader = fvePageHeader { _label "Project overview" }

    let pageHeaderLayoutExample =
        div {
            _attr ("data-signals__root__ifmissing", "{itemRefreshes: 0}")

            fvePageHeader {
                _label "Project notes"
                _description "Updated moments ago"

                fvePageHeaderActions {
                    fveButton {
                        _ariaLabel "Refresh"
                        _dataOn ("click", "$itemRefreshes++")

                        span {
                            _ariaHidden true
                            _class "inline-flex shrink-0"
                            refreshIcon
                        }
                    }
                }
            }

            output {
                _role "status"
                _class "sr-only"
                _dataText "'Refreshes: ' + $itemRefreshes"
                "Refreshes: 0"
            }
        }

    // Supply ordinary HTML for your table, filter controls, and actions.

    let pageHeaderExample =
        div {
            _attr ("data-signals__root__ifmissing", "{ledgerRefreshes: 0}")
            accountPageHeader

            output {
                _class "sr-only"
                _role "status"
                _dataText "'Balance refreshes: ' + $ledgerRefreshes"
                "Balance refreshes: 0"
            }
        }

    let examples () =
            [ sample "page-header-default" "Default" [ "defaultPageHeader" ] (detailsSurface defaultPageHeader)
              sample
                  "page-header"
                  "Title and page actions"
                  [ "refreshIcon"; "pageHeaderLayoutExample" ]
                  (themedSurface pageHeaderLayoutExample) ]


    let private galleryContent : Content =
        { description = "Compose a page title, description, metadata, and responsive actions."
          usage = []
          sections = []
          apiCategory = Some "page-header" }

    let pageHeaderPage = gallery pageHeaderRegistration galleryContent (examples ())
