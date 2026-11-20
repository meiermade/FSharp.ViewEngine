namespace Docs.ComponentPages.Table

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
open Docs.Common.LedgerExamples
open Docs.Common.SharedExamples

open Docs.Common.Registration

module View =
    let tableRegistration =
        registration "components-table" "/components/table" "Table" "Table"

    type TeamMember =
        { id: int
          name: string
          email: string
          role: string }

    let teamMembers =
        [ { id = 1
            name = "Alex Morgan"
            email = "alex@fve.meiermade.com"
            role = "Owner" }
          { id = 2
            name = "Jamie Lee"
            email = "jamie@fve.meiermade.com"
            role = "Member" }
          { id = 3
            name = "Riley Chen"
            email = "riley@fve.meiermade.com"
            role = "Member" }
          { id = 4
            name = "Sam Rivera"
            email = "sam@fve.meiermade.com"
            role = "Guest" } ]

    let teamTableHeader =
        fveTableHeader {
            fveTableRow {
                fveTableHead { "Name" }
                fveTableHead { "Email" }
                fveTableHead { "Role" }
            }
        }

    let teamTableRows (members: TeamMember list) =
        fragment {
            for person in members do
                fveTableRow {
                    fveTableHead {
                        _scope "row"
                        _mobile Mobile.Primary
                        _class "font-medium"
                        person.name
                    }

                    fveTableCell {
                        _label "Email"
                        person.email
                    }

                    fveTableCell {
                        _label "Role"
                        person.role
                    }
                }
        }

    let simpleTeamTable =
        fveTable {
            _ariaLabel "Team members"
            teamTableHeader
            fveTableBody { teamTableRows teamMembers }
        }

    let scrollableTeamTable =
        fveTable {
            _ariaLabel "Team members in a bounded viewport"
            _class "h-28"
            _scrollRows true
            teamTableHeader
            fveTableBody { teamTableRows teamMembers }
        }

    let comfortableTeamTable =
        fveTable {
            _ariaLabel "Team members with comfortable rows"
            _density Density.Comfortable
            teamTableHeader
            fveTableBody { teamTableRows teamMembers }
        }

    let hoverTeamTable =
        fveTable {
            _ariaLabel "Team members with hover highlighting"
            _hoverRows true
            _style "--fve-table-row-hover:var(--fve-brand-subtle)"
            teamTableHeader
            fveTableBody { teamTableRows teamMembers }
        }

    let memberStatusTable =
        fveTable {
            _ariaLabel "Team member status"

            fveTableHeader {
                fveTableRow {
                    fveTableHead { "Name" }
                    fveTableHead { "Email" }
                    fveTableHead { "Status" }
                }
            }

            fveTableBody {
                for person in teamMembers do
                    fveTableRow {
                        fveTableHead {
                            _scope "row"
                            person.name
                        }

                        fveTableCell { person.email }

                        fveTableCell {
                            if person.role = "Guest" then
                                fveBadge {
                                    _color Color.Warning
                                    "Invited"
                                }
                            else
                                fveBadge {
                                    _color Color.Success
                                    "Active"
                                }
                        }
                    }
            }
        }

    let selectableTeamTable =
        let keys =
            teamMembers
            |> List.map (fun person -> string person.id)
            |> System.Text.Json.JsonSerializer.Serialize

        fveTable {
            _id "team-member-selection"
            _ariaLabel "Selectable team members"
            _dataSignals "{tableMemberSelection:[]}"
            _dataOn ("fve-selection-clear", "$tableMemberSelection = []")

            _dataEffect
                "el.dispatchEvent(new CustomEvent('fve-table-selection-change', {bubbles:true, detail:{keys:Array.from($tableMemberSelection)}}))"

            output {
                _role "status"
                _ariaLive "polite"
                _class "sr-only"
                _dataText "$tableMemberSelection.length + ' selected'"
                "0 selected"
            }

            fveTableHeader {
                fveTableRow {
                    fveTableHead {
                        fveCheckbox {
                            _class "fve-table-checkbox"
                            _ariaLabel "Select all rows on this page"

                            _dataEffect
                                $"el.checked = $tableMemberSelection.length === {teamMembers.Length}; el.indeterminate = $tableMemberSelection.length > 0 && !el.checked"

                            _dataOn ("change", $"$tableMemberSelection = el.checked ? {keys} : []")
                        }
                    }

                    fveTableHead { "Name" }
                    fveTableHead { "Email" }
                    fveTableHead { "Role" }
                }
            }

            fveTableBody {
                for person in teamMembers do
                    let key = string person.id

                    fveTableRow {
                        _dataAttr ("data-selected", $"$tableMemberSelection.includes('{key}') ? 'true' : 'false'")

                        fveTableCell {
                            fveCheckbox {
                                _name "memberIds"
                                _value key
                                _class "fve-table-checkbox"
                                _ariaLabel ("Select " + person.name)
                                _dataEffect $"el.checked = $tableMemberSelection.includes('{key}')"

                                _dataOn (
                                    "change",
                                    $"$tableMemberSelection = el.checked ? [...$tableMemberSelection.filter(key => key !== '{key}'), '{key}'] : $tableMemberSelection.filter(key => key !== '{key}')"
                                )
                            }
                        }

                        fveTableHead {
                            _scope "row"
                            person.name
                        }

                        fveTableCell { person.email }
                        fveTableCell { person.role }
                    }
            }
        }

    let mobileTeamTable =
        fveTable {
            _ariaLabel "Team members as mobile records"
            _mobileLayout MobileLayout.Records
            teamTableHeader
            fveTableBody { teamTableRows teamMembers }
        }

    let emptyTeamTable =
        fveTable {
            _ariaLabel "Empty team members"
            teamTableHeader
            fveTableBody { () }

            fveTableEmpty {
                fveEmptyState {
                    _label "No team members"
                    _description "Team members will appear here when added."
                }
            }
        }

    let infiniteTeamMembers =
        [ for number in 1..24 ->
              { id = number
                name = $"Team member {number}"
                email = $"member{number}@fve.meiermade.com"
                role = "Member" } ]

    let infiniteTeamRows offset =
        fragment {
            for person in infiniteTeamMembers |> List.skip offset |> List.truncate 8 do
                fveTableRow {
                    _id $"infinite-person-{person.id}"

                    fveTableHead {
                        _scope "row"
                        person.name
                    }

                    fveTableCell { person.email }
                    fveTableCell { person.role }
                }

            let next = min (offset + 8) infiniteTeamMembers.Length

            if next < infiniteTeamMembers.Length then
                fveTableRow {
                    _id $"people-more-{next}"

                    _dataOnIntersect (
                        [ "once" ],
                        $"@get('/components/table/more?offset={next}', {{filterSignals: {{exclude: /.*/}}, retry: 'never'}})"
                    )

                    fveTableCell {
                        _colspan 3

                        span {
                            _role "status"
                            "Loading more members…"
                        }
                    }
                }
        }

    let infiniteTeamTable =
        fveTable {
            _id "infinite-team-table"
            _ariaLabel "More team members"
            _class "h-64"
            _scrollRows true
            teamTableHeader
            fveTableBody { infiniteTeamRows 0 }
        }

    type TeamMemberSort =
        | NameAscending
        | NameDescending
        | RoleAscending
        | RoleDescending

    let teamMemberSortFromQuery sort direction =
        match sort, direction with
        | "name", "desc" -> NameDescending
        | "role", "asc" -> RoleAscending
        | "role", "desc" -> RoleDescending
        | _ -> NameAscending

    let private teamMemberSortUrl sort direction =
        $"/components/table?sort={sort}&direction={direction}"

    let private sortFor column current =
        match column, current with
        | "name", NameAscending
        | "role", RoleAscending -> "ascending", "desc"
        | "name", NameDescending
        | "role", RoleDescending -> "descending", "asc"
        | _ -> "unsorted", "asc"

    let private teamSortHeading (column: string) (heading: string) current =
        let state, next = sortFor column current

        fveTableHead {
            if state <> "unsorted" then
                _ariaSort state

            a {
                _href (teamMemberSortUrl column next)
                _class "fve-table-sort-control"
                _ariaLabel $"Sort by {heading}; currently {state}"

                _dataOn (
                    "click",
                    "if (evt.button === 0 && !evt.metaKey && !evt.ctrlKey && !evt.shiftKey && !evt.altKey) { evt.preventDefault(); @get(el.getAttribute('href').replace('/components/table?', '/components/table/sort?'), { filterSignals: { exclude: /.*/ }, retry: 'never' }) }"
                )

                heading

                span {
                    _ariaHidden true

                    if state = "ascending" then "↑"
                    elif state = "descending" then "↓"
                    else "↕"
                }
            }
        }

    let sortableTeamTable current =
        let members =
            match current with
            | NameAscending -> teamMembers |> List.sortBy _.name
            | NameDescending -> teamMembers |> List.sortByDescending _.name
            | RoleAscending -> teamMembers |> List.sortBy _.role
            | RoleDescending -> teamMembers |> List.sortByDescending _.role

        fveTable {
            _ariaLabel "Sortable team members"
            _mobileLayout MobileLayout.Records

            fveTableHeader {
                fveTableRow {
                    teamSortHeading "name" "Name" current
                    fveTableHead { "Email" }
                    teamSortHeading "role" "Role" current
                }
            }

            fveTableBody { teamTableRows members }
        }

    let sortableTeamTablePreview current =
        div {
            _id "components-table-sorting-preview"
            sortableTeamTable current
        }

    let private tableExamples current =
        [ sample
              "table"
              "Default"
              [ "TeamMember"
                "teamMembers"
                "teamTableHeader"
                "teamTableRows"
                "simpleTeamTable" ]
              (detailsSurface simpleTeamTable)
          |> note
              "Import Assets/table.js from your application entry. Table creates a custom-element host; rows and cells remain native HTML. Row hover highlighting is off by default."
          sample
              "table-comfortable"
              "Comfortable rows"
              [ "TeamMember"
                "teamMembers"
                "teamTableHeader"
                "teamTableRows"
                "comfortableTeamTable" ]
              (detailsSurface comfortableTeamTable)
          sample
              "table-hover"
              "Hover highlighting"
              [ "TeamMember"; "teamMembers"; "teamTableHeader"; "teamTableRows"; "hoverTeamTable" ]
              (detailsSurface hoverTeamTable)
          |> note "Use _hoverRows true to opt in. The optional --fve-table-row-hover token changes only this table. By default, hover composites a subtle dark/light wash into the table’s opaque background; sticky cells reuse the same color. Selected rows keep their selected background, and keyboard focus remains independent."
          sample
              "table-status"
              "With status values"
              [ "TeamMember"; "teamMembers"; "memberStatusTable" ]
              (detailsSurface memberStatusTable)
          sample
              "table-selection"
              "With checkboxes"
              [ "TeamMember"; "teamMembers"; "selectableTeamTable" ]
              (detailsSurface selectableTeamTable)
          sample
              "table-scrollable"
              "Scrollable rows and sticky headings"
              [ "TeamMember"
                "teamMembers"
                "teamTableHeader"
                "teamTableRows"
                "scrollableTeamTable" ]
              (detailsSurface scrollableTeamTable)
          sample
              "table-mobile"
              "Stacked on mobile"
              [ "TeamMember"
                "teamMembers"
                "teamTableHeader"
                "teamTableRows"
                "mobileTeamTable" ]
              (detailsSurface mobileTeamTable)
          sample
              "table-sorting"
              "Sortable records"
              [ "TeamMember"
                "teamMembers"
                "teamTableRows"
                "TeamMemberSort"
                "teamMemberSortUrl"
                "sortFor"
                "teamSortHeading"
                "sortableTeamTable" ]
              (detailsSurface (sortableTeamTablePreview current))
          sample
              "table-hierarchy"
              "Hierarchical items"
              [ "HierarchyItem"; "hierarchyItems"; "hierarchicalItemTable" ]
              (detailsSurface hierarchicalItemTable)
          sample "table-row-actions" "Row actions"
              [ "RowActionAccount"; "rowActionAccounts"; "moreActionsIcon"; "rowActionMenu"; "rowActionsTable" ]
              (connectedGallerySurface (div { _class "p-3 sm:p-6"; rowActionsTable }))
          |> note "Compose Button and Dropdown menu in an actions column; Table does not own record operations. The optional installation links below lead to their focused component recipes."
          sample "table-empty" "Empty state" [ "teamTableHeader"; "emptyTeamTable" ] (detailsSurface emptyTeamTable)
          serverSample
              "table-infinite"
              "Infinite scroll"
              [ "TeamMember"
                "teamTableHeader"
                "infiniteTeamMembers"
                "infiniteTeamRows"
                "infiniteTeamTable"
                "tableMoreHandler"
                "tableMoreRoutes" ]
              (detailsSurface infiniteTeamTable)
          |> note
              "Scroll the rows. The loading row replaces itself with the next batch; the final batch has no loading row." ]

    let examples () = tableExamples NameAscending


    let private galleryContent : Content =
        { description = "Render typed tabular data with responsive layouts, row headers, selection, hierarchy, and actions."
          usage = []
          sections = []
          apiCategory = Some "table" }

    let tablePageFor sort =
        gallery tableRegistration galleryContent (tableExamples sort)

    let tablePage = tablePageFor NameAscending
