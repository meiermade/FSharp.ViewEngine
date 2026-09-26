namespace Acme.DocumentationConsumer

open FSharp.ViewEngine
open Acme.Documentation.Primitives
open Acme.Documentation.Documentation
open type Html

[<RequireQualifiedAccess>]
type Destination =
    | Home

module Consumer =
    let private navigation =
        [ Nav.page "home" "Home" "/docs" Destination.Home ]

    let private browserFixture =
        Browser.create (main { h1 { "Generated Documentation consumer" } })
        |> Browser.withAddress "/docs"
        |> Fixture.browser "documentation-consumer" "Documentation consumer" "/docs"

    let private page =
        DocumentationPage.create "home" "Generated Documentation consumer"
        |> DocumentationPage.withDescription "Compiles every API promised by the documentation aggregate."
        |> DocumentationPage.withSections [
            DocumentationSection.create "overview" "Overview" [ p { "Consumer-owned Documentation source." } ] ]
        |> DocumentationPage.withFixtures [ browserFixture ]

    let private search =
        [ DocsSearchEntry.create "/docs" page [ "generated" ] ]
        |> DocsSearch.index

    let private site =
        DocsSite.create "Acme Docs" "home"
        |> DocsSite.withNavigation navigation
        |> DocsSite.withSearch search

    let document =
        Document.create site page
        |> Document.render

    let sequenceSource =
        let author = SequenceDiagram.participant "Author" "Author"
        let docs = SequenceDiagram.participant "Docs" "Documentation"
        SequenceDiagram.sequence [ author; docs ] [ SequenceDiagram.call author docs "Render" ]
        |> SequenceDiagram.render

    let graphIssues =
        DirectedGraph.validate
            { nodes = [ "source"; "page" ]
              roots = [ "source" ]
              edges = [ "source", "page" ] }

    let targetHref =
        Target.create Destination.Home
        |> Target.withQuery "theme" "system"
        |> Target.withFragment "overview"
        |> Target.href (function Destination.Home -> "/docs")

    let apiReference =
        Operation.create DocsHttpMethod.GET "/v1/render"
        |> Operation.withDescription "Render typed HTML."
        |> Operation.withParameters [ Parameter.create "theme" "string" DocsParameterLocation.Query ]
        |> Operation.withResponses [ Response.create "200" |> Response.withExample "html" "<main>Rendered</main>" ]
        |> Operation.render

    let example =
        Example.gallery "documentation-example" "Documentation example" "fsharp" "main { h1 { \"Hello\" } }" (main { h1 { "Hello" } })

    let registryIssues =
        [ DocsRegisteredPage.create "/docs" [] page ]
        |> DocsRegistry.validate navigation

    let versionSelector =
        [ DocsVersion.create "2026.9" "/docs/2026.9" ]
        |> VersionView.selector "2026.9"
