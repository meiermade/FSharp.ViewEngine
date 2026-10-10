namespace Docs.Pages

open System
open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

module Examples =
    [<RequireQualifiedAccess>]
    type Template = Application | Specification | ApiDocumentation
    type ExampleTemplate = { template:Template; name:string; path:string; description:string; screenshot:string }
    let templates =
        [ { template=Template.Specification; name="Specification"; path="/examples/specification"; description="The Docs-shaped Spec shell: readable articles, contents rails, wide workflow fixtures, App mode, sequence diagrams, and rules."; screenshot="/images/examples/specification.png" }
          { template=Template.Application; name="Application"; path="/examples/application"; description="A financial application with Home, Accounts, Transactions, workspace selection, Settings, and Profile."; screenshot="/images/examples/application.png" }
          { template=Template.ApiDocumentation; name="API documentation"; path="/examples/api-documentation"; description="A resource-grouped API reference with descriptions and parameters beside cURL requests and JSON responses."; screenshot="/images/examples/api-documentation.png" } ]
    let registration : DocPage =
        { id="examples-gallery"; path="/examples"; aliases=[]; navLabel="Examples"; category="Examples"; title="Examples"; browserTitle="Examples · FSharp.ViewEngine"; nodes=[] }
    let gallery =
        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withDescription "Three complete source-authored templates. Copy the pages and make the structure your own."
        |> DocumentationPage.withRightRail NoRail
        |> DocumentationPage.withSections [DocumentationSection.create "templates" "Templates" [
            div {
                _class "grid min-w-0 gap-8 lg:grid-cols-3"
                for template in templates do
                    a {
                        _href template.path
                        _target "_blank"
                        _rel "noopener"
                        _ariaLabel (template.name+" example (opens in a new tab)")
                        _attr("data-example-template", template.name)
                        _class "group grid min-w-0 content-start gap-4 rounded-xl focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[var(--fve-brand-ring)]"
                        img { _src template.screenshot; _alt (template.name+" template preview"); _width "1200"; _height "800"; _class "aspect-[3/2] w-full rounded-xl border border-[var(--fve-border)] object-cover object-top transition-shadow group-hover:shadow-lg" }
                        div { _class "grid gap-2"; h2 { _class "text-lg font-semibold text-[var(--fve-text)]"; template.name }; p { _class "text-sm leading-relaxed text-[var(--fve-muted-text)]"; template.description }; span { _class "text-sm font-semibold text-[var(--fve-brand-text)]"; "Open example →" } }
                    }
            } ]]
    let sourceFiles = ["Domain/Domain.fs"; "Domain/Ledger.Domain.fsproj"; "UseCases/Operations.fs"; "UseCases/Ledger.Application.fsproj"; "Model.fs"; "Navigation.fs"; "Layout.fs"; "AppMode.fs"; "Application.fs"; "Architecture.fs"; "Specification.fs"; "ApiDocumentation.fs"; "Routing.fs"; "Hosting.fs"; "Program.fs"; "Example.fsproj"; "input.css"; "README.md"]
    let source file =
        if not (List.contains file sourceFiles) then invalidArg (nameof file) "Choose a template source file."
        SourceRegion.readEmbedded typeof<DocPage>.Assembly ("Docs.Examples."+file.Replace('/', '.'))
        |> _.Replace("FSharp.ViewEngine.Components", "Acme.Components", StringComparison.Ordinal)
    let templateForPath (path:string) = templates |> List.tryFind (fun item -> path=item.path || path.StartsWith(item.path+"/",StringComparison.Ordinal))
    let paths =
        [ for item in templates do yield item.path
          yield! Docs.Examples.Application.navigation |> List.map fst |> List.filter ((<>) "/examples/application")
          for account in Ledger.Domain.accounts do
              yield Docs.Examples.Model.applicationUrl (Docs.Examples.Model.ApplicationPage.Account account.id)
              yield Docs.Examples.Model.applicationUrl (Docs.Examples.Model.ApplicationPage.EditAccount account.id)
              yield Docs.Examples.Model.applicationUrl (Docs.Examples.Model.ApplicationPage.DeleteAccount account.id)
          yield Docs.Examples.Model.applicationUrl Docs.Examples.Model.ApplicationPage.CreateAccount
          for transaction in Ledger.Domain.transactions do yield Docs.Examples.Model.applicationUrl (Docs.Examples.Model.ApplicationPage.Transaction transaction.id)
          yield! Docs.Examples.Specification.navigation |> List.map fst |> List.filter ((<>) "/examples/specification")
          yield! Docs.Examples.ApiDocumentation.navigation |> List.map fst |> List.filter ((<>) "/examples/api-documentation") ] |> List.distinct
