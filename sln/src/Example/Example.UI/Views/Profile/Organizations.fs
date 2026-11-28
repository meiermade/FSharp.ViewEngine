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

module internal ProfileOrganizations =
    let profileOrganizations (query: Query) =
        let headings =
            [ fveTableHead { "Organization" }
              fveTableHead { "Your role" }
              fveTableHead { "Settings" } ]

        let body =
            [ for organization in query.catalog.organizations do
                  fveTableRow {
                      fveTableHead {
                          _scope "row"
                          _mobile Mobile.Primary

                          Layout.inlineLink
                              (applicationHref
                                  (organizationQuery query organization.id)
                                  (ApplicationPage.SettingsSection "general"))
                              organization.name
                      }

                      fveTableCell {
                          _label "Your role"
                          organization.role
                      }

                      fveTableCell {
                          _label "Settings"

                          Layout.secondaryLink
                              (applicationHref
                                  (organizationQuery query organization.id)
                                  (ApplicationPage.SettingsSection "general"))
                              "Open settings"
                      }
                  } ]

        RecordTable.render "Your organizations" [ _mobileLayout MobileLayout.Records ] headings body []
