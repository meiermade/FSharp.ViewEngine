namespace Example.UI.Views

open System
open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open type Html
open type Svg
open type Datastar
open Model
open ViewData
open Controls

module internal ViewEnvironment =
    let environmentDetail (query:Query) id =
        let environment = findEnvironment query.catalog id

        Layout.sectionWithEdit "Details" (applicationHref query (ApplicationPage.EditEnvironment id)) (fveDescriptionList {
            fveDescriptionListItem {
                _label "Name"
                environment.name
            }

            fveDescriptionListItem {
                _label "Type"
                if environment.sandbox then "Sandbox" else "Production"
            }

            fveDescriptionListItem {
                _label "Status"

                fveBadge {
                    _color Color.Success
                    "Ready"
                }
            }

            _columns DescriptionListColumns.Three
        })
