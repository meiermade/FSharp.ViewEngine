namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components.Documentation
open type Html
open type Datastar

/// Host-owned catalog organization, not reusable component or product fixture data.
module Catalog =
    type private Area =
        { overview: DocPage
          description: string
          sections: NavSection list
          planned: string list }

    let private overview name slug =
        { id = "catalog-" + slug
          path = "/components/" + slug
          aliases = []
          navLabel = "Overview"
          category = name
          title = name
          browserTitle = name + " · FSharp.ViewEngine.Components"
          nodes = [] }

    let private section label pages : NavSection =
        { label = label; pages = pages; sections = [] }

    let private areas =
        [ { overview = overview "Primitives" "primitives"
            description = "Shared controls, feedback, data display and layout foundations for every kind of interface."
            sections =
                [ section "Actions and feedback" Components.actionRegistrations
                  section "Data display" Components.dataDisplayRegistrations
                  section "Form controls" Components.formControlRegistrations
                  section "Navigation" Components.navigationRegistrations
                  section "Menus and overlays" Components.menuOverlayRegistrations
                  section "Layout foundations" [ Components.sectionRegistration; Components.browserRegistration; Components.phoneRegistration ] ]
            planned = [] }
          { overview = overview "Application" "application"
            description = "Compose application shells, pages, collections and matching record details from shared primitives."
            sections =
                [ section "Shells and pages" [ Components.appShellRegistration; Components.bottomNavigationRegistration; Components.pageRegistration; Components.pageTopBarRegistration; Components.pageHeaderRegistration ]
                  section "Collections and details" [ Components.collectionRegistration; Components.detailRegistration; Components.bulkActionsRegistration ]
                  section "Forms" [ Components.formLayoutsRegistration; Components.uploadRegistration ]
                  section "Workflows" [ Components.stepsRegistration; Components.firstStepsRegistration ]
                  section "Resources" [ Components.calendarRegistration; Components.mediaLibraryRegistration ]
                  section "Integration examples" Components.integrationExampleRegistrations ]
            planned = [] }
          { overview = Showcase.overviewRegistration
            description = "Documentation sites, API references, galleries and executable specifications using the same Components package."
            sections =
                [ section "Components" Showcase.componentRegistrations
                  section "Page examples" Showcase.pageExampleRegistrations ]
            planned = [] } ]

    let navigation =
        areas
        |> List.map (fun area ->
            { label = area.overview.title
              pages = [ area.overview ]
              sections = area.sections })

    let private linkCard (href:string) (title:string) (description:string) (action:string) =
        let destination = System.Text.Json.JsonSerializer.Serialize href
        a {
            _href href
            _dataOn ("click", $"window.fsharpDocsNavigation.navigate(evt, {destination})")
            _class "docs-catalog-card"
            strong { title }
            span { _class "docs-catalog-description"; description }
            span { _class "docs-catalog-action"; action; " →" }
        }

    let overviewPage =
        DocumentationPage.create Components.overviewRegistration.id "Components" |> DocumentationPage.withDescription "Accessible, server-rendered Tailwind components for applications and documentation." |> DocumentationPage.withSections [
            DocumentationSection.create "areas" "Explore the library" [
                div {
                    _class "docs-catalog-grid"
                    for area in areas do
                        let description =
                            if List.isEmpty area.planned then area.description
                            else "In development. " + area.description
                        linkCard area.overview.path area.overview.title description "Explore area"
                } ]
            DocumentationSection.create "start" "Get started" [
                CodeBlock.create "shell" "dotnet add package FSharp.ViewEngine.Components" |> CodeBlock.render
                p { _class "spec-paragraph"; "The engine and Components version independently. Delivered areas share one Components package; there are no family packages." }
                p {
                    _class "spec-paragraph"
                    a { _class "spec-content-link"; _href "/components/installation"; "Installation" }
                    " · "
                    a { _class "spec-content-link"; _href "/components/theming"; "Themes" }
                    " · "
                    a { _class "spec-content-link"; _href "/components/tailwind-css"; "Tailwind CSS" }
                } ]
            DocumentationSection.create "principles" "Ordinary typed F# composition" [
                ul {
                    li { "Required inputs stay visible. Accessible labels, names, destinations and content belong in constructors." }
                    li { "Optional behavior is piped. Immutable modifiers add variants, selection, themes and attributes." }
                    li { "Custom content stays HTML. Cells, dialogs, actions and page content remain HtmlElement values." }
                    li { "Closed choices are typed. Variants, tones, density, selected values and destinations are not visual strings." }
                } ] ]

    let private areaPage area =
        DocumentationPage.create area.overview.id area.overview.title |> DocumentationPage.withDescription area.description |> DocumentationPage.withSections [
            if not (List.isEmpty area.planned) then
                DocumentationSection.create "availability" "Not yet implemented" [
                    p { "This area is part of the current integrated library work. Its reusable components and connected example are not available yet; the planned coverage below is not a working demo." }
                    ul { for item in area.planned do li { item } }
                    p {
                        "Available now: "
                        a { _class "spec-content-link"; _href "/components/primitives"; "shared primitives" }
                        " and "
                        a { _class "spec-content-link"; _href "/components/application"; "Application compositions" }
                        "."
                    } ]
            else
                for group in area.sections do
                    DocumentationSection.create (group.label.ToLowerInvariant().Replace(" ", "-")) group.label [
                        div {
                            _class "docs-catalog-grid"
                            for page in group.pages do
                                let description =
                                    Components.examplesFor (page.path.Substring("/components/".Length))
                                    |> List.map _.title
                                    |> String.concat " · "
                                linkCard page.path page.title description "View examples"
                        } ]
                if area.overview.title = "Application" then
                    DocumentationSection.create "composition-ownership" "Composition ownership" [
                        p { "AppShell owns the responsive frame and one main landmark. Page owns route-local width, navigation, gutters and scrolling; PageHeader supplies the visible page heading. Collection and Detail compose the body without another shell." } ]
                    DocumentationSection.create "connected-experience" "Connected Application experience" [
                        p { "The App shell example connects the financial workspace, collections, matching record details, forms, actions, reports and settings through consumer-owned destinations. Browser and Phone can present that same application with optional App mode; Application does not own the Documentation viewer." } ] ]

    let private pages =
        [ yield Components.overviewRegistration.path, overviewPage
          for area in areas do
              if area.overview.path <> Showcase.overviewRegistration.path then
                  yield area.overview.path, areaPage area ]
        |> Map.ofList

    let tryPage path = Map.tryFind path pages
