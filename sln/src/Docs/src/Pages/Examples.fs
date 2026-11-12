namespace Docs.Pages

open System
open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

module Examples =
    [<RequireQualifiedAccess>]
    type Template =
        | Marketing
        | Presentation
        | Application
        | Specification
        | ApiDocumentation

    type ExampleTemplate =
        { template: Template
          name: string
          path: string
          description: string
          screenshot: string
          darkScreenshot: string }

    let templates =
        [ { template = Template.Marketing
            name = "Marketing"
            path = "/examples/marketing"
            description = "A complete Geldos product site with responsive navigation, product visuals, hosting options, pricing and FAQs."
            screenshot = "/images/examples/marketing.png"
            darkScreenshot = "/images/examples/marketing-dark.png" }
          { template = Template.Presentation
            name = "Presentation"
            path = "/examples/presentation"
            description = "An HTML treasury review with separated slides, an expand/restore viewer, standalone F# authoring and Print/Save as PDF."
            screenshot = "/images/examples/presentation.png"
            darkScreenshot = "/images/examples/presentation-dark.png" }
          { template = Template.Specification
            name = "Specification"
            path = "/examples/specification"
            description =
              "The Docs-shaped Spec shell: readable articles, contents rails, wide workflow fixtures, App mode, sequence diagrams, and rules."
            screenshot = "/images/examples/specification.png"
            darkScreenshot = "/images/examples/specification-dark.png" }
          { template = Template.Application
            name = "Application"
            path = "/examples/application"
            description =
              "A financial application with Home, Accounts, Transactions, workspace selection, Settings, and Profile."
            screenshot = "/images/examples/application.png"
            darkScreenshot = "/images/examples/application-dark.png" }
          { template = Template.ApiDocumentation
            name = "API documentation"
            path = "/examples/api-documentation"
            description =
              "A resource-grouped API reference with descriptions and parameters beside cURL requests and JSON responses."
            screenshot = "/images/examples/api-documentation.png"
            darkScreenshot = "/images/examples/api-documentation-dark.png" } ]

    let registration: DocPage =
        { id = "examples-gallery"
          path = "/examples"
          aliases = []
          navLabel = "Examples"
          category = "Examples"
          title = "Examples"
          browserTitle = "Examples · FSharp.ViewEngine"
          nodes = [] }

    let gallery =
        DocumentationPage.create registration.id registration.title
        |> DocumentationPage.withDescription
            "Complete source-authored examples, built around Geldos. Copy the pages and make the structure your own."
        |> DocumentationPage.withRightRail NoRail
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "templates"
                  "Templates"
                  [ div {
                        _class "grid min-w-0 gap-8 md:grid-cols-2"

                        for template in templates do
                            a {
                                _href template.path
                                _target "_blank"
                                _rel "noopener"
                                _ariaLabel (template.name + " example (opens in a new tab)")
                                _attr ("data-example-template", template.name)

                                _class
                                    "group grid min-w-0 content-start gap-4 rounded-xl focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[var(--fve-brand-ring)]"

                                for source, visibility in
                                    [ template.screenshot, "dark:hidden"
                                      template.darkScreenshot, "hidden dark:block" ] do
                                    img {
                                        _src source
                                        _alt (template.name + " template preview")
                                        _width "1200"
                                        _height "800"
                                        _class ("aspect-[3/2] w-full rounded-xl border border-[var(--fve-border)] object-cover object-top transition-shadow group-hover:shadow-lg " + visibility)
                                    }

                                div {
                                    _class "grid gap-2"

                                    h2 {
                                        _class "text-lg font-semibold text-[var(--fve-text)]"
                                        template.name
                                    }

                                    p {
                                        _class "text-sm leading-relaxed text-[var(--fve-muted-text)]"
                                        template.description
                                    }

                                    span {
                                        _class "text-sm font-semibold text-[var(--fve-brand-text)]"
                                        "Open example →"
                                    }
                                }
                            }
                    } ] ]

    let templateForPath (path: string) =
        templates
        |> List.tryFind (fun item -> path = item.path || path.StartsWith(item.path + "/", StringComparison.Ordinal))

    let paths =
        [ for item in templates do
              yield item.path
          yield! Example.Server.Marketing.paths
          yield!
              Example.Server.Application.navigation
              |> List.map fst
              |> List.filter ((<>) "/examples/application")
          for account in Ledger.Domain.accounts do
              yield Example.Components.Views.Model.applicationUrl (Example.Components.Views.Model.ApplicationPage.Account account.id)
              yield Example.Components.Views.Model.applicationUrl (Example.Components.Views.Model.ApplicationPage.EditAccount account.id)
              yield Example.Components.Views.Model.applicationUrl (Example.Components.Views.Model.ApplicationPage.DeleteAccount account.id)
          for environment in Ledger.Domain.environments do
              yield Example.Components.Views.Model.applicationUrl (Example.Components.Views.Model.ApplicationPage.Environment environment.id)
              yield Example.Components.Views.Model.applicationUrl (Example.Components.Views.Model.ApplicationPage.EditEnvironment environment.id)
          yield Example.Components.Views.Model.applicationUrl Example.Components.Views.Model.ApplicationPage.CreateAccount
          for transaction in Ledger.Domain.transactions do
              yield Example.Components.Views.Model.applicationUrl (Example.Components.Views.Model.ApplicationPage.Transaction transaction.id)
          yield!
              Example.Server.Specification.navigation
              |> List.map fst
              |> List.filter ((<>) "/examples/specification")
          yield!
              Example.Server.ApiDocumentation.navigation
              |> List.map fst
              |> List.filter ((<>) "/examples/api-documentation") ]
        |> List.distinct
