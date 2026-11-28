namespace Example.Specification.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.UI.Views
open Example.Specification.Web
open type Html

open Presentation

module internal Server =
    let contents =
        [ "responsibility", "Responsibility"
          "dependencies", "Dependencies"
          "contract", "Contract"
          "requirements", "Requirements" ]

    let render () =
        contracts
            "HTTP surfaces"
            "Example.Application hosts Marketing and the application on one origin. Product/ adapts routes to the shared product views. Example.Specification owns review routes and fixture selection, Example.Documentation owns the resource guides and endpoint reference, and Example.Presentation hosts the deck for the FVE gallery. Each host declares its own HTTP pipeline and serves its own browser assets."
            [ "Example.Domain and Example.UI; Giraffe at the HTTP edge." ]
            "// GET /examples/application/accounts\n// GET /examples/application/accounts/{id}\n// POST /examples/application/accounts/new\n// POST /examples/application/accounts/{id}/edit\n// POST /examples/application/accounts/{id}/delete"
            [ "GET requests SHALL render the product selected by the URL."
              "POST requests SHALL accept same-origin URL-encoded bodies of at most 64,000 bytes. Failed editors SHALL return entered values and field errors only in a private, no-store response. URLs and cookies SHALL NOT retain submitted private values."
              "Collection choices SHALL compose with OR within a filter and AND between filters. Search, filters, sorting and workspace SHALL remain URL-backed; selected record keys SHALL NOT enter URLs."
              "Deletions SHALL use protected, browser- and workspace-scoped session state without changing shared seed records. Restore records SHALL reset only the selected workspace."
              "Browser form endpoints and illustrative JSON API contracts SHALL remain distinguishable." ]
