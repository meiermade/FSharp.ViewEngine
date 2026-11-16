namespace Example.Server.ArchitecturePages

open FSharp.ViewEngine
open Example.Components
open Example.Components.Views
open Example.Server
open type Html

open Presentation

module internal Server =
    let contents =
        [ "responsibility", "Responsibility"
          "dependencies", "Dependencies"
          "contract", "Contract"
          "rules", "Rules" ]

    let render () =
        contracts
            "HTTP surfaces"
            "Example.Server.fsproj builds the standalone Giraffe host. Config.fs and Services.fs construct host settings and ephemeral demo-session protection. Common/ owns HTTP parsing, document/SSE responses and form boundaries. Product/ feature handlers invoke Application use cases and supply the shared Example.Components/Views renderers. Specification/ owns workflow fixtures and documentation chrome, reusing the same product rendering and submissions. Marketing/, ApiDocumentation/ and Presentation/ own their routes and presentation. Host.fs configures middleware/assets; Program.fs composes routes and starts the host."
            [ "Example.Application and Example.Components; Giraffe at the HTTP edge." ]
            "// GET /examples/application/accounts\n// GET /examples/application/accounts/{id}\n// GET /examples/specification/accounts/view-accounts\n// GET /examples/specification/accounts/view-account?resource=101\n// POST /examples/application/accounts/new\n// POST /examples/application/accounts/{id}/edit\n// POST /examples/application/accounts/{id}/delete\n// GET /examples/api-documentation"
            [ "GET requests SHALL render the product selected by the URL."
              "Specification App-mode links SHALL retain workflow, fixture identity, workspace and docking context."
              "POST requests SHALL accept same-origin URL-encoded bodies of at most 64,000 bytes. Failed account editors SHALL return entered values and field errors only in a private, no-store response; other outcomes SHALL resolve finite redirects without submitted private values."
              "Browser form endpoints and illustrative JSON API contracts SHALL remain distinguishable." ]
