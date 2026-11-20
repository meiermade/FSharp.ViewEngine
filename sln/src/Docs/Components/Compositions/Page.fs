namespace Docs.ComponentPages.Compositions

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

module Page =
    let private pageExampleBuildingBlocks =
        function
        | Docs.ComponentPages.Compositions.View.DependencyGraph ->
            [ "app-shell", "App shell"
              "page", "Page"
              "input", "Input"
              "description-list", "Description list"
              "badge", "Badge" ]
        | Docs.ComponentPages.Compositions.View.ExecutionDetail ->
            [ "app-shell", "App shell"
              "page", "Page"
              "table", "Table"
              "description-list", "Description list"
              "notice", "Notice" ]
        | Docs.ComponentPages.Compositions.View.FinancialReporting ->
            [ "app-shell", "App shell"
              "page", "Page"
              "metric", "Metric"
              "table", "Table" ]
        | Docs.ComponentPages.Compositions.View.Messaging ->
            [ "app-shell", "App shell"
              "page", "Page"
              "side-nav", "Side nav"
              "textarea", "Textarea" ]
        | Docs.ComponentPages.Compositions.View.Operations ->
            [ "app-shell", "App shell"
              "page", "Page"
              "metric", "Metric"
              "table", "Table"
              "floating-panel", "Floating panel" ]
        | Docs.ComponentPages.Compositions.View.Scheduling ->
            [ "app-shell", "App shell"
              "page", "Page"
              "day-calendar", "Day calendar"
              "week-calendar", "Week calendar"
              "month-calendar", "Month calendar"
              "year-calendar", "Year calendar"
              "description-list", "Description list" ]
        | Docs.ComponentPages.Compositions.View.MediaManagement ->
            [ "app-shell", "App shell"
              "page", "Page"
              "media-library", "Media library"
              "input", "Input"
              "textarea", "Textarea"
              "file-selection", "File selection" ]

    let pageExamplePageFor page query =
        let example =
            { id = "components-" + Docs.ComponentPages.Compositions.View.slug page
              title = Docs.ComponentPages.Compositions.View.title page + " workspace"
              source = Docs.ComponentPages.Compositions.View.source page
              preview = connectedGallerySurface (Docs.ComponentPages.Compositions.View.preview page query)
              note = None }

        gallery (Docs.ComponentPages.Compositions.View.registration page) { description = ""; usage = []; sections = []; apiCategory = None } [ example ]
        |> DocumentationPage.withFixtures [ Docs.ComponentPages.Compositions.View.fixture page query ]
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  example.id
                  example.title
                  [ fveExample {
                        _name example.id
                        _label example.title
                        _variant ExampleVariant.Gallery
                        _language "fsharp"
                        fveExampleCode { example.source }
                        example.preview
                    } ]
              DocumentationSection.create
                  "building-blocks"
                  "Built with"
                  [ pageExampleBuildingBlocks page
                    |> List.map (fun (path, label) -> "/components/" + path, label)
                    |> buildingBlockLinks ((Docs.ComponentPages.Compositions.View.title page) + " building blocks") ] ]
