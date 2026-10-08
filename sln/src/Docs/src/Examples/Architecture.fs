namespace Docs.Examples

open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open type Html

module Architecture =
    let navigation =
        [ "architecture", "System context"
          "architecture/solution", "Overview"
          "architecture/solution/domain", "Ledger.Domain"
          "architecture/solution/application", "Ledger.Application"
          "architecture/solution/components", "Acme.Components"
          "architecture/solution/server", "Ledger.Server" ]
    let href page = "/examples/specification/"+page
    let private click node page = $"  click {node} href \"{href page}\" _self"
    let private graph lines =
        String.concat "\n" (lines @ [
            "  classDef person fill:#f3f4f6,stroke:#6b7280,color:#111827"
            "  classDef system fill:#059669,stroke:#047857,color:#ffffff,stroke-width:2px"
            "  classDef project fill:#ecfdf5,stroke:#059669,color:#064e3b"
            "  classDef executable fill:#d1fae5,stroke:#047857,color:#064e3b,stroke-width:2px"
            "  classDef navigable stroke:#059669,stroke-width:3px" ])
    let private diagram label source =
        div { _role "region"; _ariaLabel label; _tabindex 0; _class "min-w-0 overflow-x-auto rounded-xl focus-visible:outline-2 focus-visible:outline-[var(--fve-brand-ring)]"; Mermaid.create source |> Mermaid.withC4 |> Mermaid.render }
    let private rules (items:string list) = Layout.documentationSection "rules" "Rules" (Layout.prose (ul { for item in items do li { item } }))
    let private context = graph [
        "flowchart LR"
        "  Operator[\"Financial operator<br/>Person<br/>Reviews accounts and activity\"]:::person"
        "  Developer[\"Application developer<br/>Person<br/>Reviews workflows and HTTP contracts\"]:::person"
        "  Ledger[\"Ledger<br/>Software system<br/>Financial application, executable specification and API reference\"]:::system"
        "  Operator -->|Uses financial pages| Ledger"
        "  Developer -->|Reviews specification and cURL examples| Ledger"
        "  class Ledger navigable"
        click "Ledger" "architecture/solution" ]
    let private solution = graph [
        "flowchart LR"
        "  Browser[\"Browser<br/>External runtime\"]:::person"
        "  subgraph Product[\"Ledger solution\"]"
        "    Server[\"Ledger.Server<br/>Executable F# project<br/>HTML, forms, Specification and API reference\"]:::executable"
        "    Application[\"Ledger.Application<br/>F# library project<br/>Submission validation and selection review\"]:::project"
        "    Domain[\"Ledger.Domain<br/>F# library project<br/>Financial and workspace facts, balance and deletion rules\"]:::project"
        "    Components[\"Acme.Components<br/>Source-installed F# project<br/>Reusable HTML controls and presentation\"]:::project"
        "    Server -->|Project reference| Application"
        "    Server -->|Project reference| Components"
        "    Application -->|Project reference| Domain"
        "  end"
        "  Browser -.->|HTTP GET and POST| Server"
        "  class Server,Application,Domain,Components navigable"
        click "Server" "architecture/solution/server"
        click "Application" "architecture/solution/application"
        click "Domain" "architecture/solution/domain"
        click "Components" "architecture/solution/components" ]
    let private contracts heading (description:string) (dependencies:string list) code requirements =
        div {
            _class "grid gap-12"
            Layout.documentationSection "responsibility" "Responsibility" (Layout.prose (p { description }))
            Layout.documentationSection "dependencies" "Dependencies" (Layout.prose (ul { for dependency in dependencies do li { dependency } }))
            Layout.documentationSection "contract" heading (CodeBlock.create "fsharp" code |> CodeBlock.render)
            rules requirements
        }
    let contents page =
        match page with
        | "architecture" -> ["system-context","System context";"system-boundary","System boundary";"rules","Rules"]
        | "architecture/solution" -> ["projects","Projects";"project-inventory","Project inventory";"rules","Rules"]
        | _ -> ["responsibility","Responsibility";"dependencies","Dependencies";"contract","Contract";"rules","Rules"]
    let render page =
        match page with
        | "architecture" ->
            div {
                _class "grid gap-12"
                Layout.documentationSection "system-context" "System context" (diagram "Ledger system context" context)
                Layout.documentationSection "system-boundary" "System boundary" (Layout.prose (p { "Ledger provides a financial workspace, an executable specification and a cURL API reference over one shared set of USD account and transaction fixtures." }))
                rules ["The three surfaces SHALL describe the same accounts, transactions and workspace contexts.";"Financial submissions SHALL validate finite outcomes without durably retaining private values or mutating the seeded records.";"The Ledger node SHALL open the Solution overview."]
            }
        | "architecture/solution" ->
            div {
                _class "grid gap-12"
                Layout.documentationSection "projects" "Projects" (diagram "Ledger project dependencies and HTTP boundary" solution)
                Layout.documentationSection "project-inventory" "Project inventory" (DescriptionList.create [
                    DescriptionListItem.create "Ledger.Domain" (Layout.link (href "architecture/solution/domain") "Financial types and invariants")
                    DescriptionListItem.create "Ledger.Application" (Layout.link (href "architecture/solution/application") "Validation operations and outcomes")
                    DescriptionListItem.create "Acme.Components" (Layout.link (href "architecture/solution/components") "Installed HTML building blocks")
                    DescriptionListItem.create "Ledger.Server" (Layout.link (href "architecture/solution/server") "HTTP hosting and the three rendered surfaces") ] |> DescriptionList.render)
                rules ["Compile-time references SHALL flow Server → Application → Domain; Server SHALL also reference Components.";"Domain and Application SHALL remain independent of Giraffe, HTML and browser state.";"Ledger.Server SHALL be the only executable project. Example.fsproj SHALL compile that assembly.";"Solid relationships SHALL represent project references; the dashed relationship SHALL represent HTTP requests."]
            }
        | "architecture/solution/domain" ->
            contracts "Financial types" "Domain/Domain.fs owns immutable financial fixtures, organization/environment/ledger context, balance calculations and deletion eligibility." ["System and FSharp.Core only."]
                "type Account = { id:int; name:string; accountType:AccountType; currency:string; openingBalance:decimal }\ntype Transaction = { id:int; date:DateOnly; description:string; accountId:int; amount:decimal; status:TransactionStatus }\ntype Workspace = { organization:Organization; environment:Environment; ledger:Ledger }\n// balance : Account -> decimal\n// canDeleteAccount : int -> bool"
                ["Account balances SHALL equal opening balance plus the account's signed transactions.";"Deletion eligibility SHALL require an existing account, zero balance and no transactions.";"Every ledger SHALL belong to its selected organization. Unknown context values SHALL resolve to available fixtures."]
        | "architecture/solution/application" ->
            contracts "Operations" "UseCases/Operations.fs validates account, organization and profile submissions, checks selection keys and evaluates account deletion." ["Ledger.Domain."]
                "// validateAccount : ValidateAccountRequest -> Result<ValidateAccountResponse, ValidateAccountError>\n// reviewSelection : ReviewSelectionRequest -> Result<ReviewSelectionResponse, ReviewSelectionError>\n// validateDeletion : ValidateDeletionRequest -> Result<ValidateDeletionResponse, ValidateDeletionError>\n// validateOrganization : ValidateOrganizationRequest -> Result<ValidateOrganizationResponse, ValidateOrganizationError>\n// validateProfile : ValidateProfileRequest -> Result<ValidateProfileResponse, ValidateProfileError>"
                ["Operations SHALL return typed success or validation errors without HTTP status codes or rendered HTML.";"Account names SHALL contain 1–80 characters and be unique ignoring case.";"Selection review SHALL reject empty or unknown keys.";"Validation SHALL neither mutate seeded data nor retain submitted values."]
        | "architecture/solution/components" ->
            contracts "Composition" "Components/Acme.Components.fsproj contains independently installed source for the controls used by the three surfaces." ["FSharp.ViewEngine and each selected component's declared dependencies."]
                "PageTopBar.create ()\n|> PageTopBar.withBrand brand\n|> PageTopBar.withContent breadcrumbs\n|> PageTopBar.withActions actions\n|> PageTopBar.render\n\nSideNav.create id label\n|> SideNav.withContent (SideNavContent.create groups)\n|> SideNav.render resolve"
                ["Components SHALL own their presentation, accessibility and local interaction behavior.";"The consumer SHALL own financial content, routes, theme and workflow state.";"The full-width PageTopBar SHALL own the shell's shared header height and separator."]
        | "architecture/solution/server" ->
            contracts "HTTP surfaces" "Example.fsproj builds Ledger.Server. Model.fs and Routing.fs own URLs; Layout/Application own shared product HTML; Specification/Architecture and ApiDocumentation render the other surfaces; Hosting.fs adapts native forms to Application operations." ["Ledger.Application and Acme.Components; Giraffe at the HTTP edge."]
                "// GET /examples/application/accounts\n// GET /examples/application/accounts/{id}\n// GET /examples/specification/accounts/view-accounts\n// GET /examples/specification/accounts/view-account?resource=101\n// POST /examples/application/accounts/new\n// POST /examples/application/accounts/{id}/edit\n// POST /examples/application/accounts/{id}/delete\n// GET /examples/api-documentation"
                ["GET requests SHALL render the product selected by the URL.";"Specification App-mode links SHALL retain workflow, fixture identity, workspace and docking context.";"POST requests SHALL accept same-origin URL-encoded bodies of at most 64,000 bytes. Failed account editors SHALL return entered values and field errors only in a private, no-store response; other outcomes SHALL resolve finite redirects without submitted private values.";"Browser form endpoints and illustrative JSON API contracts SHALL remain distinguishable."]
        | _ -> invalidArg (nameof page) "Choose a registered architecture page."
