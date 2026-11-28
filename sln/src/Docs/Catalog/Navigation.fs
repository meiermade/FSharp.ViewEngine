namespace Docs.Pages

open Docs.Common
open FSharp.ViewEngine
open FSharp.ViewEngine.Components
open FSharp.ViewEngine.Components.Templates
open type Html

/// The library distributes components, not application/documentation frameworks.
module Catalog =
    let private group label pages : NavSection =
        { label = label
          pages = pages
          sections = [] }

    let navigation =
        [ group "Actions" Docs.Catalog.ComponentRegistry.actionRegistrations
          group
              "Feedback"
              (Docs.Catalog.ComponentRegistry.feedbackRegistrations
               @ [ Docs.ComponentPages.Callout.View.calloutRegistration ])
          group
              "Data display"
              (Docs.Catalog.ComponentRegistry.dataDisplayRegistrations
               @ [ Docs.ComponentPages.Card.View.cardRegistration
                   Docs.ComponentPages.Message.View.messageRegistration ])
          group
              "Form controls"
              (Docs.Catalog.ComponentRegistry.formControlRegistrations
               @ [ Docs.ComponentPages.FieldGroup.View.fieldGroupRegistration ])
          group "Navigation" (Docs.Catalog.ComponentRegistry.navigationRegistrations @ [ Docs.ComponentPages.BottomNavigation.View.bottomNavigationRegistration ])
          group "Overlays" Docs.Catalog.ComponentRegistry.overlayRegistrations
          group
              "Layout"
              (Docs.Catalog.ComponentRegistry.frameRegistrations
               @ [ Docs.ComponentPages.PageHeader.View.pageHeaderRegistration
                   Docs.ComponentPages.SectionHeader.View.sectionHeaderRegistration
                   Docs.ComponentPages.Section.View.sectionRegistration
                   Docs.ComponentPages.Disclosure.View.disclosureRegistration
                   Docs.ComponentPages.PageTopBar.View.pageTopBarRegistration ])
          group
              "Code and diagrams"
              [ Docs.ComponentPages.CodeBlock.View.codeBlockRegistration
                Docs.ComponentPages.Example.View.exampleRegistration
                Docs.ComponentPages.Mermaid.View.mermaidRegistration
                Docs.ComponentPages.FSharpApiReference.View.fsharpReferenceRegistration ] ]

    let componentRegistrations = navigation |> List.collect _.pages

    let private overviewDescription =
        "Opinionated, Tailwind-styled components for F#, with Datastar-powered interactivity."

    let overviewPage =
        DocumentationPage.create Docs.Catalog.ComponentRegistry.overviewRegistration.id "Components"
        |> DocumentationPage.withDescription overviewDescription
        |> DocumentationPage.withDescriptionContent (fragment {
            overviewDescription
            " Inspired by "
            a {
                _href "https://ui.shadcn.com/"
                _class "font-semibold text-[var(--fve-brand-text)] underline underline-offset-2"
                "shadcn/ui"
            }
            "’s source-first approach, components are installed into your project as editable F# and JavaScript source. Use the defaults, or customize the code when needed."
        })
        |> DocumentationPage.withSections
            [ DocumentationSection.create
                  "start"
                  "Get started"
                  [ fveCodeBlock {
                        _language "shell"

                        "dotnet fve init src/Acme.Components/Acme.Components.fsproj --frontend-root ../Acme.Web\ndotnet fve add button input --config src/Acme.Components/fve.json"
                    }
                    p {
                        "Styles use "

                        a {
                            _href "https://tailwindcss.com/"
                            _class "text-[var(--fve-brand-text)] underline underline-offset-2"
                            "Tailwind CSS"
                        }

                        "; interactivity uses "

                        a {
                            _href "https://data-star.dev/"
                            _class "text-[var(--fve-brand-text)] underline underline-offset-2"
                            "Datastar"
                        }

                        ". The CLI copies the source and runtime assets you need; your application owns the CSS and JavaScript builds."
                    }
                    p {
                        fveLink { _href "/components/installation"; _underline LinkUnderline.Always; "Installation" }
                        " · "
                        fveLink { _href "/guides/theming"; _underline LinkUnderline.Always; "Theming" }
                        " · "
                        fveLink { _href "/examples"; _underline LinkUnderline.Always; "Complete examples" }
                    } ]
              for section in navigation do
                  DocumentationSection.create
                      (section.label.ToLowerInvariant().Replace(" ", "-"))
                      section.label
                      [ div {
                            _class "docs-catalog-grid"

                            for item in section.pages do
                                fveLink {
                                    _variant LinkVariant.Plain
                                    _href item.path
                                    _class "docs-catalog-card"
                                    strong { item.title }

                                    span {
                                        _class "docs-catalog-action"
                                        "View component →"
                                    }
                                }
                        } ] ]

    let tryPage path =
        if path = Docs.Catalog.ComponentRegistry.overviewRegistration.path then
            Some overviewPage
        else
            None
