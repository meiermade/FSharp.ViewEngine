namespace Docs.Pages

open Docs.Common

module Registry =
    let navigation =
        [ { label = "Getting started"
            pages = [ Home.page; Installation.page; CoreGuides.firstView ]
            sections = [] }
          { label = "Core concepts"
            pages = [ CoreGuides.elementsAndAttributes; CoreGuides.composition; CoreGuides.rendering; CoreGuides.encoding; CoreGuides.accessibility; Custom.page ]
            sections = [] }
          { label = "Integrations"
            pages = [ Usage.page; Svg.page; Datastar.page; Htmx.page; Alpine.page; TailwindElements.page ]
            sections = [] }
          { label = "FSharp.ViewEngine.Docs"
            pages = [ Showcase.overviewRegistration ]
            sections =
                [ { label = "Components"
                    pages = Showcase.componentRegistrations
                    sections = [] }
                  { label = "Page examples"
                    pages = Showcase.pageExampleRegistrations
                    sections = [] } ] }
          { label = "Project"
            pages = [ Benchmarks.page; Changelog.page ]
            sections = [] } ]

    let rec private sectionPages section =
        section.pages @ (section.sections |> List.collect sectionPages)

    let all = navigation |> List.collect sectionPages

    let aliases =
        all
        |> List.collect (fun page -> page.aliases |> List.map (fun alias -> alias, page.path))
